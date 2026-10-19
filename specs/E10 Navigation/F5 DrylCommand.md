# DrylCommand

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylCommand.razor
              code/DRYL.Components/Components/Navigation/DrylCommand.razor.cs
              code/DRYL.Components/Components/Navigation/DrylCommandArgument.cs
              code/DRYL.Components/Components/Navigation/CommandArgType.cs

## User Story

As a Blazor developer, I want to declare a unit of my application's
functionality once — with its title, its arguments and one run handler — so that
it can be run from the command palette by click, by keyboard or through an AI
resolution without three implementations.

## Description

`DrylCommand` renders no DOM of its own. Placed inside a `DrylCommandPalette`, it
registers itself in the palette's `ICommandRegistry` and leaves it again when it
is disposed. Its `DrylCommandArgument` children declare the arguments the
palette asks for in its argument view and that an `ICommandResolver` fills in.

How a command looks and moves in the palette — its row, its keyboard handling,
the argument view, the confirmation of destructive commands — is the palette's
and is specified in
[`F4 DrylCommandPalette/S4 Commands, arguments and the AI seam.md`](F4%20DrylCommandPalette/S4%20Commands%2C%20arguments%20and%20the%20AI%20seam.md).
Having no surface, the command has nothing of its own to animate and no colour
mode to verify (`DESIGN-11` exception); its row is covered by the palette in
both modes.

The AI-mode decision (`AI-05`): **no** `Ai` parameter. A command is not a
surface; it reaches the AI as a tool through the resolver, and the palette
renders the AI state.

The demo is the "Declarative commands" example of the `DRYL.Website` page
`/components/command-palette`; the command is reached through the palette's
`ComponentCatalog` entry.

## Public API

### `DrylCommand`

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `Title` | `string` | — (required) | Row label and the tool's display name. |
| `Description` | `string?` | `null` | Second line of the row and the tool description. |
| `Icon` | `string?` | `null` | Icon name. |
| `Group` | `string?` | `null` | Group header in the palette. |
| `Keywords` | `string[]?` | `null` | Extra search terms. |
| `Shortcut` | `string?` | `null` | Display-only key hint. |
| `Destructive` | `bool` | `false` | Confirmed before running; styled as dangerous. |
| `Disabled` | `bool` | `false` | Not runnable; not offered to a resolver. |
| `OnRun` | `EventCallback<CommandContext>` | — | The one execution path. |
| `Id` | `string?` | `null` | Stable id; derived from `Title` when absent. |
| `ChildContent` | `RenderFragment?` | `null` | Hosts `DrylCommandArgument`s. |

`ResolvedId`, `Arguments`, `AddArgument` and `RunAsync` are read by the palette
and the resolver.

### `DrylCommandArgument`

| Parameter | Type | Default | Meaning |
|---|---|---|---|
| `Name` | `string` | — (required) | Key in `CommandContext.Arguments`. |
| `Description` | `string?` | `null` | Field label and schema description. |
| `Type` | `CommandArgType` | `Text` | `Text`, `Number`, `Boolean` or `Choice`. |
| `Required` | `bool` | `false` | Must be given before the command runs. |
| `Options` | `string[]?` | `null` | Allowed values for `Choice`. |

## Acceptance Criteria

- A command inside a palette is present in the palette's registry after its
  first render.
- A disposed command is no longer in the registry.
- `ResolvedId` equals `Id` when `Id` is set.
- Without `Id`, `ResolvedId` is the lower-case `Title` with every run of
  non-alphanumeric characters replaced by one hyphen.
- A second command with an already registered id is ignored by the registry.
- Every `DrylCommandArgument` child appears once in `Arguments`, in declaration
  order.
- `RunAsync` invokes `OnRun` with the given `CommandContext`.
- `CommandContext.GetArgument<T>` converts culture-invariantly and returns
  `default` for a missing or unconvertible value.
- The command renders no element of its own.
