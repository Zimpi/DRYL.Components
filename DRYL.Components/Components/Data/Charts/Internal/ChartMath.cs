using System.Globalization;

namespace DRYL.Components.Internal;

/// <summary>
/// Pure geometry/scale helpers for the chart family. Everything that touches
/// markup goes through <see cref="F"/> so SVG parses under any locale.
/// </summary>
internal static class ChartMath
{
    /// <summary>Invariant-culture number for SVG/CSS interpolation.</summary>
    public static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Heckbert "nice numbers" axis ticks covering [min, max].</summary>
    public static IReadOnlyList<double> NiceTicks(double min, double max, int maxTicks = 5)
    {
        if (max <= min) { min -= 1; max += 1; }
        var range = NiceNum(max - min, round: false);
        var d = NiceNum(range / (maxTicks - 1), round: true);
        var lo = Math.Floor(min / d) * d;
        var hi = Math.Ceiling(max / d) * d;
        var ticks = new List<double>();
        for (var t = lo; t <= hi + d * 0.5; t += d)
            ticks.Add(Math.Round(t, 10));   // kill fp drift (0.30000000004)
        return ticks;
    }

    private static double NiceNum(double range, bool round)
    {
        var exp = Math.Floor(Math.Log10(range));
        var frac = range / Math.Pow(10, exp);
        double nice = round
            ? frac < 1.5 ? 1 : frac < 3 ? 2 : frac < 7 ? 5 : 10
            : frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10;
        return nice * Math.Pow(10, exp);
    }

    public static string LinePath(IReadOnlyList<(double X, double Y)> pts)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pts.Count; i++)
            sb.Append(i == 0 ? "M" : " L").Append(F(pts[i].X)).Append(',').Append(F(pts[i].Y));
        return sb.ToString();
    }

    /// <summary>Catmull-Rom spline through the points, emitted as cubic Béziers.</summary>
    public static string SmoothPath(IReadOnlyList<(double X, double Y)> pts)
    {
        if (pts.Count < 3) return LinePath(pts);
        var sb = new System.Text.StringBuilder();
        sb.Append('M').Append(F(pts[0].X)).Append(',').Append(F(pts[0].Y));
        for (var i = 0; i < pts.Count - 1; i++)
        {
            var p0 = pts[Math.Max(i - 1, 0)];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[Math.Min(i + 2, pts.Count - 1)];
            var c1 = (X: p1.X + (p2.X - p0.X) / 6, Y: p1.Y + (p2.Y - p0.Y) / 6);
            var c2 = (X: p2.X - (p3.X - p1.X) / 6, Y: p2.Y - (p3.Y - p1.Y) / 6);
            sb.Append(" C").Append(F(c1.X)).Append(',').Append(F(c1.Y))
              .Append(' ').Append(F(c2.X)).Append(',').Append(F(c2.Y))
              .Append(' ').Append(F(p2.X)).Append(',').Append(F(p2.Y));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Cumulative stack tops. Negative values are clamped to 0 (stacked charts
    /// don't support negatives in v1). [s][i] = running total including series s.
    /// </summary>
    public static double[][] StackTops(IReadOnlyList<IReadOnlyList<double>> series, int count)
    {
        var tops = new double[series.Count][];
        var running = new double[count];
        for (var s = 0; s < series.Count; s++)
        {
            tops[s] = new double[count];
            for (var i = 0; i < count; i++)
            {
                var v = i < series[s].Count ? Math.Max(0, series[s][i]) : 0;
                running[i] += v;
                tops[s][i] = running[i];
            }
        }
        return tops;
    }

    /// <summary>
    /// Annular sector path. Angles in degrees, clockwise, 0 = 12 o'clock.
    /// </summary>
    public static string DonutSegmentPath(double cx, double cy, double rOuter, double rInner, double startDeg, double endDeg)
    {
        var large = endDeg - startDeg > 180 ? 1 : 0;
        var (x1, y1) = Polar(cx, cy, rOuter, startDeg);
        var (x2, y2) = Polar(cx, cy, rOuter, endDeg);
        var (x3, y3) = Polar(cx, cy, rInner, endDeg);
        var (x4, y4) = Polar(cx, cy, rInner, startDeg);
        return $"M{F(x1)},{F(y1)} A{F(rOuter)},{F(rOuter)} 0 {large} 1 {F(x2)},{F(y2)} " +
               $"L{F(x3)},{F(y3)} A{F(rInner)},{F(rInner)} 0 {large} 0 {F(x4)},{F(y4)} Z";
    }

    /// <summary>Point on a circle; angle clockwise from 12 o'clock.</summary>
    public static (double X, double Y) Polar(double cx, double cy, double r, double deg)
    {
        var rad = (deg - 90) * Math.PI / 180;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }
}
