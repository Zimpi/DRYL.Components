# A route-level transition host

## Meta
- **State:** Ready

## Problem

Step 2 of [`I10 Shared-element transitions between overview and detail.md`](I10%20Shared-element%20transitions%20between%20overview%20and%20detail.md),
which shipped step 1 as `DrylMorph` in 2.25.0 and deliberately left this half
unscoped:

> Als .NET-Entwickler möchte ich … sanfte Shared-Element-Transitions zwischen
> einer Übersicht und einer Detailansicht umzusetzen … damit sich UI-Elemente
> **beim Navigieren** flüssig und konsistent in die Detailansicht überführen.

`DrylMorph` covers the switch that happens *on one route*. A real route change —
`/planets` → `/planets/42`, a `NavLink`, a browser Back button — is not covered
at all today: the `Router` tears down the overview page and builds the detail
page, and the component that has to report the render is one that did not exist
when the transition started.

## The mechanism, worked out before anything is promised

*(Tech Lead. This is the part that decides whether the idea is buildable at
all, so it comes before the options.)*

Blazor's `NavigationManager.RegisterLocationChangingHandler` runs **before** the
navigation and the navigation awaits it. That is the hook — but the obvious use
of it deadlocks:

> start the transition inside the handler and await it → the transition's update
> callback waits for the new page's render → the render cannot happen until the
> handler returns → the handler is waiting on the transition. Deadlock.

The way out is that `document.startViewTransition` **takes the old snapshot
synchronously at the call** and only needs its callback's promise to resolve
once the new DOM is in place. The DOM may change at any point in between. So:

1. The handler calls the JS bridge **without awaiting it**, and returns
   immediately. The old snapshot is now taken.
2. Blazor navigates and renders the new page.
3. The bridge invokes `ApplyChange` on .NET, which — unlike today's mutate-and-
   wait — has nothing to mutate. It only waits for the new page's render.
4. A `DrylMorph` on the new page reports its render, the
   callback resolves, the browser takes the new snapshot and morphs.

Two consequences worth stating plainly:

- **`PreventNavigation` is not needed.** The navigation is never intercepted or
  restarted, so the Back/Forward buttons and the history stack are untouched.
  That removes what would otherwise have been this idea's biggest risk.
- **Steps 3 and 4 can arrive in either order.** If the new page renders before
  the bridge calls back, a plain "wait for the next render" waits forever. The
  render signal has to be a **latch**, not an event.

There is a third consequence that is an actual gap rather than a detail: today's
`IDrylViewTransition` is built around `RunAsync(mutate)`. Navigation has no
mutate delegate — the `Router` does the mutating. The service needs a second
entry point, and it has to be *that* service instance, because `DrylMorph`
reports to the DI-scoped one.

## Solution

**`DrylRouteTransition`** — a component mounted once in the layout, the way
`DrylDialogProvider` is. It registers a location-changing handler, starts the
transition there without awaiting it, and lets the destination page's
`DrylMorph` hulls close the loop. It renders nothing.

- `ShouldMorph` — `Func<string, bool>?`. Given the target URI, decides whether
  this navigation morphs. Null means every internal navigation does.
- `Timeout` — `TimeSpan`, the bail. When no render is reported within it, the
  transition completes without a morph rather than holding the frame.

`IDrylViewTransition` gains **one member with a default implementation** —
`BeginNavigation(TimeSpan)`, defaulting to a no-op. Existing implementers keep
compiling, and an implementation that does not override it simply never morphs
a navigation.

## Scope

- **In scope:**
  - `DrylRouteTransition` with `ShouldMorph` and `Timeout`, rendering nothing.
  - `IDrylViewTransition.BeginNavigation(TimeSpan)` as a default-implemented
    member; the real implementation in `DrylViewTransition`.
  - A **latching** render signal, so a render that arrives before the JS bridge
    calls back is not lost.
  - The bail: the transition completes morph-free when `Timeout` elapses with no
    render reported.
  - A demo page and `ComponentCatalog` entry showing a real route change.
- **Out of scope:**
  - Cross-document (`@view-transition { navigation: auto }`) transitions.
  - Any change to `DrylMorph`'s own behaviour — it already reports every render,
    which is exactly what this needs.
  - Intercepting, cancelling or restarting a navigation. The handler starts the
    transition and gets out of the way; history and the Back button stay
    untouched.
  - Deciding *which elements* morph. That is the destination page's `DrylMorph`
    hulls, as on one route.

## Impact

*(Tech Lead, `IDEA-05`.)*

### Harness

- No new token, no new duration, no new easing, no new `AiState`, no new
  dependency — the morph is the vocabulary `dryl.css` already defines and
  2.25.0 just tuned. **No harness blocker.**
- The bail policy (what happens when no render is ever reported) is new
  *behaviour*, not new visuals. It belongs in the spec before it is code.

### Specs

- A new component. Its category follows its source folder (`SPEC-02`). It is
  mounted once in `Routes.razor` rather than placed on a page, which is exactly
  what `E1 Foundation` / `Components/Providers/` is for — the same reasoning
  that put `DrylMorph` in `E9 Layout` puts this one in Foundation.
- `E1 Foundation` currently has no `F{n}` specs at all (phase C has not reached
  it), so this would be its first.
- Touches `specs/E9 Layout/F17 DrylMorph.md` (the hull becomes the thing that
  closes a *navigation's* loop too) and `specs/E1 Foundation/_Interop.md`.

### Public API

- One new component with two parameters. Additive → MINOR (`REL-01`).
- **`IDrylViewTransition.BeginNavigation(TimeSpan)` as a default interface
  implementation** (settled below), so the post-1.0 interface stays
  non-breaking for implementers and the bump remains MINOR.

### Code

- `code/DRYL.Components/Motion/` — `DrylViewTransition` gains the navigation
  path and a latching render signal.
- A new component under `code/DRYL.Components/Components/Providers/`.
- **Risk — the render latch.** Steps 3 and 4 above racing is the defect that
  would show up as an app frozen behind a held frame. It needs a test.
- **Risk — nothing ever reports.** A destination page with no `DrylMorph` and no
  host-side signal leaves the callback open and the UI holding the old frame.
  This is why a bail policy is not optional.
- **Risk — Blazor Server latency.** Every route change is a round trip; the
  frame is held for its duration. The skeleton policy from I10 bounds it only if
  the destination page actually renders something quickly.

## Decisions

- 2026-08-22 (Product Owner, in I10): staged — the hull first, this second.
- 2026-08-22 (Product Owner, in I10): while a detail page loads, the transition
  **morphs onto the skeleton**; the UI is never frozen waiting for data.
- 2026-08-22 (Tech Lead): the handler does **not** prevent or restart the
  navigation, so history and the Back button stay untouched. The old snapshot is
  taken in the handler; the callback is held open until the new page renders.
- 2026-08-22 (Product Owner): **every internal navigation morphs**, with a
  `ShouldMorph` predicate to exclude individual ones. Wiring each navigation up
  by hand would have cost the "minimaler Code" the original story asked for.
- 2026-08-22 (Product Owner): **a timeout is the bail.** When nothing reports a
  render, the transition finishes morph-free; the UI never holds the old frame
  longer than that. The host does *not* report its own render — that would
  resolve the callback before the destination is really there and flatten the
  morph.
- 2026-08-22 (Product Owner): the new interface member ships as a **default
  interface implementation**, so nobody implementing `IDrylViewTransition`
  breaks and the release stays MINOR.

## Open Points

*(none — awaiting the Product Owner's explicit confirmation of this final
version, the last box of `IDEA-06`, before the state moves to `Ready`.)*
