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

`DrylProgress` is a track with a fill, optionally preceded by a label row that
carries a caption on the leading edge and the percentage on the trailing one.
`Value` is expressed on the consumer's own scale — three of five steps is
`Value="3" Max="5"` — and the component derives the percentage, clamps it and
formats it.

Two structural decisions are worth naming. The percentage is computed and
rendered with `InvariantCulture`, because a fill width of `33,33%` is not a CSS
length and would silently collapse the bar under a German locale; there is a
regression test for exactly that. And the AI aura is hosted on a wrapper
*around* the track rather than on the track itself, because the track clips its
own overflow to keep the fill inside its rounded ends — an aura painted there
would be cut off at the same edge.

`Indeterminate` replaces the fill with a sweep and removes the value from the
component's ARIA, so nothing claims a number that does not exist.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `double` | `0` | Current value, on the consumer's scale. |
| `Max` | `double` | `100` | Upper bound of that scale. |
| `Indeterminate` | `bool` | `false` | Shows a sweep instead of a fill. |
| `Variant` | `ProgressVariant` | `ProgressVariant.Accent` | Color treatment of the fill. |
| `Size` | `ProgressSize` | `ProgressSize.Medium` | Track thickness. |
| `ShowLabel` | `bool` | `false` | Shows the percentage on the trailing edge of the label row. |
| `LabelText` | `string?` | `null` | Plain-text caption on the leading edge. |
| `Label` | `RenderFragment?` | `null` | Custom caption; overrides `LabelText`. |
| `AriaLabel` | `string?` | `null` | Accessible label for the bar. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
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
- The track wraps the fill and clips it, so the fill cannot spill past the
  track's rounded ends.
- The aura host sits outside the track, so the aura is not clipped by that
  same rule.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Value and scale

- The rendered percentage is `Value` relative to `Max`.
- A `Value` above `Max` renders as a full bar rather than overflowing.
- A `Value` below zero renders as an empty bar rather than a negative fill.
- A `Max` of zero or below renders as an empty bar and throws nothing.
- The percentage in the label row is rounded to whole percent.
- The fill's width keeps two decimal places, so a long bar does not visibly
  quantise.
- Both are formatted with the invariant culture, so a locale whose decimal
  separator is a comma still produces a valid CSS length and a stable label.

### Variants and size

- Each `ProgressVariant` value renders its own fill treatment.
- `ProgressVariant.Accent` is the unmodified fill and adds no modifier class.
- `ProgressVariant.Success`, `Warning` and `Danger` each derive their fill from
  the matching semantic token.
- Each `ProgressSize` value renders its own track thickness.
- `ProgressSize.Medium` is the unmodified track and adds no modifier class.
- The track's height is the only thing `Size` changes, so a caption's type size
  does not move with it.

### Indeterminate

- `Indeterminate` replaces the fixed fill with a sweep that traverses the track.
- `Indeterminate` renders no percentage, even when `ShowLabel` is `true`.
- `Indeterminate` still renders the label row when a caption is supplied, so a
  bar can say what it is working on without claiming how far along it is.
- Switching from indeterminate to determinate stops the sweep and shows the
  fill.

### Keyboard and accessibility

- The track carries `role="progressbar"`.
- The track carries a minimum of zero.
- The track carries `Max` as its maximum, formatted with the invariant culture.
- A determinate bar carries `Value` as its current value, formatted with the
  invariant culture.
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
- The track paints `--glass-2` and the accent fill paints `--accent-grad`.
- The fill carries a glow derived from its own color rather than a second
  surface (`DESIGN-08`).
- The track and the fill are `--r-pill`, so a partially filled bar has rounded
  ends on both sides.
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

- A change of `Value` animates the fill's width over `--dur-slow` with
  `--ease-out`, so progress glides rather than jumping.
- The indeterminate sweep animates continuously while mounted.
- The indeterminate sweep animates its position by transform rather than by
  width, so it stays on the compositor.
- The indeterminate bar carries no width transition, so the sweep is not fought
  by the determinate glide.
- Under `prefers-reduced-motion: reduce` the indeterminate sweep stops and the
  bar rests as a full track, so a busy state is still visible without motion.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
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

## Recorded gaps

- **`AriaLabel` has no default.** A bar with no `AriaLabel` and no caption is
  announced as a progress bar with a number and no subject. Every other
  indicator in this category falls back to a state-aware label; this one does
  not.
- **The ARIA value is not clamped.** The fill is clamped into 0…100 %, but the
  reported current value is `Value` as given. A `Value` of 120 against a `Max`
  of 100 shows a full bar and tells a screen reader "120 of 100" — the two
  halves of the criterion "a value above `Max` renders as full" disagree.
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
- **The indeterminate sweep's width and travel are literals**, so its rhythm
  cannot be retuned from a token.
- **Only two of its criteria are guarded by tests**: the invariant-culture fill
  width in `tests/DRYL.Components.Tests/GlobalizationTests.cs`, and the class
  merge in `tests/DRYL.Components.Tests/ClassMergeTests.cs`. Nothing else above
  is covered.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-2`, `--accent-grad` and
  the three semantic fills are the mode-dependent tokens; the component defines
  no mode-specific rule.
- **Enter/exit animation** — the determinate fill's width glide is the
  component's own state animation, and the indeterminate sweep is continuous.
  There is no mount animation, the written exception `DESIGN-11` allows for an
  in-flow indicator its host mounts; a host that wants it to appear gradually
  wraps it in `DrylPresence`.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above,
  including the deliberate absence of an ARIA value while indeterminate. The
  missing default label is recorded as a gap rather than glossed.
- **AI mode** — yes. A progress bar is the natural place to show a model's
  long-running work, and the aura is hosted outside the clipping track so it is
  actually visible there.
- **Demo page** — `DRYL.Website/Components/Pages/DemoProgress.razor`, with the
  examples `Components/Examples/Progress/Determinate.razor`,
  `.../Indeterminate.razor`, `.../Variants.razor`, `.../Sizes.razor` and
  `.../AiMode.razor`.
- **`ComponentCatalog`** — registered as `"Progress"` / `progress` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
