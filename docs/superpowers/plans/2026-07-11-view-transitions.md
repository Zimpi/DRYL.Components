# DRYL View Transitions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give DRYL same-document View Transition support (FLIP-style morphs between two DOM states) via a JS bridge + `IDrylViewTransition` service, and retrofit `DrylTable` row reorder/sort as the proof of concept.

**Architecture:** One new JS module `window.dryl.viewTransition` (same conventions as `dryl.motion`), one DI service `IDrylViewTransition` doing the JS↔.NET handshake (mutation applied *inside* `document.startViewTransition`'s update callback, resolved only after `OnAfterRender`), a `DrylViewTransitionStyle` enum (`Glide`/`DepthGlass`), one new easing token `--ease-viscous`, morph-endpoint parameters on `DrylCard`, and an opt-in `AnimateReorder` on `DrylTable`.

**Tech Stack:** Blazor (net8/9/10 multi-target), vanilla JS in `wwwroot/js/dryl.js`, CSS view-transition pseudo-elements in `wwwroot/dryl.css`, xUnit + bunit tests.

**Spec:** `docs/superpowers/specs/2026-07-11-view-transitions-design.md`

## Global Constraints

- Branch: `feat/view-transitions` (per spec). `DRYL.Website` is a **separate working directory** (`c:\Users\janzi\Desktop\DRYL\DRYL.Website`) — check `git -C ../DRYL.Website rev-parse` before committing there; commit its changes in its own repo if separate.
- Tokens, not literals (CLAUDE.md 2.1). The **only** new token is `--ease-viscous: cubic-bezier(0.45, 0.05, 0.15, 1)` (maintainer sign-off = the spec). No new durations — two-speed choreography uses existing `--dur-med` (240ms) + `--dur-slow` (420ms).
- `node scripts/check-light-sync.mjs` must stay green (run from repo root `c:\Users\janzi\Desktop\DRYL\DRYL.Components`).
- The reduced-motion `@media` block in Task 2 uses `!important` — this is the one sanctioned exception, copied verbatim from the spec (accessibility kill-switch for UA-generated pseudo-elements).
- `prefers-reduced-motion: reduce` fully disables the morph: CSS block + JS `reduced()` short-circuit that never calls `startViewTransition`.
- No per-component AI states, no new AI visuals (rule 2.10 untouched).
- AI/none of the new parameters change defaults: `AnimateReorder` defaults `false`, `ViewTransitionName` defaults `null`, `ViewTransitionStyle` defaults `Glide` — existing consumers see zero change.
- XML doc comments on every new public type, member and `[Parameter]`.
- Out of scope (do NOT build): cross-document transitions, card→dialog morph choreography, auto-generated names, the `animation-composition: add` squash-stretch accent, any JS fallback name-list for `view-transition-class` (older Chromium 111–124 silently degrades DepthGlass → Glide visuals; documented, acceptable).
- Test command: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "<filter>"` from the repo root.
- Version ownership (CLAUDE.md §7.0): this is a MINOR feature → bump `<Version>` 2.1.0 → **2.2.0** and cut the changelog release in the same final commit (Task 8).

---

### Task 1: Branch setup

**Files:**
- Commit: `docs/superpowers/specs/2026-07-11-view-transitions-design.md` (currently untracked)

- [ ] **Step 1: Create the feature branch from main**

```bash
cd "c:/Users/janzi/Desktop/DRYL/DRYL.Components"
git checkout main && git pull
git checkout -b feat/view-transitions
```

The spec file is untracked, so it follows the branch switch. If `git checkout main` complains about other uncommitted files, stop and ask the user — do not stash blindly.

- [ ] **Step 2: Commit the spec**

```bash
git add docs/superpowers/specs/2026-07-11-view-transitions-design.md
git commit -m "docs: view-transitions design spec"
```

---

### Task 2: `--ease-viscous` token + view-transition CSS + `DESIGN_TOKENS.md`

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css` (token block ~line 137–142; new section appended at end of file)
- Modify: `DESIGN_TOKENS.md` (easing table ~line 306–312)

**Interfaces:**
- Produces: CSS custom property `--ease-viscous`; pseudo-element rules keyed on the transition class `dryl-depth`; keyframes `dryl-depth-clarify`; SVG filter reference `url(#dryl-merge)` (the filter element itself is injected by JS in Task 3).

- [ ] **Step 1: Add the token**

In `DRYL.Components/wwwroot/dryl.css`, the motion tokens currently read (~line 137):

```css
  --ease-out:    cubic-bezier(0.16, 1, 0.3, 1);
  --ease-in-out: cubic-bezier(0.65, 0, 0.35, 1);
  --ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
  --dur-fast:    140ms;
```

Insert one line after `--ease-spring`:

```css
  --ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
  --ease-viscous: cubic-bezier(0.45, 0.05, 0.15, 1); /* view-transition pseudo-elements only */
  --dur-fast:    140ms;
```

- [ ] **Step 2: Append the view-transition section at the end of `dryl.css`**

```css
/* ──────────────────────────────────────────────────────────
   View transitions — the "Depth Glass" morph vocabulary.
   Same-document view transitions started by dryl.viewTransition
   (IDrylViewTransition). Glide tier = viscous shape settle only.
   DepthGlass tier (view-transition-class: dryl-depth) adds the
   translucency pulse + mercury merge; clarity arrives on --dur-med,
   before the --dur-slow shape finishes settling — the glass clears
   before the motion ends.
   ────────────────────────────────────────────────────────── */
::view-transition-group(*) {
  animation-duration: var(--dur-slow);
  animation-timing-function: var(--ease-viscous);
}

/* DepthGlass: mercury-like merge. A merge filter (blur + high-contrast,
   the "goo" technique) pulls the old/new snapshots together with a
   droplet's surface tension instead of a flat cross-fade. Transient —
   the pseudo-tree (and the filter with it) disappears when the
   transition ends, and the clarify pass below lands on filter: none. */
::view-transition-image-pair(*.dryl-depth) {
  filter: url(#dryl-merge);
}

/* DepthGlass: translucency + crystalline clarity. Runs on --dur-med
   (faster than the --dur-slow shape glide above) so the surface is
   always sharp before the shape finishes settling. */
@keyframes dryl-depth-clarify {
  from { filter: blur(6px) saturate(1.35); }
  to   { filter: blur(0) saturate(1); }
}
::view-transition-new(*.dryl-depth) {
  animation: dryl-depth-clarify var(--dur-med) var(--ease-out) both;
}

@media (prefers-reduced-motion: reduce) {
  ::view-transition-group(*),
  ::view-transition-old(*),
  ::view-transition-new(*),
  ::view-transition-image-pair(*) {
    animation: none !important;
    filter: none !important;
  }
}
```

Note the deliberate deviation from the spec's draft CSS: the DepthGlass selectors use `*.dryl-depth` (transition-**class** syntax), not `(dryl-depth)` (which would match a transition *name* — a spec typo; elements are tagged via `view-transition-class: dryl-depth`).

- [ ] **Step 3: Run the light-sync guard**

Run: `node scripts/check-light-sync.mjs`
Expected: green / exit 0 (the easing token is mode-independent and lives outside the LIGHT-TOKEN-SET copies).

- [ ] **Step 4: Document the token in `DESIGN_TOKENS.md`**

Extend the easing table (~line 306):

```markdown
| Token            | Value                          | Use                                |
| ---------------- | ------------------------------ | ---------------------------------- |
| `--ease-out`     | `cubic-bezier(0.16, 1, 0.3, 1)`| Exits, fade-ins, default reveal    |
| `--ease-in-out`  | `cubic-bezier(0.65, 0, 0.35, 1)`| Layout shifts, tab content swap   |
| `--ease-spring`  | `cubic-bezier(0.34, 1.56, 0.64, 1)`| Toggles, indicator pings        |
| `--ease-viscous` | `cubic-bezier(0.45, 0.05, 0.15, 1)`| **View-transition pseudo-elements only** — viscous morph settle |
```

And directly under the table's existing "**Do not use `linear`…**" line, add:

```markdown
**`--ease-viscous` is scoped to view transitions.** It models a viscous, syrup-like
settle — resists starting, then glides with weight, no overshoot. It exists because
none of the other three fit a morph: `--ease-spring` bounces (the opposite of
viscous), `--ease-out` is snappy, `--ease-in-out` is thin. Do not use it for
hover states, indicators or presence animations — those keep the three core curves.
```

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css DESIGN_TOKENS.md
git commit -m "feat(motion): --ease-viscous token + Depth Glass view-transition CSS"
```

---

### Task 3: JS — shared `reduced()` + `window.dryl.viewTransition`

**Files:**
- Modify: `DRYL.Components/wwwroot/js/dryl.js` (top of file ~line 3; inside `dryl.motion` ~line 1029; new module after `dryl.motion`'s closing `})();` ~line 1140)

**Interfaces:**
- Consumes: nothing new.
- Produces: `window.dryl.reduced()` (shared boolean check), `window.dryl.viewTransition.start(dotNetRef)` → Promise; the JS side invokes `dotNetRef.invokeMethodAsync('ApplyChange')` — the .NET method name is fixed as `ApplyChange` (Task 4 must match). Elements carrying the attribute `data-vt-depth` trigger lazy injection of the `#dryl-merge` SVG filter.

