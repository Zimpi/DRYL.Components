# Phase C, last batch: Layout, Foundation, Agents — plan

Target: `release/3.17.0`, worked directly on the release branch.
Meter before: `104/131 components covered`. Meter after: `131/131`.

Same ground rules as [`2026-11-20-core-specs-plan.md`](2026-11-20-core-specs-plan.md):
specs are reverse-engineered from the code as it stands (`SPEC-03` … `SPEC-09`),
format and depth follow `specs/E8 Inputs/F11 DrylToggle.md`, shortfalls go into
`## Recorded debt`, never into a criterion. Spec-only tasks touch no library
code — no `<Version>` bump, no changelog entry (`REL-01`). Each task removes its
components from `scripts/spec-coverage-baseline.json` in the same commit.

## Task 1 — E9 Layout, part A (7)

Files: `specs/E9 Layout/F1 DrylAppBar.md`, `F2 DrylAspectRatio.md`,
`F3 DrylContainer.md`, `F4 DrylDivider.md`, `F5 DrylGrid.md`, `F6 DrylLayout.md`,
`F7 DrylList.md`; `specs/E9 Layout/_Api.md`, `_Interop.md` (owner of both).

## Task 2 — E9 Layout, part B (7)

Files: `specs/E9 Layout/F8 DrylListItem.md`, `F9 DrylMainContent.md`,
`F10 DrylReveal.md`, `F11 DrylScrollArea.md`, `F12 DrylSpacer.md`,
`F13 DrylStack.md`, `F14 DrylTypo.md`. Companion-file additions are handed to
the main agent, which merges them into Task 1's `_Api.md` / `_Interop.md`.

Tasks 1 + 2 commit together: `Specs: the fourteen remaining Layout components`.

## Task 3 — E1 Foundation (2)

Files: `specs/E1 Foundation/F5 DrylThemeProvider.md`,
`F6 DrylColorModeToggle.md`; `_Api.md` / `_Interop.md` where they own something.
Commit: `Specs: ThemeProvider and ColorModeToggle`.

## Task 4 — E12 Agent Runtime (4) + E13 Agent Tools (3)

Files: `specs/E12 Agent Runtime/F2 DrylAgentError.md`, `F3 DrylAgentUsage.md`,
`F4 DrylHandoffTrace.md`, `F5 DrylAgentAttachments.md`;
`specs/E13 Agent Tools/F1 DrylAskChoiceDialog.md`, `F2 DrylAskMultiChoiceDialog.md`,
`F3 DrylAskTextDialog.md`; companion files of both categories.

## Task 5 — E15 Agent Inputs (4)

Files: `specs/E15 Agent Inputs/F2 DrylAiCommandPalette.md`, `F3 DrylAiBuild.md`,
`F4 DrylAiGenerate.md`, `F5 DrylVoiceOrb.md`; companion files.

Tasks 4 + 5 commit together: `Specs: the eleven remaining Agents components`.

Tasks 1–5 touch disjoint files (Task 2 excepted, merged by the main agent) and
run in parallel, one subagent each.

## Task 6 — Eye check, both modes

Demo pages on `DRYL.Website`, dark and light. The `Both color modes` evidence
line of each spec is written only after this; a component not inspected says so.

## Verify (after each commit)

`dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs --baseline scripts/spec-coverage-baseline.json`,
`node scripts/check-motion-tokens.mjs`,
`rg -n ':[0-9]+|line[s]? [0-9]+' specs/` (no line-number citations, `SPEC-09`).
