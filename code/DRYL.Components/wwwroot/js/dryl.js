// wwwroot/js/dryl.js — DRYL JS interop, namespaced under window.dryl.

window.dryl = window.dryl || {};

/* Shared prefers-reduced-motion check — used by dryl.motion and
   dryl.viewTransition so both honour the user's setting identically. */
window.dryl.reduced = () =>
    !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);

/* --------------------------------------------------------------
 * Input echo guard — keeps fast typing intact on a slow circuit.
 *
 * A live-bound field renders as value="@Current" + @oninput, so every
 * keystroke makes a round trip and the server writes its own copy of
 * the text back into the element. Whatever was typed while that copy
 * was in flight gets overwritten — the character shows up, then
 * vanishes, and the next keystroke builds on the damaged text. Over a
 * real network "generiere mir folgende View" arrives as "geneilew".
 *
 * The fix: remember every value the element itself reported, and drop
 * a write that repeats one of them while newer local input exists —
 * that write is a late echo of an older keystroke and has nothing to
 * say. Anything else (a programmatic set, a clear-after-send, a
 * server-side correction) is not in the list and is applied as usual.
 *
 * Entries expire after ECHO_TTL so a late correction that happens to
 * match something typed a while ago still lands, and the list is
 * cleared whenever the server does write, and on blur.
 *
 * Opt in per element with data-dryl-input; the guard installs itself
 * on first focus, so nothing pays for it until a field is used.
 * -------------------------------------------------------------- */
window.dryl.inputs = (() => {
    const STATE = '__drylEchoGuard';
    const ECHO_TTL = 1500;  // ms a local value outranks the server's copy
    const MAX_PENDING = 64;

    function prune(state, now) {
        const p = state.pending;
        while (p.length && now - p[0].t > ECHO_TTL) p.shift();
        if (p.length > MAX_PENDING) p.splice(0, p.length - MAX_PENDING);
    }

    /* Every value the element locally held, in order — typed, or written by
       one of our own scripts (the input mask rewrites the field on each
       keystroke). Applied writes are recorded too: dropping the history
       there would let the next stale echo through, which is exactly how a
       masked field still lost digits. */
    function record(state, v) {
        const now = performance.now();
        const last = state.pending[state.pending.length - 1];
        if (last && last.v === v) last.t = now;
        else state.pending.push({ v: v, t: now });
        prune(state, now);
    }

    /* Shadow the element's own value property. Blazor assigns through
       this property (never setAttribute), so this is the one seam every
       server write has to pass. */
    function install(el) {
        if (!el || el[STATE]) return;
        const proto = (el instanceof HTMLTextAreaElement)
            ? HTMLTextAreaElement.prototype
            : HTMLInputElement.prototype;
        const own = Object.getOwnPropertyDescriptor(proto, 'value');
        if (!own || !own.get || !own.set) return;

        const state = { pending: [] };
        el[STATE] = state;

        Object.defineProperty(el, 'value', {
            configurable: true,
            get() { return own.get.call(this); },
            set(v) {
                prune(state, performance.now());

                // Already there — nothing to write, we have simply caught up.
                if (own.get.call(this) === v) { record(state, v); return; }

                const i = state.pending.findIndex(p => p.v === v);
                if (i >= 0 && i < state.pending.length - 1) {
                    // A value the field already held, with newer local input
                    // behind it: a stale echo. Drop it and everything older.
                    state.pending.splice(0, i + 1);
                    return;
                }

                own.set.call(this, v);
                record(state, v);
            }
        });
    }

    if (!window.__drylInputGuardBound) {
        window.__drylInputGuardBound = true;

        document.addEventListener('focusin', (e) => {
            const t = e.target;
            if (t instanceof Element && t.matches('[data-dryl-input]')) install(t);
        }, true);

        // Bubble phase on purpose: element-level handlers (the input mask
        // rewrites el.value) have already run, so what we record is exactly
        // what the server is about to receive.
        document.addEventListener('input', (e) => {
            const state = e.target && e.target[STATE];
            if (state) record(state, e.target.value);
        });

        document.addEventListener('focusout', (e) => {
            const state = e.target && e.target[STATE];
            if (state) state.pending.length = 0;
        }, true);
    }

    return { install };
})();

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
 * Panel focus — shared by every dryl.popover consumer that wants the
 * focus inside its panel while it is open (dryl.menu, dryl.datepicker,
 * dryl.timepicker).
 *
 * Not on window: a lexical top-level const is visible to the modules in
 * this file and to nothing else, so this stays private and adds no API.
 *
 * into(panel, apply) — on open this runs BEFORE the panel is focusable:
 *   a consumer's OnAfterRenderAsync fires before its child DrylPopover's
 *   (Blazor runs parent before child), so the panel is still
 *   visibility:hidden — the .is-open[data-dryl-positioned] gate in
 *   DrylPopover.razor.css only opens once dryl.popover.open has placed
 *   it. focus() on a hidden element is silently a no-op, which left the
 *   panel open with focus still on the trigger and Escape (handled on
 *   the panel) never reaching the component. So: try, and if focus did
 *   not take, leave a one-shot request on the node for dryl.popover.open
 *   to apply at the moment it reveals the panel. The decision to focus
 *   stays with the consumer; only the timing belongs to the portal —
 *   other consumers (select, autocomplete) deliberately keep focus on
 *   their trigger and never leave a request.
 *   Whether focus landed is read back from the document rather than from
 *   the panel's styles, which keeps this honest about every reason a
 *   focus() can be a no-op, not just the one we know about.
 *
 * restore(panel, input) — the counterpart on close: focus lives in the
 *   panel now, and the panel is about to go away.
 * -------------------------------------------------------------- */
