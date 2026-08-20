# DrylConfirmDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Dialogs/DrylConfirmDialog.razor

## User Story

As a Blazor developer building an app on DRYL, I want to ask the user a yes/no
question in one awaited line of C#, so that guarding a destructive action does
not cost me a component, a parameter bag and a footer full of buttons every time.

## Description

`DrylConfirmDialog` is the dialog behind `IDrylDialogService.ShowConfirmAsync`.
It is a two-button question: a message, a cancel and a confirm. A consumer does
not place it, name it or pass parameters to it — they call `ShowConfirmAsync`
and await a `DialogResult`. The component exists so that the convenience method
has something to show, and so that every confirmation in an app built on DRYL
looks and answers the same way.

It is a `DrylDialog` with content, and nothing more. The frame, the size, the
title, the close button, the focus trap and the dismissal all come from `F1` and
`F2`; this component contributes a paragraph and two buttons.

The two answers are not symmetrical, and that is the point of specifying it:
confirming closes with a positive result, while cancelling — the button, the
close button, `Escape` or the backdrop — all produce the same cancelled result.
A caller therefore has exactly one thing to check.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Message` | `string` | `""` | The question shown in the body. |
| `ConfirmLabel` | `string` | `"Confirm"` | Label of the confirming button. |
| `CancelLabel` | `string` | `"Cancel"` | Label of the cancelling button. |
| `Instance` | `IDrylDialogInstance` | — | Cascading, supplied by `DrylDialogProvider`. Required. |

The parameters are set by `ShowConfirmAsync` from its own arguments; they are
public because the service passes them through a `DialogParameters` bag, not
because the component is meant to be placed by hand.

The dialog's title is not a parameter of this component: it is the title passed
to `ShowConfirmAsync`, which reaches the header through the instance.

## Acceptance Criteria

### Content

- The component renders a `DrylDialog` as its frame.
- The body renders `Message` and nothing else.
- The footer renders exactly two buttons.
- The cancelling button precedes the confirming one, so the safe answer is not
  the one under the cursor's resting position on the primary.
- The confirming button renders `ConfirmLabel`.
- The cancelling button renders `CancelLabel`.
- The confirming button is rendered as the primary action.
- The cancelling button is rendered as a quiet action, so the two answers are
  not equally loud.

### Answering

- Activating the confirming button closes the dialog with a non-cancelled
  result.
- Activating the cancelling button closes the dialog with a cancelled result.
- The header's close button produces the same cancelled result as the cancelling
  button.
- `Escape` produces the same cancelled result, when the caller's options allow
  it (`F2`).
- A backdrop click produces the same cancelled result, when the caller's options
  allow it (`F2`).
- `ShowConfirmAsync` returns only once one of those has happened.
- The awaited result is produced exactly once, however the dialog was dismissed.

### Defaults from the service

- `ShowConfirmAsync` opens the dialog at `DialogSize.Small` when the caller
  passes no options.
- `ShowConfirmAsync` uses the caller's `DialogOptions` unchanged when they pass
  one, including its size.
- The confirm and cancel labels default to English one-word labels, and a caller
  who needs another language passes them.

### Motion

- The dialog enters and exits with the frame's own animation (`F1`, `F2`); the
  component adds no motion of its own and needs none — it is content inside an
  animated shell.

### Keyboard and accessibility

- Both buttons are reachable by `Tab` and operable by `Enter` and `Space`, as
  ordinary buttons.
- Focus lands inside the dialog when it opens, on the first focusable element
  (`F2`).
- Neither button is auto-confirmed by `Enter` from elsewhere in the dialog: a
  confirmation is answered deliberately, not by a stray keystroke.
- The dialog is announced by its title, through the frame's labelling (`F1`).

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The message is rendered in `--fg-muted`, the body text color of the frame.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`).

### AI mode

- The component takes **no `Ai` parameter**, deliberately (`AI-05`). A
  confirmation is a question the user answers, not an operation the AI performs;
  the frame around it can still carry an aura through `DialogOptions.Ai` when a
  caller wants one, which is the right level for it.

## Recorded gaps

- The body paragraph carries an inline `style` with a literal margin. It is
  layout rather than color, and it exists because the component contributes bare
  text into a body it does not style. Recorded as debt.
- The message is not referenced by `aria-describedby` on the dialog root. It is
  read as dialog content by screen readers today, but the explicit association
  is missing. Recorded as a gap, shared with `F4 DrylAlertDialog`.
- `Instance` is declared non-nullable and the component would fail if it were
  rendered outside `DrylDialogProvider`. That is true by construction — the
  service is the only thing that shows it — but it is not enforced.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`.
- **Enter/exit animation** — inherited from the frame and the provider's layer;
  the exception `DESIGN-11` allows is not needed, because the component is
  animated — by the shell it is rendered into (`F1`, `F2`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above, plus
  the recorded `aria-describedby` gap.
- **AI mode** — an explicit **no**, with its reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Examples/Dialog/ConfirmAlert.razor`
  on `Components/Pages/DemoDialog.razor`.
- **`ComponentCatalog`** — covered by the `"Dialog"` / `dialog` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`. It has no entry of its own and
  should not: it is not placed by a consumer, it is the shape
  `ShowConfirmAsync` takes, and that method is documented on the dialog page.
