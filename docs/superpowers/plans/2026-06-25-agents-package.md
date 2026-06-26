# DRYL.Components.Agents Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `DRYL.Components.Agents`, a companion NuGet package that bridges the Microsoft Agent Framework (`Microsoft.Agents.AI` v1.10.0) to DRYL's existing AI primitives — automatic `AiState`, structured streaming UI, and four `DrylDialog`-backed human-in-the-loop tool functions.

**Architecture:** A new Razor class-library project references the core `DRYL.Components` and `Microsoft.Agents.AI`. It adds three subsystems that build *on top of* the core primitives (`AiState`, `IDrylAiActivityService`, `DrylToolCall`, `IDrylDialogService`, `DrylAiStream`) without changing the core. The runner consumes `IAsyncEnumerable<AgentResponseUpdate>` from `AIAgent.RunStreamingAsync(...)`, maps `Microsoft.Extensions.AI` content types (`TextReasoningContent`/`TextContent`/`FunctionCallContent`/`FunctionResultContent`) to `AiState`, and surfaces tool calls. A test-first `PartialJsonReader<T>` turns a raw JSON token stream into progressively-richer partial snapshots.

**Tech Stack:** .NET (net8.0;net9.0;net10.0), Blazor (Server/WASM), `Microsoft.Agents.AI` 1.10.0, `Microsoft.Extensions.AI(.Abstractions)` 10.6.0, `System.Text.Json`, xUnit + bUnit.

## Global Constraints

- **Core stays dependency-free.** Do NOT add `Microsoft.Agents.AI` to `DRYL.Components`. The SDK lives only in the new package (CLAUDE.md §2.8, spec non-goal).
- **No new `AiState` values, no new AI animation/color/token, no `dryl.css` change.** Reuse the existing `AiState` enum and `.ai-aura*` primitives (CLAUDE.md §2.10, §2.1).
- **`Ai`/AI styling is opt-in and off by default.** Mapped state defaults to `AiState.None`.
- **Reuse, do not duplicate.** Tool-call visuals come from the *core* `DrylToolCall`; questions reuse the *core* `IDrylDialogService` + `ShowAsync<TDialog>` pipeline; `RequestPermission` reuses the core `DrylConfirmDialog`.
- **Multi-target `net8.0;net9.0;net10.0`** — confirmed: `Microsoft.Agents.AI` 1.10.0 ships `lib/net8.0`, `lib/net9.0`, `lib/net10.0`.
- **Independent version `0.1.0`** (experimental), own `PackageId`, decoupled from core `1.0.0`.
- **Blazor naming:** components PascalCase `Dryl`-prefixed; CSS classes kebab-case; namespace `DRYL.Components.Agents` (sub-namespaced by folder where useful).
- **Strongly-typed params, XML doc comments** on every public type/parameter.
- **Platform rule (documented, not guarded):** all three subsystems require interactive Blazor with a live circuit; the agent run must execute in the same DI scope (circuit) as the UI because `IDrylDialogService` is scoped per circuit.
- **Docs are mandatory** (CLAUDE.md §7): `CHANGELOG.md` `[Unreleased]/Added` (one entry per public type), `README.md` component table rows marked as Agents-package, new `DRYL.Components.Agents/PACKAGE.md`.

### Pinned Agent Framework API (verified against installed v1.10.0 / Extensions.AI 10.6.0)

```csharp
// Streaming run — returns IAsyncEnumerable<AgentResponseUpdate>
IAsyncEnumerable<AgentResponseUpdate> AIAgent.RunStreamingAsync(
    string message, AgentSession session, AgentRunOptions? options = null, CancellationToken ct = default);

// AgentResponseUpdate: .Contents (IList<AIContent>), .Text (string convenience), .Role
// Content types (Microsoft.Extensions.AI):
//   TextReasoningContent(string)            -> .Text                      (model reasoning)
//   TextContent(string)                     -> .Text                      (answer text delta)
//   FunctionCallContent(string callId, string name, IDictionary<string,object>? arguments) -> .CallId .Name .Arguments
//   FunctionResultContent(string callId, object? result)                  -> .CallId .Result .Exception
// AgentResponseUpdate is publicly constructible: new AgentResponseUpdate(ChatRole?, IList<AIContent>)

// Session + agent construction
AgentSession session = await agent.CreateSessionAsync(ct);
var agent = new ChatClientAgent(chatClient, instructions:, name:, description:, tools: IList<AITool>);

// Structured output: set response format on the run options
var runOptions = new ChatClientAgentRunOptions {
    ChatOptions = new ChatOptions {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<T>(jsonSerializerOptions, name, description)
    }
};

// Tools
AITool t = AIFunctionFactory.Create(delegate, name, description);
```

---

## File Structure

```
DRYL.Components.Agents/                         (NEW Razor class library, multi-target)
├─ DRYL.Components.Agents.csproj
├─ PACKAGE.md
├─ _Imports.razor                               (@using DRYL.Components, DRYL.Components.Agents, ...)
├─ Extensions/
│  └─ ServiceCollectionExtensions.cs            AddDrylAgents()
├─ Agents/                                       Subsystem 1
│  ├─ DrylToolInvocation.cs                      data model (maps 1:1 to core DrylToolCall params)
│  ├─ DrylAgentRun.cs                            observable handle (IAsyncDisposable)
│  ├─ DrylAgentRunner.cs                         scoped service: Start(...) + GenerateStreamingAsync<T>(...)
│  └─ DrylAgentToolCalls.razor                   thin wrapper -> core DrylToolCall per invocation
├─ Generation/                                   Subsystem 2
│  ├─ JsonPartialRepair.cs                       static repair engine (tolerant close)
│  ├─ PartialJsonReader.cs                       PartialJsonReader<T>: Append -> snapshot, hold-last-good
│  ├─ GenerationSnapshot.cs                      GenerationSnapshot<T> (Value, State, IsComplete)
│  └─ DrylAiGenerate.razor                       DrylAiGenerate<T> (Source-based, mirrors DrylAiStream)
└─ Tools/                                        Subsystem 3
   ├─ DrylUiTools.cs                             factory: Create(IDrylDialogService) -> AskChoice/.../All
   ├─ DrylAskChoiceDialog.razor                  radio list, recommended badge
   ├─ DrylAskMultiChoiceDialog.razor            checkbox list, recommendations pre-checked
   └─ DrylAskTextDialog.razor                    DrylInputText, returns text

tests/DRYL.Components.Tests/Agents/             (added to the EXISTING test project)
├─ JsonPartialRepairTests.cs
├─ PartialJsonReaderTests.cs
├─ DrylAgentRunnerTests.cs
├─ DrylUiToolsTests.cs
├─ DrylAgentToolCallsTests.cs
└─ DrylAiGenerateTests.cs
```

---

## Phase 1 — Foundation

### Task 1: Create the package project and wire it into the solution

**Files:**
- Create: `DRYL.Components.Agents/DRYL.Components.Agents.csproj`
- Create: `DRYL.Components.Agents/_Imports.razor`
- Create: `DRYL.Components.Agents/PACKAGE.md`
- Modify: `DRYL.slnx`
- Modify: `tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`

**Interfaces:**
- Produces: a buildable, packable project `DRYL.Components.Agents` (PackageId `DRYL.Components.Agents`, Version `0.1.0`) referencing `DRYL.Components` (ProjectReference) and `Microsoft.Agents.AI` 1.10.0; the test project references it.

- [ ] **Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>

    <!-- ===== NuGet package identity (independent, experimental) ===== -->
    <PackageId>DRYL.Components.Agents</PackageId>
    <Version>0.1.0</Version>
    <Title>DRYL.Components.Agents — Microsoft Agent Framework integration for DRYL</Title>
    <Description>Companion package for DRYL.Components: bridges the Microsoft Agent Framework to DRYL's AI vocabulary — automatic AiState from an agent run, structured streaming UI (DrylAiGenerate&lt;T&gt;), and ready-made DrylDialog-backed human-in-the-loop tool functions.</Description>
    <PackageTags>blazor;ai;agents;microsoft-agent-framework;dryl;streaming;structured-output</PackageTags>

    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>PACKAGE.md</PackageReadmeFile>
    <PackageReleaseNotes>Experimental 0.1.0. See CHANGELOG.md.</PackageReleaseNotes>

    <IsPackable>true</IsPackable>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <None Include="PACKAGE.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- During development reference the core by project; the pack still emits a
       NuGet PackageReference dependency on DRYL.Components (see ProjectReference PrivateAssets). -->
  <ItemGroup>
    <ProjectReference Include="..\DRYL.Components\DRYL.Components.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Agents.AI" Version="1.10.0" />
  </ItemGroup>

  <!-- The Components.Web metapackage version must track the target framework. -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />
  </ItemGroup>

</Project>
```

> Note: a bare `ProjectReference` to the core packs as a NuGet dependency `DRYL.Components` only if the core project is itself packable (it is). Keep the ProjectReference (no `PrivateAssets`) so consumers transitively get the core.

- [ ] **Step 2: Create `_Imports.razor`**

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using DRYL.Components
@using DRYL.Components.Agents
@using DRYL.Components.Agents.Tools
@using DRYL.Components.Dialogs
```

- [ ] **Step 3: Create a minimal `PACKAGE.md`** (full content filled in Task 17; a one-paragraph stub is enough to make the project pack now)

```markdown
# DRYL.Components.Agents

Companion package for [DRYL.Components](https://www.nuget.org/packages/DRYL.Components)
that bridges the Microsoft Agent Framework to DRYL's AI vocabulary. **Experimental (0.1.0).**

See the repository README and CHANGELOG for details.
```

- [ ] **Step 4: Add the project + test reference to the solution**

Edit `DRYL.slnx` to add the project node:

```xml
<Solution>
  <Folder Name="/tests/">
    <Project Path="tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj" />
  </Folder>
  <Project Path="DRYL.Components/DRYL.Components.csproj" />
  <Project Path="DRYL.Components.Agents/DRYL.Components.Agents.csproj" />
</Solution>
```

Add a ProjectReference in `tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj` inside the existing ProjectReference `ItemGroup`:

```xml
    <ProjectReference Include="..\..\DRYL.Components.Agents\DRYL.Components.Agents.csproj" />
```

- [ ] **Step 5: Build the solution to verify it restores and compiles**

