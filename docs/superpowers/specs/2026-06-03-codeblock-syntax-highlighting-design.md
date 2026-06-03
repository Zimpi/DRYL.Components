# DrylCodeBlock — Server-side Syntax Highlighting

Date: 2026-06-03

## Problem

`DrylCodeBlock` renders code as a single plain-text node. No token coloring →
looks flat and not "enterprise". The component comment historically declared
highlighting out of scope because DRYL ships **zero JS frameworks** (CLAUDE.md
rule 2.8). Syntax highlighting does **not** require JS — it can be done entirely
server-side in C#.

## Goal

Color code tokens (keyword / type / string / number / comment / punctuation)
using a pure-C# tokenizer, zero new runtime dependencies, mapping only onto
**existing** DRYL color tokens (no invented colors — rule 2.1).

## Architecture

- New internal static class `SyntaxHighlighter` in
  `DRYL.Components/Components/Data/Internal/SyntaxHighlighter.cs`.
- Entry point: `string Highlight(string code, string? language)` → returns an
  HTML string of `<span class="tok-…">…</span>` fragments.
- `DrylCodeBlock` renders the result as `MarkupString` instead of
  `<code>@Code</code>`.

### Security (critical)

We leave Razor's automatic HTML-encoding by emitting `MarkupString`. The
tokenizer therefore HTML-encodes **every** token's text itself via
`System.Text.Encodings.Web.HtmlEncoder.Default.Encode` before wrapping it in a
span. Model-authored code (`<script>`, `&`, `"`) stays as safe as today.

## Tokenizer

Single shared scanner recognizes, language-agnostically:

- Strings: `"…"`, `'…'`, backtick strings (with escape handling).
- Numbers: integer / float / hex.
- Comments: `// …`, `/* … */`, `# …`, `<!-- … -->` (per language).
- Identifiers → classified as keyword (per-language keyword set), type
  (heuristic: capitalized identifier, or known type keyword), or plain.
- Punctuation / operators.

Per language only a **keyword set** + which comment styles apply. Languages:
`csharp, javascript/typescript, json, html/xml, css, bash/shell, sql, python`.
Language aliases normalized (`cs→csharp`, `js/ts→javascript`, `sh/shell→bash`,
`yml`, etc.). Unknown language → one HTML-encoded plain span (today's behavior).

### Streaming resilience

Unterminated tokens (an open string while `Ai="Streaming"`) are consumed to
end-of-input as that token type — never throw, never drop the remainder.

## CSS (existing tokens only)

New classes in `DrylCodeBlock.razor.css`:

| Class          | Token         | Meaning              |
| -------------- | ------------- | -------------------- |
| `.tok-keyword` | `--accent-a`  | keywords             |
| `.tok-type`    | `--accent-b`  | types / functions    |
| `.tok-string`  | `--success`   | strings              |
| `.tok-number`  | `--warning`   | numbers              |
| `.tok-comment` | `--fg-faint`  | comments (italic)    |
| `.tok-punct`   | `--fg-muted`  | punctuation/operators|
| (unwrapped)    | `--fg`        | plain identifiers    |

## API & compatibility

- Highlighting **on by default** (this was the complaint). Flows through
  `DrylMarkdown` fenced code automatically.
- New escape hatch: `[Parameter] public bool Highlight { get; set; } = true;`
  When `false`, render plain text (today's output).
- Not a breaking change — output only looks better; public API only gains one
  optional parameter.

## Docs

- `CHANGELOG.md` → `Added`: server-side syntax highlighting + `Highlight` param.
- `README.md` → update DrylCodeBlock notes cell.
- Demo page → existing snippets gain color; add a `Highlight="false"` example.

## Out of scope

- Semantic/AST-accurate highlighting. This is lexical (token-level), which is
  what highlighters like Prism/highlight.js also do for display.
- Per-line diff highlighting, theming beyond the fixed DRYL palette.
