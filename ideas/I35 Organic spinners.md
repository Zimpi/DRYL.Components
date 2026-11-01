# Organic spinners

## Meta
- **State:** Adopted
- **Specs:** `specs/E7 Feedback/F3 DrylSpinner.md`

## Problem

`DrylSpinner`'s three variants — a rotating gradient arc, a three-dot wave and
expanding pulse rings — work, but they are the generic loading indicators every
library ships. They do not carry DRYL's "glassy, alive" identity: nothing in
them morphs, flows or breathes, while the rest of the library (the aura, the
morphs on `--ease-viscous`) does.

Target role: a Blazor developer placing a loading indicator in a button, a row
or a panel, and the end user reading it.

## Solution Idea

Replace the visuals of the three variants with organic, morphing shapes. The
enum keeps its names, so the public API does not change:

- **`Ring` → Liquid.** A torus with an off-centre hole whose outline wobbles
  (`border-radius` morph) and whose wall breathes (an `@property` percentage on
  the mask). The rotation runs on its own layer at constant speed, decoupled
  from the morph, so it never brakes.
- **`Dots` → Goo.** Inline SVG: a core and three droplets that orbit out and
  are pulled back in, fused by a goo filter (blur plus alpha threshold) while
  they touch. Droplets staggered by a third of the loop.
- **`Pulse` → Bloom.** Three translucent membranes morphing at unrelated periods
  on `--ease-viscous`, a breathing `--ai-core` nucleus and a soft halo.

The AI rate cues move into one `--spin-tempo` multiplier on the wrapper (Active
1.35, Thinking 1.9, Streaming 0.55), multiplied into every loop duration.
`Generated` and reduced motion stop every loop and leave a resting shape.

Prototype: artifact "DRYL Organic Spinners", reviewed by the Product Owner in
two rounds.

## Scope
- **In scope:** the visuals and motion of the three existing variants; the
  shared tempo multiplier; structure tests for the new markup.
- **Out of scope:** new variants or enum values, renaming the enum values, new
  parameters, changes to the shared aura vocabulary, the demo pages in
  `DRYL.Website`.

## Impact
- **Harness:** no new token, `AiState` or dependency. New keyframes are
  continuous motion, which `DESIGN-10` leaves free of the duration scale; the
  one rotation uses `linear`, which `DESIGN-10` requires for rotation. Easing
  elsewhere uses the existing `--ease-in-out` and `--ease-viscous`. New motion
  needs maintainer sign-off under `IDEA-05`; given 2026-11-01. Reduced motion
  keeps the spec's "every loop stops" (`UX-06`).
- **Specs:** `specs/E7 Feedback/F3 DrylSpinner.md` — Description, Structure,
  Size, Appearance, Motion and AI-mode criteria rewritten for the new markup;
  the Dots pill shape is dropped; recorded gaps updated.
- **Public API:** unchanged. `SpinnerVariant.Ring`, `Dots`, `Pulse` keep their
  names and values; only their XML docs describe the new looks.
- **Code:** `DrylSpinner.razor` (markup, per-instance filter id) and
  `DrylSpinner.razor.css` (rewritten). Risks: `@property` support (graceful —
  the hole stays still), one SVG filter per Goo instance (small, scoped by a
  unique id).

## Decisions
- 2026-11-01: The Product Owner chose all three organic directions from the
  prototype ("an für sich finde ich die schon ziemlich geil").
- 2026-11-01: Liquid's rotation must be continuous and harmonious, not
  "wupp, wupp, wupp" — rotation decoupled from the morph and run at constant
  speed; morphs swing between two shapes instead of stepping through three.
- 2026-11-01: The enum names stay, so the redesign is not a breaking change.
- 2026-11-01: Product Owner confirmed the final version and asked to bring it
  into the release branch ("Das passt alles so. Machen wir so.").

## Open Points
