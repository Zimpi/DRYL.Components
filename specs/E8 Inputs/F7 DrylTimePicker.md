# DrylTimePicker

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylTimePicker.razor
              code/DRYL.Components/Components/Inputs/DrylTimePicker.razor.css

## User Story

As a Blazor developer building a form on DRYL, I want a time field that opens
an hour and a minute column, lets the user build a time and commit or cancel
it, and takes part in `EditForm` validation, so that I can bind a `TimeOnly?`
without writing a time picker of my own.

## Description

`DrylTimePicker` is an `InputBase<TimeOnly?>`: a read-only text field showing
`HH:mm` that opens a panel with two columns of buttons — hours and minutes —
and a `Cancel` / `Done` footer. Picking a cell only stages the value; `Done`
(or `Enter` in the panel) commits it, `Cancel` or `Escape` throws it away.

**It is a consumer of `E11 Surfaces`/`F1 DrylPopover`** (portal, placement,
click-outside, entrance and exit), opened with `Surface="false"`,
`TriggerTogglesOpen="false"` and `CloseOnEscape="false"`; the panel announces
itself as a `dialog`, so the popover keeps `Tab` cycling inside it.

**Each column is a list-ink host.** Both `.time-col` elements carry
`data-dryl-glide=".time-cell"`, so `dryl.motion.glideScan` lays the list ink
(idea I38) under the cell under the mouse or the focused cell, one ink per
column — see `E11 Surfaces/_Interop.md`. The column ink drops the leading bar
the menu rows carry, because a cell is centred digits.

## Public API

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Value` / `ValueChanged` / `ValueExpression` | `TimeOnly?` | — | The bound time (`InputBase`). |
| `Label` | `string?` | `null` | Visible label above the field. |
| `Placeholder` | `string?` | `null` | Shown while empty; falls back to `hh:mm`. |
| `HelperText` | `string?` | `null` | Text below; replaced by validation messages. |
| `State` | `InputState` | `Default` | `Default`, `Error` or `Success` border treatment. |
| `Disabled` | `bool` | `false` | Disables the field; the panel does not open. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is not set. |
| `Min` | `TimeOnly?` | `null` | Earliest selectable time. |
| `Max` | `TimeOnly?` | `null` | Latest selectable time. |
| `MinuteStep` | `int` | `1` | Minute granularity of the minute column. |
| `Ai` | `AiState` | `None` | AI ambient state of the field. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |

## Acceptance Criteria

### Field and value

- The field is a read-only text input showing the bound time as `HH:mm`.
- The field is empty while the value is `null`.
- The field carries `aria-haspopup="dialog"` and an `aria-expanded` that is `true` exactly while the panel is open.
- A validation message for the field replaces `HelperText` and sets `aria-invalid="true"`.
- The field works without an explicit `ValueExpression`.

### Panel

- Focusing or clicking the field opens the panel, unless the field is disabled.
- The panel carries `role="dialog"` and an accessible name.
- The hour column lists every hour from `Min`'s hour (or 0) to `Max`'s hour (or 23).
- The minute column lists every multiple of `MinuteStep` within the hour's bounds.
- Opening stages the bound time, or 12:00 when there is none, with the minute snapped to `MinuteStep`.
- The staged hour and minute cells carry `.is-selected`.
- Opening scrolls each column to its staged cell and moves focus into the panel.
- A click on a cell stages that hour or minute without closing the panel.
- `Done` and `Enter` in the panel commit the staged time, clamped to `Min` and `Max`, and close.
- `Cancel` and `Escape` close without changing the value.
- Closing returns focus to the field without reopening the panel.

### Appearance and motion

- The panel surface uses `--panel-float` with `--glass-fx-float`, `--line-strong`, `--r-lg` and `--shadow-md`.
- Each `.time-col` carries `data-dryl-glide=".time-cell"`, which makes it a list-ink host of its own.
- While the ink is active, the cell under the mouse or the focused cell is covered by the column's ink — the one the user acted on last.
- The column ink has the `--r-xs` radius of a cell and no leading bar.
- While the ink is active, cells draw no hover fill of their own; the covered cell's text brightens to `--fg`.
- When the mouse leaves a column, its ink falls back to the focused cell, or fades when no cell there has focus.
- The ink glides between cells on `--dur-slow` / `--ease-spring` and appears without travel when it was hidden.
- Without the ink (no script), a hovered cell keeps a `--glass-2` fill.
- A staged cell keeps its `--accent-soft` fill whether or not it is covered.
- Under `prefers-reduced-motion` the ink moves without a transition.
- The component branches on no colour mode (`DESIGN-02`).

### AI mode

- `Ai` drives the shared aura on the field's wrapper.
- An unset `Aura` inherits the surrounding `DrylAiScope`, falling back to `AiAura.Comet`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — measured on `/components/timepicker` in dark and
  light with the first picker open and the pointer over a cell: both columns
  are ink hosts, the hovered cell carries the ink, the staged cells keep their
  `--accent-soft` fill. The stylesheet has no mode selector.
- **Enter/exit animation** — **yes**, the popover's; the ink fades in on first
  placement and glides afterwards.
- **Keyboard and a11y** — opening moves focus into the panel
  (`dryl.timepicker.focusPanel`), `Tab` cycles within it, `Enter` commits,
  `Escape` cancels and focus returns to the field (`dryl.timepicker.restoreFocus`).
- **AI mode** — **yes**: a model may propose a time, so the field carries `Ai`
  and `Aura` like the other inputs (`AI-05`).
- **Demo page** — `/components/timepicker` in `DRYL.Website`, built from the
  `Basics`, `LivePreview`, `MinMax`, `States`, `Validation` and `AiMode`
  examples under `Components/Examples/TimePicker/`.
- **`ComponentCatalog`** — registered as `"Time Picker"` / `timepicker`, group
  `Inputs`, `ClassName` `"DrylTimePicker"`, AI flag `true` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ListGlideTests.cs` covers the ink
  host markers; the ink's behaviour is covered in `tests/js/glide.test.mjs`.

## Recorded debt (`State: Implemented`)

**Deviations from the acceptance criteria above: none.**

- **No arrow keys in the columns.** Cells are buttons reached by `Tab`; there is
  no `ArrowUp`/`ArrowDown` within a column and no `ArrowLeft`/`ArrowRight`
  between them. The ink follows focus, so it would follow arrow keys the day
  they exist. Left out of I38 on purpose: keyboard behaviour is a decision, not
  a side effect of a visual rework.
- **The columns are not listboxes.** They are `div`s of buttons named `Hours`
  and `Minutes`; a screen reader hears a group of buttons, not a list with a
  selected entry. The staged cell is marked only visually.
- **Fixed English strings** — `Pick a time`, `Hours`, `Minutes`, `Cancel`,
  `Done`, `hh:mm` and the parse error are not translatable.
