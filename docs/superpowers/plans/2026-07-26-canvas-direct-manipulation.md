# Canvas Phase 6 — Direct Manipulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Der Nutzer kann ein Canvas-Element auswählen, dazu prompten, es anheften, duplizieren, entfernen und innerhalb seines Containers umsortieren.

**Architecture:** Ein geteiltes, beobachtbares `CanvasSelection`-Objekt verbindet Renderer (Kern) und Prompt-Dock (Agents), ohne dass die beiden Komponenten einander kennen. `DrylCanvas` besitzt den Spec und ist damit die einzige Stelle, die Navigation und Kommandos gegen den Baum auflöst; `CanvasNodeView` liefert nur Trefferfläche, Fokus und Werkzeugleiste. Jede Strukturänderung wird zu genau einer `CanvasOp` und läuft durch den bestehenden `CanvasPatcher`, damit Presence-, FLIP- und Pulse-Schicht sie ohne Sonderfall animieren.

**Tech Stack:** .NET 10 / Blazor (Server + WASM), xUnit + bUnit, reines JS-Modul ohne Abhängigkeiten (`dryl-canvas.js`), CSS-Isolation mit den bestehenden `dryl.css`-Tokens.

**Spec:** `docs/superpowers/specs/2026-07-26-canvas-direct-manipulation-design.md` — bei jeder Unklarheit gilt die Spec.

## Global Constraints

- **Tokens statt Literale.** Jede Farbe, jeder Abstand, jeder Radius, jede Dauer referenziert eine CSS-Variable aus `dryl.css`. **Keine neuen Tokens in dieser Phase** — `node scripts/check-light-sync.mjs` muss trivial grün bleiben.
- **Motion-Vokabular fest:** nur `--dur-fast|med|slow` und `--ease-out|in-out|spring|viscous`. Kein `linear`, keine neuen Keyframes außer den in der Spec genannten.
- **Jedes Icon-only-Bedienelement** bekommt `DrylTooltip` **und** `AriaLabel` mit demselben Text (CLAUDE.md §2.11).
- **Keine neuen Icons:** verwendet werden ausschließlich `Sparkle`, `Lock`, `Copy`, `Trash`, `GripVertical` — alle existieren in `DrylIcon.Icons`.
- **Keine npm-/JS-Abhängigkeit.** JS ausschließlich in `DRYL.Components/wwwroot/js/dryl-canvas.js`.
- **Rückwärtskompatibel:** Ohne gesetzten `Selection`-Parameter darf sich am gerenderten Markup eines Canvas **kein Attribut** ändern. Jede neue API ist additiv.
- **Alle nutzergerichteten Texte englisch**, alle modellgerichteten Skip-Gründe englisch und korrigierend formuliert (Bestandsmuster in `CanvasPatcher`).
- **Zielversionen:** `DRYL.Components` 2.16.1 → **2.17.0**, `DRYL.Components.Agents` 0.13.0 → **0.14.0** (beide erst in Task 10).
- **Branch:** `canvas-phase6-direct-manipulation` (existiert bereits, Spec ist dort committet).
- **Testkommando:** `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`. Einzelne Klasse: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~<Klassenname>"`.
- **`prefers-reduced-motion: reduce`** muss jede neue CSS-Regel berücksichtigen; die Komponente bleibt ohne Bewegung voll bedienbar.

## File Structure

**Neu (Kern, `DRYL.Components/Canvas/`)**

| Datei | Verantwortung |
| --- | --- |
| `CanvasSelection.cs` | Beobachtbarer Selektionszustand einer Canvas-Fläche + `CanvasNav`, `CanvasNodeCommand`, `CanvasEdit` |
| `CanvasLabel.cs` | Node → sprechende Kurzbezeichnung (für Werkzeugleiste, Chip, Ankündigung) |
| `CanvasNodeClone.cs` | Tiefkopie eines Teilbaums mit frischen Ids und Feldnamen |
| `CanvasTree.cs` | Gemeinsame Baumwalks (`Find`, `FindParent`, `CollectIds`) — heute privat in `CanvasPatcher` dupliziert |

**Geändert (Kern)**

| Datei | Änderung |
| --- | --- |
| `Canvas/CanvasSpec.cs` | `CanvasNode.Locked` |
| `Canvas/CanvasPatcher.cs` | `CanvasPatchAuthor`-Parameter + Pin-Regeln; nutzt `CanvasTree` |
| `Canvas/CanvasContext.cs` | `Selection` + interne `Navigate`/`Command`-Delegates |
| `Canvas/CanvasNodeView.razor` | Trefferfläche, Roving-Tabindex, Fokus, Tastatur, Werkzeugleiste, Pin-Marke |
| `Components/AI/DrylCanvas.razor` | `Selection`/`OnEdit`-Parameter, Navigation, Kommandos, Reorder-Interop, Purge-Fallback |
| `Components/AI/DrylCanvas.razor.css` | Selektionsring, Werkzeugleiste, Pin-Marke, Drag-Zustände, Drop-Marken |
| `Components/Surfaces/DrylChatComposer.razor` | `public ValueTask FocusAsync()` |
| `wwwroot/js/dryl-canvas.js` | `initReorder` / `disposeReorder` |

**Geändert (Agents)**

| Datei | Änderung |
| --- | --- |
| `Canvas/DrylAiCanvas.razor` | `Selection`/`OnEdit` durchreichen |
| `Canvas/DrylCanvasDock.razor(.css)` | Kontext-Chip, Präfix beim Senden, Fokus auf Anfrage |
| `Canvas/CanvasPrompt.cs` | Je eine Zeile zu `locked` in `SchemaText` und `UpdatePrompt` |
| `Canvas/DrylCanvasRun.cs` | `ApplyOp` patcht als `CanvasPatchAuthor.Ai` |

**Tests**

`tests/DRYL.Components.Tests/Canvas/`: `CanvasSelectionTests.cs`, `CanvasLabelTests.cs`, `CanvasNodeCloneTests.cs`, `CanvasPinPatchTests.cs`, `CanvasSelectionRenderTests.cs`, `CanvasNodeToolsTests.cs`, `CanvasReorderTests.cs`
`tests/DRYL.Components.Tests/Agents/Canvas/`: `CanvasDockSelectionTests.cs`, `CanvasPinReceiptTests.cs`

**Website**

`DRYL.Website/Components/Examples/CanvasWorkspace/Direct.razor` (+ `.razor.css`), `Components/Pages/DemoCanvasWorkspace.razor`, `Components/ComponentCatalog.cs`

---

### Task 1: `CanvasSelection` und `CanvasLabel`

**Files:**
- Create: `DRYL.Components/Canvas/CanvasSelection.cs`
- Create: `DRYL.Components/Canvas/CanvasLabel.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasSelectionTests.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasLabelTests.cs`

**Interfaces:**
- Consumes: `CanvasNode`, `CanvasJson` aus `DRYL.Components.Canvas`.
- Produces:
  - `CanvasSelection` mit `string? Id`, `string? Type`, `string? Label`, `bool Locked`, `bool HasSelection`, `string? RovingId`, `event Action? OnChange`, `event Action? OnPromptRequested`, `void Select(CanvasNode node, bool focus = false)`, `void Clear()`, `void RequestPrompt()`, `internal int FocusTick`, `internal void SetFallback(string? id)`
  - `enum CanvasNav { Previous, Next, Parent, FirstChild, First, Last }`
  - `enum CanvasNodeCommand { TogglePin, Duplicate, Remove, MoveUp, MoveDown }`
  - `readonly record struct CanvasEdit(string NodeId, CanvasNodeCommand Command, string Label)`
  - `static string CanvasLabel.For(CanvasNode node)`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests für `CanvasLabel`**

`tests/DRYL.Components.Tests/Canvas/CanvasLabelTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasLabelTests
{
    private static CanvasNode Node(string type, string props) => new()
    {
        Id = "n1",
        Type = type,
        Props = JsonSerializer.Deserialize<JsonElement>(props),
    };

    [Fact]
    public void Prefers_title_over_everything_else()
    {
        var node = Node("lineChart", """{ "title": "Revenue by month", "label": "ignored" }""");
        Assert.Equal("Revenue by month", CanvasLabel.For(node));
    }

    [Theory]
    [InlineData("""{ "label": "Revenue" }""", "Revenue")]
    [InlineData("""{ "text": "Overdue" }""", "Overdue")]
    [InlineData("""{ "submitLabel": "Approve order" }""", "Approve order")]
    [InlineData("""{ "name": "region" }""", "region")]
    public void Falls_through_the_prop_order(string props, string expected)
    {
        Assert.Equal(expected, CanvasLabel.For(Node("stat", props)));
    }

    [Fact]
    public void Uses_the_first_line_of_markdown_content()
    {
        var node = Node("markdown", """{ "content": "## Summary\nrest of it" }""");
        Assert.Equal("## Summary", CanvasLabel.For(node));
    }

    [Fact]
    public void Falls_back_to_a_readable_type_name()
    {
        Assert.Equal("Line chart", CanvasLabel.For(Node("lineChart", "{}")));
        Assert.Equal("Key value", CanvasLabel.For(Node("keyValue", "{}")));
        Assert.Equal("Divider", CanvasLabel.For(new CanvasNode { Id = "d", Type = "divider" }));
    }

    [Fact]
    public void Truncates_long_labels_to_sixty_characters()
    {
        var node = Node("card", $$"""{ "title": "{{new string('x', 90)}}" }""");
        var label = CanvasLabel.For(node);
        Assert.Equal(60, label.Length);
        Assert.EndsWith("…", label);
    }

    [Fact]
    public void Ignores_blank_props()
    {
        Assert.Equal("Stat", CanvasLabel.For(Node("stat", """{ "label": "   " }""")));
    }
}
```

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasLabelTests"`
Expected: Build-Fehler `CS0103: The name 'CanvasLabel' does not exist`.

- [ ] **Step 3: Implementiere `CanvasLabel`**

`DRYL.Components/Canvas/CanvasLabel.cs`:

```csharp
using System.Text;
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns a node into the short, speakable name a human — and a model — recognises it by.
/// Used by the node toolbar, the dock's context chip and every selection announcement, so all
/// three call the same element the same thing.
/// </summary>
public static class CanvasLabel
{
    private const int MaxLength = 60;

    // First non-blank wins. The order follows how the catalog names things: a title beats an
    // inline label, a label beats free text, and a field name is the last thing worth showing.
    private static readonly string[] Sources =
        ["title", "label", "text", "submitLabel", "name", "content"];

    /// <summary>The node's display name, at most 60 characters, never empty.</summary>
    public static string For(CanvasNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Props is { ValueKind: JsonValueKind.Object } props)
        {
            foreach (var source in Sources)
            {
                if (!props.TryGetProperty(source, out var value)) continue;
                if (value.ValueKind != JsonValueKind.String) continue;

                var text = FirstLine(value.GetString());
                if (text.Length > 0) return Truncate(text);
            }
        }

        return TypeName(node.Type);
    }

    private static string FirstLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var newline = raw.AsSpan().IndexOfAny('\r', '\n');
        var line = newline < 0 ? raw : raw[..newline];
        return line.Trim();
    }

    private static string Truncate(string text) =>
        text.Length <= MaxLength ? text : text[..(MaxLength - 1)] + "…";

    // "lineChart" → "Line chart", "keyValue" → "Key value", "stat" → "Stat".
    private static string TypeName(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Element";

        var sb = new StringBuilder(type.Length + 4);
        sb.Append(char.ToUpperInvariant(type[0]));
        foreach (var ch in type.AsSpan(1))
        {
            if (char.IsUpper(ch)) sb.Append(' ').Append(char.ToLowerInvariant(ch));
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Lauf die Tests, sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasLabelTests"`
Expected: PASS (7 Tests)

- [ ] **Step 5: Schreibe die fehlschlagenden Tests für `CanvasSelection`**

`tests/DRYL.Components.Tests/Canvas/CanvasSelectionTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasSelectionTests
{
    private static CanvasNode Chart() => new()
    {
        Id = "c1",
        Type = "lineChart",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "title": "Revenue" }"""),
    };

    [Fact]
    public void Select_records_id_type_label_and_lock()
    {
        var sel = new CanvasSelection();
        var node = Chart();
        node.Locked = true;

        sel.Select(node);

        Assert.True(sel.HasSelection);
        Assert.Equal("c1", sel.Id);
        Assert.Equal("lineChart", sel.Type);
        Assert.Equal("Revenue", sel.Label);
        Assert.True(sel.Locked);
    }

    [Fact]
    public void Select_raises_change_once_and_is_idempotent()
    {
        var sel = new CanvasSelection();
        var changes = 0;
        sel.OnChange += () => changes++;

        sel.Select(Chart());
        sel.Select(Chart());

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Select_raises_change_when_the_lock_flipped_on_the_same_node()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart());
        var changes = 0;
        sel.OnChange += () => changes++;

        var pinned = Chart();
        pinned.Locked = true;
        sel.Select(pinned);

        Assert.Equal(1, changes);
        Assert.True(sel.Locked);
    }

    [Fact]
    public void Select_with_focus_bumps_the_focus_tick_every_time()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart(), focus: true);
        var first = sel.FocusTick;
        sel.Select(Chart(), focus: true);

        Assert.True(sel.FocusTick > first);
    }

    [Fact]
    public void Clear_resets_everything_and_raises_once()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart());
        var changes = 0;
        sel.OnChange += () => changes++;

        sel.Clear();
        sel.Clear();

        Assert.Equal(1, changes);
        Assert.False(sel.HasSelection);
        Assert.Null(sel.Id);
        Assert.Null(sel.Type);
        Assert.Null(sel.Label);
        Assert.False(sel.Locked);
    }

    [Fact]
    public void RovingId_is_the_selection_and_falls_back_to_the_registered_id()
    {
        var sel = new CanvasSelection();
        sel.SetFallback("first");
        Assert.Equal("first", sel.RovingId);

        sel.Select(Chart());
        Assert.Equal("c1", sel.RovingId);

        sel.Clear();
        Assert.Equal("first", sel.RovingId);
    }

    [Fact]
    public void RequestPrompt_raises_its_own_event()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;

        sel.RequestPrompt();

        Assert.Equal(1, asked);
    }
}
```

