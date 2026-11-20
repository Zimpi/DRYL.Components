# DrylAgentAttachments

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Display/DrylAgentAttachments.razor
              code/DRYL.Components.Agents/Display/DrylAgentAttachments.razor.css

## User Story

As a Blazor developer who hands `DrylDisplayTools` to an agent, I want the
model's chart, KPI and timeline calls to appear as live DRYL components under
its answer, so that the AI can answer with a chart instead of a paragraph of
numbers and I do not have to map tool calls to components myself.

## Description

`DrylAgentAttachments` reads the tool-call trace of a `DrylRunBase` and renders
every call of one of the six display tools — `show_line_chart`,
`show_area_chart`, `show_bar_chart`, `show_donut_chart`, `show_stats`,
`show_timeline` — as an "attachment": the matching DRYL component, built from
the call's arguments. The tools themselves only validate and acknowledge (see
`DrylDisplayTools` in
[`../E13 Agent Tools/_Api.md`](../E13%20Agent%20Tools/_Api.md)); rendering
happens here, so attachments work with `Start`, `Replay` and the multi-agent
orchestrations alike.

The component parses and validates each call with the same argument types and
the same `Validate` rules the tool used, so an attachment renders exactly when
the model was told "shown to the user". A call the tool rejected renders
nothing — the model has already received a corrective tool result.

Attachments glide in one by one. A provider may deliver several tool calls in
one burst; the first new attachment shows immediately and every further one
waits for a fixed cadence after the previous reveal, so each gets its own
entrance.

Typical placement is inside the assistant `DrylMessage`, below the streamed
answer text.

## Public API

Namespace: `DRYL.Components.Agents`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylRunBase?` | `null` | The run whose display-tool calls to render. Nothing renders while it is `null`. |
| `Class` | `string?` | `null` | Extra classes merged onto the container's own. |

## Acceptance Criteria

### Selection

- The component renders nothing while `Run` is `null`.
- The component renders nothing while no attachment is revealed.
- A tool call whose `ToolName` is not one of the six display tool names
  renders nothing.
- A display tool call whose `Arguments` are missing or not valid JSON for its
  tool renders nothing.
- A display tool call whose arguments fail its tool's `Validate` rule renders
  nothing.
- Each call is parsed once, the first time the component sees its `CallId`.
- Attachments render in the run's tool-call order.

### Mapping

- `show_line_chart` renders a `DrylLineChart` with `Smooth` and
  `ShowMarkers` set.
- `show_area_chart` renders a `DrylAreaChart` with `Smooth` set.
- `show_bar_chart` renders a `DrylBarChart` whose `Stacked` is the call's
  `stacked` argument.
- The three cartesian charts receive the call's `labels`, one `ChartSeries`
  per entry of `series`, and its `valueFormat`.
- `show_donut_chart` renders a `DrylDonutChart` with one `ChartSegment` per
  entry of `segments`, and the call's `valueFormat`.
- `show_stats` renders one `DrylStat` per entry of `stats`, inside one
  `.agent-attachment-stats` grid.
- A stat's `direction` maps `up`, `down` and `neutral` to
  `DeltaDirection.Up`, `DeltaDirection.Down` and `DeltaDirection.Neutral`; no
  direction maps to `DeltaDirection.None`.
- `show_timeline` renders one `DrylTimeline` with one `DrylTimelineItem` per
  entry of `events`, carrying its `title` and `timestamp`.
- A timeline event's `kind` maps `success`, `warning` and `danger` to the
  `TimelineVariant` of the same name; anything else maps to
  `TimelineVariant.Default`.
- A timeline event with non-blank `text` renders it as the item's body; one
  without renders no body.
- Every rendered chart and stat receives `Ai` `AiState.Generated`.
- An attachment with a non-blank `title` renders it in
  `.agent-attachment-title` above the component; `show_stats` has no title.

### Reveal

- The first attachment revealed after a quiet period appears at once.
- An attachment that becomes ready less than the reveal cadence after the
  previous reveal waits until the cadence has elapsed.
- Each attachment is wrapped in its own `DrylPresence`, keyed by `CallId`,
  with `PresenceTransition.SlideUp`, `PresenceSpeed.Slow` and `Appear`.
- A revealed attachment stays rendered for as long as its call is in the run.

### Live updates

- The component picks up new tool calls when the run raises `OnChange`.
- When `Run` changes to another instance, the component drops every parsed
  and revealed attachment of the previous run and cancels its pending
  reveals.
- The component unsubscribes from the run's `OnChange` and cancels pending
  reveals when it is disposed.

### Appearance

- The container is a column spaced by `--sp-3`, set off from the content
  above by `--sp-3`.
- An attachment is a column spaced by `--sp-2`, and may shrink below its
  content's width.
- An attachment title reads in `--fg-muted`.
- The stats grid fills the row with auto-fitting columns spaced by `--sp-3`.

### Keyboard and accessibility

- The component adds no focusable element and no ARIA role of its own;
  keyboard and screen-reader behaviour are those of the composed charts,
  `DrylStat` and `DrylTimeline`.

## Recorded debt

- **Literal reveal cadence.** The stagger between attachments is a constant in
  code (`StaggerDelayMs`), not one of the motion tokens (`DESIGN-10`).
- **Literal sizes.** The title's 13 px font size and the stats grid's 150 px
  minimum column are literals (`DESIGN-01`).
- **The call's outcome is not consulted.** An attachment renders from the
  call's arguments whatever its `Approval`, `Result` or `Error`, so a display
  tool wrapped in `ApprovalRequiredAIFunction` renders while pending and stays
  rendered when denied.
- **The first parse is final.** A call first seen without `Arguments` is
  cached as "renders nothing" and is not re-parsed when its arguments arrive
  later.
- **The title is not a heading.** `.agent-attachment-title` is a plain
  element, so a titled chart has no heading in the document outline.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of the chart family,
  `DrylStat` and `DrylTimeline`; inspected on `/components/display-tools`, with attachments rendered, in dark and light (2026-11-20).
- **Enter/exit animation** — enter yes: `DrylPresence` per attachment, staggered
  in bursts. Exit: an attachment never unmounts while its run is shown;
  replacing the run removes the old attachments without an exit.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — no `Ai` parameter of its own (`AI-05`): every attachment is
  AI-made by definition, so each rendered chart and stat is handed
  `AiState.Generated` and plays the shared one-shot (`AI-07`) on arrival.
- **Demo page** — `/components/display-tools`
  (`DRYL.Website/Components/Pages/DemoDisplayTools.razor`), examples
  `Agents/DisplayTools` and `Agents/OllamaDisplayTools`.
- **`ComponentCatalog`** — the `"Display Tools"` / `display-tools` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAgentAttachmentsTests.cs`;
  the tools' validation in
  `tests/DRYL.Components.Tests/Agents/DrylDisplayToolsTests.cs`.
