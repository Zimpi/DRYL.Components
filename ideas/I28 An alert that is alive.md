# An alert that is alive

## Meta
- **State:** Adopted
- **Specs:** `specs/E7 Feedback/F1 DrylAlert.md`

## Problem

`DrylAlert` reads as dead in the demo: a neutral glass box with a tinted icon
chip, identical for a routine notice and a failure. The only motion it owns is
the dismiss button's hover and the self-dismiss fade. An alert exists to draw
attention to something — today nothing about it says "look here", and the
`Kind` is only legible through a 28px chip.

**Target role:** a Blazor developer placing a notice, and that page's reader.

## Solution Idea (options, Tech Lead)

- **A — Ember drift:** two soft radial glows in the `Kind`'s colour, clipped
  inside the alert, drifting slowly behind the content (transform-only,
  10–20 s, `infinite alternate`). Low opacity, so the surface stays neutral
  glass and the accent stays a gradient (`DESIGN-08`). Tempo scales with
  weight: Info/Success calm, Warning/Danger a little livelier.
- **B — Arrival, then rest:** a one-shot on mount — a light wave sweeps from
  the icon across the alert, the chip pops — then the alert settles to a
  barely-moving glow.
- **C — Sonar:** concentric rings pulse outward from the icon chip every few
  seconds; only for Warning/Danger.

Recommendation: **A + the arrival of B** — always quietly alive, with one
moment of attention when it appears.

## Scope
- **In scope:** visuals and motion of `DrylAlert`.
- **Out of scope:** public API changes, toast, alert dialog.

## Impact

- Needs **new keyframes** (drift, arrival) → `DESIGN-10` sign-off by the
  maintainer. No new colour token expected: glows derive from `--success`,
  `--warning`, `--danger`, `--accent-a/b` via `color-mix`.
- Must not look like the AI aura: `AlertKind.Ai`'s ambient may not imitate
  `AiState` visuals (`AI-02`, `AI-04`), and with `Ai` set the aura must still
  dominate.
- Performance: transform/opacity only, no looped custom properties or
  `background-position` (compositor rules); a page with ten alerts stays smooth.
- `prefers-reduced-motion`: static glow, no drift, no arrival.
- Changes the spec's Motion section ("no mount animation of its own") and the
  dismissal contract: `OnDismiss` is raised after the exit (about `--dur-choreo`
  later than before), and the self-dismiss `DrylPresence` wrapper is gone.
- New shared primitive `dryl.motion.collapse` / `restore` (JS, Web Animations),
  reusable by any in-flow element that leaves its place.

## Decisions
- 2026-10-30 — Product Owner: alerts should feel alive — "leichtes Brodeln im
  Hintergrund, wie Wellen". Target: the 3.13.0 visual-refresh release.
- 2026-10-30 — Product Owner chose option A + arrival (ember drift plus a
  one-shot arrival wave), for all kinds, tempo scaled by weight.
- 2026-10-30 — Product Owner signed off the new keyframes (`DESIGN-10`) and
  asked for a prototype first, shown in the browser; spec and release work
  follow only after approval.
- 2026-10-30 — Product Owner: dismissing should not just fade but shrink away
  and look great — also when no host wraps the alert in `DrylPresence`.
  Tech Lead: a two-beat exit on the alert itself (`dryl.motion.collapse`:
  shrink + blur + fade, then the space closes and neighbours glide up), for
  both lifetime owners; with a handler, `OnDismiss` is raised after the exit,
  and a host that keeps the alert gets it back (`dryl.motion.restore`). The
  self-dismiss `DrylPresence` wrapper is gone.

- 2026-10-30 — Product Owner asked to drop the `DrylPresence` around the first
  dismissible demo alert (it would exit twice), approved the result in the
  browser ("sieht gut aus") and asked to finish it into `release/3.13.0`.

## Open Points
- None.
