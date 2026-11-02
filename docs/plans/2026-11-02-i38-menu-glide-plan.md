# I38 — Ink for menus and option lists: plan

Idea: [`ideas/I38 A gliding highlight for menus and option lists.md`](../../ideas/I38%20A%20gliding%20highlight%20for%20menus%20and%20option%20lists.md) (variant A — Ink).
Target: `release/3.16.0` (library and website). Feature branch: `claude/sharp-allen-5s0yx1` in both repos.

## Task 1 — Library (DRYL.Components)

Files:
- `code/DRYL.Components/wwwroot/js/dryl.js` — `dryl.motion.glideScan` / `glideRelease`; `dryl.popover.open` / `close` call them.
- `code/DRYL.Components/wwwroot/dryl.css` — the ink (`[data-glide]::before`), the hover light (`::after`), menu item radius and the static hover gated on `:not([data-glide])`.
- `Components/Inputs/DrylSelect.razor(.css)`, `DrylMultiSelect.razor(.css)`, `DrylAutocomplete.razor(.css)`, `DrylTimePicker.razor(.css)` — `data-dryl-glide` on the list host; static hover gated; rows rounded, panels inset.
- Specs: `E10 Navigation/F1 DrylMenu.md`, `E8 Inputs/F3 DrylSelect.md`, `E8 Inputs/F5 DrylAutocomplete.md`, `E8 Inputs/_Interop.md`, new `E8 Inputs/F6 DrylMultiSelect.md`, `E8 Inputs/F7 DrylTimePicker.md`.
- Tests: `tests/DRYL.Components.Tests/ListGlideMarkupTests.cs`, `tests/js/glide.test.mjs`.
- `CHANGELOG.md` joins `[3.16.0]`; `<Version>` stays (`REL-01`). Idea → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, `node --test tests/js/*.test.mjs`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`, `node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`, `node scripts/check-motion-tokens.mjs`; both modes by eye via Playwright on the website.

## Task 2 — Website (DRYL.Website)

Files: `Components/Pages/DemoMenu.razor` (lead text), new `Components/Examples/Menu/Glide.razor`; catalog description for Menu.
Verify: `dotnet build` against the sibling library checkout; pages render, screenshots in both modes.

## Task 3 — Merge into `release/3.16.0`

Both repos: `git merge --no-ff` the feature branch into `release/3.16.0`, push.

## Task 4 — Clean up

Delete the feature branch locally and on origin in both repos; remove scratch files.