- [ ] **Step 6: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasSelectionTests"`
Expected: Build-Fehler `CS0246: The type or namespace name 'CanvasSelection' could not be found`.

- [ ] **Step 7: Implementiere `CanvasSelection` und die drei Begleit-Typen**

`DRYL.Components/Canvas/CanvasSelection.cs`:

```csharp
namespace DRYL.Components.Canvas;

/// <summary>Where a keyboard step goes, resolved against the spec tree by <c>DrylCanvas</c>.</summary>
public enum CanvasNav
{
    /// <summary>The previous sibling.</summary>
    Previous,
    /// <summary>The next sibling.</summary>
    Next,
    /// <summary>The parent node (never the root).</summary>
    Parent,
    /// <summary>The first child, for container nodes.</summary>
    FirstChild,
    /// <summary>The first sibling.</summary>
    First,
    /// <summary>The last sibling.</summary>
    Last,
}

/// <summary>What the node toolbar (or a keyboard shortcut) asks the canvas to do.</summary>
public enum CanvasNodeCommand
{
    /// <summary>Pin or unpin the node — see <see cref="CanvasNode.Locked"/>.</summary>
    TogglePin,
    /// <summary>Insert a fresh copy right after the node.</summary>
    Duplicate,
    /// <summary>Remove the node (plays its exit animation first).</summary>
    Remove,
    /// <summary>Move the node one slot up among its siblings.</summary>
    MoveUp,
    /// <summary>Move the node one slot down among its siblings.</summary>
    MoveDown,
}

/// <summary>One completed direct manipulation — raised by <c>DrylCanvas.OnEdit</c> so the host can
/// commit a version and let its document autosave run.</summary>
/// <param name="NodeId">The node the command ran on.</param>
/// <param name="Command">What the user did.</param>
/// <param name="Label">A ready-made history label, e.g. <c>"Removed Revenue"</c>.</param>
public readonly record struct CanvasEdit(string NodeId, CanvasNodeCommand Command, string Label);

/// <summary>
/// The selected node of one canvas surface — the piece of state the renderer and the prompt dock
/// share so the user can point at an element and then talk about it.
/// </summary>
/// <remarks>
/// Plain observable renderer-thread state like <see cref="CanvasWorkspace"/>: no locking, no
/// <c>INotifyPropertyChanged</c>, exactly one <see cref="OnChange"/> per mutation that changed
/// something. One instance per canvas surface; two canvases on a page share nothing.
/// </remarks>
public sealed class CanvasSelection
{
    private string? _fallbackId;

    /// <summary>Id of the selected node, or null while nothing is selected.</summary>
    public string? Id { get; private set; }

    /// <summary>Catalog type of the selected node.</summary>
    public string? Type { get; private set; }

    /// <summary>Speakable name of the selected node (see <see cref="CanvasLabel"/>).</summary>
    public string? Label { get; private set; }

    /// <summary>Whether the selected node is pinned.</summary>
    public bool Locked { get; private set; }

    /// <summary>True while a node is selected.</summary>
    public bool HasSelection => Id is not null;

    /// <summary>
    /// The one node that carries <c>tabindex="0"</c>: the selection, or — while nothing is
    /// selected — the fallback the canvas registered (its root's first child). A whole artifact
    /// tree costs exactly one tab stop.
    /// </summary>
    public string? RovingId => Id ?? _fallbackId;

    /// <summary>Raised after every mutation that changed the selection.</summary>
    public event Action? OnChange;

    /// <summary>Raised by <see cref="RequestPrompt"/> — the dock opens and focuses its composer.</summary>
    public event Action? OnPromptRequested;

    /// <summary>
    /// Selects <paramref name="node"/>. Selecting the same node with the same lock state again
    /// changes nothing and raises nothing.
    /// </summary>
    /// <param name="node">The node the user pointed at.</param>
    /// <param name="focus">Whether the node should also take DOM focus — true for keyboard
    /// navigation, false for a click (the browser has already moved focus).</param>
    public void Select(CanvasNode node, bool focus = false)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (focus) FocusTick++;

        var label = CanvasLabel.For(node);
        if (Id == node.Id && Locked == node.Locked && Label == label && Type == node.Type)
        {
            if (focus) OnChange?.Invoke();   // same node, but it has to take focus again
            return;
        }

        Id = node.Id;
        Type = node.Type;
        Label = label;
        Locked = node.Locked;
        OnChange?.Invoke();
    }

    /// <summary>Drops the selection. A no-op — and silent — when nothing was selected.</summary>
    public void Clear()
    {
        if (Id is null) return;

        Id = null;
        Type = null;
        Label = null;
        Locked = false;
        OnChange?.Invoke();
    }

    /// <summary>Asks the prompt dock to open and focus its composer for the selected element.
    /// Without a dock this is a no-op.</summary>
    public void RequestPrompt() => OnPromptRequested?.Invoke();

    /// <summary>Monotonic counter: a new value tells the selected node's view to take DOM focus.</summary>
    internal int FocusTick { get; private set; }

    /// <summary>Registers the node that owns the tab stop while nothing is selected. Set by
    /// <c>DrylCanvas</c> from the spec's root; silent, because it changes nothing the user sees.</summary>
    internal void SetFallback(string? id) => _fallbackId = id;
}
```

- [ ] **Step 8: Lauf beide Testklassen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasSelectionTests|FullyQualifiedName~CanvasLabelTests"`
Expected: PASS (14 Tests)

- [ ] **Step 9: Commit**

```bash
git add DRYL.Components/Canvas/CanvasSelection.cs DRYL.Components/Canvas/CanvasLabel.cs tests/DRYL.Components.Tests/Canvas/CanvasSelectionTests.cs tests/DRYL.Components.Tests/Canvas/CanvasLabelTests.cs
git commit -m "feat(canvas): CanvasSelection and CanvasLabel — the shared selection state"
```

---

### Task 2: Pin — `CanvasNode.Locked` und die Patcher-Regel

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasSpec.cs` (nach `Action`, vor `Removing`)
- Create: `DRYL.Components/Canvas/CanvasTree.cs`
- Modify: `DRYL.Components/Canvas/CanvasPatcher.cs`
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs:200`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasPinPatchTests.cs`

**Interfaces:**
- Consumes: `CanvasSpec`, `CanvasNode`, `CanvasOp`, `CanvasCatalog` (Task 1 nicht nötig).
- Produces:
  - `bool CanvasNode.Locked` (serialisiert als `"locked"`, `false` wird ausgelassen)
  - `enum CanvasPatchAuthor { User, Ai }`
  - `static string? CanvasPatcher.Apply(CanvasSpec spec, CanvasOp op, CanvasPatchAuthor author = CanvasPatchAuthor.User)`
  - `internal static class CanvasTree` mit `CanvasNode? Find(CanvasSpec spec, string id)`, `CanvasNode? Find(CanvasNode? node, string id)`, `CanvasNode? FindParent(CanvasSpec spec, string id)`, `CanvasNode? FindParent(CanvasNode? node, string id)`, `HashSet<string> CollectIds(CanvasNode? node)`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Canvas/CanvasPinPatchTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// The pin is an instruction to the AI author, not a freeze of the widget: an op the user
/// triggered (a data refresh, an action result, the node toolbar) still goes through.
/// </summary>
public class CanvasPinPatchTests
{
    // "grp" is a pinned card holding "b"; "a" is a pinned stat next to it.
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "locked": true, "props": { "label": "A", "value": "1" } },
            { "id": "free", "type": "stat", "props": { "label": "B", "value": "2" } },
            { "id": "grp", "type": "card", "locked": true, "children": [
                { "id": "b", "type": "stat", "props": { "label": "C", "value": "3" } } ] } ] } }
        """, CanvasJson.Options)!;

    private static JsonElement Props(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public void Locked_survives_a_json_roundtrip_and_false_is_not_written()
    {
        var spec = Spec();
        var json = JsonSerializer.Serialize(spec, CanvasJson.Options);

        Assert.Contains("\"locked\":true", json);
        Assert.DoesNotContain("\"locked\":false", json);
        Assert.True(JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!
            .Root!.Children![0].Locked);
    }

    [Fact]
    public void Ai_setProps_on_a_pinned_node_is_refused_and_changes_nothing()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "999" }"""),
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'setProps': node 'a' is pinned by the user — leave it unchanged and say so if asked.", err);
        Assert.Equal("1", spec.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
    }

    [Fact]
    public void The_same_setProps_goes_through_for_the_user()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "999" }"""),
        });

        Assert.Null(err);
        Assert.Equal("999", spec.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
    }

    [Fact]
    public void Ai_remove_of_a_pinned_node_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "a" },
                                      CanvasPatchAuthor.Ai);

        Assert.Equal("op 'remove': node 'a' is pinned by the user — it must stay.", err);
        Assert.False(spec.Root!.Children![0].Removing);
    }

    [Fact]
    public void Ai_move_of_a_pinned_node_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "a", Parent = "grp", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'a' is pinned by the user — its position must stay.", err);
        Assert.Equal("a", spec.Root!.Children![0].Id);
    }

    [Fact]
    public void Ai_move_into_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "free", Parent = "grp", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'grp' is pinned by the user — nothing may be moved out of or into it.", err);
        Assert.Equal(3, spec.Root!.Children!.Count);
    }

    [Fact]
    public void Ai_move_out_of_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "b", Parent = "root", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'grp' is pinned by the user — nothing may be moved out of or into it.", err);
        Assert.Single(spec.Root!.Children![2].Children!);
    }

    [Fact]
    public void Ai_insert_into_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode { Id = "n", Type = "divider" },
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'insert': node 'grp' is pinned by the user — nothing may be added to it.", err);
        Assert.Single(spec.Root!.Children![2].Children!);
    }

    [Fact]
    public void A_child_of_a_pinned_container_stays_patchable_for_the_ai()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "b", Props = Props("""{ "value": "42" }"""),
        }, CanvasPatchAuthor.Ai);

        Assert.Null(err);
        Assert.Equal("42", spec.Root!.Children![2].Children![0].Props!.Value
            .GetProperty("value").GetString());
    }

    [Fact]
    public void An_unpinned_node_is_untouched_by_the_rule()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "free" },
                                      CanvasPatchAuthor.Ai);

        Assert.Null(err);
        Assert.True(spec.Root!.Children![1].Removing);
    }
}
```

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasPinPatchTests"`
Expected: Build-Fehler — `CanvasNode.Locked` und `CanvasPatchAuthor` existieren nicht.

- [ ] **Step 3: Ergänze `CanvasNode.Locked`**

In `DRYL.Components/Canvas/CanvasSpec.cs`, direkt nach der `Action`-Property (Zeile 61) einfügen:

```csharp
    /// <summary>
    /// Pinned by the user: the AI author may not change, move or remove this node, and may add
    /// nothing to it. Everything the user triggers — a data refresh, an action result, the node
    /// toolbar itself — still goes through (see <c>CanvasPatchAuthor</c>). Travels with the node
    /// into a saved document.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Locked { get; set; }
```

- [ ] **Step 4: Lege `CanvasTree` an und lass `CanvasPatcher` es benutzen**

`DRYL.Components/Canvas/CanvasTree.cs`:

```csharp
namespace DRYL.Components.Canvas;

/// <summary>The tree walks every canvas subsystem needs: find a node, find its parent, collect
/// ids. One implementation, so the patcher, the renderer and the cloner cannot drift apart.</summary>
internal static class CanvasTree
{
    /// <summary>The node with this id, or null.</summary>
    public static CanvasNode? Find(CanvasSpec spec, string id) => Find(spec.Root, id);

    /// <summary>The node with this id inside this subtree, or null.</summary>
    public static CanvasNode? Find(CanvasNode? node, string id)
    {
        if (node is null) return null;
        if (node.Id == id) return node;
        if (node.Children is null) return null;
        foreach (var child in node.Children)
        {
            var found = Find(child, id);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>The parent of the node with this id, or null (also for the root itself).</summary>
    public static CanvasNode? FindParent(CanvasSpec spec, string id) => FindParent(spec.Root, id);

    /// <summary>The parent of the node with this id inside this subtree, or null.</summary>
    public static CanvasNode? FindParent(CanvasNode? node, string id)
    {
        if (node?.Children is null) return null;
        foreach (var child in node.Children)
        {
            if (child.Id == id) return node;
            var found = FindParent(child, id);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>Every id in this subtree.</summary>
    public static HashSet<string> CollectIds(CanvasNode? node)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        Collect(node, ids);
        return ids;
    }

    private static void Collect(CanvasNode? node, HashSet<string> ids)
    {
        if (node is null) return;
        ids.Add(node.Id);
        if (node.Children is null) return;
        foreach (var child in node.Children) Collect(child, ids);
    }
}
```

