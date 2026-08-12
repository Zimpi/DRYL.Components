# Chart Family Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `DrylLineChart`, `DrylBarChart`, `DrylAreaChart`, `DrylDonutChart` — dashboard-grade, pure-SVG/HTML, zero JS — per the approved spec `docs/superpowers/specs/2026-07-03-chart-family-design.md`.

**Architecture:** Shared code-behind bases (`DrylChartBase` extends the existing `DrylAiAware`; `DrylCartesianChartBase` adds series/scales/ticks). A shared internal `ChartFrame.razor` renders the cartesian skeleton (y-axis, grid, hover columns + tooltips, x-axis, legend) from a computed `CartesianLayout` record; each chart component only renders its marks. **Hybrid rendering:** line/area paths are SVG (`viewBox="0 0 100 100"` + `preserveAspectRatio="none"` + `vector-effect="non-scaling-stroke"`); bars, markers, grid, axes, legend and tooltips are HTML positioned with percentages — SVG text would distort under non-uniform scaling, HTML text never does, and tooltips get real glass tokens.

**Tech Stack:** Blazor (net8/9/10 multi-target), bUnit tests in `tests/DRYL.Components.Tests`, no new dependencies.

## Global Constraints

- Tokens only, no literal colors/radii/durations in component CSS (CLAUDE.md 2.1). Exception matching existing dryl.css idiom: px font-sizes (e.g. `11px`) appear throughout dryl.css and are allowed there.
- Motion: only `--dur-fast|med|slow` + `--ease-out|in-out|spring`; `prefers-reduced-motion` honored (CLAUDE.md 2.5/2.12).
- AI: inherit `DrylAiAware` (`Ai` param + `EffectiveAi` + scope resolution); default `AiState.None`; reuse `.ai-aura*` (CLAUDE.md 2.10).
- All numeric→markup interpolation via `CultureInfo.InvariantCulture`; display values (ticks/tooltips) intentionally culture-aware via `ValueFormat`.
- Conventions: merged `Class` param, `AdditionalAttributes` splat, bools plain adjectives, XML docs on class + every `[Parameter]` (CONVENTIONS.md).
- Validated palette (dataviz validator, dark, surface `#000000`, all six checks PASS, worst adjacent CVD ΔE 24.1):
  `--chart-1:#8b7cf8` `--chart-2:#0aa2b5` `--chart-3:#bd7a12` `--chart-4:#26a058` `--chart-5:#d6428e` `--chart-6:#5583e3`
- Series beyond slot 6 render `var(--fg-dim)`; never cycle, never repaint.
- Semantic colors (`--success/--warning/--danger`) are never series colors.
- Root element uses `role="group"` + aria-label (NOT `role="img"` — it would make the focusable hover zones presentational).
- Work on branch `feat/chart-family`.

---

### Task 1: Branch, chart tokens + CSS primitives in dryl.css

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css` (token block ~line 57 after `--info`; new section at end of file)

**Interfaces:**
- Produces: CSS custom props `--chart-1..6`; classes `.chart`, `.chart-body`, `.chart-yaxis`, `.chart-plot`, `.chart-svg`, `.chart-gridline`, `.chart-zeroline`, `.chart-xaxis`, `.chart-legend`, `.chart-legend-item`, `.chart-swatch`, `.chart-col`, `.chart-crosshair`, `.chart-tip`, `.chart-tip-flip`, `.chart-tip-row`, `.chart-line`, `.chart-area`, `.chart-marker`, `.chart-bars`, `.chart-band`, `.chart-bar-slot`, `.chart-bar`, `.chart-bar-neg`, `.chart-seg`, `.chart-seg-cap`, `.donut-box`, `.donut-slice`, `.donut-tip`, `.donut-center`; keyframes `chart-draw`, `chart-grow`, `chart-grow-neg`, `chart-fade`, `donut-in`.

- [ ] **Step 1: Create branch**

```bash
git checkout -b feat/chart-family
```

- [ ] **Step 2: Add tokens to `:root`** — directly under the `/* Semantic */` block (`--info: var(--accent-b);`):

```css
  /* Charts — categorical series palette (dark-validated: lightness band,
     chroma floor, CVD adjacent ΔE ≥ 12, contrast ≥ 3:1 on black).
     Fixed order, assigned in sequence, never cycled; series 7+ → --fg-dim. */
  --chart-1:       #8b7cf8;
  --chart-2:       #0aa2b5;
  --chart-3:       #bd7a12;
  --chart-4:       #26a058;
  --chart-5:       #d6428e;
  --chart-6:       #5583e3;
```

- [ ] **Step 3: Append the CHARTS section at the end of dryl.css**

```css
/* ============== CHARTS (DrylLineChart / DrylBarChart / DrylAreaChart / DrylDonutChart) ==============
   Hybrid rendering: marks are SVG (stretched viewBox, non-scaling strokes) or
   percent-positioned HTML; all text (axes, legend, tooltip) is HTML so it never
   distorts. One shared vocabulary for all four chart components. */

.chart {
  position: relative;
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
}

.chart-body {
  display: grid;
  grid-template-columns: auto 1fr;
  grid-template-rows: 1fr auto;
  height: var(--chart-h, 260px);
}

/* ── Y axis ── */
.chart-yaxis {
  position: relative;
  grid-row: 1;
  grid-column: 1;
  width: 3.4em;
  font-size: 11px;
  color: var(--fg-dim);
}
.chart-yaxis span {
  position: absolute;
  right: var(--sp-2);
  transform: translateY(-50%);
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
}

/* ── Plot area ── */
.chart-plot {
  position: relative;
  grid-row: 1;
  grid-column: 2;
  min-width: 0;
}
.chart-gridline {
  position: absolute;
  left: 0; right: 0;
  height: 1px;
  background: var(--line);
  pointer-events: none;
}
.chart-zeroline { background: var(--line-strong); }

.chart-svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  overflow: visible;
  pointer-events: none;
}

/* ── X axis ── */
.chart-xaxis {
  position: relative;
  grid-row: 2;
  grid-column: 2;
  height: 1.9em;
  font-size: 11px;
  color: var(--fg-dim);
}
.chart-xaxis span {
  position: absolute;
  top: var(--sp-2);
  transform: translateX(-50%);
  white-space: nowrap;
}

/* ── Legend ── */
.chart-legend {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-2) var(--sp-5);
  font-size: 12px;
  color: var(--fg-muted);
}
.chart-legend-item {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
}
.chart-swatch {
  width: 10px;
  height: 10px;
  border-radius: var(--r-xs);
  flex: none;
}

/* ── Hover columns, crosshair, tooltip (pure CSS — zero JS, zero roundtrips) ── */
.chart-col {
  position: absolute;
  top: 0; bottom: 0;
  border-radius: var(--r-xs);
}
.chart-col:focus-visible {
  outline: none;
  box-shadow: 0 0 0 1px var(--accent-line);
}
.chart-crosshair {
  position: absolute;
  left: 50%; top: 0; bottom: 0;
  width: 1px;
  background: var(--line-strong);
  opacity: 0;
  transition: opacity var(--dur-fast) var(--ease-out);
  pointer-events: none;
}
.chart-col:hover .chart-crosshair,
.chart-col:focus-visible .chart-crosshair { opacity: 1; }

