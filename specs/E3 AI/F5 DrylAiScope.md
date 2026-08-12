# DrylAiScope

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylAiScope.razor

## User Story

As a Blazor developer wiring one AI operation to several places on a page, I want
to wrap that region once and have every AI-aware component inside it light up
together, so that I do not thread the same `AiState` through a dozen parameters.

## Description

`DrylAiScope` broadcasts a shared `AiState` — and optionally an `AiAura` variant —
to the AI-aware components inside it. It renders no element of its own: it is a
`CascadingValue` around its `ChildContent` and nothing else.

There are two ways to drive it, and they are mutually exclusive by design. With a
`Key` it mirrors `IDrylAiActivityService` for that operation, so `AiActivity.Begin("compose")`
in the host's code lights the whole region up and disposing the handle settles it
back. With an explicit `State` it broadcasts that value and ignores the service
entirely — which is what makes it usable in a demo, or in an app that never called
`AddDrylComponents()`, since the service is resolved optionally rather than
required.

`State` is an `AiState?` **without** a default, and that nullability is load-bearing:
`null` means "follow the service", `AiState.None` means "actively force AI off". A
default of `AiState.None` would collapse the two and break the service path. It is
therefore not the opt-in `AI-03` governs — it is a broadcast override, and
[`_Api.md`](_Api.md) records that decision for the category.

Not every AI-aware component inherits the state. `DrylToolCall`, `DrylToolCallGroup`
and `DrylCanvas` read a surrounding scope for the **aura variant only**, because a
tool call's status is a fact about that call rather than an ambient mood; see
`_Api.md`.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Key` | `string?` | `null` | Operation key. With a registered `IDrylAiActivityService`, the scope mirrors that key's state. |
| `State` | `AiState?` | — (no default) | Explicit override. Set, it is broadcast and the service is ignored; `null` follows the service. Not an opt-in (`AI-03`) — see `_Api.md`. |
| `Aura` | `AiAura?` | `null` | Aura variant broadcast to descendants that do not pin their own. `null` lets them fall back to `AiAura.Comet`. |
| `ChildContent` | `RenderFragment?` | `null` | The content whose AI-aware descendants inherit the scope. |

The component takes no `Class` and no `AdditionalAttributes`: it has no element to
put them on.

## Acceptance Criteria

### Broadcasting

- The component renders `ChildContent` and no element of its own.
- The component cascades an `AiScope` value to its descendants.
- The cascaded value carries the resolved `AiState`.
- The cascaded value carries `Key` unchanged.
- The cascaded value carries `Aura` unchanged, including `null`.
- The cascaded value is not fixed, so descendants re-render when it changes.
- A new `AiScope` instance is produced on every parameter set, so a change is
  observable by the cascade even when the state values compare equal.

### Resolution

- The scope broadcasts `State` when `State` is non-`null`.
- The scope broadcasts the service's state for `Key` when `State` is `null`, `Key`
  is set and an `IDrylAiActivityService` is registered.
- The scope broadcasts `AiState.None` when `State` is `null` and no `Key` is set.
- The scope broadcasts `AiState.None` when `State` is `null` and no
  `IDrylAiActivityService` is registered.
- Setting `State` to `AiState.None` forces AI off for the region, and is distinct
  from leaving `State` at `null`.
- A descendant's own explicit `Ai` value wins over the scope's state.
- A descendant whose `Ai` is `AiState.None` inherits the scope's state — `AiState.None`
  is the inherit case for a descendant, not an opt-out.
- A descendant's own explicit `Aura` value wins over the scope's variant.
- A descendant with no `Aura` and a scope that pins none resolves to `AiAura.Comet`.

### Service binding

- The component resolves `IDrylAiActivityService` optionally, so it works in an
  application that never called `AddDrylComponents()`.
- The component re-renders when the service reports a change for its own `Key`.
- The component ignores a service change reported for a different key.
- The component ignores service changes entirely while `State` is non-`null`.
- A scope with no `Key` never reacts to the service, whether or not one is
  registered.
- The component unsubscribes from the service when it is disposed (`CODE-05`).

### Keyboard, accessibility and motion

- The component contributes no element, no focusable node and no ARIA semantics;
  it changes neither the focus order nor the reading order of its children
  (`UX-07`).
- The component contributes no announcement of its own. Announcing AI activity is
  the job of the surfaces that light up inside it — `DrylAiIndicator` is the
  precedent `UX-04` names.
- The component has no animation. This is the explicit exception `DESIGN-11`
  allows for a component that genuinely has nothing to animate: it renders no
  markup, so there is no surface to move, and the motion belongs entirely to the
  descendants whose aura it drives.

### Appearance

- The component contributes no CSS and no class of its own.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`) — trivially, since it paints nothing.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders no markup and defines no style, so
  it is mode-agnostic by construction; `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have no rule of its to check.
- **Enter/exit animation** — none, and the exception is written out under
  "Keyboard, accessibility and motion" above on the terms `DESIGN-11` sets.
- **Keyboard and a11y** — the criteria above: no element, no focus stop, no ARIA.
- **AI mode** — yes, and it is deliberately **not** an opt-in: `State` is a
  broadcast override for descendants rather than styling for itself, so `AI-03`
  was never about it. See `_Api.md`.
- **Demo page** — `DRYL.Website/Components/Examples/AiActivity/ScopeCoordination.razor`.
- **`ComponentCatalog`** — registered as `"AI Activity"` / `ai-activity` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
