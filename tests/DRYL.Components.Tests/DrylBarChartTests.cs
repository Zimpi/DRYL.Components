using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylBarChartTests : BunitContext
{
    private static readonly ChartSeries[] TwoSeries =
    [
        new("A", new double[] { 3, 5 }),
        new("B", new double[] { 2, 4 }),
    ];

    [Fact]
    public void Grouped_renders_one_bar_per_series_per_category()
    {
        var cut = Render<DrylBarChart>(ps => ps.Add(p => p.Series, TwoSeries));
        Assert.Equal(2, cut.FindAll(".chart-band").Count);
        Assert.Equal(4, cut.FindAll(".chart-bar").Count);
    }

    [Fact]
    public void Negative_bars_get_the_neg_class()
    {
        var cut = Render<DrylBarChart>(ps => ps.Add(p => p.Series,
            new[] { new ChartSeries("A", new double[] { 3, -2 }) }));
        Assert.Single(cut.FindAll(".chart-bar-neg"));
    }

    [Fact]
    public void Stacked_renders_segments_with_cap_on_topmost()
    {
        var cut = Render<DrylBarChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Stacked, true));
        Assert.Equal(4, cut.FindAll(".chart-seg").Count);
        // Topmost segment of each stack (series B) carries the rounded cap.
        Assert.Equal(2, cut.FindAll(".chart-seg-cap").Count);
    }

    [Fact]
    public void Stacked_range_covers_the_stack_total()
    {
        // Totals 5 and 9 → topmost tick must reach at least 9 (nice → 10).
        var cut = Render<DrylBarChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Stacked, true));
        Assert.Contains("10", cut.Find(".chart-yaxis").TextContent);
    }
}