.chart-tip {
  position: absolute;
  top: var(--sp-2);
  left: calc(50% + var(--sp-3));
  min-width: 120px;
  max-width: 240px;
  padding: var(--sp-3);
  background: rgba(10, 10, 14, 0.92);
  border: 1px solid var(--line-strong);
  border-radius: var(--r-sm);
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  box-shadow: var(--shadow-2, 0 8px 32px rgba(0,0,0,0.5));
  font-size: 12px;
  color: var(--fg);
  opacity: 0;
  transform: translateY(4px);
  transition: opacity var(--dur-fast) var(--ease-out),
              transform var(--dur-fast) var(--ease-out);
  pointer-events: none;
  z-index: 3;
}
.chart-tip-flip {
  left: auto;
  right: calc(50% + var(--sp-3));
}
.chart-col:hover .chart-tip,
.chart-col:focus-visible .chart-tip {
  opacity: 1;
  transform: translateY(0);
}
.chart-tip-title {
  color: var(--fg-dim);
  font-size: 11px;
  margin-bottom: var(--sp-2);
  white-space: nowrap;
}
.chart-tip-row {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  white-space: nowrap;
}
.chart-tip-row + .chart-tip-row { margin-top: var(--sp-1); }
.chart-tip-row .chart-swatch { width: 8px; height: 8px; }
.chart-tip-name { color: var(--fg-muted); margin-right: auto; }
.chart-tip-value {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  margin-left: var(--sp-4);
}

/* ── Line / area marks ── */
.chart-line {
  fill: none;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
  stroke-dasharray: 1;
  stroke-dashoffset: 0;
  animation: chart-draw var(--dur-slow) var(--ease-out) both;
}
.chart-area {
  stroke: none;
  animation: chart-fade var(--dur-slow) var(--ease-out) both;
}
.chart-marker {
  position: absolute;
  width: 8px;
  height: 8px;
  margin: -4px 0 0 -4px;
  border-radius: var(--r-pill);
  box-shadow: 0 0 0 2px var(--bg);   /* surface ring — legibility over lines */
  pointer-events: none;
  transition: transform var(--dur-fast) var(--ease-spring);
  animation: chart-fade var(--dur-med) var(--ease-out) both;
}
.chart-col:hover ~ .chart-svg .chart-line { /* no-op guard: markers handle hover */ }

/* ── Bars ── */
.chart-bars { position: absolute; inset: 0; }
.chart-band {
  position: absolute;
  top: 0; bottom: 0;
  display: flex;
  justify-content: center;
  align-items: stretch;
  gap: 2px;                 /* surface gap between adjacent bars */
  padding: 0 var(--sp-1);
}
.chart-bar-slot {
  position: relative;
  flex: 1 1 0;
  max-width: 24px;          /* bars never fill the slot */
  min-width: 2px;
}
.chart-bar {
  position: absolute;
  left: 0; right: 0;
  border-radius: 4px 4px 0 0;   /* rounded data-end, square baseline */
  transform-origin: bottom;
  animation: chart-grow var(--dur-slow) var(--ease-spring) both;
  animation-delay: calc(var(--i, 0) * 30ms);
  transition: filter var(--dur-fast) var(--ease-out);
}
.chart-bar-neg {
  border-radius: 0 0 4px 4px;
  transform-origin: top;
  animation-name: chart-grow-neg;
}
.chart-col:hover ~ .chart-bars .chart-bar { /* per-band hover handled below */ }
.chart-band:hover .chart-bar { filter: brightness(1.15); }

/* Stacked segments — 2px surface gap between segments, cap segment rounded */
.chart-seg {
  position: absolute;
  left: 0; right: 0;
  transform-origin: bottom;
  animation: chart-grow var(--dur-slow) var(--ease-spring) both;
  animation-delay: calc(var(--i, 0) * 30ms);
}
.chart-seg-cap { border-radius: 4px 4px 0 0; }

/* ── Donut ── */
.donut-box {
  position: relative;
  height: var(--chart-h, 260px);
  display: flex;
  justify-content: center;
}
.donut-slice {
  position: absolute;
  inset: 0;
  display: flex;
  justify-content: center;
  pointer-events: none;
}
.donut-slice svg {
  height: 100%;
  aspect-ratio: 1;
  overflow: visible;
  pointer-events: none;
}
.donut-slice path {
  pointer-events: auto;
  cursor: default;
  transition: transform var(--dur-fast) var(--ease-spring);
  animation: donut-in var(--dur-med) var(--ease-out) both;
  animation-delay: calc(var(--i, 0) * 40ms);
  outline: none;
}
.donut-slice:hover path,
.donut-slice:focus-within path {
  transform: translate(var(--dx, 0), var(--dy, 0));
}
.donut-slice:focus-within path {
  stroke: var(--accent-line);
  stroke-width: 1;
}
.donut-tip {
  top: var(--tip-top, var(--sp-2));
  left: var(--tip-left, auto);
}
.donut-slice:hover .donut-tip,
.donut-slice:focus-within .donut-tip {
  opacity: 1;
  transform: translateY(0);
}
.donut-center {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  pointer-events: none;
}
.donut-center > * { pointer-events: auto; }

/* ── Motion ── */
@keyframes chart-draw { from { stroke-dashoffset: 1; } to { stroke-dashoffset: 0; } }
@keyframes chart-grow { from { transform: scaleY(0); } to { transform: scaleY(1); } }
@keyframes chart-grow-neg { from { transform: scaleY(0); } to { transform: scaleY(1); } }
@keyframes chart-fade { from { opacity: 0; } to { opacity: 1; } }
@keyframes donut-in {
  from { opacity: 0; transform: scale(0.96); }
  to   { opacity: 1; transform: scale(1); }
}

@media (prefers-reduced-motion: reduce) {
  .chart-line, .chart-area, .chart-marker,
  .chart-bar, .chart-seg, .donut-slice path {
    animation: none;
  }
  .chart-tip, .chart-crosshair, .chart-marker,
  .chart-bar, .donut-slice path { transition: none; }
}
```

Note: `.chart-tip` background `rgba(10, 10, 14, 0.92)` — check dryl.css for an existing tooltip/popover surface token first (grep `.tooltip`/`.popover`); if one exists, reuse its background exactly instead of this literal.

- [ ] **Step 4: Build to confirm nothing broke**

Run: `dotnet build DRYL.slnx`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css
git commit -m "feat(charts): add validated --chart-1..6 tokens and chart CSS primitives"
```

---

### Task 2: ChartMath — scales, ticks, paths, stacking, arcs (TDD)

**Files:**
- Create: `DRYL.Components/Components/Data/Charts/Internal/ChartMath.cs`
- Test: `tests/DRYL.Components.Tests/ChartMathTests.cs`