- [ ] **Step 1: Hoist the reduced-motion check**

After line 3 (`window.dryl = window.dryl || {};`) insert:

```js
/* Shared prefers-reduced-motion check — used by dryl.motion and
   dryl.viewTransition so both honour the user's setting identically. */
window.dryl.reduced = () =>
    !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
```

Inside `dryl.motion` (~line 1029), replace:

```js
    const reduced = () =>
        !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
```

with:

```js
    const reduced = () => window.dryl.reduced();
```

- [ ] **Step 2: Add the module**

Insert after `dryl.motion`'s closing `})();` (before the `dryl.depthglass` banner comment):

```js
/* ──────────────────────────────────────────────────────────
 * dryl.viewTransition — same-document View Transition bridge.
 *
 * start(dotNetRef) snapshots the current DOM, asks .NET to apply its
 * state change (ApplyChange resolves only after the consuming
 * component's OnAfterRender fired, i.e. the new DOM is committed),
 * then lets the browser morph old → new. Falls back to a direct,
 * morph-free apply when the API is missing or the user prefers
 * reduced motion — the feature never blocks unsupported browsers.
 *
 * The #dryl-merge SVG "goo" filter used by DepthGlass morphs
 * (view-transition-class: dryl-depth) is injected lazily the first
 * time a DepthGlass element ([data-vt-depth]) is in the DOM —
 * the same lazy-DOM-injection pattern as the tooltip portal.
 * ────────────────────────────────────────────────────────── */
window.dryl.viewTransition = (() => {
    function ensureMergeFilter() {
        if (document.getElementById('dryl-merge')) return;
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('width', '0');
        svg.setAttribute('height', '0');
        svg.setAttribute('aria-hidden', 'true');
        svg.style.position = 'absolute';
        svg.innerHTML =
            '<defs><filter id="dryl-merge">' +
            '<feGaussianBlur in="SourceGraphic" stdDeviation="6" result="b"/>' +
            '<feColorMatrix in="b" mode="matrix" values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 24 -12" result="g"/>' +
            '<feComposite in="SourceGraphic" in2="g" operator="atop"/>' +
            '</filter></defs>';
        document.body.appendChild(svg);
    }

    function start(dotNetRef) {
        if (!document.startViewTransition || window.dryl.reduced()) {
            // No support, or user opted out of motion: apply the change
            // directly — no snapshot, no morph (same fallback shape as
            // dryl.motion.onExit).
            return dotNetRef.invokeMethodAsync('ApplyChange');
        }
        if (document.querySelector('[data-vt-depth]')) ensureMergeFilter();
        const t = document.startViewTransition(() => dotNetRef.invokeMethodAsync('ApplyChange'));
        // Swallow skip-rejections (e.g. duplicate view-transition-name):
        // the DOM change itself was applied; only the morph was skipped.
        return t.finished.catch(() => { });
    }

    return { start };
})();
```

