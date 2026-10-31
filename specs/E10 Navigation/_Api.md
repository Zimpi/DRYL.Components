# Navigation — Public API

Shared enums, parameter contracts and services of the Navigation category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Navigation/`

This reference currently covers the Stepper family and the command palette. Other navigation types
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
`Active`, `Thinking`, `Streaming`, `Generated`, `Proposed`. `Ai` defaults to `AiState.None`.

## `AiAura` (shared)

`AiAura` is defined in `code/DRYL.Components/AiAura.cs` with members `Comet` and
`Aurora`. Nullable `Aura` resolves through the surrounding `AiScope` variant,
then defaults to `AiAura.Comet`. `DrylStep` inherits the variant only; its AI
state is the explicitly supplied `Ai`. These are shared types, not new
navigation-specific enums (`AI-01`, `AI-03`).

## `CommandItem`

Defined in `code/DRYL.Components/Models/CommandItem.cs`, namespace
`DRYL.Components`. A result or static entry of `DrylCommandPalette` — the
**items** path, equal to the **commands** path of `DrylCommand`.

| Member | Type | Meaning |
|---|---|---|
| `Id` | `string?` | Stable identity across searches; keys the row. Falls back to `Category` + `Label`. |
| `Label` | `string` | The row's label. |
| `Description` | `string?` | Second line. |
| `Meta` | `string?` | Short muted fact at the trailing end. |
| `Icon` | `string?` | Icon name. |
| `Category` | `string?` | Group header. |
| `Type` | `CommandItemType` | How the item runs; defaults to `Action`. |
| `Href` | `string?` | Target of a `Navigate` item. |
| `Action` | `EventCallback` | Callback of an `Action` item. |
| `AiAction` | `EventCallback` | Callback of an `AiIntent` item. |

## `CommandItemType`

Defined beside `CommandItem`.

| Member | Meaning |
|---|---|
| `Navigate` | Closes the palette and navigates to `Href`. |
| `Action` | Closes the palette and invokes `Action`. |
| `AiIntent` | Keeps the palette open and invokes `AiAction`. |

## `CommandGroupOrder`

Defined in `code/DRYL.Components/Components/Navigation/CommandGroupOrder.cs`.

| Member | Meaning |
|---|---|
| `Alphabetical` | Groups by name; rows without a group last. The default of `DrylCommandPalette.GroupOrder`. |
| `AsProvided` | Groups in the order their first row arrives. |

## `CommandPaletteTexts`

Defined in `code/DRYL.Components/Components/Navigation/CommandPaletteTexts.cs`.
A record of `init` strings with English defaults, reached through
`CommandPaletteTexts.Default`: `Placeholder`, `AriaLabel`, `EmptyText`,
`Loading`, `Back`, `Run`, `Thinking`, `AiResult`, `ConfirmTitle`, `Confirm`,
`Cancel`, `HintNavigate`, `HintRun`, `HintGroups`, `HintClose`. Translate with
`CommandPaletteTexts.Default with { … }`.

## `CommandArgType`

Defined in `code/DRYL.Components/Components/Navigation/CommandArgType.cs`:
`Text` (default), `Number`, `Boolean`, `Choice`.

## `ICommandRegistry`

Defined in `code/DRYL.Components/Components/Navigation/ICommandRegistry.cs`,
registered scoped by `AddDrylComponents()` as `CommandRegistry`. Members:
`Commands` (registration order), `Add(DrylCommand)` (ignores a known id),
`Remove(string id)`, event `OnChanged`.

## `ICommandResolver`

Defined in `code/DRYL.Components/Components/Navigation/ICommandResolver.cs`.
`ResolveAsync(string query, IReadOnlyList<DrylCommand> commands,
CancellationToken ct)` returns a `CommandResolution?`. It never runs the
command. The core ships no implementation.

## `CommandResolution`

Defined in `code/DRYL.Components/Components/Navigation/CommandResolution.cs`: a
record of `Command` (`DrylCommand`), `Arguments`
(`IReadOnlyDictionary<string, object?>`) and `Confidence` (`double`, 0–1).

## `CommandContext`

Defined in `code/DRYL.Components/Components/Navigation/CommandContext.cs`. The
payload of `DrylCommand.OnRun`: `Arguments`, `CancellationToken` and
`GetArgument<T>(string name)`.
