// wwwroot/js/dryl.js — DRYL JS interop, namespaced under window.dryl.

window.dryl = window.dryl || {};

/* --------------------------------------------------------------
 * Storage — thin wrapper over localStorage used by DrylTable's
 * PersistStateKey. Returns null on any access failure (private
 * browsing, quota, disabled storage) so the C# side can fall back
 * to defaults without observing exceptions.
 * -------------------------------------------------------------- */
window.dryl.storage = {
    get(key) {
        try { return window.localStorage.getItem(key); }
        catch (_) { return null; }
    },
    set(key, value) {
        try { window.localStorage.setItem(key, value); }
        catch (_) { /* quota or disabled — silently ignore */ }
    },
    remove(key) {
        try { window.localStorage.removeItem(key); }
        catch (_) { /* ignore */ }
    }
};

/* --------------------------------------------------------------
 * Spotlight — track cursor on a card and expose the position via
 * CSS custom properties (--mx / --my). dryl.css picks them up to
 * render the spotlight glow.
 * -------------------------------------------------------------- */
window.dryl.spotlight = {
    track(el) {
        if (!el || el.__drylSpot) return;
        const onMove = (e) => {
            const r = el.getBoundingClientRect();
            el.style.setProperty('--mx', (e.clientX - r.left) + 'px');
            el.style.setProperty('--my', (e.clientY - r.top) + 'px');
        };
        el.addEventListener('mousemove', onMove);
        el.__drylSpot = onMove;
    },
    untrack(el) {
        if (!el || !el.__drylSpot) return;
        el.removeEventListener('mousemove', el.__drylSpot);
        delete el.__drylSpot;
    }
};

/* --------------------------------------------------------------
 * Download — client-side file download via a Blob URL. Used by
 * DrylTable's CSV export. No npm: builds a Blob, clicks a transient
 * <a download>, then revokes the URL. The C# caller is responsible for
 * any BOM (DrylTable prepends a UTF-8 BOM so Excel reads it correctly).
 * -------------------------------------------------------------- */
window.dryl.download = {
    text(filename, content, mime) {
        const blob = new Blob([content], {
            type: (mime || 'text/plain') + ';charset=utf-8;'
        });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename || 'download.txt';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },
    csv(filename, content) {
        this.text(filename || 'export.csv', content, 'text/csv');
    }
};

/* --------------------------------------------------------------
 * dryl.clipboard — copy text to the system clipboard.
 *   Prefers the async Clipboard API; falls back to a hidden
 *   <textarea> + execCommand for non-secure contexts. Returns a
 *   boolean so the caller can show copied/failed feedback.
 * -------------------------------------------------------------- */
window.dryl.clipboard = {
    async copy(text) {
        text = text ?? '';
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch { /* fall through to legacy path */ }

        try {
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.top = '-9999px';
            ta.setAttribute('readonly', '');
            document.body.appendChild(ta);
            ta.select();
            const ok = document.execCommand('copy');
            document.body.removeChild(ta);
            return ok;
        } catch {
            return false;
        }
    }
};

/* --------------------------------------------------------------
 * Modal — body scroll lock, focus trap and ESC handling for
 * DrylDialog / DrylDialogProvider.
 * -------------------------------------------------------------- */
window.dryl.modal = (() => {
    const FOCUSABLE =
        'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), ' +
        'select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
    let openCount = 0;

    function lockScroll() {
        if (openCount === 0) document.body.classList.add('dryl-scroll-locked');
        openCount++;
    }

    function unlockScroll() {
        openCount = Math.max(0, openCount - 1);
        if (openCount === 0) document.body.classList.remove('dryl-scroll-locked');
    }

    function focusable(root) {
        if (!root) return [];
        return Array.from(root.querySelectorAll(FOCUSABLE))
            .filter(el => el.offsetParent !== null || el === document.activeElement);
    }

    function attach(el, dotnetRef, options) {
        if (!el || el.__drylModal) return;

        const opts = options || {};
        const closeOnEscape = opts.closeOnEscape !== false;
        const trapFocus = opts.trapFocus !== false;
        const previouslyFocused = document.activeElement;

        const onKeyDown = (e) => {
            if (e.key === 'Escape' && closeOnEscape) {
                e.preventDefault();
                dotnetRef.invokeMethodAsync('OnEscape');
                return;
            }
            if (e.key === 'Tab' && trapFocus) {
                const items = focusable(el);
                if (items.length === 0) {
                    e.preventDefault();
                    return;
                }
                const first = items[0];
                const last = items[items.length - 1];
                const active = document.activeElement;
                if (e.shiftKey && (active === first || !el.contains(active))) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && (active === last || !el.contains(active))) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };

        el.addEventListener('keydown', onKeyDown);

        // Focus the first focusable element on open
        setTimeout(() => {
            const items = focusable(el);
            if (items.length > 0) items[0].focus();
            else el.focus();
        }, 0);

        el.__drylModal = { onKeyDown, previouslyFocused };
        lockScroll();
    }

    function detach(el) {
        if (!el || !el.__drylModal) return;
        const { onKeyDown, previouslyFocused } = el.__drylModal;
        el.removeEventListener('keydown', onKeyDown);
        delete el.__drylModal;
        unlockScroll();
        // Only hand focus back if it is still inside this dialog (or was lost
        // to the body) — a follow-up dialog may already own it, and stealing
        // it back would break that dialog's focus trap.
        const active = document.activeElement;
        const focusIsOurs = !active || active === document.body || el.contains(active);
        if (focusIsOurs && previouslyFocused && typeof previouslyFocused.focus === 'function') {
            try { previouslyFocused.focus(); } catch (_) { /* element gone */ }
        }
    }

    return { attach, detach };
})();

