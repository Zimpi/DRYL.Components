# DrylGrid

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylGrid.razor

## User Story

As a Blazor developer, I want a column grid that wraps its items by itself, or
holds a fixed column count that steps down in a narrow slot, so that card
layouts stay readable from a phone to a wide screen without breakpoints of my
own.

## Description

`DrylGrid` renders a CSS grid in one of two modes.

**Auto-fit** (the default, `Columns` null) fits as many columns as the slot
allows while each item keeps at least the `MinItemWidth` preset, and never
overflows a slot narrower than that preset. It needs no breakpoint.

**Fixed columns** (`Columns` between 2 and 4) lays out exactly that many equal
columns and steps down as the *slot* narrows: three or four columns become two
at `Breakpoint.Md`, and every count becomes one at `Breakpoint.Sm`. The step
is a container query, and because an element is never its own query
container, the grid is wrapped in a `div.cq` that serves as the query context
(the same shape as `DrylStack`'s `CollapseBelow`). Any other column count is
laid out as given, with no step-down.

`Gap` maps onto the `--sp-*` scale through `DrylStack.StackGap`, the spacing
enum the layout primitives share (see [`_Api.md`](_Api.md)).

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `MinItemWidth` | `DrylGrid.ItemWidth` | `ItemWidth.Md` | Minimum item width in auto-fit mode. |
| `Columns` | `int?` | `null` | Fixed column count; `null` selects auto-fit. |
| `Gap` | `DrylStack.StackGap` | `StackGap.Md` | Gap between cells, on the `--sp-*` scale. |
| `ChildContent` | `RenderFragment?` | `null` | Grid items. |
| `Class` | `string?` | `null` | Classes appended to the grid element. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the grid element. |

Nested enum `DrylGrid.ItemWidth`: `Xs`, `Sm`, `Md`, `Lg`.

## Acceptance Criteria

### Structure

- The grid element is a `div` with class `grid`.
- `ChildContent` renders directly inside the grid element.
- `Class` is appended to the grid element's class list.
- Unmatched attributes are applied to the grid element, never to the
  `div.cq` wrapper.

### Auto-fit

- `Columns` defaults to `null`, which selects auto-fit.
- In auto-fit mode the grid element is the root; no `div.cq` wrapper renders.
- In auto-fit mode the grid element carries an inline `display: grid` and an
  `auto-fit` column template.
- `MinItemWidth` defaults to `ItemWidth.Md`.
- Each item is at least as wide as the `MinItemWidth` preset while the slot is
  wider than it.
- In a slot narrower than the `MinItemWidth` preset, a single item column fills
  the slot without overflowing.
- The `ItemWidth` presets grow strictly in the order `Xs` < `Sm` < `Md` < `Lg`.
- Columns share the remaining width equally.

### Fixed columns

- `Columns` of 2, 3 or 4 wraps the grid element in one `div` with class `cq`.
- The `cq` class sits on the wrapper only, never on the grid element.
- `Columns` of 2, 3 or 4 adds `grid-cols-{Columns}` to the grid element.
- In this mode the grid element carries no inline column template.
- The grid element shows exactly `Columns` equal columns while its wrapper is
  wider than `Breakpoint.Md`.
- With `Columns` of 3 or 4, the grid shows two columns while the wrapper is at
  most `Breakpoint.Md` wide.
- With `Columns` of 2, 3 or 4, the grid shows one column while the wrapper is
  at most `Breakpoint.Sm` wide.
- `MinItemWidth` has no effect when `Columns` is set.

### Other column counts

- A `Columns` value outside 2 to 4 renders no wrapper and no `grid-cols-*`
  class.
- A `Columns` value outside 2 to 4 sets an inline template of exactly that
  many equal columns.
- That template never steps down.

### Gap

- `Gap` defaults to `StackGap.Md`, which maps to `--sp-3`.
- `StackGap.None` sets a zero gap.
- `StackGap.Xs` maps to `--sp-1`, `Sm` to `--sp-2`, `Lg` to `--sp-4`, `Xl` to
  `--sp-5` and `Xxl` to `--sp-6`.
- The gap is written inline on the grid element in every mode.

## Recorded debt

- **Literal item widths.** The four `ItemWidth` minimums are rem literals in
  the component's C# (`DrylGrid.Style`), not tokens (`DESIGN-01`).
- **A zero or negative `Columns` is unchecked.** It is written into the
  template as given and yields an invalid template, leaving the items in a
  single implicit column.
- **A consumer `style` replaces the layout.** The splatted attributes come
  after the computed `style`, so a consumer `style` drops the gap and, in
  auto-fit mode, `display: grid` and the template.
- **`.grid` has no rule of its own.** The class is a hook; the layout comes
  from the inline style or a `grid-cols-*` utility.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/grid` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by explicit exception (`DESIGN-11`): a
  layout primitive with no state of its own and no conditional mount; the step
  between column counts is a container-query reflow, and items carry their own
  motion (a `DrylReveal` around them where an entrance is wanted).
- **Keyboard and a11y** — no role and no focus stop; reading and tab order
  follow the source order of the items in every column count.
- **AI mode** — **no** `Ai` parameter: a layout primitive holds no
  model-written content of its own (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoGrid.razor`, route
  `/components/grid`, examples `Grid/AutoFit` and `Grid/FixedColumns`.
- **`ComponentCatalog`** — `"Grid"` / `grid`, group `Layout`, AI flag `false`
  (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylGridTests.cs`.
