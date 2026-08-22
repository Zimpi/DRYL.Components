# DrylDescriptionItem

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylDescriptionItem.razor
              code/DRYL.Components/Components/Data/DrylDescriptionItem.razor.css

## User Story

As a Blazor developer, I want one field of a record shown as a labelled value
that arranges itself the way the record around it decided, so that I write the
field once and the detail panel stays consistent whichever layout it is in.

## Description

`DrylDescriptionItem` is one term/value pair inside a `DrylDescriptionList`
(`F8`). The term is a string with an optional leading icon; the value is a
`RenderFragment`, so it takes text, a badge, a link or a whole composed row.

It owns no layout decision of its own. The arrangement — term above value, or
term beside value — is read from the list it sits in, which is what keeps a
record from being half stacked and half inline.

The pair is grouped so that the two elements move together in the list's grid:
each item is one grid cell holding a term and a value, rather than the terms and
the values being two independent runs.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Term` | `string?` | `null` | The label. |
| `Icon` | `string?` | `null` | `DrylIcon` name rendered before the term. |
| `ChildContent` | `RenderFragment?` | `null` | The value. |

The component has **no** `Class` and **no** `AdditionalAttributes` — see
"Recorded gaps". It takes no `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders one grouping element holding a `dt` and a `dd`, in that
  order.
- The grouping element is what the list's grid places, so a term and its value
  are never separated across a row boundary.
- `Term` is rendered inside the `dt`.
- `ChildContent` is rendered inside the `dd`.
- `Icon` set renders one `DrylIcon` inside the `dt`, before the term.
- `Icon` unset renders no icon element.
- The item may shrink below its content's intrinsic width, so a long value wraps
  rather than widening its column.
- Neither the `dt` nor the `dd` carries a margin of its own, so the pair's
  spacing is the list's.

### Layout

- The item reads its arrangement from the `DrylDescriptionList` it is inside.
- `DescriptionLayout.Stacked` arranges the term above the value.
- `DescriptionLayout.Inline` arranges the term and the value on one row, aligned
  on their text baselines.
- `DescriptionLayout.Inline` gives the term a fixed label column that does not
  shrink, so the values of several items line up with each other.
- An item rendered outside a `DrylDescriptionList` arranges itself as
  `DescriptionLayout.Stacked` rather than failing.
- A value long enough to have no break opportunity is broken anyway rather than
  overflowing its column.

### Keyboard and accessibility

- The term is a `dt` and the value is a `dd`, so the pair is announced as a term
  and its description without any ARIA of the component's own.
- The item is not focusable and adds no stop to the tab order, because it is a
  read-only field.
- The term's icon is decorative and is not part of the term's accessible name,
  so the field is announced by its label rather than by its glyph.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The term is set in `--fg-muted` and the value in `--fg`, so the value is the
  louder of the two.
- The value is set larger than the term, so the hierarchy survives in
  monochrome.
- The item paints no surface of its own — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`) and follows `F8`: the item paints no
  surface for an aura to sit on, and a generated *value* is better expressed by
  what the consumer renders into `ChildContent` — a `DrylAiText` while it
  streams, a `DrylSkeleton` while it is pending — than by an aura around the
  label as well.

## Recorded gaps

- **No `Class`, no `AdditionalAttributes`.** The item is one of the two
  components in the category that carry neither, so a consumer cannot attach a
  test hook, a `data-*` attribute or a style class to a single field. It is a
  known hole in the library-wide `Class` rollout and not specific to this
  component.
- **`Term` cannot be markup.** It is a `string`, so a field label carrying a
  unit, a tooltip trigger or a required marker has to be built by not using
  `Term` at all — and there is no term template to fall back on.
- **The label column's width is a literal.** The fixed width the inline layout
  gives the term is written into `DrylDescriptionItem.razor.css` as a raw
  length, as are the term's and the value's type sizes and the stacked layout's
  gap (`DESIGN-01`). The inline layout's gap *is* a token, so the file is
  half-converted rather than untouched.
- **Nothing is animated.** Switching the list's `Layout` re-arranges every item
  instantly, and an item appearing or leaving does so with no transition
  (`DESIGN-11`, `DESIGN-12`).
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the fallback to the stacked arrangement outside a list.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg-muted` and `--fg` are the
  mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the pair's semantics come from `dt`/`dd` rather
  than from ARIA, so they hold in both arrangements.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on
  `DRYL.Website/Components/Pages/DemoDescriptionList.razor` through the examples
  `Components/Examples/DescriptionList/Stacked.razor` and `.../Inline.razor`.
- **`ComponentCatalog`** — reached through the `"Description List"` /
  `description-list` entry in `DRYL.Website/Components/ComponentCatalog.cs`; the
  catalog registers the lead component of a family and not its parts.
