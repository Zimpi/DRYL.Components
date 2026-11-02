# A badge with a pulse and a counter

## Meta
- **State:** Adopted
- **Specs:** `specs/E5 Data/F3 DrylBadge.md`, `specs/E5 Data/_Api.md`

## Problem

Raised by the Product Owner on 2026-11-02, for the 3.16.0 rework:
"DrylBadge — aktuell komplett statisch."

The spec already recorded the debt (`DESIGN-11`, `DESIGN-12`): no enter, no
transition, a dot that exists to say "live" and does not move, and a status
badge whose kind changes from `Success` to `Danger` snaps between two colours.
Every other surface reworked for 3.16.0 gained a signature motion.

Target role: a Blazor developer showing a status, a count or a version beside
a row or heading, and the end user scanning a table of them.

## Solution Idea

Three directions, side by side in a prototype artifact
(`https://claude.ai/artifact/FXZfXneNdcXubr67vjbozS`), all on existing tokens:

- **A — Atem.** Quiet: colors glide, the live dot breathes. CSS only.
- **B — Glanz.** Glass: the new tone floods out of the dot, a sheen passes once.
- **C — Puls & Zähler.** Expressive: the pill grows out of its mark, a kind
  change squishes it and flashes a ring in the new tone, the dot beats in a
  tempo per kind (calm, quicker, double beat for Danger), and numbers roll
  like an odometer — only the digits that changed, in the direction of the
  change — while the width follows the label.

## Scope
- **In scope:** the visuals and motion of `DrylBadge`; a `Value` parameter so a
  label change can be detected; the demo page in `DRYL.Website`.
- **Out of scope:** a `Live` switch for the beat, an AI state, a dismiss or
  press affordance (the badge stays a label).

## Impact
- **Harness:** no new token, easing or `AiState`. One-shot motion uses
  `--dur-fast/med/slow/choreo`, `--ease-out/spring`, `--delay-short` and
  `--reveal-step` times an index; the beat is continuous motion on
  `--ease-in-out` with free periods (`DESIGN-10`). New motion needs maintainer
  sign-off (`IDEA-05`); given with the choice of C. Reduced motion switches
  every animation off (`UX-06`).
- **Specs:** `F3 DrylBadge` — Description, Public API, Structure, a11y and a
  new Motion section; the "nothing is animated" gap closed, two narrower gaps
  recorded. `_Api.md` unchanged (no new enum).
- **Public API:** new `Value` (`string?`), additive (MINOR). Without it a
  `RenderFragment` label cannot be compared between renders.
- **Code:** `DrylBadge.razor` (change tracking, digit diff, alternating
  kind-change class) and the badge block in `dryl.css`. No JS: restarts use
  `@key` and two identically drawn classes; the width follows `--badge-len`
  monospace cells. Every `.badge` in the library (list items, canvas, agents)
  gains the enter and the beat.

## Decisions
- 2026-11-02: The Product Owner chose direction C ("Wir nehmen hier C · Puls &
  Zähler. Bring es dann in den Release branch mit rein"), taken as sign-off of
  the prototype's motion and of the `Value` parameter it needs.
- 2026-11-02: The open question of a `Live` switch was not raised again; the
  beat follows the prototype — Success, Warning and Danger dots beat, Neutral
  and Accent rest. A switch can follow as its own idea.

- 2026-11-02: Renumbered from I38 to I40 on merge: a parallel session had
  filed I38 (gliding highlight) and I39 on the release branch first. The
  commits and the branch `feat/i38-badge-pulse` carry the old number.

## Open Points
