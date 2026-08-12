# AI-Aura Redesign — „Comet" + „Aurora"

**Date:** 2026-07-12
**Branch:** `feat/ai-aura-redesign`
**Status:** Approved design → implementation

## Problem

The AI aura is a core DRYL feature, but today it under-delivers on two fronts:

1. **The states are barely distinguishable.** `Active` / `Thinking` / `Streaming`
   use the *same* rotating conic-gradient ring and glow; the only difference is
   **rotation speed** (6s / 1.8s / 3s) plus a small opacity change. Speed alone is
   not legible — the states read as identical. Only `Generated` stands out (its
   one-shot wash).
2. **The ring looks patchy on wide surfaces.** A `conic-gradient` is angular from
   the element centre. On a wide, short card the long top/bottom edges collapse
   into a narrow angular slice and land on the dim crossover of the gradient, so
   only the corners/short edges catch the vivid violet→cyan. (Reproduced and
   confirmed: even base border stays even; a wide box with the current conic goes
   dim across long edges.)

The aura must look **stunning** and each state must be **felt**, not read.

## Solution overview

Two variants of the *one* AI vocabulary, selectable via a new `Aura` parameter.
Both share an **even, aspect-independent base saum** (kills the patchy-edge
problem structurally) and a breathing halo. The "life" on top differs:

- **Comet (default)** — an even base hairline + a bright travelling light
  ("comet" with a soft specular head), plus per-state *character*.
- **Aurora (variant)** — same state semantics, but a soft, blurred, flowing
  edge field instead of a hard travelling point; calmer, safe to show many at
  once (dense AI pages).

Everything stays **strictly in-palette** and **theme-aware**: all colour comes
from the themeable `--ai-a` / `--ai-b` `@property <color>` tokens (default to the
accent seeds, overridable via `DrylTheme.AiAccent`, swapped per light/dark mode).
No component branches on the mode.

### Per-state character (both variants share the colour/halo language)

| State | Narrative | Comet motion | Colour weight | Form |
|---|---|---|---|---|
| **Active** | „I'm here, at rest" | 1 comet, slow (~9s), soft white head + short tail; gentle halo (~6s) | balanced a→b | subtle saum |
| **Thinking** | „concentrating, searching" | 2 counter-rotating comets, fast (~3.4s); tight fast halo pulse (~1.6s) | violet-dominant (`--ai-a`) | stronger saum, tight halo |
| **Streaming** | „output flowing out" | 1 comet (~5s) + directional sheen sweep in reading direction (~2.4s) | cyan-dominant (`--ai-b`) | wider, calmer halo |
| **Generated** | „done, here it is" | one-shot white **bloom** (edge flash decaying inward) + gentle lift → settle | warm→balanced | one-shot, no loop |

Aurora keeps the same state semantics but expresses them through drift
speed + halo intensity + the same colour weighting + the same Generated bloom,
with a blurred flowing edge field (drifting radial blobs) instead of a comet.

Colour weighting uses internal `--ai-hot` / `--ai-cool` vars that only bias
*between* the existing `--ai-a` / `--ai-b` — no new hues.

## API

```csharp
public enum AiAura { Comet, Aurora }   // Ai/AiAura.cs — first member = default
```

- **`DrylAiAware`** gains `[Parameter] public AiAura? Aura { get; set; }` and
  `protected AiAura EffectiveAura => AiScope.ResolveAura(Aura, Scope);`.
- **`AiScope`** gains `AiAura? Aura { get; init; }` and
  `static AiAura ResolveAura(AiAura? explicitAura, AiScope? scope) => explicitAura ?? scope?.Aura ?? AiAura.Comet;`.
- **`DrylAiScope`** gains `[Parameter] public AiAura? Aura { get; set; }` and puts
  it on the cascaded `AiScope` → the variant propagates to the whole subtree
  (the "dense page → set Aurora once" story).
