---
name: verify
description: How to build, launch and drive DRYL components end-to-end for runtime verification (docs website + Playwright).
---

# Verifying DRYL.Components changes at runtime

The runtime surface for library changes is the docs website
(`../DRYL.Website`), which references the library via ProjectReference —
running it always exercises the current working-tree code.

## Launch

```bash
cd ../DRYL.Website
dotnet build          # once, so the static web assets manifest is fresh
dotnet run --launch-profile http    # serves http://localhost:5044
```

**Gotcha:** do NOT use `--no-launch-profile` + `ASPNETCORE_URLS`. The
static-assets dev handler then 500s every fingerprinted asset
(`_content/DRYL.Components/dryl.css`, `dryl.js`, scoped styles) with
`FileNotFoundException` under the website's `wwwroot`. The `http`
launch profile (Development env, port 5044) serves them correctly.

## Drive

- Component demo pages live at `/components/<slug>` (e.g. `/components/dialog`);
  each page is built from `Components/Examples/<Component>/*.razor`.
- Use the Playwright MCP tools; `browser_run_code_unsafe` is handy for
  capturing mid-animation DOM state (MutationObserver) and for probing
  races (ESC spam, double-clicks, backdrop clicks).
- Useful invariants after any dialog/overlay flow:
  `document.querySelectorAll('.dialog-backdrop,.dialog-layer').length === 0`
  and `!document.body.classList.contains('dryl-scroll-locked')`.
- Screenshots land in the repo root (Playwright server cwd) — delete them
  afterwards; they don't belong in the project.

## This skill is one input, not the whole bar

Driving the component in a browser is what this file covers. It does **not**
replace the evidence list in [`CLAUDE.md`](../../../CLAUDE.md) stage 5, which a
change must clear before it is reported as done:

```bash
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release          # bUnit, tests/DRYL.Components.Tests
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs
```

Both colour modes are checked by eye — that part is what the browser session
above is for, and `DESIGN-02` has no exception route.
