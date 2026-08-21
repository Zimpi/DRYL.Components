# DrylCitationList

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylCitationList.razor

## User Story

As a Blazor developer, I want the sources behind a generated answer collected in
one numbered list under it, so that a reader who wants to check the whole answer
rather than one claim has a single place to look.

## Description

`DrylCitationList` is the block half of the source-attribution pair whose inline
half is `DrylCitation` (`F4`). It is deliberately thin: an optional heading and
an ordered list holding `DrylCitationListItem` entries (`F6`). It holds no
state, cascades nothing to its children, and derives no numbering — every entry
carries its own `Index`, for the same reason the inline chip does.

That thinness is the point. The list exists to give the entries a semantic
container and a rhythm, not to own them; an application that renders its sources
from a collection binds the loop itself and the list stays out of the way.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Title` | `string?` | `null` | Heading above the list, e.g. "Sources". |
| `ChildContent` | `RenderFragment?` | `null` | The `DrylCitationListItem` entries. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the list's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a single root element holding the heading and the list.
- `Title` set renders one heading element above the list.
- `Title` left unset renders no heading element.
- `ChildContent` is rendered inside an `ol` element, so the entries are an
  ordered list in the document rather than a stack of divs.
- `Class` is merged onto the root's own class rather than replacing it.
- `AdditionalAttributes` are applied to the root.
- The entries are stacked vertically with a gap of `--sp-2` between them.
- The heading and the list are separated by that same gap, so the block has one
  rhythm rather than two.

### Keyboard and accessibility

- The list is not focusable and adds no stop to the tab order; the links inside
  its entries are the operable parts.
- The heading is rendered as plain text rather than as an `h*` element, so
  dropping a source list into an arbitrary place in a document cannot corrupt
  that document's heading outline.
- The component binds no key handler and manages no focus, because it owns no
  interaction.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The heading is set in `--fg-dim`, quieter than the entries below it.
- The heading is upper-cased and letter-spaced, so it reads as a label rather
  than as the first line of content.
- The list renders no bullet or number of its own, because each entry draws its
  own number.
- The list paints no surface of its own — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision follows `F4` and for the same reason (`AI-05`): the list is the
  settled record of where an answer came from, and an activity signal on it
  would contradict that. While the sources are still arriving, the surface
  streaming the answer carries the `Ai` state; the list appears when they have.

## Recorded gaps

- **Removing the list markers may remove the list semantics.** The `ol` is
  styled with `list-style: none`, which in WebKit-based browsers also drops the
  element's list role — so the sources can be announced as a run of paragraphs
  rather than as "list, 4 items". The numbers a reader sees are drawn by each
  entry and are not the list's own markers, so nothing visible would change if
  the role were restored explicitly.
- **The heading is not a heading.** Rendering `Title` as plain text is the safe
  choice for arbitrary placement (above), but it also means a screen-reader user
  cannot jump to the sources by heading navigation, which is exactly how such a
  user would look for them. Neither behaviour is available as a parameter.
- **The heading's type is literal.** The `11px` size and the letter-spacing are
  written into `.citation-list-title` in `dryl.css` with no token behind them
  (`DESIGN-01`).
- **Nothing is animated.** Sources appearing under an answer — the one moment
  this component exists for — appear instantly, and no entry animates in
  (`DESIGN-11`, `DESIGN-12`). The list is not wrapped in `DrylPresence` and does
  not wrap its entries in one.
- **No tests of its own.** None of the criteria above is guarded by a test.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg-dim` is the one
  mode-dependent token it names; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the plain-text heading, whose cost is recorded as a
  gap.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoCitation.razor`
  through the example `Components/Examples/Citation/SourceList.razor`.
- **`ComponentCatalog`** — reached through the `"Citation"` / `citation` entry
  in `DRYL.Website/Components/ComponentCatalog.cs`; the catalog registers the
  lead component of a family and not its parts.
