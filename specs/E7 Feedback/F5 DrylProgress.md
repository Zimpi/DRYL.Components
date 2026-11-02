# DrylProgress

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylProgress.razor
              code/DRYL.Components/Components/Feedback/ProgressVariant.cs

## User Story

As a Blazor developer, I want a linear progress bar that takes my own scale
rather than forcing me to convert to percent, that can also say "working, no
idea how long", and that reads the same to a screen reader as it does on screen,
so that I can show an upload, a quota or a model's progress without writing any
of the arithmetic or the ARIA myself.

## Description

`DrylProgress` is a row of glass cells, optionally preceded by a label row that
carries a caption on the leading edge and the percentage on the trailing one.
One gradient runs across every cell, so the row reads as one bar broken into
glass; the cells fill one after another and the leading lit cell is filled
partially, so the bar keeps the precision of a continuous fill (I36).
`Value` is expressed on the consumer's own scale — three of five steps is
`Value="3" Max="5"` — and the component derives the percentage, clamps it and
formats it.

When `Max` is a whole number from 1 to 12, every unit is a cell — three of
five steps lights three of five cells. Any other scale is drawn on 20 cells.

Two structural decisions are worth naming. Every number written into the
markup — the cell fill fraction, the cell count — is formatted with
`InvariantCulture`, because a fill of `0,333` is not a CSS number and would
silently empty the cell under a German locale; there is a regression test for
exactly that. And each cell clips its own fill, so the track itself clips
nothing and the AI aura, hosted on a wrapper around the track, is never cut
off.

`Indeterminate` replaces the fill with a wave of light rolling through the
cells and removes the value from the component's ARIA, so nothing claims a
number that does not exist.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `double` | `0` | Current value, on the consumer's scale. |
| `Max` | `double` | `100` | Upper bound of that scale. A whole number from 1 to 12 also sets the number of cells. |
| `Indeterminate` | `bool` | `false` | Shows a rolling wave instead of a fill. |
| `Variant` | `ProgressVariant` | `ProgressVariant.Accent` | Color treatment of the fill. |
| `Size` | `ProgressSize` | `ProgressSize.Medium` | Track thickness. |
| `ShowLabel` | `bool` | `false` | Shows the percentage on the trailing edge of the label row. |
| `LabelText` | `string?` | `null` | Plain-text caption on the leading edge. |
| `Label` | `RenderFragment?` | `null` | Custom caption; overrides `LabelText`. |
| `AriaLabel` | `string?` | `null` | Accessible label for the bar. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. `AiState.None` inherits a surrounding `DrylAiScope`. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the progress field's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Label` and `LabelText` are two parameters for one slot because the common case
is a string and the general case is markup. `ShowLabel` is independent of both:
a bar may show its percentage with no caption, a caption with no percentage,
both, or neither.

## Acceptance Criteria

### Structure

- The component renders a root field containing an optional label row and the
  track.
- The label row is rendered when `Label` is set.
- The label row is rendered when `LabelText` is non-empty.
- The label row is rendered when `ShowLabel` is `true` and `Indeterminate` is
  `false`.
- The label row is not rendered when none of those three conditions holds, so a
  bare bar occupies only the track's height.
- The caption slot renders `Label` when it is set.
- The caption slot renders `LabelText` when `Label` is `null` and `LabelText` is
  non-empty.
- The percentage is rendered only when `ShowLabel` is `true` and `Indeterminate`
  is `false`.
- The caption sits on the leading edge and the percentage on the trailing edge
  of the label row.
- The track is a row of cells. When `Max` is a whole number from 1 to 12 it
  has `Max` cells; otherwise it has 20.
- The track carries its cell count, and every cell carries its index, so one
  gradient can be laid across the whole row.
- Each cell clips its own fill, so the fill cannot spill past the cell's
  rounded ends.
- The track clips nothing, and the aura host sits outside it, so the aura is
  not clipped.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Value and scale

