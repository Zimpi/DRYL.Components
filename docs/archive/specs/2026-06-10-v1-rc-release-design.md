# Design — Road to `1.0.0-rc.1`

**Date:** 2026-06-10
**Status:** Approved (brainstorming)
**Owner:** Jan Zimprich

## 1. Goal

Ship **`DRYL.Components 1.0.0-rc.1`**: a public API that is **frozen, internally
consistent, and tested**, published to nuget.org. The RC is a promise — *"this
API is stable; break it for me"* — so the work between today and the RC is about
**stability and confidence**, not new features.

## 2. Scope

### In scope (RC-gating — must be done before cutting `rc.1`)

- **#39 — Public API freeze & consistency audit** across all ~93 components.
- **#40 — Render-mode correctness** for all 39 JS-interop components (Server /
  WASM / Prerender-SSR).
- **#41 — Test coverage** for complex / stateful components.

### Runs during the RC phase (NOT RC-gating — targets the final `1.0.0`)

- **#43 — Accessibility audit**
- **#44 — Globalization / culture-safety audit**
- **#42 — Per-component API reference docs**

These continue after `rc.1` and must be done before the final `1.0.0`, but they
do not block the RC tag. None of them should require *breaking* API changes
(if #43/#44 surface an unavoidable breaking change, it forces a new `rc.N`).

### Out of scope for the entire 1.0 line (post-1.0)

#48 GitHub Pages docs site · #49 visual regression tests · #50 CodeQL ·
#51 performance/trimming pass. Tracked separately on the board.

## 3. Versioning strategy

| Tag | When | Meaning |
| --- | --- | --- |
| `0.1.0-preview.1` | Now (Phase 0) | Claim the nuget.org ID, prove the release pipeline, gather feedback while the API is still free to change |
| `1.0.0-rc.1` | After Phase 3 | API frozen. No more breaking changes without a new `rc.N` |
| `1.0.0` | After RC stabilises + #42/#43/#44 done | Stable release |

SemVer pre-release ordering holds: `0.1.0-preview.1` < `1.0.0-rc.1` < `1.0.0`.
The git tag drives the package version (the release workflow passes
`-p:Version=${tag#v}`); the csproj `<Version>` is only the local default.

## 4. Phased plan (Approach A — sequential, tests interleaved)

### Phase 0 — Publish `0.1.0-preview.1`
- **Human-gated** (only the maintainer can do these): create a nuget.org account;
  add the GitHub repo secret `NUGET_API_KEY`.
- Automated/assisted: set the tag, confirm `CHANGELOG.md` `[Unreleased]` is
  current, push `v0.1.0-preview.1` → the `release.yml` workflow builds, tests,
  packs and publishes. (See board card #46.)
- **Done when:** the package is live on nuget.org, the ID `DRYL.Components` is
  owned, and the pipeline ran green end-to-end.

### Phase 1 — Write `CONVENTIONS.md`
A short, binding reference for public-API naming, so the audit has an objective
target and the rules survive past 1.0. Establishes (at minimum):

- **Events:** `On<Verb>` naming, `EventCallback` / `EventCallback<T>` usage,
  two-way binding via `@bind-Value` (Value + ValueChanged + ValueExpression).
- **Boolean parameters:** plain adjective/state (`Disabled`, `Loading`, `Open`,
  `Selected`) — no `Is`/`Has` prefix — defaulting to `false`.
- **Variants & sizes:** always `enum` (never `string`); enum named
  `<Component><Concept>` (e.g. `ButtonVariant`, `ButtonSize`); first member is
  the sensible default.
- **AI:** the opt-in parameter is always `Ai` (`AiState`), default `AiState.None`.
- **Slots:** `ChildContent` for the default slot; named `RenderFragment` slots
  in PascalCase (`Header`, `Footer`, `Start`, `End`).
- **Pass-through:** `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes`.

**Done when:** `CONVENTIONS.md` is committed and linked from `CLAUDE.md` /
`CONTRIBUTING.md`.

### Phase 2 — #39 API audit + #41 tests, by category
Process **per category batch**: (a) audit public API against `CONVENTIONS.md`
and produce a deviation list; (b) apply fixes — including breaking renames (we
are still pre-1.0); (c) write bUnit tests for that category's complex/stateful
components against the now-fixed API; (d) update `CHANGELOG.md` (`Changed` for
renames) and the README component table; (e) reviewable commit/PR per batch.

Batch order (largest API surface / highest risk first):

1. **Inputs (23)** + **Dialogs** — form integration, `InputBase<T>`,
   `@bind`, validation; highest consistency risk.
2. **Data (21)** — `DrylTable` (sort/selection/virtualization), `DrylBadge`,
   image/list, etc.
3. **Layout (17)** — drawer/appbar/layout, lists, split.
4. **Surfaces (10)** — card, modal, popover, expansion.
5. **Feedback (8)** — alert, toast, tooltip, notifications, skeleton, spinner.
6. **Navigation (5)** — tabs, breadcrumb, etc.
7. **Actions (3)** — button family.
8. **AI (4)** — indicator, scope, stream.

**Done when:** every category audited; deviations either fixed or explicitly
recorded as intentional; complex components in each category have tests; full
suite green; docs updated.

### Phase 3 — #40 render-mode verification pass
Cross-cutting pass over the 39 JS-interop components. For each: no JS before
first interactive render (prerender-safe), `IAsyncDisposable` cleanup with the
`_attached` dispose guard, and verified behaviour under Server, WASM and
prerender/SSR. Add prerender smoke coverage where feasible.

**Done when:** all 39 components proven safe under all three render modes;
no prerender exceptions; dispose guards in place.

### Phase 4 — Cut `1.0.0-rc.1`
Roll `CHANGELOG.md` `[Unreleased]` into a `1.0.0-rc.1` section (maintainer
action per §7), confirm README/table current, tag `v1.0.0-rc.1`, let the
pipeline publish. Announce the API freeze.

**Done when:** `1.0.0-rc.1` is live on nuget.org and the freeze is documented.

## 5. RC exit / definition of done

`1.0.0-rc.1` is cut only when **all** hold:
- `CONVENTIONS.md` exists and the public API conforms (Phase 1–2).
- Every complex/stateful component has meaningful tests; full suite green (Phase 2).
- All 39 JS-interop components verified under Server/WASM/Prerender (Phase 3).
- `dotnet build DRYL.slnx -c Release` and `dotnet test` pass on all TFMs.
- `CHANGELOG.md` and README component table reflect every API change.

## 6. Risks & mitigations

- **Breaking renames ripple into the demo/website app.** Mitigation: the demo is
  in the same solution; each batch updates it and CI catches breakage.
- **Render-mode bugs only show in published WASM / SSR.** Mitigation: dedicated
  Phase 3 with prerender smoke tests, not just unit tests.
- **Scope creep into a11y/globalization forcing breaking changes after freeze.**
  Mitigation: do a *lightweight* a11y/i18n sanity check during Phase 2 batches so
  structural fixes land before the freeze; deep audits (#43/#44) run post-RC and
  must stay non-breaking.
- **Phase 0 blocked on maintainer.** Mitigation: Phases 1–3 do not depend on
  Phase 0 being published; only the *tag* ordering matters. Work can proceed in
  parallel with the maintainer setting up the nuget account.

## 7. Documentation obligations (per CLAUDE.md §7)

Every batch updates `CHANGELOG.md` (`[Unreleased]`, `Changed`/`Added`/`Fixed`)
and the README component table where public API changes. New conventions land in
`CONVENTIONS.md`.
