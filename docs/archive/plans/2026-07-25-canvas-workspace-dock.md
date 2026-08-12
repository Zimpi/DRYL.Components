# Canvas Workspace + Prompt Dock — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Phase 3 der Canvas-Plattform — `DrylCanvasWorkspace` (benannte Views, genau eine sichtbar, morphender Wechsel) im Kern und `DrylCanvasDock` (Befehlsleiste statt Chat-Spalte) plus das `open_view`-Tool im Agents-Paket.

**Architecture:** Der Kern bekommt einen beobachtbaren Zustandshalter `CanvasWorkspace` (Views, aktive View, `OnChange`) neben `CanvasFormState`/`CanvasPulseTracker` und die Komponente `DrylCanvasWorkspace`, die ihn rendert: Chip-Leiste mit dem geteilten `[data-dryl-ink]`-Gleiter der Tabs, Körper über einen `RenderFragment<CanvasView>`-Slot, Wechsel durch `IDrylViewTransition`. Im Agents-Paket wird `DrylCanvasRun` auf die aktive View projiziert (`run.Spec` == `workspace.Active.Spec`), `DrylCanvasTools` bekommt bei gesetztem Workspace ein drittes Tool `open_view`, und `DrylCanvasDock` ist eine im Top-Layer schwebende Karte aus Statuszeile, aufklappbarem Host-`<Log>` und `DrylChatComposer`.

**Tech Stack:** .NET 9 / Blazor (Server + WASM), xUnit + bUnit, CSS-Isolation mit den DRYL-Tokens, `dryl.js` (`dryl.motion.moveIndicator`, `dryl.topLayer`, `dryl.chat.scrollToEnd`) — keine npm-Abhängigkeit.

**Spec:** `docs/superpowers/specs/2026-07-25-canvas-workspace-dock-design.md`

## Global Constraints

- **Tokens statt Literale.** Jede Farbe, jedes Padding, jeder Radius, jede Dauer, jede Kurve ist eine CSS-Variable. Erlaubt sind ausschließlich `--dur-fast|med|slow` und `--ease-out|in-out|spring|viscous`. Keine neuen Tokens in dieser Phase.
- **Beide Farbmodi.** Nie eine modusannehmende Farbe (`rgba(255,255,255,…)`) in Komponenten-CSS.
- **Jede Komponente ist animiert** (Regel 2.12): Enter/Exit über `DrylPresence`, Layoutbewegung über den geteilten Gleiter, `prefers-reduced-motion: reduce` in jedem eigenen CSS-Block abgeschaltet.
- **Icon-only-Knöpfe brauchen `DrylTooltip` *und* gleichlautendes `AriaLabel`** (Regel 2.11).
- **`Class`-Parameter mergen, nicht ersetzen** — bestehende Hauskonvention (`ClassMergeTests`).
- **Bibliotheks-Strings sind Englisch**; Zahlen in Text immer über `FormattableString.Invariant(...)`.
- **JS-Interop nur mit Prerender-Schutz:** `try/catch (JSDisconnectedException)` + `catch (InvalidOperationException)` und ein `_attached`-Flag, bevor beim Entsorgen zurückgerufen wird.
- **Kein neuer NuGet-/npm-Zusatz.**
- **Versionen am Ende:** Kern `2.13.0 → 2.14.0`, Agents `0.11.0 → 0.12.0`, CHANGELOG-Release im selben Commit.
- Build/Test: `dotnet build DRYL.sln` und `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`.

---

## File Structure

**Kern (`DRYL.Components`)**
- Create `Canvas/CanvasWorkspace.cs` — `CanvasView` + `CanvasWorkspace` (Zustand, keine UI).
- Create `Components/AI/DrylCanvasWorkspace.razor` + `.razor.css` — Leiste, Körper, Wechsel-Morph.
- Modify `DRYL.Components.csproj` — `<Version>`.

**Agents (`DRYL.Components.Agents`)**
- Modify `Canvas/DrylCanvasRun.cs` — Workspace-Projektion, Epoch-Bump, Morph-Unterdrückung.
- Modify `Canvas/DrylAiCanvas.razor` — Unterdrückung beim Spec-Swap abfragen.
- Modify `Canvas/DrylCanvasTools.cs` — optionaler Workspace, Tool `open_view`.
- Create `Canvas/DrylCanvasDock.razor` + `.razor.css` und `Canvas/DockCorner.cs`.
- Modify `DRYL.Components.Agents.csproj` — `<Version>`.

**Tests (`tests/DRYL.Components.Tests`)**
- Create `Canvas/CanvasWorkspaceTests.cs`, `Canvas/DrylCanvasWorkspaceTests.cs`.
- Create `Agents/Canvas/CanvasRunWorkspaceTests.cs`, `Agents/Canvas/CanvasOpenViewToolTests.cs`, `Agents/Canvas/DrylCanvasDockTests.cs`.

**Website (`DRYL.Website`)**
- Create `Components/Pages/DemoCanvasWorkspace.razor`, `Components/Examples/Agents/CanvasWorkspaceDemo.razor`, `Components/Examples/Agents/OpenAiCanvasWorkspace.razor`.
- Modify `Components/ComponentCatalog.cs`.

**Doku**
- Modify `CHANGELOG.md`.

---

### Task 1: `CanvasWorkspace` — der Zustand

**Files:**
- Create: `DRYL.Components/Canvas/CanvasWorkspace.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceTests.cs`

**Interfaces:**
- Consumes: nichts.
- Produces: `DRYL.Components.Canvas.CanvasView` (`string Id {get;init;}`, `string Title {get;set;}`, `string? Icon {get;set;}`, `CanvasSpec? Spec {get;set;}`, `bool Removing {get; internal set;}`) und `DRYL.Components.Canvas.CanvasWorkspace` mit `IReadOnlyList<CanvasView> Views`, `string? ActiveId`, `CanvasView? Active`, `event Action? OnChange`, `CanvasView Open(string title, string? icon = null)`, `bool Activate(string id)`, `void Close(string id)`, `void Remove(string id)`, `void Clear()`.

- [ ] **Step 1: Testdatei anlegen (schlägt fehl, weil es die Typen nicht gibt)**

`tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceTests.cs`:

```csharp
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The workspace state behind A5: named views, exactly one active.</summary>
public class CanvasWorkspaceTests
{
    [Fact]
    public void Open_creates_activates_and_notifies()
    {
        var ws = new CanvasWorkspace();
        var changes = 0;
        ws.OnChange += () => changes++;

        var view = ws.Open("Auftrag 4711");

        Assert.Equal("auftrag-4711", view.Id);
        Assert.Equal("Auftrag 4711", view.Title);
        Assert.Same(view, ws.Active);
        Assert.Single(ws.Views);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Open_with_a_known_title_activates_the_existing_view()
    {
        var ws = new CanvasWorkspace();
        var first = ws.Open("Übersicht");
        first.Spec = new CanvasSpec { Title = "Übersicht" };
        ws.Open("Auftrag 4711");

        var again = ws.Open("Übersicht");

        Assert.Same(first, again);
        Assert.Equal(2, ws.Views.Count);
        Assert.Same(first.Spec, ws.Active!.Spec);   // the spec survives re-opening
    }

    [Fact]
    public void Colliding_slugs_get_a_suffix()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Order 42");
        var b = ws.Open("Order/42");

        Assert.Equal("order-42", a.Id);
        Assert.Equal("order-42-2", b.Id);
    }

    [Fact]
    public void An_empty_title_still_yields_an_id()
    {
        var ws = new CanvasWorkspace();
        Assert.Equal("view", ws.Open("   ").Id);
    }

    [Fact]
    public void Activate_switches_and_notifies_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Activate(a.Id));
        Assert.False(ws.Activate(a.Id));            // already active — no second event
        Assert.False(ws.Activate("nope"));
        Assert.Same(a, ws.Active);
        Assert.Equal(1, changes);
        Assert.NotSame(b, ws.Active);
    }

    [Fact]
    public void Close_flags_the_view_and_hands_the_active_slot_to_a_neighbour()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");   // active

        ws.Close(b.Id);

        Assert.True(b.Removing);
        Assert.Equal(2, ws.Views.Count);            // still there — the chip is animating out
        Assert.Same(a, ws.Active);                  // the body already shows the neighbour
    }

    [Fact]
    public void Closing_twice_notifies_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Close(a.Id);
        ws.Close(a.Id);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Remove_drops_the_view_and_the_last_one_leaves_nothing_active()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");

        ws.Remove(b.Id);
        Assert.Same(a, ws.Active);
        Assert.Single(ws.Views);

        ws.Remove(a.Id);
        Assert.Null(ws.Active);
        Assert.Null(ws.ActiveId);
        Assert.Empty(ws.Views);
    }

    [Fact]
    public void Remove_of_an_unknown_id_is_a_no_op()
    {
        var ws = new CanvasWorkspace();
        ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Remove("nope");

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Clear_empties_the_workspace_once()
    {
        var ws = new CanvasWorkspace();
        ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Clear();
        ws.Clear();

        Assert.Empty(ws.Views);
        Assert.Null(ws.ActiveId);
        Assert.Equal(1, changes);
    }
}
```

