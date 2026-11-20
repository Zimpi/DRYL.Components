# DrylInputMask

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylInputMask.razor
              code/DRYL.Components/Components/Inputs/DrylInputMask.razor.css
              code/DRYL.Components/Components/Inputs/MaskType.cs

## User Story

As a Blazor developer, I want a text field that formats structured input —
phone numbers, IBANs, postal codes, card numbers or my own pattern — while the
user types or pastes, so that I bind an already formatted string without
writing caret and separator handling myself.

## Description

`DrylInputMask` is an `InputBase<string>` around one native text input. A
pattern decides what the user may type: `#` accepts one digit, `A` accepts one
letter (forced to upper case), every other character is a literal separator.
`Mask` picks one of four predefined patterns, or `MaskType.Custom` with
`CustomPattern`.

Formatting runs in the browser: after the first interactive render the
component attaches `dryl.inputmask` to its input, which re-masks the text on
every `input` and `paste` and reports the masked string back through the
`[JSInvokable]` `OnMaskedValue`. The bound value is the masked string,
separators included. Without that attachment the field does not update its
value.

Label, helper text, validation display and the AI aura follow `DrylInputText`;
the input is set in the monospace font.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IAsyncDisposable`. Inherited binding members follow the `InputBase<string>`
contract in [`_Api.md`](_Api.md); `MaskType` is listed there.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Mask` | `MaskType` | `MaskType.Phone` | Predefined pattern, or `MaskType.Custom`. |
| `CustomPattern` | `string?` | `null` | Pattern used when `Mask` is `MaskType.Custom`. |
| `Label` | `string?` | `null` | Visible label above the input. |
| `Placeholder` | `string?` | `null` | Native placeholder; defaults to the pattern with `#` and `A` shown as `_`. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Native disabled state. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state of the field's aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |
| `LeadingIcon` | `string?` | `null` | Decorative `DrylIcon` before the input text. |

Public method: `OnMaskedValue(string maskedValue)` — `[JSInvokable]`, called by
`dryl.inputmask`; not meant for consumer code.

## Acceptance Criteria

### Patterns

- `MaskType.Phone` uses the pattern `+## ### ### ####`.
- `MaskType.Iban` uses the pattern `AA## #### #### #### #### ##`.
- `MaskType.PostalCode` uses the pattern `#####`.
- `MaskType.CreditCard` uses the pattern `#### #### #### ####`.
- `MaskType.Custom` uses `CustomPattern`.
- `MaskType.Custom` without `CustomPattern` attaches no mask.

### Structure

- The component renders one native `input type="text"` carrying `.input`.
- The input carries `spellcheck="false"`.
- The input carries `autocomplete="off"`.
- The input carries `data-dryl-input`.
- Without `Placeholder`, the placeholder is the active pattern with every `#` and `A` replaced by `_`.
- A supplied `Placeholder` replaces the derived one.
- A nonempty `Label` renders a `.field-label` associated with the input's generated `id`.
- A nonempty `LeadingIcon` renders a decorative icon.
- A nonempty `LeadingIcon` adds `.has-leading-icon` to the input and to the wrapper.
- `AdditionalAttributes` are applied to the native input.
- The input text uses `--font-mono`.

### Masking

- After the first interactive render, the component calls `dryl.inputmask.attach` with its input, the active pattern and a `DotNetObjectReference`.
- A change of the active pattern re-attaches the mask with the new pattern.
- A typed character that does not fit the next `#` or `A` position is dropped.
- Letters typed into an `A` position are upper-cased.
- A literal separator is inserted only when more data follows it.
- Input beyond the pattern's last data position is dropped.
- After each masked input, the caret is placed at the end of the masked text.
- A paste is intercepted, merged into the current selection and masked as a whole.
- Every masked input or paste calls `OnMaskedValue` with the masked string.
- `OnMaskedValue` with a string different from the current value writes it through `CurrentValueAsString`.
- `OnMaskedValue` with the current value changes nothing.
- The bound value is the masked string, separators included.

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

### Cleanup

- `DisposeAsync` disposes the `AuraLifecycle`.
- After an interactive render, `DisposeAsync` calls `dryl.inputmask.detach` for the input, ignoring any failure.
- `DisposeAsync` disposes the `DotNetObjectReference`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope; listed as an I32 follow-up in
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
- **The caret always jumps to the end.** Editing in the middle of a masked value
  moves the caret behind the last character after each keystroke.
- **A pattern that becomes empty keeps the old mask.** Switching to
  `MaskType.Custom` without `CustomPattern` skips the re-attach, so the previous
  pattern's listeners stay on the input.
- **No value without script.** The input has no Blazor `oninput` binding; until
  `dryl.inputmask` is attached, typed text never reaches the bound value.
- **The first attach is unguarded.** `dryl.inputmask.attach` in
  `OnAfterRenderAsync` catches no `JSException` or `JSDisconnectedException`.
- **Literal type values** — `font-size` and `letter-spacing` in the `.input`
  rule of `DrylInputMask.razor.css`; the icon size passed to `DrylIcon`
  (`DESIGN-01`).
- **No enter/exit of conditional parts** — label, icon and helper mount without
  `DrylPresence` (`DESIGN-12`).
- **`AriaLabel` is not enforced**; the expected format is announced only
  through the placeholder.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/input-mask` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow input
  its host mounts. The shared `.input` border and focus transitions apply; the
  aura has its own lifecycle.
- **Keyboard and a11y** — native text input reachable by Tab, label or
  `AriaLabel` naming, `aria-invalid`/`aria-describedby`; masking does not
  intercept navigation keys.
- **AI mode** — **yes**: a model may fill a structured value such as an IBAN,
  so the field carries `Ai` and `Aura` (`AI-05`).
- **Demo page** — `/components/input-mask`
  (`DRYL.Website/Components/Pages/DemoInputMask.razor`), with the examples
  `Components/Examples/InputMask/Predefined.razor`, `CustomPattern.razor`,
  `States.razor`, `AiMode.razor`, `Validation.razor` and `LivePreview.razor`.
- **`ComponentCatalog`** — `"Input Mask"` / `input-mask`, group `Inputs`,
  `ClassName` `"DrylInputMask"`, AI flag `true` (`REL-04`).
- **Tests** — no dedicated bUnit or JS suite; the component appears only in the
  aura-only allowlist of `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
