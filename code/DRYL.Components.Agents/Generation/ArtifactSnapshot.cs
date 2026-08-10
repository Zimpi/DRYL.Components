namespace DRYL.Components.Agents.Generation;

/// <summary>A live snapshot of an artifact build, handed to <c>DrylAiBuild</c>'s child content.</summary>
public sealed class ArtifactSnapshot<T>
{
    /// <summary>The artifact merged so far (fields not yet provided are null/default).</summary>
    public T? Artifact { get; internal set; }

    /// <summary>The AI state of the build (Thinking → Streaming → Generated).</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The number of refine steps applied so far.</summary>
    public int Round { get; internal set; }

    /// <summary>True once the build has settled after the Generated reveal.</summary>
    public bool IsComplete { get; internal set; }
}
