# DrylTableKpi

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTableKpi.razor

## User Story

As a Blazor developer, I want a row of summary figures above a table, so that a
reader sees what the data adds up to before they start reading rows.

## Description

`DrylTableKpi` is a single tile for `DrylTable`'s summary slot: a label, a large
value, an optional delta with a direction arrow, and an optional sparkline drawn
from raw numbers. Several tiles sit side by side in a grid the table provides,
separated by rules, and the tile is built to survive that — its label truncates
rather than wrapping and its content cannot widen its column.

It is a **near-duplicate of two other components in this category**, and the
spec says so rather than describing it as a design. Its label/value/delta half
repeats `DrylStat` (`F15`) with a second set of enums for the same concept, and
its sparkline half repeats `DrylSparkline` (`F14`) with a second implementation
of the same maths. That duplication is where every gap recorded below comes
from, including the one real defect.

Unlike `DrylStat` it takes no `Ai`, has no card of its own, and its value is
plain — no count-up.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string` | `""` (`EditorRequired`) | Short caption above the value. |
| `Value` | `string` | `""` (`EditorRequired`) | The value, pre-formatted. |
| `Delta` | `string?` | `null` | Delta text, e.g. `"+8.2%"`. |
| `DeltaKind` | `DrylTableKpi.KpiDeltaKind` | `KpiDeltaKind.Neutral` | Colour treatment of the delta. |
| `Trend` | `DrylTableKpi.KpiTrend` | `KpiTrend.None` | Direction arrow before the delta. |
| `SparklineData` | `IReadOnlyList<double>?` | `null` | Raw points for the mini chart; normalised automatically. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the tile's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the tile. |

Both enums are nested in `DrylTableKpi` and are therefore written qualified —
`DrylTableKpi.KpiTrend.Up`. Their members, and their relationship to
`DeltaDirection`, are set out in [`_Api.md`](_Api.md).

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a single tile element holding a label, a value and a
  footer.
- The footer holds the delta and the sparkline.
- `Delta` non-empty renders a delta element; empty or null renders none.
- `SparklineData` holding at least two points renders one `svg`; fewer renders
  none.
- The tile's gradient definitions carry an identifier unique per instance, so
  several tiles in one row do not share a definition.
- `Class` is merged onto the tile's own class rather than replacing it.
- `AdditionalAttributes` are applied to the tile.
- The tile may shrink below its content's intrinsic width and clips its
  overflow, so one long value cannot widen the summary row.
- The label truncates with an ellipsis rather than wrapping to a second line, so
  every tile in a row is the same height.
- The footer has a minimum height, so a tile without a delta or a chart aligns
  with the tiles beside it.
- The tile is separated from the next by a rule, and the last tile in a row
  carries none.

### The delta

- `KpiTrend.Up` renders an upward arrow before the delta text.
- `KpiTrend.Down` renders a downward arrow before the delta text.
- `KpiTrend.None` renders no arrow.
- The delta carries the modifier class of its `DeltaKind`, one per value.
- `DeltaKind` and `Trend` are independent, so a tile can show a downward arrow
  in the positive colour — which is what a falling latency needs.
- The delta is aligned to the bottom of the footer, so it sits on the same line
  as the base of the chart beside it.

### The sparkline

- The series is scaled between its own smallest and largest value rather than to
  zero.
- A series whose values are all equal is drawn without dividing by zero.
- The chart renders a filled area beneath a line, both derived from the same
  points.
- The area is closed along the bottom edge of the chart, so the fill has a flat
  base.
- Both axes are inset by a constant margin, so the stroke at the extreme is not
  cut by the edge.
- Every coordinate the chart emits is formatted with invariant culture, so a
  locale that uses a decimal comma cannot corrupt the SVG.
- The chart is hidden from assistive technology, because the value and the delta
  beside it already carry the information.

### Keyboard and accessibility

- The tile is not focusable and adds no stop to the tab order, because it is a
  read-only summary.
- The label and the value are rendered as text, so a screen reader announces
  them in that order.
- The trend arrow is decorative and is not part of the delta's accessible name.
- The delta's meaning is carried by its text and its arrow — the sign is in the
  string — rather than by colour alone.

### Appearance

- The label is set in `--fg-muted` and the value in `--fg`, so the number is the
  loudest thing on the tile.
- `KpiDeltaKind.Positive` draws the delta in `--success`.
- `KpiDeltaKind.Negative` draws the delta in `--danger`.
- `KpiDeltaKind.Neutral` draws the delta in `--fg-muted`.
- The delta is set in `--font-mono`, so a changing delta does not shift the
  footer.
- The value is set with tabular figures, so a value that changes in place does
  not shift the tile.
- The tile's separators come from `--line`.
- The tile paints no fill and no frost of its own, sitting on the summary bar
  the table provides (`DESIGN-06`).
- The tile's colours, **with the exception of the sparkline's gradient**, come
  from tokens; the exception is recorded as a violation below.

### Motion

- The sparkline is drawn at a reduced opacity and rises to full opacity when the
  tile is hovered.
- That transition runs at `--dur-med` with `--ease-out`.

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the tile is one cell of a summary bar
  belonging to `DrylTable`, and that table carries the `Ai` state for the data
  underneath it. Auras on each of four tiles above one table would be four
  signals for one activity. A consumer who wants a stat card that carries its
  own state uses `DrylStat` (`F15`), which does.

## Recorded gaps

- **Four hardcoded colours — and the enforcement grep cannot see them.** The
  sparkline's stroke gradient and its fill gradient are written as literal
  colour values in `DrylTableKpi.razor`. This is a flat `DESIGN-01` violation,
  and it is not documented debt: `DESIGN-01`'s Check line greps
  `code/*/**/*.razor.css` and reads **clean**, because these four literals live
  in a `.razor` file and not in a stylesheet. Searching `.razor` files under
  `code/` for colour literals returns exactly these four hits and nothing else
  in the library.

  The consequence is not that the colours are wrong today — they happen to
  equal the current accent values — but that they are *fixed*. `DrylThemeProvider`
  re-derives the accents at runtime from a consumer's seed colour, and every
  accent in the library follows except this chart, which keeps painting the
  library's default violet-to-cyan on a re-themed page.

  The stylesheet even contains the correct rule: `.tbl-kpi-sparkline polyline`
  sets its stroke from `--accent-b`. It has never had an effect, because the
  inline `style` attribute on the same element overrides it.
- **A second sparkline implementation.** The scaling, the point list and the
  area path are written again here rather than by placing a `DrylSparkline`,
  with a different margin, a different rounding precision, a different flat-series
  rule and no bar kind. Two implementations of one chart drift, and this one is
  the copy that is not on the docs site.
- **A second delta vocabulary.** `KpiDeltaKind` and `KpiTrend` together express
  what `DeltaDirection` expresses in one enum, and neither is convertible to the
  other. A consumer with a row of `DrylStat` cards and a table summary writes
  the same trend twice, in two types. Both are frozen by the 1.0 API freeze, so
  this is recorded rather than corrected.
- **No demo page and no catalog entry.** The component appears nowhere in
  `DRYL.Website` except inside one AI example, and there without
  `SparklineData` — so the chart with the hardcoded colours has never been
  rendered on the docs site at all. That is a `CODE-20` and a `REL-04` gap, and
  it is the direct reason the colour violation survived: nothing shows it.
- **The tile itself is not animated.** Only the sparkline's hover opacity moves.
  A tile appearing above a table, and a value changing in it, do so instantly
  (`DESIGN-11`, `DESIGN-12`).
- **The chart's geometry is literal, in the markup.** The chart's width, height
  and coordinate space are written into the `svg`'s attributes and repeated in
  the scaling maths, so the chart's size cannot be changed without editing four
  numbers in two places. Its type sizes and the footer's minimum height are
  literals in `dryl.css` (`DESIGN-01`).
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the invariant formatting.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — **not clean.** The tile's own text and separators are
  token-driven and verified by `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; `--fg`, `--fg-muted`, `--success`,
  `--danger` and `--line` are the mode-dependent tokens. The sparkline's four
  literal colours are outside that system entirely — they neither swap with the
  mode nor follow a runtime theme — and are recorded above as a `DESIGN-01`
  violation.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception; only the sparkline's hover opacity transitions.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the chart is hidden from assistive technology
  rather than given a label that would repeat the value beside it.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — **none.** Recorded above as a `CODE-20` gap; the component's
  only appearance in `DRYL.Website` is
  `Components/Examples/Ai/StreamingRows.razor`, which uses neither its delta nor
  its sparkline.
- **`ComponentCatalog`** — **not registered.** Recorded above as a `REL-04` gap.
  It is not reached through a family entry either: `DrylTable`'s entry does not
  mention it, and unlike the other member components in this category it is not
  shown on its parent's page.
