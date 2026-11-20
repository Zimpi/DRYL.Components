# Inputs — Public API

Shared enums, parameter contracts and services of the Inputs category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Inputs/`

This reference covers the shared contracts used by `DrylInputText`,
`DrylTextarea` and `DrylSelect`; `DrylMultiSelect` uses `SelectItem` and
`InputState` with the same meaning, and `DrylTimePicker` uses `InputState`.
`DrylInputPassword`, `DrylInputMask`, `DrylInputNumber`, `DrylDatePicker` and
`DrylChipInput` use `InputState` with the same meaning as well (for
`DrylChipInput` without the validation-message precedence, see below). The
sections below also list `MaskType`, `SegmentedSize` and the binding
contracts of the remaining inputs. It has
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

## `InputBase<TItem?>` — `DrylAutocomplete`

`DrylAutocomplete<TItem>` (`TItem : class`) inherits `InputBase<TItem?>` rather
than `InputBase<string>`: it binds the picked or created item itself. `Value`,
`ValueChanged` and `ValueExpression` are typed `TItem?`; the component supplies
`() => Value` when no expression is given, as the string fields do. Parsing from
a string is not used — `TryParseValueFromString` always succeeds with `null`.
It uses the same `InputState`, validation-message and `AiState`/`AiAura`
contracts as the fields above. Its own parameters, including the creation path
(`OnCreate`, `CreateLabel`) and the status texts (`EmptyText`, `LoadingText`),
are listed in [`F5 DrylAutocomplete.md`](F5%20DrylAutocomplete.md).

## `InputBase<bool>` — `DrylCheckbox`, `DrylToggle`

`DrylCheckbox` and `DrylToggle` inherit `InputBase<bool>`. `Value`,
`ValueChanged` and `ValueExpression` are typed `bool`; each component supplies
`() => Value` when no expression is given, so simple binding works outside an
`EditForm`. A native `change` event writes the new state through
`CurrentValue`. Parsing from a string accepts `bool.TryParse` and treats any
other text as `false` without a parsing error. Neither component shows
validation messages, a helper or an `aria-invalid` state of its own.
`AdditionalAttributes` are applied to the native input. Both render their label
row with the shared `.choice` / `.choice-label` classes and mark a user change
with `data-touched` (idea I39); see
[`F8 DrylCheckbox.md`](F8%20DrylCheckbox.md) and
[`F11 DrylToggle.md`](F11%20DrylToggle.md).

`DrylRadioGroup<TValue>` inherits `InputBase<TValue>` and takes part in
validation like the text fields; see
[`F9 DrylRadioGroup.md`](F9%20DrylRadioGroup.md).

## `MaskType`

Defined in `code/DRYL.Components/Components/Inputs/MaskType.cs`, namespace
`DRYL.Components`; owned by `DrylInputMask`
([`F17 DrylInputMask.md`](F17%20DrylInputMask.md)). In a pattern, `#` is one
digit, `A` one letter (upper-cased), anything else a literal separator.

| Member | Pattern |
|---|---|
| `Phone` | `+## ### ### ####` |
| `Iban` | `AA## #### #### #### #### ##` |
| `PostalCode` | `#####` |
| `CreditCard` | `#### #### #### ####` |
| `Custom` | The component's `CustomPattern`. |

## `SegmentedSize`

Defined in `code/DRYL.Components/Components/Inputs/SegmentedSize.cs`, namespace
`DRYL.Components`; owned by `DrylSegmentedControl<TValue>`
([`F20 DrylSegmentedControl.md`](F20%20DrylSegmentedControl.md)).

| Member | Track class |
|---|---|
| `Small` | `.seg--sm` |
| `Medium` | `.seg--md` (the default) |
| `Large` | `.seg--lg` |

## `InputBase<string>` — `DrylInputPassword`, `DrylInputMask`

