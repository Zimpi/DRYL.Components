# Keyboard editing and announcements

## Meta
- **State:** Modified

Stage two — edit. Every change `S6` makes by pointer is reachable by keyboard
(`UX-01`). Every criterion here assumes `Editable` is set; with it left `false`
the keyboard behaves as `S4` describes.

## Acceptance Criteria

### The slot grid

- With `Editable` set, the time grid of the day and week views carries the grid
  role, with a row per slot and a cell per day or person column.
- With `Editable` set, the all-day row is a row of that grid, with a cell per
  day or person column.
- With `Editable` set, the month view carries the grid role, with a row per week
  and a cell per day.
- The grid is one tab stop, placed after the legend and before the events.
- Exactly one cell of the grid is focusable at a time.
- When the grid is first focused, the cell of `Date` at `InitialScrollTime` is
  focused, or the first cell of `Date` in the month view.
- `ArrowUp` and `ArrowDown` move the focused cell by one slot in the time grid,
  and by one week in the month view.
- `ArrowUp` from the first slot of the time grid moves the focused cell into the
  all-day row.
- `ArrowLeft` and `ArrowRight` move the focused cell by one day or one person
  column.
- `Home` and `End` move the focused cell to the first and last cell of its row.
- The focused cell never leaves the visible range.
- The focused cell is scrolled into view when it moves.
- A focused cell's accessible name states its day, its time or that it is the
  all-day row, and its person when the grid has person columns.
- The arrow keys do not scroll the page while the grid has focus.

### Creating by keyboard

- `Enter` on a focused cell starts a create of that one slot or day.
- While a create is under way, `Shift`+`ArrowDown` and `Shift`+`ArrowUp` extend
  and shrink it by one slot in the time grid.
- While a create is under way, `Shift`+`ArrowRight` and `Shift`+`ArrowLeft`
  extend and shrink it by one day in the all-day row and the month view.
- A create cannot shrink below one slot or one day.
- While a create is under way, a preview of the span is drawn, as by pointer.
- `Enter` completes the create and fires `OnEventChangeRequested` once.
- `Escape` cancels the create.
- Moving focus out of the calendar cancels the create.

### Moving and resizing an event by keyboard

- `Enter` and `Space` on a focused event with no change under way fire
  `OnEventClick`, as in stage one.
- An arrow key on a focused editable event starts a move and applies the first
  step.
- While a move is under way, `ArrowUp` and `ArrowDown` move the preview by one
  slot in the time grid.
- While a move is under way, `ArrowLeft` and `ArrowRight` move the preview by one
  day, or by one person column in person columns.
- In the month view and the all-day row, `ArrowUp` and `ArrowDown` move the
  preview by one week, within the visible range.
- `Shift`+`ArrowDown` and `Shift`+`ArrowUp` on a focused editable event in the
  time grid start a resize of its end, or continue one, by one slot.
- `Shift`+`ArrowRight` and `Shift`+`ArrowLeft` on a focused editable bar start a
  resize of its last day, or continue one, by one day.
- A keyboard move and a keyboard resize keep to the limits a pointer gesture
  keeps to (`S6` "Moving", "Resizing", "Reassigning across person columns").
- While a change is under way, `Enter` completes it and fires
  `OnEventChangeRequested` once.
- While a change is under way, `Escape` cancels it.
- While a change is under way, moving focus away from the event cancels it.
- A key press during a change fires no callback; only `Enter` does.
- An arrow key on a focused `ReadOnly` event does nothing and fires nothing.
- A keyboard change keeps focus on the event, wherever the preview has moved it.
- The arrow keys do not scroll the page while a change is under way.

### Describing what can be done

- An editable event is described to assistive technology by the
  `CalendarLabels.EditHint` text.
- An editable cell of the grid is described by the `CalendarLabels.CreateHint`
  text.
- An event that is `ReadOnly` carries neither hint.

### Announcements

- The start of a keyboard move, resize or create is announced through the
  calendar's polite live region.
- Every step of a keyboard change announces the preview's day, start and end
  time, and its person when it has moved column.
- Every step announces the conflicts the preview would have, in the
  `CalendarLabels` texts of `S8`, or nothing about conflicts when there are
  none.
- A completed change is announced with the `CalendarLabels.Requested` text.
- A cancelled change is announced with the `CalendarLabels.Cancelled` text.
- A change the host did not apply is announced as `S5` says.
- A pointer gesture announces its completion and its conflicts as a keyboard
  change does, and does not announce every slot it passes.
- Announcements go through the one live region `S4` already uses for range
  changes; the calendar renders no second one.

### Focus after a change

- After the host applies a move or a resize, the event keeps focus.
- After the host applies a create made by keyboard, focus moves to the event in
  `Events` whose `Id` equals `Proposed.Id`.
- After a create made by keyboard, focus stays on the cell when no event in
  `Events` carries `Proposed.Id`, so a host that assigns its own id leaves
  focus where the user was.
- After a change the host did not apply, focus returns to the event in its
  place, or to the focused cell for a create.

## Recorded gaps

*(none yet — filled when the story is implemented.)*
