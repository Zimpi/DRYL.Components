# Charts — Public API

Shared enums, parameter contracts and services of the Charts category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Data/Charts/`

The category holds four components — `DrylLineChart`, `DrylBarChart`,
`DrylAreaChart` and `DrylDonutChart` — and they share more than they own. Two
record types carry the data, two base classes carry the parameters, and one
palette carries the colors. Everything below is the shared half; a component
spec adds only what is genuinely its own.

`DrylSparkline` is **not** in this category. It lives in `E5 Data` because its
source file does (`SPEC-02` derives the category from the path), and it shares
none of the types below.

## `ChartSeries`

One data series for the three cartesian charts. A `sealed record`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Name` | `string` | — | Series name, shown in the legend and in tooltip rows. |
| `Data` | `IReadOnlyList<double>` | — | The series values, one per category. |
| `ColorSlot` | `int?` | `null` | Pinned palette slot (1–6). Defaults to the series' position in the list. |

## `ChartSegment`

One segment of `DrylDonutChart`. A `sealed record`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string` | — | Segment name, shown in the legend and the tooltip. |
| `Value` | `double` | — | Segment value; its share of the total drives the sweep angle. |
| `ColorSlot` | `int?` | `null` | Pinned palette slot (1–6). Defaults to the segment's position in the list. |

`ColorSlot` exists for one reason on both types: a series or segment keeps its
color when the ones before it are filtered away. Without it, hiding the first
series silently recolors every remaining one.

## `DrylChartBase`

The abstract base every chart in this category derives from, directly
(`DrylDonutChart`) or through `DrylCartesianChartBase`. It derives from
`DrylAiAware`, so the `Ai` and `Aura` parameters below are the library's
standard AI opt-in (`AI-03`), not chart-specific.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Height` | `int` | `260` | Chart height in pixels. Width always fills the container. |
| `ShowLegend` | `bool?` | `null` | Legend visibility. `null` is automatic — see below. |
| `ValueFormat` | `string?` | `null` | Display format for axis ticks and tooltip values — see below. |
| `AriaLabel` | `string?` | `null` | Accessible summary label. Defaults to a generated description. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the chart's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the chart root. |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. AI ambient state; an explicit value wins over a surrounding `DrylAiScope`. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Pins the aura variant; `null` inherits the scope. |

`Height` is a raw `int` of pixels rather than a token, and that is deliberate:
the caller sizes a chart to its slot in *their* layout, which no library token
can know. It reaches CSS as the `--chart-h` custom property, never as a
hardcoded rule.

### `ShowLegend`

`null` — the default — resolves per component, because the useful default is
not the same for all four:

- The cartesian charts show the legend for two or more series and hide it for
  one, since a single series is already named by the surrounding title.
- `DrylDonutChart` shows it always: its segments have no axis to label them,
  so without the legend the colors mean nothing.

`true` and `false` are absolute in both cases.

### `ValueFormat`

One string, two accepted shapes:

- A .NET format string applied to the value directly — `"N0"`, `"C0"`, `"0.0"`.
- A template containing the `{value}` placeholder, where the formatted number is
  substituted in place — `"{value}%"`, `"€{value} k"`. The placeholder may carry
  an inner .NET format after a colon: `"{value:0.0}"`.

`null` formats with `"0.##"`. Display values are **culture-aware on purpose** —
they are read by a human. The numbers written into SVG path data, percentages
and custom properties are a separate concern and always invariant.

An unparseable format never throws. A model-invented specifier — `"K"` for
thousands, `"{value:Q}"` — falls back to the default numeric format, because a
chart showing an unstyled number beats a chart that takes the circuit down
mid-render.

## `DrylCartesianChartBase`

Derives from `DrylChartBase`; the base of `DrylLineChart`, `DrylBarChart` and
`DrylAreaChart`. Adds the axis half of the contract.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Series` | `IReadOnlyList<ChartSeries>?` | `null` | The series to plot. |
| `Labels` | `IReadOnlyList<string>?` | `null` | Category labels for the x-axis and the tooltip titles. |
| `ShowXAxis` | `bool` | `true` | Show the x-axis label row. |
| `ShowYAxis` | `bool` | `true` | Show the y-axis tick column. |
| `ShowGridLines` | `bool` | `true` | Show horizontal gridlines at the y-ticks. |
| `YMin` | `double?` | `null` | Fixed lower bound of the y-range. `null` is automatic. |
| `YMax` | `double?` | `null` | Fixed upper bound of the y-range. `null` is automatic. |

Shared behaviour the three cartesian charts inherit rather than restate:

- **The y-range** is taken from the data, widened to "nice" round tick values,
  and overridden per bound by `YMin`/`YMax` where they are set.
- **Missing labels** fall back to the 1-based category index, so `Labels` is
  optional and a short `Labels` list is not an error.
- **X labels are thinned** so they never collide; every category still gets its
  own hover column and tooltip.
- **A zero line** is drawn only when the range spans zero.
- **`AriaLabel` defaults** to the series names, comma-separated.

## The palette

Series and segment colors come from the six chart slots — `--chart-1` …
`--chart-6` in `code/DRYL.Components/wwwroot/dryl.css` — resolved from
`ColorSlot` where it is set and from list position otherwise. Slots 1 and 2 are
derived from the theme accents so a themed app's charts follow it; slots 3–6 are
fixed anchors, tuned per color mode.

**Beyond slot 6 the color stops.** A seventh series renders in `--fg-dim`
instead of cycling back to slot 1, because two series in the same color is a
misread chart, and a muted one is visibly "not in the palette". Six is the
documented ceiling of the vocabulary, not an accident of the token list.

## Internal, not public API

`ChartFrame`, `CartesianLayout`, `AxisTick`, `TooltipRow`, `LegendItem`,
`HoverColumn` and `ChartMath` live under
`code/DRYL.Components/Components/Data/Charts/Internal/` in the
`DRYL.Components.Internal` namespace. Several are `public` only because Blazor
requires a component `[Parameter]`'s type to be public. They carry no
compatibility promise and no spec of their own; the behaviour they implement is
specified as behaviour of the four components.