**Interfaces:**
- Produces (all `internal static` on `DRYL.Components.Internal.ChartMath`):
  - `string F(double v)` — invariant `0.###` format
  - `IReadOnlyList<double> NiceTicks(double min, double max, int maxTicks = 5)`
  - `string LinePath(IReadOnlyList<(double X, double Y)> pts)` — `M x,y L x,y …`
  - `string SmoothPath(IReadOnlyList<(double X, double Y)> pts)` — Catmull-Rom → cubic Bézier
  - `double[][] StackTops(IReadOnlyList<IReadOnlyList<double>> series, int count)` — cumulative sums per index, negatives clamped to 0; `[seriesIndex][pointIndex]` = running total INCLUDING that series
  - `string DonutSegmentPath(double cx, double cy, double rOuter, double rInner, double startDeg, double endDeg)` — annular sector, angles clockwise from 12 o'clock
- Requires `InternalsVisibleTo` for the test project — check `DRYL.Components.csproj` for an existing `InternalsVisibleTo`; add one for `DRYL.Components.Tests` if missing.

- [ ] **Step 1: Write failing tests**

```csharp
using DRYL.Components.Internal;

namespace DRYL.Components.Tests;

public class ChartMathTests
{
    [Fact]
    public void NiceTicks_produces_clean_steps_for_0_to_97()
    {
        var ticks = ChartMath.NiceTicks(0, 97);
        Assert.Equal(new double[] { 0, 25, 50, 75, 100 }, ticks);
    }

    [Fact]
    public void NiceTicks_spans_negative_ranges()
    {
        var ticks = ChartMath.NiceTicks(-40, 60);
        Assert.Equal(-50, ticks[0]);
        Assert.Equal(75, ticks[^1]);
        Assert.Contains(0, ticks);
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
        Assert.DoesNotContain("12,5", d);
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
```

- [ ] **Step 2: Run tests — expect FAIL** (`ChartMath` doesn't exist)

Run: `dotnet test tests/DRYL.Components.Tests --filter ChartMathTests`

- [ ] **Step 3: Implement `ChartMath`**

```csharp
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
```

- [ ] **Step 4: Run tests — expect PASS**

Run: `dotnet test tests/DRYL.Components.Tests --filter ChartMathTests`

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Data/Charts/Internal/ChartMath.cs tests/DRYL.Components.Tests/ChartMathTests.cs
git commit -m "feat(charts): ChartMath — nice ticks, line/smooth paths, stacking, donut arcs"
```

---

### Task 3: Vertical slice — records, bases, ChartFrame, DrylLineChart (TDD)

**Files:**
- Create: `DRYL.Components/Components/Data/Charts/ChartSeries.cs`
- Create: `DRYL.Components/Components/Data/Charts/ChartSegment.cs`
- Create: `DRYL.Components/Components/Data/Charts/DrylChartBase.cs`
- Create: `DRYL.Components/Components/Data/Charts/DrylCartesianChartBase.cs`
- Create: `DRYL.Components/Components/Data/Charts/Internal/CartesianLayout.cs`
- Create: `DRYL.Components/Components/Data/Charts/Internal/ChartFrame.razor`
- Create: `DRYL.Components/Components/Data/Charts/DrylLineChart.razor`
- Test: `tests/DRYL.Components.Tests/DrylLineChartTests.cs`

**Interfaces:**
- Consumes: `ChartMath` (Task 2), `DrylAiAware` (existing), `.chart-*` CSS (Task 1).
- Produces:
  - `public sealed record ChartSeries(string Name, IReadOnlyList<double> Data) { public int? ColorSlot { get; init; } }`
  - `public sealed record ChartSegment(string Label, double Value) { public int? ColorSlot { get; init; } }`
  - `DrylChartBase : DrylAiAware` — params `Height` (int, 260), `ShowLegend` (bool?), `ValueFormat` (string?), `AriaLabel` (string?), `Class` (string?), `AdditionalAttributes`; protected `FormatValue(double)`, `SlotColor(int? slot, int position)` (both 1-based slot / 0-based position; > 6 → `var(--fg-dim)`), `RootCss(string baseClass)` (merges ai-aura classes + `Class`), `GenTick` (int, re-key for wash), `Inv(double)` (→ `ChartMath.F`).
  - `DrylCartesianChartBase : DrylChartBase` — params `Series`, `Labels`, `ShowXAxis`/`ShowYAxis`/`ShowGridLines` (true), `YMin`/`YMax` (double?); protected `HasData`, `PointCount`, `Min`/`Max`/`Ticks` (computed in `OnParametersSet` after range calc), `XPct(int i)`, `YPct(double v)`, `SeriesColor(int i)`, `LegendVisible`, `BuildLayout()` → `CartesianLayout`; `virtual bool Banded => false` (bar override: band centers + zero-included range), `virtual IEnumerable<double> RangeValues()` (bar-stacked override).
  - Internal records: `CartesianLayout(int Height, IReadOnlyList<AxisTick> Ticks, double? ZeroPct, IReadOnlyList<AxisTick> XLabels, IReadOnlyList<HoverColumn> Columns, IReadOnlyList<LegendItem> Legend, bool ShowXAxis, bool ShowYAxis, bool ShowGrid, bool ShowLegend)`; `AxisTick(double Pct, string Label)`; `HoverColumn(double LeftPct, double WidthPct, string Aria, bool Flip, string Title, IReadOnlyList<TooltipRow> Rows)`; `TooltipRow(string Color, string Name, string Value)`; `LegendItem(string Color, string Name)`.
  - `ChartFrame` (internal component) — params `Layout` (CartesianLayout), `RootClass` (string), `AriaLabel` (string), `Marks` (RenderFragment), `Aura` (RenderFragment?), `AdditionalAttributes`.
  - `DrylLineChart : DrylCartesianChartBase` — extra params `Smooth` (bool), `ShowMarkers` (bool).

- [ ] **Step 1: Write failing bUnit tests**

```csharp
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
            // 3 points → x positions 0 / 50 / 100, y values force decimals.
            var cut = Render<DrylLineChart>(ps => ps.Add(p => p.Series,
                new[] { new ChartSeries("S", new double[] { 1, 2, 4 }) }));
            var d = cut.Find("path.chart-line").GetAttribute("d")!;
            Assert.DoesNotMatch(@"\d,\d", d.Replace(" ", "_"));
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }
}
```

Note on the de-DE test: path commands legitimately contain `x,y` commas — the regex above would false-positive. Use this assertion instead: split `d` on `M`, `L`, `C` and whitespace; every token must then be exactly `number,number` where each number parses with `CultureInfo.InvariantCulture` and the token contains exactly one comma:

```csharp
var tokens = d.Split(['M', 'L', 'C', ' '], StringSplitOptions.RemoveEmptyEntries);
Assert.All(tokens, t =>
{
    var parts = t.Split(',');
    Assert.Equal(2, parts.Length);
    Assert.All(parts, p => double.Parse(p, CultureInfo.InvariantCulture));
});
```

- [ ] **Step 2: Run tests — expect FAIL** (types don't exist)

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylLineChartTests`

