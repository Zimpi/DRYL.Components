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

### Presence exit ownership (I13)

`dryl.motion.onExit(el, dotnetRef, opts)` registers one exit generation per
element. `opts.name` selects the animation-name prefix (default `presence-out`);
`opts.self` defaults to true and restricts completion to that element. Dialogs
set `self` to false because their animated panel is a descendant of the layer.

- A matching completion reports `OnExitFinished` once and releases all work.
- A cancelled/absent animation and a switch to reduced motion complete the
  current exit without relying on `animationend`.
- `clearExit(el)` cancels its listeners, queued frames and any fallback timer;
  it never reports completion and is safe repeatedly.
- Completion already queued for an old generation must not complete a newer
  generation. The component-side bridge must preserve this identity too.
- Rejected interop promises are handled on completion, including disconnection.
- Normal enter/exit animations retain their existing timing and shape.

These contracts are implemented and covered by the Presence/Popover .NET
suites, deterministic motion tests and the I13 browser regression matrix.
See `docs/2026-09-15-i13-implementation-plan.md` for final evidence and limits.

### Browser regression verification (I13)

Development-only browser tests and an interactive .NET 10 Blazor host live under
`tests/`, outside `DRYL.slnx` and all shipped packages. The host references both
working-tree libraries and loads their global JS/CSS and CSS-isolation bundle.
No website checkout or voice service credential is needed.

- The suite runs Chromium, Firefox and WebKit with recorded versions, both
  color modes, normal/reduced motion, and forced colors on a supporting engine.
- Resource ownership tests repeat attach/gesture/stop/remove cycles and compare
  owned resources to baseline; no late mutation or callback is accepted.
- Voice tests control deferred media/network operations without using a real
  microphone or paid service.
- Motion tests run live; failure screenshots do not replace animation checks
  or constitute I8 appearance baselines.
- Missing browser dependencies fail the browser job rather than skip it.
- CI and publication depend on the same verification workflow for the commit
  being built. Failure prevents package upload.
- The strict spec checker retains its existing default exit behaviour. A
  separate phase-C mode permits only the recorded set of uncovered components,
  rejects new missing specs and every structural error, and requires the debt
  set to shrink as coverage is added.
- Native browser and Windows contrast-theme checks are recorded separately;
  emulation cannot be reported as physical-device or native Safari evidence.

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
