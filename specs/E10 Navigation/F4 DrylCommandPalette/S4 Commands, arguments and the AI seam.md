# Commands, arguments and the AI seam

## Meta
- **State:** Implemented

## Acceptance Criteria

### Commands

- Every `DrylCommand` placed in `ChildContent` appears as a row while it matches
  the query.
- Commands registered in the `ICommandRegistry` from code appear the same way.
- A change to the registry while the palette is open searches again.
- A command row renders its `Shortcut` as a key hint.
- A `Destructive` command row sets its label in `--danger`.
- A `Disabled` command row is dimmed and cannot be run.
- Running a command without arguments closes the palette and invokes its
  `OnRun` with an empty `CommandContext`.
- Running a command with arguments opens the argument view instead.
- A destructive command asks for confirmation through `IDrylDialogService`
  before it runs, titled `CommandPaletteTexts.ConfirmTitle`.
- Cancelling that confirmation runs nothing.

### The argument view

- The argument view replaces the list and fades in at `--dur-fast`.
- It renders a text field, a toggle or a select per argument, by its
  `CommandArgType`.
- Its back button reads `CommandPaletteTexts.Back` and returns to the list.
- Its run button reads `CommandPaletteTexts.Run` and runs the command with the
  entered values.
- A number argument reaches `OnRun` as a `double`, or `null` when it does not
  parse.

### The AI seam

- With a `Resolver`, a non-blank query is resolved after the results arrive.
- While the resolver works, the search row shows the thinking aura and a pill
  reading `CommandPaletteTexts.Thinking`, which fades in and out.
- A resolution shows as the first row, marked with a sparkle.
- Running the suggestion with all required arguments filled runs the command.
- Running the suggestion with a required argument missing opens the argument
  view, filled with what was resolved.
- A resolver that throws leaves the manual results untouched.
- With `Ai` other than `AiState.None`, an AI result panel with `AiContent`
  slides in below the list, and slides out when `Ai` returns to `AiState.None`.
- The panel is named `CommandPaletteTexts.AiResult` and announced politely.

### Texts

- `Texts` defaults to `CommandPaletteTexts.Default`, whose words are English.
- `CommandPaletteTexts.Default.EmptyText` reads "No results".
- `Placeholder`, `EmptyText` and `AriaLabel` win over the matching members of
  `Texts` when they are set.
- Every word the palette renders of its own comes from `Texts` or from those
  three parameters.
