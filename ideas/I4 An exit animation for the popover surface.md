# An exit animation for the popover surface

## Meta
- **State:** Adopted

## Problem

`DrylPopover` animates in and never out. Measured at runtime on
`/components/popover`, sampling computed style per frame around the close:

```
t=502.9  >>> CLOSE CLICK
t=505.0  .is-open removed, panel content removed (same batch)
t=508.0  .is-positioned removed, node returned to the anchor
```

The surface is gone inside a single frame (≤16.7 ms) and no `*-out` animation
ever runs. `DESIGN-12` forbids exactly this: the panel content sits behind a
bare `@if (Open)` with no `DrylPresence`.

This is not one component's cosmetic debt. `DrylPopover` is the surface eight
components in four categories stand on — `DrylMenu`, `DrylSelect`,
`DrylMultiSelect`, `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker`,
`DrylCitation` and the agents package's `DrylAiField` — so every dropdown in
the library opens softly and then snaps out of existence.

**Target role:** the end user of any application built on DRYL. Secondarily the
Blazor developer placing a bare `DrylPopover`, who today inherits a
`DESIGN-12` violation they did not write and cannot fix from outside.

## Solution Idea

The obvious repair does not work, and this was **built and measured** rather
than argued: `DrylPresence` around the panel content animates *inside* the
surface. Over the whole exit the panel's own opacity stays `1.0` — for roughly
115 ms a fully opaque, empty glass box stands there and then jumps away in one
frame. That is worse than the jump it was meant to remove, not better.

Two further consequences of that shape, both measured:

- **ARIA.** The presence wrapper is a generic element between a container role
  and its required owned elements. Chrome's accessibility tree on
  `/components/menu`: `menu → generic → menuitem`. It breaks `menu/menuitem`
  and `listbox/option` (`DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete`);
  `role="dialog"` is unaffected.
- **A real contract change.** `DrylAiFieldTests.Prompt_enter_starts_run_with_typed_instruction`
  fails, because the content stays mounted after `Open` goes false until JS
  reports the exit — and under bUnit nobody ever reports it. Library code
  already relies on "content is gone the moment `Open` is false".

It can only look right if the **surface itself** exits. Two routes reach that,
and both sit on the sign-off bar:

1. **New `@keyframes popover-out`** beside the existing `popover-in` — a new
   animation (`DESIGN-13`, `CLAUDE.md` stage 1). Full control of the movement,
   at the cost of one more entry in the motion vocabulary that exactly one
   component uses.
2. **Bind an existing keyframe to the surface**, e.g.
   `.popover-panel.is-open.is-positioned.is-exiting { animation: presence-out-fade … }`,
   driven by `dryl.motion.onExit` called from `DrylPopover`. Precedent exists —
   `DrylDialogProvider` calls `dryl.motion.onExit` with its own prefix, and
   `dryl.css` binds `presence-in-left/right` to `.tab-panel--fwd/--back`. No new
   keyframes, but still a new animation on a surface that had none.

Route 2 is the cheaper one and reuses what exists, which is what `DESIGN-13`
asks for. **Route 2 is what was signed off** (see `## Decisions`).

Useful context for that decision, checked rather than assumed: `.popover-panel*`
is referenced **nowhere** outside `DrylPopover.razor.css` (only in `obj/`
artefacts). The blast radius of touching the surface is smaller than its
comments suggest.

## Scope

- **In scope:** an exit animation on the `DrylPopover` panel surface; the
  close-path state the component needs so the surface can still be seen while
  it plays; the teardown of the portal after the exit rather than during it; a
  fallback that closes the popover when no `animationend` arrives (no JS,
  reduced motion, a dropped circuit, bUnit); `prefers-reduced-motion`;
  the acceptance criteria and the `DESIGN-12` deviation in
  [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md).
- **Out of scope:** the three a11y deviations recorded in the same spec
  (`Escape` without focus in the panel, the panel's place in the tab order,
  focus restoration on close) — they share a component, not a cause. Any
  per-placement exit direction; the entrance animation; `DrylPresence` itself;
  every consumer's own panel, which keeps whatever motion it has.

