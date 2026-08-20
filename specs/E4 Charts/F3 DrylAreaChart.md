# DrylAreaChart

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/Charts/DrylAreaChart.razor

## User Story

As a Blazor developer building a dashboard on DRYL, I want the same multi-series
line chart with a soft fill down to the baseline, so that a volume over time
reads as a mass rather than as a thin stroke, without me hand-building gradients
and baseline paths.

## Description

`DrylAreaChart` is `DrylLineChart` with a filled body: the same line, the same
frame, plus a translucent area from the line down to the zero baseline. It
inherits its axes, gridlines, hover columns, tooltips and legend from
`DrylCartesianChartBase` and the shared chart frame, and contributes the area
path and its gradient.

The fill is a vertical fade **of the series' own color** — opaque at the line,
almost gone at the baseline. That is a deliberate limit: identity beats
decoration, so a multi-series area chart never gets cross-hue gradients that
would make two series share a middle color and stop being tellable apart.

The areas are not stacked. Each series is drawn to the baseline independently
and they overlap; the fills are faint enough to read through each other. A
consumer who needs stacked volumes uses `DrylBarChart` with `Stacked`.

The component runs without JavaScript; see [`_Interop.md`](_Interop.md).

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Smooth` | `bool` | `false` | Draw the line and the area's upper edge as a Catmull-Rom spline instead of straight segments. |

Inherited from `DrylCartesianChartBase`: `Series`, `Labels`, `ShowXAxis`,
`ShowYAxis`, `ShowGridLines`, `YMin`, `YMax`.
Inherited from `DrylChartBase`: `Height`, `ShowLegend`, `ValueFormat`,
`AriaLabel`, `Class`, `AdditionalAttributes`, `Ai`, `Aura`.
Both are specified in [`_Api.md`](_Api.md); the criteria below cover only what
this component decides.

The component has no `ShowMarkers` parameter: markers would sit on a filled
body, where they add noise rather than precision. A chart that needs point
markers is a `DrylLineChart`.

The component exposes no `EventCallback` and no `RenderFragment`.

## Acceptance Criteria

### Rendering

- The component renders nothing when `Series` is `null`.
- The component renders nothing when `Series` is empty.
- The component renders nothing when every series in `Series` has an empty
  `Data` list.
- The component renders one line per entry in `Series`.
- The component renders one filled area per entry in `Series`.
- Each line is stroked in its series' palette color, resolved as described in
  [`_Api.md`](_Api.md).
- Each area is closed down to the zero baseline of the current y-range.
- Each area is drawn beneath its own line, so the stroke stays crisp on top of
  the fill.
- Areas are drawn independently and may overlap; they are not stacked.
- The marks carry no pointer events, so they never intercept a hover intended
  for the column beneath them.
- `Class` is merged onto the chart root's own classes.
- `AdditionalAttributes` are applied to the chart root.

### Line and area shape

- `Smooth` defaults to `false`.
- The line and the area's upper edge are drawn as straight segments when
  `Smooth` is `false`.
- The line and the area's upper edge are drawn as a spline when `Smooth` is
  `true`.
- The area's upper edge follows exactly the same path as the line, so the fill
  never separates from its stroke.
- A series of fewer than three points is drawn as straight segments even when
  `Smooth` is `true`.

### The fill

- Each area is filled with a vertical gradient of its own series color, from
  more opaque at the top to nearly transparent at the baseline.
- The gradient uses one hue per series: no series is filled with a blend of two
  palette colors.
- The fill is faint enough that a line drawn behind another series' area stays
  visible.
- Each series' gradient definition carries an id unique to the component
  instance, so two area charts on one page never claim each other's fills.

### Numbers and locale

- Every number written into SVG path data is formatted with the invariant
  culture, so path data parses under any thread culture.
- Every number written into a CSS custom property or an inline style is
  formatted with the invariant culture.
- Tick and tooltip values are formatted culture-aware, per `ValueFormat` in
  [`_Api.md`](_Api.md).

### Motion

- The lines and areas wipe in from left to right on mount, over `--dur-slow`
  with `--ease-out`.
- Line and area wipe together as one mark, so the fill never lags behind its
  stroke.
- The wipe leaves no element clipped once it has finished.
- The crosshair fades in and out with `--dur-fast` and `--ease-out`.
- The tooltip fades and slides in with `--dur-fast` and `--ease-out`.
- All chart animations and transitions are switched off under
  `prefers-reduced-motion: reduce`, leaving a complete, legible chart.
- The component has no exit animation of its own; a chart that is mounted
  conditionally is wrapped in `DrylPresence` by its host (`DESIGN-12`).

  The gradient's two stop opacities and the wipe's clipping slack are written as
  literals in the component and in
  `code/DRYL.Components/wwwroot/dryl.css`. `DESIGN-01` governs colors, and the
  colors here are tokens — the opacities modulating them are not covered by a
  token today. Recorded as documented debt, not as compliance.

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
- The SVG marks are `aria-hidden`, so the data is announced once — through the
  columns — and not twice.

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
  mode-specific rule. The fill is a transparency of the series color, so it
  follows the mode with it rather than assuming a ground.
- **Enter/exit animation** — the left-to-right wipe of line and fill is the
  entrance; the exit belongs to `DrylPresence` on the host's side, per the
  "Motion" criteria above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, via `DrylAiAware`, on the same terms as the rest of the
  chart family; the criteria under "AI mode" say what it looks like.
- **Demo page** — `DRYL.Website/Components/Pages/DemoAreaChart.razor`, with the
  example `Components/Examples/AreaChart/Basic.razor`.
- **`ComponentCatalog`** — registered as `"Area Chart"` / `area-chart` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