- [ ] **Step 2: Test laufen lassen — er muss scheitern**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~CanvasWorkspaceTests`
Expected: Compile-Fehler `CS0246: The type or namespace name 'CanvasWorkspace' could not be found`.

- [ ] **Step 3: Implementierung schreiben**

`DRYL.Components/Canvas/CanvasWorkspace.cs`:

```csharp
using System.Text;

namespace DRYL.Components.Canvas;

/// <summary>
/// One named artifact in a <see cref="CanvasWorkspace"/> — the unit the user comes back to.
/// A view owns its title and its spec; who authored that spec (code, a store, an AI run) is
/// none of its business.
/// </summary>
public sealed class CanvasView
{
    /// <summary>Stable key — the slug of the title the view was opened with.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Label shown on the view's chip.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional <c>DrylIcon</c> name shown left of the title.</summary>
    public string? Icon { get; set; }

    /// <summary>The artifact this view shows, or null while it is still empty.</summary>
    public CanvasSpec? Spec { get; set; }

    /// <summary>True while the chip plays its exit animation; the bar calls
    /// <see cref="CanvasWorkspace.Remove"/> when that animation ends.</summary>
    public bool Removing { get; internal set; }
}

/// <summary>
/// The named views of one canvas surface, exactly one of them active (A5). The workspace is
/// plain observable state — <c>DrylCanvasWorkspace</c> renders it, an AI run writes into its
/// active view, and a host may pre-fill it from code.
/// </summary>
/// <remarks>Renderer-thread state, like <c>DrylRunBase</c>: no locking, no
/// <c>INotifyPropertyChanged</c>. Every mutation that actually changed something raises
/// <see cref="OnChange"/> exactly once.</remarks>
public sealed class CanvasWorkspace
{
    private readonly List<CanvasView> _views = new();

    /// <summary>The views, in the order they were opened.</summary>
    public IReadOnlyList<CanvasView> Views => _views;

    /// <summary>Id of the view currently shown, or null while the workspace is empty.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The view currently shown, or null while the workspace is empty.</summary>
    public CanvasView? Active =>
        ActiveId is null ? null : _views.FirstOrDefault(v => v.Id == ActiveId);

    /// <summary>Raised after every mutation that changed the workspace.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Opens the view with this title and activates it. A title whose slug is already present
    /// re-activates that view and keeps its spec — that is what makes "back to the overview"
    /// work for both the user and the model.
    /// </summary>
    public CanvasView Open(string title, string? icon = null)
    {
        var slug = Slug(title);
        if (_views.FirstOrDefault(v => v.Id == slug) is { } existing)
        {
            existing.Removing = false;   // re-opening a closing view keeps it alive
            if (icon is not null) existing.Icon = icon;
            if (ActiveId != existing.Id) ActiveId = existing.Id;
            OnChange?.Invoke();
            return existing;
        }

        var view = new CanvasView { Id = Unique(slug), Title = title.Trim(), Icon = icon };
        _views.Add(view);
        ActiveId = view.Id;
        OnChange?.Invoke();
        return view;
    }

    /// <summary>Shows the view with this id. Returns false when it is unknown or already active.</summary>
    public bool Activate(string id)
    {
        if (ActiveId == id) return false;
        if (_views.All(v => v.Id != id)) return false;
        ActiveId = id;
        OnChange?.Invoke();
        return true;
    }

    /// <summary>
    /// Starts closing a view: it is flagged for its exit animation and, if it was active, hands
    /// the active slot to a neighbour right away — the body must not keep showing something that
    /// is on its way out.
    /// </summary>
    public void Close(string id)
    {
        var index = _views.FindIndex(v => v.Id == id);
        if (index < 0 || _views[index].Removing) return;

        _views[index].Removing = true;
        if (ActiveId == id) ActiveId = Neighbour(index);
        OnChange?.Invoke();
    }

    /// <summary>Drops the view — called by the bar once the exit animation finished.</summary>
    public void Remove(string id)
    {
        var index = _views.FindIndex(v => v.Id == id);
        if (index < 0) return;

        var wasActive = ActiveId == id;
        _views.RemoveAt(index);
        if (wasActive) ActiveId = Neighbour(index);
        OnChange?.Invoke();
    }

    /// <summary>Empties the workspace.</summary>
    public void Clear()
    {
        if (_views.Count == 0 && ActiveId is null) return;
        _views.Clear();
        ActiveId = null;
        OnChange?.Invoke();
    }

    // The nearest view that is not itself on its way out: right first, then left.
    private string? Neighbour(int index)
    {
        for (var i = index; i < _views.Count; i++)
            if (!_views[i].Removing) return _views[i].Id;
        for (var i = Math.Min(index, _views.Count) - 1; i >= 0; i--)
            if (!_views[i].Removing) return _views[i].Id;
        return null;
    }

    private string Unique(string slug)
    {
        if (_views.All(v => v.Id != slug)) return slug;
        for (var n = 2; ; n++)
        {
            var candidate = FormattableString.Invariant($"{slug}-{n}");
            if (_views.All(v => v.Id != candidate)) return candidate;
        }
    }

    // Lower-cased, letters and digits kept, everything else collapsed to a single dash.
    // Culture-invariant on purpose: the id is a key, not display text.
    private static string Slug(string? title)
    {
        var sb = new StringBuilder();
        foreach (var ch in (title ?? string.Empty).Trim())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? "view" : slug;
    }
}
```

- [ ] **Step 4: Tests laufen lassen — sie müssen bestehen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~CanvasWorkspaceTests`
Expected: PASS (10 Tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Canvas/CanvasWorkspace.cs tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceTests.cs
git commit -m "feat(canvas): CanvasWorkspace — benannte Views, genau eine aktiv"
```

---

### Task 2: `DrylCanvasWorkspace` — Leiste, Körper, Morph

**Files:**
- Create: `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor`
- Create: `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceTests.cs`

**Interfaces:**
- Consumes: `CanvasWorkspace`, `CanvasView` aus Task 1; `IDrylViewTransition`, `DrylPresence`, `DrylCanvas`, `DrylEmptyState`, `DrylTooltip`, `DrylButton`, `DrylIcon`; JS `dryl.motion.moveIndicator` / `dryl.motion.disposeIndicator`.
- Produces: Komponente `DRYL.Components.DrylCanvasWorkspace` mit den Parametern `Workspace` (`CanvasWorkspace?`), `View` (`RenderFragment<CanvasView>?`), `AllowClose` (`bool` = true), `ShowBarWhenSingle` (`bool` = false), `EmptyText` (`string?`), `AriaLabel` (`string` = "Views"), `Class`, `AdditionalAttributes`.

- [ ] **Step 1: Test schreiben**

`tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceTests.cs`:

```csharp
using Bunit;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The workspace surface: named views, exactly one rendered, the rest one click away.</summary>
public class DrylCanvasWorkspaceTests : BunitContext
{
    public DrylCanvasWorkspaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasWorkspace TwoViews()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Overview");
        a.Spec = new CanvasSpec
        {
            Title = "Overview",
            Root = new CanvasNode { Id = "r1", Type = "text", Props = new() { ["text"] = "first view" } },
        };
        var b = ws.Open("Order 4711");
        b.Spec = new CanvasSpec
        {
            Title = "Order 4711",
            Root = new CanvasNode { Id = "r2", Type = "text", Props = new() { ["text"] = "second view" } },
        };
        ws.Activate(a.Id);
        return ws;
    }

    [Fact]
    public void Renders_a_chip_per_view_and_marks_the_active_one()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, TwoViews()));

        var chips = cut.FindAll(".ws-chip");
        Assert.Equal(2, chips.Count);
        Assert.Contains("Order 4711", cut.Markup);

        var active = cut.Find("[data-dryl-ink-active='true']");
        Assert.Contains("Overview", active.TextContent);
        Assert.Equal("true", cut.Find("[role='tab'][aria-selected='true']").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Renders_only_the_active_view()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, TwoViews()));

        Assert.Contains("first view", cut.Markup);
        Assert.DoesNotContain("second view", cut.Markup);
    }

    [Fact]
    public void Clicking_a_chip_activates_that_view()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll("[role='tab']")[1].Click();

        Assert.Equal("order-4711", ws.ActiveId);
        Assert.Contains("second view", cut.Markup);
    }

    [Fact]
    public void A_single_view_gets_no_bar_unless_asked_for()
    {
        var ws = new CanvasWorkspace();
        ws.Open("Only");

        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));
        Assert.Empty(cut.FindAll(".ws-chip"));

        cut.Render(p => p.Add(x => x.Workspace, ws).Add(x => x.ShowBarWhenSingle, true));
        Assert.Single(cut.FindAll(".ws-chip"));
    }

    [Fact]
    public void The_View_slot_receives_the_active_view()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add<CanvasView>(x => x.View, view => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "slot:" + view.Title);
                builder.CloseElement();
            }));

        Assert.Contains("slot:Overview", cut.Markup);
        Assert.DoesNotContain("first view", cut.Markup);   // the slot replaces the default canvas
    }

    [Fact]
    public void Closing_a_chip_flags_the_view_for_its_exit()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll(".ws-chip-close")[1].Click();

        Assert.True(ws.Views[1].Removing);
        Assert.Equal(2, ws.Views.Count);   // removal waits for the exit animation
    }

    [Fact]
    public void Close_buttons_disappear_when_AllowClose_is_off()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, TwoViews())
            .Add(x => x.AllowClose, false));

        Assert.Empty(cut.FindAll(".ws-chip-close"));
    }

    [Fact]
    public void Arrow_keys_walk_between_views()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll("[role='tab']")[0].KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("order-4711", ws.ActiveId);

        cut.FindAll("[role='tab']")[1].KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Home" });
        Assert.Equal("overview", ws.ActiveId);
    }

    [Fact]
    public void An_empty_workspace_shows_the_empty_state()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, new CanvasWorkspace())
            .Add(x => x.EmptyText, "Ask for a view."));

        Assert.Contains("Ask for a view.", cut.Markup);
        Assert.Empty(cut.FindAll(".ws-chip"));
    }
}
```

- [ ] **Step 2: Test laufen lassen — er muss scheitern**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~DrylCanvasWorkspaceTests`
Expected: Compile-Fehler `CS0246: … 'DrylCanvasWorkspace' could not be found`.

