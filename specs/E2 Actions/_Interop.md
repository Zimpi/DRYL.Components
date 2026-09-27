# Actions — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

The category is small and almost entirely inert: `DrylButton`, `DrylButtonGroup`
and `DrylSplitButton` are markup plus class-list composition. `DrylCopyButton` is
the one component that calls into `dryl.js`, for the clipboard.

## Interop

One entry point, used by one component:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.clipboard.copy(text)` | `DrylCopyButton.CopyAsync` on press. Writes `text` through the async Clipboard API in a secure context, otherwise through a hidden `textarea` and `execCommand('copy')`, and returns whether it worked. The same helper serves `DrylCodeBlock` (`E5 Data`). |

`DrylCopyButton` injects `IJSRuntime` for that call and nothing else. It declares
no `[JSInvokable]` method, hands out no `DotNetObjectReference` and attaches no
listener, so there is no handle to release. A `JSException` or a cancelled call
counts as a failed copy; a `JSDisconnectedException` is swallowed, because there is
no one left to tell. The call only happens on a press, which cannot occur during
static prerender.

`DrylButton`, `DrylButtonGroup` and `DrylSplitButton` make no interop call:
`rg -n 'IJSRuntime|JSInvokable|DotNetObjectReference' code/DRYL.Components/Components/Actions/`
matches `DrylCopyButton.razor` only.

The `AuraLifecycle` that `DrylButton` composes is deliberately prerender-safe: it
drives the aura's mount and fade with `Task.Delay` plus the host's re-render
callback and never touches JS. So AI mode adds no interop here either.

### What a composed dependency uses

`DrylSplitButton` renders a `DrylMenu`, which is built on `DrylPopover`, and that
pair **does** use JS — panel and trigger focus, the portal to `<body>`, arrow-key
navigation inside the panel, and click-outside dismissal, including a callback
from JS back into `DrylPopover`.

That interop belongs to `E10 Navigation` (`DrylMenu`) and `E11 Surfaces`
(`DrylPopover`) and is documented there, in
[`../E10 Navigation/_Interop.md`](../E10%20Navigation/_Interop.md) and
[`../E11 Surfaces/_Interop.md`](../E11%20Surfaces/_Interop.md). It is named here
because a consumer of `DrylSplitButton` will observe the behaviour it produces,
and it is **not** tabulated here because Actions neither calls those entry points
nor owns their contract. `DrylSplitButton` sets exactly two parameters on the
composed menu — `Placement` and `Label` — and fills its `Trigger` and `Items`
slots, the first with the caret `DrylButton`; it holds no open state. See
`F3 DrylSplitButton.md`.

## Services

This category registers no service. The only `@inject` is `DrylCopyButton`'s
framework `IJSRuntime`; there is no `[Inject]` in any of the four components, and
`AddDrylComponents()` in
`code/DRYL.Components/Extensions/ServiceCollectionExtensions.cs` registers nothing
on their behalf — every entry it makes belongs to another category.

`DrylButton` and `DrylSplitButton` each read one ambient value, and it is not a
service: the `[CascadingParameter]` named `Scope`, of type
`DRYL.Components.Ai.AiScope`, supplied by a surrounding `DrylAiScope`. Both
inherit it from `DrylAiAware`. A cascading parameter needs no registration and no
resolution guard, which is why either component works unchanged in an application
that never called `AddDrylComponents()`.

## Cleanup

Two duties exist in this category, on two components.

| Component | Contract | Released |
|---|---|---|
| `DrylButton` | `IDisposable` | Its `AuraLifecycle`. |
| `DrylCopyButton` | `IDisposable` | The `CancellationTokenSource` behind the pending return to rest; a cancelled or post-disposal timer never renders. |
| `DrylButtonGroup` | none | It holds nothing: no timer, no subscription, no interop handle. |
| `DrylSplitButton` | none | The same. |

`AuraLifecycle.Dispose()` cancels and drops the one `CancellationTokenSource` the
lifecycle may be holding. That token backs exactly two pending delays, only one of
which can be live at a time: the exit fade scheduled when the effective state
drops to `AiState.None`, and the `AiState.Generated` retirement that holds the
one-shot on screen before fading it. Cancelling it stops the pending
`Task.Delay` and the re-render callback that would follow it, so a button disposed
mid-fade leaves no continuation pointing at a component that is gone. There is no
JS handle to release, which is why the duty is synchronous `IDisposable` rather
than `IAsyncDisposable` (`CODE-05`).

`DrylButtonGroup` and `DrylSplitButton` implement neither disposal interface, and
neither needs one. `DrylButtonGroup` renders a `div` around content it does not
own. `DrylSplitButton` renders a wrapper around child components — two
`DrylButton`s and a `DrylMenu` — and each of those is disposed by the renderer as
part of the same subtree, so the main button's `AuraLifecycle` and the popover's
own async cleanup are released without this component taking a duty on. It holds
no handle, no timer and no subscription of its own.
