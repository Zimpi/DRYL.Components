# Agent Runtime — Public API

Shared enums, parameter contracts and services of the Agent Runtime category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components.Agents/Agents/, code/DRYL.Components.Agents/Display/`

This file carries no `Meta` block and claims no component (`SPEC-03`). It
records the run surface every component of the category reads — the runner, the
run types, their error, usage and multi-agent shapes — with the exact spelling
used in code. All types live in namespace `DRYL.Components.Agents`.
`DrylToolInvocation` is specified with its only renderer, in
[`F1 DrylAgentToolCalls.md`](F1%20DrylAgentToolCalls.md).

## `DrylAgentRunner`

`public sealed partial class DrylAgentRunner`, spread over
`code/DRYL.Components.Agents/Agents/DrylAgentRunner*.cs`. A scoped service
(one per circuit), registered by `AddDrylAgents()` (see
[`_Interop.md`](_Interop.md)).

- `DrylAgentRunner(IDrylAiActivityService? activity = null)` — the optional
  activity service lets a run drive a surrounding `DrylAiScope` through its
  `aiKey`.
- `DrylAgentRun Start(AIAgent agent, AgentSession session, string message, string? aiKey = null, CancellationToken ct = default)`
  — starts a streaming agent run; answers tool-approval requests on the same
  session (see `F1 DrylAgentToolCalls.md`).
- `DrylAgentRun Replay(IAsyncEnumerable<AgentResponseUpdate> updates, string? aiKey = null, CancellationToken ct = default)`
  — drives a run from a recorded update sequence, with the same state and
  tool-call mapping as `Start`; approval requests are ignored.
- `DrylAgentRun Replay(IAsyncEnumerable<AgentResponseUpdate> updates, Func<…> resume, string? aiKey = null, CancellationToken ct = default)`
  — see "`DrylAgentRunner.Replay` with a resume function" below.
- `IAsyncEnumerable<string> GenerateStreamingAsync<T>(AIAgent agent, AgentSession session, string prompt, string? aiKey = null, CancellationToken ct = default)`
  — requests JSON conforming to `T`'s schema and yields the raw text deltas,
  for `DrylAiGenerate`.
- `DrylArtifactRun<T> StartBuild<T>(AIAgent agent, AgentSession session, string prompt, DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)`
  — an iterative artifact build through an auto-injected `update_<t>` tool,
  for `DrylAiBuild`.
- `DrylMultiAgentRun StartSequential(IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey = null, CancellationToken ct = default)`
  — a handoff chain; see "Multi-agent flows".
- `DrylMultiAgentRun StartConcurrent(IReadOnlyList<DrylAgentStep> steps, string message, string? aiKey = null, CancellationToken ct = default)`
  — a fan-out; see "Multi-agent flows".

`StartSequential` and `StartConcurrent` throw `ArgumentNullException` for a
`null` step list and `ArgumentException` for an empty one.

`GenerateStreamingAsync<T>` accepts an `aiKey` but does not use it: the
surrounding scope is driven by `DrylAiGenerate`, not by the runner.

### Run state convention

Every runner path drives `State` the same way:

- A run starts at `AiState.Thinking`.
- Reasoning content and a new function call set `AiState.Thinking`.
- Text content appends to `Text`, pushes the delta to `TextStream` and sets
  `AiState.Streaming`.
- A run that ends normally settles at `AiState.Generated`.
- A run whose processing faults sets `Error` and settles at `AiState.None`.
- A cancelled run settles at `AiState.None` without an `Error`.
- With an `aiKey` and a registered activity service, every state is mirrored
  onto that key; `AiState.None` clears it.

## `DrylRunBase`

`public abstract class DrylRunBase : IAsyncDisposable`, in
`code/DRYL.Components.Agents/Agents/DrylRunBase.cs`. The observable surface
shared by every run type.

| Member | Type | Purpose |
|---|---|---|
| `State` | `AiState` | Current state, driven by the runner. Starts at `AiState.Thinking`. |
| `Text` | `string` | The accumulated answer text. |
| `Error` | `DrylRunError?` | The terminal error; `null` while running and after success. |
| `Usage` | `DrylRunUsage?` | Accumulated token usage; `null` until the first usage update. |
| `ToolCalls` | `IReadOnlyList<DrylToolInvocation>` | The observed tool calls, in arrival order. |
| `PendingApprovals` | `IReadOnlyList<DrylToolInvocation>` | See "Tool approval on `DrylRunBase`". |
| `TextStream` | `IAsyncEnumerable<string>` | The text deltas; one stable reference for the run's lifetime, so `DrylAiStream` does not restart. |
| `OnChange` | `event Action?` | Raised whenever state, text, tool calls, usage, error or subclass state changes. |
| `Approve(string callId)` | `bool` | See "Tool approval on `DrylRunBase`". |
| `Deny(string callId)` | `bool` | See "Tool approval on `DrylRunBase`". |
| `WaitForCompletionAsync()` | `Task` | Completes when the run's processing ends (or the run is disposed). |
| `DisposeAsync()` | `ValueTask` | Virtual. Cancels the run, completes `TextStream` and `WaitForCompletionAsync`. |

`OnChange` may be raised off the renderer's synchronization context; every
component of the category marshals it with `InvokeAsync`.

## `DrylAgentRun`

`public sealed class DrylAgentRun : DrylRunBase`, in
`code/DRYL.Components.Agents/Agents/DrylAgentRun.cs`. The handle returned by
`Start` and `Replay`, and the child run of each multi-agent step. It adds no
members.

## `DrylArtifactRun<T>`

`public sealed class DrylArtifactRun<T> : DrylRunBase`, in
`code/DRYL.Components.Agents/Agents/DrylArtifactRun.cs`. The handle returned by
`StartBuild<T>`.

| Member | Type | Purpose |
|---|---|---|
| `Artifact` | `T?` | The live, progressively merged artifact; fields not yet provided are `null`/default. |
| `Round` | `int` | The number of `update_<t>` merges applied. |

## `DrylBuildOptions`

`public sealed class DrylBuildOptions`, in
`code/DRYL.Components.Agents/Agents/DrylBuildOptions.cs`. Init-only options
for `StartBuild<T>`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `MaxRounds` | `int?` | `12` | Cap on refinement rounds; `null` is unbounded. At the cap the tool tells the model to finish. |
| `Guidance` | `string?` | `null` | Replaces the built-in iterative-build instructions. |
| `UpdateToolName` | `string?` | `null` | Replaces the generated tool name `update_<t>`. |
| `RevealDuration` | `TimeSpan` | 1.2 s | Target duration of each round's progressive reveal; zero or negative merges atomically. |

## `DrylRunError`

`public sealed class DrylRunError`, in
`code/DRYL.Components.Agents/Agents/DrylRunError.cs`. Constructed only by the
library. Rendered by [`F2 DrylAgentError.md`](F2%20DrylAgentError.md).

| Member | Type | Purpose |
|---|---|---|
| `Message` | `string` | The human-readable message — the exception's message. |
| `ExceptionType` | `string?` | The short type name of the exception, if any. |
| `Exception` | `Exception?` | The exception, for logging and retry policies. |
| `Source` | `string?` | In a multi-agent run, the `Name` of the failing step; `null` otherwise. |

A run with `Error` set has settled at `AiState.None` — the same convention a
failed `DrylToolInvocation` follows.

## `DrylRunUsage`

`public sealed class DrylRunUsage`, in
`code/DRYL.Components.Agents/Agents/DrylRunUsage.cs`. Rendered by
[`F3 DrylAgentUsage.md`](F3%20DrylAgentUsage.md).

| Member | Type | Purpose |
|---|---|---|
| `InputTokens` | `long?` | Summed prompt-side tokens; `null` until a provider reports any. |
| `OutputTokens` | `long?` | Summed completion-side tokens; `null` until reported. |
| `TotalTokens` | `long?` | Summed total tokens; `null` until reported. |

Each `UsageContent` in the update stream adds its counts; a count the provider
leaves out stays untouched. A run's `Usage` is `null` until the first
`UsageContent` arrives, and raises `OnChange` on every addition.

## `DrylAgentFlow`

`public enum DrylAgentFlow`, in
`code/DRYL.Components.Agents/Agents/DrylMultiAgentRun.cs`. Members, in this
order:

- `Sequential` — steps run one after another; each receives the previous
  step's answer.
- `Concurrent` — all steps run in parallel on the same message.

## `DrylAgentStep`

`public sealed class DrylAgentStep`, in
`code/DRYL.Components.Agents/Agents/DrylAgentStep.cs`. The definition of one
agent in a flow.

| Member | Type | Purpose |
|---|---|---|
| `Name` | `string` | Required. The step's display name, shown per lane by `DrylHandoffTrace`. |
| `Agent` | `AIAgent?` | The agent that runs the step. |
| `Session` | `AgentSession?` | Optional session; omit for a one-shot run. |

A step without an `Agent` fails the flow with an `InvalidOperationException`
when it is reached.

## `DrylAgentHandoff`

`public sealed class DrylAgentHandoff`, in
`code/DRYL.Components.Agents/Agents/DrylMultiAgentRun.cs`. One step's slot in
a running flow; the context of `DrylHandoffTrace.StepContent`.

| Member | Type | Purpose |
|---|---|---|
| `Name` | `string` | The step's display name. |
| `Index` | `int` | Zero-based position in the flow. |
| `Run` | `DrylAgentRun?` | The step's own run; `null` while the step is pending. |
| `HasStarted` | `bool` | `true` once `Run` is set. |
| `State` | `AiState` | The child run's state; `AiState.None` while pending. |
| `Error` | `DrylRunError?` | The child run's error, if it failed. |

## `DrylMultiAgentRun`

`public sealed class DrylMultiAgentRun : DrylRunBase`, in
`code/DRYL.Components.Agents/Agents/DrylMultiAgentRun.cs`. The handle returned
by `StartSequential` and `StartConcurrent`; rendered by
[`F4 DrylHandoffTrace.md`](F4%20DrylHandoffTrace.md).

| Member | Type | Purpose |
|---|---|---|
| `Flow` | `DrylAgentFlow` | How the steps execute. |
| `Steps` | `IReadOnlyList<DrylAgentHandoff>` | One entry per step, in definition order, created up front. |
| `ActiveIndex` | `int?` | The running step of a sequential flow; `null` otherwise and after the flow ends. |
| `DisposeAsync()` | `ValueTask` | Cancels the flow, then disposes every started child run. |

### Multi-agent flows

The inherited run surface aggregates the flow:

- **Sequential.** Steps start one at a time; each step's input is the previous
  step's `Text`, the first step's is the flow's message. While a non-final step
  reaches `AiState.Generated`, the flow reads `AiState.Thinking` — it is
  handing over. Only the final step's text is forwarded into the flow's `Text`
  and `TextStream`. A failing step stops the chain.
- **Concurrent.** Every step starts at once on the same message. The flow reads
  `AiState.Streaming` while any step streams and `AiState.Thinking` otherwise.
  The flow's `Text` stays empty; each answer lives on its step's `Run`.
- **Both.** `Usage` sums the steps' usage — after each step in a sequential
  flow, once every step has ended in a concurrent one. The first failing
  step, in step order, sets the flow's `Error` with that step's `Name` as
  `Source` and settles the flow at `AiState.None`; a cancelled flow settles at
  `AiState.None` without an `Error`; otherwise the flow ends at
  `AiState.Generated`. Approval requests are not answered inside a flow.

## `DrylToolApproval`

Where a `DrylToolInvocation` stands with a human. Members, in this order:

- `None` — the call needed no approval.
- `Pending` — the framework asked for approval; the call has not run.
- `Approved` — a human approved it; the framework runs it on resumption.
- `Declined` — a human denied it, or the run was cancelled while it waited.

## Tool approval on `DrylRunBase`

- `IReadOnlyList<DrylToolInvocation> PendingApprovals` — the calls whose
  `Approval` is `DrylToolApproval.Pending`, in arrival order.
- `bool Approve(string callId)` — approves one pending call; `false` when no
  pending call has that id.
- `bool Deny(string callId)` — denies one pending call; `false` when no pending
  call has that id.

A run whose runner path does not answer approval requests (see
`F1 DrylAgentToolCalls.md`) never has a pending call, so both methods return
`false` there.

## `DrylAgentRunner.Replay` with a resume function

```csharp
DrylAgentRun Replay(
    IAsyncEnumerable<AgentResponseUpdate> updates,
    Func<IReadOnlyList<ToolApprovalResponseContent>, CancellationToken,
         IAsyncEnumerable<AgentResponseUpdate>> resume,
    string? aiKey = null, CancellationToken ct = default)
```

Behaves like `Replay(updates, aiKey, ct)`, and answers approval requests: once
the last pending call has a decision, `resume` receives the responses and its
updates continue the same run. `Start` uses the same mechanism with the agent
and session it was given.
