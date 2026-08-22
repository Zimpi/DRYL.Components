# Search and filtering

## Meta
- **State:** Implemented

## Acceptance Criteria

### The global search

- `Searchable` set renders a search input in the toolbar.
- The input is of type `search`, so a browser offers its own clear affordance.
- The input carries `SearchPlaceholder` as both its placeholder and its
  accessible label, so the control is named without a visible label.
- Typing does not apply immediately: the search is applied after
  `SearchDebounceMs` of quiet.
- A keystroke during the debounce supersedes the pending application, so one
  burst of typing applies once.
- Applying a search returns to the first page.
- A search of only whitespace is treated as no search.
- Clearing the search restores the unfiltered view.

### What the search matches

- With no `SearchPredicate`, a row matches when any `Searchable` column's value
  contains the search text.
- The comparison is case-insensitive.
- A column that is not `Searchable` is never matched against.
- A column with no `Field` is never matched against, because there is no value
  to compare.
- `SearchPredicate` set replaces the per-column search entirely, and the
  `Searchable` flags stop having any effect.
- In server mode the search text is passed through in the `DataRequest` and the
  table matches nothing itself.

### Per-column filters

- A column marked `Filterable` renders a filter control in its header.
- The control reports whether a filter is currently applied to its column.
- Pressing the control opens that column's filter surface; pressing it again
  closes it.
- Opening one column's filter closes any other, so only one is ever open.
- The filter surface's press does not also sort the column.

### The two filter kinds

- A column's filter kind is `FilterType` when set, and derived from the field's
  type when it is `ColumnFilterType.Auto`.
- An enum, a `bool` or a nullable `bool` field derives the select kind.
- Any other field type derives the text kind.
- The text kind renders one input and filters rows whose value contains the
  entered text, case-insensitively.
- The select kind renders one checkbox per distinct value and filters rows whose
  value is among those checked.
- The select kind's values come from `FilterValues` when the column supplies it,
  and are otherwise the distinct values of the field across `Items`.
- The derived values are sorted by their text, case-insensitively, so the list
  is stable between renders.
- A select column with no values renders an explicit empty message rather than
  an empty list.
- A null value is offered under an explicit placeholder rather than as a blank
  row.

### Applying and clearing

- Setting a text filter to only whitespace removes that filter rather than
  matching everything.
- Unchecking the last value of a select filter removes that filter.
- Applying or clearing any filter returns to the first page.
- Applying or clearing any filter marks the persisted state dirty.
- Filters combine with each other and with the search by conjunction: a row must
  satisfy all of them.
- A filter on a column that no longer exists is skipped rather than excluding
  every row.

### The filter chips

- Every applied filter renders one chip in the toolbar.
- A chip names its column and the value being filtered on.
- A text filter's chip shows the text in quotes.
- A select filter's chip lists its values when there are at most two, and
  otherwise reports how many are selected.
- A chip carries a control that removes its filter, labelled with the column it
  removes the filter from.
- A chip whose column has been removed is skipped rather than rendered
  unlabelled.
- The chips are announced as a labelled list.

## Recorded gaps

- **The filter surface is not portalled and is not a `DrylPopover`.** It is
  rendered inside its own header cell, so on a table with `Height` set — or in
  any scrolling container — it is clipped by the scroll box it lives in. Every
  other floating surface in the library goes through `DrylPopover`, which
  portals to the document body for exactly this reason.
- **`Escape` closes the filter only if the user focused it.** The key handler
  sits on the filter surface, which carries `tabindex="-1"` and is never focused
  programmatically when it opens. A user who opens a filter with the mouse and
  presses `Escape` gets nothing. This is the same defect recorded against
  `DrylPopover` in `specs/E11 Surfaces/F1 DrylPopover.md`, reproduced here
  because this surface was built separately.
- **Nothing closes a filter on an outside click.** The only ways out are the
  filter control, the surface's own close and done controls, and `Escape` after
  focusing it.
- **The search covers hidden columns.** Matching iterates every registered
  column rather than the visible ones, so a row can match a search on a column
  the user has switched off and appear for no visible reason.
- **A select filter's value set is mutated in place.** Toggling a value edits
  the same `HashSet` the descriptor already holds rather than replacing it, so
  the descriptor's value is not a snapshot; a consumer who captured it from a
  persisted state sees it change underneath them.
- **Restored filters lose their type.** State persistence stores select-filter
  values as strings, so a filter on an enum column comes back as a set of
  strings. The component compensates with a string-based membership check on
  every comparison, which is why matching a filter value is done twice — once by
  equality and once by text.
- **The distinct-value list is recomputed on every render of an open filter.**
  It enumerates `Items` and sorts it each time the surface is drawn.
