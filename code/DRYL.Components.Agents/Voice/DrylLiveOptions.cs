namespace DRYL.Components.Agents;

/// <summary>Backend reasoning and tools delegated to Responses by GPT-Live.</summary>
public sealed class DrylLiveOptions
{
    /// <summary>The Responses model that performs delegated work.</summary>
    public string BackendModel { get; set; } = "gpt-5.6-terra";

    /// <summary>Task and tool instructions for the backend, separate from the spoken persona.</summary>
    public string? BackendInstructions { get; set; }

    /// <summary>Backend reasoning effort. Null or whitespace uses the provider default.</summary>
    public string? ReasoningEffort { get; set; } = "medium";

    /// <summary>Maximum tokens per delegated response, at least 16; null uses the provider default.</summary>
    public int? MaxOutputTokens { get; set; } = 4096;

    /// <summary>Allows the delegated backend to use hosted web search.</summary>
    public bool EnableWebSearch { get; set; }
}
