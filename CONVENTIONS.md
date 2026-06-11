# DRYL Public API Conventions

These rules define the **public API surface** of every DRYL component. They are
binding for the 1.0 API freeze: after `1.0.0`, changing any of these on an
existing component is a breaking change (MAJOR bump). They codify patterns the
library already follows — see `CLAUDE.md` for the design-system rules and
`COMPONENT_PATTERNS.md` for component structure.

## 1. Naming

- **Components:** PascalCase, `Dryl` prefix — `DrylButton`, `DrylDataGrid`.
- **Enums:** `<Component><Concept>` — `ButtonVariant`, `ButtonSize`,
  `BadgeKind`. Declared next to the component (nested type or sibling file).
- **CSS classes:** kebab-case, no prefix — `.btn`, `.glass-card`.

## 2. Parameters

- **Variants / sizes / kinds are always `enum`, never `string`.** The first enum
  member is the sensible default, and the parameter defaults to it:
  `[Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;`
- **Boolean parameters use a plain adjective / state word — no `Is`/`Has`
  prefix — and default to `false`:** `Disabled`, `Loading`, `Open`, `Selected`,
  `ReadOnly`, `Required`.
- **Required values** are non-nullable and named for the thing
  (`Src`, `Alt`, `Text`); optional values are nullable (`string?`).
- **Pass-through HTML attributes** use exactly:
  `[Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? AdditionalAttributes { get; set; }`
- **Extra CSS classes** use a typed, optional `string? Class` that the component
  **merges** into its own root class string (`string.Join(' ', new[] { "btn", …, Class }.Where(…))`).
  This is mandatory, not decorative: a consumer's `class="x"` binds to the
  `Class` parameter (Blazor matches attribute names case-insensitively) and is
  merged, whereas relying on the bare `@attributes` splat for `class` **clobbers**
  the component's own identity classes (the splat overrides the explicit
  `class="…"`). Therefore every consumer-facing component carries a merged
  `Class`; never drop it as "redundant".

## 3. Events

- **Two-way binding** uses the `Value` / `ValueChanged` / `ValueExpression`
  triple so consumers can write `@bind-Value`. For other bindable state, the
  change event is **`<Property>Changed`** (PascalCase property + `Changed`), with
  **no `On` prefix and no `Is` prefix** — e.g. `Open`/`OpenChanged`,
  `Expanded`/`ExpandedChanged`, `Active`/`ActiveChanged`, `PageSize`/`PageSizeChanged`.
- **Action & notification events** (one-way, "something happened") use the
  **`On<Verb>`** form: `OnClick`, `OnClose`, `OnSend`, `OnRetry`, `OnRemove`,
  `OnDismiss`, `OnClear`, `OnRowClick`.
- All events are `EventCallback` or `EventCallback<T>` — never `Action`/`Func`
  on the public surface.

## 4. AI

- The opt-in AI parameter is always named **`Ai`**, type `AiState`, default
  **`AiState.None`**. Never a per-component AI enum or a differently named
  parameter. See `CLAUDE.md` §2.10.

## 5. Slots

- The default slot is **`ChildContent`** (`RenderFragment?`).
- Named slots are PascalCase `RenderFragment?` — `Header`, `Footer`, `Start`,
  `End`, `Content`.
- Slots that take an item/context are `RenderFragment<T>` with a documented
  context type.

## 6. Form integration

- Input components that bind a single value derive from `InputBase<TValue>` and
  expose `@bind-Value`. They must override `SetParametersAsync` so `Value="..."`
  works outside an `EditForm` (avoids the `ValueExpression` `InvalidOperationException`).

## 7. Lifecycle / JS interop

- Components using `IJSRuntime` must be prerender-safe: no JS before the first
  interactive render, and `IAsyncDisposable` cleanup guarded by an `_attached`
  flag so static prerender disposal does not throw.

## Known deviations (to be fixed before 1.0)

These exist today and are tracked for the API-freeze audit (board #39):

- `DrylExpansion` — `IsOpen` / `IsOpenChanged` → should be `Open` / `OpenChanged`.
- `DrylPagination` / `DrylTable` — pagination events mix `On`-prefixed and bare
  forms; normalise to the rules in §3.
