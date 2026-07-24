# DrylAiCanvas Phase P — Render-Pipeline & Performance — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Canvas nodes parse JSON and validate only when actually mutated (not on every render); `DrylMarkdown` parses only when its content changes.

**Architecture:** A new internal mutation stamp `CanvasNode.Version` is bumped by every successful mutation in `CanvasPatcher`, `DrylCanvasRun.Purge` and `CanvasStreamReveal`. `CanvasNodeView` memoizes validation + props deserialization keyed on that stamp. `DrylMarkdown` memoizes its Markdig parse keyed on the `Content` string. Spec: `docs/superpowers/specs/2026-07-24-canvas-render-pipeline-design.md`.

**Tech Stack:** C# / .NET (multi-target net8.0/9.0/10.0), Blazor, `System.Text.Json`, Markdig, xUnit + bUnit (`tests/DRYL.Components.Tests`).

## Global Constraints

- No public API change — `Version`, memo fields and `ParseCount` are all `internal`/`private`.
- Both assemblies already expose internals to the test project (`InternalsVisibleTo` in both `.csproj` files) — internal members are directly test-reachable, do not add new visibility attributes.
- Bumps happen **only on the success path** — a rolled-back op must not invalidate any memo.
- Tests run from the repo root: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "<Filter>"`.
- Commit style: conventional, lowercase (`feat:`, `perf:`, `test:`), with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>` trailer.
- Versioning (CLAUDE.md §7.0/§7.1): version bumps + changelog release cut happen in the SAME commit (Task 6).

---

### Task 1: `CanvasNode.Version` + `CanvasPatcher` bumps

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasSpec.cs` (class `CanvasNode`, after the `Removing` property, ~line 39)
- Modify: `DRYL.Components.Agents/Canvas/CanvasPatcher.cs` (`ApplySetProps` ~line 44, `ApplyInsert` ~line 84, `ApplyRemove` ~line 102, `ApplyMove` ~line 140)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs` (new)

**Interfaces:**
- Consumes: existing `CanvasPatcher.Apply(CanvasSpec, CanvasOp)` → `string?` (null = success).
- Produces: `CanvasNode.Version` — `internal int Version { get; set; }`, bumped +1 per successful mutation touching that node. Later tasks (2, 3, 4) rely on this member existing.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Version-stamp tests: every successful tree mutation must bump
/// <c>CanvasNode.Version</c> on exactly the nodes it touches (renderers memoize
/// parse + validation work on that stamp). Rolled-back ops bump nothing.
/// </summary>
public class CanvasVersionTests
{
    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasSpec Spec() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "grp", "type": "card", "children": [
                { "id": "b", "type": "divider" } ] } ] } }
        """);

    private static CanvasSpec MoveSpec() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "g1", "type": "card", "children": [ { "id": "m", "type": "divider" } ] },
            { "id": "g2", "type": "card", "children": [] } ] } }
        """);

    private static JsonElement Props(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public void SetProps_bumps_only_the_target_node()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var grp = spec.Root!.Children![1];
        var rootV = spec.Root.Version;
        var aV = a.Version;
        var grpV = grp.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "delta": "+5%" }"""),
        });

        Assert.Null(err);
        Assert.Equal(aV + 1, a.Version);
        Assert.Equal(grpV, grp.Version);
        Assert.Equal(rootV, spec.Root.Version);
    }

    [Fact]
    public void Rolled_back_setProps_bumps_nothing()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var aV = a.Version;

        // value becomes empty -> invalid per CanvasCatalog "stat" rules -> rollback
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "" }"""),
        });

        Assert.NotNull(err);
        Assert.Equal(aV, a.Version);
    }

    [Fact]
    public void Insert_bumps_the_parent()
    {
        var spec = Spec();
        var grp = spec.Root!.Children![1];
        var grpV = grp.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp",
            Node = new CanvasNode { Id = "n1", Type = "divider" },
        });

        Assert.Null(err);
        Assert.Equal(grpV + 1, grp.Version);
    }

    [Fact]
    public void Remove_bumps_the_removed_node()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var aV = a.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "a" });

        Assert.Null(err);
        Assert.True(a.Removing);
        Assert.Equal(aV + 1, a.Version);
    }

    [Fact]
    public void Move_bumps_both_parents()
    {
        var spec = MoveSpec();
        var g1 = spec.Root!.Children![0];
        var g2 = spec.Root!.Children![1];
        var g1V = g1.Version;
        var g2V = g2.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "m", Parent = "g2" });

        Assert.Null(err);
        Assert.Equal(g1V + 1, g1.Version);
        Assert.Equal(g2V + 1, g2.Version);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests"`
