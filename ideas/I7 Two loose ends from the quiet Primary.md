# Two loose ends from the quiet Primary

## Meta
- **State:** Ready

## Problem

`I6` redefined `ButtonVariant.Primary` as quiet at rest and moved the filled
treatment to `ButtonVariant.Bold`, shipped in `2.24.0`. Two consequences were
found while verifying it, recorded in `specs/E2 Actions/F1 DrylButton.md` and in
the pull request, and deliberately left unfixed because both sat outside what the
idea had scoped. They are collected here so they are decided rather than
inherited.

### A — The toggle modifier now outshouts the variant it sits on

`.btn--active`, which `DrylButton` applies while `Pressed` is `true`, draws a flat
`--accent-line` ring **and** a resting glow derived from `--accent-a`. That was
proportionate when it was read against a `ButtonVariant.Secondary` or a
`ButtonVariant.Ghost`, both of which carry no accent at rest.

It is no longer proportionate against the new `ButtonVariant.Primary`, which
carries an accent hairline and — deliberately — **no resting glow at all**. A
toggled-on Primary is therefore visually *louder* than an untoggled one, and
louder than the variant's own hover state. The loudness ordering is inverted: the
state a user switched on shouts more than the action they are about to take.

There is a second, smaller effect on the same button: `.btn--active` sets
`border-color` while `Primary`'s hairline is a masked pseudo-element, so a toggled
Primary renders *both* — a gradient hairline and a flat accent ring, one inside
the other.

`F1 DrylButton.md` states as a criterion only that the two stay
**distinguishable**, and they do. Distinguishable was the right bar for `I6`;
it is not the right bar for this.

**How urgent this is, stated honestly: not very.** Every `Pressed` call site in
the library today uses `Ghost` or `Secondary` —
`DrylButtonGroup`'s usage comment, `DrylCanvas`'s expand toggle,
`CanvasNodeView`'s pin toggle and `DrylCanvasDock`'s log toggle. Not one uses
`Primary`. The defect is latent and reachable only by a consumer who toggles a
Primary, which is why it is an idea rather than a hotfix.

#### What the code review on 2026-08-18 added

The defect is wider than `Primary`. `.btn--active` sits *after* every variant
rule in `dryl.css` and ties their specificity, and it does not add to
`box-shadow` — it replaces it. So it wins against all of them, and against the
two other variants that carry something at rest it is wrong in two further ways:

- **`ButtonVariant.Bold`** loses its four-layer accent glow *and* its inset
  `--on-accent-hi` highlight, keeping only the thin `--accent-line` ring. A
  toggled-on Bold is therefore **quieter** than an untoggled one — the same
  inversion as `Primary`, running the other way.
- **`ButtonVariant.Danger`** is ringed and glowed in `--accent-*` and loses its
  danger shadow, so the toggle overwrites the one thing the variant exists to
  signal. A destructive mode switch — "delete mode on" — is an ordinary
  consumer pattern, not an edge case.

That changes what the three options are worth. Option 3 would have to declare
`Pressed` unsupported on three of five variants, which makes the trap larger
rather than removing it, and the defect is no longer only reachable through a
combination nobody would build.

### B — `DrylCanvasDock`'s floating action button may want `Bold`

`I6`'s call-site sweep found exactly one genuine `Bold` candidate among the
library's fifteen: the collapsed-state FAB in
`code/DRYL.Components.Agents/Canvas/DrylCanvasDock.razor`. It floats over
application content with nothing competing against it, which is precisely the
hero case `Bold` exists for, and a quiet glass button in that position may lack
the affordance a floating entry point needs.

It was left alone for a reason that has not gone away: `specs/E14 Agent Canvas/`
holds `_Api.md` and `_Interop.md` and **no component spec**. `SPEC-01` forbids
writing code for a component whose spec has not been read, and a spec that does
not exist cannot be read. Changing the FAB's appearance would be exactly the drift
that rule exists to prevent.

Two things made the current state look defensible rather than merely rule-abiding:
the FAB carries `Ai="@DockAi"`, so in an AI state its edge is the aura's rather
than the variant's; and it sits inside a `DrylTooltip`, so its affordance does not
rest on fill alone. Both were arguments from the docs site, never from a real
application background.

#### What the by-eye check on 2026-08-18 found

`DRYL.Portfolio` embeds `DrylCanvasDock` at `/admin/assistant` and references this
repository by project, so it renders the library's own CSS. The collapsed FAB was
photographed there in both modes —
`docs/screenshots/2026-08-18-canvas-dock-fab-dark.png` and
`…-fab-light.png`.

**It reads weak, and in light mode it reads very weak.** In dark it is a dark pill
with a thin accent hairline in the corner: visible, but it reads as a small icon
button rather than as the way into the assistant — the sidebar's own
"KI-Assistent" nav item carries more presence than the entry point does. In light
it is a white pill with a pale hairline on a near-white ground, and on a wide
viewport it has to be looked for.

Neither defence survives that. The tooltip presupposes the button has already been
found, and the AI aura only speaks while the AI is working — at rest, which is
exactly when the affordance is needed, it contributes nothing. `Bold` is the right
variant here: the FAB is the hero of its corner and nothing competes with it.

