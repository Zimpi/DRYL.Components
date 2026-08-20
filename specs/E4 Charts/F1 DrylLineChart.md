# DrylLineChart

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/Charts/DrylLineChart.razor

## User Story

As a Blazor developer building a dashboard on DRYL, I want to hand a list of
series and a list of labels to a component and get a readable multi-series line
chart with axes, gridlines, a legend and hover tooltips, so that I can show a
trend over time without taking on a charting library or writing a line of
JavaScript.

## Description

`DrylLineChart` plots one or more `ChartSeries` as lines over a shared category
axis. It is the plainest member of the chart family and the reference for the
other two cartesian charts: it contributes the line marks and nothing else, and
inherits the entire frame — y-ticks, gridlines, x-labels, hover columns,
crosshair, tooltips and legend — from `DrylCartesianChartBase` and the shared
chart frame.

The rendering is hybrid on purpose. The lines are SVG in a viewBox stretched to
the plot area with non-scaling strokes, so the geometry is resolution
independent while the stroke keeps a constant visual weight at any aspect ratio.
Everything that is text — ticks, labels, legend, tooltip — is ordinary HTML
outside that SVG, so it never distorts with the stretch. The whole component
runs without JavaScript; see [`_Interop.md`](_Interop.md) for what that buys.

Two parameters are its own: `Smooth` swaps the straight segments for a spline,
and `ShowMarkers` puts a dot on every data point. Everything else the consumer
touches is the shared contract in [`_Api.md`](_Api.md).

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Smooth` | `bool` | `false` | Draw the line as a Catmull-Rom spline instead of straight segments. |
| `ShowMarkers` | `bool` | `false` | Mark every data point with a dot. |

Inherited from `DrylCartesianChartBase`: `Series`, `Labels`, `ShowXAxis`,
`ShowYAxis`, `ShowGridLines`, `YMin`, `YMax`.
Inherited from `DrylChartBase`: `Height`, `ShowLegend`, `ValueFormat`,
`AriaLabel`, `Class`, `AdditionalAttributes`, `Ai`, `Aura`.
Both are specified in [`_Api.md`](_Api.md); the criteria below cover only what
this component decides.

The component exposes no `EventCallback` and no `RenderFragment`: it displays
data, it does not collect input.

## Acceptance Criteria

### Rendering

- The component renders nothing when `Series` is `null`.
- The component renders nothing when `Series` is empty.
- The component renders nothing when every series in `Series` has an empty
  `Data` list.
- The component renders one line per entry in `Series`.
- The component renders each line in its series' palette color, resolved as
  described in [`_Api.md`](_Api.md).
- The component renders a line for a series whose `Data` is shorter than the
  longest series, covering only the categories that series has.
- The line marks carry no pointer events, so they never intercept a hover
  intended for the column beneath them.
- `Class` is merged onto the chart root's own classes.
- `AdditionalAttributes` are applied to the chart root.

### Line shape

- `Smooth` defaults to `false`.
- The line is drawn as straight segments between consecutive points when
  `Smooth` is `false`.
- The line is drawn as a spline through the points when `Smooth` is `true`.
- A series of fewer than three points is drawn as straight segments even when
  `Smooth` is `true`, because a spline through two points is that same segment.
- The spline passes through every data point rather than approximating it, so
  reading a value off the curve stays honest.

### Markers

- `ShowMarkers` defaults to `false`.
- The component renders no markers when `ShowMarkers` is `false`.
- The component renders one marker per data point of every series when
  `ShowMarkers` is `true`.
- Each marker is filled in its series' palette color.
- Each marker carries a ring in `--bg`, so it stays legible where it sits on top
  of another series' line.
- Markers are decorative and are `aria-hidden`: the values they mark are already
  announced by the hover column (`UX-07`).

### Numbers and locale

- Every number written into SVG path data is formatted with the invariant
  culture, so path data parses under any thread culture.
- Every number written into a CSS custom property or an inline style is
  formatted with the invariant culture.
- Tick and tooltip values are formatted culture-aware, per `ValueFormat` in
  [`_Api.md`](_Api.md).

### Motion

- The lines wipe in from left to right on mount, over `--dur-slow` with
  `--ease-out`.
- The wipe leaves no element clipped once it has finished.
- Markers fade in with `--dur-med` and `--ease-out`.
- Markers fade in staggered from left to right, roughly tracking the line wipe
  above them.
- The crosshair fades in and out with `--dur-fast` and `--ease-out`.
- The tooltip fades and slides in with `--dur-fast` and `--ease-out`.
- All chart animations and transitions are switched off under
  `prefers-reduced-motion: reduce`, leaving a complete, legible chart.
- The component has no exit animation of its own; a chart that is mounted
  conditionally is wrapped in `DrylPresence` by its host (`DESIGN-12`).

  The marker stagger step and the wipe's clipping slack are written as literals
  in `code/DRYL.Components/wwwroot/dryl.css`. `DESIGN-10` binds the durations and
  easings, which are tokens; the per-item delay step is a stagger increment
  rather than a duration. Recorded here as documented debt, not as compliance.

### Keyboard and accessibility

- The chart root carries `role="group"`.
- The chart root carries an accessible label: `AriaLabel` when set, the
  comma-separated series names otherwise.
- Every category has a hover column that is reachable by `Tab`.
- A focused hover column shows the same crosshair and tooltip that hovering it
  shows, so the keyboard path is not a second-class one.
- A focused hover column carries a visible focus ring in `--accent-line`.
- Every hover column carries an accessible label naming its category and, per
  series, the series name and its formatted value.
- The SVG line marks are `aria-hidden`, so the data is announced once — through
  the columns — and not twice.
- The y-axis, x-axis and gridlines are `aria-hidden`: they are scaffolding for
  the eye, and the values they carry are in the column labels.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The series colors come from the six chart palette slots (`--chart-1` …
  `--chart-6`), and a series beyond the sixth renders in `--fg-dim` rather than
  cycling (see [`_Api.md`](_Api.md)).
- Gridlines use `--line` and the zero line `--line-strong`, so the zero line
  reads as the stronger of the two.
- The tooltip is a floating panel: `--panel-solid` fill, `--line-strong` border,
  `--shadow-md`, `--r-sm` radius.
- The tooltip flips to the other side of the crosshair for columns in the right
  part of the plot, so it never leaves the chart.
- Tooltip and axis values are rendered with tabular figures, so digits do not
  jitter as the pointer moves between columns.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- The component renders no frost: it is an in-flow surface on the page's ground,
  and the only floating surface it owns is the tooltip (`DESIGN-06`).

### AI mode

- `Ai` defaults to `AiState.None`, and the chart then renders exactly as a
  non-AI chart does — no ring, no glow, no added padding.
- An explicit `Ai` value wins over a surrounding `DrylAiScope`.
- `Ai` left unset inherits the state of a surrounding `DrylAiScope`.
- While the effective state is not `AiState.None`, the chart root becomes a
  contained AI panel — a `--glass-1` ground with `--r-lg` corners and padding —
  so the aura ring traces a rounded surface instead of a bare rectangle.
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
  set per mode in both LIGHT-TOKEN-SET copies; the component itself defines no
  mode-specific rule.
- **Enter/exit animation** — the line wipe and the marker stagger are the
  entrance; the exit belongs to `DrylPresence` on the host's side, per the
  "Motion" criteria above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  hover columns are the accessible representation of the data.
- **AI mode** — yes, via `DrylAiAware`. The chart is a natural place for an
  agent to show that it is producing or has just produced a figure, and the
  criteria under "AI mode" say what that looks like.
- **Demo page** — `DRYL.Website/Components/Pages/DemoLineChart.razor`, with the
  examples `Components/Examples/LineChart/Basic.razor`,
  `.../SmoothMarkers.razor` and `.../AiStates.razor`.
- **`ComponentCatalog`** — registered as `"Line Chart"` / `line-chart` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
