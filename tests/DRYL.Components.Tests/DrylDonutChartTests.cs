using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylDonutChartTests : BunitContext
{
    private static readonly ChartSegment[] Segs =
    [
        new("Cloud", 42),
        new("On-Prem", 31),
        new("Edge", 27),
    ];

    [Fact]
    public void Renders_one_slice_per_positive_segment()
    {
        var cut = Render<DrylDonutChart>(ps => ps.Add(p => p.Segments, Segs));
        Assert.Equal(3, cut.FindAll(".donut-slice").Count);
        Assert.Equal(3, cut.FindAll(".donut-slice path").Count);
    }

    [Fact]
    public void Zero_and_negative_segments_are_skipped()
    {
        var cut = Render<DrylDonutChart>(ps => ps.Add(p => p.Segments,
            new[] { new ChartSegment("A", 5), new ChartSegment("B", 0), new ChartSegment("C", -3) }));
        Assert.Single(cut.FindAll(".donut-slice"));
    }

    [Fact]
    public void Paths_are_keyboard_reachable_with_percent_labels()
    {
        var cut = Render<DrylDonutChart>(ps => ps.Add(p => p.Segments, Segs));
        var path = cut.Find(".donut-slice path");
        Assert.Equal("0", path.GetAttribute("tabindex"));
        var label = path.GetAttribute("aria-label")!;
        Assert.Contains("Cloud", label);
        Assert.Contains("42", label);
        Assert.Contains("%", label);
    }

    [Fact]
    public void Center_content_renders_in_the_hole()
    {
        var cut = Render<DrylDonutChart>(ps => ps
            .Add(p => p.Segments, Segs)
            .Add(p => p.CenterContent, "<b>73</b>"));
        Assert.Contains("<b>73</b>", cut.Find(".donut-center").InnerHtml);
    }

    [Fact]
    public void Legend_lists_every_segment()
    {
        var cut = Render<DrylDonutChart>(ps => ps.Add(p => p.Segments, Segs));
        Assert.Equal(3, cut.FindAll(".chart-legend-item").Count);
    }

    [Fact]
    public void Pie_mode_uses_zero_inner_radius()
    {
        var cut = Render<DrylDonutChart>(ps => ps
            .Add(p => p.Segments, Segs)
            .Add(p => p.InnerRadius, 0.0));
        // Inner arc collapses to the centre.
        var d = cut.Find(".donut-slice path").GetAttribute("d")!;
        Assert.Contains("A0,0", d);
    }
}
