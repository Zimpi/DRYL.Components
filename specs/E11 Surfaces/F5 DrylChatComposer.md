# DrylChatComposer

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylChatComposer.razor
              code/DRYL.Components/Components/Surfaces/DrylChatComposer.razor.css

## User Story

As a Blazor developer building a chat, I want a message box that grows with what
the user types, sends on `Enter` and keeps `Shift`+`Enter` for a new line, so that
I only handle the text that was sent and never wire key handling or textarea
sizing myself.

## Description

`DrylChatComposer` renders a `.chat-composer` wrapper holding a one-row
`textarea` and an icon-only `DrylButton` with an up-arrow. It is meant for a
`DrylChat` footer, and the agents package's canvas dock uses it too.

The draft is two-way bound through `Value` / `ValueChanged`, updated on every
`input`. Sending — by `Enter` or the button — clears the draft, raises
`ValueChanged` with an empty string, then raises `OnSend` with the text that was
sent, and finally re-measures the textarea. A draft that is empty or whitespace
only cannot be sent, and neither can a `Disabled` composer.

`Enter` handling and the growing textarea live in `dryl.chat` in `dryl.js`, not
in Blazor, because the key's default (a newline) must be suppressed in the same
event: `dryl.chat.attachComposer` binds a `keydown` listener that calls back into
`SubmitFromJs`, and an `input` listener that sets the textarea's height to its
content. A composer attached while hidden — in a collapsed panel, say — cannot be
measured; it keeps its one-row height and measures itself once a `ResizeObserver`
reports it visible. The textarea also opts into the library's input echo guard
(`data-dryl-input`).

The composer is AI-aware, but declares its own `Ai` and `Aura` instead of
inheriting `DrylAiAware`.

## Public API

