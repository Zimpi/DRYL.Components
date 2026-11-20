# The aura frames a surface, the content carries the rest

## Meta
- **State:** Adopted
- **Specs:** `specs/E7 Feedback/F3 DrylSpinner.md`, `specs/E7 Feedback/F4 DrylSkeleton.md`,
  `specs/E7 Feedback/F5 DrylProgress.md`, `specs/E8 Inputs/F9 DrylRadioGroup.md`,
  `specs/E8 Inputs/F10 DrylRadio.md`, `specs/E8 Inputs/F12 DrylRating.md`,
  `specs/E8 Inputs/F13 DrylSlider.md`, `specs/E8 Inputs/F14 DrylInputOtp.md`,
  `specs/E10 Navigation/F2 DrylStepper.md`, `specs/E10 Navigation/F3 DrylStep.md`,
  `specs/E3 AI/F7 DrylAuraElements.md`; rule `AI-08` in `harness/ai.md`

## Problem

Every AI-aware component shows its `AiState` the same way: `DrylAuraElements`
draws the ring, the comet and the halo around the component's wrapper. That
works where the wrapper is a surface the user can already see — a card, an
input, a dialog, a toast. It fails where the component has no surface of its
own, because the aura then draws a box that is not there at rest:

| Component | What the aura frames today | Finding |
|---|---|---|
| `DrylProgress` | `.progress-wrap`, a pill around the cell row | The comet orbits a row of separate glass cells; the cells themselves show nothing of the AI state. |
| `DrylSkeleton` | `.skel-wrap`, a rectangle around loose lines | A hard frame around placeholder lines. The shimmer already changes per state (`.ai-thinking .skel::before`, `.ai-streaming .skel`). |
| `DrylSpinner` | `.spinner-wrap`, a 32px circle | A second ring around a spinner. The tempo already changes per state (`--spin-tempo`). |
| `DrylRating` | `.rating-wrapper`, an input-like box around the stars | The stars already pulse, wave and flash in the AI colors; the frame is added on top and stretches over the full row width. |
| `DrylSlider` | `.slider-wrap`, a pill around track and knob | A frame around a track that has none at rest. |
| `DrylRadioGroup` | `.radio-group-wrapper` | A card appears around options that stand free at rest. |
| `DrylStep` | `.step-header` | The frame cuts through the connector line of the stepper. |
| `DrylInputOtp` | each `.otp-box-wrap` | Every box is a real surface, but six comets orbit at once. Borderline. |
| `DrylTab`, `DrylTimelineItem` | the tab pill, the round marker | Both are real, bounded surfaces. Reads fine. |

Evidence: screenshots of the `AiMode` examples on the docs website, light mode,
2026-11-19. Three of the components (`DrylSkeleton`, `DrylSpinner`,
`DrylRating`) already carry the state in their own marks **and** get the frame.

Target role: the end user who should feel where the AI is working, and the
Blazor developer who sets `Ai` on one of these components and gets a result
that looks misplaced.

## Solution Idea

One rule decides where the aura goes:

> The aura frames a surface. A component without a visible, bounded surface of
> its own at rest shows its `AiState` in its own marks — in the shared AI
> colors (`--ai-a`, `--ai-b`, `--ai-core`) and at the shared state tempo.

Three directions for the surface-less components:

- **A — the content carries the state (recommended).** No ring, comet or halo.
  The marks take the AI colors and move at the state's tempo. `DrylSkeleton`,
  `DrylSpinner` and `DrylRating` already do this and only lose the frame.
  `DrylProgress`: the lit cells take the AI gradient, the lead cell breathes at
  the state tempo, `Thinking` rolls the existing wave through the unlit cells,
  `Streaming` sweeps a sheen across the lit ones, `Generated` plays the
  existing row flash once. `DrylSlider`: fill and knob glow. `DrylStep`: the
  aura moves from the header to the step circle, which is a bounded surface
  like the timeline marker.
- **B — Aurora instead of Comet.** The existing `.ai-aura--aurora` edge, no
  travelling head. Cheapest, but it still draws a box around something that is
  not a box.
- **C — halo only.** Drop ring and comet, keep the breathing `.ai-aura-glow`
  behind the content. A soft bloom, no frame; weaker than A because the marks
  stay unchanged for `DrylProgress` and `DrylSlider`.

