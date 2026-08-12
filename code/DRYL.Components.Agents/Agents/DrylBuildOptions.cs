namespace DRYL.Components.Agents;

/// <summary>Options for <see cref="DrylAgentRunner.StartBuild{T}"/>.</summary>
public sealed class DrylBuildOptions
{
    /// <summary>Safety cap on refinement rounds; <c>null</c> = unbounded. Default 12.</summary>
    public int? MaxRounds { get; init; } = 12;

    /// <summary>Overrides the framework's default iterative-build guidance prompt.</summary>
    public string? Guidance { get; init; }

    /// <summary>Overrides the auto-generated update tool name (default <c>update_&lt;t-name&gt;</c>).</summary>
    public string? UpdateToolName { get; init; }

    /// <summary>
    /// Target wall-clock duration for each <c>update_&lt;T&gt;</c> round's progressive reveal
    /// (the round's new/changed fields type in over this span — a guided, type-as-you-go reveal).
    /// This is a target per round, not a per-character delay — long and short patches both take
    /// roughly this long. <see cref="System.TimeSpan.Zero"/> (or a negative value) disables the
    /// reveal and merges the patch atomically (identical to a single merge). Default 1.2 s.
    /// </summary>
    public TimeSpan RevealDuration { get; init; } = TimeSpan.FromMilliseconds(1200);
}