/* --------------------------------------------------------------
 * Menu — click-outside detection and keyboard navigation for
 * DrylMenu. The .NET component owns open/close state; JS only
 * handles the DOM-level concerns (event listening, focus).
 * -------------------------------------------------------------- */
window.dryl.menu = (() => {
    const ITEMS = '[role="menuitem"]:not([disabled]):not([aria-disabled="true"])';

    function attach(anchor, dotnetRef) {
        if (!anchor || anchor.__drylMenu) return;

        // Capture-phase listener: fires before any child click handlers.
        const onDocClick = (e) => {
            if (!anchor.contains(e.target)) {
                dotnetRef.invokeMethodAsync('Close');
            }
        };

        document.addEventListener('pointerdown', onDocClick, true);
        anchor.__drylMenu = { onDocClick };
    }

    function detach(anchor) {
        if (!anchor || !anchor.__drylMenu) return;
        const { onDocClick } = anchor.__drylMenu;
        document.removeEventListener('pointerdown', onDocClick, true);
        delete anchor.__drylMenu;
    }

    function focusPanel(panel) {
        if (!panel) return;
        const first = panel.querySelector(ITEMS);
        if (first) first.focus();
        else panel.focus();
    }

    function navigate(panel, direction) {
        if (!panel) return;
        const items = Array.from(panel.querySelectorAll(ITEMS));
        if (!items.length) return;
        const idx = items.indexOf(document.activeElement);
        let next;
        if (direction === 'first') {
            next = items[0];
        } else if (direction === 'last') {
            next = items[items.length - 1];
        } else if (direction === 'down') {
            next = items[idx < 0 ? 0 : Math.min(idx + 1, items.length - 1)];
        } else {
            next = items[idx < 0 ? items.length - 1 : Math.max(idx - 1, 0)];
        }
        next?.focus();
    }

    function focusTrigger(anchor) {
        if (!anchor) return;
        const sel = '.menu-trigger button:not([disabled]), .menu-trigger a, .menu-trigger [tabindex],'
                  + '.popover-trigger button:not([disabled]), .popover-trigger a, .popover-trigger [tabindex]';
        const btn = anchor.querySelector(sel);
        btn?.focus();
    }

    return { attach, detach, focusPanel, navigate, focusTrigger };
})();

/* --------------------------------------------------------------
 * Popover — portals an anchored panel to <body> and positions it
 * with position:fixed, so it escapes any ancestor that would clip
 * or re-anchor it: an overflow:hidden card, or the containing block
 * created by a backdrop-filter / transform on a glass surface.
 *
 *   open(anchor, panel, dotnetRef, opts)  — portal, position, listen
 *   close(anchor)                         — restore the panel, clean up
 *
 * Blazor still owns the panel node. open() drops a comment
 * placeholder at the panel's original slot and close() moves the
 * node back before Blazor removes it — so Blazor's diff never finds
 * the node missing from under its recorded parent.
 *
 * opts: { placement, matchWidth, closeOnOutside }
 *   placement — 'bottom-start' | 'bottom-end' | 'top-start' | 'top-end'
 * -------------------------------------------------------------- */
window.dryl.popover = (() => {
    const GAP  = 4;   // matches --sp-1: gap between trigger and panel
    const EDGE = 4;   // min viewport inset when clamping
    const state = new WeakMap(); // anchor -> { panel, onScroll, onResize, onDocClick }

    function place(anchor, panel, placement, matchWidth) {
        const a = anchor.getBoundingClientRect();
        if (matchWidth) panel.style.width = a.width + 'px';

        // Measure in the final (body) context, after any width is applied.
        const ph = panel.offsetHeight;
        const pw = panel.offsetWidth;
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        const onTop = placement.startsWith('top');

        let top  = onTop ? (a.top - ph - GAP) : (a.bottom + GAP);
        let left = placement.endsWith('end') ? (a.right - pw) : a.left;

        // Flip to the opposite side only if the preferred side overflows
        // the viewport and the opposite side has room.
        if (onTop && top < EDGE && (a.bottom + GAP + ph) <= vh) {
            top = a.bottom + GAP;
        } else if (!onTop && (top + ph) > (vh - EDGE) && (a.top - GAP - ph) >= EDGE) {
            top = a.top - ph - GAP;
        }

        // Keep the panel inside the viewport horizontally.
        left = Math.max(EDGE, Math.min(left, vw - pw - EDGE));

        panel.style.top  = top + 'px';
        panel.style.left = left + 'px';
    }

    function open(anchor, panel, dotnetRef, opts) {
        if (!anchor || !panel || state.has(anchor)) return;
        opts = opts || {};
        const placement      = opts.placement || 'bottom-start';
        const matchWidth     = !!opts.matchWidth;
        const closeOnOutside = opts.closeOnOutside !== false;

        // Portal to <body> so the panel escapes any ancestor overflow or
        // backdrop-filter/transform containing block (e.g. a glass card).
        // Blazor still owns this node — it lives in the always-rendered panel
        // wrapper and is never structurally removed, so moving it is safe.
        document.body.appendChild(panel);

        const reposition = () => place(anchor, panel, placement, matchWidth);
        reposition();
        // Reveal only now that it is correctly placed (see the two-key
        // .is-open.is-positioned gate in DrylPopover.razor.css).
        panel.classList.add('is-positioned');

        // Capture-phase scroll catches scrolling in any ancestor container.
        const onScroll = () => reposition();
        const onResize = () => reposition();
        window.addEventListener('scroll', onScroll, true);
        window.addEventListener('resize', onResize);

        let onDocClick = null;
        if (closeOnOutside) {
            // Outside = neither the trigger anchor nor the portaled panel.
            onDocClick = (e) => {
                if (!anchor.contains(e.target) && !panel.contains(e.target)) {
                    dotnetRef.invokeMethodAsync('Close');
                }
            };
            document.addEventListener('pointerdown', onDocClick, true);
        }

        state.set(anchor, { panel, onScroll, onResize, onDocClick });
    }

    function close(anchor) {
        const s = state.get(anchor);
        if (!s) return;
        window.removeEventListener('scroll', s.onScroll, true);
        window.removeEventListener('resize', s.onResize);
        if (s.onDocClick) document.removeEventListener('pointerdown', s.onDocClick, true);

        // Return the panel to its original slot (it is the anchor's last child)
        // and clear the styles/marker applied while portaled.
        s.panel.classList.remove('is-positioned');
        s.panel.style.top = '';
        s.panel.style.left = '';
        s.panel.style.width = '';
        anchor.appendChild(s.panel);

        state.delete(anchor);
    }

    return { open, close };
})();

