# Canvas Phase 5 — Canvas Document Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ein Canvas-Workspace überlebt Reload und kann einen Schritt zurück — `CanvasDocument` (Serialisierung), `CanvasHistory` (Snapshot-Ring pro View), `ICanvasDocumentStore` (Vertrag + In-Memory) plus Undo/Redo/Verlauf und Autosave in `DrylCanvasWorkspace`. Spec: `docs/superpowers/specs/2026-07-26-canvas-document-design.md`.

**Architecture:** Alles im Kern, additiv. Historie ist ein Ringpuffer serialisierter Spec-JSONs pro `CanvasView`; `CanvasWorkspace` bekommt vier Verben darüber; `DrylCanvasWorkspace` rendert sie als Werkzeuggruppe in der View-Leiste und morpht jede Zustandsänderung über `IDrylViewTransition`. Ein Dokument ist derselbe Serialisierungspfad über alle Views. Das Agents-Paket wird **nicht** angefasst.

**Tech Stack:** Blazor (net8/9/10 multi-target), `System.Text.Json` über `CanvasJson.Options`, xUnit + bUnit in `tests/DRYL.Components.Tests`.

## Global Constraints

- Kern `DRYL.Components/DRYL.Components.csproj` `<Version>`: 2.15.0 → **2.16.0** — erst in Task 9, zusammen mit dem Changelog-Release-Schnitt.
- Agents `DRYL.Components.Agents/DRYL.Components.Agents.csproj` bleibt **0.13.0**. Keine Datei unter `DRYL.Components.Agents/` wird geändert.
- Namensraum aller neuen Typen: `DRYL.Components.Canvas`. Komponenten bleiben in `DRYL.Components`.
- Serialisierung ausschließlich über `CanvasJson.Options` (camelCase, case-insensitive).
- Zahlen-Interpolation immer `FormattableString.Invariant` (deutsche Locale!).
- Nur bestehende CSS-Tokens, nur `--dur-fast|med|slow` und `--ease-out|in-out|spring`. Keine neuen Tokens ⇒ `check-light-sync` bleibt unberührt, wird in Task 9 trotzdem gelaufen.
- Icon-only-Buttons brauchen `DrylTooltip` **und** `AriaLabel` (CLAUDE.md 2.9/2.11).
- Öffentliche Typen und `[Parameter]` bekommen XML-Doc-Kommentare.
- UI-Texte englisch (Bibliothekskonvention), Spec- und Plandokumente deutsch.
- Tests laufen während der Entwicklung mit `dotnet test tests/DRYL.Components.Tests -f net10.0`; Task 9 läuft alle Frameworks.
- Commits im Repo-Stil (`feat(canvas): …`, `test(canvas): …`) mit `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.

---

### Task 1: `CanvasHistory` — der Snapshot-Ring

**Files:**
- Create: `DRYL.Components/Canvas/CanvasHistory.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasHistoryTests.cs`

**Interfaces:**
- Consumes: `CanvasSpec`, `CanvasJson.Options` aus `DRYL.Components/Canvas/CanvasSpec.cs`.
- Produces:
  - `public sealed record CanvasHistoryEntry(string Label, DateTimeOffset At, string Json)`
  - `public sealed class CanvasHistory` mit `CanvasHistory(int capacity = 20)`, `int Capacity`, `IReadOnlyList<CanvasHistoryEntry> Entries`, `int Position`, `bool CanUndo`, `bool CanRedo`, `event Action? OnChange`, `bool Record(CanvasSpec? spec, string label)`, `CanvasSpec? Undo()`, `CanvasSpec? Redo()`, `CanvasSpec? Restore(int index)`, `void Clear()`.

- [ ] **Step 1: Failing Tests schreiben** — neue Datei `tests/DRYL.Components.Tests/Canvas/CanvasHistoryTests.cs`:

```csharp
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The snapshot ring behind undo, redo and "back to version N".</summary>
public class CanvasHistoryTests
{
    private static CanvasSpec Spec(string title) =>
        new() { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };

    [Fact]
    public void Record_stores_an_entry_and_notifies()
    {
        var h = new CanvasHistory();
        var changes = 0;
        h.OnChange += () => changes++;

        Assert.True(h.Record(Spec("one"), "created"));

        Assert.Single(h.Entries);
        Assert.Equal("created", h.Entries[0].Label);
        Assert.Equal(0, h.Position);
        Assert.False(h.CanUndo);
        Assert.False(h.CanRedo);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Record_drops_a_snapshot_that_did_not_change_anything()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "created");
        var changes = 0;
        h.OnChange += () => changes++;

        Assert.False(h.Record(Spec("one"), "again"));

        Assert.Single(h.Entries);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Undo_walks_back_and_returns_a_fresh_instance()
    {
        var h = new CanvasHistory();
        var first = Spec("one");
        h.Record(first, "v1");
        h.Record(Spec("two"), "v2");

        var undone = h.Undo();

        Assert.NotNull(undone);
        Assert.Equal("one", undone!.Title);
        Assert.NotSame(first, undone);
        Assert.Equal(0, h.Position);
        Assert.False(h.CanUndo);
        Assert.True(h.CanRedo);
    }

    [Fact]
    public void Redo_walks_forward_again()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Undo();

        var redone = h.Redo();

        Assert.Equal("two", redone!.Title);
        Assert.Equal(1, h.Position);
        Assert.False(h.CanRedo);
    }

    [Fact]
    public void Undo_at_the_start_and_Redo_at_the_end_return_null()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");

        Assert.Null(h.Undo());
        Assert.Null(h.Redo());
        Assert.Equal(0, h.Position);
    }

    [Fact]
    public void Recording_after_an_undo_truncates_the_redo_branch()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");
        h.Undo();
        h.Undo();

        h.Record(Spec("other"), "v2b");

        Assert.Equal(2, h.Entries.Count);
        Assert.Equal("v1", h.Entries[0].Label);
        Assert.Equal("v2b", h.Entries[1].Label);
        Assert.Equal(1, h.Position);
        Assert.False(h.CanRedo);
    }

    [Fact]
    public void The_ring_drops_the_oldest_entry_when_it_overflows()
    {
        var h = new CanvasHistory(capacity: 3);
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");
        h.Record(Spec("four"), "v4");

        Assert.Equal(3, h.Entries.Count);
        Assert.Equal("v2", h.Entries[0].Label);
        Assert.Equal(2, h.Position);
    }

    [Fact]
    public void Capacity_is_clamped_to_a_sane_range()
    {
        Assert.Equal(2, new CanvasHistory(0).Capacity);
        Assert.Equal(200, new CanvasHistory(5000).Capacity);
    }

    [Fact]
    public void Restore_jumps_to_any_index_without_dropping_entries()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");

        var restored = h.Restore(0);

        Assert.Equal("one", restored!.Title);
        Assert.Equal(0, h.Position);
        Assert.Equal(3, h.Entries.Count);
        Assert.True(h.CanRedo);
        Assert.Null(h.Restore(9));
        Assert.Null(h.Restore(-1));
    }

    [Fact]
    public void An_empty_spec_is_a_legitimate_snapshot()
    {
        var h = new CanvasHistory();
        h.Record(null, "cleared");
        h.Record(Spec("one"), "v1");

        Assert.Null(h.Undo());   // "null" spec deserializes back to null
        Assert.Equal(0, h.Position);
    }

    [Fact]
    public void Clear_empties_the_ring()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");

        h.Clear();

        Assert.Empty(h.Entries);
        Assert.Equal(-1, h.Position);
        Assert.False(h.CanUndo);
    }
}
```

- [ ] **Step 2: Tests laufen lassen — sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasHistoryTests`
Expected: Compile-Fehler „The type or namespace name 'CanvasHistory' could not be found".

- [ ] **Step 3: `CanvasHistory` implementieren** — neue Datei `DRYL.Components/Canvas/CanvasHistory.cs`:

