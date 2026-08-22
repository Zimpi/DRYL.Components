# Columns

## Meta
- **State:** Implemented

## Acceptance Criteria

### Registration

- A `DrylColumn<TItem>` placed in the `Columns` slot registers itself with the
  table when it initialises.
- A column registered twice is added once.
- A column unregisters itself when it is disposed.
- Unregistering a column also drops its sort, its filter, its hidden state, its
  width override and its position in the column order, so a removed column
  leaves nothing behind.
- A column renders nothing itself; the table reads its registered columns to
  build the header and the cells.
- A column outside a table's `Columns` slot throws on initialisation rather than
  rendering nothing silently.
- Registration does not itself request a render; the table rebuilds once after
  the render pass in which its columns registered.

### Keys

- A column's key is `ColumnKey` when set.
- A column with a `Field` and no `ColumnKey` derives its key from the field's
  member name.
- A column with neither gets a generated key, so every column has one.
- The key is what sort, filter, visibility, width, order and persistence are all
  recorded against.

### Values and templates

- A column with a `CellTemplate` renders it with the row's item.
- A column with no `CellTemplate` renders its `Field`'s value as text.
- A column with a `HeaderTemplate` renders it as the header's content.
- A column with no `HeaderTemplate` renders `Title` as text.
- A column with no `Title` renders its key as the header, so a header is never
  blank.
- A column marked `Primary` renders its cells with the primary emphasis, so the
  identity column is legible at a glance.
- `Align` sets the horizontal alignment of both the header and the cells, and
  the start alignment adds no class of its own.

### Visibility

- A column marked `Hidden` starts hidden.
- `AllowColumnVisibility` set renders a menu listing every registered column
  with a checkbox reflecting its visibility.
- Toggling an entry shows or hides that column immediately.
- Toggling an entry marks the persisted state dirty.
- The menu is closed by its own close control and by `Escape`.
- Hidden columns are excluded from the header, from every row and from the CSV
  export.

### Width and resizing

- A column with `Width` renders at that width.
- `ResizableColumns` set renders a resize handle on every column that has not
  opted out through `Resizable`.
- A completed resize stores the new width as a runtime override for that column.
- A runtime override wins over the column's declared `Width`.
- A resize of zero or less is ignored, so a handle dragged past its own column
  cannot collapse it.
- The stored width is written with invariant formatting, so a German locale
  cannot emit a decimal comma into a CSS length.
- A completed resize marks the persisted state dirty.
- The resize helper is attached once and detached when the table is disposed.
- A failed attach is not remembered as attached, so a later render can retry.

### Order

- Columns render in registration order until the user moves one.
- `ReorderableColumns` set makes a column draggable unless it has opted out
  through `Reorderable` or is pinned.
- Dropping a column onto another moves it to that position.
- `Alt` with the left or right arrow on a focused header moves the column one
  step in that direction.
- A keyboard move returns focus to the moved header, so a sequence of moves does
  not lose the user's place.
- A move is confined to one pin group: a column cannot be dragged out of or into
  a frozen edge.
- A move at either end of its group is a no-op rather than a wrap-around.
- Any move marks the persisted state dirty.

### Pinning

- A column pinned to the start renders before every unpinned column.
- A column pinned to the end renders after every unpinned column.
- The pin partition preserves the user's order within each group, because the
  ordering is stable.
- A pinned column stays in view while the table is scrolled horizontally.
- The pinned columns' offsets are re-measured after every render, so a resize, a
  visibility change or a reorder does not leave them overlapping.
- A pinned column is excluded from drag reordering.

## Recorded gaps

- **The column-visibility menu is not portalled and is not a `DrylPopover`.**
  Like the filter surface in `S2`, it is rendered in place, closes on no outside
  click, and answers `Escape` only if the user has focused it — which nothing
  does when it opens.
- **The visibility menu lists hidden columns by their key.** A column with
  neither a `Title` nor a `Field`-derived name appears in the menu as a
  generated identifier.
- **Nothing is animated.** A column appearing, disappearing, moving or being
  resized happens between two frames. The table has a whole view-transition
  machinery for row moves (`S7`) and none of it is applied to columns
  (`DESIGN-11`, `DESIGN-12`).
- **`Pinned` requires a scroll container that the table does not enforce.**
  Pinning has no effect unless the table is horizontally scrollable, which
  requires `Height` or a constrained container; a consumer who pins a column in
  an unconstrained table sees only the reordering restriction, with no
  indication why.
- **A dead field.** The component declares a flag documented as re-measuring the
  pinned offsets after a render; nothing ever reads or writes it, and the
  re-measure actually runs unconditionally whenever any column is pinned. The
  comment describes an intent the code does not implement — the field should go
  or the comment should.
- **The resize handle is a `separator` with no keyboard operation.** It carries
  the separator role and an orientation, which announces it as a resizable
  divider, but it is driven entirely by pointer events. There is no keyboard
  path to resizing a column, unlike reordering, which has one.