In `CanvasPatcher.cs` die privaten Walks `FindNode`, `FindParent`, `CollectIds` (Zeilen 155–220, **nicht** `IsSelfOrDescendant`, `ValidateSubtree`, `CollectIdsOrFindDuplicate`) löschen und die Aufrufstellen umstellen:

- `FindNode(spec, id)` → `CanvasTree.Find(spec, id)`
- `FindParent(spec, id)` → `CanvasTree.FindParent(spec, id)`
- `CollectIds(spec.Root)` → `CanvasTree.CollectIds(spec.Root)`

- [ ] **Step 5: Implementiere die Pin-Regel im Patcher**

In `CanvasPatcher.cs` ganz oben den Autor-Typ ergänzen (über der Klasse):

```csharp
/// <summary>Who is patching. The pin (<see cref="CanvasNode.Locked"/>) only binds the AI author —
/// what the user triggers always goes through (roadmap A4).</summary>
public enum CanvasPatchAuthor
{
    /// <summary>The user, directly or through a command they pressed. Ignores pins.</summary>
    User,
    /// <summary>The AI author. Ops on pinned nodes come back as a corrective skip reason.</summary>
    Ai,
}
```

`Apply` bekommt den Parameter und reicht ihn durch:

```csharp
    /// <summary>
    /// Applies <paramref name="op"/> to <paramref name="spec"/>. Returns <c>null</c> on success;
    /// otherwise a model-facing skip reason, and <paramref name="spec"/> is left unchanged.
    /// </summary>
    /// <param name="spec">The live artifact.</param>
    /// <param name="op">The op to apply.</param>
    /// <param name="author">Who is patching — <see cref="CanvasPatchAuthor.Ai"/> respects pins.</param>
    public static string? Apply(CanvasSpec spec, CanvasOp op,
                                CanvasPatchAuthor author = CanvasPatchAuthor.User) => op.Op switch
    {
        "setProps" => ApplySetProps(spec, op, author),
        "insert" => ApplyInsert(spec, op, author),
        "remove" => ApplyRemove(spec, op, author),
        "move" => ApplyMove(spec, op, author),
        _ => $"op '{op.Op}': unknown operation — use 'setProps', 'insert', 'remove' or 'move'.",
    };
```

Die vier Handler bekommen `CanvasPatchAuthor author` als letzten Parameter und je eine Prüfung **direkt nach** dem jeweiligen `FindNode`-Erfolg, vor jeder Mutation:

```csharp
// in ApplySetProps, nach: if (node is null) return …
if (author == CanvasPatchAuthor.Ai && node.Locked)
    return $"op 'setProps': node '{node.Id}' is pinned by the user — leave it unchanged and say so if asked.";

// in ApplyRemove, nach: if (node is null) return …
if (author == CanvasPatchAuthor.Ai && node.Locked)
    return $"op 'remove': node '{node.Id}' is pinned by the user — it must stay.";

// in ApplyInsert, nach: if (!CanvasCatalog.IsContainer(parent.Type)) return …
if (author == CanvasPatchAuthor.Ai && parent.Locked)
    return $"op 'insert': node '{parent.Id}' is pinned by the user — nothing may be added to it.";

// in ApplyMove, nach: if (IsSelfOrDescendant(node, op.Parent)) return …
if (author == CanvasPatchAuthor.Ai)
{
    if (node.Locked)
        return $"op 'move': node '{node.Id}' is pinned by the user — its position must stay.";
    var from = CanvasTree.FindParent(spec, op.Id!);
    if (newParent.Locked)
        return $"op 'move': node '{newParent.Id}' is pinned by the user — nothing may be moved out of or into it.";
    if (from is { Locked: true })
        return $"op 'move': node '{from.Id}' is pinned by the user — nothing may be moved out of or into it.";
}
```

> Reihenfolge beachten: der Test `Ai_move_into_a_pinned_container_is_refused` erwartet den
> `newParent`-Text, `Ai_move_out_of_a_pinned_container_is_refused` den des alten Elternknotens —
> beide nennen `grp`, also ist die Reihenfolge zwischen den beiden egal, aber `node.Locked`
> muss **zuerst** geprüft werden.

- [ ] **Step 6: Lass den AI-Pfad als `Ai` patchen**

`DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`, Zeile 200:

```csharp
        var reason = CanvasPatcher.Apply(Spec, op, CanvasPatchAuthor.Ai);
```

- [ ] **Step 7: Lauf die neuen und die bestehenden Patcher-Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasPinPatchTests|FullyQualifiedName~CanvasPatcherTests|FullyQualifiedName~CanvasDocumentTests"`
Expected: PASS — die bestehenden Patcher- und Dokument-Tests beweisen, dass weder der `CanvasTree`-Umbau noch das neue Feld etwas gebrochen hat.

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/Canvas/CanvasSpec.cs DRYL.Components/Canvas/CanvasTree.cs DRYL.Components/Canvas/CanvasPatcher.cs DRYL.Components.Agents/Canvas/DrylCanvasRun.cs tests/DRYL.Components.Tests/Canvas/CanvasPinPatchTests.cs
git commit -m "feat(canvas): pinned nodes — the patcher refuses AI ops on them"
```

---

### Task 3: `CanvasNodeClone`

**Files:**
- Create: `DRYL.Components/Canvas/CanvasNodeClone.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasNodeCloneTests.cs`

**Interfaces:**
- Consumes: `CanvasJson`, `CanvasNode`, `CanvasCatalog.IsInteractive`, `CanvasTree.CollectIds` (Task 2).
- Produces: `static CanvasNode CanvasNodeClone.Duplicate(CanvasNode node, IReadOnlySet<string> existingIds)`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Canvas/CanvasNodeCloneTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasNodeCloneTests
{
    private static CanvasNode Parse(string json) =>
        JsonSerializer.Deserialize<CanvasNode>(json, CanvasJson.Options)!;

    private static readonly IReadOnlySet<string> Taken =
        new HashSet<string>(StringComparer.Ordinal) { "card", "in", "card-2" };

    [Fact]
    public void Gives_every_node_of_the_subtree_a_free_id()
    {
        var node = Parse("""
            { "id": "card", "type": "card", "props": { "title": "Order" }, "children": [
                { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } } ] }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("card-3", copy.Id);              // card-2 was taken
        Assert.Equal("in-2", copy.Children![0].Id);
    }

    [Fact]
    public void Renames_interactive_fields_so_the_copy_owns_its_own_value()
    {
        var node = Parse("""
            { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("qty-2", copy.Props!.Value.GetProperty("name").GetString());
        Assert.Equal("Qty", copy.Props!.Value.GetProperty("label").GetString());
    }

    [Fact]
    public void Keeps_data_and_action_bindings()
    {
        var node = Parse("""
            { "id": "c", "type": "lineChart",
              "data": { "source": "sales.byMonth", "params": { "year": 2026 } } }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("sales.byMonth", copy.Data!.Source);
        Assert.Equal(2026, copy.Data!.Params!.Value.GetProperty("year").GetInt32());
    }

    [Fact]
    public void A_copy_starts_unpinned()
    {
        var node = Parse("""{ "id": "c", "type": "divider", "locked": true }""");

        Assert.False(CanvasNodeClone.Duplicate(node, Taken).Locked);
    }

    [Fact]
    public void Leaves_the_original_untouched()
    {
        var node = Parse("""
            { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } }
            """);

        CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("in", node.Id);
        Assert.Equal("qty", node.Props!.Value.GetProperty("name").GetString());
    }

    [Fact]
    public void Two_copies_in_a_row_do_not_collide()
    {
        var node = Parse("""{ "id": "d", "type": "divider" }""");
        var ids = new HashSet<string>(StringComparer.Ordinal) { "d" };

        var first = CanvasNodeClone.Duplicate(node, ids);
        ids.Add(first.Id);
        var second = CanvasNodeClone.Duplicate(node, ids);

        Assert.Equal("d-2", first.Id);
        Assert.Equal("d-3", second.Id);
    }
}
```

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasNodeCloneTests"`
Expected: Build-Fehler `CS0103: The name 'CanvasNodeClone' does not exist`.

- [ ] **Step 3: Implementiere `CanvasNodeClone`**

`DRYL.Components/Canvas/CanvasNodeClone.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>
/// Deep-copies a node for the toolbar's "duplicate": same shape, same bindings, fresh ids and
/// fresh field names — a copy that shares an id would break every patch, a copy that shares a
/// field name would share the user's input.
/// </summary>
public static class CanvasNodeClone
{
    /// <summary>
    /// A copy of <paramref name="node"/> whose every id is free of <paramref name="existingIds"/>.
    /// The copy starts unpinned; data and action bindings travel with it.
    /// </summary>
    /// <param name="node">The node to copy. Left untouched.</param>
    /// <param name="existingIds">Every id already present in the artifact.</param>
    public static CanvasNode Duplicate(CanvasNode node, IReadOnlySet<string> existingIds)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(existingIds);

        // JSON roundtrip: the deep copy comes for free and it provably keeps exactly what a
        // saved document would keep (props, data, action) and nothing transient.
        var copy = JsonSerializer.Deserialize<CanvasNode>(
            JsonSerializer.Serialize(node, CanvasJson.Options), CanvasJson.Options)!;

        var taken = new HashSet<string>(existingIds, StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        Rename(copy, taken, names);
        return copy;
    }

    private static void Rename(CanvasNode node, HashSet<string> takenIds, HashSet<string> takenNames)
    {
        node.Locked = false;                     // a copy is not the pinned original
        node.Id = FreeName(node.Id, takenIds);
        takenIds.Add(node.Id);

        if (CanvasCatalog.IsInteractive(node.Type)) RenameField(node, takenNames);

        if (node.Children is null) return;
        foreach (var child in node.Children) Rename(child, takenIds, takenNames);
    }

    // The field name is what the form state is keyed by — sharing it would make the copy and the
    // original edit one value.
    private static void RenameField(CanvasNode node, HashSet<string> takenNames)
    {
        if (node.Props is not { ValueKind: JsonValueKind.Object } props) return;
        if (JsonNode.Parse(props.GetRawText()) is not JsonObject obj) return;
        if (obj["name"]?.GetValue<string>() is not { Length: > 0 } name) return;

        var fresh = FreeName(name, takenNames);
        takenNames.Add(fresh);
        obj["name"] = fresh;
        node.Props = JsonSerializer.SerializeToElement(obj, CanvasJson.Options);
    }

    // "id" → "id-2", "id-3", … until free. Deterministic and readable, which matters: these ids
    // end up in the model's next update prompt.
    private static string FreeName(string original, IReadOnlySet<string> taken)
    {
        var stem = string.IsNullOrEmpty(original) ? "node" : original;
        for (var n = 2; ; n++)
        {
            var candidate = FormattableString.Invariant($"{stem}-{n}");
            if (!taken.Contains(candidate)) return candidate;
        }
    }
}
```

