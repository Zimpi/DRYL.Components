namespace DRYL.Components;

/// <summary>
/// One data series for the cartesian charts (<see cref="DrylLineChart"/>,
/// <see cref="DrylBarChart"/>, <see cref="DrylAreaChart"/>).
/// </summary>
/// <param name="Name">Series name shown in the legend and tooltips.</param>
/// <param name="Data">The series values, one per category.</param>
public sealed record ChartSeries(string Name, IReadOnlyList<double> Data)
{
    /// <summary>
    /// Optional fixed palette slot (1–6). Defaults to the series' position in the
    /// list. Pin it so a series keeps its color when other series are filtered away.
    /// </summary>
    public int? ColorSlot { get; init; }
}
