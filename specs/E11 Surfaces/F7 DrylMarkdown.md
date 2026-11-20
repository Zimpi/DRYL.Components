# DrylMarkdown

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylMarkdown.razor

## User Story

As a Blazor developer showing model output, I want to bind a Markdown string —
including one that grows token by token — and get typeset, safe HTML with code
blocks in the library's code surface, so that an LLM answer renders as a document
and never as raw markup or executable HTML.

## Description

`DrylMarkdown` parses `Content` with Markdig (CommonMark plus the advanced
extensions: GFM tables, autolinks, task lists and the rest) and renders the result
inside a `.md` root and a `.md-content` column. Raw HTML in the source is disabled
in the pipeline, so `<script>` or an `<img onerror>` written by a model is escaped
as text rather than executed.

Code blocks are not rendered by Markdig. The document is walked block by block:
every code block becomes a `DrylCodeBlock` (fenced or indented; a fenced block's
info string becomes its `Language`), and every run of other blocks is rendered to
HTML and emitted as one `MarkupString`. Because that HTML carries no CSS-isolation
attribute, the typography lives in global `.md-content` rules in `dryl.css`.

**Streaming.** The component is built to be bound to an accumulating completion.
It re-parses only when `Content` actually changed, so an unrelated re-render costs
no parse. A chunk boundary can split a UTF-16 surrogate pair (half an emoji);
before parsing, unpaired surrogates are dropped, because Markdig's heading
auto-identifier throws on them and the exception would tear down a Blazor Server
circuit. The next chunk completes the pair and the character renders normally.
While the effective AI state is `AiState.Streaming`, the newest block carries the
shared streaming reveal: a hot-to-cold `--ai-a` → `--ai-b` → `--fg` temperature
and a glowing caret at its tip.

**AI mode.** The component inherits `DrylAiAware`. In an AI state the root gains
the shared aura classes, `DrylAuraElements` renders as its first child, and the
bare `.md` root becomes a padded `--glass-1` panel so the ring has a surface to
frame. The padding and fill glide in and out.

## Public API

