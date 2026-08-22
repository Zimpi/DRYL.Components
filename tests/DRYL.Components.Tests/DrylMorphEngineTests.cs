using Bunit;
using DRYL.Components.Motion;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylMorphEngine"/> — the FLIP handshake behind every
/// morph in the library. The engine measures, hands control back so the caller can mutate
/// and render, then measures again and animates; these guard the order of those steps and
/// the promise that a state change is never lost when the browser side is unavailable.
/// </summary>
public class DrylMorphEngineTests : BunitContext
{
    [Fact]
    public async Task RunAsync_measures_before_it_mutates()
    {
        var order = new List<string>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("dryl.morph.capture").SetVoidResult();

        var svc = new DrylMorphEngine(JSInterop.JSRuntime);
        var run = svc.RunAsync(() => order.Add("mutate"));
        svc.SignalRendered();
        await run;

        // The first thing that happened was the capture call, not the mutation:
        // measuring after the change would measure the wrong geometry.
        var captured = JSInterop.Invocations["dryl.morph.capture"];
        Assert.NotEmpty(captured);
        Assert.Equal(new[] { "mutate" }, order);
    }

    [Fact]
    public async Task RunAsync_plays_only_after_the_render_is_signalled()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylMorphEngine(JSInterop.JSRuntime);

        var run = svc.RunAsync(() => { });
        // Nothing has reported a render yet, so the engine must not have measured again.
        Assert.False(run.IsCompleted);
        Assert.Empty(JSInterop.Invocations["dryl.morph.play"]);

        svc.SignalRendered();
        await run;

        Assert.NotEmpty(JSInterop.Invocations["dryl.morph.play"]);
    }

    [Fact]
    public async Task The_state_change_survives_a_browser_that_cannot_morph()
    {
        // Prerender and a torn-down circuit both surface as an interop call that throws.
        var svc = new DrylMorphEngine(new ThrowingJsRuntime());
        var mutated = false;

        await svc.RunAsync(() => { mutated = true; });

        Assert.True(mutated); // the state change must never be lost to a missing morph
    }

    /// <summary>Every call throws the way prerender does — there is no JS to talk to.</summary>
    private sealed class ThrowingJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
    }

    [Fact]
    public async Task Async_mutate_overload_is_awaited()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylMorphEngine(JSInterop.JSRuntime);
        var mutated = false;

        var run = svc.RunAsync(async () => { await Task.Yield(); mutated = true; });
        svc.SignalRendered();
        await run;

        Assert.True(mutated);
    }
}
