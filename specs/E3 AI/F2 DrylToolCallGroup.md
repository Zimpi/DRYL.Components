# DrylToolCallGroup

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylToolCallGroup.razor
              code/DRYL.Components/Components/AI/DrylToolCallGroup.razor.css

## User Story

As a Blazor developer building an agent UI, I want a run of tool calls to
collapse into one quiet summary row that I can expand, so that a long agent turn
does not bury the conversation under a stack of cards.

## Description

`DrylToolCallGroup` wraps a run of `DrylToolCall` cards in one collapsible
summary. Collapsed, it is a single row: while a call is running it tickers the
active tool's name and breathes the AI aura; once the run settles it reads
"N tool calls" and goes still. Expanding reveals the cards passed as
`ChildContent`.

A component cannot introspect its `ChildContent`, so the summary is driven by
explicit parameters instead: the host passes the count, the active tool's name
and the overall state alongside the cards. `DrylAgentToolCalls` in the agents
package derives all three from a run.

Two decisions distinguish it from `DrylToolCall`. Its aura falls back to
`AiAura.Aurora` rather than `AiAura.Comet`, because a collapsed group is a dense
secondary surface that should not shout. And `HasError` reveals the body once, so
a failed tool is never swallowed by the summary.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Count` | `int` | `0` | Number of tool calls represented; shown once the run settles. |
| `ActiveLabel` | `string?` | `null` | Name of the running tool; tickered instead of the count while the group is active. |
| `Ai` | `AiState` | `AiState.None` | The opt-in (`AI-03`). Drives the status pill, the ticker and the aura. |
| `State` | `AiState` | — | **Obsolete.** Delegating alias for `Ai`; removed in `3.0.0`. See `_Api.md`. |
| `HasError` | `bool` | `false` | A tool in the run failed; the group reveals itself once and marks the row. |
| `ChildContent` | `RenderFragment?` | `null` | The individual `DrylToolCall` cards. |
| `DefaultExpanded` | `bool` | `false` | Whether the group starts expanded. |
| `Aura` | `AiAura?` | `null` | Aura variant. `null` inherits the surrounding `DrylAiScope`, ultimately `AiAura.Aurora`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the component's own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

## Acceptance Criteria

### Summary row

- The collapsed row renders `Count` followed by "tool call" when `Count` is `1`.
- The collapsed row renders `Count` followed by "tool calls" for any other
  `Count`.
- The collapsed row renders `ActiveLabel` instead of the count while the group is
  active and `ActiveLabel` is non-empty.
- The group is active exactly when `Ai` is `AiState.Thinking` or
  `AiState.Streaming`.
- The label is rendered in `--font-mono` while it tickers a tool name, matching
  the cards below it.
- The label falls back to the count when the group is active but `ActiveLabel` is
  empty.

### Status

- The status pill reads "Running" when the group is active and `Ai` is not
  `AiState.Streaming`.
- The status pill reads "Streaming" when `Ai` is `AiState.Streaming`.
- The status pill reads "Error" when `HasError` is `true` and the group is not
  active.
- The status pill reads "Done" when the group is neither active nor in error.

### AI state

- `Ai` defaults to `AiState.None`.
- `Ai` accepts exactly the five values of `AiState`.
- The group renders its count and its status unchanged when `Ai` is
  `AiState.None` — the parameter is a switch, not a precondition.
- Setting the obsolete `State` alias sets `Ai` to the same value.
- Reading the obsolete `State` alias returns the current value of `Ai`.
- The aura is removed from the surface once `Ai` returns to `AiState.None`
  (`AI-06`).
- The `AiState.Generated` reveal fires once per transition into that state, not
  on every re-render (`AI-07`).
- The aura variant resolves to the explicit `Aura`, otherwise to the surrounding
  `DrylAiScope`'s variant, otherwise to `AiAura.Aurora` — not `AiAura.Comet`, and
  this is the one documented departure from the category default.
- The AI state is never inherited from a surrounding `DrylAiScope`.

### Disclosure and error handling

- The body starts collapsed when `DefaultExpanded` is `false`.
- The body starts expanded when `DefaultExpanded` is `true`.
- Activating the head row toggles the body.
- The body remains in the DOM while collapsed, so the state of the cards inside
  it survives a collapse.
- The root carries `is-open` exactly while the body is expanded.
- The body is revealed when `HasError` transitions from `false` to `true`.
- The reveal happens once per transition: the user may collapse the group again
  while `HasError` stays `true`.
- The root carries `tool-group--error` while `HasError` is `true`.
- The error border derives from `--danger`, mixed rather than applied at full
  strength (`DESIGN-08`).

### Motion

- The body animates open and closed over `--dur-med` with `--ease-in-out`, on the
  `grid-template-rows` track rather than on the content (`DESIGN-11`). The body
  does not mount conditionally, so `DESIGN-12`'s `DrylPresence` requirement has
  no subject here; keeping the cards mounted is what preserves their state
  across a collapse.
- The chevron rotates over `--dur-med` with `--ease-in-out` rather than snapping.
- The head row's color transitions over `--dur-fast` with `--ease-out` on hover.
- The body and chevron transitions are disabled under
  `prefers-reduced-motion: reduce`.

### Keyboard and accessibility

- The head row is a `<button type="button">`.
- The head row is reachable by <kbd>Tab</kbd>.
- The head row activates on <kbd>Enter</kbd> and on <kbd>Space</kbd>.
- The head row carries `aria-expanded` reflecting the body's state.
- The head row carries `aria-controls` pointing at the body's `id`.
- The body's `id` is unique per component instance.
- The body carries `role="region"`.
- The body carries `aria-hidden="true"` exactly while collapsed, so assistive
  tech does not read the cards the user cannot see.
- The body carries `inert` exactly while collapsed, so no card head and no copy
  button inside it can be reached by <kbd>Tab</kbd> while invisible (`UX-07`).
- The head row shows a focus ring in `--accent-line` on `:focus-visible`.

### Appearance

- Every color resolves to a token (`DESIGN-01`).
- Every radius, spacing, duration and easing resolves to a token (`DESIGN-01`).
- Font sizes are written as literals, which `DESIGN-01` does not govern.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- The group sits in the document flow on `--glass-1` with a `--line` border, and
  hand-writes no `backdrop-filter` (`DESIGN-06`, `DESIGN-07`).
- The settled label is rendered in `--fg-muted` and lifts to `--fg` on hover, so
  a settled group recedes.
- The ticker label is rendered in `--fg` while a tool runs, so the active group
  reads as foreground.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only styling, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the disclosure body, the chevron and the head
  row's hover, above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and it is an opt-in: the parameter is a switch on a row
  that would otherwise render as an ordinary summary, so `AI-03` requires it to
  be called `Ai`.
- **Demo page** — `DRYL.Website/Components/Examples/ToolCallGroup/Running.razor`
  and `.../ToolCallGroup/Summary.razor`.
- **`ComponentCatalog`** — registered as `"Tool Call Group"` / `tool-call-group`
  in `DRYL.Website/Components/ComponentCatalog.cs`.
