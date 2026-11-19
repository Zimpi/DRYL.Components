# Compact presentation

## Meta
- **State:** Implemented

Below a width of its own, the table stops being a grid and becomes a record
list: one record per row, drawn as a small tile that unfolds in place. The idea
and its decisions are in `ideas/I43 A table that becomes a record list.md`.

## Acceptance Criteria

### When the table is compact

- `CompactBelow` defaults to `Breakpoint.Sm`.
- `CompactBelow` accepts every value of `Breakpoint`.
- The table is compact while its own available width is at or below the
  breakpoint, whatever the width of the viewport.
- `CompactBelow` set to `null` keeps the grid at every width.
- The switch is made by the stylesheet alone: the same markup serves both
  presentations, and nothing is measured in script to choose between them.
- A table nested inside another table's detail follows its own `CompactBelow`,
  not the surrounding table's presentation.

### The record at rest

- While compact, the header row is not shown.
- A record shows its title, its trailing value and its meta values; every other
  column is not shown until the record is unfolded.
- The title and the trailing value share the first line, the title leading.
- The meta values share the second line, the first leading and the second
  trailing.
- The selection checkbox and the reorder grip, where configured, lead the
  record.
- Records are separated by a rule in `--line`.

### Column roles

- `CompactRole` defaults to `ColumnCompactRole.Auto`.
- The title is the first visible column with `ColumnCompactRole.Title`.
- With no such column, the title is the first visible `Primary` column left on
  `ColumnCompactRole.Auto`.
- With neither, the title is the first visible column left on
  `ColumnCompactRole.Auto`.
- The trailing value is the first visible column with
  `ColumnCompactRole.Trailing`; with none, the first remaining column left on
  `ColumnCompactRole.Auto`.
- The meta values are the first two visible columns with
  `ColumnCompactRole.Meta`; an empty meta slot is filled by the next remaining
  column left on `ColumnCompactRole.Auto`.
- Every column left on `ColumnCompactRole.Auto` once the slots are filled is
  shown only in the unfolded record.
- A column with `ColumnCompactRole.Detail` is shown only in the unfolded record.
- A column with `ColumnCompactRole.Hidden` is not shown while compact, and is
  shown as usual while wide.
- A second column claiming the title or the trailing value, and a third
  claiming a meta value, is shown only in the unfolded record rather than
  dropped.
- A column hidden through the visibility menu takes no role.
- `CompactRole` has no effect while the table is wide.

### Unfolding

- A record that has something to unfold renders an unfold control: a button
  that reports its expanded state and is labelled with what it will do.
- A record has something to unfold when a column is shown only in the unfolded
  record, when the table has a trailing actions cell, or when `DetailTemplate`
  is set.
- The unfold control is the control that expands the `DetailTemplate`; a table
  without a `DetailTemplate` shows it only while compact.
- An unfolded record shows each detail column as its header title followed by
  its value.
- An unfolded record shows its row actions after the detail columns.
- An unfolded record's `DetailTemplate` output follows the record.
- Several records can be unfolded at once.
- Pressing the unfold control does not raise `OnRowClick`.
- A tap on a compact record raises `OnRowClick` when one is set, and does not
  unfold the record.
- A tap on a compact record unfolds or folds it when no `OnRowClick` is set.
- A tap on a control inside the record — a button, a link, a form control —
  belongs to that control and does not unfold the record.
- A tap on a record in edit mode does not fold it.
- A click on a row of a wide table never unfolds it.
- The unfolding content fades and rises in at `--dur-med` with `--ease-out`.
- Under `prefers-reduced-motion: reduce` the unfolding content appears without
  motion.

### Sorting and filtering

- A table with at least one sortable or filterable column renders a sort and
  filter control that is shown only while compact.
- With `ShowToolbar` set the control sits in the toolbar; without it, in a
  control row of its own that is shown only while compact.