- [ ] **Step 3: Verify no existing tests broke**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: PASS (JS changes are invisible to bunit, this is a regression tripwire only).

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/wwwroot/js/dryl.js
git commit -m "feat(motion): dryl.viewTransition JS bridge + shared reduced() guard"
```

---

### Task 4: `DrylViewTransitionStyle` enum + `IDrylViewTransition` service + DI

**Files:**
- Create: `DRYL.Components/DrylViewTransitionStyle.cs`
- Create: `DRYL.Components/Motion/IDrylViewTransition.cs`
- Create: `DRYL.Components/Motion/DrylViewTransition.cs`
- Modify: `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/DrylViewTransitionTests.cs` (new file)

**Interfaces:**
- Consumes: JS `dryl.viewTransition.start` (Task 3); the JS side calls back the `[JSInvokable] ApplyChange` method.
- Produces: `namespace DRYL.Components` → `enum DrylViewTransitionStyle { Glide, DepthGlass }`. `namespace DRYL.Components.Motion` → `public interface IDrylViewTransition { Task RunAsync(Action mutate); Task RunAsync(Func<Task> mutate); void SignalRendered(); }` and `internal sealed class DrylViewTransition(IJSRuntime) : IDrylViewTransition, IDisposable` (internal — test project has `InternalsVisibleTo`; Tasks 5/6 construct it directly with an injected `IJSRuntime`). DI: `AddDrylComponents()` registers `AddScoped<IDrylViewTransition, DrylViewTransition>()`.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/DrylViewTransitionTests.cs`:

```csharp
using Bunit;
using DRYL.Components.Motion;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylViewTransition"/> — the JS↔.NET
/// handshake behind same-document view transitions. The browser-side promise
/// resolution is simulated by calling the JSInvokable ApplyChange directly.
/// </summary>
public class DrylViewTransitionTests : BunitContext
{
    [Fact]
    public async Task ApplyChange_runs_mutate_and_completes_after_SignalRendered()
    {
        var planned = JSInterop.SetupVoid("dryl.viewTransition.start", _ => true);
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        var run = svc.RunAsync(() => { mutated = true; });
        Assert.False(mutated); // the DOM snapshot comes first — mutate waits for the JS callback

        var apply = svc.ApplyChange();
        Assert.True(mutated);            // JS called back → mutate ran
        Assert.False(apply.IsCompleted); // …but the callback resolves only after the render signal

        svc.SignalRendered();
        await apply;

        planned.SetVoidResult(); // browser: t.finished settles
        await run;
    }

    [Fact]
    public async Task RunAsync_applies_mutate_directly_when_js_never_calls_back()
    {
        // Loose interop resolves the start() call without ever invoking
        // ApplyChange — the prerender / disconnected / test-renderer shape.
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        await svc.RunAsync(() => { mutated = true; });

        Assert.True(mutated); // the state change must never be lost
    }

    [Fact]
    public async Task Async_mutate_overload_is_awaited()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var svc = new DrylViewTransition(JSInterop.JSRuntime);
        var mutated = false;

        await svc.RunAsync(async () => { await Task.Yield(); mutated = true; });

        Assert.True(mutated);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylViewTransitionTests"`
Expected: FAIL — compile error, `DrylViewTransition` / `DRYL.Components.Motion` do not exist.

- [ ] **Step 3: Create the enum**

Create `DRYL.Components/DrylViewTransitionStyle.cs` (root folder, same placement as `PresenceTransition.cs`, root namespace so Razor markup needs no extra `@using`):

```csharp
namespace DRYL.Components;

/// <summary>
/// How much of the "Depth Glass" view-transition vocabulary a morph target gets.
/// Both tiers glide on the viscous easing (<c>--ease-viscous</c>); only
/// <see cref="DepthGlass"/> pays for the blur/merge filter pass.
/// </summary>
public enum DrylViewTransitionStyle
{
    /// <summary>Viscous easing only — the shape glides, no blur/merge pass. Cheap
    /// enough for high-frequency interactions (table row reorder, list re-sort).</summary>
    Glide,

    /// <summary>Full "Depth Glass" choreography — translucency pulse + mercury
    /// merge filter + decoupled crystalline clarity. Reserved for low-frequency,
    /// high-meaning merges (shared-element morphs such as card→dialog).</summary>
    DepthGlass
}
```

- [ ] **Step 4: Create the interface**

Create `DRYL.Components/Motion/IDrylViewTransition.cs`:

