# DrylStat

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylStat.razor
              code/DRYL.Components/Components/Data/DrylStat.razor.css
              code/DRYL.Components/Components/Data/DeltaDirection.cs

## User Story

As a Blazor developer, I want one number presented as the headline of a card,
with its label, its movement and its recent history, so that a dashboard reads
as a set of answers rather than as a table of figures.

## Description

`DrylStat` is a glass card built around a single value. Above it sits a label
with an optional icon; beside it, an optional delta chip whose colour and arrow
follow `Direction`; below it, an optional sparkline slot pinned to the bottom of
the card so a row of stats lines up whether or not each one has a chart.

The value is a **pre-formatted string**, not a number. That is deliberate: a KPI
is formatted by the application that owns it — currency, units, thousands
separators, culture — and a component that took a `double` would have to guess
all four.

Two behaviours are worth naming. `CountUp` tweens the headline number rather
than snapping to it, counting from zero on the first render and from the
previous number afterwards; the tween runs in JS, rewrites the span's text
between renders and **always lands on exactly the string Blazor rendered**, so
the DOM a test or a screen reader sees is the real value with or without it. And
`Ai` gives the card the shared aura, because a KPI is one of the values a model
most often produces.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Caption for the metric. |
| `Value` | `string?` | `null` | The headline value, pre-formatted. |
| `Icon` | `string?` | `null` | `DrylIcon` name shown before the label. |
| `Delta` | `string?` | `null` | Delta text, e.g. `"+12.4%"`. |
| `Direction` | `DeltaDirection` | `DeltaDirection.None` | Trend; drives the delta's colour and arrow. |
| `Sparkline` | `RenderFragment?` | `null` | Slot for a trend chart, typically a `DrylSparkline`. |
| `CountUp` | `bool` | `false` | Tweens the headline value instead of snapping to it. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the card's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`DeltaDirection`'s members are listed in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the library's card
  classes and the stat's own.
- The root holds a head row, a value row and, when the slot is filled, a
  sparkline area.
- `Icon` set renders one `DrylIcon` before the label.
- `Icon` unset renders no icon element.
- `Label` is rendered in the head row.
- `Value` is rendered in the value row.
- `Sparkline` set renders the slot's content in its own area.
- `Sparkline` unset renders no sparkline area.
- The sparkline area is pushed to the bottom of the card, so cards of differing
  content still align their charts.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- The card has a minimum height, so a stat with no delta and no chart is not
  shorter than its neighbours.

### The delta

- The delta chip is rendered only when `Delta` is non-empty **and** `Direction`
  is not `DeltaDirection.None`.
- `DeltaDirection.Up` renders an upward arrow before the delta text.
- `DeltaDirection.Down` renders a downward arrow before the delta text.
- `DeltaDirection.Neutral` renders no arrow.
- The chip carries the modifier class of its `Direction`, one per value.
- The chip sits on the value's text baseline, so a long value and its delta read
  as one line.
- The chip wraps onto its own line rather than overflowing when the value is
  wide.

### Counting up

- `CountUp` left `false` makes no interop call at all.
- `CountUp` set to `true` requests one tween on the first render.
- A further tween is requested only when `Value` actually changes, so a
  re-render with an unchanged value does not replay the count.
- The rendered markup is identical with and without `CountUp`, so the value a
  test or a screen reader reads never depends on JS having run.
- Switching `CountUp` on later starts the tween from the value currently shown
  rather than replaying the whole history as one jump.
- A tween requested after the circuit has disconnected is abandoned silently
  rather than throwing.
- A tween requested during prerender is abandoned silently rather than throwing.

### Keyboard and accessibility

- The card is not focusable and adds no stop to the tab order, because it is a
  read-only metric.
- The label and the value are rendered as text, so a screen reader announces
  them in that order.
- The delta's arrow is decorative and is not part of the chip's accessible name,
  so the delta is announced as its text.
- The delta's direction is carried by its arrow and its text — the sign is in
  the string — rather than by colour alone.
- Every aura layer is hidden from assistive technology.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The card's fill, border and frost are the library's shared card treatment
  rather than the component's own.
- The label is set in `--fg-muted` and the value in `--fg`, so the number is the
  loudest thing on the card.
- The icon is drawn in `--accent-a`.
- `DeltaDirection.Up` draws its chip in `--success`.
- `DeltaDirection.Down` draws its chip in `--danger`.
- `DeltaDirection.Neutral` draws its chip in `--fg-dim`.
- The delta is set in `--font-mono`, so a changing delta does not shift the row.
- The value is set with negative letter-spacing and a tight line height, so a
  large number reads as a headline rather than as body text.
- The accent appears as the icon, the aura's ring and glow, and whatever the
  sparkline slot draws — never as the fill of the card (`DESIGN-08`).
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The component renders the shared aura vocabulary through the shared helper
  rather than a stat-specific AI treatment (`AI-02`).
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- The aura lifecycle's timer is disposed with the component.

## Recorded gaps

- **The card has no enter or exit animation of its own.** The aura animates, the
  value can count up, and the card itself appears and disappears instantly
  (`DESIGN-11`, `DESIGN-12`). A dashboard whose stats arrive one by one — the
  case `Ai` exists for — pops them in.
- **The delta does not animate either.** A KPI whose trend flips from `Up` to
  `Down` swaps its colour and its arrow between two frames, with no transition
  on the one element whose whole job is to say that something moved.
- **`Value` is a string, so the count-up has to re-parse it.** The tween finds
  the first number in the rendered string and animates that, which is what
  allows a currency symbol or a unit to ride along — and also means a value with
  two numbers in it (`"3 of 12"`) counts the wrong one, and a value with none
  counts nothing.
- **The card's type sizes and its minimum height are literal.** The label's
  `12px`, the value's `32px`, the delta's `11px` and the card's `132px` floor
  are written into `DrylStat.razor.css` with no token behind them
  (`DESIGN-01`). The gaps *are* tokens, so the file is half-converted rather
  than untouched.
- **Only the count-up is tested.** `tests/DRYL.Components.Tests/DrylStatCountUpTests.cs`
  guards the interop contract thoroughly; nothing guards the delta's render
  condition, the direction mapping or the sparkline slot.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-a`, `--fg`,
  `--fg-muted`, `--fg-dim`, `--success` and `--danger` are the mode-dependent
  tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent** for the card, and recorded above as debt
  rather than as an exception; the aura's enter, dissolve and completion wash
  are specified above, and the optional count-up is the value's own motion.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the tween never changes what the DOM says, so the
  count-up is invisible to assistive technology by design rather than by
  accident.
- **AI mode** — yes. A KPI is a value a model produces, and the card carries
  `Ai` and `Aura` and renders the shared vocabulary.
- **Demo page** — `DRYL.Website/Components/Pages/DemoStat.razor`, with the
  examples `Components/Examples/Stat/DashboardGrid.razor`, `.../CountUp.razor`
  and `.../AiMode.razor`.
- **`ComponentCatalog`** — registered as `"Stat"` / `stat` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylStatCountUpTests.cs` guards that
  the count-up is opt-in, that it fires once on first render and that it fires
  again only on a real value change; the `Class` merge is guarded in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`.
