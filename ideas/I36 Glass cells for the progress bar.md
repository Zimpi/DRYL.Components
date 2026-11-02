# Glass cells for the progress bar

## Meta
- **State:** Adopted
- **Specs:** `specs/E7 Feedback/F5 DrylProgress.md`

## Problem

`DrylProgress` is a gradient fill with a glow whose width glides when the value
changes. It works, but nothing on it is alive while the value stands still, and
the indeterminate sweep is the bar every library ships. It does not carry
DRYL's "glassy, alive" identity the way the organic spinners (I35) now do.

Target role: a Blazor developer showing an upload, a quota or a model's
progress, and the end user reading it.

## Solution Idea

The track becomes a row of glass cells:

- Each cell is a small glass pill. One accent gradient runs across all cells,
  so the row reads as one bar broken into glass.
- Cells fill one after another from the leading edge. The last lit cell is
  filled partially, so the fill keeps the precision of a continuous bar.
- A cell that fills completely springs up once. When the value rises, the
  newly filled cells do so in a stagger from the previous leading edge; on
  mount the row fills in from the left.
- The leading cell breathes while the bar stands still.
- At 100 % the row flashes once from left to right.
- Indeterminate: a wave of light rolls through the cells.
- Cell count: when `Max` is a whole number from 1 to 12, every unit is a cell
  (a wizard's "step 3 of 5"); otherwise the row has 20 cells.

Prototype: artifact "DRYL Progress-Varianten", direction B "Glaszellen",
next to two alternatives (Liquid, Beam).

## Scope
- **In scope:** the visuals and motion of `DrylProgress` (all variants, sizes,
  determinate and indeterminate); the cell rule; the demo page in
  `DRYL.Website`.
- **Out of scope:** new parameters or enum values, a choice between the old bar
  and the cells, changes to the shared aura vocabulary.

## Impact
- **Harness:** no new token, `AiState` or dependency. One-shot motion uses
  `--dur-med`, `--dur-slow`, `--dur-choreo` (a row flash, the case `DESIGN-10`
  names for it) on `--ease-out` and `--ease-spring`; every stagger is
  `--reveal-step` times an index. Continuous loops (breathing lead cell, the
  indeterminate wave) use `--ease-in-out`. New motion needs maintainer sign-off
  under `IDEA-05`; given 2026-11-02. Reduced motion stops every loop and rests
  the indeterminate bar as a full row (`UX-06`).
- **Specs:** `specs/E7 Feedback/F5 DrylProgress.md` — Description, Structure,
  Value and scale, Variants, Indeterminate, Appearance and Motion criteria
  rewritten for the cells; gaps updated.
- **Public API:** unchanged. The semantic variants now paint a gradient within
  their own hue instead of a flat color.
- **Code:** `DrylProgress.razor` (cell markup, stagger bookkeeping) and the
  progress block in `dryl.css`. `DrylCanvas` and the canvas `progress` node
  render `DrylProgress` and inherit the look.

## Decisions
- 2026-11-02: The Product Owner chose direction B, the glass cells ("B die
  Glaszellen. Die sind es. Setz es um.").
- 2026-11-02: The cells replace the bar as the one look of `DrylProgress`, with
  no new parameter, in line with I35. The Product Owner asked for the
  implementation directly, which is taken as confirmation and sign-off of the
  prototype's motion.

## Open Points
