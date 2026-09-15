# DrylInputText

## Meta
- **State:** Modified
- **Source:** code/DRYL.Components/Components/Inputs/DrylInputText.razor

## User Story

As a Blazor developer, I want a single-line text field that binds to my model,
shows validation and can signal AI work, so that I can compose an accessible
form without rebuilding those field behaviors.

## Description

`DrylInputText` presents a native text input with an optional label, helper
text and decorative leading/trailing icons. Changes reach the bound value as
the user types. Within an `EditForm`, messages for the bound field replace
helper text and take precedence over a manually chosen visual state. Simple
binding also works outside a form.

The field can identify content being authored or edited by AI through the
shared aura vocabulary. AI styling does not generate or change its value.

This newly documented contract includes the approved I13 H05 forced-colors
focus repair. Its state remains `Modified` until the repair and required
evidence are reconciled; recorded pre-existing debt remains outside that fix.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IDisposable`. Inherited binding parameters (`Value`, `ValueChanged`,
`ValueExpression`, `DisplayName`, `AdditionalAttributes`) are defined in
[`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the input. |
| `Placeholder` | `string?` | `null` | Native placeholder. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `LeadingIcon` | `string?` | `null` | Decorative `DrylIcon` before the input text. |
| `TrailingIcon` | `string?` | `null` | Decorative `DrylIcon` after the input text. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Native disabled state. |
| `Type` | `string` | `"text"` | HTML input type, such as `email`, `url`, `tel` or `search`. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |

There is no content slot or separate `Class` parameter. Additional attributes
target the native input, not the outer field. `Type` is passed through to the
browser; this component does not implement type-specific parsing.

## Acceptance Criteria

### Value and structure

- The field renders one native `input` carrying `.input`.
- The input renders `Type` as its HTML `type`.
- The input displays the bound string value.
- A native `input` event with changed text updates the bound value through `ValueChanged`.
- A changed value notifies the associated `EditContext` field when one is present.
- String parsing accepts the supplied text without a component parsing error.
- A null parsed input is normalized to `string.Empty`.
- An empty `Label` renders no label element.
- A nonempty `Label` renders a label associated with the generated input ID.
- `Placeholder` is passed to the input's native placeholder attribute.
- A nonempty `LeadingIcon` renders a decorative icon before the input.
- A nonempty `TrailingIcon` renders a decorative icon after the input.
- An absent icon name renders no corresponding icon.
- Each present icon adds its corresponding input-padding class.
- `AdditionalAttributes` are applied to the native input.
- The native input carries `data-dryl-input`.

### Validation and disabled state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- `InputState.Default` adds no state modifier without a validation message.
- Field validation messages replace `HelperText`.
- Multiple field validation messages are displayed joined by a space.
- With no validation message, nonempty `HelperText` is displayed below the input.
- Without a validation message or helper text, no helper element is rendered.
- `Disabled` applies the native disabled attribute.
- A disabled input cannot be edited through ordinary browser interaction.
- A disabled input is skipped by sequential keyboard focus.

### Keyboard and accessibility

- An enabled input is reachable through Tab and Shift+Tab in DOM order.
- Native text editing keys retain the behavior of the selected HTML `Type`.
- When `Label` is empty, `AriaLabel` is applied as `aria-label`.
- When `Label` is nonempty, the component's `AriaLabel` is omitted.
- A field validation message sets `aria-invalid="true"`.
- A manual `InputState.Error` alone does not set `aria-invalid`.
- A displayed helper or validation message is referenced by `aria-describedby`.
- Decorative icons contribute no accessible name and no keyboard stop.

### Appearance, focus and motion

- The field reads the shared `.input` styling without a component color-mode branch.
- Normal field text uses `--fg`.
- The base field surface uses `--glass-1`.
- The default border uses `--line-strong`.
- The hover border uses `--line-hover`.
- Normal focus uses the existing accent border and glow treatment.
- Error focus uses the existing `--danger` treatment.
- Success focus uses the existing `--success` treatment.
- Focus and surface changes use `--dur-med` with `--ease-out`.
- Under `forced-colors: active`, an enabled `.input:focus-visible` has a
  visible outline when box shadows are removed (I13 H05, `UX-02`).
