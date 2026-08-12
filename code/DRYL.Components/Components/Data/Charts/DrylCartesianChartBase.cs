using DRYL.Components.Internal;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>
/// Base for cartesian charts (line / bar / area): series, category labels,
/// axis/grid options, y-range with nice ticks, percent-space scales and the
/// shared frame layout (axes, grid, hover columns, tooltips, legend).
/// </summary>
public abstract class DrylCartesianChartBase : DrylChartBase
{
    /// <summary>The data series to plot.</summary>
    [Parameter] public IReadOnlyList<ChartSeries>? Series { get; set; }

    /// <summary>Category labels for the x-axis (and tooltip titles).</summary>
    [Parameter] public IReadOnlyList<string>? Labels { get; set; }

    /// <summary>Show the x-axis label row.</summary>
    [Parameter] public bool ShowXAxis { get; set; } = true;

    /// <summary>Show the y-axis tick column.</summary>
    [Parameter] public bool ShowYAxis { get; set; } = true;

    /// <summary>Show horizontal gridlines at the y-ticks.</summary>
    [Parameter] public bool ShowGridLines { get; set; } = true;

    /// <summary>Fixed lower bound of the y-range. Default: automatic.</summary>
    [Parameter] public double? YMin { get; set; }

    /// <summary>Fixed upper bound of the y-range. Default: automatic.</summary>
    [Parameter] public double? YMax { get; set; }

    /// <summary>Bar charts place x at band centers and include 0 in the range.</summary>
    protected virtual bool Banded => false;

    protected bool HasData => Series is { Count: > 0 } && Series.Any(s => s.Data.Count > 0);
    protected int PointCount => Series!.Max(s => s.Data.Count);

    protected double Min { get; private set; }
    protected double Max { get; private set; }
    protected IReadOnlyList<double> TickValues { get; private set; } = [];

    /// <summary>Values that must fit the y-range (stacked bars override with stack tops).</summary>
    protected virtual IEnumerable<double> RangeValues()
    {
        foreach (var s in Series!)
            foreach (var v in s.Data)
                yield return v;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!HasData) return;

        double lo = double.MaxValue, hi = double.MinValue;
        foreach (var v in RangeValues())
        {
            if (v < lo) lo = v;
            if (v > hi) hi = v;
        }
        if (Banded) { lo = Math.Min(lo, 0); hi = Math.Max(hi, 0); }
        lo = YMin ?? lo;
        hi = YMax ?? hi;

        TickValues = ChartMath.NiceTicks(lo, hi);
        Min = Math.Min(lo, TickValues[0]);
        Max = Math.Max(hi, TickValues[^1]);
    }

    /// <summary>X position (0–100, percent space) of category index i.</summary>
    protected double XPct(int i)
    {
        var n = PointCount;
        if (Banded) return (i + 0.5) / n * 100;
        return n <= 1 ? 50 : (double)i / (n - 1) * 100;
    }

    /// <summary>Y position (0–100 from the top) of value v.</summary>
    protected double YPct(double v)
    {
        var span = Max - Min;
        return span <= 0 ? 50 : (1 - (v - Min) / span) * 100;
    }

    /// <summary>Color for series i (honors ColorSlot; beyond slot 6 → muted).</summary>
    protected string SeriesColor(int i) => SlotColor(Series![i].ColorSlot, i);

    protected bool LegendVisible => ShowLegend ?? Series!.Count >= 2;

    protected string SummaryLabel =>
        AriaLabel ?? string.Join(", ", Series!.Select(s => s.Name));

    private string LabelAt(int i) =>
        Labels is not null && i < Labels.Count ? Labels[i] : (i + 1).ToString();

    /// <summary>Build the frame layout: ticks, x-labels (thinned), hover columns, legend.</summary>
    protected CartesianLayout BuildLayout()
    {
        var n = PointCount;
        var ticks = TickValues
            .Select(t => new AxisTick(YPct(t), FormatValue(t)))
            .ToList();
        double? zeroPct = Min < 0 && Max > 0 ? YPct(0) : null;

        // Thin x labels so they never collide (~max 8 shown).
        var step = Math.Max(1, (int)Math.Ceiling(n / 8.0));
        var xLabels = new List<AxisTick>();
        for (var i = 0; i < n; i += step)
            xLabels.Add(new AxisTick(XPct(i), LabelAt(i)));

        // Hover columns: boundaries midway between consecutive x-centers.
        var columns = new List<HoverColumn>(n);
        for (var i = 0; i < n; i++)
        {
            var left = i == 0 ? 0 : (XPct(i - 1) + XPct(i)) / 2;
            var right = i == n - 1 ? 100 : (XPct(i) + XPct(i + 1)) / 2;
            var rows = new List<TooltipRow>();
            var aria = new System.Text.StringBuilder(LabelAt(i));
            aria.Append(':');
            for (var s = 0; s < Series!.Count; s++)
            {
                if (i >= Series[s].Data.Count) continue;
                var val = FormatValue(Series[s].Data[i]);
                rows.Add(new TooltipRow(SeriesColor(s), Series[s].Name, val));
                aria.Append(' ').Append(Series[s].Name).Append(' ').Append(val).Append(';');
            }
            columns.Add(new HoverColumn(left, right - left, aria.ToString(),
                Flip: XPct(i) > 55, LabelAt(i), rows));
        }

        var legend = Series!
            .Select((s, idx) => new LegendItem(SeriesColor(idx), s.Name))
            .ToList();

        return new CartesianLayout(Height, ticks, zeroPct, xLabels, columns, legend,
            ShowXAxis, ShowYAxis, ShowGridLines, LegendVisible);
    }
}