/* --------------------------------------------------------------
 * Toast — auto-dismiss timer and exit animation lifecycle.
 *
 * Timer: setTimeout statt CSS-animationend, damit Blazor-Re-Renders
 * (die das .toast-progress-Element patchen können) den Timer nicht
 * zurücksetzen. Hover-Pause wird explizit per mouseenter/mouseleave
 * auf dem stabilen slot-Element gehandelt.
 *
 * Exit: Event-Delegation auf slot (von Blazor per @key erhalten),
 * da animationend bubbled — robuster als direkter Listener auf dem
 * inneren .toast-div, das Blazor bei Klassenänderungen patcht.
 *
 *   OnExpired       — fired by setTimeout after the duration.
 *   OnExitFinished  — fired when toast-out animationend bubbles to slot.
 * -------------------------------------------------------------- */
window.dryl.toast = (() => {
    function attach(slot, dotnetRef) {
        if (!slot || slot.__drylToast) return;
        const toast = slot.querySelector('.toast');
        if (!toast) return;

        // Lese die Duration aus dem inline-style des Progress-Elements.
        const progress = toast.querySelector('.toast-progress');
        let remaining  = 0;
        let startedAt  = 0;
        let timerId    = null;

        if (progress) {
            const m = (progress.getAttribute('style') || '')
                .match(/animation-duration:\s*([\d.]+)ms/);
            if (m) remaining = parseFloat(m[1]);
        }

        function startTimer() {
            if (remaining <= 0) return;
            startedAt = Date.now();
            timerId   = setTimeout(
                () => dotnetRef.invokeMethodAsync('OnExpired'),
                remaining
            );
        }

        function pauseTimer() {
            if (timerId === null) return;
            clearTimeout(timerId);
            timerId   = null;
            remaining = Math.max(0, remaining - (Date.now() - startedAt));
        }

        // Hover-Listener auf stabilem slot-Element (nicht .toast, das gepatcht werden kann).
        let onMouseEnter = () => {};
        let onMouseLeave = () => {};
        if (remaining > 0) {
            onMouseEnter = pauseTimer;
            onMouseLeave = startTimer;
            slot.addEventListener('mouseenter', onMouseEnter);
            slot.addEventListener('mouseleave', onMouseLeave);
            startTimer();
        }

        // Exit-Detection per Event-Delegation auf slot — animationend bubbled,
        // slot wird von Blazor via @key stabil gehalten.
        const onSlotAnim = (e) => {
            if (e.animationName === 'toast-out') {
                clearTimeout(timerId);
                timerId = null;
                dotnetRef.invokeMethodAsync('OnExitFinished');
            }
        };
        slot.addEventListener('animationend', onSlotAnim);

        slot.__drylToast = {
            onSlotAnim,
            onMouseEnter,
            onMouseLeave,
            cancel: () => { clearTimeout(timerId); timerId = null; }
        };
    }

    function detach(slot) {
        if (!slot || !slot.__drylToast) return;
        const { onSlotAnim, onMouseEnter, onMouseLeave, cancel } = slot.__drylToast;
        cancel();
        slot.removeEventListener('animationend', onSlotAnim);
        slot.removeEventListener('mouseenter', onMouseEnter);
        slot.removeEventListener('mouseleave', onMouseLeave);
        delete slot.__drylToast;
    }

    return { attach, detach };
})();

// ── Autocomplete helpers ──────────────────────────────────────────────────────
window.dryl.autocomplete = {
    scrollOptionIntoView(listbox, index) {
        if (!listbox) return;
        const items = listbox.querySelectorAll('[role="option"]');
        if (items[index]) items[index].scrollIntoView({ block: 'nearest' });
    }
};

// ── DatePicker helpers ────────────────────────────────────────────────────────
window.dryl.datepicker = {
    focusDay(panel, dayNumber) {
        if (!panel) return;
        const btn = panel.querySelector(`[data-day="${dayNumber}"]`);
        btn?.focus();
    }
};

