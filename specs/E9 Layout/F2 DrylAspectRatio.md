# DrylAspectRatio

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylAspectRatio.razor
              code/DRYL.Components/Components/Layout/DrylAspectRatio.razor.css

## User Story

As a Blazor developer, I want a box that holds a fixed aspect ratio for an
image, video or embed and never grows past its slot, so that media keeps its
shape at every width without hand-written CSS.

## Description

`DrylAspectRatio` renders one `div.aspect` whose inline `aspect-ratio` comes
from a preset or a custom value. The box takes its width from its slot,
derives its height from the ratio and clips its content. Every direct child is
stretched to fill the box; images, videos and iframes additionally crop to
cover instead of distorting. Inside a grid or flex row the box opts out of
stretching so the row's height cannot override the ratio.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Ratio` | `DrylAspectRatio.AspectRatio` | `AspectRatio.Video` | Ratio preset. |
| `RatioValue` | `string?` | `null` | Custom CSS `aspect-ratio` value; read only when `Ratio` is `AspectRatio.Custom`. |
| `ChildContent` | `RenderFragment?` | `null` | The boxed media or content. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `Style` | `string?` | `null` | Inline declarations merged after the component's own; a `style` attribute binds here too. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

Nested enum `DrylAspectRatio.AspectRatio`: `Square`, `Video`, `Photo`, `Wide`,
`Custom`.

## Acceptance Criteria

### Structure

- The component renders one `div` with class `aspect`.
- `ChildContent` renders inside the box.
- `Class` is appended to the box's class list.
- Unmatched attributes are applied to the box.
- `Style` is appended to the root's inline style after the component's own
  declarations, so the ratio, the width cap and the clipping stay.
- A consumer `style` attribute binds to `Style` and is merged the same way; it
  does not reach `AdditionalAttributes`.
- For a property both set, the consumer's declaration comes last and wins.

### Ratio

- `Ratio` defaults to `AspectRatio.Video`.
- `AspectRatio.Video` sets the inline `aspect-ratio` to `16 / 9`.
- `AspectRatio.Square` sets it to `1 / 1`.
- `AspectRatio.Photo` sets it to `4 / 3`.
- `AspectRatio.Wide` sets it to `21 / 9`.
- `AspectRatio.Custom` with a non-blank `RatioValue` writes `RatioValue`
  verbatim as the `aspect-ratio`.
- `AspectRatio.Custom` with a null or blank `RatioValue` falls back to
  `16 / 9`.
- `RatioValue` is ignored for every preset other than `AspectRatio.Custom`.

### Sizing and content

- The box never grows wider than its container (inline `max-width` of the
  full container width).
- Content overflowing the box is clipped.
- Inside a grid or flex container the box aligns to the start of the cross
  axis instead of stretching, so the ratio decides its height.
- Every direct child of the box fills its full width and height as a block.
- A direct `img`, `video` or `iframe` child crops with `object-fit: cover`.
- A direct `img`, `video` or `iframe` child renders without a border.

## Recorded debt

- **`RatioValue` is unchecked.** It is written into the inline style as given;
  an invalid value leaves the box without a ratio.
- **The `AspectRatio.Custom` fallback is silent.** A blank `RatioValue` renders
  a video box with no hint of the missing value.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/aspect-ratio` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by explicit exception (`DESIGN-11`): the
  box is a static sizing primitive with no state, no interaction and no
  conditional mount; its content carries any motion.
- **Keyboard and a11y** — the box adds no role, no focus stop and no
  semantics; the content keeps its own (an `img` keeps its `alt`).
- **AI mode** — **no** `Ai` parameter: a sizing box holds no model-written
  content of its own (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoAspectRatio.razor`,
  route `/components/aspect-ratio`, example `AspectRatio/Ratios`.
- **`ComponentCatalog`** — `"Aspect Ratio"` / `aspect-ratio`, group `Layout`,
  AI flag `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylAspectRatioTests.cs`.
