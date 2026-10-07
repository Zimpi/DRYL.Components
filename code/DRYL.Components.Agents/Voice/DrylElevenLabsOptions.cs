namespace DRYL.Components.Agents;

/// <summary>
/// Opts a voice session into an ElevenLabs agent instead of an OpenAI model. The agent itself —
/// its LLM, its voice model and the client tools it may call — lives in the ElevenLabs workspace;
/// this object only names it and carries what each session overrides.
/// </summary>
/// <remarks>
/// <para>The API key stays on the server. The runner exchanges it for a short-lived WebRTC
/// conversation token, and only that token reaches the browser.</para>
/// <para>The agent must allow the overrides a session sends: the prompt, the first message, the
/// language and the voice. Tools the agent calls must be registered on it as <em>client</em> tools
/// whose names match <see cref="DrylVoiceOptions.Tools"/>; they then run on the circuit exactly
/// like the tools of an OpenAI session.</para>
/// </remarks>
public sealed class DrylElevenLabsOptions
{
    /// <summary>ElevenLabs API key (<c>xi-api-key</c>). Never leaves the server.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The agent to talk to.</summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>Voice override for this session. Null keeps the agent's own voice.</summary>
    public string? VoiceId { get; set; }

    /// <summary>What the agent says first. Null or empty lets the user speak first.</summary>
    public string? FirstMessage { get; set; }

    /// <summary>API base URL — override it for a data-residency region.</summary>
    public string BaseUrl { get; set; } = "https://api.elevenlabs.io";

    /// <summary>
    /// Where the browser loads the ElevenLabs client from. Pinned to an exact version: the SDK's
    /// callbacks are the contract <c>dryl-voice.js</c> is written against. Point it at a copy on
    /// your own origin to drop the CDN.
    /// </summary>
    public string ClientScriptUrl { get; set; } =
        "https://cdn.jsdelivr.net/npm/@elevenlabs/client@1.27.0/dist/lib.iife.js";

    /// <summary>True when key and agent are both set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(AgentId);
}
