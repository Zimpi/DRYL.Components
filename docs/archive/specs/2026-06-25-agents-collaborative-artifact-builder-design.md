# DRYL.Components.Agents — Collaborative Artifact Builder

**Date:** 2026-06-25
**Status:** Approved design, ready for implementation plan
**Package:** `DRYL.Components.Agents` (experimental, v0.1.0, `[Unreleased]`)

## Problem

The framework's structured-generation primitive `GenerateStreamingAsync<T>` forces
`ResponseFormat = json_schema(T)`. That constraint tells the model its output must be exactly
one `T` JSON object, leaving no channel for an ongoing dialogue. The model therefore asks at
most one clarifying question, then dumps the whole artifact in a single shot — it cannot work
*iteratively*.

The free-chat path (`Start`) already iterates over tool calls (the Microsoft Agent Framework's
`FunctionInvokingChatClient` runs a real multi-round loop). The thing that *kills* iteration is
enforced structured output. To get **both** iteration and a structured artifact, the artifact
must be built via **tool calls**, not via `response_format`.

## Goal

Make collaborative, iterative artifact-building a first-class, developer-accessible capability
of `DRYL.Components.Agents`. A developer hands an agent (with their human-in-the-loop tools), a
prompt, and a target type `T`; the framework drives an iterative loop where the model alternates
*ask the user → think → refine `T` → ask → …* and a UI shows the artifact growing — without the
developer writing any orchestration or iteration prompting.

Out of scope (YAGNI for this spec): generic non-artifact iteration loops, self-critique/reviewer
passes, array-append merge semantics, multi-artifact builds.

## Chosen approach (A): Tool-injected artifact builder

The loop is **not** hand-built — it is the Microsoft `FunctionInvokingChatClient` loop. The
framework's contribution is the **artifact substrate**: it auto-generates an `update_<T>` merge
tool, supplies a default iteration system prompt, observes the run, and exposes the live merged
artifact. Orchestration stays with the model (where the MS loop is strong).

Rejected: (B) framework-driven multi-pass orchestration — complex, brittle, HITL-inside-a
`response_format`-pass is awkward, reinvents the MS loop. (C) pure prompt convention — no growing
artifact, no merge, fragile final-JSON parsing.

## Developer API

```csharp
public DrylArtifactRun<T> StartBuild<T>(
    AIAgent agent, AgentSession session, string prompt,
    DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default);
```

```csharp
public sealed class DrylBuildOptions
{
    public int? MaxRounds { get; init; } = 12;   // safety cap; null = unbounded
    public string? Guidance { get; init; }        // overrides the framework iteration prompt
    public string? UpdateToolName { get; init; }  // default: "update_<T-name>"
}
```

`DrylArtifactRun<T>` — observable handle, same surface as `DrylAgentRun` plus:

```csharp
public T?  Artifact { get; }   // the live merged artifact
public int Round    { get; }   // count of applied update_<T> steps
// shared with DrylAgentRun: State, Text, ToolCalls, TextStream, OnChange, WaitForCompletionAsync()
```

`DrylAiBuild<T>` — UI sugar parallel to `DrylAiGenerate<T>` (see UI section).

Developer-side usage (the entire demo wiring):

```razor
<DrylAiScope Key="build">
  <DrylCard Ai="@_build.State">
    <DrylAgentToolCalls Run="@_build" />
    <DrylAiBuild T="Recipe" Run="@_build" Key="build">
      <ChildContent Context="a">
        <h3>@(a.Artifact?.Title ?? "…")</h3>
        @if (a.Artifact?.Steps is { Count: > 0 })
        { <ol>@foreach (var s in a.Artifact.Steps) { <li>@s</li> }</ol> }
      </ChildContent>
    </DrylAiBuild>
  </DrylCard>
</DrylAiScope>

@code {
    _build = Runner.StartBuild<Recipe>(_agent!, session,
        "Brainstorme mit mir ein Rezept. Vorgabe: Pasta.", aiKey: "build");
}
```

## The `update_<T>` tool

`StartBuild<T>` generates one extra tool at run time and passes it via run options so the agent's
own HITL tools are untouched:

```csharp
var runOptions = new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions { Tools = { updateTool } }   // additive to agent tools
};
```

**Verification point (plan phase):** confirm run-level `ChatOptions.Tools` are *merged* with the
agent's construction-time tools, not replaced. If replaced, `StartBuild` must also accept the
developer's HITL tools and compose the full set itself.

Tool shape:
- Name `update_<T-name>` (e.g. `update_recipe`). Description carries usage guidance:
  *"Record or refine the artifact as you learn more. Call this repeatedly — include only the
  fields you want to set or change."*
- Parameter mirrors `T`'s schema **with every field optional**, so the model knows the shape
  (`title`, `description`, `steps`) but may send partial updates. No `response_format`, no strict
  schema trap.
- Implementation (closure over the run): deep-merge the patch → `Round++` → raise `OnChange`.
  Returns a short receipt to the model, e.g. *"Updated. Current: title=set, steps=3."*, so the
  model can see its own progress.

## Merge semantics

Deep-merge runs on the accumulated JSON via `System.Text.Json.Nodes.JsonNode`; `Artifact` is the
value deserialized from that JSON (JSON is the source of truth, `Artifact` is the typed view).

| Patch contains …      | Behavior                              |
| --------------------- | ------------------------------------- |
| scalar field (`title`)| overwrites the existing value         |
| nested object         | recursively merged                    |
| array / list (`steps`)| **replaces** the whole array          |
| field absent / `null` | existing value left untouched         |

