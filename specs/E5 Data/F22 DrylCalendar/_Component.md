# DrylCalendar

## Meta
- **State:** Modified
- **Source:** code/DRYL.Components/Components/Data/DrylCalendar.razor
              code/DRYL.Components/Components/Data/DrylCalendar.razor.css
              code/DRYL.Components/Components/Data/CalendarView.cs
              code/DRYL.Components/Components/Data/CalendarPersonLayout.cs
              code/DRYL.Components/Components/Data/CalendarColor.cs
              code/DRYL.Components/Components/Data/CalendarAvailabilityKind.cs
              code/DRYL.Components/Components/Data/Models/CalendarEvent.cs
              code/DRYL.Components/Components/Data/Models/CalendarPerson.cs
              code/DRYL.Components/Components/Data/Models/CalendarAvailability.cs
              code/DRYL.Components/Components/Data/Models/CalendarRange.cs
              code/DRYL.Components/Components/Data/Models/CalendarLabels.cs
              code/DRYL.Components/Components/Data/Internal/CalendarLayout.cs

This spec is split (`SPEC-02`). The acceptance criteria live in the `S{n}` files
beside this one; this file carries the `Meta` block, the description and the
public API. The state above is the rolled-up state: it reads `Implemented` only
while every `S{n}` beside it does.

The component is split because it ships in three stages, each its own release
(`ideas/I19 A scheduling calendar.md`), and a per-story state is what keeps
"which stage is in the code" readable. `S1` to `S4` are **stage one — show**,
shipped in 3.5.0. `S5` to `S8` are **stage two — edit**, specified and not yet
implemented; until they are, the state above reads `Modified`. Stage three (the
event editor and AI) appends its stories and its API when it is specified.
Nothing in an earlier stage is renamed by a later one.

The types stage two adds — `CalendarChangeKind`, `CalendarChangeRequest`,
`CalendarConflict`, `CalendarConflictKind` — join `Source` when their files
exist; `SPEC-03` lets `Source` name existing files only.

| File | Subject |
|---|---|
| `S1 Views and navigation.md` | The three views, the toolbar, the bound view and date, the visible range, the time grid, the current time |
| `S2 Events.md` | What an event's times mean, placement, overlaps, all-day and multi-day events, series, colour, the event template, the click |
| `S3 People and availability.md` | Overlaid people and person columns, the legend and hiding, working hours and absences |
| `S4 Presentation, narrow layout and accessibility.md` | The surface, tokens, motion, the narrow layouts, labels, keyboard, announcements, cleanup |
| `S5 Change requests.md` | Switching editing on, the request a change raises, what is drawn while the host decides, series |
| `S6 Pointer and touch editing.md` | Moving, resizing, reassigning and creating by pointer and touch, snapping, handles, motion while editing |
| `S7 Keyboard editing and announcements.md` | The slot grid, creating, moving and resizing by keyboard, hints, announcements, focus after a change |
| `S8 Conflicts.md` | The three kinds of conflict, how they are detected, how they are shown on events and while editing |

## User Story

As a Blazor developer, I want to hand a calendar my events, my people and their
availability and get a day, week and month view of them, so that an application
that plans several people's time shows that plan in the library's own language
instead of in a third-party scheduler that knows none of its tokens.

## Description

`DrylCalendar` shows events on a time axis, for several people at once. It has
three views. **Day** and **week** are time grids: a column per day, the hours
running down it, each event placed by its start and sized by its duration, with
a row above the grid for events that take whole days. **Month** is a grid of
days in which an event is a line or, when it spans days, a bar.

**The host owns the data.** `Events`, `People` and `Availability` are
parameters; the calendar stores none of them and changes none of them. It tells
the host which span of time it is showing through `OnRangeChanged`, and the host
supplies what belongs in that span — the same contract for events, for the
occurrences of a recurring event, and for working hours. The calendar renders
what it is given and does no expansion of its own.

**Times are wall-clock times in one zone.** The calendar reads the date and time
of every `DateTime` it is handed and ignores its `Kind`; converting between
zones is the host's work. An `End` is exclusive everywhere, so an event that
ends at ten and one that starts at ten do not overlap, and an all-day event for
a single day ends on the following day.

**Several people are shown in one of two ways.** Overlaid, every person's events
share one grid and are told apart by colour; a legend lets the user hide people.
As columns — in the day view — each person gets a column of their own beside a
shared time axis, so who is free when can be read across. Colours come from the
six categorical slots the charts already use, never from a free colour value.

**Availability is drawn, not enforced.** Working hours and absences appear as
muted areas behind the events. In stage one they are information only.

In stage one the calendar is a display with one interaction: an event can be
activated, which raises `OnEventClick`. Everything a user reaches by pointer
they also reach by keyboard.

