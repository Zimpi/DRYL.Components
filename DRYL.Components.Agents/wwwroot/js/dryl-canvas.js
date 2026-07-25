// dryl-canvas.js — width bridge for DrylAiCanvas (lazy ES module, no consumer wiring).
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
        try { state.dotnet.invokeMethodAsync('OnWidthChanged', w)?.catch(() => { }); }
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
