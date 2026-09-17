using System.Net;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests.Agents.Voice;

public class DrylVoiceCancellationTests
{
    private static TaskCompletionSource<T> Gate<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TokenHandler : HttpMessageHandler
    {
        public TaskCompletionSource<HttpResponseMessage>? Pending;
        public TaskCompletionSource<bool> Entered = Gate<bool>();
        public CancellationToken Token;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Token = ct; Entered.TrySetResult(true);
            return Pending?.Task ?? Task.FromResult(Response());
        }
        public static HttpResponseMessage Response() => new(HttpStatusCode.OK) { Content = new StringContent("{\"value\":\"ek_fixture\"}") };
    }

    private sealed class Js : IJSRuntime
    {
        public int Imports;
        public TaskCompletionSource<IJSObjectReference>? Pending;
        public TaskCompletionSource<bool> Entered = Gate<bool>();
        public readonly List<Module> Modules = [];
        public Action<Module>? Configure;
        public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) => InvokeAsync<T>(name, CancellationToken.None, args);
        public async ValueTask<T> InvokeAsync<T>(string name, CancellationToken ct, object?[]? args)
        {
            Imports++;
            var module = new Module(); Configure?.Invoke(module); Modules.Add(module); Entered.TrySetResult(true);
            return (T)(object)(Pending is { } gate ? await gate.Task : module);
        }
    }

    private sealed class Module : IJSObjectReference
    {
        public readonly Session Session = new();
        public int Disposals;
        public object? Callback;
        public TaskCompletionSource<IJSObjectReference>? Pending;
        public TaskCompletionSource<bool> Entered = Gate<bool>();
        public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) => InvokeAsync<T>(name, CancellationToken.None, args);
        public async ValueTask<T> InvokeAsync<T>(string name, CancellationToken ct, object?[]? args)
        {
            if (name is "createSession" or "start")
            {
                Callback = args![2]!.GetType().GetProperty("Value")!.GetValue(args[2]);
                Entered.TrySetResult(true);
                if (name == "createSession") return (T)(object)(Pending is { } gate ? await gate.Task : Session);
                Session.Starts++;
            }
            else if (name == "stop") Session.Stops++;
            return default!;
        }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }

    private sealed class Session : IJSObjectReference
    {
        public int Starts, Stops, Disposals;
        public bool Closed, RejectStop;
        public int Acquisitions;
        public TaskCompletionSource<bool>? PendingStart;
        public TaskCompletionSource<bool> Entered = Gate<bool>();
        public ValueTask<T> InvokeAsync<T>(string name, object?[]? args) => InvokeAsync<T>(name, CancellationToken.None, args);
        public async ValueTask<T> InvokeAsync<T>(string name, CancellationToken ct, object?[]? args)
        {
            if (name == "start")
            {
                Starts++; Entered.TrySetResult(true);
                if (PendingStart is { } pending) await pending.Task;
                if (!Closed) Acquisitions++;
            }
            if (name == "stop")
            {
                Stops++; Closed = true;
                if (RejectStop) throw new JSException("already disconnected");
            }
            return default!;
        }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }

    private static DrylVoiceRun Run(Js js, TokenHandler handler, DrylVoiceOptions? options = null) =>
        new DrylVoiceRunner(js, new HttpClient(handler)).Create(options ?? new() { ApiKey = "fixture" });
    private static object? Call(Module module, string name, params object[] args) => module.Callback!.GetType().GetMethod(name)!.Invoke(module.Callback, args);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_or_dispose_during_token_does_not_import_after_late_success(bool dispose)
    {
        var js = new Js(); var http = new TokenHandler { Pending = Gate<HttpResponseMessage>() }; var run = Run(js, http);
        var start = run.StartAsync(); await http.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (dispose) await run.DisposeAsync(); else await run.StopAsync();
        http.Pending.SetResult(TokenHandler.Response()); await start;
        Assert.Equal(0, js.Imports); Assert.Equal(VoicePhase.Idle, run.Phase); Assert.Null(run.Error);
        Assert.True(http.Token.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_import_is_disposed_without_starting_or_harming_restart(bool reject)
    {
        var js = new Js { Pending = Gate<IJSObjectReference>() }; var run = Run(js, new());
        var old = run.StartAsync(); await js.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); await run.StopAsync();
        var pending = js.Pending; js.Pending = null; await run.StartAsync();
        var phase = run.Phase; var beats = 0; run.OnChange += () => beats++;
        var stale = new Module();
        if (reject) pending.SetException(new InvalidOperationException("obsolete import")); else pending.SetResult(stale);
        await old;
        Assert.Equal(phase, run.Phase); Assert.Equal(0, beats); Assert.Null(run.Error);
        Assert.Equal(0, stale.Session.Starts); if (!reject) Assert.Equal(1, stale.Disposals);
        Assert.Equal(1, js.Modules[^1].Session.Starts); await run.DisposeAsync();
    }

    [Fact]
    public async Task Old_bridge_callbacks_and_awaited_predicate_cannot_mutate_restart()
    {
        var js = new Js(); var run = Run(js, new()); await run.StartAsync(); var old = js.Modules[0];
        Call(old, "OnConnected"); var gate = Gate<bool>(); run.ShouldContinue = () => new ValueTask<bool>(gate.Task);
        var decision = (Task<bool>)Call(old, "OnTurnEndedAsync")!;
        await run.StopAsync(); await run.StartAsync(); Call(js.Modules[^1], "OnConnected");
        var beats = 0; run.OnChange += () => beats++;
        Call(old, "OnConnected"); Call(old, "OnActivity", "Speaking"); Call(old, "OnTranscript", "User", "old");
        Call(old, "OnFailed", "old failure"); Call(old, "OnClosed"); gate.SetResult(true);
        Assert.False(await decision); Assert.Equal(0, beats); Assert.Equal(VoicePhase.Live, run.Phase);
        Assert.Equal(VoiceActivity.Listening, run.Activity); Assert.Empty(run.Transcript); Assert.Null(run.Error);
        await run.DisposeAsync();
    }

    [Fact]
    public async Task Old_tool_result_does_not_change_even_the_retained_trace_item()
    {
        var gate = Gate<string>(); var invoked = 0;
        var options = new DrylVoiceOptions { ApiKey = "fixture" };
        options.Tools.Add(AIFunctionFactory.Create(() => { invoked++; return gate.Task; }, "tool"));
        var js = new Js(); var run = Run(js, new(), options); await run.StartAsync(); var old = js.Modules[0]; Call(old, "OnConnected");
        var pending = (Task<string>)Call(old, "OnToolCallAsync", "c", "tool", "{}")!;
        var trace = Assert.Single(run.ToolCalls); await run.StopAsync(); await run.StartAsync();
        var beats = 0; run.OnChange += () => beats++;
        var late = (Task<string>)Call(old, "OnToolCallAsync", "late", "tool", "{}")!;
        gate.SetResult("done"); await pending; await late;
        Assert.Equal(1, invoked); Assert.Null(trace.Result); Assert.Null(trace.Error); Assert.Empty(run.ToolCalls); Assert.Equal(0, beats);
        await run.DisposeAsync();
    }

    [Fact]
    public async Task Disposed_run_cannot_start_and_repeated_cleanup_is_safe()
    {
        var js = new Js(); var run = Run(js, new()); await run.StartAsync();
        await run.DisposeAsync(); await run.DisposeAsync(); await run.StopAsync(); await run.StartAsync();
        Assert.Equal(1, js.Imports); Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(1, js.Modules[0].Disposals); Assert.Equal(1, js.Modules[0].Session.Disposals);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Factory_completion_after_stop_or_dispose_releases_its_owned_handle(bool dispose, bool reject)
    {
        var gate = Gate<IJSObjectReference>(); var js = new Js { Configure = module => module.Pending = gate };
        var run = Run(js, new()); var start = run.StartAsync(); await js.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); var old = js.Modules[0]; await old.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (dispose) await run.DisposeAsync(); else await run.StopAsync();
        var beats = 0; run.OnChange += () => beats++;
        if (!dispose) { js.Configure = null; await run.StartAsync(); beats = 0; }
        if (reject) gate.SetException(new JSException("late factory")); else gate.SetResult(old.Session);
        await start;
        Assert.Equal(0, old.Session.Starts); Assert.Equal(1, old.Disposals); Assert.Equal(0, beats); Assert.Null(run.Error);
        if (!reject) { Assert.Equal(1, old.Session.Stops); Assert.Equal(1, old.Session.Disposals); }
        await run.DisposeAsync();
    }

    [Fact]
    public async Task Delayed_start_dispatch_uses_the_stopped_handle_instead_of_the_new_run_handle()
    {
        var gate = Gate<bool>(); var js = new Js { Configure = module => module.Session.PendingStart = gate };
        var one = Run(js, new()); var pending = one.StartAsync(); await js.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); var old = js.Modules[0]; await old.Session.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await one.StopAsync(); js.Configure = null; var two = Run(js, new()); await two.StartAsync();
        var current = js.Modules[^1]; gate.SetResult(true); await pending; await one.DisposeAsync();
        Assert.Equal(0, old.Session.Acquisitions); Assert.Equal(1, old.Session.Stops);
        Assert.Equal(1, current.Session.Acquisitions); Assert.Equal(0, current.Session.Stops);
        await two.DisposeAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Caller_cancellation_during_import_observes_and_releases_the_late_reference(bool dispose)
    {
        using var cancellation = new CancellationTokenSource();
        var gate = Gate<IJSObjectReference>(); var js = new Js { Pending = gate }; var run = Run(js, new());
        var start = run.StartAsync(ct: cancellation.Token); await js.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (dispose) await run.DisposeAsync(); else cancellation.Cancel();
        Assert.Equal(VoicePhase.Idle, run.Phase); Assert.False(start.IsCompleted);
        var imported = new Module(); gate.SetResult(imported); await start;
        Assert.Equal(1, imported.Disposals); Assert.Equal(0, imported.Session.Starts); Assert.Null(run.Error);
    }

    [Fact]
    public async Task Cancellation_of_a_pending_start_does_not_stop_the_later_session()
    {
        using var cancellation = new CancellationTokenSource();
        var gate = Gate<bool>(); var js = new Js { Configure = module => module.Session.PendingStart = gate };
        var run = Run(js, new()); var start = run.StartAsync(ct: cancellation.Token); await js.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); var old = js.Modules[0]; await old.Session.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel(); Assert.Equal(VoicePhase.Idle, run.Phase);
        js.Configure = null; await run.StartAsync(); var current = js.Modules[^1]; gate.SetResult(true); await start;
        Assert.Equal(0, old.Session.Acquisitions); Assert.Equal(0, current.Session.Stops); Assert.Equal(VoicePhase.Connecting, run.Phase);
        await run.DisposeAsync();
    }

    [Fact]
    public async Task Failed_stop_still_releases_both_references_and_unused_disposal_does_no_interop()
    {
        var js = new Js { Configure = module => module.Session.RejectStop = true }; var run = Run(js, new()); await run.StartAsync();
        await run.DisposeAsync();
        Assert.Equal(1, js.Modules[0].Session.Disposals); Assert.Equal(1, js.Modules[0].Disposals);
        var unused = Run(js, new()); await unused.DisposeAsync(); Assert.Equal(1, js.Imports);
    }

    [Fact]
    public async Task Caller_cancellation_after_start_has_returned_does_not_end_a_live_session()
    {
        using var cancellation = new CancellationTokenSource(); var js = new Js(); var run = Run(js, new());
        await run.StartAsync(ct: cancellation.Token); Call(js.Modules[0], "OnConnected"); cancellation.Cancel();
        Assert.Equal(VoicePhase.Live, run.Phase); Assert.Equal(0, js.Modules[0].Session.Stops); await run.DisposeAsync();
    }

    [Fact]
    public async Task User_speech_invalidates_a_pending_continuation_without_spending_its_new_budget()
    {
        var js = new Js(); var run = Run(js, new()); await run.StartAsync(); var module = js.Modules[0];
        Call(module, "OnConnected"); run.MaxAutoContinuations = 1;
        var gate = Gate<bool>(); run.ShouldContinue = () => new ValueTask<bool>(gate.Task);
        var pending = (Task<bool>)Call(module, "OnTurnEndedAsync")!;
        Call(module, "OnActivity", nameof(VoiceActivity.UserSpeaking));
        var beats = 0; run.OnChange += () => beats++;
        gate.SetResult(true);
        Assert.False(await pending);
        Assert.Equal(VoiceActivity.UserSpeaking, run.Activity); Assert.Equal(AiState.Active, run.State);
        Assert.Equal(0, beats);
        run.ShouldContinue = () => ValueTask.FromResult(true);
        Assert.True(await (Task<bool>)Call(module, "OnTurnEndedAsync")!);
        await run.DisposeAsync();
    }
}