```csharp
namespace DRYL.Components.Motion;

/// <summary>
/// Runs a Blazor state change inside a same-document
/// <see href="https://developer.mozilla.org/docs/Web/API/View_Transition_API">View Transition</see>,
/// so elements carrying a <c>view-transition-name</c> morph (position, size, opacity)
/// to their new state instead of snapping. Falls back to applying the change
/// directly — no snapshot, no morph — in browsers without the API, during
/// prerender, and when the user prefers reduced motion.
/// </summary>
/// <remarks>
/// <para><b>Contract for consuming components:</b> the mutate delegate must end with
/// <c>StateHasChanged()</c>, and the component must report its render back so the
/// browser knows when to take the "new" snapshot:</para>
/// <code>
/// protected override void OnAfterRender(bool firstRender) => _viewTransition.SignalRendered();
/// </code>
/// <para>One transition runs at a time per service instance; the service is
/// registered scoped via <c>AddDrylComponents()</c>.</para>
/// </remarks>
public interface IDrylViewTransition
{
    /// <summary>Runs <paramref name="mutate"/> (which must call <c>StateHasChanged()</c>)
    /// inside a view transition and completes when the morph has finished.</summary>
    Task RunAsync(Action mutate);

    /// <summary>Async-mutation overload of <see cref="RunAsync(Action)"/>. Keep the work
    /// synchronous where possible — the browser holds rendering while it waits.</summary>
    Task RunAsync(Func<Task> mutate);

    /// <summary>Reports that the consuming component's <c>OnAfterRender</c> fired, i.e.
    /// the mutated state has reached the DOM. Call this unconditionally from
    /// <c>OnAfterRender</c> — it is a cheap no-op when no transition is in flight.</summary>
    void SignalRendered();
}
```

- [ ] **Step 5: Create the implementation**

Create `DRYL.Components/Motion/DrylViewTransition.cs`:

```csharp
using Microsoft.JSInterop;

namespace DRYL.Components.Motion;

/// <inheritdoc cref="IDrylViewTransition"/>
internal sealed class DrylViewTransition : IDrylViewTransition, IDisposable
{
    private readonly IJSRuntime _js;
    private readonly DotNetObjectReference<DrylViewTransition> _selfRef;
    private TaskCompletionSource? _renderTcs;
    private Func<Task>? _pending;

    public DrylViewTransition(IJSRuntime js)
    {
        _js = js;
        _selfRef = DotNetObjectReference.Create(this);
    }

    public Task RunAsync(Action mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        return RunAsync(() => { mutate(); return Task.CompletedTask; });
    }

    public async Task RunAsync(Func<Task> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        _pending = mutate;
        try
        {
            await _js.InvokeVoidAsync("dryl.viewTransition.start", _selfRef);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Circuit gone, stale dryl.js without the module, or prerender
            // (no JS yet) — fall through to the direct apply below.
        }
        // JS guarantees ApplyChange ran before its promise resolves. If it never
        // came (prerender, disconnected circuit, non-browser renderer), the state
        // change must still happen — apply it directly, morph-free.
        if (Interlocked.Exchange(ref _pending, null) is { } missed) await missed();
    }

    /// <summary>Invoked from JS inside <c>document.startViewTransition</c>'s update
    /// callback (or directly on the fallback path). Applies the pending mutation and
    /// resolves once the consuming component reports the render reached the DOM.</summary>
    [JSInvokable]
    public async Task ApplyChange()
    {
        var pending = Interlocked.Exchange(ref _pending, null);
        if (pending is null) return; // already applied — nothing to snapshot
        _renderTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await pending();
        // The caller's StateHasChanged() (inside mutate) has queued a render;
        // resolve only once it actually reaches the DOM (SignalRendered).
        await _renderTcs.Task;
    }

    public void SignalRendered() => _renderTcs?.TrySetResult();

    public void Dispose()
    {
        _renderTcs?.TrySetResult(); // unblock an in-flight ApplyChange
        _selfRef.Dispose();
    }
}
```

- [ ] **Step 6: Register in DI**

In `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`, add `using DRYL.Components.Motion;` to the usings, add `IDrylViewTransition` to the XML summary list, and add inside `AddDrylComponents`:

```csharp
        services.AddScoped<IDrylViewTransition, DrylViewTransition>();
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylViewTransitionTests"`
Expected: 3 PASS

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/DrylViewTransitionStyle.cs DRYL.Components/Motion/ DRYL.Components/Extensions/ServiceCollectionExtensions.cs tests/DRYL.Components.Tests/DrylViewTransitionTests.cs
git commit -m "feat(motion): IDrylViewTransition service + DrylViewTransitionStyle enum"
```

---

### Task 5: `DrylCard` morph-endpoint parameters

**Files:**
- Modify: `DRYL.Components/Components/Surfaces/DrylCard.razor` (root `<div>` ~line 28; parameters after `Class` ~line 86)
- Test: `tests/DRYL.Components.Tests/DrylCardViewTransitionTests.cs` (new file)

**Interfaces:**
- Consumes: `DrylViewTransitionStyle` (Task 4), CSS class `dryl-depth` + `[data-vt-depth]` marker (Tasks 2/3).
- Produces: `[Parameter] string? ViewTransitionName`, `[Parameter] DrylViewTransitionStyle ViewTransitionStyle` on `DrylCard` — the first morph endpoint of the §3 pattern (the full card→dialog choreography stays out of scope; this only marks the element).

**Scope note:** the spec's §3 requires the parameter pair to exist on morph-endpoint components and the changelog DoD lists them; `DrylCard` is the cheapest legitimate host (pure markup opt-in, no behavior). The card→dialog *morph* itself remains the out-of-scope follow-up.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/DrylCardViewTransitionTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylCard"/>'s view-transition morph-endpoint opt-in:
/// ViewTransitionName renders the CSS name; DepthGlass additionally tags the
/// transition class and the [data-vt-depth] marker the JS filter-injection keys on.
/// </summary>
public class DrylCardViewTransitionTests : BunitContext
{
    public DrylCardViewTransitionTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void No_name_renders_no_style_or_marker()
    {
        var cut = Render<DrylCard>(ps => ps.AddChildContent("x"));

        var root = cut.Find(".glass-card");
        Assert.Null(root.GetAttribute("style"));
        Assert.False(root.HasAttribute("data-vt-depth"));
    }

    [Fact]
    public void Name_renders_view_transition_name()
    {
        var cut = Render<DrylCard>(ps => ps
            .Add(p => p.ViewTransitionName, "hero-card")
            .AddChildContent("x"));

        var style = cut.Find(".glass-card").GetAttribute("style");
        Assert.Contains("view-transition-name: hero-card", style);
        Assert.DoesNotContain("dryl-depth", style);
    }

    [Fact]
    public void DepthGlass_adds_transition_class_and_marker()
    {
        var cut = Render<DrylCard>(ps => ps
            .Add(p => p.ViewTransitionName, "hero-card")
            .Add(p => p.ViewTransitionStyle, DrylViewTransitionStyle.DepthGlass)
            .AddChildContent("x"));

        var root = cut.Find(".glass-card");
        Assert.Contains("view-transition-class: dryl-depth", root.GetAttribute("style"));
        Assert.True(root.HasAttribute("data-vt-depth"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylCardViewTransitionTests"`
