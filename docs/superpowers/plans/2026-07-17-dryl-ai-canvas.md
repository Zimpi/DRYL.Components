# DrylAiCanvas Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Interactive chat artifacts for DRYL — the AI builds and iterates a live, fully interactive composition of DRYL components next to the chat, streaming node-by-node, morphing on patches, and receiving user interactions back as structured events.

**Architecture:** Hybrid (spec decision C): the chat agent gets two slim tools (`create_artifact` / `update_artifact`). Each tool internally runs a dedicated structured-streaming generation (raw JSON deltas → `PartialJsonReader<T>` → progressive snapshots) into an observable `DrylCanvasRun`. `DrylAiCanvas` renders the run's `CanvasSpec` tree recursively through a curated ~19-type catalog with validate-and-fallback per node. Patches are 4 op types applied one-by-one with animation; user interactions surface as `CanvasInteraction` via `OnInteraction`.

**Tech Stack:** .NET 8/9/10 multi-target, Blazor, Microsoft Agent Framework v1.10 (`Microsoft.Agents.AI`, `Microsoft.Extensions.AI`), xunit + bUnit tests, no npm.

**Spec:** `docs/superpowers/specs/2026-07-17-dryl-ai-canvas-design.md` — read it first.

## Global Constraints

- Branch: `feat/ai-canvas` (exists). Never push `v*` tags; never publish by hand.
- `DRYL.Components.Agents` version: **0.5.0 → 0.6.0** (Task 11; `publish.yml` does NOT publish the Agents package — no auto-release risk).
- `DRYL.Components` (core) version: **2.8.3 → 2.9.0** — only change is the new `dryl.motion.autoFlip` primitive (Task 9). Cutting 2.9.0 in `CHANGELOG.md` happens in the same commit as the core version bump (CLAUDE.md §7.0/§7.1).
- CLAUDE.md hard rules bind every task: tokens not literals (rule 2.1), no mode-assuming colors (2.2), fixed motion vocabulary `--dur-fast|med|slow` + `--ease-out|in-out|spring` (2.5), `AiState`/aura primitives only — no new AI visuals (2.10), every component animated incl. exit (2.12), `prefers-reduced-motion` honoured, no external deps (2.8).
- All new public types get XML doc comments (library — IntelliSense matters).
- Namespace for all new types: `DRYL.Components.Agents` (matches `DrylArtifactRun`, `DrylAgentAttachments`). Files live in `DRYL.Components.Agents/Canvas/`.
- JSON convention: camelCase, case-insensitive (`JsonSerializerDefaults.Web`) — same as `DisplayJson`.
- Test project: `tests/DRYL.Components.Tests` (net10.0). Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`. New test files go under `tests/DRYL.Components.Tests/Agents/Canvas/`.
- Reuse, don't duplicate: chart/timeline/stat validation classes from `DRYL.Components.Agents/Tools/DisplaySpecs.cs` are `internal` in the same assembly — use them directly.
- Commit after every task (steps say when). Commit messages end with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.

## File Structure (final state)

```
DRYL.Components.Agents/Canvas/
  CanvasSpec.cs           — CanvasSpec, CanvasNode, CanvasJson (model + serializer options)
  CanvasCatalog.cs        — type registry: Validate(node), IsContainer(type), props POCOs for non-chart types
  CanvasPatch.cs          — CanvasPatchDoc, CanvasOp
  CanvasPatcher.cs        — pure op application onto a CanvasSpec (returns skip reason or null)
  DrylCanvasRun.cs        — observable run handle (extends DrylRunBase)
  CanvasPrompt.cs         — compact schema text + create/update prompt builders
  DrylCanvasTools.cs      — create_artifact / update_artifact AI tools + replay seam
  CanvasInteraction.cs    — interaction record + ToPromptMessage()
  CanvasFormState.cs      — per-canvas input value store
  DrylAiCanvas.razor      — public canvas surface (panel chrome, aura, aria-live)
  DrylAiCanvas.razor.css  — scoped layout styles (tokens only)
  CanvasNodeView.razor    — internal recursive node renderer

DRYL.Components/wwwroot/js/dryl.js          — + dryl.motion.autoFlip / stopAutoFlip
tests/DRYL.Components.Tests/Agents/Canvas/  — one test file per unit (see tasks)
DRYL.Website/…                              — demo example + catalog entry (Task 10)
```

---

### Task 1: Canvas spec model + JSON handling

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasSpec.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasSpecTests.cs`

