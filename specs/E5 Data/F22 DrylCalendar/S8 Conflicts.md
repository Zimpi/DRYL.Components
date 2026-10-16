# Conflicts

## Meta
- **State:** Implemented

Stage two — edit. The calendar detects conflicts itself, from `Events` and
`Availability`, and shows them. **A conflict warns; it never blocks**
(`ideas/I19 A scheduling calendar.md`): every change that can be made can be
dropped, and refusing it is the host's business logic.

## Acceptance Criteria

### When conflicts are detected

- Conflicts are detected and drawn only while `Editable` is set.
- With `Editable` left `false`, no conflict mark is rendered and no accessible
  name mentions a conflict.
- Conflicts are detected for timed events only; an all-day event has none and
  causes none.
- An unassigned event has no conflict and causes none.
- Conflicts are detected per person: an event with several people can conflict
  for one of them and not for another.
- A hidden person's conflicts are detected as a shown person's are, so hiding a
  person does not change any event's conflicts.
- Conflicts are detected among the events and the availability the host
  supplied; the calendar does not look outside them.

### The three kinds

- `CalendarConflictKind` has exactly three members: `Overlap`, `Absence` and
  `OutsideWorkingHours`.
- An event has an `Overlap` conflict for a person when another timed event of
  that person overlaps it in time.
- Two events that only touch — one ending when the other starts — do not
  overlap, as `End` is exclusive.
- An `Overlap` conflict is detected on both events concerned.
- An event has an `Absence` conflict for a person when it overlaps an absence of
  that person.
- An event has an `OutsideWorkingHours` conflict for a person when part of it
  lies outside that person's working spans, on a day for which the person has
  at least one working span.
- On a day for which a person has no working span, no event has an
  `OutsideWorkingHours` conflict for them, as `S3` "Availability" makes none of
  their time unavailable for lack of one.
- An event that lies in an absence outside working hours has both an `Absence`
  and an `OutsideWorkingHours` conflict.

### `CalendarConflict`

- `CalendarConflict.Kind` names the kind.
- `CalendarConflict.PersonId` names the person the conflict is for.
- `CalendarConflict.EventId` names the other event of an `Overlap` conflict.
- `CalendarConflict.EventId` is `null` for an `Absence` and an
  `OutsideWorkingHours` conflict.
- An event that overlaps two events of one person has two `Overlap` conflicts.
- The conflicts of `CalendarChangeRequest.Conflicts` are those of `Proposed`
  against every other event in `Events` and against `Availability`; `Original`
  takes no part, so a moved event never conflicts with its own old place.

### Showing a conflict on an event

- An event with at least one conflict renders a conflict mark.
- The conflict mark is drawn in `--warning`, as a small indicator, never as the
  fill of the event's box (`DESIGN-08`).
- The conflict mark is drawn with or without `EventTemplate`.
- The conflict mark is rendered in every segment and every bar of the event.
- An event without conflicts renders no conflict mark.
- An event's accessible name ends with its conflicts, one phrase per conflict,
  from the `CalendarLabels.ConflictOverlap`, `CalendarLabels.ConflictAbsence`
  and `CalendarLabels.ConflictOutsideWorkingHours` texts, each naming the
  person.
- The conflict mark itself is hidden from assistive technology, as the
  accessible name already states the conflicts.
- The conflict mark carries a tooltip listing the same phrases.
- A conflict is never shown by colour alone: the mark is an icon, and the
  conflicts are stated in words (`UX-03`).

### Showing a conflict while editing

- During a move, a resize or a create, the preview shows the conflict mark
  whenever the place it would land at has a conflict.
- The preview's conflicts are detected as the pointer moves, without a round
  trip to the server.
- The preview's conflict mark appears and disappears with a transition at
  `--dur-fast`, and at once under `prefers-reduced-motion: reduce`.
- Every other event that the preview would put into conflict shows the conflict
  mark while the preview is there.
- An event whose only conflict is with the changed event loses its conflict mark
  while the preview is at a place that no longer conflicts with it.
- A conflict never prevents a drop: a change with conflicts is completed and
  requested as one without.

### The month view

- In the month view, a single-line event with a conflict renders the conflict
  mark before its title.
- In the narrow month view, the list below the grid renders the conflict mark on
  every event with a conflict.

## Recorded gaps

- **The preview of a month-view move shows no conflict mark.** In the month
  view a change is previewed on the day cells, which carry no mark; the request
  and the event after the drop carry the conflicts as everywhere else.
- **The mark's tooltip is reached by pointer only.** The mark is hidden from
  assistive technology; a keyboard or screen-reader user gets the same phrases
  in the event's accessible name.
- **Phrases are not repeated**: two `Overlap` conflicts for one person read as
  one phrase in the accessible name and the tooltip, while the request carries
  both.
