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
writes it qualified — `Variant="DrylButton.ButtonVariant.Ghost"` — and that is
the spelling every call site in the library and in `DRYL.Website` uses. The
unqualified `ButtonVariant.Ghost` appears only in the components' Razor usage
comments, which are not compiled call sites.

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
| `AiState` | `E1 Foundation` — `code/DRYL.Components/AiState.cs` | `DrylButton.Ai` (inherited from `DrylAiAware`), `DrylSplitButton.Ai`. |
| `AiAura` | `E1 Foundation` — `code/DRYL.Components/AiAura.cs` | `DrylButton.Aura` (inherited from `DrylAiAware`). |

`AiState` and `AiAura` are documented in `E1 Foundation`'s companion files, under
that category's "AI state vocabulary" — which is still a phase-C scaffold at the
time of writing. That is where their members will be listed; they are not listed
here even in the meantime.

## The AI opt-in contract

The three components take three different shapes, and the divergence is real
rather than an accident of wording:

| Component | Shape | Consequence |
|---|---|---|
| `DrylButton` | `@inherits DrylAiAware` | Has both `Ai` (`AiState`, default `AiState.None`) and `Aura` (`AiAura?`, default `null`), plus the `[CascadingParameter]` `AiScope` and the `EffectiveAi` / `EffectiveAura` resolution built on it. An explicit value wins over a surrounding `DrylAiScope`; `Aura` falls back through the scope to `AiAura.Comet`. |
| `DrylSplitButton` | A plain `[Parameter] public AiState Ai { get; set; } = AiState.None;` | No `Aura` parameter, no cascading `AiScope`, no resolution of its own. `Ai` is forwarded to the main `DrylButton` only; each segment, being a `DrylButton`, resolves the surrounding scope independently. |
| `DrylButtonGroup` | Neither | No `Ai`, no `Aura`, no aura of any kind. It is a layout wrapper; AI mode is set on the segments the consumer places inside it. |

Both `Ai` parameters satisfy `AI-03`: named `Ai`, typed `AiState`, defaulting to
`AiState.None`, and a switch on a component that renders as an ordinary control
without it.

`DrylSplitButton`'s shape has two consumer-visible consequences — the aura variant
cannot be pinned on it, and its two segments can end up in different AI states
inside a scope. `F3 DrylSplitButton.md` analyses both in full, under its
deviations and recorded-design-gaps sections; that analysis is not repeated here.

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
attribute of the same name overrides the component's own value. Concretely, and
uniformly across the category:

- A consumer-supplied `disabled` disables a `DrylButton` even when `Disabled` and
  `Loading` are both `false`.
- A consumer-supplied `type` overrides the one `IsSubmit` selected.
- A consumer-supplied `role` overrides `DrylButtonGroup`'s `role="group"`.
- A consumer-supplied `aria-*` attribute — `aria-label`, `aria-pressed` — wins
  over the one the component produced from `AriaLabel` or `Pressed`.

`class` is **the one exception**: it binds to the `Class` parameter rather than
being captured as unmatched, so it never reaches the splat and is merged instead
of overriding.

## Services

**None.** This category owns no service, registers none, and consumes none. No
component under `code/DRYL.Components/Components/Actions/` carries an `@inject`
or `[Inject]` of any kind; `AddDrylComponents()` registers nothing on their
behalf. The only ambient value any of them reads is the `[CascadingParameter]`
`AiScope` that `DrylButton` inherits from `DrylAiAware` — a cascading value, not
a DI service. See [`_Interop.md`](_Interop.md).
