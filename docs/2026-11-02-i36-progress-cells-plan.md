# I36 — Glass cells for the progress bar: implementation plan

Idea: [`ideas/I36 Glass cells for the progress bar.md`](../ideas/I36%20Glass%20cells%20for%20the%20progress%20bar.md)
(direction B, chosen by the Product Owner on 2026-11-02).
Branch: `feat/i36-progress-redesign` in both repositories, merged into `release/3.16.0`.
Version: 3.16.0 (block already open on the release branch, `REL-01`).

## Tasks

| # | Task | Files | Verification | Commit |
|---|---|---|---|---|
| 1 | Idea, spec, code, tests, changelog | `ideas/I36 …`, `specs/E7 Feedback/F5 DrylProgress.md`, `specs/E7 Feedback/_Api.md`, `Components/Feedback/DrylProgress.razor`, `Components/Feedback/ProgressVariant.cs`, `wwwroot/dryl.css`, `tests/…/DrylProgressTests.cs`, `tests/…/GlobalizationTests.cs`, `CHANGELOG.md` | `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, the five `scripts/*.mjs` checks | one commit (`SPEC-01`, `REL-02`) |
| 2 | Website demo: steps example, texts, catalog blurb | `DRYL.Website/Components/Examples/Progress/*`, `Components/ComponentCatalog.cs` | `dotnet build` of the website | one commit in `DRYL.Website` |
| 3 | Browser check, both modes | the website, Playwright | determinate, steps, indeterminate, variants, sizes, AI mode, light and dark | — |
| 4 | Merge into `release/3.16.0` in both repositories, push, delete the feature branches | — | `git log` on both release branches | merge commits |
