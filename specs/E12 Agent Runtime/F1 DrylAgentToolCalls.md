# DrylAgentToolCalls

## Meta
- **State:** Modified
- **Source:** code/DRYL.Components.Agents/Agents/DrylAgentToolCalls.razor
              code/DRYL.Components.Agents/Agents/DrylToolInvocation.cs

## User Story

As a Blazor developer running a Microsoft Agent Framework agent through
`DrylAgentRunner`, I want the run's tool calls — including the ones that wait
for my user's approval — to render as one live trace with the decision built
in, so that approval-required tools work without me writing a loop, a dialog or
a status vocabulary of my own.

## Description

`DrylAgentToolCalls` renders the tool calls of a `DrylRunBase` as one
`DrylToolCallGroup` holding a `DrylToolCall` per call. It derives everything the
two core components need from the run — the count, the running tool's name, the
group's overall state, the error flag — and re-renders whenever the run raises
`OnChange`.

Each call is a `DrylToolInvocation`. Its `State` maps the call's lifecycle onto
`AiState`, and its `Approval` records where the call stands with a human.

A consumer who wraps a tool in `ApprovalRequiredAIFunction` gets that tool
approved inline. The framework does not run such a tool: it emits a
`ToolApprovalRequestContent` and waits. `DrylAgentRunner.Start` turns the request
into a call whose `Approval` is `DrylToolApproval.Pending` and whose `State` is
`AiState.Proposed`, and the run rests at `AiState.Proposed` too. The component
binds the card's Approve / Deny pair to `DrylRunBase.Approve` and
`DrylRunBase.Deny`, and the group's bulk buttons to the same methods over every
pending call. Once the last pending call has a decision, the runner answers the
agent on the same `AgentSession` and the same run streams on.

`Microsoft.Extensions.AI` requires approval for **every** call of a model
response once one of them needs it, so several pending calls at once are the
normal case — hence the bulk buttons and the group opening by itself.

An approved call moves straight from `AiState.Proposed` to `AiState.Generated`:
the human accepted, and the card plays the shared accept wave from the button.
It stays at `AiState.Generated` while the tool runs and after its result
arrives. A denied call ends at `AiState.None` with `Declined` set on its card;
the framework hands the model a failed function result and the run goes on.

Scope limits, stated so they are not mistaken for gaps:

- Only `DrylAgentRunner.Start` and the `Replay` overload that takes a `resume`
  function answer approval requests. `Generate`, `Build`, `StartSequential` and
  `StartConcurrent` ignore them as before.
- Pending decisions live in the run object; they do not survive a reload or a
  new circuit.
- `DrylUiTools.RequestPermission` — a tool the model calls, which opens a
  dialog — is a different path and is unchanged: its call stays at
  `AiState.Thinking` while the dialog is open.

## Public API

