# DrylSpacer

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylSpacer.razor

## User Story

As a Blazor developer laying out a toolbar or a stack, I want an empty element
that either pushes its neighbours apart or holds a fixed gap from the spacing
scale, so that I do not write margin utilities or empty `div`s with inline
widths.

## Description

`DrylSpacer` renders one empty, decorative `div.spacer`. Without `Size` it grows
to fill the free space of its flex parent, pushing the siblings on either side
apart. With `Size` it is a fixed square block whose width and height come from
the `--sp-*` scale through the same `DrylStack.StackGap` steps that `DrylStack`
and `DrylGrid` use for their gaps.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Size` | `DrylStack.StackGap?` | `null` | Fixed size from the spacing scale; `null` grows to fill. |
| `Class` | `string?` | `null` | Classes merged onto the spacer's own classes. |
| `Style` | `string?` | `null` | Inline declarations merged after the component's own; a `style` attribute binds here too. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

`DrylStack.StackGap` is specified in [`F13 DrylStack.md`](F13%20DrylStack.md).

## Acceptance Criteria

### Structure

- The component renders one empty `div` carrying `.spacer`.
- The root carries `aria-hidden="true"`.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- `Style` is appended to the root's inline style after the component's own
  declarations, so the flex sizing stay.
- A consumer `style` attribute binds to `Style` and is merged the same way; it
  does not reach `AdditionalAttributes`.
- For a property both set, the consumer's declaration comes last and wins.

### Flexible

- `Size` defaults to `null`.
- Without `Size`, the root's inline style is `flex: 1 1 auto`, so it grows into
  the parent's free space.
- Without `Size`, the root sets no width or height.

### Fixed

- With `Size`, the root neither grows nor shrinks (`flex: 0 0 auto`).
- With `Size`, the root's width and height are the same spacing value.
- `DrylStack.StackGap.None` sets width and height to zero.
- `DrylStack.StackGap.Xs` maps to `--sp-1`.
- `DrylStack.StackGap.Sm` maps to `--sp-2`.
- `DrylStack.StackGap.Md` maps to `--sp-3`.
- `DrylStack.StackGap.Lg` maps to `--sp-4`.
- `DrylStack.StackGap.Xl` maps to `--sp-5`.
- `DrylStack.StackGap.Xxl` maps to `--sp-6`.

### Keyboard and accessibility

- The spacer is hidden from assistive technology and is never a tab stop.

## Recorded debt

- **Fixed size is square.** A fixed spacer sets both width and height, so in a
  horizontal row it also imposes a minimum height on the row, and vice versa.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/spacer` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by written exception (`DESIGN-11`): an
  empty, invisible layout element has nothing to animate.
- **Keyboard and a11y** — `aria-hidden`, no focus stop; nothing to operate.
- **AI mode** — **no** (`AI-05`): an empty spacer has no content to carry AI
  state.
- **Demo page** — `/components/spacer`
  (`DRYL.Website/Components/Pages/DemoSpacer.razor`), example `Spacer/Toolbar`.
- **`ComponentCatalog`** — registered as `"Spacer"` / `spacer`, flagged not
  AI-capable (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylSpacerTests.cs` (flexible by
  default, fixed size maps to the spacing token).
