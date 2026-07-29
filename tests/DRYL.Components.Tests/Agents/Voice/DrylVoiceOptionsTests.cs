using System.ComponentModel;
using System.Text.Json.Nodes;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Voice;

/// <summary>The options are the whole developer-facing API — and the payload has to be exactly
/// what /v1/realtime/client_secrets expects, because a typo there fails at runtime only.</summary>
public class DrylVoiceOptionsTests
{
    [Fact]
    public void The_payload_carries_model_voice_and_speed()
    {
        var options = new DrylVoiceOptions
        {
            ApiKey = "sk-test",
            Model = "gpt-realtime-2.1",
            Voice = "cedar",
            Speed = 1.25,
            Instructions = "Sei knapp.",
        };

        var session = options.ToSessionPayload();

        Assert.Equal("realtime", (string?)session["type"]);
        Assert.Equal("gpt-realtime-2.1", (string?)session["model"]);
        Assert.Equal("Sei knapp.", (string?)session["instructions"]);
        Assert.Equal("cedar", (string?)session["audio"]!["output"]!["voice"]);
        Assert.Equal(1.25, (double?)session["audio"]!["output"]!["speed"]);
    }

    [Fact]
    public void Audio_format_is_left_to_the_peer_connection()
    {
        // WebRTC negotiates Opus by itself. Pinning audio/pcm here is how you get a session that
        // connects and then stays silent.
        var session = new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload();

        Assert.Null(session["audio"]!["input"]!["format"]);
        Assert.Null(session["audio"]!["output"]!["format"]);
    }

    [Fact]
    public void Semantic_vad_is_the_default_turn_detection()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload();

        Assert.Equal("semantic_vad", (string?)session["audio"]!["input"]!["turn_detection"]!["type"]);
    }

    [Fact]
    public void Server_vad_is_available_for_the_impatient()
    {
        var session = new DrylVoiceOptions
        {
            ApiKey = "sk-test",
            TurnDetection = VoiceTurnDetection.ServerVad,
        }.ToSessionPayload();

        Assert.Equal("server_vad", (string?)session["audio"]!["input"]!["turn_detection"]!["type"]);
    }

    [Fact]
    public void Transcription_is_on_so_the_user_half_of_the_conversation_survives()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test", Language = "de" }.ToSessionPayload();

        var transcription = session["audio"]!["input"]!["transcription"]!;
        Assert.Equal("gpt-4o-transcribe", (string?)transcription["model"]);
        Assert.Equal("de", (string?)transcription["language"]);
    }

    [Fact]
    public void Transcription_can_be_switched_off_entirely()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test", TranscriptionModel = null }
            .ToSessionPayload();

        Assert.Null(session["audio"]!["input"]!["transcription"]);
    }

    [Fact]
    public void Noise_reduction_can_be_switched_off_entirely()
    {
        var session = new DrylVoiceOptions
        {
            ApiKey = "sk-test",
            NoiseReduction = VoiceNoiseReduction.Off,
        }.ToSessionPayload();

        Assert.Null(session["audio"]!["input"]!["noise_reduction"]);
    }

    [Fact]
    public void Tools_are_emitted_as_realtime_function_schemas()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(
            ([Description("the city")] string city) => "sunny",
            "get_weather",
            "Looks up the weather"));

        var tools = (JsonArray)options.ToSessionPayload()["tools"]!;

        var tool = tools[0]!;
        Assert.Equal("function", (string?)tool["type"]);
        Assert.Equal("get_weather", (string?)tool["name"]);
        Assert.Equal("Looks up the weather", (string?)tool["description"]);
        Assert.NotNull(tool["parameters"]);
    }

    [Fact]
    public void Without_tools_there_is_no_tool_choice_either()
    {
        var session = new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload();

        Assert.Null(session["tools"]);
        Assert.Null(session["tool_choice"]);
    }

    [Fact]
    public void Reasoning_is_omitted_unless_asked_for()
    {
        Assert.Null(new DrylVoiceOptions { ApiKey = "sk-test" }.ToSessionPayload()["reasoning"]);

        var session = new DrylVoiceOptions { ApiKey = "sk-test", ReasoningEffort = "low" }
            .ToSessionPayload();

        Assert.Equal("low", (string?)session["reasoning"]!["effort"]);
    }

    [Fact]
    public void An_invented_tool_name_is_not_found()
    {
        var options = new DrylVoiceOptions { ApiKey = "sk-test" };
        options.Tools.Add(AIFunctionFactory.Create(() => "ok", "list_projects"));

        Assert.NotNull(options.FindTool("list_projects"));
        Assert.Null(options.FindTool("delete_everything"));
    }
}
