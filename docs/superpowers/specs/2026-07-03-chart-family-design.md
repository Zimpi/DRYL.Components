# Chart Family — Design Spec

**Date:** 2026-07-03
**Status:** Approved (brainstorming session)
**Components:** `DrylLineChart`, `DrylBarChart`, `DrylAreaChart`, `DrylDonutChart`

## Goal

Close the biggest gap in DRYL for enterprise dashboards: a real chart family
(line, bar, area, donut/pie) beyond `DrylSparkline`. Pure SVG, zero JS, zero
external dependencies (Rule 2.8) — like `DrylSparkline`, but dashboard-grade:
axes, gridlines, legend, multi-series, hover tooltips.

## Scope decisions (from brainstorming)

| Decision | Choice |
| --- | --- |
| Feature depth | Dashboard standard: axes + labels, gridlines, legend, multi-series, per-point hover tooltip. **No** zoom/pan/brush/series-toggle/export. |
| Data API | Series objects (`ChartSeries` record), not markup children. |
| Tooltips | Pure CSS/SVG — pre-rendered SVG tooltips shown via `:hover`/`:focus-visible`. No JS interop, no Blazor mouse events (Server latency). |
| AI mode | Standard `Ai` parameter (`AiState`, default `None`) on all four charts, mapped to the existing `.ai-aura*` primitives. |
| Colors | New tokens `--chart-1..6` in `dryl.css`, validated with the dataviz palette validator. |
| Sizing | Container-filling: width 100%, `Height` parameter (px, default 260). Fits the container-query-first responsive foundation. |
| v1 variants | Stacked bars ✔, smooth curves (line/area) ✔, donut center slot + pie mode ✔. Stacked areas ✘ (deliberately out). |
| Architecture | Shared core: `DrylChartBase` + `DrylCartesianChartBase` code-behind base classes; four thin `.razor` components render only their marks. |

## File layout

```
DRYL.Components/Components/Data/Charts/
├─ ChartSeries.cs             (record: Name, Data, ColorSlot?)
├─ ChartSegment.cs            (record: Label, Value, ColorSlot?)
├─ DrylChartBase.cs           (series, legend, Ai, formatting, aria)
├─ DrylCartesianChartBase.cs  (scales, nice ticks, axes, grid, hover zones)
├─ DrylLineChart.razor        (line marks, Smooth, ShowMarkers)
├─ DrylBarChart.razor         (grouped/stacked rects)
├─ DrylAreaChart.razor        (area fill + line)
└─ DrylDonutChart.razor       (arcs, InnerRadius, CenterContent)
```

Namespace stays `DRYL.Components`. Shared CSS primitives go into `dryl.css`
(new "Charts" section) because four components share them — not 4× scoped CSS.

**Rendering model (refined during planning):** hybrid SVG/HTML. Line and area
paths are SVG (`viewBox="0 0 100 100"`, `preserveAspectRatio="none"`,
`vector-effect="non-scaling-stroke"`); bars, markers, grid, axes, legend and
tooltips are percent-positioned HTML. Reason: stretched SVG would distort text
at narrow container widths; HTML text never distorts and the tooltip gets real
glass tokens. The donut is aspect-preserved per-segment SVG (no distortion),
with HTML tooltips driven by CSS `:hover`/`:focus-within` on slice wrappers.

## Data model

```csharp
/// <summary>One data series for cartesian charts.</summary>
public sealed record ChartSeries(string Name, IReadOnlyList<double> Data)
{
    /// <summary>Optional fixed palette slot (1–6). Default: position in the Series list.</summary>
    public int? ColorSlot { get; init; }
}

/// <summary>One segment for DrylDonutChart.</summary>
public sealed record ChartSegment(string Label, double Value)
{
    public int? ColorSlot { get; init; }
}
```

`ColorSlot` implements "color follows the entity, never its rank": when a
dashboard filter removes a series, the survivors keep their color.

## Public API

### Shared (`DrylChartBase`)

