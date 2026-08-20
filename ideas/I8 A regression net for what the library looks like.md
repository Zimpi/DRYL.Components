# A regression net for what the library looks like

## Meta
- **State:** Draft

## Problem

**Nothing in this repository checks what DRYL looks like.** 1066 tests assert
markup: classes, attributes, rendered children, the state machine behind an exit
animation. Not one of them can see a colour, a size, a shadow or a layout. For a
library whose product *is* its appearance, that is the gap in the middle.

It is not hypothetical, and the repository already says so in its own words.
`specs/E11 Surfaces/F1 DrylPopover.md` carries, under **Recorded gaps**:

> **Most of this file has no regression net.** … the portal, the placement, the
> dismissal, the focus behaviour, the trigger's ARIA claim — still rests on
> reading the code and on measurement in a browser, because bUnit executes no
> `dryl.js` and manages no real focus. Tests that claimed otherwise would be
> lying, which is why there are none.

Three separate findings in the single session of 2026-08-20 rested entirely on
measurements taken by hand in a browser and recorded in prose:

| Finding | What proved it | What guards it now |
|---|---|---|
| The portal discarded the scroll state of what it moved | `scrollTop` 0 vs. 310/854 at `/components/timepicker` | nothing |
| `Block` stretches, the caret keeps its width | 420 / 383 / 38 px measured | nothing |
| A toggled `Bold` read *quieter* than an untoggled one (`I7 A`) | by eye, both modes | nothing |

Each was real, each was fixed, and **each can silently come back tomorrow.** A
change to one token in `dryl.css` ripples through every component in the
library; nothing would report it. That is the shape of the risk: not a component
breaking loudly, but the whole system drifting quietly.

**What happens today without the feature.** Visual correctness is established by
whoever is at the keyboard opening the docs site and looking — which is the
maintainer, or an agent that will not be in the room next week. `DESIGN-02` says
both colour modes are checked by eye and has no exception route, so every change
that touches appearance costs a manual pass, and every manual pass is as good as
the attention on it that day.

**Target role:** the maintainer, as the reviewer who has to believe a change is
safe; and any agent working on the library, which today has no way to prove a
CSS change did not move something three components away.

## Solution Idea

Render the docs site in CI, screenshot it, diff against approved baselines, fail
the PR on an unexplained difference — and make updating a baseline a deliberate,
reviewable act.

The shape is conventional. The two decisions that are *not* conventional, and
that this idea exists to settle, are below.

### The hard part: DRYL moves, on purpose

Rule 2.12 — **every component is deliberately animated** — is what makes this
harder here than in an ordinary component library. Measured in `code/`:

- **147 `infinite` animations** across the CSS.
- Only **12** of the `prefers-reduced-motion: reduce` blocks in `dryl.css`
  actually set `animation: none`; the other guards drop a transition, hide a
  pseudo-element or shorten something.

So **`prefers-reduced-motion` alone does not freeze the page**, and a screenshot
taken at an arbitrary moment of a 6-second `ai-comet-spin` is a coin toss.
Playwright's `animations: "disabled"` does freeze it — finite animations are
fast-forwarded to their end state, infinite ones are cancelled to their initial
state — which makes the shot reproducible.

**The price is worth naming out loud rather than discovering later: this net
catches composition, not motion.** Layout, colour, spacing, radius, shadow,
glass, the shape of a state — all covered. Whether the comet actually spins,
whether the exit animation runs, whether a glide lands where it should — not
covered, and still resting on browser measurement. An honest name for this
feature is *appearance regression*, not *visual regression*, and it should not be
sold to a future reader as more than it is.

### The second hard part: DRYL is made of glass

`backdrop-filter` blur is the one CSS feature whose rasterisation differs most
between platforms, GPUs and driver versions — and it is on `--glass-fx-float`
and `--glass-fx-flow`, which is to say on most floating and in-flow surfaces in
the library. A baseline captured on the maintainer's Windows machine will not
match a Linux CI runner, and may not match the same runner after an image
update.

That is survivable but it dictates the design: **baselines are captured by the
CI container and only by it**, never locally, and the container is pinned. The
local workflow becomes "push, let CI produce the diff", which is slower than
running it on your own machine and is the price of the numbers meaning anything.
A per-pixel tolerance is the usual escape hatch; it should be set *low* and
argued, because a tolerance wide enough to absorb GPU differences is also wide
enough to absorb the accent hairline that `I6` spent a whole idea on.

## Scope

- **In scope:**
  - A screenshot suite driving the docs site (`DRYL.Website`) against the
    working-tree library, in **both colour modes** (`DESIGN-02`).
  - Baselines stored, reviewed and updated deliberately.
  - A CI job that fails a pull request on an unapproved difference, with the
    diff image reachable from the run.
  - The decision of which repository owns it and what it gates.

- **Out of scope:**
  - **Motion.** See above — frozen frames cannot assert an animation, and
    pretending otherwise would be the same lie the popover spec refused to tell.
  - **Accessibility.** Contrast, ARIA and keyboard belong to the a11y audit
    already on the backlog; a screenshot proves none of them.
  - **Responsive breakpoints beyond whatever widths are chosen here.** The
    responsive foundation is container-query-first and deserves its own
    treatment rather than a bolt-on.
  - **`DRYL.Portfolio`.** It vendors GSAP and three.js and is a consumer, not
    the library.
  - Retrofitting baselines as *specifications*. A baseline records what the
    library looks like today, including its current defects; it is a change
    detector, not a statement that the current appearance is correct.

