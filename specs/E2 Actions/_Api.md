# Actions — Public API

Shared enums, parameter contracts and services of the Actions category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/Actions/`

This file carries no `Meta` block: it is a reference for the specs around it, not
a unit of implementation (`SPEC-03`). The category holds three components —
`DrylButton`, `DrylButtonGroup` and `DrylSplitButton` — and owns exactly two
types, both of them enums nested inside `DrylButton`.

## `DrylButton.ButtonVariant`

Declared **nested inside** `DrylButton`, in
`code/DRYL.Components/Components/Actions/DrylButton.razor`. A consumer therefore
writes it qualified — `Variant="DrylButton.ButtonVariant.Ghost"` — and every
consumer call site in this repository does. The enum is referenced unqualified
only inside `DrylButton` itself, where it is declared and the short form is
unavoidable: the parameter defaults and the variant switch in its `CssClass`. In
any other component the unqualified `ButtonVariant.Ghost` appears only in Razor
usage comments and in `code/DRYL.Components/PACKAGE.md` — prose, not compiled
call sites.

| Member | Notes |
|---|---|
| `Primary` | The default of `DrylButton.Variant`. |
| `Secondary` | The default of `DrylSplitButton.Variant`. |
| `Ghost` | |
| `Danger` | |

## `DrylButton.ButtonSize`

Nested in the same component and spelled the same way —
`Size="DrylButton.ButtonSize.Small"`.

| Member | Notes |
|---|---|
| `Small` | |
| `Medium` | The default of both `DrylButton.Size` and `DrylSplitButton.Size`. |
| `Large` | |

## Where the two enums are shared

They are category-shared rather than component-private because `DrylSplitButton`
types its own parameters with them:

```csharp
[Parameter] public DrylButton.ButtonVariant Variant { get; set; } = DrylButton.ButtonVariant.Secondary;
[Parameter] public DrylButton.ButtonSize    Size    { get; set; } = DrylButton.ButtonSize.Medium;
```

That is the whole reason they belong in this file. The **defaults differ between
the two components**, and the difference is consumer-visible: a lone `DrylButton`
defaults to `ButtonVariant.Primary`, a `DrylSplitButton` to
`ButtonVariant.Secondary`, because a split button is an outlined pair rather than
the page's one filled call to action. `Size` defaults to `ButtonSize.Medium` on
both. `F3 DrylSplitButton.md` records the reasoning.

`DrylButtonGroup` declares no enum and takes neither: variant and size are set on
each `DrylButton` the consumer places inside it.

Both enums fall back rather than fail. A value outside the declared members —
reachable only by casting an out-of-range integer — renders as
`ButtonVariant.Primary` and `ButtonSize.Medium` respectively; the per-component
criteria in `F1 DrylButton.md` pin that.

## Enums this category uses but does not own

Named here so a consumer knows where to look, with their members deliberately
**not** restated — a second copy of a member list is the second source of truth
`SPEC-07` warns about.

| Type | Owner | Used by |
|---|---|---|
| `DrylMenu.MenuPlacement` | `E10 Navigation` — nested inside `DrylMenu`, `code/DRYL.Components/Components/Navigation/DrylMenu.razor` | `DrylSplitButton.MenuPlacement`, defaulting to `MenuPlacement.BottomEnd` where a lone `DrylMenu` defaults to `MenuPlacement.BottomStart`. |
| `AiState` | `E1 Foundation` — `code/DRYL.Components/AiState.cs` | `DrylButton.Ai` and `DrylSplitButton.Ai` (both inherited from `DrylAiAware`). |
| `AiAura` | `E1 Foundation` — `code/DRYL.Components/AiAura.cs` | `DrylButton.Aura` and `DrylSplitButton.Aura` (both inherited from `DrylAiAware`). |

`AiState` and `AiAura` are documented in
[`../E1 Foundation/_Api.md`](../E1%20Foundation/_Api.md), under that category's
"AI state vocabulary". That is where their members are listed; they are not
listed here.

## The AI opt-in contract

The three components take **two** shapes:

| Component | Shape | Consequence |
|---|---|---|
| `DrylButton`, `DrylSplitButton` | `@inherits DrylAiAware` | Both have `Ai` (`AiState`, default `AiState.None`) and `Aura` (`AiAura?`, default `null`), plus the `[CascadingParameter]` named `Scope` (of type `DRYL.Components.Ai.AiScope`) and the `EffectiveAi` / `EffectiveAura` resolution built on it. An explicit value wins over a surrounding `DrylAiScope`; `Aura` falls back through the scope to `AiAura.Comet`. |
| `DrylButtonGroup` | Neither | No `Ai`, no `Aura`, no aura of any kind. It is a layout wrapper; AI mode is set on the segments the consumer places inside it. |

Both `Ai` parameters satisfy `AI-03`: named `Ai`, typed `AiState`, defaulting to
`AiState.None`, and a switch on a component that renders as an ordinary control
without it.

`DrylSplitButton` is composed of two `DrylButton`s but resolves the scope **once**
itself and hands `EffectiveAi` and `EffectiveAura` to both segments, so its two
halves cannot show different AI states. `F3 DrylSplitButton.md` carries the
reasoning; it is not repeated here.

## Shared parameter conventions

All three components carry the same two pass-through parameters, and the 1.0
freeze binds them:

| Member | Type | Default | Behaviour |
|---|---|---|---|
| `Class` | `string?` | `null` | Extra CSS class(es) **merged** onto the component's own class list, never replacing it. Because Blazor matches parameter names case-insensitively, a consumer-supplied `class="…"` attribute also binds here and is merged the same way, instead of landing in `AdditionalAttributes` and clobbering the identity classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | `[Parameter(CaptureUnmatchedValues = true)]`. Splatted onto the component's root element — the `button` for `DrylButton`, the wrapper `div` for `DrylButtonGroup` and `DrylSplitButton`. |

### Attribute precedence

In all three markups the `@attributes` splat sits **last**, after every attribute
the component writes itself. Blazor's last-write-wins means a consumer-supplied
attribute of the same name overrides the component's own value. The rule is
uniform; what it lands on is not, since each component splats onto its own root.
Concretely, per component:

- On `DrylButton`: a consumer-supplied `disabled` disables the button even when
  `Disabled` and `Loading` are both `false`; a `type` overrides the one `IsSubmit`
  selected; an `aria-label` or `aria-pressed` wins over the one the component
  produced from `AriaLabel` or `Pressed`.
- On `DrylButtonGroup`: a consumer-supplied `role` overrides its `role="group"`,
  and an `aria-label` overrides the one produced from `AriaLabel`.
- On `DrylSplitButton`: the splat reaches the wrapper `div` only. The component
  writes no attribute on it besides `class`, and passes nothing through to either
  segment — so a `disabled` or `type` set here lands on the wrapper and changes
  neither button.

`class` is **the one exception**: it binds to the `Class` parameter rather than
being captured as unmatched, so it never reaches the splat and is merged instead
of overriding.

## Services

**None** — this category owns no service, registers none and consumes none; the
evidence is in [`_Interop.md`](_Interop.md), which carries the argument.