/* --------------------------------------------------------------
 * CommandPalette — global Ctrl+K hotkey + result list scrolling.
 *
 * attachGlobal(dotnetRef) — registers a document-level keydown
 *   listener for Ctrl+K (or Cmd+K on macOS). When triggered outside
 *   an input or textarea, calls dotnetRef.invokeMethodAsync('OnGlobalOpen').
 *   Uses a WeakMap so multiple DrylCommandPalette instances can
 *   coexist without duplicate or leaked listeners.
 *
 * detachGlobal(dotnetRef) — removes the listener for this ref.
 *
 * focusInput(inputEl) — focuses the search <input> via rAF so the
 *   browser has finished painting the overlay before focus is set.
 *
 * scrollItemIntoView(listEl, itemId) — scrolls the result row with
 *   the given id to the nearest visible edge.
 * -------------------------------------------------------------- */
window.dryl.commandpalette = (() => {
    const _listeners = new WeakMap();

    function attachGlobal(dotnetRef) {
        if (!dotnetRef || _listeners.has(dotnetRef)) return;
        const fn = (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                const tag = document.activeElement?.tagName?.toLowerCase();
                if (tag === 'input' || tag === 'textarea') return;
                e.preventDefault();
                dotnetRef.invokeMethodAsync('OnGlobalOpen');
            }
        };
        document.addEventListener('keydown', fn);
        _listeners.set(dotnetRef, fn);
    }

    function detachGlobal(dotnetRef) {
        if (!dotnetRef) return;
        const fn = _listeners.get(dotnetRef);
        if (fn) {
            document.removeEventListener('keydown', fn);
            _listeners.delete(dotnetRef);
        }
    }

    function focusInput(inputEl) {
        if (!inputEl) return;
        requestAnimationFrame(() => { try { inputEl.focus(); } catch (_) {} });
    }

    function scrollItemIntoView(listEl, itemId) {
        if (!listEl || !itemId) return;
        listEl.querySelector(`#${CSS.escape(itemId)}`)?.scrollIntoView({ block: 'nearest' });
    }

    // Move the backdrop element to <body> so it is always in the root stacking
    // context, regardless of where <DrylCommandPalette> is placed in the DOM tree.
    // Blazor tracks elements by reference (not parent position), so it can still
    // diff and remove the element correctly after re-parenting.
    function portal(el) {
        if (!el || el.parentNode === document.body) return;
        document.body.appendChild(el);
    }

    return { attachGlobal, detachGlobal, focusInput, scrollItemIntoView, portal };
})();

// ── File Upload — drag-over visual feedback ──────────────────────────────────
window.dryl.fileupload = (() => {
    const _map = new WeakMap();

    function attach(el, dotnetRef) {
        if (!el) return;
        let counter = 0;

        function onDragEnter(e) {
            e.preventDefault();
            counter++;
            if (counter === 1) dotnetRef.invokeMethodAsync('SetDragActive', true);
        }
        function onDragLeave(e) {
            e.preventDefault();
            counter = Math.max(0, counter - 1);
            if (counter === 0) dotnetRef.invokeMethodAsync('SetDragActive', false);
        }
        function onDragOver(e) { e.preventDefault(); }
        function onDrop(e) {
            e.preventDefault();
            counter = 0;
            dotnetRef.invokeMethodAsync('SetDragActive', false);
        }

        el.addEventListener('dragenter', onDragEnter);
        el.addEventListener('dragleave', onDragLeave);
        el.addEventListener('dragover',  onDragOver);
        el.addEventListener('drop',      onDrop);

        _map.set(el, { onDragEnter, onDragLeave, onDragOver, onDrop });
    }

    function detach(el) {
        if (!el) return;
        const handlers = _map.get(el);
        if (!handlers) return;
        el.removeEventListener('dragenter', handlers.onDragEnter);
        el.removeEventListener('dragleave', handlers.onDragLeave);
        el.removeEventListener('dragover',  handlers.onDragOver);
        el.removeEventListener('drop',      handlers.onDrop);
        _map.delete(el);
    }

    return { attach, detach };
})();

// ── OTP — digit-box focus management and paste distribution ──────────────────
window.dryl.otp = (() => {
    const _map = new WeakMap();

    function getInputs(container) {
        return container ? Array.from(container.querySelectorAll('input')) : [];
    }

    function focusNext(container, idx) {
        const inputs = getInputs(container);
        if (idx + 1 < inputs.length) inputs[idx + 1].focus();
    }

    function focusPrev(container, idx) {
        const inputs = getInputs(container);
        if (idx > 0) inputs[idx - 1].focus();
    }

    function attach(container, dotnetRef) {
        if (!container) return;
        detach(container);

        const onPaste = (e) => {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData).getData('text') || '';
            const inputs = getInputs(container);
            const digits = text.replace(/\D/g, '').split('').slice(0, inputs.length);
            // Fill inputs in the DOM immediately for visual feedback
            inputs.forEach((inp, i) => { inp.value = digits[i] ?? ''; });
            // Focus the first unfilled box (or last if all filled)
            const focusIdx = Math.min(digits.length, inputs.length - 1);
            inputs[focusIdx]?.focus();
            // Notify Blazor
            dotnetRef.invokeMethodAsync('SetDigits', digits);
        };

        container.addEventListener('paste', onPaste);
        _map.set(container, { onPaste });
    }

    function detach(container) {
        if (!container) return;
        const h = _map.get(container);
        if (!h) return;
        container.removeEventListener('paste', h.onPaste);
        _map.delete(container);
    }

    return { attach, detach, focusNext, focusPrev };
})();