## Scope
- **In scope:** the AI visual of the surface-less components named above; a
  harness rule that states where the aura belongs; their specs and demo pages.
- **Out of scope:** components with a real surface (inputs, cards, dialogs,
  toasts, tables, `DrylTab`, `DrylTimelineItem`); the aura primitives
  themselves; a new `AiState`.

## Impact
- **Harness:** `AI-02` today says a component composes the aura primitives and
  "does not draw its own gradient border or glow". Direction A needs a new rule
  (next free ID in `ai.md`) that narrows `AI-02` to components with a surface
  and names the surface-less ones. New keyframes for `DrylProgress` and
  `DrylSlider` are new AI motion and need maintainer sign-off (`AI-04`,
  `DESIGN-10`). No new token, no new `AiState`, no dependency.
- **Specs:** `specs/E7 Feedback/F3 DrylSpinner.md`, `F4 DrylSkeleton.md`,
  `F5 DrylProgress.md`, `specs/E10 Navigation/F3 DrylStep.md`,
  `specs/E8 Inputs/F9 DrylRadioGroup.md`. `DrylRating` and `DrylSlider` have no
  spec yet; the first spec of a component removes its baseline line.
- **Public API:** no rename. The `Aura` parameter (`AiAura.Comet` / `Aurora`)
  loses its effect on components that no longer draw a ring; it stays and is
  documented as such — removing it would be MAJOR (`REL-01`).
- **Code:** remove `DrylAuraElements` from the affected `.razor` files, keep
  `AiAuraCss.Append` for the state classes and `AuraLifecycle` for the
  `Generated` one-shot; scoped CSS for `DrylSpinner`, `DrylSkeleton`,
  `DrylRating`, `DrylSlider`; the progress block in `dryl.css`. Risk:
  `Proposed` on the inputs (`DrylRating`, `DrylSlider`, `DrylRadioGroup`) has no
  frame to freeze any more and needs a resting look in the marks.

Prototype: artifact "AI-State ohne Rahmen"
(https://claude.ai/artifact/Qbt4HLjM8E3ZB2vm9JTx9S) — all eight components,
today next to A, B and C, across every `AiState` and both modes. What direction
A does there, per component:

| Component | Active | Thinking | Streaming | Generated | Proposed |
|---|---|---|---|---|---|
| `DrylProgress` | slow sheen over the lit cells | wave through the unlit cells, violet fill | fast sheen, cyan fill | row flash, once | like `None` |
| `DrylSkeleton` | violet tint on the lines | stronger tint, faster shimmer | violet-cyan shimmer (as today) | lines fade out (as today) | like `None` |
| `DrylSpinner` | tempo (as today) | tempo, violet | tempo, cyan | stops (as today) | like `None` |
| `DrylRating` | stars in AI color (as today) | pulse (as today) | wave (as today) | flash (as today) | stars rest in AI color |
| `DrylSlider` | light ring breathes around the knob | faster ring, violet fill | ring, cyan fill, sheen through the fill | ring runs out, once | resting ring |
| `DrylRadioGroup` | light ring breathes around the selected circle, dot in AI colors | faster, violet | cyan | ring runs out, once | resting ring |
| `DrylStep` | the aura (ring, comet, halo) sits on the step circle instead of the header | | | | |
| `DrylInputOtp` | boxes keep ring and halo; one light walks through them in turn instead of six comets | | | | frozen edge per box (as today) |

## Decisions
- 2026-11-19: The Product Owner chose direction A after viewing the prototype
  ("Richtung A"). B and C are dropped.

- 2026-11-19: The three borderline cases go in as the prototype shows them:
  `DrylRadioGroup` (light ring around the selected circle), `DrylStep` (aura on
  the circle), `DrylInputOtp` (walking light).
- 2026-11-19: Maintainer sign-off under `AI-04` / `DESIGN-10` for the new rule
  in `ai.md` and the new AI motion: the cell sheen and wave of `DrylProgress`,
  the breathing and run-out ring of `DrylSlider` and `DrylRadioGroup`, the
  walking light of `DrylInputOtp`, the tint of `DrylSkeleton`.
- 2026-11-19: The green halo behind the `DrylRating` stars in `Thinking` (the
  keyframe interpolates between a `color-mix` and a bare `var(--ai-a)`) is
  fixed within this idea.
- 2026-11-19: The Product Owner confirmed the final version as the prototype
  shows it ("Ja, alles freigeben").

## Open Points
