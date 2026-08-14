# An exit animation for the popover surface

## Meta
- **State:** Blocked — needs maintainer sign-off
- **Source of the finding:** [`../docs/2026-08-14-popover-exit-animation-plan.md`](../docs/2026-08-14-popover-exit-animation-plan.md)

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

## Why this is blocked rather than done

It can only look right if the **surface itself** exits. Both routes to that are
on the sign-off bar:

1. New `@keyframes popover-out` beside the existing `popover-in` — a new
   animation (`DESIGN-13`, `CLAUDE.md` stage 1).
2. Bind an existing keyframe to the surface, e.g.
   `.popover-panel.is-open.is-positioned.is-exiting { animation: presence-out-fade … }`
   driven by `dryl.motion.onExit` called from `DrylPopover`. Precedent exists —
   `DrylDialogProvider` calls `dryl.motion.onExit` with its own prefix, and
   `dryl.css` binds `presence-in-left/right` to `.tab-panel--fwd/--back`. No new
   keyframes, but still a new animation on a surface that had none.

Route 2 is the cheaper one and reuses what exists, which is what `DESIGN-13`
asks for. It is not what the plan prescribed, so it is not something to slip in
without a decision.

Useful for that decision: `.popover-panel*` is referenced **nowhere** outside
`DrylPopover.razor.css` (only in `obj/` artefacts). The blast radius of touching
the surface is smaller than its comments suggest.

## Documentation that does not hold

Found while measuring, both worth correcting whenever those files are next
touched:

- `DrylPopover.razor.css` claims dropping `.is-open` hides the panel
  "atomically … so no empty surface box ever flashes". That is a description of
  the very behaviour `DESIGN-12` forbids, written as if it were a virtue.
- The `dryl.popover` module comment claims a placeholder comment node is left
  behind when portalling. `open()` does no such thing — it is
  `document.body.appendChild`, and `close()` is `anchor.appendChild`.

## What the maintainer is asked to decide

Route 1 or route 2 (or neither, and `DESIGN-12` records `DrylPopover` as
documented debt with a reason). Route 2 additionally needs a decision on
whether binding an existing keyframe to a new surface counts as "extending the
primitive" (allowed after sign-off) or as a one-off (`DESIGN-13` forbids it).