## Impact

- **Harness:** one blocker, and it is smaller than it first looks.
  `CODE-03` forbids external **runtime** dependencies — "zero npm packages, zero
  JS frameworks", with `Markdig` as the single approved exception — and its
  documented check is `rg -n '<PackageReference' code/*/*.csproj`, which is
  scoped to the two **shipped** projects and does not read `tests/`. The tool
  for this is `Microsoft.Playwright`, a .NET NuGet under `Microsoft.*` — the two
  properties the rule's own exception bar asks for ("a .NET NuGet only, never
  npm/JS"). It ships to no consumer and appears in no `.nupkg`.
  **Nevertheless `IDEA-05` requires this to be signed off explicitly, not
  reasoned around, and it is Open Point 1.** Two real costs come with it: the
  package downloads browser binaries (~150 MB) on first use, which is a CI
  minute and a cache entry, and `dotnet test` would no longer be self-contained
  on a fresh machine. No new token, no new animation, no new `AiState`.
- **Specs:** none is contradicted, and one is directly served —
  `specs/E11 Surfaces/F1 DrylPopover.md` names this feature by description as the
  route that would cover its untestable ground. If this lands, that recorded gap
  and the equivalent one added for the portal's scroll state can both be
  narrowed rather than merely restated. No new component, so `check-spec-coverage`
  does not move.
- **Public API:** **none.** No parameter, no enum, no service, nothing a
  consumer can see. This is entirely a development-time net.
- **Code:** nothing under `code/` changes. The touch points are a new test
  project or a new job, `.github/workflows/ci.yml` here and/or
  `docker-publish.yml` in `DRYL.Website`, and a baseline directory. The
  cross-repo mechanics are already proven: `DRYL.Website`'s existing workflow
  checks the site out into a subfolder and `git clone`s `DRYL.Components`
  alongside it, which is exactly the layout its `ProjectReference` expects — the
  same trick works in the other direction.
  **The main risk is not technical, it is the failure mode of the thing itself:**
  a suite that reports differences nobody caused is abandoned within a month, and
  an abandoned suite is worse than none because its green is believed. Every
  design decision below should be read as an answer to that risk.

### Three shapes, with a recommendation

1. **Per-example screenshots, in `DRYL.Website`, gating that repo's PRs.**
   Finest diffs — a failure names the example, not the page. Roughly 96 catalog
   entries × several examples × 2 modes: many hundreds of images.
   *Against:* it gates the wrong repository. A change to `dryl.css` lands in
   `DRYL.Components`, is merged, is **published to NuGet by that merge**
   (`REL-05`), and only afterwards does the website's CI notice. The net would
   catch the regression after shipping it.

2. **Per-page screenshots, in `DRYL.Components` CI, cloning the site.**
   About 102 pages × 2 modes ≈ 200 images. Gates the repository that can
   actually break the library, before the merge that publishes it.
   *Against:* a full-page shot is a coarse signal — one changed button reports
   as "the button page differs" and the eye has to find it in the diff image.
   Storage is real but bounded; PNGs of a mostly-flat UI compress well.

3. **A curated set: one screenshot per *component*, of its example area only.**
   Roughly 96 × 2 ≈ 190 images, each tight around the thing it guards. Same
   gating as 2.
   *Against:* someone has to decide and maintain what the "example area" is per
   page, and a component with five interesting states gets one of them.

**Recommendation: 2 to start, with 3 as where it should end up.** Reason: the
gating question decides whether this feature does its job at all, and both 2 and
3 answer it the same way — so start with the shape that needs no per-page
curation, learn what actually flaps, and tighten to 3 once there is evidence
about which pages are noisy. Starting at 3 means paying the curation cost for 96
components before knowing whether the approach survives the glass problem at all.

## Decisions

- 2026-08-20: Raised as an idea rather than started as work, per `IDEA-01`. It
  is a new capability, it is not specified anywhere, and it carries a `CODE-03`
  question that `IDEA-05` says belongs in this dialogue and not in a commit.
- 2026-08-20: Chosen from the backlog over three alternatives (phase C specs,
  `DrylAiScope` cascading, the a11y audit) by the Product Owner, on the argument
  that three findings in one session rested on unrepeatable manual measurement.

## Open Points

1. **`CODE-03` sign-off.** `Microsoft.Playwright` in `tests/` — a `Microsoft.*`
   .NET NuGet, outside the rule's documented check, shipped to no consumer, and
   costing a ~150 MB browser download in CI plus a `dotnet test` that no longer
   runs offline out of the box. Approved, approved with conditions, or refused?
   Everything else here depends on this answer.
2. **Which shape** — 1, 2 or 3 above. Equivalently: *should a merge to `main`
   here be blockable by a screenshot diff?* Given that such a merge publishes to
   NuGet, my answer is yes, but it makes the net load-bearing on release day and
   that is the Product Owner's risk to accept.
3. **Where the baselines live and how one is approved.** In git next to the
   code (reviewable in a PR, and the repository grows by every accepted visual
   change forever), or outside it as CI artefacts (repository stays small, and
   approving a change becomes a workflow run rather than a diff a reviewer can
   see). This one has no obviously right answer and I do not have a strong
   recommendation.