```csharp
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>One recorded state of a view: what it was called, when it was taken, and the spec
/// as JSON. Storing the serialized form (not the tree) is what makes an entry immutable and
/// makes "did anything actually change?" a string comparison.</summary>
/// <param name="Label">Human-readable name of the step that produced this state.</param>
/// <param name="At">When the snapshot was taken (UTC).</param>
/// <param name="Json">The serialized <see cref="CanvasSpec"/>, or <c>"null"</c> for an empty view.</param>
public sealed record CanvasHistoryEntry(string Label, DateTimeOffset At, string Json);

/// <summary>
/// The version history of one canvas view — a bounded ring of snapshots with a cursor.
/// </summary>
/// <remarks>
/// There is no op log in the canvas: patches mutate the spec in place and a fresh generation
/// replaces it wholesale. A snapshot ring covers both paths with one mechanism, and the entry
/// it stores is exactly what a document persists.
/// Renderer-thread state like <c>CanvasWorkspace</c> — no locking.
/// </remarks>
public sealed class CanvasHistory
{
    private readonly List<CanvasHistoryEntry> _entries = new();

    /// <summary>Creates a history that keeps at most <paramref name="capacity"/> snapshots.</summary>
    /// <param name="capacity">Ring size; clamped to 2…200.</param>
    public CanvasHistory(int capacity = 20) => Capacity = Math.Clamp(capacity, 2, 200);

    /// <summary>How many snapshots the ring keeps before it drops the oldest.</summary>
    public int Capacity { get; }

    /// <summary>The snapshots, oldest first.</summary>
    public IReadOnlyList<CanvasHistoryEntry> Entries => _entries;

    /// <summary>Index of the snapshot currently shown, or -1 while the history is empty.</summary>
    public int Position { get; private set; } = -1;

    /// <summary>True when there is an earlier snapshot to go back to.</summary>
    public bool CanUndo => Position > 0;

    /// <summary>True when an undo can still be taken back.</summary>
    public bool CanRedo => Position >= 0 && Position < _entries.Count - 1;

    /// <summary>Raised after every change to the entries or the cursor.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Records the current state. A snapshot identical to the one at <see cref="Position"/> is
    /// dropped — a round that changed nothing must not fill the ring. Recording after an undo
    /// truncates the redo branch.
    /// </summary>
    /// <returns>True when an entry was added.</returns>
    public bool Record(CanvasSpec? spec, string label)
    {
        var json = JsonSerializer.Serialize(spec, CanvasJson.Options);
        if (Position >= 0 && _entries[Position].Json == json) return false;

        if (Position < _entries.Count - 1)
            _entries.RemoveRange(Position + 1, _entries.Count - Position - 1);

        _entries.Add(new CanvasHistoryEntry(label, DateTimeOffset.UtcNow, json));
        if (_entries.Count > Capacity) _entries.RemoveAt(0);
        Position = _entries.Count - 1;

        OnChange?.Invoke();
        return true;
    }

    /// <summary>Steps one snapshot back. Null when there is nothing earlier.</summary>
    public CanvasSpec? Undo() => CanUndo ? Move(Position - 1) : null;

    /// <summary>Steps one snapshot forward. Null when there is nothing later.</summary>
    public CanvasSpec? Redo() => CanRedo ? Move(Position + 1) : null;

    /// <summary>Jumps to any snapshot ("back to version N"). Null when the index is unknown.</summary>
    public CanvasSpec? Restore(int index) =>
        index < 0 || index >= _entries.Count ? null : Move(index);

    /// <summary>Drops every snapshot.</summary>
    public void Clear()
    {
        if (_entries.Count == 0 && Position < 0) return;
        _entries.Clear();
        Position = -1;
        OnChange?.Invoke();
    }

    // Always a fresh tree: the caller mounts it into a live view and will mutate it.
    private CanvasSpec? Move(int index)
    {
        Position = index;
        OnChange?.Invoke();
        return JsonSerializer.Deserialize<CanvasSpec>(_entries[index].Json, CanvasJson.Options);
    }
}
```

- [ ] **Step 4: Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasHistoryTests`
Expected: PASS (11 Tests).

- [ ] **Step 5: Committen**

```bash
git add DRYL.Components/Canvas/CanvasHistory.cs tests/DRYL.Components.Tests/Canvas/CanvasHistoryTests.cs
git commit -m "feat(canvas): CanvasHistory — snapshot ring for undo/redo"
```

---

### Task 2: Historie an View und Workspace anbinden

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasWorkspace.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceHistoryTests.cs`

**Interfaces:**
- Consumes: `CanvasHistory` aus Task 1.
- Produces:
  - `CanvasView.History` → `public CanvasHistory History { get; } = new();`
  - `CanvasWorkspace.Commit(string label)` → `bool`
  - `CanvasWorkspace.Undo()` / `Redo()` → `bool`
  - `CanvasWorkspace.RestoreVersion(int index)` → `bool`
  - `CanvasWorkspace.CanUndo` / `CanRedo` → `bool`

- [ ] **Step 1: Failing Tests schreiben** — neue Datei `tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceHistoryTests.cs`:

```csharp
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Undo/redo as the workspace exposes it: always about the active view.</summary>
public class CanvasWorkspaceHistoryTests
{
    private static CanvasSpec Spec(string title) =>
        new() { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };

    [Fact]
    public void Commit_records_the_active_view_and_notifies()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Commit("created"));

        Assert.Single(view.History.Entries);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Commit_without_an_active_view_does_nothing()
    {
        var ws = new CanvasWorkspace();
        Assert.False(ws.Commit("created"));
        Assert.False(ws.CanUndo);
        Assert.False(ws.Undo());
    }

    [Fact]
    public void Commit_of_an_unchanged_spec_is_dropped()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");

        Assert.False(ws.Commit("v1 again"));
        Assert.Single(view.History.Entries);
    }

    [Fact]
    public void Undo_puts_the_previous_spec_back_on_the_view()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");
        view.Spec = Spec("two");
        ws.Commit("v2");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Undo());

        Assert.Equal("one", view.Spec!.Title);
        Assert.Equal(1, changes);
        Assert.True(ws.CanRedo);

        Assert.True(ws.Redo());
        Assert.Equal("two", view.Spec!.Title);
    }

    [Fact]
    public void RestoreVersion_jumps_to_a_named_entry()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");
        view.Spec = Spec("two");
        ws.Commit("v2");
        view.Spec = Spec("three");
        ws.Commit("v3");

        Assert.True(ws.RestoreVersion(0));
        Assert.Equal("one", view.Spec!.Title);
        Assert.False(ws.RestoreVersion(42));
    }

    [Fact]
    public void Each_view_keeps_its_own_history()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        a.Spec = Spec("a1");
        ws.Commit("a1");
        var b = ws.Open("B");          // Open activates B
        b.Spec = Spec("b1");
        ws.Commit("b1");

        Assert.Single(a.History.Entries);
        Assert.Single(b.History.Entries);
        Assert.False(ws.CanUndo);      // B has one entry only

        ws.Activate(a.Id);
        Assert.False(ws.CanUndo);
    }
}
```

- [ ] **Step 2: Tests laufen lassen — sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasWorkspaceHistoryTests`
Expected: Compile-Fehler „'CanvasWorkspace' does not contain a definition for 'Commit'".

- [ ] **Step 3: `CanvasView` erweitern** — in `DRYL.Components/Canvas/CanvasWorkspace.cs`, direkt nach der `Spec`-Property von `CanvasView`:

```csharp
    /// <summary>This view's version history — snapshots taken by <see cref="CanvasWorkspace.Commit"/>.</summary>
    public CanvasHistory History { get; } = new();
```

- [ ] **Step 4: Die vier Verben ergänzen** — in `DRYL.Components/Canvas/CanvasWorkspace.cs`, hinter `Clear()` und vor der privaten `Neighbour`-Methode:

```csharp
    /// <summary>True when the active view has an earlier snapshot to go back to.</summary>
    public bool CanUndo => Active?.History.CanUndo == true;

    /// <summary>True when the active view's undo can be taken back.</summary>
    public bool CanRedo => Active?.History.CanRedo == true;

    /// <summary>
    /// Records the active view's current spec as a version. A snapshot that changed nothing is
    /// dropped, so committing generously is free.
    /// </summary>
    /// <param name="label">What produced this state, e.g. the prompt that was sent.</param>
    /// <returns>True when a version was recorded.</returns>
    public bool Commit(string label)
    {
        if (Active is not { } view) return false;
        if (!view.History.Record(view.Spec, label)) return false;

        OnChange?.Invoke();
        return true;
    }

    /// <summary>Puts the active view's previous version back. False when there is none.</summary>
    public bool Undo() => Apply(v => v.History.Undo());

    /// <summary>Takes the last undo back. False when there is nothing to redo.</summary>
    public bool Redo() => Apply(v => v.History.Redo());

    /// <summary>Puts version <paramref name="index"/> of the active view back. False when unknown.</summary>
    public bool RestoreVersion(int index) => Apply(v => v.History.Restore(index));

    // The three history verbs differ only in which snapshot they ask for. A move that the
    // history refused leaves the view — and the cursor — exactly where it was.
    private bool Apply(Func<CanvasView, CanvasSpec?> move)
    {
        if (Active is not { } view) return false;

        var before = view.History.Position;
        var spec = move(view);
        if (view.History.Position == before) return false;

        view.Spec = spec;
        OnChange?.Invoke();
        return true;
    }
```

- [ ] **Step 5: Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasWorkspace`
Expected: PASS — sowohl `CanvasWorkspaceHistoryTests` (6) als auch die bestehenden `CanvasWorkspaceTests`.

- [ ] **Step 6: Committen**

```bash
git add DRYL.Components/Canvas/CanvasWorkspace.cs tests/DRYL.Components.Tests/Canvas/CanvasWorkspaceHistoryTests.cs
git commit -m "feat(canvas): per-view history on CanvasWorkspace (commit/undo/redo/restore)"
```

---

### Task 3: `CanvasDocument` — Serialisierung eines ganzen Workspace

**Files:**
- Create: `DRYL.Components/Canvas/CanvasDocument.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasDocumentTests.cs`

**Interfaces:**
- Consumes: `CanvasWorkspace`, `CanvasView`, `CanvasSpec`, `CanvasNode`, `CanvasFormState`, `CanvasJson.Options`, `CanvasCatalog.IsInteractive`.
- Produces:
  - `public sealed class CanvasDocumentView { string Id; string Title; string? Icon; CanvasSpec? Spec }`
  - `public sealed class CanvasDocument` mit `const int CurrentSchema = 1`, `int Schema`, `string? Id`, `string? Title`, `DateTimeOffset SavedAt`, `List<CanvasDocumentView>? Views`, `string? ActiveId`
  - `static CanvasDocument Capture(CanvasWorkspace workspace, string? title = null, CanvasFormState? form = null)`
  - `void Restore(CanvasWorkspace workspace)`
  - `string ToJson()`
  - `static bool TryFromJson(string json, out CanvasDocument? document, out string? error)`
  - `CanvasDocument AsTemplate(string title)`

**Hinweis zum Falten der Feldwerte:** interaktive Nodes seeden ihren Wert aus dem `value`-Prop und nur, solange das Feld leer ist (`CanvasNodeView.SeedFormOnce`). Ein Feldstand wird deshalb als `value`-Prop in den gespeicherten Spec geschrieben — beim Laden füllt sich das Formular dann von allein.

