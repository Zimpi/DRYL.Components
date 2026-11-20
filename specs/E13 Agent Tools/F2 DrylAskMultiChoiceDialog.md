# DrylAskMultiChoiceDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Tools/DrylAskMultiChoiceDialog.razor

## User Story

As a Blazor developer who hands `DrylUiTools` to an agent, I want the model's
"pick any of these" question to open as a DRYL dialog with the model's
recommendations pre-checked, so that my agent can collect several choices from
my user mid-run without me building the dialog.

## Description

`DrylAskMultiChoiceDialog` is the dialog behind the `ask_multi_choice` tool of
`DrylUiTools` (see [`_Api.md`](_Api.md)). It is a `DrylDialog` with content:
the question, one checkbox per option, a "Recommended" badge beside each
recommended option, and a Cancel / Confirm pair. A consumer does not place it;
the tool opens it through `IDrylDialogService.ShowAsync` with the title
"Choose", a depth-glass handoff, and awaits the `DialogResult`.

Confirming closes with the checked options as a `string[]`, in option order;
cancelling closes with a cancelled result. The tool joins the answer into the
string the model receives.

## Public API

Namespace: `DRYL.Components.Agents.Tools`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Question` | `string` | `""` | The question shown above the options. |
| `Options` | `IReadOnlyList<string>` | empty | The selectable options, in order. |
| `Recommended` | `IReadOnlyList<string>?` | `null` | The model's recommendations; badged and checked initially. |
| `Instance` | `IDrylDialogInstance` | — | Cascading, supplied by `DrylDialogProvider`. Required. |

The parameters are set by the `ask_multi_choice` tool from the model's
arguments.

## Acceptance Criteria

### Content

- The component renders a `DrylDialog` as its frame.
- The body renders `Question` in a paragraph above the options.
- The body renders one `DrylCheckbox` per entry of `Options`, in order,
  labelled with the option's text.
- An option that equals any entry of `Recommended`, ignoring case, renders a
  `DrylBadge.BadgeKind.Accent` badge reading "Recommended" beside it.
- No other option renders a badge.
- The footer renders exactly two buttons: "Cancel" first, "Confirm" second.
- "Cancel" uses `DrylButton.ButtonVariant.Ghost`; "Confirm" uses
  `DrylButton.ButtonVariant.Primary`.

### Selection

- An option that equals an entry of `Recommended`, ignoring case, is checked
  initially.
- Every other option is unchecked initially.
- Without `Recommended`, every option is unchecked initially.
- Toggling a checkbox flips that option alone.

### Answering

- Activating "Confirm" closes the dialog with a non-cancelled result whose data
  is a `string[]` of the checked options.
- That array lists the checked options in `Options` order.
- Activating "Confirm" with nothing checked closes with an empty array.
- Activating "Cancel" closes the dialog with a cancelled result.
- The frame's close button, `Escape` and a backdrop click produce the same
  cancelled result, as far as the dialog's options allow them
  (`F2 DrylDialogProvider` in `E6 Dialogs`).

### Through the tool

- The `ask_multi_choice` tool opens this dialog titled "Choose".
- The tool opens it with `AnimateHandoff` on and
  `DrylMorphStyle.DepthGlass` as handoff style.
- The tool returns the checked options joined by a comma and a space.
- The tool returns "(no selection)" when nothing was checked.
- The tool returns "The user cancelled the question." for a cancelled result.
- Cancelling the run's token while the dialog is open cancels the dialog.

### Motion

- The dialog enters and exits with the frame's and the provider's own
  animation, including the depth-glass handoff when it replaces another
  dialog; the checkboxes animate as `DrylCheckbox` does; the component adds no
  motion of its own.

### Keyboard and accessibility

- Every option is a native checkbox: reachable by `Tab`, toggled by `Space`.
- Both buttons are reachable by `Tab` and operable by `Enter` and `Space`.
- The dialog is announced by its title through the frame's labelling.

### Appearance

- The question reads in `--fg-muted`, set off from the options by `--sp-3`.
- An option row aligns checkbox and badge centrally, spaced by `--sp-2`.

## Recorded debt

- **Fixed English labels.** "Cancel", "Confirm", "Recommended" and the tool's
  title "Choose" are not parameters, and the tool exposes no way to pass them.
- **Inline styles.** The question paragraph and each option row carry an
  inline `style` (token-based, but outside the stylesheet).
- **Duplicate options share one checkbox state.** The checked state is keyed by
  option text, so two equal options toggle together and both appear in the
  answer.
- **No group semantics.** The checkboxes are not wrapped in a group labelled by
  the question (`fieldset` / `role="group"` with `aria-labelledby`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of `DrylDialog`,
  `DrylCheckbox`, `DrylBadge` and `DrylButton`; inspected on `/components/human-tools`, dialog opened by the demo agent, in dark and light (2026-11-20).
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
- **Tests** —
  `tests/DRYL.Components.Tests/Agents/DrylAskMultiChoiceDialogTests.cs` and
  `tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs`.