- [ ] **Step 3: Create the records**

`ChartSeries.cs`:

```csharp
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
```

`ChartSegment.cs`:

```csharp
namespace DRYL.Components;

/// <summary>One segment of a <see cref="DrylDonutChart"/>.</summary>
/// <param name="Label">Segment name shown in the legend and tooltip.</param>
/// <param name="Value">Segment value; share of the total drives the sweep angle.</param>
public sealed record ChartSegment(string Label, double Value)
{
    /// <summary>Optional fixed palette slot (1–6). Defaults to the segment's position.</summary>
    public int? ColorSlot { get; init; }
}
```

- [ ] **Step 4: Create `DrylChartBase.cs`**

```csharp
namespace DRYL.Components;

/// <summary>
/// Shared base for the chart family: sizing, legend policy, value formatting,
/// palette slots and the AI aura lifecycle. Series-agnostic — cartesian charts
/// derive via <see cref="DrylCartesianChartBase"/>, <see cref="DrylDonutChart"/>
/// derives directly.
/// </summary>
public abstract class DrylChartBase : DrylAiAware
{
    /// <summary>Chart height in pixels. Width always fills the container.</summary>
    [Parameter] public int Height { get; set; } = 260;

    /// <summary>
    /// Legend visibility. Default (null) is automatic: shown for two or more
    /// series, hidden for one (the title/context already names a single series).
    /// </summary>
    [Parameter] public bool? ShowLegend { get; set; }

    /// <summary>
    /// .NET format string for axis ticks and tooltip values (e.g. "N0", "C0").
    /// Display values are intentionally culture-aware.
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>Accessible summary label. Defaults to an auto-generated description.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Extra CSS class(es) merged onto the chart's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Pass-through HTML attributes on the chart root.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Re-key counter for the one-shot Generated wash.</summary>
    protected int GenTick { get; private set; }

    private AiState _prevAi = AiState.None;

    protected override void OnParametersSet()
    {
        if (EffectiveAi == AiState.Generated && _prevAi != AiState.Generated) GenTick++;
        _prevAi = EffectiveAi;
    }

    /// <summary>Invariant-culture number for SVG/CSS interpolation.</summary>
    protected static string Inv(double v) => Internal.ChartMath.F(v);

    /// <summary>Culture-aware display value for ticks and tooltips.</summary>
    protected string FormatValue(double v) =>
        ValueFormat is null ? v.ToString("0.##") : v.ToString(ValueFormat);

    /// <summary>
    /// Palette color for a series/segment. <paramref name="slot"/> is the 1-based
    /// pinned slot (wins when set); <paramref name="position"/> is the 0-based
    /// list position. Anything beyond slot 6 renders muted — never cycle colors.
    /// </summary>
    protected static string SlotColor(int? slot, int position)
    {
        var s = slot ?? position + 1;
        return s is >= 1 and <= 6 ? $"var(--chart-{s})" : "var(--fg-dim)";
    }

    /// <summary>Root class string: base + AI aura classes + merged Class.</summary>
    protected string RootCss(string baseClass)
    {
        var classes = new List<string> { baseClass };
        if (EffectiveAi != AiState.None) classes.Add("ai-aura");
        switch (EffectiveAi)
        {
            case AiState.Thinking:  classes.Add("ai-thinking");  break;
            case AiState.Streaming: classes.Add("ai-streaming"); break;
            case AiState.Generated: classes.Add("ai-generated"); break;
        }
        if (!string.IsNullOrWhiteSpace(Class)) classes.Add(Class!);
        return string.Join(' ', classes);
    }
}
```

(Add `using Microsoft.AspNetCore.Components;` — check `_Imports.razor` conventions; plain `.cs` files need explicit usings.)

- [ ] **Step 5: Create `Internal/CartesianLayout.cs`**

```csharp
namespace DRYL.Components.Internal;

internal sealed record AxisTick(double Pct, string Label);
internal sealed record TooltipRow(string Color, string Name, string Value);
internal sealed record LegendItem(string Color, string Name);

internal sealed record HoverColumn(
    double LeftPct, double WidthPct, string Aria, bool Flip,
    string Title, IReadOnlyList<TooltipRow> Rows);

/// <summary>Everything ChartFrame needs to render the cartesian skeleton.</summary>
internal sealed record CartesianLayout(
    int Height,
    IReadOnlyList<AxisTick> Ticks,
    double? ZeroPct,
    IReadOnlyList<AxisTick> XLabels,
    IReadOnlyList<HoverColumn> Columns,
    IReadOnlyList<LegendItem> Legend,
    bool ShowXAxis, bool ShowYAxis, bool ShowGrid, bool ShowLegend);
```

- [ ] **Step 6: Create `DrylCartesianChartBase.cs`**

```csharp
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

    /// <summary>Color for series i (honors ColorSlot; > 6 → muted).</summary>
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
```

- [ ] **Step 7: Create `Internal/ChartFrame.razor`**

```razor
@namespace DRYL.Components.Internal
@using DRYL.Components

@*  Internal — the shared cartesian chart skeleton. Renders axes, gridlines,
    hover columns with pure-CSS tooltips, x labels and the legend around the
    chart-specific Marks fragment. Not part of the public API. *@

<div class="@RootClass"
     style="--chart-h:@(Layout.Height)px"
     role="group"
     aria-label="@AriaLabel"
     @attributes="AdditionalAttributes">
    @Aura
    <div class="chart-body">
        @if (Layout.ShowYAxis)
        {
            <div class="chart-yaxis" aria-hidden="true">
                @foreach (var t in Layout.Ticks)
                {
                    <span style="top:@(ChartMath.F(t.Pct))%">@t.Label</span>
                }
            </div>
        }
        <div class="chart-plot">
            @if (Layout.ShowGrid)
            {
                @foreach (var t in Layout.Ticks)
                {
                    <div class="chart-gridline" style="top:@(ChartMath.F(t.Pct))%"></div>
                }
            }
            @if (Layout.ZeroPct is { } zero)
            {
                <div class="chart-gridline chart-zeroline" style="top:@(ChartMath.F(zero))%"></div>
            }
            @Marks
            @foreach (var col in Layout.Columns)
            {
                <div class="chart-col"
                     tabindex="0"
                     aria-label="@col.Aria"
                     style="left:@(ChartMath.F(col.LeftPct))%;width:@(ChartMath.F(col.WidthPct))%">
                    <div class="chart-crosshair" aria-hidden="true"></div>
                    <div class="chart-tip @(col.Flip ? "chart-tip-flip" : null)" aria-hidden="true">
                        <div class="chart-tip-title">@col.Title</div>
                        @foreach (var row in col.Rows)
                        {
                            <div class="chart-tip-row">
                                <span class="chart-swatch" style="background:@row.Color"></span>
                                <span class="chart-tip-name">@row.Name</span>
                                <span class="chart-tip-value">@row.Value</span>
                            </div>
                        }
                    </div>
                </div>
            }
        </div>
        @if (Layout.ShowXAxis)
        {
            <div class="chart-xaxis" aria-hidden="true">
                @foreach (var l in Layout.XLabels)
                {
                    <span style="left:@(ChartMath.F(l.Pct))%">@l.Label</span>
                }
            </div>
        }
    </div>
    @if (Layout.ShowLegend)
    {
        <div class="chart-legend">
            @foreach (var item in Layout.Legend)
            {
                <span class="chart-legend-item">
                    <span class="chart-swatch" style="background:@item.Color"></span>@item.Name
                </span>
            }
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public CartesianLayout Layout { get; set; } = default!;
    [Parameter, EditorRequired] public string RootClass { get; set; } = "chart";
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public RenderFragment? Marks { get; set; }
    [Parameter] public RenderFragment? Aura { get; set; }
    [Parameter] public IDictionary<string, object>? AdditionalAttributes { get; set; }
}
```

