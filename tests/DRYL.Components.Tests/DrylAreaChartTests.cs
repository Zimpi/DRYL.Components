using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylAreaChartTests : BunitContext
{
    [Fact]
    public void Renders_area_fill_and_line_per_series()
    {
        var cut = Render<DrylAreaChart>(ps => ps.Add(p => p.Series,
            new[] { new ChartSeries("A", new double[] { 1, 3, 2 }),
                    new ChartSeries("B", new double[] { 2, 1, 4 }) }));
        Assert.Equal(2, cut.FindAll("path.chart-area").Count);
        Assert.Equal(2, cut.FindAll("path.chart-line").Count);
    }

    [Fact]
    public void Area_closes_to_the_zero_baseline()
    {
        var cut = Render<DrylAreaChart>(ps => ps.Add(p => p.Series,
            new[] { new ChartSeries("A", new double[] { 0, 4 }) }));
        var d = cut.Find("path.chart-area").GetAttribute("d")!;
        Assert.EndsWith("Z", d.Trim());
    }

    [Fact]
    public void Fill_uses_a_per_series_gradient()
    {
        var cut = Render<DrylAreaChart>(ps => ps.Add(p => p.Series,
            new[] { new ChartSeries("A", new double[] { 1, 2 }) }));
        var area = cut.Find("path.chart-area");
        var fill = area.GetAttribute("fill")!;
        Assert.StartsWith("url(#", fill);
        Assert.Single(cut.FindAll("linearGradient"));
    }
}
