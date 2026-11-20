# A thin frost for scrims and the depth glass

## Meta
- **State:** Adopted
- **Specs:** `specs/E11 Surfaces/F6 DrylDepthGlass.md`,
  `specs/E6 Dialogs/F2 DrylDialogProvider.md`, `specs/E9 Layout/F18 DrylDrawer.md`,
  `specs/E15 Agent Inputs/F5 DrylVoiceOrb.md`; the token in `harness/tokens.md`,
  `DESIGN-07` in `harness/design.md`

## Problem

`DESIGN-07` forbids a hand-written `backdrop-filter: blur(...)`. Three rules in
`dryl.css` still write one, because neither glass tier fits them:

| Rule | Today | Why no tier fits |
|---|---|---|
| `.dialog-backdrop` | `blur(8px)` | A full-viewport scrim. `--glass-fx-float` (24 px, saturated) would smear the page behind a dialog and cost a heavy full-screen blur. |
| `.sidebar-backdrop` | `blur(4px)` | The same, behind the flyout sidebar. |
| `.dg-surface` (`DrylDepthGlass`) | `blur(8px) saturate(1.5)` | An in-flow pane. `--glass-fx-flow` is `none` by default — tried on 2026-11-20 and withdrawn: the ground shows sharp through the pane and the glass reads as a tinted outline. |

All three want the same thing: a light frost you can still see through.

## Solution Idea

- **A — one new token, `--glass-fx-thin`.** `blur(8px)`, defined once on
  `:root` beside the two tiers (not a per-mode value). The two scrims and the
  depth-glass pane take it. Visible change: the sidebar scrim goes from 4 px to
  8 px, the depth glass loses its extra saturation; the dialog scrim is
  unchanged. `DESIGN-07` then has no exception left. A new token needs the
  maintainer's sign-off (`DESIGN-03`).
- **B — no new token; use the tiers.** Scrims and pane take `--glass-fx-float`.
  Everything frosts much more strongly (24 px), and two full-viewport layers
  pay the heavy blur.
- **C — keep them, as named exceptions.** `DESIGN-07` lists the three rules as
  sanctioned; no code changes. The rule keeps exceptions a new component could
  point to.

Chosen: **A**, with the value `blur(8px)`.

## Scope
- **In scope:** `.dialog-backdrop`, `.sidebar-backdrop`, `.dg-surface`; the
  token and its entry in `harness/tokens.md`; the `DESIGN-07` check line.
- **Out of scope:** `DrylVoiceOrb`'s core, which uses `--glass-fx-float` — the
  orb is a floating mark and the tier fits; its debt line is reworded, not
  resolved by a change. `DrylHandoffTrace`'s lane body frost.

## Impact
- **Harness:** **a new token** (`DESIGN-03`) — signed off by the maintainer on
  2026-11-20.
  `harness/tokens.md`, `harness/design.md` (`DESIGN-06`, `DESIGN-07`).
- **Specs:** `DrylDialogProvider`, `DrylDrawer`, `DrylDepthGlass`,
  `DrylVoiceOrb`.
- **Public API:** none; one new CSS custom property a theme may override.
- **Code:** `dryl.css` only. The token is mode-independent, so the
  LIGHT-TOKEN-SET copies are untouched.

## Decisions
- 2026-11-20: Way **A** — one new token for the two scrims and the depth-glass
  pane (Product Owner, on the Tech Lead's recommendation).
- 2026-11-20: **Sign-off for the new token `--glass-fx-thin`** with the value
  `blur(8px)`, without the depth glass's former saturation (`DESIGN-03`,
  Product Owner). The Tech Lead took that answer as the confirmation of the
  final version.

## Open Points