- [ ] **Step 1: Failing Tests schreiben** — neue Datei `tests/DRYL.Components.Tests/Canvas/CanvasDocumentTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>A dashboard that survives a reload: capture, serialize, restore.</summary>
public class CanvasDocumentTests
{
    private static CanvasSpec Spec(string title, string nodeId = "r") =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""
            { "title": "{{title}}", "root": { "id": "{{nodeId}}", "type": "stack", "children": [
                { "id": "{{nodeId}}-c", "type": "lineChart",
                  "data": { "source": "sales.byMonth", "params": { "year": 2026 } } }
            ] } }
            """,
            CanvasJson.Options)!;

    private static CanvasWorkspace TwoViews()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Overview", "Chart");
        a.Spec = Spec("Overview", "a");
        var b = ws.Open("Order 4711");
        b.Spec = Spec("Order 4711", "b");
        ws.Activate(a.Id);
        return ws;
    }

    [Fact]
    public void Capture_takes_every_view_the_active_id_and_a_title()
    {
        var doc = CanvasDocument.Capture(TwoViews(), "My dashboard");

        Assert.Equal(CanvasDocument.CurrentSchema, doc.Schema);
        Assert.Equal("My dashboard", doc.Title);
        Assert.Equal(2, doc.Views!.Count);
        Assert.Equal("overview", doc.ActiveId);
        Assert.Equal("Chart", doc.Views[0].Icon);
        Assert.NotEqual(default, doc.SavedAt);
    }

    [Fact]
    public void Capture_defaults_the_title_to_the_active_view()
    {
        Assert.Equal("Overview", CanvasDocument.Capture(TwoViews()).Title);
        Assert.Equal("Canvas", CanvasDocument.Capture(new CanvasWorkspace()).Title);
    }

    [Fact]
    public void Capture_is_a_deep_copy()
    {
        var ws = TwoViews();
        var doc = CanvasDocument.Capture(ws);

        ws.Active!.Spec!.Title = "changed live";

        Assert.Equal("Overview", doc.Views![0].Spec!.Title);
    }

    [Fact]
    public void Capture_skips_a_view_that_is_on_its_way_out()
    {
        var ws = TwoViews();
        ws.Close("order-4711");

        var doc = CanvasDocument.Capture(ws);

        Assert.Single(doc.Views!);
        Assert.Equal("overview", doc.Views![0].Id);
    }

    [Fact]
    public void A_roundtrip_keeps_views_data_bindings_and_the_active_view()
    {
        var json = CanvasDocument.Capture(TwoViews(), "My dashboard").ToJson();

        Assert.True(CanvasDocument.TryFromJson(json, out var doc, out var error));
        Assert.Null(error);
        Assert.Equal("My dashboard", doc!.Title);
        Assert.Equal("overview", doc.ActiveId);
        Assert.Equal("sales.byMonth", doc.Views![0].Spec!.Root!.Children![0].Data!.Source);
    }

    [Fact]
    public void Restore_rebuilds_the_workspace()
    {
        var doc = CanvasDocument.Capture(TwoViews(), "My dashboard");
        var target = new CanvasWorkspace();
        target.Open("Stale");

        doc.Restore(target);

        Assert.Equal(2, target.Views.Count);
        Assert.Equal("overview", target.ActiveId);
        Assert.Equal("Order 4711", target.Views[1].Title);
        Assert.Equal("Chart", target.Views[0].Icon);
    }

    [Fact]
    public void Restore_hands_the_workspace_its_own_spec_instances()
    {
        var doc = CanvasDocument.Capture(TwoViews());
        var target = new CanvasWorkspace();
        doc.Restore(target);

        target.Active!.Spec!.Title = "changed live";

        Assert.NotEqual("changed live", doc.Views![0].Spec!.Title);
    }

    [Fact]
    public void TryFromJson_rejects_garbage()
    {
        Assert.False(CanvasDocument.TryFromJson("not json", out var doc, out var error));
        Assert.Null(doc);
        Assert.Contains("not valid JSON", error);
    }

    [Fact]
    public void TryFromJson_rejects_a_document_from_a_newer_build()
    {
        var json = $$"""{ "schema": {{CanvasDocument.CurrentSchema + 1}}, "views": [] }""";

        Assert.False(CanvasDocument.TryFromJson(json, out _, out var error));
        Assert.Contains("newer version of DRYL", error);
    }

    [Fact]
    public void TryFromJson_rejects_a_document_without_schema_or_views()
    {
        Assert.False(CanvasDocument.TryFromJson("""{ "views": [] }""", out _, out var noSchema));
        Assert.Contains("no schema version", noSchema);

        Assert.False(CanvasDocument.TryFromJson("""{ "schema": 1 }""", out _, out var noViews));
        Assert.Contains("no views", noViews);
    }

    [Fact]
    public void AsTemplate_drops_the_id_and_takes_a_new_title()
    {
        var doc = CanvasDocument.Capture(TwoViews());
        doc.Id = "abc";

        var template = doc.AsTemplate("Copy of my dashboard");

        Assert.Null(template.Id);
        Assert.Equal("Copy of my dashboard", template.Title);
        Assert.Equal(default, template.SavedAt);
        Assert.Equal(2, template.Views!.Count);
        Assert.NotSame(doc.Views![0].Spec, template.Views[0].Spec);
    }

    [Fact]
    public void Capture_folds_live_field_values_into_the_value_props()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Form");
        view.Spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "root": { "id": "r", "type": "stack", "children": [
                { "id": "t", "type": "inputText", "props": { "name": "customer", "label": "Kunde", "value": "old" } },
                { "id": "s", "type": "select",    "props": { "name": "status", "options": ["a", "b"] } },
                { "id": "l", "type": "slider",    "props": { "name": "amount", "min": 0, "max": 10 } },
                { "id": "g", "type": "toggle",    "props": { "name": "rush", "label": "Eilt" } }
            ] } }
            """, CanvasJson.Options)!;

        var form = new CanvasFormState();
        form.Set("customer", "ACME");
        form.Set("status", "b");
        form.Set("amount", 7d);
        form.Set("rush", true);

        var doc = CanvasDocument.Capture(ws, form: form);
        var children = doc.Views![0].Spec!.Root!.Children!;

        Assert.Equal("ACME", children[0].Props!.Value.GetProperty("value").GetString());
        Assert.Equal("b", children[1].Props!.Value.GetProperty("value").GetString());
        Assert.Equal(7d, children[2].Props!.Value.GetProperty("value").GetDouble());
        Assert.True(children[3].Props!.Value.GetProperty("value").GetBoolean());
        Assert.Equal("Kunde", children[0].Props!.Value.GetProperty("label").GetString());
    }
}
```

- [ ] **Step 2: Tests laufen lassen — sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasDocumentTests`
Expected: Compile-Fehler „The type or namespace name 'CanvasDocument' could not be found".

- [ ] **Step 3: `CanvasDocument` implementieren** — neue Datei `DRYL.Components/Canvas/CanvasDocument.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>One view inside a <see cref="CanvasDocument"/>: what a workspace chip needs to come back.</summary>
public sealed class CanvasDocumentView
{
    /// <summary>The view's stable id (the slug of its title).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The label shown on the chip.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional <c>DrylIcon</c> name shown left of the title.</summary>
    public string? Icon { get; set; }

    /// <summary>The artifact this view showed.</summary>
    public CanvasSpec? Spec { get; set; }
}

/// <summary>
/// A saved <see cref="CanvasWorkspace"/> — every named view with its artifact, plus which one was
/// open. This is what makes a dashboard survive a reload, a user switch and a deployment.
/// </summary>
/// <remarks>
/// Data bindings travel with the document; the numbers do not. A restored document asks the
/// host's registered sources for fresh values, so a document is never a stale copy of a
/// database (A2/A3).
/// </remarks>
public sealed class CanvasDocument
{
    /// <summary>The document schema this build writes and is able to read.</summary>
    public const int CurrentSchema = 1;

    /// <summary>Schema version of this document.</summary>
    public int Schema { get; set; } = CurrentSchema;

    /// <summary>Store key; null until an <see cref="ICanvasDocumentStore"/> has saved it.</summary>
    public string? Id { get; set; }

    /// <summary>Human-readable document title.</summary>
    public string? Title { get; set; }

    /// <summary>When this snapshot was taken (UTC).</summary>
    public DateTimeOffset SavedAt { get; set; }

    /// <summary>The views, in the order they were opened.</summary>
    public List<CanvasDocumentView>? Views { get; set; }

    /// <summary>Id of the view that was open.</summary>
    public string? ActiveId { get; set; }

    /// <summary>
    /// Takes a snapshot of <paramref name="workspace"/>. Views that are already closing are left
    /// out — what is animating away does not belong in a document.
    /// </summary>
    /// <param name="workspace">The workspace to capture.</param>
    /// <param name="title">Document title; defaults to the active view's title.</param>
    /// <param name="form">Live field values to fold into the active view's <c>value</c> props,
    /// so a restored document shows what the user had typed. Optional.</param>
    public static CanvasDocument Capture(CanvasWorkspace workspace,
                                         string? title = null,
                                         CanvasFormState? form = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var views = new List<CanvasDocumentView>();
        foreach (var view in workspace.Views)
        {
            if (view.Removing) continue;

            var spec = Copy(view.Spec);
            if (form is not null && view.Id == workspace.ActiveId && spec?.Root is not null)
                FoldFields(spec.Root, form);

            views.Add(new CanvasDocumentView
            {
                Id = view.Id, Title = view.Title, Icon = view.Icon, Spec = spec,
            });
        }

        return new CanvasDocument
        {
            Title = title ?? workspace.Active?.Title ?? "Canvas",
            SavedAt = DateTimeOffset.UtcNow,
            Views = views,
            ActiveId = views.Any(v => v.Id == workspace.ActiveId) ? workspace.ActiveId : views.FirstOrDefault()?.Id,
        };
    }