**Interfaces:**
- Produces: `CanvasSpec { string? Title; CanvasNode? Root }`, `CanvasNode { string Id; string Type; JsonElement? Props; List<CanvasNode>? Children; bool Removing }`, `static class CanvasJson { JsonSerializerOptions Options }`. All later tasks consume these exact names.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Text.Json;
using DRYL.Components.Agents;
using DRYL.Components.Agents.Generation;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasSpecTests
{
    private const string Sample = """
        { "title": "Q2", "root": { "id": "root", "type": "stack",
          "props": { "gap": "md" },
          "children": [ { "id": "rev", "type": "stat",
            "props": { "label": "Revenue", "value": "48.2k" } } ] } }
        """;

    [Fact]
    public void Deserializes_camelCase_tree()
    {
        var spec = JsonSerializer.Deserialize<CanvasSpec>(Sample, CanvasJson.Options)!;
        Assert.Equal("Q2", spec.Title);
        Assert.Equal("stack", spec.Root!.Type);
        Assert.Equal("rev", spec.Root.Children![0].Id);
        Assert.Equal("stat", spec.Root.Children[0].Type);
    }

    [Fact]
    public void PartialJsonReader_materializes_nodes_progressively()
    {
        var reader = new PartialJsonReader<CanvasSpec>(CanvasJson.Options);
        // Cut mid-way through the second node's props:
        var cut = Sample.IndexOf("\"value\"", StringComparison.Ordinal);
        var first = reader.Append(Sample[..cut]);
        Assert.NotNull(first?.Root);
        Assert.Equal("root", first!.Root!.Id);          // container already there
        var second = reader.Append(Sample[cut..]);
        Assert.Equal("48.2k", GetProp(second!.Root!.Children![0], "value"));
    }

    [Fact]
    public void Removing_flag_is_not_serialized()
    {
        var node = new CanvasNode { Id = "a", Type = "divider", Removing = true };
        var json = JsonSerializer.Serialize(node, CanvasJson.Options);
        Assert.DoesNotContain("removing", json, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetProp(CanvasNode n, string name) =>
        n.Props!.Value.TryGetProperty(name, out var v) ? v.GetString() : null;
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter CanvasSpecTests`
Expected: build FAILURE — `CanvasSpec` / `CanvasJson` do not exist.

- [ ] **Step 3: Implement the model**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DRYL.Components.Agents;

/// <summary>Shared JSON handling for canvas specs and patches (camelCase, case-insensitive).</summary>
public static class CanvasJson
{
    /// <summary>Web-default serializer options used for every canvas (de)serialization.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>An AI-generated artifact: a titled tree of catalog nodes rendered by <c>DrylAiCanvas</c>.</summary>
public sealed class CanvasSpec
{
    /// <summary>Short artifact title shown in the canvas header.</summary>
    public string? Title { get; set; }

    /// <summary>The root node — by convention a <c>stack</c> container.</summary>
    public CanvasNode? Root { get; set; }
}

/// <summary>One node of a canvas artifact. <see cref="Type"/> selects a curated catalog entry.</summary>
public sealed class CanvasNode
{
    /// <summary>Stable unique id — the anchor for patches, move animations and interaction events.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Catalog type key, e.g. <c>stack</c>, <c>stat</c>, <c>lineChart</c>, <c>button</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Type-specific properties; parsed and validated by <c>CanvasCatalog</c>.</summary>
    public JsonElement? Props { get; set; }

    /// <summary>Child nodes (container types only).</summary>
    public List<CanvasNode>? Children { get; set; }

    /// <summary>Transient exit flag: node plays its exit animation, then is purged. Never serialized.</summary>
    [JsonIgnore] public bool Removing { get; set; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter CanvasSpecTests`
Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasSpec.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasSpecTests.cs
git commit -m "feat(agents): canvas spec model (CanvasSpec/CanvasNode/CanvasJson)"
```

---

### Task 2: Catalog validation

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasCatalog.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasCatalogTests.cs`

**Interfaces:**
- Consumes: `CanvasNode`, `CanvasJson` (Task 1); `CartesianChartArgs`, `DonutChartArgs`, `TimelineArgs`, `StatSpec`, `DisplayJson.TryParse` from `DRYL.Components.Agents.Tools` (existing, internal, same assembly).
- Produces: `static class CanvasCatalog` with:
  - `string? Validate(CanvasNode node)` — null when valid, else a corrective **model-facing** sentence (style of `DisplaySpecs`: `"stat node 'rev': value must be non-empty."`).
  - `bool IsKnownType(string type)`; `bool IsContainer(string type)` (true for `stack`, `grid`, `card`, `tabs`); `bool IsInteractive(string type)` (true for `inputText`, `select`, `slider`, `toggle`).
  - Typed props POCOs used by the renderer: `StackNodeProps { string? Gap }`, `GridNodeProps { int Columns }`, `CardNodeProps { string? Title }`, `TabsNodeProps { List<string>? Labels }`, `MarkdownNodeProps { string? Content }`, `BadgeNodeProps { string? Text; string? Kind }`, `ProgressNodeProps { double Value; string? Label }`, `TableNodeProps { List<string>? Columns; List<List<string>>? Rows }`, `InputTextNodeProps { string? Name; string? Label; string? Placeholder; string? Value }`, `SelectNodeProps { string? Name; string? Label; List<string>? Options; string? Value }`, `SliderNodeProps { string? Name; string? Label; double Min; double Max; double? Step; double? Value }`, `ToggleNodeProps { string? Name; string? Label; bool? Value }`, `ButtonNodeProps { string? Label; string? Intent; string? Kind }` — all `internal sealed class`, camelCase JSON via `CanvasJson.Options`.

**Catalog rules to implement (from spec §3 + prompt in Task 5 — keep the three in sync):**

| type | rule |
| --- | --- |
| `stack` | gap null or `sm/md/lg` |
| `grid` | columns 1–4 |
| `card` | (no required props) |
| `tabs` | labels non-empty; labels.Count must equal Children count |
| `divider` | no props required |
| `markdown` | content non-empty |
| `stat` | reuses `StatSpec` fields: label+value non-empty, direction null/up/down/neutral |
| `badge` | text non-empty; kind null or default/success/warning/danger |
| `progress` | 0 ≤ value ≤ 100 |
| `table` | columns non-empty; every row has columns.Count cells; ≤ 30 rows |
| `timeline` | delegate to `TimelineArgs.Validate()` (wrap props as `{ events: … }` shape — props ARE `{ "events": [...] }`) |
| `lineChart` / `areaChart` / `barChart` | delegate to `CartesianChartArgs.Validate()` |
| `donutChart` | delegate to `DonutChartArgs.Validate()` |
| `inputText`/`select`/`slider`/`toggle` | name + label non-empty; select: options non-empty; slider: min < max |
| `button` | label + intent non-empty; kind null or primary/secondary |
| container without children allowed (may still be streaming); children on a non-container → invalid |
| unknown type → `"type 'X' is not in the canvas catalog."` |

- [ ] **Step 1: Write the failing tests** — cover: one happy case per category (container, chart via delegation, input, button), unknown type, children-on-leaf, tabs label/child mismatch, slider min≥max, progress out of range, button without intent. Use this shape:

```csharp
using System.Text.Json;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasCatalogTests
{
    private static CanvasNode Node(string type, string propsJson, params CanvasNode[] children) => new()
    {
        Id = "n1", Type = type,
        Props = JsonSerializer.Deserialize<JsonElement>(propsJson),
        Children = children.Length == 0 ? null : children.ToList(),
    };

    [Fact] public void Valid_stat_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("stat", """{ "label": "Revenue", "value": "48k" }""")));

    [Fact] public void Unknown_type_is_rejected() =>
        Assert.Contains("not in the canvas catalog", CanvasCatalog.Validate(Node("hologram", "{}")));

    [Fact] public void Children_on_leaf_are_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("stat",
            """{ "label": "a", "value": "b" }""", Node("divider", "{}"))));

    [Fact] public void Tabs_label_child_mismatch_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("tabs",
            """{ "labels": ["A", "B"] }""", Node("divider", "{}"))));

    [Fact] public void Chart_validation_is_delegated() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("lineChart",
            """{ "labels": ["Jan"], "series": [] }""")));   // empty series → CartesianChartArgs error

    [Fact] public void Button_needs_intent() =>
        Assert.Contains("intent", CanvasCatalog.Validate(Node("button", """{ "label": "Go" }""")));

    [Fact] public void Slider_needs_min_below_max() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("slider",
            """{ "name": "n", "label": "L", "min": 5, "max": 5 }""")));

    [Fact] public void Interactive_and_container_classification()
    {
        Assert.True(CanvasCatalog.IsContainer("grid"));
        Assert.False(CanvasCatalog.IsContainer("stat"));
        Assert.True(CanvasCatalog.IsInteractive("toggle"));
        Assert.False(CanvasCatalog.IsInteractive("button"));
    }
}
```

- [ ] **Step 2: Run to verify failure** — `dotnet test … --filter CanvasCatalogTests` → build failure (CanvasCatalog missing).

- [ ] **Step 3: Implement `CanvasCatalog`** — a switch over `node.Type`; parse props with `DisplayJson.TryParse<T>(node.Props?.GetRawText(), out var p)` (null/absent props → treat as `{}`); prefix every delegated error with `"{type} node '{id}': "`. Chart/timeline delegation example:

```csharp
case "lineChart" or "areaChart" or "barChart":
    if (!TryProps<CartesianChartArgs>(node, out var cart)) return Err(node, "props are not valid JSON.");
    return Prefix(node, cart!.Validate());
```

with helpers:

```csharp
private static bool TryProps<T>(CanvasNode n, out T? value) where T : class =>
    DisplayJson.TryParse(n.Props?.GetRawText() ?? "{}", out value);
private static string? Prefix(CanvasNode n, string? error) =>
    error is null ? null : $"{n.Type} node '{n.Id}': {error}";
private static string Err(CanvasNode n, string msg) => $"{n.Type} node '{n.Id}': {msg}";
```

Also validate the generic shape first: empty `Id` → error; `Children is { Count: > 0 }` on non-container → error.

- [ ] **Step 4: Run tests** — expect all pass.
- [ ] **Step 5: Commit** — `feat(agents): canvas catalog validation (~19 curated node types)`

---

### Task 3: Patch model + patcher

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasPatch.cs`, `DRYL.Components.Agents/Canvas/CanvasPatcher.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasPatcherTests.cs`

**Interfaces:**
- Consumes: `CanvasSpec`, `CanvasNode`, `CanvasJson`, `CanvasCatalog.Validate` (Tasks 1–2), `JsonMerge.Merge(JsonNode?, JsonNode?)` (existing, `DRYL.Components.Agents.Generation`).
- Produces:
  - `CanvasPatchDoc { List<CanvasOp>? Ops }`
  - `CanvasOp { string Op; string? Id; string? Parent; int? Index; JsonElement? Props; CanvasNode? Node }` (ops: `setProps`, `insert`, `remove`, `move`)
  - `static class CanvasPatcher { string? Apply(CanvasSpec spec, CanvasOp op) }` — mutates the spec; returns null on success, else a model-facing skip reason. `remove` only sets `node.Removing = true` (exit animation; purge is Task 4's job). `insert` validates the new node (subtree, recursively) via `CanvasCatalog.Validate` before touching the spec. `setProps` shallow-merges via `JsonMerge` and then re-validates the node — on invalid result, roll the props back and return the validation error. `move` re-parents `Id` under `Parent` at `Index` (clamped); moving a node into its own subtree → skip reason.

- [ ] **Step 1: Failing tests** — cases: setProps merges one key and keeps others; setProps producing invalid props rolls back; insert adds at index and validates subtree (invalid child → skipped, spec untouched); insert with duplicate id → skipped; remove marks `Removing` (node still present); remove unknown id → reason; move re-orders within parent and across parents; move into own subtree → reason; index clamping (index 99 → appended). Test skeleton:

```csharp
public class CanvasPatcherTests
{
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "grp", "type": "card", "children": [
                { "id": "b", "type": "divider" } ] } ] } }
        """, CanvasJson.Options)!;

    [Fact]
    public void SetProps_merges_shallow()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp {
            Op = "setProps", Id = "a",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }""") });
        Assert.Null(err);
        var a = spec.Root!.Children![0];
        Assert.Equal("+5%", a.Props!.Value.GetProperty("delta").GetString());
        Assert.Equal("A", a.Props!.Value.GetProperty("label").GetString());   // kept
    }

    [Fact]
    public void Move_into_own_subtree_is_skipped()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "grp", Parent = "b", Index = 0 });
        Assert.NotNull(err);
    }
    // …remaining cases per the list above, same style…
}
```

- [ ] **Step 2: Run → build failure.**
- [ ] **Step 3: Implement.** Core helpers inside `CanvasPatcher`: `FindNode(CanvasSpec, string id)` and `FindParent(CanvasSpec, string id)` — simple recursive walks (spec trees are small; no index needed). For setProps rollback keep `var before = node.Props;` and restore on validation failure. Skip reasons are model-facing sentences, e.g. `"op 'move': node 'grp' cannot be moved into its own subtree."`, `"op 'setProps': no node with id 'x'."`.
- [ ] **Step 4: Run tests → pass.**
- [ ] **Step 5: Commit** — `feat(agents): canvas patch ops (setProps/insert/remove/move) + patcher`

---

### Task 4: DrylCanvasRun

**Files:**
- Create: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasRunTests.cs`

