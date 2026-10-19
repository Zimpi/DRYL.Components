# Navigation — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

This reference currently covers `DrylStepper`, `DrylStep`, `DrylCommandPalette` and `DrylCommand`. Other navigation
interop remains phase-C work; the statements below make no category-wide claim.

## Interop

None for `DrylStepper` or `DrylStep`. Neither injects `IJSRuntime`, attaches JS
listeners, creates a `DotNetObjectReference` or calls a `dryl.*` entry point.
Header activation uses native buttons and Blazor callbacks.

The I13 panel-motion and forced-colors focus targets are stylesheet behaviour.
Verification must load both global `dryl.css` and the generated isolated CSS
bundle; no new interop API is part of these repairs.

## Services

None registered or injected by the Stepper family. `DrylStepper` cascades its
own instance for child registration. `DrylStep` optionally consumes the
surrounding `DRYL.Components.Ai.AiScope` for aura-variant resolution; it does not
subscribe directly to the AI activity service.

## Cleanup

`DrylStepper.Dispose()` clears its registered-step list.

`DrylStep.Dispose()` disposes its `AuraLifecycle` and then unregisters from the
parent. `AuraLifecycle.Dispose()` cancels pending generated retirement or exit
delay; lifecycle-triggered rendering targets the parent because the child has
no DOM. The shared lifecycle implementation is
`code/DRYL.Components/Ai/AuraLifecycle.cs` (`CODE-05`).

Removing the active declaration asks `ActiveStepChanged` to report the first
remaining enabled identifier, or `null`. `UnregisterStep` invokes this callback
without awaiting it; no async-disposal contract is supplied. Neither component
owns JS timers, pointer capture, observers or document/window listeners.

## DrylCommandPalette and DrylCommand

### Interop

`DrylCommandPalette` uses `dryl.commandpalette` and `dryl.modal` in
`code/DRYL.Components/wwwroot/js/dryl.js`:

| Entry point | When | What |
|---|---|---|
| `dryl.commandpalette.attachGlobal(ref)` | first render, with `HotKey` | Document keydown for Ctrl+K / ⌘K outside text fields → `OnGlobalOpen`. |
| `dryl.commandpalette.detachGlobal(ref)` | disposal | Removes that listener. |
| `dryl.commandpalette.portal(el)` | every open | Moves the `.cmd-layer` wrapper of the backdrop to `<body>`. |
| `dryl.modal.attach(el, ref, opts)` | every open | Focus trap, Escape → `OnEscape`, scroll lock, remembers the focused element. |
| `dryl.modal.detach(el)` | close, disposal | Releases all of that and returns focus. |
| `dryl.commandpalette.attach(backdrop, input, ref)` | every open | Tab / Shift+Tab in the field → `OnGroupKey`; ↑ ↓ keep the caret; `animationend` of a leaving row → `OnRowLeft(key)`. Returns whether reduced motion is on. |
| `dryl.commandpalette.focusInput(el)` | every open | Focuses the search field on the next frame. |
| `dryl.commandpalette.scrollItemIntoView(list, id)` | keyboard moves | Scrolls the focused row into view. |

The overlay's exit is timed by `DrylPresence` through `dryl.motion.onExit`.
`DrylCommand` uses no interop.

### Services

`DrylCommandPalette` resolves `ICommandRegistry` (a private `CommandRegistry`
when none is registered) and, optionally, `IDrylDialogService` for the
confirmation of destructive commands. `DrylCommand` receives the registry as a
cascading value from its palette.

### Cleanup

`DrylCommandPalette.DisposeAsync` disposes its `AuraLifecycle`, unsubscribes
from `ICommandRegistry.OnChanged`, cancels a running search, detaches the global
hotkey and — while open — the modal, and disposes its `DotNetObjectReference`.
The per-open listeners of `dryl.commandpalette.attach` live on the input and the
backdrop and go with those elements. `DrylCommand.Dispose` removes the command
from the registry.
