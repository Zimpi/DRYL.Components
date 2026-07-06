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

## Unit tests (CI's job, not verification)

`dotnet test tests/DRYL.Components.Tests` from the repo root (bUnit).