- [ ] **Step 3: Komponente schreiben**

`DRYL.Components/Components/AI/DrylCanvasWorkspace.razor`:

```razor
@namespace DRYL.Components
@using DRYL.Components.Canvas
@using DRYL.Components.Motion
@inject IJSRuntime JS
@inject IDrylViewTransition ViewTransition
@implements IAsyncDisposable

@*  ─────────────────────────────────────────────────────────
    DrylCanvasWorkspace — a set of named canvas views, exactly one visible.

    A line-of-business page is not one artifact; it is "the overview", "order
    4711", "last month's report" — and the user must be able to get back to
    what they had. The workspace keeps those views side by side as chips and
    shows exactly one of them large.

    Switching runs through IDrylViewTransition, so the surface morphs into the
    other view instead of snapping (A8: one state change, one movement), while
    the shared [data-dryl-ink] indicator glides between the chips — the same
    primitive DrylTabs uses.

    Usage:
      <DrylCanvasWorkspace Workspace="_ws">
          <View><DrylAiCanvas Run="_run" AllowExpand="false" /></View>
      </DrylCanvasWorkspace>
    ───────────────────────────────────────────────────────── *@

<div class="canvas-workspace @Class" @attributes="AdditionalAttributes">
    @if (ShowBar)
    {
        <div class="ws-bar" role="tablist" aria-label="@AriaLabel" @ref="_bar">
            <div class="ws-ink" data-dryl-ink aria-hidden="true"></div>
            @foreach (var v in Workspace!.Views)
            {
                var view = v;                       // one capture per chip, not one for the loop
                var isActive = view.Id == Workspace.ActiveId;
                <DrylPresence @key="view.Id"
                              Visible="@(!view.Removing)"
                              Appear
                              Transition="PresenceTransition.Scale"
                              Speed="PresenceSpeed.Fast"
                              OnExited="@(() => Workspace.Remove(view.Id))">
                    <div class="ws-chip @(isActive ? "is-active" : null)"
                         data-dryl-ink-active="@(isActive ? "true" : null)">
                        <button type="button"
                                role="tab"
                                class="ws-chip-label"
                                aria-selected="@(isActive ? "true" : "false")"
                                tabindex="@(isActive ? 0 : -1)"
                                @onclick="@(() => ActivateAsync(view.Id))"
                                @onkeydown="@(e => OnKeyDownAsync(e, view))">
                            @if (!string.IsNullOrEmpty(view.Icon))
                            {
                                <DrylIcon Name="@view.Icon" Size="14" />
                            }
                            <span>@view.Title</span>
                        </button>
                        @if (AllowClose)
                        {
                            <DrylTooltip Text="@CloseLabel(view)">
                                <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                            Size="DrylButton.ButtonSize.Small"
                                            AriaLabel="@CloseLabel(view)"
                                            Class="ws-chip-close"
                                            OnClick="@(() => Workspace.Close(view.Id))">
                                    <DrylIcon Name="X" Size="12" />
                                </DrylButton>
                            </DrylTooltip>
                        }
                    </div>
                </DrylPresence>
            }
        </div>
    }

    <div class="ws-body">
        @if (Workspace?.Active is { } active)
        {
            @if (View is not null)
            {
                @View(active)
            }
            else
            {
                <DrylCanvas Spec="active.Spec" EmptyText="@EmptyText" />
            }
        }
        else
        {
            <DrylEmptyState Title="No view yet" Description="@EmptyText" />
        }
    </div>
</div>

@code {
    /// <summary>The views to show. Without one the workspace renders its empty state.</summary>
    [Parameter] public CanvasWorkspace? Workspace { get; set; }

    /// <summary>How the active view is rendered — put your <c>DrylAiCanvas</c> here. Without the
    /// slot the workspace renders a plain <c>DrylCanvas</c> over the view's spec.</summary>
    [Parameter] public RenderFragment<CanvasView>? View { get; set; }

    /// <summary>Whether each chip offers a close button. Default true.</summary>
    [Parameter] public bool AllowClose { get; set; } = true;

    /// <summary>Whether the bar shows for a single view too. Default false — one artifact
    /// deserves no chrome.</summary>
    [Parameter] public bool ShowBarWhenSingle { get; set; }

    /// <summary>Text shown while there is nothing to show.</summary>
    [Parameter] public string? EmptyText { get; set; } = "Ask the assistant to open one.";

    /// <summary>Accessible label of the view bar.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Views";

    /// <summary>Extra CSS class(es) merged onto the workspace root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Pass-through HTML attributes on the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    private CanvasWorkspace? _subscribed;
    private ElementReference _bar;
    private bool _inkAttached;
    private string? _lastInkActive;

    private bool ShowBar =>
        Workspace is { Views.Count: > 0 } && (ShowBarWhenSingle || Workspace.Views.Count > 1);

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Workspace)) return;
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
        _subscribed = Workspace;
        if (_subscribed is not null) _subscribed.OnChange += HandleChange;
    }

    // The workspace is mutated from anywhere (a chip, the host, an AI tool call) — a change
    // that did not come through our own handler still has to reach the DOM.
    private void HandleChange() => InvokeAsync(StateHasChanged);

    private Task ActivateAsync(string id)
    {
        if (Workspace is null || Workspace.ActiveId == id) return Task.CompletedTask;

        // The morph belongs to the workspace, not to whatever is inside the body: nesting two
        // view transitions loses one of the mutations.
        return ViewTransition.RunAsync(() =>
        {
            Workspace.Activate(id);
            StateHasChanged();
        });
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e, CanvasView view)
    {
        if (Workspace is null) return;
        var open = Workspace.Views.Where(v => !v.Removing).ToList();
        if (open.Count == 0) return;
        var index = open.IndexOf(view);
        if (index < 0) return;

        switch (e.Key)
        {
            case "ArrowRight": await ActivateAsync(open[(index + 1) % open.Count].Id); break;
            case "ArrowLeft":  await ActivateAsync(open[(index - 1 + open.Count) % open.Count].Id); break;
            case "Home":       await ActivateAsync(open[0].Id); break;
            case "End":        await ActivateAsync(open[^1].Id); break;
            case "Enter" or " ": await ActivateAsync(view.Id); break;
            case "Delete" or "Backspace" when AllowClose: Workspace.Close(view.Id); break;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Tells the browser the switched-to view has reached the DOM (cheap no-op otherwise).
        ViewTransition.SignalRendered();

        if (!ShowBar)
        {
            _lastInkActive = null;
            return;
        }

        // Re-measure the gliding indicator whenever the active chip changed — same contract
        // as DrylTabs, including the deferred is-ink-ready class inside moveIndicator.
        if (_lastInkActive == Workspace!.ActiveId && _inkAttached) return;
        _lastInkActive = Workspace.ActiveId;
        try
        {
            await JS.InvokeVoidAsync("dryl.motion.moveIndicator", _bar);
            _inkAttached = true;
        }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (InvalidOperationException) { /* prerender — no JS */ }
    }

    private static string CloseLabel(CanvasView view) => $"Close {view.Title}";

    public async ValueTask DisposeAsync()
    {
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;

        if (_inkAttached)
        {
            try { await JS.InvokeVoidAsync("dryl.motion.disposeIndicator", _bar); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (InvalidOperationException) { /* prerender — no JS */ }
        }
    }
}
```

- [ ] **Step 4: CSS schreiben**

`DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css`:

