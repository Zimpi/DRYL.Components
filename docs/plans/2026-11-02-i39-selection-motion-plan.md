# I39 — Selection controls that move: plan

Idea: [`ideas/I39 Selection controls that move.md`](../../ideas/I39%20Selection%20controls%20that%20move.md) (variant A — Tinte).
Target: `release/3.16.0` (library and website). Feature branch: `claude/gracious-sagan-i0vz58` in both repos.

## Task 1 — Library (DRYL.Components)

Files:
- `code/DRYL.Components/wwwroot/dryl.css` — Checkbox, Toggle and Radio blocks: fill layer, drawn check, two-edge knob, press, `dryl-choice-pulse` / `dryl-toggle-pulse` gated on `[data-touched]`, focus ring, disabled look, `.choice` / `.choice-label`, reduced motion.
- `Components/Inputs/DrylCheckbox.razor`, `DrylToggle.razor`, `DrylRadio.razor` — `data-touched` after the first user change; label classes instead of inline styles.
- `Components/Inputs/DrylRadioGroup.razor` — `AiScope.Resolve` (I32 follow-up).
- Specs: new `E8 Inputs/F8 DrylCheckbox.md`, `F9 DrylRadioGroup.md`, `F10 DrylRadio.md`, `F11 DrylToggle.md`; `E8 Inputs/_Api.md`; `scripts/spec-coverage-baseline.json`.
- Tests: `tests/DRYL.Components.Tests/DrylSelectionControlTests.cs`, `AiScopeInheritanceTests.cs`.
- `CHANGELOG.md` joins `[3.16.0]`; `<Version>` stays (`REL-01`). Idea → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, `node --test tests/js/*.test.mjs`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`, `node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`, `node scripts/check-motion-tokens.mjs`; both modes by eye via Playwright on the website.

## Task 2 — Website (DRYL.Website)

Files: new `Components/Examples/Form/Choices.razor`, `Components/Pages/DemoForm.razor`, `Components/Pages/DemoRadio.razor` (lead text), `Components/ComponentCatalog.cs` (Form Controls and Radio Group descriptions).
Verify: `dotnet build` against the sibling library checkout; pages render, screenshots in both modes.

## Task 3 — Merge into `release/3.16.0`

Both repos: `git merge --no-ff` the feature branch into `release/3.16.0`, push.

## Task 4 — Clean up

Delete the feature branch on origin in both repos; remove scratch files.
