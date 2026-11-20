# DrylAskTextDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Tools/DrylAskTextDialog.razor

## User Story

As a Blazor developer who hands `DrylUiTools` to an agent, I want the model's
free-text question to open as a DRYL dialog with one text field, so that my
agent can ask my user for a name, a detail or a correction mid-run and wait for
the typed answer without me building the dialog.

## Description

`DrylAskTextDialog` is the dialog behind the `ask_text` tool of `DrylUiTools`
(see [`_Api.md`](_Api.md)). It is a `DrylDialog` with content: the question, a
`DrylInputText` with an optional placeholder, and a Cancel / Submit pair. A
consumer does not place it; the tool opens it through
`IDrylDialogService.ShowAsync` with the title "Question", a depth-glass
handoff, and awaits the `DialogResult`.

Submitting — by the button or by `Enter` in the field — closes with the typed
text; cancelling closes with a cancelled result. The tool returns the text to
the model as it is.

## Public API

Namespace: `DRYL.Components.Agents.Tools`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Question` | `string` | `""` | The question shown above the field; also the field's accessible name. |
| `Placeholder` | `string?` | `null` | Optional placeholder of the field. |
| `Instance` | `IDrylDialogInstance` | — | Cascading, supplied by `DrylDialogProvider`. Required. |

The parameters are set by the `ask_text` tool from the model's arguments.

## Acceptance Criteria

### Content

- The component renders a `DrylDialog` as its frame.
- The body renders `Question` in a paragraph above the field.
- The body renders one `DrylInputText` inside a form.
- The field's placeholder is `Placeholder`.
- The field's `AriaLabel` is `Question`.
- The field starts empty.
- The footer renders exactly two buttons: "Cancel" first, "Submit" second.
- "Cancel" uses `DrylButton.ButtonVariant.Ghost`; "Submit" uses
  `DrylButton.ButtonVariant.Primary`.

### Answering

- Activating "Submit" closes the dialog with a non-cancelled result whose data
  is the field's text.
- Pressing `Enter` in the field submits the form and closes the dialog the same
  way.
- Submitting an empty field closes with an empty string as data.
- Activating "Cancel" closes the dialog with a cancelled result.
- The frame's close button, `Escape` and a backdrop click produce the same
  cancelled result, as far as the dialog's options allow them
  (`F2 DrylDialogProvider` in `E6 Dialogs`).

### Through the tool

- The `ask_text` tool opens this dialog titled "Question".
- The tool opens it with `AnimateHandoff` on and
  `DrylMorphStyle.DepthGlass` as handoff style.
- The tool returns the submitted text to the model unchanged.
- The tool returns "The user cancelled the question." for a cancelled result.
- Cancelling the run's token while the dialog is open cancels the dialog.

### Motion

- The dialog enters and exits with the frame's and the provider's own
  animation, including the depth-glass handoff when it replaces another
  dialog; the field animates as `DrylInputText` does; the component adds no
  motion of its own.

### Keyboard and accessibility

- The field and both buttons are reachable by `Tab`.
- The field is named by the question through its `aria-label`.
- The dialog is announced by its title through the frame's labelling.

### Appearance

- The question reads in `--fg-muted`, set off from the field by `--sp-3`.

## Recorded debt

- **Fixed English labels.** "Cancel", "Submit" and the tool's title
  "Question" are not parameters, and the tool exposes no way to pass them.
- **Inline style.** The question paragraph carries an inline `style`
  (token-based, but outside the stylesheet).
- **The question is announced twice.** It is both the visible paragraph and the
  field's `aria-label`, rather than the field being labelled by the paragraph.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of `DrylDialog`,
  `DrylInputText` and `DrylButton`; inspected on `/components/human-tools` and `/components/collaborative-build`, dialog opened by the demo agent, in dark and light (2026-11-20).
- **Enter/exit animation** — inherited from the dialog frame and provider,
  plus the depth-glass handoff the tool requests.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — **no** `Ai` parameter (`AI-05`): the dialog is the user's turn
  to answer, not an operation the AI performs; the calling tool's card shows
  the AI waiting.
- **Demo page** — `/components/human-tools`
  (`DRYL.Website/Components/Pages/DemoHumanTools.razor`), example
  `Agents/HumanTools`.
- **`ComponentCatalog`** — the `"Human-in-the-Loop"` / `human-tools` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAskTextDialogTests.cs`
  and `tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs`.