```css
/* DrylCanvasWorkspace — view bar plus the one visible view.
   Tokens only; the glide lives on .ws-ink, the same shape DrylTabs uses. */

.canvas-workspace {
    display: flex;
    flex-direction: column;
    gap: var(--sp-2);
    min-width: 0;
}

/* ---- View bar ---------------------------------------------------- */
.ws-bar {
    position: relative;
    display: flex;
    align-items: flex-end;
    gap: var(--sp-1);
    padding-bottom: 2px;
    border-bottom: 1px solid var(--line);
    overflow-x: auto;
    scrollbar-width: thin;
}

.ws-chip {
    display: inline-flex;
    align-items: center;
    gap: var(--sp-1);
    padding: var(--sp-1) var(--sp-2);
    border: 1px solid transparent;
    border-bottom: none;
    border-radius: var(--r-md) var(--r-md) 0 0;
    color: var(--fg-muted);
    white-space: nowrap;
    transition: color var(--dur-fast) var(--ease-out),
                border-color var(--dur-fast) var(--ease-out),
                background var(--dur-fast) var(--ease-out);
}

.ws-chip:hover {
    color: var(--fg);
    border-color: var(--line);
    background: var(--glass-1);
}

.ws-chip.is-active {
    color: var(--fg);
    border-color: var(--line-strong);
    background: var(--glass-1);
}

.ws-chip-label {
    display: inline-flex;
    align-items: center;
    gap: var(--sp-1);
    background: none;
    border: none;
    padding: 0;
    font: inherit;
    color: inherit;
    cursor: pointer;
}

.ws-chip-label:focus-visible {
    outline: none;
    box-shadow: 0 0 0 2px var(--accent-line);
    border-radius: var(--r-xs);
}

::deep .ws-chip-close {
    opacity: 0.55;
    transition: opacity var(--dur-fast) var(--ease-out);
}
.ws-chip:hover ::deep .ws-chip-close,
::deep .ws-chip-close:focus-visible { opacity: 1; }

/* The gliding underline. Width/transform come from dryl.motion.moveIndicator;
   the transition only switches on after the first placement (is-ink-ready). */
.ws-ink {
    position: absolute;
    left: 0;
    bottom: 0;
    height: 2px;
    width: 0;
    border-radius: var(--r-pill);
    background: var(--accent-grad);
    opacity: 0;
    pointer-events: none;
}

.ws-bar.is-ink-ready .ws-ink {
    transition: transform var(--dur-med) var(--ease-spring),
                width var(--dur-med) var(--ease-spring),
                opacity var(--dur-fast) var(--ease-out);
}

/* ---- Body -------------------------------------------------------- */
.ws-body {
    min-width: 0;
    min-height: 0;
}

@media (prefers-reduced-motion: reduce) {
    .ws-chip,
    ::deep .ws-chip-close,
    .ws-bar.is-ink-ready .ws-ink { transition: none; }
}
```

> `::deep` ist nötig, weil `.ws-chip-close` als `Class` an `DrylButton` durchgereicht wird und damit im
> Output einer *anderen* Komponente landet (siehe `feedback_scoped_css_navlink_deep`).

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~DrylCanvasWorkspaceTests`
Expected: PASS (9 Tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/AI/DrylCanvasWorkspace.razor DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceTests.cs
git commit -m "feat(canvas): DrylCanvasWorkspace — gleitende View-Leiste, morphender Wechsel"
```

---

### Task 3: Run-Projektion auf die aktive View

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`
- Modify: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor:125-148` (`HandleChange`)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasRunWorkspaceTests.cs`

**Interfaces:**
- Consumes: `CanvasWorkspace` (Task 1); `DrylCanvasRun` mit `Spec`, `ArtifactEpoch`, `BeginCreate()`, `RevealSnapshot()`, `CompleteReveal()`, `ApplyOp()`, `Purge()`.
- Produces: `public void DrylCanvasRun.UseWorkspace(CanvasWorkspace workspace)` und `internal bool DrylCanvasRun.ConsumeSwapMorphSuppression()`.

- [ ] **Step 1: Test schreiben**

`tests/DRYL.Components.Tests/Agents/Canvas/CanvasRunWorkspaceTests.cs`:

```csharp
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The run projected onto the workspace: one run, the active view's spec.</summary>
public class CanvasRunWorkspaceTests
{
    private static CanvasSpec Spec(string title) => new()
    {
        Title = title,
        Root = new CanvasNode { Id = "root", Type = "stack" },
    };

    [Fact]
    public void A_generation_fills_the_active_view()
    {
        var ws = new CanvasWorkspace();
        var overview = ws.Open("Overview");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        run.BeginCreate();
        run.CompleteReveal(Spec("Overview"));

        Assert.NotNull(overview.Spec);
        Assert.Same(overview.Spec, run.Spec);
    }

    [Fact]
    public void Without_an_active_view_the_run_opens_one()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        run.BeginCreate();
        run.CompleteReveal(Spec("Report"));

        Assert.Single(ws.Views);
        Assert.NotNull(ws.Active!.Spec);
        Assert.Same(ws.Active.Spec, run.Spec);
    }

    [Fact]
    public void Switching_views_switches_the_runs_spec()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        a.Spec = Spec("A");
        b.Spec = Spec("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        ws.Activate(a.Id);
        Assert.Same(a.Spec, run.Spec);

        ws.Activate(b.Id);
        Assert.Same(b.Spec, run.Spec);
    }

    [Fact]
    public void Switching_views_bumps_the_epoch_and_raises_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var epoch = run.ArtifactEpoch;
        var raised = 0;
        run.OnChange += () => raised++;

        ws.Activate(a.Id);

        Assert.Equal(epoch + 1, run.ArtifactEpoch);
        Assert.Equal(1, raised);
        Assert.NotSame(b, ws.Active);
    }

    [Fact]
    public void The_swap_morph_is_suppressed_exactly_once_per_switch()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        ws.Open("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        ws.Activate(a.Id);

        Assert.True(run.ConsumeSwapMorphSuppression());
        Assert.False(run.ConsumeSwapMorphSuppression());
    }

    [Fact]
    public void Without_a_workspace_the_run_keeps_its_own_spec()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.CompleteReveal(Spec("Solo"));

        Assert.NotNull(run.Spec);
        Assert.False(run.ConsumeSwapMorphSuppression());
    }
}
```

`ConsumeSwapMorphSuppression` ist `internal`; das Testprojekt sieht Agents-Internals bereits über
`InternalsVisibleTo` — falls nicht, in Step 3 `DRYL.Components.Agents.csproj` um
`<ItemGroup><InternalsVisibleTo Include="DRYL.Components.Tests" /></ItemGroup>` ergänzen (das Kern-Projekt
macht es genauso).

- [ ] **Step 2: Test laufen lassen — er muss scheitern**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~CanvasRunWorkspaceTests`
Expected: Compile-Fehler `CS1061: 'DrylCanvasRun' does not contain a definition for 'UseWorkspace'`.

- [ ] **Step 3: `DrylCanvasRun` umbauen**

In `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs` die Auto-Property

```csharp
    public CanvasSpec? Spec { get; private set; }
```

ersetzen durch Projektion plus Bindung:

```csharp
    private CanvasSpec? _spec;
    private CanvasWorkspace? _workspace;
    private string? _lastActiveId;
    private bool _suppressSwapMorph;

    /// <summary>The live spec (progressively richer as generations complete), or null before the
    /// first one. Bound to a workspace (see <see cref="UseWorkspace"/>), this is the active view's
    /// spec — a generation always fills the view the user is looking at (A5).</summary>
    public CanvasSpec? Spec
    {
        get => _workspace is null ? _spec : _workspace.Active?.Spec;
        private set
        {
            if (_workspace is null) { _spec = value; return; }
            // A create without a previous open_view still needs somewhere to land.
            var view = _workspace.Active ?? _workspace.Open("Artifact");
            view.Spec = value;
        }
    }

    /// <summary>
    /// Binds the run to a workspace: from here on <see cref="Spec"/> reads and writes the active
    /// view's spec, and switching views resets the interactive form state (a fresh artifact must
    /// not inherit the previous one's field values).
    /// </summary>
    public void UseWorkspace(CanvasWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (ReferenceEquals(_workspace, workspace)) return;

        if (_workspace is not null) _workspace.OnChange -= HandleWorkspaceChanged;
        _workspace = workspace;
        _lastActiveId = workspace.ActiveId;
        _workspace.OnChange += HandleWorkspaceChanged;
    }

    private void HandleWorkspaceChanged()
    {
        if (_lastActiveId == _workspace!.ActiveId) return;
        _lastActiveId = _workspace.ActiveId;

        _artifactEpoch++;           // the other view's artifact owns its own form values
        _suppressSwapMorph = true;  // DrylCanvasWorkspace already morphs this switch
        Raise();
    }

    /// <summary>Reads and clears the "this spec swap was a view switch" flag — the workspace has
    /// already run that morph, and two nested view transitions lose one of the mutations.</summary>
    internal bool ConsumeSwapMorphSuppression()
    {
        var suppress = _suppressSwapMorph;
        _suppressSwapMorph = false;
        return suppress;
    }
```

