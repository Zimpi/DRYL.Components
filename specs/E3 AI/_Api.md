# AI — Public API

Shared enums, parameter contracts and services of the AI category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components/Components/AI/`

This file carries no `Meta` block: it is a reference for the specs around it, not
a unit of implementation (`SPEC-03`). The `AiState` and `AiAura` enums themselves
belong to `E1 Foundation` and are referenced here, never redefined.

*Partially filled. The `AiState` parameter contract below is complete, because
`AI-03` binds it and `ideas/I1 AI parameter naming for AI-native components.md`
settled it. The remaining shared types are filled in during phase C.*

## The `AiState` parameter contract

Every component in this category takes an `AiState` parameter, but they are not
all the same kind of parameter. `AI-03` in
[`../../harness/ai.md`](../../harness/ai.md) governs one of them: the **opt-in**,
the parameter that turns AI styling on for a component that would otherwise
render normally. The test is a property of the component:

> Does the component still render something meaningful with `AiState.None`?
> **Yes** → the parameter is an opt-in: named `Ai`, defaulting to `AiState.None`.
> **No** → the value is the component's content or its control input: it carries
> its own descriptive name and its own default.

### `Ai`

`[Parameter] public AiState Ai { get; set; } = AiState.None;`

The opt-in. Carried by `DrylToolCall`, `DrylToolCallGroup` and `DrylCanvas` —
each renders its own content with `AiState.None` (the tool-call card, the group
summary, the artifact tree), so AI styling is something they switch on rather
than something they are.

### `State` — obsolete alias

`[Obsolete] [Parameter] public AiState State { get; set; }`

On those same three components only, `State` remains as a delegating alias for
`Ai`. It exists so the rename is not a break: setting it is equivalent to setting
`Ai`, and a Razor call site that uses it compiles with `CS0618` naming the
replacement. Setting both on one component is not supported — the markup order
would decide, and that is not a promise this library makes.

The alias is removed in the next planned `3.0.0` (`REL-01` in
[`../../harness/releasing.md`](../../harness/releasing.md)). New code uses `Ai`.

### The non-opt-ins of this category

These are not `AI-03` cases. Each renders nothing meaningful with `AiState.None`,
so each names its parameter for what it is:

| Component | Parameter | What it is |
|---|---|---|
| `DrylAiIndicator` | `AiState State`, default `AiState.Active` | The value the pill displays. With `None` the component renders nothing at all, so `None` is not an "off" default — it is an empty component. |
| `DrylAiStream` | `AiState SettleTo`, default `AiState.None` | The state to settle to *after* the `AiState.Generated` reveal. The live state comes from the stream itself, so this is not a switch. |
| `DrylAiScope` | `AiState? State`, no default | A broadcast override. `null` means "follow `IDrylAiActivityService`"; `AiState.None` means "actively force AI off". A default of `AiState.None` would collapse the two and break the component. |

`DrylAuraElements` takes no `AiState` parameter: it renders the aura layers for a
host component that has already resolved the state, and is driven by an
`AuraLifecycle` instead.

## Aura resolution

`[Parameter] public AiAura? Aura { get; set; }`

Three components in this category take the aura variant as a nullable `AiAura`:
`DrylToolCall`, `DrylToolCallGroup` and `DrylAiScope`. `null` means "inherit", and
resolution runs `AiScope.ResolveAura(Aura, Scope)`: an explicit value wins,
otherwise the surrounding `DrylAiScope`'s variant, otherwise `AiAura.Comet`. There
is no "off" sentinel — `Comet` is a real default, so the opt-out is `null`.

`DrylToolCallGroup` is the documented exception to the fallback: it resolves to
`AiAura.Aurora` when neither the parameter nor the scope pins one. A collapsed
group is a dense secondary surface, and `Comet` would make it shout.

`DrylCanvas` carries **no** aura at all — no `Aura` parameter, no `.ai-aura`
classes. Its `Ai` value drives the build line and renders not-yet-valid nodes as
waiting skeletons instead of finished-broken placeholders. An artifact tree is
too large a surface for a glowing border (`DESIGN-08`), and the state is already
legible from the content.

`DrylAuraElements` also has a parameter spelled `Aura`, and it is a different
thing: an `AuraLifecycle`, not an `AiAura`. It renders the aura layers for a host
that has already resolved both, and takes no `AiState` parameter of its own.

**The AI state is not inherited from the scope in this category.** `DrylToolCall`,
`DrylToolCallGroup` and `DrylCanvas` read the surrounding `DrylAiScope` for the
aura variant only (where they have one); their `Ai` value stays explicit, because
a tool call's status is a fact about that call, not an ambient mood. This is
deliberate and is why they do not inherit `DrylAiAware`, whose `EffectiveAi`
resolves the state through `AiScope.Resolve` (see `E1 Foundation`).

## Remaining shared types

*(phase C — the canvas contract: `CanvasSpec`, `CanvasSelection`,
`CanvasPulseTracker`, `CanvasInteraction`, `CanvasActionOutcome`, `CanvasEdit`,
and the stream context types of `DrylAiStream`.)*
