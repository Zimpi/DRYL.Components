# A popover that flows out of its trigger

## Meta
- **State:** Adopted
- **Specs:** `specs/E11 Surfaces/F1 DrylPopover.md`

## Problem

`DrylPopover` opens with `popover-in` — 4 px slide plus fade on `--dur-fast` —
and leaves with `presence-out-fade` (I4). Both are correct and both are
anonymous: the panel appears *next to* the trigger, not *out of* it. Nothing in
the motion says "this button produced this panel", and the 3.13.0 visual
refresh has given its neighbours (Stepper, Alert, Sidebar) a signature movement
the popover does not have.

**Target role:** the end user of an app built on DRYL, opening any popover;
secondarily the developer who places a bare `DrylPopover` as a showcase surface.

## Solution Idea

Shown as a clickable prototype (today + three variants, slow motion, reach and
duration switches) on 2026-10-31:
<https://claude.ai/artifact/T2apHX3qTPHkuAFz4QYjJK>

- **A — Container morph** *(chosen)*: the panel starts exactly on the
  trigger's rectangle and radius (`clip-path: inset(… round …)` plus a
  `translateY` that lays the panel over the trigger, both measured per open)
  and grows on `--ease-spring` to its own size; the content fades in from
  about a third of the way; the drop shadow fades in once the morph has
  settled; the trigger gives a short squeeze as the panel leaves it. Close runs
  the same path back into the trigger. Crisp text throughout (nothing scales).
- **B — Liquid drop:** A plus a gooey neck on a separate layer. Rejected.
- **C — Squash and stretch:** `transform: scale` from the trigger point.
  Rejected — reads as a zoom.

Behaviour, derived from the prototype the Product Owner approved:

- **Open:** `--dur-slow` (420 ms), `--ease-spring`, on every popover.
- **Close:** the reverse path into the trigger on `--dur-med` (240 ms),
  `--ease-in-out`; the content fades out first. (The prototype closed in
  ~360 ms, which is no token — `--dur-med` is the nearest exit duration.)
- **Placement flip:** a panel placed above its trigger morphs from the
  trigger's top edge instead of its bottom.
- **Re-render while open** (filtering, selecting in a multi-select, changing
  the month) never replays the morph — only opening does.
- **Reduced motion:** no morph, no squeeze; the panel appears and leaves
  without animation, through the same exit path as today.

## Scope
- **In scope:** entrance and exit of the `DrylPopover` panel surface, and
  therefore of every component built on it: `DrylMenu`, `DrylSelect`,
  `DrylMultiSelect`, `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker`,
  `DrylCitation`, `DrylNotifications`, `DrylCanvasWorkspace` and the agents
  package's `DrylAiField`. The trigger squeeze.
- **Out of scope:** variants B and C; any parameter to switch the morph off or
  pick a variant; a per-component duration; the a11y deviations in the
  popover spec; other floating surfaces (`DrylTooltip`, `DrylDialog`,
  `DrylDrawer`); light/dark differences (the morph uses no colour).

## Impact
- **Harness:** new keyframes — morph in, morph out, the trigger squeeze and (found in the browser) a shadow fade — replace `popover-in`
  and the `presence-out-fade` binding on the panel → `DESIGN-10`/`DESIGN-13`,
  **signed off** by the Product Owner's choice of A (2026-10-31). Durations and
  easings from the existing set only (`--dur-slow`, `--dur-med`,
  `--ease-spring`, `--ease-in-out`, `--ease-out`). No new token, `AiState` or
  dependency. The per-open start geometry is written by JS as inline custom
  properties that the keyframes read once — set, never looped, so the
  compositor rules hold. The exit keyframe keeps the `presence-out` prefix (or
  `dryl.motion.onExit` gets the new name) so the I4 teardown path, its
  watchdog and the bUnit fallback stay as they are.
- **Specs:** `specs/E11 Surfaces/F1 DrylPopover.md` — the `Appearance and
  motion` criteria and the `Enter/exit animation` evidence line. Consumers'
  specs only where they state their own open motion.
- **Public API:** none.
- **Code:** `DrylPopover.razor.css` (keyframes, entrance/exit binding,
  shadow settle, reduced motion), `dryl.popover` in `dryl.js` (`place()`
  already measures anchor and panel — it also writes the start inset,
  `translateY` and trigger radius per open, and triggers the squeeze),
  `DrylPopover.razor` only if the exit needs the same geometry recomputed.
  **Main risk:** five consumers (`DrylSelect`, `DrylMultiSelect`,
  `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker`) pass
  `Surface="false"` and draw their own glass *inside* the panel. A `clip-path`
  on `.popover-panel` is a backdrop root, so their frost would only see the
  panel's own contents while the morph runs and snap back afterwards. The clip
  therefore has to sit on whichever element carries the glass — the panel
  itself with `Surface`, its first child without — measured in the browser,
  not assumed. Further risks: the panel lies over the trigger for the first
  frames (pointer events off until settled), and a panel that flips or
  clamps to the viewport edge must still start on the trigger.

## Decisions
- 2026-10-31 — Product Owner: the popover should "morph out of the element it
  comes from, like a liquid"; target is the 3.13.0 visual-refresh release.
- 2026-10-31 — Product Owner asked for the three variants as a prototype,
  including the select-like consumers and the date picker, before deciding on
  reach; specs and code only after the decision.
- 2026-10-31 — Product Owner chose **A (container morph), 420 ms, on all
  components**. This is the `DESIGN-10` sign-off for the new keyframes.

- 2026-10-31 — Product Owner confirmed the final version ("schieß los"): exit
  on `--dur-med`, trigger squeeze kept, no switch parameter.
- 2026-10-31 — **Adopted.** Carried into
  [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md).
  Three findings from the browser, recorded because they changed the build:
  the backdrop-root risk is real (Chrome 156: a clipped parent stops the
  child's blur at its own contents), so the morph runs on the panel's child
  without `Surface`; the start shape is measured on the trigger slot, because
  the docs page's anchor is block-level and 823 px wide around a 144 px button;
  and the spring's overshoot pushed the clip past the surface and cut the drop
  shadow into a hard grey slab in the light mode, so the shadow fades in only
  after the overshoot (`popover-morph-shadow`, a fourth keyframe under the same
  sign-off).

## Open Points
- None.