Danach die Stellen anpassen, die `Spec` direkt setzen — sie funktionieren unverändert, weil der
private Setter jetzt projiziert: `ApplySnapshot`, `RevealSnapshot` (`Spec = new CanvasSpec();`),
`CompleteReveal`, `CompleteGeneration(CanvasSpec final)`. In `RevealSnapshot`/`CompleteReveal` steht
`CanvasStreamReveal.Reveal(Spec!, …)` — das bleibt so, `Spec` liest jetzt aus der View.

`using DRYL.Components.Canvas;` steht bereits in Zeile 1.

- [ ] **Step 4: `DrylAiCanvas` die Unterdrückung abfragen lassen**

In `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor`, `HandleChange`, die `swap`-Zeile ersetzen:

```csharp
        // A brand-new CanvasSpec instance means the artifact was replaced wholesale — morph the
        // old tree into the new one. Except when the swap came from a view switch: the workspace
        // owns that morph, and nesting two view transitions loses one of the mutations.
        var switched = Run?.ConsumeSwapMorphSuppression() == true;
        var swap = !switched && _renderedSpec is not null && !ReferenceEquals(_renderedSpec, Run?.Spec);
```

- [ ] **Step 5: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasRunWorkspaceTests|FullyQualifiedName~DrylArtifactRunTests"`
Expected: PASS — die bestehenden Run-Tests dürfen sich nicht verändern.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasRun.cs DRYL.Components.Agents/Canvas/DrylAiCanvas.razor tests/DRYL.Components.Tests/Agents/Canvas/CanvasRunWorkspaceTests.cs
git commit -m "feat(canvas): der Run schreibt in die aktive View des Workspace"
```

---

### Task 4: `open_view` — die AI eröffnet eine View

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasOpenViewToolTests.cs`

**Interfaces:**
- Consumes: `CanvasWorkspace` (Task 1), `DrylCanvasRun.UseWorkspace` (Task 3), `DrylCanvasTools.CreateReplay(run, generate, data, actions)`.
- Produces: `DrylCanvasTools.Create(DrylCanvasRun run, AIAgent generator, ICanvasDataService? data = null, ICanvasActionService? actions = null, CanvasWorkspace? workspace = null)` und dieselbe zusätzliche Signaturposition an `CreateReplay`; `public AITool? OpenView { get; }`; `All` enthält `open_view` genau dann, wenn ein Workspace übergeben wurde.

- [ ] **Step 1: Test schreiben**

`tests/DRYL.Components.Tests/Agents/Canvas/CanvasOpenViewToolTests.cs`:

```csharp
using System.Runtime.CompilerServices;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The model may open a named view — and only ever the one it then builds into.</summary>
public class CanvasOpenViewToolTests
{
    private const string ArtifactJson =
        """{"title":"Order 4711","root":{"id":"root","type":"stack","children":[
            {"id":"t1","type":"text","props":{"text":"open"}}]}}""";

    private static async IAsyncEnumerable<string> Emit(
        string json, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        yield return json;
    }

    private static async Task<string> InvokeAsync(AITool tool, Dictionary<string, object?> args)
    {
        var result = await ((AIFunction)tool).InvokeAsync(new AIFunctionArguments(args));
        return result?.ToString() ?? string.Empty;
    }

    [Fact]
    public void Without_a_workspace_there_is_no_open_view_tool()
    {
        var tools = DrylCanvasTools.CreateReplay(new DrylCanvasRun(), (_, ct) => Emit(ArtifactJson, ct));

        Assert.Equal(2, tools.All.Count);
        Assert.Null(tools.OpenView);
    }

    [Fact]
    public void With_a_workspace_the_tool_set_grows_by_one()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        var tools = DrylCanvasTools.CreateReplay(run, (_, ct) => Emit(ArtifactJson, ct), workspace: ws);

        Assert.Equal(3, tools.All.Count);
        Assert.NotNull(tools.OpenView);
        Assert.Equal("open_view", ((AIFunction)tools.OpenView!).Name);
    }

    [Fact]
    public async Task Open_view_creates_activates_and_fills_the_view()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var tools = DrylCanvasTools.CreateReplay(run, (_, ct) => Emit(ArtifactJson, ct), workspace: ws);

        var receipt = await InvokeAsync(tools.OpenView!, new()
        {
            ["name"] = "Order 4711",
            ["brief"] = "Show order 4711 with its two positions.",
        });

        Assert.Single(ws.Views);
        Assert.Equal("Order 4711", ws.Active!.Title);
        Assert.NotNull(ws.Active.Spec);
        Assert.Contains("Order 4711", receipt);
        Assert.Same(ws.Active.Spec, run.Spec);
    }

    [Fact]
    public async Task Re_opening_a_view_builds_into_the_same_one()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var tools = DrylCanvasTools.CreateReplay(run, (_, ct) => Emit(ArtifactJson, ct), workspace: ws);

        await InvokeAsync(tools.OpenView!, new() { ["name"] = "Overview", ["brief"] = "a" });
        await InvokeAsync(tools.OpenView!, new() { ["name"] = "Order 4711", ["brief"] = "b" });
        await InvokeAsync(tools.OpenView!, new() { ["name"] = "Overview", ["brief"] = "c" });

        Assert.Equal(2, ws.Views.Count);
        Assert.Equal("overview", ws.ActiveId);
    }
}
```

- [ ] **Step 2: Test laufen lassen — er muss scheitern**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~CanvasOpenViewToolTests`
Expected: Compile-Fehler `CS1061: 'DrylCanvasTools' does not contain a definition for 'OpenView'`.

- [ ] **Step 3: Tools erweitern**

In `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`:

1. Feld und Konstruktor-Parameter ergänzen (`_workspace`), im Konstruktor nach den beiden bestehenden
   `AIFunctionFactory.Create`-Aufrufen:

```csharp
        All = new List<AITool> { CreateArtifact, UpdateArtifact };

        if (_workspace is not null)
        {
            OpenView = AIFunctionFactory.Create(OpenViewImpl, "open_view",
                "Open a named view on the workspace and build its artifact there. Use this when the " +
                "user turns to a new subject (a specific order, a second report) and the current " +
                "artifact should stay reachable — the view bar keeps both, and the user can switch " +
                "back. Re-using a name activates that view and rebuilds it. Put ALL concrete data " +
                "and numbers into the brief; the generator sees only this brief.");
            All.Add(OpenView);
        }
```

   `All` dafür als `List<AITool>` deklarieren (`public IList<AITool> All { get; }` bleibt).

2. Die Beschreibung von `create_artifact` um einen abgrenzenden Satz ergänzen:
   `"… Use this once per distinct artifact; it always replaces the artifact in the view the user is currently looking at — call open_view instead when the previous artifact should stay reachable."`

3. Property und Implementierung:

```csharp
    /// <summary>Open-view tool (<c>open_view</c>): activates (or creates) a named workspace view and
    /// runs a create generation into it. Null when the tools were built without a workspace.</summary>
    public AITool? OpenView { get; }

    private Task<string> OpenViewImpl(
        [Description("Short name of the view, e.g. \"Order 4711\". Re-using a name re-opens that view.")] string name,
        [Description("What the artifact should show, incl. all concrete data/numbers it needs.")] string brief,
        [Description("Short artifact title; defaults to the view name.")] string? title = null,
        CancellationToken ct = default)
    {
        var view = _workspace!.Open(string.IsNullOrWhiteSpace(name) ? "View" : name.Trim());
        return CreateArtifactImpl(brief, title ?? view.Title, ct, view.Title);
    }
```

4. `CreateArtifactImpl` bekommt einen optionalen letzten Parameter, damit der Receipt die View nennen
   kann — er steht **nach** `ct` und ist damit kein Teil des Tool-Schemas:

```csharp
    private async Task<string> CreateArtifactImpl(
        [Description("What the artifact should show, incl. all concrete data/numbers it needs.")] string brief,
        [Description("Short artifact title.")] string? title = null,
        CancellationToken ct = default,
        string? viewName = null)
    {
        …
            var where = viewName is null ? "" : $" in view \"{viewName}\"";
            var receipt = $"Artifact created{where}: {nodes} elements, {interactive} inputs." + recovery;
        …
    }
```

> Achtung: `AIFunctionFactory` liest die Parameter der übergebenen Methode. `CreateArtifactImpl` wird
> weiterhin direkt registriert — ein optionaler Parameter *nach* dem `CancellationToken` wird von der
> Factory nicht als Tool-Argument angeboten. Die Signatur von `create_artifact` bleibt damit
> unverändert; Test 2 in `CanvasOpenViewToolTests` und die bestehenden Replay-Tests decken das ab.
> Sollte die Factory den Parameter doch aufnehmen (sichtbar als drittes Argument im Schema), wird
> stattdessen ein privates `CreateAsync(brief, title, viewName, ct)` extrahiert und
> `CreateArtifactImpl` zur dünnen Hülle darüber.

5. Beide Factory-Methoden bekommen den Parameter:

```csharp
    public static DrylCanvasTools Create(DrylCanvasRun run, AIAgent generator,
                                        ICanvasDataService? data = null,
                                        ICanvasActionService? actions = null,
                                        CanvasWorkspace? workspace = null) =>
        new(run, LiveGenerate(generator), data, actions, workspace);

    public static DrylCanvasTools CreateReplay(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate,
        ICanvasDataService? data = null, ICanvasActionService? actions = null,
        CanvasWorkspace? workspace = null) =>
        new(run, generate, data, actions, workspace);
