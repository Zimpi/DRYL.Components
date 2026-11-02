# DrylRadio

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylRadio.razor

## User Story

As a Blazor developer, I want to declare each option of a radio group as its own
element with its own value, so that the group stays readable in markup and each
option animates on its own when it is chosen.

## Description

`DrylRadio<TValue>` is one option of a
[`DrylRadioGroup<TValue>`](F9%20DrylRadioGroup.md). It reads the group's
selection, name and disabled flag from the cascading `RadioGroupContext<TValue>`
and reports a pick through it. It renders a `label.radio` holding a visually
hidden native `input type="radio"` (`.radio-input`), the drawn control
(`.radio-control`) and an optional `.radio-label`.

**The look is idea I39, variant "Tinte".** The accent gradient fills the control
on its own layer (`.radio-control::before`), springing up from the centre; the
white dot (`::after`) springs in on `--ease-spring`. Pressing squashes the
control. Once the user has picked this option, every move into the checked
state sends the shared `dryl-choice-pulse` ring, gated on `data-touched`.

## Public API

Namespace: `DRYL.Components`. A plain component with type parameter `TValue`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `TValue?` | — (`EditorRequired`) | The value this option stands for. |
| `Label` | `string?` | `null` | Text after the control. |
| `Disabled` | `bool` | `false` | Disables this option regardless of the group. |

## Acceptance Criteria

### Structure and selection

- The option renders `label.radio` with `input.radio-input`, `span.radio-control`
  and, for a nonempty `Label`, `span.radio-label`.
- The input's `name` is the group's generated name; its `value` is
  `Value?.ToString()`.
- The input is checked when the group's selected value equals `Value`
  (`EqualityComparer<TValue>.Default`).
- A `change` on the input reports `Value` to the group.
- Outside a group the option renders unchecked and reports nothing.

### Disabled

- The option is disabled when its own `Disabled` or the group's is set.
- A disabled option carries `.radio--disabled`: reduced opacity, `not-allowed`
  cursor, no pointer events.

### Motion

- The control transitions `border-color` on `--dur-fast`, `box-shadow` on
  `--dur-med` and `transform` on `--dur-med` / `--ease-spring`.
- The fill layer fades on `--dur-fast` and scales up from half size on
  `--dur-med` / `--ease-spring`; the dot scales in on `--dur-med` / `--ease-spring`.
- Pressing the option scales the control down on `--dur-fast`.
- No `data-touched` is rendered until the user picks this option; from then on
  it stays set.
- A touched option moving into the checked state plays `dryl-choice-pulse` once
  on `--dur-slow` / `--ease-out`.
- Under `prefers-reduced-motion: reduce`, control, fill and dot have no
  transition and no animation.

### Appearance and focus

- Unchecked, the control reads `--glass-1` with a `--line-strong` border; hover
  turns the border `--accent-line`.
- Checked, the border is transparent, the fill is `--accent-grad`, the dot is
  `--knob`, with a soft accent glow.
- Keyboard focus on the hidden input shows a 2 px accent outline on the control
  (`.radio-input:focus-visible + .radio-control`); a mouse pick shows none.
- The label reads 13 px in `--fg-muted`, matching `.choice-label`.

## Recorded debt

- **Literal geometry** — 18 px control, 8 px dot, 13 px label (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/radio` in
  dark and light.
- **Enter/exit animation** — **no**; state changes are animated as above.
- **Keyboard and a11y** — native radio semantics within the group's
  `role="radiogroup"`; arrow keys move between options natively.
- **AI mode** — **no** on the option; the group carries it.
- **Demo page** — `/components/radio`.
- **`ComponentCatalog`** — through the `"Radio Group"` / `radio` entry.
- **Tests** — `tests/DRYL.Components.Tests/DrylSelectionControlTests.cs`.