// ── TimePicker — click-outside and scroll-to-active ──────────────────────────
window.dryl.timepicker = (() => {
    const _map = new WeakMap();

    function attach(anchor, dotnetRef) {
        if (!anchor) return;
        detach(anchor);

        const onClick = (e) => {
            if (!anchor.contains(e.target))
                dotnetRef.invokeMethodAsync('Close');
        };

        // Use setTimeout so the click that opened the panel is not immediately
        // caught by this listener.
        const timerId = setTimeout(() => {
            document.addEventListener('click', onClick, { capture: true });
        }, 0);

        _map.set(anchor, { onClick, timerId });
    }

    function detach(anchor) {
        if (!anchor) return;
        const h = _map.get(anchor);
        if (!h) return;
        clearTimeout(h.timerId);
        document.removeEventListener('click', h.onClick, { capture: true });
        _map.delete(anchor);
    }

    function scrollToActive(panel) {
        if (!panel) return;
        panel.querySelectorAll('.time-col').forEach(col => {
            const selected = col.querySelector('.is-selected');
            if (selected) selected.scrollIntoView({ block: 'nearest' });
        });
    }

    return { attach, detach, scrollToActive };
})();

// ── InputMask — format-on-input with cursor preservation ─────────────────────
window.dryl.inputmask = (() => {
    const _map = new WeakMap();

    // Strip value down to only the raw data chars (digits / letters) that
    // correspond to placeholder positions (#, A) in the pattern.
    function stripToData(value, pattern) {
        let data = '';
        let pi = 0;
        for (const c of value) {
            // Advance to next placeholder position
            while (pi < pattern.length && pattern[pi] !== '#' && pattern[pi] !== 'A') pi++;
            if (pi >= pattern.length) break;
            if (pattern[pi] === '#' && /\d/.test(c))            { data += c;             pi++; }
            else if (pattern[pi] === 'A' && /[a-zA-Z]/.test(c)) { data += c.toUpperCase(); pi++; }
            // Non-matching char: skip in value without advancing pi
        }
        return data;
    }

    // Re-apply the mask pattern to a clean data string.
    // Literal separators are inserted only when followed by more data.
    function applyMask(data, pattern) {
        let out = '';
        let di = 0;
        for (let pi = 0; pi < pattern.length && di < data.length; pi++) {
            const pc = pattern[pi];
            if (pc === '#') {
                out += data[di++];
            } else if (pc === 'A') {
                out += data[di++].toUpperCase();
            } else {
                // Literal separator: include only when there's more data to follow
                if (di < data.length) out += pc;
                else break;
            }
        }
        return out;
    }

    function process(el, pattern, dotnetRef) {
        const data = stripToData(el.value, pattern);
        const masked = applyMask(data, pattern);
        if (el.value !== masked) {
            el.value = masked;
        }
        // Place cursor after the last typed data char in the masked string
        el.setSelectionRange(masked.length, masked.length);
        dotnetRef.invokeMethodAsync('OnMaskedValue', masked);
    }

    function attach(el, pattern, dotnetRef) {
        if (!el || !pattern) return;
        detach(el);

        const onInput = () => process(el, pattern, dotnetRef);

        const onPaste = (e) => {
            e.preventDefault();
            const pasted = (e.clipboardData || window.clipboardData).getData('text') || '';
            const combined = el.value.slice(0, el.selectionStart) + pasted + el.value.slice(el.selectionEnd);
            const data = stripToData(combined, pattern);
            const masked = applyMask(data, pattern);
            el.value = masked;
            el.setSelectionRange(masked.length, masked.length);
            dotnetRef.invokeMethodAsync('OnMaskedValue', masked);
        };

        el.addEventListener('input', onInput);
        el.addEventListener('paste', onPaste);
        _map.set(el, { onInput, onPaste });
    }

    function detach(el) {
        if (!el) return;
        const h = _map.get(el);
        if (!h) return;
        el.removeEventListener('input', h.onInput);
        el.removeEventListener('paste', h.onPaste);
        _map.delete(el);
    }

    return { attach, detach };
})();

/* ──────────────────────────────────────────────────────────
 * dryl.chat — DrylChat scroll + composer helpers.
 *   scrollToEnd(el)             pins a scroll region to the bottom.
 *   attachComposer(el, ref)     Enter sends (Shift+Enter = newline),
 *                               textarea auto-grows on input.
 *   detachComposer(el)          cleans up listeners.
 * ────────────────────────────────────────────────────────── */
window.dryl.chat = (() => {
    const _map = new WeakMap();

    function scrollToEnd(el) {
        if (el) el.scrollTop = el.scrollHeight;
    }

    function autoGrow(el) {
        el.style.height = 'auto';
        el.style.height = el.scrollHeight + 'px';
    }

    function attachComposer(el, dotnetRef) {
        if (!el) return;
        detachComposer(el);

        const onKeyDown = (e) => {
            if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                dotnetRef.invokeMethodAsync('SubmitFromJs');
            }
        };
        const onInput = () => autoGrow(el);

        el.addEventListener('keydown', onKeyDown);
        el.addEventListener('input', onInput);
        _map.set(el, { onKeyDown, onInput });
        autoGrow(el);
    }

    function detachComposer(el) {
        if (!el) return;
        const h = _map.get(el);
        if (!h) return;
        el.removeEventListener('keydown', h.onKeyDown);
        el.removeEventListener('input', h.onInput);
        _map.delete(el);
    }

    function resize(el) {
        if (el) autoGrow(el);
    }

    return { scrollToEnd, attachComposer, detachComposer, resize };
})();

