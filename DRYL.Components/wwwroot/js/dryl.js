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
        const btn = anchor.querySelector('.menu-trigger button:not([disabled]), .menu-trigger a, .menu-trigger [tabindex]');
        btn?.focus();
    }

    return { attach, detach, focusPanel, navigate, focusTrigger };
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

