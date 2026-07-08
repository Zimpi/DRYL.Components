namespace DRYL.Components.Agents.Generation;

/// <summary>A partial snapshot of a structured generation, handed to <c>DrylAiGenerate</c>'s child content.</summary>
public sealed class GenerationSnapshot<T>
{
    /// <summary>The partial value parsed so far (fields not yet streamed are null/default).</summary>
    public T? Value { get; internal set; }

    /// <summary>The AI state of the generation (Thinking → Streaming → Generated).</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>True once the stream has completed.</summary>
    public bool IsComplete { get; internal set; }

    /// <summary>The raw model output streamed so far (the accumulated JSON buffer, before repair/parsing).</summary>
    public string Raw { get; internal set; } = "";
}
