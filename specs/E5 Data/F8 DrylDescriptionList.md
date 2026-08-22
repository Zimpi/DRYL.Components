# DrylDescriptionList

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylDescriptionList.razor
              code/DRYL.Components/Components/Data/DrylDescriptionList.razor.css
              code/DRYL.Components/Components/Data/DescriptionLayout.cs

## User Story

As a Blazor developer, I want to show a record's fields as read-only term/value
pairs that reflow sensibly when the space narrows, so that a detail panel is
legible on a phone and dense on a desktop without me writing two layouts.

## Description

`DrylDescriptionList` is the read-only counterpart of a form: a semantic `dl`
holding `DrylDescriptionItem` pairs (`F9`). It owns two decisions and cascades
one of them.

`Layout` decides how each pair is arranged — term above value, or term beside
value in a label column — and is cascaded to every item, so a list is
consistently one or the other rather than a mix.

`Columns` decides how many pairs sit in a row, as a grid. The number is a
maximum rather than a promise: the list is measured against **its own container**
rather than the viewport, and collapses to a single column when that container
gets narrow. That is what lets the same list be used in a wide detail page and
in a narrow drawer without a parameter changing.

The container measurement is why the component renders a wrapper around its
`dl`: a container query cannot match the element that declares the containment,
so the containment lives one level up.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Layout` | `DescriptionLayout` | `DescriptionLayout.Stacked` | Term/value arrangement, applied to every item. |
| `Columns` | `int` | `1` | Number of pairs per row while there is room. |
| `ChildContent` | `RenderFragment?` | `null` | The `DrylDescriptionItem` entries. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the list's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the `dl` element. |

`DescriptionLayout`'s members are listed in [`_Api.md`](_Api.md).

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a `dl` element holding its items, so a record is a
  description list in the document rather than a grid of divs.
- The `dl` is wrapped in one element that establishes the size containment the
  layout is measured against.
- `ChildContent` is rendered inside a cascading value that hands the list itself
  to its items.
- The cascade is fixed, so an item never re-subscribes to it.
- `Columns` is published to the stylesheet as a custom property on the `dl`.
- `Class` is merged onto the `dl`'s own class rather than replacing it.
- `AdditionalAttributes` are applied to the `dl`.
- The `dl` carries no margin of its own, so the list does not add space its host
  did not ask for.

### Layout

- The items are laid out in a grid of `Columns` equal columns.
- Each column may shrink below its content's intrinsic width, so a long value
  wraps rather than widening the list.
- The gap between rows and the gap between columns are different, so a
  two-column list does not read as a grid of four unrelated cells.
- Both gaps come from the spacing scale rather than from written lengths.
- The list collapses to a single column when its own container is narrow,
  whatever `Columns` says.
- The collapse is measured against the list's container and not against the
  viewport, so a list inside a narrow drawer collapses on a wide screen.
- `Layout` is readable by every item in the list.

### Keyboard and accessibility

- The list is not focusable and adds no stop to the tab order, because it is a
  read-only record.
- The pairs are announced as terms and values by virtue of being a `dl`, without
  any ARIA of the component's own.
- The component binds no key handler and manages no focus, because it owns no
  interaction.

### Appearance

- The component renders no colour of its own and therefore names no literal
  colour (`DESIGN-01`); the colours belong to the items.
- The list paints no surface of its own — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the list is a container that paints
  nothing, so it has no surface for an aura to sit on. Where a *value* is
  model-produced, the AI state belongs to whatever the consumer renders inside
  that item — `DrylAiText`, a `DrylBadge`, a `DrylSkeleton` — at the granularity
  of the field that is actually generated rather than the whole record.

## Recorded gaps

- **The collapse breakpoint is a literal, and a private one.** The width at
  which the list drops to one column is written into the container query in
  `DrylDescriptionList.razor.css` as a raw length, with no token behind it and
  no relation to the `Breakpoint` scale the rest of the library uses
  (`DESIGN-01`). A consumer cannot change it, and a list beside a
  `DrylGrid` collapses at a different width than the grid does.
- **`Columns` is unvalidated.** Zero or a negative value produces an invalid
  grid declaration that the browser discards, so the list silently falls back to
  the browser's default rather than to one column, and a very large value
  produces columns narrower than their content. Nothing clamps the parameter and
  nothing reports it.
- **Nothing is animated.** Neither the list nor its collapse is animated: the
  grid snaps from two columns to one as the container crosses the threshold,
  and items appearing or leaving do so instantly (`DESIGN-11`, `DESIGN-12`). The
  component is exactly the kind of layout the library animates elsewhere.
- **No tests of its own.** None of the criteria above is guarded by a test, and
  the component is absent from `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  despite carrying a `Class` parameter.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component names no colour at all, so
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have nothing of its own to check;
  the mode-dependent tokens are the items'.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the semantics come from the `dl` element rather
  than from ARIA, so they survive every layout the component offers.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoDescriptionList.razor`,
  with the examples `Components/Examples/DescriptionList/Stacked.razor` and
  `.../Inline.razor`.
- **`ComponentCatalog`** — registered as `"Description List"` /
  `description-list` in `DRYL.Website/Components/ComponentCatalog.cs`, flagged
  not AI-capable.
