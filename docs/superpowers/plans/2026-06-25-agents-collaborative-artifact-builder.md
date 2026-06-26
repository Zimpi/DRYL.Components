# Collaborative Artifact Builder — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an iterative, collaborative structured-generation primitive to `DRYL.Components.Agents` where the model alternates *ask the user → refine artifact → ask* and a UI shows the artifact growing.

**Architecture:** A new `StartBuild<T>` runs the agent on the existing Microsoft `FunctionInvokingChatClient` loop, injecting an auto-generated `update_<T>` merge tool (passed via run options) plus a framework-owned iteration prompt. The merged artifact lives in an observable `DrylArtifactRun<T>`. Shared run plumbing is extracted into `DrylRunBase`.

**Tech Stack:** .NET 8/9/10, Blazor, `Microsoft.Agents.AI` 1.10.0, `Microsoft.Extensions.AI` (`ChatOptions`, `AIFunctionFactory`, `AIJsonUtilities`), `System.Text.Json.Nodes`, xUnit + bUnit.

## Global Constraints

- Package is experimental, independently versioned **0.1.0**, entirely under `[Unreleased]`; the core `DRYL.Components` stays dependency-free.
- Reuse the shared `AiState` vocabulary (`None/Active/Thinking/Streaming/Generated`); never invent per-component AI states.
- All new public types/members get XML doc comments (`GenerateDocumentationFile` is on; `CS1591` is suppressed but document anyway).
- Test project `DRYL.Components.Tests` already has `InternalsVisibleTo`; `internal` members are test-reachable.
- Component/file naming: `Dryl` prefix, PascalCase; CSS classes kebab-case (none needed here).
- `JsonOpts` = the runner's existing `private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };` in `DrylAgentRunner.Run.cs`.
- Build command (Agents): `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
- Test command: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`
- Commit trailer on every commit: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- Branch: `feature/agents-package` (already checked out).

---

### Task 1: Extract `DrylRunBase` (refactor — preserves public surface + TextStream fix)

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylRunBase.cs`
- Modify: `DRYL.Components.Agents/Agents/DrylAgentRun.cs` (full rewrite to thin subclass)
- Modify: `DRYL.Components.Agents/Agents/DrylAgentRunner.Run.cs` (retype `ProcessAsync`/`SetState`/`SetStateRaw` params to `DrylRunBase`)
- Modify: `DRYL.Components.Agents/Agents/DrylAgentToolCalls.razor` (retype `Run` parameter to `DrylRunBase?`)
- Test: `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerTests.cs` (existing `TextStream_returns_a_stable_reference` must still pass)

**Interfaces:**
- Produces: `public abstract class DrylRunBase : IAsyncDisposable` with public `AiState State { get; internal set; }`, `string Text { get; internal set; }`, `IReadOnlyList<DrylToolInvocation> ToolCalls`, `event Action? OnChange`, `IAsyncEnumerable<string> TextStream`, `Task WaitForCompletionAsync()`, `ValueTask DisposeAsync()`; internal `AddToolCall`, `Raise`, `PushText`, `CompleteText`, `MarkCompleted`.
- Produces: `public sealed class DrylAgentRun : DrylRunBase` (no added members).

- [ ] **Step 1: Run existing Agents tests to capture the green baseline**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`
Expected: PASS (DrylAgentRunnerTests incl. `TextStream_returns_a_stable_reference`, DrylAgentToolCallsTests, etc.)

- [ ] **Step 2: Create `DrylRunBase.cs`**

