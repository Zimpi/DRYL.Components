# DrylSparkline

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylSparkline.razor
              code/DRYL.Components/Components/Data/DrylSparkline.razor.css
              code/DRYL.Components/Components/Data/SparklineKind.cs

## User Story

As a Blazor developer, I want a trend drawn at the size of a line of text, so
that a KPI tile, a table cell or a list row can show the shape of its history
without becoming a chart.

## Description

`DrylSparkline` draws a series as an inline `svg` — a line, a filled area or a
row of bars — sized in pixels rather than laid out. It is the smallest member of
the library's charting surface and deliberately not part of it: the components
in `E4 Charts` have axes, legends, tooltips and a shared base class, and this
one has a path.

It is computed entirely on the server and rendered as markup. There is no
measurement, no interop and nothing to dispose, which is what makes it safe to
put a hundred of them in a table.

Every coordinate it emits is formatted with invariant culture. That is not a
detail: a German locale renders `0.5` as `0,5`, and an SVG point list containing
a comma where a decimal point belongs is silently mis-parsed. The component
formats every number through `FormattableString.Invariant`, including the ones
inside path data and rectangle attributes.

The series is scaled to its own extremes rather than to zero, so a sparkline
shows the *shape* of a movement rather than its magnitude — and a flat series is
drawn as a centred line rather than collapsing onto an edge.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Data` | `IReadOnlyList<double>?` | `null` | The series to plot. |
| `Kind` | `SparklineKind` | `SparklineKind.Line` | Render style. |
| `Width` | `int` | `96` | Width in pixels. |
| `Height` | `int` | `32` | Height in pixels. |
| `ShowLastDot` | `bool` | `false` | Marks the final point with a dot. Line and area only. |
| `AriaLabel` | `string` | `"Trend"` | Accessible label for the chart. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the sparkline's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the `svg`. |

`SparklineKind`'s members are listed in [`_Api.md`](_Api.md).

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- `Data` holding at least one value renders one `svg` element.
- `Data` null or empty renders nothing at all.
- The `svg` renders at `Width` and `Height`.
- The `svg`'s coordinate space matches `Width` and `Height`, so a unit of data
  space is a pixel.
- The `svg` declares one gradient whose identifier is unique per component
  instance, so two sparklines on one page do not share a definition.
- `Class` is merged onto the `svg`'s own class rather than replacing it.
- `AdditionalAttributes` are applied to the `svg`.
- The `svg` does not clip its content, so a stroke on the outer edge is not cut
  in half.

### Scaling

- The series is scaled between its own smallest and largest value rather than to
  zero.
- A series whose values are all equal is drawn on the vertical centre line.
- A series of one value is drawn at the horizontal centre.
- Both axes are inset by a constant padding, so a stroke at the extreme does not
  touch the edge of the box.
- The vertical scale is inverted, so a larger value is drawn higher.

### Drawing

- `SparklineKind.Line` renders one polyline through the points.
- `SparklineKind.Area` renders a filled path beneath the line **and** the line
  itself, so the area kind is the line kind with a fill rather than a different
  chart.
- The area path is closed along the baseline, so the fill has a flat bottom
  rather than following the line back.
- `SparklineKind.Bar` renders one rectangle per value and no line.
- Each bar is centred in its slot and narrower than it, so bars are separated
  without a gap value being written.
- A bar has a minimum width, so a long series does not render as invisible
  hairlines.
- A bar has a minimum height, so a value at the series minimum still renders a
  visible mark.
- A bar's corner radius never exceeds half its width, so a narrow bar does not
  render as a lozenge.
- `ShowLastDot` set renders one dot at the final point, for the line and area
  kinds.
- `ShowLastDot` set renders no dot for the bar kind.
- `ShowLastDot` left `false` renders no dot.

### Locale safety

- Every coordinate the component emits is formatted with invariant culture, so a
  locale that uses a decimal comma cannot corrupt the SVG.
- This holds for the polyline's point list, the area path's commands, every
  rectangle attribute and the dot's position alike.
- Coordinates are rounded to at most two decimals, so the emitted markup stays
  small.

### Keyboard and accessibility

- The `svg` carries `role="img"`, so it is announced as one image rather than as
  its shapes.
- The `svg` carries `AriaLabel` as its accessible label.
- `AriaLabel` has a non-null default, so a sparkline is never an unlabelled
  image.
- The sparkline is not focusable and adds no stop to the tab order, because it
  is a picture and not a control.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The line, the area and the bars are painted from a horizontal gradient running
  from `--accent-a` to `--accent-b`.
- The gradient's stops are set by classes in the stylesheet rather than by
  attributes, so the colours follow the theme rather than the markup.
- The last-point dot is filled with `--accent-b`.
- The area's fill is drawn at a low opacity, so the line stays the loudest part
  of the mark.
- The line's ends and joins are rounded, so a short series does not read as a
  spike.
- The sparkline paints no surface of its own — no fill behind the data, no
  border, no frost — so it inherits whatever ground it is placed on
  (`DESIGN-06`).
- The accent appears as a hairline, a faint fill or a row of small bars — never
  as the fill of a large surface (`DESIGN-08`).
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the sparkline is a mark inside another
  surface — a `DrylStat`, a table cell, a list row — and every one of those
  carries its own `Ai`. An aura at this size would be larger than the data it
  surrounded, and it would compete with the aura of the tile it sits in.

## Recorded gaps

- **Nothing is animated.** The sparkline has no draw-in, no transition when the
  data changes and no enter or exit (`DESIGN-11`, `DESIGN-12`). Its larger
  siblings in `E4 Charts` animate their paths in; this one, which is most often
  bound to live data, redraws by snapping. It is not wrapped in `DrylPresence`
  and does not wrap itself in one.
- **An empty series renders nothing, not an empty box.** A sparkline whose data
  has not arrived occupies no space and then occupies `Width` by `Height`, which
  shifts whatever is beside it. Returning an empty frame of the same size would
  cost nothing.
- **The label describes the chart, not the data.** `AriaLabel` defaults to a
  generic word and nothing derives anything from the series, so a screen-reader
  user learns that there is a trend and never learns which way it goes. The
  component knows the first value, the last value and both extremes at render
  time.
- **The geometry is written in literals across two files.** The stroke width and
  the area's opacity are in `DrylSparkline.razor.css`; the padding, the minimum
  bar width, the minimum bar height, the bar's share of its slot, the corner cap
  and the dot's radius are constants and inline numbers in the component
  (`DESIGN-01`). `Width` and `Height` are `int` pixels, the same gap `F10`
  records for icons.
- **The bounds are computed more often than needed.** Every drawing helper
  recomputes the series' extremes with its own pass, and the helper that returns
  the last point's horizontal position computes them and discards the result.
  Harmless at this size, and pointless at any size.
- **No tests of its own.** None of the criteria above is guarded by a test — in
  particular not the invariant formatting, which is this repository's
  best-documented recurring failure and the reason the component's own header
  comment calls it out.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-a` and `--accent-b` are
  the mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the mark is always labelled; the substantive
  omission is that the label says nothing about the data, recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoSparkline.razor`, with the
  examples `Components/Examples/Sparkline/Kinds.razor`, `.../Sizes.razor` and
  `.../EdgeCases.razor`.
- **`ComponentCatalog`** — registered as `"Sparkline"` / `sparkline` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