const drylPanelFocus = (() => {
    function into(panel, apply) {
        if (!panel) return;
        const attempt = () => {
            apply(panel);
            return panel.contains(document.activeElement);
        };
        if (!attempt()) panel.__drylPendingFocus = attempt;
    }

    // By the time this runs the panel is already hidden (Blazor dropped
    // .is-open in the render that closed it), so the browser has usually
    // moved focus to <body> — accept that, and the still-in-panel case.
    // Anything else means focus sits somewhere the user put it, and taking
    // it back would be a steal, so leave it. Reports whether it moved, so
    // the caller can drop a suppression it no longer needs.
    function restore(panel, input) {
        if (!input) return false;
        const active = document.activeElement;
        // Already there — a focus() would be a no-op and fire no focus event, so
        // reporting "moved" here would leave the caller's one-shot suppression
        // armed against the user's next, genuine focus.
        if (active === input) return false;
        const ours = !active || active === document.body
                     || (panel && panel.contains(active));
        if (!ours) return false;
        try { input.focus(); } catch (_) { return false; }
        return document.activeElement === input;
    }

    return { into, restore };
})();

/* --------------------------------------------------------------
 * Panel keys — the key policy for a focused popover panel, in the one
 * place that can see which element a key came from. Private, like
 * drylPanelFocus above, and used by dryl.datepicker and dryl.timepicker.
 *
 * Blazor's own @onkeydown:preventDefault cannot do this job: it takes a
 * value rendered *before* the key, so it is one render behind (measured —
 * off for the first key after opening, on for the key after an arrow). And
 * a .NET handler bound to the panel never learns which descendant was
 * focused, because KeyboardEventArgs carries no target. Both of the bugs
 * this exists for come straight out of those two facts:
 *
 *   - Enter/Space on a month chevron ran the calendar's "select the
 *     focused day" case, so paging a month committed a date and closed
 *     the picker. Here the target is visible: when a key lands on a
 *     control that activates on Enter/Space, the event is stopped at the
 *     panel and never reaches the .NET handler — the control's own click
 *     is the whole action. The panel's handler then only sees the keys
 *     that belong to the panel itself.
 *   - Tab left the panel entirely (portaled to <body>, so "the next tab
 *     stop" is the far end of the page) and stranded it open. Tab now
 *     cycles inside the panel. Escape stays the way out, on the panel and
 *     on the picker's input.
 *
 * navKeys: suppress the browser default for the arrows, Home/End and
 * paging. Both panels pass true, for the same reason from two directions:
 * the calendar consumes those keys itself and must not scroll the page as
 * well, and the time panel consumes none of them but would otherwise
 * scroll the page out from under an open dialog — its columns are
 * descendants of the focused panel, so they never receive that scroll.
 *
 * The listener is not removed: it holds nothing but the panel node it
 * lives on, so it dies with that node when Blazor discards the component.
 * -------------------------------------------------------------- */