Series-agnostic base — `DrylDonutChart` derives from it directly; `Series`/
`Labels` live on `DrylCartesianChartBase`.

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `Height` | `int` | `260` | px; width = 100% of container |
| `ShowLegend` | `bool?` | `null` | auto: legend from 2 series, none for 1 (dataviz rule) |
| `ValueFormat` | `string?` | `null` | .NET format string for ticks/tooltips (`"N0"`, `"C0"`); culture-aware by design |
| `Ai` | `AiState` | `AiState.None` | Rule 2.10; `Generated` = one-shot reveal |
| `AriaLabel` | `string?` | `null` | fallback auto-generated from series names |
| `Class` | `string?` | `null` | merged class param (no splat clobber) |
| `AdditionalAttributes` | splat | — | pass-through |

### Cartesian (`DrylCartesianChartBase`)

`Series` (`IReadOnlyList<ChartSeries>`, required), `Labels`
(`IReadOnlyList<string>?`, category labels for the x-axis),
`ShowXAxis` / `ShowYAxis` / `ShowGridLines` (all `bool`, default `true`),
`YMin` / `YMax` (`double?`, default auto via nice-tick algorithm; bar charts
always include 0 in the auto range).

### Per component

- **`DrylLineChart`** — `Smooth` (`bool`, Catmull-Rom → cubic Bézier),
  `ShowMarkers` (`bool`; dots r=4 with 2px surface ring).
- **`DrylAreaChart`** — `Smooth`. Fill is a vertical fade of the series hue
  (~12% → 2% via `color-mix`). No stacked mode.
- **`DrylBarChart`** — `Stacked` (`bool`, default grouped).
- **`DrylDonutChart`** — takes `Segments` (`IReadOnlyList<ChartSegment>`)
  instead of `Series`/`Labels`; `InnerRadius` (`double` 0–0.9, default 0.65;
  `0` = pie), `CenterContent` (`RenderFragment`, rendered in the hole).

### Usage

```razor
<DrylLineChart Labels="@months"
               Series="@(new[]{ new ChartSeries("Umsatz", rev), new ChartSeries("Kosten", cost) })"
               ValueFormat="C0" Smooth />

<DrylDonutChart Segments="@(new[]{ new ChartSegment("Cloud", 42), new ChartSegment("On-Prem", 31) })">
    <CenterContent><DrylStat Value="73" Label="Kunden" /></CenterContent>
</DrylDonutChart>
```

## Visual design

### Color tokens (new in `dryl.css`)

```css
--chart-1: #8b7cf8;  /* violet — accent-a family */
--chart-2: #0aa2b5;  /* cyan   — accent-b family */
--chart-3: #bd7a12;  /* amber  */
--chart-4: #26a058;  /* green  */
--chart-5: #d6428e;  /* magenta */
--chart-6: #5583e3;  /* blue   */
```

These values are **final** — validated with the dataviz palette validator
(`validate_palette.js --mode dark --surface "#000000"`): lightness band,
chroma floor, adjacent-pair CVD ΔE ≥ 12 (worst pair 24.1), contrast ≥ 3:1 on
black — all six checks PASS. DRYL is dark-only, so one validation run suffices.

Rules:

- Six slots, fixed order, assigned in sequence, **never cycled**. Series 7+ is
  a documented anti-pattern (recommend "Other"/small multiples in docs); any
  series beyond slot 6 renders in a muted fallback (`var(--fg-dim)`), visually
  reading as "other" — colors are never repeated and existing series never
  repaint.
- Semantic colors (`--ok/--warning/--danger`) are **never** used as series
  colors, and v1 provides no mechanism to do so (status charts are a follow-up
  topic).
- No violet→cyan cross-hue gradients on multi-series marks — identity beats
  decoration. Area fills fade the *same* hue vertically.

### Mark specs (fixed, from the dataviz skill)

- Lines: 2px, round cap/join. Markers: r=4 (8px) + 2px ring in `var(--bg)`.
- Bars: ≤ 24px thick, 4px rounded data-end, **square at the baseline**; 2px
  surface-color gaps between stacked segments and between adjacent bars.
