# A stepper that moves

## Meta
- **State:** Adopted
- **Specs:** `specs/E10 Navigation/F2 DrylStepper.md`

## Problem

`DrylStepper` reads as static in the demo: the active ring jumps from one
circle to the next, connectors stay grey unless a consumer marks steps
`Completed`, the body only fades in and vanishes instantly on leave (recorded
`DESIGN-12` debt), and the vertical body hangs below the whole track instead of
under its step. Dashed pending circles read as "broken" on the dark glass.
A wizard is where a user feels progress — the component should show it.

**Target role:** a Blazor developer building a multi-step flow, and that flow's
end user.

## Solution Idea

- **Gliding ring (horizontal):** one gradient ring for the whole track,
  translated by the active index in pure CSS (`--step-n`, `--step-i`); steps are
  equal-width, so no measuring and no JS. Travels on `--ease-spring`.
- **Connector fill:** each connector behind the active step fills with the
  accent gradient (`scaleX`/`scaleY`, compositor-only); a multi-step jump
  staggers the fills by `--dur-fast` per step. Explicit `Completed` stays
  `--success`.
- **Directional body:** every step body sits in a `DrylPresence`; forward moves
  enter from the right and leave to the left, backward the reverse, both in one
  grid cell. Closes the `DESIGN-12` body-exit debt.
- **Vertical accordion:** the body unfolds under its own header
  (`grid-template-rows 0fr → 1fr`, the existing expansion/nav pattern), the rail
  runs beside it; the active indicator springs into its own gradient ring.
- **Calmer pending state:** solid `--line-strong` hairline on `--glass-1`.

## Scope
- **In scope:** visuals and motion of `DrylStepper`; vertical body placement.
- **Out of scope:** public API changes, navigation guards, arrow-key navigation,
  a check-mark draw animation (would need a new keyframe → `DESIGN-10` sign-off).

## Impact

- No new token, keyframe, `AiState` or dependency. Uses `presence-*` slides,
  `--ease-spring`, `--dur-*`, `--accent-a/b`, `--success`, `--glass-1/2`.
- DrylMorph was considered and rejected: `IDrylMorph.RunAsync` must wrap the
  state change, but a stepper's change usually arrives as a bound parameter from
  the consumer's Next button, and FLIP over Blazor Server is known to flash.
- Behaviour change: the vertical body renders inside the track under its header;
  the previous body stays mounted (inert) for one exit animation.
- `DOM`: headers are wrapped in `.step-item`; `.step-header`, `.step-panel`
  keep their names (browser tests key on them).

## Decisions
- 2026-10-30 — Product Owner delegated the visual design to the Tech Lead
  ("mach einfach mal wie du denkst"), with a visual check before close.
- 2026-10-30 — Connector fill follows the active step (traversed), independent
  of `StepState`; selection still does not mark steps `Completed`.
- 2026-10-30 — Product Owner approved the result after the visual check in the
  browser ("Sehr schön gemacht"). Ships as part of the 3.13.0 visual-refresh
  release, collected on `release/3.13.0`.

## Open Points
- None.
