# I38 — A badge with a pulse and a counter: implementation plan

Idea: [`ideas/I38 A badge with a pulse and a counter.md`](../../ideas/I38%20A%20badge%20with%20a%20pulse%20and%20a%20counter.md)
(direction C, chosen by the Product Owner on 2026-11-02).
Branch: `feat/i38-badge-pulse` in both repositories, merged into `release/3.16.0`.
Version: 3.16.0 (block already open on the release branch, `REL-01`).

## Tasks

| # | Task | Files | Verification | Commit |
|---|---|---|---|---|
| 1 | Idea, spec, code, tests, changelog | `ideas/I38 …`, `specs/E5 Data/F3 DrylBadge.md`, `Components/Data/DrylBadge.razor`, `wwwroot/dryl.css`, `tests/…/DrylBadgeTests.cs`, `CHANGELOG.md` | `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, the five `scripts/*.mjs` checks | one commit (`SPEC-01`, `REL-02`) |
| 2 | Website demo: a live example, texts, catalog blurb | `DRYL.Website/Components/Examples/Badge/Live.razor`, `Components/Pages/DemoBadge.razor`, `Components/ComponentCatalog.cs` | `dotnet build` of the website | one commit in `DRYL.Website` |
| 3 | Browser check, both modes | the website, Playwright | enter, kind change, beat, digit roll, word swap, light and dark, reduced motion | — |
| 4 | Merge into `release/3.16.0` in both repositories, push, delete the feature branches | — | `git log` on both release branches | merge commits |
