# I37 — Tooltip "Faden" with an open delay: plan

Idea: [`ideas/I37 A morphing tooltip with an open delay.md`](../../ideas/I37%20A%20morphing%20tooltip%20with%20an%20open%20delay.md)
Target: `release/3.16.0` (library and website). Feature branch: `claude/festive-fermi-ahnaes` in both repos.

## Task 1 — Library: `Delay`, the thread morph, the glide (DRYL.Components)

Files:
- `code/DRYL.Components/Components/Feedback/DrylTooltip.razor` — `TimeSpan Delay`, rendered as `data-tt-delay` (whole ms, invariant) when > 0.
- `code/DRYL.Components/wwwroot/js/dryl.js` — `window.dryl.tooltip`: open timer, warm-up window, glide, bubble inner parts.
- `code/DRYL.Components/wwwroot/dryl.css` — `.tt-portal` rules for the thread variant; padding/radius tokenised.
- `specs/E7 Feedback/F2 DrylTooltip.md`, `specs/E7 Feedback/_Interop.md` — updated in the same commit (`SPEC-01`).
- `tests/DRYL.Components.Tests/DrylTooltipTests.cs` — new.
- `CHANGELOG.md` — joins the unreleased 3.16.0 block; `<Version>` stays 3.16.0 (`REL-01`).
- `ideas/I37 …md` — Ready → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`,
`node scripts/check-motion-tokens.mjs`; both modes by eye via Playwright on the website.

## Task 2 — Website demo (DRYL.Website)

Files: `Components/Pages/DemoTooltip.razor` (header text), new
`Components/Examples/Tooltip/Delay.razor` (toolbar with `Delay`).

Verify: `dotnet build` of the website against the sibling library checkout; page renders, screenshot in both modes.

## Task 3 — Merge into `release/3.16.0`

Both repos: `git merge --no-ff` feature branch into `release/3.16.0`, push.

## Task 4 — Clean up

Delete the feature branch locally and on origin in both repos, remove scratch files.
