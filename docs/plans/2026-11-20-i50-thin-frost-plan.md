# I50 — A thin frost for scrims and the depth glass: plan

Idea: [`ideas/I50 A thin frost for scrims and the depth glass.md`](../../ideas/I50%20A%20thin%20frost%20for%20scrims%20and%20the%20depth%20glass.md) (way A; the token is signed off).
Target: `release/3.17.0`. One commit.

## Task

Files:
- `code/DRYL.Components/wwwroot/dryl.css` — `--glass-fx-thin: blur(8px)` on
  `:root` beside the two tiers (mode-independent, so the LIGHT-TOKEN-SET copies
  are untouched); `.dialog-backdrop`, `.sidebar-backdrop` and `.dg-surface`
  take it.
- `harness/tokens.md` — the token and when it applies; `harness/design.md` —
  `DESIGN-07` names all three tokens and its check line returns no hit.
- Specs of `DrylDepthGlass`, `DrylDialogProvider`, `DrylDrawer`,
  `DrylVoiceOrb` (its frost line becomes a criterion: the float tier fits a
  floating mark).
- `CHANGELOG.md` — `3.17.0` Added and Changed.

Verify: `rg -n 'backdrop-filter:\s*blur\(' code/` returns nothing;
`node scripts/check-light-sync.mjs`; by eye in both modes: the depth-glass demo
over its striped ground, an open dialog, the flyout sidebar at a narrow width;
then the full evidence list of `CLAUDE.md` stage 5.
