# DrylRadioGroup

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylRadioGroup.razor
              code/DRYL.Components/Components/Inputs/RadioGroupContext.cs
              code/DRYL.Components/Components/Inputs/RadioGroupOrientation.cs

## User Story

As a Blazor developer, I want a radio group that binds one value of any type,
validates inside an `EditForm` and can show that an AI is choosing for the user,
so that a mutually exclusive choice behaves like every other DRYL field.

## Description

`DrylRadioGroup<TValue>` inherits `InputBase<TValue>`. It renders a borderless
`fieldset` with an optional `legend`, a wrapper that carries the AI aura, and a
`role="radiogroup"` container in which its
[`DrylRadio<TValue>`](F10%20DrylRadio.md) children are laid out. It cascades a
`RadioGroupContext<TValue>` (selected value, change callback, disabled flag and a
generated input name) to the children.

The look and motion of each option belong to `DrylRadio`. The group owns layout,
validation, helper text and AI.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<TValue>` and implements
`IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The `DrylRadio` options. |
| `Label` | `string?` | `null` | Legend above the group. |
| `HelperText` | `string?` | `null` | Text below the group, replaced by validation messages. |
| `Orientation` | `RadioGroupOrientation` | `Vertical` | `Vertical` or `Horizontal` (wrapping) layout. |
| `Disabled` | `bool` | `false` | Disables every option. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |

`RadioGroupContext<TValue>` is an internal record the group creates for its
options; it is not part of the public API.

## Acceptance Criteria

### Structure and value

- The group renders a `fieldset`; a nonempty `Label` renders a
  `legend.field-label`.
- Options sit in a `role="radiogroup"` container carrying `.radio-group` and
  `.radio-group--vertical` or `.radio-group--horizontal`.
- Every option receives the same generated `name`.
- A pick in an option writes that option's value through `ValueChanged`.
- Parsing a string value uses `BindConverter` with the invariant culture; a
  failure reports "Invalid value.".
- `Disabled` disables every option.

### Validation and helper

- Validation messages for the bound field replace `HelperText`, joined by a
  space, in `.input-helper.input-helper-error`.
- Without a message, a nonempty `HelperText` renders in `.input-helper`.
- A displayed helper or message is referenced by the radiogroup's
  `aria-describedby`.

### AI mode

- `Ai` defaults to `AiState.None` (`AI-03`).
- With `AiState.None`, the group shows the state of a surrounding
  `DrylAiScope` (I32); an explicit non-`None` `Ai` wins.
- `Aura` overrides the scope's aura variant, which defaults to `AiAura.Comet`.
- The aura surrounds `.radio-group-wrapper` through the shared
  `DrylAuraElements`; an effective move into `Generated` plays the wash once.
- Disposing the group cancels its pending aura work.

## Recorded debt

- **Inline `fieldset` reset.** The `fieldset` carries an inline style resetting
  border, padding, margin and min-width.
- **No group-level keyboard model.** Arrow-key movement is the browser's native
  radio behaviour.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/radio` in
  dark and light.
- **Enter/exit animation** — **no** on the group; the aura has its own
  lifecycle, the options animate their state.
- **Keyboard and a11y** — `fieldset`/`legend`, `role="radiogroup"`,
  `aria-describedby` for helper and messages.
- **AI mode** — **yes**: an agent may pick an option for the user (`AI-05`).
- **Demo page** — `/components/radio`: vertical, horizontal, disabled,
  validation and AI mode examples.
- **`ComponentCatalog`** — `"Radio Group"` / `radio`, `ClassName`
  `"DrylRadioGroup"`.
- **Tests** — `tests/DRYL.Components.Tests/DrylSelectionControlTests.cs`;
  scope inheritance in `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