```csharp
using System.Threading.Channels;

namespace DRYL.Components.Agents;

/// <summary>
/// Shared observable plumbing for agent runs: accumulated <see cref="Text"/>, a live
/// <see cref="ToolCalls"/> trace, automatic <see cref="State"/>, a stable <see cref="TextStream"/>,
/// and an <see cref="OnChange"/> notification. Base for <see cref="DrylAgentRun"/> and
/// <see cref="DrylArtifactRun{T}"/>.
/// </summary>
public abstract class DrylRunBase : IAsyncDisposable
{
    private readonly List<DrylToolInvocation> _toolCalls = new();
    private readonly Channel<string> _textChannel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Cached once so TextStream is a STABLE reference across re-renders: consumers like
    // DrylAiStream restart their enumeration whenever Source changes by reference, and the run
    // re-renders on every tool-call/state change. A fresh ReadAllAsync() per access would reset
    // the streamed text mid-run.
    private readonly IAsyncEnumerable<string> _textStream;

    /// <summary>Creates the run plumbing.</summary>
    protected DrylRunBase() => _textStream = _textChannel.Reader.ReadAllAsync();

    /// <summary>Current AI state, driven automatically by the run.</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The accumulated answer text so far.</summary>
    public string Text { get; internal set; } = string.Empty;

    /// <summary>The tool calls observed in this run, in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> ToolCalls => _toolCalls;

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, <see cref="ToolCalls"/> or subclass state changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textStream;

    internal void AddToolCall(DrylToolInvocation t) { _toolCalls.Add(t); Raise(); }
    internal void Raise() => OnChange?.Invoke();
    internal void PushText(string delta) => _textChannel.Writer.TryWrite(delta);
    internal void CompleteText() => _textChannel.Writer.TryComplete();
    internal void MarkCompleted() => _completed.TrySetResult();

    /// <summary>Test/consumer helper: completes when the run's processing loop finishes.</summary>
    public Task WaitForCompletionAsync() => _completed.Task;

    /// <summary>Cancels the run and releases its resources.</summary>
    public ValueTask DisposeAsync()
    {
        _textChannel.Writer.TryComplete();
        _completed.TrySetResult();
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 3: Rewrite `DrylAgentRun.cs` as a thin subclass**

Replace the entire file contents with:

```csharp
namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to a running agent. Drives <see cref="DrylRunBase.State"/> automatically and
/// exposes the accumulated <see cref="DrylRunBase.Text"/>, the live <see cref="DrylRunBase.ToolCalls"/>
/// trace, and a <see cref="DrylRunBase.TextStream"/> ready to drop into <c>DrylAiStream</c>/<c>DrylMarkdown</c>.
/// </summary>
public sealed class DrylAgentRun : DrylRunBase
{
}
```

- [ ] **Step 4: Retype the processing loop in `DrylAgentRunner.Run.cs`**

Change these three signatures (bodies unchanged) from `DrylAgentRun` to `DrylRunBase`:

```csharp
private async Task ProcessAsync(
    DrylRunBase run, IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)
```
```csharp
private void SetState(DrylRunBase run, AiState state, string? aiKey)
```
```csharp
private void SetStateRaw(DrylRunBase run, AiState state, string? aiKey)
```

Leave `StartFromUpdates` returning `DrylAgentRun` (it still does `var run = new DrylAgentRun();` and passes it to `ProcessAsync`).

- [ ] **Step 5: Retype `Run` in `DrylAgentToolCalls.razor`**

In the `@code` block, change:
```csharp
    [Parameter] public DrylAgentRun? Run { get; set; }
```
to
```csharp
    [Parameter] public DrylRunBase? Run { get; set; }
```
and
```csharp
    private DrylAgentRun? _subscribed;
```
to
```csharp
    private DrylRunBase? _subscribed;
```

- [ ] **Step 6: Build and run all Agents tests (refactor must be behavior-preserving)**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: `Build succeeded`, 0 errors.

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`
Expected: PASS — same count as Step 1, incl. `TextStream_returns_a_stable_reference` (now exercising the cached field on the base).

- [ ] **Step 7: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylRunBase.cs DRYL.Components.Agents/Agents/DrylAgentRun.cs DRYL.Components.Agents/Agents/DrylAgentRunner.Run.cs DRYL.Components.Agents/Agents/DrylAgentToolCalls.razor
git commit -m "refactor(agents): extract DrylRunBase from DrylAgentRun

Shared run plumbing (text channel, completion, OnChange, stable
TextStream) moves to a base class so DrylArtifactRun<T> can reuse it.
Public surface of DrylAgentRun is unchanged.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: `JsonMerge` deep-merge engine

**Files:**
- Create: `DRYL.Components.Agents/Generation/JsonMerge.cs`
- Test: `tests/DRYL.Components.Tests/Agents/JsonMergeTests.cs`

**Interfaces:**
- Produces: `public static class JsonMerge` with `public static JsonNode? Merge(JsonNode? target, JsonNode? patch)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Agents/JsonMergeTests.cs`:

