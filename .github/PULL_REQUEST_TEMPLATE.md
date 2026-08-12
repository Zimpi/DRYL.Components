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
- [ ] Dark-only; no light-theme additions
- [ ] Glass surfaces; accents only as gradient / 1px border / glow / indicator
- [ ] Strongly-typed parameters (`enum` for variants)
- [ ] Accessible: keyboard-reachable, ARIA-labeled, focus ring intact
- [ ] Icon-only buttons wrapped in `DrylTooltip`
- [ ] AI-aware components use the shared `AiState` + `.ai-aura*` (no new AI states/colors)

## Docs (mandatory for library changes)

- [ ] `CHANGELOG.md` updated under `[Unreleased]` with the right sub-heading
- [ ] `README.md` component table updated (if component is new or its public API changed)

## Verification

- [ ] `dotnet build DRYL.slnx -c Release` passes
- [ ] `dotnet test DRYL.slnx -c Release` passes
- [ ] Added / updated tests where it makes sense