**Interfaces:**
- Consumes: `DrylRunBase` (internal members `State`, `Error`, `Raise()`, `MarkCompleted()` are same-assembly accessible), `CanvasSpec`, `CanvasPatcher`, `CanvasOp`, `DrylRunError`.
- Produces (exact surface later tasks rely on):

```csharp
/// <summary>Observable handle to an AI-built canvas artifact rendered by <c>DrylAiCanvas</c>.</summary>
public sealed class DrylCanvasRun : DrylRunBase
{
    public DrylCanvasRun();                                  // public — created by the host page
    public CanvasSpec? Spec { get; }                         // live spec (progressively richer)
    public int Round { get; }                                // completed create/update generations
    public IReadOnlyCollection<string> ChangedIds { get; }   // ids touched by the latest ops
    internal void BeginGeneration();                         // → State=Streaming, ChangedIds cleared, Raise
    internal void ApplySnapshot(CanvasSpec snapshot);        // create-streaming: Spec = snapshot; Raise
    internal string? ApplyOp(CanvasOp op);                   // patcher + ChangedIds tracking; Raise; returns skip reason
    internal void CompleteGeneration(CanvasSpec final);      // Spec=final, Round++, State=Generated, Raise
    internal void FailGeneration(Exception ex);              // Error=…, State=None, Raise  (run stays alive!)
    public void Purge(string id);                            // drop a Removing node after its exit animation
}
```

Notes for the implementer:
- `ApplyOp` adds the affected ids to `ChangedIds` (`setProps`/`move` → `Id`; `insert` → `Node.Id`; `remove` → nothing, exit handles it).
- `CompleteGeneration` after an **update** run must NOT replace `Spec` (ops already mutated it) — give it an overload `CompleteGeneration()` without argument for the update path; the create path passes the final strict-parsed spec.
- `FailGeneration` mirrors the runner's convention (`Error != null` + `AiState.None`) but must NOT call `MarkCompleted()` — the canvas outlives a failed generation; the next `create_artifact` may succeed.
- `BeginGeneration` also clears `Error` (a retry starts clean).
- `Purge(id)` removes the node from its parent's `Children` and calls `Raise()`; unknown id is a no-op (exit animation may race a fresh create).

