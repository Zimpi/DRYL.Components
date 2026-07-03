using System.Globalization;
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylLineChartTests : BunitContext
{
    private static readonly ChartSeries[] TwoSeries =
    [
        new("Umsatz", new double[] { 3, 7, 5, 9 }),
        new("Kosten", new double[] { 2, 4, 3, 6 }),
    ];

    [Fact]
    public void Renders_one_path_per_series_with_sequential_slot_colors()
    {
        var cut = Render<DrylLineChart>(ps => ps.Add(p => p.Series, TwoSeries));
        var paths = cut.FindAll("path.chart-line");
        Assert.Equal(2, paths.Count);
        Assert.Contains("var(--chart-1)", paths[0].GetAttribute("style"));
        Assert.Contains("var(--chart-2)", paths[1].GetAttribute("style"));
    }

    [Fact]
    public void ColorSlot_overrides_position_and_slot_7_plus_is_dim()
    {
        var series = new ChartSeries[]
        {
            new("A", new double[] { 1, 2 }) { ColorSlot = 5 },
            new("B", new double[] { 2, 1 }) { ColorSlot = 9 },
        };
        var cut = Render<DrylLineChart>(ps => ps.Add(p => p.Series, series));
        var paths = cut.FindAll("path.chart-line");
        Assert.Contains("var(--chart-5)", paths[0].GetAttribute("style"));
        Assert.Contains("var(--fg-dim)", paths[1].GetAttribute("style"));
    }

    [Fact]
    public void Legend_is_automatic_one_series_none_two_series_present()
    {
        var one = Render<DrylLineChart>(ps => ps.Add(p => p.Series,
            new[] { new ChartSeries("Solo", new double[] { 1, 2 }) }));
        Assert.Empty(one.FindAll(".chart-legend"));

        var two = Render<DrylLineChart>(ps => ps.Add(p => p.Series, TwoSeries));
        Assert.Single(two.FindAll(".chart-legend"));
        Assert.Equal(2, two.FindAll(".chart-legend-item").Count);
    }

    [Fact]
    public void ShowLegend_false_wins_over_auto()
    {
        var cut = Render<DrylLineChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.ShowLegend, false));
        Assert.Empty(cut.FindAll(".chart-legend"));
    }

    [Fact]
    public void Empty_series_renders_nothing()
    {
        var cut = Render<DrylLineChart>();
        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Hover_columns_are_focusable_and_labelled()
    {
        var cut = Render<DrylLineChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Labels, new[] { "Jan", "Feb", "Mär", "Apr" }));
        var cols = cut.FindAll(".chart-col");
        Assert.Equal(4, cols.Count);
        Assert.All(cols, c => Assert.Equal("0", c.GetAttribute("tabindex")));
        Assert.Contains("Jan", cols[0].GetAttribute("aria-label"));
        Assert.Contains("Umsatz", cols[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void Smooth_emits_cubic_beziers()
    {
        var cut = Render<DrylLineChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Smooth, true));
        var d = cut.Find("path.chart-line").GetAttribute("d")!;
        Assert.Contains("C", d);
    }

    [Fact]
    public void ShowMarkers_renders_a_dot_per_point()
    {
        var cut = Render<DrylLineChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.ShowMarkers, true));
        Assert.Equal(8, cut.FindAll(".chart-marker").Count);
    }

    [Fact]
    public void Ai_generated_adds_aura_and_wash()
    {
        var cut = Render<DrylLineChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Ai, AiState.Generated));
        Assert.Contains("ai-aura", cut.Find(".chart").ClassList);
        Assert.Single(cut.FindAll(".ai-aura-wash"));
    }

    [Fact]
    public void Svg_coordinates_stay_dot_decimal_under_german_culture()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            // 3 points → x at 0 / 50 / 100; y values force fractional percents.
            var cut = Render<DrylLineChart>(ps => ps.Add(p => p.Series,
                new[] { new ChartSeries("S", new double[] { 1, 2, 4 }) }));
            var d = cut.Find("path.chart-line").GetAttribute("d")!;
            // Every token must be exactly "x,y" with both halves invariant-parseable.
            var tokens = d.Split(['M', 'L', 'C', ' '], StringSplitOptions.RemoveEmptyEntries);
            Assert.All(tokens, t =>
            {
                var parts = t.Split(',');
                Assert.Equal(2, parts.Length);
                Assert.All(parts, p => double.Parse(p, CultureInfo.InvariantCulture));
            });
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }
}
