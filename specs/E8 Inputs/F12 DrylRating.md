# DrylRating

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylRating.razor
              code/DRYL.Components/Components/Inputs/DrylRating.razor.css

## User Story

As a Blazor developer, I want a star rating bound like any other input, so that
a user can give or read a score without a number field.

## Description

`DrylRating` inherits `InputBase<int?>`. It renders a row of star buttons inside
a `role="group"` container, with an optional label above and helper or
validation text below. A click sets the score, a click on the current score
clears it when `AllowClear` allows it, and hovering previews a score without
committing it.

The stars are free-standing: the component has no surface of its own. In AI mode
it therefore draws no aura frame (`AI-08`) — the stars themselves take the AI
color and move with the state.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<int?>` and implements
`IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Label above the stars. |
| `HelperText` | `string?` | `null` | Text below the stars, replaced by validation messages. |
| `MaxStars` | `int` | `5` | Number of stars. |
| `ReadOnly` | `bool` | `false` | Shows the score without accepting input. |
| `AllowClear` | `bool` | `true` | A click on the current score clears the value. |
| `Disabled` | `bool` | `false` | Greys the stars out and blocks input. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is not set. |
| `Ai` | `AiState` | `AiState.None` | AI state, shown by the stars. |
| `Aura` | `AiAura?` | `null` | Has no visible effect; the rating draws no aura frame. |

## Acceptance Criteria

### Structure and value

- The component renders `MaxStars` buttons of `type="button"`.
- Every star up to the active score carries `.rating-btn--filled`.
- The active score is the hovered star while a star is hovered, the bound value
  otherwise.
- A click on a star sets the value to that star's position.
- A click on the star that equals the current value clears the value when
  `AllowClear` is true.
- A click on the star that equals the current value keeps the value when
  `AllowClear` is false.
- `Disabled` disables every star.
- `ReadOnly` disables every star.
- A nonempty `Label` renders above the stars in `.field-label`.
- A component used outside an `EditForm` binds `Value` without throwing.

### Validation and helper

- Validation messages for the bound field replace `HelperText`, joined by a
  space, in `.input-helper.input-helper-error`.
- Without a message, a nonempty `HelperText` renders in `.input-helper`.

### Keyboard and accessibility

- The stars sit in a `role="group"` container.
- The group is named by the label when `Label` is set.
- The group is named by `AriaLabel` when `Label` is not set.
- The group is named "Star rating" when neither is set.
- Every star is named "{n} of {MaxStars} stars".
- The star that equals the value carries `aria-pressed="true"`; every other star
  carries `aria-pressed="false"`.
- Exactly one star is in the tab order: the current score, or the first star
  when there is no value.
- `ArrowRight` and `ArrowUp` raise the value by one, up to `MaxStars`.
- `ArrowLeft` and `ArrowDown` lower the value by one.
- `ArrowLeft` on a value of one clears the value when `AllowClear` is true and
  keeps it otherwise.
- `Home` sets the value to one.
- `End` sets the value to `MaxStars`.
- No key changes the value while the rating is `Disabled` or `ReadOnly`.
- A focused star shows a focus ring in `--accent-line`.

### Appearance and motion

- A filled star uses `--warning`.
- An empty star uses `--fg-dim`.
- A hovered or focused star grows and takes `--warning`, over `--dur-fast`.
- A disabled star is dimmed and shows the default cursor.
- Under `prefers-reduced-motion: reduce` no star runs an AI animation.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in (`AI-03`).
- The rating renders no aura layer — no ring, comet, glow or wash — because the
  stars have no surface of their own for one to trace (`AI-08`).
- The wrapper around the stars carries the shared state classes.
- The wrapper gains no padding, radius or fill in AI mode, so entering AI mode
  moves nothing.
- `Aura` has no visible effect.
- Under every AI state the filled stars use `--ai-a` with a glow of the same
  color.
- Under every AI state the empty stars use a faint `--ai-a`.
- Under `AiState.Thinking` the filled stars pulse together.
- The `AiState.Thinking` pulse stays within the hue of `--ai-a` from start to
  end.
- Under `AiState.Streaming` a ripple from `--ai-a` to `--ai-b` runs through the
  filled stars, one after another.
- Entering `AiState.Generated` flashes the filled stars once, one after another,
  and settles them on `--ai-a`.
- `AiState.Generated` retires by itself through `AuraLifecycle` (`AI-07`).
- Under `AiState.Proposed` the stars rest in the AI color and nothing loops
  (`AI-06`).
- Under `AiState.Proposed` the component renders the visually hidden text
  "AI suggestion, awaiting your decision".
- Disposing the component cancels its pending aura work.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope, and the variant has no visible effect
  here.
- **The ripple and flash staggers stop at ten stars.** The delays are written
  per `nth-child`, so an eleventh star moves in phase with the first.
- **The star size and the AI loop periods are literals** in
  `DrylRating.razor.css`.
- **The accessible names are English and not translatable.**

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/rating` in
  dark and light, including `AiState.Thinking`.
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts (`DESIGN-11`). Its state changes animate.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — **yes**: an agent may propose or set a score (`AI-05`). The
  stars carry the state; there is no frame (`AI-08`).
- **Demo page** — `/components/rating`
  (`DRYL.Website/Components/Pages/DemoRating.razor`), with the examples
  `Components/Examples/Rating/Basics.razor`, `.../States.razor`,
  `.../Validation.razor`, `.../LivePreview.razor` and `.../AiMode.razor`.
- **`ComponentCatalog`** — `"Rating"` / `rating`, `ClassName` `"DrylRating"`,
  flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/AiFramelessTests.cs` covers the AI
  markup. The value, keyboard and validation criteria have no test of their own.
