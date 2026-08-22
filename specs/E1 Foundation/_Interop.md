# Foundation — Interop

The JS interop surface, DI services and cleanup duties that belong to the
library as a whole rather than to one category.

*Scaffold. Filled in during phase C.*

## Interop

| Entry point | Used by | Purpose |
|---|---|---|
| `dryl.viewTransition.start` | `DrylViewTransition` (the service behind `IDrylViewTransition`) | Takes the old snapshot, asks .NET to apply its change, then lets the browser morph old → new. Falls back to a direct, morph-free apply when the API is missing or the user prefers reduced motion. |

*(the rest: phase C)*

## Services

| Service | Lifetime | Registered by | Used by |
|---|---|---|---|
| `IDrylViewTransition` | scoped | `AddDrylComponents()` | `DrylRouteTransition` — calls `BeginNavigation` from a location-changing handler. `DrylMorph` reports every render to the same instance, which is what completes a navigation's morph. |

*(the rest: phase C)*

## Cleanup

`DrylRouteTransition` disposes the registration returned by
`RegisterLocationChangingHandler` (`CODE-05`); without it a torn-down host would
keep starting transitions. It makes no interop call of its own and holds no
`IJSObjectReference`.

`DrylViewTransition` owns a `DotNetObjectReference` to itself and disposes it,
releasing any in-flight wait first so a disposed circuit cannot leave a
navigation blocked.

*(the rest: phase C)*
