# Pointer and touch editing

## Meta
- **State:** Modified

Stage two — edit. Every criterion here assumes `Editable` is set and the event
concerned is not `ReadOnly` (`S5`). What a finished gesture reports is `S5`;
the keyboard path to the same changes is `S7`.

## Acceptance Criteria

### The gesture runs in the browser

- A gesture follows the pointer without a round trip to the server per pointer
  move.
- A gesture reports to the server once, when it is completed.
- The calendar re-renders nothing while a gesture is under way.
- A gesture whose pointer is cancelled by the browser is cancelled.
- `Escape` during a gesture cancels it.
- A cancelled gesture leaves every event where it was.
- A gesture under way is cancelled when the calendar re-renders with a different
  visible range, view or `PersonLayout`.

### Snapping

- In the time grid, a moved or resized event snaps to slot boundaries of
  `SlotMinutes`.
- A moved timed event keeps the minutes past the slot boundary its `Start` had,
  so an event starting off the slot grid moves by whole slots.
- In the all-day row and in the month view, a moved or resized event snaps to
  whole days.
- A move in the month view keeps the time of day of a timed event and changes
  its date only.

### Click or drag

- A press on an event that is released before the pointer has left the slot it
  was pressed in is a click and fires `OnEventClick`, as in stage one.
- In the month view, the same holds for the day cell the press started in.
- A press on an event becomes a move once the pointer has left that slot or day
  cell.

### Moving

- Dragging an event moves it, keeping its duration.
- During the move, a preview of the event is drawn at the place it would land.
- During the move, the event's original place stays visible, drawn faded.
- During the move, the preview names the start and end time it would land at.
- Releasing the pointer completes the move.
- A timed event can be moved within the time grid only, and an all-day event
  within the all-day row or the month view only; a timed event never becomes an
  all-day event by a move, nor the reverse.
- In the week view, a move can change the day within the visible week.
- A move never leaves the visible range: at the edge of the range the preview
  stops.
- A move never takes an event before `DayStart` or past `DayEnd` of the grid.
- Every segment of an event that crosses midnight moves the whole event.

### Reassigning across person columns

- In the day view with `CalendarPersonLayout.Columns`, dragging an event into
  another person's column reassigns it.
- A reassigned event's `Proposed.PersonIds` replaces the person of the column
  the drag started in with the person of the column it ends in.
- A reassigned event's other people stay in `Proposed.PersonIds`, in their
  order.
- Reassigning to a person already in `PersonIds` removes the starting column's
  person and adds no duplicate.
- Dragging an event into the unassigned column removes the starting column's
  person from `Proposed.PersonIds`.
- Dragging an unassigned event into a person's column adds that person to
  `Proposed.PersonIds`.
- In the overlaid layout, no move changes `PersonIds`.

### Resizing

- An event in the time grid can be resized at its top edge, which changes its
  `Start`.
- An event in the time grid can be resized at its bottom edge, which changes its
  `End`.
- A bar in the all-day row or the month view can be resized at its leading
  edge, which changes its first day.
- A bar in the all-day row or the month view can be resized at its trailing
  edge, which changes its last day.
- An event cannot be resized to less than one slot in the time grid, or to less
  than one day as a bar.
- A resize never takes an edge past the other edge.
- The edge of a segment that continues from or into another day is not a resize
  handle.
- A single-line event of the month view has no resize handle.

### Creating

- Pressing on an empty part of the time grid and dragging selects a span of
  slots.
- The selected span is drawn as a preview while the pointer moves.
- Releasing the pointer requests a create covering the selected span.
- A press on an empty slot that is released inside that slot requests a create
  of that one slot.
- Pressing on an empty part of the all-day row or of a month day cell and
  dragging selects a span of days.
- A press on an empty month day cell that is released inside it requests a
  create of that one day as an all-day event.
- A press on a muted availability area creates as on any empty part of the grid.
- A press on a day header, a day number or an overflow control creates nothing.

### Resize handles

- A pointer that hovers an editable event shows its resize handles.
- A focused editable event shows its resize handles.
- No other event shows resize handles.
- A resize handle is drawn with `--accent-line` as a small indicator, never as a
  fill of the event's box (`DESIGN-08`).
- Resize handles are hidden from assistive technology; the keyboard path to a
  resize is `S7`.

### Touch

- A touch that starts on an event and moves before the hold is complete scrolls
  the grid and changes nothing.
- A touch held on an event for `--dur-slow` without moving grabs it.
- A grabbed event is moved by the finger as by a pointer.
- While an event is grabbed, the grid and the page do not scroll.
- A touch held on an empty part of the grid for `--dur-slow` starts a create.
- A tap on an event fires `OnEventClick`, as in stage one, and focuses it, so
  its resize handles show.
- Touching a resize handle of the focused event resizes it at once, without a
  hold.

### Scrolling during a gesture

- A gesture whose pointer nears the top or bottom edge of the time grid's
  scroller scrolls the grid in that direction.
- In the narrow person columns, a gesture whose pointer nears the left or right
  edge scrolls the columns in that direction.
- The scrolling stops when the pointer leaves the edge or the gesture ends.

### Motion while editing

- An event that is grabbed lifts, with `--shadow-md`, at `--dur-fast` with
  `--ease-out`.
- The preview follows the pointer from slot to slot with a transition at
  `--dur-fast` with `--ease-out` rather than jumping.
- A completed gesture settles the preview into its place at `--dur-fast` with
  `--ease-out`.
- A change the host did not apply returns the event to its place at `--dur-med`
  with `--ease-out`.
- A created event the host did not apply exits through the same transition as
  an event leaving `Events` (`S4` "Motion").
- A cancelled gesture returns the preview to the event's place at `--dur-fast`
  with `--ease-out`.
- Under `prefers-reduced-motion: reduce` none of these transitions runs, and the
  preview moves from slot to slot at once (`UX-06`).
- No transition of this section changes the tab order (`UX-07`).

### Narrow layouts

- In the narrow week view, the column of `Date` is editable as the day view is.
- In the narrow week view, a move cannot change the day.
- The list of events below the narrow month view is not editable.
- In the narrow month view, the marks per day are not editable.
- In the narrow month view, a press on a day sets `Date` as in stage one and
  creates nothing.
- The narrow person columns are editable as the wide ones are.

## Recorded gaps

*(none yet — filled when the story is implemented.)*
