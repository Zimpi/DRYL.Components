# DrylDonutChart

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/Charts/DrylDonutChart.razor
              code/DRYL.Components/Components/Data/Charts/ChartSegment.cs

## User Story

As a Blazor developer building a dashboard on DRYL, I want to show how a whole
splits into parts, with the headline number sitting in the middle of the ring,
so that a share-of-total reads at a glance without me computing angles, arcs or
percentages myself.

## Description

`DrylDonutChart` plots a list of `ChartSegment` as a ring of proportional
segments. It is the one member of the chart family with no axes: it derives from
`DrylChartBase` directly rather than from `DrylCartesianChartBase`, and shares
with its siblings the sizing, legend, value formatting, palette and AI aura, but
none of the axis machinery.

`InnerRadius` scales the hole. At its default the component is a donut; at `0`
it is a pie, and the `CenterContent` slot is then ignored because there is no
hole to put anything in. That slot is what makes the donut a KPI surface rather
than only a proportion display — the usual content is a `DrylStat` naming the
total the segments add up to.

Two details are load-bearing and easy to get wrong when reimplementing it. The
separators between segments are a **stroke** in the surface color, not angular
padding: angular padding wedges to a point at the centre and looks broken on a
pie, while a stroke keeps the same visible gap at every radius. And a lone
segment covering the entire circle is drawn just short of a full turn, because
an arc whose start and end angle are equal draws nothing at all.

