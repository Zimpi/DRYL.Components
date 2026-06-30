# Responsive Foundation & Mobile Hardening — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make any UI built exclusively from DRYL.Components responsive by default — via a container-query-first foundation, five layout primitives, and a staged mobile hardening of existing components.

**Architecture:** Components react to the width of their own slot (container queries), not the viewport. A small defensive global safety layer in `dryl.css` kills the "nothing shrinks / clips" bug class. New layout primitives (`DrylGrid`, `DrylContainer`, `DrylSpacer`, `DrylAspectRatio`, plus a responsive `DrylStack` extension) give consumers automatic reflow. Existing components are hardened in batches, verified visually on DRYL.Website at 375px.

**Tech Stack:** Blazor (.NET 8/9/10 multi-target), CSS isolation + `dryl.css`, bUnit + xUnit tests, DRYL.Website (demo pages + ComponentCatalog).

## Global Constraints

- **Tokens, not literals** — every color/space/radius/shadow/duration references a CSS var. The *only* literal exception is breakpoint px values, which live solely in `dryl.css` (`var()` is illegal in `@container`/`@media` conditions).
- **Dark only** — no light theme, no `prefers-color-scheme`.
- **Motion vocabulary fixed** — `--dur-fast|med|slow`, `--ease-out|in-out|spring`. No new durations/easings/animations/colors.
- **Naming (CONVENTIONS.md):** components PascalCase `Dryl*`; enums `<Component><Concept>` nested next to the component; CSS classes kebab-case, no prefix; boolean params plain adjective, default false; every consumer-facing component carries a merged `string? Class` + `AdditionalAttributes` (`CaptureUnmatchedValues`). XML doc on class + every `[Parameter]`.
- **`prefers-reduced-motion: reduce`** must leave every component fully usable.
- **Accessibility** — decorative motion is `aria-hidden`; never break focus order/keyboard reachability.
- **Docs per change** — `CHANGELOG.md` `[Unreleased]` entry + `ComponentCatalog` registration for every new component / visible change. No README component table.
- **No new runtime deps** — zero npm/JS libraries.
- Branch: `feat/responsive-foundation`.

---

## File Map

**Foundation / library CSS**
- Modify: `DRYL.Components/wwwroot/dryl.css` — breakpoint scale comment, `.cq`, global safety layer, `.stack-collapse-*` + `.grid-cols-*` container-query utilities.
- Create: `DRYL.Components/Components/Layout/Breakpoint.cs` — shared `Breakpoint` enum.

**New primitives** (each `.razor` + optional `.razor.css`)
- Create: `DRYL.Components/Components/Layout/DrylGrid.razor`
- Create: `DRYL.Components/Components/Layout/DrylContainer.razor`
- Create: `DRYL.Components/Components/Layout/DrylSpacer.razor`
- Create: `DRYL.Components/Components/Layout/DrylAspectRatio.razor`
- Modify: `DRYL.Components/Components/Layout/DrylStack.razor` — add `CollapseBelow`.

**Tests**
- Create: `tests/DRYL.Components.Tests/DrylGridTests.cs`, `DrylContainerTests.cs`, `DrylSpacerTests.cs`, `DrylAspectRatioTests.cs`
- Modify: `tests/DRYL.Components.Tests/` — add `DrylStackTests.cs` (CollapseBelow).

**Docs site** (per new primitive)
- Create: `DRYL.Website/Components/Examples/<Comp>/<Example>.razor`
- Create: `DRYL.Website/Components/Pages/Demo<Comp>.razor`
- Modify: `DRYL.Website/Components/ComponentCatalog.cs`

**Existing-component hardening** (Phase 2) — `dryl.css` and/or per-component `.razor.css`.

**Reference docs**
- Modify: `CHANGELOG.md`, `DESIGN_TOKENS.md`.

---

## PHASE 0 — Foundation

### Task 1: Breakpoint scale, `.cq` utility, global safety layer

**Files:**
- Create: `DRYL.Components/Components/Layout/Breakpoint.cs`
- Modify: `DRYL.Components/wwwroot/dryl.css` (append a new `/* ── Responsive foundation ── */` block)
- Modify: `DESIGN_TOKENS.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Produces: `public enum Breakpoint { Sm, Md, Lg, Xl }` in namespace `DRYL.Components`; mapping Sm=480, Md=768, Lg=1024, Xl=1280 (px values only in CSS).
- Produces: CSS classes `.cq`, `.stack-collapse-{sm,md,lg,xl}`, `.grid-cols-{2,3,4}`, and the global safety rules.

- [ ] **Step 1:** Create `Breakpoint.cs`:

```csharp
namespace DRYL.Components;

/// <summary>
/// The fixed DRYL breakpoint scale. The pixel values live only in <c>dryl.css</c>
/// (CSS query conditions cannot read custom properties); this enum is the typed
/// handle consumers pass to responsive parameters such as
/// <see cref="DrylStack.CollapseBelow"/>.
/// </summary>
public enum Breakpoint
{
    /// <summary>480px — phone landscape / small slots.</summary>
    Sm,
    /// <summary>768px — tablet.</summary>
    Md,
    /// <summary>1024px — desktop.</summary>
    Lg,
    /// <summary>1280px — large.</summary>
    Xl
}
```

- [ ] **Step 2:** Append to `dryl.css`:

```css
/* ─────────────────────────────────────────────────────────
   Responsive foundation
   Breakpoint scale (px values intentionally literal — var() is
   illegal inside @container / @media conditions):
     Sm 480  ·  Md 768  ·  Lg 1024  ·  Xl 1280
   ───────────────────────────────────────────────────────── */