Namespace: `DRYL.Components`. Inherits `DrylAiAware`; implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Content` | `string?` | `null` | The Markdown source. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the root's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root (captures unmatched values). |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. AI ambient state. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Aura variant; `null` inherits a surrounding `DrylAiScope`. |

`ParseCount` is `internal` (a test seam for the parse memo) and not public API.

## Acceptance Criteria

### Structure

- The component renders exactly one `div` root carrying the `md` class.
- The root holds one `.md-content` element that receives the rendered document.
- `Class` is merged onto the root's own classes and never replaces them.
- `AdditionalAttributes` is splatted onto the root.
- A `null` or empty `Content` renders an empty `.md-content`.

### Rendering

- CommonMark constructs — headings, emphasis, strong, lists, blockquotes, links,
  inline code, thematic breaks — render as their HTML elements.
- GFM tables render as an HTML `table`.
- Every code block in the source renders as a `DrylCodeBlock` in document order,
  between the HTML of the blocks around it.
- A fenced code block's info string is passed to that `DrylCodeBlock` as its
  `Language`.
- A code block's text is passed to `DrylCodeBlock` unrendered, as its `Code`.
- Raw HTML in the source is escaped and shown as text, never inserted as markup.
- Inline HTML in the source is escaped the same way.

### Streaming robustness

- An unpaired high or low UTF-16 surrogate in `Content` is dropped before parsing,
  and rendering does not throw.
- A complete surrogate pair is preserved and renders as its character.
- Growing `Content` one character at a time, through a split surrogate pair,
  re-renders at every step without throwing.
- A render whose `Content` equals the previous `Content` does not re-parse.
- A render whose `Content` differs from the previous one parses exactly once.

### Typography

- Body text and list items are coloured `--fg-muted`; headings, `strong` and
  table headers `--fg`.
- Blocks in `.md-content` are spaced by `--sp-3`, with their own margins removed.
- Links are coloured `--accent-b` with an `--accent-line` underline, and turn
  `--fg` on hover over `--dur-fast` / `--ease-out`.
- Blockquotes carry an `--accent-line` rule on the leading edge, `--fg-dim` text
  and a `--glass-1` fill.
- Inline code is set in `--font-mono` on `--glass-2` with a `--line` border and
  `--r-xs` corners.
- Table cells are padded by `--sp-2` / `--sp-3` and separated by `--line` rules.
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
- In an AI state the root is filled with `--glass-1` and padded by `--sp-4`.
- Outside an AI state the root has no fill and no padding.
- The root's padding and fill transition on `--dur-slow` / `--ease-out`, in both
  directions.
- The aura stays mounted and fades after the effective state drops to
  `AiState.None`.
- The one-shot `Generated` wash is re-keyed on each entry into
  `AiState.Generated` (`AI-07`).
- `AiState.Proposed` renders the shared resting proposal look and its visually
  hidden description; a change straight to `AiState.Generated` adds
  `ai-from-proposed` and plays the accept wave.
- The `Ai` state is not passed on to the `DrylCodeBlock`s the component renders.

### Streaming reveal

- While the root carries `ai-aura` and `ai-streaming`, the last top-level block of
  `.md-content` — unless it is a code block — is painted with a vertical gradient
  from `--ai-a` at the bottom through `--ai-b` to `--fg`.
- That block's brightness breathes continuously on `--dur-slow` /
  `--ease-in-out`.
- That block ends in a glowing caret drawn from `--ai-a` and `--ai-b`, which
  pulses continuously.
- When a new block starts, the reveal moves to it and the finished block returns
  to normal text.
- Under `prefers-reduced-motion: reduce`, the block is painted in `--fg-muted`
  without the gradient, nothing breathes, and the caret stays as a static marker.
- Under `prefers-reduced-motion: reduce`, the root's padding and fill change
  without a transition.

### Keyboard and accessibility

- The component adds no tab stop, role or key handling of its own; links and the
  code blocks' controls are reachable in document order.
- Headings render as real heading elements, so the document's outline is exposed
  to assistive technology.

## Recorded debt

- **Literal padding on inline code.** `.md-content :not(pre) > code` pads with
  `0.1em 0.4em` (`DESIGN-01`).
- **The aura invents a surface (`AI-08`, open question).** At `AiState.None` the
  `.md` root has no fill, border or padding — by `AI-08`'s binding test it has no
  surface. In AI mode `.md.ai-aura` gives it one (`--glass-1`, `--sp-4`) so that
  the full frame has something to trace. That is the shape `AI-08` warns against;
  `DrylMarkdown` is not among the eight components I45 resolved, and the question
  is open rather than decided.

### Recorded gaps — not deviations

- **Link targets are not filtered — a security gap.** The pipeline disables raw
  HTML only; it adds no URL-scheme filter, and Markdig 1.6.0 carries none of its
  own. Measured on 2026-11-20 against this exact pipeline
  (`UseAdvancedExtensions().DisableHtml()`): `[x](javascript:alert(1))`, the
  mixed-case `JaVaScRiPt:` form and the autolink `<javascript:alert(1)>` all
  render as `<a href="javascript:…">`, `![i](javascript:…)` as an `img` `src`,
  and a `data:text/html` target passes unchanged. Model-authored Markdown can
  therefore place a clickable script link in the host app, which contradicts the
  pipeline's own comment that untrusted Markdown cannot inject active markup.
  No test covers link targets.
- **Heading identifiers can collide.** The advanced extensions give every heading
  an auto-generated `id`; two `DrylMarkdown` instances on one page with the same
  heading text produce duplicate ids.
- **The whole document is re-parsed per change.** The memo prevents re-parses of
  unchanged content, not the full re-parse of a growing stream on every chunk.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/markdown` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of the component: it does not
  mount conditionally. Content changes are not animated in general; the AI path is
  — the panel's padding and fill glide in and out with the aura, and a streaming
  block carries the shared cooling reveal and caret (`DESIGN-11`). Code blocks
  inserted mid-stream appear without a transition.
- **Keyboard and a11y** — semantic HTML out of Markdig (headings, lists, tables,
  links); no own tab stop; the code blocks bring their own copy control. The
  component sets no live region — that belongs to the host (`DrylChat`'s log).
- **AI mode** — **yes**. `DrylMarkdown` inherits `DrylAiAware` and composes
  `DrylAuraElements` and `AiAuraCss.Append`; it is the library's natural sink for
  model output. The `ComponentCatalog` row carries the AI flag **`false`**, which
  disagrees with the code (`REL-04`); see the AI-08 debt above.
- **Demo page** — `/components/markdown`, built from
  `DRYL.Website/Components/Pages/DemoMarkdown.razor` with the examples
  `Markdown/RichContent`, `Markdown/AiStreaming` and `Markdown/Xss`.
- **`ComponentCatalog`** — `"Markdown"` / `markdown`, category `Surfaces`,
  `ClassName` `"DrylMarkdown"`, AI flag `false`, in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/DrylMarkdownTests.cs` (rendering,
  surrogate robustness, parse memo), `tests/DRYL.Components.Tests/AiProposedTests.cs`
  and `tests/DRYL.Components.Tests/AiProposedSweepTests.cs` (proposal, accept
  wave). Code-block delegation and HTML escaping have no dedicated test.