/* ──────────────────────────────────────────────────────────
 * dryl.keynav — shared navigation-key helper.
 *   Prevents the default page-scroll for the navigation keys so a
 *   Blazor @onkeydown handler can move roving focus / a highlight.
 *   Tab, Enter and Escape are left untouched so focus can still
 *   leave the widget and activation/dismissal work normally.
 *   Used by DrylTreeView (alias dryl.tree) and DrylSelect.
 * ────────────────────────────────────────────────────────── */
window.dryl.keynav = (() => {
    const _map = new WeakMap();
    const navKeys = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

    function attach(el) {
        if (!el) return;
        detach(el);
        const onKey = (e) => { if (navKeys.has(e.key)) e.preventDefault(); };
        el.addEventListener('keydown', onKey);
        _map.set(el, { onKey });
    }

    function detach(el) {
        if (!el) return;
        const h = _map.get(el);
        if (!h) return;
        el.removeEventListener('keydown', h.onKey);
        _map.delete(el);
    }

    return { attach, detach };
})();

/* dryl.tree — backwards-compatible alias of dryl.keynav (DrylTreeView). */
window.dryl.tree = window.dryl.keynav;

/* ──────────────────────────────────────────────────────────
 * dryl.table — small helpers for DrylTable.
 *   focusGrip(root, index): after a keyboard row-move, restore focus to the
 *   reorder grip handle now sitting at the row's new position so repeated
 *   Alt+Arrow presses keep working. Deferred to the next frame so the moved
 *   DOM node exists before we focus it.
 * ────────────────────────────────────────────────────────── */
window.dryl.table = {
    focusGrip(root, index) {
        if (!root) return;
        requestAnimationFrame(() => {
            const btn = root.querySelector('.tbl-grip[data-grip-index="' + index + '"]');
            if (btn) { try { btn.focus(); } catch (_) { /* element gone */ } }
        });
    },
    // focusFirstEditor(root): after a cell/row enters inline-edit mode, move focus to the
    // first form control inside the editing row so the user can type immediately and so
    // Enter/Escape (handled on the row) reach the keyboard. Deferred a frame so the editor
    // template has been rendered into the DOM before we try to focus it.
    focusFirstEditor(root) {
        if (!root) return;
        requestAnimationFrame(() => {
            const row = root.querySelector('tr.tbl-row--editing');
            if (!row) return;
            const editor = row.querySelector(
                '.tbl-td-editing input, .tbl-td-editing select, .tbl-td-editing textarea, ' +
                '.tbl-td-editing [tabindex]:not([tabindex="-1"])');
            if (editor) {
                try {
                    editor.focus();
                    if (typeof editor.select === 'function') editor.select();
                } catch (_) { /* element gone */ }
            }
        });
    },

    // focusHeader(root, key): restore focus to a column header's clickable region after a
    // keyboard column-move so repeated Alt+Arrow presses keep working.
    focusHeader(root, key) {
        if (!root) return;
        const sel = (window.CSS && CSS.escape) ? CSS.escape(key) : key;
        requestAnimationFrame(() => {
            const th = root.querySelector('th[data-col-key="' + sel + '"]');
            const target = th && (th.querySelector('.tbl-th-clickable') || th);
            if (target) { try { target.focus(); } catch (_) { /* element gone */ } }
        });
    },

    // initColumnResize(root, dotnet): one delegated pointerdown listener on the stable table root.
    // When it lands on a .tbl-col-resize grip we live-resize the owning <th> (and its body cells,
    // matched by data-col-key) until pointerup, then report the final width back to .NET so it can
    // store + persist it. No Blazor re-render happens mid-drag, so JS owns the width until release.
    initColumnResize(root, dotnet) {
        if (!root || root.__drylResizeAttached) return;
        root.__drylResizeAttached = true;

        let active = false, th = null, key = null, startX = 0, startW = 0, lastW = 0, bodyCells = null;
        const esc = (k) => (window.CSS && CSS.escape) ? CSS.escape(k) : k;

        const onMove = (e) => {
            if (!active) return;
            lastW = Math.max(48, startW + (e.clientX - startX));
            th.style.width = lastW + 'px';
            if (bodyCells) bodyCells.forEach(c => { c.style.width = lastW + 'px'; });
        };
        const onUp = () => {
            if (!active) return;
            active = false;
            root.classList.remove('tbl-resizing');
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
            if (dotnet && lastW > 0) {
                try { dotnet.invokeMethodAsync('OnColumnResized', key, lastW); } catch (_) { /* circuit gone */ }
            }
        };
        const onDown = (e) => {
            const grip = e.target.closest && e.target.closest('.tbl-col-resize');
            if (!grip || !root.contains(grip)) return;
            th = grip.closest('th');
            if (!th) return;
            key = grip.getAttribute('data-col-key');
            const table = th.closest('table');
            bodyCells = table ? Array.from(table.querySelectorAll('td[data-col-key="' + esc(key) + '"]')) : null;
            active = true;
            startX = e.clientX;
            startW = th.offsetWidth;
            lastW = startW;
            root.classList.add('tbl-resizing');
            e.preventDefault();
            e.stopPropagation();
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp);
        };

        root.__drylResizeDown = onDown;
        root.addEventListener('pointerdown', onDown);
    },

    disposeColumnResize(root) {
        if (!root || !root.__drylResizeAttached) return;
        if (root.__drylResizeDown) root.removeEventListener('pointerdown', root.__drylResizeDown);
        root.__drylResizeAttached = false;
        root.__drylResizeDown = null;
    },

    // layoutPinned(root): measure cumulative widths of pinned columns and set the sticky left/right
    // offset on the matching header + body cells (by column index, which lines up because thead and
    // tbody share the same leading/trailing structural columns). Re-run after every render; pure
    // horizontal scrolling is CSS-only and doesn't re-render, so the offsets stay put while scrolling.
    layoutPinned(root) {
        if (!root) return;
        requestAnimationFrame(() => {
            const table = root.querySelector('table.tbl');
            if (!table) return;
            const headRow = table.querySelector('thead tr');
            if (!headRow) return;
            const ths = Array.from(headRow.children);

            const lefts = {}, rights = {};
            let leftAcc = 0;
            for (let i = 0; i < ths.length; i++) {
                if (ths[i].dataset.pin === 'start') { lefts[i] = leftAcc; leftAcc += ths[i].offsetWidth; }
            }
            let rightAcc = 0;
            for (let i = ths.length - 1; i >= 0; i--) {
                if (ths[i].dataset.pin === 'end') { rights[i] = rightAcc; rightAcc += ths[i].offsetWidth; }
            }

            const applyRow = (cells) => {
                for (let i = 0; i < cells.length; i++) {
                    if (lefts[i] !== undefined) cells[i].style.left = lefts[i] + 'px';
                    else if (rights[i] !== undefined) cells[i].style.right = rights[i] + 'px';
                }
            };
            applyRow(ths);
            table.querySelectorAll('tbody tr').forEach(tr => {
                const cells = Array.from(tr.children);
                if (cells.length === ths.length) applyRow(cells); // skip colspan (empty/group/detail) rows
            });
        });
    }
};

