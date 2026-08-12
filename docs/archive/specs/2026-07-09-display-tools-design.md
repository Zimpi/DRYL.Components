# Display Tools — the AI renders DRYL components into the chat

**Date:** 2026-07-09
**Package:** `DRYL.Components.Agents` (plus one small core addition)
**Status:** Approved (display-only v1; interaction stays with the `DrylUiTools` dialogs)

## Idea

`DrylUiTools` gave agents ready-made *input* tools (dialogs that ask the user).
This feature is the mirror image: ready-made *output* tools. Hand them to any
agent and the model can answer with **live DRYL components inline in the chat**
instead of text alone — a `DrylLineChart` glides in under the streaming answer,
flanked by `DrylStat` KPI cards.

One line to enable:

```csharp
var display = DrylDisplayTools.Create();
agent.Tools = display.All;                 // or pick individual tools
```

One line to render:

```razor
<DrylAgentAttachments Run="_run" />        @* under the streaming text *@
```

Nothing comparable exists in the .NET/Blazor ecosystem.

## Scope decision (locked)

- **Display-only.** No interactive widgets in v1 — interaction keeps flowing
  through the `DrylUiTools` dialogs so there is no Medienbruch. Tool-rendered
  buttons/choices are a possible v2.
- Per-tool, tightly typed schemas (not one recursive component tree). Small
  schemas = the model almost cannot produce broken UI.

## Architecture

### Tools validate & acknowledge; the run trace renders

The tool functions themselves have **no UI side effect**. They validate the
arguments and return a model-facing acknowledgement string. Rendering is driven
declaratively from `DrylRunBase.ToolCalls` — the same pattern as
`DrylAgentToolCalls`. Consequences:

- Works with `Start`, `Replay`, `StartSequential` — anything that produces a run.
- No dialog service / circuit coupling: `DrylDisplayTools.Create()` takes no
  arguments.
- Validation failures return an instructive string
  (`"NOT shown to the user — series 'Revenue' has 3 values but there are 4
  labels. Fix the arguments and call the tool again."`) so the model
  self-corrects; the renderer independently skips anything it cannot parse.

### The tool set — `DrylDisplayTools` (namespace `DRYL.Components.Agents.Tools`)

| Tool | Renders | Arguments (all camelCase in JSON) |
| ---- | ------- | --------------------------------- |
| `show_line_chart`  | `DrylLineChart` (Smooth, markers) | `title?`, `labels[]`, `series[{name, data[]}]`, `valueFormat?` |
| `show_area_chart`  | `DrylAreaChart` (Smooth)          | same as line |
| `show_bar_chart`   | `DrylBarChart`                    | same as line + `stacked?` |
| `show_donut_chart` | `DrylDonutChart`                  | `title?`, `segments[{label, value}]` |
| `show_stats`       | row of `DrylStat`                 | `stats[{label, value, delta?, direction?}]` (direction: `up`/`down`/`neutral`) |
| `show_timeline`    | `DrylTimeline` + items            | `title?`, `events[{title, timestamp?, text?, kind?}]` (kind: `default`/`success`/`warning`/`danger`) |

Properties mirror `DrylUiTools`: one `AITool` per entry plus `All`.
Tool descriptions tell the model these render **inline in the conversation**
and to prefer them over ASCII/markdown approximations of data.