- The control reports its expanded state and opens a panel between the toolbar
  and the records.
- The control is marked active while any sort or filter is applied.
- The panel lists every visible sortable column as a button showing that
  column's current direction.
- Pressing a column's button moves it through the cycle of `S3`: ascending,
  descending, none.
- The button reports the column's direction in its accessible name.
- The panel lists every visible filterable column with the same filter editor
  its header offers while wide.
- A change in the panel has the same effect on the view, the filter chips and
  the persisted state as the same change made from the header.
- The panel enters and leaves through `DrylPresence`.

### Editing

- A record in edit mode shows every cell under edit on a line of its own, at
  the full width of the record, under its header title.
- A record in edit mode shows the commit and cancel controls whether or not it
  is unfolded.

### What stays, and what rests

- Search, selection, the bulk bar, grouping, paging, the streaming row entry
  and the aura behave as they do while wide.
- A group header spans the list as a section header.
- The reorder grip stays operable while compact.
- Column resize handles, header drag-reorder and pinning have no effect while
  compact, because the header row they belong to is not shown.
- A pinned column loses its opaque backing and its edge rule while compact, so
  it reads as part of the record.

### Accessibility

- The table, its rows, its header cells and its cells carry explicit table
  roles, so the structure is announced the same way in both presentations.
- While compact, each value shown in the unfolded record is preceded by its
  column's title as text.

## Recorded gaps

- **Folding is not animated, and the records below do not glide.** The unfold
  is a `display` change on cells, so the content can fade and rise in but
  cannot leave gradually, and the records beneath it move in one frame
  (`DESIGN-11`). The same is recorded for the detail row in `S8`.
- **`VirtualizeItemSize` is wrong while compact.** The component does not know
  it is compact, so it cannot hand the virtualizer the tile's height, and an
  unfolded record changes height underneath it. A virtualized table is still
  shown as a record list; its scroll estimate is imprecise there.
- **The tap needs script.** The tap on a record is forwarded to its unfold
  control by `dryl.table.initCompactTap`, because the component cannot know
  that it is compact. Before the page is interactive, and wherever that helper
  failed to attach, only the unfold control itself unfolds a record.
- **A wide trailing value squeezes the title.** The trailing value takes the
  width its content asks for and the title takes what is left, so a long
  trailing text leaves the title a narrow column to wrap in.
- **Touch cannot drag a record.** The grip relies on HTML drag-and-drop, which
  touch screens do not fire; on a phone a record can be reordered only with a
  keyboard.
- **The table's width no longer follows its content.** A table that can become
  compact is a size container, so in a shrink-to-fit context — an inline
  block, a flex item without a basis — it no longer takes its width from its
  columns. `CompactBelow` set to `null` restores that.
- **The panel's texts are fixed.** "Sort", "Filter" and the control's label are
  English literals, as the table's other built-in texts are.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the compact rules name only tokens and the properties
  already in use by the wide table; no mode-specific rule exists.
- **Enter/exit animation** — the unfold fades and rises; the panel uses
  `DrylPresence`. The missing exit and glide are recorded above.
- **Keyboard and a11y** — the unfold control and every panel control are
  buttons in the tab order; the explicit roles are described above.
- **AI mode** — no decision of its own: the compact presentation is the same
  table and carries its aura and streaming entry unchanged.
- **Demo page** — every example under `DRYL.Website/Components/Examples/Table/`
  becomes compact at a narrow width. An example that assigns `CompactRole`
  does not exist yet; every demo table shows the roles the table assigns
  itself.
- **Tests** — `tests/DRYL.Components.Tests/DrylTableCompactTests.cs` guards
  what the markup carries: the breakpoint class and the opt-out, the role
  assignment, the unfold control, the tap marker, the sort and filter control
  and its panel, the explicit roles and the width property. The layout itself
  is the stylesheet's and was checked by eye on the docs page at a phone width
  and at desktop width, in both color modes.
