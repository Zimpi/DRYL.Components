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
}