Run: `dotnet build DRYL.slnx -c Debug`
Expected: Build succeeded; `DRYL.Components.Agents` restores `Microsoft.Agents.AI 1.10.0` for all three TFMs.

- [ ] **Step 6: Verify the package packs**

Run: `dotnet pack DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release -o artifacts`
Expected: produces `artifacts/DRYL.Components.Agents.0.1.0.nupkg` (+ `.snupkg`).

- [ ] **Step 7: Commit**

```bash
git add DRYL.Components.Agents DRYL.slnx tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj
git commit -m "feat(agents): scaffold DRYL.Components.Agents package"
```

---

### Task 2: `AddDrylAgents()` DI extension + smoke test

**Files:**
- Create: `DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`
- Create: `DRYL.Components.Agents/Agents/DrylAgentRunner.cs` (minimal shell — full body in Task 4)
- Test: `tests/DRYL.Components.Tests/Agents/AddDrylAgentsTests.cs`

**Interfaces:**
- Produces: `IServiceCollection AddDrylAgents(this IServiceCollection)` registering `DrylAgentRunner` as **scoped**; `DrylAgentRunner` is resolvable.

- [ ] **Step 1: Write the failing test**

```csharp
using DRYL.Components;                       // AddDrylComponents
using DRYL.Components.Agents;                // AddDrylAgents, DrylAgentRunner
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Agents;

public class AddDrylAgentsTests
{
    [Fact]
    public void AddDrylAgents_registers_runner_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();   // core services the runner builds on
        services.AddDrylAgents();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var runner = scope.ServiceProvider.GetService<DrylAgentRunner>();
        Assert.NotNull(runner);

        // Scoped: a different scope yields a different instance.
        using var scope2 = provider.CreateScope();
        Assert.NotSame(runner, scope2.ServiceProvider.GetService<DrylAgentRunner>());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~AddDrylAgentsTests`
Expected: FAIL — `AddDrylAgents` / `DrylAgentRunner` not defined.

- [ ] **Step 3: Create the minimal `DrylAgentRunner` shell**

```csharp
using DRYL.Components.Ai;

namespace DRYL.Components.Agents;

/// <summary>
/// Scoped service that starts Microsoft Agent Framework runs and bridges them to
/// DRYL's AI vocabulary. Registered via <c>AddDrylAgents()</c>.
/// </summary>
public sealed partial class DrylAgentRunner
{
    private readonly IDrylAiActivityService? _activity;

    /// <summary>Creates the runner. The optional AI-activity service drives a surrounding <c>DrylAiScope</c>.</summary>
    public DrylAgentRunner(IDrylAiActivityService? activity = null) => _activity = activity;
}
```

- [ ] **Step 4: Create the DI extension**

```csharp
using DRYL.Components.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Agents;

/// <summary>DI helpers for registering DRYL.Components.Agents services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the DRYL Agents services. Call alongside <c>AddDrylComponents()</c>:
    /// <code>builder.Services.AddDrylComponents().AddDrylAgents();</code>
    /// Registers <see cref="DrylAgentRunner"/> as scoped (one per Blazor circuit).
    /// </summary>
    public static IServiceCollection AddDrylAgents(this IServiceCollection services)
    {
        services.AddScoped<DrylAgentRunner>();
        return services;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~AddDrylAgentsTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Extensions DRYL.Components.Agents/Agents/DrylAgentRunner.cs tests/DRYL.Components.Tests/Agents/AddDrylAgentsTests.cs
git commit -m "feat(agents): AddDrylAgents() DI registration"
```

---

### Task 3: Wire the package into CI

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: CI builds/tests `DRYL.slnx` (already includes the new project) and validates packing of the Agents package.

- [ ] **Step 1: Add an Agents pack-validation step** after the existing "Pack (validate packaging)" step in `ci.yml`:

```yaml
      - name: Pack Agents (validate packaging)
        run: dotnet pack DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release --no-build -o artifacts
```

(The existing "Build" step builds `DRYL.slnx`, which now includes the Agents project, so `--no-build` is valid. The existing "Upload package artifacts" globs `artifacts/*.*nupkg` and will include the Agents package automatically.)

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci(agents): validate Agents package packing"
```

> Release wiring is intentionally deferred: `release.yml` derives the version from the pushed `v*.*.*` tag and publishes only `DRYL.Components`. The Agents package has an independent `0.1.0` version; its first publish is a maintainer action and is out of scope for this plan (noted in PACKAGE.md, Task 17).

---

## Phase 2 — Subsystem 1: agent run → automatic AiState & tool calls

### Task 4: `DrylToolInvocation` model

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylToolInvocation.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylToolInvocationTests.cs`

**Interfaces:**
- Produces: `DrylToolInvocation` with mutable fields `CallId`, `ToolName`, `Arguments` (JSON string), `Result` (JSON string), `Error`, and a computed `State` (`AiState`): running (no result, no error) → `Thinking`; completed → `Generated`; error set → `None`.

- [ ] **Step 1: Write the failing test**

```csharp
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylToolInvocationTests
{
    [Fact]
    public void State_is_Thinking_while_running()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "get_weather" };
        Assert.Equal(AiState.Thinking, t.State);
    }

    [Fact]
    public void State_is_Generated_when_result_set()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "get_weather", Result = "\"sunny\"" };
        Assert.Equal(AiState.Generated, t.State);
    }

    [Fact]
    public void State_is_None_when_error_set()
    {
        var t = new DrylToolInvocation { CallId = "1", ToolName = "x", Error = "boom" };
        Assert.Equal(AiState.None, t.State);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylToolInvocationTests`
Expected: FAIL — type not defined.

- [ ] **Step 3: Implement**

```csharp
namespace DRYL.Components.Agents;

/// <summary>
/// A single agent tool / function call captured from an agent run. Its fields map 1:1
/// onto the core <c>DrylToolCall</c> presentational component; <see cref="State"/> is
/// derived from the call's lifecycle so the UI shows the right AI vocabulary.
/// </summary>
public sealed class DrylToolInvocation
{
    /// <summary>The framework call id, used to match a result back to its call.</summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>The tool / function name the model invoked.</summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>The call arguments as JSON (shown in the collapsible body).</summary>
    public string? Arguments { get; set; }

    /// <summary>The call result as JSON; <c>null</c> until the result arrives.</summary>
    public string? Result { get; set; }

    /// <summary>An error message; when set, the call is rendered as failed.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Lifecycle mapped to the shared AI vocabulary: running → <see cref="AiState.Thinking"/>,
    /// completed → <see cref="AiState.Generated"/>, errored → <see cref="AiState.None"/>
    /// (the error is shown via the core component's danger alert).
    /// </summary>
    public AiState State =>
        Error is not null ? AiState.None
        : Result is not null ? AiState.Generated
        : AiState.Thinking;
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylToolInvocationTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylToolInvocation.cs tests/DRYL.Components.Tests/Agents/DrylToolInvocationTests.cs
git commit -m "feat(agents): DrylToolInvocation model with lifecycle AiState"
```

---

### Task 5: `DrylAgentRun` handle + the update→state core in `DrylAgentRunner`

This is the heart of Subsystem 1. The runner's public `Start(AIAgent, ...)` is a thin wrapper over an **internal, testable** method that consumes a raw `IAsyncEnumerable<AgentResponseUpdate>` — so the state machine is unit-tested without a real provider.

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylAgentRun.cs`
- Modify: `DRYL.Components.Agents/Agents/DrylAgentRunner.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerTests.cs`

**Interfaces:**
- Consumes: `DrylToolInvocation` (Task 4); `IDrylAiActivityService` (core); `AgentResponseUpdate`, `TextContent`, `TextReasoningContent`, `FunctionCallContent`, `FunctionResultContent` (SDK).
- Produces:
  - `DrylAgentRun` (`IAsyncDisposable`): `AiState State`, `string Text`, `IReadOnlyList<DrylToolInvocation> ToolCalls`, `event Action? OnChange`, `IAsyncEnumerable<string> TextStream`.
  - `DrylAgentRunner.Start(AIAgent agent, AgentSession session, string message, string? aiKey = null, CancellationToken ct = default) : DrylAgentRun`.
  - `internal DrylAgentRun StartFromUpdates(IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)` (test seam).

- [ ] **Step 1: Write the failing tests** (state transitions + tool mapping, against hand-built updates)

```csharp
using System.Runtime.CompilerServices;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerTests
{
    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        IEnumerable<AgentResponseUpdate> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var i in items) { ct.ThrowIfCancellationRequested(); yield return i; await Task.Yield(); }
    }

    private static AgentResponseUpdate Content(params AIContent[] c) =>
        new(ChatRole.Assistant, c.ToList());

    [Fact]
    public async Task Text_deltas_accumulate_and_drive_Streaming_then_Generated()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new TextContent("Hel")),
            Content(new TextContent("lo")),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        Assert.Equal("Hello", run.Text);
        Assert.Equal(AiState.Generated, run.State);
    }

    [Fact]
    public async Task Tool_call_then_result_maps_to_invocation_with_Generated_state()
    {
        var runner = new DrylAgentRunner();
        var run = runner.StartFromUpdates(Updates(new AgentResponseUpdate[]
        {
            Content(new FunctionCallContent("call-1", "get_weather",
                new Dictionary<string, object?> { ["city"] = "Berlin" }!)),
            Content(new FunctionResultContent("call-1", "sunny")),
            Content(new TextContent("It is sunny.")),
        }), aiKey: null, ct: default);

        await run.WaitForCompletionAsync();

        var call = Assert.Single(run.ToolCalls);
        Assert.Equal("get_weather", call.ToolName);
        Assert.Contains("Berlin", call.Arguments);
        Assert.Contains("sunny", call.Result);
        Assert.Equal(AiState.Generated, call.State);
    }

    [Fact]
    public async Task Reasoning_before_text_keeps_state_Thinking()
    {
        var runner = new DrylAgentRunner();
        var states = new List<AiState>();
        var run = runner.StartFromUpdates(Updates(new[]
        {
            Content(new TextReasoningContent("hmm")),
        }), aiKey: null, ct: default);
        run.OnChange += () => states.Add(run.State);

        await run.WaitForCompletionAsync();

        Assert.Contains(AiState.Thinking, states);
    }
}
```

> The test uses a helper `WaitForCompletionAsync()` on the run (added below) so the assertion is deterministic without sleeping.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentRunnerTests`
Expected: FAIL — `StartFromUpdates` / `DrylAgentRun` not defined.