Expected: FAIL — `ViewTransitionName` parameter does not exist.

- [ ] **Step 3: Implement**

In `DrylCard.razor`, change the root element (line 28–30) to:

```razor
<div @ref="_el"
     class="@CssClass"
     style="@VtStyle"
     data-vt-depth="@VtDepthMarker"
     @attributes="AdditionalAttributes">
```

Add parameters after the `Class` parameter (~line 86):

```csharp
    /// <summary>
    /// Marks this card as a shared-element morph endpoint for view transitions
    /// (<see cref="Motion.IDrylViewTransition"/>): renders <c>view-transition-name</c>
    /// with this value. Must be a valid CSS identifier (letters, digits, <c>-</c>,
    /// <c>_</c>) and unique among elements simultaneously in the DOM — a duplicate
    /// name voids the entire transition. Supply a stable per-instance id, same
    /// discipline as <c>@key</c>. Null (default) opts out.
    /// </summary>
    [Parameter] public string? ViewTransitionName { get; set; }

    /// <summary>
    /// How much "Depth Glass" a morph of this card gets — <see cref="DrylViewTransitionStyle.Glide"/>
    /// (default, viscous shape settle only) or <see cref="DrylViewTransitionStyle.DepthGlass"/>
    /// (adds the translucency pulse + mercury merge; reserve for rare, high-meaning merges).
    /// Ignored unless <see cref="ViewTransitionName"/> is set.
    /// </summary>
    [Parameter] public DrylViewTransitionStyle ViewTransitionStyle { get; set; } = DrylViewTransitionStyle.Glide;
```

Add the two helpers next to `CssClass`:

```csharp
    private bool VtDepth =>
        !string.IsNullOrWhiteSpace(ViewTransitionName)
        && ViewTransitionStyle == DrylViewTransitionStyle.DepthGlass;

    // Marker attribute the JS bridge keys on to lazily inject the #dryl-merge filter.
    private string? VtDepthMarker => VtDepth ? "" : null;

    private string? VtStyle =>
        string.IsNullOrWhiteSpace(ViewTransitionName)
            ? null
            : VtDepth
                ? $"view-transition-name: {ViewTransitionName}; view-transition-class: dryl-depth"
                : $"view-transition-name: {ViewTransitionName}";
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylCardViewTransitionTests"`
Expected: 3 PASS. Also run the full suite once (`dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`) — existing `DrylCard` tests must not regress (the new `style` attribute is null by default, so no markup change for existing consumers).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Surfaces/DrylCard.razor tests/DRYL.Components.Tests/DrylCardViewTransitionTests.cs
git commit -m "feat(surfaces): DrylCard ViewTransitionName/ViewTransitionStyle morph-endpoint opt-in"
```

---

### Task 6: `DrylTable` — `AnimateReorder` + `RowIdSelector`

**Files:**
- Modify: `DRYL.Components/Components/Data/DrylTable.razor`
  - `@using` block (top of file, after `@namespace`)
  - RowFragment `<tr>` (~line 421)
  - Parameters after `OnRowReordered` (~line 714)
  - Private state (~line 824)
  - `OnAfterRenderAsync` (~line 1023)
  - `OnHeaderClick` / `OnHeaderKeyDown` (~line 1253–1275)
  - `OnDrop` / `MoveRow` (~line 1579–1620)
  - `DisposeAsync` (~line 1960)
- Test: `tests/DRYL.Components.Tests/DrylTableTests.cs` (append)

**Interfaces:**
- Consumes: `DrylViewTransition` (internal class, Task 4 — constructed directly with the injected `IJSRuntime`, *not* resolved from DI, so `AnimateReorder` works even in apps that never call `AddDrylComponents()`), `--ease-viscous` group rule (Task 2).
- Produces: `[Parameter] bool AnimateReorder` (default `false`), `[Parameter] Func<TItem, object>? RowIdSelector`. Rows render `style="view-transition-name: tbl-row-<sanitized-id>"` when active. Style: `Glide` — rows never get the DepthGlass pass (no `view-transition-class`, no `data-vt-depth`).

- [ ] **Step 1: Write the failing tests**

Append to `tests/DRYL.Components.Tests/DrylTableTests.cs` (inside the existing class; it already has `RenderTable(extra)` and `FirstColumnValues` helpers and the `Person` record):

```csharp
    // ───── AnimateReorder (view transitions) ─────

    private static IReadOnlyList<string> NameCellValues(IRenderedComponent<DrylTable<Person>> cut) =>
        cut.FindAll("tbody tr")
           .Select(tr => tr.QuerySelectorAll("td").Skip(1).FirstOrDefault()?.TextContent.Trim() ?? "")
           .ToList(); // td[0] is the reorder grip column

    [Fact]
    public void Rows_have_no_view_transition_name_by_default()
    {
        var cut = RenderTable(ps => ps.Add(p => p.Reorderable, true));

        foreach (var tr in cut.FindAll("tbody tr"))
            Assert.DoesNotContain("view-transition-name", tr.GetAttribute("style") ?? "");
    }

    [Fact]
    public void AnimateReorder_adds_a_view_transition_name_per_row()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true)
            .Add(p => p.RowIdSelector, (Person p) => p.Name));

        var styles = cut.FindAll("tbody tr").Select(tr => tr.GetAttribute("style")).ToList();
        Assert.Equal(3, styles.Count);
        Assert.Contains("view-transition-name: tbl-row-Charlie", styles[0]);
        Assert.Contains("view-transition-name: tbl-row-Alice", styles[1]);
        Assert.Contains("view-transition-name: tbl-row-Bob", styles[2]);
    }

    [Fact]
    public void Row_id_is_sanitized_to_a_css_ident()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true)
            .Add(p => p.RowIdSelector, (Person p) => $"{p.Name} v/2"));

        Assert.Contains("view-transition-name: tbl-row-Charlie_v_2",
            cut.FindAll("tbody tr")[0].GetAttribute("style"));
    }

    [Fact]
    public void Drag_reorder_with_AnimateReorder_still_moves_the_row()
    {
        // Loose JSInterop never invokes ApplyChange back — this exercises the
        // service's direct-apply fallback, so the mutation must still land.
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true));

        cut.FindAll(".tbl-grip")[0].TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll("tbody tr")[2].TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, NameCellValues(cut));
    }

    [Fact]
    public void Sort_with_AnimateReorder_still_sorts()
    {
        var cut = RenderTable(ps => ps
            .Add(p => p.Reorderable, true)
            .Add(p => p.AnimateReorder, true));

        cut.FindAll(".tbl-th-clickable")[0].Click(); // Name → ascending

        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, NameCellValues(cut));
    }
