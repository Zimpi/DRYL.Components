# A panel that reveals its body

## Meta
- **State:** Adopted
- **Specs:** `specs/E9 Layout/F19 DrylExpansion.md` (new), `specs/E9 Layout/_Interop.md`

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0:

> "Komponente um die es geht: DrylExpansion. Eine mögliche Idee: Den Body
> enthüllen wie den Stepper-Body in 3.13 (Blur und Fade beim Aufklappen),
> Chevron mit Spring, Hover-Light auf dem Header."

`DrylExpansion` has moved the same way since before the 3.13 visual refresh: the
grid track grows, the chevron turns on `--ease-out`, and the header does nothing
under the pointer except tint its chevron. The content is simply uncovered by
the growing track, so a panel opening reads as a box getting taller rather than
as something being shown. Stepper, drawer, menus, tooltip and progress have all
been given their own movement in 3.13 to 3.16; this panel now looks left behind
next to them.

Target role: the end user opening a panel; the Blazor developer, who gets the
motion with no new parameter. `DrylToolCallGroup` and `CanvasNodeView` render a
`DrylExpansion` and inherit whatever is decided here.

One correction to the brief: the 3.13 stepper body has no blur.
`DrylStepper.razor.css` uses the same `grid-template-rows` track, and its
horizontal body uses `DrylPresence` slides. A blur on reveal would be new in the
library's in-flow components, though not a new token or keyframe.

## Solution Idea

Three variants in the prototype artifact
`https://claude.ai/artifact/2Q3yNTWxWx1a4Fd9MDg9ud` (with today's look as a fourth tab):

- **A — Fokus.** The Product Owner's idea. The body comes into focus while the
  track grows: `opacity` and `filter: blur(var(--sp-1))` to sharp on `--dur-slow`
  / `--ease-out`; closing is quick, on `--dur-fast`. Chevron on `--ease-spring`.
  An accent spot follows the pointer across the header, as on `.nav-item::after`.
  *Tech Lead recommendation.*
- **B — Kaskade.** The body's direct children rise one after another
  (`calc(var(--reveal-step) * var(--i))`, a shift of `--sp-2`). A neutral glass
  light under the pointer; the chevron sits on a small glass disc that springs.
- **C — Naht.** A 1 px accent seam draws out from the point of the click along the
  header's lower edge, and the body is uncovered from the seam downwards behind a
  soft edge (`mask-size`). Under the pointer, a light runs along that edge. The
  open chevron glows.

## Scope
- **In scope:** body reveal, chevron, header hover light, in both modes.
- **Out of scope:** public API changes; stacking behaviour and the open panel's
  detach; a single-open group; AI aura and `Generated` wash.

## Impact
- **Harness:** every variant uses existing tokens and the three durations and
  easings, with transitions only: no new token, no new keyframe, no new
  dependency. The blur radius in A reads `--sp-1`.
- **Risk, A:** `filter` is not compositor-cheap on a tall body, and only for the
  length of the transition. It ends on `none`, so at rest it creates no
  containing block for `position: fixed` descendants. To be measured on a long
  body.
- **Risk, B:** the stagger needs real child elements. Bare text and a single
  wrapping `div` move as one block, and the delay stops at the last enumerated
  `nth-child`.
- **Risk, C:** a mask at rest would make the body a backdrop root and kill the
  frost of any glass nested in it. Resolved during the build, see Decisions.
- **Specs:** `DrylExpansion` has no spec. It gets its first under `E9 Layout`,
  and its baseline line in `scripts/spec-coverage-baseline.json` is removed.
- **Public API:** none.
- **Code:** rules in `wwwroot/dryl.css`; a delegated `dryl.expansion` in
  `wwwroot/js/dryl.js` for the pointer. The component makes no JS call.
- **Website:** the expansion demo page text and the catalog summary.

## Decisions
- 2026-11-02: Variant **C — Naht** is built (Product Owner: "Wir nehmen C-Naht.
  Setz es um"). A and B are dropped.
- 2026-11-02: The seam, the mask and the glowing chevron are the default look of
  every `DrylExpansion`, with no opt-out parameter (Tech Lead default, in line
  with I38).
- 2026-11-02: The mask leaves after the reveal instead of staying at rest:
  `mask-image` transitions with `allow-discrete`, held by `--delay-long`. A browser
  without it shows the track's reveal without the soft edge. This takes away the
  risk named for C (Tech Lead).
- 2026-11-02: The pointer is read by listeners delegated from the document, as in
  `dryl.tooltip`, so a panel has no interop and nothing to dispose. The existing
  `dryl.spotlight.track` was not reused: it needs a per-element call and writes on
  every move (Tech Lead).

## Open Points
