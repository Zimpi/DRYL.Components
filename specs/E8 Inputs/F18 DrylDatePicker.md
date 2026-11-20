# DrylDatePicker

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylDatePicker.razor
              code/DRYL.Components/Components/Inputs/DrylDatePicker.razor.css

## User Story

As a Blazor developer building a form on DRYL, I want a date field that opens
a keyboard-navigable month calendar, respects a minimum and maximum date and
can also pick a range, so that I can bind a `DateOnly?` (or a start and end
date) without writing a calendar of my own.

## Description

`DrylDatePicker` is an `InputBase<DateOnly?>`: a read-only text field showing
`dd.MM.yyyy` with a trailing calendar icon. Focusing or clicking it opens a
panel with a month header (previous/next chevrons) and a Monday-first grid of
six weeks. In single mode, choosing a day commits it and closes the panel. In
`Range` mode, the first choice sets the start, the second the end (swapped if
earlier), then the panel closes; the range is bound through `RangeStart` /
`RangeEnd` instead of `Value`.

**It is a consumer of `E11 Surfaces`/`F1 DrylPopover`** (portal, placement,
outside dismissal, entrance and exit), opened with `Block="true"`,
`Surface="false"`, `TriggerTogglesOpen="false"` and `CloseOnEscape="false"`.
Opening moves focus onto the focused day (`dryl.datepicker.focusDay`), which
also installs the shared panel key policy: `Tab` cycles inside the panel, a
control that activates on `Enter`/`Space` handles that key itself, and the
browser defaults of the navigation keys are suppressed. Closing hands focus
back to the field (`dryl.datepicker.restoreFocus`) without reopening the panel.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<DateOnly?>` and implements
`IDisposable`. The inherited binding members follow the
`InputBase<DateOnly?>` contract in [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the field. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Disables the field; the panel does not open. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state of the field's aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |
| `Min` | `DateOnly?` | `null` | Earliest selectable date, inclusive. |
| `Max` | `DateOnly?` | `null` | Latest selectable date, inclusive. |
| `Range` | `bool` | `false` | Range mode; binds `RangeStart` / `RangeEnd`. |
| `RangeStart` | `DateOnly?` | `null` | Start of the range (range mode). |
| `RangeStartChanged` | `EventCallback<DateOnly?>` | — | Fires when the range start changes. |
| `RangeEnd` | `DateOnly?` | `null` | End of the range (range mode). |
| `RangeEndChanged` | `EventCallback<DateOnly?>` | — | Fires when the range end changes. |

## Acceptance Criteria

### Field

- The field is a read-only `input type="text"`.
- The field carries `.input` and `.has-trailing-icon`.
- A decorative `Calendar` icon follows the input.
- In single mode, the field shows the value as `dd.MM.yyyy`.
- In single mode, the field is empty while the value is `null`.
- In range mode with both ends set, the field shows `dd.MM.yyyy – dd.MM.yyyy`.
- In range mode with only `RangeStart` set, the field shows the start as `dd.MM.yyyy`.
- The placeholder is `dd.MM.yyyy` in single mode.
- The placeholder is `Select range…` in range mode.
- The field carries `aria-haspopup="dialog"`.
- The field's `aria-expanded` is `true` exactly while the panel is open.
- In range mode, the component works without an explicit `ValueExpression`.
- A string parsed through `CurrentValueAsString` is accepted as empty or in the exact format `dd.MM.yyyy`.
- Any other string produces the parsing message `Invalid date. Expected format: dd.MM.yyyy`.

### Opening and closing

- Focusing or clicking the enabled field opens the panel.
- A disabled field does not open the panel.
- Opening shows the month of the value, or the current month when there is none.
- Opening focuses the value's day, or today when there is none.
- While the panel is closed, a new value moves the shown month to the value's month.
- Focus returning to the field while the panel is open sends focus back into the panel.
- `Escape` in the panel closes it without changing the value.
- `Escape` on the field closes an open panel.
- After `Escape` or a selection closes the panel, focus returns to the field.
- The focus returned after a close does not reopen the panel.
- An outside press closes the panel.
- A close reported by the popover (`OnClose`) resets range selection to the start.

### Panel and grid

- The panel carries `role="dialog"`.
- The panel carries `tabindex="-1"`.
- The panel's `aria-label` names the shown month.
- The header renders a `Previous month` and a `Next month` icon button around the month label.
- The chevrons page the shown month by one.
- The grid carries `role="grid"` and renders six `role="row"` weeks of seven cells.
- The weekday header row reads `Mo` to `Su` and is hidden from assistive technology.
- Cells before the first and after the last day of the month are empty, `aria-hidden` spans.
- Each day is a `button` with `role="gridcell"` and an `aria-label` of its full date.
- Only the focused day carries `tabindex="0"`; every other day carries `tabindex="-1"`.
- Each day carries `data-day` with its `DateOnly.DayNumber`.
- Today's cell carries `.is-today`.
- A day before `Min` or after `Max` carries `.is-disabled`.
- A day before `Min` or after `Max` carries the native `disabled` attribute.
- A disabled day cannot be selected by click or key.

### Selection

- In single mode, choosing a day writes it through `CurrentValue` and closes the panel.
- In single mode, the selected day carries `.is-selected`.
- A selected day carries `aria-selected="true"`.
- A selected day carries `aria-pressed="true"`.
- In range mode, the first choice invokes `RangeStartChanged` with the day.
- In range mode, the first choice invokes `RangeEndChanged` with `null`.
- In range mode, the second choice invokes `RangeStartChanged` and `RangeEndChanged` with the earlier and the later day.
- In range mode, the second choice closes the panel.
- In range mode, the start day carries `.is-range-start`.
- In range mode, the end day carries `.is-range-end`.
- In range mode, days strictly between start and end carry `.is-in-range`.
- In range mode, the panel shows `Select start date` before the first choice.
- In range mode, the panel shows `Select end date` after the first choice.

### Keyboard

- `ArrowRight` and `ArrowLeft` move the focused day by one day.
- `ArrowDown` and `ArrowUp` move the focused day by seven days.
- Moving the focused day into another month shows that month.
- `PageDown` and `PageUp` show the next or previous month, keeping the day number clamped to the month's length.
- `Home` moves the focused day to the Monday of its week.
- `End` moves the focused day to the Sunday of its week.
- `Enter` or `Space` selects the focused day.
- `Enter` or `Space` on a month chevron pages the month without selecting a day.
- `Tab` and `Shift+Tab` cycle among the panel's focusable controls.
- Each focus move is applied to the day's button through `dryl.datepicker.focusDay`.

### Validation and state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- Validation messages replace `HelperText`, joined by a space, in `.input-helper.input-helper-error`.
- A validation message sets `aria-invalid="true"` on the field.
- A displayed helper or validation message is referenced by the field's `aria-describedby`.
- When `Label` is empty, `AriaLabel` is applied as `aria-label`.

### Appearance and motion

- The panel surface uses `--panel-float` with `--glass-fx-float`.
- The panel has a `--line-strong` border, the `--r-lg` radius and `--shadow-lg`.
- Day cells have the `--r-sm` radius.
- Day cells read `--fg-muted` at rest.
- A hovered, enabled day reads `--fg` on `--glass-2`.
- Day background and color transition on `--dur-fast` / `--ease-out`.
- A keyboard-focused day shows an outline in `--accent-b`.
- Today's cell carries an `--accent-line` border.
- Selected days and range ends fill with `--accent-grad`.
- Days inside a range fill with `--accent-soft`.
- Panel entrance and exit are the popover's.
- The stylesheet branches on no colour mode (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`.
- A non-`None` `Ai` renders the shared aura on the field's `.input-wrapper` through `DrylAuraElements`.
- An unset `Aura` inherits the surrounding `DrylAiScope`'s variant, falling back to `AiAura.Comet`.
- Disposing the component disposes its `AuraLifecycle`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope; listed as an I32 follow-up in
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
- **No `ValueExpression` fallback in single mode.** Only range mode supplies one
  (pointing at a private dummy field); a single-mode `Value` without
  `@bind-Value` or an explicit `ValueExpression` is rejected by `InputBase`.