- [ ] **Step 1: Failing tests** — assert: initial `State` is `Thinking` (DrylRunBase default) and `Spec` null; `BeginGeneration` → `Streaming` + OnChange fired (count via `run.OnChange += () => n++`); `ApplySnapshot` sets Spec + raises; `ApplyOp` routes to patcher and records ChangedIds; `CompleteGeneration()` → `Generated`, Round incremented; `FailGeneration` → `Error != null`, `State == AiState.None`; `BeginGeneration` after failure clears `Error`; `Purge` drops a Removing node.
- [ ] **Step 2: Run → build failure.**
- [ ] **Step 3: Implement** (straightforward given the surface above; `ChangedIds` is a `HashSet<string>` exposed as `IReadOnlyCollection<string>`).
- [ ] **Step 4: Run tests → pass.**
- [ ] **Step 5: Commit** — `feat(agents): DrylCanvasRun observable canvas handle`

---

### Task 5: Prompts + DrylCanvasTools (create path)

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`, `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsCreateTests.cs`

**Interfaces:**
- Consumes: `DrylCanvasRun` (Task 4), `PartialJsonReader<CanvasSpec>`, `CanvasCatalog`, `AIFunctionFactory.Create` (pattern from `DrylDisplayTools`), `DrylAgentRunner.ExtractJsonDeltas` (existing internal static).
- Produces:

```csharp
/// <summary>Chat-agent tools that build and iterate a canvas artifact via a dedicated
/// structured-streaming sub-generation. Hand <see cref="All"/> to the chat agent.</summary>
public sealed class DrylCanvasTools
{
    /// <summary>Create the tools; <paramref name="generator"/> runs the artifact generations
    /// (a fresh session per call — generations are stateless, the current spec travels in the prompt).</summary>
    public static DrylCanvasTools Create(DrylCanvasRun run, AIAgent generator);

    /// <summary>Replay/demo/test seam: like <see cref="Create"/>, but generations come from
    /// <paramref name="generate"/> (prompt → raw JSON delta stream) instead of a live agent.</summary>
    public static DrylCanvasTools CreateReplay(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate);

    public AITool CreateArtifact { get; }   // tool name "create_artifact"
    public AITool UpdateArtifact { get; }   // tool name "update_artifact"  (Task 6)
    public IList<AITool> All { get; }
}
```

`CanvasPrompt` (internal static): `string SchemaText` — the compact catalog contract, verbatim:

```
You produce ONLY one JSON object — no markdown fences, no prose.
Shape: { "title": string, "root": Node }
Node: { "id": string, "type": string, "props": object, "children": Node[] }
- "id": short, unique, stable, kebab-case. Never reuse an id.
- "children" only on container types: stack, grid, card, tabs.
Types and props:
- stack { "gap": "sm"|"md"|"lg"? } — vertical layout; use as root.
- grid { "columns": 1|2|3|4 } — equal-width responsive grid.
- card { "title": string? } — glass card grouping its children.
- tabs { "labels": string[] } — exactly one child per label.
- divider { }
- markdown { "content": string } — rich text (headings, lists, tables).
- stat { "label": string, "value": string, "delta": string?, "direction": "up"|"down"|"neutral"? }
- badge { "text": string, "kind": "default"|"success"|"warning"|"danger"? }
- progress { "value": number 0..100, "label": string? }
- table { "columns": string[], "rows": string[][] } — max 30 rows.
- timeline { "events": [{ "title": string, "timestamp": string?, "text": string?, "kind": "default"|"success"|"warning"|"danger"? }] }
- lineChart|areaChart|barChart { "title": string?, "labels": string[], "series": [{ "name": string, "data": number[] }], "valueFormat": string? } — one value per label.
- donutChart { "title": string?, "segments": [{ "label": string, "value": number }], "valueFormat": string? } — max 6 segments.
- inputText { "name": string, "label": string, "placeholder": string?, "value": string? }
- select { "name": string, "label": string, "options": string[], "value": string? }
- slider { "name": string, "label": string, "min": number, "max": number, "step": number?, "value": number? }
- toggle { "name": string, "label": string, "value": boolean? }
- button { "label": string, "intent": string, "kind": "primary"|"secondary"? } — "intent" is a short
  machine-readable action id; clicking sends the intent plus all current input values back to you.
Interactive nodes (inputText/select/slider/toggle) each need a unique "name".
Prefer charts and stats over prose for numbers. Keep the artifact focused.
```

plus `string CreatePrompt(string brief, string? title)`:

```csharp
internal static string CreatePrompt(string brief, string? title) =>
    $"{SchemaText}\n\nBuild a new artifact{(title is null ? "" : $" titled \"{title}\"")} for this request:\n{brief}";
```

**`create_artifact` implementation (the heart of the task):**

```csharp
private async Task<string> CreateArtifactImpl(
    [Description("What the artifact should show, incl. all concrete data/numbers it needs.")] string brief,
    [Description("Short artifact title.")] string? title = null,
    CancellationToken ct = default)
{
    _run.BeginGeneration();
    var reader = new PartialJsonReader<CanvasSpec>(CanvasJson.Options);
    try
    {
        await foreach (var delta in _generate(CanvasPrompt.CreatePrompt(brief, title), ct))
        {
            var snapshot = reader.Append(delta);
            if (snapshot is not null) _run.ApplySnapshot(snapshot);
        }
        var final = JsonSerializer.Deserialize<CanvasSpec>(reader.Buffer, CanvasJson.Options);
        if (final?.Root is null)
            throw new InvalidOperationException("generator returned no artifact root");

        var problems = new List<string>();
        int nodes = 0, interactive = 0;
        Walk(final.Root, n => { nodes++; if (CanvasCatalog.IsInteractive(n.Type)) interactive++;
                                if (CanvasCatalog.Validate(n) is { } e) problems.Add(e); });
        _run.CompleteGeneration(final);
        var receipt = $"Artifact created: {nodes} elements, {interactive} inputs.";
        return problems.Count == 0 ? receipt
            : receipt + " Some elements were invalid and are shown as placeholders — fix via update_artifact: "
              + string.Join(" ", problems.Take(3));
    }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex)
    {
        _run.FailGeneration(ex);
        return "Artifact generation failed: " + ex.Message + " You may retry with a simpler brief.";
    }
}
```

with `private static void Walk(CanvasNode n, Action<CanvasNode> visit)` recursing over children. The live-agent `_generate` in `Create(run, generator)`:

```csharp
static Func<string, CancellationToken, IAsyncEnumerable<string>> LiveGenerate(AIAgent generator) =>
    (prompt, ct) =>
    {
        var session = generator.GetNewSession();
        var options = new ChatClientAgentRunOptions
        { ChatOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json } };
        return DrylAgentRunner.ExtractJsonDeltas(
            generator.RunStreamingAsync(prompt, session, options, ct), ct);
    };
