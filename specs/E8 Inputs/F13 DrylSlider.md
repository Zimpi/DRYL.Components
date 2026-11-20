# DrylSlider

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylSlider.razor
              code/DRYL.Components/Components/Inputs/DrylSlider.razor.css

## User Story

As a Blazor developer, I want a range slider bound like any other input, so that
a user can pick a number on a scale by dragging.

## Description

`DrylSlider` inherits `InputBase<double>`. It renders a native
`input type="range"` with an accent gradient that fills the track up to the
knob, an optional label and value readout above, and helper or validation text
below.

The track is a thin line and the component has no surface of its own. In AI mode
it therefore draws no aura frame (`AI-08`): a light ring around the knob and the
color of the fill show the state.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<double>` and implements
`IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Label above the slider. |
| `HelperText` | `string?` | `null` | Text below the slider, replaced by validation messages. |
| `Min` | `double` | `0` | Lowest value. |
| `Max` | `double` | `100` | Highest value. |
| `Step` | `double` | `1` | Distance between two values. |
| `ShowValue` | `bool` | `true` | Shows the current value at the trailing edge of the label row. |
| `Disabled` | `bool` | `false` | Greys the slider out and blocks input. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is not set. |
| `Ai` | `AiState` | `AiState.None` | AI state, shown by the knob and the fill. |
| `Aura` | `AiAura?` | `null` | Has no visible effect; the slider draws no aura frame. |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Applied to the range input. |

## Acceptance Criteria

### Structure and value

- The component renders one `input type="range"` carrying `Min`, `Max` and
  `Step`.
- Dragging the knob writes the new value through `ValueChanged` on every input
  event.
- The fill ends at the knob for every value between `Min` and `Max`.
- A value outside `Min`…`Max` draws the fill clamped to the track.
- Every number written into the markup uses the invariant culture, so a comma
  culture cannot produce an invalid attribute or CSS value.
- A nonempty `Label` renders above the slider and is tied to the input.
- `ShowValue` renders the current value in the label row.
- The label row is not rendered when `Label` is empty and `ShowValue` is false.
- `Disabled` disables the input.
- A value that cannot be parsed reports "The field must be a valid number.".
- A component used outside an `EditForm` binds `Value` without throwing.

### Validation and helper

- Validation messages for the bound field replace `HelperText`, joined by a
  space, in `.input-helper.input-helper-error`.
- Without a message, a nonempty `HelperText` renders in `.input-helper`.
- A displayed helper or message is referenced by the input's
  `aria-describedby`.
- A validation message turns the fill to `--danger`.

### Keyboard and accessibility

- The input is the native range control, so the arrow keys, `Home`, `End` and
  the page keys move the knob as the browser defines.
- The input carries `aria-valuemin`, `aria-valuemax` and `aria-valuenow`.
- The input is named by `AriaLabel` when `Label` is not set.
- A focused knob grows and shows a ring derived from `--accent-a`.
- The AI ring and the sheen are hidden from assistive technology and take no
  pointer events, so they never intercept a drag.

### Appearance and motion

- The fill is a gradient from `--accent-a` to `--accent-b`.
- The unfilled track uses `--glass-3`.
- The knob uses `--knob`.
- The knob's focus change runs over `--dur-fast`.
- A disabled slider is dimmed and shows the not-allowed cursor.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in (`AI-03`).
- The slider renders no aura layer — no ring, comet, glow or wash — because the
  track has no surface of its own for one to trace (`AI-08`).
- The wrapper around the input carries the shared state classes.
- The wrapper gains no padding, radius or fill in AI mode, so entering AI mode
  moves nothing.
- `Aura` has no visible effect.
- While an AI state is shown, the shared `.ai-mark-ring` is rendered centred on
  the knob.
- The ring follows the knob for every value between `Min` and `Max`.
- Without an AI state no ring and no sheen element is rendered.
- Under `AiState.Active`, `AiState.Thinking` and `AiState.Streaming` the ring
  breathes at the state's halo period.
- Under `AiState.Thinking` and `AiState.Streaming` the fill takes the state's
  weighting of `--ai-a` and `--ai-b`.
- Under `AiState.Streaming` a sheen runs through the fill in reading direction
  and repeats.
- Entering `AiState.Generated` lets the ring run out once over `--dur-choreo`.
- `AiState.Generated` retires by itself through `AuraLifecycle` (`AI-07`).
- Under `AiState.Proposed` the ring rests around the knob and nothing loops
  (`AI-06`).
- Under `AiState.Proposed` the component renders the visually hidden text
  "AI suggestion, awaiting your decision".
- Leaving AI mode fades the ring over `--dur-slow` before it is removed.
- Under `prefers-reduced-motion: reduce` the ring is still and no sheen is
  shown.
- Disposing the component cancels its pending aura work.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope, and the variant has no visible effect
  here.
- **The knob size, the track height and the input's margin are literals** in
  `DrylSlider.razor.css`; the AI ring's position is computed from the same
  literals.
- **The sheen spans the fill from the track's start**, while the knob's centre
  sits half a knob inside the track, so the sheen's end and the knob can differ
  by up to half a knob.
- **The label carries an inline style** resetting its bottom margin.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/slider` in
  dark and light, including `AiState.Thinking`.
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts (`DESIGN-11`). The AI ring fades out when AI mode ends.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above; the
  keyboard model is the native range input's.
- **AI mode** — **yes**: an agent may set or propose a value (`AI-05`). The knob
  and the fill carry the state; there is no frame (`AI-08`).
- **Demo page** — `/components/slider`
  (`DRYL.Website/Components/Pages/DemoSlider.razor`), with the examples
  `Components/Examples/Slider/Basics.razor`, `.../FineGrained.razor`,
  `.../NoLabel.razor`, `.../States.razor`, `.../Validation.razor` and
  `.../AiMode.razor`.
- **`ComponentCatalog`** — `"Slider"` / `slider`, `ClassName` `"DrylSlider"`,
  flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/AiFramelessTests.cs` covers the AI
  markup and the invariant-culture knob position. The value and validation
  criteria have no test of their own.
