# DrylAskChoiceDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Tools/DrylAskChoiceDialog.razor

## User Story

As a Blazor developer who hands `DrylUiTools` to an agent, I want the model's
"pick one of these" question to open as a DRYL dialog with the model's
recommendation pre-selected, so that my agent can ask my user an either/or
question mid-run and wait for the answer without me building the dialog.

## Description

`DrylAskChoiceDialog` is the dialog behind the `ask_choice` tool of
`DrylUiTools` (see [`_Api.md`](_Api.md)). It is a `DrylDialog` with content: the
question, a radio list of the options, a "Recommended" badge beside the
model's recommendation, and a Cancel / Confirm pair. A consumer does not place
it; the tool opens it through `IDrylDialogService.ShowAsync` with the title
"Choose", a depth-glass handoff, and awaits the `DialogResult`.

Confirming closes with the selected option as a `string`; cancelling —
the button, the frame's close button, `Escape` or the backdrop — closes with a
cancelled result. The tool turns either into the string the model receives.

## Public API

Namespace: `DRYL.Components.Agents.Tools`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Question` | `string` | `""` | The question shown above the options. |
| `Options` | `IReadOnlyList<string>` | empty | The selectable options, in order. |
| `Recommended` | `string?` | `null` | The model's recommendation; badged and selected initially. |
| `Instance` | `IDrylDialogInstance` | — | Cascading, supplied by `DrylDialogProvider`. Required. |

The parameters are set by the `ask_choice` tool from the model's arguments.

## Acceptance Criteria

### Content

- The component renders a `DrylDialog` as its frame.
- The body renders `Question` in a paragraph above the options.
- The body renders one `DrylRadioGroup` of `string`.
- The group renders one `DrylRadio` per entry of `Options`, in order, labelled
  with the option's text.
- An option that equals `Recommended`, ignoring case, renders a
  `DrylBadge.BadgeKind.Accent` badge reading "Recommended" beside it.
- No other option renders a badge.
- The footer renders exactly two buttons: "Cancel" first, "Confirm" second.
- "Cancel" uses `DrylButton.ButtonVariant.Ghost`; "Confirm" uses
  `DrylButton.ButtonVariant.Primary`.

### Selection

- With `Recommended` set, it is the initial selection.
- Without `Recommended`, the first option is the initial selection.
- Without `Recommended` and without options, nothing is selected.
- Choosing a radio changes the selection to that option.

### Answering

- Activating "Confirm" closes the dialog with a non-cancelled result whose data
  is the selected option.
- Activating "Confirm" with nothing selected closes with an empty string as
  data.
- Activating "Cancel" closes the dialog with a cancelled result.
- The frame's close button, `Escape` and a backdrop click produce the same
  cancelled result, as far as the dialog's options allow them
  (`F2 DrylDialogProvider` in `E6 Dialogs`).

### Through the tool

- The `ask_choice` tool opens this dialog titled "Choose".
- The tool opens it with `AnimateHandoff` on and
  `DrylMorphStyle.DepthGlass` as handoff style.
- The tool returns the confirmed option's text to the model.
- The tool returns "The user cancelled the question." for a cancelled result.
- Cancelling the run's token while the dialog is open cancels the dialog.

### Motion

- The dialog enters and exits with the frame's and the provider's own
  animation, including the depth-glass handoff when it replaces another
  dialog; the component adds no motion of its own.

### Keyboard and accessibility

- The options are native radios in one group: `Tab` reaches the group, arrow
  keys move the selection.
- Both buttons are reachable by `Tab` and operable by `Enter` and `Space`.
- The dialog is announced by its title through the frame's labelling.

### Appearance

- The question reads in `--fg-muted`, set off from the options by `--sp-3`.
- An option row aligns radio and badge centrally, spaced by `--sp-2`.

## Recorded debt

- **Fixed English labels.** "Cancel", "Confirm", "Recommended" and the tool's
  title "Choose" are not parameters, and the tool exposes no way to pass them.
- **Inline styles.** The question paragraph and each option row carry an
  inline `style` (token-based, but outside the stylesheet).
- **Recommendation matching is inconsistent.** The badge matches `Recommended`
  ignoring case, the initial selection uses it verbatim: a recommendation that
  differs from its option in case is badged but not selected, and one that is
  not among `Options` becomes a selection no radio shows — and is returned on
  "Confirm".
- **The question is not the group's name.** The radio group is not labelled by
  the question paragraph (`aria-labelledby`).
- **"(no selection)" is unreachable.** An empty confirm returns an empty
  string, so the tool's "(no selection)" fallback never applies.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of `DrylDialog`,
  `DrylRadio`, `DrylBadge` and `DrylButton`; inspected on `/components/human-tools`, dialog opened by the demo agent, in dark and light (2026-11-20).
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
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAskChoiceDialogTests.cs`
  and `tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs`.