```

- [ ] **Step 4: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasOpenViewToolTests|FullyQualifiedName~DrylArtifactRunTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasTools.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasOpenViewToolTests.cs
git commit -m "feat(canvas): open_view — die AI eroeffnet eine benannte View und baut hinein"
```

---

### Task 5: `DrylCanvasDock` — die Befehlsleiste

**Files:**
- Create: `DRYL.Components.Agents/Canvas/DockCorner.cs`
- Create: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor`
- Create: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs`

**Interfaces:**
- Consumes: `DrylCanvasRun` (Task 3), `DrylChatComposer`, `DrylAiIndicator`, `DrylPresence`, `DrylTooltip`, `DrylButton`, `DrylIcon`; JS `dryl.topLayer.show/hide`, `dryl.chat.scrollToEnd`.
- Produces: `enum DRYL.Components.Agents.DockCorner { BottomRight, BottomLeft, TopRight, TopLeft }` und Komponente `DRYL.Components.Agents.DrylCanvasDock` mit `Run`, `Busy`, `OnSend`, `Corner`, `Placeholder`, `Status`, `Log`, `Collapsed`/`CollapsedChanged`, `Title`, `Class`, `AdditionalAttributes`.

- [ ] **Step 1: Test schreiben**

`tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs`:

```csharp
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The dock is a command bar, not a chat: one input, one status line, the log on demand.</summary>
public class DrylCanvasDockTests : BunitContext
{
    public DrylCanvasDockTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static DrylCanvasRun BuildingRun()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(new CanvasSpec
        {
            Title = "Report",
            Root = new CanvasNode
            {
                Id = "root",
                Type = "stack",
                Children = new List<CanvasNode>
                {
                    new() { Id = "a", Type = "text", Props = new() { ["text"] = "x" } },
                    new() { Id = "b", Type = "text", Props = new() { ["text"] = "y" } },
                },
            },
        });
        return run;
    }

    [Fact]
    public void The_status_line_reports_the_run()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Contains("Building", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_failed_run_puts_its_message_in_the_status_line()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, run));

        Assert.Contains("generator gave up", cut.Find(".dock-status").TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").GetAttribute("class"));
    }

    [Fact]
    public void An_explicit_status_wins()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, BuildingRun())
            .Add(x => x.Status, "Waiting for approval"));

        Assert.Equal("Waiting for approval", cut.Find(".dock-status").TextContent.Trim());
    }

    [Fact]
    public void Sending_raises_OnSend_with_the_draft()
    {
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.OnSend, EventCallback.Factory.Create<string>(this, s => sent = s)));

        var composer = cut.FindComponent<DrylChatComposer>();
        composer.Find("textarea").Input("open the order view");
        composer.Find("button").Click();

        Assert.Equal("open the order view", sent);
    }

    [Fact]
    public void Busy_disables_the_composer()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Busy, true));

        Assert.True(cut.FindComponent<DrylChatComposer>().Instance.Disabled);
    }

    [Fact]
    public void Collapsed_leaves_a_single_labelled_button()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.Title, "Assistant"));

        Assert.Empty(cut.FindAll(".dock-panel"));
        Assert.Equal("Assistant", cut.Find(".dock-fab button").GetAttribute("aria-label"));

        cut.Find(".dock-fab button").Click();
        Assert.Single(cut.FindAll(".dock-panel"));
    }

    [Fact]
    public void Without_a_log_there_is_no_disclosure_button()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Empty(cut.FindAll(".dock-log-toggle"));
        Assert.Empty(cut.FindAll(".dock-log"));
    }

    [Fact]
    public void The_log_slot_renders_and_toggles()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Log, (RenderFragment)(b => b.AddMarkupContent(0, "<p>turn one</p>"))));

        Assert.Contains("turn one", cut.Markup);
        Assert.Equal("true", cut.Find(".dock-log").GetAttribute("aria-hidden"));

        cut.Find(".dock-log-toggle").Click();
        Assert.Null(cut.Find(".dock-log").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void The_corner_becomes_a_class()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Corner, DockCorner.TopLeft));

        Assert.Contains("canvas-dock--tl", cut.Find(".canvas-dock").GetAttribute("class"));
    }
}
```

- [ ] **Step 2: Test laufen lassen — er muss scheitern**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~DrylCanvasDockTests`
Expected: Compile-Fehler `CS0246: … 'DrylCanvasDock' could not be found`.

- [ ] **Step 3: Enum anlegen**

`DRYL.Components.Agents/Canvas/DockCorner.cs`:

```csharp
namespace DRYL.Components.Agents;

/// <summary>Which corner of the viewport <c>DrylCanvasDock</c> floats in.</summary>
public enum DockCorner
{
    /// <summary>Bottom right — the default reading position for a command bar.</summary>
    BottomRight,

    /// <summary>Bottom left.</summary>
    BottomLeft,

    /// <summary>Top right.</summary>
    TopRight,

    /// <summary>Top left.</summary>
    TopLeft,
}
```

- [ ] **Step 4: Komponente schreiben**

`DRYL.Components.Agents/Canvas/DrylCanvasDock.razor`:

```razor
@namespace DRYL.Components.Agents
@using DRYL.Components.Ai
@using DRYL.Components.Canvas
@inject IJSRuntime JS
@implements IAsyncDisposable

@*  ─────────────────────────────────────────────────────────
    DrylCanvasDock — the prompt dock: a command bar, not a chat (A6).

    A floating card in a corner of the viewport: one input, one line of live
    status ("Building · 7 elements") and the full transcript only on demand.
    The artifact on the canvas is the answer — the text next to it is not, so
    it does not get to own half the screen.

    The dock lives in the browser's top layer (popover="manual"), because a
    position: fixed element is measured against the nearest ancestor with a
    transform or backdrop-filter — in a real app almost always some glass card.
    Without JS (prerender) it stays a plain fixed element in flow.

    Usage:
      <DrylCanvasDock Run="_run" Busy="Busy" OnSend="Send">
          <Log>@foreach (var t in _turns) { <DrylMessage …>@t.Text</DrylMessage> }</Log>
      </DrylCanvasDock>
    ───────────────────────────────────────────────────────── *@

<div class="@RootCssClass" popover="@PopoverMode" @ref="_el" @attributes="AdditionalAttributes">
    <DrylPresence Visible="@Collapsed" Transition="PresenceTransition.Scale" Speed="PresenceSpeed.Fast">
        <div class="dock-fab">
            <DrylTooltip Text="@Title">
                <DrylButton Variant="DrylButton.ButtonVariant.Primary"
                            AriaLabel="@Title"
                            Ai="@DockAi"
                            OnClick="@(() => SetCollapsedAsync(false))">
                    <DrylIcon Name="Sparkle" Size="16" />
                </DrylButton>
            </DrylTooltip>
        </div>
    </DrylPresence>

    <DrylPresence Visible="@(!Collapsed)" Transition="PresenceTransition.Scale" Speed="PresenceSpeed.Fast">
        <div class="dock-panel glass-card">
            <div class="dock-head">
                <DrylAiIndicator State="@DockAi" />

                @*  One line, and it moves: re-keying the presence on the text makes the old
                    line fade out and the new one in — a status change is a change, not a jump. *@
                <div class="dock-status @(HasError ? "is-error" : null)" aria-live="polite">
                    <DrylPresence @key="StatusText" Visible Appear
                                  Transition="PresenceTransition.Fade" Speed="PresenceSpeed.Fast">
                        <span>@StatusText</span>
                    </DrylPresence>
                </div>

                @if (Log is not null)
                {
                    <DrylTooltip Text="@LogLabel">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="@LogLabel"
                                    Pressed="_logOpen"
                                    Class="dock-log-toggle"
                                    OnClick="ToggleLog">
                            <DrylIcon Name="@(_logOpen ? "ChevronDown" : "ChevronUp")" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                }

                <DrylTooltip Text="Collapse assistant">
                    <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                Size="DrylButton.ButtonSize.Small"
                                AriaLabel="Collapse assistant"
                                Class="dock-collapse"
                                OnClick="@(() => SetCollapsedAsync(true))">
                        <DrylIcon Name="Minimize" Size="14" />
                    </DrylButton>
                </DrylTooltip>
            </div>

            @if (Log is not null)
            {
                @*  0fr → 1fr disclosure (the DrylToolCallGroup trick): the log animates open
                    without ever squeezing its content and stays in the DOM while closed. *@
                <div class="dock-log @(_logOpen ? "is-open" : null)"
                     role="log"
                     aria-hidden="@(_logOpen ? null : "true")">
                    <div class="dock-log-inner">
                        <div class="dock-log-content" @ref="_logEl">@Log</div>
                    </div>
                </div>
            }

            <DrylChatComposer @bind-Value="_draft"
                              OnSend="SendAsync"
                              Disabled="@Busy"
                              Placeholder="@Placeholder"
                              AriaLabel="@Title"
                              Ai="@DockAi" />
        </div>
    </DrylPresence>