Expected: BUILD FAILURE — `CanvasNode` does not contain a definition for `Version`.

- [ ] **Step 3: Add `Version` to `CanvasNode`**

In `DRYL.Components.Agents/Canvas/CanvasSpec.cs`, directly after the `Removing` property:

```csharp
    /// <summary>Transient mutation stamp — bumped by every successful patcher/reveal/purge
    /// mutation touching this node (own props or its children list). Renderers memoize
    /// parse + validation work on it. Never serialized.</summary>
    [JsonIgnore] internal int Version { get; set; }
```

- [ ] **Step 4: Bump in `CanvasPatcher` (success paths only)**

In `DRYL.Components.Agents/Canvas/CanvasPatcher.cs`:

`ApplySetProps` — after the validation check, before `return null;`:

```csharp
        var error = CanvasCatalog.Validate(node);
        if (error is not null)
        {
            node.Props = before;
            return error;
        }

        node.Version++;
        return null;
```

`ApplyInsert` — after the parent validation check, before `return null;`:

```csharp
        var parentError = CanvasCatalog.Validate(parent);
        if (parentError is not null)
        {
            parent.Children.RemoveAt(index);
            return parentError;
        }

        parent.Version++;
        return null;
```

`ApplyRemove` — replace the final two lines:

```csharp
        node.Removing = true;
        node.Version++;
        return null;
```

`ApplyMove` — after the validation/rollback block, before `return null;`:

```csharp
        var error = newParentError ?? oldParentError;
        if (error is not null)
        {
            newParent.Children.Remove(node);
            oldParent.Children.Insert(oldIndex, node);
            return error;
        }

        oldParent.Version++;
        newParent.Version++;
        return null;
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests"`
Expected: 5 passed.

Run the existing patcher tests too: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasPatcherTests"`
Expected: all pass (no regression).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasSpec.cs DRYL.Components.Agents/Canvas/CanvasPatcher.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs
git commit -m "perf: add CanvasNode.Version mutation stamp, bump in CanvasPatcher

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `DrylCanvasRun.Purge` bumps the parent

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs` (`RemoveChild` helper, ~line 157)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs` (append)

**Interfaces:**
- Consumes: `CanvasNode.Version` (Task 1); `DrylCanvasRun.ApplySnapshot(CanvasSpec)` — internal, test-visible.
- Produces: purge-after-exit bumps the parent whose children list shrank. Task 4's memo relies on this for `tabs` containers losing a tab.

- [ ] **Step 1: Write the failing test**

Append to `CanvasVersionTests`:

```csharp
    [Fact]
    public void Purge_bumps_the_parent()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        var grp = run.Spec!.Root!.Children![1];
        var grpV = grp.Version;

        run.Purge("b");

        Assert.Empty(grp.Children!);
        Assert.Equal(grpV + 1, grp.Version);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests.Purge_bumps_the_parent"`
Expected: FAIL — `Assert.Equal() Expected: 1 Actual: 0` (child removed, version not bumped).

- [ ] **Step 3: Bump in `RemoveChild`**

In `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`:

```csharp
    private static bool RemoveChild(CanvasNode node, string id)
    {
        if (node.Children is null) return false;
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i].Id == id)
            {
                node.Children.RemoveAt(i);
                node.Version++;
                return true;
            }
        }
        foreach (var child in node.Children)
            if (RemoveChild(child, id)) return true;
        return false;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests"`
