# No focus ring on the heading a route change focuses

## Meta
- **State:** Ready

## Problem

Blazor's `FocusOnNavigate` (part of the default `Routes.razor` template,
`Selector="h1"`) gives the page's first `h1` a `tabindex="-1"` and focuses it
after every navigation. It exists so that a screen reader announces the new
page and so that the next Tab starts at the top of the page.

In a DRYL application that programmatic focus matches `:focus-visible`, and the
global `:focus-visible { outline: 2px solid var(--accent-b); … }` rule in
`dryl.css` draws the accent ring around the whole heading. The result is a
bright cyan box around the page title on every page load and every route
change, although nobody pressed a key and the heading is not something a user
can operate.

The stock Blazor template hides this with `h1:focus { outline: none; }` in the
application's own `app.css`. A DRYL consumer who does not carry that line over
gets the ring, and the library is what draws it.

## Solution Idea

`dryl.css` stops drawing the ring on a heading that is only programmatically
focusable. Focus still moves there — the announcement and the Tab starting
point are untouched — only the outline is not painted.

Three options were weighed:

- **A — a narrow rule in `dryl.css` (recommended).** Suppress the outline for
  headings carrying `tabindex="-1"`. No API, no JavaScript, no token, works for
  every consumer the moment they update. `main` / `[role="main"]` with
  `tabindex="-1"` is covered the same way. Roving-tabindex widgets (`.tab`,
  `.menu-item`, …) are not headings and keep their ring.
- **B — a `DrylFocusOnNavigate` component.** Replaces Blazor's component and
  moves focus somewhere invisible. New public API, new interop, and every
  consumer has to edit `Routes.razor` — more surface for the same visual result.
- **C — documentation only.** Tell consumers to remove `FocusOnNavigate` or to
  add the template's `h1:focus` line themselves. Removing it costs the screen
  reader announcement and the focus reset; the line stays a per-app chore.

## Scope
- **In scope:** the global focus-ring rule in `dryl.css`; the written exception
  in `harness/uiux.md` (`UX-02`); a browser test proving the heading receives
  focus without an outline while an interactive control keeps its ring.
- **Out of scope:** where focus lands after navigation (stays the application's
  concern, as `F1 DrylRouteTransition` already states); any replacement for
  `FocusOnNavigate`; focus rings on interactive elements.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency. `UX-02`
  forbids `outline: none` without a replacement indicator, so this needs a
  written exception there — a non-interactive element that is focusable only by
  script and is not in the tab order. That rule change needs maintainer
  sign-off.
- **Specs:** no component owns the global focus ring, so no spec is added or
  changed; the criterion lives in `UX-02` and its test (see Decisions).
  `specs/E1 Foundation/F1 DrylRouteTransition.md` mentions `FocusOnNavigate`
  and stays correct.
- **Public API:** none.
- **Code:** one rule next to `:focus-visible` in
  `code/DRYL.Components/wwwroot/dryl.css`; the forced-colors block must not
  re-add the outline for the same selector. `tests/DRYL.BrowserHost` already
  ships `FocusOnNavigate` with `Selector="h1"`, so `FocusTests` can cover it.
  Risk: a consumer who deliberately makes a heading a focus target of their own
  widget loses the ring there — considered acceptable, since a heading is never
  an operable control.

## Decisions
- 2026-11-19: Variant A — a narrow rule in `dryl.css`. No new API and no
  consumer change; B and C are dropped. (Product Owner)
- 2026-11-19: The rule covers `h1`–`h6` and `main` / `[role="main"]`, each only
  when carrying `tabindex="-1"` — so `FocusOnNavigate Selector="main"` and
  skip-link targets are covered too. (Product Owner)
- 2026-11-19: Maintainer sign-off for the `UX-02` exception: `outline: none`
  without a replacement indicator is allowed for non-operable elements that are
  focusable only by script and sit outside the tab order. Operable elements
  keep the requirement unchanged. (Product Owner)
- 2026-11-19: No new spec. The record is the `UX-02` exception in
  `harness/uiux.md` plus a `FocusTests` browser test; no component owns the
  global focus ring. (Product Owner)
- 2026-11-19: Final version confirmed; the idea is `Ready`. (Product Owner)

## Open Points
