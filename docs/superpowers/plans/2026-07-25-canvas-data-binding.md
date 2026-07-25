# Canvas Data Binding (Phase 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A canvas node reads its values from a registered host data source instead of from prompt text — including the A1 move of the renderer into the core package.

**Architecture:** The canvas renderer (`CanvasSpec`, catalog, `CanvasNodeView`, patcher, form state) moves from `DRYL.Components.Agents` to `DRYL.Components` under namespace `DRYL.Components.Canvas`, with a new dumb `DrylCanvas` component in `DRYL.Components`. `DrylAiCanvas` stays in the Agents package and wraps `DrylCanvas`. A singleton `CanvasDataRegistry` holds named host sources; a scoped `ICanvasDataService` runs them; a per-canvas `CanvasDataBinder` dedupes by `source + resolved params`, drives four refresh triggers, and feeds mapped props into `CanvasNodeView`. A `CanvasPulseTracker` in the core is the single source of "this node just changed" for both the AI patcher and the data binder.

**Tech Stack:** .NET 8/9/10, Blazor (Server + WASM), `System.Text.Json`, bUnit + xUnit, Microsoft.Agents.AI (Agents package only).

## Global Constraints

- Core `DRYL.Components`: `<Version>` 2.11.0 → **2.12.0** (MINOR, purely additive).
- Agents `DRYL.Components.Agents`: `<Version>` 0.9.0 → **0.10.0** (MINOR = breaking in 0.x).
- Both version bumps + the cut `CHANGELOG.md` release land in the **final** commit of the phase, not earlier — a half-done push must not publish.
- `publish.yml` already publishes **both** packages (core tag `v<ver>`, agents tag `agents-v<ver>`). No workflow change needed; verify only.
- No new CSS tokens, durations, easings or colors. Refresh visuals reuse the existing change-pulse (`.canvas-pulse`) and `DrylStat.CountUp`.
- No `[Obsolete]` aliases for moved types (roadmap §5.2).
- Every icon-only button gets `DrylTooltip` + `aria-label` (CLAUDE.md 2.11).
- All numeric string interpolation for CSS/SVG/receipts uses `FormattableString.Invariant`.
- `data` on a node is optional; a node without it behaves exactly as today (A2).
- `Ai`/`AiState` vocabulary unchanged — no new AI states.
- Tests live in `tests/DRYL.Components.Tests/`; canvas tests move `Agents/Canvas/` → `Canvas/` for the moved types, Agents-only tests stay under `Agents/Canvas/`.

---

## File Structure

**Core — new folder `DRYL.Components/Canvas/` (namespace `DRYL.Components.Canvas` unless noted):**

