# DrylInputNumber

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylInputNumber.razor

## User Story

As a Blazor developer, I want a numeric field that binds to an `int`, `long`,
`float`, `double` or `decimal` property, carries native `min`/`max`/`step`
constraints and can show its own increment/decrement buttons, so that I can
collect numbers in a form without writing parsing or stepping code.

## Description

`DrylInputNumber<TValue>` is an `InputBase<TValue>` around one native
`input type="number"`, laid out like `DrylInputText`: an optional label above,
an optional decorative leading icon, and a helper line below that validation
messages replace. Values are formatted and parsed with the invariant culture,
so a decimal separator is always a dot, whatever the server's locale.

`Min`, `Max` and `Step` become the native `min`, `max` and `step` attributes
only when the consumer actually supplies them; an unset constraint on a
non-nullable `TValue` would otherwise read as `0`. With `ShowStepper`, a column
of two chevron buttons on the right edge adds or subtracts `Step` (or 1).

The field can show the shared AI aura. Only the explicit `Ai` drives it; the
aura variant follows a surrounding `DrylAiScope`, the state does not.

## Public API

Namespace: `DRYL.Components`. Generic `TValue`; inherits `InputBase<TValue>`
and implements `IDisposable`. The inherited binding members follow the
`InputBase<TValue>` contract for `DrylInputNumber` in [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the input. |
| `Placeholder` | `string?` | `null` | Native placeholder. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `LeadingIcon` | `string?` | `null` | Decorative `DrylIcon` before the input text. |
| `Min` | `TValue?` | `default` | Native `min`; emitted only when supplied. |
| `Max` | `TValue?` | `default` | Native `max`; emitted only when supplied. |
| `Step` | `TValue?` | `default` | Native `step` and stepper increment; emitted only when supplied. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `ShowStepper` | `bool` | `false` | Shows the increment/decrement buttons. |
| `Disabled` | `bool` | `false` | Native disabled state, also on the stepper buttons. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state of the field's aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |

## Acceptance Criteria

### Value and structure

- The component renders one native `input` with `type="number"` carrying `.input`.
- The input carries `inputmode="decimal"`.
- The input carries `data-dryl-input`.
- The input displays a bound `double`, `float` or `decimal` formatted with the invariant culture.
- The input displays any other bound value through its `ToString()`.
- A `null` value renders an empty input.
- A native `input` event writes the typed text through `CurrentValueAsString`.
- Text that converts to `TValue` under the invariant culture updates the bound value.
- Text that does not convert produces the parsing message `The field must be a valid number.`
- The component works without an explicit `ValueExpression`.
- `AdditionalAttributes` are applied to the native input.
- A nonempty `Label` renders a `.field-label` associated with the input's generated `id`.
- An empty `Label` renders no label element.
- A nonempty `LeadingIcon` renders a decorative icon and adds `.has-leading-icon` to the input.

### Constraints

- A supplied `Min` renders as the native `min` attribute.
- A supplied `Max` renders as the native `max` attribute.
- A supplied `Step` renders as the native `step` attribute.
- A `double`, `float` or `decimal` constraint is formatted with the invariant culture.
- An unsupplied `Min`, `Max` or `Step` renders no corresponding attribute, even for a non-nullable `TValue`.

### Stepper

- Without `ShowStepper`, no `.num-stepper` is rendered.
- With `ShowStepper`, the wrapper carries `.has-stepper` and renders a `.num-stepper` with two `.num-step-btn` buttons.
- The upper button carries `aria-label="Increase"`.
- The lower button carries `aria-label="Decrease"`.
- Both stepper buttons carry `tabindex="-1"`.
- Both stepper buttons are disabled while `Disabled` is `true`.
- `Increase` adds `Step` to the current value.
- `Decrease` subtracts `Step` from the current value.
- Without a supplied `Step`, or with a `Step` of zero, the stepper moves by 1.
- With a `null` current value, the stepper buttons change nothing.
- The stepper's separator border reads `--line-strong` at rest.
- The separator border turns `--line-hover` while the field is hovered.
- The separator border turns `--accent-line` while the field has focus.
- The separator border transitions on `--dur-med` / `--ease-out`.
- A hovered stepper button reads `--fg` on `--glass-2`.
- A pressed stepper button reads `--accent-a` on `--glass-3`.
- Stepper button color and background transition on `--dur-fast` / `--ease-out`.
- The browser's own number spinners are hidden.

### Validation and disabled state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- Validation messages replace `HelperText`, joined by a space, in `.input-helper.input-helper-error`.
- A validation message sets `aria-invalid="true"` on the input.
- A displayed helper or validation message is referenced by the input's `aria-describedby`.
- Without a validation message or helper text, no helper element is rendered.
- `Disabled` sets the native `disabled` attribute on the input.

### Keyboard and accessibility

- An enabled input is reachable through Tab.
- When `Label` is empty, `AriaLabel` is applied as `aria-label`.
- When `Label` is nonempty, `AriaLabel` is omitted.
- The leading icon contributes no accessible name.

### AI mode

- `Ai` defaults to `AiState.None`.
- A non-`None` `Ai` renders the shared aura on the `.input-wrapper` through `DrylAuraElements`.
- An unset `Aura` inherits the surrounding `DrylAiScope`'s variant, falling back to `AiAura.Comet`.
- An effective transition into `AiState.Generated` replays the one-shot wash.
- A generated aura retires without the consumer resetting `Ai`.
- Disposing the component disposes its `AuraLifecycle`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope (`AiScope.ResolveAura`); the state passed
  to `AuraLifecycle.Sync` is the raw `Ai`. Listed as an I32 follow-up in
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
- **Stepper buttons are icon-only, outside the tab order and without a
  `DrylTooltip`** (`UX-01`, `UX-05`).
- **The stepper ignores `Min` and `Max`.** It adds or subtracts `Step` without
  clamping; only the native constraint validation of the browser sees the bounds.
- **`inputmode="decimal"` is fixed**, also for integer `TValue`s.
- **Literal geometry and opacity** — the stepper button width, the 1 px
  separators, the focus outline geometry and the disabled opacity in the
  `.num-stepper` / `.num-step-btn` rules of `dryl.css`; the icon sizes passed to
  `DrylIcon` (`DESIGN-01`).
- **Fixed English strings** — `Increase`, `Decrease` and the parsing message.
- **No enter/exit of conditional parts** — label, icon, stepper and helper mount
  without `DrylPresence` (`DESIGN-12`).
- **`AriaLabel` is not enforced**; a consumer can omit every accessible name.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/input-number` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts. State changes (border, stepper separator, button hover) are
  transitioned; the aura has its own lifecycle. Conditional parts are recorded
  above.
- **Keyboard and a11y** — native number input reachable by Tab, label or
  `AriaLabel` naming, `aria-invalid` and `aria-describedby`; the stepper's
  missing keyboard stop is recorded above.
- **AI mode** — **yes**: a model may propose a number, so the field carries
  `Ai` and `Aura` (`AI-05`).
- **Demo page** — `/components/input-number`
  (`DRYL.Website/Components/Pages/DemoInputNumber.razor`), with the examples
  `Components/Examples/InputNumber/Integer.razor`, `Decimal.razor`,
  `States.razor`, `Validation.razor` and `AiMode.razor`.
- **`ComponentCatalog`** — `"Number"` / `input-number`, group `Inputs`,
  `ClassName` `"DrylInputNumber"`, AI flag `true` (`REL-04`).
- **Tests** — no dedicated bUnit suite; the component appears only in the
  aura-only allowlist of `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
