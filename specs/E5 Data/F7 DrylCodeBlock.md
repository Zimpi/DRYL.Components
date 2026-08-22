# DrylCodeBlock

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylCodeBlock.razor
              code/DRYL.Components/Components/Data/DrylCodeBlock.razor.css

## User Story

As a Blazor developer, I want code — my own or a model's — shown on a surface
that names its language, colours its syntax and lets a reader copy it in one
press, so that a snippet in my app is as usable as one in an editor without me
adding a JavaScript highlighter to the page.

## Description

`DrylCodeBlock` is a bordered glass surface in two parts: a header carrying the
language label and a copy button, and a body holding the code with an optional
line-number gutter.

Its defining decision is that **highlighting happens on the server, in C#**.
`SyntaxHighlighter` under `Components/Data/Internal/` lexes the source into
token spans whose classes map onto tokens the palette already has — no new
colours were invented for code, and no highlighting library was added
(`CODE-03`). Every token's text is HTML-encoded before it is wrapped, which is
what makes it safe to render model-authored code as markup.

It is the natural surface for code an LLM produced, and it says so twice: it
takes `Ai` and renders the shared aura vocabulary, and `DrylMarkdown` delegates
every fenced code block to it, so a streamed markdown answer gets this surface
without the consumer asking for it.

The copy button is stateful in a small way: it swaps its icon and its label to a
confirmation for a moment after a successful copy, and swaps back. The swap is
cancellable, so a rapid second press or a disposal never lands late.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Code` | `string` | `""` | The source to display. |
| `Language` | `string?` | `null` | Language label, and the highlighter's language selector. |
| `ShowLineNumbers` | `bool` | `false` | Renders a leading gutter of 1-based line numbers. |
| `Highlight` | `bool` | `true` | Highlights server-side. `false` renders encoded plain text. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the block's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`SyntaxHighlighter` is `internal` and is not part of the public surface; the
languages it understands are listed under "Highlighting" below.

## Acceptance Criteria

### Structure

- The component renders a single root element holding a header and a body.
- The header renders the language label and the copy button.
- The body renders the code inside a `pre` holding a `code` element, so the
  whitespace of the source is preserved by the document rather than by CSS
  alone.
- `ShowLineNumbers` set renders one gutter element before the code.
- `ShowLineNumbers` left `false` renders no gutter element.
- The gutter holds one number per line of `Code`, counting from 1.
- Empty `Code` yields a gutter holding the single number 1, so an empty block
  does not render an empty gutter.
- Line counting treats a CRLF pair as one line break, so a Windows-authored
  snippet is not numbered twice over.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- The root clips its content, so the header's corners are not overdrawn by the
  body's fill.
- The body scrolls horizontally rather than widening the block, so a long line
  does not stretch the page.

### The language label

- The label renders `Language` when one is given.
- The label renders a generic text label when `Language` is null, empty or
  whitespace, so the header never renders an empty slot.
- The label is rendered upper-cased by the stylesheet, so the label's casing
  does not depend on how the consumer spelled the language.

### Highlighting

- `Highlight` left `true` renders the code as token markup produced by the
  server-side highlighter.
- `Highlight` set to `false` renders the code as HTML-encoded plain text.
- Every token's text is HTML-encoded before it is wrapped, so code containing
  markup cannot inject anything into the page.
- An unrecognised `Language` renders HTML-encoded plain text rather than
  failing.
- A null or empty `Language` renders HTML-encoded plain text.
- `Language` is matched case-insensitively and after trimming.
- Common aliases of a language select the same highlighter — for example `cs`,
  `c#` and `dotnet` all select C#.
- The highlighter recognises C#, JavaScript, TypeScript, JSON, CSS, Bash, SQL
  and Python through one generic lexer, and HTML and XML through a second.
- The highlighting is lexical rather than semantic, which is the same
  granularity a browser highlighter uses.
- Each token kind maps onto an existing palette token; the highlighter
  introduces no colour of its own.

### Copying

- Pressing the copy button writes `Code` to the clipboard.
- A successful copy swaps the button's label and its icon to a confirmation.
- The confirmation reverts on its own after a moment.
- A second press before the confirmation reverts supersedes the first, so the
  label does not revert early.
- A failed copy leaves the button unchanged, so the confirmation never claims
  something that did not happen.
- Disposing the component while the confirmation is pending cancels the revert,
  so nothing calls into a disposed component.
- A copy attempted after the circuit has disconnected is abandoned silently
  rather than throwing.
- Copying an empty `Code` writes an empty string rather than failing.

### Keyboard and accessibility

- The copy button is a `DrylButton` and is therefore reachable by `Tab` and
  activated by `Enter` and `Space` without a key handler of its own.