</div>

@code {
    /// <summary>The canvas run the status line reads. Optional — without it the dock is just an input.</summary>
    [Parameter] public DrylCanvasRun? Run { get; set; }

    /// <summary>The host is busy with a turn: the composer locks and the dock breathes.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Raised with the submitted text.</summary>
    [Parameter] public EventCallback<string> OnSend { get; set; }

    /// <summary>Which corner the dock floats in. Default <see cref="DockCorner.BottomRight"/>.</summary>
    [Parameter] public DockCorner Corner { get; set; } = DockCorner.BottomRight;

    /// <summary>Composer placeholder.</summary>
    [Parameter] public string? Placeholder { get; set; } = "Ask for a view…";

    /// <summary>Overrides the status line derived from <see cref="Run"/>.</summary>
    [Parameter] public string? Status { get; set; }

    /// <summary>The transcript, revealed on demand. Without it the dock offers no disclosure.</summary>
    [Parameter] public RenderFragment? Log { get; set; }

    /// <summary>Whether the dock is collapsed to a single button. Supports two-way binding.</summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>Fires when the dock collapses or expands.</summary>
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }

    /// <summary>Name of the assistant — the collapsed button's label and the composer's aria-label.</summary>
    [Parameter] public string Title { get; set; } = "Assistant";

    /// <summary>Extra CSS class(es) merged onto the dock root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Pass-through HTML attributes on the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    private DrylCanvasRun? _subscribed;
    private ElementReference _el;
    private ElementReference _logEl;
    private string? _draft;
    private bool _logOpen;
    private bool _topLayer;     // the popover attribute is in the DOM
    private bool _shown;        // …and the element is actually promoted
    private bool _scrollLog;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Run)) return;
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
        _subscribed = Run;
        if (_subscribed is not null) _subscribed.OnChange += HandleChange;
    }

    private void HandleChange() => InvokeAsync(StateHasChanged);

    private async Task SendAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _scrollLog = _logOpen;
        await OnSend.InvokeAsync(text);
    }

    private async Task SetCollapsedAsync(bool collapsed)
    {
        if (Collapsed == collapsed) return;
        Collapsed = collapsed;
        await CollapsedChanged.InvokeAsync(collapsed);
    }

    private void ToggleLog()
    {
        _logOpen = !_logOpen;
        _scrollLog = _logOpen;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Two-step promotion: the popover attribute has to be in the DOM before showPopover
        // may be called, so the first render renders without it and asks for a re-render.
        if (firstRender)
        {
            _topLayer = true;
            StateHasChanged();
            return;
        }

        if (_topLayer && !_shown)
        {
            try
            {
                await JS.InvokeVoidAsync("dryl.topLayer.show", _el);
                _shown = true;
            }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (InvalidOperationException) { /* prerender — no JS */ }
        }

        if (_scrollLog)
        {
            _scrollLog = false;
            try { await JS.InvokeVoidAsync("dryl.chat.scrollToEnd", _logEl); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (InvalidOperationException) { /* prerender — no JS */ }
        }
    }

    private bool HasError => Status is null && Run?.Error is not null;

    // The AI vocabulary of the whole dock: the run's own state, or Thinking while the host
    // is mid-turn — the dock must breathe before the first artifact node exists.
    private AiState DockAi =>
        Run?.State is AiState.Streaming or AiState.Thinking ? Run.State
        : Busy ? AiState.Thinking
        : Run?.State ?? AiState.None;

    private string StatusText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Status)) return Status!;
            if (Run?.Error is { } error) return error.Message;

            var n = Run?.NodeCount ?? 0;
            var elements = FormattableString.Invariant($"{n} element{(n == 1 ? "" : "s")}");
            return (Run?.State ?? AiState.None) switch
            {
                AiState.Streaming => "Building · " + elements,
                AiState.Thinking => "Working…",
                AiState.Generated => "Ready · " + elements,
                _ => Busy ? "Working…" : "Idle",
            };
        }
    }

    private string LogLabel => _logOpen ? "Hide conversation" : "Show conversation";

    // "manual": the dock owns its own collapse; a light-dismissing popover would vanish
    // behind the component's back on the first outside click.
    private string? PopoverMode => _topLayer ? "manual" : null;

    private string CornerCss => Corner switch
    {
        DockCorner.BottomLeft => "bl",
        DockCorner.TopRight => "tr",
        DockCorner.TopLeft => "tl",
        _ => "br",
    };

    private string RootCssClass
    {
        get
        {
            var parts = new List<string> { "canvas-dock", "canvas-dock--" + CornerCss };
            if (Collapsed) parts.Add("is-collapsed");
            if (!string.IsNullOrWhiteSpace(Class)) parts.Add(Class!);
            return string.Join(' ', parts);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;

        if (_shown)
        {
            try { await JS.InvokeVoidAsync("dryl.topLayer.hide", _el); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (JSException) { /* element already gone */ }
            catch (InvalidOperationException) { /* prerender — no JS */ }
        }
    }
}
```

- [ ] **Step 5: CSS schreiben**

`DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css`:

```css
/* DrylCanvasDock — a floating command bar. Tokens only.
   The popover default styling (inset: 0; margin: auto) is overridden explicitly:
   the dock is a corner card, not a centred sheet. */

.canvas-dock {
    position: fixed;
    inset: auto;
    margin: 0;
    padding: 0;
    border: none;
    background: none;
    overflow: visible;
    z-index: 60;
    width: min(420px, calc(100vw - var(--sp-6)));
}

.canvas-dock.is-collapsed { width: auto; }

.canvas-dock--br { right: var(--sp-4); bottom: var(--sp-4); }
.canvas-dock--bl { left:  var(--sp-4); bottom: var(--sp-4); }
.canvas-dock--tr { right: var(--sp-4); top:    var(--sp-4); }
.canvas-dock--tl { left:  var(--sp-4); top:    var(--sp-4); }

/* Collapsed: just the button, nothing else to see. */
.dock-fab { display: inline-flex; }

.dock-panel {
    display: flex;
    flex-direction: column;
    gap: var(--sp-2);
    padding: var(--sp-2);
    border-radius: var(--r-lg);
    box-shadow: var(--shadow-2);
}

.dock-head {
    display: flex;
    align-items: center;
    gap: var(--sp-2);
    min-width: 0;
}

/* A6: exactly one line of status — it truncates rather than growing the dock. */
.dock-status {
    flex: 1 1 auto;
    min-width: 0;
    font-size: 13px;
    color: var(--fg-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.dock-status.is-error { color: var(--danger); }

/* Disclosure: animate the track, never squeeze the content. */
.dock-log {
    display: grid;
    grid-template-rows: 0fr;
    transition: grid-template-rows var(--dur-med) var(--ease-in-out);
}

.dock-log.is-open { grid-template-rows: 1fr; }

.dock-log-inner { overflow: hidden; }

.dock-log-content {
    display: flex;
    flex-direction: column;
    gap: var(--sp-2);
    max-height: 42vh;
    overflow-y: auto;
    padding-right: var(--sp-1);
}

/* On a phone the dock is the bottom bar — a 420px card in the corner is not a target. */
@media (max-width: 640px) {
    .canvas-dock {
        left: var(--sp-3);
        right: var(--sp-3);
        bottom: var(--sp-3);
        top: auto;
        width: auto;
    }
    .canvas-dock.is-collapsed { left: auto; }
    .dock-log-content { max-height: 50vh; }
}

@media (prefers-reduced-motion: reduce) {
    .dock-log { transition: none; }
}
```

- [ ] **Step 6: Tests laufen lassen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter FullyQualifiedName~DrylCanvasDockTests`
Expected: PASS (9 Tests).

- [ ] **Step 7: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DockCorner.cs DRYL.Components.Agents/Canvas/DrylCanvasDock.razor DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs
git commit -m "feat(canvas): DrylCanvasDock — Befehlsleiste im Top-Layer statt Chat-Spalte"
```

---

### Task 6: Demo, Katalog, Changelog, Versionen

**Files:**
- Create: `DRYL.Website/Components/Examples/Agents/CanvasWorkspaceDemo.razor`
- Create: `DRYL.Website/Components/Examples/Agents/OpenAiCanvasWorkspace.razor`
- Create: `DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor`
- Modify: `DRYL.Website/Components/ComponentCatalog.cs`
- Modify: `CHANGELOG.md`
- Modify: `DRYL.Components/DRYL.Components.csproj:8`, `DRYL.Components.Agents/DRYL.Components.Agents.csproj:8`

**Interfaces:**
- Consumes: alles aus Task 1–5.
- Produces: Route `/components/canvas-workspace`; Katalogeinträge `DrylCanvasWorkspace` und `DrylCanvasDock`.

- [ ] **Step 1: Replay-Beispiel schreiben**

`DRYL.Website/Components/Examples/Agents/CanvasWorkspaceDemo.razor` — ein Skript ohne Modell: drei
Knopfdrücke über `DrylCanvasTools.CreateReplay`, jeder liefert festes JSON. Aufbau analog zum
bestehenden `CanvasArtifacts.razor`:

```razor
@using DRYL.Components.Canvas
@implements IDisposable

<div class="ws-demo">
    <DrylCanvasWorkspace Workspace="_ws">
        <View>
            <DrylAiCanvas Run="_run" AllowExpand="false" />
        </View>
    </DrylCanvasWorkspace>

    <DrylCanvasDock Run="_run" Busy="_busy" OnSend="Send" Title="Builder"
                    Placeholder="Try: overview · order 4711 · back">
        <Log>
            @foreach (var line in _log)
            {
                <DrylMessage @key="line" Role="MessageRole.User" Author="You">@line</DrylMessage>
            }
        </Log>
    </DrylCanvasDock>
</div>

@code {
    private readonly CanvasWorkspace _ws = new();
    private readonly DrylCanvasRun _run = new();
    private readonly List<string> _log = new();
    private DrylCanvasTools? _tools;
    private bool _busy;

    protected override void OnInitialized()
    {
        _run.UseWorkspace(_ws);
        _run.OnChange += Changed;
        _ws.OnChange += Changed;
        _tools = DrylCanvasTools.CreateReplay(_run, Generate, workspace: _ws);
    }

    private void Changed() => InvokeAsync(StateHasChanged);

    // The scripted "generator": the brief decides which canned artifact streams back.
    private async IAsyncEnumerable<string> Generate(
        string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var json = prompt.Contains("4711", StringComparison.OrdinalIgnoreCase) ? OrderJson : OverviewJson;
        foreach (var chunk in Chunks(json))
        {
            await Task.Delay(24, ct);
            yield return chunk;
        }
    }

    private static IEnumerable<string> Chunks(string json)
    {
        const int size = 48;
        for (var i = 0; i < json.Length; i += size)
            yield return json.Substring(i, Math.Min(size, json.Length - i));
    }

    private async Task Send(string text)
    {
        _log.Add(text);
        _busy = true;
        try
        {
            var wantsOrder = text.Contains("4711", StringComparison.OrdinalIgnoreCase);
            var name = wantsOrder ? "Order 4711" : "Overview";
            await ((Microsoft.Extensions.AI.AIFunction)_tools!.OpenView!).InvokeAsync(
                new Microsoft.Extensions.AI.AIFunctionArguments(new Dictionary<string, object?>
                {
                    ["name"] = name,
                    ["brief"] = text,
                }));
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose()
    {
        _run.OnChange -= Changed;
        _ws.OnChange -= Changed;
    }

    private const string OverviewJson = """
        {"title":"Overview","root":{"id":"root","type":"stack","props":{"gap":"md"},"children":[
          {"id":"k","type":"stat","props":{"label":"Open orders","value":"38"}},
          {"id":"c","type":"lineChart","props":{"title":"Revenue","labels":["Apr","May","Jun"],
            "series":[{"name":"2026","points":[41,47,52]}]}}]}}
        """;

    private const string OrderJson = """
        {"title":"Order 4711","root":{"id":"root","type":"stack","props":{"gap":"md"},"children":[
          {"id":"h","type":"text","props":{"text":"Order 4711 — Contoso GmbH","variant":"title"}},
          {"id":"t","type":"table","props":{"columns":["Position","Qty","Sum"],
            "rows":[["Sensor A","12","1 440 €"],["Cable set","3","210 €"]]}}]}}
        """;
}
```

Die Wrapper-CSS-Klasse `.ws-demo` bekommt eine `min-height: 420px` und `position: relative` in der
bestehenden Beispiel-CSS-Datei der Website — der Dock schwebt im Top-Layer, das Beispiel braucht nur
Höhe, damit die Fläche nicht kollabiert.

- [ ] **Step 2: Live-Beispiel schreiben**

`DRYL.Website/Components/Examples/Agents/OpenAiCanvasWorkspace.razor`: Kopie von
`OpenAiCanvasArtifacts.razor` mit drei Unterschieden:
1. `private readonly CanvasWorkspace _ws = new();` und `_run.UseWorkspace(_ws);` in `OnInitialized`.
2. `DrylCanvasTools.Create(_run, generatorAgent, workspace: _ws)`.
3. Markup: `DrylCanvasWorkspace` + `DrylCanvasDock` statt `DrylChat`-Spalte + `DrylAiCanvas`; die
   bestehende Turn-Liste wandert unverändert in den `<Log>`-Slot.
   Die Instructions-Konstante bekommt einen Satz: `"Call open_view when the user turns to a new subject so the previous artifact stays reachable; call create_artifact only to replace what is currently shown."`

- [ ] **Step 3: Demo-Seite schreiben**

`DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor` nach dem Muster von `DemoAiCanvas.razor`:
`@page "/components/canvas-workspace"`, `ComponentDocHeader Slug="canvas-workspace"`, ein
Erklärabschnitt („Views statt einer Leinwand", „das Dock ist eine Befehlsleiste, kein Chat"), zwei
`DrylCodeBlock` (Registrierung + Markup) und die beiden `DemoExample`-Blöcke — der Live-Block hinter
`@if (OpenAi.IsConfigured)` samt `DrylAlert Kind="AlertKind.Ai"` wie auf `DemoAiCanvas`.

- [ ] **Step 4: Katalog ergänzen**

In `DRYL.Website/Components/ComponentCatalog.cs` zwei Einträge im Stil der bestehenden anlegen
(gleiche Kategorie wie `DrylAiCanvas`), beide mit `Slug = "canvas-workspace"` bzw. dem in der Datei
üblichen Routen-Feld auf `/components/canvas-workspace`:
- `DrylCanvasWorkspace` — „Named canvas views, exactly one visible; switching morphs."
- `DrylCanvasDock` — „Floating prompt dock: one input, one line of live status, the transcript on demand."

- [ ] **Step 5: Bauen und alle Tests laufen lassen**

Run: `dotnet build DRYL.sln` und `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: Build ohne Warnungen-als-Fehler, alle Tests grün.

- [ ] **Step 6: Versionen und Changelog**

`DRYL.Components/DRYL.Components.csproj:8` → `<Version>2.14.0</Version>`
`DRYL.Components.Agents/DRYL.Components.Agents.csproj:8` → `<Version>0.12.0</Version>`

In `CHANGELOG.md` den `[Unreleased]`-Block zu `## [2.14.0] - 2026-07-25` umbenennen, einen frischen
leeren `[Unreleased]` darüber setzen und darunter eintragen:

```markdown
### Added
- `DrylCanvasWorkspace` — Named canvas views with a gliding view bar; exactly one view is
  visible and switching morphs through the shared view-transition layer
- `CanvasWorkspace` / `CanvasView` — Observable workspace state (open, activate, close) a host
  or an AI run writes into
- `DrylCanvasDock` — Floating prompt dock in the browser's top layer: one input, one line of
  live status and the transcript on demand; corners via `DockCorner`
- `DrylCanvasRun` — New `UseWorkspace(...)`: the run reads and writes the active view's spec
- `DrylCanvasTools` — New optional `workspace` argument adds the `open_view` tool, letting the
  model open a named view and build into it while the previous artifact stays reachable
```

- [ ] **Step 7: Manuelle Prüfung**

`dotnet run --project DRYL.Website` und `/components/canvas-workspace` öffnen:
- Beide Farbmodi (`data-dryl-mode` am `<html>` umschalten), 375 px, `prefers-reduced-motion: reduce`.
- Der Viewwechsel morpht **einmal** — kein Doppel-Morph, kein Sprung (A8).
- Das Dock hängt am Viewport, nicht an der Beispiel-Karte (die Karten der Seite haben `backdrop-filter`).
- Tooltips der Dock-Knöpfe sind sichtbar; falls sie im Top-Layer verschwinden, als eigenen Befund an
  `DrylTooltip` notieren, nicht im Dock umgehen.
- Tastaturweg Composer → Aufklappen → Chips → Canvas ohne Falle.

- [ ] **Step 8: Commit**

```bash
git add DRYL.Website CHANGELOG.md DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj
git commit -m "feat(canvas): Workspace-Demo, Katalog, 2.14.0 / 0.12.0"
```

---

## Nach dem Plan

Die Projektnotiz `project_canvas_platform` fortschreiben (Phase 3 umgesetzt, Kern 2.14.0 /
Agents 0.12.0, offene Phasen 4–6) — DoD 8 der Roadmap.