/* ──────────────────────────────────────────────────────────
 * dryl.motion — the shared motion layer.
 *
 * Three concerns, all reduced-motion aware (a user who prefers
 * reduced motion gets the end state with no animation):
 *
 *   Presence (exit) — onExit/clearExit defer a node's removal until
 *     its CSS exit animation finishes, generalising the toast pattern
 *     so DrylPresence can animate-out any single child.
 *
 *   Indicator glide — moveIndicator measures the active child in a
 *     container and positions a shared [data-dryl-ink] element; CSS
 *     transitions it on --ease-spring. Used by DrylTabs whose tabs are
 *     variable-width (DrylSegmentedControl glides in pure CSS instead,
 *     because its segments are equal-width).
 *
 *   Reveal — observe/unobserve drive an IntersectionObserver that adds
 *     .is-revealed when an element scrolls into view, with optional
 *     per-child stagger. Carries DrylReveal.
 * ────────────────────────────────────────────────────────── */
window.dryl.motion = (() => {
    const _exit   = new WeakMap(); // wrapper el -> animationend handler
    const _ind    = new WeakMap(); // container  -> { ro }
    const _reveal = new WeakMap(); // wrapper el -> { io }

    const reduced = () =>
        !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);

    /* ---- Presence: exit lifecycle ----------------------------------
     * onExit(el, ref, opts) fires OnExitFinished when an exit animation ends.
     *   opts.name — animationName prefix to match (default 'presence-out').
     *   opts.self — if true (default) only el's own animation counts; set false
     *               to also accept an animation that bubbles up from a child
     *               (used by DrylDialog, whose dialogOut runs on the inner
     *               .dialog and bubbles to the backdrop el we listen on).
     * ---------------------------------------------------------------- */
    function onExit(el, dotnetRef, opts) {
        if (!el || _exit.has(el)) return;
        opts = opts || {};
        const name = opts.name || 'presence-out';
        const self = opts.self !== false;

        // Reduced motion (or no exit animation): resolve on the next frame so
        // the C# side still gets a single, asynchronous OnExitFinished.
        if (reduced()) {
            requestAnimationFrame(() => {
                try { dotnetRef.invokeMethodAsync('OnExitFinished'); } catch (_) { /* circuit gone */ }
            });
            return;
        }

        const handler = (e) => {
            if (self && e.target !== el) return;
            if (!String(e.animationName).startsWith(name)) return;
            clearExit(el);
            try { dotnetRef.invokeMethodAsync('OnExitFinished'); } catch (_) { /* circuit gone */ }
        };
        el.addEventListener('animationend', handler);
        _exit.set(el, handler);
    }

    function clearExit(el) {
        if (!el) return;
        const h = _exit.get(el);
        if (h) { el.removeEventListener('animationend', h); _exit.delete(el); }
    }

    /* ---- Indicator glide ------------------------------------------- */
    function placeIndicator(container) {
        const ink    = container.querySelector('[data-dryl-ink]');
        if (!ink) return;
        const active = container.querySelector('[data-dryl-ink-active="true"]');
        if (!active) { ink.style.opacity = '0'; return; }
        const cr = container.getBoundingClientRect();
        const ar = active.getBoundingClientRect();
        ink.style.opacity   = '1';
        ink.style.width     = ar.width + 'px';
        ink.style.transform = 'translateX(' + (ar.left - cr.left + container.scrollLeft) + 'px)';
    }

    function moveIndicator(container) {
        if (!container) return;
        const first = !_ind.has(container);
        if (first) {
            const ro = new ResizeObserver(() => placeIndicator(container));
            ro.observe(container);
            _ind.set(container, { ro });
        }
        placeIndicator(container);
        // Enable the CSS transition only after the first placement so the ink
        // does not slide in from x=0 on initial render.
        if (first) requestAnimationFrame(() => container.classList.add('is-ink-ready'));
    }

    function disposeIndicator(container) {
        const s = _ind.get(container);
        if (s) { s.ro.disconnect(); _ind.delete(container); }
    }

    /* ---- Reveal ---------------------------------------------------- */
    function observe(el, dotnetRef, opts) {
        if (!el || _reveal.has(el)) return;
        opts = opts || {};
        const once      = opts.once !== false;
        const threshold = typeof opts.threshold === 'number' ? opts.threshold : 0.15;

        if (opts.stagger) {
            Array.from(el.children).forEach((c, i) => c.style.setProperty('--reveal-i', i));
        }

        // No observer support or reduced motion → show immediately, no animation.
        if (reduced() || !('IntersectionObserver' in window)) {
            el.classList.add('is-revealed');
            return;
        }

        const io = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-revealed');
                    if (once) io.unobserve(entry.target);
                } else if (!once) {
                    entry.target.classList.remove('is-revealed');
                }
            });
        }, { threshold });
        io.observe(el);
        _reveal.set(el, { io });
    }

    function unobserve(el) {
        const s = _reveal.get(el);
        if (s) { s.io.disconnect(); _reveal.delete(el); }
    }

    return { onExit, clearExit, moveIndicator, disposeIndicator, observe, unobserve };
})();