- [ ] **Step 8: Create `DrylLineChart.razor`**

```razor
@namespace DRYL.Components
@inherits DrylCartesianChartBase
@using DRYL.Components.Internal

@*  ─────────────────────────────────────────────────────────
    DrylLineChart — multi-series line chart. Pure SVG/HTML, zero JS.

    Usage:
      <DrylLineChart Labels="@months"
                     Series="@(new[]{ new ChartSeries("Umsatz", rev),
                                      new ChartSeries("Kosten", cost) })"
                     ValueFormat="C0" Smooth ShowMarkers />
    ───────────────────────────────────────────────────────── *@

@if (HasData)
{
    <ChartFrame Layout="@BuildLayout()"
                RootClass="@RootCss("chart chart-kind-line")"
                AriaLabel="@SummaryLabel"
                AdditionalAttributes="@AdditionalAttributes">
        <Aura>
            @if (EffectiveAi != AiState.None)
            {
                <div class="ai-aura-ring"></div>
                <div class="ai-aura-glow"></div>
                @if (EffectiveAi == AiState.Generated)
                {
                    <div class="ai-aura-wash" @key="GenTick"></div>
                }
            }
        </Aura>
        <Marks>
            <svg class="chart-svg" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
                @for (var s = 0; s < Series!.Count; s++)
                {
                    <path class="chart-line"
                          d="@PathFor(s)"
                          pathLength="1"
                          vector-effect="non-scaling-stroke"
                          style="stroke:@SeriesColor(s)" />
                }
            </svg>
            @if (ShowMarkers)
            {
                @for (var s = 0; s < Series!.Count; s++)
                {
                    var color = SeriesColor(s);
                    for (var i = 0; i < Series[s].Data.Count; i++)
                    {
                        <span class="chart-marker" aria-hidden="true"
                              style="left:@(Inv(XPct(i)))%;top:@(Inv(YPct(Series[s].Data[i])))%;background:@color"></span>
                    }
                }
            }
        </Marks>
    </ChartFrame>
}

@code {
    /// <summary>Smooth the line with a Catmull-Rom spline instead of straight segments.</summary>
    [Parameter] public bool Smooth { get; set; }

    /// <summary>Mark every data point with a dot (8px, surface ring).</summary>
    [Parameter] public bool ShowMarkers { get; set; }

    private string PathFor(int s)
    {
        var data = Series![s].Data;
        var pts = new List<(double X, double Y)>(data.Count);
        for (var i = 0; i < data.Count; i++)
            pts.Add((XPct(i), YPct(data[i])));
        return Smooth ? ChartMath.SmoothPath(pts) : ChartMath.LinePath(pts);
    }
}
```

- [ ] **Step 9: Run tests — expect PASS**

Run: `dotnet test tests/DRYL.Components.Tests --filter "DrylLineChartTests|ChartMathTests"`
Expected: all PASS. If `ChartFrame`/`CartesianLayout` aren't visible to the razor compiler, add `@using DRYL.Components.Internal` where needed (already in the .razor files above).

- [ ] **Step 10: Commit**

```bash
git add DRYL.Components/Components/Data/Charts tests/DRYL.Components.Tests/DrylLineChartTests.cs
git commit -m "feat(charts): DrylLineChart + shared chart bases and frame"
```

---

### Task 4: DrylAreaChart

**Files:**
- Create: `DRYL.Components/Components/Data/Charts/DrylAreaChart.razor`
- Test: `tests/DRYL.Components.Tests/DrylAreaChartTests.cs`

**Interfaces:**
- Consumes: everything from Task 3.
- Produces: `DrylAreaChart : DrylCartesianChartBase` with `Smooth` (bool).

- [ ] **Step 1: Write failing tests**

```csharp
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
        // Range 0..4 → zero line sits at 100% (bottom); area path must end with Z.
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
```

- [ ] **Step 2: Run — expect FAIL**, then implement `DrylAreaChart.razor`:

```razor
@namespace DRYL.Components
@inherits DrylCartesianChartBase
@using DRYL.Components.Internal

@*  ─────────────────────────────────────────────────────────
    DrylAreaChart — line chart with a soft same-hue fill down to the
    zero baseline. Pure SVG/HTML, zero JS.

    Usage:
      <DrylAreaChart Labels="@months" Series="@series" Smooth />
    ───────────────────────────────────────────────────────── *@

@if (HasData)
{
    <ChartFrame Layout="@BuildLayout()"
                RootClass="@RootCss("chart chart-kind-area")"
                AriaLabel="@SummaryLabel"
                AdditionalAttributes="@AdditionalAttributes">
        <Aura>
            @if (EffectiveAi != AiState.None)
            {
                <div class="ai-aura-ring"></div>
                <div class="ai-aura-glow"></div>
                @if (EffectiveAi == AiState.Generated)
                {
                    <div class="ai-aura-wash" @key="GenTick"></div>
                }
            }
        </Aura>
        <Marks>
            <svg class="chart-svg" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
                <defs>
                    @for (var s = 0; s < Series!.Count; s++)
                    {
                        <linearGradient id="@GradId(s)" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0" stop-color="@SeriesColor(s)" stop-opacity="0.12" />
                            <stop offset="1" stop-color="@SeriesColor(s)" stop-opacity="0.02" />
                        </linearGradient>
                    }
                </defs>
                @for (var s = 0; s < Series!.Count; s++)
                {
                    <path class="chart-area" fill="url(#@GradId(s))" d="@AreaPathFor(s)" />
                    <path class="chart-line"
                          d="@LinePathFor(s)"
                          pathLength="1"
                          vector-effect="non-scaling-stroke"
                          style="stroke:@SeriesColor(s)" />
                }
            </svg>
        </Marks>
    </ChartFrame>
}

@code {
    /// <summary>Smooth the line with a Catmull-Rom spline instead of straight segments.</summary>
    [Parameter] public bool Smooth { get; set; }

    private readonly string _gid = $"chart-area-{Guid.NewGuid():N}";
    private string GradId(int s) => $"{_gid}-{s}";

    private List<(double X, double Y)> Points(int s)
    {
        var data = Series![s].Data;
        var pts = new List<(double X, double Y)>(data.Count);
        for (var i = 0; i < data.Count; i++)
            pts.Add((XPct(i), YPct(data[i])));
        return pts;
    }

    private string LinePathFor(int s)
    {
        var pts = Points(s);
        return Smooth ? ChartMath.SmoothPath(pts) : ChartMath.LinePath(pts);
    }

    private string AreaPathFor(int s)
    {
        var pts = Points(s);
        var baseline = YPct(Math.Clamp(0, Min, Max));
        var d = Smooth ? ChartMath.SmoothPath(pts) : ChartMath.LinePath(pts);
        return $"{d} L{Inv(pts[^1].X)},{Inv(baseline)} L{Inv(pts[0].X)},{Inv(baseline)} Z";
    }
}
```

