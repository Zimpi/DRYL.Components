# DrylPresence

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylPresence.razor
              code/DRYL.Components/PresenceTransition.cs
              code/DRYL.Components/PresenceSpeed.cs

## User Story

As a Blazor developer, I want conditional content to remain mounted through its
exit and then be removed reliably, so that closing a surface is both animated
and safe when motion is disabled or interrupted.

## Description

`DrylPresence` owns a wrapping element and the mounted state of `ChildContent`.
`Visible` is authoritative: reopening cancels a pending exit. The wrapper uses
the shared presence classes in `dryl.css`; the browser reports completion through
`dryl.motion.onExit`. This contract adopts the exit portion of I13.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Visible` | `bool` | `false` | Desired visibility. |
| `ChildContent` | `RenderFragment?` | `null` | Content whose lifetime is managed. |
| `Transition` | `PresenceTransition` | `Fade` | Enter/exit shape. |
| `Speed` | `PresenceSpeed` | `Medium` | Shared duration token. |
| `Appear` | `bool` | `false` | Animate the initial visible render. |
| `OnExited` | `EventCallback` | unset | Reports the completed logical exit. |
| `Class` | `string?` | `null` | Classes merged on the wrapper. |
| `OnExitFinished()` | `Task` | — | Compatibility entry point completing the current exit; actual browser registrations use a private callback carrying the exit identity. |
| `DisposeAsync()` | `ValueTask` | — | Invalidates completion and releases interop. |

`PresenceTransition`: `Fade`, `Scale`, `SlideUp`, `SlideDown`, `SlideLeft`,
`SlideRight`. `PresenceSpeed`: `Fast`, `Medium`, `Slow`. Neither enum changes in
I13. The component has no `AdditionalAttributes`, `Ai` or `Aura` parameter.

## Acceptance Criteria

### Visibility and completion

- Initially hidden content renders no wrapper and no children.
- Initially visible content mounts without enter motion unless `Appear` is set.
- A hidden-to-visible change mounts the content with its enter treatment.
- A visible-to-hidden change keeps the same child mounted during its exit.
- Completion sets the mounted state to false and raises `OnExited` exactly once
  for that exit. Duplicate completion does nothing.
- Reopening before completion preserves the child, cancels the old completion,
  and plays the enter treatment. The cancelled exit raises no `OnExited`.
- Completion from an older exit cannot remove content belonging to a newer
  close/reopen sequence, even when its interop callback was already queued.
- Each browser registration owns a distinct completion bridge; cancelling or
  completing that exit releases its .NET interop reference.
- An exit registration that fails because the element or circuit is gone
  completes only its own pending exit and raises no interop exception.
- An absent or cancelled exit animation still completes the current exit.
- Changing to reduced motion during exit completes removal without waiting for
  an animation-end event that will no longer occur.
- Disposal cancels all pending exit work and prevents subsequent component
  callbacks. Repeated disposal is safe; prerender needs no browser cleanup.
- A disconnected circuit does not turn disposal or completion into an unhandled
  exception.

The public parameterless `OnExitFinished()` remains a compatibility call for
the current exit. It cannot identify the source of an arbitrary direct call;
the component never passes itself as the browser's exit callback target.

### Appearance and accessibility

- `Class` merges with the wrapper's `presence` and variant classes.
- `Transition` selects the matching shared presence shape, with `Fade` as the
  fallback for an unmapped value.
- `Speed` maps to `--dur-fast`, `--dur-med` or `--dur-slow`.
- Motion is decorative: the wrapper adds no tab stop, role or live announcement.
- Reduced motion leaves visible content at its usable end state and removes
  hidden content without decorative movement (`UX-06`).
- The same classes and tokens work in both color modes (`DESIGN-02`).
- The wrapper has no AI semantics; the child owns its AI state (`AI-05`).

## Recorded debt

- Attribute passthrough is absent. I13 does not add a public parameter.
- The wrapper cannot infer focus restoration for arbitrary child content; the
  owning dialog/popover/control retains that responsibility.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes / motion:** the I13 browser host exercises normal, reduced,
  absent and interrupted exits in both modes. Until those tests land this is a
  target, not evidence of a pass.
- **Keyboard / AI:** no wrapper focus or AI semantics; exercise interactive child
  content in the browser so retained exits do not break the owner's focus flow.
- **Tests:** `tests/DRYL.Components.Tests/DrylPresenceTests.cs` covers the Blazor
  state machine, stale and duplicate callbacks, disposal during a pending
  registration, registration rejection and the public compatibility entry.
  Before the fix, its stale callback removed the newer exit and disposal still
  raised `OnExited`. The I13 .NET filter for `Presence`, `Dialog` and `Popover`
  passes all 47 cases. Browser and JS lifecycle checks must still establish
  actual animations, preference changes and DOM cleanup.
- **Demo:** `DRYL.Website/Components/Pages/DemoPresence.razor` and
  `Components/Examples/Presence/Toggle.razor`, `Transitions.razor`,
  `Appear.razor` were found in the sibling website checkout.
- **Catalog:** `DRYL.Website/Components/ComponentCatalog.cs` registers
  `Presence` / `presence`, class `DrylPresence`, provider folder, AI flag false.
  Reverify rendered examples during release closure.

## I13 verification — 2026-09-17

The adopted exit/motion contract is implemented. The 1,146-case .NET suite and
10 deterministic motion cases pass; the browser matrix exercises normal,
reduced, missing and cancelled animations in both modes. Final engine results
and platform limits are recorded in `docs/2026-09-15-i13-implementation-plan.md`.
This supersedes the earlier pending-I13 evidence wording; unrelated recorded
debt remains outside this repair.
