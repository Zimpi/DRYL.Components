# DrylBarChart

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/Charts/DrylBarChart.razor

## User Story

As a Blazor developer building a dashboard on DRYL, I want to plot my series as
columns — side by side, or stacked into one column per category — so that I can
compare discrete categories instead of showing a trend, without leaving the
chart vocabulary the rest of my page already uses.

## Description

`DrylBarChart` renders each `ChartSeries` as a column per category. It shares
the whole frame with the other cartesian charts — y-ticks, gridlines, x-labels,
hover columns, crosshair, tooltips and legend — and contributes only the bars.

It differs from its two siblings in where it places a category on the x-axis.
Lines and areas sit *on* the category positions; bars sit in *bands* around
them, centred in an equal share of the plot width. The same decision brings
zero into the y-range unconditionally: a bar is read as a length from the
baseline, so a range that does not contain zero would make every bar a lie.

`Stacked` turns the grouped columns into one column per category. Negative
values are not supported when stacking and are clamped to zero — stacking mixed
signs has no single honest reading, and the clamp is the documented behaviour
rather than an exception thrown at the consumer.

The bars are percent-positioned HTML rather than SVG, which is what lets them
grow, round only at the data end and brighten on hover with plain CSS. The
component runs without JavaScript; see [`_Interop.md`](_Interop.md).

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Stacked` | `bool` | `false` | Stack the series into one column per category instead of grouping them side by side. |

Inherited from `DrylCartesianChartBase`: `Series`, `Labels`, `ShowXAxis`,
`ShowYAxis`, `ShowGridLines`, `YMin`, `YMax`.
Inherited from `DrylChartBase`: `Height`, `ShowLegend`, `ValueFormat`,
`AriaLabel`, `Class`, `AdditionalAttributes`, `Ai`, `Aura`.
Both are specified in [`_Api.md`](_Api.md); the criteria below cover only what
this component decides.

The component exposes no `EventCallback` and no `RenderFragment`.

## Acceptance Criteria

### Rendering

- The component renders nothing when `Series` is `null`.
- The component renders nothing when `Series` is empty.
- The component renders nothing when every series in `Series` has an empty
  `Data` list.
- The component renders one band per category.
- Bands are of equal width and together fill the plot area.
- Each category sits at the centre of its band, so a bar is centred over its
  x-label.
- Each bar is filled in its series' palette color, resolved as described in
  [`_Api.md`](_Api.md).
- A bar is never wider than a fixed maximum, so a chart of two categories shows
  two bars rather than two blocks.
- Adjacent bars within a band are separated by a gap in the surface color.
- `Class` is merged onto the chart root's own classes.
- `AdditionalAttributes` are applied to the chart root.

### Grouped mode

- `Stacked` defaults to `false`.
- Grouped mode renders one bar per series per category, side by side within the
  band.
- A bar grows from the zero line to its value.
- A bar for a negative value grows downward from the zero line.
- A bar for a negative value is rounded at its lower end and square at the zero
  line, mirroring the positive case.
- A series that carries no value for a category is drawn at zero height there,
  so the remaining bars keep their slots and the band stays aligned with its
  neighbours.

### Stacked mode

- Stacked mode renders one bar per category, composed of one segment per series.
- Each segment's height is that series' share of the category's total.
- Segments are ordered so the first series sits at the baseline.
- A negative value contributes zero to the stack and renders no segment.
- A series whose value is zero for a category renders no segment for it.
- Only the topmost segment of a stack is rounded; the ones below it are square,
  so the column reads as one bar rather than as a row of pills.
- Adjacent segments are separated by a gap in the surface color.
- The y-range covers the tallest stack, not the largest single value.

### Y-range

- Zero is always inside the y-range, whether or not the data reaches it.
- `YMin` and `YMax` each override their bound of the automatic range when set.
- Tick values are rounded to readable numbers rather than to the raw data
  extremes.

### Numbers and locale

- Every number written into a CSS custom property or an inline style is
  formatted with the invariant culture, so the style parses under any thread
  culture.
- Tick and tooltip values are formatted culture-aware, per `ValueFormat` in
  [`_Api.md`](_Api.md).

### Motion

- Bars grow from their baseline on mount, over `--dur-slow` with
  `--ease-spring`.
- Bars grow staggered from left to right, so the chart builds up rather than
  appearing at once.
- Stacked segments grow with the same duration, easing and stagger as grouped
  bars, so the two modes read as one component.
- Hovering a band brightens its bars over `--dur-fast` with `--ease-out`.
- The crosshair fades in and out with `--dur-fast` and `--ease-out`.
- The tooltip fades and slides in with `--dur-fast` and `--ease-out`.
- All chart animations and transitions are switched off under
  `prefers-reduced-motion: reduce`, leaving a complete, legible chart.
- The component has no exit animation of its own; a chart that is mounted
  conditionally is wrapped in `DrylPresence` by its host (`DESIGN-12`).

  The bar stagger step, the maximum bar width, the inter-bar gap and the bar's
  corner radius are written as literals in
  `code/DRYL.Components/wwwroot/dryl.css`. `DESIGN-10` binds the durations and
  easings, which are tokens; the per-item delay step is a stagger increment
  rather than a duration. Recorded here as documented debt, not as compliance.

### Keyboard and accessibility

- The chart root carries `role="group"`.
- The chart root carries an accessible label: `AriaLabel` when set, the
  comma-separated series names otherwise.
- Every category has a hover column that is reachable by `Tab`.
- A focused hover column shows the same crosshair and tooltip that hovering it
  shows.
- A focused hover column carries a visible focus ring in `--accent-line`.
- Every hover column carries an accessible label naming its category and, per
  series, the series name and its formatted value.
- The bars are `aria-hidden`, so the data is announced once — through the
  columns — and not twice.
- The stacked mode announces the per-series values, not the stack totals, so a
  screen reader hears the same numbers the tooltip shows.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The series colors come from the six chart palette slots (`--chart-1` …
  `--chart-6`), and a series beyond the sixth renders in `--fg-dim` rather than
  cycling (see [`_Api.md`](_Api.md)).
- Gridlines use `--line` and the zero line `--line-strong`.
- The tooltip is a floating panel: `--panel-solid` fill, `--line-strong` border,
  `--shadow-md`, `--r-sm` radius.
- The tooltip flips to the other side of the crosshair for columns in the right
  part of the plot, so it never leaves the chart.
- Tooltip and axis values are rendered with tabular figures.
- A bar is a saturated fill of a palette color, not of an accent: the palette
  slots are data colors, and using one is not the accent-as-surface `DESIGN-08`
  forbids.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- The component renders no frost; the only floating surface it owns is the
  tooltip (`DESIGN-06`).

### AI mode

- `Ai` defaults to `AiState.None`, and the chart then renders exactly as a
  non-AI chart does.
- An explicit `Ai` value wins over a surrounding `DrylAiScope`.
- `Ai` left unset inherits the state of a surrounding `DrylAiScope`.
- While the effective state is not `AiState.None`, the chart root becomes a
  contained AI panel — a `--glass-1` ground with `--r-lg` corners and padding.
- The aura variant follows `Aura` when set and the surrounding scope otherwise.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- `AiState.Generated` retires itself to `AiState.None` without the host having to
  hand it back.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. The chart palette carries a tuned
  set per mode in both LIGHT-TOKEN-SET copies; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the staggered bar growth is the entrance; the exit
  belongs to `DrylPresence` on the host's side, per the "Motion" criteria above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, via `DrylAiAware`, on the same terms as the rest of the
  chart family; the criteria under "AI mode" say what it looks like.
- **Demo page** — `DRYL.Website/Components/Pages/DemoBarChart.razor`, with the
  examples `Components/Examples/BarChart/Grouped.razor` and `.../Stacked.razor`.
- **`ComponentCatalog`** — registered as `"Bar Chart"` / `bar-chart` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
