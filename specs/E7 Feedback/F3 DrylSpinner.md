# DrylSpinner

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylSpinner.razor
              code/DRYL.Components/Components/Feedback/DrylSpinner.razor.css

## User Story

As a Blazor developer, I want a compact loading indicator that fits inline in a
button, a row or a panel, and that changes its rhythm when the work behind it is
an AI thinking rather than a request loading, so that a user can feel what kind
of wait they are in without reading a label.

## Description

`DrylSpinner` is the smallest of the category's indicators: an inline-flex box
sized by one token, holding one of three animations. `Ring` is a rotating
gradient arc, `Dots` is a three-dot wave, `Pulse` is concentric rings expanding
from a core.

Its distinguishing behaviour is that **the AI state changes the spinner's own
rhythm, not only its aura.** Every other component in the category treats `Ai`
as a decoration applied around unchanged content. Here the loop itself
retimes — slower while a model is thinking, faster while it is streaming, and
stopped once it is done — so the same spinner communicates a different kind of
waiting in the same space.

The size system is one custom property per size on the wrapper. Every child
dimension derives from it, which is why a variant can change shape without any
size value being written twice.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `DrylSpinner.SpinnerVariant` | `SpinnerVariant.Ring` | Visual style. |
| `Size` | `DrylSpinner.SpinnerSize` | `SpinnerSize.Medium` | Physical size. |
| `Label` | `string?` | `null` | Accessible label. `null` uses a state-aware default. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state; also retimes the spinner's own loop. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the spinner's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the wrapper classes.
- The root carries the modifier class of its `Size`, one per value.
- The root carries the modifier class of its `Variant`, one per value.
- `SpinnerVariant.Ring` supplies the class for any value the switch does not
  match, so an unmapped value still renders a spinner.
- `SpinnerVariant.Ring` renders one arc element.
- `SpinnerVariant.Dots` renders exactly three dot elements.
- `SpinnerVariant.Pulse` renders two ring elements and one core element.
- The root is inline-flex, so a spinner sits on the text baseline row of a
  button or a label without breaking the line.
- The root does not shrink when placed in a flex row that runs out of space.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Size

- Each `SpinnerSize` value sets its own size custom property on the root.
- Every child dimension derives from that property, so no variant carries a
  second copy of a size value.
- Each `SpinnerSize` value sets its own track thickness, so a small ring stays
  proportionate rather than looking heavy.
- `SpinnerVariant.Dots` renders as a pill wider than it is tall, so the aura
  ring wraps a capsule rather than being cropped to a circle.
- Every other variant renders inside a circle.

### Keyboard and accessibility

- The root carries `role="status"`.
- The root carries `aria-live="polite"`, so a spinner appearing mid-page does
  not interrupt what the screen reader is saying.
- The root carries an accessible label.
- `Label` set wins over the state-aware default.
- `Label` left `null` yields a label describing the current `Ai` state, so a
  screen-reader user learns that a model is thinking rather than only that
  something is loading.
- `Label` left `null` with `Ai` at `AiState.None` yields the plain loading
  label.
- The spinner is not focusable and adds no stop to the tab order, because it is
  an indicator and not a control.
- The spinner's moving parts are hidden from assistive technology: the state is
  carried by the label, not by the animation.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The spinner paints no surface of its own — no fill, no border, no frost — so
  it inherits whatever ground it is placed on and `DESIGN-06` has nothing to
  apply to.
- `SpinnerVariant.Ring` draws its arc from `--accent-a` and `--accent-b`, with
  the centre masked out so only the track is painted.
- `SpinnerVariant.Dots` alternates its dots between `--accent-a` and
  `--accent-b`.
- `SpinnerVariant.Pulse` draws its rings from `--accent-a` and `--accent-b` and
  its core from `--accent-grad`.
- The accent appears as a thin arc, three small dots or a small core — never as
  the fill of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- Every variant animates continuously while mounted.
- `SpinnerVariant.Dots` staggers its three dots, so the wave reads as a sequence
  rather than three things blinking together.
- `SpinnerVariant.Pulse` staggers its two rings, so a second ring is always
  mid-flight when the first expires.
- The looping durations are chosen for the rhythm each variant needs and are
  deliberately not `--dur-*`: that scale governs transitions and one-shots,
  while continuous motion is free of it (`DESIGN-10`).
- The rotating variant uses `linear` timing, which `DESIGN-10` requires for
  anything that rotates — an eased rotation stutters once per revolution.
- Wherever an easing is applied at all, it is an easing token rather than a bare
  keyword (`DESIGN-10`).
- Under `prefers-reduced-motion: reduce` every loop stops.
- Under `prefers-reduced-motion: reduce` each variant is left in a legible
  resting state rather than blank, so the indicator still reads as present.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The spinner renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than a spinner-specific AI treatment (`AI-02`).
- `AiState.Thinking` slows the spinner's own loop relative to its default.
- `AiState.Streaming` speeds the spinner's own loop relative to its default.
- `AiState.Active` sets a rate between the two, so an idle-but-engaged model
  reads as different from both.
- `AiState.Generated` stops the loop and leaves the spinner in a settled state,
  so a finished operation stops asking for attention.
- Each of the three variants implements all four of those rate changes, so the
  signal does not depend on which variant a consumer picked.
- The rate change is carried by the animation's duration alone, so the spinner's
  geometry does not shift when the state changes.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- `AiState.Active` is signalled by an extra class the shared aura helper
  deliberately omits, because the generic aura treats `Active` as ring-only and
  the spinner needs the rate cue as well.

## Recorded gaps

- **A dozen and a half loose loop durations.** `DESIGN-10` leaves the duration
  of continuous motion free, so none of them is a violation — but three variants
  times "default plus four AI states", with the pulse variant timing its rings
  and its core separately, means sixteen hand-picked seconds values in
  `DrylSpinner.razor.css` with nothing relating them to each other. A fifth
  `AiState` would add three more the same way, and the ratios between "slower
  while thinking" and "faster while streaming" differ per variant.
- **`AiState.Active` needs an extra class the shared helper does not emit.** The
  component appends `ai-active` itself, so the AI-state contract of this
  component is one class wider than the shared vocabulary. Anything reusing the
  helper alone would miss the `Active` rate cue.
- **No tests of its own.** None of the criteria above is guarded by a test.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-a`, `--accent-b`,
  `--accent-grad` and `--glow-accent` are the mode-dependent tokens; the
  component defines no mode-specific rule.
- **Enter/exit animation** — none of its own, and that is the written exception
  `DESIGN-11` allows: a spinner *is* an animation, mounted and unmounted by its
  host, which wraps it in `DrylPresence` when its appearance should be animated
  too.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the state-aware default label: the AI state is
  announced in words, not only in motion.
- **AI mode** — yes, and further than anywhere else in the category: the
  component's own loop retimes per state, which is the cue a user reads before
  they read the aura.
- **Demo page** — `DRYL.Website/Components/Pages/DemoSpinner.razor`, with the
  examples `Components/Examples/Spinner/Variants.razor`, `.../Sizes.razor`,
  `.../AiMode.razor`, `.../InContext.razor` and `.../Live.razor`.
- **`ComponentCatalog`** — registered as `"Spinner"` / `spinners` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
