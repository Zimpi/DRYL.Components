# The orb and the handoff lane wear the shared aura

## Meta
- **State:** Adopted
- **Specs:** `specs/E15 Agent Inputs/F5 DrylVoiceOrb.md`,
  `specs/E12 Agent Runtime/F4 DrylHandoffTrace.md`

## Problem

Two Agents components draw the AI aura by hand instead of composing
`AuraLifecycle`, `AiAuraCss` and `DrylAuraElements` (`AI-02`, `AI-07`):

- **`DrylVoiceOrb`** always carries `.ai-aura` with ring, comet and halo. The
  bare `.ai-aura` is the `Active` look, so an orb without a run, an idle run
  and a working run all show the same orbiting comet — an aura that runs while
  no AI is working (`AI-06`). `AiState.Proposed` has no mapping, a `Generated`
  is never retired, and nothing exits gracefully. Inside `DrylCanvasDock` the
  orb is mounted only during a session, so this shows where a host places the
  orb on its own.
- **`DrylHandoffTrace`** writes the three aura elements into the active lane
  under an `@if`. When the step finishes the aura is cut off at once, and a
  finished lane never plays the `Generated` one-shot every other AI surface
  plays.

## Solution Idea

Both components compose the shared primitives, so they behave like every other
AI surface: enter, live state, graceful exit, `Generated` one-shot retired by
`AuraLifecycle`.

What that means visibly:

- **Orb at rest.** With no run, or a run at `AiState.None`:
  - **A — the sphere alone.** No ring, no comet, no halo; the glass core sits
    still. The aura arrives when the session works and leaves gracefully.
  - **B — a resting ring.** A still, dim ring stays around the sphere so the
    orb reads as "an AI lives here" even when idle. That is a new resting look
    for `None` and needs a ruling under `AI-04`/`AI-06`.
  Chosen: **A** — it is what `None` means everywhere else.
- **Lane that finishes.**
  - **A — the one-shot.** A lane that completes plays `Generated` once (the
    aura runs out and settles), then is plain; a lane that fails or is stopped
    just fades the aura out.
  - **B — fade only.** Every lane fades the aura out, no one-shot.
  Chosen: **A** — "done" should look the same here as on a message or a card.
  A lane that was already done when the trace first saw it is shown settled and
  does not replay the one-shot: a trace opened on a finished run stays calm.

## Scope
- **In scope:** `DrylVoiceOrb` and `DrylHandoffTrace` — how the aura is
  composed, the resting look of the orb, the end of a lane.
- **Out of scope:** the orb's frost tier (it uses `--glass-fx-float` wherever
  it is placed — that belongs with the blur question of `DrylDepthGlass` and
  the backdrops); fixed English labels and literal geometry of both components;
  the level animation of the orb, which stays as it is.

## Impact
- **Harness:** way A on both points needs nothing new. Orb way B would be a new
  resting visual for `AiState.None` — a blocker needing sign-off (`AI-04`).
- **Specs:** `specs/E15 Agent Inputs/F5 DrylVoiceOrb.md`,
  `specs/E12 Agent Runtime/F4 DrylHandoffTrace.md`.
- **Public API:** none.
- **Code:** `DrylVoiceOrb.razor` (+ `.razor.css`: the muted look hangs on the
  aura elements), `DrylHandoffTrace.razor` — one `AuraLifecycle` per lane,
  which means lane state keyed by step or a small internal lane component.
  Risk: the orb's round shape and its level scale are built on the hand-written
  elements; the shared `DrylAuraElements` must fit the circle unchanged.

## Decisions
- 2026-11-20: Orb at rest way **A** — the sphere alone (Product Owner, on the
  Tech Lead's recommendation).
- 2026-11-20: Lane that finishes way **A** — the `Generated` one-shot; a failed
  or stopped lane fades out (Product Owner, on the Tech Lead's recommendation).
  With both recommendations accepted and nothing new from the feasibility
  check, the Tech Lead took that answer as the confirmation of the final
  version.
- 2026-11-20: Detail settled by the Tech Lead during implementation: a lane
  already done at first sight is shown settled, without the one-shot.

## Open Points