```csharp
using System.Text.Json.Nodes;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class JsonMergeTests
{
    private static JsonNode P(string json) => JsonNode.Parse(json)!;

    [Fact]
    public void Merge_into_null_target_returns_patch()
    {
        var result = JsonMerge.Merge(null, P("""{"title":"A"}"""));
        Assert.Equal("A", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Scalar_field_in_patch_overwrites_existing()
    {
        var result = JsonMerge.Merge(P("""{"title":"old"}"""), P("""{"title":"new"}"""));
        Assert.Equal("new", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Field_absent_from_patch_is_left_untouched()
    {
        var result = JsonMerge.Merge(P("""{"title":"keep","note":"x"}"""), P("""{"note":"y"}"""));
        Assert.Equal("keep", result!["title"]!.GetValue<string>());
        Assert.Equal("y", result["note"]!.GetValue<string>());
    }

    [Fact]
    public void Null_value_in_patch_leaves_existing()
    {
        var result = JsonMerge.Merge(P("""{"title":"keep"}"""), P("""{"title":null}"""));
        Assert.Equal("keep", result!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Array_in_patch_replaces_whole_array()
    {
        var result = JsonMerge.Merge(P("""{"steps":["a","b","c"]}"""), P("""{"steps":["x"]}"""));
        Assert.Single(result!["steps"]!.AsArray());
        Assert.Equal("x", result["steps"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Nested_object_is_merged_recursively()
    {
        var result = JsonMerge.Merge(P("""{"meta":{"a":1,"b":2}}"""), P("""{"meta":{"b":9,"c":3}}"""));
        Assert.Equal(1, result!["meta"]!["a"]!.GetValue<int>());
        Assert.Equal(9, result["meta"]!["b"]!.GetValue<int>());
        Assert.Equal(3, result["meta"]!["c"]!.GetValue<int>());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~JsonMergeTests"`
Expected: FAIL — `JsonMerge` does not exist (compile error).

- [ ] **Step 3: Implement `JsonMerge.cs`**

```csharp
using System.Text.Json.Nodes;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Deep-merges a partial JSON patch into a running JSON object. Objects merge recursively; scalars
/// and arrays in the patch replace; a JSON <c>null</c> value or an absent key leaves the existing
/// value untouched. Pure; used to apply <c>update_&lt;T&gt;</c> patches onto the live artifact.
/// </summary>
public static class JsonMerge
{
    /// <summary>Returns a new node: <paramref name="patch"/> deep-merged onto <paramref name="target"/>.</summary>
    public static JsonNode? Merge(JsonNode? target, JsonNode? patch)
    {
        if (patch is null) return target;
        if (target is not JsonObject t || patch is not JsonObject p)
            return patch.DeepClone();   // scalar / array / type-mismatch -> replace

        var result = (JsonObject)t.DeepClone();
        foreach (var (key, value) in p)
        {
            if (value is null) continue;   // explicit null -> leave existing
            result[key] = result.TryGetPropertyValue(key, out var existing) && existing is not null
                ? Merge(existing.DeepClone(), value.DeepClone())
                : value.DeepClone();
        }
        return result;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~JsonMergeTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Generation/JsonMerge.cs tests/DRYL.Components.Tests/Agents/JsonMergeTests.cs
git commit -m "feat(agents): JsonMerge deep-merge engine for artifact patches

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: `DrylBuildOptions` + `DrylArtifactRun<T>`

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylBuildOptions.cs`
- Create: `DRYL.Components.Agents/Agents/DrylArtifactRun.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`

**Interfaces:**
- Consumes: `JsonMerge.Merge` (Task 2); `DrylRunBase` (Task 1).
- Produces: `public sealed class DrylBuildOptions { int? MaxRounds; string? Guidance; string? UpdateToolName; }`.
- Produces: `public sealed class DrylArtifactRun<T> : DrylRunBase` with `public T? Artifact { get; }`, `public int Round { get; }`, `internal DrylArtifactRun(JsonSerializerOptions jsonOptions)`, `internal string ApplyPatch(JsonElement patch, int? maxRounds)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylArtifactRunTests
{
    private sealed class Dish
    {
        public string? Title { get; set; }
        public List<string> Steps { get; set; } = new();
    }

    private static JsonElement El(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void ApplyPatch_merges_progressively_and_counts_rounds()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var changes = 0;
        run.OnChange += () => changes++;

        run.ApplyPatch(El("""{"title":"Pasta"}"""), maxRounds: null);
        run.ApplyPatch(El("""{"steps":["boil","drain"]}"""), maxRounds: null);

        Assert.Equal("Pasta", run.Artifact!.Title);
        Assert.Equal(2, run.Artifact.Steps.Count);
        Assert.Equal(2, run.Round);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ApplyPatch_returns_a_receipt_below_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = run.ApplyPatch(El("""{"title":"X"}"""), maxRounds: 12);
        Assert.Contains("round 1", receipt);
    }

    [Fact]
    public void ApplyPatch_returns_a_finalize_nudge_at_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = run.ApplyPatch(El("""{"title":"X"}"""), maxRounds: 1);
        Assert.Contains("Maximum refinement rounds reached", receipt);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylArtifactRunTests"`
