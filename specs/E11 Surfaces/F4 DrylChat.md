# DrylChat

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylChat.razor
              code/DRYL.Components/Components/Surfaces/DrylChat.razor.css

## User Story

As a Blazor developer adding an assistant to my app, I want a conversation panel
with a scrolling, announced message log and a pinned place for the composer, so
that I only supply the messages and the send handler and the panel keeps the
newest message in view.

## Description

`DrylChat` is the frame of a conversation. It renders a glass panel of a fixed
height with two parts: a scrollable `.chat-scroll` log that receives
`ChildContent` (typically `DrylMessage` elements) and, when `Footer` is supplied, a
`.chat-footer` pinned below it (typically a `DrylChatComposer`). The log is a
polite live region (`role="log"`), so assistive technology announces what is
appended to it.

With `AutoScroll` on, the component scrolls the log to its end after **every**
render of the chat, through `dryl.chat.scrollToEnd`. Because the parent re-renders
the chat whenever its message list changes, a streaming answer stays in view as it
grows.

The panel is AI-aware: it inherits `DrylAiAware` and composes the shared aura on
the whole panel. Individual messages carry their own `Ai`; the two are
independent.

## Public API

Namespace: `DRYL.Components`. Inherits `DrylAiAware`; implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The message log content. |
| `Footer` | `RenderFragment?` | `null` | Pinned footer content, typically a `DrylChatComposer`. |
| `Height` | `string` | `"480px"` | CSS height of the panel. |
| `AutoScroll` | `bool` | `true` | Scrolls the log to its end after each render. |
| `AriaLabel` | `string` | `"Conversation"` | Accessible name of the log. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the panel's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the panel (captures unmatched values). |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. AI state of the whole panel. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Aura variant; `null` inherits a surrounding `DrylAiScope`. |

## Acceptance Criteria

### Structure

- The component renders exactly one `div` root carrying the `chat` class.
- The root holds a `.chat-scroll` element that receives `ChildContent`.
- The root holds a `.chat-footer` element after the log exactly while `Footer` is
  not `null`, and renders `Footer` inside it.
- `Height` is written on the root as the `--chat-height` custom property, and the
  panel takes that height.
- The log fills the height the footer leaves and scrolls vertically when its
  content is taller.
- The footer keeps its natural height and never scrolls with the log.
- `Class` is merged onto the root's own classes and never replaces them.
- `AdditionalAttributes` is splatted onto the root.

### Auto-scroll

- `AutoScroll` defaults to `true`.
- After each render of the chat while `AutoScroll` is `true`, the component calls
  `dryl.chat.scrollToEnd` on the log, which sets its scroll position to the end.
- While `AutoScroll` is `false`, the component makes no JS call.
- A call on a closing circuit raises nothing.

### Surface

- The panel's fill is `--glass-1`.
- The panel's border is `--line`.
- The panel's corner radius is `--r-lg`.
- The panel's shadow is `--shadow-md`.
- The log's messages are spaced by `--sp-4` and the log is padded by `--sp-5`.
- The footer is padded by `--sp-3` / `--sp-4` and separated from the log by a
  `--line-soft` rule.
- The component branches on no colour mode (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`, and the root then carries no aura class and no
  aura markup (`AI-03`).
- An explicit `Ai` other than `AiState.None` wins over a surrounding
  `DrylAiScope`; an `Ai` of `AiState.None` inherits the scope's state.
- An explicit `Aura` wins over the scope's variant; with neither, the variant is
  `AiAura.Comet`.
- In an AI state the root carries the shared aura classes from `AiAuraCss.Append`
  and `DrylAuraElements` is its first child (`AI-02`).
- The aura stays mounted and fades after the effective state drops to
  `AiState.None`.
- The one-shot `Generated` wash is re-keyed on each entry into
  `AiState.Generated` (`AI-07`).
- The panel's state is not passed to the messages in it.
- Disposing the chat disposes its aura lifecycle.

### Keyboard and accessibility

- The log carries `role="log"`.
- The log carries `aria-live="polite"`, so appended messages are announced without
  interrupting (`UX-04`).
- The log carries `AriaLabel` as its `aria-label`.
- `AriaLabel` defaults to `"Conversation"`.
- The panel adds no tab stop and no key handling of its own; the footer's controls
  keep their own order.

## Recorded debt

- **Hand-written frost.** The `.chat` rule in `DrylChat.razor.css` declares
  `backdrop-filter: blur(var(--glass-blur))` (and its `-webkit-` twin) instead of
  `--glass-fx-flow`, the in-flow tier (`DESIGN-07`, `DESIGN-06`). It is one of the
  pre-existing hits the `DESIGN-07` check names, and it charges the panel a real
  blur that every `DrylCard` in the flow no longer pays.
- **Auto-scroll ignores the reader.** The log is pinned to its end after every
  render, wherever the user had scrolled. Reading back through a conversation
  while an answer streams is not possible with `AutoScroll` on.
- **The footer snaps.** `Footer` is mounted under a bare `@if`; a consumer who
  toggles it sees the footer appear and disappear without a transition
  (`DESIGN-12`).
- **No enter animation for the panel or its log.** The panel mounts without a
  transition; new log entries are animated only by what `DrylMessage` itself does
  (`DESIGN-11`).

### Recorded gaps — not deviations

- `Height` is a free CSS string and its default is a literal, repeated as the
  fallback of `var(--chat-height, …)` in `DrylChat.razor.css`. No token covers a
  component height.
- A consumer `style` passed through `AdditionalAttributes` is splatted after the
  component's own `style`, so it replaces it — and with it `--chat-height`. A
  consumer who styles the root must restate the height.
- `dryl.chat.scrollToEnd` is called with no guard for a `JSException`; only a
  disconnected circuit is caught.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/chat` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of the panel (it is placed in
  the flow, not mounted conditionally); the AI aura animates in and out through
  `AuraLifecycle`, and the footer's unanimated toggle is recorded as debt.
- **Keyboard and a11y** — `role="log"`, `aria-live="polite"` and a configurable
  `aria-label` on the log; no own tab stop. The scroll region carries no
  `tabindex`, so keyboard scrolling of the log relies on the browser making
  scrollers focusable.
- **AI mode** — **yes**. `DrylChat` inherits `DrylAiAware` and composes
  `DrylAuraElements` and `AiAuraCss.Append` on the panel, a bounded surface
  (`AI-08`). The `ComponentCatalog` row carries the AI flag **`false`**, which
  disagrees with the code (`REL-04`).
- **Demo page** — `/components/chat`, built from
  `DRYL.Website/Components/Pages/DemoChat.razor` with the examples
  `Chat/LiveConversation` (`Height`, `Footer`, auto-scroll) and
  `Chat/MessageStates` (`Height="auto"`, `AutoScroll="false"`). No example sets the
  panel's `Ai`.
- **`ComponentCatalog`** — `"Chat"` / `chat`, category `Surfaces`, `ClassName`
  `"DrylChat"`, AI flag `false`, in `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — none dedicated: no bUnit test renders `DrylChat`. Every criterion
  above rests on reading `DrylChat.razor`, `DrylChat.razor.css` and the
  `dryl.chat` module.
