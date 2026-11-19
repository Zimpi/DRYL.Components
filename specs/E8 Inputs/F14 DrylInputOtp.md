# DrylInputOtp

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylInputOtp.razor
              code/DRYL.Components/Components/Inputs/DrylInputOtp.razor.css

## User Story

As a Blazor developer, I want a one-time-code field with one box per digit, so
that a user can type or paste a verification code without thinking about the
caret.

## Description

`DrylInputOtp` inherits `InputBase<string>`. It renders `Digits` single-character
boxes inside a `role="group"` container, with an optional label above and helper
or validation text below. Focus advances as digits are typed, a pasted code
fills every box at once, and the bound string is updated on every keystroke.

Caret movement between the boxes happens in the browser (`dryl.otp`), not over
the circuit, so a code typed quickly lands in the right boxes on a slow
connection.

Every box is a surface of its own and carries the AI aura's ring and halo. A
comet per box would put several comets side by side, so the comet is retired
here and one light walks through the boxes in turn (`AI-08`).

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Digits` | `int` | `6` | Number of boxes. |
| `Label` | `string?` | `null` | Label above the boxes. |
| `HelperText` | `string?` | `null` | Text below the boxes, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Shows the boxes as default, error or success. |
| `Disabled` | `bool` | `false` | Greys the boxes out and blocks input. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is not set. |
| `Ai` | `AiState` | `AiState.None` | AI state, shown on every box. |
| `Aura` | `AiAura?` | `null` | Aura variant of the boxes' ring; `null` inherits a surrounding `DrylAiScope`. |
| `SetDigits` | method (`[JSInvokable]`) | — | Called by `dryl.otp` with the pasted digits. Not for consumers. |

## Acceptance Criteria

### Structure and value

- The component renders `Digits` text inputs with `maxlength="1"` and
  `inputmode="numeric"`.
- Changing `Digits` keeps the characters already entered that still fit.
- A typed digit fills its box.
- A typed character that is not a digit leaves its box empty.
- The bound value is the boxes' characters joined in order, updated on every
  keystroke.
- A pasted text fills the boxes with its digits, in order, ignoring every other
  character.
- A pasted text with more digits than boxes fills the boxes and drops the rest.
- `Disabled` disables every box.
- The first box carries `autocomplete="one-time-code"`; the others carry
  `autocomplete="off"`.
- A component used outside an `EditForm` binds `Value` without throwing.

### Focus movement

- Typing a digit moves focus to the next box.
- Typing a digit into the last box keeps focus there.
- `Backspace` in a filled box clears it.
- `Backspace` in an empty box moves focus to the previous box.
- `ArrowLeft` moves focus to the previous box.
- `ArrowRight` moves focus to the next box.
- After a paste, focus sits on the first empty box, or on the last box when the
  code is complete.
- Focus movement needs no round trip to the server.

### Validation and helper

- Validation messages for the bound field replace `HelperText`, joined by a
  space, in `.input-helper.input-helper-error`.
- Without a message, a nonempty `HelperText` renders in `.input-helper`.
- A validation message or `InputState.Error` gives every box a border in
  `--danger`.
- `InputState.Success` gives every box a border in `--success`.

### Keyboard and accessibility

- The boxes sit in a `role="group"` container.
- The group is named by the label when `Label` is set.
- The group is named by `AriaLabel` when `Label` is not set.
- The group is named "{Digits}-digit code" when neither is set.
- Every box is named "Digit {n} of {Digits}".
- A focused box shows a ring derived from `--accent-a` and `--accent-line`.

### Appearance and motion

- A box uses `--glass-1` with a border in `--line-strong`.
- A hovered box uses `--line-hover` for its border.
- A box's border, background and shadow change over `--dur-med`.
- A disabled box is dimmed and shows the not-allowed cursor.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in (`AI-03`).
- Every box composes the shared aura layers through `DrylAuraElements`
  (`AI-02`).
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- While an aura is present a box's own border recedes, so only the aura's ring
  is seen.
- No comet is visible on any box under any AI state (`AI-08`).
- Under `AiState.Active`, `AiState.Thinking` and `AiState.Streaming` the ring
  and the halo of each box brighten and fade in turn, from the first box to the
  last, and repeat.
- The period of that walk follows the state, so `AiState.Thinking` walks faster
  than `AiState.Active`.
- The walk's offset between two boxes is derived from the box's index and
  `Digits`, so a code of any length is crossed once per period.
- Entering `AiState.Generated` plays the shared completion wash on every box.
- `AiState.Generated` retires by itself through `AuraLifecycle` (`AI-07`).
- Under `AiState.Proposed` every box shows the shared resting edge and nothing
  loops (`AI-06`).
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves.
- Under `prefers-reduced-motion: reduce` the walk does not run and every box
  keeps a still ring.
- Disposing the component cancels its pending aura work and detaches
  `dryl.otp`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope.
- **The box size and the type size are literals** in `DrylInputOtp.razor.css`.
- **The helper text is not tied to the boxes** by `aria-describedby`.
- **The accessible names are English and not translatable.**
- **The proposal description is rendered once per box**, because every box
  hosts its own `DrylAuraElements`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/otp` in
  dark and light, including `AiState.Thinking`.
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts (`DESIGN-11`). The aura has its own lifecycle.
- **Keyboard and a11y** — the "Focus movement" and "Keyboard and accessibility"
  criteria above.
- **AI mode** — **yes**: an agent may fill or verify a code (`AI-05`). The boxes
  keep ring and halo; the comets are replaced by one walking light (`AI-08`).
- **Demo page** — `/components/otp`
  (`DRYL.Website/Components/Pages/DemoInputOtp.razor`), with the examples
  `Components/Examples/InputOtp/Basics.razor`, `.../States.razor`,
  `.../Validation.razor`, `.../LivePreview.razor` and `.../AiMode.razor`.
- **`ComponentCatalog`** — `"OTP Input"` / `otp`, `ClassName` `"DrylInputOtp"`,
  flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/AiFramelessTests.cs` covers the
  per-box index markup. The value and focus criteria have no test of their own.
