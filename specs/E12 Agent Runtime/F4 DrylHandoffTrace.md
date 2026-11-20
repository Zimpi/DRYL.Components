# DrylHandoffTrace

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Agents/DrylHandoffTrace.razor
              code/DRYL.Components.Agents/Agents/DrylHandoffTrace.razor.css

## User Story

As a Blazor developer chaining several agents with
`DrylAgentRunner.StartSequential` or fanning them out with `StartConcurrent`, I
want one live timeline that shows which agent holds the baton, which are done
and which failed, so that my user can follow a multi-agent flow without me
building a status board.

## Description

`DrylHandoffTrace` renders a `DrylMultiAgentRun` (see [`_Api.md`](_Api.md)) as
a vertical list with one lane per step. Each lane has a rail — an optional
connector line and a round node — beside a bordered body that carries the
step's name, its status, and optionally its usage, its error and consumer
content.

The flow's shape decides the rail. A **sequential** flow draws a connector into
every lane but the first; the connector picks up the accent once the previous
step has handed over (reached `AiState.Generated`). A **concurrent** flow draws
no connectors and spaces its lanes apart instead, because nothing is handed on.

The lane speaks the shared AI vocabulary: while its step is at
`AiState.Thinking` or `AiState.Streaming` the body wears the AI aura in that
state and the node lifts with an accent glow; the status pill is a
`DrylAiIndicator` once the step has started. A finished step's node shows a
check, a failed step's node shows an alert icon, a red-tinted border and — by
default — the step's `DrylAgentError`.

Per-lane content is the consumer's: `StepContent` receives the lane's
`DrylAgentHandoff`, typically to render `context.Run?.Text` through
`DrylMarkdown`.

## Public API

