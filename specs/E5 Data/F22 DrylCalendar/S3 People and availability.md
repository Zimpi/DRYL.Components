# People and availability

## Meta
- **State:** Modified

## Acceptance Criteria

### People

- `People` left empty renders a calendar without legend, person columns or
  avatars.
- A person's display order is their order in `People`.
- A `CalendarPerson` whose `Color` is a slot other than `CalendarColor.Auto`
  uses that slot.
- A `CalendarPerson` whose `Color` is `CalendarColor.Auto` is given a slot by
  their position in `People`, cycling through the six slots.
- A person keeps the slot of their position while they are hidden, so hiding one
  person does not recolour the others.
- An id in an event's `PersonIds` that matches no `CalendarPerson` is ignored.
- An event none of whose `PersonIds` matches a `CalendarPerson` is unassigned.
- Two people with the same `Id` are reported once on the console, and the later
  one is not rendered.

### The legend

- `ShowLegend` left `true` renders a legend entry for every person.
- `ShowLegend` set to `false` renders no legend element at all.
- No legend is rendered when `People` is empty.
- A legend entry renders the person's avatar, their `Name` and their colour.
- `PersonTemplate` supplied replaces the avatar and the name of a legend entry
  with the template's output.
- A legend entry is a toggle that reports whether its person is shown.
- Activating a legend entry hides a shown person and shows a hidden one.
- The `HiddenPersonIdsChanged` callback fires with the complete new set on every
  toggle.
- The calendar applies a toggle whether or not `HiddenPersonIds` is bound.
- `HiddenPersonIds` left `null` shows every person.
- An id in `HiddenPersonIds` that matches no `CalendarPerson` has no effect.

### Hidden people

- An event all of whose assigned people are hidden is not rendered.
- An event with at least one shown person is rendered.
- An unassigned event is rendered regardless of `HiddenPersonIds`.
- An event's avatars include its hidden people, so who attends does not depend
  on who is shown.

### The overlaid layout

- `PersonLayout` defaults to `CalendarPersonLayout.Overlay`.
- `PersonLayout` accepts exactly the two values of `CalendarPersonLayout`.
- In the overlaid layout, every shown person's events share the day's one
  column.
- The week view and the month view are always overlaid, whatever `PersonLayout`
  says.

### Person columns

- `CalendarPersonLayout.Columns` in the day view renders one column for every
  shown person, in the order of `People`.
- The columns share one time axis.
- Every column has a header naming its person.
- A column header renders the person's avatar and `Name`.
- `PersonTemplate` supplied replaces the avatar and the name of a column header
  with the template's output.
- A timed event is drawn in the column of every shown person assigned to it.
- An all-day event is drawn in the all-day row of every shown person assigned to
  it.
- Overlaps are resolved per column: two events overlap only when they are drawn
  in the same person's column.
- A leading column for unassigned events is rendered when the visible day has at
  least one.
- That column's header carries the `CalendarLabels.Unassigned` text.
- No unassigned column is rendered when the visible day has no unassigned event.
- With `People` empty, `CalendarPersonLayout.Columns` renders the day view as
  the overlaid layout does.
- With every person hidden, the day view renders its time axis and the
  unassigned column only.

### Availability

- A `CalendarAvailability` whose `PersonId` matches no `CalendarPerson` is
  ignored.
- `CalendarAvailability.End` is exclusive, as an event's is.
- A `CalendarAvailability` of kind `CalendarAvailabilityKind.Working` states a
  span in which the person works.
- A `CalendarAvailability` of kind `CalendarAvailabilityKind.Absent` states a
  span in which the person is away.
- On a day for which a person has at least one working span, their time outside
  those spans is unavailable.
- On a day for which a person has no working span, none of their time is
  unavailable for lack of one.
- A person's time inside an absence is unavailable, whether or not it lies in a
  working span.
- The calendar derives nothing beyond the spans it is given: it repeats no
  working span on other days.

### Drawing availability

- In a person's column, that person's unavailable time is drawn as a muted area
  behind the events.
- In the overlaid day and week views, a time is drawn as a muted area when every
  shown person is unavailable at it.
- In the overlaid day and week views, no muted area is drawn when no person is
  shown.
- An absence in a person's column renders its `Label` when it has one.
- An absence that covers a person's whole visible day in their column is also
  named in that column's all-day row.
- The month view draws no availability.
- A muted area is not interactive and is not a tab stop.
- A muted area is filled with `--line-soft`, which darkens a light ground and
  lightens a dark one, so one rule serves both colour modes (`DESIGN-02`).
- Events drawn over a muted area keep their own tint and contrast.