/* ──────────────────────────────────────────────────────────
 * dryl.depthglass — pointer-driven depth for DrylDepthGlass and the
 *   optional Depth warp on DrylCard. Tracks the pointer over the
 *   surface and exposes it as CSS custom properties the stylesheet
 *   turns into a 3D tilt (--tx/--ty) and a travelling specular
 *   highlight (--mx/--my). All motion is CSS; JS only writes the
 *   variables, so there is no per-frame Blazor cost. The CSS side is
 *   gated on prefers-reduced-motion.
 * ────────────────────────────────────────────────────────── */
window.dryl.depthglass = {
    track(el) {
        if (!el || el.__drylDg) return;
        const onMove = (e) => {
            const r = el.getBoundingClientRect();
            if (!r.width || !r.height) return;
            const px = (e.clientX - r.left) / r.width;   // 0..1
            const py = (e.clientY - r.top) / r.height;   // 0..1
            el.style.setProperty('--mx', (px * 100) + '%');
            el.style.setProperty('--my', (py * 100) + '%');
            el.style.setProperty('--tx', (px - 0.5).toFixed(3)); // -0.5..0.5
            el.style.setProperty('--ty', (py - 0.5).toFixed(3));
        };
        const onLeave = () => {
            el.style.setProperty('--tx', '0');
            el.style.setProperty('--ty', '0');
        };
        el.addEventListener('pointermove', onMove);
        el.addEventListener('pointerleave', onLeave);
        el.__drylDg = { onMove, onLeave };
    },
    untrack(el) {
        if (!el || !el.__drylDg) return;
        el.removeEventListener('pointermove', el.__drylDg.onMove);
        el.removeEventListener('pointerleave', el.__drylDg.onLeave);
        delete el.__drylDg;
    }
};

/* --------------------------------------------------------------
 * Theme — apply DRYL theme seed variables to the document root.
 * Called by DrylThemeProvider on runtime theme changes; the
 * registered @property color seeds + :root transition make the
 * derived color-mix chain glide. `vars` is "--k:v;--k:v;".
 * -------------------------------------------------------------- */
window.dryl.theme = {
    // Every var a DrylTheme may emit. Cleared before each apply so optional
    // overrides from the previous theme (AI accent, semantics, chart slots)
    // fall back to their dryl.css defaults instead of lingering.
    managed: ['--accent-a', '--accent-b', '--ai-a', '--ai-b',
              '--success', '--warning', '--danger', '--info',
              '--chart-1', '--chart-2', '--chart-3', '--chart-4', '--chart-5', '--chart-6'],
    apply(vars) {
        const root = document.documentElement;
        this.managed.forEach(k => root.style.removeProperty(k));
        (vars || '').split(';').forEach(pair => {
            const i = pair.indexOf(':');
            if (i > 0) {
                root.style.setProperty(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
            }
        });
    },
    /* Explicit color-mode forcing. mode: 'light' | 'dark' | 'system'.
       'system' removes the attribute so the prefers-color-scheme media
       query in dryl.css takes over (live, no JS listener needed). */
    applyMode(mode, persist) {
        const root = document.documentElement;
        try {
            if (mode === 'light' || mode === 'dark') {
                root.setAttribute('data-dryl-mode', mode);
                if (persist) localStorage.setItem('dryl-color-mode', mode);
            } else {
                root.removeAttribute('data-dryl-mode');
                if (persist) localStorage.removeItem('dryl-color-mode');
            }
        } catch { /* storage unavailable (private mode etc.) — attribute still applied */ }
    },
    /* The persisted explicit choice, or null when the user follows System. */
    storedMode() {
        try {
            const m = localStorage.getItem('dryl-color-mode');
            return (m === 'light' || m === 'dark') ? m : null;
        } catch { return null; }
    }
};

