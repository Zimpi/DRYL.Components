namespace DRYL.Components;

/// <summary>
/// Render context handed to <c>DrylAiStream</c>'s child content: the text accumulated so
/// far and the current <see cref="AiState"/> of the stream. Both update as tokens arrive.
/// </summary>
public sealed class AiStreamContext
{
    /// <summary>The text streamed so far.</summary>
    public string Text { get; internal set; } = string.Empty;

    /// <summary>The current AI state of the stream (Thinking → Streaming → Generated → settled).</summary>
    public AiState State { get; internal set; }
}
