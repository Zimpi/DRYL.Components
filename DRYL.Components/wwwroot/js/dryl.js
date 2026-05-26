// wwwroot/js/dryl.js — DRYL JS interop, namespaced under window.dryl.

window.dryl = window.dryl || {};

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
 * Toast — wire the auto-dismiss timer and the exit animation
 * lifecycle. The CSS animation on .toast-progress is the single
 * source of truth for "how long does the toast live": browsers
 * pause animations under `animation-play-state: paused`, which
 * keeps hover-pause perfectly in sync with the visible bar.
 *
 *   OnExpired       — fired when the progress animation finishes.
 *   OnExitFinished  — fired when the .toast.is-leaving animation
 *                     finishes (so .NET can remove the entry).
 * -------------------------------------------------------------- */
window.dryl.toast = (() => {
    function attach(slot, dotnetRef) {
        if (!slot || slot.__drylToast) return;
        const toast = slot.querySelector('.toast');
        if (!toast) return;
        const progress = toast.querySelector('.toast-progress');

        const onToastAnim = (e) => {
            // Only react to the exit animation on the toast itself.
            if (e.target !== toast) return;
            if (e.animationName === 'toast-out') {
                dotnetRef.invokeMethodAsync('OnExitFinished');
            }
        };

        const onProgressAnim = (e) => {
            if (e.target !== progress) return;
            if (e.animationName === 'toast-progress') {
                dotnetRef.invokeMethodAsync('OnExpired');
            }
        };

        toast.addEventListener('animationend', onToastAnim);
        if (progress) progress.addEventListener('animationend', onProgressAnim);

        slot.__drylToast = { toast, progress, onToastAnim, onProgressAnim };
    }

    function detach(slot) {
        if (!slot || !slot.__drylToast) return;
        const { toast, progress, onToastAnim, onProgressAnim } = slot.__drylToast;
        toast.removeEventListener('animationend', onToastAnim);
        if (progress) progress.removeEventListener('animationend', onProgressAnim);
        delete slot.__drylToast;
    }

    return { attach, detach };
})();
