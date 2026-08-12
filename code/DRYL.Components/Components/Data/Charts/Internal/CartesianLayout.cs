namespace DRYL.Components.Internal;

// Internal chart-frame infrastructure. These types are public only because
// Blazor requires component [Parameter]s to be public — they live in the
// .Internal namespace on purpose and are NOT a stable public API.

/// <summary>Internal — one axis tick (percent position + display label).</summary>
public sealed record AxisTick(double Pct, string Label);

/// <summary>Internal — one tooltip line (swatch color, series name, value).</summary>
public sealed record TooltipRow(string Color, string Name, string Value);

/// <summary>Internal — one legend entry.</summary>
public sealed record LegendItem(string Color, string Name);

/// <summary>Internal — one hover column with its tooltip payload.</summary>
public sealed record HoverColumn(
    double LeftPct, double WidthPct, string Aria, bool Flip,
    string Title, IReadOnlyList<TooltipRow> Rows);

/// <summary>Internal — everything ChartFrame needs to render the cartesian skeleton.</summary>
public sealed record CartesianLayout(
    int Height,
    IReadOnlyList<AxisTick> Ticks,
    double? ZeroPct,
    IReadOnlyList<AxisTick> XLabels,
    IReadOnlyList<HoverColumn> Columns,
    IReadOnlyList<LegendItem> Legend,
    bool ShowXAxis, bool ShowYAxis, bool ShowGrid, bool ShowLegend);