## Solution Idea

Two separate decisions that happen to share an origin. They can be taken
independently and in either order.

**For A**, three shapes worth weighing, to be argued rather than assumed:

1. **Make the modifier relative to its variant.** `.btn--active` keeps its current
   treatment on the variants that carry no resting accent, and takes a quieter one
   on `ButtonVariant.Primary` — for instance the hairline going fully opaque and
   the fill taking the tint that hover otherwise brings, with no added ring. Most
   faithful to the design; the most rules to write.
2. **Make the modifier quieter everywhere.** Drop the resting glow from
   `.btn--active` entirely and let the accent ring alone carry the on-state. One
   change, applies uniformly, and would make every existing toggle in the library
   quieter — which may be an improvement or a regression depending on how the
   segmented groups then read.
3. **Declare the combination unsupported.** State in `F1 DrylButton.md` that
   `Pressed` is intended for `Secondary` and `Ghost`, and leave the treatment
   alone. Cheapest, and honest about how the component is actually used — but it
   pushes a discoverable combination onto the consumer as a trap rather than
   removing it.

**For B**, the sequencing is the decision, not the value: either `E14 Agent Canvas`
gets its component specs first and the FAB's variant is settled inside
`DrylCanvasDock`'s own spec as one line among many, or the FAB is judged by eye in
a real application and, if it reads weak, that finding becomes the reason to write
the spec sooner.

## Scope

- **In scope:**
  - The `.btn--active` treatment in `code/DRYL.Components/wwwroot/dryl.css` and
    the criteria describing it in `specs/E2 Actions/F1 DrylButton.md`.
  - The `Variant` of the collapsed FAB in `DrylCanvasDock`, and whatever spec work
    `SPEC-01` requires before it can be touched.

- **Out of scope:**
  - Any further variant. The `Success`/`Warning` question was raised and closed on
    2026-08-17 without an idea, and nothing here reopens it.
  - `ButtonVariant.Primary`'s own resting and hover treatment, settled by `I6` and
    shipped. This idea adjusts what sits *on top* of it, never it.
  - The `Bold` treatment itself, which is the previous Primary carried over
    unchanged on purpose.

## Impact

- **Harness:** no blocker, confirmed on 2026-08-18 against the chosen shape.
  **A** uses `--accent-line`, `--accent-a`, `--accent-b`, `--on-accent-line`,
  `--danger` and `--danger-fg` only, all present in both LIGHT-TOKEN-SET copies,
  and `.btn`'s existing transition already carries `background`, `border-color`
  and `box-shadow`, so no duration or easing is added. **B** needs no token at
  all; its cost is spec work, not design work.
- **Specs:** `specs/E2 Actions/F1 DrylButton.md` for **A**, whose "Toggle state",
  "Appearance" and "Motion" criteria all describe `.btn--active`; it returns to
  `State: Modified` on any change. For **B**, a new component spec
  `specs/E14 Agent Canvas/F1 DrylCanvasDock.md` — the category carries companion
  files only today, and this idea adds exactly that one component, not the rest.
- **Public API:** none expected. **A** is appearance behind an existing parameter;
  **B** is a call site inside a component. Option 3 for **A** would change no code
  at all, only what the spec promises.
- **Code:** for **A**, the `.btn--active` rules in `dryl.css`, and nothing in
  `DrylButton.razor` — the modifier class is already applied conditionally. For
  **B**, one attribute in `DrylCanvasDock.razor`, gated behind the spec work.

## Decisions

- 2026-08-17: Both points are recorded as **one idea rather than two**, because
  they share an origin — consequences of `I6` that were scoped out of it — and
  because neither is large enough to carry its own dialogue. They are still
  decided separately.
- 2026-08-17: Neither was fixed inside `I6`. Reason: the toggle treatment was
  named out of scope in that idea, and the FAB is blocked by `SPEC-01` on a spec
  that does not exist. Fixing either quietly during an implementation would have
  skipped exactly the process this repository runs on.

- 2026-08-18: **A** takes shape 1, **variant-relative**. Reason: the review that
  day showed the modifier also breaks `Bold` and `Danger`, which turns "quieter
  everywhere" into a fix for one third of the defect and "declared unsupported"
  into a promise that excludes most of the enum. Only a treatment defined against
  the variant it sits on restores the ordering everywhere.
- 2026-08-18: **A** is implemented now rather than parked as a spec change.
  Reason: with `Danger` involved the combination is one a consumer would
  plausibly build, so the defect is no longer purely latent.
- 2026-08-18: **B** was judged by eye in `DRYL.Portfolio` before any spec work, so
  the spec would be written for a confirmed need rather than a suspected one. The
  FAB reads weak in both modes and clearly weak in light.
- 2026-08-18: `SPEC-01` is satisfied for **B** by writing a component spec for
  `DrylCanvasDock` alone under `specs/E14 Agent Canvas/`, not the whole category.
  Reason: the by-eye finding justifies pulling that one component forward; the
  rest of `E14` has no such reason and stays on phase C's schedule.

## Open Points

- None.
