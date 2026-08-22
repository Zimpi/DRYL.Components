# DrylCitationListItem

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylCitationListItem.razor

## User Story

As a Blazor developer, I want each source under a generated answer shown with
its number, its title, the passage it contributed and a link to it, so that a
reader can judge whether a source supports the claim without opening it.

## Description

`DrylCitationListItem` is one entry of a `DrylCitationList` (`F5`). It renders
as a bordered row: the reference number in its own tile on the leading edge, and
a body holding the title, the grounding snippet and the external link — each
part optional and each omitted from the markup entirely when it is not given.

It is the block counterpart of the panel `DrylCitation` (`F4`) opens, and it
shows the same four values in the same order. The difference is permanence: the
chip's panel is a glance, this is the record.

Like the chip, it is numbered by the consumer through `Index`. Unlike the chip,
it is not interactive — the only operable thing in the entry is the link.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Index` | `int` | `1` | 1-based reference number, matching the inline `DrylCitation`. |
| `Title` | `string?` | `null` | Source title. |
| `Url` | `string?` | `null` | Source URL, rendered as an external link. |
| `Snippet` | `string?` | `null` | Grounding excerpt. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the item's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the list item. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders an `li` element as its root, so an entry is a list item
  of the `ol` around it rather than a div inside one.
- The root carries `Index` as its ordinal value, so assistive technology reading
  the list's numbering agrees with the number drawn in the entry.
- The root holds a number tile and a body, in that order.
- The number tile renders `Index`.
- `Title` set renders a title element inside the body.
- `Title` left unset renders no title element.
- `Snippet` set renders the excerpt as a paragraph inside the body.
- `Snippet` left unset renders no excerpt.
- `Url` set renders a link holding a link icon and the URL's display form.
- `Url` left unset renders no link.
- All three parts left unset renders an entry holding its number alone rather
  than failing.
- `Class` is merged onto the root's own class rather than replacing it.
- `AdditionalAttributes` are applied to the root.
- The number tile does not shrink, so a long title cannot squeeze the number out
  of shape.
- The body may shrink below its content's intrinsic width, so a long unbroken
  title wraps rather than widening the entry.

### The link

- The link's target is `Url` exactly as given.
- The link's visible text is the URL's host when `Url` parses as an absolute
  URI.
- The link's visible text is `Url` unchanged when it does not parse as an
  absolute URI.
- The link opens in a new browsing context, so following a source does not
  discard the answer that cited it.
- The link is opened with `noopener` and `noreferrer`.
- A long URL breaks across lines rather than overflowing the entry.

### Keyboard and accessibility

- The entry itself is not focusable and adds no stop to the tab order, because
  it is a record and not a control.
- The link is the entry's only tab stop.
- The link icon is decorative and is not part of the link's accessible name, so
  the link is announced as the source it points at.
- The number is rendered as text in the entry, so it is announced along with the
  title rather than being carried by a marker the stylesheet removed.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The entry is filled with `--glass-1` and outlined with `--line`.
- The entry's corner comes from `--r-sm`.
- The number tile is set in `--accent-b` and filled with `--accent-soft`, so it
  matches the inline chip that points at it.
- The number tile's corner comes from `--r-xs`, the same as the inline chip's.
- The number is set in `--font-mono`, so entries of one and two digits align.
- The title is set in `--fg`.
- The snippet is set in `--fg-muted`, quieter than the title above it.
- The link is set in `--accent-b` at rest and `--fg` on hover.
- The entry sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as a small number tile and the link's text, never as the
  fill of the entry (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision follows `F4` and `F5` for the same reason (`AI-05`): the entry is
  a settled record of provenance, and an activity signal on it would contradict
  what it is for.

## Recorded gaps

- **The link changes colour but does not transition.** Unlike the inline chip,
  which transitions its hover state at `--dur-fast`, the entry's link swaps
  colour instantly and the entry itself has no hover treatment at all
  (`DESIGN-11`).
- **Nothing is animated.** An entry appearing under an answer appears instantly;
  the component is not wrapped in `DrylPresence` and has no enter or exit
  (`DESIGN-12`).
- **The external link does not announce that it leaves**, exactly as in `F4`:
  a new browsing context with nothing in the accessible name saying so.
- **The entry's type sizes are literal.** `13px` for the title, `12.5px` for the
  snippet, `12px` for the link and `11px` for the number, along with the number
  tile's `18px` box, are written into the `.citation-item` rules in `dryl.css`
  with no token behind them (`DESIGN-01`).
- **`Index` is trusted twice.** It is drawn as the entry's visible number *and*
  set as the `li`'s ordinal value, so a consumer who numbers two entries alike
  produces a list whose semantics and whose appearance are consistently wrong
  together. Nothing derives the number from the entry's position.
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the URL display rule it shares with `F4` — the same logic is written
  out twice in the two components and tested in neither.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--line`,
  `--accent-b`, `--accent-soft`, `--fg` and `--fg-muted` are the mode-dependent
  tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the number is rendered as text rather than left
  to the list marker the stylesheet removes, so it survives into the accessible
  name.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoCitation.razor`
  through the example `Components/Examples/Citation/SourceList.razor`.
- **`ComponentCatalog`** — reached through the `"Citation"` / `citation` entry
  in `DRYL.Website/Components/ComponentCatalog.cs`; the catalog registers the
  lead component of a family and not its parts.