Arrays are replaced, not appended (predictable; append is YAGNI).

## Iteration loop, guidance & safety cap

The loop is the MS `FunctionInvokingChatClient`. `StartBuild<T>` calls
`agent.RunStreamingAsync(prompt, session, runOptions, ct)` and the model alternates HITL question
→ `update_<T>` → question → … → final text. The framework only observes and provides substrate.

**Framework-owned iteration prompt** (the core of "accessible to developers"): the framework
injects, via run options (additional to the agent's instructions), a default guidance text:

> "Build the result collaboratively and iteratively. Don't gather everything up front, then dump
> a result. Instead: ask the user one focused question via your question tools, record progress
> with `update_<T>`, then ask the next — alternating question → refine → question. Use
> `update_<T>` many times as the picture sharpens. When the user is satisfied and the artifact is
> complete, give a brief final confirmation and stop."

Overridable via `DrylBuildOptions.Guidance`.

**MaxRounds safety cap** — `Round` counts applied `update_<T>` steps:
- When `Round >= MaxRounds`, the `update_<T>` tool returns, instead of the normal receipt:
  *"Maximum refinement rounds reached — stop refining and give your final answer now."* — a soft
  nudge; the model wraps up.
- Default `MaxRounds = 12`; `null` = unbounded. This is a runaway guard, not the normal exit. The
  normal exit is the loop's natural end (model stops calling tools, emits final text).

## State mapping (reuses the `AiState` vocabulary; nothing new invented)

- HITL tool open / `update_<T>` running → `Thinking`
- final text streaming → `Streaming`
- run finished → `Generated`, then settle to `None`

**Scope ownership** (consistent with the existing split): the **runner** drives the scope during
the run (`Thinking`/`Streaming`/`Generated`) via `aiKey`, exactly like `Start`. The **component**
does only the final settle (`Generated` → after dwell → `None`) via `Key`, exactly like
`DrylAiStream`/`DrylAiGenerate` today. No double-driving.

## UI component `DrylAiBuild<T>`

In `Generation/`, parallel to `DrylAiGenerate.razor`. Deliberately thin, because the artifact
grows via **discrete** tool merges (no token stream → no `PartialJsonReader`).

- Parameters: `Run` (`DrylArtifactRun<T>`), `Key`, `ChildContent` (receives a snapshot exposing
  `Artifact`, `State`, `Round`, `IsComplete`), `SettleTo`.
- Subscribes to `Run.OnChange` → `InvokeAsync(StateHasChanged)`; `IDisposable` unsubscribes
  cleanly (guard for prerender per the package's existing pattern).
- Does only the final settle to `None` after the `Generated` reveal (dwell), via `Key`.

## Internal refactor: `DrylRunBase`

Extract the shared run plumbing (the `Channel<string>` text pipe, completion `TaskCompletionSource`,
`OnChange`, `Text`, `ToolCalls`, `State`, `TextStream`, `PushText`/`CompleteText`/`MarkCompleted`/
`Raise`/`AddToolCall`) from `DrylAgentRun` into an internal/base `DrylRunBase`, shared by
`DrylAgentRun` and `DrylArtifactRun<T>`. Internal only — the public surface of `DrylAgentRun` is
preserved, including the just-fixed **stable `TextStream` reference** (the cached enumerable moves
into the base and must remain a single stable instance).

## Testing strategy (no live API required)

- **Merge engine** (`JsonMerge`) unit tests: scalar overwrite, array replace, nested merge,
  null/absent leaves existing.
- **Update-tool closure**: build the generated `update_<T>` tool and invoke it directly with JSON
  args → assert `Artifact` merged, `Round++`, `OnChange` fired.
- **State mapping & cap**: at `MaxRounds`, the tool returns the nudge string.
- **Regression preserved**: `DrylAgentRun.TextStream` stable-reference test continues to pass
  after the `DrylRunBase` extraction.

## Files

New:
- `Agents/DrylRunBase.cs` — shared observable run plumbing
- `Agents/DrylArtifactRun.cs` — `DrylArtifactRun<T>` (Artifact/Round)
- `Agents/DrylAgentRunner.Build.cs` — `StartBuild<T>`, update-tool factory, merge invocation
- `Agents/DrylBuildOptions.cs`
- `Generation/JsonMerge.cs` — `JsonNode` deep-merge engine
- `Generation/DrylAiBuild.razor`

Changed:
- `Agents/DrylAgentRun.cs` — derive from `DrylRunBase` (public surface unchanged)
- `DRYL.Website/Components/Pages/AgentsLive.razor` — convert Section 2 to `StartBuild`
- `CHANGELOG.md` — Agents `[Unreleased]` → `Added`: `StartBuild`, `DrylArtifactRun<T>`,
  `DrylAiBuild<T>`, `DrylBuildOptions`
- `README.md` — component table row for `DrylAiBuild`
- tests under `tests/DRYL.Components.Tests/Agents/`

## Open verification points for the plan

1. Run-level `ChatOptions.Tools` merge vs. replace with agent tools (drives whether `StartBuild`
   must also take the HITL tools).
2. How to attach per-run additional instructions (the guidance) in `Microsoft.Agents.AI` 1.10.0 —
   run-options instructions vs. a prepended system message.
3. Constructing an `AIFunction` whose parameter is "T with all fields optional" — typed delegate
   with a post-processed schema vs. a raw-JSON parameter described by T's schema.
