# Presentation, narrow layout and accessibility

## Meta
- **State:** Modified

## Acceptance Criteria

### The surface

- The calendar renders on the library's card surface, and its fill, border and
  frost are that shared treatment rather than the component's own (`DESIGN-06`).
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- The toolbar and the legend are absent from the markup entirely when they are
  switched off, rather than rendered empty.

### Tokens

- Every colour the component renders comes from a token; the component names no
  literal colour (`DESIGN-01`).
- Every size, radius and spacing the component's own stylesheet sets comes from
  a token (`DESIGN-01`).
- Every grid line is drawn in `--line`.
- The line of a full hour is drawn in `--line`, and the lines of the slots
  between hours in `--line-soft`, so the hours read first.
- The time axis and the day headers are set in `--fg-dim`.
- Today's header is set in `--fg` and carries an accent indicator.
- The current-time line is drawn in `--accent-line`.
- The accent appears as today's indicator, the current-time line and the focus
  ring — never as the fill of a day, a column or the grid (`DESIGN-08`).
- A day of the month view that lies outside the month is set in `--fg-faint`.
- The component branches on no colour mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- Switching the view fades the outgoing grid out and the incoming grid in, at
  `--dur-med` with `--ease-out`.
- Navigating to another range moves the incoming grid in from the side the user
  moved towards, at `--dur-med` with `--ease-out`.
- The view switch's active marker glides between the views rather than jumping.
- An event that joins `Events` while its range is shown enters with a
  transition, at `--dur-med` with `--ease-out`.
- An event that leaves `Events` while its range is shown exits with a transition
  before it is removed (`DESIGN-12`).
- Hiding a person makes their events exit, and showing them makes the events
  enter, with those same transitions.
- The events of a range that is navigated to arrive with their grid, not one by
  one.
- An event's hover and focus states change at `--dur-fast` rather than stepping.
- The current-time line changes position without an animation.
- Under `prefers-reduced-motion: reduce` none of these transitions runs, and
  every change is applied at once (`UX-06`).
- No transition changes the tab order or removes a control from it while it runs
  (`UX-07`).

### The narrow layouts

- `CollapseBelow` defaults to `Breakpoint.Md`.
- The narrow layouts apply when the calendar's own width is below
  `CollapseBelow`, whatever the width of the viewport.
- `CollapseBelow` set to `null` never applies the narrow layouts.
- Narrow, the week view shows the column of `Date` alone.
- Narrow, the week view's day headers become a strip of the week's seven days.
- Activating a day in the strip sets `Date` to that day and stays in the week
  view.
- The day of `Date` is marked as selected in the strip.
- Narrow, a day in the strip with at least one event carries a mark saying so.
- Narrow, the month view replaces the lines and bars in its cells with one mark
  per day that has events.
- Narrow, activating a day in the month view sets `Date` to that day and stays
  in the month view.
- Narrow, the month view lists the events of `Date` below the grid, all-day
  events first and then by `Start`.
- Narrow, that list renders the `CalendarLabels.NoEvents` text when `Date` has
  no event.
- Narrow, person columns keep a minimum width and scroll horizontally.
- Narrow, the time axis stays in place while person columns scroll horizontally.
- Narrow, the toolbar wraps onto further lines rather than overflowing.
- The calendar never causes horizontal scrolling of the page, at any width.

### Labels

- Every text the calendar renders that is not a formatted date or time comes
  from `Labels`.
- `Labels` left at its default renders English texts.
- A member of `CalendarLabels` that is overridden replaces that text everywhere
  it is used — visible text, tooltip and accessible name alike.
- Day names, month names, dates and times follow `Culture`, not `Labels`.

### Keyboard and accessibility

- The calendar carries `AriaLabel` as its accessible label.
- Every control of the toolbar, every legend entry, every day header that
  switches the view, every overflow control and every event is a tab stop.
- The tab order runs toolbar, legend, then the grid.
- Within the grid, events are reached in order of day, then all-day events
  before timed ones, then `Start`.
- Every tab stop shows the accent focus ring when focused by keyboard
  (`UX-02`).
- The previous and next controls are icon-only, each wrapped in a tooltip whose
  text equals its accessible name (`UX-05`).
- The view switch reports which view is selected.
- An event's accessible name states its title, its day, its start and end time
  or that it lasts all day, the names of its people, and that it recurs when it
  does.
- An event's accessible name is the same with and without `EventTemplate`.
- An event segment that continues from or into another day says so in its
  accessible name.
- The grid lines, the muted areas, the current-time line and the colour
  indicators are hidden from assistive technology.
- A person's colour is never the only thing that identifies them: their name is
  in the legend, in the column header and in every event's accessible name.
- After a navigation or a view change, the new range's title is announced
  through a polite live region.
- The loading state is announced as a status.

### AI mode

- The component takes no `Ai` and no `Aura` parameter in stage one.

### Cleanup

- The timer that advances the current-time line is disposed with the component.
- No timer runs while `Now` is set.
- The scroll to `InitialScrollTime` is skipped during prerender and survives a
  disconnected circuit without throwing.
- Every interop call the calendar makes is best-effort: a missing element or a
  closed circuit is swallowed rather than surfaced.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colours, to be verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` and by eye in both modes. The event
  tints over `--chart-1` to `--chart-6`, the muted areas over `--line-soft` and
  the text over a tinted event are the mode-dependent parts; the component
  defines no mode-specific rule and adds no token.
- **Enter/exit animation** — the view change, the navigation, an event entering
  and leaving, a person being hidden and shown; the criteria under "Motion"
  above.
- **Keyboard and a11y** — the criteria under "Keyboard and accessibility" above.
  The substantive decision is that every event is a button and a tab stop of its
  own. The grid is deliberately **not** given the grid role in stage one: that
  role promises arrow-key navigation between cells, which arrives with the
  editable slots of stage two.
- **AI mode** — no, in stage one, and the "no" is a deferral rather than a
  principle. The calendar's AI story is per event — an event the AI proposes,
  accepts or is still working on — and it is specified with stage three, which
  depends on the `Proposed` state of `ideas/I20 An AiState for work awaiting a
  human.md`. A surface-level aura shipped before that would light the calendar
  up without being able to say which event the AI is concerned with (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoCalendar.razor`, with the
  examples under `DRYL.Website/Components/Examples/Calendar/`, written with the
  implementation.
- **`ComponentCatalog`** — to be registered as `"Calendar"` / `calendar` in
  `DRYL.Website/Components/ComponentCatalog.cs`, in the `Data` section, not
  flagged AI-capable until stage three.
- **Tests** — `tests/DRYL.Components.Tests/DrylCalendarTests.cs`, written with
  the implementation.