### `DrylAgentToolCalls`

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylRunBase?` | `null` | The run whose tool calls to render. Nothing renders while it is `null` or has no calls. |
| `ActiveOnly` | `bool` | `false` | Render only the calls that are running or awaiting approval, instead of the full trace. |
| `ApproveLabel` | `string` | `"Approve"` | Passed to each card's `ApproveLabel`. |
| `DenyLabel` | `string` | `"Deny"` | Passed to each card's `DenyLabel`. |
| `ApproveAllLabel` | `string` | `"Approve all"` | Passed to the group's `ApproveAllLabel`. |
| `DenyAllLabel` | `string` | `"Deny all"` | Passed to the group's `DenyAllLabel`. |

### `DrylToolInvocation`

| Member | Type | Purpose |
|---|---|---|
| `CallId` | `string` | The framework call id. |
| `ToolName` | `string` | The tool the model invoked. |
| `Arguments` | `string?` | The call arguments as JSON. |
| `Result` | `string?` | The result as JSON; `null` until it arrives. |
| `Error` | `string?` | An error message; the call failed. |
| `Approval` | `DrylToolApproval` | Where the call stands with a human. Defaults to `DrylToolApproval.None`. |
| `State` | `AiState` | Derived; see the criteria. |

### `DrylToolApproval`

`None` (the call needed no approval) · `Pending` · `Approved` · `Declined`.

The run members (`PendingApprovals`, `Approve`, `Deny`) and the runner's
`Replay` overload are listed in `_Api.md`.

## Acceptance Criteria

### Trace

- The component renders nothing while `Run` is `null`.
- The component renders nothing while the run has no tool calls.
- The component renders one `DrylToolCall` per tool call of the run, in arrival
  order.
- The component renders the cards inside one `DrylToolCallGroup`.
- The group's `Count` is the number of rendered cards.
- The group's `ActiveLabel` is the name of the first call at
  `AiState.Thinking` or `AiState.Streaming`.
- The group's `HasError` is `true` while any rendered call has a non-empty
  `Error`.
- The component re-renders when the run raises `OnChange`.
- The component unsubscribes from the run's `OnChange` when `Run` changes.
- The component unsubscribes from the run's `OnChange` when it is disposed.
- With `ActiveOnly`, the component renders the calls at `AiState.Thinking`.
- With `ActiveOnly`, the component renders the calls at `AiState.Proposed`.
- With `ActiveOnly`, the component renders no call in any other state.

### Group state

- The group's `Ai` is the state of the first call at `AiState.Thinking` or
  `AiState.Streaming`.
- The group's `Ai` is `AiState.Proposed` when no call is running and at least
  one call is `DrylToolApproval.Pending`.
- The group's `Ai` is `AiState.None` when no call is running and none is
  pending.
- The group's `PendingCount` is the number of rendered calls that are
  `DrylToolApproval.Pending`.

### Invocation state

- `Approval` defaults to `DrylToolApproval.None`.
- `State` is `AiState.None` when `Error` is non-empty.
- `State` is `AiState.None` when `Approval` is `DrylToolApproval.Declined`.
- `State` is `AiState.Proposed` when `Approval` is `DrylToolApproval.Pending`
  and `Error` is empty.
- `State` is `AiState.Generated` when `Approval` is `DrylToolApproval.Approved`
  and `Error` is empty, whether or not `Result` has arrived.
- `State` is `AiState.Generated` when `Result` is set, `Error` is empty and the
  call is neither pending nor declined.
- `State` is `AiState.Thinking` when none of the above applies.

### Approval request

- `DrylAgentRunner.Start` turns a `ToolApprovalRequestContent` into a tool call
  with `Approval` `DrylToolApproval.Pending`.
- That tool call carries the `CallId`, the tool name and the arguments of the
  request's wrapped function call.
- A function call that arrives for a call id already in the trace updates that
  call instead of adding a second one.
- The run's `State` is `AiState.Proposed` once the agent's response has ended
  with at least one pending call.
- The run does not reach `AiState.Generated` while a call is pending.
- The run's `PendingApprovals` lists exactly the calls that are
  `DrylToolApproval.Pending`, in arrival order.

### Decision

- `Approve` sets a pending call's `Approval` to `DrylToolApproval.Approved`.
- `Deny` sets a pending call's `Approval` to `DrylToolApproval.Declined`.
- `Approve` returns `true` when it found a pending call with that id.
- `Deny` returns `true` when it found a pending call with that id.
- `Approve` and `Deny` return `false` and change nothing for an id that is not
  pending.
- Each decision raises the run's `OnChange`.
- The run stays at `AiState.Proposed` while any call is still pending.
- Once the last pending call has a decision, the runner sends one
  `ToolApprovalResponseContent` per decided call to the agent, on the same
  `AgentSession`.
- Each response's `Approved` matches the human's decision for that call.
- The agent's continuation streams into the same `DrylAgentRun`: its text is
  appended to `Text` and `TextStream`.
- The agent's continuation adds its tool calls to the same trace.
- The run leaves `AiState.Proposed` when the continuation starts.
- A response that again ends with pending calls returns the run to
  `AiState.Proposed`, and the cycle repeats.
- An approved call's `Result` is filled in when the tool's result arrives.
- A denied call keeps `Approval` `DrylToolApproval.Declined` when the
  framework's failed function result for it arrives, and gains no `Error`.

### Wiring

- Each card's `Declined` is `true` exactly when its call is
  `DrylToolApproval.Declined`.
- A pending call's card renders Approve and Deny buttons.
- Activating a card's Approve button calls `Approve` with that card's `CallId`.
- Activating a card's Deny button calls `Deny` with that card's `CallId`.
- Activating the group's Approve all button approves every pending call.
- Activating the group's Deny all button denies every pending call.
- The labels `ApproveLabel`, `DenyLabel`, `ApproveAllLabel` and
  `DenyAllLabel` reach the cards and the group unchanged.

### Cancellation

- Cancelling the run's token while calls are pending sets each pending call to
  `DrylToolApproval.Declined`.
- Cancelling the run's token while calls are pending settles the run at
  `AiState.None` without an `Error`.
- Disposing the run while calls are pending ends the wait.
- `Replay` without a `resume` function leaves approval requests unanswered and
  creates no pending call, as before.

### Keyboard and accessibility

- Keyboard operation, focus order, accessible names and focus return are those
  of `DrylToolCall` and `DrylToolCallGroup`; the component adds no focusable
  element of its own.
- The run's resting `AiState.Proposed` is announced through `DrylAiIndicator`
  hosts and the cards' visually hidden proposal text, not through a live region
  of this component.

### Appearance

- The component writes no CSS of its own; color, motion and both color modes
  are those of `DrylToolCallGroup` and `DrylToolCall` (`DESIGN-01`,
  `DESIGN-02`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — no styling of its own; the composed core components
  are token-only and verified by `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`.
- **Enter/exit animation** — the group's disclosure and the cards' approval
  buttons enter and leave through the core components' motion; an accepted
  call plays the shared accept wave, a declined one leaves through the aura's
  graceful exit.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and it is not an opt-in: a tool call trace is AI work by
  definition, so the component takes no `Ai` parameter and derives every state
  from the run (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Examples/Agents/ToolApproval.razor`
  on the Agent Run page.
- **`ComponentCatalog`** — the `"Agent Run"` / `agent-run` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`.