- [ ] **Step 4: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasNodeCloneTests"`
Expected: PASS (6 Tests)

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Canvas/CanvasNodeClone.cs tests/DRYL.Components.Tests/Canvas/CanvasNodeCloneTests.cs
git commit -m "feat(canvas): CanvasNodeClone — duplicate a subtree with fresh ids and field names"
```

---

### Task 4: Selektion im Renderer — Trefferfläche, Roving-Tabindex, Tastatur

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasContext.cs`
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasSelectionRenderTests.cs`

**Interfaces:**
- Consumes: `CanvasSelection`, `CanvasNav` (Task 1); `CanvasTree` (Task 2).
- Produces:
  - `CanvasContext.Selection` (`public CanvasSelection? Selection { get; internal set; }`)
  - `CanvasContext.Navigate` (`internal Func<string, CanvasNav, bool>?`)
  - `DrylCanvas.Selection` (`[Parameter] public CanvasSelection? Selection { get; set; }`)
  - CSS-Klasse `.canvas-node.is-selected`, Attribut `tabindex` auf `.canvas-node`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Canvas/CanvasSelectionRenderTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasSelectionRenderTests : BunitContext
{
    public CanvasSelectionRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "Revenue", "value": "1" } },
            { "id": "grp", "type": "card", "props": { "title": "Group" }, "children": [
                { "id": "b", "type": "stat", "props": { "label": "Inner", "value": "2" } } ] } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(CanvasSelection sel) =>
        Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec()).Add(x => x.Selection, sel));

    [Fact]
    public void Without_a_selection_object_nothing_changes_in_the_markup()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec()));

        Assert.DoesNotContain("tabindex=\"0\"", cut.Find(".canvas-body").ToMarkup());
        Assert.Empty(cut.FindAll(".canvas-node-tools"));
    }

    [Fact]
    public void Clicking_a_node_selects_it()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='a']").Click();

        Assert.Equal("a", sel.Id);
        Assert.Equal("Revenue", sel.Label);
        Assert.Contains("is-selected", cut.Find("[data-cid='a']").GetAttribute("class"));
    }

    [Fact]
    public void Clicking_an_inner_node_selects_the_inner_node_not_its_container()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='b']").Click();

        Assert.Equal("b", sel.Id);
    }

    [Fact]
    public void The_root_node_is_not_selectable()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        Assert.Null(cut.Find("[data-cid='root']").GetAttribute("tabindex"));
    }

    [Fact]
    public void Exactly_one_node_carries_the_tab_stop()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        Assert.Single(cut.FindAll(".canvas-node[tabindex='0']"));
        Assert.Equal("a", cut.Find(".canvas-node[tabindex='0']").GetAttribute("data-cid"));

        cut.Find("[data-cid='b']").Click();

        Assert.Single(cut.FindAll(".canvas-node[tabindex='0']"));
        Assert.Equal("b", cut.Find(".canvas-node[tabindex='0']").GetAttribute("data-cid"));
    }

    [Theory]
    [InlineData("a", "ArrowDown", "grp")]
    [InlineData("grp", "ArrowUp", "a")]
    [InlineData("grp", "ArrowRight", "b")]
    [InlineData("b", "ArrowLeft", "grp")]
    [InlineData("grp", "Home", "a")]
    [InlineData("a", "End", "grp")]
    public void Arrow_keys_walk_the_tree(string from, string key, string expected)
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find($"[data-cid='{from}']").Focus();
        cut.Find($"[data-cid='{from}']").Click();

        cut.Find($"[data-cid='{from}']").KeyDown(key);

        Assert.Equal(expected, sel.Id);
    }

    [Fact]
    public void Arrow_keys_do_nothing_while_the_wrapper_has_no_focus()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Click();
        cut.Find("[data-cid='a']").Blur();

        cut.Find("[data-cid='a']").KeyDown("ArrowDown");

        Assert.Equal("a", sel.Id);
    }

    [Fact]
    public void Escape_clears_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Escape");

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Enter_asks_the_dock_for_a_prompt()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Enter");

        Assert.Equal(1, asked);
    }

    [Fact]
    public void A_new_spec_instance_drops_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Click();

        cut.Render(p => p.Add(x => x.Spec, Spec()).Add(x => x.Selection, sel));

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void The_node_announces_itself()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='a']").Click();

        Assert.Contains("Selected: Revenue, stat", cut.Markup);
    }
}
```

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasSelectionRenderTests"`
Expected: Build-Fehler — `DrylCanvas` hat keinen `Selection`-Parameter.

- [ ] **Step 3: Erweitere `CanvasContext`**

In `DRYL.Components/Canvas/CanvasContext.cs`, nach der `Actions`-Property einfügen:

```csharp
    /// <summary>The selected node of this canvas, or <c>null</c> when the host did not opt into
    /// direct manipulation. A set selection is what turns the nodes into pickable elements.</summary>
    public CanvasSelection? Selection { get; internal set; }

    /// <summary>Moves the selection one step from the given node. Returns false when there is
    /// nowhere to go. Set by <c>DrylCanvas</c>, which owns the tree.</summary>
    internal Func<string, CanvasNav, bool>? Navigate { get; set; }
```

- [ ] **Step 4: Mach den Node-Wrapper selektierbar**

In `DRYL.Components/Canvas/CanvasNodeView.razor` den Wrapper (Zeile 30) ersetzen:

```razor
    <div class="@WrapperCss" data-cid="@Node.Id" @ref="_el"
         tabindex="@TabIndex"
         aria-label="@WrapperAriaLabel"
         @onclick="SelectSelf" @onclick:stopPropagation="Selectable"
         @onfocus="() => _focused = true" @onblur="() => _focused = false"
         @onkeydown="OnNodeKeyDown" @onkeydown:preventDefault="OwnsKeys">
```

Am Ende des `@code`-Blocks (vor `BuildTable`) ergänzen:

```csharp
    // ───── selection ─────

    /// <summary>True for the one node DrylCanvas renders itself — the artifact's root is the
    /// artifact, not an element inside it, so it is never selectable.</summary>
    [Parameter] public bool IsRoot { get; set; }

    private ElementReference _el;
    private bool _focused;
    private int _seenFocusTick;

    private bool Selectable => Ctx.Selection is not null && !IsRoot;

    private bool IsSelected => Selectable && Ctx.Selection!.Id == Node.Id;

    // Arrow keys are navigation only while this wrapper itself holds focus. `focus` does not
    // bubble, so an input inside the node never sets this flag and keeps its own key handling.
    private bool OwnsKeys => Selectable && _focused;

    // Roving tabindex: exactly one node in the tree is in the tab order.
    private string? TabIndex =>
        !Selectable ? null : Ctx.Selection!.RovingId == Node.Id ? "0" : "-1";

    private string WrapperCss => IsSelected ? "canvas-node is-selected" : "canvas-node";

    private string? WrapperAriaLabel
    {
        get
        {
            if (!Selectable) return null;
            var label = $"{CanvasLabel.For(Node)}, {Node.Type}";
            return Node.Locked ? label + ", pinned" : label;
        }
    }

    private void SelectSelf()
    {
        if (!Selectable) return;
        Ctx.Selection!.Select(Node);
    }

    private void OnNodeKeyDown(KeyboardEventArgs e)
    {
        if (!OwnsKeys) return;
        var selection = Ctx.Selection!;

        switch (e.Key)
        {
            case "ArrowUp": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Previous); break;
            case "ArrowDown": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Next); break;
            case "ArrowLeft": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Parent); break;
            case "ArrowRight": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.FirstChild); break;
            case "Home": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.First); break;
            case "End": Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Last); break;
            case "Enter": selection.RequestPrompt(); break;
            case "Escape": selection.Clear(); break;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Keyboard navigation moved the selection here — take the focus with it, so the next
        // arrow key comes back to this wrapper.
        if (Ctx.Selection is not { } sel || sel.Id != Node.Id) return;
        if (sel.FocusTick == _seenFocusTick) return;
        _seenFocusTick = sel.FocusTick;

        try { await _el.FocusAsync(); }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (InvalidOperationException) { /* prerender — no JS */ }
    }
```

Ganz oben in der Datei die nötigen Usings ergänzen (nach `@using System.Text.Json`):

```razor
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
```

> `_seenFocusTick` startet bei 0 und `FocusTick` ebenfalls — ein Klick (`focus: false`) erhöht
> ihn nicht, also holt sich nur eine Tastaturnavigation den Fokus. Genau richtig.

- [ ] **Step 5: Verdrahte Selektion und Navigation in `DrylCanvas`**

In `DRYL.Components/Components/AI/DrylCanvas.razor`:

Parameter nach `Pulse` einfügen:

```csharp
    /// <summary>
    /// Opt-in for direct manipulation: hand the canvas a selection and its nodes become
    /// pickable — click or keyboard, with a toolbar to pin, duplicate, remove or prompt about the
    /// selected element. Share the same instance with <c>DrylCanvasDock</c> and its context chip
    /// carries the element into the next prompt. Without it nothing about the canvas changes.
    /// </summary>
    [Parameter] public CanvasSelection? Selection { get; set; }
```

Feld und Abo (bei den anderen Feldern):

```csharp
    private CanvasSelection? _subscribedSelection;
    private string? _selectionAnnouncement;
```

`OnInitialized` am Ende ergänzen:

```csharp
        _ctx.Navigate = Navigate;
```

`OnParametersSet` ergänzen — Abo, Fallback und Lebenszyklus:

```csharp
        if (!ReferenceEquals(_subscribedSelection, Selection))
        {
            if (_subscribedSelection is not null) _subscribedSelection.OnChange -= HandleSelectionChanged;
            _subscribedSelection = Selection;
            if (_subscribedSelection is not null) _subscribedSelection.OnChange += HandleSelectionChanged;
            _ctx.Selection = Selection;
        }

        // The tab stop while nothing is selected: the root's first child.
        Selection?.SetFallback(Spec?.Root?.Children?.FirstOrDefault()?.Id);
```

…und im bestehenden `if (!ReferenceEquals(_boundSpec, Spec))`-Block **nach** `_binder?.Reset();`:

```csharp
            // A different artifact — a selection into the old one means nothing.
            Selection?.Clear();
```

Handler und Navigation (bei den anderen privaten Methoden):

```csharp
    private void HandleSelectionChanged()
    {
        _selectionAnnouncement = Selection is { HasSelection: true, Label: { } label, Type: { } type }
            ? $"Selected: {label}, {type}"
            : "Selection cleared.";
        InvokeAsync(StateHasChanged);
    }

    // The canvas owns the spec, so it is the only place that can turn "one step up" into a node.
    private bool Navigate(string fromId, CanvasNav direction)
    {
        if (Spec?.Root is not { } root || Selection is null) return false;

        var node = CanvasTree.Find(root, fromId);
        if (node is null) return false;

        var target = direction switch
        {
            CanvasNav.FirstChild => node.Children?.FirstOrDefault(c => !c.Removing),
            CanvasNav.Parent => CanvasTree.FindParent(root, fromId) is { } p && p.Id != root.Id ? p : null,
            _ => Sibling(root, node, direction),
        };

        if (target is null) return false;
        Selection.Select(target, focus: true);
        return true;
    }

    private static CanvasNode? Sibling(CanvasNode root, CanvasNode node, CanvasNav direction)
    {
        var parent = CanvasTree.FindParent(root, node.Id);
        var siblings = parent?.Children?.Where(c => !c.Removing).ToList();
        if (siblings is not { Count: > 0 }) return null;

        var index = siblings.IndexOf(node);
        if (index < 0) return null;

        return direction switch
        {
            CanvasNav.Previous => index > 0 ? siblings[index - 1] : null,
            CanvasNav.Next => index < siblings.Count - 1 ? siblings[index + 1] : null,
            CanvasNav.First => siblings[0],
            CanvasNav.Last => siblings[^1],
            _ => null,
        };
    }
```

Zweite Live-Region im Markup, direkt unter der bestehenden `.canvas-live`:

```razor
    <div class="canvas-live" aria-live="polite">@_selectionAnnouncement</div>
```

Root-Node mit `IsRoot` rendern (im `.canvas-body`):

```razor
                <CanvasNodeView Node="root" IsRoot="true" />
```

`DisposeAsync` ergänzen:

```csharp
        if (_subscribedSelection is not null) _subscribedSelection.OnChange -= HandleSelectionChanged;
```

- [ ] **Step 6: CSS für den Selektionsring**

An `DRYL.Components/Components/AI/DrylCanvas.razor.css` anhängen (nach dem `.canvas-pulse`-Block):

```css
/* ── Direct manipulation ─────────────────────────────────────────────────────
   A selected node wears the same accent language as the change-pulse, only it
   stays: a 1px accent ring plus the shared glow. Never a filled background —
   the accent glows, it does not scream (CLAUDE.md §2.4). */
::deep .canvas-node.is-selected {
    box-shadow: 0 0 0 1px var(--accent-line), var(--glow-accent);
    border-radius: var(--r-lg);
    transition: box-shadow var(--dur-fast) var(--ease-out);
}

@media (prefers-reduced-motion: reduce) {
    ::deep .canvas-node.is-selected { transition: none; }
}
```