- [ ] **Step 3: Run tests — expect PASS**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAreaChartTests`

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/Components/Data/Charts/DrylAreaChart.razor tests/DRYL.Components.Tests/DrylAreaChartTests.cs
git commit -m "feat(charts): DrylAreaChart with same-hue gradient fill"
```

---

### Task 5: DrylBarChart (grouped + stacked)

**Files:**
- Create: `DRYL.Components/Components/Data/Charts/DrylBarChart.razor`
- Test: `tests/DRYL.Components.Tests/DrylBarChartTests.cs`

**Interfaces:**
- Consumes: Task 3 bases; `ChartMath.StackTops`.
- Produces: `DrylBarChart : DrylCartesianChartBase` with `Stacked` (bool); overrides `Banded => true` and `RangeValues()` (stack tops when stacked).

- [ ] **Step 1: Write failing tests**

```csharp
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
        // Totals 5 and 9 → topmost tick must be >= 9 (nice → 10).
        var cut = Render<DrylBarChart>(ps => ps
            .Add(p => p.Series, TwoSeries)
            .Add(p => p.Stacked, true));
        Assert.Contains("10", cut.Find(".chart-yaxis").TextContent);
    }
}
```

- [ ] **Step 2: Run — expect FAIL**, then implement `DrylBarChart.razor`:

```razor
@namespace DRYL.Components
@inherits DrylCartesianChartBase
@using DRYL.Components.Internal

@*  ─────────────────────────────────────────────────────────
    DrylBarChart — grouped or stacked column chart. Bars are percent-
    positioned HTML (≤ 24px wide, rounded data-end, 2px surface gaps),
    so they animate and hover with plain CSS. Zero JS.

    Usage:
      <DrylBarChart Labels="@months" Series="@series" />
      <DrylBarChart Labels="@months" Series="@series" Stacked />

    Stacked mode clamps negative values to 0 (unsupported in v1).
    ───────────────────────────────────────────────────────── *@

@if (HasData)
{
    <ChartFrame Layout="@BuildLayout()"
                RootClass="@RootCss("chart chart-kind-bar")"
                AriaLabel="@SummaryLabel"
                AdditionalAttributes="@AdditionalAttributes">
        <Aura>
            @if (EffectiveAi != AiState.None)
            {
                <div class="ai-aura-ring"></div>
                <div class="ai-aura-glow"></div>
                @if (EffectiveAi == AiState.Generated)
                {
                    <div class="ai-aura-wash" @key="GenTick"></div>
                }
            }
        </Aura>
        <Marks>
            <div class="chart-bars" aria-hidden="true">
                @{ var n = PointCount; var bandW = 100.0 / n; }
                @for (var i = 0; i < n; i++)
                {
                    var idx = i;
                    <div class="chart-band" style="left:@(Inv(idx * bandW))%;width:@(Inv(bandW))%">
                        @if (Stacked)
                        {
                            <div class="chart-bar-slot">
                                @{ var capDone = false; }
                                @for (var s = Series!.Count - 1; s >= 0; s--)
                                {
                                    var top = _stackTops![s][idx];
                                    var bottom = top - StackValue(s, idx);
                                    if (top <= bottom) { continue; }
                                    var cap = !capDone; capDone = true;
                                    <div class="chart-seg @(cap ? "chart-seg-cap" : null)"
                                         style="top:@(Inv(YPct(top)))%;height:calc(@(Inv(YPct(bottom) - YPct(top)))% - 2px);background:@SeriesColor(s);--i:@idx"></div>
                                }
                            </div>
                        }
                        else
                        {
                            for (var s = 0; s < Series!.Count; s++)
                            {
                                var v = idx < Series[s].Data.Count ? Series[s].Data[idx] : 0;
                                var zero = YPct(Math.Clamp(0, Min, Max));
                                var y = YPct(v);
                                var neg = v < 0;
                                var top = neg ? zero : y;
                                var h = Math.Abs(zero - y);
                                <div class="chart-bar-slot">
                                    <div class="chart-bar @(neg ? "chart-bar-neg" : null)"
                                         style="top:@(Inv(top))%;height:@(Inv(h))%;background:@SeriesColor(s);--i:@idx"></div>
                                </div>
                            }
                        }
                    </div>
                }
            </div>
        </Marks>
    </ChartFrame>
}

@code {
    /// <summary>Stack the series on top of each other instead of grouping side by side.</summary>
    [Parameter] public bool Stacked { get; set; }

    protected override bool Banded => true;

    private double[][]? _stackTops;

    private double StackValue(int s, int i) =>
        i < Series![s].Data.Count ? Math.Max(0, Series[s].Data[i]) : 0;

    protected override IEnumerable<double> RangeValues()
    {
        if (!Stacked) return base.RangeValues();
        _stackTops = ChartMath.StackTops(Series!.Select(s => s.Data).ToList(), PointCount);
        return _stackTops[^1];
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();   // triggers RangeValues() → fills _stackTops
        if (Stacked && _stackTops is null && HasData)
            _stackTops = ChartMath.StackTops(Series!.Select(s => s.Data).ToList(), PointCount);
    }
}
```

- [ ] **Step 3: Run tests — expect PASS**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylBarChartTests`

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/Components/Data/Charts/DrylBarChart.razor tests/DRYL.Components.Tests/DrylBarChartTests.cs
git commit -m "feat(charts): DrylBarChart — grouped and stacked columns"
```

---

### Task 6: DrylDonutChart (donut / pie, center slot)

**Files:**
- Create: `DRYL.Components/Components/Data/Charts/DrylDonutChart.razor`
- Test: `tests/DRYL.Components.Tests/DrylDonutChartTests.cs`

**Interfaces:**
- Consumes: `DrylChartBase`, `ChartMath.DonutSegmentPath`/`Polar`, `.donut-*` CSS.
- Produces: `DrylDonutChart : DrylChartBase` — `Segments` (`IReadOnlyList<ChartSegment>?`), `InnerRadius` (double, 0.65; 0 = pie), `CenterContent` (RenderFragment?).