Expected: FAIL — `DrylArtifactRun` does not exist (compile error).

- [ ] **Step 3: Implement `DrylBuildOptions.cs`**

```csharp
namespace DRYL.Components.Agents;

/// <summary>Options for <see cref="DrylAgentRunner.StartBuild{T}"/>.</summary>
public sealed class DrylBuildOptions
{
    /// <summary>Safety cap on refinement rounds; <c>null</c> = unbounded. Default 12.</summary>
    public int? MaxRounds { get; init; } = 12;

    /// <summary>Overrides the framework's default iterative-build guidance prompt.</summary>
    public string? Guidance { get; init; }

    /// <summary>Overrides the auto-generated update tool name (default <c>update_&lt;t-name&gt;</c>).</summary>
    public string? UpdateToolName { get; init; }
}
```

- [ ] **Step 4: Implement `DrylArtifactRun.cs`**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to an iterative artifact build (see <see cref="DrylAgentRunner.StartBuild{T}"/>).
/// Adds the live, progressively-merged <see cref="Artifact"/> and the <see cref="Round"/> counter to
/// the shared run surface.
/// </summary>
public sealed class DrylArtifactRun<T> : DrylRunBase
{
    private readonly JsonSerializerOptions _jsonOptions;
    private JsonNode? _json;

    internal DrylArtifactRun(JsonSerializerOptions jsonOptions) => _jsonOptions = jsonOptions;

    /// <summary>The live, progressively-merged artifact (fields not yet provided are null/default).</summary>
    public T? Artifact { get; private set; }

    /// <summary>The number of <c>update_&lt;T&gt;</c> merge steps applied so far.</summary>
    public int Round { get; private set; }

