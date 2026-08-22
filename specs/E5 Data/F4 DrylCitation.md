# DrylCitation

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylCitation.razor

## User Story

As a Blazor developer, I want a generated answer to carry its sources inline
without the prose being interrupted by them, so that a reader can check where a
claim came from at the moment they doubt it, and ignore the sources entirely
while they do not.

## Description

`DrylCitation` is the inline half of the library's source-attribution pair. It
renders a superscript `[n]` chip in the flow of the text; pressing it opens a
popover holding the source's title, an optional grounding snippet, and a link to
the source itself. The numbered list of all sources is the other half, in `F5`.

The design decision the component makes is that **attribution is on demand**. A
generated paragraph that names its sources in full is unreadable, and one that
names them nowhere is unverifiable. The chip is the smallest mark that can carry
both: two characters in the prose, everything else one press away.

The panel is a `DrylPopover` (`specs/E11 Surfaces/F1 DrylPopover.md`), so the
portal, the placement, the outside-click and the `Escape` handling — and that
component's recorded debt — belong to it rather than being re-specified here.

The chip is numbered by the consumer, not by the component: `Index` is a
parameter. Nothing derives it, because the numbering has to agree with a list
that may be rendered somewhere else entirely.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Index` | `int` | `1` | 1-based reference number shown in the chip and named in its label. |
| `Title` | `string?` | `null` | Source title, shown at the top of the panel. |
| `Url` | `string?` | `null` | Source URL, rendered as an external link in the panel. |
| `Snippet` | `string?` | `null` | Excerpt that grounds the cited claim. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the chip's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the chip. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### The chip

- The component renders a `button` as its trigger.
- The button is of type `button`, so a citation inside a form never submits it.
- The button renders `Index` as its content.
- The button carries an open-state modifier class while the panel is open.
- `Class` is merged onto the button's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the button.
- The chip sits raised above the text baseline, so it reads as a reference mark
  rather than as part of the sentence.
- The chip renders inline, so a citation does not break the line it is in.

### The panel

- Pressing the chip opens the panel; pressing it again closes it.
- The panel is rendered by `DrylPopover`, so the portalling, placement, outside
  click and `Escape` behaviour specified for that component apply unchanged.
- The panel prefers to open above the chip and aligned to its start edge.
- `Title` set renders a title row holding a quote icon and the title.
- `Title` left unset renders no title row.
- `Snippet` set renders the excerpt as a paragraph.
- `Snippet` left unset renders no excerpt.
- `Url` set renders a link holding a link icon and the URL's display form.
- `Url` left unset renders no link.
- All three parts left unset renders an empty panel rather than failing.
- The panel is width-capped, so a long snippet wraps instead of stretching
  across the viewport.

### The link

- The link's target is `Url` exactly as given.
- The link's visible text is the URL's host when `Url` parses as an absolute
  URI, so a long tracking URL does not fill the panel.
- The link's visible text is `Url` unchanged when it does not parse as an
  absolute URI.
- The link opens in a new browsing context, so following a source does not
  discard the answer that cited it.
- The link is opened with `noopener` and `noreferrer`, so the source page can
  neither reach back into the opener nor be told where the reader came from.
- A long URL breaks across lines rather than overflowing the panel.

### Keyboard and accessibility

- The chip is a real button, so it is reachable by `Tab` and activated by
  `Enter` and `Space` without a key handler of its own.
- The chip carries an accessible label naming it as a source and stating its
  number.
- The chip's accessible label includes `Title` when one is set, so a
  screen-reader user hears which source they are about to open.
- The panel carries `role="dialog"`.
- The panel carries the same accessible label as the chip that opened it.
- The panel's `Escape`, outside-click and focus behaviour is
  `DrylPopover`'s — including that component's recorded focus debt.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The chip at rest is set in `--accent-b`, filled with `--accent-soft` and
  outlined with `--accent-line`.
- The chip on hover and while open is filled with `--accent-grad` and set in
  `--fg`.
- The chip's corner comes from `--r-xs`.
- The chip's number is set in `--font-mono`, so chips of one and two digits
  align.
- The panel's title icon is drawn in `--accent-b`.
- The panel's snippet is set in `--fg-muted`, quieter than the title above it.
- The panel's link is set in `--accent-b` at rest and `--fg` on hover.
- The panel's frost and fill are `DrylPopover`'s `--panel-float` and
  `--glass-fx-float`, because the panel floats (`DESIGN-06`).
- The accent appears as a two-character chip and a 1px border, never as the fill
  of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The chip transitions its fill and its text colour between rest and hover.
- Both transitions run at `--dur-fast` with `--ease-out`.
- The panel's enter animation is `DrylPopover`'s, including that component's
  recorded absence of an exit animation.

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`), and it is the interesting one in this
  category: a citation is the component most obviously *about* AI, and precisely
  for that reason it must not carry the AI vocabulary. The aura says "something
  is happening here". A citation says the opposite — that this claim is settled
  and here is where it came from. A chip that glowed while a model was thinking
  would attach an activity signal to the one element whose job is to be a
  verifiable fact. The surface that streams the answer carries `Ai`;
  the citations inside it do not.

## Recorded gaps

- **Nothing enforces that `Index` agrees with anything.** The chip's number, the
  matching `DrylCitationListItem`'s number and the actual position of a source in
  the list are three independent values a consumer keeps in step by hand. Two
  chips can carry the same number, and a chip can point at a number no list
  entry has.
- **The chip's geometry is literal.** `16px` of minimum width and height, `4px`
  of padding, `1px` of margin and the `10px` type are written into
  `.citation-chip` in `dryl.css` with no token behind them (`DESIGN-01`). The
  same is true of the panel's `280px` cap and its `13px`/`12.5px`/`12px` type
  sizes.
- **The external link does not announce that it leaves.** The link opens a new
  browsing context with no marker in its accessible name and no icon saying so,
  so a screen-reader user is moved to another tab without warning.
- **The panel has no exit animation**, inherited from `DrylPopover` along with
  that component's other recorded a11y debt — `Escape` not reaching an unfocused
  panel, the portalled panel falling out of the tab order, and focus not being
  returned on close (`DESIGN-12`).
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the URL display rule, which is the component's only logic.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-b`, `--accent-soft`,
  `--accent-line`, `--accent-grad`, `--fg` and `--fg-muted` are the
  mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — the chip's hover transition is its own; the panel's
  enter animation and its missing exit are `DrylPopover`'s, recorded above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the chip is a real `button` with a label naming
  both the number and the source, so a citation is operable and identifiable
  without sight of the superscript.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoCitation.razor`, with the
  examples `Components/Examples/Citation/InlineChips.razor` and
  `.../SourceList.razor`.
- **`ComponentCatalog`** — registered as `"Citation"` / `citation` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable —
  consistent with the AI-mode decision above.