- The forced-colors fallback preserves the ordinary keyboard focus order.
- The focus fallback introduces no new design token or focus dimension.

### AI mode

- `Ai` defaults to `AiState.None` (`AI-03`).
- An explicit non-`None` `Ai` overrides an inherited scope state.
- With `AiState.None`, the field inherits a surrounding scope state when present.
- Without an active scope or explicit AI state, a settled field has no aura.
- An explicit `Aura` overrides the inherited aura variant.
- The aura variant defaults to `AiAura.Comet` when neither field nor scope sets it.
- The aura surrounds the input wrapper using the shared `DrylAuraElements` primitives.
- Effective `Generated` entry replays the shared one-shot wash.
- A generated aura retires without requiring the consumer to reset `Ai`.
- Re-rendering a spent `Generated` state does not replay it.
- Returning to live AI work cancels a pending aura exit.
- Disposing the component cancels its pending aura lifecycle work.
- Aura decoration does not change the bound value or keyboard order.
- Aura elements are hidden from assistive technology (`UX-07`).
- Reduced motion leaves the field usable with the shared aura at a static end
  state (I13 H03, `UX-06`).

## Recorded debt and limits

- `.input` and the icon/helper rules in `code/DRYL.Components/wwwroot/dryl.css`
  retain literal padding and focus-shadow geometry; `LeadingIcon` and
  `TrailingIcon` also pass a literal `Size` (`DESIGN-01`). I13 uses the
  existing values and does not approve replacements.
- The field itself has state transitions but no root enter/exit animation.
  Conditional labels, icons and helper messages have no `DrylPresence`
  (`DESIGN-11`, `DESIGN-12`). This is debt, not an animation exception.
- `.input:focus-visible` currently suppresses outlines. H05 must establish
  the forced-colors result above before this spec can claim that repair.
- `AriaLabel` is not enforced. A consumer can omit every accessible name.
  Attribute overrides can break label/helper relationships; classes are not
  automatically merged. See [`_Api.md`](_Api.md) (`UX-01`).
- AI state has no built-in live announcement (`UX-04`). A surrounding status
  component is necessary when the application needs AI activity announced.
- `State` is visual only; manual error styling does not produce validation text.

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** source inspection confirms shared mode tokens and no isolated
  stylesheet. A rendered dark/light check is still required; token-sync and
  contrast scripts alone do not prove this field's rendered appearance.
- **Motion:** focus/background/border transitions are in `.input`; aura
  entry/exit is supplied by `AuraLifecycle` and `DrylAuraElements`. The missing
  field/helper enter/exit is recorded above. Normal/reduced-motion browser
  evidence for the I13 shared aura repair remains pending.
- **Keyboard/a11y:** the native input, generated label, helper association and
  decorative icons are evidenced in `DrylInputText.razor`. H05 requires a live
  Tab/Shift+Tab pass in both modes, a forced-colors outline assertion and a
  Windows contrast-theme pass. None is claimed by this spec-only update.
- **AI decision:** explicitly yes, because a text field can contain AI-authored
  or AI-edited content; shared state/variant/lifecycle behavior is specified above.
- **Demo:** source verified in the sibling website checkout at
  `DRYL.Website/Components/Pages/DemoInputText.razor`, route `/components/inputs`,
  with `Components/Examples/InputText/Basics.razor`, `Icons.razor`,
  `States.razor` and `Validation.razor`. AI examples live in
  `DRYL.Website/Components/Examples/Ai/Inputs.razor`; the examples show static
  live AI states and do not establish application lifecycle correctness (`AI-06`).
- **Catalog:** source verified in `DRYL.Website/Components/ComponentCatalog.cs`
  as `"Input Text"` / `inputs`, category `Inputs`, class `DrylInputText`,
  AI flag `true` (`REL-04`).
- **Tests:** no dedicated `DrylInputText` unit-test suite was found in
  `tests/DRYL.Components.Tests` during this contract pass. I13's separate browser
  host must render the working-tree library and both actual stylesheets for its
  focus evidence. Website source inspection is not a browser pass.