Deliberately **not** in v1: `show_table` (`DrylTable<TItem>` is declarative /
generic — markdown tables in the text already render via `DrylMarkdown`),
icons (the model doesn't know the icon vocabulary), per-series `ColorSlot`
(palette order is the design system's job).

### Payload DTOs

Internal records in `DRYL.Components.Agents.Tools` (e.g. `ChartSeriesSpec`,
`CartesianChartArgs`, `DonutChartArgs`, `StatSpec`, `TimelineEventSpec`) with
`[Description]` on every property — they are both the AIFunction schema source
and the renderer's parse target (`PropertyNameCaseInsensitive`). A shared
`Validate()` per args type returns `null` or the model-facing error; the tool
and the renderer use the same code path, so "tool said shown" ⇔ "renderer
renders" by construction.

`valueFormat` is validated by attempting `0d.ToString(format)`; invalid formats
are rejected with a hint (`"use a .NET numeric format string like 'C0' or 'N0'"`).

### The renderer — `DrylAgentAttachments` (namespace `DRYL.Components.Agents`)

- Parameters: `Run` (`DrylRunBase?`), `Class` (merged). That's it for v1.
- Subscribes to `Run.OnChange` (identical lifecycle to `DrylAgentToolCalls`).
- For every tool call whose name is a display tool and whose arguments parse &
  validate: render the mapped component, `@key`ed by `CallId`, wrapped in
  `DrylPresence` (`Appear`, `Transition=SlideUp`, `Speed=Slow`) so each
  attachment glides in — and out again if the host clears the run inside its
  own presence.
- Rendered components get `Ai="AiState.Generated"` — the shared one-shot
  reveal wash marks them as model-made (rule 2.10 vocabulary, nothing new).
- Numbers inside SVG payloads go through the existing chart components, which
  already handle invariant formatting.
- Unknown / invalid calls render nothing (the model already got the error text).
- Accessibility: charts/stat/timeline carry their own aria labels; the
  container is a plain `div.agent-attachments` (scoped CSS: vertical stack,
  `gap: var(--sp-3)`). The chat log's `aria-live="polite"` announces arrival.

### Core addition — `DrylPresence` `Speed` (separate user request, same branch)

The presence animations are hard-wired to `--dur-med` (240 ms), which is too
fast to register for content like chat attachments. New parameter:

```csharp
/// <summary>Playback speed of the enter/exit animation, mapped to the fixed duration tokens.</summary>
[Parameter] public PresenceSpeed Speed { get; set; } = PresenceSpeed.Medium;
```

`enum PresenceSpeed { Medium, Fast, Slow }` (first member = default, per
CONVENTIONS §2). CSS in `dryl.css` next to the existing presence block:

```css
.presence--fast.presence-enter,  .presence--fast.presence-exit  { animation-duration: var(--dur-fast); }
.presence--slow.presence-enter,  .presence--slow.presence-exit  { animation-duration: var(--dur-slow); }
```

No new duration values — rule 2.5's fixed vocabulary only. Default `Medium`
keeps every existing consumer pixel-identical. `prefers-reduced-motion`
already kills both classes via the existing override.

## Website demo

New `DemoExample` on the Agents page (`Agents/DisplayTools.razor`): a
`DrylChat` conversation with a scripted "data analyst" agent
(`SimScenarios.AnalystAgent()` — `SimulatedChatClient`, no API key). The user
asks about quarterly sales; the agent streams text and calls `show_stats`,
`show_line_chart`, `show_donut_chart`; the assistant message hosts
`DrylAiStream` + `DrylAgentAttachments`, so KPI cards and charts assemble
live in the chat. Tool invocation is real (Agent Framework), only the model is
scripted — same disclaimer as the rest of the page.

Catalog: update the existing `Agents` entry description in `ComponentCatalog`
to mention display tools. The page header's hardcoded package version is
brought in line (0.3.0).

## Docs & versioning

- **Core** `DRYL.Components`: 1.2.0 → **1.3.0** (MINOR — new `Speed` parameter).
  Publish workflow ships it.
- **Agents** `DRYL.Components.Agents`: 0.2.0 → **0.3.0** (new tools + component);
  not auto-published (known workflow gap), version + release notes still bumped.
- `CHANGELOG.md`: cut `[1.3.0] — 2026-07-09` with the core and Agents entries.
- Agents `PACKAGE.md`: add the display tools to the feature list.

## Testing / verification

The repo has no unit-test project; verification follows the established route:
`dotnet build` the solution, then drive the new demo end-to-end via the
`verify` skill (website + Playwright) — attachments appear animated during the
run, error path exercised by a deliberately invalid scripted call in a scratch
check (not committed).

## Error handling summary

| Failure | Behaviour |
| ------- | --------- |
| Model sends invalid args | Tool returns corrective error string; nothing renders; model retries |
| Arguments JSON unparseable in renderer | Attachment skipped silently |
| Run cleared / disposed mid-stream | Renderer unsubscribes (`Dispose`), presence exit plays if host wraps it |
| Framework-level tool exception | Existing `DrylToolInvocation.Error` path (danger state in trace) |