Expected: 6 passed.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasRun.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs
git commit -m "perf: bump parent Version when purge drops an exited node

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: `CanvasStreamReveal` bumps (root props, tail props, children adds)

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs` (`Reveal` ~line 50, `RevealChildren` ~lines 77, 99, 103)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs` (append)

**Interfaces:**
- Consumes: `CanvasNode.Version` (Task 1); `CanvasStreamReveal.Reveal(CanvasSpec live, CanvasSpec snapshot, bool streamDone)` — internal static.
- Produces: reveal-time mutations stamp the live (frozen) nodes, so Task 4's memo re-validates a `tabs` shell once its streamed children arrive.

- [ ] **Step 1: Write the failing tests**

Append to `CanvasVersionTests`:

```csharp
    [Fact]
    public void Reveal_adding_a_child_bumps_the_live_parent()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);
        var root = live.Root!;
        Assert.Single(root.Children!);   // d2 (streaming tail leaf) is withheld
        var v = root.Version;

        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"},
                {"id":"d3","type":"divider"}]}}
            """), streamDone: false);

        Assert.Equal(2, root.Children!.Count);
        Assert.Equal(v + 1, root.Version);
    }

    [Fact]
    public void Reveal_updating_tail_props_bumps_the_live_tail()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"A"},"children":[]}]}}
            """), streamDone: false);
        var tail = live.Root!.Children![1];
        var v = tail.Version;

        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"AB"},"children":[]}]}}
            """), streamDone: false);

        Assert.Equal(v + 1, tail.Version);
        Assert.Equal("AB", tail.Props!.Value.GetProperty("title").GetString());
    }

    [Fact]
    public void Reveal_updating_root_props_bumps_the_root()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{"gap":"sm"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);
        var root = live.Root!;
        var v = root.Version;

        // Same children, only the root's props change -> exactly one bump.
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{"gap":"md"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);

        Assert.Equal(v + 1, root.Version);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests.Reveal"`
Expected: 3 FAIL — versions stay at their initial values.

- [ ] **Step 3: Bump at the four mutation sites**

In `DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs`:

Site 1 — `Reveal`, root props update:

```csharp
        else if (!streamDone && PropsDiffer(live.Root.Props, snapRoot.Props))
        {
            live.Root.Props = snapRoot.Props;
            live.Root.Version++;
            changed = true;
        }
```

Site 2 — `RevealChildren`, complete child appended:

```csharp
            var existing = Find(liveParent.Children, s.Id);
            if (existing is null)
            {
                liveParent.Children.Add(s);   // freeze the whole complete subtree by reference
                liveParent.Version++;
                changed = true;
            }
```

Site 3 — `RevealChildren`, tail shell seeded:

```csharp
                if (liveTail is null)
                {
                    liveTail = Shell(tail);
                    liveParent.Children.Add(liveTail);
                    liveParent.Version++;
                    changed = true;
                }
```

Site 4 — `RevealChildren`, tail props update:

```csharp
                else if (PropsDiffer(liveTail.Props, tail.Props))
                {
                    liveTail.Props = tail.Props;
                    liveTail.Version++;
                    changed = true;
                }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasVersionTests"`
Expected: 9 passed.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasVersionTests.cs
git commit -m "perf: bump Version on reveal-time mutations of frozen nodes

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: `CanvasNodeView` memo (validation + props keyed on `Version`)

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasNodeView.razor` (markup ~lines 29-31, `Props<T>` ~lines 318-325)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs` (append)

**Interfaces:**
- Consumes: `CanvasNode.Version` (Tasks 1-3); `DrylCanvasRun.ApplyOp(CanvasOp)` — internal; `DrylCanvasRun.BeginCreate()` / `RevealSnapshot(CanvasSpec)` — internal.
- Produces: no new surface — behavior must stay pixel-identical, only the work per render drops.

- [ ] **Step 1: Write the failing tests**