```

Add `using Microsoft.AspNetCore.Components.Web;` to the file's usings if not present (for `DragEventArgs`).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylTableTests"`
Expected: FAIL — `AnimateReorder` / `RowIdSelector` parameters do not exist.

- [ ] **Step 3: Implement in `DrylTable.razor`**

**(a)** Top of file, after `@namespace DRYL.Components` add:

```razor
@using DRYL.Components.Motion
```

**(b)** Parameters — insert after the `OnRowReordered` parameter (~line 714):

```csharp
    /// <summary>
    /// Animates row reorder (drag / Alt+Arrow) and click-to-sort with a same-document
    /// view transition: rows glide to their new position on the viscous easing instead
    /// of snapping. Opt-in and additive — off by default. Requires a plain client-side
    /// list (inactive when <see cref="Virtualize"/>, <see cref="GroupBy"/> or
    /// <see cref="DataProvider"/> is set) and falls back to an instant, morph-free
    /// update in browsers without the View Transition API, under
    /// <c>prefers-reduced-motion</c>, and during prerender. Rows need a stable
    /// identity for the morph — see <see cref="RowIdSelector"/>.
    /// </summary>
    [Parameter] public bool AnimateReorder { get; set; }

    /// <summary>
    /// Stable per-row id used to build each row's <c>view-transition-name</c> when
    /// <see cref="AnimateReorder"/> is on. Falls back to <c>GetHashCode()</c> — stable
    /// for records and immutable rows, but pass an explicit selector (e.g. a database
    /// key) when row instances mutate in place. Ids must be unique per row; the value
    /// is sanitized to letters, digits, <c>-</c> and <c>_</c>.
    /// </summary>
    [Parameter] public Func<TItem, object>? RowIdSelector { get; set; }
```

**(c)** Private state — next to `_rootEl` (~line 824) add:

```csharp
    private DrylViewTransition? _rowTransition;
```

**(d)** Computed gate + helpers — place after `CanReorderNow` (~line 889):

```csharp
    // View-transition morphs only make sense over a plain client-side list — the same
    // constraints as row reorder itself, minus the sort lock (sorting morphs too).
    private bool AnimateReorderActive =>
        AnimateReorder && !Virtualize && GroupBy is null && DataProvider is null;

    private string? RowVtStyle(TItem item) =>
        AnimateReorderActive ? $"view-transition-name: tbl-row-{RowVtId(item)}" : null;

    private string RowVtId(TItem item)
    {
        var raw = (RowIdSelector is not null ? RowIdSelector(item)?.ToString() : null)
                  ?? item?.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture)
                  ?? "null";
        return SanitizeVtName(raw);
    }

    // view-transition-name must be a CSS custom-ident: keep [A-Za-z0-9_-], map the
    // rest to '_'. The "tbl-row-" prefix guarantees a valid ident start.
    private static string SanitizeVtName(string raw)
    {
        Span<char> buf = raw.Length <= 64 ? stackalloc char[raw.Length] : new char[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            buf[i] = char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_';
        }
        return new string(buf);
    }

    // Wraps a view mutation in a view transition when active; otherwise applies it
    // directly. The service guarantees the mutation runs even if the JS side never
    // calls back (prerender, disconnected circuit, test renderer).
    private async Task RunRowTransitionAsync(Func<Task> mutate)
    {
        if (!AnimateReorderActive)
        {
            await mutate();
            return;
        }
        _rowTransition ??= new DrylViewTransition(JS);
        await _rowTransition.RunAsync(async () =>
        {
            await mutate();
            StateHasChanged();
        });
    }
```

**(e)** RowFragment `<tr>` (~line 421) — add the style attribute:

```razor
        <tr class="@RowCssClass(item, rowIndex)"
            style="@RowVtStyle(item)"
            @onclick="() => HandleRowClick(item)"
```

(rest of the `<tr>` attributes unchanged.)

**(f)** `OnAfterRenderAsync` (~line 1023) — first line of the method body:

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Tell an in-flight view transition that the mutated state reached the DOM
        // (no-op when none is running).
        _rowTransition?.SignalRendered();

        if (firstRender && !string.IsNullOrEmpty(PersistStateKey))
```

**(g)** `OnDrop` (~line 1579) — clear the drag-highlight state *before* the morph so the drop-target classes don't linger through the 420ms glide:

```csharp
    private async Task OnDrop(int index)
    {
        var from = _dragIndex;
        _dragIndex = null;
        _dragOverIndex = null;
        if (from is int f)
            await MoveRow(f, index);
    }
