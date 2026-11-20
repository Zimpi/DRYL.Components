# DrylDivider

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylDivider.razor
              code/DRYL.Components/Components/Layout/DrylDivider.razor.css

## User Story

As a Blazor developer, I want a thin rule that separates content
horizontally, vertically inside a row, or around a short centred label, so
that sections and toolbar groups read apart without hand-drawn borders.

## Description

`DrylDivider` renders one of three shapes. Horizontal without content it is a
full-width hairline (`.divider`). Horizontal with `ChildContent` it is a
labelled separator: line, label, line (`.divider-labelled`). Vertical it is a
hairline that stretches to the height of its flex row (`.divider-v`); a label
is ignored in that orientation. Every shape carries `role="separator"`.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Orientation` | `DrylDivider.DividerOrientation` | `DividerOrientation.Horizontal` | Direction of the rule. |
| `ChildContent` | `RenderFragment?` | `null` | Centred label; honoured only for a horizontal divider. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

Nested enum `DrylDivider.DividerOrientation`: `Horizontal`, `Vertical`.

## Acceptance Criteria

### Horizontal

- `Orientation` defaults to `DividerOrientation.Horizontal`.
- Horizontal without `ChildContent`, the component renders one empty
  `div.divider` with `role="separator"`.
- `.divider` spans the full width of its parent.
- `.divider` is a hairline filled with `--line`.

### Labelled

- Horizontal with `ChildContent`, the component renders one
  `div.divider-labelled` with `role="separator"`.
- `.divider-labelled` holds, in order, a `.divider-labelled-line`, a
  `.divider-labelled-text` with `ChildContent`, and a second
  `.divider-labelled-line`.
- The two lines share the free width equally, so the label stays centred.
- Each line is a hairline filled with `--line`.
- Label and lines are separated by `--sp-3`.
- The label is set in `--font-mono`, uppercase, in `--fg-dim`.
- The label never wraps.

### Vertical

- `DividerOrientation.Vertical` renders one empty `div.divider-v` with
  `role="separator"` and `aria-orientation="vertical"`.
- `DividerOrientation.Vertical` ignores `ChildContent`.
- `.divider-v` is a hairline filled with `--line`.
- Inside a flex row, `.divider-v` stretches to the row's height.

### Class and attributes

- `Class` is appended to the rendered root's class list in every shape.
- Unmatched attributes are applied to the rendered root in every shape.
- A consumer `class` passed as an unmatched attribute is merged with the
  divider's own class, not replacing it.

## Recorded debt

- **The label may go unannounced.** `role="separator"` treats its children as
  presentational, so assistive technology may not read the label of a
  `.divider-labelled`.
- **Literal type.** The label's font size and letter spacing are literals in
  `DrylDivider.razor.css` (`DESIGN-01`).
- **The hairline thickness is literal.** All three shapes draw a literal
  one-pixel rule (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/divider` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by explicit exception (`DESIGN-11`): a
  static rule with no state, no interaction and no conditional mount.
- **Keyboard and a11y** — `role="separator"` on every shape;
  `aria-orientation="vertical"` on the vertical one. Not focusable, which is
  correct for a non-interactive separator. The label caveat is recorded above.
- **AI mode** — **no** `Ai` parameter: a separator holds no model-written
  content (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoDivider.razor`, route
  `/components/divider`, examples `Divider/Basic`, `Divider/Labelled` and
  `Divider/Vertical`.
- **`ComponentCatalog`** — `"Divider"` / `divider`, group `Layout`, AI flag
  `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`Divider_merges_consumer_class_without_clobbering`).
