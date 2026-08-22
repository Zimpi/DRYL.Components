# DrylMorph — implementation plan

Idea: `ideas/I10 Shared-element transitions between overview and detail.md`
(`Ready`, step 1 of two).
Spec: `specs/E9 Layout/F17 DrylMorph.md` (`Modified`).

Step 2 of the idea — the route-level transition host — is **not** in this plan.

Target version: **2.25.0** (MINOR — new component, additive API). 2.24.3 has
shipped, so this is a fresh version block, not an addition to an existing one
(`REL-01`).

---

## T1 — Spec and bookkeeping

Files:
- `ideas/I10 Shared-element transitions between overview and detail.md` (done)
- `specs/E9 Layout/F17 DrylMorph.md` (done)
- `specs/E9 Layout/_Interop.md` (done)
- `harness/requirements.md` — `SPEC-02` table: `E9 Layout` 16 → 17, total
  127 → 128
- `CLAUDE.md` — the `x/127 components covered` line → `x/128`

Verify: `node scripts/check-harness-links.mjs` and
`node scripts/check-spec-coverage.mjs`. The coverage check exits non-zero
(phase C is unfinished); the evidence is that `DrylMorph.razor` is **not** in
its "without a spec" list once T2 lands, and that no structural error is
reported.

Commit: `spec(E9): DrylMorph — the shared-element hull`

## T2 — The component

Files:
- `code/DRYL.Components/Components/Layout/DrylMorph.razor` (new)

Shape (mirrors `DrylTypo`, which solves the same dynamic-tag problem):

- `@inject IDrylViewTransition ViewTransition`
- parameters `Name`, `Style`, `As`, `Active`, `ChildContent`, `Class`,
  `AdditionalAttributes`
- `private RenderFragment Render => builder => { builder.OpenElement(0, As); … }`
  — a `.razor` cannot both carry markup and override `BuildRenderTree`, so the
  element is built in a `RenderFragment` the way `DrylTypo.Render` is
- the inline style is built exactly as `DrylCard.VtStyle` builds it today
- `protected override void OnAfterRender(bool firstRender) =>
  ViewTransition.SignalRendered();` — unconditional, per the spec

No stylesheet, no JS, no `IDisposable`.

Verify: `dotnet build DRYL.slnx -c Release`.

Commit: `feat(layout): DrylMorph — a transition ID for any content`

## T3 — Tests

Files:
- `tests/DRYL.Components.Tests/DrylMorphTests.cs` (new)
- `tests/DRYL.Components.Tests/ClassMergeTests.cs` (add `DrylMorph`)

Cases, one per acceptance criterion group: tag rendering and `As`; the name
rendered / not rendered for null, empty, whitespace and `Active=false`; the
`DepthGlass` pair (`view-transition-class` + `data-vt-depth`) and their absence
on `Glide`; `SignalRendered` called on render, including while unnamed (fake
`IDrylViewTransition`); `Class` merge and attribute splat.

Verify: `dotnet test DRYL.slnx -c Release`.

Commit: `test(layout): DrylMorph`

## T4 — DrylCard delegates

Files:
- `code/DRYL.Components/Components/Surfaces/DrylCard.razor`
- `specs/E11 Surfaces/` — only if a `DrylCard` spec already exists (`SPEC-01`)

`DrylCard.ViewTransitionName` / `ViewTransitionStyle` keep their exact public
behaviour; the inline-style construction moves to the shared helper `DrylMorph`
uses, so the string exists in one place. **Not** a wrap of `DrylCard`'s root in
a `DrylMorph` — that would add a box inside every card and change consumers'
layout.

Verify: `dotnet build DRYL.slnx -c Release` and
`dotnet test DRYL.slnx -c Release` — `DrylCardViewTransitionTests.cs` is the
regression net and must stay green **unchanged**.

Commit: `refactor(surfaces): DrylCard builds its morph style through the shared helper`

## T5 — Release bookkeeping

Files:
- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>` 2.24.3 → 2.25.0
- `CHANGELOG.md` — a `## [2.25.0]` block under `Added`
- `specs/E9 Layout/F17 DrylMorph.md` — `State: Modified` → `Implemented`

Verify: `dotnet build DRYL.slnx -c Release`,
`node scripts/check-motion-tokens.mjs`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`.

Commit: `release: 2.25.0 — DrylMorph`

## T6 — Website (separate repository)

`../DRYL.Website` is its own repository (`CODE-20`: demos live there). Files:
- `DRYL.Website/Components/Pages/DemoMorph.razor`
- `DRYL.Website/Components/Examples/Morph/…`
- `DRYL.Website/Components/ComponentCatalog.cs`

Verify: `dotnet build`, `dotnet test DRYL.Website.slnx`, and the page driven in
the browser in both color modes — the morph is the deliverable and a screenshot
of a static page proves nothing about it.

Commit: in that repository, separately.

---

## Risks carried into implementation

- **Duplicate `view-transition-name`.** Two live instances sharing a name make
  the browser skip the morph silently. Covered by `Active` and by T3.
- **Which service instance is signalled.** `DrylMorph` injects the DI-scoped
  service; `DrylDialogProvider` and `DrylTable` deliberately run their own
  instances and keep signalling those. Recorded in `specs/E9 Layout/_Interop.md`
  so it is not "fixed" later.
- **The extra box.** The hull is a real element — `view-transition-name` does
  not apply to `display: contents`. `As` keeps the DOM valid; T6 must show this
  honestly rather than hide it.