- The rendered percentage is `Value` relative to `Max`.
- A `Value` above `Max` renders as a full bar rather than overflowing.
- A `Value` below zero renders as an empty bar rather than a negative fill.
- A `Max` of zero or below renders as an empty bar and throws nothing.
- The percentage in the label row is rounded to whole percent.
- Cells fill in order from the leading edge. Every cell below the value is
  full, the cell the value falls in is filled by the fraction it covers, and
  every cell after it is empty.
- A cell's fill fraction keeps three decimal places, so the leading cell does
  not visibly quantise.
- Both are formatted with the invariant culture, so a locale whose decimal
  separator is a comma still produces a valid CSS number and a stable label.

### Variants and size

- Each `ProgressVariant` value renders its own fill treatment.
- `ProgressVariant.Accent` is the unmodified fill and adds no modifier class.
- `ProgressVariant.Success`, `Warning` and `Danger` each paint a gradient that
  starts at the matching semantic token and stays within its hue.
- Each `ProgressSize` value renders its own track thickness.
- `ProgressSize.Medium` is the unmodified track and adds no modifier class.
- The track's height is the only thing `Size` changes, so a caption's type size
  does not move with it.

### Indeterminate

- `Indeterminate` lights no cell by value; a wave of light rolls through the
  cells instead.
- `Indeterminate` never renders the bar as complete, whatever `Value` is.
- `Indeterminate` renders no percentage, even when `ShowLabel` is `true`.
- `Indeterminate` still renders the label row when a caption is supplied, so a
  bar can say what it is working on without claiming how far along it is.
- Switching from indeterminate to determinate stops the wave and shows the
  filled cells.

### Keyboard and accessibility

- The track carries `role="progressbar"`.
- The track carries a minimum of zero.
- The track carries `Max` as its maximum, formatted with the invariant culture.
- A determinate bar carries the value it draws as its current value, formatted
  with the invariant culture.
- A `Value` above `Max` is reported as `Max`, so the bar never announces more
  than the scale it was given.
- A `Value` below zero is reported as zero.
- The reported value, the rendered percentage and the cells' fill are derived
  from the same clamped number, so what is drawn and what is announced cannot
  disagree.
- An indeterminate bar carries no current value, so assistive technology
  announces it as busy rather than as a number.
- The track carries `AriaLabel` as its accessible label.
- The bar is not focusable and adds no stop to the tab order, because it is an
  indicator and not a control.
- The percentage text is rendered as text, so it is announced without depending
  on the ARIA value.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- Each cell paints `--glass-2` with a 1px `--line` edge, and the accent fill
  is a gradient from `--accent-a` to `--accent-b` laid across the whole row.
- A lit cell carries a glow derived from the fill's own color rather than a
  second surface (`DESIGN-08`).
- Cells are `--r-pill`, so a partially filled cell has rounded ends on both
  sides; cells are spaced by `--sp-1`.
- The component paints no frost: the track is a small in-flow element on
  whatever ground it sits, and `DESIGN-07` reserves frost for surfaces that can
  show it.
- The caption is `--fg-muted` and the percentage is `--fg`, so the number is the
  louder of the two.
- The percentage is set in `--font-mono`, so a rising number does not shift the
  label row's width digit by digit.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- A cell's fill grows and shrinks by transform over `--dur-med` with
  `--ease-out`, so it stays on the compositor.
- A change of `Value` staggers the cells it changes by `--reveal-step`,
  counting outward from the previous leading edge — forward when the value
  rises, backward when it falls. A render that does not change the value
  keeps the stagger it had.
- A freshly mounted bar starts from an empty row, so its cells fill in from
  the left.
- A cell that becomes full springs up once over `--dur-slow` on
  `--ease-spring`, after its stagger delay.
- The leading lit cell breathes continuously while the bar is determinate and
  not complete.
- A bar that reaches its maximum is marked complete and flashes once across
  the row over `--dur-choreo`, staggered by `--reveal-step` per cell.
- The indeterminate wave animates continuously while mounted, staggered by
  `--reveal-step` per cell, and carries no fill transition.