**Editing is opt-in and stays the host's decision.** With `Editable` set — stage
two — the user creates, moves and resizes events on the grid, and moves them
across person columns to reassign them, by pointer, touch or keyboard. The
calendar still changes no data: a finished change raises
`OnEventChangeRequested` once, with the event as it was and as it would be.
While the host's handler runs, the event stays where it was dropped; afterwards
`Events` alone decides, so a change the host applied stays and one it did not
glides back. The calendar detects **conflicts** itself — two events of one
person overlapping, an event in an absence, an event outside working hours —
and marks them, already while dragging. A conflict warns and never blocks.

## Public API

### Data

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Events` | `IEnumerable<CalendarEvent>` | `[]` | The events to show. The host supplies those of the visible range. |
| `People` | `IEnumerable<CalendarPerson>` | `[]` | The people events are assigned to, in display order. |
| `Availability` | `IEnumerable<CalendarAvailability>` | `[]` | Working hours and absences per person. |

### View and date

| Member | Type | Default | Purpose |
|---|---|---|---|
| `View` | `CalendarView` | `CalendarView.Week` | The view shown, for two-way binding. |
| `ViewChanged` | `EventCallback<CalendarView>` | — | Raised when the user switches the view. |
| `Date` | `DateOnly` | today | The date the visible range is built around, for two-way binding. Left unset — or bound to a field that still holds the default date — the calendar starts on today; the property itself then reads `default`. |
| `DateChanged` | `EventCallback<DateOnly>` | — | Raised when the user navigates. |
| `OnRangeChanged` | `EventCallback<CalendarRange>` | — | Raised with the visible range, first and on every change of it. |
| `Now` | `DateTime?` | `null` | The host's current time, for "today" and the current-time line. `null` uses the server's clock. |
| `FirstDayOfWeek` | `DayOfWeek?` | `null` | The first column of a week. `null` follows `Culture`. |
| `Culture` | `CultureInfo?` | `null` | Formats dates, day names and times. `null` uses the current culture. |

### The time grid

| Member | Type | Default | Purpose |
|---|---|---|---|
| `DayStart` | `TimeSpan` | `00:00` | First time of day the grid shows. |
| `DayEnd` | `TimeSpan` | `24:00` | Time of day the grid ends at. |
| `SlotMinutes` | `int` | `30` | Length of one grid slot, in minutes. |
| `InitialScrollTime` | `TimeSpan` | `07:00` | Time of day scrolled into view when the time grid is first shown. |
| `Height` | `string?` | `null` | Height of the calendar; the grid scrolls inside it. |

### People

| Member | Type | Default | Purpose |
|---|---|---|---|
| `PersonLayout` | `CalendarPersonLayout` | `CalendarPersonLayout.Overlay` | Overlaid in one grid, or a column per person in the day view. |
| `HiddenPersonIds` | `IReadOnlySet<string>?` | `null` | The ids of hidden people, for two-way binding. |
| `HiddenPersonIdsChanged` | `EventCallback<IReadOnlySet<string>>` | — | Raised when the user hides or shows a person. |
| `ShowLegend` | `bool` | `true` | Renders the legend of people. |

### Slots

| Member | Type | Default | Purpose |
|---|---|---|---|
| `EventTemplate` | `RenderFragment<CalendarEvent>?` | `null` | Replaces the content of every event. |
| `PersonTemplate` | `RenderFragment<CalendarPerson>?` | `null` | Replaces the content of a person's column header and legend entry. |
| `ToolbarContent` | `RenderFragment?` | `null` | Extra content at the end of the toolbar. |

### Interaction

| Member | Type | Default | Purpose |
|---|---|---|---|
| `OnEventClick` | `EventCallback<CalendarEvent>` | — | Raised when an event is activated by pointer or keyboard. |
| `Editable` | `bool` | `false` | Lets the user create, move and resize events and shows conflicts. Stage two. |
| `OnEventChangeRequested` | `EventCallback<CalendarChangeRequest>` | — | Raised once per completed create, move or resize; the host applies it to `Events` or not. Stage two. |

### Presentation

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ShowToolbar` | `bool` | `true` | Renders the toolbar. |
| `Loading` | `bool` | `false` | Marks the calendar as waiting for the host's data. |
| `CollapseBelow` | `Breakpoint?` | `Breakpoint.Md` | The calendar's own width below which the narrow layouts apply. `null` never collapses. |
| `Labels` | `CalendarLabels` | `new()` | Every text the calendar renders that is not a formatted date. |
| `AriaLabel` | `string?` | `null` | Accessible label of the calendar. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the calendar's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`CalendarEvent`, `CalendarPerson`, `CalendarAvailability`, `CalendarRange`,
`CalendarLabels`, `CalendarChangeRequest`, `CalendarConflict` and the six enums
are set out in [`../_Api.md`](../_Api.md).

The component takes no `Ai` and no `Aura` parameter in stages one and two; the
decision and its reason are recorded in `S4` (`AI-05`).