Namespace: `DRYL.Components`. Implements `IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `string?` | `null` | The draft text. Supports `@bind-Value`. |
| `ValueChanged` | `EventCallback<string>` | — | Fires when the draft changes, including the clear on send. |
| `OnSend` | `EventCallback<string>` | — | Fires with the sent text when the user sends. |
| `Placeholder` | `string?` | `"Type a message…"` | Placeholder of the textarea. |
| `Disabled` | `bool` | `false` | Disables the textarea and the send button. |
| `AriaLabel` | `string` | `"Message"` | Accessible name of the textarea. |
| `Ai` | `AiState` | `AiState.None` | AI state of the composer. |
| `Aura` | `AiAura?` | `null` | Aura variant; `null` inherits a surrounding `DrylAiScope`. |

The component declares no `Class` and no `AdditionalAttributes`.

Public members beyond the parameters:

| Member | Kind | Purpose |
|---|---|---|
| `FocusAsync()` | `ValueTask` | Moves keyboard focus into the textarea. |
| `SubmitFromJs()` | `[JSInvokable] Task` | Called from `dryl.js` on `Enter`. Public because interop requires it. |
| `DisposeAsync()` | `ValueTask` | Releases the key and input listeners and the object reference. |

## Acceptance Criteria

### Structure

- The component renders one `div` root carrying `chat-composer`.
- The root holds a `textarea` carrying `chat-composer-input` and `rows="1"`.
- The root holds one `DrylButton` after the textarea, of
  `DrylButton.ButtonVariant.Primary`, with the `ArrowUp` leading icon and no text.
- The textarea shows `Value`.
- The textarea shows `Placeholder` while empty.
- The textarea carries `data-dryl-input`, so the library's input echo guard
  protects it from late server echoes.

### Draft binding

- Every `input` on the textarea writes its text to `Value` and raises
  `ValueChanged` with it.

### Sending

- Clicking the send button sends the draft.
- `Enter` in the textarea sends the draft and inserts no newline.
- `Shift`+`Enter` inserts a newline and does not send.
- `Enter` pressed while an IME composition is in progress does not send.
- Sending raises `ValueChanged` with an empty string, then `OnSend` with the text
  that was in the draft, in that order.
- The text handed to `OnSend` is the draft as typed, not trimmed.
- After sending, the textarea is re-measured, so it shrinks back to one row.
- A draft that is `null`, empty or whitespace only is not sent: neither callback
  fires.
- A `Disabled` composer sends nothing, by button or by key.

### Disabled state

- `Disabled` defaults to `false`.
- `Disabled` sets the textarea's native `disabled` attribute.
- The send button is disabled while `Disabled` is `true` or the draft is empty or
  whitespace only.
- A disabled textarea shows its text in `--fg-dim` with a `not-allowed` cursor.

### Growing textarea

- After the first render in the browser, the component calls
  `dryl.chat.attachComposer` with the textarea and an object reference to itself.
- On every `input`, the textarea's height is set to its content's height.
- The textarea stops growing at a fixed maximum height and scrolls beyond it.
- A composer attached while hidden keeps its one-row height rather than collapsing
  to zero, and takes its content's height once it becomes visible.
- The textarea cannot be resized by the user.

### Surface and focus

- The wrapper's fill is `--glass-1`.
- The wrapper's border is `--line-strong`, its corners `--r-md`, and it is padded
  by `--sp-2` with `--sp-2` between textarea and button.
- While anything inside the wrapper has focus, its border turns `--accent-line`,
  transitioning on `--dur-fast` / `--ease-out`.
- The textarea draws no outline and no background of its own; the wrapper's
  border is its focus indicator.
- The textarea's text is `--fg` in `--font-sans`; its placeholder `--fg-dim`.
- The component branches on no colour mode (`DESIGN-02`).

### Focus

- `FocusAsync` moves focus into the textarea.
- `FocusAsync` on a closed circuit or during static prerender raises nothing.

### AI mode

- `Ai` defaults to `AiState.None`, and the wrapper then carries no aura class and
  no aura markup (`AI-03`).
- An explicit `Aura` wins over a surrounding `DrylAiScope`'s variant; with
  neither, the variant is `AiAura.Comet`.
- The state rendered is `Ai` as given: a surrounding `DrylAiScope`'s state is not
  inherited.
- In an AI state the wrapper carries the shared aura classes from
  `AiAuraCss.Append` and `DrylAuraElements` is its first child (`AI-02`).
- The aura stays mounted and fades after `Ai` drops to `AiState.None`.
- The one-shot `Generated` wash is re-keyed on each entry into
  `AiState.Generated` (`AI-07`).

### Disposal

- Disposing an attached composer calls `dryl.chat.detachComposer`, which removes
  both listeners and disconnects a pending `ResizeObserver` (`CODE-05`).
- Disposing a composer that never attached makes no JS call.
- Detaching on a closing circuit, for an element already gone, or in a statically
  rendered component raises nothing.
- Disposing the composer disposes its object reference and its aura lifecycle.

### Keyboard and accessibility

- The textarea and the send button are native controls, reachable with `Tab` in
  that order (`UX-01`).
- The textarea carries `AriaLabel` as its `aria-label`; `AriaLabel` defaults to
  `"Message"`.
- The send button carries `aria-label="Send message"`.

## Recorded debt

- **The send button is icon-only without a tooltip.** It is named by
  `aria-label` but not wrapped in a `DrylTooltip`, so a pointer user gets no
  label (`UX-05`). Its name is hard-coded English with no parameter.
- **The scope's state is not inherited.** `Aura` resolves through
  `AiScope.ResolveAura`, but the aura is driven from the raw `Ai`, so a composer
  inside a `DrylAiScope` stays dark. `AiScopeInheritanceTests` carries it on its
  accounted-for list as "I32 follow-up — spec first"; this spec records the
  current behaviour, and the change is still to be decided.
- **Literal padding.** `.chat-composer-input` pads with `8px 6px`
  (`DESIGN-01`).
- **A thin focus indicator.** The textarea suppresses its outline; the
  replacement is a border-colour change on the wrapper (`UX-02` allows it), with
  no glow ring like the library's other text inputs.
- **No `Class` and no attribute pass-through.** The component captures no
  unmatched attributes, so a consumer `class`, `id` or `data-*` on
  `<DrylChatComposer>` fails at render rather than being applied.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/chat` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit: the composer is placed in a
  footer, not mounted conditionally. Its state changes are animated — the focus
  border, the send button's own states — and the AI aura animates in and out
  through `AuraLifecycle` (`DESIGN-11`). The textarea's growth is a direct height
  change, not a transition.
- **Keyboard and a11y** — a native `textarea` and a native button in tab order;
  `Enter` sends, `Shift`+`Enter` breaks the line, IME composition is respected;
  both controls are named. The missing tooltip is recorded as debt.
- **AI mode** — **yes**. The composer declares `Ai` (`AiState`, default
  `AiState.None`) and `Aura` itself and composes `DrylAuraElements` and
  `AiAuraCss.Append`; the wrapper is a bordered surface at rest (`AI-08`). The
  demo sets `AiState.Thinking` while the simulated assistant works.
- **Demo page** — `/components/chat`, example `Chat/LiveConversation`, in
  `DRYL.Website/Components/Pages/DemoChat.razor`. `FocusAsync` and `Disabled`
  alone have no example.
- **`ComponentCatalog`** — no entry of its own; the `"Chat"` / `chat` entry in
  `DRYL.Website/Components/ComponentCatalog.cs` is the page that documents it
  (`REL-04`).
- **Tests** — no dedicated test. It is exercised through the agents package's
  dock: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs` and
  `tests/DRYL.Components.Tests/Agents/Canvas/CanvasDockSelectionTests.cs` type into
  the textarea, click the send button and assert `OnSend`'s text and `Disabled`;
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs` records the
  non-inherited scope state.