- [ ] **Step 7: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasSelectionRenderTests|FullyQualifiedName~DrylCanvasStandaloneTests|FullyQualifiedName~CanvasCatalogRenderTests"`
Expected: PASS — die beiden Bestandsklassen beweisen, dass ein Canvas ohne `Selection` unverändert rendert.

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/Canvas/CanvasContext.cs DRYL.Components/Canvas/CanvasNodeView.razor DRYL.Components/Components/AI/DrylCanvas.razor DRYL.Components/Components/AI/DrylCanvas.razor.css tests/DRYL.Components.Tests/Canvas/CanvasSelectionRenderTests.cs
git commit -m "feat(canvas): node selection — click, roving tabindex and keyboard walk"
```

---

### Task 5: Werkzeugleiste, Kommandos und `OnEdit`

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasContext.cs`
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasNodeToolsTests.cs`

**Interfaces:**
- Consumes: `CanvasNodeCommand`, `CanvasEdit` (Task 1); `CanvasNodeClone` (Task 3); `CanvasSelection`, `CanvasContext.Selection` (Task 4).
- Produces:
  - `CanvasContext.Command` (`internal Func<string, CanvasNodeCommand, Task>?`)
  - `DrylCanvas.OnEdit` (`[Parameter] public EventCallback<CanvasEdit> OnEdit { get; set; }`)
  - CSS-Klassen `.canvas-node-tools`, `.canvas-node-pin`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Canvas/CanvasNodeToolsTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasNodeToolsTests : BunitContext
{
    public CanvasNodeToolsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "Revenue", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "Orders", "value": "2" } } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(
        CanvasSpec spec, CanvasSelection sel, List<CanvasEdit>? edits = null) =>
        Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, sel)
            .Add(x => x.OnEdit, e => edits?.Add(e)));

    private static IElement Tool(IRenderedComponent<DrylCanvas> cut, string label) =>
        cut.Find($".canvas-node-tools button[aria-label='{label}']");

    [Fact]
    public void The_toolbar_appears_only_on_the_selected_node()
    {
        var cut = Canvas(Spec(), new CanvasSelection());
        Assert.Empty(cut.FindAll(".canvas-node-tools"));

        cut.Find("[data-cid='a']").Click();

        Assert.Single(cut.FindAll(".canvas-node-tools"));
        Assert.Contains("canvas-node-tools",
            cut.Find("[data-cid='a']").InnerHtml);
    }

    [Fact]
    public void Pinning_locks_the_node_and_reports_an_edit()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Pin element").Click();

        Assert.True(spec.Root!.Children![0].Locked);
        Assert.True(sel.Locked);
        Assert.Equal(CanvasNodeCommand.TogglePin, edits[0].Command);
        Assert.Equal("Pinned Revenue", edits[0].Label);
        Assert.Contains("Revenue pinned.", cut.Markup);
    }

    [Fact]
    public void A_pinned_node_shows_its_mark_and_disables_the_destructive_tools()
    {
        var spec = Spec();
        spec.Root!.Children![0].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Click();

        Assert.Single(cut.FindAll(".canvas-node-pin"));
        Assert.True(Tool(cut, "Duplicate element").HasAttribute("disabled"));
        Assert.True(Tool(cut, "Remove element").HasAttribute("disabled"));
        Assert.False(Tool(cut, "Unpin element").HasAttribute("disabled"));
    }

    [Fact]
    public void Duplicating_inserts_a_fresh_copy_right_after_the_original()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Duplicate element").Click();

        Assert.Equal(3, spec.Root!.Children!.Count);
        Assert.Equal("a-2", spec.Root.Children[1].Id);
        Assert.Equal("a-2", sel.Id);                       // the copy is selected
        Assert.Equal("Duplicated Revenue", edits[0].Label);
    }

    [Fact]
    public void Removing_flags_the_node_for_its_exit_and_clears_the_selection()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Remove element").Click();

        Assert.True(spec.Root!.Children![0].Removing);
        Assert.False(sel.HasSelection);
        Assert.Equal("Removed Revenue", edits[0].Label);
    }

    [Fact]
    public void Delete_removes_the_focused_node()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Delete");

        Assert.True(spec.Root!.Children![0].Removing);
    }

    [Fact]
    public void Delete_refuses_a_pinned_node()
    {
        var spec = Spec();
        spec.Root!.Children![0].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Delete");

        Assert.False(spec.Root.Children[0].Removing);
    }

    [Fact]
    public void Without_a_purge_handler_the_canvas_drops_the_node_itself()
    {
        var spec = Spec();
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, new CanvasSelection()));
        cut.Find("[data-cid='a']").Click();
        cut.Find(".canvas-node-tools button[aria-label='Remove element']").Click();

        cut.InvokeAsync(() => cut.Instance.PurgeForTest("a"));

        Assert.Single(spec.Root!.Children!);
        Assert.Equal("b", spec.Root.Children[0].Id);
    }

    [Fact]
    public void The_prompt_tool_asks_for_a_prompt()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;
        var cut = Canvas(Spec(), sel);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Prompt about this element").Click();

        Assert.Equal(1, asked);
    }
}
```

> `PurgeForTest` ist eine Test-Naht: `DrylCanvas` bekommt dafür in Step 4 eine `internal`
> Methode, weil die echte Purge über `DrylPresence.OnExited` läuft und bUnit keine
> CSS-Transition abspielt. Ergänze in derselben Datei ganz oben
> `using AngleSharp.Dom;` für `IElement`.

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasNodeToolsTests"`
Expected: Build-Fehler — `OnEdit` und `PurgeForTest` existieren nicht.

- [ ] **Step 3: `CanvasContext.Command`**

In `DRYL.Components/Canvas/CanvasContext.cs` nach `Navigate` einfügen:

```csharp
    /// <summary>Runs a node command (pin, duplicate, remove, reorder) against the spec. Set by
    /// <c>DrylCanvas</c>, which owns the spec and the history it feeds.</summary>
    internal Func<string, CanvasNodeCommand, Task>? Command { get; set; }
```

- [ ] **Step 4: Kommandos in `DrylCanvas`**

Parameter nach `OnAction` ergänzen:

```csharp
    /// <summary>
    /// Raised after every completed direct manipulation — pin, duplicate, remove, reorder. Bump
    /// your workspace's <c>Revision</c> and set <c>RevisionLabel</c> from
    /// <see cref="CanvasEdit.Label"/> to make the user's own change a version like an AI round.
    /// </summary>
    [Parameter] public EventCallback<CanvasEdit> OnEdit { get; set; }
```

`OnInitialized` ergänzen:

```csharp
        _ctx.Command = RunCommandAsync;
```

`_ctx.Purge` durch die Fallback-Variante ersetzen (bestehende Zeile in `OnInitialized`):

```csharp
        // Without a host that owns the spec (a plain DrylCanvas with a spec from code or a
        // document), nobody would ever drop the node and it would linger as an invisible,
        // Removing-flagged element. Purge it ourselves.
        _ctx.Purge = id => OnPurge.HasDelegate ? OnPurge.InvokeAsync(id) : PurgeSelf(id);
```

Die Kommando-Implementierung (bei den anderen privaten Methoden):

```csharp
    // Every structural change is one CanvasOp through the one patcher — that is what makes the
    // presence, FLIP and pulse layers animate a user edit exactly like an AI edit (A8).
    private async Task RunCommandAsync(string id, CanvasNodeCommand command)
    {
        if (Spec?.Root is not { } root || Selection is null) return;
        var node = CanvasTree.Find(root, id);
        if (node is null) return;

        var label = CanvasLabel.For(node);
        var done = command switch
        {
            CanvasNodeCommand.TogglePin => TogglePin(node),
            CanvasNodeCommand.Duplicate => Duplicate(root, node),
            CanvasNodeCommand.Remove => Remove(node),
            _ => Reorder(root, node, command == CanvasNodeCommand.MoveUp ? -1 : 1),
        };
        if (!done) return;

        _selectionAnnouncement = command switch
        {
            CanvasNodeCommand.TogglePin => node.Locked ? $"{label} pinned." : $"{label} unpinned.",
            CanvasNodeCommand.Duplicate => $"{label} duplicated.",
            CanvasNodeCommand.Remove => $"{label} removed.",
            _ => PositionAnnouncement(root, node, label),
        };

        StateHasChanged();
        await OnEdit.InvokeAsync(new CanvasEdit(id, command, EditLabel(command, label)));
    }

    private static string EditLabel(CanvasNodeCommand command, string label) => command switch
    {
        CanvasNodeCommand.TogglePin => $"Pinned {label}",
        CanvasNodeCommand.Duplicate => $"Duplicated {label}",
        CanvasNodeCommand.Remove => $"Removed {label}",
        _ => $"Moved {label}",
    };

    // The pin is metadata, not content: it never goes through the patcher and never pulses.
    private bool TogglePin(CanvasNode node)
    {
        node.Locked = !node.Locked;
        node.Version++;
        Selection!.Select(node);
        return true;
    }

    private bool Duplicate(CanvasNode root, CanvasNode node)
    {
        var parent = CanvasTree.FindParent(root, node.Id);
        if (parent?.Children is null || node.Locked) return false;

        var copy = CanvasNodeClone.Duplicate(node, CanvasTree.CollectIds(root));
        var index = parent.Children.IndexOf(node) + 1;
        if (_ctx.Patch!(new CanvasOp { Op = "insert", Parent = parent.Id, Index = index, Node = copy }) is not null)
            return false;

        Selection!.Select(copy);
        return true;
    }

    private bool Remove(CanvasNode node)
    {
        if (node.Locked) return false;
        if (_ctx.Patch!(new CanvasOp { Op = "remove", Id = node.Id }) is not null) return false;

        Selection!.Clear();
        return true;
    }

    // One slot up or down among the siblings that are not on their way out.
    private bool Reorder(CanvasNode root, CanvasNode node, int delta)
    {
        var parent = CanvasTree.FindParent(root, node.Id);
        if (parent?.Children is null || node.Locked || parent.Locked) return false;

        var index = parent.Children.IndexOf(node) + delta;
        if (index < 0 || index >= parent.Children.Count) return false;

        return _ctx.Patch!(new CanvasOp
        {
            Op = "move", Id = node.Id, Parent = parent.Id, Index = index,
        }) is null;
    }

    private string PositionAnnouncement(CanvasNode root, CanvasNode node, string label)
    {
        var siblings = CanvasTree.FindParent(root, node.Id)?.Children;
        if (siblings is null) return $"{label} moved.";
        return FormattableString.Invariant(
            $"{label} moved to position {siblings.IndexOf(node) + 1} of {siblings.Count}.");
    }

    private Task PurgeSelf(string id)
    {
        if (Spec?.Root is not { } root) return Task.CompletedTask;
        var parent = CanvasTree.FindParent(root, id);
        var node = parent?.Children?.FirstOrDefault(c => c.Id == id);
        if (parent?.Children is null || node is null) return Task.CompletedTask;

        parent.Children.Remove(node);
        parent.Version++;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Test seam: bUnit never plays the exit transition that would call
    /// <c>DrylPresence.OnExited</c>, so a test drives the purge directly.</summary>
    internal Task PurgeForTest(string id) => _ctx.Purge!(id);
```

- [ ] **Step 5: Werkzeugleiste und Pin-Marke in `CanvasNodeView`**

In `CanvasNodeView.razor` direkt **nach** dem öffnenden `<div class="@WrapperCss" …>` und **vor**
dem `@if (_pulseKey > 0)`-Block einfügen:

```razor
        @if (Selectable && Node.Locked)
        {
            <span class="canvas-node-pin" aria-hidden="true">
                <DrylIcon Name="Lock" Size="12" />
            </span>
        }
        @if (Selectable)
        {
            <DrylPresence Visible="IsSelected" Transition="PresenceTransition.Scale" Speed="PresenceSpeed.Fast">
                <div class="canvas-node-tools glass-card">
                    <DrylTooltip Text="Prompt about this element">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="Prompt about this element"
                                    OnClick="@(() => Ctx.Selection!.RequestPrompt())">
                            <DrylIcon Name="Sparkle" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                    <DrylTooltip Text="@PinLabel">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="@PinLabel"
                                    Pressed="Node.Locked"
                                    OnClick="@(() => RunCommand(CanvasNodeCommand.TogglePin))">
                            <DrylIcon Name="Lock" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                    <DrylTooltip Text="Duplicate element">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="Duplicate element"
                                    Disabled="Node.Locked"
                                    OnClick="@(() => RunCommand(CanvasNodeCommand.Duplicate))">
                            <DrylIcon Name="Copy" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                    <DrylTooltip Text="Remove element">
                        <DrylButton Variant="DrylButton.ButtonVariant.Danger"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="Remove element"
                                    Disabled="Node.Locked"
                                    OnClick="@(() => RunCommand(CanvasNodeCommand.Remove))">
                            <DrylIcon Name="Trash" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                </div>
            </DrylPresence>
        }
```

Im `@code`-Block ergänzen:

```csharp
    private string PinLabel => Node.Locked ? "Unpin element" : "Pin element";

    private Task RunCommand(CanvasNodeCommand command) =>
        Ctx.Command?.Invoke(Node.Id, command) ?? Task.CompletedTask;
```

Und im `OnNodeKeyDown`-Switch zwei Fälle ergänzen (vor `case "Enter"`):

```csharp
            case "Delete" or "Backspace":
                if (!Node.Locked) _ = RunCommand(CanvasNodeCommand.Remove);
                break;
```

- [ ] **Step 6: CSS für Werkzeugleiste und Pin-Marke**

An `DrylCanvas.razor.css` anhängen:

```css
/* The toolbar sits on the node's top-right corner and comes and goes with a scale presence —
   it is chrome, so it never takes space in the node's own layout. */
::deep .canvas-node-tools {
    position: absolute;
    top: calc(var(--sp-1) * -1);
    right: calc(var(--sp-1) * -1);
    z-index: 2;
    display: flex;
    align-items: center;
    gap: var(--sp-1);
    padding: var(--sp-1);
    border-radius: var(--r-pill);
}

/* A pinned node says so even while nothing is selected — the state is in the wrapper's
   aria-label, so the mark itself is decoration. */
::deep .canvas-node-pin {
    position: absolute;
    top: var(--sp-1);
    right: var(--sp-1);
    z-index: 1;
    display: inline-flex;
    color: var(--fg-dim);
    pointer-events: none;
}

/* …except while the toolbar is up, which carries the pin state on its own button. */
::deep .canvas-node.is-selected .canvas-node-pin { opacity: 0; }
```

