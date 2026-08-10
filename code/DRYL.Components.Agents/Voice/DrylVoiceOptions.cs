using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>How the model decides that the user has finished a turn.</summary>
public enum VoiceTurnDetection
{
    /// <summary>Waits for a complete thought rather than for silence — the natural choice for a
    /// conversation, because it stops the model from cutting into a thinking pause.</summary>
    SemanticVad,

    /// <summary>Plain silence detection. Snappier, but it interrupts hesitant speakers.</summary>
    ServerVad,
}

/// <summary>Server-side noise handling on the input stream.</summary>
public enum VoiceNoiseReduction
{
    /// <summary>Headset or handset — the microphone sits close to the mouth.</summary>
    NearField,

    /// <summary>Laptop or room microphone.</summary>
    FarField,

    /// <summary>No server-side reduction.</summary>
    Off,
}

/// <summary>
/// Everything a developer configures about a voice session. There is deliberately no settings UI
/// anywhere in DRYL for this: voice, persona and model are code, not preferences.
/// </summary>
/// <remarks>
/// The whole object is baked into the ephemeral client secret, so the browser can change none of
/// it. Note that <see cref="Voice"/> is locked by the API once a session has emitted audio —
/// switching voices means starting a new session, not updating this object.
/// </remarks>
public sealed class DrylVoiceOptions
{
    /// <summary>OpenAI API key. Stays on the server — only the minted <c>ek_…</c> token ever
    /// reaches the browser.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Realtime model. Also <c>gpt-realtime-2</c> and <c>gpt-realtime-2.1-mini</c>.</summary>
    public string Model { get; set; } = "gpt-realtime-2.1";

    /// <summary>The system prompt — role, personality, tone, language. Hand it the same prompt
    /// the text assistant uses, plus whatever is specific to being spoken aloud.</summary>
    public string? Instructions { get; set; }

    /// <summary>One of alloy, ash, ballad, coral, echo, sage, shimmer, verse, marin, cedar.
    /// <c>marin</c> and <c>cedar</c> are the highest quality.</summary>
    public string Voice { get; set; } = "marin";

    /// <summary>Speaking rate, 0.25–1.5.</summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>How the end of a user turn is detected.</summary>
    public VoiceTurnDetection TurnDetection { get; set; } = VoiceTurnDetection.SemanticVad;

    /// <summary>Server-side input noise reduction.</summary>
    public VoiceNoiseReduction NoiseReduction { get; set; } = VoiceNoiseReduction.NearField;

    /// <summary>Reasoning effort — <c>low</c>, <c>medium</c> or <c>high</c>. Null leaves it to the
    /// model's own default. Only the 2.1 family supports it.</summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>Model that transcribes the user's audio. Null switches transcription off — and
    /// with it every trace of what the user actually said.</summary>
    public string? TranscriptionModel { get; set; } = "gpt-4o-transcribe";

    /// <summary>ISO language code for the transcription, e.g. <c>de</c>. Null lets it detect.</summary>
    public string? Language { get; set; }

    /// <summary>The tools the voice may call — hand it the same list the text agent has.</summary>
    public IList<AITool> Tools { get; set; } = new List<AITool>();

    /// <summary>Silence after which the session closes itself. An open session bills per minute
    /// whether or not anyone is talking.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Hard cap on session length. The API's own limit is 60 minutes.</summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>API base URL — override it for Azure or a proxy.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>A stable, privacy-preserving user identifier (e.g. a hashed internal id), sent as
    /// the <c>OpenAI-Safety-Identifier</c> header.</summary>
    public string? SafetyIdentifier { get; set; }

    /// <summary>True when a key is configured — hosts use this to decide whether to offer voice
    /// at all.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Builds the <c>session</c> block for <c>POST /v1/realtime/client_secrets</c>.
    /// </summary>
    /// <remarks>Audio formats are deliberately absent: over WebRTC the peer connection negotiates
    /// the codec itself, and pinning <c>audio/pcm</c> here produces a session that connects and
    /// then stays silent.</remarks>
    public JsonNode ToSessionPayload()
    {
        var input = new JsonObject
        {
            ["turn_detection"] = new JsonObject
            {
                ["type"] = TurnDetection == VoiceTurnDetection.ServerVad
                    ? "server_vad"
                    : "semantic_vad",
            },
        };

        if (NoiseReduction != VoiceNoiseReduction.Off)
        {
            input["noise_reduction"] = new JsonObject
            {
                ["type"] = NoiseReduction == VoiceNoiseReduction.FarField ? "far_field" : "near_field",
            };
        }

        if (!string.IsNullOrWhiteSpace(TranscriptionModel))
        {
            var transcription = new JsonObject { ["model"] = TranscriptionModel };
            if (!string.IsNullOrWhiteSpace(Language)) transcription["language"] = Language;
            input["transcription"] = transcription;
        }

        var session = new JsonObject
        {
            ["type"] = "realtime",
            ["model"] = Model,
            ["output_modalities"] = new JsonArray("audio"),
            ["audio"] = new JsonObject
            {
                ["input"] = input,
                ["output"] = new JsonObject
                {
                    ["voice"] = Voice,
                    ["speed"] = Speed,
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(Instructions)) session["instructions"] = Instructions;

        if (!string.IsNullOrWhiteSpace(ReasoningEffort))
            session["reasoning"] = new JsonObject { ["effort"] = ReasoningEffort };

        var functions = ToolSchemas();
        if (functions.Count > 0)
        {
            session["tools"] = functions;
            session["tool_choice"] = "auto";
        }

        return session;
    }

    private JsonArray ToolSchemas()
    {
        var array = new JsonArray();

        foreach (var tool in Tools)
        {
            if (tool is not AIFunction function) continue;

            array.Add(new JsonObject
            {
                ["type"] = "function",
                ["name"] = function.Name,
                ["description"] = function.Description,
                ["parameters"] = JsonNode.Parse(function.JsonSchema.GetRawText()),
            });
        }

        return array;
    }

    /// <summary>Finds a tool by the name the model used, or null if it invented one.</summary>
    internal AIFunction? FindTool(string name) =>
        Tools.OfType<AIFunction>()
             .FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));
}