- **InputBase family** (cannot inherit `DrylAiAware`) replicates the two members
  inline, exactly as it already does for `Ai` / `EffectiveAi`.

**Why `AiAura?` (nullable):** `null` = "inherit" (scope, else Comet). Unlike `Ai`,
`Aura` has no natural `None` sentinel — `Comet` (0) is a *real* default, not
"unset". Nullable expresses inherit-vs-explicit cleanly, needs no
`ParameterView` tricks, and works uniformly for the base class and the inline
(InputBase) hosts. Effective default remains **Comet**.

Usage: `<DrylCard Ai="Streaming" />` (Comet) ·
`<DrylAiScope Aura="Aurora"> … </DrylAiScope>` (whole subtree calm).

## CSS architecture (`dryl.css`)

**DOM contract unchanged.** Hosts still render `.ai-aura-ring` + `.ai-aura-glow`
(+ `.ai-aura-wash` for Generated). The only markup change: the host adds
`ai-aura--aurora` when `EffectiveAura == Aurora` (Comet is the base, no extra
class). Everything new comes from CSS + **pseudo-elements** — no new DOM nodes:

- `.ai-aura-glow` → breathing halo (box-shadow); `::before` → Streaming sheen
  (background-position sweep, clipped to `border-radius`).
- `.ai-aura-ring` → even base saum (masked, aspect-independent); `::before` →
  comet 1; `::after` → comet 2 (Thinking).
- `.ai-aura-wash` → Generated bloom (one-shot).
- `.ai-aura--aurora` overrides ring/glow to the flowing-field look.

New theme-aware token **`--ai-core`** (the comet's specular head): dark = near-
white bright core; light = a saturated accent core (white is invisible on a
light card — must not be a literal). Added to **both** LIGHT-TOKEN-SET copies so
`node scripts/check-light-sync.mjs` stays green.

Motion: the ambient **loop** durations (already 6s/4s/1.8s… today) are the
established exception to the three-durations rule; the shaped **one-shots**
(bloom, lift) use the standard `--dur-*` / `--ease-*`.
`prefers-reduced-motion: reduce` → both variants degrade to a static even saum
with no travel/drift (as today). The aura stays decorative and `aria-hidden`.

## Rollout

- **Phase 1 — Core.** Enum + plumbing (`DrylAiAware` / `AiScope` /
  `DrylAiScope`) + CSS core primitive rewrite + `--ai-core` token in both
  LIGHT-TOKEN-SET copies, with **DrylCard + DrylMarkdown** as reference hosts.
  Verify live (both variants × all states × light + dark).
- **Phase 2 — Propagation.** Thread `Aura` + the variant class through every
  AI-aware host and its adaptation (table, toast, inputs, file-upload,
  radio-group, chat-bubble, chart, `ai-indicator`). **Compact hosts (Button,
  Tab)** keep their slim comet saum and ignore `Aura` (documented — negligible
  at that size).
- **Phase 3 — Docs & release.** Agents demo gets an Aura toggle; `CHANGELOG.md`
  (Added: `Aura` / `AiAura` + "redesigned AI aura"); `DESIGN_TOKENS.md` (`--ai-core`)
  + `COMPONENT_PATTERNS.md` (aura variants) updated; `CONVENTIONS.md` §4 extended
  with `Aura`; `<Version>` MINOR bump (additive API + new look); register nothing
  new in `ComponentCatalog` (no new component) but note the change.

## Verification

- Live via the docs website (verify skill) + Playwright screenshots across both
  variants, all states, light and dark.
- bUnit tests for `AiScope.ResolveAura` (explicit wins, else scope, else Comet),
  mirroring the existing `AiScope.Resolve` tests.

## Non-goals

- No new mechanisms beyond ring + glow + pseudo-elements (no mesh, particles,
  JS-driven motion, canvas). "Wow" comes from choreography, not layers.
- No per-scale variant system — 1–2 large auras are the typical case; Aurora
  covers the "many at once" need.
- Button/Tab do not gain the Aurora look.
