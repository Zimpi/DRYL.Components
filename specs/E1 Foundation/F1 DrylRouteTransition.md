# DrylRouteTransition

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylRouteTransition.razor

## User Story

As a Blazor developer, I want a route change to morph instead of cutting, so
that navigating from an overview page to a detail page carries the object the
user pressed across with them — without me hooking into the router or timing
anything myself.

## Description

`DrylRouteTransition` extends the shared-element morph across a **real route
change**. `DrylMorph` already covers a switch that happens on one route; this
covers `/planets` → `/planets/42`, a `NavLink`, and a Back button.

A consumer mounts it once, next to `DrylDialogProvider` in the layout, and puts
a `DrylMorph` with the same `Name` on both pages. Nothing else is wired up.

It works by starting the transition in a location-changing handler and then
getting out of the way. It never prevents, cancels or restarts a navigation, so
the history stack and the browser's Back and Forward buttons behave exactly as
they would without it. The old snapshot is taken when the handler runs; the
transition is then held open until a `DrylMorph` on the destination page reports
its render, at which point the browser takes the new snapshot and morphs.

Because a destination page can fail to report — it may carry no `DrylMorph` at
all, or be waiting on data that never arrives — the component always carries a
bail. `Timeout` bounds how long the previous frame can be held; when it elapses
the navigation simply completes without a morph. The component holds the old
frame, never the application.

The component renders no markup.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ShouldMorph` | `Func<string, bool>?` | `null` | Given the target URI, decides whether that navigation morphs. `null` morphs every internal navigation. |
| `Timeout` | `TimeSpan` | 1 second | How long the old frame may be held waiting for the destination to report a render, before the navigation completes morph-free. |

`IDrylViewTransition` gains one member for this, documented in
[`_Api.md`](_Api.md):

| Member | Purpose |
|---|---|
| `BeginNavigation(TimeSpan timeout)` | Starts a transition that a coming navigation completes. Ships as a **default interface implementation** that does nothing, so an existing implementer of the interface keeps compiling and simply never morphs a navigation. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Mounting and rendering

- The component renders no markup.
- The component registers its location-changing handler when it is first
  rendered.
- The component removes its handler when it is disposed.
- A second instance mounted by mistake does not start two transitions for one
  navigation.

### Starting a transition

- An internal navigation starts a view transition before the router changes the
  route.
- The component does not await the transition inside the handler, so the
  navigation is never delayed by it.
- The component never calls `PreventNavigation`, so a navigation always
  proceeds.
- The browser's Back and Forward buttons navigate exactly as they do without the
  component present.

### Choosing which navigations morph

- `ShouldMorph` left `null` morphs every internal navigation.
- `ShouldMorph` returning `false` for a target leaves that navigation with no
  transition at all.
- `ShouldMorph` receives the target URI of the navigation.
- `ShouldMorph` returning `true` morphs that navigation.

### Completing the transition

- The transition completes when a `DrylMorph` on the destination page reports
  its render.
- A render reported *before* the JS bridge asks for it still completes the
  transition, rather than being lost.
- The component reports a render of its own once the new route has rendered, so
  a destination carrying no `DrylMorph` completes the transition immediately
  instead of waiting out `Timeout`.
- The component's own report is made after the route reached the DOM, never
  before, so it does not pre-empt a destination that does carry a hull.
- A destination that is still loading its data completes the transition on its
  first render, morphing onto whatever placeholder it shows rather than holding
  the previous page.

### The bail

- `Timeout` defaults to one second.
- The transition completes without a morph when `Timeout` elapses with no render
  reported at all — the second net under the component's own report, for a route
  that never renders.
- The old frame is never held longer than `Timeout`.
- A navigation whose transition bailed still arrives at its destination.

### Behaviour where the morph cannot run

- The component renders nothing and registers nothing during prerender.
- Navigation behaves identically when the browser has no View Transition API:
  the route change happens, morph-free.
- Navigation behaves identically under `prefers-reduced-motion`, since the
  shared bridge already falls back to a direct apply.
- A disconnected circuit does not leave a navigation blocked.

### Keyboard and accessibility

- The component renders nothing, so it adds no focus stop, no landmark and no
  announcement.
- The component does not move focus; where focus lands after a route change
  stays the application's own concern (`FocusOnNavigate` and the like keep
  working).
- The component changes nothing about how a link or a `NavLink` is operated by
  keyboard.

### Appearance

- The component names no color, length, duration or easing (`DESIGN-01`); the
  morph is the shared `::view-transition-*` vocabulary in `dryl.css`.
- The component adds no stylesheet of its own.
- The component renders nothing, so it has no appearance to differ between color
  modes (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the component has no surface. It renders
  nothing at all, so there is nothing for an aura to sit on, and a navigation is
  not an AI activity. A page that *is* AI-driven carries its own `Ai` on the
  surfaces it morphs into.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders nothing; there is no per-mode
  value to hold. `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` are unaffected. The morph itself is
  checked by eye in both modes on the demo page.
- **Enter/exit animation** — the component *is* the enter animation of a route.
  It has none of its own and renders no element that could carry one; this is
  the written exception `DESIGN-11` allows.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that focus handling is left alone: a component that
  moved focus on every route change would fight `FocusOnNavigate`.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoRouteTransition.razor`,
  which needs two real routes to demonstrate anything, so it carries an overview
  route and a detail route rather than a switch on one page.
- **`ComponentCatalog`** — registered as `"Route Transition"` /
  `route-transition` in `DRYL.Website/Components/ComponentCatalog.cs`, flagged
  not AI-capable.
