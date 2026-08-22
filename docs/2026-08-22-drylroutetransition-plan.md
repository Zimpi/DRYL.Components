# DrylRouteTransition — implementation plan

Idea: `ideas/I11 A route-level transition host.md` (`Ready`).
Spec: `specs/E1 Foundation/F1 DrylRouteTransition.md` (`Modified`).

Target version: **2.26.0** (MINOR — new component, additive API). 2.25.0 has not
shipped, but it is a released *block* in `CHANGELOG.md`; this is a separate
feature, so it gets its own block and its own bump (`REL-01`).

---

## T1 — The service path

Files:
- `code/DRYL.Components/Motion/IDrylViewTransition.cs` — add
  `void BeginNavigation(TimeSpan timeout)` as a **default interface
  implementation** that does nothing.
- `code/DRYL.Components/Motion/DrylViewTransition.cs` — implement it, and turn
  the render signal into a **latch**.

Shape:

- `BeginNavigation(timeout)`: arm the latch (`_navRenderSeen = false`), then call
  the JS bridge **without awaiting** — the old snapshot is taken here.
- `ApplyChange` (already `[JSInvokable]`): when a navigation is pending, there is
  nothing to mutate. It waits for the latch, bounded by `timeout`, and returns.
- `SignalRendered`: unchanged for the mutate path; additionally sets the latch,
  so a render arriving before `ApplyChange` is not lost.

Verify: `dotnet build DRYL.slnx -c Release`.

Commit: `feat(motion): a navigation entry point on IDrylViewTransition`

## T2 — The component

Files:
- `code/DRYL.Components/Components/Providers/DrylRouteTransition.razor` (new)

- `@implements IDisposable`, `@inject NavigationManager`, `@inject
  IDrylViewTransition`
- registers the handler in `OnAfterRender(firstRender)` — never in
  `OnInitialized`, so prerender registers nothing (`patterns.md`)
- handler: consult `ShouldMorph(context.TargetLocation)`, then
  `ViewTransition.BeginNavigation(Timeout)` and return. No `PreventNavigation`,
  no await.
- renders no markup; disposes the handler registration.

Verify: `dotnet build DRYL.slnx -c Release`.

Commit: `feat(foundation): DrylRouteTransition — morph across a route change`

## T3 — Tests

Files:
- `tests/DRYL.Components.Tests/DrylRouteTransitionTests.cs` (new)

Cases: renders no markup; registers on first render and unregisters on dispose;
`ShouldMorph` null/true/false decides whether `BeginNavigation` is called and
with which URI; `Timeout` is passed through; the latch — a render signalled
*before* `ApplyChange` still completes it; the bail — `ApplyChange` returns
within `Timeout` when nothing signals.

The latch and the bail are tested against `DrylViewTransition` directly (its
`ApplyChange` is `[JSInvokable]` and callable), because they are the two defects
that would freeze a real app.

Verify: `dotnet test DRYL.slnx -c Release`.

Commit: `test(foundation): DrylRouteTransition and the navigation latch`

## T4 — Spec, bookkeeping, release

Files:
- `harness/requirements.md` — `E1 Foundation` 5 → 6, total 128 → 129
- `CLAUDE.md` — the `x/128` line → `x/129`
- `specs/E1 Foundation/_Api.md` — the new interface member (its scaffold gains
  its first real content)
- `specs/E1 Foundation/_Interop.md` — the service and its cleanup duty
- `specs/E1 Foundation/F1 DrylRouteTransition.md` — `State` → `Implemented` once
  the website lands
- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>` → 2.26.0
- `CHANGELOG.md` — a `## [2.26.0]` block

Verify: all of `node scripts/check-harness-links.mjs`,
`check-spec-coverage.mjs`, `check-motion-tokens.mjs`, `check-light-sync.mjs`,
`validate-light-contrast.mjs`, plus build and test.

Commit: `release: 2.26.0 — DrylRouteTransition`

## T5 — Website (separate repository)

Files in `../DRYL.Website`:
- `Components/Pages/DemoRouteTransition.razor` — **two real routes**
  (`/components/route-transition` and `/components/route-transition/{Id:int}`),
  because a route morph cannot be demonstrated on one page
- `Components/ComponentCatalog.cs` — `"Route Transition"` / `route-transition`
- the host mounted in `Components/Layout/MainLayout.razor` (or wherever
  `DrylDialogProvider` sits)

Verify: `dotnet build`, `dotnet test DRYL.Website.slnx`, and driven in the
browser: a real route change morphs, the Back button morphs back and lands on
the right history entry, and a destination without a hull still navigates.

---

## Risks carried into implementation

- **The latch race.** `ApplyChange` and the destination's render can arrive in
  either order. Getting this wrong holds the old frame forever. T3 covers it.
- **A stale signal.** `SignalRendered` is called by every `DrylMorph` on every
  render, so a render between the handler and the actual route change would
  complete the transition too early and flatten the morph. Blazor renders
  nothing in that window in practice; recorded in the spec rather than guarded.
- **Mounting it twice.** Two hosts would start two transitions for one
  navigation. The second `startViewTransition` is skipped by the browser, so the
  failure mode is a missing morph rather than a broken app — but it is worth a
  criterion.
