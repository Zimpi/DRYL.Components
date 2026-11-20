# I49 — An AI command palette that speaks your language and can be seen: plan

Idea: [`ideas/I49 An AI command palette that speaks your language and can be seen.md`](../../ideas/I49%20An%20AI%20command%20palette%20that%20speaks%20your%20language%20and%20can%20be%20seen.md) (way B — forward everything).
Target: `release/3.17.0`; Agents becomes `0.26.0`.

## Task 1 — library (this repository), one commit

Files:
- `code/DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor` —
  `Texts`, `Items`, `SearchProvider`, `Debounce`, `GroupOrder`, `Fallback`,
  `OnItemRun`, `Ai`, `Aura`, `AiContent`, `AriaLabel`, each forwarded.
- `tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs` — the parameter
  lists of wrapper and core palette are equal except `Resolver`; the new
  parameters arrive at the inner palette; `Texts` translates it.
- `specs/E15 Agent Inputs/F2 DrylAiCommandPalette.md`.
- `code/DRYL.Components.Agents/DRYL.Components.Agents.csproj` — `0.26.0`;
  `CHANGELOG.md` — the block is renamed and gains `Added`.

Verify: `dotnet test DRYL.slnx -c Release --filter "FullyQualifiedName~DrylAiCommandPalette"`,
then the full evidence list of `CLAUDE.md` stage 5.

## Task 2 — website (`DRYL.Website` repository), its own pull request

- An example `CommandPalette/AiPalette` on `/components/command-palette` that
  uses `DrylAiCommandPalette` with a demo resolver registered in DI.
- A `ComponentCatalog` entry for `DrylAiCommandPalette` (`REL-04`).
- Then the spec's "no demo, no catalog entry" debt line and its evidence lines
  are updated.

Verify: `dotnet test DRYL.Website.slnx`; the page in both colour modes.