- Area fill: vertical fade of series hue ~12% → 2%.
- Grid: 1px solid `var(--line)`, recessive. Axis text `var(--fg-dim)`.
- **Text never wears the series color** — legend/tooltips show a color swatch
  next to neutral text (`var(--fg)` / `var(--fg-muted)`).
- Donut: flat segments, 2px gaps, hover shifts segment slightly outward.

### Motion (Rule 2.12; tokens only: `--dur-fast|med|slow`, `--ease-out|in-out|spring`)

- **Enter:** lines draw in via `pathLength`/`stroke-dashoffset`
  (`--dur-slow` `--ease-out`); bars grow from the baseline (`scaleY`,
  `--ease-spring`, ~30ms stagger per bar via `animation-delay`); donut
  segments stagger-fade in; areas fade.
- **Hover:** markers enlarge, bars/segments get a subtle glow (`--dur-fast`),
  tooltip fades in with a small translate.
- **AI:** `Ai` wraps the chart container in the existing `.ai-aura*`
  primitives; `Generated` plays the one-shot reveal.
- `prefers-reduced-motion: reduce` disables all of it; chart stays fully usable.

## Interaction & accessibility

- **Cartesian hover:** one invisible full-height column hover zone per category
  index. `:hover` reveals (pure CSS) a crosshair line + a pre-rendered SVG
  tooltip listing **all** series values at that index (swatch + name +
  formatted value). Tooltip x-position is computed server-side and flipped
  near the right edge so it never clips.
- **Donut hover:** the segment itself is the zone; tooltip shows label, value,
  percent.
- **Keyboard:** every hover zone gets `tabindex="0"` and an `aria-label`
  containing the full values ("Jan: Umsatz 1.200 €, Kosten 830 €");
  `:focus-visible` shows the same tooltip. Data is reachable without a mouse
  (Rule 2.9).
- **Screen readers:** the chart root has `role="group"` + a generated summary
  label (`role="img"` would make the focusable hover zones presentational).
  Docs recommend pairing with `DrylTable` for a full table view (deliberately
  not built in — YAGNI).
- Legend is always present for ≥ 2 series; a single series gets no legend box
  (`ShowLegend=null` auto rule; explicit `true`/`false` overrides).

## Edge cases

- `Series` empty/null → component renders nothing (like `DrylSparkline`;
  placeholder is the consumer's job, e.g. `DrylSkeleton`).
- Series of different lengths → shorter series simply ends earlier; no crash.
- Negative values: supported for line/area/grouped bars (baseline at 0 drawn).
  **Stacked + negative values is unsupported in v1** (documented; values are
  clamped to ≥ 0 in stacking math).
- All SVG coordinates via `FormattableString.Invariant` (de-DE locale bug);
  *display* values (ticks, tooltips) are intentionally culture-aware via
  `ValueFormat`.
- No JS, no `IDisposable`, prerender-safe by construction.

## Testing (bUnit + plain unit tests)

- Nice-tick algorithm (ranges incl. negatives, tiny spans, flat series).
- Stacking math (sums, clamping of negatives).
- Donut arc math (angles sum to 360°, zero-value segments skipped).
- Invariant rendering under `de-DE` culture (paths/points parse).
- Legend auto rule (1 series = no box; ≥ 2 = box; explicit override wins).
- ColorSlot stability when a series is removed.
- `Ai` class mapping (aura classes present/absent, default off).
- Smooth path produces a valid `d` (starts with `M`, contains `C`).

## Documentation duties

- `CHANGELOG.md` → `[Unreleased]` → `Added`: 4 components + `--chart-1..6`
  tokens.
- `ComponentCatalog` (DRYL.Website): 4 new entries under the Data category,
  each with a demo page (DemoExample framework) covering: multi-series,
  Smooth, Stacked, donut/pie, center slot, `Ai` states, empty state.
- `DESIGN_TOKENS.md`: document `--chart-1..6` (+ validator provenance).