```

**(h)** `MoveRow` (~line 1609) — wrap the view mutation:

```csharp
    // Optimistically reorders the displayed view so the move is reflected immediately
    // (morphing via a view transition when AnimateReorder is active), then raises
    // OnRowReordered so the consumer can apply the same move to its backing collection.
    private async Task MoveRow(int from, int to)
    {
        if (!CanReorderNow) return;
        if (from < 0 || from >= _view.Count) return;
        to = Math.Clamp(to, 0, _view.Count - 1);
        if (from == to) return;

        await RunRowTransitionAsync(() =>
        {
            var item = _view[from];
            _view.RemoveAt(from);
            _view.Insert(to, item);
            return Task.CompletedTask;
        });
        await OnRowReordered.InvokeAsync(new RowReorderEventArgs(from, to));
    }
```

**(i)** Sort paths — in `OnHeaderClick` (~line 1253) replace the final `await RebuildViewAsync();` with:

```csharp
        await RunRowTransitionAsync(RebuildViewAsync);
```

and identically in `OnHeaderKeyDown`'s sort branch (~line 1274, the `await RebuildViewAsync();` after `MarkPersistDirty()`).

(Note: `AnimateReorderActive` excludes `DataProvider`, so `RebuildViewAsync` inside the update callback is pure synchronous compute — no server round-trip ever runs inside the browser's snapshot window.)

**(j)** `DisposeAsync` (~line 1960) — add before `_dotNetRef?.Dispose();`:

```csharp
        _rowTransition?.Dispose();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "DrylTableTests"`
Expected: all PASS (new 5 + all pre-existing table tests — sort/pagination behavior must be unchanged).

- [ ] **Step 5: Run the full suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Data/DrylTable.razor tests/DRYL.Components.Tests/DrylTableTests.cs
git commit -m "feat(data): DrylTable AnimateReorder — view-transition row morphs for reorder & sort"
```

---

### Task 7: Website demo — animated reorder example

**Files:**
- Create: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\Examples\Table\Reorder.razor`
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\Pages\DemoTable.razor`
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\ComponentCatalog.cs` (line 76, Table entry description)

**Interfaces:**
- Consumes: `DrylTable` `AnimateReorder`/`RowIdSelector` (Task 6). Example files are embedded resources (`Components/Examples/**/*.razor` glob in the csproj — a new file is picked up automatically).

- [ ] **Step 1: Create the example**

Create `Components\Examples\Table\Reorder.razor`:

```razor
<DrylTable TItem="TaskRow"
           Items="@_tasks"
           Reorderable
           AnimateReorder
           RowIdSelector="@(t => t.Id)"
           OnRowReordered="OnReordered"
           AriaLabel="Sprint backlog">
    <Columns>
        <DrylColumn TItem="TaskRow" Field="@(t => t.Title)" Title="Task" Primary Sortable />
        <DrylColumn TItem="TaskRow" Field="@(t => t.Owner)" Title="Owner" />
        <DrylColumn TItem="TaskRow" Field="@(t => t.Points)" Title="Points" Align="ColumnAlign.End" Sortable />
    </Columns>
</DrylTable>

@code {
    private readonly List<TaskRow> _tasks =
    [
        new(1, "Wire OAuth flow",        "Mira",  5),
        new(2, "Design empty states",    "Jonas", 3),
        new(3, "Migrate billing tables", "Ade",   8),
        new(4, "Ship dark-mode audit",   "Lena",  2),
        new(5, "Refactor search index",  "Piotr", 5),
    ];

    private void OnReordered(RowReorderEventArgs e)
    {
        var item = _tasks[e.From];
        _tasks.RemoveAt(e.From);
        _tasks.Insert(e.To, item);
    }

    private sealed record TaskRow(int Id, string Title, string Owner, int Points);
}
```

- [ ] **Step 2: Register the example on the page**

In `Components\Pages\DemoTable.razor`, insert after the "Inline editing" `DemoExample` block (line 31):

```razor
    <DemoExample Title="Animated reorder — view transitions" Source="Table/Reorder"
                 Description="AnimateReorder morphs rows to their new position with a same-document view transition — drag the grip (or Alt+Arrow) and rows glide instead of snapping; click-to-sort glides too. RowIdSelector gives each row a stable morph identity. Falls back to an instant update in browsers without the View Transition API or when reduced motion is set.">
        <DRYL.Website.Components.Examples.Table.Reorder />
    </DemoExample>
```

- [ ] **Step 3: Update the catalog description**

In `Components\ComponentCatalog.cs` line 76, change the Table entry description:

```csharp
        new("Table",            "tables",           "Data", "DrylTable",           "Data", true,  "Declarative columns — search, sort, filter, paging, animated reorder.",  "Grid"),
```

(Keep the column alignment style of neighboring entries.)

- [ ] **Step 4: Build the website**

Run: `dotnet build "c:/Users/janzi/Desktop/DRYL/DRYL.Website/DRYL.Website.csproj"`
Expected: Build succeeded.

- [ ] **Step 5: Commit (in the website repo if separate)**

```bash
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" add Components/Examples/Table/Reorder.razor Components/Pages/DemoTable.razor Components/ComponentCatalog.cs
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" commit -m "docs(table): animated reorder example (AnimateReorder view transitions)"
```

(If `DRYL.Website` turns out to be part of the same repo, commit from the components repo instead.)

---

### Task 8: CHANGELOG + version cut 2.2.0

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `DRYL.Components/DRYL.Components.csproj` (line 8: `<Version>2.1.0</Version>`)

- [ ] **Step 1: Add entries and cut the release**

In `CHANGELOG.md`: add the entries below under `[Unreleased]`, then rename that block to `## [2.2.0] — 2026-07-11` and start a fresh empty `## [Unreleased]` above it. (Any pre-existing `[Unreleased]` entries — e.g. the Agents-package `DrylAiField` note if this branch includes it — stay inside the cut block.)