## Impact

- **Harness:** one blocker, and it is the reason this idea existed —
  `DESIGN-10`/`DESIGN-13`: binding `presence-out-fade` to a surface that had no
  exit is a new animation. **Signed off by the Product Owner on 2026-08-17,
  route 2**, and explicitly as *extending the primitive* rather than as a
  one-off, which is the second decision `DESIGN-13` needed. No new token, no new
  duration or easing (`--dur-fast`, `--ease-out` are already on the entrance),
  no new `AiState`, no new dependency.
- **Specs:** [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md)
  — the `Appearance and motion` criteria, the `Enter/exit animation` line under
  cross-cutting evidence, and the `DESIGN-12` deviation, which is retired rather
  than reworded. No other spec has a criterion about this; `E10 Navigation` and
  `E8 Inputs` are still scaffolds.
- **Public API:** none intended. No new parameter, no renamed one, no changed
  enum, so no `REL-01` MAJOR. What *does* change is an unwritten behavioural
  contract: `PanelContent` currently unmounts in the same render that sets
  `Open` to `false`, and an animated exit means it stays mounted while the
  animation plays. That is the contract `DrylAiFieldTests` relies on, so it is
  named here rather than discovered in a red test.
- **Code:** `DrylPopover.razor` (close path, the exit state, the `OnExitFinished`
  callback and its fallback), `DrylPopover.razor.css` (the `.is-exiting` binding
  and its reduced-motion case), and `dryl.js` only if `dryl.popover.close` has to
  be sequenced after the animation rather than before it. Risks, in the order
  they are likely to bite: the click-outside path and the trigger-toggle path
  must not race a second open during an exit; a popover disposed mid-exit must
  still release its portal; and any consumer that reads `Open` to decide whether
  content is live sees a window where `Open` is `false` and the content is not
  yet gone.

## Decisions

- 2026-08-14: The `DrylPresence`-around-the-content repair is **rejected**. It
  was built and measured, not argued away: 115 ms of opaque empty glass, a
  generic element between `menu` and `menuitem`, and one red test.
- 2026-08-14: Filed as a blocked idea rather than fixed in place. Both remaining
  routes are a new animation, and `DESIGN-10`/`DESIGN-13` put that decision with
  the maintainer, not with the Tech Lead.
- 2026-08-17: **Route 2** — bind the existing `presence-out-fade` to
  `.popover-panel` in its exiting state, driven by `dryl.motion.onExit` from
  `DrylPopover`. Reason: it reuses the motion vocabulary instead of growing it,
  and the precedent (`DrylDialogProvider`, `.tab-panel--fwd/--back`) already
  exists in the library.
- 2026-08-17: The `DESIGN-13` follow-up question is decided with it — binding an
  existing keyframe to a new surface counts as **extending the primitive**, not
  as a one-off. Consequence, and the reason this is worth writing down: the next
  surface that needs an exit reuses `presence-out-*` on the same footing, and
  does not come back for its own sign-off.
- 2026-08-17: The three a11y deviations on the same component stay **out of
  scope** here and keep their own tickets. They were raised in the same
  conversation; folding them in would have made one animation decision carry
  three unrelated fixes.

- 2026-08-17: **Adopted.** Carried into
  [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md),
  whose motion criteria, cross-cutting `Enter/exit animation` line and
  `Recorded debt` section now hold the exit; the `DESIGN-12` entry is retired
  and replaced by the one deviation this shape really costs — the content
  outliving `Open`. Implemented in `7199ea9` with `DrylPopoverTests` as its
  regression net.
- 2026-08-17: One thing this idea did not foresee, recorded because it changes
  how the next such component is built: the second visibility key had to move
  from a class to `data-dryl-positioned`. Blazor rewrites the whole `class`
  attribute on every render, so the render that starts the exit was dropping the
  class JS had added — measured, not reasoned. The idea's `Code` impact called
  the close path the risk; the real risk was an attribute two owners were
  writing.

## Open Points

- None.