- [ ] **Step 3: Implement `DrylAgentRun`**

```csharp
using System.Threading.Channels;
using DRYL.Components.Ai;

namespace DRYL.Components.Agents;

/// <summary>
/// Observable handle to a running agent. Drives <see cref="State"/> automatically and
/// exposes the accumulated <see cref="Text"/>, the live <see cref="ToolCalls"/> trace,
/// and a <see cref="TextStream"/> ready to drop into <c>DrylAiStream</c>/<c>DrylMarkdown</c>.
/// </summary>
public sealed class DrylAgentRun : IAsyncDisposable
{
    private readonly List<DrylToolInvocation> _toolCalls = new();
    private readonly Channel<string> _textChannel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource _completed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Current AI state, driven automatically by the run.</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>The accumulated answer text so far.</summary>
    public string Text { get; internal set; } = string.Empty;

    /// <summary>The tool calls observed in this run, in arrival order.</summary>
    public IReadOnlyList<DrylToolInvocation> ToolCalls => _toolCalls;

    /// <summary>Raised whenever <see cref="State"/>, <see cref="Text"/>, or <see cref="ToolCalls"/> changes.</summary>
    public event Action? OnChange;

    /// <summary>The text deltas as an async stream — feed directly to <c>DrylAiStream Source="..."</c>.</summary>
    public IAsyncEnumerable<string> TextStream => _textChannel.Reader.ReadAllAsync();

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

- [ ] **Step 4: Implement the runner core** (append to `DrylAgentRunner.cs`)

```csharp
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

public sealed partial class DrylAgentRunner
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    /// <summary>
    /// Start an agent run and return an observable <see cref="DrylAgentRun"/>. The run drives
    /// <see cref="AiState"/> automatically and (when <paramref name="aiKey"/> is set and the
    /// activity service is registered) lights up a surrounding <c>DrylAiScope Key="..."</c>.
    /// </summary>
    public DrylAgentRun Start(AIAgent agent, AgentSession session, string message,
                              string? aiKey = null, CancellationToken ct = default)
    {
        var updates = agent.RunStreamingAsync(message, session, options: null, cancellationToken: ct);
        return StartFromUpdates(updates, aiKey, ct);
    }

    internal DrylAgentRun StartFromUpdates(
        IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)
    {
        var run = new DrylAgentRun();
        _ = ProcessAsync(run, updates, aiKey, ct);
        return run;
    }

    private async Task ProcessAsync(
        DrylAgentRun run, IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey, CancellationToken ct)
    {
        SetState(run, AiState.Thinking, aiKey);
        var sawText = false;
        var byCallId = new Dictionary<string, DrylToolInvocation>();

        try
        {
            await foreach (var update in updates.WithCancellation(ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextReasoningContent:
                            SetState(run, AiState.Thinking, aiKey);
                            break;

                        case FunctionCallContent fc:
                        {
                            var inv = new DrylToolInvocation
                            {
                                CallId = fc.CallId,
                                ToolName = fc.Name,
                                Arguments = fc.Arguments is null
                                    ? null : JsonSerializer.Serialize(fc.Arguments, JsonOpts),
                            };
                            byCallId[fc.CallId] = inv;
                            run.AddToolCall(inv);
                            SetState(run, AiState.Thinking, aiKey);
                            break;
                        }

                        case FunctionResultContent fr:
                        {
                            if (byCallId.TryGetValue(fr.CallId, out var inv))
                            {
                                if (fr.Exception is not null) inv.Error = fr.Exception.Message;
                                else inv.Result = SerializeResult(fr.Result);
                                run.Raise();
                            }
                            break;
                        }

                        case TextContent tc when !string.IsNullOrEmpty(tc.Text):
                            if (!sawText) { sawText = true; }
                            run.Text += tc.Text;
                            run.PushText(tc.Text);
                            SetState(run, AiState.Streaming, aiKey);
                            break;
                    }
                }
            }

            SetState(run, AiState.Generated, aiKey);
        }
        catch (OperationCanceledException)
        {
            SetStateRaw(run, AiState.None, aiKey);   // clear the scope on cancel
        }
        catch (Exception)
        {
            SetStateRaw(run, AiState.None, aiKey);    // surface via consumer; keep UI consistent
        }
        finally
        {
            run.CompleteText();
            run.MarkCompleted();
        }
    }

    private static string SerializeResult(object? result) =>
        result switch
        {
            null => "null",
            string s => JsonSerializer.Serialize(s, JsonOpts),
            JsonElement je => je.GetRawText(),
            _ => JsonSerializer.Serialize(result, JsonOpts),
        };

    private void SetState(DrylAgentRun run, AiState state, string? aiKey)
    {
        run.State = state;
        if (_activity is not null && aiKey is not null) _activity.Set(aiKey, state);
        run.Raise();
    }

    private void SetStateRaw(DrylAgentRun run, AiState state, string? aiKey)
    {
        run.State = state;
        if (_activity is not null && aiKey is not null)
        {
            if (state == AiState.None) _activity.Clear(aiKey);
            else _activity.Set(aiKey, state);
        }
        run.Raise();
    }
}
```

- [ ] **Step 5: Run to verify the tests pass**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentRunnerTests`
Expected: PASS (all three).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylAgentRun.cs DRYL.Components.Agents/Agents/DrylAgentRunner.cs tests/DRYL.Components.Tests/Agents/DrylAgentRunnerTests.cs
git commit -m "feat(agents): DrylAgentRun + automatic AiState/tool-call bridge"
```

---

### Task 6: `DrylAgentToolCalls` wrapper component

**Files:**
- Create: `DRYL.Components.Agents/Agents/DrylAgentToolCalls.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAgentToolCallsTests.cs`

**Interfaces:**
- Consumes: `DrylAgentRun` (Task 5), core `DrylToolCall`.
- Produces: `DrylAgentToolCalls` with `[Parameter] DrylAgentRun? Run`, `[Parameter] bool ActiveOnly`; renders one core `DrylToolCall` per (optionally only the running) invocation, and re-renders on `Run.OnChange`.

- [ ] **Step 1: Write the failing bUnit test**

```csharp
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentToolCallsTests : TestContext
{
    [Fact]
    public void Renders_one_tool_call_per_invocation()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(new DrylToolInvocation { CallId = "1", ToolName = "alpha", Result = "\"ok\"" });
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "beta" });

        var cut = RenderComponent<DrylAgentToolCalls>(p => p.Add(x => x.Run, run));

        Assert.Equal(2, cut.FindAll(".tool-call").Count);
        Assert.Contains("alpha", cut.Markup);
        Assert.Contains("beta", cut.Markup);
    }

    [Fact]
    public void ActiveOnly_shows_only_running_calls()
    {
        var run = new DrylAgentRun();
        run.AddToolCall(new DrylToolInvocation { CallId = "1", ToolName = "done", Result = "\"ok\"" });
        run.AddToolCall(new DrylToolInvocation { CallId = "2", ToolName = "running" });

        var cut = RenderComponent<DrylAgentToolCalls>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ActiveOnly, true));

        Assert.Single(cut.FindAll(".tool-call"));
        Assert.Contains("running", cut.Markup);
        Assert.DoesNotContain("done", cut.Markup);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentToolCallsTests`
Expected: FAIL — component not defined.

- [ ] **Step 3: Implement**

```razor
@namespace DRYL.Components.Agents
@implements IDisposable

@*  Thin wrapper: renders the agent run's tool calls using the core DrylToolCall. *@

@if (Run is not null)
{
    foreach (var t in Visible)
    {
        <DrylToolCall @key="t.CallId"
                      ToolName="@t.ToolName"
                      Arguments="@t.Arguments"
                      Result="@t.Result"
                      Error="@t.Error"
                      State="@t.State" />
    }
}

@code {
    /// <summary>The agent run whose tool calls to visualise.</summary>
    [Parameter] public DrylAgentRun? Run { get; set; }

    /// <summary>When true, render only the currently-running call; otherwise the full trace (default).</summary>
    [Parameter] public bool ActiveOnly { get; set; }

    private DrylAgentRun? _subscribed;

    private IEnumerable<DrylToolInvocation> Visible =>
        Run is null ? Enumerable.Empty<DrylToolInvocation>()
        : ActiveOnly ? Run.ToolCalls.Where(t => t.State == AiState.Thinking)
        : Run.ToolCalls;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Run)) return;
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
        _subscribed = Run;
        if (_subscribed is not null) _subscribed.OnChange += HandleChange;
    }

    private void HandleChange() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null) _subscribed.OnChange -= HandleChange;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentToolCallsTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylAgentToolCalls.razor tests/DRYL.Components.Tests/Agents/DrylAgentToolCallsTests.cs
