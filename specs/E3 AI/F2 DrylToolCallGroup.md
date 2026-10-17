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

Calls can wait for a human. With `Ai` at `AiState.Proposed` the summary reads
"N awaiting approval" from `PendingCount`, and the group opens by itself once,
so nobody approves a call whose arguments they have not seen. From two pending
calls on, and with the callbacks bound, the summary offers "Approve all" and
"Deny all"; each card inside keeps its own pair. The group decides nothing
itself — it raises the callback and the host resolves the calls.

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
| `PendingCount` | `int` | `0` | Number of calls awaiting a human decision; read while `Ai` is `AiState.Proposed`. |
| `OnApproveAll` | `EventCallback` | — | Raised by the Approve all button. |
| `OnDenyAll` | `EventCallback` | — | Raised by the Deny all button. |
| `ApproveAllLabel` | `string` | `"Approve all"` | Visible text of the Approve all button. |
| `DenyAllLabel` | `string` | `"Deny all"` | Visible text of the Deny all button. |
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
- The collapsed row renders `PendingCount` followed by "awaiting approval" when
  `Ai` is `AiState.Proposed` and `PendingCount` is greater than `0`.
- The collapsed row renders the count when `Ai` is `AiState.Proposed` and
  `PendingCount` is `0`.

### Status

- The status pill reads "Running" when the group is active and `Ai` is not
  `AiState.Streaming`.
- The status pill reads "Streaming" when `Ai` is `AiState.Streaming`.
- The status pill reads "Awaiting approval" when `Ai` is `AiState.Proposed`.
- The status pill reads "Error" when `HasError` is `true`, the group is not
  active and `Ai` is not `AiState.Proposed`.
- The status pill reads "Done" when the group is neither active, nor in error, nor
  at `AiState.Proposed`.

### AI state

- `Ai` defaults to `AiState.None`.
- `Ai` accepts exactly the six values of `AiState`.
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
- `AiState.Proposed` renders the shared resting proposal look of `DrylAuraElements`:
  the frozen aurora edge, with no comet and no halo.
- A change from `AiState.Proposed` straight to `AiState.Generated` plays the shared
  accept wave of `DrylAuraElements` before the completion wash.

### Approval

- The component renders an Approve all button when `Ai` is `AiState.Proposed`,
  `PendingCount` is at least `2` and `OnApproveAll` is bound.
- The component renders a Deny all button when `Ai` is `AiState.Proposed`,
  `PendingCount` is at least `2` and `OnDenyAll` is bound.
- The component renders neither bulk button when `PendingCount` is below `2`.
- The component renders neither bulk button when `Ai` is not
  `AiState.Proposed`.
- The bulk buttons sit outside the collapsible body, under the head row.
- Activating the Approve all button fires `OnApproveAll` once.
- Activating the Deny all button fires `OnDenyAll` once.
- Activating either bulk button does not toggle the body.
- Once a bulk decision was sent, activating either bulk button again while they
  fade out sends nothing further.
- The Approve all button reads `ApproveAllLabel`, defaulting to "Approve all".
- The Deny all button reads `DenyAllLabel`, defaulting to "Deny all".
- The Approve all button is a `DrylButton` of `ButtonVariant.Primary` in
  `ButtonSize.Small`.
- The Deny all button is a `DrylButton` of `ButtonVariant.Ghost` in
  `ButtonSize.Small`.

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
- The body is revealed when `Ai` transitions into `AiState.Proposed`.
- That reveal happens once per transition: the user may collapse the group again
  while `Ai` stays `AiState.Proposed`.
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
- The bulk buttons enter and leave through `DrylPresence` with
  `PresenceTransition.Fade` (`DESIGN-12`).

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
- The bulk buttons follow the head row in the tab order.
- After either bulk button is activated, focus moves to the head row, so focus
  is not lost when the buttons leave.
- Moving focus is best effort: when the host removed the group in its callback,
  or the connection is gone, the click completes without an error.

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
- **Enter/exit animation** — the disclosure body, the chevron, the head row's
  hover and the bulk buttons' `DrylPresence`, above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and it is an opt-in: the parameter is a switch on a row
  that would otherwise render as an ordinary summary, so `AI-03` requires it to
  be called `Ai`.
- **Demo page** — `DRYL.Website/Components/Examples/ToolCallGroup/Running.razor`,
  `.../ToolCallGroup/Summary.razor` and `.../ToolCallGroup/Approval.razor`.
- **`ComponentCatalog`** — registered as `"Tool Call Group"` / `tool-call-group`
  in `DRYL.Website/Components/ComponentCatalog.cs`.
