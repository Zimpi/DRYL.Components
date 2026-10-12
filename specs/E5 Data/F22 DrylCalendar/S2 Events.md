# Events

## Meta
- **State:** Implemented

## Acceptance Criteria

### What an event's times mean

- `CalendarEvent.Start` and `CalendarEvent.End` are read as wall-clock times;
  their `DateTime.Kind` has no effect on where the event is drawn.
- `End` is exclusive: an event ending at a time and one starting at that same
  time do not overlap.
- An event whose `End` is not later than its `Start` is drawn one slot long.
- An event with `AllDay` set covers every day from the date of `Start` up to,
  but not including, the date of `End`.
- An event with `AllDay` set whose `End` date is not later than its `Start` date
  covers the date of `Start` alone.
- The time-of-day parts of an all-day event's `Start` and `End` have no effect.
- An event that lies wholly outside the visible range is not rendered.
- Two events with the same `Id` are reported once on the console, and the later
  one is not rendered.

### Placement in the day and week views

- A timed event is drawn in the column of its day, starting at its `Start` and
  ending at its `End`.
- An event's height is proportional to its duration.
- An event shorter than one slot is drawn one slot high, so its title stays
  readable.
- An event that starts before `DayStart` is drawn from the top of the grid.
- An event that ends after `DayEnd` is drawn to the bottom of the grid.
- An event that lies wholly outside `DayStart` to `DayEnd` on a day is not drawn
  in that day's column.
- A timed event that crosses midnight is drawn as one segment in each day it
  touches.
- A segment that continues from the previous day, or into the next, is marked as
  continuing at that edge.
- A timed event lasting twenty-four hours or more is drawn in the all-day row
  instead of the time grid.

### Overlapping events

- Events of one column that overlap in time are drawn side by side rather than
  on top of each other.
- Events that do not overlap any other use the full width of their column.
- Side-by-side events are ordered by `Start`, and by longer duration first when
  they start together.
- An event that overlaps a group only later takes the first free position rather
  than adding a new one.

### All-day and multi-day events

- The day and week views render an all-day row between the day headers and the
  time grid.
- An all-day event is drawn in the all-day row as a bar spanning its days.
- A bar that starts before the visible range is marked as continuing at its
  leading edge.
- A bar that ends after the visible range is marked as continuing at its
  trailing edge.
- Bars that share a day are stacked in separate lanes.
- The all-day row grows to fit its lanes.
- The all-day row is rendered, empty, when no event needs it, so the grid does
  not shift when one arrives.
- The all-day row carries the `CalendarLabels.AllDay` text as its label.

### The month view

- A timed event of a single day is drawn as one line in that day's cell, with
  its start time before its title.
- An all-day event, and any event that touches more than one day, is drawn as a
  bar spanning its days.
- A bar that crosses the end of a week row continues at the start of the next
  row.
- Within a cell, bars are placed above lines.
- Lines within a cell are ordered by `Start`.
- A cell shows as many events as fit its height.
- A cell with more events than fit renders an overflow control with the
  `CalendarLabels.More` text, naming how many are hidden.
- Activating the overflow control switches to the day view of that day.

### What an event shows

- An event renders its `Title`.
- A timed event in the day and week views renders its start and end time.
- Times are formatted with `Culture`.
- An event with people assigned renders them as avatars, in the order of
  `PersonIds`.
- An event with a non-null `SeriesId` renders a recurring-event mark.
- The recurring-event mark carries the `CalendarLabels.Recurring` text as its
  accessible name.
- Two events with the same `SeriesId` are rendered as two independent events;
  the calendar derives nothing from the series beyond the mark.
- `EventTemplate` supplied replaces the title, the times, the avatars and the
  mark with the template's output.
- `EventTemplate` receives the `CalendarEvent` as its context.
- The event's box, its position and its colour are the calendar's with or
  without `EventTemplate`.

### Colour

- An event whose `Color` is a slot other than `CalendarColor.Auto` uses that
  slot.
- An event whose `Color` is `CalendarColor.Auto` uses the colour of the first
  person in its `PersonIds` that is in `People`.
- An event with `CalendarColor.Auto` and no such person uses
  `CalendarColor.Slot1`.
- `CalendarColor.Slot1` to `CalendarColor.Slot6` draw from `--chart-1` to
  `--chart-6`.
- An event carries its colour as a tint and an edge indicator, never as a solid
  fill of its box (`DESIGN-08`).
- An event's text is set in `--fg`, not in the event's colour, so its contrast
  does not depend on the slot.

### Activating an event

- Every rendered event is a button.
- Activating an event by pointer fires the `OnEventClick` callback with that
  event.
- `Enter` and `Space` on a focused event fire the `OnEventClick` callback with
  that event.
- Every segment and every bar of one event fires `OnEventClick` with the same
  `CalendarEvent`.
- An event is focusable whether or not `OnEventClick` has a handler, so its
  content can be read by keyboard.

## Recorded gaps

- **A month cell's capacity is a fixed three rows, not a measurement.** "A cell
  shows as many events as fit its height" would need the rendered height, and
  all layout arithmetic is server-side. A day with more gives its third row to
  the overflow control; a bar hidden on one of its days is hidden, and counted,
  on all of them.
- **Avatars and the recurring mark are not shown in a box narrower than 120px**,
  and a single-line segment narrower than 150px drops its times, so the title
  stays readable. The event's accessible name states all of it at any width.
- **A timed event wholly outside `DayStart`–`DayEnd` on every day it touches**
  is drawn in no column, as "Placement" says — and is therefore not visible in
  the day and week views at all. The month view shows it.
- **Overlap is judged on the drawn box.** An event shorter than a slot is drawn
  one slot high, and that height is what is packed, so two five-minute events
  ten minutes apart sit side by side although their times do not overlap.
