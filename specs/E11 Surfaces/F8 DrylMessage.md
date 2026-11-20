# DrylMessage

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylMessage.razor
              code/DRYL.Components/Components/Surfaces/DrylMessage.razor.css
              code/DRYL.Components/Components/Surfaces/MessageRole.cs

## User Story

As a Blazor developer building a chat, I want one message component that lays out
a user turn, an assistant turn or a system line, shows who wrote it and when, and
lets an assistant answer type, stream and settle visibly, so that a conversation
reads correctly without me styling bubbles, avatars or a streaming reveal.

## Description

`DrylMessage` is one entry of a `DrylChat` log. Its `Role` decides the shape:

- **`MessageRole.User`** — a row aligned to the trailing edge: avatar, then a body
  with meta line and an accent-tinted bubble.
- **`MessageRole.Assistant`** (the default) — the same row aligned to the leading
  edge, on a glass bubble. Its default avatar icon is `Sparkle`.
- **`MessageRole.System`** — a centred, muted line with no avatar, no meta and no
  bubble. Only `ChildContent` is shown.

The bubble's content is chosen in one order: `Typing` shows three bouncing dots;
otherwise `Text` with `Markdown` renders through `DrylMarkdown`; otherwise `Text`
renders as plain text; otherwise `ChildContent`. `Text` exists so a bound,
streaming string can drive the bubble.

**Plain-text streaming reveal.** While the effective AI state is
`AiState.Streaming` and the bubble shows plain `Text`, each update's newly arrived
text is split into "word plus trailing whitespace" tokens, each rendered as its own
`.stream-token` span inside a `.stream-text` span. Only the new spans mount, so
only the new words animate in — blurring to sharp and cooling from `--ai-a`
through `--ai-b` to `--fg` — while a glowing caret rides the tip. Text that no
longer extends what was shown (a restarted stream) is re-tokenised from scratch.
Leaving `Streaming` drops the tokens and shows the text plainly. A Markdown bubble
cannot be tokenised this way; it uses the newest-block reveal on `.chat-bubble`
instead, described in [`F7 DrylMarkdown.md`](F7%20DrylMarkdown.md).

The aura sits on the **bubble**, not on the row: in an AI state the bubble carries
the shared aura classes and renders `DrylAuraElements`. The `DrylMarkdown` inside
is deliberately given no `Ai`, so the text is not framed a second time.

## Public API

