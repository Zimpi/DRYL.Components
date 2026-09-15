# Performance and hardening audit plan

## Scope

Audit the current repository and prepare an evidence-based update proposal. The
maintainer requested analysis and a commit of existing uncommitted work. No runtime
change or release is part of this audit. Follow `harness/ideas.md` before deriving
specifications or an implementation plan for the proposed update.

## Tasks

1. **Preserve existing work.** Review `CLAUDE.md`, `AGENTS.md`, the working diff and
   staged diff. Verify with `git diff --check` and
   `node scripts/check-harness-links.mjs`. Commit only the existing `AGENTS.md`.
   Completed: `d168bd8` (`docs: add Codex repository instructions`). The only content
   differences from `CLAUDE.md` are the assistant name and the stale 127 component
   count; preserve the supplied file and record the discrepancy in the audit.
2. **Establish evidence and propose the update.** Read all four runtime JS files,
   `code/DRYL.Components/wwwroot/dryl.css`, isolated CSS, relevant interop/spec
   contracts, package projects, asset installation instructions and CI workflows.
   Obtain independent CSS review; compare recommendations with current primary
   browser/.NET documentation. Create
   `docs/2026-09-15-performance-hardening-audit.md` and
   `ideas/I13 Performance and hardening update.md` (Draft). Update this plan with
   completed verification. Commit these three documentation files together after
   reviewing their diff and evidence. No library version bump (`REL-03`).

## Verification commands

Run and read each result independently:

```text
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs
node scripts/check-motion-tokens.mjs
node --check code/DRYL.Components/wwwroot/js/dryl.js
node --check code/DRYL.Components/wwwroot/js/dryl-canvas.js
node --check code/DRYL.Components.Agents/wwwroot/js/dryl-aifield.js
node --check code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js
git diff --check
git status --short
```

Record expected incomplete spec coverage separately from passing gates. Visually
inspect a representative local rendering of the actual CSS in light and dark;
state its limits relative to a full Blazor/browser performance benchmark. Audit
helpers and raw build/test logs belong outside the repository. No timing or memory
improvement claim without measurement.

## Progress

- Existing-work commit complete; working tree was clean immediately afterwards.
- Baseline verification complete: build passed with 80 warnings; all 1,116 tests
  passed; light sync, configured contrast, harness links, motion syntax and all
  four JS syntax checks passed. Spec coverage remains 54/129 (exit 1).
- NuGet advisory audit including transitive dependencies reported no vulnerable
  packages. Three deterministic production-JS mock reproductions confirmed voice,
  canvas and table gesture lifecycle defects; no actual microphone/network used.
- Representative actual CSS viewed in light and dark in a local Chromium fixture;
  computed badge colors checked. Full Blazor, four-engine, reduced-motion,
  forced-colors, mobile and performance profiling are explicitly not claimed.
- Audit report and I13 Draft complete. Independent final review found no material
  factual corrections; local document links and the staged diff check passed.
  This documentation task is recorded in its own commit. No runtime/spec/version
  changes made.
