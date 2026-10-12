# Views and navigation

## Meta
- **State:** Modified

## Acceptance Criteria

### The view

- `View` defaults to `CalendarView.Week`.
- `View` accepts exactly the three values of `CalendarView`.
- `CalendarView.Day` renders one day as a time grid.
- `CalendarView.Week` renders seven consecutive days as a time grid.
- `CalendarView.Month` renders a grid of days, one row per week.
- Exactly one view is rendered at a time.

### The visible range

- The day view's range is the day of `Date`.
- The week view's range is the seven days of the week that contains `Date`.
- The week starts on `FirstDayOfWeek` when it is set.
- The week starts on the first day of the week of `Culture` when
  `FirstDayOfWeek` is `null`.
- The month view's range is every whole week that contains a day of the month of
  `Date`.
- A day of the month view that belongs to a neighbouring month is rendered and
  marked as outside the month.
- `Date` defaults to the date of `Now`, or of the server's clock when `Now` is
  `null`.
- `OnRangeChanged` fires once after the first render with the visible range.
- `OnRangeChanged` fires whenever a change of `View`, `Date` or `FirstDayOfWeek`
  changes the visible range.
- `OnRangeChanged` does not fire when a parameter changes and the visible range
  stays the same.
- The `CalendarRange` raised starts at midnight of the first visible day.
- The `CalendarRange` raised ends at midnight after the last visible day, as an
  exclusive end.

### The toolbar

- `ShowToolbar` left `true` renders the toolbar above the grid.
- `ShowToolbar` set to `false` renders no toolbar element at all.
- The toolbar renders a control that moves to the previous range.
- The toolbar renders a control that moves to the next range.
- The toolbar renders a control that moves to today.
- The toolbar renders a title naming the visible range.
- The toolbar renders a view switch offering the three views.
- `ToolbarContent` is rendered at the end of the toolbar when it is supplied.
- The title of the day view names the weekday and the full date.
- The title of the week view names the first and the last day of the week.
- The title of the month view names the month and the year.
- The title is formatted with `Culture`.

### Navigating

- The previous and next controls move `Date` by one day in the day view.
- The previous and next controls move `Date` by seven days in the week view.
- The previous and next controls move `Date` by one month in the month view.
- A move by one month from a day the target month does not have lands on the
  target month's last day.
- The today control sets `Date` to the date of `Now`, or of the server's clock
  when `Now` is `null`.
- The `DateChanged` callback fires with the new date on every navigation.
- The calendar shows the new range after a navigation whether or not `Date` is
  bound.
- Selecting a view in the view switch fires the `ViewChanged` callback with that
  view.
- The calendar shows the selected view whether or not `View` is bound.
- Activating a day's header in the week view switches to the day view of that
  day.
- Activating a day's number in the month view switches to the day view of that
  day.
- Switching to a day this way fires both `DateChanged` and `ViewChanged`.

### The time grid

- The day and week views render a time axis beside the day columns.
- The grid starts at `DayStart` and ends at `DayEnd`.
- `DayStart` defaults to midnight and `DayEnd` to the following midnight, so the
  whole day is shown.
- A `DayEnd` that is not later than `DayStart` is replaced by the following
  midnight.
- The grid is divided into slots of `SlotMinutes` minutes.
- `SlotMinutes` defaults to `30`.
- A `SlotMinutes` that does not divide sixty evenly is replaced by `30`.
- A replaced `DayEnd` or `SlotMinutes` is reported once on the console.
- The time axis labels every full hour.
- Hour labels are formatted with `Culture`, so a culture with a twelve-hour clock
  shows one.
- `Height` set gives the calendar that height and makes the time grid scroll
  inside it.
- The day headers and the all-day row stay visible while the time grid scrolls.
- The time grid is scrolled so that `InitialScrollTime` is in view on the first
  render.
- A later render does not move the scroll position on its own.

### Today and the current time

- The day that holds the date of `Now` is marked as today in every view.
- That day carries `aria-current="date"`.
- The day and week views draw a current-time line across today's column at the
  time of `Now`.
- No current-time line is drawn when today is outside the visible range.
- No current-time line is drawn when the time of `Now` is outside `DayStart` to
  `DayEnd`.
- With `Now` left `null`, the current-time line moves on its own as the server's
  clock advances.
- With `Now` set, the current-time line follows the parameter and nothing moves
  on its own.

### Loading

- `Loading` set marks the calendar as busy for assistive technology.
- `Loading` set renders a progress indication in the toolbar.
- `Loading` set keeps the events already rendered in place rather than emptying
  the grid.
