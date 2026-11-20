# Spec-gap fixes — plan

Source: the `Recorded debt` sections written while closing the spec gap
(2026-11-20). Four of the findings are code that does not hold a contract its
spec already states; they need no idea document (`IDEA-01`) and are fixed here.
The remaining findings (consumer `style` on layout primitives, `DrylThemeProvider`
parameters and CSP, hand-drawn auras of `DrylVoiceOrb` / `DrylHandoffTrace`,
`DrylAiCommandPalette` texts + demo, the two backdrop blurs) go through the idea
dialogue first.

Target: `release/3.17.0`, worked directly on the release branch. `3.17.0` and
`Agents 0.25.1` are both unpublished, so every entry joins its block and the
versions stay put (`REL-01`); the block dates move to 2026-11-20.

One commit per task. Each task carries its spec change (`SPEC-01`) and its
changelog entry.

## Task 1 — `DrylAgentError`, `DrylAgentUsage`: the exit has something to exit with

Files:
- `code/DRYL.Components.Agents/Agents/DrylAgentError.razor`
- `code/DRYL.Components.Agents/Agents/DrylAgentUsage.razor`
- `tests/DRYL.Components.Tests/Agents/DrylAgentErrorTests.cs`, `DrylAgentUsageTests.cs`
- `specs/E12 Agent Runtime/F2 DrylAgentError.md`, `F3 DrylAgentUsage.md`
- `CHANGELOG.md` (`Agents 0.25.1` → Fixed)

Change: each component holds the last non-null value (`DrylRunError` /
usage) in a field and renders from it, so the `DrylPresence` exit pass still has
the alert / the badge row. The held value is dropped in `OnExited`.

Verify: new bUnit tests — after the run's error / usage goes away, the alert /
row is still in the markup while the presence is exiting.
`dotnet test DRYL.slnx -c Release --filter "FullyQualifiedName~DrylAgent"`.

## Task 2 — `DrylAiCommandPalette`: English default words

Files:
- `code/DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor`
- `tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs`
- `specs/E15 Agent Inputs/F2 DrylAiCommandPalette.md`
- `CHANGELOG.md` (`Agents 0.25.1` → Changed)

Change: `Placeholder` and `EmptyText` become `string?` with no default and are
passed through as they are, so the core palette's `CommandPaletteTexts.Default`
supplies the words unless the consumer sets them. No new parameter — the
`Texts` passthrough is part of the idea, not of this fix.

Verify: bUnit test — with no parameter the search field's placeholder is
`Search commands…`; with `Placeholder` set it is the given text.

## Task 3 — `DESIGN-07`: four hand-written blurs move onto the tiers

Files:
- `code/DRYL.Components/wwwroot/dryl.css` — `.sidebar`, `.topbar`, `.toast` →
  `--glass-fx-float`; `.dg-surface` → `--glass-fx-flow`
- `harness/design.md` — `DESIGN-07` Check line names the two remaining hits
  (`.sidebar-backdrop`, `.dialog-backdrop`) instead of claiming none
- specs of `DrylDrawer`, `DrylAppBar`, `DrylToast`, `DrylDepthGlass`
- `CHANGELOG.md` (`3.17.0` → Changed)

Not in this task: `.sidebar-backdrop` (4 px) and `.dialog-backdrop` (8 px) are
scrims with a deliberately lighter blur than `--glass-fx-float`; moving them
needs either a new token (`DESIGN-03`) or a changed look — idea dialogue.

Verify: `rg -n 'backdrop-filter:\s*blur\(' code/` returns exactly the two
backdrop rules; `node scripts/check-light-sync.mjs`; by eye in both modes:
app shell (sidebar + topbar over scrolling content), a toast, the depth-glass
demo.

## Task 4 — `DrylTypo`: `Variant` drives the look

Files:
- `code/DRYL.Components/Components/Layout/DrylTypo.razor.css` — deleted
- `code/DRYL.Components/wwwroot/dryl.css` — `.typo*` rules in the TYPE section,
  before `.gradient-text`
- `tests/DRYL.Components.Tests/` — a test that the element carries the variant
  class with `As` set (markup), existing tests stay green
- `specs/E9 Layout/F14 DrylTypo.md`
- `CHANGELOG.md` (`3.17.0` → Fixed)

Change: the element is built with a `RenderTreeBuilder` and never receives the
scoped-CSS attribute, so the rules move to `dryl.css`, where a class selector
also outranks the `h1`–`h4` element rules. The heading steps share their
declarations with the element rules (`h1, .typo-h1 { … }`), so the scale is
written once. The block sits before `.gradient-text`: at equal specificity the
later rule wins, so `Gradient` keeps its transparent colour.

Verify: on the running docs site, computed `font-size` of
`<DrylTypo Variant="H2" As="h1">` is the `h2` step; `Lead`, `Body`, `Caption`,
`Eyebrow` each show their step; a `Gradient` heading still shows the gradient;
both colour modes by eye.

## Task 5 — Full verification

`dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`,
`node scripts/check-motion-tokens.mjs`.
