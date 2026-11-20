# DrylInputPassword

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylInputPassword.razor
              code/DRYL.Components/Components/Inputs/DrylInputPassword.razor.css

## User Story

As a Blazor developer building a sign-in or account form, I want a password
field with a show/hide toggle that binds and validates like the other DRYL
text fields, so that users can check what they typed without me writing the
toggle and its labelling.

## Description

`DrylInputPassword` is an `InputBase<string>` around one native input that
starts as `type="password"`. An eye button in the trailing slot switches the
input between `password` and `text`; switching never touches the value. Label,
helper text, validation display and the AI aura follow `DrylInputText`.

The input asks the browser for `autocomplete="current-password"`; a consumer
building a registration form overrides it through an attribute.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IDisposable`. Inherited binding members follow the `InputBase<string>`
contract in [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the input. |
| `Placeholder` | `string?` | `null` | Native placeholder. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Native disabled state, also on the toggle. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state of the field's aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |

## Acceptance Criteria

### Value and structure

- The component renders one native `input` carrying `.input` and `.has-trailing-icon`.
- The input renders `type="password"` initially.
- The input carries `autocomplete="current-password"`.
- An `autocomplete` passed through `AdditionalAttributes` replaces `current-password`.
- The input carries `data-dryl-input`.
- A native `input` event updates the bound value through `ValueChanged`.
- A null parsed input is normalized to `string.Empty` without a parsing error.
- The component works without an explicit `ValueExpression`.
- A nonempty `Label` renders a `.field-label` associated with the input's generated `id`.
- `AdditionalAttributes` are applied to the native input.

### Visibility toggle

- The wrapper renders one `button type="button"` carrying `.pwd-toggle` after the input.
- While the password is hidden, the toggle shows the `Eye` icon.
- While the password is hidden, the toggle carries `aria-label="Show password"`.
- While the password is hidden, the toggle carries `aria-pressed="false"`.
- Activating the toggle switches the input to `type="text"`.
- While the password is shown, the toggle shows the `EyeOff` icon.
- While the password is shown, the toggle carries `aria-label="Hide password"`.
- While the password is shown, the toggle carries `aria-pressed="true"`.
- Activating the toggle again switches the input back to `type="password"`.
- Toggling does not change the bound value.
- The toggle carries `tabindex="-1"`.
- The toggle is disabled while `Disabled` is `true`.
- The toggle's icon reads `--fg-dim` at rest.
- A hovered toggle reads `--fg` on `--glass-2`.
- The toggle's color and background transition on `--dur-fast` / `--ease-out`.
- The toggle has the `--r-xs` radius.
- A keyboard-focused toggle shows an outline mixed from `--accent-a`.

### Validation and disabled state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- Validation messages replace `HelperText`, joined by a space, in `.input-helper.input-helper-error`.
- A validation message sets `aria-invalid="true"` on the input.
- A displayed helper or validation message is referenced by the input's `aria-describedby`.
- `Disabled` sets the native `disabled` attribute on the input.

### Keyboard and accessibility

- An enabled input is reachable through Tab.
- When `Label` is empty, `AriaLabel` is applied as `aria-label`.
- When `Label` is nonempty, `AriaLabel` is omitted.

### AI mode

- `Ai` defaults to `AiState.None`.
- A non-`None` `Ai` renders the shared aura on the `.input-wrapper` through `DrylAuraElements`.
- An unset `Aura` inherits the surrounding `DrylAiScope`'s variant, falling back to `AiAura.Comet`.
- An effective transition into `AiState.Generated` replays the one-shot wash.
- A generated aura retires without the consumer resetting `Ai`.
- Disposing the component disposes its `AuraLifecycle`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope; listed as an I32 follow-up in
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
- **The toggle is outside the tab order** (`tabindex="-1"`), so a keyboard user
  cannot reveal the password (`UX-01`).
- **The icon-only toggle has no `DrylTooltip`** (`UX-05`).
- **The icon swap snaps.** `Eye` and `EyeOff` replace each other without a
  transition (`DESIGN-11`).
- **Literal geometry and opacity** in the `.pwd-toggle` rule — size, right
  offset, focus outline width/offset and the disabled opacity; the icon size
  passed to `DrylIcon` (`DESIGN-01`).
- **Fixed English strings** — `Show password`, `Hide password`.
- **No enter/exit of conditional parts** — label and helper mount without
  `DrylPresence` (`DESIGN-12`).
- **Catalog AI flag is `false`** although the component exposes `Ai` (`REL-04`
  consistency).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/input-password` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts. Border, focus and toggle hover transitions are present; the
  aura has its own lifecycle. The snapping icon swap is recorded above.
- **Keyboard and a11y** — native input reachable by Tab, label or `AriaLabel`
  naming, `aria-invalid`/`aria-describedby`, toggle with `aria-pressed` and a
  state-matching `aria-label`; its missing tab stop is recorded above.
- **AI mode** — **yes**: the field shares the input family's aura so a form an
  agent fills reads as one (`AI-05`); the demo shows `Thinking` and `Generated`.
- **Demo page** — `/components/input-password`
  (`DRYL.Website/Components/Pages/DemoInputPassword.razor`), with the examples
  `Components/Examples/InputPassword/Basics.razor`, `States.razor`,
  `Validation.razor` and `AiMode.razor`.
- **`ComponentCatalog`** — `"Password"` / `input-password`, group `Inputs`,
  `ClassName` `"DrylInputPassword"`, AI flag `false` (see debt) (`REL-04`).
- **Tests** — no dedicated bUnit suite; the component appears only in the
  aura-only allowlist of `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
