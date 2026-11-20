# DrylTypo

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylTypo.razor
              code/DRYL.Components/wwwroot/dryl.css

## User Story

As a Blazor developer writing page copy, I want one text component that renders
any step of the DRYL type scale and lets me pick the HTML tag separately, so
that a heading can look like an H2 while staying an `h1` in the document
outline, and body copy reads in the right colour without hand-written classes.

## Description

`DrylTypo` renders its content as a single element for one step of the type
scale. `Variant` picks the rendered tag and a `.typo-*` class; `As`
independently overrides the tag. The variant classes are styled in the TYPE
section of `dryl.css`, where a class outranks the element rules — the look
follows the variant, not the tag. `Color` swaps the variant's colour for one
of the `--fg*` tokens, `Align` sets the text alignment, and `Gradient` fills the
text with the accent gradient through the shared `.gradient-text` primitive.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `DrylTypo.TypoVariant` | `TypoVariant.Body` | Type step: size, weight, default colour. |
| `As` | `string?` | `null` | Rendered tag (e.g. `"h1"`, `"span"`, `"label"`); `null` picks it from `Variant`. |
| `Color` | `DrylTypo.TypoColor` | `TypoColor.Default` | Foreground colour from the `--fg*` tokens; `Default` keeps the variant's own. |
| `Align` | `DrylTypo.TypoAlign` | `TypoAlign.Start` | Text alignment; `Start` inherits. |
| `Gradient` | `bool` | `false` | Fills the text with the accent gradient. |
| `ChildContent` | `RenderFragment?` | `null` | Text content. |
| `Class` | `string?` | `null` | Classes merged onto the element's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the element. |

Nested enums (always written qualified, e.g. `DrylTypo.TypoVariant.H1`):

- `DrylTypo.TypoVariant`: `H1`, `H2`, `H3`, `H4`, `Lead`, `Body`, `Caption`, `Eyebrow`
- `DrylTypo.TypoColor`: `Default`, `Strong`, `Muted`, `Dim`, `Faint`
- `DrylTypo.TypoAlign`: `Start`, `Center`, `End`, `Justify`

## Acceptance Criteria

### Structure

- The component renders exactly one element holding `ChildContent`.
- The element carries `.typo`.
- `Class` is appended to the element's class list.
- Unmatched attributes are applied to the element.

### Tag

- With `As` set, the element's tag is the value of `As`.
- Without `As`, `DrylTypo.TypoVariant.H1` renders an `h1`.
- Without `As`, `DrylTypo.TypoVariant.H2` renders an `h2`.
- Without `As`, `DrylTypo.TypoVariant.H3` renders an `h3`.
- Without `As`, `DrylTypo.TypoVariant.H4` renders an `h4`.
- Without `As`, `DrylTypo.TypoVariant.Lead` renders a `p`.
- Without `As`, `DrylTypo.TypoVariant.Body` renders a `p`.
- Without `As`, `DrylTypo.TypoVariant.Caption` renders a `span`.
- Without `As`, `DrylTypo.TypoVariant.Eyebrow` renders a `div`.

### Variant

- `Variant` defaults to `DrylTypo.TypoVariant.Body`.
- Each variant adds its own class: `.typo-h1`, `.typo-h2`, `.typo-h3`,
  `.typo-h4`, `.typo-lead`, `.typo-body`, `.typo-caption`, `.typo-eyebrow`.
- The variant class sets the look whatever tag is rendered: an
  `DrylTypo.TypoVariant.H2` with `As="h1"` has the size, weight, line height
  and letter spacing of the `h2` step.
- The heading variants share their step with the `h1`–`h4` element rules of
  `dryl.css` and read `--fg`.
- `DrylTypo.TypoVariant.Lead` is the 17 px step in `--fg-muted`, at most `64ch`
  wide.
- `DrylTypo.TypoVariant.Body` is the 14 px step in `--fg-muted`.
- `DrylTypo.TypoVariant.Caption` is the 12 px step in `--fg-dim`.
- `DrylTypo.TypoVariant.Eyebrow` is the 11 px uppercase `--font-mono` step in
  `--fg-dim`, shared with `.eyebrow`.
- The element has no margin.
- The rules live in the TYPE section of `dryl.css`, not in a scoped
  stylesheet: the element is built with a `RenderTreeBuilder` and carries no
  scoped-CSS attribute.

### Color

- `Color` defaults to `DrylTypo.TypoColor.Default`, which writes no inline
  colour and keeps the variant's own.
- `DrylTypo.TypoColor.Strong` sets the inline colour to `--fg`.
- `DrylTypo.TypoColor.Muted` sets the inline colour to `--fg-muted`.
- `DrylTypo.TypoColor.Dim` sets the inline colour to `--fg-dim`.
- `DrylTypo.TypoColor.Faint` sets the inline colour to `--fg-faint`.

### Gradient

- `Gradient` defaults to `false`.
- `Gradient` adds `.gradient-text` to the element.
- Under `.gradient-text`, the text is filled with `--accent-grad`.
- With `Gradient`, `Color` writes no inline colour.
- The gradient shows on every variant: `.gradient-text` follows the `.typo-*`
  rules in `dryl.css`, so its transparent colour wins over the variant's.

### Alignment

- `Align` defaults to `DrylTypo.TypoAlign.Start`, which writes no inline
  alignment.
- `DrylTypo.TypoAlign.Center` sets `text-align: center`.
- `DrylTypo.TypoAlign.End` sets `text-align: right`.
- `DrylTypo.TypoAlign.Justify` sets `text-align: justify`.
- With neither a colour nor an alignment to write, the element carries no
  inline `style`.

### Keyboard and accessibility

- The element's semantics are those of its tag; heading variants are real
  headings unless `As` says otherwise.
- The component adds no role and no focus stop.

## Recorded debt

- **Literal type scale (`DESIGN-01`).** Font sizes, line heights and letter
  spacing of every `.typo-*` class and of the `h1`–`h4` scale it shares are
  literals in `dryl.css`; there are no type tokens.
- **`TypoAlign.End` is physical.** It writes `right`, not `end`, so it does not
  flip in a right-to-left document.
- **`As` is an unchecked string.** Any value becomes the tag; there is no guard
  against an invalid or unsafe tag name.
- **A consumer `style` replaces colour and alignment.** `AdditionalAttributes`
  is applied after the component's `style`, so a captured `style` attribute
  overwrites the inline colour and alignment.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/typography` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by written exception (`DESIGN-11`): static
  text with no state of its own; entrance motion is composed with `DrylReveal`.
- **Keyboard and a11y** — semantic tag per variant, overridable with `As` for
  the document outline; no focus stop. Look and tag are decoupled: measured on
  `/components/typography`, `Variant` `H2` with `As="h1"` computes the 32 px
  step (2026-11-20).
- **AI mode** — **no** (`AI-05`): a type primitive renders text, not a surface;
  streamed or generated text is carried by the AI components around it.
- **Demo page** — `/components/typography`
  (`DRYL.Website/Components/Pages/DemoTypo.razor`), examples `Typo/Scale`,
  `Typo/Colors` and `Typo/GradientAndTag`.
- **`ComponentCatalog`** — registered as `"Typography"` / `typography`, flagged
  not AI-capable (`REL-04`).
- **Tests** — none of its own.