- **Range mode is outside `EditForm` validation.** Its `FieldIdentifier` is the
  private dummy field, and `RangeStart`/`RangeEnd` changes notify no
  `EditContext`.
- **Grid semantics are mixed.** Day buttons carry `role="gridcell"` together
  with `aria-pressed`; the weekday header is hidden from assistive technology,
  so column headers are not announced.
- **Fixed format and English strings** — `dd.MM.yyyy`, `Select range…`, the
  weekday abbreviations, `Previous month`, `Next month`, `Pick a date — …`,
  `Select start date`, `Select end date` and the parsing message; the week
  always starts on Monday. Month names and the day `aria-label` follow the
  current culture while the field format does not.
- **Icon-only chevrons have no `DrylTooltip`** (`UX-05`).
- **Literal values** in `DrylDatePicker.razor.css` — the panel's minimum width,
  the type sizes of `.date-month-label`, `.date-weekday`, `.date-cell` and
  `.date-range-hint`, the 2 px paddings/margins, outline geometry and the
  disabled opacity; the icon sizes passed to `DrylIcon` (`DESIGN-01`).
- **The range hint text swaps without a transition**, and label and helper mount
  without `DrylPresence` (`DESIGN-12`).
- **Single-day ranges** — a day that is both start and end gets only
  `.is-range-start`, so the `.is-range-start.is-range-end` rule never applies.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/datepicker` in dark and light (2026-11-20)
- **Enter/exit animation** — **yes**, the popover's; day state changes are
  transitioned. The swapping range hint is recorded above.
- **Keyboard and a11y** — the "Keyboard" criteria above; opening moves focus
  into the calendar (`dryl.datepicker.focusDay`), `Escape` closes and focus
  returns to the field (`dryl.datepicker.restoreFocus`). The grid-role gaps are
  recorded above.
- **AI mode** — **yes**: a model may propose a date, so the field carries `Ai`
  and `Aura` like the other inputs (`AI-05`).
- **Demo page** — `/components/datepicker`
  (`DRYL.Website/Components/Pages/DemoDatePicker.razor`), with the examples
  `Components/Examples/DatePicker/SingleDate.razor`, `MinMax.razor`,
  `Range.razor`, `Validation.razor`, `AiMode.razor` and `Disabled.razor`.
- **`ComponentCatalog`** — `"Date Picker"` / `datepicker`, group `Inputs`,
  `ClassName` `"DrylDatePicker"`, AI flag `true` (`REL-04`).
- **Tests** — no dedicated bUnit suite; the component appears only in the
  aura-only allowlist of `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
