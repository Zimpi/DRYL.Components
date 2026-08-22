# Foundation — Public API

The public surface that belongs to no single component: theming types, the DI
registration, the AI state vocabulary, the motion primitives, and the token
surface consuming apps are allowed to override.

**Source folder:** `code/DRYL.Components/Components/Providers/`

Foundation is the one category whose subject is not a family of widgets but the
library's own footing — and the five components in that folder are exactly that:
the plumbing a consumer mounts once in the layout rather than places on a page
(`DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal`,
`DrylColorModeToggle`). They moved here from `Components/Surfaces/` on
2026-08-11; the reasoning is in `ideas/I3 Component folder layout.md`.

Alongside them this category still documents the surface that belongs to no
component at all — which is why the sections below exist. This file carries no
`Meta` block: it is a reference for the specs around it, not a unit of
implementation.

*Scaffold. The shared types below are filled in during phase C, each listed with
the exact spelling used in code. Until then this file claims nothing.*

## Theming

*(phase C)*

## AI state vocabulary

*(phase C)*

## Motion primitives

*(mostly phase C. The view-transition surface is documented here because
`DrylRouteTransition` and `DrylMorph` both depend on it and neither owns it.)*

### `DrylMorphStyle`

How much of the morph vocabulary a target gets. Both tiers glide on
`--ease-viscous`; only `DepthGlass` pays for the blur/merge pass.

| Value | Meaning |
|---|---|
| `Glide` | Viscous easing only — the shape glides, no blur/merge pass. Cheap enough for high-frequency interactions. |
| `DepthGlass` | The full choreography — translucency pulse, mercury merge, decoupled clarity. For low-frequency, high-meaning merges. |

### `IDrylMorph`

Animates a state change as a movement of the elements that exist on both sides
of it, using FLIP — measure, re-render, invert, play. Registered scoped by
`AddDrylComponents()`. Targets announce themselves through the DOM
(`data-dryl-morph`), which is what `DrylMorph` renders.

| Member | Purpose |
|---|---|
| `RunAsync(Action mutate)` | Runs `mutate` (which must end in `StateHasChanged()`) inside a morph; completes when the morph has finished. |
| `RunAsync(Func<Task> mutate)` | Async-mutation overload. |
| `SignalRendered()` | Reports that a render reached the DOM. Called unconditionally; a no-op when no transition is in flight. `DrylMorph` calls it for its consumers. |
| `BeginNavigation(TimeSpan timeout)` | Starts a transition that a **coming navigation** completes, rather than one this service mutates itself. Ships as a **default interface implementation that does nothing**, so an existing implementer keeps compiling and simply never morphs a navigation. `timeout` bounds how long the old frame may be held. |

All four fall back to applying the change directly — no measuring, no
movement — during prerender, on a disconnected circuit, and when the user
prefers reduced motion (which the engine checks before it animates anything).

## Token surface

*(phase C — the consumer-overridable custom properties, cross-referenced to
[`../../harness/tokens.md`](../../harness/tokens.md))*
