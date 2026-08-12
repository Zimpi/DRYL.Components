<!-- Thanks for contributing to DRYL! Please complete the checklist below. -->

## Summary

<!-- What does this PR do? Link the issue it closes, e.g. "Closes #12". -->

## Type of change

- [ ] New component
- [ ] New parameter / feature on an existing component
- [ ] Bug fix
- [ ] Docs only
- [ ] Refactor / internal (no public API change)

## Design-system checklist (see CLAUDE.md)

- [ ] Uses **tokens**, not literals (colors, spacing, radii, shadows, durations)
- [ ] Both color modes: no branching on the mode, no mode-assuming value (`DESIGN-02`)
- [ ] Glass surfaces; accents only as gradient / 1px border / glow / indicator
- [ ] Strongly-typed parameters (`enum` for variants)
- [ ] Accessible: keyboard-reachable, ARIA-labeled, focus ring intact
- [ ] Icon-only buttons wrapped in `DrylTooltip`
- [ ] AI-aware components use the shared `AiState` + `.ai-aura*` (no new AI states/colors)

## Docs (mandatory for library changes)

- [ ] `CHANGELOG.md` updated under `[Unreleased]` with the right sub-heading (`REL-02`)
- [ ] `<Version>` bumped in the same commit as the library change (`REL-01`)
- [ ] The component's spec under `specs/` updated in the same commit, and its `State` still honest (`SPEC-01`, `SPEC-04`)
- [ ] Registered in `ComponentCatalog` in `DRYL.Website` (`REL-04`)

## Verification

- [ ] `dotnet build DRYL.slnx -c Release` passes
- [ ] `dotnet test DRYL.slnx -c Release` passes
- [ ] `node scripts/check-light-sync.mjs` passes
- [ ] `node scripts/validate-light-contrast.mjs` passes
- [ ] `node scripts/check-motion-tokens.mjs` passes
- [ ] `node scripts/check-harness-links.mjs` passes
- [ ] Both color modes checked by eye
- [ ] Added / updated tests where it makes sense
