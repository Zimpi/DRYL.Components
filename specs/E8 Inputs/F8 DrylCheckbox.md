# DrylCheckbox

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylCheckbox.razor

## User Story

As a Blazor developer building a form or a settings page, I want a bound
checkbox whose check visibly draws itself when the user ticks it, so that a
yes/no choice feels as deliberate as the rest of the library without my writing
any motion of my own.

## Description

`DrylCheckbox` renders one native `input type="checkbox"` carrying `.checkbox`
inside a `label` row, followed by its label text. It inherits
`InputBase<bool>` and binds through `@bind-Value`.

**The look is idea I39, variant "Tinte".** The accent gradient fills the box on
its own layer (`.checkbox::before`), springing up from the centre, so it fades
in rather than snapping. The white check is a mask on `.checkbox::after` that a
`clip-path` wipe draws from left to right; unticking erases it faster than it
drew. Pressing squashes the box. After the user has changed the box once, every
move into the checked state sends one glow ring outwards (`dryl-choice-pulse`).
The ring is gated on `data-touched`, which the component sets on its first
change, so a page never pulses on first render.

The `.checkbox` look is shared library CSS. `DrylTable` and `DrylMultiSelect`
render bare `.checkbox` inputs and get the fill, the drawn check and the press,
but no pulse, because they do not set `data-touched`.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<bool>`; the inherited
binding members follow the `InputBase<bool>` contract in [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Plain-text label after the box. |
| `ChildContent` | `RenderFragment?` | `null` | Rich label; wins over `Label`. |
| `Disabled` | `bool` | `false` | Native disabled state. |
| `AriaLabel` | `string?` | `null` | Accessible name when neither `Label` nor `ChildContent` is set. |

There is no `Ai` parameter and no indeterminate state.

## Acceptance Criteria

### Value and structure

- The component renders one native `input type="checkbox"` carrying `.checkbox`.
- The input is checked when the bound `Value` is `true`.
- A native `change` event writes the new value through `ValueChanged`.
- The input sits in a `label` carrying `.row` and `.choice`, so a click on the
  text toggles the box.
- `ChildContent` is rendered in a `.choice-label` span; otherwise a nonempty
  `Label` is.
- With neither, no label span is rendered and `AriaLabel` becomes the input's
  `aria-label`.
- With a visible label, the component's `AriaLabel` is omitted.
- Neither the row nor the label span carries an inline `style`.
- `AdditionalAttributes` are applied to the input.

### Disabled

- `Disabled` sets the native `disabled` attribute.
- A disabled checkbox adds `.choice--disabled` to its row.
- The disabled box reads at reduced opacity with a `not-allowed` cursor; its
  label reads in `--fg-dim`.
- A disabled box neither shows the hover border nor squashes on press.

### Motion

- The box transitions `border-color` on `--dur-fast`, `box-shadow` on
  `--dur-med` and `transform` on `--dur-med` / `--ease-spring`; it names its
  properties and never uses `transition: all` (`DESIGN-10`).
- The fill layer moves into the checked state with an opacity fade on
  `--dur-fast` and a scale from half size on `--dur-med` / `--ease-spring`.
- Checking draws the check left to right on `--dur-med` / `--ease-out`;
  unchecking erases it on `--dur-fast` / `--ease-in-out`.
- Pressing an enabled box scales it down on `--dur-fast`; releasing springs it back.
- The component renders no `data-touched` on first render.
- The first user change sets `data-touched` on the input; it stays set.
- A touched box moving into the checked state plays `dryl-choice-pulse` once,
  on `--dur-slow` / `--ease-out`, ending on the checked state's resting glow.
- Under `prefers-reduced-motion: reduce`, the box, its fill and its check have
  no transition and no animation.

### Appearance and focus

- The box reads `--glass-1`, a `--line-strong` border and the `--r-xs` radius.
- Hover on an enabled box turns the border `--accent-line`.
- Checked, the border is transparent, the fill is `--accent-grad`, the check is
  `--knob`, and the box carries a soft accent glow.
- Keyboard focus shows a 2 px accent outline offset by 2 px (`:focus-visible`).
- The label text reads `.choice-label`: 13 px, `--fg-muted`.

### Keyboard and accessibility

- The box is a native checkbox: Tab reaches it, Space toggles it.
- The checked state is the native one and is announced by assistive technology.

## Recorded debt

- **Literal geometry.** The 18 px box, 12 px check, the 9 px pulse spread and the
  13 px label size are literals (`DESIGN-01`); DRYL has no size or type-scale
  token for them.
- **No indeterminate state.** `DrylTable`'s "select all" cannot show a partial
  selection. A separate idea, not part of I39.
- **The first pulse waits for a render.** On Blazor Server the very first tick
  pulses only once the render that sets `data-touched` arrives; later ticks
  pulse at once.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the box reads only mode tokens (`--glass-1`,
  `--line-strong`, `--accent-*`, `--knob`); checked and unchecked boxes were
  inspected on `/components/forms` in dark and light.
- **Enter/exit animation** — **no**: the box does not mount conditionally. Its
  state changes are animated as above (`DESIGN-11`).
- **Keyboard and a11y** — native checkbox semantics; visible `:focus-visible` ring.
- **AI mode** — **no**: a single yes/no box carries no AI-authored content of its
  own; an AI-filled form shows its aura on the form's surfaces.
- **Demo page** — `/components/forms` in `DRYL.Website`, examples
  `Form/Controls` and `Form/Choices`.
- **`ComponentCatalog`** — covered by the shared `"Form Controls"` / `forms`
  entry (`ClassName` null), which names the checkbox in its description.
- **Tests** — `tests/DRYL.Components.Tests/DrylSelectionControlTests.cs`.
