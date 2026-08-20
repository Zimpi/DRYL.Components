# DrylAlertDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Dialogs/DrylAlertDialog.razor

## User Story

As a Blazor developer building an app on DRYL, I want to tell the user something
and wait until they have seen it, in one awaited line of C#, so that a message
that must not be missed does not cost me a component of its own.

## Description

`DrylAlertDialog` is the dialog behind `IDrylDialogService.ShowAlertAsync`: a
message and a single acknowledging button. It is the one-answer sibling of
[`F3 DrylConfirmDialog`](F3%20DrylConfirmDialog.md), and everything said there
about being service-driven applies here — a consumer calls the service, they do
not place this component.

The difference from a toast is the point of it. A toast informs and disappears;
an alert **blocks until acknowledged**. Choosing between them is a choice about
whether the user may miss the message, and this component is the "may not" half.

Its result is deliberately uninteresting: whatever the user does — the button,
the close button, `Escape`, the backdrop — the caller's await returns and the
flow continues. There is nothing to branch on, and the spec says so rather than
leaving a caller to check a `Canceled` flag that carries no meaning here.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Message` | `string` | `""` | The message shown in the body. |
| `OkLabel` | `string` | `"OK"` | Label of the acknowledging button. |
| `Instance` | `IDrylDialogInstance` | — | Cascading, supplied by `DrylDialogProvider`. Required. |

The dialog's title is not a parameter of this component: it is the title passed
to `ShowAlertAsync`, which reaches the header through the instance.

## Acceptance Criteria

### Content

- The component renders a `DrylDialog` as its frame.
- The body renders `Message` and nothing else.
- The footer renders exactly one button.
- The button renders `OkLabel`.
- The button is rendered as the primary action.

### Acknowledging

- Activating the button closes the dialog with a non-cancelled result.
- `ShowAlertAsync` returns only once the dialog has closed.
- `ShowAlertAsync` returns whichever way the dialog was dismissed, so a caller
  never has to branch on the outcome.
- The awaited result is produced exactly once, however the dialog was dismissed.
- The dialog cannot be dismissed by any means the caller's `DialogOptions` have
  switched off, so an alert that must be acknowledged explicitly is configurable
  through `CloseOnEscape` and `CloseOnBackdropClick`.

### Defaults from the service

- `ShowAlertAsync` opens the dialog at `DialogSize.Small` when the caller passes
  no options.
- `ShowAlertAsync` uses the caller's `DialogOptions` unchanged when they pass
  one, including its size.
- The acknowledging label defaults to an English one, and a caller who needs
  another language passes it.

### Motion

- The dialog enters and exits with the frame's own animation (`F1`, `F2`); the
  component adds no motion of its own and needs none — it is content inside an
  animated shell.

### Keyboard and accessibility

- The button is reachable by `Tab` and operable by `Enter` and `Space`, as an
  ordinary button.
- Focus lands inside the dialog when it opens, on its first focusable element —
  the header's close button while it is shown, the acknowledging button
  otherwise (`F2`).
- `Tab` cycles within the dialog and never reaches the page behind it (`F2`).
- The dialog is announced by its title, through the frame's labelling (`F1`).

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The message is rendered in `--fg-muted`, the body text color of the frame.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`).

### AI mode

- The component takes **no `Ai` parameter**, deliberately (`AI-05`). An alert
  reports a finished fact; there is no ongoing operation for an aura to
  describe. A caller who is announcing the end of an AI operation sets
  `DialogOptions.Ai` and the frame carries it, which is the right level for it.

## Recorded gaps

- The body paragraph carries an inline `style` with a literal margin, for the
  same reason as in `F3`. Recorded as debt.
- The message is not referenced by `aria-describedby` on the dialog root, and
  the dialog carries `role="dialog"` rather than `role="alertdialog"`. For a
  component whose whole purpose is that the message is not missed, the more
  specific role is the better fit. Recorded as a gap; changing it is a behaviour
  change and belongs in an idea, not in a spec edit (`SPEC-01`).
- `Instance` is declared non-nullable and the component would fail if it were
  rendered outside `DrylDialogProvider`. True by construction, not enforced.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`.
- **Enter/exit animation** — inherited from the frame and the provider's layer;
  no exception is needed, the component is animated by the shell it renders into
  (`F1`, `F2`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above, plus
  the recorded role and `aria-describedby` gaps.
- **AI mode** — an explicit **no**, with its reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Examples/Dialog/ConfirmAlert.razor`
  on `Components/Pages/DemoDialog.razor`.
- **`ComponentCatalog`** — covered by the `"Dialog"` / `dialog` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`. It has no entry of its own and
  should not: it is the shape `ShowAlertAsync` takes, documented on that page.
