# I16 + I17 — Autocomplete creation and a copy button: implementation plan

Source ideas: [`ideas/I16 Creating entries from the autocomplete.md`](../ideas/I16%20Creating%20entries%20from%20the%20autocomplete.md),
[`ideas/I17 A copy button.md`](../ideas/I17%20A%20copy%20button.md).
Version: `DRYL.Components` 3.2.0 → **3.3.0** (new component, new parameters).
3.2.0 is on `origin/main` and therefore shipped (`REL-01`), so the version bumps
and a new changelog block is cut. The Agents package is not touched.

Each task updates its spec in the same commit (`SPEC-01`) and carries its own
verification. The maintainer commits; the plan names one commit per task.

## T1 — `DrylAutocomplete`: spec, creation path, status texts

- `specs/E8 Inputs/F5 DrylAutocomplete.md` — first spec of the component
  (phase C): current contract plus `OnCreate`, `CreateLabel`, `EmptyText`,
  `LoadingText`.
- `specs/E8 Inputs/_Api.md`, `_Interop.md` — the component's binding type,
  its interop entry point, its cleanup.
- `scripts/spec-coverage-baseline.json` — drop the now covered entry.
- `code/DRYL.Components/Components/Inputs/DrylAutocomplete.razor` — create
  option (last, `DrylPresence`, `Plus`), commit on Enter/Tab/outside press,
  exact match before create, provider re-check on stale results, single
  in-flight create, results applied via `InvokeAsync`, `.is-highlighted`.
- `code/DRYL.Components/Components/Inputs/DrylAutocomplete.razor.css` —
  create option hairline, glyph colour and spring, reduced motion.
- `tests/DRYL.Components.Tests/DrylAutocompleteTests.cs`.
- Verify: `dotnet test DRYL.slnx -c Release --filter DrylAutocompleteTests`.

## T2 — `DrylCopyButton`

- `specs/E2 Actions/F4 DrylCopyButton.md`; `_Api.md`, `_Interop.md` of E2.
- `harness/requirements.md` SPEC-02 table (E2 4, total 130), `CLAUDE.md`
  progress line.
- `code/DRYL.Components/Components/Actions/DrylCopyButton.razor` + `.razor.css`
  — composes `DrylButton` + `DrylTooltip`, `dryl.clipboard.copy`, stacked
  crossfade, polite status region, cancellable reset.
- `tests/DRYL.Components.Tests/DrylCopyButtonTests.cs`.
- Verify: `dotnet test DRYL.slnx -c Release --filter DrylCopyButtonTests`.

## T3 — Four icons for the contact book

- `code/DRYL.Components/Components/Data/DrylIcon.razor` — `Phone`, `Car`,
  `Fingerprint`, `Crown` (lucide `phone`, `car`, `fingerprint`, `crown`),
  requested by the maintainer for the same release.
- `specs/E5 Data/F10 DrylIcon.md` — test evidence line.
- `tests/DRYL.Components.Tests/DrylIconTests.cs` — the four names are in the set.
- Verify: `dotnet test DRYL.slnx -c Release --filter DrylIconTests`.

## T4 — Close the loop

- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>3.3.0</Version>`.
- `CHANGELOG.md` — `[3.3.0]` block.
- Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
  `node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
  `node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`
  (with and without the baseline), `node scripts/check-motion-tokens.mjs`.
- Open, outside this repository: `DRYL.Website` demo and `ComponentCatalog`
  entry for `DrylCopyButton`, and a creation example for `DrylAutocomplete`
  (`REL-04`, `CODE-20`); both color modes by eye.
