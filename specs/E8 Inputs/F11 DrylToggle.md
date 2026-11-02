# DrylToggle

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylToggle.razor

## User Story

As a Blazor developer building a settings page, I want a bound on/off switch
whose knob feels physical, so that turning an option on reads as a gesture and
not as a repaint.

## Description

`DrylToggle` renders a `label` row with the label text on the left and one
native `input type="checkbox"` carrying `.toggle` on the right. It inherits
`InputBase<bool>` and binds through `@bind-Value`.

**The look is idea I39, variant "Tinte".** The knob (`.toggle::after`) is
positioned by two edges. Moving on, its right edge leads on `--dur-fast` and its
left edge follows on `--dur-slow` / `--ease-spring`; moving off, the roles swap.
The knob therefore stretches in travel and settles round. Pressed, it goes wide
and flat. The track fades into the accent gradient on its own layer
(`.toggle::before`). After the user has flipped the switch once, every move into
the on state sends one glow ring outwards (`dryl-toggle-pulse`), gated on
`data-touched` exactly as in [`F8 DrylCheckbox.md`](F8%20DrylCheckbox.md).

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<bool>`; the inherited
binding members follow the `InputBase<bool>` contract in [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Plain-text label left of the switch. |
| `ChildContent` | `RenderFragment?` | `null` | Rich label; wins over `Label`. |
| `Disabled` | `bool` | `false` | Native disabled state. |
| `AriaLabel` | `string?` | `null` | Accessible name when neither `Label` nor `ChildContent` is set. |

## Acceptance Criteria

### Value and structure

- The component renders one native `input type="checkbox"` carrying `.toggle`.
- The input is checked when the bound `Value` is `true`.
- A native `change` event writes the new value through `ValueChanged`.
- The row is a `label` carrying `.row`, `.choice` and `.choice--end`, with the
  `.choice-label` span first and the switch last.
- `ChildContent` wins over `Label` in the label span.
- With neither, `AriaLabel` becomes the input's `aria-label`.
- Neither the row nor the label span carries an inline `style`.
- `AdditionalAttributes` are applied to the input.

### Disabled

- `Disabled` sets the native `disabled` attribute and adds `.choice--disabled`.
- A disabled switch reads at reduced opacity with a `not-allowed` cursor and
  neither shows the hover border nor deforms on press.

### Motion

- The track transitions `border-color` on `--dur-fast` and `box-shadow` on
  `--dur-med`; the gradient layer fades on `--dur-med`.
- Moving on, the knob's right edge moves on `--dur-fast` / `--ease-out` and its
  left edge on `--dur-slow` / `--ease-spring`.
- Moving off, the knob's left edge moves on `--dur-fast` / `--ease-out` and its
  right edge on `--dur-slow` / `--ease-spring`.
- The knob's color moves from `--fg-muted` to `--knob` on `--dur-med`.
- Pressing an enabled switch widens the knob towards the middle and flattens it
  by 1 px top and bottom, on `--dur-fast`.
- The first user change sets `data-touched` on the input; none is rendered before.
- A touched switch moving on plays `dryl-toggle-pulse` once, on `--dur-slow` /
  `--ease-out`, ending on the on state's resting glow.
- Under `prefers-reduced-motion: reduce`, track, layer and knob have no
  transition and no animation.

### Appearance and focus

- Off, the track reads `--glass-2` with a `--line-strong` border; on, the border
  is transparent, the track is `--accent-grad` and carries a soft accent glow.
- Hover on an enabled switch turns the border `--accent-line`.
- Keyboard focus shows a 2 px accent outline offset by 2 px (`:focus-visible`).

### Keyboard and accessibility

- Tab reaches the switch, Space flips it (native checkbox).

## Recorded debt

- **No `role="switch"`.** The control is announced as a checkbox, not a switch
  (`UX-01`).
- **Literal geometry.** The 36 × 20 px track and the knob's 1 / 2 / 12 / 17 px
  edge offsets are literals (`DESIGN-01`).
- **The first pulse waits for a render** — as for `DrylCheckbox`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/forms` in
  dark and light, on and off.
- **Enter/exit animation** — **no** (does not mount conditionally); state
  changes are animated as above.
- **Keyboard and a11y** — native checkbox semantics, visible focus ring; the
  missing switch role is recorded above.
- **AI mode** — **no**, for the same reason as `DrylCheckbox`.
- **Demo page** — `/components/forms`, examples `Form/Controls` and `Form/Choices`.
- **`ComponentCatalog`** — the shared `"Form Controls"` / `forms` entry.
- **Tests** — `tests/DRYL.Components.Tests/DrylSelectionControlTests.cs`.