Append to `DrylAiCanvasTests` (class already sets `JSInterop.Mode = JSRuntimeMode.Loose` and has a `Parse` helper):

```csharp
    [Fact]
    public void Patched_node_renders_its_new_props()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k"}}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("€10k", cut.Markup);

        cut.InvokeAsync(() => run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "s1",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "value": "€12k" }"""),
        }));

        cut.WaitForAssertion(() => Assert.Contains("€12k", cut.Markup));
    }

    [Fact]
    public void Tabs_shell_renders_once_its_children_stream_in()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        // The streaming tail is a tabs shell whose labels are known but whose
        // children have not started -> invalid (labels.Count != children.Count),
        // shown as a "waiting" skeleton.
        run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"t","type":"tabs","props":{"labels":["One","Two"]},"children":[]}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("waiting for tabs", cut.Markup);

        cut.InvokeAsync(() => run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"t","type":"tabs","props":{"labels":["One","Two"]},"children":[
                    {"id":"t1","type":"divider"},
                    {"id":"t2","type":"divider"}]},
                {"id":"d2","type":"divider"}]}}
            """)));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".canvas-waiting"));
            Assert.Contains("One", cut.Markup);
        });
    }
```

Note: these two tests also pass **without** the memo (they pin behavior). Their job is to catch a stale-memo regression the moment Task 4's implementation lands — run them before AND after the implementation.

- [ ] **Step 2: Run tests to verify they pass pre-implementation**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAiCanvasTests"`
Expected: all pass (old + 2 new) — baseline is green before touching the renderer.

- [ ] **Step 3: Implement the memo**

In `DRYL.Components.Agents/Canvas/CanvasNodeView.razor`, markup — replace:

```razor
        @{
            var error = CanvasCatalog.Validate(Node);
        }
```

with:

```razor
        @{
            EnsureMemo();
            var error = _memoError;
        }
```

In the `@code` block, add the memo fields and helper (next to `_seeded`):

```csharp
    // Parse + validate only on mutation: the patcher/reveal layers bump
    // CanvasNode.Version on every successful change (own props or children list),
    // so an unchanged stamp means both memos are still valid.
    private int _memoVersion = -1;
    private string? _memoError;
    private Type? _memoPropsType;
    private object? _memoProps;

    private void EnsureMemo()
    {
        if (_memoVersion == Node.Version) return;
        _memoVersion = Node.Version;
        _memoError = CanvasCatalog.Validate(Node);
        _memoProps = null;
        _memoPropsType = null;
    }
```

Replace `Props<T>`:

```csharp
    private T? Props<T>() where T : class
    {
        EnsureMemo();
        if (_memoPropsType == typeof(T)) return (T?)_memoProps;

        var json = Node.Props is { } p && p.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? p.GetRawText()
            : "{}";
        DisplayJson.TryParse<T>(json, out var value);
        _memoProps = value;
        _memoPropsType = typeof(T);
        return value;
    }
```

- [ ] **Step 4: Run the full canvas test suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Canvas"`
Expected: all pass — including the two new behavior pins and every pre-existing canvas test.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasNodeView.razor tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs
git commit -m "perf: memoize canvas node validation + props parse on Version stamp

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: `DrylMarkdown` content memo (Core)

**Files:**
- Modify: `DRYL.Components/Components/Surfaces/DrylMarkdown.razor` (`OnParametersSet` ~line 77, `BuildSegments` ~line 89)
- Test: `tests/DRYL.Components.Tests/DrylMarkdownTests.cs` (append)

**Interfaces:**
- Consumes: nothing from Tasks 1-4.
- Produces: `internal int ParseCount` on `DrylMarkdown` — test seam counting actual Markdig parses.

- [ ] **Step 1: Write the failing test**

Append to `DrylMarkdownTests`:

```csharp
    [Fact]
    public void Unchanged_content_is_not_reparsed()
    {
        var cut = Render<DrylMarkdown>(ps => ps.Add(p => p.Content, "# Hi"));
        var count = cut.Instance.ParseCount;

        cut.SetParametersAndRender(ps => ps.Add(p => p.Content, "# Hi"));
        Assert.Equal(count, cut.Instance.ParseCount);

        cut.SetParametersAndRender(ps => ps.Add(p => p.Content, "# Hi there"));
        Assert.Equal(count + 1, cut.Instance.ParseCount);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylMarkdownTests.Unchanged_content_is_not_reparsed"`
Expected: BUILD FAILURE — `DrylMarkdown` does not contain a definition for `ParseCount`.

- [ ] **Step 3: Implement the memo**

In `DRYL.Components/Components/Surfaces/DrylMarkdown.razor`, add fields next to `_segments`:

```csharp
    private string? _lastContent;
    private int _parseCount;

    /// <summary>Total Markdig parses performed — test seam for the content memo.</summary>
    internal int ParseCount => _parseCount;
```

In `OnParametersSet`, replace the bare `BuildSegments();` call:

```csharp
        if (Content != _lastContent)
        {
            _lastContent = Content;
            BuildSegments();
        }
```

In `BuildSegments`, immediately before `var document = Markdown.Parse(...)`:

```csharp
        _parseCount++;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylMarkdownTests"`
Expected: all pass (old + new) — the streaming-growth test still passes because every chunk changes the content.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Surfaces/DrylMarkdown.razor tests/DRYL.Components.Tests/DrylMarkdownTests.cs
git commit -m "perf: skip Markdig re-parse when DrylMarkdown content is unchanged

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Version bumps + CHANGELOG release cut + full verification

**Files:**
- Modify: `DRYL.Components/DRYL.Components.csproj` (`<Version>2.10.0</Version>` → `2.10.1`)
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj` (`<Version>0.8.0</Version>` → `0.8.1`)
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: publishable releases `v2.10.1` (core) and `agents-v0.8.1` (the publish workflow tags on push to main).

- [ ] **Step 1: Bump both versions**

`DRYL.Components/DRYL.Components.csproj`: `<Version>2.10.1</Version>`
`DRYL.Components.Agents/DRYL.Components.Agents.csproj`: `<Version>0.8.1</Version>`

- [ ] **Step 2: Update CHANGELOG.md**

Append the two entries to the existing `[Unreleased]` block under a new `### Changed` heading, then cut the release: rename `## [Unreleased]` to `## [2.10.1] — 2026-07-24` and add a fresh empty `## [Unreleased]` above it. The `### Changed` entries:

```markdown
### Changed
- `DrylAiCanvas` — (Agents 0.8.1) Canvas nodes memoize catalog validation and props deserialization on a new internal mutation stamp (`CanvasNode.Version`, bumped by patcher/reveal/purge): JSON parsing and validation now run only when a node actually changes, not on every render — during create streaming this removes O(nodes × deltas) re-parses.
- `DrylMarkdown` — Unchanged `Content` no longer re-parses through Markdig on every parent render; the parse runs only when the content actually changes.
```

(The pre-existing `[Unreleased]` entries — the `(Agents 0.8.0)` items — move into the `[2.10.1]` section along with everything else; that matches the established convention of core-keyed release sections carrying `(Agents x.y.z)`-tagged entries.)

- [ ] **Step 3: Full build + full test suite**

Run: `dotnet build DRYL.slnx`
Expected: 0 errors, 0 warnings.

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj CHANGELOG.md
git commit -m "chore: release core 2.10.1 + agents 0.8.1 (canvas render pipeline)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Self-Review Notes (already applied)

- Spec coverage: version stamp (Task 1-3), NodeView memo (Task 4), Markdown memo (Task 5), versions+changelog (Task 6), autoFlip coalescing deliberately dropped per spec §4.
- The `tabs`-shell memo trap (parent bump on `Children.Add`) is covered by Task 3 site 2 and pinned by `Tabs_shell_renders_once_its_children_stream_in` in Task 4.
- `Reveal` root replacement (`live.Root = Shell(snapRoot)`) creates a fresh instance — no bump needed, no test needed.
