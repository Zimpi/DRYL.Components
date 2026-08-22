# Sorting

## Meta
- **State:** Implemented

## Acceptance Criteria

### Making a column sortable

- A column marked `Sortable` renders a clickable, focusable header.
- A column not marked `Sortable` renders a header that is neither clickable nor
  a tab stop, unless it is reorderable.
- A sortable header renders a direction indicator.
- The indicator reports whether that column is currently part of the sort.

### The cycle

- Clicking a sortable header with no sort on it sorts it ascending.
- Clicking it again sorts it descending.
- Clicking it a third time removes the sort entirely, so a user can get back to
  the source order.
- `Enter` and `Space` on a focused sortable header do the same as a click.

### Single and multi-column sort

- A plain click replaces the whole sort with the clicked column.
- A click with `Shift` adds the column to the existing sort instead of replacing
  it.
- A `Shift` click on a column already in the sort moves it to the end of the
  sort order at its next direction.
- The sort is applied in the order the columns were added, so the first is
  primary.
- A sort descriptor naming a column that no longer exists is skipped rather than
  breaking the sort.
- A column with no `Field` cannot contribute a sort key and is skipped.

### Comparing values

- Two nulls compare equal.
- A null sorts before any non-null value.
- Two values of the same comparable type are compared by that type, so numbers
  and dates sort numerically and chronologically rather than as text.
- Values of differing types fall back to an ordinal text comparison rather than
  throwing.

### Reach into other features

- Changing the sort marks the persisted state dirty.
- Changing the sort rebuilds the view through the same morph path as a row move,
  so a re-sort glides when the morph is active.
- In server mode the sort descriptors are passed through in the `DataRequest`
  and the table sorts nothing itself.
- Row reordering is disabled while any sort is applied, because a manual order
  over a sorted view is meaningless — the grip stays visible and becomes
  inoperable rather than disappearing.

### Accessibility

- A sortable header reports its sort state to assistive technology, as
  ascending, descending or none.
- A header that is not sortable reports no sort state at all, rather than
  reporting that it is unsorted.

## Recorded gaps

- **The multi-sort gesture is undiscoverable.** `Shift`-click is the only way to
  build a multi-column sort, and nothing in the header, the tooltip or the
  accessible name mentions it. There is no visible sort order — a header that is
  second in the sort looks exactly like one that is first.
- **The keyboard cannot build a multi-column sort reliably.** `Shift`+`Enter` is
  handled, but a screen-reader user has no way to learn that it exists and no
  feedback about the resulting order.
- **The fallback comparison is ordinal text.** Values of different types, and
  values of the same type that are not `IComparable`, are compared by
  `ToString`. That is a defensible fallback, but it is culture-independent by
  construction, so a column of localised strings sorts by code point rather than
  by the user's collation.
- **The sort's direction indicator is always drawn.** A sortable column that is
  not part of the sort still renders an ascending arrow, marked inactive by a
  class. Whether that reads as an affordance or as a wrong state depends
  entirely on the inactive styling.
