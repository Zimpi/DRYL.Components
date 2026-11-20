# DrylStack

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylStack.razor

## User Story

As a Blazor developer composing a page, I want a typed flex container with a
direction, a gap from the spacing scale, alignment, distribution and an
optional collapse to a column on narrow slots, so that I lay out rows and
columns without ad-hoc `.row` / `.col` utility markup.

## Description

`DrylStack` renders one flex container (`div.stack`) whose direction, gap,
cross-axis alignment, main-axis distribution and wrapping are set by typed
parameters and written as inline style. The gap comes from the `--sp-*` scale.

`CollapseBelow` makes a horizontal stack responsive to **its own available
width**, not the viewport: the stack is wrapped in a `div.cq` container-query
context, and below the chosen `Breakpoint` the stack flips to a column. The
direction is then owned by a `.stack-collapse-*` class instead of the inline
style, so the container query can override it. A vertical stack ignores
`CollapseBelow`.

The nested enums declared here — in particular `DrylStack.StackGap` — are also
used by `DrylSpacer` and `DrylGrid`.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Direction` | `DrylStack.StackDirection` | `StackDirection.Vertical` | Main-axis direction. |
| `Gap` | `DrylStack.StackGap` | `StackGap.Md` | Spacing between children, from the `--sp-*` scale. |
| `Align` | `DrylStack.StackAlign` | `StackAlign.Stretch` | Cross-axis alignment (`align-items`). |
| `Justify` | `DrylStack.StackJustify` | `StackJustify.Start` | Main-axis distribution (`justify-content`). |
| `Wrap` | `bool` | `false` | Children wrap onto further lines when they overflow. |
| `CollapseBelow` | `Breakpoint?` | `null` | Below this container width a horizontal stack becomes vertical; `null` never collapses. |
| `ChildContent` | `RenderFragment?` | `null` | Stack content. |
| `Class` | `string?` | `null` | Classes merged onto the stack's own classes. |
| `Style` | `string?` | `null` | Inline declarations merged after the component's own; a `style` attribute binds here too. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the stack element. |

Nested enums (always written qualified, e.g. `DrylStack.StackGap.Md`):

- `DrylStack.StackDirection`: `Vertical`, `Horizontal`
- `DrylStack.StackGap`: `None`, `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Xxl`
- `DrylStack.StackAlign`: `Stretch`, `Start`, `Center`, `End`, `Baseline`
- `DrylStack.StackJustify`: `Start`, `Center`, `End`, `Between`, `Around`, `Evenly`

`Breakpoint` is a category-wide type, see [`_Api.md`](_Api.md).

## Acceptance Criteria

### Structure

- Without collapse, the component renders exactly one `div` carrying `.stack`.
- `ChildContent` renders directly inside the stack element.
- `Class` is appended to the stack element's class list.
- A consumer `class` attribute is merged into the class list, keeping `.stack`.
- Unmatched attributes are applied to the stack element.
- The stack element's inline style declares `display: flex`.
- The stack element can shrink below its content's width (`min-width: 0`).
- `Style` is appended to the root's inline style after the component's own
  declarations, so `display: flex` and the flex declarations stay.
- A consumer `style` attribute binds to `Style` and is merged the same way; it
  does not reach `AdditionalAttributes`.
- For a property both set, the consumer's declaration comes last and wins.

### Direction

- `Direction` defaults to `DrylStack.StackDirection.Vertical`.
- `DrylStack.StackDirection.Vertical` lays children out as a column.
- `DrylStack.StackDirection.Horizontal` lays children out as a row.

### Gap

- `Gap` defaults to `DrylStack.StackGap.Md`.
- `DrylStack.StackGap.None` sets no gap.
- `DrylStack.StackGap.Xs` sets the gap to `--sp-1`.
- `DrylStack.StackGap.Sm` sets the gap to `--sp-2`.
- `DrylStack.StackGap.Md` sets the gap to `--sp-3`.
- `DrylStack.StackGap.Lg` sets the gap to `--sp-4`.
- `DrylStack.StackGap.Xl` sets the gap to `--sp-5`.
- `DrylStack.StackGap.Xxl` sets the gap to `--sp-6`.

### Alignment

- `Align` defaults to `DrylStack.StackAlign.Stretch`.
- `DrylStack.StackAlign.Stretch` maps to `align-items: stretch`.
- `DrylStack.StackAlign.Start` maps to `align-items: flex-start`.
- `DrylStack.StackAlign.Center` maps to `align-items: center`.
- `DrylStack.StackAlign.End` maps to `align-items: flex-end`.
- `DrylStack.StackAlign.Baseline` maps to `align-items: baseline`.

### Distribution

- `Justify` defaults to `DrylStack.StackJustify.Start`.
- `DrylStack.StackJustify.Start` maps to `justify-content: flex-start`.
- `DrylStack.StackJustify.Center` maps to `justify-content: center`.
- `DrylStack.StackJustify.End` maps to `justify-content: flex-end`.
- `DrylStack.StackJustify.Between` maps to `justify-content: space-between`.
- `DrylStack.StackJustify.Around` maps to `justify-content: space-around`.
- `DrylStack.StackJustify.Evenly` maps to `justify-content: space-evenly`.

### Wrap

- `Wrap` defaults to `false`, which keeps children on one line.
- `Wrap` lets children wrap onto further lines.

### Collapse

- `CollapseBelow` defaults to `null`, and the stack never collapses.
- With `CollapseBelow` set on a horizontal stack, the stack element is wrapped
  in a `div` carrying `.cq`.
- The `.cq` wrapper is an inline-size query container.
- `Class` and unmatched attributes go to the inner stack element, not to the
  `.cq` wrapper.
- `Breakpoint.Sm` adds `.stack-collapse-sm` to the stack element.
- `Breakpoint.Md` adds `.stack-collapse-md` to the stack element.
- `Breakpoint.Lg` adds `.stack-collapse-lg` to the stack element.
- `Breakpoint.Xl` adds `.stack-collapse-xl` to the stack element.
- A collapsing stack carries no `flex-direction` in its inline style.
- A collapsing stack lays children out as a row while its container is wider
  than the breakpoint.
- A collapsing stack lays children out as a column once its container is at or
  below the breakpoint.
- On a vertical stack, `CollapseBelow` has no effect: no `.cq` wrapper and no
  `.stack-collapse-*` class are rendered.

### Keyboard and accessibility

- The stack adds no role and no focus stop; children keep their own semantics
  and DOM order.

## Recorded debt

- **Breakpoint widths are literals in the container queries.** The
  `.stack-collapse-*` queries carry the pixel values of the `Breakpoint` scale
  because CSS query conditions cannot read custom properties; the scale is
  documented once in the responsive-foundation block of `dryl.css`.
- **The collapse snaps.** Flipping between row and column is not animated.
- **The gap mapping is duplicated.** `DrylStack` maps `DrylStack.StackGap` to
  tokens on its own, beside `DrylGrid.GapToken`, which `DrylSpacer` and
  `DrylGrid` use; the two maps must be kept equal by hand.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/stack` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by written exception (`DESIGN-11`): a flex
  container renders no visible surface and does not mount conditionally;
  content that enters or leaves composes `DrylPresence` or `DrylReveal` inside
  it.
- **Keyboard and a11y** — no role, no focus stop; DOM order is visual order
  (no `row-reverse` or `order` is ever written).
- **AI mode** — **no** (`AI-05`): a layout container carries no content of its
  own; the AI state belongs to its children.
- **Demo page** — `/components/stack`
  (`DRYL.Website/Components/Pages/DemoStack.razor`), examples
  `Stack/Directions`, `Stack/Distribute` and `Stack/Wrap`.
- **`ComponentCatalog`** — registered as `"Stack"` / `stack`, flagged not
  AI-capable (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylStackTests.cs` (single `div`
  with inline direction, `.cq` wrapper without inline direction under
  `CollapseBelow`, collapse ignored when vertical);
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`Stack_merges_consumer_class_without_clobbering`).
