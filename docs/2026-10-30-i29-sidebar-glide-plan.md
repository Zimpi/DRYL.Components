# I29 — A sidebar with gliding glass: implementation plan

Idea: `ideas/I29 A sidebar with gliding glass.md` (`Ready`).
Branch: `feat/sidebar-glide` → PR into `release/3.13.0`.
Version: `<Version>` already names unreleased 3.13.0 — the changelog entry joins
that block, the version stays (`REL-01`).

## Task 1 — Specs (`SPEC-01`)

- New `specs/E9 Layout/F18 DrylDrawer.md`, `specs/E10 Navigation/F6 DrylNavGroup.md`,
  `specs/E10 Navigation/F7 DrylNavLink.md`, written `Implemented` together with
  the code (`SPEC-04`).
- `specs/E9 Layout/_Interop.md`: `dryl.motion.navGlide` / `disposeNavGlide`.
- `scripts/spec-coverage-baseline.json`: drop the three now-covered lines.
- Verify: `node scripts/check-spec-coverage.mjs --baseline`, `node scripts/check-harness-links.mjs`.

## Task 2 — Code

- `DrylDrawer.razor`: render `<span class="nav-ink" aria-hidden="true">` as the
  first child of the scrolling region (`.sidebar-content`, or the `aside` without
  slots); attach `dryl.motion.navGlide` after the first interactive render; dispose
  behind an `_inkAttached` guard (prerender).
- `dryl.js` (`dryl.motion`): `navGlide(host)` — places the ink on the most specific
  active `.nav-item` / `.nav-section-link` inside the host; follows `class`
  mutations (navigation, group toggle), tracks frame by frame while a group's
  `grid-template-rows` transition runs or the host resizes; fades out when the
  item sits in a closed group; falls back (no ink) when the item sits in a nested
  scroller. One delegated `pointermove` per host drives the hover light
  (mouse only, rAF-throttled). State lives in `data-nav-ink`, never `class`.
- `dryl.css`: `.nav-ink`, inside accent edge for `.nav-item.active::before` /
  `.nav-section-link.active::before` (the clipped-edge fix), active icon in
  `--accent-ico`, hover light `::after` (transform only), scroll-edge fade on
  `.sidebar-content`, reduced-motion overrides.
- bUnit: `tests/DRYL.Components.Tests/DrylDrawerTests.cs` — ink rendered in both
  layouts, aria-hidden, interop call.
- Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
  `node scripts/check-motion-tokens.mjs`, `node scripts/check-light-sync.mjs`,
  `node scripts/validate-light-contrast.mjs`; browser check of the docs website in
  both modes (glide, group collapse, rail, hover light, reduced motion).

## Task 3 — Close the loop

- `CHANGELOG.md` entry in the 3.13.0 block; idea → `Adopted`.
- One commit for tasks 1–3 (specs and code are one artifact), PR into
  `release/3.13.0`.
