using Bunit;
using DRYL.Components.Motion;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylViewTransition"/> — the JS↔.NET
/// handshake behind same-document view transitions. The browser-side promise
/// resolution is simulated by calling the JSInvokable ApplyChange directly.
/// </summary>
public class DrylViewTransitionTests : BunitContext
{
    [Fact]
    public async Task ApplyChange_runs_mutate_and_completes_after_SignalRendered()
    {
        var planned = JSInterop.SetupVoid("dryl.viewTransition.start", _ => true);
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        var run = svc.RunAsync(() => { mutated = true; });
        Assert.False(mutated); // the DOM snapshot comes first — mutate waits for the JS callback

        var apply = svc.ApplyChange();
        Assert.True(mutated);            // JS called back → mutate ran
        Assert.False(apply.IsCompleted); // …but the callback resolves only after the render signal

        svc.SignalRendered();
        await apply;

        planned.SetVoidResult(); // browser: t.finished settles
        await run;
    }

    [Fact]
    public async Task RunAsync_applies_mutate_directly_when_js_never_calls_back()
    {
        // Loose interop resolves the start() call without ever invoking
        // ApplyChange — the prerender / disconnected / test-renderer shape.
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        await svc.RunAsync(() => { mutated = true; });

        Assert.True(mutated); // the state change must never be lost
    }

    [Fact]
    public async Task Async_mutate_overload_is_awaited()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        await svc.RunAsync(async () => { await Task.Yield(); mutated = true; });

        Assert.True(mutated);
    }
}
