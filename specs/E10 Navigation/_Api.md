# Navigation — Public API

Shared enums, parameter contracts and services of the Navigation category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Navigation/`

This reference currently covers the Stepper family. Other navigation types
remain phase-C work. It carries no `Meta` block and claims no component
(`SPEC-03`).

## `StepperOrientation`

Defined in `code/DRYL.Components/Components/Navigation/StepperOrientation.cs`,
namespace `DRYL.Components`.

| Member | Meaning |
|---|---|
| `Horizontal` | Header track runs in a row; the default `DrylStepper.Orientation`. |
| `Vertical` | Header track runs in a column. |

## `StepState`

Defined in `code/DRYL.Components/Components/Navigation/StepState.cs`, namespace
`DRYL.Components`.

| Member | Meaning |
|---|---|
| `Pending` | Default progress state; the parent still decides whether the header is active. |
| `Active` | Declared active state; it does not override the parent's selected identifier. |
| `Completed` | Uses the completed indicator and connector appearance. |
| `Error` | Uses the error indicator appearance. |

The parent tests `Completed` and `Error` explicitly. For both `Pending` and
`Active`, its selected identifier decides the active-versus-pending indicator.
Selection and progress state are separate; selection does not complete a step.

## `DrylStepper` / `DrylStep` parameter boundary

`DrylStepper` exposes `ActiveStep`, `ActiveStepChanged`, `Orientation`,
`AriaLabel`, `ChildContent`, `Class` and `AdditionalAttributes`. Full types and
defaults are in [`F2 DrylStepper.md`](F2%20DrylStepper.md).

`DrylStep` exposes `Id`, `Label`, `Description`, `Icon`, `StepState`, `Disabled`,
`Ai`, `Aura` and `ChildContent`. Full types and defaults are in
[`F3 DrylStep.md`](F3%20DrylStep.md).

The parent cascades itself as a fixed value to its declarations. The child
registers against this parent and renders no element of its own. A stable,
unique, nonempty `Id` is the consumer's binding key. There is no public step
collection, registration method, navigation method, validation hook or
completion callback.

## `AiState` (shared)

`AiState` is defined in `code/DRYL.Components/AiState.cs` with members `None`,
`Active`, `Thinking`, `Streaming`, `Generated`. `Ai` defaults to `AiState.None`.

## `AiAura` (shared)

`AiAura` is defined in `code/DRYL.Components/AiAura.cs` with members `Comet` and
`Aurora`. Nullable `Aura` resolves through the surrounding `AiScope` variant,
then defaults to `AiAura.Comet`. `DrylStep` inherits the variant only; its AI
state is the explicitly supplied `Ai`. These are shared types, not new
navigation-specific enums (`AI-01`, `AI-03`).