- [ ] **Step 7: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasNodeToolsTests|FullyQualifiedName~CanvasSelectionRenderTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/Canvas/CanvasContext.cs DRYL.Components/Canvas/CanvasNodeView.razor DRYL.Components/Components/AI/DrylCanvas.razor DRYL.Components/Components/AI/DrylCanvas.razor.css tests/DRYL.Components.Tests/Canvas/CanvasNodeToolsTests.cs
git commit -m "feat(canvas): node toolbar — prompt, pin, duplicate and remove an element"
```

---

### Task 6: Drag-Reorder

**Files:**
- Modify: `DRYL.Components/wwwroot/js/dryl-canvas.js`
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasReorderTests.cs`

**Interfaces:**
- Consumes: `CanvasNodeCommand.MoveUp/MoveDown`, `CanvasContext.Command` (Task 5).
- Produces:
  - `[JSInvokable] Task DrylCanvas.OnNodeReorder(string id, int index)`
  - JS-Exporte `initReorder(root, dotnet)`, `disposeReorder(root)`
  - Attribut `data-drag-handle` auf dem Griff, `data-drop-before` / `data-drop-after` auf Geschwistern

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Canvas/CanvasReorderTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasReorderTests : BunitContext
{
    public CanvasReorderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "B", "value": "2" } },
            { "id": "c", "type": "stat", "props": { "label": "C", "value": "3" } } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(
        CanvasSpec spec, CanvasSelection sel, List<CanvasEdit>? edits = null) =>
        Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, sel)
            .Add(x => x.OnEdit, e => edits?.Add(e)));

    private static IReadOnlyList<string> Ids(CanvasSpec spec) =>
        spec.Root!.Children!.Select(c => c.Id).ToList();

    [Fact]
    public void The_drop_reported_from_js_becomes_one_move_op()
    {
        var spec = Spec();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, new CanvasSelection(), edits);

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("c", 0));

        Assert.Equal(new[] { "c", "a", "b" }, Ids(spec));
        Assert.Equal(CanvasNodeCommand.MoveUp, edits[0].Command);
    }

    [Fact]
    public void A_drop_onto_the_same_slot_changes_nothing_and_reports_nothing()
    {
        var spec = Spec();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, new CanvasSelection(), edits);

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("a", 0));

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
        Assert.Empty(edits);
    }

    [Fact]
    public void A_drop_on_a_pinned_node_is_refused()
    {
        var spec = Spec();
        spec.Root!.Children![2].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("c", 0));

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
    }

    [Fact]
    public void Alt_arrow_moves_the_focused_node_one_slot()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='b']").Focus();
        cut.Find("[data-cid='b']").Click();

        cut.Find("[data-cid='b']").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        Assert.Equal(new[] { "b", "a", "c" }, Ids(spec));
    }

    [Fact]
    public void Alt_arrow_at_the_edge_does_nothing()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
    }

    [Fact]
    public void The_grip_is_offered_only_when_the_node_has_siblings()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(Spec(), sel);
        cut.Find("[data-cid='a']").Click();
        Assert.Single(cut.FindAll("[data-drag-handle]"));

        var solo = JsonSerializer.Deserialize<CanvasSpec>("""
            { "root": { "id": "root", "type": "stack", "children": [
                { "id": "only", "type": "divider" } ] } }
            """, CanvasJson.Options)!;
        var second = Canvas(solo, new CanvasSelection());
        second.Find("[data-cid='only']").Click();

        Assert.Empty(second.FindAll("[data-drag-handle]"));
    }
}
```

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasReorderTests"`
Expected: Build-Fehler — `OnNodeReorder` existiert nicht. `using Microsoft.AspNetCore.Components.Web;` in der Testdatei ergänzen.

- [ ] **Step 3: `OnNodeReorder` und Alt+Pfeil**

In `DrylCanvas.razor` bei den anderen `[JSInvokable]`-Methoden:

```csharp
    /// <summary>JS bridge: the reorder gesture dropped node <paramref name="id"/> at
    /// <paramref name="index"/> among its siblings. Not part of the component's supported API.</summary>
    [JSInvokable]
    public async Task OnNodeReorder(string id, int index)
    {
        if (Spec?.Root is not { } root) return;
        var node = CanvasTree.Find(root, id);
        var parent = CanvasTree.FindParent(root, id);
        if (node is null || parent?.Children is null) return;
        if (node.Locked || parent.Locked) return;

        var current = parent.Children.IndexOf(node);
        if (index == current || index < 0 || index >= parent.Children.Count) return;

        await RunCommandAsync(id, index < current ? CanvasNodeCommand.MoveUp : CanvasNodeCommand.MoveDown);
    }
```

> `RunCommandAsync` verschiebt um **einen** Slot. Für einen Drop über mehrere Positionen wird
> `Reorder` mit dem Zielindex statt mit einem Delta gebraucht — passe `RunCommandAsync` an,
> indem du den Zielindex optional durchreichst:
>
> ```csharp
> private async Task RunCommandAsync(string id, CanvasNodeCommand command, int? targetIndex = null)
> …
>     _ => Reorder(root, node, command == CanvasNodeCommand.MoveUp ? -1 : 1, targetIndex),
> …
> private bool Reorder(CanvasNode root, CanvasNode node, int delta, int? targetIndex = null)
> {
>     var parent = CanvasTree.FindParent(root, node.Id);
>     if (parent?.Children is null || node.Locked || parent.Locked) return false;
>
>     var index = targetIndex ?? parent.Children.IndexOf(node) + delta;
>     …
> }
> ```
>
> und rufe aus `OnNodeReorder`
> `await RunCommandAsync(id, index < current ? CanvasNodeCommand.MoveUp : CanvasNodeCommand.MoveDown, index);`

In `CanvasNodeView.OnNodeKeyDown` die Pfeilfälle um den Alt-Zweig erweitern:

```csharp
            case "ArrowUp":
                if (e.AltKey) _ = RunCommand(CanvasNodeCommand.MoveUp);
                else Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Previous);
                break;
            case "ArrowDown":
                if (e.AltKey) _ = RunCommand(CanvasNodeCommand.MoveDown);
                else Ctx.Navigate?.Invoke(Node.Id, CanvasNav.Next);
                break;
```

- [ ] **Step 4: Griff in der Werkzeugleiste**

In `CanvasNodeView.razor` als **letztes** Element der `.canvas-node-tools`:

```razor
                    @if (HasSiblings)
                    {
                        <DrylTooltip Text="Reorder element">
                            <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                        Size="DrylButton.ButtonSize.Small"
                                        AriaLabel="Reorder element"
                                        Disabled="Node.Locked"
                                        data-drag-handle="">
                                <DrylIcon Name="GripVertical" Size="14" />
                            </DrylButton>
                        </DrylTooltip>
                    }
```

Dazu im `@code`-Block:

```csharp
    /// <summary>Set by the parent view: a node without siblings has nothing to reorder against.</summary>
    [Parameter] public bool HasSiblings { get; set; }
```

…und in `RenderChildren` (die bestehende Schleife) den Parameter mitgeben:

```csharp
    private RenderFragment RenderChildren => builder =>
    {
        var children = Node.Children;
        var hasSiblings = children is { Count: > 1 };
        var i = 0;
        foreach (var child in children ?? Enumerable.Empty<CanvasNode>())
        {
            builder.OpenComponent<CanvasNodeView>(i++);
            builder.SetKey(child.Id);
            builder.AddComponentParameter(1, nameof(Node), child);
            builder.AddComponentParameter(2, nameof(HasSiblings), hasSiblings);
            builder.CloseComponent();
        }
    };
```

- [ ] **Step 5: `initReorder` in `dryl-canvas.js`**

An `DRYL.Components/wwwroot/js/dryl-canvas.js` anhängen:

```js
// ── Reorder gesture ─────────────────────────────────────────────────────────
// Dragging a node among its siblings is a pointer loop — over a circuit that would be one
// message per frame, so JS owns the gesture and .NET hears exactly one result: the index the
// node was dropped at. The move itself is a normal CanvasOp, which means the existing FLIP
// layer glides every sibling into its new slot (one op, one movement).
//
// Nothing is inserted into or removed from the DOM Blazor owns: the drop marker is a
// data-attribute on a sibling, drawn by CSS.

const _drag = new WeakMap();

// The siblings of `el`: every [data-cid] whose nearest [data-cid] ancestor is the same one.
// Same anchor rule dryl.motion.autoFlip uses, so DrylPresence wrappers in between are irrelevant.
function siblingsOf(root, el) {
    const anchorOf = (node) => {
        for (let n = node.parentElement; n && n !== root; n = n.parentElement)
            if (n.hasAttribute('data-cid')) return n;
        return root;
    };
    const anchor = anchorOf(el);
    return [...root.querySelectorAll('[data-cid]')].filter(n => anchorOf(n) === anchor);
}

function clearMarks(siblings) {
    for (const s of siblings) {
        s.removeAttribute('data-drop-before');
        s.removeAttribute('data-drop-after');
    }
}

export function initReorder(root, dotnet) {
    if (!root || !dotnet || _drag.has(root)) return;

    const state = { dotnet };

    const onDown = (e) => {
        const handle = e.target.closest?.('[data-drag-handle]');
        if (!handle || e.button !== 0) return;
        const el = handle.closest('[data-cid]');
        if (!el || !root.contains(el)) return;

        const siblings = siblingsOf(root, el);
        if (siblings.length < 2) return;

        const from = siblings.indexOf(el);
        const rects = siblings.map(s => s.getBoundingClientRect());
        // Which axis the siblings actually spread along — a stack is vertical, a grid row is not.
        const spreadX = Math.max(...rects.map(r => r.left)) - Math.min(...rects.map(r => r.left));
        const spreadY = Math.max(...rects.map(r => r.top)) - Math.min(...rects.map(r => r.top));
        const vertical = spreadY >= spreadX;

        const g = {
            el, siblings, from, to: from, vertical,
            startX: e.clientX, startY: e.clientY,
            centers: rects.map(r => (vertical ? r.top + r.height / 2 : r.left + r.width / 2)),
        };
        state.g = g;

        el.classList.add('is-dragging');
        handle.setPointerCapture(e.pointerId);
        e.preventDefault();

        const onMove = (ev) => {
            const dx = ev.clientX - g.startX;
            const dy = ev.clientY - g.startY;
            g.el.style.transform = `translate(${dx}px, ${dy}px)`;

            const pointer = g.vertical ? ev.clientY : ev.clientX;
            let to = 0;
            for (let i = 0; i < g.centers.length; i++)
                if (i !== g.from && g.centers[i] < pointer) to++;
            g.to = Math.min(to, g.siblings.length - 1);

            clearMarks(g.siblings);
            const marked = g.siblings[g.to];
            if (marked && marked !== g.el)
                marked.setAttribute(g.to > g.from ? 'data-drop-after' : 'data-drop-before', '');
        };

        const finish = (commit) => {
            window.removeEventListener('pointermove', onMove);
            window.removeEventListener('pointerup', onUp);
            window.removeEventListener('pointercancel', onCancel);
            window.removeEventListener('keydown', onKey);

            g.el.classList.remove('is-dragging');
            g.el.style.transform = '';
            clearMarks(g.siblings);
            state.g = null;

            if (!commit || g.to === g.from) return;
            const cid = g.el.getAttribute('data-cid');
            try { state.dotnet.invokeMethodAsync('OnNodeReorder', cid, g.to)?.catch(() => { }); }
            catch { /* circuit gone */ }
        };

        const onUp = () => finish(true);
        const onCancel = () => finish(false);
        const onKey = (ev) => { if (ev.key === 'Escape') finish(false); };

        window.addEventListener('pointermove', onMove);
        window.addEventListener('pointerup', onUp);
        window.addEventListener('pointercancel', onCancel);
        window.addEventListener('keydown', onKey);
    };

    root.addEventListener('pointerdown', onDown);
    state.onDown = onDown;
    _drag.set(root, state);
}

export function disposeReorder(root) {
    const state = root && _drag.get(root);
    if (!state) return;
    root.removeEventListener('pointerdown', state.onDown);
    _drag.delete(root);
}
```

- [ ] **Step 6: Interop in `DrylCanvas`**

In `OnAfterRenderAsync`, im `firstRender`-Block direkt nach `await _widthModule.InvokeVoidAsync("observe", _bodyEl, _selfRef);`:

```csharp
            // Reorder is opt-in with the selection: a canvas nobody may edit needs no gesture.
            if (Selection is not null)
            {
                await _widthModule.InvokeVoidAsync("initReorder", _bodyEl, _selfRef);
                _reorderAttached = true;
            }
```

Feld dazu: `private bool _reorderAttached;`

In `DisposeAsync`, im `_widthModule is not null`-Block, vor `unobserve`:

```csharp
                if (_reorderAttached) await _widthModule.InvokeVoidAsync("disposeReorder", _bodyEl);
```

- [ ] **Step 7: CSS für Drag und Drop-Marken**

An `DrylCanvas.razor.css` anhängen:

