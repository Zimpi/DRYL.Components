# A sidebar with gliding glass

## Meta
- **State:** Adopted
- **Specs:** `specs/E9 Layout/F18 DrylDrawer.md`
           `specs/E10 Navigation/F6 DrylNavGroup.md`
           `specs/E10 Navigation/F7 DrylNavLink.md`

## Problem

The sidebar (`DrylDrawer` with `DrylNavGroup` / `DrylNavLink`) is calm but
static. The active item swaps its fill without any transition, while
`DrylTabs` next door glides its indicator — the sidebar is the one piece of
app chrome that does not follow "everything moves" (`DESIGN-11`). Hover is a
flat `--glass-2` wash, identical on every row.

A defect sits on top: `.nav-item.active::before` (the glowing accent edge,
`left: -18px`) is clipped by the scrolling `.sidebar-content`
(`overflow-y: auto` forces `overflow-x` off `visible`), so the edge never
shows on the docs website.

**Target role:** a Blazor developer building an app shell with `DrylDrawer`,
and that app's user moving between pages.

## Solution Idea (options, Tech Lead)

Shown as a clickable prototype (four variants) on 2026-10-30:

- **A — Status quo.**
- **B — Gliding glass:** the existing active look becomes one shared
  indicator that glides on `--ease-spring` to the new active item; the accent
  edge sits inside the pill (no longer clipped); the active icon turns
  `--accent-ico`; the scroll area fades out at top and bottom.
- **C — Light rail:** a hairline per group, the active segment glows; no fills.
- **D — Floating island:** detached glass panel, a hover light that follows the
  cursor, group counts, a dot on the active item.

Recommendation: **B**, with the cursor-following hover light of **D**.

## Scope
- **In scope:** visuals and motion of `DrylNavLink` / `DrylNavGroup` inside a
  `DrylDrawer`: gliding active indicator, active icon tint, scroll-edge fade,
  cursor hover light; the clipped accent edge is fixed along the way.
- **Out of scope:** public API changes, `DrylMenu`, `DrylTabs`, group counts,
  "New" badges, the floating-island surface of D, the docs website's doubled
  brand.

## Impact
- **Harness:** no new token, keyframe, duration or easing — the glide reuses
  the `moveIndicator` pattern (`--dur-slow` / `--ease-spring`), the hover
  light is an opacity transition on `--dur-med` / `--ease-out` with its colour
  from `--accent-soft`. The cursor-following light is nevertheless a new
  motion behaviour → `DESIGN-10` sign-off by the maintainer. Compositor
  rules: the light moves by `transform` on a pseudo-element, rAF-throttled,
  never by repainting a gradient; mouse pointers only; off under
  `prefers-reduced-motion`.
- **Specs:** `DrylDrawer`, `DrylNavGroup` and `DrylNavLink` have no spec yet.
  `SPEC-01` requires one before code: new `specs/E9 Layout/F… DrylDrawer.md`,
  `specs/E10 Navigation/F… DrylNavGroup.md` and `F… DrylNavLink.md`
  (raises the coverage meter; their baseline lines go, see the publish gotcha).
- **Public API:** none.
- **Code:** `DrylDrawer.razor` renders one indicator element inside the
  scrolling region; `dryl.js` gains a drawer variant of the indicator glide
  (vertical, follows `NavLink`'s `active` class through a class-attribute
  `MutationObserver`, re-measures on group collapse via `ResizeObserver`) and
  one delegated `pointermove` listener for the light. Risks: Blazor owns the
  `class` attribute, so JS state lives in `data-*`/inline style; a
  `DrylNavLink` outside any drawer keeps today's static active look; the
  collapsed icon rail must shrink the pill to the icon square.

## Decisions
- 2026-10-30 — Product Owner: the sidebar is "okay hübsch" but could be
  better; asked for examples and a prototype with variants.
- 2026-10-30 — Product Owner chose **B (gliding glass) with the hover light
  of D**. Target: the 3.13.0 visual-refresh release.
- 2026-10-30 — Product Owner signed off the cursor-following hover light
  (`DESIGN-10`): fixed behaviour of every `DrylNavLink` in a `DrylDrawer`,
  no parameter to switch it.
- 2026-10-30 — Product Owner: when the active item's group collapses, the
  indicator fades out and returns when the group opens again.

- 2026-10-30 — Product Owner confirmed the final version ("passt"). Tech Lead:
  the hover light's colour moved from `--accent-soft` to a stronger
  `color-mix` of `--accent-a` after the browser check showed it barely visible;
  same in both modes, no new token.

## Open Points
- None.
