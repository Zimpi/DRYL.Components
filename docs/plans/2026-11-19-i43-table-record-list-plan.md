# I43 — A table that becomes a record list: plan

Idea: [`ideas/I43 A table that becomes a record list.md`](../../ideas/I43%20A%20table%20that%20becomes%20a%20record%20list.md) (variant C — unfolds in place).
Target: `release/3.17.0`, worked directly on the release branch.

## How the switch works

One DOM, switched by a container query. `.tbl-root` becomes a named container
when `CompactBelow` is set. Each of the four breakpoint classes
(`tbl-compact-sm` … `tbl-compact-xl`) has one small query that sets a fixed
vocabulary of custom properties on the root's children (`--tblc-on`,
`--tblc-block`, `--tblc-grid`, `--tblc-flex`, `--tblc-none`, …). The compact
rules are written once and read that vocabulary with the wide value as the
fallback — `display: var(--tblc-grid, table-row)` — so no rule is duplicated
per breakpoint and the wide presentation is the fallback of every declaration.
A declaration that must not exist while wide names a vocabulary property
without a fallback and is invalid at computed-value time there.

## Task 1 — Library (DRYL.Components)

Files:
- `Components/Data/Models/ColumnCompactRole.cs` — new enum.
- `Components/Data/DrylColumn.cs` — `CompactRole`.
- `Components/Data/DrylTable.razor` — `CompactBelow`, role resolution, the
  `data-compact` / `data-label` attributes, explicit table roles, the unfold
  control and the open row class, the tap handler, the compact sort/filter
  control and its panel.
- `wwwroot/dryl.css` — the vocabulary queries and the compact rules in the
  `.tbl*` block; in-place fallbacks on the rules the compact presentation
  overrides.
- `wwwroot/js/dryl.js` — `dryl.table.isCompact`.
- Specs: `E5 Data/F16 DrylTable/_Component.md`, new `S10 Compact presentation.md`,
  `S5`, `S8`, `S9`; `E5 Data/_Api.md`, `E5 Data/_Interop.md`.
- Tests: `tests/DRYL.Components.Tests/DrylTableCompactTests.cs`.
- `CHANGELOG.md` joins `[3.17.0]`; `<Version>` stays (`REL-01`). Idea → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node --test tests/js/*.test.mjs`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`, `node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`, `node scripts/check-motion-tokens.mjs`;
the docs page at 375px and at desktop width, both color modes, by eye: every
demo table, an unfolded record, a record in edit mode, the sort/filter panel,
and the wide presentation unchanged.

## Task 2 — Website (DRYL.Website)

Files: a `Compact` example under `Components/Examples/Table/` that assigns
column roles, and its entry on `Components/Pages/DemoTable.razor`.
Verify: `dotnet build`, `dotnet test DRYL.Website.slnx`; the example renders at
375px in both modes.
