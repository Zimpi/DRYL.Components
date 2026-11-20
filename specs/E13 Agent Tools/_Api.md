# Agent Tools — Public API

Shared enums, parameter contracts and services of the Agent Tools category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components.Agents/Tools/`

This file carries no `Meta` block and claims no component (`SPEC-03`). It
records the two ready-made tool sets of the category — the human-in-the-loop
tools behind the three dialogs, and the display tools rendered by
`DrylAgentAttachments` in `E12 Agent Runtime` — with the exact spelling used in
code. Both live in namespace `DRYL.Components.Agents.Tools`. The tool names and
argument names are a contract with the model as well as with the consumer:
renaming one changes what every agent prompt sees.

## `DrylUiTools`

`public sealed class DrylUiTools`, in
`code/DRYL.Components.Agents/Tools/DrylUiTools.cs`. Human-in-the-loop
`AIFunction`s backed by DRYL dialogs. Not registered in DI: the consumer
creates one per circuit, and the agent run must execute in that same circuit.

| Member | Type | Purpose |
|---|---|---|
| `Create(IDrylDialogService dialogs)` | `static DrylUiTools` | Creates the tool set over the circuit's dialog service. |
| `AskChoice` | `AITool` | The `ask_choice` tool. |
| `AskMultiChoice` | `AITool` | The `ask_multi_choice` tool. |
| `RequestPermission` | `AITool` | The `request_permission` tool. |
| `AskText` | `AITool` | The `ask_text` tool. |
| `All` | `IList<AITool>` | The four tools, in the order above. |

Every tool opens its dialog with `DialogOptions.AnimateHandoff` on and
`DrylMorphStyle.DepthGlass` as `HandoffStyle`, and returns a `string` to the
model.

### `ask_choice`

- Arguments: `question` (`string`), `options` (`string[]`), `recommended`
  (`string?`, optional).
- Opens [`F1 DrylAskChoiceDialog.md`](F1%20DrylAskChoiceDialog.md) titled
  "Choose".
- Returns the chosen option, or `The user cancelled the question.`

### `ask_multi_choice`

- Arguments: `question` (`string`), `options` (`string[]`), `recommended`
  (`string[]?`, optional).
- Opens [`F2 DrylAskMultiChoiceDialog.md`](F2%20DrylAskMultiChoiceDialog.md)
  titled "Choose".
- Returns the checked options joined by `, `, `(no selection)` when none was
  checked, or `The user cancelled the question.`

### `request_permission`

- Arguments: `action` (`string`), `details` (`string?`, optional).
- Opens the core `DrylConfirmDialog` through
  `IDrylDialogService.ShowConfirmAsync`, titled "Permission required", with the
  action — followed by a blank line and the details, when given — as message,
  and "Allow" / "Deny" as labels.
- Returns `true (the user allowed it)` or `false (the user denied permission)`.

### `ask_text`

- Arguments: `question` (`string`), `placeholder` (`string?`, optional).
- Opens [`F3 DrylAskTextDialog.md`](F3%20DrylAskTextDialog.md) titled
  "Question".
- Returns the typed text, or `The user cancelled the question.`

### Cancellation

- For `ask_choice`, `ask_multi_choice` and `ask_text`, cancelling the run's
  token cancels the open dialog, and the tool returns its cancelled answer.
- For `request_permission`, cancelling the run's token returns the denied
  answer at once, but the confirm dialog stays open until the user dismisses
  it.

## `DrylDisplayTools`

`public sealed class DrylDisplayTools`, in
`code/DRYL.Components.Agents/Tools/DrylDisplayTools.cs`. Display `AIFunction`s
that let the model answer with DRYL components. The tools only validate and
acknowledge; `DrylAgentAttachments` renders them from the run's tool-call
trace.

| Member | Type | Purpose |
|---|---|---|
| `Create()` | `static DrylDisplayTools` | Creates the tool set; no dependencies. |
| `LineChart` | `AITool` | `show_line_chart` → `DrylLineChart`. |
| `AreaChart` | `AITool` | `show_area_chart` → `DrylAreaChart`. |
| `BarChart` | `AITool` | `show_bar_chart` → `DrylBarChart`. |
| `DonutChart` | `AITool` | `show_donut_chart` → `DrylDonutChart`. |
| `Stats` | `AITool` | `show_stats` → a row of `DrylStat`. |
| `Timeline` | `AITool` | `show_timeline` → `DrylTimeline`. |
| `All` | `IList<AITool>` | The six tools, in the order above. |

A valid call returns `The {what} is now shown to the user inline in the
conversation. Do not repeat its data as text.`; an invalid one returns
`NOT shown to the user — {error} Fix the arguments and call the tool again.`,
where `{error}` is the first failing rule below.

### Display tool arguments

Declared as internal types in
`code/DRYL.Components.Agents/Tools/DisplaySpecs.cs`; the contract is their JSON
shape, read with web defaults (camelCase, case-insensitive property names).

| Tool | Arguments |
|---|---|
| `show_line_chart`, `show_area_chart` | `labels` (`string[]`), `series` (array of series), `title` (`string?`), `valueFormat` (`string?`) |
| `show_bar_chart` | as above, plus `stacked` (`bool`, default `false`) |
| `show_donut_chart` | `segments` (array of segments), `title` (`string?`), `valueFormat` (`string?`) |
| `show_stats` | `stats` (array of stats) |
| `show_timeline` | `events` (array of events), `title` (`string?`) |

- **Series:** `name` (`string`), `data` (`double[]`).
- **Segment:** `label` (`string`), `value` (`double`).
- **Stat:** `label` (`string`), `value` (`string`, pre-formatted), `delta`
  (`string?`), `direction` (`string?`: `up`, `down` or `neutral`).
- **Event:** `title` (`string`), `timestamp` (`string?`), `text`
  (`string?`), `kind` (`string?`: `default`, `success`, `warning` or
  `danger`).

### Validation rules

- Cartesian charts: at least one label; one to six series; every series has a
  non-blank name and exactly one value per label.
- Donut chart: one to six segments; every segment has a non-blank label and a
  finite value greater than zero.
- `valueFormat`, when given, must be a valid .NET numeric format string.
- Stats: one to six entries; every entry has a non-blank label and value;
  `direction` is absent or one of its three values, matched exactly.
- Timeline: at least one event; every event has a non-blank title; `kind` is
  absent or one of its four values, matched exactly.
