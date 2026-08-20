# Dialogs — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

All of it belongs to `DrylDialogProvider`. `DrylDialog`, `DrylConfirmDialog` and
`DrylAlertDialog` call no JS at all: the frame is CSS, and everything below is
the provider's doing.

## Interop

| Entry point | Called by | Purpose |
|---|---|---|
| `dryl.modal.attach(layer, dotNetRef, options)` | `DrylDialogProvider` | Installs the key handler, the focus trap and the body scroll lock on one dialog layer. |
| `dryl.modal.detach(layer)` | `DrylDialogProvider` | Removes them again and hands focus back. |
| `dryl.motion.onExit(layer, dotNetRef, options)` | `DrylDialogProvider` | Reports the end of the dialog's exit animation, so the entry can leave the render tree only once it is invisible. |
| `dryl.motion.clearExit(layer)` | `DrylDialogProvider` | Drops that listener. |
| `IDrylViewTransition.RunAsync` | `DrylDialogProvider` | Wraps a handoff — the predecessor's removal plus the successor's mount — in one browser view transition. |

`dryl.modal.attach` carries three duties that are easy to read as one:

- **`Escape`** invokes back into the dialog's own reference, so the key reaches
  the dialog the user is looking at rather than the topmost one. It is installed
  only when `DialogOptions.CloseOnEscape` is set.
- **The focus trap** cycles `Tab` and `Shift+Tab` inside the layer, and pulls
  focus back in when it has escaped. A layer with nothing focusable takes focus
  itself rather than letting `Tab` walk out.
- **The scroll lock** is reference-counted across layers, so a stack of dialogs
  locks the body once and unlocks it when the last one is gone.

`dryl.modal.detach` restores focus to the element that had it before the dialog
opened — but only when focus is still inside the closing dialog or has been lost
to the body. A follow-up dialog may already own it, and stealing it back would
break that dialog's trap.

The view transition uses a **dedicated** `DrylViewTransition` instance rather
than the DI-scoped one, so a dialog handoff is independent of whatever else in
the host application is mid-transition. It falls back to the plain CSS
cross-fade in browsers without View Transition support, during prerender, and
under reduced motion.

## Services

| Service | Lifetime | Registered by |
|---|---|---|
| `IDrylDialogService` | scoped | `AddDrylComponents()` |
| `IDrylViewTransition` | scoped | `AddDrylComponents()` |

Scoped means one per Blazor circuit, which is what makes the service's dialog
list per-user rather than per-server. `DrylDialogProvider` consumes both by
injection; the category registers nothing itself.

The provider is also the reason the service's three events are public: it
subscribes to them in `OnInitialized` and unsubscribes in `DisposeAsync`. A
provider that is disposed without unsubscribing would keep a dead component
alive through the scoped service for the life of the circuit.

## Cleanup

`DrylDialogProvider` implements `IAsyncDisposable`, and its disposal is not
optional bookkeeping — every item below is a leak, a stuck overlay or a dead
circuit if it is dropped:

- **The three service subscriptions** are removed.
- **Every `DotNetObjectReference`** the provider handed to JS — the escape
  bridge and the exit bridge, per entry — is disposed.
- **`dryl.modal.detach` and `dryl.motion.clearExit`** run for every layer still
  mounted, so no listener outlives its component.
- **The exit watchdog** of every entry is cancelled and disposed.
- **The lazily created `DrylViewTransition`** is disposed.
- Every JS call is wrapped against `JSDisconnectedException`: on Blazor Server a
  circuit can be gone before disposal runs, and a throw there would take the
  teardown down with it.

**The watchdog is a rule, not a detail.** A dialog's entry stays mounted through
its exit animation and is removed when `dryl.motion.onExit` reports the end. If
that report is lost — an interop race, DOM churn, a reduced-motion edge — the
entry would stay in the tree as an invisible, click-eating full-viewport
overlay. A C#-side timer finalizes the entry regardless, and the exiting layer
carries `pointer-events: none` as the second net. Never rely on `animationend`
alone.

`DrylDialog` itself has exactly one cleanup duty and no JS: it implements
`IDisposable` for the `AuraLifecycle` that keeps its AI aura mounted for one
`--dur-slow` beat after the state drops, and disposes it with the component.
