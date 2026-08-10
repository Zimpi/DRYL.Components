// dryl-canvas.js — width bridge for DrylCanvas (lazy ES module, no consumer wiring).
// Reports the artifact body's usable inline size back to .NET so the artifact generator
// can budget its layout (column counts, label lengths, chart density) against the space
// the artifact actually has — which is the canvas element, not the viewport: on a wide
// desktop the canvas may still live in a narrow side panel.
//
// Chatty resize traffic would cost a circuit message per frame while the user drags a
// window, so a report only leaves the browser once the width has moved past DEADBAND.

const DEADBAND = 8;

const _obs = new WeakMap();

function widthOf(el, entry) {
    // contentBoxSize is the space actually available to children (padding excluded);
    // fall back to clientWidth on engines that only fill the legacy rect.
    const box = entry?.contentBoxSize;
    if (box) {
        const first = Array.isArray(box) ? box[0] : box;
        if (first && typeof first.inlineSize === 'number') return first.inlineSize;
    }
    return entry?.contentRect?.width ?? el.clientWidth;
}

export function observe(el, dotnet) {
    if (!el || !dotnet) return;
    unobserve(el);

    const state = { last: -1, dotnet };

    const report = (width) => {
        const w = Math.round(width);
        if (w <= 0 || Math.abs(w - state.last) < DEADBAND) return;
        state.last = w;
        // The circuit may already be gone while the observer still fires — a rejected
        // invoke here must not surface as an unhandled rejection.
        try { state.dotnet.invokeMethodAsync('OnWidthMeasured', w)?.catch(() => { }); }
        catch { /* disposed */ }
    };

    const ro = new ResizeObserver((entries) => {
        for (const entry of entries) report(widthOf(el, entry));
    });
    ro.observe(el);
    state.ro = ro;
    _obs.set(el, state);

    // First measurement without waiting for the observer's initial callback, so the
    // width is already known if a tool call lands in the same beat as the first render.
    report(el.clientWidth);
}

export function unobserve(el) {
    const state = el && _obs.get(el);
    if (!state) return;
    state.ro?.disconnect();
    _obs.delete(el);
}

// ── Reorder gesture ─────────────────────────────────────────────────────────
// Dragging a node among its siblings is a pointer loop — over a circuit that would be one
// message per frame, so JS owns the gesture and .NET hears exactly one result: the index the
// node was dropped at. The move itself is a normal CanvasOp, which means the existing FLIP
// layer glides every sibling into its new slot (one op, one movement).
//
// Nothing is inserted into or removed from the DOM Blazor owns: the drop marker is a
// data-attribute on a sibling, drawn by CSS.

const _drag = new WeakMap();

// The siblings of `el`: every [data-cid] whose nearest [data-cid] ancestor is the same one.
// Same anchor rule dryl.motion.autoFlip uses, so the DrylPresence wrappers in between — which
// make the nodes anything but DOM siblings — are irrelevant.
function siblingsOf(root, el) {
    const anchorOf = (node) => {
        for (let n = node.parentElement; n && n !== root; n = n.parentElement)
            if (n.hasAttribute('data-cid')) return n;
        return root;
    };
    const anchor = anchorOf(el);
    return [...root.querySelectorAll('[data-cid]')].filter(n => anchorOf(n) === anchor);
}

function clearMarks(siblings) {
    for (const s of siblings) {
        s.removeAttribute('data-drop-before');
        s.removeAttribute('data-drop-after');
    }
}

export function initReorder(root, dotnet) {
    if (!root || !dotnet || _drag.has(root)) return;

    const state = { dotnet };

    const onDown = (e) => {
        const handle = e.target.closest?.('[data-drag-handle]');
        if (!handle || e.button !== 0) return;
        const el = handle.closest('[data-cid]');
        if (!el || !root.contains(el)) return;

        const siblings = siblingsOf(root, el);
        if (siblings.length < 2) return;

        const from = siblings.indexOf(el);
        const rects = siblings.map(s => s.getBoundingClientRect());
        // Which axis the siblings actually spread along — a stack is vertical, a grid row is not.
        const spreadX = Math.max(...rects.map(r => r.left)) - Math.min(...rects.map(r => r.left));
        const spreadY = Math.max(...rects.map(r => r.top)) - Math.min(...rects.map(r => r.top));
        const vertical = spreadY >= spreadX;

        const g = {
            el, siblings, from, to: from, vertical,
            startX: e.clientX, startY: e.clientY,
            centers: rects.map(r => (vertical ? r.top + r.height / 2 : r.left + r.width / 2)),
        };

        el.classList.add('is-dragging');
        try { handle.setPointerCapture(e.pointerId); } catch { /* stale pointer */ }
        e.preventDefault();

        const onMove = (ev) => {
            g.el.style.transform =
                `translate(${ev.clientX - g.startX}px, ${ev.clientY - g.startY}px)`;

            const pointer = g.vertical ? ev.clientY : ev.clientX;
            let to = 0;
            for (let i = 0; i < g.centers.length; i++)
                if (i !== g.from && g.centers[i] < pointer) to++;
            g.to = Math.min(to, g.siblings.length - 1);

            clearMarks(g.siblings);
            const marked = g.siblings[g.to];
            if (marked && marked !== g.el)
                marked.setAttribute(g.to > g.from ? 'data-drop-after' : 'data-drop-before', '');
        };

        const finish = (commit) => {
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
            window.removeEventListener('pointercancel', onCancel);
            window.removeEventListener('keydown', onKey);

            g.el.classList.remove('is-dragging');
            g.el.style.transform = '';
            clearMarks(g.siblings);

            if (!commit || g.to === g.from) return;
            const cid = g.el.getAttribute('data-cid');
            // The circuit may already be gone while the gesture was still running.
            try { state.dotnet.invokeMethodAsync('OnNodeReorder', cid, g.to)?.catch(() => { }); }
            catch { /* disposed */ }
        };

        const onUp = () => finish(true);
        const onCancel = () => finish(false);
        const onKey = (ev) => { if (ev.key === 'Escape') finish(false); };

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp);
        window.addEventListener('pointercancel', onCancel);
        window.addEventListener('keydown', onKey);
    };

    root.addEventListener('pointerdown', onDown);
    state.onDown = onDown;
    _drag.set(root, state);
}

export function disposeReorder(root) {
    const state = root && _drag.get(root);
    if (!state) return;
    root.removeEventListener('pointerdown', state.onDown);
    _drag.delete(root);
}
