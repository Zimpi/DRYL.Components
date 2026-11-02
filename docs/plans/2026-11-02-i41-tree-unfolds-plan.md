# I41 — A tree that unfolds: plan

Idea: [`ideas/I41 A tree that unfolds.md`](../../ideas/I41%20A%20tree%20that%20unfolds.md) (variant B — Thread).
Target: `release/3.16.0` (library and website). Feature branch: `feat/i41-tree-rework` in both repos.

## Task 1 — Library (DRYL.Components)

Files:
- `Components/Data/DrylTreeNode.razor(.css)` — the fold instead of `hidden`, the stagger marker, the chevron inside its toggle, guide line, thread, hover light.
- `Components/Data/DrylTreeView.razor(.css)` — the ink, attach and release, the focus fallback, focus moving to a folder that closes.
- `wwwroot/js/dryl.js` — `dryl.motion.treeGlide` / `disposeTreeGlide`.
- Specs: `E5 Data/F20 DrylTreeView.md`, `E5 Data/F21 DrylTreeNode.md`, `E5 Data/_Interop.md`.
- Tests: `tests/DRYL.Components.Tests/DrylTreeViewTests.cs`, `tests/js/tree-glide.test.mjs`.
- `CHANGELOG.md` joins `[3.16.0]`; `<Version>` stays (`REL-01`). Idea → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`, `node --test tests/js/*.test.mjs`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`, `node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`, `node scripts/check-motion-tokens.mjs`; both modes by eye via Playwright on the website.

## Task 2 — Website (DRYL.Website)

Files: `Components/Pages/DemoTreeView.razor` (lead text), `Components/ComponentCatalog.cs` (description).
Verify: `dotnet build` against the sibling library checkout, `dotnet test DRYL.Website.slnx`; the page renders, screenshots in both modes.

## Task 3 — Merge into `release/3.16.0`

Both repos: `git merge --no-ff` the feature branch into `release/3.16.0`, push.

## Task 4 — Clean up

Remove the worktrees, delete the feature branch in both repos, and delete branches that are fully merged and no longer in use.