```css
/* The dragged node lifts off its surface and follows the pointer (transform only — no layout
   thrash, no re-render). The glide back into the grid is the FLIP layer's job. */
::deep .canvas-node.is-dragging {
    box-shadow: var(--shadow-lg);
    opacity: .9;
    z-index: 3;
    cursor: grabbing;
}

::deep [data-drag-handle] {
    cursor: grab;
    touch-action: none;
}

/* The insertion marker is drawn, never inserted — JS only sets the attribute, so the DOM
   Blazor owns is never touched from the outside. */
::deep .canvas-node[data-drop-before]::before,
::deep .canvas-node[data-drop-after]::after {
    content: "";
    position: absolute;
    left: 0;
    right: 0;
    height: 2px;
    border-radius: var(--r-pill);
    background: var(--accent-line);
    box-shadow: var(--glow-accent);
}

::deep .canvas-node[data-drop-before]::before { top: calc(var(--sp-1) * -1); }
::deep .canvas-node[data-drop-after]::after { bottom: calc(var(--sp-1) * -1); }
```

- [ ] **Step 8: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasReorderTests|FullyQualifiedName~CanvasNodeToolsTests"`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add DRYL.Components/wwwroot/js/dryl-canvas.js DRYL.Components/Canvas/CanvasNodeView.razor DRYL.Components/Components/AI/DrylCanvas.razor DRYL.Components/Components/AI/DrylCanvas.razor.css tests/DRYL.Components.Tests/Canvas/CanvasReorderTests.cs
git commit -m "feat(canvas): reorder a node among its siblings by drag or Alt+Arrow"
```

---

### Task 7: Dock-Kontext-Chip und `DrylChatComposer.FocusAsync`

**Files:**
- Modify: `DRYL.Components/Components/Surfaces/DrylChatComposer.razor`
- Modify: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor`
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor`
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasDockSelectionTests.cs`

