using System.Diagnostics;
using Bunit;
using DRYL.Components;
using DRYL.Components.Motion;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylRouteTransition"/> and the navigation path it drives on
/// <see cref="IDrylMorph"/>. Two of these guard defects that would freeze a
/// real application: the render latch (a destination that renders before the engine looks) and the bail (a destination that never reports at all).
/// </summary>
public class DrylRouteTransitionTests : BunitContext
{
    private sealed class RecordingMorph : IDrylMorph
    {
        public List<TimeSpan> Begun { get; } = [];
        public Task RunAsync(Action mutate) { mutate(); return Task.CompletedTask; }
        public Task RunAsync(Func<Task> mutate) => mutate();
        public void SignalRendered() { }
        public Task BeginNavigationAsync(TimeSpan timeout) { Begun.Add(timeout); return Task.CompletedTask; }
    }

    private RecordingMorph UseRecorder()
    {
        var fake = new RecordingMorph();
        Services.AddSingleton<IDrylMorph>(fake);
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
    public async Task A_render_signalled_before_the_engine_looks_still_completes_it()
    {
        var svc = new DrylMorphEngine(new NoopJsRuntime());
        var nav = svc.BeginNavigationAsync(TimeSpan.FromSeconds(30));

        // The destination rendered before JS got round to calling back.
        svc.SignalRendered();

        var sw = Stopwatch.StartNew();
        await nav;
        sw.Stop();

        // Had the signal been treated as an event rather than a latch, this would
        // have sat here for the full 30 seconds.
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"the navigation morph waited {sw.Elapsed}");
    }

    [Fact]
    public async Task The_navigation_morph_bails_when_nothing_ever_reports_a_render()
    {
        var svc = new DrylMorphEngine(new NoopJsRuntime());
        var nav = svc.BeginNavigationAsync(TimeSpan.FromMilliseconds(150));

        var sw = Stopwatch.StartNew();
        await nav;   // no SignalRendered ever arrives
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"the navigation morph waited {sw.Elapsed}");
    }

    [Fact]
    public async Task A_render_arriving_after_the_wait_started_completes_it()
    {
        var svc = new DrylMorphEngine(new NoopJsRuntime());
        var nav = svc.BeginNavigationAsync(TimeSpan.FromSeconds(30));

        await Task.Delay(20);
        svc.SignalRendered();

        var finished = await Task.WhenAny(nav, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(nav, finished);
    }


    /// <summary>A JS runtime whose calls resolve immediately with nothing, so the engine
    /// runs its whole navigation path — capture, wait, play — without a browser.</summary>
    private sealed class NoopJsRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            new(default(TValue)!);
    }
}
