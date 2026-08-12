# DrylToolCall

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylToolCall.razor
              code/DRYL.Components/Components/AI/DrylToolCall.razor.css

## User Story

As a Blazor developer building an agent UI, I want to render one tool call as a
card that shows its name, its live status and — on demand — its arguments and
result, so that a user can follow what the agent is doing without me designing a
status vocabulary of my own.

## Description

`DrylToolCall` visualises a single agent tool or function call. Collapsed, it is
one row: an icon, the tool name in monospace, a status pill, and a chevron.
Expanded, it reveals the call's arguments and its result, each rendered as JSON
in a `DrylCodeBlock`, or a danger `DrylAlert` when the call failed.

The status is not a vocabulary of its own — it is the shared `AiState`, which is
what lets a tool call, a streaming answer and a generated table all read as the
same system. `Ai` is a switch, not the card's subject: with `AiState.None` the
card still renders its name, its arguments and its result, and simply carries no
AI styling.

The component is a leaf. It does not fetch, poll or own the call; the host passes
the current values on every render. Several stacked inside a `DrylTimeline` make
an agent trace; wrapped in a `DrylToolCallGroup` they collapse into one summary
row.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ToolName` | `string?` | `null` | The name of the tool being called; rendered in `--font-mono`. |
| `Arguments` | `string?` | `null` | The call arguments, typically JSON. Shown in the body when non-empty. |
| `Result` | `string?` | `null` | The call result, typically JSON. Shown when `Error` is empty. |
| `Error` | `string?` | `null` | An error message; when set it replaces the result with a danger alert. |
| `Ai` | `AiState` | `AiState.None` | The opt-in (`AI-03`). Drives the status pill and the aura. |
| `State` | `AiState` | — | **Obsolete.** Delegating alias for `Ai`; removed in `3.0.0`. See `_Api.md`. |
| `Aura` | `AiAura?` | `null` | Aura variant. `null` inherits the surrounding `DrylAiScope`, ultimately `AiAura.Comet`. |
| `DefaultExpanded` | `bool` | `false` | Whether the body starts expanded. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the component's own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes no `EventCallback`: expanding is internal state, and the
call's lifecycle belongs to the host.

## Acceptance Criteria

### Content

- The component renders `ToolName` in the head row.
- The component renders the `Arguments` section only when `Arguments` is
  non-empty.
- The component renders the `Result` section only when `Result` is non-empty and
  `Error` is empty.
- The component renders a `DrylAlert` of kind `Danger` when `Error` is non-empty.
- The component renders the `Result` section not at all when `Error` is
  non-empty.
- `Arguments` and `Result` are each rendered through `DrylCodeBlock` with
  `Language="json"`.

### AI state

- `Ai` defaults to `AiState.None`.
- `Ai` accepts exactly the five values of `AiState`.
- The status pill reads "Idle" when `Ai` is `AiState.None`.
- The status pill reads "Running" when `Ai` is `AiState.Thinking`.
- The status pill reads "Streaming" when `Ai` is `AiState.Streaming`.
- The status pill reads "Done" when `Ai` is `AiState.Generated`.
- The status pill reads "Active" when `Ai` is `AiState.Active`.
- The card renders its name unchanged when `Ai` is `AiState.None`.
- The card renders its arguments and its result unchanged when `Ai` is
  `AiState.None` — the parameter is a switch, not a precondition.
- Setting the obsolete `State` alias sets `Ai` to the same value.
- Reading the obsolete `State` alias returns the current value of `Ai`.
- The aura is removed from the surface once `Ai` returns to `AiState.None`
  (`AI-06`).
- The `AiState.Generated` reveal fires once per transition into that state, not
  on every re-render (`AI-07`).
- The aura variant resolves to the explicit `Aura`, otherwise to the surrounding
  `DrylAiScope`'s variant, otherwise to `AiAura.Comet`.
- The AI state is never inherited from a surrounding `DrylAiScope` — a tool
  call's status is a fact about that call.

### Disclosure

- The body starts collapsed when `DefaultExpanded` is `false`.
- The body starts expanded when `DefaultExpanded` is `true`.
- Activating the head row toggles the body.
- The body remains in the DOM while collapsed, so content state inside it
  survives a collapse.
- The root carries `is-open` exactly while the body is expanded.

### Motion

- The body animates open and closed over `--dur-med` with `--ease-in-out`, on
  the `grid-template-rows` track rather than on the content (`DESIGN-11`).

  `DESIGN-12` asks for a `DrylPresence` wrapper around anything that mounts
  conditionally. This body does not mount conditionally any more, so the rule no
  longer has a subject here — it is satisfied by removal of the condition, not by
  the wrapper. The disclosure is preferred over `DrylPresence` because the body
  must keep its content's state across a collapse, and it is what
  `DrylToolCallGroup` and `DrylExpansion` already do.
- The chevron rotates over `--dur-med` with `--ease-in-out` rather than snapping.
- Both transitions are disabled under `prefers-reduced-motion: reduce`.
- The sections inside the body carry no transition of their own: they sit inside
  the animated disclosure, and a result arriving should not animate a second
  time under the first.

### Keyboard and accessibility

- The head row is a `<button type="button">`.
- The head row is reachable by <kbd>Tab</kbd>.
- The head row activates on <kbd>Enter</kbd> and on <kbd>Space</kbd>.
- The head row carries `aria-expanded` reflecting the body's state.
- The head row carries `aria-controls` pointing at the body's `id`.
- The body's `id` is unique per component instance.
- The body carries `role="region"`.
- The body carries `aria-hidden="true"` exactly while collapsed, so assistive
  tech does not read content the user cannot see — it is in the DOM either way.
- The body carries `inert` exactly while collapsed, so nothing inside it can be
  reached by <kbd>Tab</kbd> while it is invisible. `aria-hidden` alone does not
  remove an element from the tab order, and the body holds the copy buttons of
  its code blocks; focusable content inside an `aria-hidden` subtree is a
  contradiction assistive tech cannot resolve (`UX-07`).
- The head row shows a focus ring in `--accent-line` on `:focus-visible`.

### Appearance

- Every color resolves to a token (`DESIGN-01`).
- Every radius, spacing, duration and easing resolves to a token (`DESIGN-01`).
- Font sizes and letter spacing are written as literals, which `DESIGN-01` does
  not govern.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- The card sits in the document flow on `--glass-1` with a `--line` border, and
  hand-writes no `backdrop-filter` (`DESIGN-06`, `DESIGN-07`).
- The accent appears as the icon color and the focus ring only, never as a fill
  of the card (`DESIGN-08`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only styling, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the disclosure body and the chevron, above. Added
  2026-08-11: the body previously sat behind a bare `@if` and had none, which
  `DESIGN-12` does not allow. The `DESIGN-12` note under "Motion" states how the
  rule is met.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and it is an opt-in: the parameter is a switch on a card
  that would otherwise render as an ordinary one, so `AI-03` requires it to be
  called `Ai`.
- **Demo page** — `DRYL.Website/Components/Examples/ToolCall/States.razor` and
  `.../ToolCall/AgentTrace.razor`.
- **`ComponentCatalog`** — registered as `"Tool Call"` / `tool-call` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