Key mechanics (the CSS hover trick): each segment renders as its own absolutely-stacked square SVG inside a `.donut-slice` wrapper whose `pointer-events` is `none`; only the `path` accepts pointer events. Hovering the path makes ALL its ancestors match `:hover`, so `.donut-slice:hover .donut-tip` shows the right tooltip — exact wedge hit-testing with zero JS. Keyboard: the path carries `tabindex="0"` + `role="img"` + `aria-label`; `.donut-slice:focus-within` mirrors hover.

- [ ] **Step 1: Write failing tests**

```csharp
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
        // Inner arc collapses to the centre: no second arc radius > 0.
        var d = cut.Find(".donut-slice path").GetAttribute("d")!;
        Assert.Contains("A0,0", d);
    }
}
```

- [ ] **Step 2: Run — expect FAIL**, then implement `DrylDonutChart.razor`:

```razor
@namespace DRYL.Components
@inherits DrylChartBase
@using DRYL.Components.Internal

@*  ─────────────────────────────────────────────────────────
    DrylDonutChart — proportional donut / pie. Each segment is its own
    stacked SVG inside a .donut-slice wrapper; only the path takes pointer
    events, so :hover / :focus-within on the wrapper drives the pure-CSS
    tooltip and the outward lift. Zero JS.

    Usage:
      <DrylDonutChart Segments="@segs">
          <CenterContent><DrylStat Value="73" Label="Kunden" /></CenterContent>
      </DrylDonutChart>
      <DrylDonutChart Segments="@segs" InnerRadius="0" />   @* pie *@
    ───────────────────────────────────────────────────────── *@

@if (HasSegments)
{
    <div class="@RootCss("chart chart-kind-donut")"
         style="--chart-h:@(Height)px"
         role="group"
         aria-label="@(AriaLabel ?? SummaryLabel)"
         @attributes="AdditionalAttributes">
        @if (EffectiveAi != AiState.None)
        {
            <div class="ai-aura-ring"></div>
            <div class="ai-aura-glow"></div>
            @if (EffectiveAi == AiState.Generated)
            {
                <div class="ai-aura-wash" @key="GenTick"></div>
            }
        }
        <div class="donut-box">
            @{
                var total = Visible.Sum(v => v.Seg.Value);
                var angle = 0.0;
                var n = 0;
            }
            @foreach (var (seg, pos) in Visible)
            {
                var span = seg.Value / total * 360;
                var pad = Math.Min(1.6, span / 4);
                var start = angle + pad;
                var end = angle + span - pad;
                var mid = (start + end) / 2;
                var (lx, ly) = ChartMath.Polar(50, 50, ROuter * 0.75, mid);
                var (dx, dy) = ChartMath.Polar(0, 0, 2, mid);
                var pct = seg.Value / total * 100;
                var idx = n++;
                angle += span;
                <div class="donut-slice">
                    <svg viewBox="0 0 100 100" aria-hidden="false">
                        <path d="@ChartMath.DonutSegmentPath(50, 50, ROuter, RInner, start, end)"
                              fill="@SlotColor(seg.ColorSlot, pos)"
                              tabindex="0"
                              role="img"
                              aria-label="@($"{seg.Label}: {FormatValue(seg.Value)} ({FormatValue(Math.Round(pct))} %)")"
                              style="--dx:@(Inv(dx))px;--dy:@(Inv(dy))px;--i:@idx" />
                    </svg>
                    <div class="chart-tip donut-tip @(dx > 0 ? null : "chart-tip-flip")"
                         style="--tip-top:@(Inv(Math.Max(4, ly - 10)))%;--tip-left:@(Inv(lx))%"
                         aria-hidden="true">
                        <div class="chart-tip-row">
                            <span class="chart-swatch" style="background:@SlotColor(seg.ColorSlot, pos)"></span>
                            <span class="chart-tip-name">@seg.Label</span>
                            <span class="chart-tip-value">@FormatValue(seg.Value) · @FormatValue(Math.Round(pct)) %</span>
                        </div>
                    </div>
                </div>
            }
            @if (CenterContent is not null && InnerRadius > 0)
            {
                <div class="donut-center">@CenterContent</div>
            }
        </div>
        @if (ShowLegend ?? true)
        {
            <div class="chart-legend">
                @foreach (var (seg, pos) in Visible)
                {
                    <span class="chart-legend-item">
                        <span class="chart-swatch" style="background:@SlotColor(seg.ColorSlot, pos)"></span>@seg.Label
                    </span>
                }
            </div>
        }
    </div>
}

@code {
    /// <summary>The segments to plot. Zero and negative values are skipped.</summary>
    [Parameter] public IReadOnlyList<ChartSegment>? Segments { get; set; }

    /// <summary>Hole radius as a fraction of the outer radius (0–0.9). 0 renders a pie.</summary>
    [Parameter] public double InnerRadius { get; set; } = 0.65;

    /// <summary>Content rendered inside the hole (e.g. a KPI). Ignored when InnerRadius is 0.</summary>
    [Parameter] public RenderFragment? CenterContent { get; set; }

    private const double ROuter = 46;
    private double RInner => ROuter * Math.Clamp(InnerRadius, 0, 0.9);

    private bool HasSegments => Segments is { Count: > 0 } && Segments.Any(s => s.Value > 0);

    private IReadOnlyList<(ChartSegment Seg, int Pos)> Visible =>
        Segments!.Select((s, i) => (Seg: s, Pos: i)).Where(t => t.Seg.Value > 0).ToList();

    private string SummaryLabel => string.Join(", ", Visible.Select(v => v.Seg.Label));
}
```

Note: the donut legend default is "always show" (`ShowLegend ?? true`) — a donut with one segment is meaningless, and segment identity has no axis to fall back on.

- [ ] **Step 3: Run tests — expect PASS**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylDonutChartTests`

- [ ] **Step 4: Run the FULL test suite (regression gate)**

Run: `dotnet test DRYL.slnx`
Expected: all tests green across net8/net9/net10.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Data/Charts/DrylDonutChart.razor tests/DRYL.Components.Tests/DrylDonutChartTests.cs
git commit -m "feat(charts): DrylDonutChart — donut/pie, center slot, CSS-only hover"
```

---

### Task 7: CHANGELOG + DESIGN_TOKENS

**Files:**
- Modify: `CHANGELOG.md` (under `[Unreleased]` → `Added`; read the file first and match its existing entry style)
- Modify: `DESIGN_TOKENS.md` (add a Charts token block near the semantic colors section; match surrounding format)

- [ ] **Step 1: CHANGELOG entries**