/* Make an element a container-query context so descendants can
   adapt to this element's inline size instead of the viewport. */
.cq { container-type: inline-size; }

/* Global safety layer — defensive + additive. Kills the
   "nothing shrinks / clips" bug class without an opinionated reset. */
img, svg, video, canvas { max-width: 100%; height: auto; }
.stack, .row, .col, .between, .glass-card { min-width: 0; }
.glass-card { overflow-wrap: anywhere; }
pre, .code-block, code { overflow-wrap: anywhere; }
pre { overflow-x: auto; }

/* DrylStack CollapseBelow — horizontal stack flips to vertical
   below the chosen container width. Default row; query overrides. */
.stack-collapse-sm, .stack-collapse-md,
.stack-collapse-lg, .stack-collapse-xl { flex-direction: row; }
@container (max-width: 480px)  { .stack-collapse-sm { flex-direction: column; } }
@container (max-width: 768px)  { .stack-collapse-md { flex-direction: column; } }
@container (max-width: 1024px) { .stack-collapse-lg { flex-direction: column; } }
@container (max-width: 1280px) { .stack-collapse-xl { flex-direction: column; } }

/* DrylGrid fixed-column modes that step down on narrow slots. */
.grid-cols-2, .grid-cols-3, .grid-cols-4 { display: grid; }
.grid-cols-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.grid-cols-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.grid-cols-4 { grid-template-columns: repeat(4, minmax(0, 1fr)); }
@container (max-width: 768px) {
  .grid-cols-3, .grid-cols-4 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@container (max-width: 480px) {
  .grid-cols-2, .grid-cols-3, .grid-cols-4 { grid-template-columns: 1fr; }
}
```

- [ ] **Step 3:** Add a "Breakpoints" section to `DESIGN_TOKENS.md` documenting the Sm/Md/Lg/Xl scale, the `.cq` rule, and the safety layer.

- [ ] **Step 4:** Add to `CHANGELOG.md` `[Unreleased]` → `Added`:
```markdown
- Responsive foundation — `Breakpoint` scale (Sm/Md/Lg/Xl), `.cq` container-query utility, and a global safety layer (media `max-width:100%`, flex `min-width:0`, word-wrap) so DRYL UIs resist horizontal overflow on small screens.
```

- [ ] **Step 5:** Build to confirm the new enum compiles. Run: `dotnet build DRYL.Components/DRYL.Components.csproj`. Expected: Build succeeded.

- [ ] **Step 6:** Commit.
```bash
git add DRYL.Components/Components/Layout/Breakpoint.cs DRYL.Components/wwwroot/dryl.css DESIGN_TOKENS.md CHANGELOG.md
git commit -m "feat(layout): responsive foundation — breakpoint scale, .cq, global safety layer"
```

---

## PHASE 1 — Layout Primitives

### Task 2: `DrylGrid`

**Files:**
- Create: `DRYL.Components/Components/Layout/DrylGrid.razor`
- Test: `tests/DRYL.Components.Tests/DrylGridTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: reuses `DrylStack.StackGap` enum for `Gap` (token-driven gaps already defined there).
- Produces: `<DrylGrid MinItemWidth Columns Gap Class>`; nested enum `DrylGrid.ItemWidth { Xs, Sm, Md, Lg }` mapping Xs=12rem, Sm=16rem, Md=20rem, Lg=28rem.

- [ ] **Step 1: Write failing tests** — `DrylGridTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylGridTests : BunitContext
{
    [Fact]
    public void Autofit_is_default_and_emits_min_clamp()
    {
        var cut = Render<DrylGrid>(ps => ps.AddChildContent("<div>a</div>"));
        var style = cut.Find("div.grid").GetAttribute("style")!;
        Assert.Contains("repeat(auto-fit, minmax(min(20rem, 100%), 1fr))", style); // Md default
    }

    [Theory]
    [InlineData(DrylGrid.ItemWidth.Xs, "12rem")]
    [InlineData(DrylGrid.ItemWidth.Sm, "16rem")]
    [InlineData(DrylGrid.ItemWidth.Lg, "28rem")]
    public void MinItemWidth_maps_to_rem(DrylGrid.ItemWidth w, string expected)
    {
        var cut = Render<DrylGrid>(ps => ps.Add(p => p.MinItemWidth, w).AddChildContent("x"));
        Assert.Contains($"min({expected}, 100%)", cut.Find("div.grid").GetAttribute("style"));
    }

    [Fact]
    public void Columns_uses_responsive_utility_class_not_inline_template()
    {
        var cut = Render<DrylGrid>(ps => ps.Add(p => p.Columns, 3).AddChildContent("x"));
        var cls = cut.Find("div.grid").GetAttribute("class")!;
        Assert.Contains("cq", cls);
        Assert.Contains("grid-cols-3", cls);
        Assert.DoesNotContain("auto-fit", cut.Find("div.grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Gap_maps_to_spacing_token()
    {
        var cut = Render<DrylGrid>(ps => ps
            .Add(p => p.Gap, DrylStack.StackGap.Lg).AddChildContent("x"));
        Assert.Contains("gap: var(--sp-4)", cut.Find("div.grid").GetAttribute("style"));
    }

    [Fact]
    public void Merges_class_and_forwards_attributes()
    {
        var cut = Render<DrylGrid>(ps => ps
            .Add(p => p.Class, "mine").AddUnmatched("data-x", "1").AddChildContent("x"));
        var el = cut.Find("div.grid");
        Assert.Contains("mine", el.GetAttribute("class"));
        Assert.Equal("1", el.GetAttribute("data-x"));
    }
}
```

- [ ] **Step 2: Run, verify fail.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylGridTests`. Expected: FAIL (DrylGrid not found).

- [ ] **Step 3: Implement** `DrylGrid.razor`:

```razor
@namespace DRYL.Components

@*  DrylGrid — responsive column grid.
    Default mode is auto-fit (needs no breakpoints): items wrap once they
    fall below MinItemWidth. Set Columns for a fixed count that steps down
    on narrow slots. See CONVENTIONS.md / CLAUDE.md.  *@

<div class="@CssClass" style="@Style" @attributes="AdditionalAttributes">
    @ChildContent
</div>

@code {
    /// <summary>Minimum width an item may occupy before the grid wraps to fewer
    /// columns. Used only in auto-fit mode (when <see cref="Columns"/> is null).
    /// Defaults to <see cref="ItemWidth.Md"/> (20rem).</summary>
    [Parameter] public ItemWidth MinItemWidth { get; set; } = ItemWidth.Md;

    /// <summary>Fixed column count. When set (2–4 get automatic responsive
    /// step-down on narrow slots), overrides auto-fit. Leave null for auto-fit.</summary>
    [Parameter] public int? Columns { get; set; }

    /// <summary>Gap between cells, mapped to the <c>--sp-*</c> scale. Defaults to
    /// <see cref="DrylStack.StackGap.Md"/>.</summary>
    [Parameter] public DrylStack.StackGap Gap { get; set; } = DrylStack.StackGap.Md;

    /// <summary>Grid content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Pass-through HTML attributes on the grid container.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Extra CSS class(es) merged onto the grid's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    private bool UsesFixedColumns => Columns is >= 2 and <= 4;

    private string CssClass
    {
        get
        {
            var classes = new List<string> { "grid" };
            if (UsesFixedColumns) { classes.Add("cq"); classes.Add($"grid-cols-{Columns}"); }
            if (!string.IsNullOrWhiteSpace(Class)) classes.Add(Class!);
            return string.Join(' ', classes);
        }
    }

    private string Style
    {
        get
        {
            var gap = GapToken(Gap);
            if (UsesFixedColumns)
                return $"gap: {gap};";
            // auto-fit (default) or an out-of-range fixed count → inline template
            var min = MinItemWidth switch
            {
                ItemWidth.Xs => "12rem",
                ItemWidth.Sm => "16rem",
                ItemWidth.Lg => "28rem",
                _ => "20rem"
            };
            var template = Columns is int n
                ? $"repeat({n}, minmax(0, 1fr))"
                : $"repeat(auto-fit, minmax(min({min}, 100%), 1fr))";
            return $"display: grid; grid-template-columns: {template}; gap: {gap};";
        }
    }

    internal static string GapToken(DrylStack.StackGap gap) => gap switch
    {
        DrylStack.StackGap.None => "0",
        DrylStack.StackGap.Xs   => "var(--sp-1)",
        DrylStack.StackGap.Sm   => "var(--sp-2)",
        DrylStack.StackGap.Md   => "var(--sp-3)",
        DrylStack.StackGap.Lg   => "var(--sp-4)",
        DrylStack.StackGap.Xl   => "var(--sp-5)",
        DrylStack.StackGap.Xxl  => "var(--sp-6)",
        _ => "var(--sp-3)"
    };

    /// <summary>Minimum item width presets for <see cref="DrylGrid"/> auto-fit mode.</summary>
    public enum ItemWidth { Xs, Sm, Md, Lg }
}
```

- [ ] **Step 4: Run, verify pass.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylGridTests`. Expected: PASS.

- [ ] **Step 5:** `CHANGELOG.md` `[Unreleased]` → `Added`:
```markdown
- `DrylGrid` — Responsive column grid; auto-fit by default (`MinItemWidth`) or fixed `Columns` with automatic step-down; token-driven `Gap`.
```

- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/Components/Layout/DrylGrid.razor tests/DRYL.Components.Tests/DrylGridTests.cs CHANGELOG.md
git commit -m "feat(layout): add DrylGrid responsive grid primitive"
```

---

### Task 3: `DrylContainer`

**Files:**
- Create: `DRYL.Components/Components/Layout/DrylContainer.razor`
- Test: `tests/DRYL.Components.Tests/DrylContainerTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Produces: `<DrylContainer Size Class>`; nested `DrylContainer.ContainerSize { Sm, Md, Lg, Xl, Full }` → max-width Sm=40rem, Md=52rem, Lg=64rem, Xl=80rem, Full=none. Responsive side padding `clamp(var(--sp-4), 4vw, var(--sp-6))`.

- [ ] **Step 1: Write failing tests** — `DrylContainerTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylContainerTests : BunitContext
{
    [Fact]
    public void Default_size_is_lg_and_centers()
    {
        var cut = Render<DrylContainer>(ps => ps.AddChildContent("x"));
        var style = cut.Find("div.container").GetAttribute("style")!;
        Assert.Contains("max-width: 64rem", style);
        Assert.Contains("margin-inline: auto", style);
        Assert.Contains("clamp(var(--sp-4), 4vw, var(--sp-6))", style);
    }

    [Theory]
    [InlineData(DrylContainer.ContainerSize.Sm, "40rem")]
    [InlineData(DrylContainer.ContainerSize.Md, "52rem")]
    [InlineData(DrylContainer.ContainerSize.Xl, "80rem")]
    public void Size_maps_to_max_width(DrylContainer.ContainerSize s, string expected)
    {
        var cut = Render<DrylContainer>(ps => ps.Add(p => p.Size, s).AddChildContent("x"));
        Assert.Contains($"max-width: {expected}", cut.Find("div.container").GetAttribute("style"));
    }

    [Fact]
    public void Full_emits_no_max_width()
    {
        var cut = Render<DrylContainer>(ps => ps
            .Add(p => p.Size, DrylContainer.ContainerSize.Full).AddChildContent("x"));
        Assert.DoesNotContain("max-width", cut.Find("div.container").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Merges_class_and_forwards_attributes()
    {
        var cut = Render<DrylContainer>(ps => ps
            .Add(p => p.Class, "mine").AddUnmatched("data-x", "1").AddChildContent("x"));
        var el = cut.Find("div.container");
        Assert.Contains("mine", el.GetAttribute("class"));
        Assert.Equal("1", el.GetAttribute("data-x"));
    }
}
```

- [ ] **Step 2: Run, verify fail.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylContainerTests`. Expected: FAIL.

- [ ] **Step 3: Implement** `DrylContainer.razor`:

```razor
@namespace DRYL.Components

@*  DrylContainer — centers content at a readable max width with responsive
    side padding so a page is never edge-to-edge on a phone.  *@

<div class="container @Class" style="@Style" @attributes="AdditionalAttributes">
    @ChildContent
</div>

@code {
    /// <summary>Maximum content width preset. Defaults to <see cref="ContainerSize.Lg"/>.</summary>
    [Parameter] public ContainerSize Size { get; set; } = ContainerSize.Lg;

    /// <summary>Container content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Pass-through HTML attributes on the container.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Extra CSS class(es) merged onto the container's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    private string Style
    {
        get
        {
            var pad = "padding-inline: clamp(var(--sp-4), 4vw, var(--sp-6));";
            if (Size == ContainerSize.Full)
                return $"width: 100%; {pad}";
            var max = Size switch
            {
                ContainerSize.Sm => "40rem",
                ContainerSize.Md => "52rem",
                ContainerSize.Xl => "80rem",
                _ => "64rem"
            };
            return $"max-width: {max}; margin-inline: auto; {pad}";
        }
    }

    /// <summary>Max-width presets for <see cref="DrylContainer"/>.</summary>
    public enum ContainerSize { Sm, Md, Lg, Xl, Full }
}
```

- [ ] **Step 4: Run, verify pass.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylContainerTests`. Expected: PASS.

- [ ] **Step 5:** `CHANGELOG.md` `Added`:
```markdown
- `DrylContainer` — Centers content at a readable max width (`Size`) with responsive side padding so pages are never edge-to-edge on mobile.
```

- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/Components/Layout/DrylContainer.razor tests/DRYL.Components.Tests/DrylContainerTests.cs CHANGELOG.md
git commit -m "feat(layout): add DrylContainer max-width primitive"
```

---

### Task 4: `DrylStack` `CollapseBelow`

**Files:**
- Modify: `DRYL.Components/Components/Layout/DrylStack.razor`
- Test: `tests/DRYL.Components.Tests/DrylStackTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: `Breakpoint` (Task 1), the `.stack-collapse-*` / `.cq` CSS (Task 1).
- Produces: `DrylStack.CollapseBelow` (`Breakpoint?`). When set with `Direction=Horizontal`, the stack wraps in a `.cq` container and the inner flex flips to column below that width. Existing single-div output is byte-identical when `CollapseBelow` is null (no breaking change).

- [ ] **Step 1: Write failing tests** — `DrylStackTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylStackTests : BunitContext
{
    [Fact]
    public void Without_collapse_emits_single_div_with_inline_direction()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Horizontal)
            .AddChildContent("x"));
        Assert.Single(cut.FindAll("div.stack"));
        Assert.Contains("flex-direction: row", cut.Find("div.stack").GetAttribute("style"));
        Assert.DoesNotContain("stack-collapse", cut.Markup);
    }

    [Fact]
    public void CollapseBelow_wraps_in_cq_container_and_omits_inline_direction()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Horizontal)
            .Add(p => p.CollapseBelow, Breakpoint.Md)
            .AddChildContent("x"));
        Assert.NotNull(cut.Find("div.cq"));
        var inner = cut.Find("div.stack");
        Assert.Contains("stack-collapse-md", inner.GetAttribute("class"));
        Assert.DoesNotContain("flex-direction", inner.GetAttribute("style") ?? "");
    }

    [Fact]
    public void CollapseBelow_ignored_when_vertical()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Vertical)
            .Add(p => p.CollapseBelow, Breakpoint.Md)
            .AddChildContent("x"));
        Assert.Empty(cut.FindAll("div.cq"));
        Assert.Contains("flex-direction: column", cut.Find("div.stack").GetAttribute("style"));
    }
}
```

- [ ] **Step 2: Run, verify fail.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylStackTests`. Expected: FAIL.

- [ ] **Step 3: Implement.** Edit `DrylStack.razor`. Replace the markup block (lines 24-26) with:

```razor
@if (Collapses)
{
    <div class="cq">
        <div class="stack stack-collapse-@BreakpointSlug @Class" style="@Style" @attributes="AdditionalAttributes">
            @ChildContent
        </div>
    </div>
}
else
{
    <div class="stack @Class" style="@Style" @attributes="AdditionalAttributes">
        @ChildContent
    </div>
}
```

Add the parameter (next to the other `[Parameter]`s):

```csharp
    /// <summary>When set on a <see cref="StackDirection.Horizontal"/> stack, the
    /// stack flips to vertical below this container width (container-query driven).
    /// No effect on a vertical stack. Defaults to null (never collapses).</summary>
    [Parameter] public Breakpoint? CollapseBelow { get; set; }
```

Add helpers to `@code` (and make `Style` omit `flex-direction` when collapsing):

```csharp
    private bool Collapses => CollapseBelow is not null && Direction == StackDirection.Horizontal;

    private string BreakpointSlug => CollapseBelow switch
    {
        Breakpoint.Sm => "sm",
        Breakpoint.Lg => "lg",
        Breakpoint.Xl => "xl",
        _ => "md"
    };
```

In the `Style` getter, change the return so `flex-direction` is dropped when `Collapses` (the CSS class owns direction then):

```csharp
            var directionDecl = Collapses ? "" : $"flex-direction: {direction}; ";
            return $"display: flex; {directionDecl}gap: {gap}; " +
                   $"align-items: {align}; justify-content: {justify}; flex-wrap: {wrap};";
```

- [ ] **Step 4: Run, verify pass.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylStackTests`. Expected: PASS. Then run full file to confirm no regression: `dotnet test tests/DRYL.Components.Tests`.

- [ ] **Step 5:** `CHANGELOG.md` → `Added`:
```markdown
- `DrylStack` — New `CollapseBelow` (`Breakpoint?`) flips a horizontal stack to vertical below the chosen container width; off by default, no change to existing usage.
```

- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/Components/Layout/DrylStack.razor tests/DRYL.Components.Tests/DrylStackTests.cs CHANGELOG.md
git commit -m "feat(layout): add DrylStack CollapseBelow responsive direction"
```

---

### Task 5: `DrylSpacer`

**Files:**
- Create: `DRYL.Components/Components/Layout/DrylSpacer.razor`
- Test: `tests/DRYL.Components.Tests/DrylSpacerTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: `DrylStack.StackGap` (reused) + `DrylGrid.GapToken`.
- Produces: `<DrylSpacer Size?>`; no `Size` → `flex: 1 1 auto` (push); with `Size` → fixed block of that `--sp` value. `aria-hidden`.

- [ ] **Step 1: Write failing tests** — `DrylSpacerTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylSpacerTests : BunitContext
{
    [Fact]
    public void Flexible_by_default()
    {
        var cut = Render<DrylSpacer>();
        var el = cut.Find("div.spacer");
        Assert.Contains("flex: 1 1 auto", el.GetAttribute("style"));
        Assert.Equal("true", el.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Fixed_size_maps_to_spacing_token()
    {
        var cut = Render<DrylSpacer>(ps => ps.Add(p => p.Size, DrylStack.StackGap.Xl));
        var style = cut.Find("div.spacer").GetAttribute("style")!;
        Assert.Contains("var(--sp-5)", style);   // Xl
        Assert.DoesNotContain("flex: 1", style);
    }
}
```

- [ ] **Step 2: Run, verify fail.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylSpacerTests`. Expected: FAIL.

- [ ] **Step 3: Implement** `DrylSpacer.razor`:

```razor
@namespace DRYL.Components

@*  DrylSpacer — layout spacer. With no Size it grows to fill (flex:1),
    pushing siblings apart (e.g. in a toolbar). With Size it is a fixed
    block from the --sp scale. Decorative, so aria-hidden.  *@

<div class="spacer @Class" style="@Style" aria-hidden="true" @attributes="AdditionalAttributes"></div>

@code {
    /// <summary>Fixed spacer size from the <c>--sp-*</c> scale. When null, the
    /// spacer grows to fill available space (<c>flex: 1</c>).</summary>
    [Parameter] public DrylStack.StackGap? Size { get; set; }

    /// <summary>Pass-through HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Extra CSS class(es) merged onto the spacer's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    private string Style => Size is { } s
        ? $"flex: 0 0 auto; width: {DrylGrid.GapToken(s)}; height: {DrylGrid.GapToken(s)};"
        : "flex: 1 1 auto;";
}
```

- [ ] **Step 4: Run, verify pass.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylSpacerTests`. Expected: PASS.

- [ ] **Step 5:** `CHANGELOG.md` → `Added`:
```markdown
- `DrylSpacer` — Layout spacer; grows to fill by default or a fixed `Size` from the spacing scale.
```

- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/Components/Layout/DrylSpacer.razor tests/DRYL.Components.Tests/DrylSpacerTests.cs CHANGELOG.md
git commit -m "feat(layout): add DrylSpacer primitive"
```

---

### Task 6: `DrylAspectRatio`

**Files:**
- Create: `DRYL.Components/Components/Layout/DrylAspectRatio.razor`
- Test: `tests/DRYL.Components.Tests/DrylAspectRatioTests.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Produces: `<DrylAspectRatio Ratio RatioValue? Class>`; nested `DrylAspectRatio.AspectRatio { Square, Video, Photo, Wide, Custom }` → `1/1`, `16/9`, `4/3`, `21/9`, custom uses `RatioValue`. Root gets `aspect-ratio` + `max-width:100%`.

- [ ] **Step 1: Write failing tests** — `DrylAspectRatioTests.cs`:

```csharp
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylAspectRatioTests : BunitContext
{
    [Theory]
    [InlineData(DrylAspectRatio.AspectRatio.Square, "1 / 1")]
    [InlineData(DrylAspectRatio.AspectRatio.Video, "16 / 9")]
    [InlineData(DrylAspectRatio.AspectRatio.Photo, "4 / 3")]
    [InlineData(DrylAspectRatio.AspectRatio.Wide, "21 / 9")]
    public void Ratio_maps_to_css(DrylAspectRatio.AspectRatio r, string expected)
    {
        var cut = Render<DrylAspectRatio>(ps => ps.Add(p => p.Ratio, r).AddChildContent("x"));
        var style = cut.Find("div.aspect").GetAttribute("style")!;
        Assert.Contains($"aspect-ratio: {expected}", style);
        Assert.Contains("max-width: 100%", style);
    }

    [Fact]
    public void Custom_uses_ratio_value()
    {
        var cut = Render<DrylAspectRatio>(ps => ps
            .Add(p => p.Ratio, DrylAspectRatio.AspectRatio.Custom)
            .Add(p => p.RatioValue, "3 / 2").AddChildContent("x"));
        Assert.Contains("aspect-ratio: 3 / 2", cut.Find("div.aspect").GetAttribute("style"));
    }

    [Fact]
    public void Default_is_video()
    {
        var cut = Render<DrylAspectRatio>(ps => ps.AddChildContent("x"));
        Assert.Contains("aspect-ratio: 16 / 9", cut.Find("div.aspect").GetAttribute("style"));
    }
}
```

- [ ] **Step 2: Run, verify fail.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAspectRatioTests`. Expected: FAIL.

- [ ] **Step 3: Implement** `DrylAspectRatio.razor`:

```razor
@namespace DRYL.Components

@*  DrylAspectRatio — holds a fixed aspect ratio for media/embeds and never
    exceeds its slot. The child fills via object-fit: cover.  *@

<div class="aspect @Class" style="@Style" @attributes="AdditionalAttributes">
    @ChildContent
</div>

@code {
    /// <summary>Aspect ratio preset. Defaults to <see cref="AspectRatio.Video"/> (16/9).</summary>
    [Parameter] public AspectRatio Ratio { get; set; } = AspectRatio.Video;

    /// <summary>Custom <c>aspect-ratio</c> value (e.g. "3 / 2"); used only when
    /// <see cref="Ratio"/> is <see cref="AspectRatio.Custom"/>.</summary>
    [Parameter] public string? RatioValue { get; set; }

    /// <summary>Boxed content (image, iframe, video …).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Pass-through HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Extra CSS class(es) merged onto the box's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    private string Style
    {
        get
        {
            var ratio = Ratio switch
            {
                AspectRatio.Square => "1 / 1",
                AspectRatio.Photo  => "4 / 3",
                AspectRatio.Wide   => "21 / 9",
                AspectRatio.Custom => string.IsNullOrWhiteSpace(RatioValue) ? "16 / 9" : RatioValue!,
                _ => "16 / 9"
            };
            return $"aspect-ratio: {ratio}; max-width: 100%; overflow: hidden;";
        }
    }

    /// <summary>Aspect-ratio presets for <see cref="DrylAspectRatio"/>.</summary>
    public enum AspectRatio { Square, Video, Photo, Wide, Custom }
}
```

Also add to `DrylAspectRatio.razor.css` (cover behavior for media children):
```css
.aspect > :deep(img), .aspect > :deep(video), .aspect > :deep(iframe) {
    width: 100%; height: 100%; object-fit: cover; border: 0; display: block;
}
```

- [ ] **Step 4: Run, verify pass.** Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAspectRatioTests`. Expected: PASS.

- [ ] **Step 5:** `CHANGELOG.md` → `Added`:
```markdown
- `DrylAspectRatio` — Holds a fixed ratio (Square / Video / Photo / Wide / Custom) for media and embeds; never exceeds its slot.
```

- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/Components/Layout/DrylAspectRatio.razor DRYL.Components/Components/Layout/DrylAspectRatio.razor.css tests/DRYL.Components.Tests/DrylAspectRatioTests.cs CHANGELOG.md
git commit -m "feat(layout): add DrylAspectRatio primitive"
```

---

### Task 7: Docs site — pages, examples, catalog for the four new components

**Files:**
- Create: `DRYL.Website/Components/Examples/Grid/AutoFit.razor`, `Grid/FixedColumns.razor`
- Create: `DRYL.Website/Components/Examples/Container/Sizes.razor`
- Create: `DRYL.Website/Components/Examples/Spacer/Toolbar.razor`
- Create: `DRYL.Website/Components/Examples/AspectRatio/Ratios.razor`
- Create: `DRYL.Website/Components/Pages/DemoGrid.razor`, `DemoContainer.razor`, `DemoSpacer.razor`, `DemoAspectRatio.razor`
- Modify: `DRYL.Website/Components/ComponentCatalog.cs`

**Interfaces:**
- Consumes: the four new components + `DemoExample` / `ComponentDocHeader` website patterns.
- Produces: four catalog entries (`grid`, `container`, `spacer`, `aspect-ratio`) under "Layout"; routes `/components/<slug>`.

- [ ] **Step 1:** Add four entries to `ComponentCatalog.cs` in the Layout block (after the `Stack` entry, line 114):
```csharp
new("Grid",         "grid",         "Layout", "DrylGrid",         "Layout", false, "Responsive column grid — auto-fit or fixed columns.",   "Blocks"),
new("Container",    "container",    "Layout", "DrylContainer",    "Layout", false, "Centers content at a readable max width with padding.", "Square"),
new("Spacer",       "spacer",       "Layout", "DrylSpacer",       "Layout", false, "Flexible or fixed layout spacer.",                       "Minus"),
new("Aspect Ratio", "aspect-ratio", "Layout", "DrylAspectRatio",  "Layout", false, "Fixed-ratio box for media and embeds.",                 "Image"),
```
(If any `Icon` name above isn't in DrylIcon, substitute an existing one — verify against `DrylIcon`.)

- [ ] **Step 2:** Create each example `.razor` (real component usage). E.g. `Grid/AutoFit.razor`:
```razor
<DrylGrid MinItemWidth="DrylGrid.ItemWidth.Sm" Gap="DrylStack.StackGap.Lg">
    @for (var i = 1; i <= 6; i++)
    {
        <DrylCard Spotlight="false" Padding="DrylCard.CardPadding.Tight">Cell @i</DrylCard>
    }
</DrylGrid>
```
`Grid/FixedColumns.razor`:
```razor
<DrylGrid Columns="3" Gap="DrylStack.StackGap.Md">
    @for (var i = 1; i <= 6; i++)
    {
        <DrylCard Spotlight="false" Padding="DrylCard.CardPadding.Tight">Col @i</DrylCard>
    }
</DrylGrid>
```
`Container/Sizes.razor`:
```razor
<DrylContainer Size="DrylContainer.ContainerSize.Sm">
    <DrylCard Spotlight="false">Centered at Sm (40rem) with responsive padding.</DrylCard>
</DrylContainer>
```
`Spacer/Toolbar.razor`:
```razor
<DrylStack Direction="DrylStack.StackDirection.Horizontal" Align="DrylStack.StackAlign.Center">
    <DrylBadge>Left</DrylBadge>
    <DrylSpacer />
    <DrylButton Variant="DrylButton.ButtonVariant.Secondary">Right</DrylButton>
</DrylStack>
```
`AspectRatio/Ratios.razor`:
```razor
<DrylGrid MinItemWidth="DrylGrid.ItemWidth.Xs" Gap="DrylStack.StackGap.Md">
    <DrylAspectRatio Ratio="DrylAspectRatio.AspectRatio.Video"><DrylCard Spotlight="false">16 / 9</DrylCard></DrylAspectRatio>
    <DrylAspectRatio Ratio="DrylAspectRatio.AspectRatio.Square"><DrylCard Spotlight="false">1 / 1</DrylCard></DrylAspectRatio>
</DrylGrid>
```

- [ ] **Step 3:** Create each `Demo<Comp>.razor` page mirroring `DemoStack.razor` (page route `/components/<slug>`, `ComponentDocHeader Slug=...`, one `DemoExample` per example). Example `DemoGrid.razor`:
```razor
@page "/components/grid"
<PageTitle>DRYL — Grid</PageTitle>
<div class="col fade-in" style="gap: var(--sp-7);">
    <ComponentDocHeader Slug="grid">
        A responsive column grid. Auto-fit by default via <code>MinItemWidth</code>,
        or a fixed <code>Columns</code> count that steps down on narrow slots.
    </ComponentDocHeader>
    <DemoExample Title="Auto-fit" Source="Grid/AutoFit">
        <DRYL.Website.Components.Examples.Grid.AutoFit />
    </DemoExample>
    <DemoExample Title="Fixed columns" Source="Grid/FixedColumns">
        <DRYL.Website.Components.Examples.Grid.FixedColumns />
    </DemoExample>
</div>
```
(Repeat the analogous page for Container, Spacer, AspectRatio.)

- [ ] **Step 4:** Build the website. Run: `dotnet build DRYL.Website/DRYL.Website.csproj`. Expected: Build succeeded.

- [ ] **Step 5: Commit.**
```bash
git add DRYL.Website/Components/Examples DRYL.Website/Components/Pages DRYL.Website/Components/ComponentCatalog.cs
git commit -m "docs(website): catalog + demo pages for Grid/Container/Spacer/AspectRatio"
```

---

## PHASE 2 — Existing-Component Hardening

> Each batch is CSS-only (or minimal markup) hardening verified visually on DRYL.Website at 375px. Acceptance per batch: **no horizontal overflow at 375px, nothing clipped right**. Run the website (`dotnet run --project DRYL.Website`) and check each touched component page with Playwright at a 375×812 viewport, asserting `document.documentElement.scrollWidth <= window.innerWidth`.

### Task 8: Batch A — Cards & Surfaces

**Files:** Modify `DRYL.Components/wwwroot/dryl.css` (`.glass-card` internals) and, if present, the website demo wrapper CSS (Preview/Code tabs).

- [ ] **Step 1:** Reproduce: run website, open `/components/cards` at 375px, confirm horizontal overflow / clipped badges (matches the reported screenshots).
- [ ] **Step 2:** In `dryl.css`, ensure rows commonly placed inside cards wrap and children may shrink. Confirm the global safety layer (Task 1) already applies `min-width:0` to `.glass-card`/`.row`; add `flex-wrap: wrap` to the generic in-card header row utility if one exists (`.between` stays nowrap for true space-between bars, but add `.between { gap: var(--sp-2); }` and allow its children `min-width:0`). Add:
```css
.glass-card .row { flex-wrap: wrap; }
```
- [ ] **Step 3:** Fix the website demo wrapper (Preview/Code) so the preview area scrolls instead of bursting: add `overflow-x: auto` to the demo preview container class (locate via grep for the DemoExample wrapper class).
- [ ] **Step 4: Verify** at 375px: no page overflow on `/components/cards`. Re-screenshot.
- [ ] **Step 5:** `CHANGELOG.md` → `Fixed`:
```markdown
- `DrylCard` — Card content now wraps instead of clipping on narrow screens (rows wrap, children may shrink).
```
- [ ] **Step 6: Commit.**
```bash
git add DRYL.Components/wwwroot/dryl.css DRYL.Website
git commit -m "fix(card): wrap card content on narrow screens (Batch A)"
```

### Task 9: Batch B — AppBar / Topbar

**Files:** Modify `DRYL.Components/wwwroot/dryl.css` (`.topbar`, `.topbar-start/center/end`) and/or `DrylAppBar.razor`.

- [ ] **Step 1:** Reproduce at 375px on a page with a full topbar (search pill + theme button clipped — screenshot 1).
- [ ] **Step 2:** Allow the search pill / center slot to shrink: confirm `.topbar-*` already have `min-width:0` (they do per dryl.css:1476-1478). Add a container query so the topbar tightens on narrow: reduce horizontal padding and let the center (search) slot shrink first:
```css
@container (max-width: 480px) {
  .topbar-center { display: none; }   /* search collapses to an icon trigger */
}
```
(If the topbar isn't a `.cq` container, add `.cq` to its root in `DrylAppBar.razor`; ensure the search has an icon-only fallback trigger — wrap that icon button in a `DrylTooltip` per rule 2.11.)
- [ ] **Step 3: Verify** at 375px: topbar fits, theme + menu buttons visible, no clipping.
- [ ] **Step 4:** `CHANGELOG.md` → `Fixed`:
```markdown
- `DrylAppBar` — Topbar no longer overflows on phones; the center/search slot collapses on narrow widths.
```
- [ ] **Step 5: Commit.**
```bash
git add DRYL.Components/wwwroot/dryl.css DRYL.Components/Components/Layout/DrylAppBar.razor
git commit -m "fix(appbar): collapse topbar center slot on narrow screens (Batch B)"
```

### Task 10: Batch C — Data (Table / DescriptionList / Pagination)

**Files:** `DrylTable.razor`(.css), `DrylDescriptionList.razor`(.css), `DrylPagination.razor`(.css).

- [ ] **Step 1:** Reproduce each at 375px.
- [ ] **Step 2:** `DrylTable` — wrap the `<table>` in a scroll container: `.dryl-table-scroll { overflow-x: auto; max-width: 100%; }` (thin DRYL scrollbar). Table keeps its columns; the wrapper scrolls instead of clipping the page.
- [ ] **Step 3:** `DrylDescriptionList` — below `Md`, force single column. Add `.cq` to its root and:
```css
@container (max-width: 768px) { .desc-list-cols { grid-template-columns: 1fr; } }
```
(use the actual grid class name from the component.)
- [ ] **Step 4:** `DrylPagination` — on narrow, hide the numbered middle and keep prev/next + current (use a container query to `display:none` the page-number list below `Sm`).
- [ ] **Step 5: Verify** all three at 375px.
- [ ] **Step 6:** `CHANGELOG.md` → `Fixed` (one bullet per component).
- [ ] **Step 7: Commit.**
```bash
git commit -am "fix(data): responsive Table/DescriptionList/Pagination (Batch C)"
```

### Task 11: Batch D — Navigation (Tabs / Stepper / Breadcrumbs)

**Files:** `DrylTabs.razor`(.css), `DrylStepper.razor`(.css), `DrylBreadcrumbs.razor`(.css).

- [ ] **Step 1:** Reproduce at 375px.
- [ ] **Step 2:** `DrylTabs` / `DrylStepper` (horizontal) — make the strip horizontally scrollable: `overflow-x: auto; flex-wrap: nowrap;` with thin scrollbar; ensure the gliding indicator still tracks. Verify the indicator stays `aria-hidden`.
- [ ] **Step 3:** `DrylBreadcrumbs` — allow wrapping: `flex-wrap: wrap;` and `overflow-wrap: anywhere` on long crumbs.
- [ ] **Step 4: Verify** at 375px.
- [ ] **Step 5:** `CHANGELOG.md` → `Fixed`.
- [ ] **Step 6: Commit.**
```bash
git commit -am "fix(nav): scrollable Tabs/Stepper, wrapping Breadcrumbs (Batch D)"
```

### Task 12: Batch E — Overlays (Dialog / Popover / Menu / Toast)

**Files:** `dryl.css` overlay rules and/or the respective `.razor.css`.

- [ ] **Step 1:** Reproduce at 375px (overlays exceeding the viewport).
- [ ] **Step 2:** Constrain each overlay width to the viewport: `max-width: min(<existing>, calc(100vw - var(--sp-4)));` for Dialog, Popover, Menu, Toast. (Dialog already uses `min(440px, calc(100vw - var(--sp-7)))` per dryl.css:2671 — apply the same pattern to the others that lack it.)
- [ ] **Step 3: Verify** each overlay at 375px stays within the viewport.
- [ ] **Step 4:** `CHANGELOG.md` → `Fixed`.
- [ ] **Step 5: Commit.**
```bash
git commit -am "fix(overlays): constrain Dialog/Popover/Menu/Toast to viewport (Batch E)"
```

---

## Final verification

- [ ] Run full test suite: `dotnet test DRYL.slnx`. Expected: all pass.
- [ ] Build website: `dotnet build DRYL.Website/DRYL.Website.csproj`. Expected: succeeded.
- [ ] Playwright sweep: for each touched component page, load at 375×812 and assert no horizontal scroll (`scrollWidth <= clientWidth`).
- [ ] `CHANGELOG.md` `[Unreleased]` reviewed — every new component + every fix has a bullet.
- [ ] `ComponentCatalog` has the four new Layout entries.

---

## Self-Review notes (spec coverage)

- Spec §3 Foundation → Task 1. ✅
- Spec §4.1–4.5 primitives → Tasks 2–6. ✅
- Spec §4.6 animation note → structural primitives documented; Grid reflow + Stack collapse covered (flex-direction flip is a structural reflow softened for reduced-motion, called out in PRs). ✅
- Spec §5 batches A–E → Tasks 8–12. ✅
- Spec §6 verification → Phase 2 acceptance + Final verification. ✅
- Spec §7 docs/catalog → Task 7 + per-task CHANGELOG steps. ✅