const drylPanelKeys = (() => {
    const FOCUSABLE = 'a[href], button, input, select, textarea, [tabindex]';
    const NAV = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
                 'Home', 'End', 'PageUp', 'PageDown'];
    // Elements that turn Enter/Space into a click of their own.
    const ACTIVATES = 'button, a[href], input, select, textarea';

    function tabbables(panel) {
        return Array.from(panel.querySelectorAll(FOCUSABLE)).filter(el =>
            !el.disabled && el.tabIndex >= 0 && el.getClientRects().length > 0);
    }

    function cycleTab(panel, e) {
        const items = tabbables(panel);
        if (!items.length) return;
        e.preventDefault();
        const i = items.indexOf(document.activeElement);
        const next = e.shiftKey
            ? items[(i <= 0 ? items.length : i) - 1]
            : items[(i + 1) % items.length];
        next?.focus();
    }

    function install(panel, navKeys) {
        if (!panel || panel.__drylPanelKeys) return;
        const onKey = (e) => {
            if (e.key === 'Tab') {
                cycleTab(panel, e);
                return;
            }
            if ((e.key === 'Enter' || e.key === ' ')
                && e.target !== panel && e.target.closest?.(ACTIVATES)) {
                // Let the control activate itself; keep the panel out of it.
                e.stopPropagation();
                return;
            }
            // Only the keys the panel really consumes lose their default —
            // never a bare modifier or a browser shortcut such as Ctrl+F,
            // which an unconditional preventDefault used to swallow.
            if (navKeys && !e.ctrlKey && !e.metaKey && !e.altKey
                && NAV.includes(e.key)) {
                e.preventDefault();
            }
        };
        panel.addEventListener('keydown', onKey);
        panel.__drylPanelKeys = onKey;
    }

    return { install };
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

    // First item if there is one, else the panel itself. The hidden-panel
    // timing is handled by drylPanelFocus.into (see there).
    function focusPanel(panel) {
        drylPanelFocus.into(panel, p => {
            const first = p.querySelector(ITEMS);
            if (first) first.focus();
            else p.focus();
        });
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
 *   claimTrigger(anchor, role, open)      — put aria-haspopup / aria-expanded
 *                                           on the trigger, additively
 *
 * Blazor still owns the panel node. open() moves it with
 * document.body.appendChild and close() puts it back with
 * anchor.appendChild — no placeholder is left behind, and none is
 * needed: the panel wrapper is rendered whether the popover is open
 * or closed, so Blazor never structurally removes the node while JS
 * is holding it. What is conditional is only the panel's content.
 *
 * opts: { placement, matchWidth, closeOnOutside, role }
 *   placement — 'bottom-start' | 'bottom-end' | 'top-start' | 'top-end'
 *   role      — the panel's ARIA role, used for the trigger's aria-haspopup
 * -------------------------------------------------------------- */
window.dryl.popover = (() => {
    const GAP  = 4;   // matches --sp-1: gap between trigger and panel
    const EDGE = 4;   // min viewport inset when clamping
    const state = new WeakMap(); // anchor -> { panel, onScroll, onResize, onDocClick, trigger }

    /* Trigger ARIA state ------------------------------------------------
     * A trigger that opens a panel must announce it (aria-haspopup) and
     * report whether it is open (aria-expanded). Components that build their
     * own trigger markup — DrylSelect, DrylMultiSelect, both pickers,
     * DrylNotifications — already write both themselves. The ones composed
     * from plain buttons (DrylMenu, and therefore DrylSplitButton's caret)
     * cannot: the open state lives in DrylMenu, out of reach of the trigger
     * fragment's own render context.
     *
     * So the popover does it, strictly additively — and additively PER
     * ATTRIBUTE, because the two are orthogonal promises: naming a popup type
     * says what this control opens, reporting an open state says what it is
     * doing right now, and a consumer can make either promise without the
     * other. The library's own ThemeSwitcher on the docs site is exactly that
     * case: it writes aria-haspopup="dialog" and no aria-expanded. So each
     * attribute is claimed only where it is absent, each claim is marked on
     * the node separately, and only a claimed attribute is ever written again.
     * A consumer that keeps its own aria-expanded — as DrylSelect, both
     * pickers and DrylNotifications do — never has it touched, and nothing
     * here writes against Blazor's attribute diffing.
     *
     * The element is found by this module's OWN rule (see ariaTarget below),
     * deliberately not with the selector dryl.menu.focusTrigger uses. Two
     * differences, both about what ARIA describes rather than what focus
     * needs: a DISABLED button is
     * included, because aria-haspopup describes the control, not its momentary
     * operability — a disabled caret still is the thing that opens the menu,
     * and excluding it would leave it permanently silent, since a disabled
     * trigger can never be opened to be claimed later. And tabindex="-1" is
     * excluded, because that is the programmatic-focus marker decorative nodes
     * carry; a <span tabindex="-1"> ahead of the real button would otherwise
     * take the attributes instead of it. focusTrigger's own selector is left
     * alone: which element focus should return to is a different question.
     *
     * claimTrigger runs at the popover's FIRST RENDER, not only on open.
     * aria-haspopup exists for the state before the panel is opened: it is
     * what tells a screen-reader user that this button unfolds something. An
     * attribute that appeared only after the first open would announce the
     * fact at the moment the user had already discovered it — and it would
     * leave DrylMenu out of step with DrylSelect and the pickers, which carry
     * theirs from the first render. Opening re-claims (a trigger rebuilt in
     * the meantime is a fresh node with no attributes and no owner marks), so
     * the two entry points are one function called twice, not two rules. */
    const OWNS_HASPOPUP = '__drylTriggerHasPopup';
    const OWNS_EXPANDED = '__drylTriggerExpanded';
    const ARIA_CANDIDATE = 'button, a, [tabindex]:not([tabindex="-1"])';
    // Only roles aria-haspopup actually defines. An unknown or absent panel
    // role means we stay silent rather than claim a popup type that is not true.
    const HASPOPUP_ROLES = ['menu', 'listbox', 'tree', 'grid', 'dialog'];

    /* Which element inside the trigger is THE trigger.
     *
     * Two rules, in order. An element that already carries aria-haspopup has
     * named itself the trigger, and it wins outright — that is how DrylSelect's
     * and DrylMultiSelect's own containers are recognised even when they are
     * disabled and have dropped their tabindex. Otherwise the SHALLOWEST
     * candidate wins, because a control nested deeper is a part of the trigger
     * rather than the trigger: a multiselect's chip has a remove button inside
     * the trigger container, and a plain document-order query hands the
     * attributes to that button as soon as the container itself stops matching.
     * (Measured on /components/multiselect: the disabled example's anchor
     * resolves to .chip-remove that way. Nothing is written there today, but
     * the wrong node was being selected.) */
    function ariaTarget(anchor) {
        const scope = anchor.querySelector('.popover-trigger, .menu-trigger') || anchor;
        const named = scope.querySelector('[aria-haspopup]');
        if (named) return named;
        let best = null, bestDepth = Infinity;
        for (const el of scope.querySelectorAll(ARIA_CANDIDATE)) {
            let depth = 0;
            for (let n = el; n && n !== scope; n = n.parentElement) depth++;
            if (depth < bestDepth) { best = el; bestDepth = depth; }
        }
        return best;
    }

    function claimTrigger(anchor, role, open) {
        if (!anchor || HASPOPUP_ROLES.indexOf(role) < 0) return null;
        const el = ariaTarget(anchor);
        if (!el) return null;
        // Two independent claims — see the note above.
        if (!el[OWNS_HASPOPUP] && !el.hasAttribute('aria-haspopup')) {
            el[OWNS_HASPOPUP] = true;
            el.setAttribute('aria-haspopup', role);
        }
        if (!el[OWNS_EXPANDED] && !el.hasAttribute('aria-expanded')) {
            el[OWNS_EXPANDED] = true;
        }
        if (el[OWNS_EXPANDED]) el.setAttribute('aria-expanded', open ? 'true' : 'false');
        return el;
    }

    function releaseTrigger(el) {
        // aria-haspopup stays — the trigger still opens a panel while closed.
        if (el && el[OWNS_EXPANDED]) el.setAttribute('aria-expanded', 'false');
    }

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
        //
        // Hardening, not a fix for anything currently broken: re-parenting a
        // node blurs whatever was focused inside it, so carry that focus across
        // the move. No consumer hits this today (the panel is still hidden, and
        // therefore unfocusable, until the reveal below), but one that focuses
        // into its panel before the portal runs would otherwise lose it in
        // silence. Keep it — it is not the pending-focus mechanism below.
        // Only the capture belongs here: it has to read activeElement before
        // the move. The restore waits until after the reveal, further down.
        const focused = document.activeElement;
        const refocus = focused && panel.contains(focused);
        document.body.appendChild(panel);

        const reposition = () => place(anchor, panel, placement, matchWidth);
        reposition();
        // Reveal only now that it is correctly placed (see the two-key
        // .is-open[data-dryl-positioned] gate in DrylPopover.razor.css).
        //
        // An ATTRIBUTE and not a class, and the distinction is load-bearing:
        // Blazor renders this element's `class`, and every render rewrites the
        // whole attribute from its own render tree. A class added here survives
        // only until Blazor next touches class on this node — which it does the
        // moment the popover starts its exit animation, silently dropping the
        // second visibility key and with it the animation that needed it.
        // Blazor renders no data-* attribute on the panel, so this one is ours.
        panel.setAttribute('data-dryl-positioned', '');

        // Both focus moves belong AFTER the line above, because that is what
        // makes the panel focusable — restoring beside the appendChild would
        // call focus() on a still-hidden element, which is silently a no-op and
        // is the very failure this whole mechanism exists to avoid.
        //
        // Restore the carried-over focus first (see the hardening note above),
        // so that an explicit pending request still wins if there is one.
        if (refocus) focused.focus();

        // A consumer that asked to focus into the panel while it was still
        // hidden (DrylMenu, whose OnAfterRenderAsync runs before ours) parked
        // the request on the node; honour it now. One-shot: cleared before
        // running, and again in close() for the request that is never reached
        // because the popover closed.
        const pendingFocus = panel.__drylPendingFocus;
        if (pendingFocus) {
            delete panel.__drylPendingFocus;
            pendingFocus();
        }

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

        state.set(anchor, { panel, onScroll, onResize, onDocClick, trigger: claimTrigger(anchor, opts.role, true) });
    }

    function close(anchor) {
        const s = state.get(anchor);
        if (!s) return;
        window.removeEventListener('scroll', s.onScroll, true);
        window.removeEventListener('resize', s.onResize);
        if (s.onDocClick) document.removeEventListener('pointerdown', s.onDocClick, true);

        releaseTrigger(s.trigger);

        // Drop any focus request that open() never got to apply, so it cannot
        // fire later against a stale panel.
        delete s.panel.__drylPendingFocus;

        // Return the panel to its original slot (it is the anchor's last child)
        // and clear the styles/marker applied while portaled.
        s.panel.removeAttribute('data-dryl-positioned');
        s.panel.style.top = '';
        s.panel.style.left = '';
        s.panel.style.width = '';
        anchor.appendChild(s.panel);

        state.delete(anchor);
    }

    return { open, close, claimTrigger };
})();

/* --------------------------------------------------------------
 * Toast — auto-dismiss timer and exit animation lifecycle.
 *
 * Timer: setTimeout rather than CSS animationend, so that Blazor re-renders
 * (which may patch the .toast-progress element) cannot reset it. The hover
 * pause is handled explicitly via mouseenter/mouseleave on the stable slot
 * element.
 *
 * Exit: event delegation on the slot (kept alive by Blazor via @key), since
 * animationend bubbles — more robust than a listener on the inner .toast div,
 * which Blazor patches whenever its classes change.
 *
 *   OnExpired       — fired by setTimeout after the duration.
 *   OnExitFinished  — fired when toast-out animationend bubbles to slot.
 * -------------------------------------------------------------- */
window.dryl.toast = (() => {
    function attach(slot, dotnetRef) {
        if (!slot || slot.__drylToast) return;
        const toast = slot.querySelector('.toast');
        if (!toast) return;

        // Read the duration from the progress element's inline style.
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

        // Hover listeners on the stable slot element (not .toast, which can be patched).
        let onMouseEnter = () => {};
        let onMouseLeave = () => {};
        if (remaining > 0) {
            onMouseEnter = pauseTimer;
            onMouseLeave = startTimer;
            slot.addEventListener('mouseenter', onMouseEnter);
            slot.addEventListener('mouseleave', onMouseLeave);
            startTimer();
        }

        // Exit detection via event delegation on the slot — animationend bubbles,
        // and Blazor keeps the slot stable via @key.
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
    // Focus the given day cell. Until this went through drylPanelFocus.into the
    // call found its button and still did nothing, because the panel was not yet
    // revealed — and with focus left on the input, the panel's keydown handler
    // (Escape, arrows, Home/End, PageUp/PageDown, Enter) never got a key.
    focusDay(panel, dayNumber) {
        // navKeys: the calendar consumes the arrows, Home/End and paging.
        drylPanelKeys.install(panel, true);
        drylPanelFocus.into(panel, p => {
            const btn = p.querySelector(`[data-day="${dayNumber}"]`);
            if (btn) btn.focus();
            else p.focus();
        });
    },

    // Hand focus back to the input when the panel closes; returns whether it
    // actually moved (see drylPanelFocus.restore).
    restoreFocus(panel, input) {
        return drylPanelFocus.restore(panel, input);
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

        /* Caret movement stays in the browser. Asking the server where to
           focus costs a full round trip, and until the answer arrives every
           further digit lands in the box the user has already left — a code
           typed at any speed came out as "16" over a slow connection. */
        const onInput = (e) => {
            const el = e.target;
            if (!(el instanceof HTMLInputElement) || el.value === '') return;
            const inputs = getInputs(container);
            const i = inputs.indexOf(el);
            if (i >= 0 && i + 1 < inputs.length) inputs[i + 1].focus();
        };

        const onKeyDown = (e) => {
            const el = e.target;
            if (!(el instanceof HTMLInputElement)) return;
            const inputs = getInputs(container);
            const i = inputs.indexOf(el);
            if (i < 0) return;

            if (e.key === 'Backspace' && el.value === '' && i > 0) {
                inputs[i - 1].focus();
            } else if (e.key === 'ArrowLeft' && i > 0) {
                e.preventDefault();
                inputs[i - 1].focus();
            } else if (e.key === 'ArrowRight' && i + 1 < inputs.length) {
                e.preventDefault();
                inputs[i + 1].focus();
            }
        };

        container.addEventListener('paste', onPaste);
        container.addEventListener('input', onInput);
        container.addEventListener('keydown', onKeyDown);
        _map.set(container, { onPaste, onInput, onKeyDown });
    }

    function detach(container) {
        if (!container) return;
        const h = _map.get(container);
        if (!h) return;
        container.removeEventListener('paste', h.onPaste);
        container.removeEventListener('input', h.onInput);
        container.removeEventListener('keydown', h.onKeyDown);
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

    // Focus .time-panel — the popover's own panel node is what the caller
    // hands us, but the keydown handler (Escape, Enter) sits on .time-panel
    // inside it, and a key pressed on an ancestor never reaches it. Measured:
    // focusing the wrapper leaves Escape just as dead as focusing the input did.
    //
    // Deliberately not a cell: the columns have no notion of a current cell —
    // no roving tabindex, no arrow navigation, and .is-selected marks the
    // pending value, not a focus position. Focusing one cell out of sixty would
    // announce it as the user's place in a list they cannot navigate, and would
    // scroll its column to wherever that cell happens to sit.
    function focusPanel(panel) {
        // navKeys: the arrows and paging keys reach no column from here — the
        // columns are descendants of the focused .time-panel, not ancestors, so
        // the browser scrolls the nearest scrollable ancestor instead, which is
        // the document the portaled panel now hangs in. Measured: ArrowDown
        // scrolled the page 0 -> 40 and PageDown 0 -> 655 while both .time-col
        // scrollTops stayed 0, dragging the page out from under an open dialog.
        // So the keys are swallowed and do nothing, which is the right answer
        // for a role="dialog". Actually navigating the columns with the arrows
        // would be new behaviour and belongs in an idea and a spec, not in a
        // bugfix — it is left undone on purpose, not forgotten.
        drylPanelKeys.install(panel, true);
        drylPanelFocus.into(panel, p => (p.querySelector('.time-panel') || p).focus());
    }

    // Hand focus back to the input when the panel closes; returns whether it
    // actually moved (see drylPanelFocus.restore).
    function restoreFocus(panel, input) {
        return drylPanelFocus.restore(panel, input);
    }

    return { attach, detach, scrollToActive, focusPanel, restoreFocus };
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

    // A hidden element has no layout: scrollHeight is 0. Writing that back would pin the
    // textarea to zero height and it would stay there until the first keystroke — which is
    // exactly what a composer inside a not-yet-shown popover or a collapsed panel does on
    // attach. Leave the CSS height untouched instead (rows="1" is the right fallback) and
    // report back that the measurement did not happen.
    function autoGrow(el) {
        const prev = el.style.height;
        el.style.height = 'auto';
        const h = el.scrollHeight;
        if (h > 0) {
            el.style.height = h + 'px';
            return true;
        }
        el.style.height = prev;
        return false;
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
        const handlers = { onKeyDown, onInput, observer: null };
        _map.set(el, handlers);

        // Attached while hidden — measure once the host reveals it, so a pre-filled draft
        // still gets its real height without waiting for a keystroke.
        if (!autoGrow(el) && typeof ResizeObserver !== 'undefined') {
            handlers.observer = new ResizeObserver(() => {
                if (autoGrow(el)) {
                    handlers.observer.disconnect();
                    handlers.observer = null;
                }
            });
            handlers.observer.observe(el);
        }
    }

    function detachComposer(el) {
        if (!el) return;
        const h = _map.get(el);
        if (!h) return;
        el.removeEventListener('keydown', h.onKeyDown);
        el.removeEventListener('input', h.onInput);
        if (h.observer) h.observer.disconnect();
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
 *
 *   FLIP glide — autoFlip/stopAutoFlip watch a root's [data-cid]
 *     descendants and, whenever a mutation moves one, invert the
 *     position delta into a transform then let it transition back to
 *     identity (First-Last-Invert-Play). Compositor-only (transform),
 *     reduced-motion aware. Powers DrylAiCanvas artifact reflows.
 * ────────────────────────────────────────────────────────── */
window.dryl.motion = (() => {
    const _exit   = new WeakMap(); // wrapper el -> animationend handler
    const _ind    = new WeakMap(); // container  -> { ro }
    const _reveal = new WeakMap(); // wrapper el -> { io }
    const _flip   = new WeakMap(); // root el    -> MutationObserver

    const reduced = () => window.dryl.reduced();

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
        // nodeType check: a Blazor ElementReference whose element has already left the DOM
        // (a bar inside DrylPresence, mid exit) marshals as a non-Element stub — feeding that
        // to ResizeObserver.observe throws and kills the circuit.
        if (!container || container.nodeType !== 1) return;
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

    /* ---- FLIP glide -------------------------------------------------
     * autoFlip(root) remembers the position of every [data-cid] descendant
     * (root-relative, scroll- and in-flight-transform-compensated); when a
     * STRUCTURAL mutation (a [data-cid] node added/removed/moved) changes
     * their layout, the delta is applied as an inverted transform and
     * released after one forced reflow, so the element glides
     * (transform-only) back to its new identity position over the fixed
     * --dur-med/--ease-spring vocabulary. Non-structural mutations (chart
     * SVG redraws, aura elements, tooltips) never replay the glide.
     * Reduced-motion users get the plain, unanimated reflow (no-op).
     * ---------------------------------------------------------------- */
    function autoFlip(root) {
        if (!root || _flip.has(root)) return;
        if (reduced()) return;

        // Baselines are stored relative to the node's nearest [data-cid] ancestor
        // (canvas nodes nest!). Local deltas mean: when a card glides, its children
        // ride along on the card's transform instead of each being inverted again
        // (a root-relative scheme double-moves every descendant of a moved node).
        // They are also scroll- and in-flight-transform-compensated, so neither
        // scrolling nor a half-finished enter/glide animation reads as movement.
        let rects = new Map();
        const anchorOf = (el) => {
            for (let n = el.parentElement; n && n !== root; n = n.parentElement)
                if (n.hasAttribute('data-cid')) return n;
            return root;
        };
        const measure = () => {
            const out = new Map();
            for (const el of root.querySelectorAll('[data-cid]')) {
                const anchor = anchorOf(el);
                const r = el.getBoundingClientRect();
                const rA = anchor.getBoundingClientRect();
                // Sum the transforms between el (inclusive) and its anchor
                // (exclusive) — our own glide plus any presence-enter wrapper —
                // so the stored position is the true layout slot, not the
                // visually interpolated mid-animation frame.
                let tx = 0, ty = 0;
                for (let n = el; n && n !== anchor; n = n.parentElement) {
                    const t = getComputedStyle(n).transform;
                    if (t && t !== 'none') { const m = new DOMMatrixReadOnly(t); tx += m.e; ty += m.f; }
                }
                out.set(el.getAttribute('data-cid'), {
                    el,
                    left: r.left - tx - rA.left + (anchor === root ? root.scrollLeft : 0),
                    top: r.top - ty - rA.top + (anchor === root ? root.scrollTop : 0),
                    tx, ty,
                });
            }
            return out;
        };
        let settle = 0;
        const play = () => {
            // Pass 1: measure everything BEFORE any style is written.
            const next = measure();
            // Pass 2: invert old-vs-next deltas. The glide starts from the node's
            // current visual position (layout delta + any in-flight offset) so an
            // interrupted glide continues seamlessly instead of restarting.
            const moved = [];
            for (const [cid, now] of next) {
                const prev = rects.get(cid);
                if (!prev) continue;
                const dx = prev.left + now.tx - now.left;
                const dy = prev.top + now.ty - now.top;
                if (Math.abs(dx) < 1 && Math.abs(dy) < 1) continue;
                now.el.style.transition = 'none';
                now.el.style.transform = `translate(${dx}px, ${dy}px)`;
                moved.push(now.el);
            }
            if (moved.length) {
                void root.offsetWidth; // commit the inverted transforms in one reflow…
                for (const el of moved) {
                    // …then release them, still pre-paint, so the transition
                    // provably starts from the inverted position (a rAF release
                    // races the style flush and can skip the glide entirely).
                    el.style.transition = 'transform var(--dur-med) var(--ease-spring)';
                    el.style.transform = '';
                    el.addEventListener('transitionend', () => { el.style.transition = ''; }, { once: true });
                }
            }
            rects = next;
            // Re-baseline once everything is at rest: a measurement taken while
            // an enter animation was mid-flight would otherwise stay poisoned
            // until the next structural change glides every node at once.
            clearTimeout(settle);
            settle = setTimeout(() => { rects = measure(); }, 600);
        };
        // Only structural changes to the artifact tree replay the glide — a chart
        // redrawing its SVG internals, an aura element or a tooltip must not
        // re-invert every node on the canvas (that constant replay reads as
        // flicker). Check the fragment root itself first: querySelector never
        // matches the root of an added/removed fragment.
        const structural = (muts) => {
            for (const m of muts) {
                for (const list of [m.addedNodes, m.removedNodes]) {
                    for (const n of list) {
                        if (n.nodeType === 1 && (n.matches('[data-cid]') || n.querySelector('[data-cid]')))
                            return true;
                    }
                }
            }
            return false;
        };
        const observer = new MutationObserver((muts) => { if (structural(muts)) play(); });
        observer.observe(root, { childList: true, subtree: true, attributes: false });
        rects = measure();
        _flip.set(root, { observer, dispose: () => clearTimeout(settle) });
    }

    function stopAutoFlip(root) {
        const s = root && _flip.get(root);
        if (s) { s.observer.disconnect(); s.dispose(); _flip.delete(root); }
    }

    /* ---- Count-up ---------------------------------------------------
     * countUp(el, text) tweens the FIRST number found in `text` from the
     * value the element last landed on (0 initially) up to the target,
     * writing prefix and suffix through unchanged. Carries DrylStat's
     * CountUp parameter.
     *
     * Two properties make this safe to point at Blazor-owned DOM:
     *   1. The final frame always writes `text` verbatim, so a misread
     *      grouping/decimal separator is at worst a cosmetic mid-tween
     *      frame — never a wrong end value.
     *   2. Blazor only patches a text node whose *virtual* value changed,
     *      so an unrelated re-render never fights the tween; the next real
     *      value change resets the target and the tween continues from
     *      wherever it had landed.
     *
     * Duration comes from --dur-slow (rule 2.1: no literals) and the
     * easing mirrors --ease-out. Reduced motion writes the text directly.
     * ---------------------------------------------------------------- */
    // Digits plus every separator a formatted number may carry: '.', ',', plain,
    // non-breaking and narrow no-break space (de-DE / fr-FR group with the latter two).
    const SEPS   = '.,\\u00A0\\u202F ';
    const NUMBER = new RegExp('-?\\d[\\d' + SEPS + ']*\\d|-?\\d');

    function slowMs() {
        const raw = getComputedStyle(document.documentElement).getPropertyValue('--dur-slow').trim();
        const ms = raw.endsWith('ms') ? parseFloat(raw) : raw.endsWith('s') ? parseFloat(raw) * 1000 : NaN;
        return isFinite(ms) && ms > 0 ? ms : 420;
    }

    // Split a number token into its separators so intermediate frames look like
    // the target: the last separator followed by 1–2 digits is the decimal point,
    // anything else is grouping.
    function shapeOf(token) {
        const m = token.match(/[.,](\d+)$/);
        const decimals = m && m[1].length <= 2 ? m[1].length : 0;
        const decSep = decimals ? token[token.length - decimals - 1] : '';
        const body = decimals ? token.slice(0, -decimals - 1) : token;
        const grp = (body.match(new RegExp('[' + SEPS + ']')) || [''])[0];
        return { decimals, decSep: decSep || '.', grp };
    }

    function toNumber(token, shape) {
        let s = token;
        if (shape.grp) s = s.split(shape.grp).join('');
        if (shape.decimals) s = s.slice(0, -shape.decimals - 1) + '.' + s.slice(-shape.decimals);
        return parseFloat(s);
    }

    function render(value, shape) {
        const neg = value < 0;
        const fixed = Math.abs(value).toFixed(shape.decimals);
        let [int, dec] = fixed.split('.');
        if (shape.grp) int = int.replace(/\B(?=(\d{3})+(?!\d))/g, shape.grp);
        return (neg ? '-' : '') + int + (dec ? shape.decSep + dec : '');
    }

    function countUp(el, text) {
        if (!el) return;
        text = String(text ?? '');

        const prev = el.__drylCount;
        if (prev) { cancelAnimationFrame(prev.raf); el.__drylCount = null; }

        const token = text.match(NUMBER);
        if (!token || reduced()) { el.textContent = text; return; }

        const shape = shapeOf(token[0]);
        const to = toNumber(token[0], shape);
        const from = prev && isFinite(prev.value) ? prev.value : 0;
        if (!isFinite(to) || to === from) { el.textContent = text; el.__drylCount = { value: to, raf: 0 }; return; }

        const head = text.slice(0, token.index);
        const tail = text.slice(token.index + token[0].length);
        const dur = slowMs();
        const t0 = performance.now();

        const step = (now) => {
            if (!el.isConnected) { el.__drylCount = null; return; }
            const p = Math.min(1, (now - t0) / dur);
            // easeOutCubic — the JS mirror of --ease-out's decelerating shape.
            const eased = 1 - Math.pow(1 - p, 3);
            if (p < 1) {
                el.textContent = head + render(from + (to - from) * eased, shape) + tail;
                el.__drylCount.raf = requestAnimationFrame(step);
            } else {
                el.textContent = text;   // land on the exact string Blazor rendered
                el.__drylCount = { value: to, raf: 0 };
            }
        };
        el.__drylCount = { value: to, raf: requestAnimationFrame(step) };
    }

    return { onExit, clearExit, moveIndicator, disposeIndicator, observe, unobserve, autoFlip, stopAutoFlip, countUp };
})();

/* ──────────────────────────────────────────────────────────
 * dryl.viewTransition — same-document View Transition bridge.
 *
 * start(dotNetRef) snapshots the current DOM, asks .NET to apply its
 * state change (ApplyChange resolves only after the consuming
 * component's OnAfterRender fired, i.e. the new DOM is committed),
 * then lets the browser morph old → new. Falls back to a direct,
 * morph-free apply when the API is missing or the user prefers
 * reduced motion — the feature never blocks unsupported browsers.
 *
 * The #dryl-merge SVG "goo" filter used by DepthGlass morphs
 * (view-transition-class: dryl-depth) is injected lazily the first
 * time a DepthGlass element ([data-vt-depth]) is in the DOM —
 * the same lazy-DOM-injection pattern as the tooltip portal.
 * ────────────────────────────────────────────────────────── */
window.dryl.viewTransition = (() => {
    function ensureMergeFilter() {
        if (document.getElementById('dryl-merge')) return;
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('width', '0');
        svg.setAttribute('height', '0');
        svg.setAttribute('aria-hidden', 'true');
        svg.style.position = 'absolute';
        svg.innerHTML =
            '<defs><filter id="dryl-merge">' +
            '<feGaussianBlur in="SourceGraphic" stdDeviation="6" result="b"/>' +
            '<feColorMatrix in="b" mode="matrix" values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 24 -12" result="g"/>' +
            '<feComposite in="SourceGraphic" in2="g" operator="atop"/>' +
            '</filter></defs>';
        document.body.appendChild(svg);
    }

    function start(dotNetRef) {
        if (!document.startViewTransition || window.dryl.reduced()) {
            // No support, or user opted out of motion: apply the change
            // directly — no snapshot, no morph (same fallback shape as
            // dryl.motion.onExit).
            return dotNetRef.invokeMethodAsync('ApplyChange');
        }
        if (document.querySelector('[data-vt-depth]')) ensureMergeFilter();
        const t = document.startViewTransition(() => dotNetRef.invokeMethodAsync('ApplyChange'));
        // Swallow skip-rejections (e.g. duplicate view-transition-name):
        // the DOM change itself was applied; only the morph was skipped.
        return t.finished.catch(() => { });
    }

    return { start };
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
/* ──────────────────────────────────────────────────────────
 * dryl.topLayer — promote an element to the browser's top layer.
 *
 * `position: fixed` is measured against the nearest ancestor with a
 * transform, filter, backdrop-filter or containment — and in a real app
 * that is almost always something: a page fade-in wrapper, a glass card,
 * a tilting surface. A "fullscreen" overlay built on fixed positioning
 * therefore quietly fills a card instead of the viewport.
 *
 * The top layer has no containing block at all, so an element promoted
 * into it really does span the viewport. The caller renders
 * popover="manual" on the element and calls show/hide from OnAfterRender.
 *
 * Progressive by construction: a browser without the Popover API ignores
 * the unknown attribute and these calls no-op, leaving the element in
 * flow with whatever `position: fixed` gets it — today's behaviour.
 * ────────────────────────────────────────────────────────── */
window.dryl.topLayer = {
    show(el) {
        // Throws if the element is already open or has no popover attribute yet —
        // both are benign races with Blazor's render/interop ordering.
        try { el && el.showPopover && el.showPopover(); } catch (_) { /* already open */ }
    },
    hide(el) {
        try { el && el.hidePopover && el.hidePopover(); } catch (_) { /* already closed */ }
    },
};

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

/* --------------------------------------------------------------
 * Tooltip — a single delegated, body-portaled bubble for every
 * DrylTooltip ([data-tt] wrapper). position:fixed on <body> escapes
 * any ancestor overflow/backdrop-filter clipping (glass cards, app
 * bars); the preferred placement flips when the viewport has no
 * room on that side. Purely decorative (aria-hidden) — triggers
 * carry their own aria-label.
 * -------------------------------------------------------------- */
window.dryl.tooltip = (() => {
    const GAP = 8, PAD = 4;
    let bubble = null;
    let current = null;

    function ensureBubble() {
        if (bubble && bubble.isConnected) return bubble;
        bubble = document.createElement('div');
        bubble.className = 'tt-portal';
        bubble.setAttribute('aria-hidden', 'true');
        document.body.appendChild(bubble);
        return bubble;
    }

    function place(wrap) {
        if (!wrap.isConnected) return hide();
        const b = ensureBubble();
        b.textContent = wrap.getAttribute('data-tt') || '';
        if (!b.textContent) return hide();

        // Measure invisibly at the final size, then position and reveal.
        b.classList.remove('is-open');
        b.style.top = '0px'; b.style.left = '-9999px';
        const tr = wrap.getBoundingClientRect();
        const bw = b.offsetWidth, bh = b.offsetHeight;
        const vw = window.innerWidth, vh = window.innerHeight;
        let placement = wrap.getAttribute('data-tt-placement') || 'top';

        // Flip when the preferred side has no room.
        if (placement === 'top'    && tr.top - bh - GAP < PAD)        placement = 'bottom';
        else if (placement === 'bottom' && tr.bottom + bh + GAP > vh - PAD) placement = 'top';
        else if (placement === 'left'   && tr.left - bw - GAP < PAD)        placement = 'right';
        else if (placement === 'right'  && tr.right + bw + GAP > vw - PAD)  placement = 'left';

        let top, left;
        switch (placement) {
            case 'bottom': top = tr.bottom + GAP;            left = tr.left + tr.width / 2 - bw / 2; break;
            case 'left':   top = tr.top + tr.height / 2 - bh / 2; left = tr.left - bw - GAP;         break;
            case 'right':  top = tr.top + tr.height / 2 - bh / 2; left = tr.right + GAP;             break;
            default:       top = tr.top - bh - GAP;          left = tr.left + tr.width / 2 - bw / 2; break;
        }
        // Clamp into the viewport.
        left = Math.max(PAD, Math.min(left, vw - bw - PAD));
        top  = Math.max(PAD, Math.min(top, vh - bh - PAD));

        b.style.top = top + 'px';
        b.style.left = left + 'px';
        b.classList.toggle('from-below', placement === 'bottom');
        // Reveal on the next frame so the enter transition runs.
        requestAnimationFrame(() => { if (current === wrap && wrap.isConnected) b.classList.add('is-open'); });
    }

    function show(wrap) {
        if (current === wrap) return;
        current = wrap;
        place(wrap);
    }

    function hide() {
        current = null;
        if (bubble) bubble.classList.remove('is-open');
    }

    function wrapFrom(e) {
        return e.target instanceof Element ? e.target.closest('[data-tt]') : null;
    }

    if (!window.__drylTooltipBound) {
        window.__drylTooltipBound = true;
        document.addEventListener('pointerover', e => {
            const w = wrapFrom(e);
            if (w) show(w); else if (current) hide();
        }, true);
        // Hide when the pointer leaves the current wrap — including the case
        // where the trigger is removed from the DOM mid-hover (Blazor
        // re-render): a detached node fires no pointerover, but pointerout
        // does fire on the way out, and the isConnected guard in place()
        // covers programmatic removal.
        document.addEventListener('pointerout', e => {
            if (!current) return;
            const to = e.relatedTarget;
            if (!(to instanceof Element) || to.closest('[data-tt]') !== current) hide();
        }, true);
        document.addEventListener('focusin', e => {
            const w = wrapFrom(e);
            if (w) show(w);
        }, true);
        document.addEventListener('focusout', () => hide(), true);
        document.addEventListener('pointerdown', () => hide(), true);
        window.addEventListener('scroll', () => hide(), true);
        window.addEventListener('resize', () => hide());
    }

    return { hide };
})();