| File | Responsibility |
| --- | --- |
| `CanvasSpec.cs` | `CanvasJson` (options + `TryParse`), `CanvasSpec`, `CanvasNode` (+ new `Data`), `CanvasDataBinding` |
| `CanvasPropTypes.cs` | internal per-type prop classes incl. canvas-local chart/stat/timeline prop types |
| `CanvasCatalog.cs` | type sets, `Validate(node)`, `Validate(node, context)`, `CanvasValidationContext`, shape↔type table |
| `CanvasPatch.cs` | `CanvasPatchDoc`, `CanvasOp` |
| `CanvasPatcher.cs` | in-place op application |
| `CanvasJsonMerge.cs` | internal deep JSON merge (core copy — Agents' public `JsonMerge` stays put) |
| `CanvasFormState.cs` | live interactive values + `OnChanged` |
| `CanvasInteraction.cs` | interaction record + `ToPromptMessage()` |
| `CanvasPulseTracker.cs` | monotonic per-node change stamps (one truth for AI patch + data refresh) |
| `CanvasData.cs` | `CanvasData` + `Scalar/Series/Segments/Rows` shapes + `CanvasDataShape` |
| `CanvasDataRegistry.cs` | singleton registry, `CanvasDataSource`, descriptor derivation, `CanvasDataContext` |
| `CanvasDataService.cs` | `ICanvasDataService` + `CanvasDataService` + `CanvasInvalidation` |
| `CanvasDataBinder.cs` | per-canvas binder: keys, dedupe, four triggers, states |
| `CanvasDataMapper.cs` | shape → node props |
| `CanvasDataPrompt.cs` | descriptor list → model-facing prompt block |
| `CanvasNodeView.razor` | recursive renderer (now binding-aware) |
| `CanvasContext.cs` | the single cascaded per-canvas context |

**Core — elsewhere:**
- `DRYL.Components/Components/Ai/DrylCanvas.razor` + `.razor.css` (namespace `DRYL.Components`)
- `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs` — `AddDrylCanvasDataSource`
- `DRYL.Components/Extensions/ServiceCollectionExtensions.cs` — register registry + data service
- `DRYL.Components/wwwroot/js/dryl-canvas.js` (moved)
- `DRYL.Components/_Imports.razor` — `@using DRYL.Components.Canvas`

**Agents — keeps:** `Canvas/DrylAiCanvas.razor` (+ css), `Canvas/DrylCanvasRun.cs`, `Canvas/DrylCanvasTools.cs`, `Canvas/CanvasPrompt.cs`, `Canvas/CanvasStreamReveal.cs`; `_Imports.razor` gains `@using DRYL.Components.Canvas`.

**Website:** `_Imports.razor` `@using DRYL.Components.Canvas`; `ComponentCatalog.cs` new `DrylCanvas` entry; `Components/Pages/DemoCanvas.razor`; `Components/Examples/Canvas/CanvasDataBinding.razor`.

---

### Task 1: Move the renderer into the core (A1) — mechanical

**Files:**
- Create: `DRYL.Components/Canvas/CanvasSpec.cs`, `CanvasPropTypes.cs`, `CanvasCatalog.cs`, `CanvasPatch.cs`, `CanvasPatcher.cs`, `CanvasJsonMerge.cs`, `CanvasFormState.cs`, `CanvasInteraction.cs`, `CanvasPulseTracker.cs`, `CanvasContext.cs`, `CanvasNodeView.razor`
- Create: `DRYL.Components/Components/Ai/DrylCanvas.razor` + `.razor.css`
- Create: `DRYL.Components/wwwroot/js/dryl-canvas.js`
- Delete: the same files under `DRYL.Components.Agents/Canvas/` and `DRYL.Components.Agents/wwwroot/js/dryl-canvas.js`
- Modify: `DRYL.Components/_Imports.razor`, `DRYL.Components.Agents/_Imports.razor`, `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor` (+ css), `DrylCanvasRun.cs`, `DrylCanvasTools.cs`, `CanvasStreamReveal.cs`, `CanvasPrompt.cs`
- Modify: `DRYL.Website/Components/_Imports.razor`, `ComponentCatalog.cs`
- Test: move `tests/DRYL.Components.Tests/Agents/Canvas/{CanvasCatalogTests,CanvasPatcherTests,CanvasSpecTests,CanvasVersionTests,CanvasInteractionTests}.cs` → `tests/DRYL.Components.Tests/Canvas/`

**Interfaces produced:**

```csharp
namespace DRYL.Components.Canvas;

public sealed class CanvasPulseTracker
{
    public int TickOf(string id);
    public void Stamp(string id);
    public void Clear();
}

public sealed class CanvasContext
{
    public CanvasFormState Form { get; }
    public CanvasPulseTracker Pulse { get; }
    public CanvasDataBinder? Binder { get; internal set; }
    public AiState State { get; internal set; }
    public EventCallback<CanvasInteraction> Intent { get; internal set; }
    internal Func<string, Task>? Purge;
}
```

```csharp
// DrylCanvas parameters (namespace DRYL.Components)
[Parameter] public CanvasSpec? Spec { get; set; }
[Parameter] public AiState State { get; set; } = AiState.None;
[Parameter] public string? Error { get; set; }
[Parameter] public string? Announcement { get; set; }
[Parameter] public int Epoch { get; set; }
[Parameter] public CanvasPulseTracker? Pulse { get; set; }
[Parameter] public EventCallback<CanvasInteraction> OnInteraction { get; set; }
[Parameter] public EventCallback<string> OnPurge { get; set; }
[Parameter] public EventCallback<int> OnWidthChanged { get; set; }
[Parameter] public bool AllowExpand { get; set; } = true;
[Parameter] public RenderFragment? HeaderTools { get; set; }
[Parameter] public RenderFragment? Overlay { get; set; }
[Parameter] public string? Class { get; set; }
[Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? AdditionalAttributes { get; set; }
```

- [ ] **Step 1:** Copy the eight renderer files into `DRYL.Components/Canvas/`, rewrite `namespace DRYL.Components.Agents;` → `namespace DRYL.Components.Canvas;` and `@namespace DRYL.Components.Agents` → `@namespace DRYL.Components.Canvas`.
- [ ] **Step 2:** Break the Agents-only dependencies: add `CanvasJson.TryParse<T>` (copy of `DisplayJson.TryParse`), replace `DisplayJson.TryParse` calls with it; add `CanvasJsonMerge.Merge` (copy of `JsonMerge.Merge`) and use it in `CanvasPatcher`; move the chart/stat/timeline prop types into `CanvasPropTypes.cs` renamed `CanvasChartProps`, `CanvasChartSeriesProps`, `CanvasDonutProps`, `CanvasChartSegmentProps`, `CanvasStatProps`, `CanvasTimelineProps`, `CanvasTimelineEventProps` (the Agents `Tools/DisplaySpecs.cs` copies stay untouched).
- [ ] **Step 3:** Replace `CanvasNodeView`'s three cascades with one `[CascadingParameter] internal CanvasContext Ctx`; `Run.State` → `Ctx.State`, `Run.ChangeTickOf(id)` → `Ctx.Pulse.TickOf(id)`, `Run.Purge(id)` → `Ctx.Purge`, `Form` → `Ctx.Form`, `OnIntent` → `Ctx.Intent`.
- [ ] **Step 4:** Write `DrylCanvas.razor` with the parameter list above: root div (`canvas glass-card`, `view-transition-name`, `popover`, keydown/Escape), `@Overlay`, header (title · `@HeaderTools` · expand), busy build line driven by `State`, `.canvas-live` with `@Announcement`, body with error alert / `CanvasNodeView` inside `<CascadingValue Value="_ctx" IsFixed="true">` / `DrylEmptyState`, `dryl.motion.autoFlip` + width observer + `dryl.topLayer` (all copied verbatim from `DrylAiCanvas`). Move the structural CSS from `DrylAiCanvas.razor.css` into `DrylCanvas.razor.css`, renaming `.ai-canvas` → `.canvas`.
- [ ] **Step 5:** Rewrite `DrylAiCanvas.razor` as a wrapper: keeps `Run` subscription, aura lifecycle, gen-tick, announcement computation and the artifact-swap view transition; renders `<DrylCanvas Spec="Run?.Spec" State="..." Error="Run?.Error?.Message" Announcement="_announcement" Epoch="Run?.ArtifactEpoch ?? 0" Pulse="Run?.Pulse" OnInteraction="..." OnPurge="..." OnWidthChanged="..." AllowExpand="AllowExpand" Class="@RootCssClass"><Overlay><DrylAuraElements .../></Overlay><HeaderTools><DrylAiIndicator .../></HeaderTools></DrylCanvas>`. Its `.razor.css` keeps only aura-related rules.
- [ ] **Step 6:** Add `public CanvasPulseTracker Pulse { get; } = new();` to `DrylCanvasRun`; `ApplyOp` stamps it on `setProps`; `BeginCreate` clears it; delete `ChangeTickOf` and `_changeTicks`.
- [ ] **Step 7:** Add `@using DRYL.Components.Canvas` to core `_Imports.razor`, Agents `_Imports.razor`, Website `_Imports.razor`; add `using DRYL.Components.Canvas;` to the Agents `.cs` files and the moved tests.
- [ ] **Step 8:** Move `dryl-canvas.js` to `DRYL.Components/wwwroot/js/` and change the module path in `DrylCanvas.razor` to `./_content/DRYL.Components/js/dryl-canvas.js`.
- [ ] **Step 9:** Move the five pure-renderer test files to `tests/DRYL.Components.Tests/Canvas/`, namespace `DRYL.Components.Tests.Canvas`.
- [ ] **Step 10:** Add `tests/DRYL.Components.Tests/Canvas/DrylCanvasStandaloneTests.cs` pinning that `DrylCanvas` renders a spec with **no** Agents type in play.

```csharp
[Fact]
public void Renders_a_spec_without_any_agents_type()
{
    var spec = JsonSerializer.Deserialize<CanvasSpec>(
        """{"title":"Report","root":{"id":"r","type":"stack","children":[
            {"id":"s","type":"stat","props":{"label":"Revenue","value":"10k"}}]}}""",
        CanvasJson.Options)!;

    var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, spec));

    Assert.Contains("Revenue", cut.Markup);
    Assert.Contains("Report", cut.Markup);
}
```

- [ ] **Step 11:** Run `dotnet build DRYL.slnx` then `dotnet test DRYL.slnx` — the pre-existing canvas test set must be green, unchanged in count except for the one added test.
- [ ] **Step 12:** Commit `refactor(canvas): move the renderer into DRYL.Components (A1)`.

---

### Task 2: Result shapes + registry + descriptor derivation

**Files:** Create `DRYL.Components/Canvas/CanvasData.cs`, `CanvasDataRegistry.cs`, `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs`; Test: `tests/DRYL.Components.Tests/Canvas/CanvasDataRegistryTests.cs`

**Interfaces produced:**

```csharp
public enum CanvasDataShape { Scalar, Series, Segments, Rows }

public abstract class CanvasData
{
    public abstract CanvasDataShape Shape { get; }
    public static CanvasScalarData  Scalar(double value, string? text = null, string? delta = null, string? direction = null);
    public static CanvasScalarData  Scalar(string text, string? delta = null, string? direction = null);
    public static CanvasSeriesData  Series(IEnumerable<string> labels, params (string Name, IEnumerable<double> Data)[] series);
    public static CanvasSegmentData Segments(IEnumerable<(string Label, double Value)> segments);
    public static CanvasRowData     Rows(IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows);
}

public sealed record CanvasDataDescriptor(string Name, string Description, CanvasDataShape Shape, IReadOnlyList<CanvasParamInfo> Params);
public sealed record CanvasParamInfo(string Name, string TypeName, bool Required);
public sealed class CanvasDataContext { public IServiceProvider Services { get; } }

public sealed class CanvasDataRegistry
{
    public IReadOnlyList<CanvasDataDescriptor> Descriptors { get; }
    public bool TryGet(string name, out CanvasDataSource source);
    internal void Add(CanvasDataSource source);
}

public static class CanvasServiceCollectionExtensions
{
    public static IServiceCollection AddDrylCanvasDataSource<TParams, TData>(this IServiceCollection services,
        string name, string description,
        Func<TParams, CanvasDataContext, CancellationToken, Task<TData>> handler)
        where TParams : class where TData : CanvasData;

    public static IServiceCollection AddDrylCanvasDataSource<TData>(this IServiceCollection services,
        string name, string description,
        Func<CanvasDataContext, CancellationToken, Task<TData>> handler)
        where TData : CanvasData;
}
```

- [ ] Step 1 — failing tests: descriptor derivation marks `int Year` required and `string? Region = null` optional; unsupported param type throws at registration; duplicate name throws; shape inferred from `TData`.
- [ ] Step 2 — run, confirm they fail to compile/assert.
- [ ] Step 3 — implement shapes, registry, reflection over the record's primary constructor (`NullabilityInfoContext` for reference types, `HasDefaultValue` for optionals), supported-type whitelist, `Rows` truncation at 30 with a `Truncated` flag.
- [ ] Step 4 — run, green.
- [ ] Step 5 — commit `feat(canvas): result shapes and the data source registry`.

---

### Task 3: `ICanvasDataService` + prompt block

**Files:** Create `CanvasDataService.cs`, `CanvasDataPrompt.cs`; Modify `Extensions/ServiceCollectionExtensions.cs`; Test: `CanvasDataServiceTests.cs`, `CanvasDataPromptTests.cs`

```csharp
public sealed record CanvasInvalidation(string Source, string? ParamsKey);

public interface ICanvasDataService
{
    IReadOnlyList<CanvasDataDescriptor> Descriptors { get; }
    void Invalidate(string source);
    void Invalidate(string source, object parameters);
    event Action<CanvasInvalidation>? Invalidated;                       // infrastructure
    Task<CanvasData> LoadAsync(string source, JsonElement? parameters, CancellationToken ct); // infrastructure
}

public static class CanvasDataPrompt
{
    public static string Block(IReadOnlyList<CanvasDataDescriptor> descriptors); // "" when empty
}
```

- [ ] Steps: failing tests (`Invalidate(source)` raises with null key, `Invalidate(source, new {year=2026})` raises with the canonical key, `LoadAsync` on an unknown source throws a named exception, prompt block contains name/signature/shape/description and is empty for an empty registry, 40+ sources logs a warning) → implement → green → commit `feat(canvas): scoped data service and prompt block`.

---

### Task 4: Binder + mapper

**Files:** Create `CanvasDataBinder.cs`, `CanvasDataMapper.cs`; Test: `CanvasDataBinderTests.cs`, `CanvasDataMapperTests.cs`

```csharp
public sealed class CanvasDataBinder : IAsyncDisposable
{
    public CanvasDataBinder(ICanvasDataService? data, CanvasFormState form, CanvasPulseTracker pulse, ILogger? log = null);
    public bool HasBindings { get; }
    public bool Busy { get; }
    public CanvasBindingState? StateOf(string nodeId);
    public void Register(string nodeId, CanvasDataBinding binding);
    public void Unregister(string nodeId);
    public Task RefreshAllAsync();
    public event Action? OnChanged;
    public ValueTask DisposeAsync();
}

public sealed class CanvasBindingState
{
    public bool Loading { get; }
    public bool HasValue { get; }
    public CanvasData? Data { get; }
    public string? Error { get; }
}
```

- [ ] Failing tests: dedupe (two nodes, one key ⇒ one handler call); `$field` dependency (`region` change reloads only the dependent key); 300 ms debounce coalesces bursts; a late result with a stale sequence is discarded; `Invalidate(source)` hits all keys, `Invalidate(source, params)` only one; the interval timer is disposed on `DisposeAsync` and in-flight loads are cancelled; an unchanged result raises **no** `OnChanged` and stamps **no** pulse; a changed result stamps the pulse of every node on the key; a handler exception becomes a binding error and never escapes.
- [ ] Mapper tests: each shape onto each allowed node type fills the right props; each disallowed pair returns the documented sentence; a text-only `Scalar` on `progress` fails; >30 `Rows` truncate.
- [ ] Implement → green → commit `feat(canvas): the data binder and shape→props mapper`.

---

### Task 5: Render path — `CanvasNodeView` + refresh button

**Files:** Modify `CanvasNodeView.razor`, `CanvasContext.cs`, `DrylCanvas.razor` + `.razor.css`; Test: `CanvasBindingRenderTests.cs`

- [ ] Failing bUnit tests: first load shows a skeleton then the content; a refreshed value shows **no** skeleton but a pulse; an unchanged refresh produces neither; an error after a good value keeps the value and shows the marker; an error without a good value shows the inline error; the refresh button appears only with a binding and carries `aria-label` + tooltip; a `$field` change reloads exactly the dependent node.
- [ ] Implement: binding registration in `OnParametersSet`, effective-props memo keyed on the binding state's data identity, skeleton variant per node type, `.canvas-data-error` marker, header `↻` button (`DrylIcon Name="Refresh"`, busy while loading).
- [ ] Green → commit `feat(canvas): bound nodes render loading, ready and error states`.

---

### Task 6: Validation + AI wiring

**Files:** Modify `CanvasCatalog.cs`, Agents `CanvasPrompt.cs`, `DrylCanvasTools.cs`; Test: `CanvasBindingValidationTests.cs`, Agents `CanvasDataReceiptTests.cs`

```csharp
public sealed class CanvasValidationContext
{
    public IReadOnlyList<CanvasDataDescriptor> Sources { get; init; }
    public IReadOnlyCollection<string> FieldNames { get; init; }
}
public static string? Validate(CanvasNode node);                          // unchanged
public static string? Validate(CanvasNode node, CanvasValidationContext? context);
```

- [ ] Failing tests for the five validation rules + five replay cases (unknown source, wrong shape, missing required param, `$field` on a non-existent field, `interval:1s`) each producing a corrective receipt sentence while the artifact still renders.
- [ ] Implement; `DrylCanvasTools.Create/CreateReplay` gain an optional `ICanvasDataService? data = null`; the prompt block goes into `CreatePrompt` **and** `UpdatePrompt`.
- [ ] Green → commit `feat(canvas): validate data bindings and teach the model about sources`.

---

### Task 7: Demo, catalog, docs, versions

**Files:** `DRYL.Website/Components/Examples/Canvas/CanvasDataBinding.razor`, `Components/Pages/DemoCanvas.razor`, `ComponentCatalog.cs`, `Program.cs`; `CHANGELOG.md`; both `.csproj`

- [ ] Demo page registering two in-memory sources, a `select` node driving a `$field` param, an `interval:5s` stat, and the refresh button — no model needed.
- [ ] `ComponentCatalog`: new `DrylCanvas` entry; `DrylAiCanvas` entry mentions data binding.
- [ ] `CHANGELOG.md`: `Added` (data binding, `DrylCanvas`, `AddDrylCanvasDataSource`) + `Changed` (the move, with the migration line `using DRYL.Components.Canvas;`); cut both releases dated 2026-07-25.
- [ ] Bump core to `2.12.0`, Agents to `0.10.0`.
- [ ] Verify both color modes, 375 px, `prefers-reduced-motion`; `node scripts/check-light-sync.mjs` (no new tokens expected).
- [ ] Commit `feat(canvas): data binding demo, catalog entry, 2.12.0 / 0.10.0`.