The component runs without JavaScript; see [`_Interop.md`](_Interop.md).

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Segments` | `IReadOnlyList<ChartSegment>?` | `null` | The segments to plot. Zero and negative values are skipped. |
| `InnerRadius` | `double` | `0.65` | Hole radius as a fraction of the outer radius, clamped to 0–0.9. `0` renders a pie. |
| `CenterContent` | `RenderFragment?` | `null` | Content rendered inside the hole. Ignored while `InnerRadius` is `0`. |

Inherited from `DrylChartBase`: `Height`, `ShowLegend`, `ValueFormat`,
`AriaLabel`, `Class`, `AdditionalAttributes`, `Ai`, `Aura`. They are specified
in [`_Api.md`](_Api.md); the criteria below cover only what this component
decides.

The component takes no `Labels` and no `Series`: a segment carries its own label
in `ChartSegment.Label`, and there is no category axis to label.

The component exposes no `EventCallback`. Segments are focusable and
hoverable, but they are not selectable — the chart displays a split, it does not
collect a choice.

## Acceptance Criteria

### Rendering

- The component renders nothing when `Segments` is `null`.
- The component renders nothing when `Segments` is empty.
- The component renders nothing when no segment has a value greater than zero.
- The component renders one arc per segment whose value is greater than zero.
- A segment whose value is zero or negative is skipped entirely — no arc, no
  legend entry, no tooltip.
- Each arc's sweep is that segment's share of the total of the plotted segments.
- The arcs together cover the full circle, starting at twelve o'clock and
  running clockwise.
- A single plotted segment is drawn as a closed ring rather than as nothing.
- Each arc is filled in its segment's palette color, resolved from `ColorSlot`
  where set and from the segment's position in `Segments` otherwise.
- A skipped segment does not shift the colors of the segments after it, because
  the position used is the position in `Segments`.
- `Class` is merged onto the chart root's own classes.
- `AdditionalAttributes` are applied to the chart root.

### The hole and the centre slot

- `InnerRadius` defaults to a donut, not a pie.
- `InnerRadius` is clamped to the range 0–0.9, so a value outside it degrades
  instead of producing an inverted or invisible ring.
- `InnerRadius` set to `0` renders a filled pie.
- `CenterContent` is rendered inside the hole when `InnerRadius` is greater than
  `0`.
- `CenterContent` is not rendered when `InnerRadius` is `0`.
- The centre slot is sized to the hole, so its content never covers the ring.
- The centre slot does not intercept pointer events except on its own content,
  so hovering across the hole still reaches the segment beneath.

### Sizing

- The wheel is square: its height and width are equal at any container width.
- The wheel never exceeds the width of its container, so a donut in a narrow
  column shrinks instead of overflowing.
- The tooltip anchors and the hole size ride along with that shrink, staying
  aligned with the arcs.

### Legend

- The legend is shown by default, because the segments have no axis to name
  them.
- `ShowLegend` set to `false` hides the legend.
- The legend lists one entry per plotted segment, in the order the segments are
  plotted.
- Each legend entry shows a swatch in that segment's color next to its label.

### Values and locale

- A tooltip shows the segment's label, its formatted value and its share of the
  total as a percentage.
- Values are formatted culture-aware, per `ValueFormat` in
  [`_Api.md`](_Api.md).
- Every number written into SVG path data is formatted with the invariant
  culture, so path data parses under any thread culture.
- Every number written into a CSS custom property or an inline style is
  formatted with the invariant culture.

### Motion

- Segments fade and scale in on mount, over `--dur-med` with `--ease-out`.
- Segments enter staggered in plot order, so the ring builds up rather than
  appearing at once.
- Hovering a segment lifts it outward along its own mid-angle, over `--dur-fast`
  with `--ease-spring`.
- Focusing a segment produces the same outward lift as hovering it.
- The tooltip fades and slides in with `--dur-fast` and `--ease-out`.
- All animations and transitions are switched off under
  `prefers-reduced-motion: reduce`, leaving a complete, legible chart.
- The component has no exit animation of its own; a chart that is mounted
  conditionally is wrapped in `DrylPresence` by its host (`DESIGN-12`).

  The segment stagger step, the lift distance and the separator stroke width are
  written as literals. `DESIGN-10` binds the durations and easings, which are
  tokens; the per-item delay step is a stagger increment rather than a duration.
  Recorded here as documented debt, not as compliance.

### Keyboard and accessibility

- The chart root carries `role="group"`.
- The chart root carries an accessible label: `AriaLabel` when set, the
  comma-separated segment labels otherwise.
- Every plotted segment is reachable by `Tab`.
- A focused segment shows the same tooltip that hovering it shows, so the
  keyboard path is not a second-class one.
- A focused segment is outlined in `--accent-line`, replacing the surface-colored
  separator stroke on that segment only.
- Every segment carries `role="img"` and an accessible label naming its label,
  its formatted value and its percentage of the total.
- The tooltip itself is `aria-hidden`, so its content is announced once — through
  the segment's own label — and not twice.
- The legend is not focusable: it repeats what the segments already announce.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The segment colors come from the six chart palette slots (`--chart-1` …
  `--chart-6`), and a segment beyond the sixth renders in `--fg-dim` rather than
  cycling (see [`_Api.md`](_Api.md)).
- Segments are separated by a stroke in `--bg`, giving a gap of constant width at
  every radius.
- The tooltip is a floating panel: `--panel-solid` fill, `--line-strong` border,
  `--shadow-md`, `--r-sm` radius.
- The tooltip is centred on the segment's mid-angle anchor, so it points at the
  segment it describes.
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
  `node scripts/validate-light-contrast.mjs`. The separator stroke is `--bg`
  rather than a literal, so the gaps follow the mode instead of assuming a dark
  ground.
- **Enter/exit animation** — the staggered segment bloom is the entrance; the
  exit belongs to `DrylPresence` on the host's side, per the "Motion" criteria
  above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. Each
  segment is its own tab stop and announces its own share.
- **AI mode** — yes, via `DrylAiAware`, on the same terms as the rest of the
  chart family; the criteria under "AI mode" say what it looks like.
- **Demo page** — `DRYL.Website/Components/Pages/DemoDonutChart.razor`, with the
  examples `Components/Examples/DonutChart/Basic.razor` and
  `.../CenterSlot.razor`.
- **`ComponentCatalog`** — registered as `"Donut Chart"` / `donut-chart` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
