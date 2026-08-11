namespace DRYL.Components;

/// <summary>Visual and semantic state of a single <see cref="DrylStep"/>.</summary>
public enum StepState
{
    /// <summary>Step has not been reached yet.</summary>
    Pending,
    /// <summary>Step is currently active (overridden by the parent's active tracking).</summary>
    Active,
    /// <summary>Step has been completed successfully — shows a check icon.</summary>
    Completed,
    /// <summary>Step encountered an error — shows an X icon.</summary>
    Error
}
