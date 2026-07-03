using DRYL.Components.Internal;

namespace DRYL.Components.Tests;

/// <summary>
/// Pure-math tests for the chart family's scale/path helpers. Everything here
/// must be locale-independent — <see cref="ChartMath.F"/> guards the SVG output.
/// </summary>
public class ChartMathTests
{
    [Fact]
    public void NiceTicks_produces_clean_steps_for_0_to_97()
    {
        // Heckbert: raw range 97 → nice 100, step nice(100/4) → 20.
        var ticks = ChartMath.NiceTicks(0, 97);
        Assert.Equal(new double[] { 0, 20, 40, 60, 80, 100 }, ticks);
    }

    [Fact]
    public void NiceTicks_spans_negative_ranges()
    {
        var ticks = ChartMath.NiceTicks(-40, 60);
        Assert.Equal(new double[] { -40, -20, 0, 20, 40, 60 }, ticks);
    }

    [Fact]
    public void NiceTicks_handles_flat_series()
    {
        var ticks = ChartMath.NiceTicks(5, 5);
        Assert.True(ticks.Count >= 2);
        Assert.True(ticks[0] <= 5 && ticks[^1] >= 5);
    }

    [Fact]
    public void LinePath_is_invariant_and_well_formed()
    {
        var d = ChartMath.LinePath([(0, 12.5), (50, 37.25), (100, 0)]);
        Assert.StartsWith("M0,12.5", d);
        Assert.Contains("L50,37.25", d);
        Assert.DoesNotContain("12,5 ", d);
    }

    [Fact]
    public void SmoothPath_produces_cubic_beziers()
    {
        var d = ChartMath.SmoothPath([(0, 10), (33, 80), (66, 20), (100, 60)]);
        Assert.StartsWith("M0,10", d);
        Assert.Contains("C", d);
        // Curve must end exactly at the last data point.
        Assert.EndsWith("100,60", d.Replace(" ", ""));
    }

    [Fact]
    public void SmoothPath_with_two_points_degrades_to_line()
    {
        var d = ChartMath.SmoothPath([(0, 10), (100, 60)]);
        Assert.Equal(ChartMath.LinePath([(0, 10), (100, 60)]), d);
    }

    [Fact]
    public void StackTops_accumulates_and_clamps_negatives()
    {
        var tops = ChartMath.StackTops([new double[] { 3, -2 }, new double[] { 4, 5 }], 2);
        Assert.Equal(3, tops[0][0]);   // first series alone
        Assert.Equal(7, tops[1][0]);   // 3 + 4
        Assert.Equal(0, tops[0][1]);   // -2 clamped to 0
        Assert.Equal(5, tops[1][1]);   // 0 + 5
    }

    [Fact]
    public void DonutSegmentPath_contains_arcs_and_is_invariant()
    {
        var d = ChartMath.DonutSegmentPath(50, 50, 45, 29.25, 0, 120);
        Assert.StartsWith("M", d);
        Assert.Equal(2, d.Split('A').Length - 1);   // outer + inner arc
        Assert.Contains("Z", d);
        Assert.DoesNotContain(",2925", d);          // no comma decimals
    }
}
