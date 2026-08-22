# Foundation — Interop

The JS interop surface, DI services and cleanup duties that belong to the
library as a whole rather than to one category.

*Scaffold. Filled in during phase C.*

## Interop

| Entry point | Used by | Purpose |
|---|---|---|
| `dryl.morph.capture` | `DrylMorphEngine` (the service behind `IDrylMorph`) | Measures every `[data-dryl-morph]` element in the document before a state change. Reads all rects before writing anything, so one morph costs one layout pass rather than one per target. |
| `dryl.morph.play` | `DrylMorphEngine` | Measures again after the render and animates each target from where it was to where it now is; a target that changed size hands over to a clone of its old face, which rides the same curve and fades out. Does nothing when the user prefers reduced motion. |

*(the rest: phase C)*

## Services

| Service | Lifetime | Registered by | Used by |
|---|---|---|---|
| `IDrylMorph` | scoped | `AddDrylComponents()` | `DrylRouteTransition` — calls `BeginNavigation` from a location-changing handler. `DrylMorph` reports every render to the same instance, which is what completes a navigation's morph. |

*(the rest: phase C)*

## Cleanup

`DrylRouteTransition` disposes the registration returned by
`RegisterLocationChangingHandler` (`CODE-05`); without it a torn-down host would
keep starting transitions. It makes no interop call of its own and holds no
`IJSObjectReference`.

`DrylMorphEngine` owns a `DotNetObjectReference` to itself and disposes it,
releasing any in-flight wait first so a disposed circuit cannot leave a
navigation blocked.

*(the rest: phase C)*
