# Agent Runtime — Public API

Shared enums, parameter contracts and services of the Agent Runtime category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components.Agents/Agents/, code/DRYL.Components.Agents/Display/`

*Scaffold. The shared types below are filled in during phase C, each listed with
the exact spelling used in code. Until then this file claims nothing: it carries
no `Meta` block and the coverage check does not treat it as covering a
component (`SPEC-03`).*

## Shared types

*(phase C — the remaining types. The tool-approval contract below was written
with I21 and is complete.)*

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
