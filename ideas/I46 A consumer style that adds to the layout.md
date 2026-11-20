# A consumer style that adds to the layout

## Meta
- **State:** Adopted
- **Specs:** `specs/E9 Layout/F13 DrylStack.md`, `specs/E9 Layout/F5 DrylGrid.md`,
  `specs/E9 Layout/F3 DrylContainer.md`, `specs/E9 Layout/F2 DrylAspectRatio.md`,
  `specs/E9 Layout/F12 DrylSpacer.md`, `specs/E9 Layout/F11 DrylScrollArea.md`,
  `specs/E9 Layout/F14 DrylTypo.md`, `specs/E9 Layout/F18 DrylDrawer.md`,
  `specs/E9 Layout/F6 DrylLayout.md`, `specs/E5 Data/F11 DrylImage.md`,
  `specs/E5 Data/F22 DrylCalendar/_Component.md`; the convention in
  `harness/conventions.md` (§ parameters, beside the merged `Class`)

## Problem

The layout primitives write their layout as an inline `style` and splat
`AdditionalAttributes` after it. A `style` attribute the consumer passes is
captured into `AdditionalAttributes` and replaces the component's own: on
`DrylStack` it removes `display: flex` and every flex declaration, on
`DrylGrid`, `DrylContainer`, `DrylAspectRatio`, `DrylSpacer` and
`DrylScrollArea` the grid, width, ratio, size or height the parameters asked
for, on `DrylTypo` the colour and alignment. The consumer wrote
`style="margin-top: 8px"` and lost the layout, with no warning.

It is the same failure the `class` attribute had before the merged `Class`
parameter: a splatted attribute clobbers the component's own.

## Solution Idea

Each component in scope gains a typed `Style` parameter (`string?`), the twin
of the merged `Class`. The component writes its own declarations first and
appends the consumer's, so both apply. Because Blazor matches parameters
case-insensitively, a plain `style="…"` attribute binds to `Style` and is
merged as well — it can no longer reach `AdditionalAttributes` and replace the
component's own style.

On a conflict the consumer wins for the property they name:
`<DrylStack Style="display: block">` is a block, and every other declaration of
the stack stays. That is what `style` means everywhere else, and it is the
escape hatch that needs no `!important`.

Considered and not chosen:

- **A — merge the captured attribute silently.** No new API, but invisible in
  IntelliSense and unlike `Class`.
- **C — move the layout out of the inline style** into classes and custom
  properties. Cleanest result, largest change, and a consumer `style` would
  still replace the custom properties unless merged anyway.

## Scope
- **In scope** — every component whose root writes an inline `style` and
  splats `AdditionalAttributes` after it (checked in `code/` on 2026-11-20),
  eleven in all:
  - Layout primitives: `DrylStack`, `DrylGrid`, `DrylContainer`,
    `DrylAspectRatio`, `DrylSpacer`, `DrylScrollArea`, `DrylTypo`.
  - App shell: `DrylDrawer`, `DrylLayout`. They write custom properties only
    (`--sidebar-w`, `--sidebar-collapsed-w`), but a consumer `style` replaces
    those just the same, so `Width` / `SidebarWidth` silently stop working.
  - Data: `DrylCalendar` (`RootStyle`), `DrylImage` (`WrapStyle`).
- **Out of scope:** components whose inline styles sit on inner elements only
  (`DrylBadge`, `DrylTable`, `DrylProgress`, `DrylSkeleton`, `DrylStepper`,
  `DrylToast`) — a consumer `style` lands on their root and clobbers nothing.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency. One
  convention entry beside the merged `Class`.
- **Specs:** the eleven specs lose their "consumer `style` replaces …" debt
  line where they carry one and gain the `Style` parameter and its criteria.
- **Public API:** a `Style` parameter (`string?`) on each component in scope —
  additive, MINOR; 3.17.0 is unpublished and already a MINOR.
- **Code:** one shared internal helper for the merge; the private `Style`
  members of seven components are renamed to make room for the parameter; none
  of the eleven has a public `Style` today. bUnit tests per component: own
  declarations survive, the consumer's come last, `style="…"` binds to `Style`.

## Decisions
- 2026-11-20: Way **B**, a typed `Style` parameter merged with the component's
  own inline style, the twin of `Class` (Product Owner).
- 2026-11-20: On a conflict the consumer's declaration wins for the property
  it names; the component's other declarations stay (Product Owner, on the
  Tech Lead's recommendation).
- 2026-11-20: Scope is all eleven components, not the layout primitives alone;
  the rule is written down once as a pattern in `harness/patterns.md`
  (Product Owner, on the Tech Lead's recommendation). The Tech Lead's first
  draft had left `DrylDrawer` and `DrylLayout` out as "custom properties only";
  the code check showed a consumer `style` replaces those too.

- 2026-11-20: The Product Owner confirmed this version as `Ready`. The rule
  is filed in `harness/conventions.md`, where the merged `Class` lives, rather
  than in `harness/patterns.md` as first written (Tech Lead).

## Open Points
