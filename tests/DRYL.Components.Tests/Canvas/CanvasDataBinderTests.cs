using System.Text.Json;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasDataBinderTests
{
    public sealed record RegionParams(string? Region = null);

    private static CanvasDataBinding Binding(string source, string? paramsJson = null, string? refresh = null) =>
        new()
        {
            Source = source,
            Params = paramsJson is null ? null : JsonDocument.Parse(paramsJson).RootElement.Clone(),
            Refresh = refresh,
        };

    /// <summary>Builds a scope with one counted source plus a hook to control what it returns.</summary>
    private sealed class Host : IDisposable
    {
        private readonly ServiceProvider _root;

        public Host(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            services.AddDrylComponents();
            register(services);
            _root = services.BuildServiceProvider();
            Data = _root.CreateScope().ServiceProvider.GetRequiredService<ICanvasDataService>();
        }

        public ICanvasDataService Data { get; }

        public CanvasFormState Form { get; } = new();
        public CanvasPulseTracker Pulse { get; } = new();

        public CanvasDataBinder Binder() => new(Data, Form, Pulse);

        public void Dispose() => _root.Dispose();
    }

    /// <summary>Waits until <paramref name="predicate"/> holds, so a test never sleeps for a fixed
    /// span it does not need. Loads run on the thread pool; 3 s is a generous ceiling.</summary>
    private static async Task Until(Func<bool> predicate, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return;
            await Task.Delay(10);
        }
        Assert.Fail($"Timed out waiting for: {what}");
    }

    [Fact]
    public async Task Two_nodes_on_the_same_key_cost_one_handler_call()
    {
        var calls = 0;
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
            (CanvasDataContext _, CancellationToken _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(CanvasData.Scalar(7));
            }));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        binder.Register("b", Binding("orders.open"));

        await Until(() => binder.StateOf("a")?.HasValue == true, "the shared load to finish");

        Assert.Equal(1, calls);                                  // that is the dedupe
        Assert.True(binder.StateOf("b")!.HasValue);              // both nodes see it
        Assert.True(binder.HasBindings);
    }

    [Fact]
    public async Task Different_params_are_different_keys()
    {
        var seen = new List<string?>();
        using var host = new Host(s => s.AddDrylCanvasDataSource("sales.byRegion", "Umsatz.",
            (RegionParams p, CanvasDataContext _, CancellationToken _) =>
            {
                lock (seen) seen.Add(p.Region);
                return Task.FromResult(CanvasData.Scalar(1));
            }));

        await using var binder = host.Binder();
        binder.Register("a", Binding("sales.byRegion", """{ "region": "north" }"""));
        binder.Register("b", Binding("sales.byRegion", """{ "region": "south" }"""));

        await Until(() => binder.StateOf("a")?.HasValue == true && binder.StateOf("b")?.HasValue == true,
                    "both loads to finish");

        lock (seen) Assert.Equal(new[] { "north", "south" }, seen.OrderBy(x => x));
    }

    [Fact]
    public async Task Property_order_in_params_does_not_split_the_key()
    {
        var calls = 0;
        using var host = new Host(s => s.AddDrylCanvasDataSource("x.two", "Two.",
            (TwoParams _, CanvasDataContext _, CancellationToken _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(CanvasData.Scalar(1));
            }));

        await using var binder = host.Binder();
        binder.Register("a", Binding("x.two", """{ "year": 2026, "region": "north" }"""));
        binder.Register("b", Binding("x.two", """{ "region": "north", "year": 2026 }"""));

        await Until(() => binder.StateOf("a")?.HasValue == true, "the shared load to finish");
        Assert.Equal(1, calls);
    }

    public sealed record TwoParams(int Year, string? Region = null);

    [Fact]
    public async Task A_field_change_reloads_only_the_dependent_binding()
    {
        var regionCalls = 0;
        var plainCalls = 0;
        using var host = new Host(s => s
            .AddDrylCanvasDataSource("sales.byRegion", "Umsatz.",
                (RegionParams _, CanvasDataContext _, CancellationToken _) =>
                {
                    Interlocked.Increment(ref regionCalls);
                    return Task.FromResult(CanvasData.Scalar(1));
                })
            .AddDrylCanvasDataSource("orders.open", "Offen.",
                (CanvasDataContext _, CancellationToken _) =>
                {
                    Interlocked.Increment(ref plainCalls);
                    return Task.FromResult(CanvasData.Scalar(2));
                }));

        await using var binder = host.Binder();
        binder.Register("bound", Binding("sales.byRegion", """{ "region": { "$field": "region" } }"""));
        binder.Register("plain", Binding("orders.open"));
        await Until(() => binder.StateOf("bound")?.HasValue == true && binder.StateOf("plain")?.HasValue == true,
                    "the initial loads to finish");

        host.Form.Set("region", "north");

        await Until(() => regionCalls == 2, "the dependent binding to reload");
        Assert.Equal(1, plainCalls);   // the unrelated key never moved
    }

    [Fact]
    public async Task Rapid_field_changes_are_debounced_into_one_reload()
    {
        var calls = 0;
        using var host = new Host(s => s.AddDrylCanvasDataSource("sales.byRegion", "Umsatz.",
            (RegionParams _, CanvasDataContext _, CancellationToken _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(CanvasData.Scalar(1));
            }));

        await using var binder = host.Binder();
        binder.Register("bound", Binding("sales.byRegion", """{ "region": { "$field": "q" } }"""));
        await Until(() => binder.StateOf("bound")?.HasValue == true, "the initial load");

        // Someone typing "north" — five keystrokes must not be five queries.
        foreach (var s in new[] { "n", "no", "nor", "nort", "north" }) host.Form.Set("q", s);

        await Until(() => calls == 2, "exactly one debounced reload");
        await Task.Delay(400);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Invalidate_hits_every_key_of_a_source_and_the_targeted_overload_only_one()
    {
        var calls = 0;
        using var host = new Host(s => s.AddDrylCanvasDataSource("sales.byRegion", "Umsatz.",
            (RegionParams _, CanvasDataContext _, CancellationToken _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.FromResult(CanvasData.Scalar(1));
            }));

        await using var binder = host.Binder();
        binder.Register("a", Binding("sales.byRegion", """{ "region": "north" }"""));
        binder.Register("b", Binding("sales.byRegion", """{ "region": "south" }"""));
        await Until(() => calls == 2, "the initial loads");

        host.Data.Invalidate("sales.byRegion", new { region = "north" });
        await Until(() => calls == 3, "only the matching key to reload");
        await Task.Delay(150);
        Assert.Equal(3, calls);

        host.Data.Invalidate("sales.byRegion");
        await Until(() => calls == 5, "both keys to reload");
    }

    [Fact]
    public async Task An_unchanged_result_stamps_no_pulse()
    {
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offen.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(7, "7"))));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        await Until(() => binder.StateOf("a")?.HasValue == true, "the initial load");

        Assert.Equal(0, host.Pulse.TickOf("a"));   // an arrival replaces a skeleton — motion enough

        await binder.RefreshAllAsync();
        // Same numbers as before: no pulse, or the dashboard blinks every interval for nothing.
        Assert.Equal(0, host.Pulse.TickOf("a"));
    }

    [Fact]
    public async Task A_changed_result_stamps_the_pulse_of_every_node_on_the_key()
    {
        var value = 7d;
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offen.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(value))));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        binder.Register("b", Binding("orders.open"));
        await Until(() => binder.StateOf("a")?.HasValue == true, "the initial load");

        value = 8d;
        await binder.RefreshAllAsync();

        Assert.True(host.Pulse.TickOf("a") > 0);
        Assert.True(host.Pulse.TickOf("b") > 0);
    }

    [Fact]
    public async Task A_failing_handler_becomes_a_binding_error_and_keeps_the_last_good_value()
    {
        var fail = false;
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offen.",
            (CanvasDataContext _, CancellationToken _) => fail
                ? throw new InvalidOperationException("the database is on fire")
                : Task.FromResult(CanvasData.Scalar(7, "7"))));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        await Until(() => binder.StateOf("a")?.HasValue == true, "the initial load");

        fail = true;
        await binder.RefreshAllAsync();

        var state = binder.StateOf("a")!;
        Assert.True(state.HasValue);                              // "briefly disturbed" is not "broken"
        Assert.Contains("orders.open", state.Error);
        Assert.DoesNotContain("on fire", state.Error);            // the exception goes to ILogger, not the user
    }

    [Fact]
    public async Task A_failure_without_a_good_value_reports_only_the_error()
    {
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offen.",
            (CanvasDataContext _, CancellationToken _) =>
                Task.FromException<CanvasScalarData>(new InvalidOperationException("nope"))));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        await Until(() => binder.StateOf("a")?.Error is not null, "the failure to land");

        Assert.False(binder.StateOf("a")!.HasValue);
    }

    [Fact]
    public async Task Disposing_cancels_in_flight_loads_and_stops_the_interval_timer()
    {
        var cancelled = new TaskCompletionSource();
        using var host = new Host(s => s.AddDrylCanvasDataSource("slow.source", "Slow.",
            async (CanvasDataContext _, CancellationToken ct) =>
            {
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
                return CanvasData.Scalar(1);
            }));

        var binder = host.Binder();
        binder.Register("a", Binding("slow.source", null, "interval:5s"));
        await Until(() => binder.StateOf("a")?.Loading == true, "the load to start");

        await binder.DisposeAsync();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void An_interval_below_the_floor_is_lifted_to_it()
    {
        Assert.Null(CanvasDataBinder.IntervalOf(null));
        Assert.Null(CanvasDataBinder.IntervalOf("manual"));
        Assert.Equal(30, CanvasDataBinder.IntervalOf("interval:30s"));
        Assert.Equal(5, CanvasDataBinder.IntervalOf("interval:1s"));
        Assert.Null(CanvasDataBinder.IntervalOf("every 30 seconds"));
    }

    [Fact]
    public async Task Reset_drops_every_binding()
    {
        using var host = new Host(s => s.AddDrylCanvasDataSource("orders.open", "Offen.",
            (CanvasDataContext _, CancellationToken _) => Task.FromResult(CanvasData.Scalar(1))));

        await using var binder = host.Binder();
        binder.Register("a", Binding("orders.open"));
        await Until(() => binder.StateOf("a")?.HasValue == true, "the initial load");

        binder.Reset();

        Assert.False(binder.HasBindings);
        Assert.Null(binder.StateOf("a"));
    }
}
