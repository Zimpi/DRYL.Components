# DrylList

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylList.razor

## User Story

As a Blazor developer, I want a typed list container that picks the marker,
the row spacing and optional dividers for all of its items at once, so that
lists across my app look alike without ad-hoc bullet markup.

## Description

`DrylList` renders a semantic `ul`, or an `ol` when `Ordered` is set, and
cascades itself to its `DrylListItem` children. The list itself draws no
native markers: each item reads the list's `Variant` and draws its own marker
(see [`F8 DrylListItem.md`](F8%20DrylListItem.md)). `Density` sets row padding
and the gap between rows through two custom properties, `Dividers` replaces the
gap with a hairline between adjacent rows, and a `DrylList` nested inside a
`DrylListItem` is indented with a connector rail. Decimal numbering restarts
in every list, so nested lists count on their own.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Ordered` | `bool` | `false` | Renders an `ol` instead of a `ul`. |
| `Variant` | `DrylList.ListVariant` | `ListVariant.Default` | Marker style of items without their own icon or start slot. |
| `Density` | `DrylList.ListDensity` | `ListDensity.Default` | Row padding and gap, on the `--sp-*` scale. |
| `Dividers` | `bool` | `false` | Draws a hairline between adjacent items. |
| `ChildContent` | `RenderFragment?` | `null` | The items — normally `DrylListItem`s. |
| `Class` | `string?` | `null` | Classes appended to the list. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the list. |

Nested enum `DrylList.ListVariant`: `Default`, `Disc`, `Decimal`, `None`,
`Dash`. Nested enum `DrylList.ListDensity`: `Compact`, `Default`,
`Comfortable`. The list cascades itself (`IsFixed`) to its items.

## Acceptance Criteria

### Structure

- With `Ordered` false the component renders a `ul` with class `list`.
- With `Ordered` true it renders an `ol` with class `list`.
- `Ordered` defaults to `false`.
- `ChildContent` renders inside the list element.
- `Class` is appended to the list's class list.
- Unmatched attributes are applied to the list element.
- The list cascades itself to its descendants.
- The list renders no native list markers and no native indentation.
- Items stack vertically.

### Marker variant

- `Variant` defaults to `ListVariant.Default`, which adds
  `list--marker-default`.
- `ListVariant.Disc` adds `list--disc`.
- `ListVariant.Decimal` adds `list--decimal`.
- `ListVariant.Dash` adds `list--dash`.
- `ListVariant.None` adds `list--none`.
- Items of the list draw the marker that matches the list's `Variant`.
- `Variant` is independent of `Ordered`: an `ol` with `ListVariant.Default`
  draws dot markers, a `ul` with `ListVariant.Decimal` draws numbers.
- Decimal numbering restarts at one in every list, including a nested one.

### Density

- `Density` defaults to `ListDensity.Default`, which adds `list--default`.
- `ListDensity.Compact` adds `list--compact`.
- `ListDensity.Comfortable` adds `list--comfortable`.
- `ListDensity.Compact` pads rows by `--sp-1` and leaves no gap between them.
- `ListDensity.Default` pads rows by `--sp-2` and separates them by `--sp-1`.
- `ListDensity.Comfortable` pads rows by `--sp-3` and separates them by
  `--sp-2`.

### Dividers

- `Dividers` defaults to `false`.
- `Dividers` adds `list--dividers`.
- With `Dividers`, adjacent items touch with no gap.
- With `Dividers`, every item after the first is separated from the one above
  by a `--line` hairline.
- With `Dividers`, items lose their rounded corners.

### Nesting

- A `DrylList` inside a `DrylListItem` of another list is indented by
  `--sp-4` from the inline start.
- A nested list carries a `--line` connector rail along its inline start.
- A nested list is separated from its parent row by `--sp-1`.

## Recorded debt

- **The divider thickness is literal.** The hairline between items is a
  literal one-pixel border (`DESIGN-01`).
- **`Ordered` does not imply numbers.** An `ol` keeps the dot marker unless
  `Variant` is set to `ListVariant.Decimal` as well; the two parameters can
  contradict each other.
- **No tests.** No bUnit test renders `DrylList`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/list` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** of its own, by explicit exception
  (`DESIGN-11`): the container holds no state and never mounts conditionally;
  row hover, selection and press motion belong to `DrylListItem` (see `F8`).
- **Keyboard and a11y** — native `ul` / `ol` semantics, so assistive
  technology announces a list and its item count; markers drawn by the items
  are `aria-hidden`. The list itself has no focus stop; interactive items are
  native buttons (see `F8`).
- **AI mode** — **no** `Ai` parameter: a list container holds no
  model-written content of its own; streamed text inside an item is carried by
  the components placed there (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoList.razor`, route
  `/components/list`, examples `List/Markers`, `List/Interactive` and
  `List/Nested`.
- **`ComponentCatalog`** — `"List"` / `list`, group `Layout`, AI flag `false`
  (`REL-04`).
- **Tests** — none.
