using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests.Agents.Voice;

public class DrylLiveTests
{
    private static TaskCompletionSource<T> Gate<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static DrylVoiceOptions Options() => new()
    {
        ApiKey = "sk-server-only", Model = "gpt-live-1", Voice = "gleam", Instructions = "Speak naturally.",
        BaseUrl = "https://proxy.example/v1/", SafetyIdentifier = "hashed-user",
        Live = new() { BackendInstructions = "Do the work.", EnableWebSearch = true },
    };

    private sealed record Request(string Path, string? Authorization, string? Safety, string? Body);
    private sealed class Http : HttpMessageHandler
    {
        public List<Request> Requests { get; } = [];
        public HttpContent? CreationContent;
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Response = """{"session":{"id":"live_fixture"},"transport":{"type":"webrtc","sdp":"answer"}}""";
        public CancellationToken CreationToken;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(new(request.RequestUri!.AbsoluteUri, request.Headers.Authorization?.Parameter,
                request.Headers.TryGetValues("OpenAI-Safety-Identifier", out var values) ? values.Single() : null,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));
            if (request.RequestUri.AbsolutePath.EndsWith("/hangup")) return new(HttpStatusCode.OK);
            CreationToken = ct;
            return new(Status) { Content = CreationContent ?? new StringContent(Response) };
        }
    }

    private sealed class PendingContent : HttpContent
    {
        public TaskCompletionSource<bool> Entered { get; } = Gate<bool>();
        public TaskCompletionSource<string> Body { get; } = Gate<string>();
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            Entered.TrySetResult(true);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(await Body.Task));
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }

    private sealed class Js : IJSRuntime
    {
        public List<Module> Modules { get; } = [];
        public bool Handshake = true;
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var module = new Module(Handshake); Modules.Add(module);
            return ValueTask.FromResult((T)(object)module);
        }
    }

    private sealed class Module(bool handshake) : IJSObjectReference
    {
        public object? Callback;
        public string? Token;
        public string? Config;
        public int Disposals;
        public Session Session { get; } = new(handshake);
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Token = args![0] as string; Config = JsonSerializer.Serialize(args[1]);
            Callback = args[2]!.GetType().GetProperty("Value")!.GetValue(args[2]);
            Session.Module = this;
            return ValueTask.FromResult((T)(object)Session);
        }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }

    private sealed class Session(bool handshake) : IJSObjectReference
    {
        public Module Module = null!;
        public Func<Task>? DuringStop;
        public int Stops;
        public int Disposals;
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public async ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "start" && handshake)
            {
                await (Task<string>)Call(Module, "OnLiveOfferAsync", "offer")!;
                Call(Module, "OnConnected");
            }
            if (identifier == "stop")
            {
                Stops++;
                if (DuringStop is not null) await DuringStop();
            }
            return default!;
        }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }

    private static object? Call(Module module, string name, params object[] args) =>
        module.Callback!.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!.Invoke(module.Callback, args);
    private static DrylVoiceRun Run(Js js, Http http, DrylVoiceOptions? options = null) =>
        new DrylVoiceRunner(js, new HttpClient(http)).Create(options ?? Options());
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void Live_payload_separates_voice_from_backend_and_omits_realtime_settings()
    {
        var options = Options();
        options.Tools.Add(AIFunctionFactory.Create((string city, string? unit = null) => city, "weather"));
        var session = options.ToSessionPayload();
        Assert.Equal("gpt-live-1", (string?)session["model"]);
        Assert.Equal("Speak naturally.", (string?)session["instructions"]);
        Assert.Equal("gleam", (string?)session["audio"]!["output"]!["voice"]);
        Assert.False((bool)session["store"]!);
        Assert.Null(session["type"]); Assert.Null(session["tools"]); Assert.Null(session["reasoning"]);
        Assert.Null(session["audio"]!["input"]); Assert.Null(session["audio"]!["output"]!["speed"]);
        var backend = session["delegation"]!["responses"]!;
        Assert.Equal("gpt-5.6-terra", (string?)backend["model"]);
        Assert.Equal("Do the work.", (string?)backend["instructions"]);
        Assert.Equal("medium", (string?)backend["reasoning"]!["effort"]);
        Assert.Equal(4096, (int)backend["max_output_tokens"]!);
        Assert.False((bool)backend["parallel_tool_calls"]!);
        Assert.False((bool)backend["tools"]![0]!["strict"]!);
        Assert.Equal("weather", (string?)backend["tools"]![0]!["name"]);
        Assert.Equal("web_search", (string?)backend["tools"]![1]!["type"]);
        Assert.DoesNotContain("sk-server-only", session.ToJsonString());
    }

    [Fact]
    public void Web_search_is_opt_in_and_optional_backend_settings_can_be_omitted()
    {
        var options = Options(); options.Live = new() { ReasoningEffort = null, MaxOutputTokens = null };
        var backend = options.ToSessionPayload()["delegation"]!["responses"]!;
        Assert.Null(backend["tools"]); Assert.Null(backend["tool_choice"]);
        Assert.Null(backend["reasoning"]); Assert.Null(backend["max_output_tokens"]);
        options.Live.MaxOutputTokens = 15;
        Assert.Throws<InvalidOperationException>(() => options.ToSessionPayload());
        options.Live.MaxOutputTokens = 16;
        Assert.Equal(16, (int)options.ToSessionPayload()["delegation"]!["responses"]!["max_output_tokens"]!);
    }

    [Fact]
    public async Task Live_start_keeps_configuration_private_and_posts_sdp_with_bounded_history()
    {
        var js = new Js(); var http = new Http(); await using var run = Run(js, http);
        var history = Enumerable.Range(0, 300).Select(i => new DrylVoiceMessage(VoiceRole.User, $"message {i}"));
        await run.StartAsync(history);
        Assert.Equal(VoicePhase.Live, run.Phase);
        var request = Assert.Single(http.Requests);
        Assert.Equal("https://proxy.example/v1/live/sessions", request.Path);
        Assert.Equal("sk-server-only", request.Authorization); Assert.Equal("hashed-user", request.Safety);
        var payload = JsonNode.Parse(request.Body!)!;
        Assert.Equal("webrtc", (string?)payload["transport"]!["type"]);
        Assert.Equal("offer", (string?)payload["transport"]!["sdp"]);
        var input = (JsonArray)payload["session"]!["input"]!;
        Assert.InRange(input.Count, 1, 128);
        Assert.Equal("message 299", (string?)input[^1]!["content"]![0]!["text"]);
        Assert.All(input, item => Assert.Equal("input_text", (string?)item!["content"]![0]!["type"]));
        Assert.True(input.Sum(item => Encoding.UTF8.GetByteCount((string)item!["content"]![0]!["text"]!) + 32) <= 4096);
        var module = Assert.Single(js.Modules);
        Assert.Null(module.Token); Assert.True((bool)JsonNode.Parse(module.Config!)!["live"]!);
        Assert.DoesNotContain("sk-server-only", module.Config); Assert.DoesNotContain("Do the work", module.Config);
        Assert.DoesNotContain("message 299", module.Config); Assert.DoesNotContain("gpt-5.6-terra", module.Config);
    }

    [Fact]
    public async Task Oversized_history_retains_recent_unicode_without_splitting_a_scalar()
    {
        var js = new Js(); var http = new Http(); await using var run = Run(js, http);
        await run.StartAsync([new(VoiceRole.Assistant, new string('x', 20000) + string.Concat(Enumerable.Repeat("😀", 1500)))]);
        var input = (JsonArray)JsonNode.Parse(http.Requests[0].Body!)!["session"]!["input"]!;
        var item = Assert.Single(input)!;
        var text = (string)item["content"]![0]!["text"]!;
        Assert.StartsWith("😀", text); Assert.EndsWith("😀", text);
        Assert.DoesNotContain("�", text); Assert.InRange(Encoding.UTF8.GetByteCount(text), 1, 4064);
        Assert.Equal("output_text", (string?)item["content"]![0]!["type"]);
    }

    [Fact]
    public async Task Duplicate_offers_share_one_creation_and_stop_closes_the_known_session()
    {
        var js = new Js { Handshake = false }; var http = new Http(); await using var run = Run(js, http);
        await run.StartAsync(); var module = js.Modules[0];
        var one = (Task<string>)Call(module, "OnLiveOfferAsync", "offer")!;
        var two = (Task<string>)Call(module, "OnLiveOfferAsync", "offer")!;
        Assert.Same(one, two); Assert.Equal("answer", await one); Assert.Single(http.Requests);
        await run.StopAsync();
        Assert.Equal(2, http.Requests.Count);
        Assert.EndsWith("/live/sessions/live_fixture/hangup", http.Requests[1].Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_creation_body_arriving_after_stop_or_cancellation_is_closed_without_harming_restart(bool cancel)
    {
        var content = new PendingContent(); var js = new Js(); var http = new Http { CreationContent = content };
        using var cancellation = new CancellationTokenSource(); await using var run = Run(js, http);
        var start = run.StartAsync(ct: cancellation.Token);
        await content.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (cancel) cancellation.Cancel(); else await run.StopAsync();
        Assert.Equal(VoicePhase.Idle, run.Phase);
        http.CreationContent = null; await run.StartAsync();
        var beats = 0; run.OnChange += () => beats++;
        content.Body.SetResult("""{"session":{"id":"live_old"},"transport":{"sdp":"old answer"}}""");
        await start;
        Assert.Equal(VoicePhase.Live, run.Phase); Assert.Null(run.Error); Assert.Equal(0, beats);
        Assert.Contains(http.Requests, r => r.Path.EndsWith("/live_old/hangup"));
    }

    [Fact]
    public async Task Creation_errors_and_malformed_answers_fail_startup_and_release_known_sessions()
    {
        var http = new Http { Status = HttpStatusCode.BadRequest, Response = """{"error":{"message":"Unknown Live model."}}""" };
        var js = new Js(); await using var run = Run(js, http);
        await run.StartAsync(); Assert.Equal("Unknown Live model.", run.Error!.Message); Assert.Equal(VoicePhase.Idle, run.Phase);
        http.Status = HttpStatusCode.OK; http.Response = """{"session":{"id":"live_bad"},"transport":{}}""";
        await run.StartAsync(); Assert.Contains("no SDP answer", run.Error!.Message);
        Assert.Contains(http.Requests, r => r.Path.EndsWith("/live_bad/hangup"));
        http.Response = """{"transport":{"sdp":"answer"}}""";
        await run.StartAsync(); Assert.Contains("no session ID", run.Error!.Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("\"invalid\"")]
    public async Task A_known_session_with_malformed_transport_is_closed(string transport)
    {
        var http = new Http { Response = "{\"session\":{\"id\":\"live_bad\"},\"transport\":" + transport + "}" };
        var js = new Js(); await using var run = Run(js, http);
        await run.StartAsync();
        Assert.Contains("no SDP answer", run.Error!.Message);
        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal(2, http.Requests.Count);
        Assert.EndsWith("/live_bad/hangup", http.Requests[1].Path);
    }

    [Fact]
    public async Task Interleaved_and_late_fragments_preserve_text_timing_and_independent_speaker_rows()
    {
        var js = new Js(); await using var run = Run(js, new()); await run.StartAsync(); var module = js.Modules[0];
        Call(module, "OnLiveTranscriptDelta", "Assistant", "Hello", 100d, 200d);
        Call(module, "OnLiveTranscriptDelta", "User", "Yes", 150d, 250d);
        Call(module, "OnLiveTranscriptDelta", "Assistant", " world", 300d, 400d);
        Call(module, "OnLiveTranscriptDelta", "User", " please", 350d, 450d);
        Call(module, "OnLiveTranscriptDelta", "Assistant", "Later", 5000d, 5100d);
        Call(module, "OnLiveTranscriptDelta", "Assistant", "! ", 450d, 500d);
        Call(module, "OnLiveTranscriptDelta", "Assistant", " ", 5100d, 5150d);
        Assert.Equal(7, run.TranscriptDeltas.Count);
        Assert.Equal(new(VoiceRole.Assistant, "! ", 450, 500), run.TranscriptDeltas[5]);
        Assert.Equal(["Hello world! ", "Yes please", "Later "], run.Transcript.Select(m => m.Text));
        Assert.Equal("Hello worldLater!  ", run.Text);
        await run.StopAsync(); Assert.Equal(7, run.TranscriptDeltas.Count);
        await run.StartAsync(); Assert.Empty(run.TranscriptDeltas); Assert.Equal("", run.Text);
    }

    [Fact]
    public async Task Completed_backend_results_preserve_citations_and_are_separate_from_captions()
    {
        var js = new Js(); await using var run = Run(js, new()); await run.StartAsync(); var module = js.Modules[0];
        var received = new List<JsonElement>(); run.BackendResponseReceived = response => { received.Add(response); return ValueTask.CompletedTask; };
        var response = Json("""{"id":"resp_1","status":"completed","output":[{"type":"message","content":[{"type":"output_text","text":"Result","annotations":[{"type":"url_citation","url":"https://example.com","title":"Source"}]}]}]}""");
        await (Task)Call(module, "OnBackendResponseAsync", response)!;
        await (Task)Call(module, "OnBackendResponseAsync", response)!;
        Assert.Contains("url_citation", Assert.Single(received).GetRawText()); Assert.Empty(run.Transcript); Assert.Equal("", run.Text);
        await (Task)Call(module, "OnBackendResponseAsync", Json("""{"id":"resp_failed","status":"failed"}"""))!;
        Assert.Single(received);
        run.ShouldContinue = () => throw new InvalidOperationException("Live must not call this");
        Assert.False(await (Task<bool>)Call(module, "OnTurnEndedAsync")!);
    }

    [Fact]
    public async Task Graceful_stop_records_final_metadata_and_captions_but_cannot_dispatch_tools()
    {
        var js = new Js(); var http = new Http(); var options = Options(); var invoked = 0;
        options.Tools.Add(AIFunctionFactory.Create(() => ++invoked, "change"));
        await using var run = Run(js, http, options); await run.StartAsync(); var module = js.Modules[0];
        Call(module, "OnLiveUsage", 3d); Call(module, "OnLiveUsage", 4d); Assert.Equal(4d, run.LiveUsageSeconds);
        module.Session.DuringStop = async () =>
        {
            Assert.Equal(VoicePhase.Closing, run.Phase);
            Call(module, "OnLiveTranscriptDelta", "Assistant", "Bye", 500d, 600d);
            await (Task<string>)Call(module, "OnToolCallAsync", "call_late", "change", "{}")!;
            Call(module, "OnLiveSessionClosed", Json("""{"usage":{"seconds":5.5},"reason":"close_requested"}"""));
            await (Task)Call(module, "OnClosed")!;
        };
        await run.StopAsync();
        Assert.Equal(0, invoked); Assert.Empty(run.ToolCalls); Assert.Equal("Bye", Assert.Single(run.Transcript).Text);
        Assert.Equal(5.5, run.LiveUsageSeconds); Assert.True(run.LiveFinalized); Assert.Equal("close_requested", run.LiveCloseReason);
        Assert.Single(http.Requests); // final event made fallback hangup unnecessary
        Assert.Equal(VoicePhase.Idle, run.Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repeated_stop_or_dispose_joins_the_existing_graceful_drain(bool dispose)
    {
        var js = new Js(); var http = new Http(); await using var run = Run(js, http); await run.StartAsync();
        var module = js.Modules[0]; var release = Gate<bool>();
        module.Session.DuringStop = () => release.Task;
        var first = run.StopAsync();
        var repeated = run.StopAsync();
        Assert.Same(first, repeated); Assert.False(first.IsCompleted);
        Assert.Equal(VoicePhase.Closing, run.Phase); Assert.Equal(1, module.Session.Stops);
        await run.StartAsync(); Assert.Single(js.Modules);

        Task? disposal = null;
        if (dispose)
        {
            disposal = run.DisposeAsync().AsTask();
            Assert.Same(disposal, run.DisposeAsync().AsTask());
            Assert.False(disposal.IsCompleted);
        }
        var beats = 0; run.OnChange += () => beats++;
        Call(module, "OnLiveSessionClosed", Json("""{"usage":{"seconds":8},"reason":"close_requested"}"""));
        Assert.Equal(!dispose, run.LiveFinalized);
        if (dispose) Assert.Equal(0, beats);
        Assert.Equal(0, module.Disposals); Assert.Equal(0, module.Session.Disposals);

        release.SetResult(true); await first;
        if (disposal is not null) await disposal;
        Assert.Equal(1, module.Disposals); Assert.Equal(1, module.Session.Disposals);
        Assert.Equal(1, module.Session.Stops); Assert.Equal(VoicePhase.Idle, run.Phase);
        await run.StartAsync(); Assert.Equal(dispose ? 1 : 2, js.Modules.Count);
    }

    [Fact]
    public async Task Repeated_dispose_and_stop_join_disposal_while_the_browser_is_draining()
    {
        var js = new Js(); await using var run = Run(js, new()); await run.StartAsync();
        var module = js.Modules[0]; var release = Gate<bool>(); module.Session.DuringStop = () => release.Task;
        var disposal = run.DisposeAsync().AsTask();
        Assert.Same(disposal, run.DisposeAsync().AsTask());
        Assert.Same(disposal, run.StopAsync());
        Assert.False(disposal.IsCompleted); Assert.Equal(1, module.Session.Stops);
        await run.StartAsync(); Assert.Single(js.Modules);
        release.SetResult(true); await disposal;
        Assert.Equal(1, module.Disposals); Assert.Equal(1, module.Session.Disposals);
    }

    [Fact]
    public async Task Obsolete_live_callbacks_cannot_mutate_a_restarted_run_or_create_another_session()
    {
        var js = new Js(); var http = new Http(); await using var run = Run(js, http); await run.StartAsync(); var old = js.Modules[0];
        await run.StopAsync(); await run.StartAsync(); var calls = http.Requests.Count;
        var beats = 0; var results = 0; run.OnChange += () => beats++;
        run.BackendResponseReceived = _ => { results++; return ValueTask.CompletedTask; };
        Call(old, "OnLiveTranscriptDelta", "User", "stale", 1d, 2d); Call(old, "OnLiveUsage", 99d);
        Call(old, "OnLiveSessionClosed", Json("""{"usage":{"seconds":99},"reason":"old"}"""));
        await (Task)Call(old, "OnBackendResponseAsync", Json("""{"id":"old","status":"completed"}"""))!;
        await Assert.ThrowsAsync<InvalidOperationException>(() => (Task<string>)Call(old, "OnLiveOfferAsync", "stale")!);
        Assert.Equal(calls, http.Requests.Count); Assert.Equal(0, beats); Assert.Equal(0, results);
        Assert.Empty(run.TranscriptDeltas); Assert.Null(run.LiveUsageSeconds); Assert.False(run.LiveFinalized);
        Assert.Equal(VoicePhase.Live, run.Phase);
    }
}
