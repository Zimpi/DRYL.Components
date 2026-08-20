# Charts — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

## Interop

**none** — and this is a property of the category, not a gap in it.

All four charts render from server-computed geometry into SVG paths and
percent-positioned HTML. Hover crosshairs, tooltips, the donut's outward lift
and every entrance animation are CSS. No chart calls `IJSRuntime`, imports a
module from `dryl.js`, measures an element or observes a resize.

Two consequences worth stating, because they are what the decision bought:

- **Static prerendering renders a complete chart.** There is no post-render
  pass that fills anything in, so no chart needs the `_attached` dispose guard
  the JS-interop components carry (`CODE-05`).
- **Hovering costs no roundtrip.** On Blazor Server, moving the pointer across a
  chart produces no circuit traffic at all — the tooltip is a CSS
  `:hover`/`:focus-visible` state, not an event handler.

Responsiveness follows the same rule. The plot area is percent-based and the
donut sizes itself against a container query on its own root; neither needs a
resize observer.

## Services

**none.** No chart injects a service, and the category registers none. The AI
aura resolves through the `DrylAiScope` cascading parameter, which is a
component cascade rather than DI.

## Cleanup

Every chart derives from `DrylChartBase`, which implements `IDisposable` for one
reason: the shared `AuraLifecycle` that keeps the AI aura mounted for one
`--dur-slow` beat after the state drops to `AiState.None`, so it dissolves
instead of snapping. `DrylChartBase.Dispose` disposes it, cancelling the pending
exit timer.

A subclass that needs its own disposal overrides nothing today; if one ever
does, it must keep the base disposal — dropping it leaks a timer that will call
back into a component that is gone.
