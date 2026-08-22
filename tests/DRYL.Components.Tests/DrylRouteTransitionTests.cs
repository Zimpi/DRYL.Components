using System.Diagnostics;
using Bunit;
using DRYL.Components;
using DRYL.Components.Motion;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylRouteTransition"/> and the navigation path it drives on
/// <see cref="IDrylViewTransition"/>. Two of these guard defects that would freeze a
/// real application: the render latch (a destination that renders before JS calls
/// back) and the bail (a destination that never reports at all).
/// </summary>
public class DrylRouteTransitionTests : BunitContext
{
    private sealed class RecordingViewTransition : IDrylViewTransition
    {
        public List<TimeSpan> Begun { get; } = [];
        public Task RunAsync(Action mutate) { mutate(); return Task.CompletedTask; }
        public Task RunAsync(Func<Task> mutate) => mutate();
        public void SignalRendered() { }
        public void BeginNavigation(TimeSpan timeout) => Begun.Add(timeout);
    }

    /// <summary>An implementer that predates BeginNavigation — it must still compile
    /// and behave, which is the whole point of the default implementation.</summary>
    private sealed class LegacyViewTransition : IDrylViewTransition
    {
        public Task RunAsync(Action mutate) { mutate(); return Task.CompletedTask; }
        public Task RunAsync(Func<Task> mutate) => mutate();
        public void SignalRendered() { }
    }

    private RecordingViewTransition UseRecorder()
    {
        var fake = new RecordingViewTransition();
        Services.AddSingleton<IDrylViewTransition>(fake);
        return fake;
    }

    private Microsoft.AspNetCore.Components.NavigationManager Nav =>
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

    // ------------------------------------------------------------------ mounting

    [Fact]
    public void Renders_no_markup()
    {
        UseRecorder();

        var cut = Render<DrylRouteTransition>();

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void An_internal_navigation_begins_a_transition()
    {
        var fake = UseRecorder();
        Render<DrylRouteTransition>();

        Nav.NavigateTo("/planets/42");

        Assert.Single(fake.Begun);
    }

    [Fact]
    public void No_transition_is_begun_after_dispose()
    {
        var fake = UseRecorder();
        var cut = Render<DrylRouteTransition>();
        cut.Instance.Dispose();

        Nav.NavigateTo("/planets/42");

        Assert.Empty(fake.Begun);
    }

    [Fact]
    public void The_timeout_parameter_is_passed_through()
    {
        var fake = UseRecorder();
        Render<DrylRouteTransition>(ps => ps.Add(p => p.Timeout, TimeSpan.FromMilliseconds(250)));

        Nav.NavigateTo("/planets/42");

        Assert.Equal(TimeSpan.FromMilliseconds(250), Assert.Single(fake.Begun));
    }

    [Fact]
    public void The_timeout_defaults_to_one_second()
    {
        var fake = UseRecorder();
        Render<DrylRouteTransition>();

        Nav.NavigateTo("/planets/42");

        Assert.Equal(TimeSpan.FromSeconds(1), Assert.Single(fake.Begun));
    }

    // -------------------------------------------------------------- ShouldMorph

    [Fact]
    public void ShouldMorph_returning_false_leaves_the_navigation_alone()
    {
        var fake = UseRecorder();
        Render<DrylRouteTransition>(ps => ps.Add(p => p.ShouldMorph, _ => false));

        Nav.NavigateTo("/logout");

        Assert.Empty(fake.Begun);
    }

    [Fact]
    public void ShouldMorph_returning_true_morphs_the_navigation()
    {
        var fake = UseRecorder();
        Render<DrylRouteTransition>(ps => ps.Add(p => p.ShouldMorph, _ => true));

        Nav.NavigateTo("/planets/42");

        Assert.Single(fake.Begun);
    }

    [Fact]
    public void ShouldMorph_receives_the_target_uri()
    {
        UseRecorder();
        var seen = new List<string>();
        Render<DrylRouteTransition>(ps => ps.Add(p => p.ShouldMorph, url => { seen.Add(url); return true; }));

        Nav.NavigateTo("/planets/42");

        Assert.Contains(seen, u => u.Contains("/planets/42"));
    }

    // ------------------------------------- the latch and the bail (freeze guards)

    [Fact]
    public async Task A_render_signalled_before_ApplyChange_still_completes_it()
    {
        var svc = new DrylViewTransition(new NoopJsRuntime());
        svc.BeginNavigation(TimeSpan.FromSeconds(30));

        // The destination rendered before JS got round to calling back.
        svc.SignalRendered();

        var sw = Stopwatch.StartNew();
        await svc.ApplyChange();
        sw.Stop();

        // Had the signal been treated as an event rather than a latch, this would
        // have sat here for the full 30 seconds.
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"ApplyChange waited {sw.Elapsed}");
    }

    [Fact]
    public async Task ApplyChange_bails_when_nothing_ever_reports_a_render()
    {
        var svc = new DrylViewTransition(new NoopJsRuntime());
        svc.BeginNavigation(TimeSpan.FromMilliseconds(150));

        var sw = Stopwatch.StartNew();
        await svc.ApplyChange();   // no SignalRendered ever arrives
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"ApplyChange waited {sw.Elapsed}");
    }

    [Fact]
    public async Task A_render_arriving_after_ApplyChange_started_completes_it()
    {
        var svc = new DrylViewTransition(new NoopJsRuntime());
        svc.BeginNavigation(TimeSpan.FromSeconds(30));

        var applying = svc.ApplyChange();
        await Task.Delay(20);
        svc.SignalRendered();

        var finished = await Task.WhenAny(applying, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(applying, finished);
    }

    [Fact]
    public void A_legacy_implementer_still_satisfies_the_interface()
    {
        IDrylViewTransition legacy = new LegacyViewTransition();

        // The default implementation: does nothing, throws nothing.
        legacy.BeginNavigation(TimeSpan.FromSeconds(1));
    }

    /// <summary>A JS runtime that never resolves anything — BeginNavigation's bridge
    /// call is fire-and-forget, so the tests above drive ApplyChange directly, exactly
    /// as the browser does.</summary>
    private sealed class NoopJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            new(default(TValue)!);
    }
}
