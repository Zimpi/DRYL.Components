# Agent Tools — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

## Interop

none — the three dialogs and both tool sets call no JavaScript. Motion comes
from the core dialog frame and provider.

## Services

The category registers no service. It depends on two from the core package:

- **`IDrylDialogService`** — registered by `AddDrylComponents()`; passed to
  `DrylUiTools.Create`, which captures it, so the tool set is bound to the
  circuit it was created in.
- **`DrylDialogProvider`** — must be mounted in the layout; it renders the
  three dialogs and supplies their cascading `IDrylDialogInstance`.

`DrylDisplayTools.Create` takes no service.

## Cleanup

- **Open dialogs follow the run.** `ask_choice`, `ask_multi_choice` and
  `ask_text` register on the run's cancellation token and cancel their dialog
  when it fires; the registration is disposed once the dialog has answered.
- **`request_permission` does not close its dialog** on cancellation: it stops
  waiting and returns the denied answer, and the confirm dialog stays open
  until the user dismisses it.
- The dialogs hold no subscription or timer of their own.
