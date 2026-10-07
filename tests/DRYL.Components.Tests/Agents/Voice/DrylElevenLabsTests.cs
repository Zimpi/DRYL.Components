using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests.Agents.Voice;

public class DrylElevenLabsTests
{
    private static DrylVoiceOptions Options() => new()
    {
        Instructions = "Speak like a consejero.",
        Language = "de",
        ElevenLabs = new()
        {
            ApiKey = "xi-server-only", AgentId = "agent_fixture", VoiceId = "voice_fixture",
            BaseUrl = "https://eu.example/",
        },
    };

    private sealed record Request(string Path, string? Key);
    private sealed class Http : HttpMessageHandler
    {
        public List<Request> Requests { get; } = [];
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Response = """{"token":"lk_token"}""";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(new(request.RequestUri!.AbsoluteUri,
                request.Headers.TryGetValues("xi-api-key", out var values) ? values.Single() : null));
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Response) });
        }
    }

    private sealed class Js : IJSRuntime
    {
        public Module Module { get; } = new();
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult((T)(object)Module);
    }

    private sealed class Module : IJSObjectReference
    {
        public object? Callback;
        public string? Token;
        public string? Config;
        public Session Session { get; } = new();
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Token = args![0] as string;
            Config = JsonSerializer.Serialize(args[1]);
            Callback = args[2]!.GetType().GetProperty("Value")!.GetValue(args[2]);
            Session.Module = this;
            return ValueTask.FromResult((T)(object)Session);
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class Session : IJSObjectReference
    {
        public Module Module = null!;
        public int Stops;
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, default, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "start")
            {
                Call(Module, "OnConversationStarted", "conv_fixture");
                Call(Module, "OnConnected");
            }
            if (identifier == "stop") Stops++;
            return ValueTask.FromResult(default(T)!);
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static object? Call(Module module, string name, params object[] args) =>
        module.Callback!.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public)!.Invoke(module.Callback, args);

    private static DrylVoiceRun Run(Js js, Http http, DrylVoiceOptions? options = null) =>
        new DrylVoiceRunner(js, new HttpClient(http)).Create(options ?? Options());

    [Fact]
    public void ElevenLabs_is_configured_by_its_own_key_and_agent_not_the_OpenAI_key()
    {
        var options = Options();
        Assert.True(options.IsConfigured);

        options.ElevenLabs!.AgentId = "";
        Assert.False(options.IsConfigured);

        options.ElevenLabs = null;
        options.ApiKey = "sk-openai";
        Assert.True(options.IsConfigured);
    }

    [Fact]
    public void Overrides_carry_prompt_language_voice_and_recent_history()
    {
        var history = Enumerable.Range(0, 500).Select(i => new DrylVoiceMessage(
            i % 2 == 0 ? VoiceRole.User : VoiceRole.Assistant, $"Zeile {i}"));

        var overrides = Options().ToElevenLabsOverrides(history);

        var prompt = (string)overrides["agent"]!["prompt"]!["prompt"]!;
        Assert.StartsWith("Speak like a consejero.", prompt);
        Assert.Contains("## Conversation so far", prompt);
        Assert.EndsWith("Assistant: Zeile 499", prompt);
        Assert.DoesNotContain("User: Zeile 0\n", prompt);
        Assert.True(prompt.Length < 6000 + 200);
        Assert.Equal("de", (string?)overrides["agent"]!["language"]);
        Assert.Equal("", (string?)overrides["agent"]!["firstMessage"]);
        Assert.Equal("voice_fixture", (string?)overrides["tts"]!["voiceId"]);
    }

    [Fact]
    public void Overrides_without_history_or_voice_leave_the_agent_its_own()
    {
        var options = Options();
        options.ElevenLabs!.VoiceId = null;

        var overrides = options.ToElevenLabsOverrides([]);

        Assert.Equal("Speak like a consejero.", (string?)overrides["agent"]!["prompt"]!["prompt"]);
        Assert.Null(overrides["tts"]);
    }

    [Fact]
    public async Task Start_fetches_a_token_server_side_and_hands_the_browser_only_token_and_overrides()
    {
        var js = new Js(); var http = new Http();
        var options = Options();
        options.Tools.Add(AIFunctionFactory.Create((string auftrag) => auftrag, "auftrag_abgeben"));
        await using var run = Run(js, http, options);

        await run.StartAsync([new(VoiceRole.User, "Was liegt im Lager?")]);

        var request = Assert.Single(http.Requests);
        Assert.Equal("https://eu.example/v1/convai/conversation/token?agent_id=agent_fixture", request.Path);
        Assert.Equal("xi-server-only", request.Key);

        Assert.Equal("lk_token", js.Module.Token);
        var config = JsonNode.Parse(js.Module.Config!)!;
        Assert.False((bool)config["live"]!);
        Assert.Equal("auftrag_abgeben", (string?)config["elevenLabs"]!["tools"]![0]);
        Assert.Contains("Was liegt im Lager?", (string?)config["elevenLabs"]!["overrides"]!["agent"]!["prompt"]!["prompt"]);
        Assert.Empty((JsonArray)config["history"]!);
        Assert.DoesNotContain("xi-server-only", js.Module.Config);

        Assert.Equal(VoicePhase.Live, run.Phase);
        Assert.Equal("conv_fixture", run.ConversationId);
    }

    [Fact]
    public async Task A_refused_token_fails_the_session_with_the_api_message()
    {
        var js = new Js();
        var http = new Http
        {
            Status = HttpStatusCode.Unauthorized,
            Response = """{"detail":{"status":"invalid_api_key","message":"Invalid API key"}}""",
        };
        await using var run = Run(js, http);

        await run.StartAsync();

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.Equal("Invalid API key", run.Error?.Message);
        Assert.Null(js.Module.Config);
    }

    [Fact]
    public async Task A_plain_detail_string_is_the_failure_message()
    {
        var js = new Js();
        var http = new Http { Status = HttpStatusCode.NotFound, Response = """{"detail":"Agent not found"}""" };
        await using var run = Run(js, http);

        await run.StartAsync();

        Assert.Equal("Agent not found", run.Error?.Message);
    }

    [Fact]
    public async Task A_response_without_a_token_fails_before_the_browser_starts()
    {
        var js = new Js();
        var http = new Http { Response = "{}" };
        await using var run = Run(js, http);

        await run.StartAsync();

        Assert.Equal(VoicePhase.Idle, run.Phase);
        Assert.NotNull(run.Error);
        Assert.Null(js.Module.Config);
    }

    [Fact]
    public async Task A_blank_conversation_id_is_ignored()
    {
        var js = new Js(); var http = new Http();
        await using var run = Run(js, http);
        await run.StartAsync();

        Call(js.Module, "OnConversationStarted", " ");

        Assert.Equal("conv_fixture", run.ConversationId);
    }

    [Fact]
    public void An_OpenAI_session_records_no_conversation_id()
    {
        var run = new DrylVoiceRunner(new Js(), new HttpClient(new Http())).Create(new DrylVoiceOptions { ApiKey = "sk" });

        run.OnConversationStarted("conv_stray");

        Assert.Null(run.ConversationId);
    }

    [Fact]
    public async Task Client_tool_calls_run_on_the_circuit_like_any_other_tool()
    {
        var js = new Js(); var http = new Http();
        var options = Options();
        options.Tools.Add(AIFunctionFactory.Create((string auftrag) => $"Erledigt: {auftrag}", "auftrag_abgeben"));
        await using var run = Run(js, http, options);
        await run.StartAsync();

        var result = await (Task<string>)Call(js.Module, "OnToolCallAsync", "el_1", "auftrag_abgeben", """{"auftrag":"Bestand Lager Nord"}""")!;

        Assert.Contains("Erledigt: Bestand Lager Nord", result);
    }

    [Fact]
    public async Task Audio_tags_are_performed_not_transcribed()
    {
        var js = new Js(); var http = new Http();
        await using var run = Run(js, http);
        await run.StartAsync();

        Call(js.Module, "OnTranscript", "Assistant", "[nachdenklich] Das Lager Nord, hermano. [laughs] Sauber.");
        Call(js.Module, "OnTranscript", "Assistant", "[sighs]");
        Call(js.Module, "OnTranscript", "Assistant", "Sprecher [49] hat das gesagt.");
        Call(js.Module, "OnTranscript", "User", "[räuspert sich] Danke");

        Assert.Equal(
            ["Das Lager Nord, hermano. Sauber.", "Sprecher [49] hat das gesagt.", "[räuspert sich] Danke"],
            run.Transcript.Select(z => z.Text));
    }

    [Fact]
    public void An_OpenAI_session_keeps_brackets_as_spoken()
    {
        var run = new DrylVoiceRunner(new Js(), new HttpClient(new Http())).Create(new DrylVoiceOptions { ApiKey = "sk" });

        run.OnTranscript("Assistant", "[laughs] Gut.");

        Assert.Equal("[laughs] Gut.", run.Transcript.Single().Text);
    }

    [Fact]
    public async Task A_new_session_forgets_the_old_conversation_id()
    {
        var js = new Js(); var http = new Http();
        await using var run = Run(js, http);
        await run.StartAsync();
        Assert.Equal("conv_fixture", run.ConversationId);

        await run.StopAsync();
        Assert.Equal("conv_fixture", run.ConversationId);

        run.MarkConnecting();
        Assert.Null(run.ConversationId);
    }
}
