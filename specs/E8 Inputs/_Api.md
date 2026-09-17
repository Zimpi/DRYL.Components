# Inputs — Public API

Shared enums, parameter contracts and services of the Inputs category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Inputs/`

This reference covers the shared contracts used by `DrylInputText`,
`DrylTextarea` and `DrylSelect`. Other Inputs types remain phase-C work. It has
no `Meta` block and claims no component (`SPEC-03`).

## `InputState`

Defined in `code/DRYL.Components/Components/Inputs/InputState.cs`, namespace
`DRYL.Components`.

| Member | Meaning |
|---|---|
| `Default` | Neutral border with the ordinary accent focus treatment. |
| `Error` | Danger border and focus treatment. |
| `Success` | Success border and focus treatment, set by the consumer. |

The three components default `State` to `InputState.Default`. A validation
message for the bound field takes precedence over `State`, including
`InputState.Success`. Setting `State` is a visual override: it does not add a
validation message to the `EditContext`, set `aria-invalid` by itself, or
validate the field.

## `SelectItem`

Defined in `code/DRYL.Components/Models/SelectItem.cs`, namespace
`DRYL.Components`. A sealed positional record with constructor
`SelectItem(string Value, string Label)`.

| Member | Type | Meaning |
|---|---|---|
| `Value` | `string` | Value written to the bound model on selection. |
| `Label` | `string` | Text displayed in the option and, when matched, in the trigger. |

There are no item-level disabled, group, icon or template members. `DrylSelect`
preserves enumeration order and uses string equality to match `Value`; it does
not enforce uniqueness or membership of the bound value.

## `InputBase<string>` binding contract

The three components inherit `Microsoft.AspNetCore.Components.Forms.InputBase<string>`.
Its consumer-facing parameters are available on each component:

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Value` | `string?` | `null` | Current field value. |
| `ValueChanged` | `EventCallback<string>` | unset | Value-change callback used by `@bind-Value`. |
| `ValueExpression` | `Expression<Func<string>>?` | `null` | Identifies the bound field for form validation. |
| `DisplayName` | `string?` | `null` | Inherited display name for parsing errors; these components always accept string parsing. |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Unmatched attributes applied to the actual input, textarea or combobox trigger. |

Each component supplies `() => Value` if no `ValueExpression` is supplied.
It therefore supports simple binding outside an `EditForm`; model-field
validation inside a form needs the actual model expression, normally supplied
by `@bind-Value`. The header comment in `DrylInputText.razor` that says an
enclosing form is required is stale.

Native `input` events update text values; Select commits when an option is
chosen. Each component passes changes through `CurrentValueAsString`, so
`InputBase<string>` owns `ValueChanged` and field-change notification. Parsing
accepts a string unchanged and normalizes a null parsed input to `string.Empty`.

All three inspect `EditContext.GetValidationMessages(FieldIdentifier)`.
Messages replace `HelperText`, are joined with a space, and set
`aria-invalid="true"`. A visible helper or validation message is linked by
`aria-describedby`. Without either, no helper is rendered.

There is no separate `Class` parameter. Attribute splatting occurs on the
control after its generated attributes, so a supplied `class` can replace the
component's classes rather than merge them. A supplied `id` can likewise
disconnect the generated label association. These are existing limitations;
I13 does not change the attribute contract.

## AI state and aura inheritance

`AiState` and `AiAura` are Foundation types, defined in
`code/DRYL.Components/AiState.cs` and `code/DRYL.Components/AiAura.cs`; see
[`../E1 Foundation/_Api.md`](../E1%20Foundation/_Api.md).

The three fields expose `Ai` (`AiState`, default `AiState.None`) and `Aura`
(`AiAura?`, default `null`). They deliberately support AI styling because a
field can display AI-authored or AI-edited content (`AI-03`, `AI-05`).

`AiScope.Resolve` in `code/DRYL.Components/Ai/AiScope.cs` uses an explicit
non-`None` `Ai` first, otherwise the surrounding `DrylAiScope` state, otherwise
`None`. Passing `AiState.None` does not opt a child out of an active scope.
`AiScope.ResolveAura` uses explicit `Aura`, then the scope's variant, then
`AiAura.Comet`. Both `AiAura.Comet` and `AiAura.Aurora` use the same state
semantics.

Each field composes `AuraLifecycle`, `AiAuraCss.Append` and
`DrylAuraElements`. An effective transition into `Generated` replays its wash;
`Generated` retires itself without rewriting `Ai` or the bound value. The
decorative aura has no field-value generation, validation or live announcement
behavior. Applications that announce AI work must supply appropriate status
feedback (`UX-04`); applications must stop live states when their work ends
(`AI-06`).

## Shared field focus — I13 H05

`code/DRYL.Components/wwwroot/dryl.css` owns `.input`, `.textarea` and
`.select`. Their normal focus treatment retains the existing accent, danger
and success tokens. Under `forced-colors: active`, keyboard focus must have a
visible outline even when the browser removes box shadows (`UX-02`). Restore
a browser-provided outline using existing color tokens and browser-defined
geometry; I13 approves no focus token, system-color token or new dimension.

Verification covers the actual native text input and textarea plus the
non-native Select trigger with its real popover. Check Tab/Shift+Tab and the
controls' keyboard actions in both modes. Forced-colors emulation verifies a
surviving outline and is supplemented by a Windows contrast-theme pass.
Other isolated focus families remain outside this bounded repair. These are
required I13 results, not results established by this reference.