git commit -m "feat(agents): DrylAgentToolCalls wrapper over core DrylToolCall"
```

---

## Phase 3 — Subsystem 2: structured streaming UI

The `PartialJsonReader` is the highest-risk piece — built strictly test-first, one repair concern per task.

### Task 7: `JsonPartialRepair` — close open strings & containers

**Files:**
- Create: `DRYL.Components.Agents/Generation/JsonPartialRepair.cs`
- Test: `tests/DRYL.Components.Tests/Agents/JsonPartialRepairTests.cs`

**Interfaces:**
- Produces: `static string JsonPartialRepair.Close(string partial)` — returns a *parseable* JSON string by virtually closing open strings/objects/arrays and dropping trailing incompletes. For already-complete input it returns the input unchanged (modulo whitespace).

- [ ] **Step 1: Write the failing tests (string + container closing, partial string content preserved)**

```csharp
using System.Text.Json;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class JsonPartialRepairTests
{
    private static JsonDocument Parse(string s) => JsonDocument.Parse(JsonPartialRepair.Close(s));

    [Fact]
    public void Complete_object_is_unchanged_semantically()
    {
        using var doc = Parse("""{"a":1,"b":"x"}""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.Equal("x", doc.RootElement.GetProperty("b").GetString());
    }

    [Fact]
    public void Unclosed_object_is_closed()
    {
        using var doc = Parse("""{"a":1""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }

    [Fact]
    public void Open_string_is_closed_keeping_partial_content()
    {
        using var doc = Parse("""{"title":"Hel""");
        Assert.Equal("Hel", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void Unclosed_array_with_half_object_is_closed()
    {
        using var doc = Parse("""{"steps":["a","b""");
        var steps = doc.RootElement.GetProperty("steps");
        Assert.Equal(2, steps.GetArrayLength());
        Assert.Equal("a", steps[0].GetString());
        Assert.Equal("b", steps[1].GetString());
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~JsonPartialRepairTests`
Expected: FAIL — type not defined.

- [ ] **Step 3: Implement the scanner + closer**

```csharp
using System.Text;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Turns a partial (mid-stream) JSON buffer into a parseable JSON string by closing
/// open strings and containers and dropping trailing incomplete tokens. Pure and
/// allocation-light; called once per streamed chunk.
/// </summary>
public static class JsonPartialRepair
{
    /// <summary>Return a parseable JSON string derived from <paramref name="partial"/>.</summary>
    public static string Close(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return "null";

        var stack = new Stack<char>();   // '{' or '['
        var inString = false;
        var escaped = false;
        var lastSignificant = -1;        // index of last non-whitespace, non-string char outside strings

        for (var i = 0; i < partial.Length; i++)
        {
            var c = partial[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': case '[': stack.Push(c); break;
                case '}': case ']': if (stack.Count > 0) stack.Pop(); break;
            }
            if (!char.IsWhiteSpace(c)) lastSignificant = i;
        }

        // Trim to the last significant char unless we're inside a string (then keep the tail).
        var sb = new StringBuilder();
        if (inString)
        {
            sb.Append(partial);                 // keep partial string content as-is
            if (escaped) sb.Append(' ');        // neutralise a dangling backslash
            sb.Append('"');                      // close the open string
        }
        else
        {
            var end = lastSignificant + 1;
            var trimmed = partial.AsSpan(0, end).ToString();
            trimmed = DropTrailingIncomplete(trimmed);
            sb.Append(trimmed);
        }

        // Close remaining open containers, innermost first.
        foreach (var open in stack)
            sb.Append(open == '{' ? '}' : ']');

        return sb.Length == 0 ? "null" : sb.ToString();
    }

    // Drops a trailing structural token that can't be closed cleanly:
    //   trailing ',' (before a not-yet-arrived element)  -> drop
    //   trailing ':' (key with no value yet)             -> drop the key too
    private static string DropTrailingIncomplete(string s)
    {
        var t = s.TrimEnd();
        if (t.Length == 0) return t;

        var last = t[^1];
        if (last == ',') return t[..^1].TrimEnd();
        if (last == ':')
        {
            // remove ': ' then the preceding "key" string, then a dangling comma if any
            var idx = t.Length - 1;            // at ':'
            idx--;                              // before ':'
            while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
            if (idx >= 0 && t[idx] == '"')
            {
                idx--;                         // skip closing quote of key
                while (idx >= 0 && !(t[idx] == '"' && t[idx - 1] != '\\')) idx--;
                idx--;                         // skip opening quote of key
            }
            while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
            if (idx >= 0 && t[idx] == ',') idx--;
            return t[..(idx + 1)].TrimEnd();
        }
        return t;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~JsonPartialRepairTests`
Expected: PASS (all four).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Generation/JsonPartialRepair.cs tests/DRYL.Components.Tests/Agents/JsonPartialRepairTests.cs
git commit -m "feat(agents): JsonPartialRepair — close open strings & containers"
```

---

### Task 8: `JsonPartialRepair` — drop half-written values

**Files:**
- Modify: `DRYL.Components.Agents/Generation/JsonPartialRepair.cs`
- Modify: `tests/DRYL.Components.Tests/Agents/JsonPartialRepairTests.cs`

**Interfaces:**
- Produces: `Close(...)` additionally tolerates a half-written **number** (`12.`, `-`, `1e`), a half-written **literal** (`tru`, `fals`, `nul`), and a dangling key-before-value, by dropping that property/element so the rest still parses.

- [ ] **Step 1: Add failing tests**

```csharp
    [Fact]
    public void Half_written_number_value_is_dropped()
    {
        using var doc = Parse("""{"name":"x","price":12.""");
        Assert.Equal("x", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.TryGetProperty("price", out _));
    }

    [Fact]
    public void Half_written_literal_is_dropped()
    {
        using var doc = Parse("""{"a":1,"ok":tru""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.False(doc.RootElement.TryGetProperty("ok", out _));
    }

    [Fact]
    public void Dangling_key_before_colon_is_dropped()
    {
        using var doc = Parse("""{"a":1,"b""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
        Assert.False(doc.RootElement.TryGetProperty("b", out _));
    }

    [Fact]
    public void Trailing_comma_is_dropped()
    {
        using var doc = Parse("""{"a":1,""");
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }
```

> Note: `Dangling_key_before_colon_is_dropped` ends inside a *string* (`"b`). After Task 7 the open string is closed to `"b"`, leaving `{"a":1,"b"}` which is invalid (key with no value). This is the case the retry-on-failure trim (below) must repair. Implement via a bounded retry that re-runs `Close` on progressively shorter buffers until it parses.

- [ ] **Step 2: Run to verify the new tests fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~JsonPartialRepairTests`
Expected: FAIL on the four new tests (the half-value and dangling-key cases produce invalid JSON).

- [ ] **Step 3: Add a validated, self-correcting entry point**

Replace the public `Close` with a version that validates and retry-trims, keeping the scanner (`CloseOnce`) as the inner step:

```csharp
    /// <summary>Return a parseable JSON string derived from <paramref name="partial"/>.</summary>
    public static string Close(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return "null";

        // Try closing the full buffer, then progressively drop the trailing char until it
        // parses. Bounded by the buffer length; in practice succeeds within a few chars.
        for (var len = partial.Length; len > 0; len--)
        {
            var candidate = CloseOnce(partial.AsSpan(0, len).ToString());
            if (IsParseable(candidate)) return candidate;
        }
        return "null";
    }

    private static bool IsParseable(string json)
    {
        try { using var _ = System.Text.Json.JsonDocument.Parse(json); return true; }
        catch (System.Text.Json.JsonException) { return false; }
    }
```

Rename the previous `Close` body to `private static string CloseOnce(string partial)` (the scanner from Task 7, including `DropTrailingIncomplete`).

- [ ] **Step 4: Run to verify all repair tests pass**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~JsonPartialRepairTests`
Expected: PASS (all eight).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Generation/JsonPartialRepair.cs tests/DRYL.Components.Tests/Agents/JsonPartialRepairTests.cs
git commit -m "feat(agents): JsonPartialRepair — tolerate half-written values via retry-trim"
```

---

### Task 9: `PartialJsonReader<T>` — snapshots with hold-last-good

**Files:**
- Create: `DRYL.Components.Agents/Generation/PartialJsonReader.cs`
- Test: `tests/DRYL.Components.Tests/Agents/PartialJsonReaderTests.cs`

**Interfaces:**
- Consumes: `JsonPartialRepair.Close` (Tasks 7–8).
- Produces: `PartialJsonReader<T>` with ctor `(JsonSerializerOptions? options = null)`, `T? Append(string chunk)`, `T? Current { get; }`, `string Buffer { get; }`. Deserializes the repaired buffer to `T?` (case-insensitive, allow trailing commas); on parse/repair failure returns the last good snapshot rather than null.

- [ ] **Step 1: Write the failing tests** (uses a small DTO)

```csharp
using System.Text.Json;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Tests.Agents;

public class PartialJsonReaderTests
{
    public sealed class Recipe
    {
        public string? Title { get; set; }
        public List<string> Steps { get; set; } = new();
    }

    [Fact]
    public void Surfaces_partial_title_character_by_character()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("""{"title":"Pan""");
        Assert.Equal("Pan", r.Current!.Title);

        r.Append("cakes\"");
        Assert.Equal("Pancakes", r.Current!.Title);
    }

    [Fact]
    public void Builds_array_incrementally()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("""{"title":"X","steps":["mix""");
        Assert.Single(r.Current!.Steps);
        Assert.Equal("mix", r.Current!.Steps[0]);

        r.Append("""","bake"]}""");
        Assert.Equal(2, r.Current!.Steps.Count);
        Assert.Equal("bake", r.Current!.Steps[1]);
    }

    [Fact]
    public void Holds_last_good_snapshot_on_unparseable_intermediate()
    {
        var r = new PartialJsonReader<Recipe>();
        r.Append("""{"title":"Done"""");      // -> Title "Done"
        var before = r.Current!.Title;
        r.Append(",\\");                        // garbage-ish continuation
        Assert.Equal(before, r.Current!.Title); // never regress to null
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~PartialJsonReaderTests`
Expected: FAIL — type not defined.

- [ ] **Step 3: Implement**

```csharp
using System.Text;
using System.Text.Json;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Accumulates a streamed JSON buffer and produces a progressively-richer partial
/// snapshot of <typeparamref name="T"/> on every chunk. Open strings surface their
/// content-so-far, so titles and text grow character by character. On a parse failure
/// it holds the last good snapshot rather than flickering back to <c>null</c>.
/// </summary>
public sealed class PartialJsonReader<T>
{
    private static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly JsonSerializerOptions _options;
    private readonly StringBuilder _buffer = new();
    private T? _last;

    /// <summary>Creates the reader, optionally with custom serializer options.</summary>
    public PartialJsonReader(JsonSerializerOptions? options = null) => _options = options ?? Default;

    /// <summary>The raw accumulated JSON buffer.</summary>
    public string Buffer => _buffer.ToString();

    /// <summary>The most recent successfully-parsed snapshot (may be partial), or <c>null</c>.</summary>
    public T? Current => _last;

    /// <summary>Append a chunk and return the current best snapshot.</summary>
    public T? Append(string chunk)
    {
        _buffer.Append(chunk);
        try
        {
            var repaired = JsonPartialRepair.Close(_buffer.ToString());
            var snapshot = JsonSerializer.Deserialize<T>(repaired, _options);
            if (snapshot is not null) _last = snapshot;
        }
        catch (JsonException)
        {
            // hold last good
        }
        return _last;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~PartialJsonReaderTests`
Expected: PASS (all three).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Generation/PartialJsonReader.cs tests/DRYL.Components.Tests/Agents/PartialJsonReaderTests.cs
git commit -m "feat(agents): PartialJsonReader<T> — partial snapshots, hold-last-good"
```

---

### Task 10: `GenerationSnapshot<T>` + `DrylAiGenerate<T>` component

**Files:**
- Create: `DRYL.Components.Agents/Generation/GenerationSnapshot.cs`
- Create: `DRYL.Components.Agents/Generation/DrylAiGenerate.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAiGenerateTests.cs`

**Interfaces:**
- Consumes: `PartialJsonReader<T>` (Task 9); `IDrylAiActivityService` (core, optional).
- Produces:
  - `GenerationSnapshot<T>`: `T? Value`, `AiState State`, `bool IsComplete`.
  - `DrylAiGenerate<T>` component: `[Parameter] IAsyncEnumerable<string>? Source`, `[Parameter] string? Key`, `[Parameter] RenderFragment<GenerationSnapshot<T>>? ChildContent`, `[Parameter] AiState SettleTo = AiState.None`. Drives Thinking→Streaming→Generated automatically; pushes state to the activity service under `Key`; cancels/restarts cleanly when `Source` changes (mirrors `DrylAiStream`).

- [ ] **Step 1: Write the failing bUnit test**

```csharp
using System.Runtime.CompilerServices;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Agents.Generation;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests.Agents;

public class DrylAiGenerateTests : TestContext
{
    public sealed class Recipe { public string? Title { get; set; } }

    private static async IAsyncEnumerable<string> Stream(
        string[] chunks, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var c in chunks) { yield return c; await Task.Yield(); }
    }

    [Fact]
    public void Renders_partial_then_complete_snapshot()
    {
        var src = Stream(new[] { """{"title":"Pan""", """cakes"}""" });

        var cut = RenderComponent<DrylAiGenerate<Recipe>>(p => p
            .Add(x => x.Source, src)
            .Add(x => x.ChildContent, (RenderFragment<GenerationSnapshot<Recipe>>)(snap =>
                builder => builder.AddContent(0, snap.Value?.Title))));

        cut.WaitForAssertion(() => Assert.Contains("Pancakes", cut.Markup));
        cut.WaitForAssertion(() => Assert.True(cut.Instance is not null));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAiGenerateTests`
Expected: FAIL — component not defined.

- [ ] **Step 3: Implement `GenerationSnapshot<T>`**

```csharp
namespace DRYL.Components.Agents.Generation;

/// <summary>A partial snapshot of a structured generation, handed to <c>DrylAiGenerate</c>'s child content.</summary>
public sealed class GenerationSnapshot<T>
{
    /// <summary>The partial value parsed so far (fields not yet streamed are null/default).</summary>
    public T? Value { get; internal set; }

    /// <summary>The AI state of the generation (Thinking → Streaming → Generated).</summary>
    public AiState State { get; internal set; } = AiState.Thinking;

    /// <summary>True once the stream has completed.</summary>
    public bool IsComplete { get; internal set; }
}
```

- [ ] **Step 4: Implement `DrylAiGenerate<T>`** (mirrors `DrylAiStream` lifecycle)

```razor
@namespace DRYL.Components.Agents
@typeparam T
@using System.Threading
@using DRYL.Components.Agents.Generation
@using DRYL.Components.Ai
@using Microsoft.Extensions.DependencyInjection
@implements IDisposable

@*  DrylAiGenerate<T> — streams raw JSON tokens (Source) into progressively-richer
    partial snapshots of T. Parallel to DrylAiStream: Source-based, SDK-free, testable. *@

@if (ChildContent is not null)
{
    @ChildContent(_snapshot)
}

@code {
    /// <summary>Raw JSON token stream (e.g. from <c>DrylAgentRunner.GenerateStreamingAsync&lt;T&gt;</c>). Replacing it restarts.</summary>
    [Parameter] public IAsyncEnumerable<string>? Source { get; set; }

    /// <summary>Optional activity key; when set, a surrounding <c>DrylAiScope Key="..."</c> reacts.</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>Child content receiving the live <see cref="GenerationSnapshot{T}"/>.</summary>
    [Parameter] public RenderFragment<GenerationSnapshot<T>>? ChildContent { get; set; }

    /// <summary>State to settle to after the Generated reveal. Default <see cref="AiState.None"/>.</summary>
    [Parameter] public AiState SettleTo { get; set; } = AiState.None;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private const int SettleDelayMs = 1200;

    private readonly GenerationSnapshot<T> _snapshot = new();
    private IDrylAiActivityService? _service;
    private IAsyncEnumerable<string>? _current;
    private CancellationTokenSource? _cts;

    protected override void OnInitialized() =>
        _service = Services.GetService<IDrylAiActivityService>();

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Source, _current))
        {
            _current = Source;
            _ = RunAsync();
        }
    }

    private async Task RunAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var reader = new PartialJsonReader<T>();
        Apply(AiState.Thinking, default, complete: false);

        if (Source is null) { Apply(AiState.None, default, complete: false); return; }

        var sawToken = false;
        try
        {
            await foreach (var token in Source.WithCancellation(ct))
            {
                if (!sawToken) sawToken = true;
                var value = reader.Append(token);
                Apply(AiState.Streaming, value, complete: false);
            }
        }
        catch (OperationCanceledException) { return; }
        catch { Apply(AiState.None, _snapshot.Value, complete: false); return; }

        Apply(AiState.Generated, reader.Current, complete: true);

        try { await Task.Delay(SettleDelayMs, ct); }
        catch (OperationCanceledException) { return; }

        Apply(SettleTo, _snapshot.Value, complete: true);
    }

    private void Apply(AiState state, T? value, bool complete)
    {
        _snapshot.State = state;
        _snapshot.Value = value;
        _snapshot.IsComplete = complete;

        if (_service is not null && Key is not null)
        {
            if (state == AiState.None) _service.Clear(Key);
            else _service.Set(Key, state);
        }
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAiGenerateTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Generation/GenerationSnapshot.cs DRYL.Components.Agents/Generation/DrylAiGenerate.razor tests/DRYL.Components.Tests/Agents/DrylAiGenerateTests.cs
git commit -m "feat(agents): DrylAiGenerate<T> + GenerationSnapshot<T>"
```

---

### Task 11: `DrylAgentRunner.GenerateStreamingAsync<T>` — typed structured stream bridge

**Files:**
- Modify: `DRYL.Components.Agents/Agents/DrylAgentRunner.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerGenerateTests.cs`

**Interfaces:**
- Consumes: SDK `ChatClientAgentRunOptions`, `ChatOptions`, `ChatResponseFormat.ForJsonSchema<T>`, `AIAgent.RunStreamingAsync`.
- Produces: `IAsyncEnumerable<string> DrylAgentRunner.GenerateStreamingAsync<T>(AIAgent agent, AgentSession session, string prompt, string? aiKey = null, CancellationToken ct = default)` — sets the JSON-schema response format for `T` and yields the raw JSON text deltas, ready for `<DrylAiGenerate T Source="...">`. Also an internal `ExtractJsonDeltas(IAsyncEnumerable<AgentResponseUpdate>)` for testing the text-extraction (no schema/provider needed).

- [ ] **Step 1: Write the failing test** (covers the testable text-extraction seam)

```csharp
using System.Runtime.CompilerServices;
using DRYL.Components.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylAgentRunnerGenerateTests
{
    private static async IAsyncEnumerable<AgentResponseUpdate> Updates(
        string[] textChunks, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var c in textChunks)
        {
            yield return new AgentResponseUpdate(ChatRole.Assistant,
                new List<AIContent> { new TextContent(c) });
            await Task.Yield();
        }
    }

    [Fact]
    public async Task ExtractJsonDeltas_yields_only_text_content()
    {
        var deltas = new List<string>();
        await foreach (var d in DrylAgentRunner.ExtractJsonDeltas(
            Updates(new[] { "{\"a\":", "1}" })))
        {
            deltas.Add(d);
        }
        Assert.Equal(new[] { "{\"a\":", "1}" }, deltas);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentRunnerGenerateTests`
Expected: FAIL — `ExtractJsonDeltas` not defined.

- [ ] **Step 3: Implement** (append to `DrylAgentRunner.cs`)

```csharp
public sealed partial class DrylAgentRunner
{
    /// <summary>
    /// Stream a typed structured generation: instructs the model to emit JSON conforming to
    /// <typeparamref name="T"/>'s schema and yields the raw JSON text deltas — drop the result
    /// straight into <c>&lt;DrylAiGenerate T Source="..."&gt;</c>.
    /// </summary>
    public IAsyncEnumerable<string> GenerateStreamingAsync<T>(
        AIAgent agent, AgentSession session, string prompt,
        string? aiKey = null, CancellationToken ct = default)
    {
        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<T>(
                    JsonOpts,
                    schemaName: typeof(T).Name,
                    schemaDescription: $"A {typeof(T).Name} object."),
            }
        };
        var updates = agent.RunStreamingAsync(prompt, session, runOptions, ct);
        return ExtractJsonDeltas(updates, ct);
    }

    /// <summary>Yields the text content of each update — the raw JSON token stream.</summary>
    internal static async IAsyncEnumerable<string> ExtractJsonDeltas(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var update in updates.WithCancellation(ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                    yield return tc.Text;
            }
        }
    }
}
```

> `JsonOpts` is the field already defined in Task 5. `ChatResponseFormat.ForJsonSchema<T>(JsonSerializerOptions, schemaName, schemaDescription)` is the verified generic overload.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAgentRunnerGenerateTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylAgentRunner.cs tests/DRYL.Components.Tests/Agents/DrylAgentRunnerGenerateTests.cs
git commit -m "feat(agents): GenerateStreamingAsync<T> structured-stream bridge"
```

---

## Phase 4 — Subsystem 3: DrylDialog-backed tool functions

> Circuit dispatch note: `DrylDialogProvider` already marshals its renders via `InvokeAsync(StateHasChanged)` in its `OnAdded`/`OnClosed`/`OnUpdated` handlers (verified). So `IDrylDialogService.ShowAsync` is safe to call from the agent-run thread; the dialog renders and its result completes on the renderer thread. No extra dispatcher is needed in `DrylUiTools`. Cancellation is wired by registering the run's `CancellationToken` to cancel the open dialog reference.

### Task 12: `DrylAskTextDialog`

**Files:**
- Create: `DRYL.Components.Agents/Tools/DrylAskTextDialog.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAskTextDialogTests.cs`

**Interfaces:**
- Consumes: core `DrylDialog`, `DrylInputText`, `DrylButton`, `IDrylDialogInstance`, `DialogResult`.
- Produces: `DrylAskTextDialog` with `[Parameter] string Question`, `[Parameter] string? Placeholder`; closes with `DialogResult.Ok(string)` on submit, `Cancel()` on dismiss.

- [ ] **Step 1: Write the failing bUnit test**

```csharp
using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskTextDialogTests : TestContext
{
    private sealed class FakeInstance : IDrylDialogInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? Title => null;
        public DialogOptions Options { get; } = new();
        public AiState Ai => AiState.None;
        public DialogResult? Closed { get; private set; }
        public void Close(DialogResult result) => Closed = result;
        public void Cancel() => Closed = DialogResult.Cancel();
        public void SetAi(AiState state) { }
    }

    [Fact]
    public void Submit_returns_entered_text()
    {
        var instance = new FakeInstance();
        var cut = RenderComponent<DrylAskTextDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Your name?"));

        cut.Find("input").Change("Jan");
        cut.FindAll("button").Last().Click();   // submit button

        Assert.NotNull(instance.Closed);
        Assert.False(instance.Closed!.Canceled);
        Assert.Equal("Jan", instance.Closed.DataAs<string>());
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskTextDialogTests`
Expected: FAIL — component not defined.

- [ ] **Step 3: Implement** (wrap the input in an `EditForm` so `DrylInputText`/`InputBase<string>` binds outside a parent form)

```razor
@namespace DRYL.Components.Agents.Tools
@using DRYL.Components.Dialogs

<DrylDialog>
    <ChildContent>
        <p style="margin: 0 0 var(--sp-3); color: var(--fg-muted);">@Question</p>
        <EditForm Model="@this" OnValidSubmit="Submit">
            <DrylInputText @bind-Value="_text" Placeholder="@Placeholder" AriaLabel="@Question" />
        </EditForm>
    </ChildContent>
    <ActionContent>
        <DrylButton Variant="DrylButton.ButtonVariant.Ghost" @onclick="Cancel">Cancel</DrylButton>
        <DrylButton Variant="DrylButton.ButtonVariant.Primary" @onclick="Submit">Submit</DrylButton>
    </ActionContent>
</DrylDialog>

@code {
    /// <summary>The question shown above the text field.</summary>
    [Parameter] public string Question { get; set; } = string.Empty;

    /// <summary>Optional placeholder for the text field.</summary>
    [Parameter] public string? Placeholder { get; set; }

    [CascadingParameter] public IDrylDialogInstance Instance { get; set; } = default!;

    private string _text = string.Empty;

    private void Submit() => Instance.Close(DialogResult.Ok(_text));
    private void Cancel() => Instance.Cancel();
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskTextDialogTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylAskTextDialog.razor tests/DRYL.Components.Tests/Agents/DrylAskTextDialogTests.cs
git commit -m "feat(agents): DrylAskTextDialog"
```

---

### Task 13: `DrylAskChoiceDialog` (single choice, recommended badge)

**Files:**
- Create: `DRYL.Components.Agents/Tools/DrylAskChoiceDialog.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAskChoiceDialogTests.cs`

**Interfaces:**
- Consumes: core `DrylDialog`, `DrylRadioGroup<string>`, `DrylRadio<string>`, `DrylBadge`, `DrylButton`, `IDrylDialogInstance`.
- Produces: `DrylAskChoiceDialog` with `[Parameter] string Question`, `[Parameter] IReadOnlyList<string> Options`, `[Parameter] string? Recommended`; closes with `DialogResult.Ok(string)` (the chosen option), defaulting the selection to `Recommended` (or the first option).

- [ ] **Step 1: Write the failing bUnit test**

```csharp
using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskChoiceDialogTests : TestContext
{
    private sealed class FakeInstance : IDrylDialogInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? Title => null;
        public DialogOptions Options { get; } = new();
        public AiState Ai => AiState.None;
        public DialogResult? Closed { get; private set; }
        public void Close(DialogResult result) => Closed = result;
        public void Cancel() => Closed = DialogResult.Cancel();
        public void SetAi(AiState state) { }
    }

    [Fact]
    public void Defaults_to_recommended_and_returns_it_on_confirm()
    {
        var instance = new FakeInstance();
        var cut = RenderComponent<DrylAskChoiceDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Pick one")
            .Add(x => x.Options, new[] { "A", "B", "C" })
            .Add(x => x.Recommended, "B"));

        cut.FindAll("button").Last().Click();   // confirm

        Assert.Equal("B", instance.Closed!.DataAs<string>());
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskChoiceDialogTests`
Expected: FAIL — component not defined.

- [ ] **Step 3: Implement**

```razor
@namespace DRYL.Components.Agents.Tools
@using DRYL.Components.Dialogs

<DrylDialog>
    <ChildContent>
        <p style="margin: 0 0 var(--sp-3); color: var(--fg-muted);">@Question</p>
        <DrylRadioGroup TValue="string" @bind-Value="_selected">
            @foreach (var option in Options)
            {
                <div class="row" style="align-items: center; gap: var(--sp-2);">
                    <DrylRadio TValue="string" Value="@option" Label="@option" />
                    @if (IsRecommended(option))
                    {
                        <DrylBadge Variant="DrylBadge.BadgeVariant.Accent">Recommended</DrylBadge>
                    }
                </div>
            }
        </DrylRadioGroup>
    </ChildContent>
    <ActionContent>
        <DrylButton Variant="DrylButton.ButtonVariant.Ghost" @onclick="Cancel">Cancel</DrylButton>
        <DrylButton Variant="DrylButton.ButtonVariant.Primary" @onclick="Confirm">Confirm</DrylButton>
    </ActionContent>
</DrylDialog>

@code {
    /// <summary>The question shown above the options.</summary>
    [Parameter] public string Question { get; set; } = string.Empty;

    /// <summary>The selectable options.</summary>
    [Parameter] public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();

    /// <summary>The recommended option, marked with a badge and selected by default.</summary>
    [Parameter] public string? Recommended { get; set; }

    [CascadingParameter] public IDrylDialogInstance Instance { get; set; } = default!;

    private string? _selected;

    protected override void OnInitialized() =>
        _selected = Recommended ?? (Options.Count > 0 ? Options[0] : null);

    private bool IsRecommended(string option) =>
        Recommended is not null && string.Equals(option, Recommended, StringComparison.OrdinalIgnoreCase);

    private void Confirm() => Instance.Close(DialogResult.Ok(_selected ?? string.Empty));
    private void Cancel() => Instance.Cancel();
}
```

> Verify the `DrylBadge` variant enum member name against `DRYL.Components/Components/.../DrylBadge.razor` before finalizing; if `Accent` is not a member, use the closest accent-glow variant (e.g. `Info`). Adjust the markup accordingly.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskChoiceDialogTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylAskChoiceDialog.razor tests/DRYL.Components.Tests/Agents/DrylAskChoiceDialogTests.cs
git commit -m "feat(agents): DrylAskChoiceDialog"
```

---

### Task 14: `DrylAskMultiChoiceDialog` (multi choice, recommendations pre-checked)

**Files:**
- Create: `DRYL.Components.Agents/Tools/DrylAskMultiChoiceDialog.razor`
- Test: `tests/DRYL.Components.Tests/Agents/DrylAskMultiChoiceDialogTests.cs`

**Interfaces:**
- Consumes: core `DrylDialog`, `DrylCheckbox`, `DrylBadge`, `DrylButton`, `IDrylDialogInstance`.
- Produces: `DrylAskMultiChoiceDialog` with `[Parameter] string Question`, `[Parameter] IReadOnlyList<string> Options`, `[Parameter] IReadOnlyList<string>? Recommended`; closes with `DialogResult.Ok(string[])` (the checked options), pre-checking the recommended set.

- [ ] **Step 1: Write the failing bUnit test**

```csharp
using Bunit;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;

namespace DRYL.Components.Tests.Agents;

public class DrylAskMultiChoiceDialogTests : TestContext
{
    private sealed class FakeInstance : IDrylDialogInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? Title => null;
        public DialogOptions Options { get; } = new();
        public AiState Ai => AiState.None;
        public DialogResult? Closed { get; private set; }
        public void Close(DialogResult result) => Closed = result;
        public void Cancel() => Closed = DialogResult.Cancel();
        public void SetAi(AiState state) { }
    }

    [Fact]
    public void Returns_prechecked_recommendations_on_confirm()
    {
        var instance = new FakeInstance();
        var cut = RenderComponent<DrylAskMultiChoiceDialog>(p => p
            .AddCascadingValue<IDrylDialogInstance>(instance)
            .Add(x => x.Question, "Pick several")
            .Add(x => x.Options, new[] { "A", "B", "C" })
            .Add(x => x.Recommended, new[] { "A", "C" }));

        cut.FindAll("button").Last().Click();   // confirm

        var result = instance.Closed!.DataAs<string[]>()!;
        Assert.Equal(new[] { "A", "C" }, result);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskMultiChoiceDialogTests`
Expected: FAIL — component not defined.

- [ ] **Step 3: Implement**

```razor
@namespace DRYL.Components.Agents.Tools
@using DRYL.Components.Dialogs

<DrylDialog>
    <ChildContent>
        <p style="margin: 0 0 var(--sp-3); color: var(--fg-muted);">@Question</p>
        @foreach (var option in Options)
        {
            <div class="row" style="align-items: center; gap: var(--sp-2);">
                <DrylCheckbox @bind-Value="_checked[option]" Label="@option" />
                @if (IsRecommended(option))
                {
                    <DrylBadge Variant="DrylBadge.BadgeVariant.Accent">Recommended</DrylBadge>
                }
            </div>
        }
    </ChildContent>
    <ActionContent>
        <DrylButton Variant="DrylButton.ButtonVariant.Ghost" @onclick="Cancel">Cancel</DrylButton>
        <DrylButton Variant="DrylButton.ButtonVariant.Primary" @onclick="Confirm">Confirm</DrylButton>
    </ActionContent>
</DrylDialog>

@code {
    /// <summary>The question shown above the options.</summary>
    [Parameter] public string Question { get; set; } = string.Empty;

    /// <summary>The selectable options.</summary>
    [Parameter] public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();

    /// <summary>Options recommended by the model — pre-checked and badged.</summary>
    [Parameter] public IReadOnlyList<string>? Recommended { get; set; }

    [CascadingParameter] public IDrylDialogInstance Instance { get; set; } = default!;

    private readonly Dictionary<string, bool> _checked = new();

    protected override void OnInitialized()
    {
        foreach (var option in Options)
            _checked[option] = IsRecommended(option);
    }

    private bool IsRecommended(string option) =>
        Recommended is not null &&
        Recommended.Any(r => string.Equals(r, option, StringComparison.OrdinalIgnoreCase));

    private void Confirm() =>
        Instance.Close(DialogResult.Ok(
            Options.Where(o => _checked.TryGetValue(o, out var v) && v).ToArray()));

    private void Cancel() => Instance.Cancel();
}
```

> `DrylCheckbox @bind-Value="_checked[option]"` binds to a dictionary indexer — valid in Razor. As in Task 13, confirm the `DrylBadge` variant member name and adjust if needed.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylAskMultiChoiceDialogTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylAskMultiChoiceDialog.razor tests/DRYL.Components.Tests/Agents/DrylAskMultiChoiceDialogTests.cs
git commit -m "feat(agents): DrylAskMultiChoiceDialog"
```

---

### Task 15: `DrylUiTools` factory — the four tool functions

**Files:**
- Create: `DRYL.Components.Agents/Tools/DrylUiTools.cs`
- Test: `tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs`

**Interfaces:**
- Consumes: core `IDrylDialogService` (its `ShowAsync<TDialog>` + `ShowConfirmAsync`), `DialogParameters`, `DialogResult`; the three dialogs (Tasks 12–14) + core `DrylConfirmDialog`.
- Produces:
  - `DrylUiTools DrylUiTools.Create(IDrylDialogService dialogs)`.
  - Properties `AITool AskChoice`, `AskMultiChoice`, `RequestPermission`, `AskText`, and `IList<AITool> All`.
  - Each tool calls the dialog service, awaits the result, and returns a model-friendly value; on Cancel returns a defined "user declined/cancelled" sentinel (never throws).

- [ ] **Step 1: Write the failing test** (fake dialog service drives Confirm/Cancel; invoke the `AIFunction` directly)

```csharp
using System.Text.Json;
using DRYL.Components;
using DRYL.Components.Agents.Tools;
using DRYL.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylUiToolsTests
{
    // Fake service: completes every dialog with a preset result.
    private sealed class FakeDialogService : IDrylDialogService
    {
        private readonly DialogResult _result;
        public FakeDialogService(DialogResult result) => _result = result;

        public Task<IDrylDialogReference> ShowAsync<TDialog>(
            string? title = null, DialogParameters? parameters = null, DialogOptions? options = null)
            where TDialog : IComponent =>
            Task.FromResult<IDrylDialogReference>(new FakeRef(_result));

        public Task<DialogResult> ShowConfirmAsync(string title, string message,
            string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null) =>
            Task.FromResult(_result);

        public Task<DialogResult> ShowAlertAsync(string title, string message,
            string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());

        public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
        public event Action<IDrylDialogReference>? OnDialogCloseRequested;
        public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;

        private sealed class FakeRef : IDrylDialogReference
        {
            private readonly DialogResult _r;
            public FakeRef(DialogResult r) => _r = r;
            public Guid Id { get; } = Guid.NewGuid();
            public Task<DialogResult> Result => Task.FromResult(_r);
            public void Close(DialogResult result) { }
            public void Cancel() { }
        }
    }

    private static async Task<string?> InvokeAsync(AITool tool, Dictionary<string, object?> args)
    {
        var fn = (AIFunction)tool;
        var result = await fn.InvokeAsync(new AIFunctionArguments(args));
        return result?.ToString();
    }

    [Fact]
    public async Task AskText_returns_entered_text()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok("Jan")));
        var answer = await InvokeAsync(tools.AskText,
            new() { ["question"] = "Name?", ["placeholder"] = null });
        Assert.Contains("Jan", answer);
    }

    [Fact]
    public async Task RequestPermission_returns_true_on_confirm()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok(true)));
        var answer = await InvokeAsync(tools.RequestPermission,
            new() { ["action"] = "Delete file", ["details"] = null });
        Assert.Contains("true", answer!.ToLowerInvariant());
    }

    [Fact]
    public async Task AskText_returns_declined_on_cancel()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Cancel()));
        var answer = await InvokeAsync(tools.AskText,
            new() { ["question"] = "Name?", ["placeholder"] = null });
        Assert.Contains("cancel", answer!.ToLowerInvariant());
    }

    [Fact]
    public void All_contains_four_tools()
    {
        var tools = DrylUiTools.Create(new FakeDialogService(DialogResult.Ok()));
        Assert.Equal(4, tools.All.Count);
    }
}
```

> Confirm `AIFunction.InvokeAsync` / `AIFunctionArguments` shapes against Extensions.AI 10.6.0 when implementing; if the invoke signature differs, adapt the test helper (the public behavior — return strings, never throw — is what matters).

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylUiToolsTests`
Expected: FAIL — `DrylUiTools` not defined.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using DRYL.Components.Dialogs;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents.Tools;

/// <summary>
/// Ready-made human-in-the-loop tool functions for the Microsoft Agent Framework, backed by
/// DRYL dialogs. Create one per circuit with <see cref="Create"/> (it captures the scoped
/// <see cref="IDrylDialogService"/>), then hand <see cref="All"/> — or individual tools — to
/// your agent. Requires the agent run to execute in the same circuit as the UI.
/// </summary>
public sealed class DrylUiTools
{
    private readonly IDrylDialogService _dialogs;

    private DrylUiTools(IDrylDialogService dialogs)
    {
        _dialogs = dialogs;
        AskChoice = AIFunctionFactory.Create(AskChoiceImpl, "ask_choice",
            "Ask the user to pick exactly one option from a list. Use for either/or decisions.");
        AskMultiChoice = AIFunctionFactory.Create(AskMultiChoiceImpl, "ask_multi_choice",
            "Ask the user to pick one or more options from a list.");
        RequestPermission = AIFunctionFactory.Create(RequestPermissionImpl, "request_permission",
            "Ask the user to allow or deny an action before performing it.");
        AskText = AIFunctionFactory.Create(AskTextImpl, "ask_text",
            "Ask the user a free-text question and get their typed answer.");
        All = new List<AITool> { AskChoice, AskMultiChoice, RequestPermission, AskText };
    }

    /// <summary>Create the tool set, capturing the circuit's dialog service.</summary>
    public static DrylUiTools Create(IDrylDialogService dialogs) => new(dialogs);

    /// <summary>Single-choice question tool (backed by <c>DrylAskChoiceDialog</c>).</summary>
    public AITool AskChoice { get; }

    /// <summary>Multi-choice question tool (backed by <c>DrylAskMultiChoiceDialog</c>).</summary>
    public AITool AskMultiChoice { get; }

    /// <summary>Permission tool (backed by the core <c>DrylConfirmDialog</c>).</summary>
    public AITool RequestPermission { get; }

    /// <summary>Free-text question tool (backed by <c>DrylAskTextDialog</c>).</summary>
    public AITool AskText { get; }

    /// <summary>All four tools — hand straight to the agent.</summary>
    public IList<AITool> All { get; }

    private async Task<string> AskChoiceImpl(
        [Description("The question to ask the user.")] string question,
        [Description("The options the user can choose from.")] string[] options,
        [Description("The option you recommend (must match one of options), or null.")] string? recommended = null)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Options"] = options,
            ["Recommended"] = recommended,
        };
        var reference = await _dialogs.ShowAsync<DrylAskChoiceDialog>("Choose", p);
        var result = await reference.Result;
        return result.Canceled
            ? "The user cancelled the question."
            : result.DataAs<string>() ?? "(no selection)";
    }

    private async Task<string> AskMultiChoiceImpl(
        [Description("The question to ask the user.")] string question,
        [Description("The options the user can choose from.")] string[] options,
        [Description("The options you recommend (subset of options), or null.")] string[]? recommended = null)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Options"] = options,
            ["Recommended"] = recommended,
        };
        var reference = await _dialogs.ShowAsync<DrylAskMultiChoiceDialog>("Choose", p);
        var result = await reference.Result;
        if (result.Canceled) return "The user cancelled the question.";
        var chosen = result.DataAs<string[]>() ?? Array.Empty<string>();
        return chosen.Length == 0 ? "(no selection)" : string.Join(", ", chosen);
    }

    private async Task<string> RequestPermissionImpl(
        [Description("The action you want permission to perform.")] string action,
        [Description("Optional extra detail shown to the user.")] string? details = null)
    {
        var message = string.IsNullOrWhiteSpace(details) ? action : $"{action}\n\n{details}";
        var result = await _dialogs.ShowConfirmAsync("Permission required", message, "Allow", "Deny");
        return result.Canceled ? "false (the user denied permission)" : "true (the user allowed it)";
    }

    private async Task<string> AskTextImpl(
        [Description("The question to ask the user.")] string question,
        [Description("Optional placeholder for the input field.")] string? placeholder = null)
    {
        var p = new DialogParameters
        {
            ["Question"] = question,
            ["Placeholder"] = placeholder,
        };
        var reference = await _dialogs.ShowAsync<DrylAskTextDialog>("Question", p);
        var result = await reference.Result;
        return result.Canceled
            ? "The user cancelled the question."
            : result.DataAs<string>() ?? string.Empty;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylUiToolsTests`
Expected: PASS (all four).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylUiTools.cs tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs
git commit -m "feat(agents): DrylUiTools — four DrylDialog-backed tool functions"
```

---

### Task 16: Cancellation wiring for tool dialogs

**Files:**
- Modify: `DRYL.Components.Agents/Tools/DrylUiTools.cs`
- Modify: `tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs`

**Interfaces:**
- Produces: each tool impl accepts a trailing `CancellationToken` (supplied by the framework). When cancelled while a dialog is open, the dialog reference is cancelled and the tool returns a "cancelled" string.

- [ ] **Step 1: Add a failing test**

```csharp
    [Fact]
    public async Task AskText_returns_cancelled_when_token_cancelled()
    {
        // A dialog whose Result never completes until cancelled.
        var tools = DrylUiTools.Create(new NeverCompletingDialogService());
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        var fn = (AIFunction)tools.AskText;
        var result = await fn.InvokeAsync(
            new AIFunctionArguments(new Dictionary<string, object?> { ["question"] = "Name?" }),
            cts.Token);

        Assert.Contains("cancel", result!.ToString()!.ToLowerInvariant());
    }

    private sealed class NeverCompletingDialogService : IDrylDialogService
    {
        public Task<IDrylDialogReference> ShowAsync<TDialog>(
            string? title = null, DialogParameters? parameters = null, DialogOptions? options = null)
            where TDialog : IComponent =>
            Task.FromResult<IDrylDialogReference>(new PendingRef());

        public Task<DialogResult> ShowConfirmAsync(string title, string message,
            string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null)
            => new PendingRef().Result;
        public Task<DialogResult> ShowAlertAsync(string title, string message,
            string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());
        public event Action<IDrylDialogReference>? OnDialogInstanceAdded;
        public event Action<IDrylDialogReference>? OnDialogCloseRequested;
        public event Action<IDrylDialogReference>? OnDialogInstanceUpdated;

        private sealed class PendingRef : IDrylDialogReference
        {
            private readonly TaskCompletionSource<DialogResult> _tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Guid Id { get; } = Guid.NewGuid();
            public Task<DialogResult> Result => _tcs.Task;
            public void Close(DialogResult result) => _tcs.TrySetResult(result);
            public void Cancel() => _tcs.TrySetResult(DialogResult.Cancel());
        }
    }
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylUiToolsTests`
Expected: FAIL — current impls ignore the token; the await hangs / test times out or compile fails (no token param).

- [ ] **Step 3: Add a cancellation-aware await helper and thread `CancellationToken` through every impl**

Add the helper and update each `Impl` signature to take `CancellationToken ct = default` as the last parameter, replacing `await reference.Result` with `await Await(reference, ct)`:

```csharp
    // Awaits the dialog result; if the run is cancelled first, cancels the dialog and reports it.
    private static async Task<DialogResult> Await(IDrylDialogReference reference, CancellationToken ct)
    {
        await using var _ = ct.Register(reference.Cancel).ConfigureAwait(false);
        return await reference.Result.ConfigureAwait(false);
    }
```

Example for `AskTextImpl` (apply the same pattern to the other three; `RequestPermissionImpl` has no reference, so guard it instead — see note):

```csharp
    private async Task<string> AskTextImpl(
        [Description("The question to ask the user.")] string question,
        [Description("Optional placeholder for the input field.")] string? placeholder = null,
        CancellationToken ct = default)
    {
        var p = new DialogParameters { ["Question"] = question, ["Placeholder"] = placeholder };
        var reference = await _dialogs.ShowAsync<DrylAskTextDialog>("Question", p);
        var result = await Await(reference, ct);
        return result.Canceled ? "The user cancelled the question." : result.DataAs<string>() ?? string.Empty;
    }
```

> For `RequestPermissionImpl`, which uses `ShowConfirmAsync` (no reference handle), wrap the call so cancellation resolves to "false": `var result = await ShowConfirmCancellable(...);` using `Task.WhenAny(showTask, ct-as-task)`. Simplest correct form:
> ```csharp
> var showTask = _dialogs.ShowConfirmAsync("Permission required", message, "Allow", "Deny");
> var tcs = new TaskCompletionSource<DialogResult>();
> await using (ct.Register(() => tcs.TrySetResult(DialogResult.Cancel())))
>     result = await await Task.WhenAny(showTask, tcs.Task);
> ```

`ct.Register(...)` returns `CancellationTokenRegistration`, which is `IAsyncDisposable` on net8+. Verify and, if targeting a TFM without async dispose on the registration, use `using` (sync) instead.

- [ ] **Step 4: Run to verify the whole tool suite passes**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~DrylUiToolsTests`
Expected: PASS (all six).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Tools/DrylUiTools.cs tests/DRYL.Components.Tests/Agents/DrylUiToolsTests.cs
git commit -m "feat(agents): cancel open tool dialogs when the run is cancelled"
```

---

## Phase 5 — Documentation & finalisation

### Task 17: PACKAGE.md, CHANGELOG, README

**Files:**
- Modify: `DRYL.Components.Agents/PACKAGE.md` (full content)
- Modify: `CHANGELOG.md`
- Modify: `README.md`

**Interfaces:**
- Produces: complete docs per CLAUDE.md §7.

- [ ] **Step 1: Write the full `PACKAGE.md`** covering: what the package is (3 subsystems), install (`dotnet add package DRYL.Components.Agents`), `AddDrylComponents().AddDrylAgents()`, the three usage snippets from the spec (chat with auto-AiState; `DrylAiGenerate<T>` recipe card; `DrylUiTools.Create(...)` HITL), the platform constraint (live circuit; run in the circuit scope), and the experimental-0.1.0 / independent-version note.

- [ ] **Step 2: Add to `CHANGELOG.md`** under `## [Unreleased]` a new `### Added` block — one bullet per public type:

```markdown
### Added
- `DRYL.Components.Agents` — New companion package integrating the Microsoft Agent Framework (`Microsoft.Agents.AI`). Experimental, independently versioned (0.1.0), decoupled from core. The core stays dependency-free
- `AddDrylAgents()` — DI extension registering `DrylAgentRunner` (scoped); call alongside `AddDrylComponents()`
- `DrylAgentRunner` — Starts agent runs and bridges them to DRYL's AI vocabulary; `Start(...)` returns an observable run, `GenerateStreamingAsync<T>(...)` streams typed structured output
- `DrylAgentRun` — Observable run handle (`State`, `Text`, `ToolCalls`, `TextStream`, `OnChange`); drives `AiState` automatically and feeds `DrylAiScope`
- `DrylToolInvocation` — Captured tool/function call with lifecycle-derived `AiState`; maps 1:1 onto the core `DrylToolCall`
- `DrylAgentToolCalls` — Renders an agent run's tool calls via the core `DrylToolCall` (full trace, or `ActiveOnly`)
- `PartialJsonReader<T>` / `JsonPartialRepair` — Tolerant partial-JSON snapshot engine for structured streaming (hold-last-good on parse failure)
- `DrylAiGenerate<T>` / `GenerationSnapshot<T>` — Streams a typed object from raw JSON tokens and renders progressive partial snapshots; mirrors `DrylAiStream`
- `DrylUiTools` — Factory for four human-in-the-loop `AIFunction` tools (`AskChoice`, `AskMultiChoice`, `RequestPermission`, `AskText`) backed by DRYL dialogs, plus an `All` collection
- `DrylAskChoiceDialog` / `DrylAskMultiChoiceDialog` / `DrylAskTextDialog` — Agent-question dialogs (Agents package) composed from core components; `RequestPermission` reuses the core `DrylConfirmDialog`
```

- [ ] **Step 3: Add rows to the `README.md` component table** (the "What's in the box" section) for `DrylAgentToolCalls`, `DrylAiGenerate`, `DrylAskChoiceDialog`, `DrylAskMultiChoiceDialog`, `DrylAskTextDialog`, each Notes column noting **(Agents package)**. Match the existing column layout exactly; do not reformat other rows. Example:

```markdown
| `DrylAiGenerate`  | Intelligence | ✅      | ✅ Done    | (Agents package) Streams a typed object → progressive partial-snapshot UI |
```

- [ ] **Step 4: Build, run the full test suite, and pack both packages**

Run: `dotnet build DRYL.slnx -c Release` then `dotnet test DRYL.slnx -c Release` then `dotnet pack DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release -o artifacts`
Expected: build + all tests green; `DRYL.Components.Agents.0.1.0.nupkg` produced.

- [ ] **Step 5: Commit**

```bash
git add CHANGELOG.md README.md DRYL.Components.Agents/PACKAGE.md
git commit -m "docs(agents): PACKAGE.md, CHANGELOG and README for the Agents package"
```

---

### Task 18 (optional): Sample pages in DRYL.Website

> The spec lists sample pages under "Documentation". This repo has no `samples/` project; demos live in the separate `DRYL.Website` working directory. Adding website pages depends on that project referencing the new package. Track as a follow-up: a chat page (auto-AiState + `DrylAgentToolCalls`), a generative recipe-card page (`DrylAiGenerate<Recipe>`), and a HITL tool-demo page. Not required for the package to ship.

---

## Self-Review

**Spec coverage:**
- Subsystem 1 (run → AiState + tool calls): Tasks 4–6, 11 ✓
- Subsystem 2 (`DrylAiGenerate<T>` + `PartialJsonReader`): Tasks 7–10 ✓ (TDD, corpus tests, hold-last-good)
- Subsystem 3 (four tools + three dialogs, circuit dispatch/cancel): Tasks 12–16 ✓
- Separate package, multi-target, version 0.1.0, `AddDrylAgents()`: Tasks 1–2 ✓
- CI wiring: Task 3 ✓
- Reuse core `DrylToolCall`/`DrylConfirmDialog`/dialog pipeline: Tasks 6, 13–15 ✓
- Platform constraint documented: Phase 4 note + Task 17 PACKAGE.md ✓
- Docs (CHANGELOG/README/PACKAGE.md): Task 17 ✓
- Sample pages: Task 18 (deferred, documented) ✓

**Type consistency:** `DrylToolInvocation` (CallId/ToolName/Arguments/Result/Error/State) used consistently in Tasks 4–6. `DrylAgentRun` members (State/Text/ToolCalls/OnChange/TextStream + internal AddToolCall/Raise/PushText/CompleteText/MarkCompleted/WaitForCompletionAsync) consistent across Tasks 5–6. `JsonOpts` defined in Task 5, reused in Task 11. `GenerationSnapshot<T>` (Value/State/IsComplete) consistent Tasks 10. `DrylUiTools` (AskChoice/AskMultiChoice/RequestPermission/AskText/All + Create) consistent Tasks 15–16.

**Risk flags carried into execution (verify, don't assume):**
1. `DrylBadge` variant enum member name (`Accent` vs `Info`) — Tasks 13–14.
2. `AIFunction.InvokeAsync` / `AIFunctionArguments` exact shape in Extensions.AI 10.6.0 — Task 15 test helper.
3. `CancellationTokenRegistration` async-dispose availability per TFM — Task 16.
4. `ChatResponseFormat.ForJsonSchema<T>` parameter names (`schemaName`/`schemaDescription`) — Task 11 (verified overload exists; confirm arg names or use positional).