    /// <summary>
    /// Merge a partial-<typeparamref name="T"/> patch into the running artifact, raise
    /// <see cref="DrylRunBase.OnChange"/>, and return a short receipt for the model. When
    /// <paramref name="maxRounds"/> is reached, returns a finalize nudge instead.
    /// </summary>
    internal string ApplyPatch(JsonElement patch, int? maxRounds)
    {
        var patchNode = patch.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : JsonNode.Parse(patch.GetRawText());

        _json = JsonMerge.Merge(_json, patchNode);
        Round++;
        Artifact = _json is null ? default : _json.Deserialize<T>(_jsonOptions);
        Raise();

        return maxRounds is { } m && Round >= m
            ? "Maximum refinement rounds reached — stop refining and give your final answer now."
            : $"Updated (round {Round}).";
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylArtifactRunTests"`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylBuildOptions.cs DRYL.Components.Agents/Agents/DrylArtifactRun.cs tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs
git commit -m "feat(agents): DrylArtifactRun<T> + DrylBuildOptions

Observable artifact run with progressive merge (ApplyPatch), round
counter and MaxRounds finalize nudge.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: `StartBuild<T>` + the auto-generated `update_<T>` tool

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerBuildTests.cs`

**Interfaces:**
- Consumes: `DrylArtifactRun<T>` + `ApplyPatch` (Task 3); `DrylBuildOptions` (Task 3); the runner's private `JsonOpts` and private `ProcessAsync(DrylRunBase, …)` (Tasks 1).
- Produces: `public DrylArtifactRun<T> StartBuild<T>(AIAgent agent, AgentSession session, string prompt, DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)`.
- Produces: `internal static AITool CreateUpdateTool<T>(DrylArtifactRun<T> run, DrylBuildOptions options)` (extracted for testability).

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerBuildTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerBuildTests
{
    private sealed class Recipe { public string? Title { get; set; } }

    [Fact]
    public void CreateUpdateTool_default_name_is_update_lowercased_type()
    {
        var run = new DrylArtifactRun<Recipe>(new JsonSerializerOptions());
        var tool = DrylAgentRunner.CreateUpdateTool(run, new DrylBuildOptions());
        Assert.Equal("update_recipe", tool.Name);
    }

    [Fact]
    public void CreateUpdateTool_honours_a_custom_name()
    {
        var run = new DrylArtifactRun<Recipe>(new JsonSerializerOptions());
        var tool = DrylAgentRunner.CreateUpdateTool(run, new DrylBuildOptions { UpdateToolName = "draft" });
        Assert.Equal("draft", tool.Name);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAgentRunnerBuildTests"`
Expected: FAIL — `DrylAgentRunner.CreateUpdateTool` does not exist (compile error).

- [ ] **Step 3: Implement `DrylAgentRunner.Build.cs`**

```csharp
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    private const string DefaultBuildGuidance =
        "Build the result collaboratively and iteratively. Do not gather everything up front and " +
        "then dump a result. Instead: ask the user one focused question via your question tools, " +
        "record progress with the update tool, then ask the next — alternating question -> refine " +
        "-> question. Call the update tool many times as the picture sharpens. When the user is " +
        "satisfied and the artifact is complete, give a brief final confirmation and stop.";

    /// <summary>
    /// Start an iterative, collaborative artifact build. The model alternates asking the user (via
    /// the agent's own tools), thinking, and refining a <typeparamref name="T"/> through an
    /// auto-injected <c>update_&lt;T&gt;</c> tool, until it produces a final answer. Returns an
    /// observable <see cref="DrylArtifactRun{T}"/> whose <see cref="DrylArtifactRun{T}.Artifact"/>
    /// grows round by round.
    /// </summary>
    public DrylArtifactRun<T> StartBuild<T>(
        AIAgent agent, AgentSession session, string prompt,
        DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)
    {
        options ??= new DrylBuildOptions();
        var run = new DrylArtifactRun<T>(JsonOpts);

        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = options.Guidance ?? DefaultBuildGuidance,
                Tools = new List<AITool> { CreateUpdateTool(run, options) },
            }
        };

        var updates = agent.RunStreamingAsync(prompt, session, runOptions, ct);
        _ = ProcessAsync(run, updates, aiKey, ct);
        return run;
    }

    /// <summary>
    /// Builds the auto-generated <c>update_&lt;T&gt;</c> tool: it accepts a partial-<typeparamref name="T"/>
    /// JSON patch, merges it into <paramref name="run"/>, and returns a receipt for the model.
    /// </summary>
    internal static AITool CreateUpdateTool<T>(DrylArtifactRun<T> run, DrylBuildOptions options)
    {
        var toolName = options.UpdateToolName ?? $"update_{typeof(T).Name.ToLowerInvariant()}";
        var schema = AIJsonUtilities.CreateJsonSchema(typeof(T), serializerOptions: JsonOpts).GetRawText();
        var description =
            "Record or refine the artifact as you learn more. Call this repeatedly — include only " +
            "the fields you want to set or change; all fields are optional. Artifact shape: " + schema;

        return AIFunctionFactory.Create(
            (JsonElement patch) => run.ApplyPatch(patch, options.MaxRounds),
            toolName, description);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAgentRunnerBuildTests"`
Expected: PASS (2 tests).

- [ ] **Step 5: Build the whole package to confirm `StartBuild` compiles**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs tests/DRYL.Components.Tests/Agents/DrylAgentRunnerBuildTests.cs
git commit -m "feat(agents): StartBuild<T> with auto-injected update_<T> merge tool

Runs the MS function-invocation loop with a framework-owned iteration
prompt; the model refines T via update_<T> while asking the user.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: `DrylAiBuild<T>` UI component + `ArtifactSnapshot<T>`

**Files:**
- Create: `DRYL.Components.Agents/Generation/ArtifactSnapshot.cs`
- Create: `DRYL.Components.Agents/Generation/DrylAiBuild.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAiBuildTests.cs`

**Interfaces:**
- Consumes: `DrylArtifactRun<T>` (Task 3); `IDrylAiActivityService` (core, optional).
- Produces: `public sealed class ArtifactSnapshot<T> { T? Artifact; AiState State; int Round; bool IsComplete; }`.
- Produces: component `DrylAiBuild<T>` (namespace `DRYL.Components.Agents`) with parameters `DrylArtifactRun<T>? Run`, `string? Key`, `RenderFragment<ArtifactSnapshot<T>>? ChildContent`, `AiState SettleTo = AiState.None`.

- [ ] **Step 1: Implement `ArtifactSnapshot.cs`**

```csharp
namespace DRYL.Components.Agents.Generation;

/// <summary>A live snapshot of an artifact build, handed to <c>DrylAiBuild</c>'s child content.</summary>
public sealed class ArtifactSnapshot<T>
{
    /// <summary>The artifact merged so far (fields not yet provided are null/default).</summary>
    public T? Artifact { get; internal set; }

    /// <summary>The AI state of the build (Thinking → Streaming → Generated).</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The number of refine steps applied so far.</summary>
    public int Round { get; internal set; }

    /// <summary>True once the build has settled after the Generated reveal.</summary>
    public bool IsComplete { get; internal set; }
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/DRYL.Components.Tests/Agents/DrylAiBuildTests.cs`:

```csharp
using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Agents.Generation;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests.Agents;

public class DrylAiBuildTests : BunitContext
{
    private sealed class Dish { public string? Title { get; set; } }

    [Fact]
    public void Renders_the_current_artifact_snapshot()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        run.ApplyPatch(JsonDocument.Parse("""{"title":"Pasta"}""").RootElement, maxRounds: null);

        var cut = Render<DrylAiBuild<Dish>>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ChildContent, (RenderFragment<ArtifactSnapshot<Dish>>)(snap => builder =>
                builder.AddContent(0, snap.Artifact?.Title))));

        Assert.Contains("Pasta", cut.Markup);
    }

    [Fact]
    public void Re_renders_when_the_run_raises_a_change()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());

        var cut = Render<DrylAiBuild<Dish>>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ChildContent, (RenderFragment<ArtifactSnapshot<Dish>>)(snap => builder =>
                builder.AddContent(0, snap.Artifact?.Title ?? "empty"))));

        Assert.Contains("empty", cut.Markup);

        cut.InvokeAsync(() => run.ApplyPatch(JsonDocument.Parse("""{"title":"Risotto"}""").RootElement, null));

        Assert.Contains("Risotto", cut.Markup);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAiBuildTests"`
Expected: FAIL — `DrylAiBuild` does not exist (compile error).

- [ ] **Step 4: Implement `DrylAiBuild.razor`**

```razor
@namespace DRYL.Components.Agents
@typeparam T
@using DRYL.Components.Agents.Generation
@using DRYL.Components.Ai
@using Microsoft.Extensions.DependencyInjection
@implements IDisposable

@*  DrylAiBuild<T> — renders the live, progressively-merged artifact of a DrylArtifactRun<T>.
    Parallel to DrylAiGenerate<T>, but driven by discrete tool merges (no token stream). *@

@if (ChildContent is not null)
{
    @ChildContent(_snapshot)
}

@code {
    /// <summary>The artifact build to visualise. Replacing it resubscribes.</summary>
    [Parameter] public DrylArtifactRun<T>? Run { get; set; }

    /// <summary>Optional activity key; when set, a surrounding <c>DrylAiScope Key="..."</c> settles with the build.</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>Child content receiving the live <see cref="ArtifactSnapshot{T}"/>.</summary>
    [Parameter] public RenderFragment<ArtifactSnapshot<T>>? ChildContent { get; set; }

    /// <summary>State to settle to after the Generated reveal. Default <see cref="AiState.None"/>.</summary>
    [Parameter] public AiState SettleTo { get; set; } = AiState.None;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private const int SettleDelayMs = 1200;

    private readonly ArtifactSnapshot<T> _snapshot = new();
    private IDrylAiActivityService? _service;
    private DrylArtifactRun<T>? _subscribed;
    private bool _settling;

    protected override void OnInitialized() =>
        _service = Services.GetService<IDrylAiActivityService>();

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Run)) return;
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
        _subscribed = Run;
        if (_subscribed is not null) _subscribed.OnChange += HandleChange;
        Sync();
    }

    private void HandleChange() { Sync(); _ = InvokeAsync(StateHasChanged); }

    private void Sync()
    {
        if (Run is null) return;
        _snapshot.Artifact = Run.Artifact;
        _snapshot.Round = Run.Round;
        if (!_settling) _snapshot.State = Run.State;

        if (Run.State == AiState.Generated && !_settling)
        {
            _settling = true;
            _ = SettleAsync();
        }
    }

    private async Task SettleAsync()
    {
        try { await Task.Delay(SettleDelayMs); } catch { /* fire-and-forget reveal dwell */ }

        if (_service is not null && Key is not null)
        {
            if (SettleTo == AiState.None) _service.Clear(Key);
            else _service.Set(Key, SettleTo);
        }
        _snapshot.State = SettleTo;
        _snapshot.IsComplete = true;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAiBuildTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Generation/ArtifactSnapshot.cs DRYL.Components.Agents/Generation/DrylAiBuild.razor tests/DRYL.Components.Tests/Agents/DrylAiBuildTests.cs
git commit -m "feat(agents): DrylAiBuild<T> renders the live artifact build

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Convert demo Section 2 to `StartBuild`

**Files:**
- Modify: `DRYL.Website/Components/Pages/AgentsLive.razor`

**Interfaces:**
- Consumes: `StartBuild<T>` (Task 4); `DrylAiBuild<T>` (Task 5); `DrylAgentToolCalls` retyped (Task 1).

- [ ] **Step 1: Replace the Section 2 markup**

In `DRYL.Website/Components/Pages/AgentsLive.razor`, replace the whole `@* ── Live structured generation … *@` `<section>` (the block beginning `<h3>2 · Live structured generation` and ending at its closing `</section>`) with:

```razor
        @* ── Live collaborative build ────────────────────────────────────────── *@
        <section class="col" style="gap: var(--sp-3);">
            <h3>2 · Live collaborative build — <code>StartBuild&lt;Recipe&gt;</code></h3>
            <p class="lead">
                The model builds a <code>Recipe</code> <em>iteratively</em>: it asks you via DRYL
                dialogs, refines the recipe through its <code>update_recipe</code> tool, asks again
                — and the card grows round by round.
            </p>

            <div class="row" style="gap: var(--sp-2); align-items: flex-start;">
                <div style="flex: 1;">
                    <DrylInputText @bind-Value="_dish" Placeholder="A dish, e.g. Tiramisu" AriaLabel="Dish" />
                </div>
                <DrylButton Variant="DrylButton.ButtonVariant.Primary" @onclick="Build" Disabled="@_buildBusy">
                    Build
                </DrylButton>
            </div>

            @if (_build is not null)
            {
                <DrylAiScope Key="build">
                    <DrylCard Ai="@_build.State">
                        <div class="col" style="gap: var(--sp-3);">
                            <DrylAgentToolCalls Run="@_build" />
                            <DrylAiBuild T="Recipe" Run="@_build" Key="build">
                                <ChildContent Context="a">
                                    <div class="col" style="gap: var(--sp-2);">
                                        <h3 style="margin: 0;">@(string.IsNullOrEmpty(a.Artifact?.Title) ? "…" : a.Artifact!.Title)</h3>
                                        @if (a.Artifact?.Description is { Length: > 0 })
                                        {
                                            <p class="lead" style="margin: 0;">@a.Artifact.Description</p>
                                        }
                                        @if (a.Artifact?.Steps is { Count: > 0 })
                                        {
                                            <ol style="margin: 0; padding-left: var(--sp-5);">
                                                @foreach (var step in a.Artifact.Steps) { <li>@step</li> }
                                            </ol>
                                        }
                                    </div>
                                </ChildContent>
                            </DrylAiBuild>
                        </div>
                    </DrylCard>
                </DrylAiScope>
            }
        </section>
```

- [ ] **Step 2: Replace the Section 2 fields and method in `@code`**

Replace:
```csharp
    private string _dish = string.Empty;
    private bool _genBusy;
    private IAsyncEnumerable<string>? _recipeStream;
```
with:
```csharp
    private string _dish = string.Empty;
    private bool _buildBusy;
    private DrylArtifactRun<Recipe>? _build;
```

Replace the whole `private async Task Generate()` method with:
```csharp
    private async Task Build()
    {
        if (string.IsNullOrWhiteSpace(_dish) || _buildBusy) return;
        _buildBusy = true;
        try
        {
            EnsureAgent();
            _session ??= await _agent!.CreateSessionAsync();
            _build = Runner.StartBuild<Recipe>(_agent!, _session,
                $"Build a recipe for: {_dish}. Brainstorm it with me.", aiKey: "build");
            _build.OnChange += OnRunChange;
            _ = WatchRunAsync(_build);
        }
        finally
        {
            _buildBusy = false;
        }
    }
```

Note: `WatchRunAsync` and `OnRunChange` already exist and accept `DrylAgentRun`; change `WatchRunAsync`'s parameter type to the shared base so both runs use it. Change its signature:
```csharp
    private async Task WatchRunAsync(DrylRunBase run)
```
(The body already only uses `WaitForCompletionAsync()`, `OnChange`, and `InvokeAsync` — all on `DrylRunBase`.) Add `@using DRYL.Components.Agents` is already present via `@using DRYL.Components.Agents.Generation`? Confirm `DrylRunBase`/`DrylArtifactRun` resolve — add `@using DRYL.Components.Agents` to the page if not already imported by `_Imports.razor`.

- [ ] **Step 3: Build the website**

Run: `dotnet build DRYL.Website/DRYL.Website.csproj -c Debug`
Expected: `Build succeeded`, 0 errors (pre-existing `CS8620` warning in `AgentsPlayground.razor` is unrelated).

- [ ] **Step 4: Manual verification (requires OPENAI_API_KEY)**

Run the site (`dotnet run --project DRYL.Website`), open `/agents-live`, Section 2, enter "Tiramisu", click **Build**. Expected behavior:
- The agent asks at least **two** separate questions via DRYL dialogs (not one), interleaved with `update_recipe` tool-call cards appearing in the trace.
- The recipe card **grows progressively** (title first, steps filled/!refined over multiple rounds) rather than appearing all at once.
- After the agent's final confirmation, the card settles (glow relaxes).

Two failure modes to check against the spec's open verification points:

- **The agent never asks a question (no DRYL dialogs appear), only `update_recipe` fires** → run-level `ChatOptions.Tools` are *replacing* the agent's construction-time HITL tools instead of merging. Fix: make `StartBuild<T>` also accept the HITL tools and compose the full set. Change the signature to `StartBuild<T>(AIAgent agent, AgentSession session, string prompt, IList<AITool>? extraTools = null, DrylBuildOptions? options = null, …)` and build `Tools = new List<AITool>(extraTools ?? Array.Empty<AITool>()) { CreateUpdateTool(run, options) }`; the demo passes `tools.All`. Re-run.
- **The agent asks once, then dumps the whole recipe (no iteration)** → the guidance didn't reach the model. Check whether `ChatOptions.Instructions` is additive to the agent's instructions in `Microsoft.Agents.AI` 1.10.0. If it is **replaced/ignored**, change `StartBuild` to prepend the guidance to the prompt instead: `RunStreamingAsync($"{options.Guidance ?? DefaultBuildGuidance}\n\n---\n\n{prompt}", session, runOptionsWithoutInstructions, ct)`. Re-run.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Website/Components/Pages/AgentsLive.razor
git commit -m "feat(website): demo Section 2 uses StartBuild collaborative builder

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Documentation — CHANGELOG + README

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `README.md`

- [ ] **Step 1: Add CHANGELOG entries**

In `CHANGELOG.md`, under `## [Unreleased]` → `### Added`, append these bullets after the existing `DrylUiTools` / dialog lines:

```markdown
- `DrylAgentRunner.StartBuild<T>` — Iterative, collaborative structured generation: the agent refines a `T` over multiple rounds via an auto-injected `update_<T>` merge tool while asking the user back, instead of a single-shot `response_format` dump
- `DrylArtifactRun<T>` — Observable handle for a build; live merged `Artifact` + `Round` counter atop the shared run surface
- `DrylBuildOptions` — `MaxRounds` safety cap (default 12), overridable `Guidance` prompt, custom `UpdateToolName`
- `DrylAiBuild<T>` / `ArtifactSnapshot<T>` — Renders the live, progressively-merged artifact (parallel to `DrylAiGenerate<T>`)
- `JsonMerge` — Deep-merge engine for partial artifact patches (objects merge, arrays/scalars replace, null/absent leaves existing)
- `DrylRunBase` — Shared run plumbing extracted from `DrylAgentRun`; base for `DrylAgentRun` and `DrylArtifactRun<T>` (public surface of `DrylAgentRun` unchanged)
```

- [ ] **Step 2: Add the README component-table row**

In `README.md`, in the "What's in the box (today)" table, add a row for the new UI component (match the existing column layout — name, category, AI mode, status, notes):

```markdown
| `DrylAiBuild<T>` | Agents | ✅ | ✅ Done | Renders a live, iteratively-built structured artifact |
```

(If the table's column set differs, mirror the existing `DrylAiGenerate` row exactly, changing only name and notes.)

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md README.md
git commit -m "docs(agents): changelog + README for collaborative artifact builder

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Final verification

- [ ] Run the full Agents test suite: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"` — all green (JsonMergeTests, DrylArtifactRunTests, DrylAgentRunnerBuildTests, DrylAiBuildTests, plus the unchanged DrylAgentRunnerTests/DrylAgentToolCallsTests).
- [ ] Build package + website clean.
- [ ] Manual demo confirms multi-round iteration (Task 6 Step 4).
