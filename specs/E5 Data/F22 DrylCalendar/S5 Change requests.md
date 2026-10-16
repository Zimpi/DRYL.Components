# Change requests

## Meta
- **State:** Modified

Stage two — edit. This story is the contract between the calendar and its host
for every change made on the grid; how a change is made is `S6` (pointer and
touch) and `S7` (keyboard), what makes it conflict is `S8`.

## Acceptance Criteria

### Switching editing on

- `Editable` defaults to `false`.
- With `Editable` left `false`, the calendar renders and behaves exactly as in
  stage one: no event can be created, moved or resized, no resize handle is
  rendered and no conflict mark is drawn.
- With `Editable` set, every event whose `ReadOnly` is `false` can be moved and
  resized.
- With `Editable` set, a new event can be created on an empty part of the grid.
- `CalendarEvent.ReadOnly` defaults to `false`.
- An event whose `ReadOnly` is set cannot be moved or resized, with `Editable`
  set or not.
- An event whose `ReadOnly` is set still fires `OnEventClick` when activated.
- An event whose `ReadOnly` is set still counts when the conflicts of other
  events are detected (`S8`).

### The request

- Every completed create, move or resize fires the `OnEventChangeRequested`
  callback exactly once.
- No callback fires while a change is still being made — not per pointer move,
  not per slot, not per key press.
- A cancelled change fires no callback.
- A change that ends where it started fires no callback.
- `CalendarChangeRequest.Kind` is `CalendarChangeKind.Create` for a new event.
- `CalendarChangeRequest.Kind` is `CalendarChangeKind.Move` for a change of an
  event's start that keeps its duration.
- A move that also changes the event's people is a `CalendarChangeKind.Move`.
- `CalendarChangeRequest.Kind` is `CalendarChangeKind.Resize` for a change of
  an event's start or end that changes its duration.
- `CalendarChangeRequest.Original` is the `CalendarEvent` from `Events` that the
  change was made to, the same instance the host supplied.
- `CalendarChangeRequest.Original` is `null` for a create.
- `CalendarChangeRequest.Proposed` is a `CalendarEvent` carrying the event as it
  would be after the change.
- For a move or a resize, `Proposed` equals `Original` in every member except
  `Start`, `End` and `PersonIds`.
- For a move or a resize, `Proposed.Id` equals `Original.Id`, so the host can
  find the event it replaces.
- `CalendarChangeRequest.Conflicts` lists the conflicts `Proposed` would have,
  as `S8` defines them.
- `CalendarChangeRequest.Conflicts` is empty when `Proposed` would have none.

### A new event

- A created event's `Proposed.Id` is a new, non-empty identifier that no event
  in `Events` carries.
- A created event's `Proposed.Title` is empty.
- A created event's `Proposed.Color` is `CalendarColor.Auto`.
- A created event's `Proposed.SeriesId` and `Proposed.Data` are `null`.
- An event created in a person's column carries that person as its only entry
  in `Proposed.PersonIds`.
- An event created in the unassigned column, or in the overlaid layout, has an
  empty `Proposed.PersonIds`.
- An event created in the time grid is a timed event whose `Start` and `End`
  lie on slot boundaries.
- An event created in the all-day row or in the month view has `AllDay` set and
  covers whole days.

### While the host decides

- While the task returned by `OnEventChangeRequested` runs, the changed event is
  drawn where the change put it.
- While that task runs, a created event is drawn at the place it was created,
  titled with the `CalendarLabels.NewEvent` text.
- While that task runs, the changed event is marked as busy for assistive
  technology.
- While that task runs, the changed event cannot be changed again.
- While that task runs, every other editable event can still be changed.
- Once the task has completed, the event is drawn from `Events` alone.
- A host that applied `Proposed` to `Events` sees the event stay where it was
  dropped.
- A host that left `Events` unchanged sees a moved or resized event return to
  its place in `Events` with a transition (`S6` "Motion while editing").
- A host that left `Events` unchanged sees a created event exit with a
  transition.
- A change the host did not apply is announced with the
  `CalendarLabels.NotApplied` text through the calendar's polite live region.
- A task that faults is treated as a change the host did not apply, and the
  exception is not swallowed.
- The calendar never adds, changes or removes an entry of `Events` on its own.

### Series

- A change to an event with a `SeriesId` is requested for that one event; the
  calendar derives nothing for the other events of the series.
- `Proposed.SeriesId` of a moved or resized occurrence equals its
  `Original.SeriesId`; whether the change breaks it out of the series is the
  host's decision.

## Cross-cutting evidence for stage two (`SPEC-05`)

Stage two adds behaviour to a component whose evidence is recorded in `S4`;
this section records only what stage two adds, and is completed when the stage
is implemented.

- **Both color modes** — the drag preview, the resize handles, the conflict mark
  and the busy state are drawn from existing tokens (`--accent-line`,
  `--warning`, `--glass-*`, `--shadow-*`); no token is added and no mode is
  branched on. To be looked at in both modes when implemented.
- **Enter/exit animation** — `S6` "Motion while editing": the lift, the settle,
  the return of a change the host did not apply, a created event entering and
  exiting.
- **Keyboard and a11y** — `S7` in full; with `Editable` set the time grid and
  the month grid take the grid role that `S4` deferred to this stage.
- **AI mode** — still no `Ai` parameter in stage two; the decision in `S4`
  stands and is taken up by stage three.
- **Demo page** — an editing example on `DemoCalendar.razor`, the host applying
  and refusing changes, and showing conflicts.
- **`ComponentCatalog`** — the existing `calendar` entry; its description gains
  editing.
