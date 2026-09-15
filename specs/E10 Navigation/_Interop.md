# Navigation — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

This reference currently covers `DrylStepper` and `DrylStep`. Other navigation
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
