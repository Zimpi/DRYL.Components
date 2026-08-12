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

Five of this category's eight components take an `AiState` parameter, and they
are not all the same kind of parameter. `AI-03` in
[`../../harness/ai.md`](../../harness/ai.md) governs one of them: the **opt-in**,
the parameter that turns AI styling on for a component that would otherwise
render normally. The test asks what the parameter is:

> Is it a switch that turns AI styling on for a component that would otherwise
> render as an ordinary one?
> **Yes** → an opt-in: named `Ai`, defaulting to `AiState.None`.
> **No** → the value is the component's own content, its settle state, or a
> broadcast override: it carries its own descriptive name and its own default.

`DrylAuraElements` and `DrylCanvasWorkspace` take no `AiState` parameter at all.
`DrylAuraElements` renders the aura layers for a host that has already resolved
its state, driven by an `AuraLifecycle`; `DrylCanvasWorkspace` renders a plain
`DrylCanvas` and leaves AI to whatever wraps it.

### `Ai`

`[Parameter] public AiState Ai { get; set; } = AiState.None;`

The opt-in. Carried by `DrylToolCall`, `DrylToolCallGroup` and `DrylCanvas` —
each renders its own content with `AiState.None` (the tool-call card, the group
summary, the artifact tree), so AI styling is something they switch on rather
than something they are.

### `State` — obsolete alias

`[Obsolete] [Parameter] public AiState State { get => Ai; set => Ai = value; }`

On those same three components only, `State` remains as a delegating alias for
`Ai`. It exists so the rename is not a break: setting it is equivalent to setting
`Ai`, and a Razor call site that uses it compiles with `CS0618` naming the
replacement. Setting both on one component is not supported — the markup order
would decide, and that is not a promise this library makes.

The alias is removed in the next planned `3.0.0` (`REL-01` in
[`../../harness/releasing.md`](../../harness/releasing.md)). New code uses `Ai`.

### The non-opt-ins of this category

These are not `AI-03` cases. None of them is a switch, so each names its
parameter for what it is:

| Component | Parameter | What it is |
|---|---|---|
| `DrylAiIndicator` | `AiState State`, default `AiState.Active` | The parameter **is** the component's content: the pill exists to display an AI state, and `State` selects which one — including the idle "AI" label it shows for `None`. Not a switch, so `None` would not mean "off"; it would mean "display idle". |
| `DrylAiStream` | `AiState SettleTo`, default `AiState.None` | The state to settle to *after* the `AiState.Generated` reveal. The live state comes from the stream itself, so this parameter never turns anything on — it says where to land. |
| `DrylAiScope` | `AiState? State`, no default | A broadcast override for descendants rather than styling for itself. `null` means "follow `IDrylAiActivityService`"; `AiState.None` means "actively force AI off". A default of `AiState.None` would collapse the two and break the component. |

`AI-03` names five legitimate non-opt-ins where this table names three. The two
missing ones — `DrylAiGenerate` and `DrylAiBuild`, both carrying `SettleTo` for
the same reason as `DrylAiStream` — live under
`code/DRYL.Components.Agents/Generation/` and therefore belong to `E15 Agent
Inputs`, not to this category.

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

## `AiStreamContext`

`code/DRYL.Components/Ai/AiStreamContext.cs` — the render context `DrylAiStream`
hands to its `ChildContent`.

| Member | Type | Purpose |
|---|---|---|
| `Text` | `string` | The text streamed so far, in arrival order. |
| `State` | `AiState` | The stream's current state: `Thinking` → `Streaming` → `Generated` → the value of `SettleTo`. |

Both setters are internal: a consumer reads the context, it never drives it. The
component reuses **one** instance for the lifetime of the stream and mutates it in
place, so the context is read during render and not cached across renders. It is
the only shared type this category owns outright — everything else it exposes
belongs to `E1 Foundation` (`AiState`, `AiAura`, `AiScope`, `AuraLifecycle`,
`IDrylAiActivityService`) or to the canvas contract below.

## Remaining shared types

*(phase C — the canvas contract: `CanvasSpec`, `CanvasSelection`,
`CanvasPulseTracker`, `CanvasInteraction`, `CanvasActionOutcome`, `CanvasEdit`.
`CanvasWorkspace`, `CanvasView` and `CanvasHistory` are described in the criteria
of `F8 DrylCanvasWorkspace.md` and still need their contract stated here.)*
