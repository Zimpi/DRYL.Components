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
        if (previouslyFocused && typeof previouslyFocused.focus === 'function') {
            try { previouslyFocused.focus(); } catch (_) { /* element gone */ }
        }
    }

    return { attach, detach };
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