Namespace: `DRYL.Components`. Inherits `DrylAiAware`; implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Role` | `MessageRole` | `MessageRole.Assistant` | Author role; drives alignment and styling. |
| `Author` | `string?` | `null` | Display name above the bubble; also the default avatar's name. |
| `Timestamp` | `string?` | `null` | Pre-formatted time shown next to the author. |
| `ShowAvatar` | `bool` | `true` | Shows an avatar beside the bubble. |
| `Avatar` | `RenderFragment?` | `null` | Custom avatar content; replaces the default `DrylAvatar`. |
| `AvatarSrc` | `string?` | `null` | Image URL for the default avatar. |
| `AvatarIcon` | `string?` | `null` | Icon for the default avatar; falls back per role. |
| `Typing` | `bool` | `false` | Shows the typing indicator instead of any content. |
| `Text` | `string?` | `null` | Text content; wins over `ChildContent`. |
| `Markdown` | `bool` | `false` | Renders `Text` through `DrylMarkdown`. |
| `ChildContent` | `RenderFragment?` | `null` | Bubble content, used when `Text` is `null`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the row's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the row (captures unmatched values). |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. AI state of the bubble. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Aura variant; `null` inherits a surrounding `DrylAiScope`. |

`MessageRole` is a top-level enum in
`code/DRYL.Components/Components/Surfaces/MessageRole.cs`; its members are listed
in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Row structure

- A `MessageRole.User` message renders a `div` root carrying `chat-msg` and
  `chat-msg--user`.
- A `MessageRole.Assistant` message renders a `div` root carrying `chat-msg` and
  `chat-msg--assistant`.
- A `MessageRole.System` message renders a `div` root carrying `chat-msg` and
  `chat-msg--system`, holding one `.chat-system-line` span with `ChildContent`.
- A system message renders no avatar, no meta line and no bubble.
- A system message ignores `Text`, `Markdown` and `Typing`.
- `Role` defaults to `MessageRole.Assistant`.
- A user row lays out from the trailing edge; an assistant row from the leading
  edge; a system row is centred.
- `Class` is merged onto the row's own classes and never replaces them, for every
  role.
- `AdditionalAttributes` is splatted onto the row, for every role.

### Avatar

- `ShowAvatar` defaults to `true`.
- While `ShowAvatar` is `true`, the row holds a `.chat-msg-avatar` element before
  the body.
- While `ShowAvatar` is `false`, no `.chat-msg-avatar` element is rendered.
- `Avatar`, when set, is rendered in the avatar slot instead of the default.
- The default avatar is a `DrylAvatar` of `AvatarSize.Medium` given `Author` as
  its `Name`, `AvatarSrc` as its `Src` and the resolved icon as its `Icon`, so its
  own image → initials → icon order applies.
- The resolved icon is `AvatarIcon` when set, `Sparkle` for an assistant message
  without one, and none for a user message without one.

### Meta line

- A `.chat-msg-meta` line is rendered above the bubble when `Author` or
  `Timestamp` is non-empty, and not otherwise.
- `Author` renders in `.chat-msg-author`, coloured `--fg-muted`.
- `Timestamp` renders in `.chat-msg-time`, coloured `--fg-dim` and set in
  `--font-mono`.
- A user message's body — meta line and bubble — aligns to the trailing edge.

### Bubble

- The bubble is a `div` carrying `chat-bubble` and `chat-bubble--user` or
  `chat-bubble--assistant`.
- The bubble holds one `.chat-bubble-content` element that receives the content.
- An assistant bubble is filled `--glass-2` with a `--line` border.
- A user bubble is filled `--accent-soft` with an `--accent-line` border.
- The bubble's corners are `--r-md`, with the corner nearest the avatar drawn at
  `--r-xs`.
- The bubble is padded by `--sp-3` / `--sp-4` and its text is `--fg`.
- A long unbroken string wraps inside the bubble instead of widening it.
- Stacked blocks inside `.chat-bubble-content` are separated by `--sp-3`; a single
  block gets no extra margin.
- The component branches on no colour mode (`DESIGN-02`).

### Content precedence

- While `Typing` is `true`, the bubble shows the typing indicator and nothing
  else.
- With `Typing` `false`, `Markdown` `true` and `Text` set, the bubble renders
  `Text` through a `DrylMarkdown`.
- That `DrylMarkdown` is given no `Ai`, so it draws no aura of its own.
- With `Typing` `false`, `Markdown` `false` and `Text` set, the bubble renders
  `Text` as plain text.
- With `Text` `null`, the bubble renders `ChildContent`.
- `Markdown` has no effect while `Text` is `null`.

### Typing indicator

- The typing indicator is a `.chat-typing` span of three dots coloured
  `--fg-muted`.
- The dots bounce in a continuous staggered loop on `--ease-in-out`.
- Under `prefers-reduced-motion: reduce`, the dots stand still at reduced opacity.
- The indicator carries `aria-label="Assistant is typing"`.

### Plain-text streaming reveal

- While the effective state is `AiState.Streaming`, `Markdown` is `false`,
  `Typing` is `false` and `Text` is set, the bubble renders the text as
  `.stream-token` spans inside one `.stream-text` span.
- Each token is a word with its trailing whitespace; a leading whitespace run is
  a token of its own.
- The spans are written with no markup whitespace between them, so the rendered
  text equals `Text`.
- An update that extends the previous `Text` appends spans for the new text only;
  the spans already shown are left untouched and do not replay.
- The new spans of one update carry a rising stagger index in `--stream-i`,
  capped, so a burst reads as a wave.
- An update whose `Text` does not start with the previously shown text discards
  all spans and tokenises the whole text again.
- Each new token animates in with `stream-token-in` on `--dur-slow` /
  `--ease-out`: from transparent and blurred to sharp, from `--ai-a` through
  `--ai-b` to `--fg`.
- `.stream-text` ends in a glowing caret drawn from `--ai-a` and `--ai-b`, which
  pulses continuously.
- Leaving `AiState.Streaming` — or setting `Markdown` or `Typing` — drops the
  spans and the bubble shows `Text` as plain text.
- Under `prefers-reduced-motion: reduce`, tokens appear without animation and the
  caret stays as a static marker.
- A Markdown bubble in `AiState.Streaming` carries the newest-block reveal of the
  `.chat-bubble.ai-aura.ai-streaming .md-content` rules instead.

### AI mode

- `Ai` defaults to `AiState.None`, and the bubble then carries no aura class and
  no aura markup (`AI-03`).
- An explicit `Ai` other than `AiState.None` wins over a surrounding
  `DrylAiScope`; an `Ai` of `AiState.None` inherits the scope's state.
- An explicit `Aura` wins over the scope's variant; with neither, the variant is
  `AiAura.Comet`.
- In an AI state the bubble — not the row — carries the shared aura classes from
  `AiAuraCss.Append`, and `DrylAuraElements` is the bubble's first child (`AI-02`).
- The aura stays mounted and fades after the effective state drops to
  `AiState.None`.
- The one-shot `Generated` wash is re-keyed on each entry into
  `AiState.Generated` (`AI-07`).
- A system message renders no aura in any state.

### Keyboard and accessibility

- The message adds no tab stop, role or key handling of its own.
- The message carries no live region of its own; announcement is the host log's
  job (`DrylChat`'s `role="log"`).

## Recorded debt

- **Messages do not animate in.** A message added to a log, and the swap from the
  typing dots to the answer, appear without a transition (`DESIGN-11`; `DESIGN-12`
  names list items explicitly). Only the AI paths animate: the aura, and the
  streamed words.
- **The typing indicator's name is on a generic `span`.** `aria-label` on an
  element with no role is not reliably announced, and the string is hard-coded
  English with no parameter to replace it (`UX-04`).
- **Literal padding and radius.** `.chat-typing` pads with `2px 0`; the typing
  dots are rounded with `50%` instead of `--r-pill` (`DESIGN-01`).
- **Literal stagger delays.** The typing dots' second and third
  `animation-delay` are literals, and `.stream-token`'s delay multiplies the index
  by a literal step rather than a token. Neither is caught by
  `check-motion-tokens.mjs`, which reads shorthands (`DESIGN-10`).

### Recorded gaps — not deviations

- `.chat-msg-body` and `.chat-typing` use a literal `gap`, and the meta line and
  system line literal font sizes; no token category covers either.
- The typing indicator's label says "Assistant" whatever the `Role` and `Author`
  of the message showing it.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/chat` in dark and light (2026-11-20)
- **Enter/exit animation** — **partly**. The AI aura animates in and out through
  `AuraLifecycle`, streamed words materialise one by one, and the typing dots
  loop. The message itself mounts and the typing-to-content swap happens without
  a transition; recorded as debt above.
- **Keyboard and a11y** — no interactive element of its own; content stays in the
  host log's live region. The typing indicator's naming is recorded as debt.
- **AI mode** — **yes**. `DrylMessage` inherits `DrylAiAware`; the bubble is a
  bounded surface at rest, so it composes the full frame (`AI-08`), and the
  plain-text streaming reveal is the library's word-level streaming signal.
- **Demo page** — `/components/chat`, built from
  `DRYL.Website/Components/Pages/DemoChat.razor` with the examples
  `Chat/LiveConversation` (streaming `Text`) and `Chat/MessageStates` (system,
  typing, streaming and generated messages).
- **`ComponentCatalog`** — no entry of its own. The `"Chat"` / `chat` entry
  (`ClassName` `"DrylChat"`) in `DRYL.Website/Components/ComponentCatalog.cs` is
  the page that documents it (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`Message_merges_consumer_class_without_clobbering`). Role layout, content
  precedence and the streaming tokeniser have no bUnit test.
