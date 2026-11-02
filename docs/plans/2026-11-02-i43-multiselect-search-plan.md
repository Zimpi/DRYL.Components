# I43 — A multi-select that searches: plan

Idea: [`ideas/I43 A multi-select that searches.md`](../../ideas/I43%20A%20multi-select%20that%20searches.md) (variant B — search in the trigger, collapsing rows, matched text highlighted).
Target: `release/3.16.0` (library and website). Feature branch: `claude/bold-newton-iqkm4y` in both repos.

## Task 1 — Idea and spec

Files: `ideas/I43 A multi-select that searches.md` (→ Ready, then Adopted), `specs/E8 Inputs/F6 DrylMultiSelect.md`
(→ Modified: description, Public API, a "Search" block of acceptance criteria, motion, evidence),
`specs/E8 Inputs/_Interop.md` (`dryl.keynav.attach(element, keys)`, the ink following a collapsing row).
Verify: `node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`.

## Task 2 — Shared script

Files: `code/DRYL.Components/wwwroot/js/dryl.js` — `dryl.keynav.attach` takes an optional key list, so a text field
keeps its caret keys; the list ink re-places itself every frame while a row's `grid-template-rows` track transitions.
Tests: `tests/js/keynav.test.mjs` (new), `tests/js/glide.test.mjs` (follow loop).
Verify: `node --test tests/js/*.test.mjs`.

## Task 3 — Component

Files: `Components/Inputs/DrylMultiSelect.razor` / `.razor.css` — `Searchable`, `SearchPlaceholder`, `SearchFunc`,
`EmptyText`; the chip input; filtering over all mounted rows; the collapsing row track; the `<mark>`; Backspace and
Escape in two steps. Tests: `tests/DRYL.Components.Tests/DrylMultiSelectSearchTests.cs` (new).
`CHANGELOG.md` joins `[3.16.0]`; `<Version>` stays (`REL-01`). Spec → Implemented, idea → Adopted.
Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`, `node scripts/check-motion-tokens.mjs`.

## Task 4 — Website (DRYL.Website)

Files: new `Components/Examples/MultiSelect/Search.razor`, `Components/Pages/DemoMultiSelect.razor`,
the `multiselect` summary in `Components/ComponentCatalog.cs`.
Verify: `dotnet build` against the sibling checkout; Playwright on `/components/multiselect` in dark and light: type,
watch rows collapse, pick with Enter, Backspace twice, Escape twice.

Merging into `release/3.16.0` is left to the maintainer.