    /// <summary>
    /// Replaces everything in <paramref name="workspace"/> with this document. Each view gets its
    /// own spec instance, so the live workspace and the document can never drift into each other.
    /// </summary>
    public void Restore(CanvasWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        workspace.Clear();
        foreach (var v in Views ?? new List<CanvasDocumentView>())
            workspace.Open(v.Title, v.Icon).Spec = Copy(v.Spec);

        if (ActiveId is not null) workspace.Activate(ActiveId);
    }

    /// <summary>Serializes the document for an <see cref="ICanvasDocumentStore"/>.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, CanvasJson.Options);

    /// <summary>
    /// Reads a document written by <see cref="ToJson"/>. The only supported entry point — it is
    /// where the schema gate lives.
    /// </summary>
    /// <param name="json">The stored document.</param>
    /// <param name="document">The parsed document, or null on failure.</param>
    /// <param name="error">A host-facing message explaining the refusal, or null on success.</param>
    public static bool TryFromJson(string json, out CanvasDocument? document, out string? error)
    {
        document = null;
        error = null;

        CanvasDocument? parsed;
        try { parsed = JsonSerializer.Deserialize<CanvasDocument>(json, CanvasJson.Options); }
        catch (JsonException) { parsed = null; }

        if (parsed is null)
        {
            error = "The document could not be read: it is not valid JSON.";
            return false;
        }

        if (parsed.Schema <= 0)
        {
            error = "The document has no schema version.";
            return false;
        }

        if (parsed.Schema > CurrentSchema)
        {
            error = FormattableString.Invariant(
                $"This document was written by a newer version of DRYL (schema {parsed.Schema}, this build reads up to {CurrentSchema}).");
            return false;
        }

        if (parsed.Views is not { Count: > 0 })
        {
            error = "The document contains no views.";
            return false;
        }

        document = parsed;
        return true;
    }

    /// <summary>
    /// A copy of this document as the starting point of a new one: same views, no store id, new
    /// title. That is how an application ships its standard dashboards.
    /// </summary>
    public CanvasDocument AsTemplate(string title) => new()
    {
        Schema = Schema,
        Id = null,
        Title = title,
        SavedAt = default,
        ActiveId = ActiveId,
        Views = (Views ?? new List<CanvasDocumentView>())
            .Select(v => new CanvasDocumentView { Id = v.Id, Title = v.Title, Icon = v.Icon, Spec = Copy(v.Spec) })
            .ToList(),
    };

    // A round trip is the cheapest deep copy that also proves the spec survives serialization.
    private static CanvasSpec? Copy(CanvasSpec? spec) =>
        spec is null ? null
            : JsonSerializer.Deserialize<CanvasSpec>(JsonSerializer.Serialize(spec, CanvasJson.Options), CanvasJson.Options);

    // Writes the live field values into the nodes' `value` props. Interactive nodes seed their
    // form value from that prop (CanvasNodeView.SeedFormOnce), so a restored document fills its
    // own form without a single line in the loading path.
    private static void FoldFields(CanvasNode node, CanvasFormState form)
    {
        if (CanvasCatalog.IsInteractive(node.Type) && node.Props is { } props
            && props.ValueKind == JsonValueKind.Object
            && props.TryGetProperty("name", out var nameProp)
            && nameProp.GetString() is { Length: > 0 } name
            && form.Get(name) is { } value)
        {
            var obj = JsonNode.Parse(props.GetRawText())!.AsObject();
            obj["value"] = value switch
            {
                bool b => JsonValue.Create(b),
                double d => JsonValue.Create(d),
                int i => JsonValue.Create((double)i),
                _ => JsonValue.Create(value.ToString()),
            };
            node.Props = JsonSerializer.Deserialize<JsonElement>(obj.ToJsonString(), CanvasJson.Options);
        }

        foreach (var child in node.Children ?? new List<CanvasNode>())
            FoldFields(child, form);
    }
}
```

- [ ] **Step 4: `CanvasCatalog.IsInteractive` prüfen**

Run: `grep -n "IsInteractive" DRYL.Components/Canvas/CanvasCatalog.cs`
Expected: eine `public static bool IsInteractive(string type)`. Ist sie `internal`, dann in Step 3 stattdessen die vier Typen direkt prüfen: `node.Type is "inputText" or "select" or "slider" or "toggle"`.

- [ ] **Step 5: Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasDocumentTests`
Expected: PASS (12 Tests).

- [ ] **Step 6: Committen**

```bash
git add DRYL.Components/Canvas/CanvasDocument.cs tests/DRYL.Components.Tests/Canvas/CanvasDocumentTests.cs
git commit -m "feat(canvas): CanvasDocument — serialize and restore a whole workspace"
```

---

### Task 4: `ICanvasDocumentStore`, In-Memory-Implementierung und DI

**Files:**
- Create: `DRYL.Components/Canvas/CanvasDocumentStore.cs`
- Modify: `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs` (ans Ende der Klasse)
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasDocumentStoreTests.cs`

**Interfaces:**
- Consumes: `CanvasDocument` aus Task 3.
- Produces:
  - `public sealed record CanvasDocumentInfo(string Id, string Title, DateTimeOffset SavedAt, int ViewCount)`
  - `public interface ICanvasDocumentStore` mit `Task<string> SaveAsync(CanvasDocument, CancellationToken)`, `Task<CanvasDocument?> LoadAsync(string, CancellationToken)`, `Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken)`, `Task DeleteAsync(string, CancellationToken)`
  - `public sealed class InMemoryCanvasDocumentStore : ICanvasDocumentStore`
  - `IServiceCollection.AddDrylCanvasDocumentStore()` und `AddDrylCanvasDocumentStore<TStore>()`

- [ ] **Step 1: Failing Tests schreiben** — neue Datei `tests/DRYL.Components.Tests/Canvas/CanvasDocumentStoreTests.cs`:

```csharp
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The persistence contract DRYL ships — the host owns the database (A7).</summary>
public class CanvasDocumentStoreTests
{
    private static CanvasDocument Doc(string title)
    {
        var ws = new CanvasWorkspace();
        ws.Open(title).Spec = new CanvasSpec { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };
        return CanvasDocument.Capture(ws, title);
    }

    [Fact]
    public async Task Save_mints_an_id_and_writes_it_back()
    {
        var store = new InMemoryCanvasDocumentStore();
        var doc = Doc("Overview");

        var id = await store.SaveAsync(doc);

        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(id, doc.Id);
    }

    [Fact]
    public async Task Save_with_a_known_id_overwrites()
    {
        var store = new InMemoryCanvasDocumentStore();
        var doc = Doc("Overview");
        var id = await store.SaveAsync(doc);

        doc.Title = "Renamed";
        var again = await store.SaveAsync(doc);

        Assert.Equal(id, again);
        Assert.Single(await store.ListAsync());
        Assert.Equal("Renamed", (await store.LoadAsync(id))!.Title);
    }

    [Fact]
    public async Task Load_returns_a_document_that_the_caller_cannot_mutate_in_the_store()
    {
        var store = new InMemoryCanvasDocumentStore();
        var id = await store.SaveAsync(Doc("Overview"));

        var loaded = await store.LoadAsync(id);
        loaded!.Title = "tampered";

        Assert.Equal("Overview", (await store.LoadAsync(id))!.Title);
    }

    [Fact]
    public async Task List_returns_the_newest_first()
    {
        var store = new InMemoryCanvasDocumentStore();
        var older = Doc("Older");
        older.SavedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await store.SaveAsync(older);
        await store.SaveAsync(Doc("Newer"));

        var list = await store.ListAsync();

        Assert.Equal(2, list.Count);
        Assert.Equal("Newer", list[0].Title);
        Assert.Equal(1, list[0].ViewCount);
    }

    [Fact]
    public async Task Load_of_an_unknown_id_is_null_and_Delete_is_silent()
    {
        var store = new InMemoryCanvasDocumentStore();
        Assert.Null(await store.LoadAsync("nope"));
        await store.DeleteAsync("nope");
    }

    [Fact]
    public async Task Delete_removes_the_document()
    {
        var store = new InMemoryCanvasDocumentStore();
        var id = await store.SaveAsync(Doc("Overview"));

        await store.DeleteAsync(id);

        Assert.Null(await store.LoadAsync(id));
        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public void AddDrylCanvasDocumentStore_registers_the_in_memory_store_as_a_singleton()
    {
        var provider = new ServiceCollection().AddDrylCanvasDocumentStore().BuildServiceProvider();

        var store = provider.GetService<ICanvasDocumentStore>();

        Assert.IsType<InMemoryCanvasDocumentStore>(store);
        Assert.Same(store, provider.GetService<ICanvasDocumentStore>());
    }

    [Fact]
    public void A_host_store_registered_first_wins()
    {
        var provider = new ServiceCollection()
            .AddDrylCanvasDocumentStore<HostStore>()
            .AddDrylCanvasDocumentStore()
            .BuildServiceProvider();

        Assert.IsType<HostStore>(provider.GetService<ICanvasDocumentStore>());
    }

    private sealed class HostStore : ICanvasDocumentStore
    {
        public Task<string> SaveAsync(CanvasDocument d, CancellationToken ct = default) => Task.FromResult("x");
        public Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default) => Task.FromResult<CanvasDocument?>(null);
        public Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CanvasDocumentInfo>>(Array.Empty<CanvasDocumentInfo>());
        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Tests laufen lassen — sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasDocumentStoreTests`
Expected: Compile-Fehler „The type or namespace name 'ICanvasDocumentStore' could not be found".

- [ ] **Step 3: Store implementieren** — neue Datei `DRYL.Components/Canvas/CanvasDocumentStore.cs`:

```csharp
using System.Collections.Concurrent;

namespace DRYL.Components.Canvas;

/// <summary>What a document listing shows without loading the artifact itself.</summary>
/// <param name="Id">Store key.</param>
/// <param name="Title">Document title.</param>
/// <param name="SavedAt">When it was last written (UTC).</param>
/// <param name="ViewCount">How many views it holds.</param>
public sealed record CanvasDocumentInfo(string Id, string Title, DateTimeOffset SavedAt, int ViewCount);

/// <summary>
/// Where canvas documents live. DRYL ships the contract and an in-memory implementation and no
/// database code at all — the host keeps owning its data (A7).
/// </summary>
/// <remarks>Task-based on purpose: on WebAssembly a host implements this over HTTP or
/// <c>localStorage</c> without a single server-side construct in the contract (A9).</remarks>
public interface ICanvasDocumentStore
{
    /// <summary>Writes the document. A document without an <see cref="CanvasDocument.Id"/> gets a
    /// new one, which is written back onto the instance and returned.</summary>
    Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default);

    /// <summary>Reads a document, or null when the id is unknown.</summary>
    Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default);