Both follow the `InputBase<string>` binding contract above: `() => Value` is
supplied when no `ValueExpression` is given, parsing accepts any string and
normalizes `null` to `string.Empty`, validation messages replace `HelperText`
and set `aria-invalid="true"`, and `AdditionalAttributes` land on the native
input. `DrylInputPassword` updates on the native `input` event.
`DrylInputMask` has no Blazor `input` binding: its value arrives masked from
`dryl.inputmask` through the `[JSInvokable]` method `OnMaskedValue(string maskedValue)`.

## `InputBase<TValue>` — `DrylInputNumber`

`DrylInputNumber<TValue>` inherits `InputBase<TValue>` for `int`, `long`,
`float`, `double`, `decimal` and their nullable forms. `Value`,
`ValueChanged` and `ValueExpression` are typed `TValue`; `() => Value` is
supplied when no expression is given. Parsing uses
`BindConverter.TryConvertTo<TValue>` with the invariant culture and reports
`The field must be a valid number.` on failure. `double`, `float` and `decimal`
values are formatted with the invariant culture. `Min`, `Max` and `Step` are
typed `TValue?`; because that is `TValue` itself for a non-nullable value type,
the component tracks whether each was supplied rather than null-checking it.
Validation display follows the string fields.

## `InputBase<DateOnly?>` — `DrylDatePicker`

`DrylDatePicker` inherits `InputBase<DateOnly?>`. In single mode it binds
`Value` and supplies **no** fallback `ValueExpression`. In range mode
(`Range="true"`) it binds `RangeStart` / `RangeStartChanged` and `RangeEnd` /
`RangeEndChanged` (each `DateOnly?` / `EventCallback<DateOnly?>`), and points
`ValueExpression` at a private dummy field. A parsed string is accepted when
empty or in the exact format `dd.MM.yyyy`; otherwise the parsing message is
`Invalid date. Expected format: dd.MM.yyyy`. Validation display follows the
string fields.

## `@bind-Tags` — `DrylChipInput`

`DrylChipInput` is not an `InputBase`. It binds `Tags`
(`IReadOnlyList<string>`, default `[]`) through `TagsChanged`
(`EventCallback<IReadOnlyList<string>>`); it never mutates `Tags` and reports
each change as a new list. It has no `EditContext` integration: `InputState`
styles the box and, for `InputState.Error`, the helper, without a validation
message or `aria-invalid`. `AdditionalAttributes` is typed
`IDictionary<string, object>?` and lands on the inner text input; `Class`
merges onto the `.field` root.

## `@bind-Value` — `DrylSegmentedControl<TValue>` / `DrylSegment<TValue>`

`DrylSegmentedControl<TValue>` is not an `InputBase` and takes no part in
validation. It binds `Value` (`TValue`) through `ValueChanged`
(`EventCallback<TValue>`); a selection also updates its own `Value`, so it
works unbound. Segment values are compared with
`EqualityComparer<TValue>.Default`. `DrylSegment<TValue>` declares one option
(`Value`, `Label`, `Icon`, `Disabled`, `ChildContent`) and must sit inside a
control of the same `TValue`.

## `DrylFormField<TValue>` and `DrylValidationSummary`

Neither binds a value. `DrylFormField<TValue>` takes `For`
(`Expression<Func<TValue>>?`) only to look up validation messages of that field
in the cascading `EditContext`; `TValue` is the field's type.
`DrylValidationSummary` reads every message of the cascading `EditContext`.
Both take `Class` (merged onto their root) and `AdditionalAttributes`
(`IDictionary<string, object>?`, applied to their root).

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

### Aura-only inputs

`DrylInputPassword`, `DrylInputMask`, `DrylInputNumber`, `DrylDatePicker` and
`DrylChipInput` expose the same `Ai` and `Aura` parameters and compose the same
`AuraLifecycle` / `AiAuraCss.Append` / `DrylAuraElements`, but resolve only the
**variant** through `AiScope.ResolveAura`. Their state is the explicit `Ai`
alone: a surrounding `DrylAiScope` state does not reach them. They are listed
as I32 follow-ups in `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`
and record the gap as debt in their specs.

`DrylSegmentedControl`, `DrylSegment`, `DrylFormField` and
`DrylValidationSummary` have no `Ai` parameter (`AI-05`); each spec states why.

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