- The copy button carries an accessible label, so it is announced as copying
  code rather than as its icon.
- The line-number gutter is hidden from assistive technology, so the code is
  announced as code rather than interleaved with its line numbers.
- The gutter's numbers are not selectable, so selecting the code and copying it
  by hand does not pick them up.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The root is filled with `--glass-1` and outlined with `--line`.
- The root's corner comes from `--r-md`.
- The header is filled with `--glass-2` and separated from the body by a rule of
  `--line`, so the two parts read as one surface with a seam rather than as two
  surfaces.
- The language label is set in `--fg-dim` and in `--font-mono`.
- The code is set in `--fg` and in `--font-mono`.
- The gutter is set in `--fg-faint`, quieter than the code beside it, and
  separated from it by a rule of `--line`.
- Keyword tokens are drawn in `--accent-a` and type tokens in `--accent-b`.
- String tokens are drawn in `--success` and number tokens in `--warning`.
- Comment tokens are drawn in `--fg-faint` and italicised.
- Punctuation tokens are drawn in `--fg-muted`.
- The token rules reach into the highlighter's markup through `::deep`, because
  that markup is rendered as a `MarkupString` and carries no scope attribute.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The block sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as syntax colour and, in AI mode, as a border and a glow —
  never as the fill of the surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The component renders the shared aura vocabulary rather than a code-specific
  AI treatment (`AI-02`).
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- Re-entering `AiState.Generated` from a different state replays the wash rather
  than being suppressed as a no-op.
- The aura lifecycle's timer is disposed with the component.

## Recorded gaps

- **The copy button never announces that it copied.** Its accessible label is
  fixed, and an `aria-label` overrides the visible text — so the label a screen
  reader reads stays "copy code" while the visible label reads "Copied". The one
  user who most needs the confirmation is the one who does not get it.
- **The scrollable code has no keyboard access.** The body scrolls horizontally
  and carries no `tabindex`, so a keyboard-only user cannot scroll a long line
  into view — the classic WCAG 2.1.1 failure of a scroll container that is not
  focusable. Two nested elements are scrollable, which also means the inner
  scroll can be reached only with a pointer.
- **The code names no language to assistive technology.** `Language` is drawn as
  a visible label in the header but is not carried on the `code` element, so a
  screen reader has no way to know what it is reading.
- **The confirmation's duration is a literal.** The revert waits a
  hand-picked number of milliseconds, written into the component in C#. It is
  not a `--dur-*` violation — that scale governs CSS transitions — but it is the
  one duration in the component with nothing behind it.
- **Streaming code is highlighted as if it were finished.** The highlighter runs
  over whatever `Code` currently holds, so a half-arrived string literal or an
  unclosed comment colours the remainder of the block until the closing
  character arrives. The colours settle correctly; they flicker on the way.
- **The type sizes are literal.** `11px` for the language label and `12.5px` for
  the code and the gutter are written into `DrylCodeBlock.razor.css` with no
  token behind them (`DESIGN-01`). The paddings and gaps *are* tokens, so the
  file is half-converted rather than untouched.
- **No tests of its own.** None of the criteria above is guarded by a test —
  neither the line counting, nor the alias mapping, nor the encoding guarantee,
  which is the one criterion with a security consequence.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--glass-2`,
  `--line`, `--fg`, `--fg-dim`, `--fg-faint`, `--fg-muted`, `--accent-a`,
  `--accent-b`, `--success` and `--warning` are the mode-dependent tokens; the
  component defines no mode-specific rule.
- **Enter/exit animation** — none of its own, and that is the written exception
  `DESIGN-11` allows for a static surface whose host decides when it appears;
  `DrylMarkdown` and the AI surfaces that place it wrap it where its appearance
  should be animated. The component *is* animated in the state that matters to
  it — the aura, whose enter, dissolve and completion wash are specified above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decisions are the hidden, unselectable gutter and the labelled
  copy button; the substantive omissions are the unannounced confirmation, the
  unreachable scroll container and the unnamed language, all recorded above.
- **AI mode** — yes. The block carries `Ai` and `Aura` and renders the shared
  vocabulary, because code is the artifact a model most often produces and the
  surface should say while it is still arriving.
- **Demo page** — `DRYL.Website/Components/Pages/DemoCodeBlock.razor`, with the
  examples `Components/Examples/CodeBlock/Basic.razor`, `.../Languages.razor`,
  `.../LineNumbers.razor`, `.../HighlightingOff.razor` and `.../AiStates.razor`.
- **`ComponentCatalog`** — registered as `"Code Block"` / `code-block` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
