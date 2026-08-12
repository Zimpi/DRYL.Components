namespace DRYL.Components;

/// <summary>One segment of a <see cref="DrylDonutChart"/>.</summary>
/// <param name="Label">Segment name shown in the legend and tooltip.</param>
/// <param name="Value">Segment value; share of the total drives the sweep angle.</param>
public sealed record ChartSegment(string Label, double Value)
{
    /// <summary>Optional fixed palette slot (1–6). Defaults to the segment's position.</summary>
    public int? ColorSlot { get; init; }
}