Namespace: `DRYL.Components.Agents`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylMultiAgentRun?` | `null` | The flow to render. Nothing renders while it is `null`. |
| `ShowUsage` | `bool` | `true` | Render each started step's `DrylAgentUsage` inside its lane. |
| `ShowErrors` | `bool` | `true` | Render each started step's `DrylAgentError` (title "Step failed") inside its lane. |
| `StepContent` | `RenderFragment<DrylAgentHandoff>?` | `null` | Optional per-lane content; the context is the lane's step. |
| `Class` | `string?` | `null` | Extra classes merged onto the root's own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, splatted on the root. |

## Acceptance Criteria

### Structure

- The component renders nothing while `Run` is `null`.
- The root is one `.handoff-trace` element with `role="list"` and
  `aria-label="Agent flow"`.
- The root carries `.handoff-trace--concurrent` exactly when `Run.Flow` is
  `DrylAgentFlow.Concurrent`.
- `Class` is appended to the root's own classes.
- `AdditionalAttributes` are applied to the root.
- The component renders one `.handoff-step` with `role="listitem"` per entry
  of `Run.Steps`, in step order.
- Each lane renders the step's `Name` in `.handoff-name`.

### Rail

- In a `DrylAgentFlow.Sequential` flow, every lane but the first renders one
  `.handoff-connector`.
- In a `DrylAgentFlow.Concurrent` flow, no lane renders a `.handoff-connector`.
- A connector carries `.handoff-connector--done` exactly when the previous
  step's `State` is `AiState.Generated`.
- The rail is hidden from assistive technology (`aria-hidden="true"`).
- A lane's node shows the step's one-based position while the step has
  neither failed nor reached `AiState.Generated`.
- A lane's node shows the `Check` icon when the step's `State` is
  `AiState.Generated` and it has no `Error`.
- A lane's node shows the `Alert` icon when the step has an `Error`.

### Lane state

- A step whose `Error` is set marks its lane `.handoff-step--failed` and its
  node `.handoff-node--failed`.
- A step at `AiState.Thinking` or `AiState.Streaming` without an error marks
  its lane `.handoff-step--active` and its node `.handoff-node--active`.
- A step at `AiState.Generated` without an error marks its lane
  `.handoff-step--done` and its node `.handoff-node--done`.
- Each lane's aura is driven by an `AuraLifecycle` of its own and rendered
  through `AiAuraCss` and `DrylAuraElements`.
- An active lane's body carries `.ai-aura` plus `.ai-thinking` or
  `.ai-streaming` matching the step's state, and renders the aura's ring,
  comet and glow layers.
- A lane that completes while the trace is shown plays the `AiState.Generated`
  one-shot once — `.ai-generated` and the wash layer — and is then plain.
- A lane that was already at `AiState.Generated` when the trace first saw it is
  shown settled: no aura classes, no aura layers, no one-shot.
- A lane that fails or is stopped keeps its aura for the exit with
  `ai-aura--out`; then it is removed.
- A lane that never ran renders no aura classes and no aura layers.
- Passing another `Run` drops the lanes' aura state of the previous one.

### Status

- A step that has not started renders a "Pending" `DrylBadge` with a dot.
- A failed step renders a "Failed" `DrylBadge` with
  `DrylBadge.BadgeKind.Danger` and a dot.
- A started step without an error renders a `DrylAiIndicator` whose `State` is
  the step's `State`.
- That indicator reads "Running" at `AiState.Thinking`, "Streaming…" at
  `AiState.Streaming`, "Done" at `AiState.Generated`, "Active" at
  `AiState.Active`, and "Stopped" in any other state — a cancelled step.

### Lane content

- With `ShowUsage`, every lane renders a `DrylAgentUsage` for its step's run.
- With `ShowUsage` off, no lane renders usage.
- With `ShowErrors`, every lane renders a `DrylAgentError` for its step's run,
  titled "Step failed".
- With `ShowErrors` off, no lane renders an error alert.
- With `StepContent` set, every lane renders it in `.handoff-content`, with
  that lane's `DrylAgentHandoff` as context.
- Without `StepContent`, no `.handoff-content` element renders.

### Live updates

- The component re-renders when the flow raises `OnChange`.
- The component unsubscribes from the previous flow's `OnChange` when `Run`
  changes to another instance.
- The component unsubscribes from the flow's `OnChange` when it is disposed.

### Motion

- Each lane enters with `handoff-enter` (a fade and short rise) on `--dur-med`
  / `--ease-out`.
- The lanes' entrances are staggered by `--reveal-step` per lane index.
- A connector turning done fades its color on `--dur-slow` / `--ease-in-out`.
- A node's border, color and glow change on `--dur-med` / `--ease-out`, and
  its scale on `--dur-med` / `--ease-spring`.
- An active node is scaled up slightly; no other node is.
- A changed status pill fades in with `handoff-status-in` on `--dur-fast` /
  `--ease-out`, because the status element is re-keyed per lifecycle phase.
- A lane body's border color changes on `--dur-med` / `--ease-out`.
- Under `prefers-reduced-motion: reduce`, lanes and status pills do not
  animate, nodes, connectors and bodies do not transition, and the active node
  is not scaled.

### Appearance

- A connector reads `--line-strong`, and `--accent-line` once done.
- A node is a pill-round `--glass-1` disc with a `--line-strong` border and a
  `--fg-dim` number.
- An active node has an `--accent-line` border, `--fg` text and an
  `--accent-soft` glow.
- A done node has an `--accent-line` border and an `--accent-b` icon.
- A failed node has a `--danger` border and icon.
- A lane body is a `--glass-1` surface with a `--line` border and `--r-md`
  corners.
- A failed lane's body border is tinted with `--danger`.
- The step name reads in `--fg`; lane content in `--fg-muted`.
- In a concurrent flow, consecutive lanes are separated by `--sp-2`.

### Keyboard and accessibility

- The trace is a labelled list of list items; the step name and its status
  text are read per item.
- The status pill of a started step is a `DrylAiIndicator`, which announces
  state changes politely.
- The component adds no focusable element of its own; the composed
  `DrylAgentError` gets no `OnRetry`, so focusable content comes only from
  `StepContent`.

## Recorded debt

- **Fixed English labels.** "Agent flow", "Pending", "Failed", "Running",
  "Streaming…", "Done", "Active", "Stopped" and "Step failed" are not
  parameters.
- **Literal values.** The 11 px node font and icon size, the 1 px connector,
  the rise distances of the two keyframes, the active node's scale and glow
  radius, and the 35 % danger mix of a failed body are literals
  (`DESIGN-01`).
- **No exit.** The root mounts under `@if (Run is not null)` and lanes enter
  by keyframe only; neither is wrapped in `DrylPresence`, so clearing `Run`
  removes the trace without an exit (`DESIGN-12`). Lanes are keyed by index,
  so a new flow of the same length reuses them and does not replay the
  entrance.
- **Lane body frost.** The body is an in-flow `--glass-1` surface without
  `--glass-fx-flow`; whether frost is wanted here is undecided (`DESIGN-06`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only (`--glass-1`, `--line`,
  `--line-strong`, `--accent-*`, `--danger`, `--fg*`); inspected on `/components/multi-agent`, after a full pipeline run, in dark and light (2026-11-20).
- **Enter/exit animation** — enter yes, staggered per lane; state changes
  animate; no exit, recorded above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and not an opt-in: a multi-agent flow is AI work by
  definition, so the component takes no `Ai` parameter and derives each lane's
  state from its step's run (`AI-05`). The aura is composed from the shared
  primitives, one `AuraLifecycle` per lane; inspected in dark and light on a
  probe page of `tests/DRYL.BrowserHost` (2026-11-20).
- **Demo page** — `/components/multi-agent`
  (`DRYL.Website/Components/Pages/DemoMultiAgent.razor`), example
  `Agents/HandoffFlow`.
- **`ComponentCatalog`** — the `"Multi-Agent Handoff"` / `multi-agent` entry
  in `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylHandoffTraceTests.cs`;
  the flow semantics in
  `tests/DRYL.Components.Tests/Agents/DrylAgentRunnerOrchestrationTests.cs`.
