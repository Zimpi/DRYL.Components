# DrylContainer

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylContainer.razor

## User Story

As a Blazor developer, I want to wrap a page in a container that caps its
reading width, centres it and pads its sides responsively, so that content is
never edge to edge on a phone and never sprawls on a wide screen.

## Description

`DrylContainer` renders one `div.container` with inline sizing. A `Size`
preset caps its maximum width and centres it with automatic inline margins;
`ContainerSize.Full` drops the cap and spans the full width. In every size the
inline padding grows with the viewport between `--sp-4` and `--sp-6`.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Size` | `DrylContainer.ContainerSize` | `ContainerSize.Lg` | Maximum-width preset. |
| `ChildContent` | `RenderFragment?` | `null` | Container content. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

Nested enum `DrylContainer.ContainerSize`: `Sm`, `Md`, `Lg`, `Xl`, `Full`.

## Acceptance Criteria

### Structure

- The component renders one `div` with class `container`.
- `ChildContent` renders inside it.
- `Class` is appended to the container's class list.
- Unmatched attributes are applied to the container.

### Size

- `Size` defaults to `ContainerSize.Lg`.
- `ContainerSize.Sm`, `Md`, `Lg` and `Xl` each set an inline `max-width`.
- The four capped sizes grow strictly in the order `Sm` < `Md` < `Lg` < `Xl`.
- A capped container is centred by automatic inline margins.
- `ContainerSize.Full` renders no `max-width`.
- `ContainerSize.Full` spans the full width of its parent.

### Padding

- The inline padding is never less than `--sp-4`.
- The inline padding is never more than `--sp-6`.
- Between those bounds the inline padding scales with the viewport width.
- Every size, `ContainerSize.Full` included, carries the same inline padding.

## Recorded debt

- **Literal widths.** The four maximum widths are rem literals in the
  component's C# (`DrylContainer.Style`), not tokens (`DESIGN-01`); the
  padding's viewport factor is a literal too.
- **A consumer `style` replaces the sizing.** The splatted attributes come
  after the computed `style`, so a consumer `style` drops the width cap, the
  centring and the padding.
- **Viewport-relative padding.** The padding scales with the viewport, not
  with the container's slot, so a container in a narrow panel on a wide screen
  takes the wide padding.
- **`.container` has no rule of its own.** All sizing is inline; the class is
  only a hook.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/container` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by explicit exception (`DESIGN-11`): a
  static width wrapper with no state, no interaction and no conditional mount.
- **Keyboard and a11y** — no role, no focus stop; the container adds no
  semantics to its content.
- **AI mode** — **no** `Ai` parameter: a width wrapper holds no model-written
  content of its own (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoContainer.razor`, route
  `/components/container`, example `Container/Sizes`.
- **`ComponentCatalog`** — `"Container"` / `container`, group `Layout`, AI
  flag `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylContainerTests.cs`.