**Interfaces:**
- Consumes: `CanvasSelection`, `CanvasEdit` (Task 1); `DrylCanvas.Selection`/`OnEdit` (Tasks 4–5).
- Produces:
  - `public ValueTask DrylChatComposer.FocusAsync()`
  - `DrylAiCanvas.Selection`, `DrylAiCanvas.OnEdit`
  - `DrylCanvasDock.Selection`

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Agents/Canvas/CanvasDockSelectionTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasDockSelectionTests : BunitContext
{
    public CanvasDockSelectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasNode Chart() => new()
    {
        Id = "c3",
        Type = "lineChart",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "title": "Revenue by month" }"""),
    };

    [Fact]
    public void Without_a_selection_the_dock_shows_no_chip()
    {
        var cut = Render<DrylCanvasDock>();

        Assert.Empty(cut.FindAll(".dock-context"));
    }

    [Fact]
    public void The_chip_names_the_selected_element()
    {
        var sel = new CanvasSelection();
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Selection, sel));

        cut.InvokeAsync(() => sel.Select(Chart()));

        Assert.Contains("Revenue by month", cut.Find(".dock-context").TextContent);
        Assert.Contains("lineChart", cut.Find(".dock-context").TextContent);
    }

    [Fact]
    public void The_chips_clear_button_drops_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Selection, sel));
        cut.InvokeAsync(() => sel.Select(Chart()));

        cut.Find(".dock-context button[aria-label='Clear context']").Click();

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Sending_prefixes_the_text_with_the_element_reference()
    {
        var sel = new CanvasSelection();
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, sel)
            .Add(x => x.OnSend, t => sent = t));
        cut.InvokeAsync(() => sel.Select(Chart()));

        cut.Find("textarea").Change("make it a bar chart");
        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(
            "Regarding the artifact element \"c3\" (lineChart, \"Revenue by month\"):\nmake it a bar chart",
            sent);
    }

    [Fact]
    public void Without_a_selection_the_text_goes_out_unchanged()
    {
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, new CanvasSelection())
            .Add(x => x.OnSend, t => sent = t));

        cut.Find("textarea").Change("build an overview");
        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("build an overview", sent);
    }

    [Fact]
    public void A_prompt_request_expands_a_collapsed_dock()
    {
        var sel = new CanvasSelection();
        var collapsed = true;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, sel)
            .Add(x => x.Collapsed, true)
            .Add(x => x.CollapsedChanged, v => collapsed = v));

        cut.InvokeAsync(() => sel.RequestPrompt());

        Assert.False(collapsed);
    }
}
```

> `using Microsoft.AspNetCore.Components.Web;` ergänzen. Der Sende-Weg des Composers ist
> `Change` + `Enter` — prüfe an `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs`,
> wie der Bestand ihn auslöst, und übernimm exakt dieses Muster.

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasDockSelectionTests"`
Expected: Build-Fehler — `DrylCanvasDock` hat keinen `Selection`-Parameter.

- [ ] **Step 3: `FocusAsync` am Composer**

In `DRYL.Components/Components/Surfaces/DrylChatComposer.razor` bei `SubmitFromJs` ergänzen:

```csharp
    /// <summary>Moves keyboard focus into the composer's input.</summary>
    public async ValueTask FocusAsync()
    {
        try { await _ref.FocusAsync(); }
        catch (JSDisconnectedException) { /* circuit gone */ }
        catch (InvalidOperationException) { /* prerender — no JS */ }
    }
```

- [ ] **Step 4: Durchreichen in `DrylAiCanvas`**

Parameter ergänzen:

```csharp
    /// <summary>Opt-in for direct manipulation — see <c>DrylCanvas.Selection</c>. Share the same
    /// instance with <c>DrylCanvasDock</c> so the selected element travels into the next prompt.</summary>
    [Parameter] public CanvasSelection? Selection { get; set; }

    /// <summary>Raised after every completed direct manipulation — see <c>DrylCanvas.OnEdit</c>.</summary>
    [Parameter] public EventCallback<CanvasEdit> OnEdit { get; set; }
```

…und im Markup an `<DrylCanvas …>` weiterreichen:

```razor
            Selection="Selection"
            OnEdit="OnEdit"
```

- [ ] **Step 5: Chip und Präfix im Dock**

`DrylCanvasDock.razor`: Parameter ergänzen

```csharp
    /// <summary>The canvas's selection. With it the dock shows a context chip for the selected
    /// element and prefixes every prompt with a reference to it, so "make it a bar chart" lands
    /// on the right node. Without it the dock behaves exactly as before.</summary>
    [Parameter] public CanvasSelection? Selection { get; set; }
```

Felder und Abo:

```csharp
    private CanvasSelection? _subscribedSelection;
    private DrylChatComposer? _composer;
    private bool _focusComposer;
```

`OnParametersSet` ergänzen (das bestehende Run-Abo bleibt):

```csharp
        if (!ReferenceEquals(_subscribedSelection, Selection))
        {
            if (_subscribedSelection is not null)
            {
                _subscribedSelection.OnChange -= HandleChange;
                _subscribedSelection.OnPromptRequested -= HandlePromptRequested;
            }
            _subscribedSelection = Selection;
            if (_subscribedSelection is not null)
            {
                _subscribedSelection.OnChange += HandleChange;
                _subscribedSelection.OnPromptRequested += HandlePromptRequested;
            }
        }
```

> Achtung: das bestehende `OnParametersSet` beginnt mit `if (ReferenceEquals(_subscribed, Run)) return;`
> — dieses frühe `return` muss zu einem `if (!ReferenceEquals(...)) { … }`-Block umgebaut werden,
> sonst wird der Selektionsblock nie erreicht.

Handler:

```csharp
    private void HandlePromptRequested() => InvokeAsync(async () =>
    {
        if (Collapsed) await SetCollapsedAsync(false);
        _focusComposer = true;
        StateHasChanged();
    });
```

`OnAfterRenderAsync` am Ende ergänzen:

```csharp
        if (_focusComposer && _composer is not null)
        {
            _focusComposer = false;
            await _composer.FocusAsync();
        }
```

Markup: den Chip direkt **über** dem Composer einfügen und den Composer per `@ref` fassen:

```razor
            <DrylPresence Visible="@(Selection?.HasSelection == true)"
                          Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Fast">
                <div class="dock-context">
                    <DrylIcon Name="Sparkle" Size="12" />
                    <span class="dock-context-label">@Selection?.Label</span>
                    <DrylBadge>@Selection?.Type</DrylBadge>
                    <DrylTooltip Text="@ClearContextLabel">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="@ClearContextLabel"
                                    OnClick="@(() => Selection?.Clear())">
                            <DrylIcon Name="X" Size="12" />
                        </DrylButton>
                    </DrylTooltip>
                </div>
            </DrylPresence>

            <DrylChatComposer @ref="_composer" @bind-Value="_draft" … />
```

`SendAsync` um das Präfix erweitern:

```csharp
    private async Task SendAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _scrollLog = _logOpen;
        await OnSend.InvokeAsync(WithContext(text));
    }

    // The one line that turns "make it a bar chart" into an instruction the model can patch with:
    // the node id it needs for update_artifact, plus type and label so it does not have to guess.
    private string WithContext(string text) =>
        Selection is { HasSelection: true, Id: { } id }
            ? FormattableString.Invariant(
                $"Regarding the artifact element \"{id}\" ({Selection.Type}, \"{Selection.Label}\"):\n{text}")
            : text;

    private const string ClearContextLabel = "Clear context";
```

`DisposeAsync` ergänzen:

```csharp
        if (_subscribedSelection is not null)
        {
            _subscribedSelection.OnChange -= HandleChange;
            _subscribedSelection.OnPromptRequested -= HandlePromptRequested;
        }
```

- [ ] **Step 6: CSS für den Chip**

An `DrylCanvasDock.razor.css` anhängen:

```css
/* The context chip: what the next prompt is about, one line above the input. */
.dock-context {
    display: flex;
    align-items: center;
    gap: var(--sp-2);
    padding: var(--sp-1) var(--sp-2);
    border: 1px solid var(--accent-line);
    border-radius: var(--r-pill);
    background: var(--glass-1);
    color: var(--fg-muted);
    font-size: .82rem;
}

.dock-context-label {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
```

> Prüfe vor dem Schreiben, ob `--glass-1` in dieser Datei schon verwendet wird; falls die Datei
> ihre Flächen anders baut, übernimm ihr Muster statt ein neues einzuführen.

- [ ] **Step 7: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasDockSelectionTests|FullyQualifiedName~DrylCanvasDockTests|FullyQualifiedName~DrylAiCanvasTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/Components/Surfaces/DrylChatComposer.razor DRYL.Components.Agents/Canvas/DrylAiCanvas.razor DRYL.Components.Agents/Canvas/DrylCanvasDock.razor DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css tests/DRYL.Components.Tests/Agents/Canvas/CanvasDockSelectionTests.cs
git commit -m "feat(canvas): the dock carries the selected element into the prompt"
```

---

### Task 8: Der Modellvertrag — `locked` im Prompt und im Receipt

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasPinReceiptTests.cs`

**Interfaces:**
- Consumes: `CanvasPatchAuthor` (Task 2), `DrylCanvasTools.CreateReplay`, `DrylCanvasRun`.
- Produces: keine neue API — zwei Zeilen Prompttext und der Beweis, dass der Receipt sie trägt.

- [ ] **Step 1: Schreibe die fehlschlagenden Tests**

`tests/DRYL.Components.Tests/Agents/Canvas/CanvasPinReceiptTests.cs`:

```csharp
using System.Runtime.CompilerServices;
using System.Text.Json;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// The pin has to reach the model twice: as a rule in the prompt, and — when it tries anyway —
/// as a corrective sentence in the receipt it can act on next turn.
/// </summary>
public class CanvasPinReceiptTests
{
    private static async IAsyncEnumerable<string> Stream(
        string json, [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return json;
        await Task.CompletedTask;
    }

    [Fact]
    public void The_schema_tells_the_model_about_pinned_nodes()
    {
        Assert.Contains("\"locked\": true", CanvasPromptAccessor.Schema);
        Assert.Contains("Never change, move or remove a pinned node", CanvasPromptAccessor.Schema);
    }

    [Fact]
    public void The_update_prompt_repeats_the_rule_next_to_the_ops()
    {
        var prompt = CanvasPromptAccessor.Update("do something", """{"root":{"id":"r","type":"stack"}}""");
        Assert.Contains("pinned by the user", prompt);
    }

    [Fact]
    public async Task An_update_that_targets_a_pinned_node_is_skipped_with_a_reason()
    {
        var run = new DrylCanvasRun();
        var tools = DrylCanvasTools.CreateReplay(run, (_, ct) => Stream("""
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } } ] } }
            """, ct));

        await (Task<string>)tools.CreateArtifact.InvokeAsync(
            new() { ["brief"] = "build it" }, CancellationToken.None)!;

        run.Spec!.Root!.Children![0].Locked = true;

        var patchTools = DrylCanvasTools.CreateReplay(run, (_, ct) => Stream("""
            { "ops": [ { "op": "setProps", "id": "a", "props": { "value": "999" } },
                       { "op": "remove", "id": "a" } ] }
            """, ct));

        var receipt = await (Task<string>)patchTools.UpdateArtifact.InvokeAsync(
            new() { ["brief"] = "change it" }, CancellationToken.None)!;

        Assert.Contains("2 ops skipped", receipt);
        Assert.Contains("pinned by the user", receipt);
        Assert.Equal("1", run.Spec.Root.Children[0].Props!.Value.GetProperty("value").GetString());
        Assert.False(run.Spec.Root.Children[0].Removing);
    }
}
```

> `CanvasPrompt` ist `internal`; das Testprojekt sieht Internals des Agents-Pakets nur, wenn
> `DRYL.Components.Agents.csproj` ein `<InternalsVisibleTo Include="DRYL.Components.Tests" />`
> trägt. Prüfe das zuerst — `tests/.../Agents/Canvas/CanvasPromptTests.cs` greift bereits auf
> `CanvasPrompt` zu, also übernimm exakt dessen Zugriffsmuster und **ersetze**
> `CanvasPromptAccessor.Schema` / `.Update(...)` durch die dort verwendeten Aufrufe
> (vermutlich `CanvasPrompt.SchemaText` und `CanvasPrompt.UpdatePrompt(brief, json)`).
> Die `InvokeAsync`-Signatur der Tools übernimmst du analog aus
> `DrylCanvasToolsUpdateTests.cs` — dort steht das exakte Aufrufmuster für `AITool`.

- [ ] **Step 2: Lauf die Tests, sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasPinReceiptTests"`
Expected: FAIL — der Prompttext fehlt.

- [ ] **Step 3: Ergänze die zwei Prompt-Zeilen**

In `CanvasPrompt.SchemaText`, als letzte Zeile **vor** `Interactive nodes (inputText/…)`:

```text
        - Any node may carry "locked": true — the user pinned it. Never change, move or remove a pinned node, and add nothing to it.
```

In `CanvasPrompt.UpdatePrompt`, im Op-Block direkt nach `Use existing ids; new nodes get fresh unique ids. …`:

```text
            Nodes marked "locked": true are pinned by the user — no op may target them; report that instead of trying.
```

- [ ] **Step 4: Lauf die Tests**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasPinReceiptTests|FullyQualifiedName~CanvasPromptTests|FullyQualifiedName~DrylCanvasToolsUpdateTests"`
Expected: PASS — `CanvasPromptTests` prüft ggf. Prompt-Längen/Inhalte und muss die neue Zeile mittragen; falls dort eine Zeilenzahl fest verdrahtet ist, ziehe sie mit.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasPrompt.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasPinReceiptTests.cs
git commit -m "feat(canvas): teach the artifact generator about pinned nodes"
```

---

### Task 9: Demo und Komponentenkatalog

**Files:**
- Create: `../DRYL.Website/Components/Examples/CanvasWorkspace/Direct.razor`
- Create: `../DRYL.Website/Components/Examples/CanvasWorkspace/Direct.razor.css`
- Modify: `../DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor`
- Modify: `../DRYL.Website/Components/ComponentCatalog.cs`

**Interfaces:**
- Consumes: alles aus den Tasks 1–8.
- Produces: keine Bibliotheks-API.

- [ ] **Step 1: Lies die Vorlage**

Lies `../DRYL.Website/Components/Examples/CanvasWorkspace/Basic.razor` vollständig — die neue Demo
ist deren Zwilling und muss ihr Replay-Muster (`DrylCanvasTools.CreateReplay`, gescriptete
`Generate`-Methode, `_log`, `IAsyncDisposable`) **exakt** übernehmen.

- [ ] **Step 2: Schreibe `Direct.razor`**

Struktur (die gescriptete `Generate`-Methode und die Log-Mechanik aus `Basic.razor` kopieren):

```razor
@using DRYL.Components.Canvas
@implements IAsyncDisposable

@*  Direct manipulation — the artifact is not just watched, it is handled.
    Same replay generator as Basic; what is new is the selection: click an element,
    then pin, duplicate, remove, reorder or prompt about exactly that node.        *@

<div class="ws-demo">
    <p class="direct-hint">
        Click an element to select it — then pin, duplicate, remove or prompt about it.
        Arrow keys walk the artifact, <kbd>Alt</kbd> + <kbd>↑</kbd>/<kbd>↓</kbd> reorders,
        <kbd>Enter</kbd> asks the dock about the selected element.
    </p>

    <DrylCanvasWorkspace Workspace="_ws" ShowHistory Revision="_revision" RevisionLabel="@_revisionLabel">
        <View>
            <DrylAiCanvas Run="_run" AllowExpand="false" Class="ws-demo-surface"
                          Selection="_selection" OnEdit="HandleEdit" />
        </View>
    </DrylCanvasWorkspace>
</div>

<DrylCanvasDock Run="_run" Busy="_busy" OnSend="Send" Selection="_selection" Title="Builder"
                Placeholder="Ask for a view, or select an element first" />
```

```csharp
    private readonly CanvasSelection _selection = new();
    private int _revision;
    private string? _revisionLabel;

    // A user edit is a version like an AI round is — that is what makes undo cover both.
    private void HandleEdit(CanvasEdit edit)
    {
        _revisionLabel = edit.Label;
        _revision++;
    }
```

Zusätzlich, damit die Replay-Variante den Pin **sichtbar** macht: nach dem ersten
`create_artifact` einen Node vorpinnen —

```csharp
    // The demo pins one node up front so the next update round visibly bounces off it.
    private void PinFirstStat()
    {
        if (_run.Spec?.Root?.Children?.FirstOrDefault() is { } first) first.Locked = true;
    }
```

`Direct.razor.css`: nur die Hinweiszeile, alles andere erbt von `ws-demo`:

```css
.direct-hint {
    color: var(--fg-muted);
    margin: 0;
}
```

- [ ] **Step 3: Verdrahte die Demo auf der Seite**

In `DemoCanvasWorkspace.razor` nach dem „Documents and history"-Abschnitt einen neuen Abschnitt
mit `<h3>Direct manipulation</h3>`, zwei bis drei `<p class="lead">`-Absätzen (Selektion,
Pin als Ansage an das Modell, Reorder als eine Bewegung), einem `DrylCodeBlock` mit dem
Razor-Ausschnitt oben und einem `DemoExample Title="Direct manipulation"
Source="CanvasWorkspace/Direct"` einfügen, das `<DRYL.Website.Components.Examples.CanvasWorkspace.Direct />`
rendert. Übernimm Formulierung und Aufbau der bestehenden Abschnitte.

- [ ] **Step 4: Katalog aktualisieren**

In `../DRYL.Website/Components/ComponentCatalog.cs` die Einträge für `DrylCanvas`,
`DrylAiCanvas` und `DrylCanvasDock` suchen und ihre Beschreibungen um die neuen Parameter
ergänzen (`Selection`, `OnEdit` bzw. `Selection`). **Keine neue Komponente** eintragen — Phase 6
liefert keine.

- [ ] **Step 5: Baue und starte die Website**

Run: `dotnet build ../DRYL.Website/DRYL.Website.csproj`
Expected: 0 Warnungen, 0 Fehler.

Dann die Skill `verify` benutzen, um die Seite `/components/canvas-workspace` zu starten und
manuell zu prüfen:
- Element anklicken → Ring + Werkzeugleiste erscheinen (nicht springen).
- Pin setzen → Schloss bleibt sichtbar, Duplizieren/Entfernen deaktiviert.
- Duplizieren → Kopie gleitet ein, ist selektiert.
- Entfernen → Node animiert aus.
- Griff ziehen → Marke erscheint, Drop glidet die Geschwister.
- Chip im Dock zeigt das Element; Senden präfixt (im Log sichtbar).
- **Beide Farbmodi** (`data-dryl-mode` auf `<html>` umschalten), **375 px**, **reduced motion**.

- [ ] **Step 6: Commit**

```bash
git add ../DRYL.Website/Components/Examples/CanvasWorkspace/Direct.razor ../DRYL.Website/Components/Examples/CanvasWorkspace/Direct.razor.css ../DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor ../DRYL.Website/Components/ComponentCatalog.cs
git commit -m "docs(canvas): direct-manipulation demo on the workspace page"
```

---

### Task 10: Release — Changelog, Versionen, Gesamtprüfung

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `DRYL.Components/DRYL.Components.csproj`
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj`

**Interfaces:**
- Consumes: alles.
- Produces: veröffentlichbare Versionen 2.17.0 / 0.14.0.

- [ ] **Step 1: Volltest**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: alle Tests grün. Bei Fehlschlägen zuerst reparieren — kein Release über roten Tests.

- [ ] **Step 2: Light-Sync prüfen**

Run: `node scripts/check-light-sync.mjs`
Expected: grün (es kam kein Token dazu).

- [ ] **Step 3: Versionen setzen**

`DRYL.Components/DRYL.Components.csproj`: `<Version>2.17.0</Version>`
`DRYL.Components.Agents/DRYL.Components.Agents.csproj`: `<Version>0.14.0</Version>`

- [ ] **Step 4: Changelog schneiden**

In `CHANGELOG.md` den `[Unreleased]`-Block zu `## [2.17.0] - 2026-07-26` machen, einen frischen
leeren `[Unreleased]` darüber setzen, und folgende Einträge aufnehmen (Format des Bestands
übernehmen — insbesondere die dort verwendete Trennung zwischen Kern- und Agents-Version):

```markdown
### Added
- `DrylCanvas` — New `Selection` parameter (`CanvasSelection`) turns on direct manipulation: click or keyboard-select a node, with a toolbar to prompt about it, pin, duplicate, remove or reorder it. Off unless a selection is supplied.
- `DrylCanvas` — New `OnEdit` callback (`CanvasEdit`) reports every completed direct manipulation, so a workspace can commit it as a version.
- `CanvasSelection` — New shared selection state for one canvas surface; `CanvasNav`, `CanvasNodeCommand` and `CanvasEdit` come with it.
- `CanvasLabel` — New helper turning a node into its speakable short name.
- `CanvasNodeClone` — New helper duplicating a subtree with fresh ids and field names.
- `CanvasNode` — New `Locked` property (`"locked": true`): a pinned node the AI author may not change, move, remove or add to. Everything the user triggers still goes through.
- `CanvasPatcher.Apply` — New optional `CanvasPatchAuthor` parameter; `CanvasPatchAuthor.Ai` enforces pins and returns a corrective skip reason.
- `DrylChatComposer` — New `FocusAsync()` method.
- `DrylAiCanvas` — New `Selection` and `OnEdit` parameters, forwarded to `DrylCanvas`.
- `DrylCanvasDock` — New `Selection` parameter: a context chip for the selected element, and a one-line element reference prefixed onto every prompt.

### Changed
- `CanvasPrompt` — The generator contract now documents `"locked": true`, so the model skips pinned nodes instead of being corrected afterwards.

### Fixed
- `DrylCanvas` — A removed node is now dropped from the spec even when the host handles no `OnPurge`; it used to linger as an invisible, removing-flagged element.
```

- [ ] **Step 5: Verifiziere den Release-Stand**

Run: `grep -n "<Version>" DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: `2.17.0` und `0.14.0`.

Run: `dotnet build DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj`
Expected: 0 Warnungen, 0 Fehler.

- [ ] **Step 6: Projektnotiz fortschreiben**

Ergänze in `/Users/deryl/.claude/projects/-Users-deryl-Desktop-DRYL-DRYL-Components/memory/` eine
Datei `project-ai-canvas.md` (Typ `project`) mit dem Stand: Phasen 1–6 der Canvas-Roadmap
umgesetzt, Kern 2.17.0 / Agents 0.14.0, Agents damit bereit für 1.0.0; verlinke die Roadmap und
die Phase-6-Spec. Trage sie in `MEMORY.md` ein.

- [ ] **Step 7: Commit**

```bash
git add CHANGELOG.md DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj
git commit -m "chore(release): DRYL.Components 2.17.0 / Agents 0.14.0 — canvas direct manipulation"
```

---

## Self-Review

**Spec-Abdeckung**

| Spec | Task |
| --- | --- |
| §3 `CanvasSelection` | 1 |
| §3.1 `CanvasLabel` | 1 |
| §4.1 Aktivierung, §4.2 Trefferregel, §4.3 Tastatur, §4.4 Ankündigung, §4.5 Lebenszyklus | 4 |
| §5 Werkzeugleiste | 5 (Griff: 6) |
| §6.1 `Locked`, §6.2 Patcher-Regel | 2 |
| §6.3 Pin setzen | 5 |
| §7.1 Duplizieren, §7.2 Entfernen + Purge-Fix | 3 (Clone), 5 (Kommando) |
| §8 Drag-Reorder | 6 |
| §9 Kontext-Chip, §9.1 `FocusAsync` | 7 |
| §10 Prompt-Vertrag | 8 |
| §11 Persistenz | 2 (nur das Feld nötig — kein weiterer Code) |
| §12 Historie/`OnEdit` | 5 (API), 9 (Demo-Verdrahtung) |
| §13 CSS | 4, 5, 6, 7 |
| §14 Tests | in jedem Task |
| §15 Demo | 9 |
| §16 Paketgrenze/Publish | 10 |
| §17 DoD | 10 |

**Offene Punkte, die der Umsetzer prüfen muss** (jeweils im Task benannt): das
`InternalsVisibleTo` des Agents-Pakets für Task 8, das exakte Sende-Muster des Composers in
Task 7, und das Aufrufmuster für `AITool.InvokeAsync` in Task 8.