```markdown
### Added
- `IDrylViewTransition` / `dryl.viewTransition` — Same-document View Transition bridge: `RunAsync(mutate)` snapshots the DOM, applies the state change once the render is committed, and morphs old → new (FLIP-style position/size/opacity). Direct, morph-free fallback for unsupported browsers, prerender and `prefers-reduced-motion`; registered by `AddDrylComponents()`
- `DrylViewTransitionStyle` — `Glide / DepthGlass` morph tiers. New `--ease-viscous` easing token (view-transition pseudo-elements only) plus the `dryl-depth` "Depth Glass" CSS choreography: mercury merge filter + translucency pulse with crystalline clarity landing on `--dur-med`, before the `--dur-slow` shape settle
- `DrylCard` — New `ViewTransitionName` / `ViewTransitionStyle` parameters mark a card as a shared-element morph endpoint

### Changed
- `DrylTable` — New `AnimateReorder` + `RowIdSelector` parameters: row drag-reorder (and click-to-sort) morphs rows to their new position via a view transition instead of snapping. Off by default; requires a client-side list
```

- [ ] **Step 2: Bump the version**

In `DRYL.Components/DRYL.Components.csproj` line 8:

```xml
    <Version>2.2.0</Version>
```

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md DRYL.Components/DRYL.Components.csproj
git commit -m "chore(release): cut 2.2.0 — view transitions (IDrylViewTransition, DrylTable AnimateReorder)"
```

---

### Task 9: Full verification

- [ ] **Step 1: Full test suite + guards**

Run from the repo root:

```bash
dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj
node scripts/check-light-sync.mjs
```

Expected: all tests PASS; sync guard green.

- [ ] **Step 2: Runtime verification (use the `verify` project skill)**

Invoke the `verify` skill and drive the docs website with Playwright:

1. Navigate to `/components/tables`, scroll to "Animated reorder — view transitions".
2. Assert each `tbody tr` in that example carries `style="view-transition-name: tbl-row-…"` (unique per row).
3. Drag-reorder: `browser_drag` from the first row's `.tbl-grip` onto the third row → assert the row order changed and **no console errors** were logged (`browser_console_messages`).
4. Click the "Task" header → rows re-sort, no console errors.
5. Flip `data-dryl-mode` on `<html>` between `dark`/`light` (`browser_evaluate`) and re-check the example renders correctly in both modes (rule 2.2 — the feature adds no colors, this is a tripwire).
6. Emulate `prefers-reduced-motion: reduce` (`browser_evaluate` can't override media queries — instead assert the JS fallback exists: `browser_evaluate` → `typeof window.dryl.reduced === 'function' && typeof window.dryl.viewTransition.start === 'function'`), and confirm reorder still works after `document.startViewTransition` is stubbed out: `browser_evaluate` → `delete Document.prototype.startViewTransition` (page-level), then drag again → order still changes instantly.

- [ ] **Step 3: §7.4 checklist**

- `CHANGELOG.md` — 2.2.0 cut with Added/Changed entries ✓ (Task 8)
- `ComponentCatalog` — Table entry description updated ✓ (Task 7)
- `DRYL.Components.csproj` — `<Version>` 2.2.0 in lockstep with the changelog ✓ (Task 8)

- [ ] **Step 4: Report**

Summarize what shipped and hand off per superpowers:finishing-a-development-branch (merge/PR decision belongs to the user).

---

## Self-review notes (spec ↔ plan)

- **Spec §1 JS module** → Task 3. Deviation: `t.finished.catch(() => {})` added (duplicate-name skip must not fault the .NET await); `[data-vt-depth]`-keyed lazy SVG filter injection realizes the spec's "injected once … the first time a DepthGlass transition runs".
- **Spec §2 service** → Task 4. Deviations, each deliberate: `SignalRendered` is on the **public interface** (the spec's own usage example has consuming components calling it; app components live outside the assembly); a **post-await direct apply** guarantees the mutation is never lost when JS never calls back (prerender/disconnect/test renderer) — this also makes the handshake unit-testable without a browser.
- **Spec §3 parameters** → Task 5 (`DrylCard` as first morph endpoint — pattern + changelog DoD require a host; full card→dialog morph stays out of scope). The `view-transition-class` JS fallback name-list is explicitly **not** built (global constraint; old-Chromium degrades DepthGlass→Glide visuals).
- **Spec §4 CSS** → Task 2, with the `*.dryl-depth` class-selector fix. The `animation-composition` pinch accent: not built (spec's own out-of-scope rule — merge filter carries the mercury read).
- **Spec PoC `DrylTable`** → Task 6: `AnimateReorder` (default false), internal `tbl-row-{id}` names, `GetHashCode` fallback + documented `RowIdSelector`, `Glide` only, drop + sort paths wrapped, drag-state cleanup moved ahead of the morph.
- **Accessibility** → reduced-motion CSS + JS short-circuit (Tasks 2/3), no focus/ARIA changes anywhere, verification step 9.2.6.
- **Docs DoD** → `DESIGN_TOKENS.md` (Task 2), `CHANGELOG` + version (Task 8), XML docs (Tasks 4–6), demo (Task 7), catalog description (Task 7).
- **Type consistency check:** `DrylViewTransition(IJSRuntime)` ctor used identically in Tasks 4 (tests), 6 (`new DrylViewTransition(JS)`); `ApplyChange` name matches JS ↔ `[JSInvokable]`; `RunAsync(Func<Task>)` used by `RunRowTransitionAsync`; enum `DrylViewTransitionStyle.{Glide,DepthGlass}` matches Task 5 markup and tests.
