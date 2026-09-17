# DrylTextarea

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylTextarea.razor

## User Story

As a Blazor developer, I want a multiline field with model binding, validation
and optional AI styling, so that longer editable content behaves consistently
with the rest of my form.

## Description

`DrylTextarea` presents a native, vertically resizable textarea with an optional
label and helper text. It updates its bound string on input. Within an
`EditForm`, bound-field validation replaces helper text and the chosen visual
state. Simple binding outside a form is also supported.

The aura can identify text currently being authored or edited by AI. The
component supplies no AI generation or automatic textarea resizing.

This newly documented contract includes I13 H05's forced-colors focus repair.
The repair is implemented. Existing debt below remains outside I13; native
Windows contrast-theme evidence is still unavailable and is not claimed.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IDisposable`. Inherited `Value`, `ValueChanged`, `ValueExpression`,
`DisplayName` and `AdditionalAttributes` follow [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the textarea. |
| `Placeholder` | `string?` | `null` | Native placeholder. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `Rows` | `int` | `4` | Initial visible row count passed to the native control. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Native disabled state. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |

There are no icon or content slots and no separate `Class` parameter.
Additional attributes target the textarea. `Rows` is passed through without
component-level range validation; the browser and shared minimum height affect
the final visible size.

## Acceptance Criteria

### Value and structure

- The field renders one native `textarea` carrying `.textarea`.
- The textarea displays the bound string value.
- A native `input` event with changed text updates the bound value through `ValueChanged`.
- A changed value notifies the associated `EditContext` field when one is present.
- String parsing accepts the supplied text without a component parsing error.
- A null parsed input is normalized to `string.Empty`.
- `Rows` defaults to `4`.
- The supplied `Rows` value is rendered as the native `rows` attribute.
- The textarea supports the browser's vertical resize affordance.
- A nonempty `Label` renders a label associated with the generated textarea ID.
- An empty `Label` renders no label element.
- `Placeholder` is passed to the native placeholder attribute.
- `AdditionalAttributes` are applied to the native textarea.
- The native textarea carries `data-dryl-input`.

### Validation and disabled state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- `InputState.Default` adds no state modifier without a validation message.
- Field validation messages replace `HelperText`.
- Multiple field validation messages are displayed joined by a space.
- With no validation message, nonempty `HelperText` is displayed below the textarea.
- Without a validation message or helper text, no helper element is rendered.
- `Disabled` applies the native disabled attribute.
- A disabled textarea cannot be edited through ordinary browser interaction.
- A disabled textarea is skipped by sequential keyboard focus.

### Keyboard and accessibility

- An enabled textarea is reachable through Tab and Shift+Tab in DOM order.
- Enter inserts a newline through the native textarea behavior.
- Native text selection and caret-navigation keys remain available.
- When `Label` is empty, `AriaLabel` is applied as `aria-label`.
- When `Label` is nonempty, the component's `AriaLabel` is omitted.
- A field validation message sets `aria-invalid="true"`.
- A manual `InputState.Error` alone does not set `aria-invalid`.
- A displayed helper or validation message is referenced by `aria-describedby`.

### Appearance, focus and motion

- The field reads shared `.textarea` styling without a component color-mode branch.
- Normal text uses `--fg`.
- The base field surface uses `--glass-1`.
- The default border uses `--line-strong`.
- The hover border uses `--line-hover`.
- Normal focus uses the existing accent border and glow treatment.
- Error focus uses the existing `--danger` treatment.
- Success focus uses the existing `--success` treatment.
- Focus and surface changes use `--dur-med` with `--ease-out`.
- Under `forced-colors: active`, an enabled `.textarea:focus-visible` has a
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
- The aura surrounds `.textarea-wrapper` using shared `DrylAuraElements` primitives.
- Effective `Generated` entry replays the shared one-shot wash.
- A generated aura retires without requiring the consumer to reset `Ai`.
- Re-rendering a spent `Generated` state does not replay it.
- Returning to live AI work cancels a pending aura exit.
- Disposing the component cancels its pending aura lifecycle work.
- Aura decoration does not change the bound text or keyboard order.
- Aura elements are hidden from assistive technology (`UX-07`).
- Reduced motion leaves the textarea usable with the shared aura at a static
  end state (I13 H03, `UX-06`).

## Recorded debt and limits

- `.textarea` and the shared field/helper rules in
  `code/DRYL.Components/wwwroot/dryl.css` retain literal padding and focus-shadow
  geometry (`DESIGN-01`). I13 does not approve replacement tokens.
- The field itself has state transitions but no root enter/exit animation.
  Conditional labels and helper messages do not use `DrylPresence`
  (`DESIGN-11`, `DESIGN-12`). This is recorded debt, not an exception.
- `.textarea:focus-visible` currently suppresses outlines. H05's forced-colors
  result is a pending requirement rather than an established pass.
- An accessible name is not enforced. Additional attribute overrides can break
  generated associations or replace control classes. See [`_Api.md`](_Api.md)
  (`UX-01`).
- AI changes carry no built-in live announcement (`UX-04`).
- The field does not auto-grow as content changes and does not validate `Rows`.

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** source inspection confirms shared mode tokens and no isolated
  stylesheet. Rendered dark/light inspection remains required; synchronized
  token definitions do not establish textarea contrast or focus visibility.
- **Motion:** `.textarea` provides focus/background/border transitions;
  `AuraLifecycle` and `DrylAuraElements` provide the aura's enter/exit behavior.
  Root/helper enter/exit debt is recorded above. The normal/reduced-motion
  browser result for I13's shared aura repair remains pending.
- **Keyboard/a11y:** native textarea semantics and generated label/helper
  associations are present in `DrylTextarea.razor`. H05 requires live
  Tab/Shift+Tab and multiline editing in both modes, forced-colors outline
  evidence and a Windows contrast-theme pass. No such pass is claimed here.
- **AI decision:** explicitly yes, because multiline content may be produced or
  edited by AI; state/variant inheritance and shared decoration are specified above.
- **Demo:** source verified in `DRYL.Website/Components/Pages/DemoForm.razor`,
  route `/components/forms`, through
  `DRYL.Website/Components/Examples/Form/Controls.razor`. AI examples also use
  the textarea in `Components/Examples/Ai/Inputs.razor` and
  `Components/Examples/Agents/AiField.razor`. These examples do not cover every
  textarea validation/disabled/aura variant (`CODE-20`).
- **Catalog:** the source-verified shared `"Form Controls"` / `forms` entry in
  `DRYL.Website/Components/ComponentCatalog.cs` mentions Textarea in its
  description. It has `ClassName` null and AI flag `false`, so it supplies shared
  navigation rather than an individual `DrylTextarea` registration; this is
  existing catalog debt under `REL-04`.
- **Tests:** `Aura_reaches_generated_on_review` in
  `tests/DRYL.Components.Tests/Agents/DrylAiFieldTests.cs` checks inherited
  generated aura classes on `.textarea-wrapper`. It does not establish native
  typing, focus, reduced motion or rendered colors. No dedicated textarea
  unit-test suite was found in this contract pass. I13's real browser host owns
  the focus evidence with actual global and generated isolated CSS loaded.

## I13 focus verification — 2026-09-17

`tests/DRYL.BrowserTests/FocusTests.cs` exercises Tab/Shift+Tab, Stepper
activation, text and multiline input, the actual Select popover with arrows,
Enter/Escape and focus return, and dialog close/return in both modes.
Ordinary keyboard cases pass in Chromium, Firefox and WebKit; Chromium and
installed Chrome/Edge also pass forced-colors outline assertions. Shared
controls and Stepper headers use existing `--accent-b` with browser-defined
outline geometry. Normal focus styles remain unchanged. Visual inspection
and final counts are recorded in `docs/2026-09-15-i13-implementation-plan.md`.

This supersedes the earlier pending-I13 verification wording. A native Windows
contrast-theme inspection and native Safari/Firefox smoke remain outstanding;
forced-colors emulation is not claimed as that native evidence. No full-library
accessibility audit or repair of the unrelated recorded debt is claimed.