```markdown
### Added
- `DrylLineChart` — Multi-series line chart; axes, gridlines, legend, pure-CSS hover tooltips, `Smooth` splines, `ShowMarkers`; AI-Mode
- `DrylBarChart` — Grouped or `Stacked` column chart; ≤ 24px bars, rounded data-ends, surface gaps; AI-Mode
- `DrylAreaChart` — Line chart with same-hue gradient fill to the zero baseline; `Smooth`; AI-Mode
- `DrylDonutChart` — Donut/pie with `InnerRadius`, `CenterContent` slot, per-segment CSS hover; AI-Mode
- CSS tokens `--chart-1` … `--chart-6` — categorical series palette, CVD-validated for the dark surface (adjacent ΔE ≥ 12, contrast ≥ 3:1)
```

- [ ] **Step 2: DESIGN_TOKENS.md** — add (adapting to the file's table/format conventions):

```markdown
### Chart series palette

| Token | Value | Use |
| --- | --- | --- |
| `--chart-1` | `#8b7cf8` | Series 1 (violet — accent-a family) |
| `--chart-2` | `#0aa2b5` | Series 2 (cyan) |
| `--chart-3` | `#bd7a12` | Series 3 (amber) |
| `--chart-4` | `#26a058` | Series 4 (green) |
| `--chart-5` | `#d6428e` | Series 5 (magenta) |
| `--chart-6` | `#5583e3` | Series 6 (blue) |

Fixed order, assigned in sequence, never cycled — series 7+ renders `--fg-dim`.
Validated (dataviz six checks, dark surface `#000000`): lightness band, chroma
floor, adjacent-pair CVD ΔE ≥ 12 (worst 24.1), contrast ≥ 3:1. Never use
`--success/--warning/--danger` as series colors — status is reserved.
```

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md DESIGN_TOKENS.md
git commit -m "docs(charts): changelog + chart palette tokens documented"
```

---

### Task 8: Website — catalog entries, demo pages, examples

**Files (all in `c:\Users\janzi\Desktop\DRYL\DRYL.Website`):**
- Modify: `Components/ComponentCatalog.cs` (4 entries in the Data block, after Sparkline)
- Create: `Components/Pages/DemoLineChart.razor`, `DemoBarChart.razor`, `DemoAreaChart.razor`, `DemoDonutChart.razor`
- Create: `Components/Examples/LineChart/{Basic,SmoothMarkers,AiStates}.razor`, `Components/Examples/BarChart/{Grouped,Stacked}.razor`, `Components/Examples/AreaChart/Basic.razor`, `Components/Examples/DonutChart/{Basic,CenterSlot}.razor`
  (Examples are embedded resources via the existing `Components/Examples/**/*.razor` glob — no csproj change.)

**Interfaces:**
- Consumes: the four public chart components + `ChartSeries`/`ChartSegment`.

- [ ] **Step 1: Catalog entries** (append to the Data block after the Sparkline line):

```csharp
new("Line Chart",  "line-chart",  "Data", "DrylLineChart",  "Data", true, "Multi-series lines — axes, tooltips, smooth splines.",      "Chart"),
new("Bar Chart",   "bar-chart",   "Data", "DrylBarChart",   "Data", true, "Grouped or stacked columns — CSS-only tooltips.",           "Chart"),
new("Area Chart",  "area-chart",  "Data", "DrylAreaChart",  "Data", true, "Soft gradient fills under multi-series lines.",             "Chart"),
new("Donut Chart", "donut-chart", "Data", "DrylDonutChart", "Data", true, "Proportions as donut or pie — center KPI slot.",            "Chart"),
```

(Check available `DrylIcon` names first — if a pie/bar icon exists, prefer it over `"Chart"` for donut/bar.)

- [ ] **Step 2: Demo pages** — follow the `DemoSparkline.razor` pattern exactly. Example `DemoLineChart.razor`:

```razor
@page "/components/line-chart"

<PageTitle>DRYL — Line Chart</PageTitle>

<div class="col fade-in" style="gap: var(--sp-7);">

    <ComponentDocHeader Slug="line-chart">
        Multi-series line chart with axes, gridlines, legend and pure-CSS hover
        tooltips — zero JavaScript. Series colors come from the CVD-validated
        <code>--chart-1…6</code> palette; coordinates are invariant-culture SVG.
    </ComponentDocHeader>

    <DemoExample Title="Basic" Source="LineChart/Basic">
        <DRYL.Website.Components.Examples.LineChart.Basic />
    </DemoExample>

    <DemoExample Title="Smooth + markers" Source="LineChart/SmoothMarkers">
        <DRYL.Website.Components.Examples.LineChart.SmoothMarkers />
    </DemoExample>

    <DemoExample Title="AI states" Source="LineChart/AiStates">
        <DRYL.Website.Components.Examples.LineChart.AiStates />
    </DemoExample>

</div>
```

Example content file `Examples/LineChart/Basic.razor`:

```razor
<DrylLineChart Labels="@_months"
               Series="@_series"
               ValueFormat="N0" />

@code {
    private readonly string[] _months = ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun"];
    private readonly ChartSeries[] _series =
    [
        new("Umsatz", new double[] { 4200, 5100, 4800, 6300, 5900, 7100 }),
        new("Kosten", new double[] { 3100, 3300, 3600, 3500, 3900, 4100 }),
    ];
}
```

The remaining example files follow the same shape and must together cover: grouped vs `Stacked` bars, `Smooth`, `ShowMarkers`, negative values (grouped bars), donut vs `InnerRadius="0"` pie, `CenterContent` with `DrylStat`, `Ai="AiState.Thinking|Streaming|Generated"`, and one `ShowLegend="false"` case.

- [ ] **Step 3: Build website**

Run: `dotnet build "c:\Users\janzi\Desktop\DRYL\DRYL.Website"`
Expected: Build succeeded.

- [ ] **Step 4: Commit** (website repo is part of the same working tree? It is a separate project directory — commit in whichever repo it belongs to; check `git -C c:\Users\janzi\Desktop\DRYL\DRYL.Website rev-parse --show-toplevel` first.)

```bash
git add Components/ComponentCatalog.cs Components/Pages/Demo*Chart.razor Components/Examples
git commit -m "docs(website): chart family — catalog entries, demo pages, examples"
```

---

### Task 9: Visual verification & polish (dataviz step 7 — render it and look at it)

- [ ] **Step 1: Run the website** (`dotnet run` in DRYL.Website or the project's usual launch profile) and open the four demo pages with Playwright; screenshot each.
- [ ] **Step 2: Eyeball checklist** — label collisions (x-axis thinning working?), tooltip flip near the right edge, bars ≤ 24px with visible gaps, donut gaps and hover lift, legend wrapping, enter animations (line draw, bar grow stagger, donut stagger), Ai aura on charts.
- [ ] **Step 3: Check at 375px viewport** (responsive foundation baseline) — axis labels legible, tooltips inside bounds.
- [ ] **Step 4: Check anti-patterns** — re-read `references/anti-patterns.md` from the dataviz skill and confirm none apply.
- [ ] **Step 5: Fix anything found; re-run full test suite; commit fixes.**

```bash
dotnet test DRYL.slnx
git add -A && git commit -m "fix(charts): visual polish from verification pass"
```