    /// <summary>Lists the stored documents, newest first.</summary>
    Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default);

    /// <summary>Deletes a document. An unknown id is not an error.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}

/// <summary>
/// The in-process reference implementation — enough for demos, tests and single-node prototypes,
/// and the shape a real store should behave like.
/// </summary>
/// <remarks>Stores the serialized form, not the object: a caller cannot mutate a loaded document
/// back into the store, and every save exercises the serialization path.</remarks>
public sealed class InMemoryCanvasDocumentStore : ICanvasDocumentStore
{
    private readonly ConcurrentDictionary<string, string> _documents = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Id ??= Guid.NewGuid().ToString("n");
        document.SavedAt = DateTimeOffset.UtcNow;
        _documents[document.Id] = document.ToJson();
        return Task.FromResult(document.Id);
    }

    /// <inheritdoc />
    public Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default)
    {
        if (id is null || !_documents.TryGetValue(id, out var json)) return Task.FromResult<CanvasDocument?>(null);
        return Task.FromResult(CanvasDocument.TryFromJson(json, out var doc, out _) ? doc : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default)
    {
        var list = _documents.Values
            .Select(json => CanvasDocument.TryFromJson(json, out var d, out _) ? d : null)
            .Where(d => d is not null)
            .Select(d => new CanvasDocumentInfo(d!.Id!, d.Title ?? "Canvas", d.SavedAt, d.Views?.Count ?? 0))
            .OrderByDescending(i => i.SavedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<CanvasDocumentInfo>>(list);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (id is not null) _documents.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: DI-Erweiterungen ergänzen** — ans Ende der Klasse in `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs` (die `using`-Zeilen `Microsoft.Extensions.DependencyInjection.Extensions` und `DRYL.Components.Canvas` bei Bedarf ergänzen):

```csharp
    /// <summary>
    /// Registers the in-memory canvas document store, so <c>DrylCanvasWorkspace</c> can save and
    /// load dashboards. Replace it with your own by registering an <see cref="ICanvasDocumentStore"/>
    /// first — this call never overwrites an existing registration.
    /// </summary>
    public static IServiceCollection AddDrylCanvasDocumentStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ICanvasDocumentStore, InMemoryCanvasDocumentStore>();
        return services;
    }

    /// <summary>Registers a host implementation of <see cref="ICanvasDocumentStore"/> as a singleton.</summary>
    /// <typeparam name="TStore">The host's store.</typeparam>
    public static IServiceCollection AddDrylCanvasDocumentStore<TStore>(this IServiceCollection services)
        where TStore : class, ICanvasDocumentStore
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ICanvasDocumentStore, TStore>();
        return services;
    }
```

- [ ] **Step 5: Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~CanvasDocumentStoreTests`
Expected: PASS (8 Tests).

- [ ] **Step 6: Committen**

```bash
git add DRYL.Components/Canvas/CanvasDocumentStore.cs DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs tests/DRYL.Components.Tests/Canvas/CanvasDocumentStoreTests.cs
git commit -m "feat(canvas): ICanvasDocumentStore + in-memory implementation"
```

---

### Task 5: Drei Icons — `Undo`, `Redo`, `History`

**Files:**
- Modify: `DRYL.Components/Components/Data/DrylIcon.razor` (das `Icons`-Dictionary)

**Interfaces:**
- Produces: `DrylIcon.Icons` kennt `"Undo"`, `"Redo"`, `"History"`.

Der Icon-Satz hat heute keinen Pfeil für Rückgängig/Wiederholen. Die Pfade stammen wie alle anderen aus Lucide (ISC) — `THIRD_PARTY_NOTICES.md` deckt das bereits ab und braucht keine Änderung.

- [ ] **Step 1: Einträge ergänzen** — in `DRYL.Components/Components/Data/DrylIcon.razor`, im `Icons`-Dictionary direkt hinter dem `["Refresh"]`-Eintrag (Format exakt wie die Nachbarn: Kommentar `// lucide: <name>` am Zeilenende):

```csharp
        ["Undo"]         = """<path d="M9 14 4 9l5-5"/><path d="M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5 5.5 5.5 0 0 1-5.5 5.5H11"/>""",                                    // lucide: undo-2
        ["Redo"]         = """<path d="m15 14 5-5-5-5"/><path d="M20 9H9.5A5.5 5.5 0 0 0 4 14.5 5.5 5.5 0 0 0 9.5 20H13"/>""",                                     // lucide: redo-2
        ["History"]      = """<path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M12 7v5l4 2"/>""",                         // lucide: history
```

- [ ] **Step 2: Test schreiben und laufen lassen** — an `tests/DRYL.Components.Tests/` die vorhandene Icon-Testdatei suchen (`grep -rln "DrylIcon" tests/DRYL.Components.Tests | head`) und dort anhängen; existiert keine, neue Datei `tests/DRYL.Components.Tests/Data/DrylIconTests.cs`:

```csharp
using DRYL.Components;
using Xunit;

namespace DRYL.Components.Tests.Data;

public class DrylIconHistoryTests
{
    [Theory]
    [InlineData("Undo")]
    [InlineData("Redo")]
    [InlineData("History")]
    public void The_history_icons_are_in_the_set(string name) =>
        Assert.True(DrylIcon.Icons.ContainsKey(name));
}
```

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~DrylIconHistoryTests`
Expected: PASS (3 Tests).

- [ ] **Step 3: Committen**

```bash
git add DRYL.Components/Components/Data/DrylIcon.razor tests/DRYL.Components.Tests
git commit -m "feat(icons): Undo, Redo and History icons"
```

---

### Task 6: Leiste umbauen und Undo/Redo/Verlauf einbauen

**Files:**
- Modify: `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceHistoryTests.cs`

**Interfaces:**
- Consumes: `CanvasWorkspace.Commit/Undo/Redo/RestoreVersion/CanUndo/CanRedo` (Task 2), `CanvasHistoryEntry` (Task 1), Icons aus Task 5.
- Produces: neue Parameter auf `DrylCanvasWorkspace`: `bool ShowHistory`, `int Revision`, `string? RevisionLabel`.
- Produces: CSS-Klassen `.ws-chips`, `.ws-tools`, `.ws-versions`, `.ws-version`, `.ws-version.is-current`, `.ws-live`.

**Umbau der Leiste:** `.ws-bar` scrollt heute selbst (`overflow-x: auto`) — eine rechtsbündige Werkzeuggruppe würde mitscrollen. Deshalb wandern die Chips (und der Ink-Balken, und `role="tablist"`, und `@ref="_bar"`) in ein neues inneres `.ws-chips`; `.ws-bar` wird die nicht scrollende Zeile darum.

- [ ] **Step 1: Failing Tests schreiben** — neue Datei `tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceHistoryTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Undo, redo and "back to version N" as the workspace bar offers them.</summary>
public class DrylCanvasWorkspaceHistoryTests : BunitContext
{
    public DrylCanvasWorkspaceHistoryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec(string title) =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""{ "title": "{{title}}", "root": { "id": "r", "type": "markdown", "props": { "content": "{{title}}" } } }""",
            CanvasJson.Options)!;

    private static CanvasWorkspace OneView(params string[] versions)
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        foreach (var v in versions)
        {
            view.Spec = Spec(v);
            ws.Commit(v);
        }
        return ws;
    }

    [Fact]
    public void Without_ShowHistory_the_bar_has_no_tools()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, OneView("v1", "v2")));

        Assert.Empty(cut.FindAll(".ws-tools"));
    }

    [Fact]
    public void The_bar_shows_for_a_single_view_once_history_is_on()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1"))
            .Add(x => x.ShowHistory, true));

        Assert.Single(cut.FindAll(".ws-tools"));
        Assert.Single(cut.FindAll(".ws-chip"));
    }

    [Fact]
    public void Undo_is_disabled_with_one_version_and_enabled_with_two()
    {
        var one = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1")).Add(x => x.ShowHistory, true));
        Assert.True(one.Find(".ws-tools button[aria-label='Undo']").HasAttribute("disabled"));

        var two = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1", "v2")).Add(x => x.ShowHistory, true));
        Assert.False(two.Find(".ws-tools button[aria-label='Undo']").HasAttribute("disabled"));
    }

    [Fact]
    public void Clicking_undo_puts_the_previous_spec_back()
    {
        var ws = OneView("v1", "v2");
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws).Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Undo']").Click();

        Assert.Equal("v1", ws.Active!.Spec!.Title);
        Assert.False(cut.Find(".ws-tools button[aria-label='Redo']").HasAttribute("disabled"));
    }

    [Fact]
    public void A_changed_Revision_commits_the_active_view_exactly_once()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("v1");

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 1)
            .Add(x => x.RevisionLabel, "first prompt"));

        Assert.Single(view.History.Entries);
        Assert.Equal("first prompt", view.History.Entries[0].Label);

        cut.Render(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 1)
            .Add(x => x.RevisionLabel, "first prompt"));

        Assert.Single(view.History.Entries);

        view.Spec = Spec("v2");
        cut.Render(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.Revision, 2)
            .Add(x => x.RevisionLabel, "second prompt"));

        Assert.Equal(2, view.History.Entries.Count);
        Assert.Equal("second prompt", view.History.Entries[1].Label);
    }

    [Fact]
    public void The_version_list_shows_the_newest_first_and_marks_the_current_one()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, OneView("v1", "v2", "v3")).Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Version history']").Click();

        var items = cut.FindAll(".ws-version");
        Assert.Equal(3, items.Count);
        Assert.Contains("v3", items[0].TextContent);
        Assert.Contains("is-current", items[0].ClassName);
    }

    [Fact]
    public void Picking_a_version_restores_it()
    {
        var ws = OneView("v1", "v2", "v3");
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws).Add(x => x.ShowHistory, true));

        cut.Find(".ws-tools button[aria-label='Version history']").Click();
        cut.FindAll(".ws-version")[2].Click();      // oldest

        Assert.Equal("v1", ws.Active!.Spec!.Title);
    }
}
```

- [ ] **Step 2: Tests laufen lassen — sie müssen fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~DrylCanvasWorkspaceHistoryTests`
Expected: Compile-Fehler „'DrylCanvasWorkspace' does not contain a definition for 'ShowHistory'".

- [ ] **Step 3: Markup umbauen** — in `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor` den Block von `<div class="ws-bar" …>` bis zu seinem schließenden `</div>` (innerhalb der `DrylPresence`) durch dieses ersetzen. Die Chip-Schleife selbst bleibt Zeichen für Zeichen wie sie ist — sie zieht nur ein `<div>` tiefer:

```razor
        <div class="ws-bar">
            <div class="ws-chips" role="tablist" aria-label="@AriaLabel" @ref="_bar">
                <div class="ws-ink" data-dryl-ink aria-hidden="true"></div>
                @*  Not Workspace!.Views: while the bar plays its exit the parameter may already
                    have been swapped out from under it. *@
                @foreach (var v in Workspace?.Views ?? Array.Empty<CanvasView>())
                {
                    @* … unverändert: der bestehende Chip-Block … *@
                }
            </div>

            @if (ShowHistory)
            {
                <div class="ws-tools">
                    <DrylTooltip Text="Undo">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="Undo"
                                    Disabled="@(Workspace?.CanUndo != true)"
                                    OnClick="UndoAsync">
                            <DrylIcon Name="Undo" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                    <DrylTooltip Text="Redo">
                        <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                    Size="DrylButton.ButtonSize.Small"
                                    AriaLabel="Redo"
                                    Disabled="@(Workspace?.CanRedo != true)"
                                    OnClick="RedoAsync">
                            <DrylIcon Name="Redo" Size="14" />
                        </DrylButton>
                    </DrylTooltip>
                    <DrylPopover @bind-Open="_versionsOpen" PanelAriaLabel="Version history">
                        <TriggerContent>
                            <DrylTooltip Text="Version history">
                                <DrylButton Variant="DrylButton.ButtonVariant.Ghost"
                                            Size="DrylButton.ButtonSize.Small"
                                            AriaLabel="Version history"
                                            Disabled="@(Versions.Count == 0)">
                                    <DrylIcon Name="History" Size="14" />
                                </DrylButton>
                            </DrylTooltip>
                        </TriggerContent>
                        <PanelContent>
                            <div class="ws-versions">
                                @for (var i = Versions.Count - 1; i >= 0; i--)
                                {
                                    var index = i;                       // one capture per row
                                    var entry = Versions[index];
                                    <button type="button"
                                            class="ws-version @(index == Position ? "is-current" : null)"
                                            @onclick="@(() => RestoreAsync(index))">
                                        <span class="ws-version-label">@entry.Label</span>
                                        <span class="ws-version-time">@entry.At.ToLocalTime().ToString("t")</span>
                                    </button>
                                }
                            </div>
                        </PanelContent>
                    </DrylPopover>
                </div>
            }

            <span class="ws-live" aria-live="polite">@_announcement</span>
        </div>
```

- [ ] **Step 4: Code-Block ergänzen** — in `@code { … }` von `DrylCanvasWorkspace.razor`:

Die drei neuen Parameter direkt hinter `ShowBarWhenSingle`:

```csharp
    /// <summary>Whether the bar offers undo, redo and the version history. Default false.</summary>
    [Parameter] public bool ShowHistory { get; set; }

    /// <summary>A counter the host bumps after every settled round (typically <c>run.Round</c>).
    /// Every change commits a version of the active view.</summary>
    [Parameter] public int Revision { get; set; }

    /// <summary>Label for the next committed version — typically the prompt that produced it.</summary>
    [Parameter] public string? RevisionLabel { get; set; }
```

Die Felder zu den bestehenden privaten Feldern:

```csharp
    private bool _versionsOpen;
    private int _lastRevision;
    private string? _announcement;
```

`ShowBar` erweitern (History braucht die Leiste auch bei einer einzigen View):

```csharp
    private bool ShowBar =>
        Workspace is { Views.Count: > 0 } &&
        (ShowBarWhenSingle || ShowHistory || Workspace.Views.Count > 1);
```

Die Helfer und Verben hinter `ActivateAsync`:

```csharp
    private IReadOnlyList<CanvasHistoryEntry> Versions =>
        Workspace?.Active?.History.Entries ?? Array.Empty<CanvasHistoryEntry>();

    private int Position => Workspace?.Active?.History.Position ?? -1;

    private Task UndoAsync() => HistoryStep(
        () => Workspace!.Undo(),
        () => $"Undone: {Versions.ElementAtOrDefault(Position + 1)?.Label ?? "last change"}");

    private Task RedoAsync() => HistoryStep(
        () => Workspace!.Redo(),
        () => $"Redone: {Versions.ElementAtOrDefault(Position)?.Label ?? "last change"}");

    private Task RestoreAsync(int index)
    {
        _versionsOpen = false;
        return HistoryStep(
            () => Workspace!.RestoreVersion(index),
            () => FormattableString.Invariant($"Restored version {index + 1} of {Versions.Count}"));
    }

    // A history step is a state change, so it is a movement: the same view-transition layer the
    // view switch uses morphs the artifact instead of blinking it away (A8).
    private async Task HistoryStep(Func<bool> step, Func<string> announce)
    {
        if (Workspace is null) return;

        var moved = false;
        await ViewTransition.RunAsync(() =>
        {
            moved = step();
            if (moved) StateHasChanged();
        });

        if (!moved) return;
        _announcement = announce();
        StateHasChanged();
    }
```

In `OnParametersSet`, hinter der bestehenden Abo-Umschaltung:

```csharp
        // A settled round is a version. Record() drops a snapshot that changed nothing, so a
        // superfluous commit costs nothing.
        if (Revision != _lastRevision)
        {
            _lastRevision = Revision;
            Workspace?.Commit(RevisionLabel ?? FormattableString.Invariant($"Version {Revision}"));
        }
```

`@using DRYL.Components.Canvas` ist bereits oben in der Datei — `CanvasHistoryEntry` braucht keinen weiteren `@using`.

- [ ] **Step 5: CSS ergänzen** — in `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css`. Den bestehenden `.ws-bar`-Block ersetzen und die neuen Blöcke hinter `.ws-ink` einfügen:

```css
/* ---- View bar ---------------------------------------------------- */
/* The bar is the non-scrolling row; only the chips scroll, so the tools stay put at 375px. */
.ws-bar {
    display: flex;
    align-items: flex-end;
    gap: var(--sp-2);
    border-bottom: 1px solid var(--line);
}

.ws-chips {
    position: relative;
    display: flex;
    align-items: flex-end;
    gap: var(--sp-1);
    flex: 1 1 auto;
    min-width: 0;
    padding-bottom: 2px;
    overflow-x: auto;
    scrollbar-width: thin;
}

.ws-tools {
    display: flex;
    align-items: center;
    gap: var(--sp-1);
    flex: none;
    padding-bottom: var(--sp-1);
}

/* ---- Version list ------------------------------------------------ */
.ws-versions {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 200px;
    max-height: 280px;
    overflow-y: auto;
}

.ws-version {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: var(--sp-3);
    padding: var(--sp-1) var(--sp-2);
    border: 1px solid transparent;
    border-radius: var(--r-sm);
    background: none;
    font: inherit;
    color: var(--fg-muted);
    text-align: left;
    cursor: pointer;
    transition: color var(--dur-fast) var(--ease-out),
                border-color var(--dur-fast) var(--ease-out),
                background var(--dur-fast) var(--ease-out);
}

.ws-version:hover {
    color: var(--fg);
    border-color: var(--line);
    background: var(--glass-1);
}

.ws-version:focus-visible {
    outline: none;
    box-shadow: 0 0 0 2px var(--accent-line);
}

.ws-version.is-current {
    color: var(--fg);
    border-color: var(--accent-line);
}

.ws-version-time {
    color: var(--fg-dim);
    font-size: var(--fs-xs);
    white-space: nowrap;
}

/* Announcements are for screen readers only — the movement is the visual feedback. */
.ws-live {
    position: absolute;
    width: 1px;
    height: 1px;
    margin: -1px;
    padding: 0;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
}
```

Im `prefers-reduced-motion`-Block am Dateiende `.ws-version` ergänzen:

```css
@media (prefers-reduced-motion: reduce) {
    .ws-chip,
    .ws-version,
    ::deep .ws-chip-close,
    .ws-bar.is-ink-ready .ws-ink {
        transition: none;
    }
}
```

Achtung: `is-ink-ready` setzt `dryl.motion.moveIndicator` auf das übergebene Element — das ist jetzt `.ws-chips`. Den Selektor entsprechend anpassen: `.ws-chips.is-ink-ready .ws-ink` (an **beiden** Stellen, im Haupt- und im Reduced-Motion-Block).

- [ ] **Step 6: Bestehende Tests prüfen und Selektoren nachziehen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~DrylCanvasWorkspaceTests`
Expected: PASS. Schlägt ein Test wegen `.ws-bar`-Struktur fehl (z. B. eine `role="tablist"`-Suche), den Selektor in der **Testdatei** auf `.ws-chips` umstellen — die Semantik ist unverändert, nur der Träger.

- [ ] **Step 7: Neue Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~DrylCanvasWorkspaceHistoryTests`
Expected: PASS (7 Tests).

- [ ] **Step 8: Committen**

```bash
git add DRYL.Components/Components/AI/DrylCanvasWorkspace.razor DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css tests/DRYL.Components.Tests/Canvas
git commit -m "feat(canvas): undo, redo and version history in the workspace bar"
```

---

### Task 7: Autosave gegen den registrierten Store

**Files:**
- Modify: `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor`
- Test: `tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceHistoryTests.cs` (anhängen)

**Interfaces:**
- Consumes: `ICanvasDocumentStore`, `CanvasDocument.Capture` (Tasks 3/4).
- Produces: neue Parameter `bool AutoSave`, `string? DocumentId`, `EventCallback<string> DocumentIdChanged`, `string? DocumentTitle`, `EventCallback<CanvasDocumentInfo> OnSaved`.

- [ ] **Step 1: Failing Test schreiben** — an `tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceHistoryTests.cs` anhängen:

```csharp
    [Fact]
    public async Task AutoSave_writes_the_workspace_to_the_registered_store()
    {
        var store = new InMemoryCanvasDocumentStore();
        Services.AddSingleton<ICanvasDocumentStore>(store);

        var ws = OneView("v1");
        string? savedId = null;

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.ShowHistory, true)
            .Add(x => x.AutoSave, true)
            .Add(x => x.AutoSaveDelayMs, 0)
            .Add(x => x.DocumentTitle, "My dashboard")
            .Add(x => x.DocumentIdChanged, (string id) => savedId = id)
            .Add(x => x.Revision, 1));

        await cut.InvokeAsync(async () =>
        {
            for (var i = 0; i < 50 && savedId is null; i++) await Task.Delay(20);
        });

        Assert.NotNull(savedId);
        var list = await store.ListAsync();
        Assert.Single(list);
        Assert.Equal("My dashboard", list[0].Title);
    }

    [Fact]
    public void AutoSave_without_a_registered_store_is_a_no_op()
    {
        var ws = OneView("v1");

        var ex = Record.Exception(() => Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.AutoSave, true)
            .Add(x => x.AutoSaveDelayMs, 0)
            .Add(x => x.Revision, 1)));

        Assert.Null(ex);
    }
```

Der Test braucht die `using`-Zeilen `using DRYL.Components.Canvas;` (schon da) und `using Microsoft.Extensions.DependencyInjection;` (schon da) sowie `using System.Threading.Tasks;` (implizit).

- [ ] **Step 2: Test laufen lassen — er muss fehlschlagen**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~AutoSave`
Expected: Compile-Fehler „does not contain a definition for 'AutoSave'".

- [ ] **Step 3: Autosave implementieren** — in `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor`.

Parameter hinter `RevisionLabel`:

```csharp
    /// <summary>Whether a settled revision is written to the registered
    /// <c>ICanvasDocumentStore</c> after a short pause. Default false; without a registered
    /// store it does nothing.</summary>
    [Parameter] public bool AutoSave { get; set; }

    /// <summary>How long to wait after the last revision before saving, in milliseconds.
    /// Default 1500.</summary>
    [Parameter] public int AutoSaveDelayMs { get; set; } = 1500;

    /// <summary>The document autosave writes to. Null until the first save mints one.</summary>
    [Parameter] public string? DocumentId { get; set; }

    /// <summary>Reports the id the store assigned on the first save.</summary>
    [Parameter] public EventCallback<string> DocumentIdChanged { get; set; }

    /// <summary>Title stored with the document; defaults to the active view's title.</summary>
    [Parameter] public string? DocumentTitle { get; set; }

    /// <summary>Raised after every successful autosave.</summary>
    [Parameter] public EventCallback<CanvasDocumentInfo> OnSaved { get; set; }
```

Felder zu den privaten Feldern:

```csharp
    private CancellationTokenSource? _saveDebounce;
    private DrylCanvas? _bodyCanvas;
```

Der Store wird optional gezogen — die Komponente muss ohne Registrierung weiter rendern. Ganz oben in der Datei zu den `@inject`-Zeilen:

```razor
@inject IServiceProvider Services
```

Im Body-Slot dem eingebauten Canvas eine Referenz geben (nur der `else`-Zweig ändert sich):

```razor
                <DrylCanvas @ref="_bodyCanvas" Spec="active.Spec" EmptyText="@EmptyText" />
```

In `OnParametersSet` den Commit-Block um den Debounce erweitern:

```csharp
        if (Revision != _lastRevision)
        {
            _lastRevision = Revision;
            Workspace?.Commit(RevisionLabel ?? FormattableString.Invariant($"Version {Revision}"));
            ScheduleSave();
        }
```

Die Save-Mechanik hinter `HistoryStep`:

```csharp
    // Debounced with a CancellationTokenSource rather than a Timer: it cancels cleanly in
    // DisposeAsync and needs no separate elapsed handler.
    private void ScheduleSave()
    {
        if (!AutoSave || Workspace is null) return;
        if (Services.GetService(typeof(ICanvasDocumentStore)) is not ICanvasDocumentStore store) return;

        _saveDebounce?.Cancel();
        _saveDebounce?.Dispose();
        var cts = new CancellationTokenSource();
        _saveDebounce = cts;

        _ = SaveAfterDelayAsync(store, cts.Token);
    }

    private async Task SaveAfterDelayAsync(ICanvasDocumentStore store, CancellationToken ct)
    {
        try
        {
            if (AutoSaveDelayMs > 0) await Task.Delay(AutoSaveDelayMs, ct);
            if (ct.IsCancellationRequested || Workspace is null) return;

            var doc = CanvasDocument.Capture(Workspace, DocumentTitle, _bodyCanvas?.Context.Form);
            doc.Id = DocumentId;
            var id = await store.SaveAsync(doc, ct);

            if (ct.IsCancellationRequested) return;
            if (DocumentId != id)
            {
                DocumentId = id;
                await InvokeAsync(() => DocumentIdChanged.InvokeAsync(id));
            }
            await InvokeAsync(() => OnSaved.InvokeAsync(
                new CanvasDocumentInfo(id, doc.Title ?? "Canvas", doc.SavedAt, doc.Views?.Count ?? 0)));
        }
        catch (OperationCanceledException) { /* superseded by a newer revision, or disposed */ }
        catch (Exception)
        {
            // A broken store must never take a running dashboard down — the same stance the
            // node-level binding error takes in phase 1.
        }
    }
```

In `DisposeAsync`, ganz am Anfang:

```csharp
        _saveDebounce?.Cancel();
        _saveDebounce?.Dispose();
```

- [ ] **Step 4: Tests laufen lassen — sie müssen grün sein**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter FullyQualifiedName~DrylCanvasWorkspaceHistoryTests`
Expected: PASS (9 Tests).

- [ ] **Step 5: Committen**

```bash
git add DRYL.Components/Components/AI/DrylCanvasWorkspace.razor tests/DRYL.Components.Tests/Canvas/DrylCanvasWorkspaceHistoryTests.cs
git commit -m "feat(canvas): debounced autosave against the registered document store"
```

---

### Task 8: Demo auf der Website

**Files:**
- Create: `DRYL.Website/Components/Examples/Canvas/CanvasDocument.razor`
- Modify: `DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor`
- Modify: `DRYL.Website/Components/Examples/Agents/OpenAiCanvasWorkspace.razor`
- Modify: `DRYL.Website/Components/ComponentCatalog.cs`
- Modify: `DRYL.Website/Program.cs`

**Interfaces:**
- Consumes: alles aus Tasks 1–7.

- [ ] **Step 1: Ablageort und Muster prüfen**

Run:
```bash
ls DRYL.Website/Components/Examples/
grep -n "DemoExample\|Example" DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor | head -20
```
Das eingebettete `.razor`-Beispielframework (`DemoExample`) bestimmt, wo die Datei liegen muss und wie sie eingebunden wird. Existiert kein Ordner `Examples/Canvas`, dann den anlegen und das `@using`/Namensraum-Muster einer Nachbardatei exakt übernehmen (Razor: gepunktete Tag-Namen brauchen das passende `@using` — siehe `project_website_rebuild`).

- [ ] **Step 2: Beispiel schreiben** — `DRYL.Website/Components/Examples/Canvas/CanvasDocument.razor`:

```razor
@using DRYL.Components.Canvas
@inject ICanvasDocumentStore Store

<div class="doc-demo">
    <DrylStack Direction="DrylStack.StackDirection.Horizontal" Gap="2" Wrap>
        <DrylButton Size="DrylButton.ButtonSize.Small" OnClick="AddCard">Add a card</DrylButton>
        <DrylButton Size="DrylButton.ButtonSize.Small" OnClick="SaveAsync">Save</DrylButton>
        <DrylButton Size="DrylButton.ButtonSize.Small" Variant="DrylButton.ButtonVariant.Secondary"
                    Disabled="@(_savedId is null)" OnClick="LoadAsync">Load</DrylButton>
        <DrylButton Size="DrylButton.ButtonSize.Small" Variant="DrylButton.ButtonVariant.Ghost"
                    Disabled="@(_savedId is null)" OnClick="TemplateAsync">New from template</DrylButton>
    </DrylStack>

    <DrylCanvasWorkspace Workspace="_ws" ShowHistory Revision="_revision" RevisionLabel="@_label" />

    @if (_status is not null)
    {
        <DrylText Variant="DrylText.TextVariant.Muted">@_status</DrylText>
    }
</div>

@code {
    private readonly CanvasWorkspace _ws = new();
    private int _revision;
    private string? _label;
    private string? _savedId;
    private string? _status;
    private int _cards;

    protected override void OnInitialized()
    {
        var overview = _ws.Open("Overview", "Chart");
        overview.Spec = Spec("Overview", "Revenue is up 12% this quarter.");
        var order = _ws.Open("Order 4711", "File");
        order.Spec = Spec("Order 4711", "Three positions, two of them shipped.");
        _ws.Activate(overview.Id);
        Bump("opened");
    }

    private void AddCard()
    {
        if (_ws.Active?.Spec?.Root is not { } root) return;
        root.Children ??= new List<CanvasNode>();
        _cards++;
        root.Children.Add(new CanvasNode
        {
            Id = FormattableString.Invariant($"card-{_cards}"),
            Type = "stat",
            Props = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                FormattableString.Invariant($$"""{ "label": "Metric {{_cards}}", "value": "{{_cards * 12}}k" }"""),
                CanvasJson.Options),
        });
        root.Version++;
        Bump(FormattableString.Invariant($"added metric {_cards}"));
    }

    private async Task SaveAsync()
    {
        var doc = CanvasDocument.Capture(_ws, "Demo dashboard");
        doc.Id = _savedId;
        _savedId = await Store.SaveAsync(doc);
        _status = "Saved. Change something, then load it back.";
    }

    private async Task LoadAsync()
    {
        if (_savedId is null) return;
        var doc = await Store.LoadAsync(_savedId);
        if (doc is null) { _status = "The document is gone."; return; }
        doc.Restore(_ws);
        Bump("loaded");
        _status = "Loaded.";
    }

    private async Task TemplateAsync()
    {
        if (_savedId is null) return;
        var doc = await Store.LoadAsync(_savedId);
        if (doc is null) return;
        var copy = doc.AsTemplate("Copy of the demo dashboard");
        _savedId = await Store.SaveAsync(copy);
        copy.Restore(_ws);
        Bump("new from template");
        _status = "Started a new document from the saved one.";
    }

    private void Bump(string label)
    {
        _label = label;
        _revision++;
    }

    private static CanvasSpec Spec(string title, string text) =>
        System.Text.Json.JsonSerializer.Deserialize<CanvasSpec>(
            $$"""
            { "title": "{{title}}", "root": { "id": "root", "type": "stack", "children": [
                { "id": "intro", "type": "markdown", "props": { "content": "{{text}}" } }
            ] } }
            """, CanvasJson.Options)!;
}
```

- [ ] **Step 3: Store registrieren** — in `DRYL.Website/Program.cs` bei den übrigen `AddDryl…`-Aufrufen:

```csharp
builder.Services.AddDrylCanvasDocumentStore();
```

- [ ] **Step 4: Beispiel einbinden** — in `DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor` einen neuen Abschnitt im Muster der bestehenden Beispiele der Seite ergänzen (Titel „Document & history", Beschreibung: „Save the workspace, load it back, start a new document from it — and step back through the versions the bar recorded."). Das exakte Wrapper-Markup von einem bestehenden Abschnitt derselben Datei kopieren.

- [ ] **Step 5: Live-Variante ergänzen** — in `DRYL.Website/Components/Examples/Agents/OpenAiCanvasWorkspace.razor` am `<DrylCanvasWorkspace …>`-Tag ergänzen:

```razor
                      ShowHistory
                      Revision="_run.Round"
                      RevisionLabel="@_lastPrompt"
```

Gibt es kein Feld `_lastPrompt`, eines anlegen und in der Send-Methode auf den abgeschickten Text setzen. **Keine** Datei unter `DRYL.Components.Agents/` anfassen.

- [ ] **Step 6: Katalogeintrag aktualisieren** — in `DRYL.Website/Components/ComponentCatalog.cs` den Eintrag von `DrylCanvasWorkspace` suchen und seine Beschreibung um Undo/Redo, Versionsverlauf und Dokument-Store erweitern.

- [ ] **Step 7: Bauen und ansehen**

Run: `dotnet build DRYL.Website/DRYL.Website.csproj`
Expected: Build erfolgreich.

Danach die Seite starten und `/components/canvas-workspace` (bzw. die Route aus dem `@page`-Attribut) öffnen: Karte hinzufügen → Undo → Redo → Verlauf → Speichern → ändern → Laden. Beide Farbmodi (`data-dryl-mode` auf `<html>` umschalten) und 375 px prüfen; die Leiste darf nicht umbrechen und der Body nicht seitwärts scrollen.

- [ ] **Step 8: Committen**

```bash
git add DRYL.Website
git commit -m "docs(website): canvas document & history demo"
```

---

### Task 9: Changelog, Version, Gesamtlauf

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `DRYL.Components/DRYL.Components.csproj`

- [ ] **Step 1: Vollständigen Testlauf machen**

Run: `dotnet test tests/DRYL.Components.Tests`
Expected: PASS über alle Zielframeworks. Fehlschläge hier reparieren, nicht wegdrücken.

- [ ] **Step 2: Token-Sync prüfen**

Run: `node scripts/check-light-sync.mjs`
Expected: grün (es kamen keine Tokens dazu).

- [ ] **Step 3: Version bumpen** — in `DRYL.Components/DRYL.Components.csproj`:

```xml
    <Version>2.16.0</Version>
```

`DRYL.Components.Agents/DRYL.Components.Agents.csproj` bleibt bei `0.13.0` — Phase 5 fasst das Agents-Paket nicht an.

- [ ] **Step 4: Changelog schneiden** — in `CHANGELOG.md` den `[Unreleased]`-Block zu `## [2.16.0] - 2026-07-26` machen, darüber ein frisches leeres `[Unreleased]` anlegen, und diese Einträge aufnehmen:

```markdown
### Added
- `CanvasDocument` — Serializes a whole `CanvasWorkspace` (views, active view, specs) with a schema version; `Capture` / `Restore` / `ToJson` / `TryFromJson` / `AsTemplate`
- `CanvasHistory` — Per-view snapshot ring behind undo, redo and "back to version N"; `CanvasView.History`, plus `Commit` / `Undo` / `Redo` / `RestoreVersion` / `CanUndo` / `CanRedo` on `CanvasWorkspace`
- `ICanvasDocumentStore` + `InMemoryCanvasDocumentStore` + `AddDrylCanvasDocumentStore()` — the persistence contract; DRYL ships no database code
- `DrylCanvasWorkspace` — New `ShowHistory`, `Revision`, `RevisionLabel` parameters: undo, redo and a version-history popover in the view bar, every step morphing through `IDrylViewTransition`
- `DrylCanvasWorkspace` — New `AutoSave`, `AutoSaveDelayMs`, `DocumentId`, `DocumentIdChanged`, `DocumentTitle`, `OnSaved` parameters: debounced saving against the registered store
- `DrylIcon` — New `Undo`, `Redo` and `History` icons

### Changed
- `DrylCanvasWorkspace` — The view bar no longer scrolls as a whole; the chips scroll inside `.ws-chips` while the new tool group stays put. Custom CSS targeting `.ws-bar` for the scrolling row must move to `.ws-chips`
```

- [ ] **Step 5: Committen**

```bash
git add CHANGELOG.md DRYL.Components/DRYL.Components.csproj
git commit -m "chore(release): DRYL.Components 2.16.0 — canvas document & history"
```

- [ ] **Step 6: Projektnotiz fortschreiben**

`C:\Users\janzi\.claude\projects\c--Users-janzi-Desktop-DRYL-DRYL-Components\memory\project_canvas_platform.md` um Phase 5 ergänzen: umgesetzt (Kern 2.16.0, Agents unverändert), der Befund „es gibt keinen Op-Log ⇒ Snapshot-Ring", der Feldwert-Trick über die `value`-Props, und der offene Punkt „`DrylAiCanvas` reicht `Context` nicht durch — live getippte Felder persistieren dort nicht (Phase 6)".

---

## Self-Review

**Spec-Abdeckung**

| Spec-Abschnitt | Task |
| --- | --- |
| §2 Snapshot statt Op-Log | 1 |
| §3 `CanvasDocument` inkl. Schema-Gate, Template | 3 |
| §4 `CanvasHistory` | 1 |
| §4.1 View-/Workspace-Anbindung | 2 |
| §4.2 Wer committet (`Revision`) | 6 |
| §4.3 Feldwerte in `value`-Props | 3 (Falten) + 7 (`_bodyCanvas.Context.Form`) |
| §5 Store + DI | 4 |
| §6.1 Parameter | 6, 7 |
| §6.2 Leiste, Icons | 5, 6 |
| §6.3 A8 über `ViewTransition` | 6 |
| §6.4 aria-live | 6 |
| §6.5 Autosave inkl. Cleanup | 7 |
| §7 Tests | 1–7 |
| §8 Demo + Katalog | 8 |
| §10 DoD | 8 (Modi/375 px), 9 (Version, Changelog, Sync, Notiz) |

**Offene Prüfpunkte, die im Plan als Schritt stehen** (nicht als Annahme): Sichtbarkeit von `CanvasCatalog.IsInteractive` (Task 3, Step 4), Selektoren bestehender Workspace-Tests nach dem Leisten-Umbau (Task 6, Step 6), Ablageort/Einbindungsmuster der Website-Beispiele (Task 8, Step 1).