- Under `prefers-reduced-motion: reduce` every transition and animation stops,
  and the indeterminate bar rests as a fully lit row, so a busy state is still
  visible without motion.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- A component that leaves `Ai` at `AiState.None` shows the surrounding
  `DrylAiScope`'s state, including the one-shot `AiState.Generated` reveal on an
  inherited move into it; an explicit `Ai` wins over the scope.
- The bar renders the shared aura vocabulary — ring, comet, glow, wash — rather
  than a progress-specific AI treatment (`AI-02`).
- The aura traces the track's pill shape rather than a rectangle around it.
- The aura is independent of `Variant` and `Indeterminate`, so any combination
  renders.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- The AI state changes nothing about the bar's layout, so the label row does not
  move when an operation starts or ends.
- `AiState.Proposed` renders exactly like `AiState.None`: a progress
  bar reports how far work has come, and nothing on it waits for a human
  decision.

## Recorded gaps

- **`AriaLabel` has no default.** A bar with no `AriaLabel` and no caption is
  announced as a progress bar with a number and no subject. Every other
  indicator in this category falls back to a state-aware label; this one does
  not.
- **The percentage is not announced as it changes.** The label row carries no
  live region, so a screen-reader user hears the value only when they navigate
  to the bar. `role="progressbar"` covers the value; the visible text is
  silent.
- **The track heights are literals** in
  `code/DRYL.Components/wwwroot/dryl.css` — one per `ProgressSize`. `DESIGN-01`
  covers colors, radii, shadows, durations and easings, which are tokens here;
  a track's thickness is not covered by a token today. Recorded as debt, not as
  compliance.
- **The label row's type sizes are literals** in the same file.
- **The cell rhythm's literals.** The breathing and wave loop periods, the
  pop height, the flash brightness and the wave's resting opacity are
  literals in `code/DRYL.Components/wwwroot/dryl.css`; continuous loops are
  outside the duration scale (`DESIGN-10`), but they cannot be retuned from a
  token. The lit cell's glow radius is a literal, as the bar's glow was.
- **The cell count is not configurable.** A scale other than 1–12 whole units
  always draws 20 cells, however wide or narrow the bar is.
- **The motion is unguarded by tests.** Tested: the reported value and its
  clamping, the cell count, the fill order and partial leading cell, the
  leading and complete markers, the stagger order, the variants, the sizes,
  the label row and the indeterminate rendering in
  `tests/DRYL.Components.Tests/DrylProgressTests.cs`; the invariant-culture
  fill fraction in `tests/DRYL.Components.Tests/GlobalizationTests.cs`; the
  class merge in `tests/DRYL.Components.Tests/ClassMergeTests.cs`. What the
  CSS does with those markers is checked by eye only.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-2`, `--accent-a`, `--accent-b` and
  the three semantic fills are the mode-dependent tokens; the component defines
  no mode-specific rule.
- **Enter/exit animation** — a mounted bar fills its cells in from the left,
  staggered, and every later change staggers from the leading edge; the
  indeterminate wave is continuous. There is no exit animation, the written
  exception `DESIGN-11` allows for an in-flow indicator its host mounts; a
  host that wants it to leave gradually wraps it in `DrylPresence`.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above,
  including the deliberate absence of an ARIA value while indeterminate, and
  the clamp that keeps the announced value equal to the drawn one. The missing
  default label is recorded as a gap rather than glossed.
- **AI mode** — yes. A progress bar is the natural place to show a model's
  long-running work, and the aura is hosted outside the clipping track so it is
  actually visible there.
- **Demo page** — `DRYL.Website/Components/Pages/DemoProgress.razor`, with the
  examples `Components/Examples/Progress/Determinate.razor`,
  `.../Indeterminate.razor`, `.../Variants.razor`, `.../Sizes.razor` and
  `.../AiMode.razor`, `.../Steps.razor`.
- **`ComponentCatalog`** — registered as `"Progress"` / `progress` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
