# DrylAuraElements

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylAuraElements.razor

## User Story

As a maintainer adding an AI-aware component to DRYL, I want one place that emits
the aura's layers, so that the thirty-odd surfaces that light up share one visual
vocabulary instead of each hand-rolling a ring and a glow.

## Description

`DrylAuraElements` is the shared child markup of an AI aura: the gradient ring, the
travelling comet, the breathing halo and the one-shot `Generated` wash. It is
dropped as the first child of an AI-aware host and renders nothing else.

It is a composition building block, not a component a consumer places. `AI-02`
requires that a new AI-aware surface compose the existing `.ai-aura*` primitives
rather than draw its own gradient border; this component is how that requirement is
met in practice, and it is used by `DrylToolCall`, `DrylButton`, `DrylTable`,
`DrylAiCanvas` and roughly thirty further components across both packages.

It carries **no `AiState` parameter at all**. The host has already resolved its
state; what it hands down is an `AuraLifecycle`, which is the mount lifecycle of
the aura rather than the AI state itself — that is what lets the aura animate *out*
as well as in instead of being yanked from the DOM. [`_Api.md`](_Api.md) records
this for the category, including the trap in the naming: the parameter is spelled
`Aura`, like the one on `DrylToolCall`, and is a different type.

The component only draws. The matching classes on the **host root** —
`ai-aura`, `ai-aura--aurora`, `ai-aura--out` and the state class — come from
`AiAuraCss.Append`. Without them the elements rendered here have nothing to style
them, which makes the pairing a contract rather than a convention.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Aura` | `AuraLifecycle` | — (`[EditorRequired]`) | The host's aura lifecycle: what state to render, and whether the aura is live or fading out. |
| `GenTick` | `int` | `0` | A counter the host bumps on each entry into `AiState.Generated`, so the one-shot wash re-keys and replays. |

The component takes no `AiState`, no `Class`, no `AdditionalAttributes` and no
`ChildContent`: it is markup a host embeds, not a surface a consumer configures.

## Acceptance Criteria

### Rendering

- The component renders the ring, comet and glow layers exactly while the
  lifecycle is present.
- The component renders nothing at all while the lifecycle is not present, so a
  host with AI off carries no aura markup.
- The component renders the layers while the lifecycle is exiting, so the aura
  stays in the DOM for its fade-out (`DESIGN-12`).
- The component renders the one-shot wash layer only while the lifecycle's render
  state is `AiState.Generated`.
- The component renders no wash while the lifecycle is exiting, so a spent reveal
  does not bloom again on its way off the surface.
- The wash is keyed by `GenTick`, so each entry into `AiState.Generated` replays
  the bloom rather than leaving the previous element in place (`AI-07`).
- The component composes only the `.ai-aura*` primitives and defines no gradient,
  glow or border of its own (`AI-02`).

### Host contract

- `Aura` is `[EditorRequired]`, so a host that forgets it is warned at compile
  time.
- The host root carries the matching aura classes, produced by `AiAuraCss.Append`
  from the same lifecycle and the resolved variant.
- The host root carries `ai-aura` exactly while the lifecycle is present.
- The host root carries `ai-aura--out` exactly while the lifecycle is exiting.
- The host root carries the variant class exactly when the resolved variant is
  `AiAura.Aurora`; `AiAura.Comet` is the unmodified look.
- The host root carries a state class for `AiState.Thinking`, `AiState.Streaming`
  and `AiState.Generated`; `AiState.Active` is the unmodified `ai-aura` look and
  gets none.
- The host drives the lifecycle by calling `AuraLifecycle.Sync` from
  `OnParametersSet` rather than managing the `Generated` one-shot by hand
  (`AI-07`).
- The host disposes the lifecycle with itself (`CODE-05`).

### Motion

- The comet orbits continuously, at a period that differs per state so speed alone
  reads as the state.
- The halo breathes continuously, at a period that differs per state in the same
  way.
- A directional sheen runs across the glow only under `AiState.Streaming`.
- The wash blooms once per entry into `AiState.Generated` and ends invisible.
- The comet and the halo retire themselves after the `Generated` one-shot, so a
  finished surface does not keep animating (`AI-06`).
- Leaving AI mode fades the ring, comet and glow over `--dur-slow` instead of
  unmounting them instantly (`DESIGN-12`).
- The exit window the lifecycle waits before unmounting is at least as long as that
  fade, so the dissolve completes before the elements leave the DOM.

### Accessibility

- The layers hold no text and are not focusable.
- The layers do not receive pointer events, so the aura never intercepts a click
  meant for the surface underneath.
- Every layer is hidden from assistive technology. `UX-07` requires that a purely
  decorative moving indicator carry `aria-hidden="true"` and names the AI aura as
  one of its examples; the ring, the comet, the glow and the wash each carry it.
  This matches the precedent set elsewhere in the library by the gliding
  indicators, `.tab-ink` in `DrylTabs` and `.ws-ink` in `DrylCanvasWorkspace`.
- Hiding the layers removes nothing usable from the accessibility tree: they are
  empty, hold no text and are not focusable, and the component accepts no
  `ChildContent`, so a host cannot place content inside them.

### Appearance

- Every color in the aura layers resolves to a token, including the AI hue pair and
  the accent core.
- The layers branch on no color mode and hold no mode-assuming value, so the same
  markup serves light and dark (`DESIGN-02`).
- The aura is an accent as `DESIGN-08` defines one: a 1px gradient border, a glow
  ring and a travelling highlight, never the fill of the surface it rings.

  The layers' geometry — the ring's padding, the comet's inset, the halo's blur
  radius — is written as literals in the shared
  `code/DRYL.Components/wwwroot/dryl.css`, and the continuous animation periods
  are literals too. `DESIGN-01` governs the first group; its check greps colors in
  `*.razor.css` only and does not see them. `DESIGN-10` does not govern the second:
  `node scripts/check-motion-tokens.mjs` judges each segment on its own and lets a
  continuous segment pick its period. Recorded here as documented debt, not as
  compliance.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the aura defines no mode-specific
  rule.
- **Enter/exit animation** — this component *is* the library's exit animation for
  AI mode: the lifecycle keeps it mounted while it dissolves. See "Motion" above.
- **Keyboard and a11y** — the "Accessibility" criteria above. The aura adds no tab
  stop and no announcement: it is `aria-hidden` on all four layers, takes no pointer
  events, and never changes the host's focus order (`UX-07`).
- **AI mode** — yes, and it takes **no** `AiState` parameter: the host has already
  resolved its state and hands down a lifecycle instead. `AI-03` has no subject
  here, since there is no `AiState` parameter to name. See `_Api.md`.
- **Demo page** — **none.** `SPEC-05` requires one and allows no written exception,
  so this is a gap and not a waiver. The reason it exists: no consumer places this
  component, so a demo page would have to be a page about the *inside* of another
  component's markup. What is demonstrated instead is the aura itself, on the
  surfaces that carry it — `DRYL.Website/Components/Examples/Ai/Lifecycle.razor`,
  `.../Ai/States.razor` and `.../Ai/ExpansionPanels.razor`. Recorded as debt; the
  maintainer decided on 2026-08-12 to document the gap rather than close it or
  amend `SPEC-05`.
- **`ComponentCatalog`** — **none.** The same gap and the same decision: the
  identifier `DrylAuraElements` appears nowhere in `DRYL.Website`, only inside
  other components' markup and in `AiAuraCss`. The aura is documented through the
  `"AI Mode"` / `ai` entry, whose `ClassName` names `DrylAiIndicator`.
