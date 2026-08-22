# Selection

## Meta
- **State:** Implemented

## Acceptance Criteria

### The checkbox column

- `Selectable` set renders a leading checkbox column in the header and in every
  row.
- `Selectable` left `false` renders no checkbox column at all.
- A row's checkbox reflects whether that row is selected.
- Toggling a row's checkbox adds or removes it from the selection.
- Clicking a row's checkbox cell does not also raise `OnRowClick`.
- Every selection change raises `SelectedItemsChanged` with a copy of the
  selection rather than with the table's own set.

### The header checkbox

- The header checkbox is checked when every row in the current view is selected.
- The header checkbox is unchecked when the view is empty, so an empty table
  does not claim everything is selected.
- Checking it selects every row in the current view.
- Unchecking it deselects every row in the current view.
- Rows outside the current view keep whatever selection state they had.

### Binding

- `SelectedItems` supplied replaces the table's selection on every parameter
  set, so the consumer's set is the source of truth when they bind one.
- `SelectedItems` left `null` lets the table keep its own selection across
  renders.
- The selection survives paging, sorting and filtering, because it is held by
  item rather than by index.

### The bulk-action bar

- The bar is rendered only when `BulkActions` is supplied **and** at least one
  row is selected.
- The bar reports how many rows are selected.
- The bar renders `BulkActions` with the current selection.
- The bar carries a control that clears the selection.
- Clearing the selection raises `SelectedItemsChanged` with an empty set.
- Clearing an already-empty selection raises nothing.
- The bar is announced as a labelled region.

### Row clicks

- Clicking a row raises `OnRowClick` with that row's item.
- Clicking the checkbox cell, the expand cell, the grip cell or the actions cell
  does not raise it, so an affordance inside a row is not also a row click.
- `OnRowClick` and `Selectable` are independent: a table can raise row clicks
  without a checkbox column, and vice versa.

## Recorded gaps

- **"Select all rows" selects the page, not the rows.** The header checkbox's
  accessible label says it selects all rows; it selects the rows in the current
  view, which under paging is one page of them. A user on page 1 of 13 who
  checks it and then acts on the selection gets 20 rows, not 247, and nothing in
  the UI says so.
- **Selection is by item identity.** The set is a `HashSet<TItem>` with the
  default comparer, so a table over a record type treats two equal-valued rows
  as one selection, and a table whose items are replaced by fresh instances on
  every fetch loses its selection even though the same rows are on screen. There
  is a `RowIdSelector`, but it is used only for the morph animations and not for
  selection.
- **`SelectedItems` is copied in on every parameter set.** The whole set is
  cleared and refilled on each render pass rather than only when the reference
  changes, which is proportional work per render for a large selection.
- **The bulk bar's appearance is not animated.** It is mounted and unmounted by
  a plain conditional, with no `DrylPresence` and no transition, so the table's
  content jumps down the moment a first row is selected (`DESIGN-12`).