```

> ⚠️ Verify the session API name before coding: `grep -rn "GetNewSession\|CreateSession" c:/Users/janzi/Desktop/DRYL/DRYL.Portfolio --include=*.cs --include=*.razor` and check existing usages in this repo's samples/tests. Use whatever `DrylAgentRunner` callers use in MAF v1.10. Everything else in this task is framework-independent.

Tool registration mirrors `DrylDisplayTools`: `AIFunctionFactory.Create(CreateArtifactImpl, "create_artifact", "Create a live visual artifact next to the chat …")`. Description must tell the chat model to put **all concrete data** into `brief` (the generator has no other context).

- [ ] **Step 1: Failing tests** — use `CreateReplay` with scripted deltas; invoke the tool function directly via `AIFunction.InvokeAsync` the same way `DrylDisplayToolsTests` does (check that file first and mirror its invocation style). Cases:
  - happy path: two-chunk scripted spec stream → intermediate `ApplySnapshot` observed (subscribe OnChange, capture `run.Spec?.Root?.Children?.Count` history), final `State == Generated`, `Round == 1`, receipt contains `"2 elements"`.
  - invalid node in final spec → receipt contains `"placeholders"` and the corrective sentence.
  - generator throws → `run.Error != null`, `run.State == AiState.None`, receipt starts with `"Artifact generation failed"`.
- [ ] **Step 2: Run → failure.**
- [ ] **Step 3: Implement `CanvasPrompt` + `DrylCanvasTools` (create tool only; `UpdateArtifact` throws `NotImplementedException` until Task 6 — do NOT include it in `All` yet).**
- [ ] **Step 4: Run tests → pass.**
- [ ] **Step 5: Commit** — `feat(agents): create_artifact tool with streaming canvas generation`

---

### Task 6: DrylCanvasTools (update path)

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`, `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsUpdateTests.cs`

**Interfaces:**
- Consumes: `CanvasPatchDoc`/`CanvasOp` (Task 3), `DrylCanvasRun.ApplyOp` (Task 4).
- Produces: working `UpdateArtifact` tool; `All` now contains both tools. `CanvasPrompt.UpdatePrompt(string brief, string currentSpecJson)`:

```csharp
internal static string UpdatePrompt(string brief, string currentSpecJson) =>
    SchemaText + """


        You UPDATE the existing artifact below. Output ONLY: { "ops": [Op, …] }
        Op is one of:
        - { "op": "setProps", "id": string, "props": object }  — shallow-merge props into the node
        - { "op": "insert", "parent": string, "index": number, "node": Node }
        - { "op": "remove", "id": string }
        - { "op": "move", "id": string, "parent": string, "index": number }
        Use existing ids; new nodes get fresh unique ids. Emit the smallest op set that fulfils the request.

        Current artifact:
        """ + currentSpecJson + "\n\nRequest:\n" + brief;
```

**Staged op application** — an op may be truncated mid-stream, so only ops strictly before the last parsed one are safe to apply during streaming; the rest apply at stream end:

```csharp
private async Task<string> UpdateArtifactImpl(
    [Description("What should change, incl. any new data needed.")] string brief,
    CancellationToken ct = default)
{
    if (_run.Spec?.Root is null)
        return "There is no artifact yet — call create_artifact first.";
    _run.BeginGeneration();
    var reader = new PartialJsonReader<CanvasPatchDoc>(CanvasJson.Options);
    var applied = 0;
    var skipped = new List<string>();
    try
    {
        var current = JsonSerializer.Serialize(_run.Spec, CanvasJson.Options);
        await foreach (var delta in _generate(CanvasPrompt.UpdatePrompt(brief, current), ct))
        {
            var ops = reader.Append(delta)?.Ops;
            while (ops is not null && applied < ops.Count - 1)   // last op may still be truncated
                await ApplyStaggeredAsync(ops[applied++], skipped, ct);
        }
        var final = JsonSerializer.Deserialize<CanvasPatchDoc>(reader.Buffer, CanvasJson.Options)?.Ops
                    ?? new List<CanvasOp>();
        while (applied < final.Count)
            await ApplyStaggeredAsync(final[applied++], skipped, ct);
        _run.CompleteGeneration();
        var receipt = $"Artifact updated: {applied - skipped.Count} changes applied.";
        return skipped.Count == 0 ? receipt
            : receipt + $" {skipped.Count} ops skipped: " + string.Join(" ", skipped.Take(3));
    }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex)
    {
        _run.FailGeneration(ex);
        return "Artifact update failed: " + ex.Message;
    }
}

// One op per beat so the user sees a choreography, not a jump (cadence like DrylAgentAttachments).
private async Task ApplyStaggeredAsync(CanvasOp op, List<string> skipped, CancellationToken ct)
{
    if (_run.ApplyOp(op) is { } reason) skipped.Add(reason);
    else await Task.Delay(OpStaggerMs, ct);   // const int OpStaggerMs = 260;
}
```

- [ ] **Step 1: Failing tests** — cases: (a) two ops streamed in three chunks where chunk 2 truncates op 2 → after chunk 2 only op 1 applied (assert via OnChange history / `ChangedIds`), both applied at end, receipt `"2 changes applied"`; (b) op on unknown id → skipped, receipt contains `"skipped"`, spec unchanged; (c) update without prior artifact → corrective receipt, no state change; (d) remove op → target node has `Removing == true` (not gone).
- [ ] **Step 2: Run → failure.** (NotImplementedException / missing prompt)
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run → pass. Also re-run Task 5 tests (`--filter DrylCanvasTools`).**
- [ ] **Step 5: Commit** — `feat(agents): update_artifact tool with staged patch-op streaming`

---

### Task 7: CanvasInteraction + form state

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasInteraction.cs`, `DRYL.Components.Agents/Canvas/CanvasFormState.cs`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasInteractionTests.cs`

**Interfaces:**
- Produces:

```csharp
/// <summary>A user interaction inside a canvas artifact (button click), carrying the intent
/// and a snapshot of every input value. Feed <see cref="ToPromptMessage"/> to the chat agent.</summary>
public sealed record CanvasInteraction(
    string Intent, string NodeId, IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>Structured chat message describing this interaction — send it as the next
    /// user turn so the assistant can react (typically with update_artifact).</summary>
    public string ToPromptMessage() =>
        "The user interacted with the artifact. intent: \"" + Intent + "\", values: "
        + JsonSerializer.Serialize(Values, CanvasJson.Options)
        + ". React accordingly; update the artifact via update_artifact if appropriate.";
}

/// <summary>Live values of a canvas's interactive nodes, keyed by their "name" prop.</summary>
public sealed class CanvasFormState
{
    public object? Get(string name);
    public T? Get<T>(string name);                       // typed convenience (returns default on miss)
    public void Set(string name, object? value);         // fires OnChanged
    public IReadOnlyDictionary<string, object?> Snapshot();  // defensive copy
    public event Action? OnChanged;
}
```

- [ ] **Step 1: Failing tests** — Set/Get roundtrip incl. typed `Get<double>`; `Snapshot` is a copy (mutating the state afterwards doesn't change it); `OnChanged` fires on Set; `ToPromptMessage` contains intent and serialized values (`Assert.Contains("\"budget\":42", …)` — camelCase key preserved as given).
- [ ] **Step 2: Run → failure. Step 3: Implement (Dictionary-backed, trivial). Step 4: Run → pass.**
- [ ] **Step 5: Commit** — `feat(agents): CanvasInteraction + CanvasFormState`

---

### Task 8: Renderer — CanvasNodeView + DrylAiCanvas

The biggest task. Read `DRYL.Components.Agents/Display/DrylAgentAttachments.razor` and `DRYL.Components/Components/AI/DrylToolCall.razor` first — the render-fragment mapping and the aura-host idiom below come from them.

**Files:**
- Create: `DRYL.Components.Agents/Canvas/CanvasNodeView.razor` (internal recursive renderer)
- Create: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor`, `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1–7; `DrylPresence` (`Visible`, `Appear`, `Transition`, `Speed`, `OnExited`), `DrylAuraElements` + `AuraLifecycle` + `AiAuraCss.Append` (aura idiom), `DrylAiIndicator` (`State`), catalog components (see mapping).
- Produces: `<DrylAiCanvas Run="…" OnInteraction="…" Class="…" />`.

**⚠️ Parameter-name verification first:** before writing markup, grep the exact `[Parameter]` names of every mapped component:
`grep -n "\[Parameter\] public" DRYL.Components/Components/{Layout/DrylStack.razor,Layout/DrylGrid.razor,Layout/DrylTabs.razor,Layout/DrylTab.razor,Surfaces/DrylCard.razor,Surfaces/DrylMarkdown.razor,Data/DrylBadge.razor,Feedback/DrylProgress.razor,Feedback/DrylSkeleton.razor,Data/DrylTimeline.razor,Data/DrylTimelineItem.razor,Data/DrylStat.razor,Inputs/DrylInputText.razor,Inputs/DrylSelect.razor,Inputs/DrylSlider.razor,Inputs/DrylToggle.razor,Actions/DrylButton.razor}`
Adjust the markup below to the real names — the mapping targets and behavior stay as specified. Known already: `DrylStat(Label, Value, Delta, Direction, Ai)`, `DrylBadge(Kind: BadgeKind, ChildContent)`, `DrylProgress(Value, Max, LabelText, Ai)`, `DrylSlider(Label, Min, Max, Step)`, `DrylToggle(Label)`, `DrylSelect(Label, Items: IEnumerable<SelectItem>, Ai)`, charts as used in `DrylAgentAttachments`. Inputs are `InputBase`-derived → bind `Value`/`ValueChanged` (works outside `EditForm` thanks to the existing `SetParametersAsync` override).

**Node → component mapping (behavioral contract):**

| type | renders |
| --- | --- |
| `stack` | `DrylStack` (Gap sm/md/lg → `StackGap`; default Md) around child views |
| `grid` | `DrylGrid` with the given column count |
| `card` | `DrylCard`; title (if any) as a heading line inside |
| `tabs` | `DrylTabs` with one `DrylTab` per label, child i inside tab i |
| `divider` | `DrylDivider` |
| `markdown` | `DrylMarkdown Content=…` |
| `stat` | `DrylStat` with parsed direction (reuse `StatSpec.ParsedDirection`), `Ai="AiState.Generated"` |
| `badge` | `DrylBadge` with mapped `BadgeKind` |
| `progress` | `DrylProgress` |
| `table` | build a markdown pipe table from columns/rows (escape `\|` in cells as `\\|`) → `DrylMarkdown`. (Deliberate: avoids the generic `DrylTable<T>` API for AI-generated string data.) |
| `timeline` | `DrylTimeline` + items (copy the fragment from `DrylAgentAttachments.Timeline`) |
| `lineChart`/`areaChart`/`barChart`/`donutChart` | exactly like `DrylAgentAttachments` (`Ai="AiState.Generated"`) |
| `inputText`/`select`/`slider`/`toggle` | bound to `CanvasFormState` via the node's `name` prop; initial `value` prop seeds the form state once |
| `button` | `DrylButton` (`kind` primary/secondary → `ButtonVariant`); click → `OnIntent` callback |
| invalid props / unknown type | fallback: `DrylSkeleton` + a `--fg-dim` caption `"waiting for {type}…"` — this is BOTH the still-streaming placeholder and the invalid-node fallback (they're the same state: props don't validate yet) |

**`CanvasNodeView.razor` skeleton (recursive; cascade the shared pieces):**

```razor
@namespace DRYL.Components.Agents

@if (Node.Removing)
{
    <DrylPresence Visible="false" Transition="PresenceTransition.SlideUp"
                  OnExited="() => Run.Purge(Node.Id)">
        @Body
    </DrylPresence>
}
else
{
    <DrylPresence @key="Node.Id" Visible="true" Appear
                  Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Slow">
        <div class="canvas-node" data-cid="@Node.Id">@Body</div>
    </DrylPresence>
}

@code {
    [Parameter, EditorRequired] public CanvasNode Node { get; set; } = default!;
    [CascadingParameter] internal DrylCanvasRun Run { get; set; } = default!;
    [CascadingParameter] internal CanvasFormState Form { get; set; } = default!;
    [CascadingParameter(Name = "CanvasIntent")] internal EventCallback<CanvasInteraction> OnIntent { get; set; }

    private RenderFragment Body => builder => { /* switch over CanvasCatalog-validated Node.Type,
        mapping table above; invalid/unknown → skeleton fallback. Children render as
        <CanvasNodeView Node="child" /> — the cascading params flow down automatically. */ };
}
```

(The `data-cid` attribute is the FLIP anchor for Task 9. `@key="Node.Id"` keeps moves as moves, not remove+add.)

**`DrylAiCanvas.razor` (public host — aura idiom copied from `DrylToolCall`):**

```razor
@namespace DRYL.Components.Agents
@implements IDisposable

<div class="@RootCssClass" @attributes="AdditionalAttributes">
    <DrylAuraElements Aura="_aura" GenTick="_genTick" />
    <div class="canvas-head">
        <span class="canvas-title">@(Run?.Spec?.Title ?? "Artifact")</span>
        <DrylAiIndicator State="@(Run?.State ?? AiState.None)">@StatusLabel</DrylAiIndicator>
    </div>
    <div class="canvas-live" aria-live="polite">@_announcement</div>
    <div class="canvas-body" @ref="_bodyEl">
        @if (Run?.Error is not null)
        {
            <DrylAlert Kind="DrylAlert.AlertKind.Danger" Title="Artifact failed">@Run.Error.Message</DrylAlert>
        }
        else if (Run?.Spec?.Root is { } root)
        {
            <CascadingValue Value="Run" IsFixed="true">
            <CascadingValue Value="_form" IsFixed="true">
            <CascadingValue Name="CanvasIntent" Value="_intentCallback">
                <CanvasNodeView Node="root" />
            </CascadingValue></CascadingValue></CascadingValue>
        }
        else
        {
            <DrylEmptyState Title="No artifact yet">Ask the assistant to create one.</DrylEmptyState>
        }
    </div>
</div>

@code {
    /// <summary>The canvas run to render.</summary>
    [Parameter] public DrylCanvasRun? Run { get; set; }

    /// <summary>Raised when the user triggers a button intent inside the artifact.
    /// Send <c>interaction.ToPromptMessage()</c> as the next chat turn to route it to the AI.</summary>
    [Parameter] public EventCallback<CanvasInteraction> OnInteraction { get; set; }

    /// <summary>Extra CSS class(es) merged onto the canvas root.</summary>
    [Parameter] public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? AdditionalAttributes { get; set; }
    // OnParametersSet: subscribe/unsubscribe Run.OnChange exactly like DrylAgentAttachments
    // (ReferenceEquals guard, InvokeAsync(StateHasChanged)); reset _form on run swap.
    // Aura lifecycle: _aura.Sync(Run?.State ?? AiState.None, …) + _genTick bump on ->Generated,
    // RootCssClass = "ai-canvas glass-card" + AiAuraCss.Append(...) + Class — verbatim DrylToolCall idiom.
    // _announcement: "Building artifact…" (Streaming), "Artifact updated, N changes." (Generated, Round>1),
    // "Artifact ready." (Generated, Round==1), "Artifact failed." (Error).
    // StatusLabel: Streaming→"Building", Thinking→"Working", Generated→"Ready", None→"Idle".
    // _intentCallback = EventCallback.Factory.Create<CanvasInteraction>(this, i => OnInteraction.InvokeAsync(i));
}
```

**`DrylAiCanvas.razor.css`** — tokens only; layout for `.canvas-head` (flex, `gap: var(--sp-3)`, `padding: var(--sp-4)`, `border-bottom: 1px solid var(--line)`), `.canvas-body` (`padding: var(--sp-4)`, `display:flex; flex-direction:column; gap: var(--sp-4)`, `overflow:auto`), `.canvas-title` (`color: var(--fg)`), `.canvas-live` visually hidden (absolute, 1px clip pattern). Remember: scoped CSS doesn't reach child components — style children via `::deep` if needed (known gotcha).

- [ ] **Step 1: Failing bUnit tests** (mirror setup style of `tests/DRYL.Components.Tests/Agents/DrylAgentAttachmentsTests.cs` — read it first; it shows required service registrations / JSInterop setup):
  - renders stat node text after `ApplySnapshot`;
  - invalid node renders the skeleton fallback (assert a `.skeleton`-ish marker / "waiting for" text);
  - `aria-live` region announces after `CompleteGeneration`;
  - button click raises `OnInteraction` with intent and current form values (set an inputText value first through the bound component);
  - `Run.Error` renders the danger alert;
  - node with `Removing` set renders a `DrylPresence` with `Visible="false"` (assert exit class) and `Purge` drops it after `OnExited` invocation.
- [ ] **Step 2: Run → failure. Step 3: Implement (markup above + mapping table; verify param names per the grep note). Step 4: Run → pass; run the FULL test suite once (`dotnet test`).**
- [ ] **Step 5: Verify both color modes + reduced motion manually later in Task 10's demo (note it in the PR text).**
- [ ] **Step 6: Commit** — `feat(agents): DrylAiCanvas renderer with recursive catalog node view`

---

### Task 9: FLIP move-glide (`dryl.motion.autoFlip`) — core library

**Files:**
- Modify: `DRYL.Components/wwwroot/js/dryl.js` (inside the existing `dryl.motion` namespace — read the file's structure first)
- Modify: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor` (wire up)
- Modify: `CHANGELOG.md`, `DRYL.Components/DRYL.Components.csproj` (core 2.8.3 → 2.9.0, cut release — §7.0/7.1)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasFlipTests.cs`

**Interfaces:**
- Produces: `dryl.motion.autoFlip(rootEl)` / `dryl.motion.stopAutoFlip(rootEl)` — observes `[data-cid]` descendants; whenever their positions change between frames, plays a FLIP transform glide. Compositor-only (transform), fixed vocabulary, reduced-motion aware.

- [ ] **Step 1: Implement the JS primitive** (no unit test target for JS; the C# test covers the interop wiring):

```javascript
// FLIP glide for AI-canvas reflows: remembers [data-cid] rects and, whenever a DOM
// mutation moves one, inverts the delta and lets it transition back to identity.
const _flipState = new WeakMap();
motion.autoFlip = (root) => {
    if (!root || _flipState.has(root)) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const rects = new Map();
    const capture = () => {
        rects.clear();
        for (const el of root.querySelectorAll('[data-cid]'))
            rects.set(el.getAttribute('data-cid'), el.getBoundingClientRect());
    };
    const play = () => {
        for (const el of root.querySelectorAll('[data-cid]')) {
            const prev = rects.get(el.getAttribute('data-cid'));
            if (!prev) continue;
            const now = el.getBoundingClientRect();
            const dx = prev.left - now.left, dy = prev.top - now.top;
            if (Math.abs(dx) < 1 && Math.abs(dy) < 1) continue;
            el.style.transition = 'none';
            el.style.transform = `translate(${dx}px, ${dy}px)`;
            requestAnimationFrame(() => {
                el.style.transition = 'transform var(--dur-med) var(--ease-spring)';
                el.style.transform = '';
                el.addEventListener('transitionend', () => { el.style.transition = ''; }, { once: true });
            });
        }
        capture();
    };
    const observer = new MutationObserver(() => play());
    observer.observe(root, { childList: true, subtree: true, attributes: false });
    capture();
    _flipState.set(root, observer);
};
motion.stopAutoFlip = (root) => {
    const obs = root && _flipState.get(root);
    if (obs) { obs.disconnect(); _flipState.delete(root); }
};
```

(Adapt the attachment point to dryl.js's actual module shape — it already exposes `dryl.motion.onExit` etc.; add these two alongside. Compositor rule: only `transform` is animated — no layout properties.)

- [ ] **Step 2: Wire into `DrylAiCanvas`** — `@inject IJSRuntime JS`, `@implements IAsyncDisposable` (keep the existing IDisposable for the run unsubscribe or merge into async dispose): in `OnAfterRenderAsync(firstRender)` call `dryl.motion.autoFlip` on `_bodyEl` guarded by an `_attached` flag (prerender-guard convention — see any JS-interop component, e.g. `DrylPresence`); in dispose, `stopAutoFlip` inside a `try { … } catch (JSDisconnectedException) {}`.
- [ ] **Step 3: bUnit test** — using bUnit's `JSInterop.SetupVoid("dryl.motion.autoFlip", _ => true)`, assert the invocation happens after first render and `stopAutoFlip` on dispose.
- [ ] **Step 4: Run tests → pass (full suite).**
- [ ] **Step 5: Core release bookkeeping in the SAME commit** — `DRYL.Components.csproj` `<Version>2.9.0</Version>`; `CHANGELOG.md`: rename `[Unreleased]` to `## [2.9.0] - <today>` with `### Added` entry:
  `- dryl.motion — New autoFlip/stopAutoFlip primitive: FLIP position-glide for [data-cid] children (powers DrylAiCanvas move ops); reduced-motion aware, transform-only`
  and a fresh empty `[Unreleased]` above.
- [ ] **Step 6: Commit** — `feat(release): bump version to 2.9.0 and add dryl.motion.autoFlip FLIP primitive`

---

### Task 10: Website demo + ComponentCatalog

**Files (in `c:/Users/janzi/Desktop/DRYL/DRYL.Website` — separate repo, separate commit!):**
- Create: `Components/Examples/Agents/CanvasArtifacts.razor`
- Modify: `Components/Pages/DemoAgents.razor` (new section), `Components/ComponentCatalog.cs` (register `DrylAiCanvas`)

**Interfaces:**
- Consumes: `DrylCanvasTools.CreateReplay`, `DrylCanvasRun`, `DrylAiCanvas`, `CanvasInteraction.ToPromptMessage`. The website references the library via ProjectReference (verify: `grep -n "ProjectReference\|PackageReference.*DRYL" DRYL.Website/*.csproj` — if it's a PackageReference, switch the demo work to after-publish or a local feed; ask the maintainer).

- [ ] **Step 1: Read `Components/Examples/Agents/StructuredGeneration.razor` and `DemoAgents.razor`** — mirror the embedded-example framework (`DemoExample` wrapper) and the replay-without-API-key approach used there.
- [ ] **Step 2: Build `CanvasArtifacts.razor`** — layout: chat pane (scripted `runner.Replay` conversation) + `DrylAiCanvas`. Script:
  1. Scripted user message "Zeig mir die Q2-Umsatzanalyse" → replayed assistant turn whose trace contains a `create_artifact` tool call; the demo drives `DrylCanvasTools.CreateReplay(run, ScriptedDeltas)` where `ScriptedDeltas` yields a revenue-dashboard `CanvasSpec` JSON (stack → grid of 3 stats → lineChart → card with select `region` + button `intent:"breakdown-by-region"`) in ~30-char chunks with `Task.Delay(35)` between chunks — the visible node-by-node build-up.
  2. Clicking the artifact's button → `OnInteraction` handler appends the interaction message to the chat and replays an update generation streaming ops (insert barChart per region, setProps on the lineChart) — the visible morph.
  Culture note: any numeric literals interpolated into the scripted JSON must use `FormattableString.Invariant` (known German-locale gotcha).
- [ ] **Step 3: Register in `ComponentCatalog`** — entry for `DrylAiCanvas` (category: Agents/AI; follow the existing record shape in that file).
- [ ] **Step 4: Runtime verification (REQUIRED — use the project's `verify` skill):** launch the docs website, open the Agents page, and check with Playwright: artifact streams in node-by-node; button click triggers the morph; ops glide (autoFlip); both color modes (`data-dryl-mode` flip); 375px viewport; reduced motion (emulate) still fully usable. Screenshot the money shot.
- [ ] **Step 5: Commit (website repo)** — `feat(agents-demo): DrylAiCanvas interactive artifact showcase + catalog entry`

---

### Task 11: Agents release bookkeeping + final verification

**Files:**
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj` (0.5.0 → 0.6.0), `CHANGELOG.md`, `README.md` (only if it documents the Agents tool list — check)

- [ ] **Step 1: CHANGELOG entries** (under the CURRENT `[Unreleased]` — the 2.9.0 cut happened in Task 9; follow the file's existing style for Agents-package entries — read how 0.4.0/0.5.0 were recorded first):
  - `### Added` — `DrylAiCanvas` — Interactive chat artifacts: AI builds/iterates a live DRYL-component composition (Agents 0.6.0); `DrylCanvasTools` — `create_artifact`/`update_artifact` chat-agent tools with streaming sub-generation + replay seam; `DrylCanvasRun`, `CanvasInteraction`, curated 19-type canvas catalog.
- [ ] **Step 2: Bump Agents `<Version>` to 0.6.0.**
- [ ] **Step 3: Full verification:** `dotnet build DRYL.sln` (or the repo's actual solution file — check root) and `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj` → all green. `node scripts/check-light-sync.mjs` → green (no dryl.css change expected, run it anyway).
- [ ] **Step 4: Commit** — `feat(release): DRYL.Components.Agents 0.6.0 — DrylAiCanvas interactive chat artifacts`
- [ ] **Step 5: Finish the branch** — use superpowers:finishing-a-development-branch (merge/PR decision belongs to the maintainer).

---

## Self-Review Notes (already applied)

- Spec §10 said Agents 0.4.0; reality is 0.5.0 → plan targets **0.6.0**, spec corrected.
- Spec's "auto-send interactions to the AI" is refined to `OnInteraction` + `ToPromptMessage()` (one line of host wiring): sending a chat turn requires the host's agent/session, which the canvas cannot own. Demo (Task 10) shows the wiring.
- Spec's `table` node renders through a generated markdown table (`DrylMarkdown`) instead of generic `DrylTable<T>` — deliberate, documented in Task 8.
- FLIP glide requires one new core primitive (`dryl.motion.autoFlip`) → core 2.9.0; allowed by spec §10's caveat clause.
- Two open verification points are flagged inline with exact grep commands: MAF session-creation API name (Task 5) and website ProjectReference vs PackageReference (Task 10).
