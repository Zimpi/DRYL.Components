# Inline editing

## Meta
- **State:** Implemented

## Acceptance Criteria

### When editing is possible

- `Editable` set with no `DataProvider` enables inline editing.
- `Editable` set with a `DataProvider` disables it and reports the conflict once
  on the console, because a server-side source owns its own rows.
- Editing affordances appear only when at least one column supplies an
  `EditTemplate`.
- A column with no `EditTemplate` stays read-only while its row is being edited.

### Starting an edit

- The trailing actions cell renders an edit control on every row while editing
  is enabled and some column is editable.
- Pressing that control starts editing the row.
- Double-clicking an editable cell starts editing.
- Double-clicking a cell of a row already being edited does nothing, so a stray
  double-click cannot discard buffered changes.
- Double-clicking a non-editable cell does nothing.
- `BeginEdit` starts editing a row programmatically.
- `BeginEditCell` starts editing one named cell programmatically, and is
  honoured only in cell mode and only for an editable column.
- Starting an edit schedules focus into the first editor, so the user can type
  immediately.
- Only one row is ever being edited.

### Row mode and cell mode

- `TableEditMode.Row` puts every editable column of the row into its editor.
- `TableEditMode.Cell` puts only the activated column into its editor.
- `BeginEdit` in cell mode activates the first editable visible column.
- A cell rendering its editor is marked as such, so the editing cell is visually
  distinct from its neighbours.

### The working copy

- `CloneRow` set makes the editors bind to a clone, so cancelling reverts
  cleanly.
- `CloneRow` left `null` makes the editors bind to the live item, so edits are
  applied as they are typed and cancelling reverts nothing.
- The distinction is the consumer's to make, and it is the difference between a
  cancellable edit and a live one.

### Committing and cancelling

- `Enter` in the row being edited commits.
- `Escape` in the row being edited cancels.
- The commit and cancel controls in the actions cell do the same.
- Committing raises `OnRowCommitted` with both the original item and the edited
  one, so a consumer can diff, validate or persist.
- Cancelling raises `OnRowCancelled` with the original item.
- Both clear the edit state before raising, so a handler that re-renders the
  table does not find it still in edit mode.
- The table applies nothing itself: committing an edit changes the collection
  only if the consumer's handler does.

### Losing the row

- An edit whose row is no longer in `Items` is dropped on the next parameter
  set, so commit and cancel cannot act on a row that has vanished.

## Recorded gaps

- **Keyboard commit and cancel reach only the row's own key events.** The
  handler sits on the row element, so an editor that stops key propagation — or
  a `DrylSelect` panel portalled out of the row — swallows `Enter` and `Escape`
  before the table sees them.
- **There is no validation hook.** `OnRowCommitted` is raised unconditionally;
  the table offers no way to refuse a commit, and no way to show a per-field
  error inside the editing row. A consumer who needs validation has to prevent
  the edit from being committed by re-rendering the row back into edit mode.
- **Cell mode has no cell-to-cell keyboard movement.** `Tab` leaves the editor
  to the next focusable element in the document rather than to the next editable
  cell, and there is no arrow-key movement between cells.
- **Editing is not announced.** Entering and leaving edit mode changes the
  markup with no live region and no focus announcement beyond the editor
  receiving focus, so a screen-reader user is told which field they are in and
  not that the row became editable.
- **Nothing about entering or leaving an edit is animated** (`DESIGN-11`,
  `DESIGN-12`): the cells swap between text and editors between two frames.
- **The identity comparison is the default one.** Whether a row "is" the row
  being edited is decided by `EqualityComparer<TItem>.Default`, so a table over
  a record type can consider two equal-valued rows the same row — the same
  identity gap `S4` records for selection.
