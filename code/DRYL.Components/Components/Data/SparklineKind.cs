namespace DRYL.Components;

/// <summary>Render style of a <see cref="DrylSparkline"/>.</summary>
public enum SparklineKind
{
    /// <summary>Connected line through the data points.</summary>
    Line,
    /// <summary>Filled area under a connected line.</summary>
    Area,
    /// <summary>Vertical bars, one per data point.</summary>
    Bar
}
