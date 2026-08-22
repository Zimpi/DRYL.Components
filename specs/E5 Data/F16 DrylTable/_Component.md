# DrylTable

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTable.razor
              code/DRYL.Components/Components/Data/DrylColumn.cs
              code/DRYL.Components/Components/Data/Models/ColumnAlign.cs
              code/DRYL.Components/Components/Data/Models/ColumnFilterType.cs
              code/DRYL.Components/Components/Data/Models/ColumnPin.cs
              code/DRYL.Components/Components/Data/Models/DataRequest.cs
              code/DRYL.Components/Components/Data/Models/DataResult.cs
              code/DRYL.Components/Components/Data/Models/FilterDescriptor.cs
              code/DRYL.Components/Components/Data/Models/RowEditEventArgs.cs
              code/DRYL.Components/Components/Data/Models/RowReorderEventArgs.cs
              code/DRYL.Components/Components/Data/Models/SortDescriptor.cs
              code/DRYL.Components/Components/Data/Models/TableEditMode.cs

This spec is split (`SPEC-02`). The acceptance criteria live in the `S{n}` files
beside this one; this file carries the `Meta` block, the description and the
public API. The state above is the rolled-up state: it reads `Implemented` only
while every `S{n}` beside it does.

| File | Subject |
|---|---|
| `S1 Data source and paging.md` | `Items` versus `DataProvider`, the view pipeline, paging, virtualization |
| `S2 Search and filtering.md` | The toolbar search, per-column filters, the filter chips |
| `S3 Sorting.md` | Click-to-sort, multi-sort, the sort's reach into other features |
| `S4 Selection.md` | Row selection, the header checkbox, the bulk-action bar |
| `S5 Columns.md` | Registration, order, visibility, resize, reorder, pinning |
| `S6 Inline editing.md` | Row and cell editing, the working copy, commit and cancel |
| `S7 Row reordering and motion.md` | Drag and keyboard reorder, view-transition morphs, streaming glide |
| `S8 Grouping, detail rows and export.md` | `GroupBy`, `DetailTemplate`, CSV export |
| `S9 Presentation, accessibility and AI.md` | Layout, tokens, roles, keyboard, `Ai`, state persistence, cleanup |

## User Story

As a Blazor developer, I want a table I describe by declaring its columns, and
that then handles searching, sorting, filtering, paging, selecting, editing and
exporting on its own, so that a data screen is a description of what the columns
are rather than an implementation of what a table does.

## Description

`DrylTable<TItem>` is the largest component in the library by an order of
magnitude, and its size comes from one design choice: **the columns are
components**. A consumer writes `DrylColumn<TItem>` elements into the `Columns`
slot; each column registers itself with the table through a cascade and renders
nothing itself. The table then reads its registered columns to build the header
and every cell.

That is what makes the feature list declarative rather than configured. A column
that says `Sortable` gets a clickable header and joins the sort pipeline. A
column that says `Searchable` joins the toolbar search. `Filterable` adds a
filter control to its header and a chip to the toolbar when it is active.
`Field` supplies the value, the sort key and the search source at once, and its
member name becomes the column's stable key, which is what filter state, the
column order, the resize widths and the persisted state are all recorded
against.

The table works in **two modes and knows which one it is in**. With `Items` it
owns the whole pipeline in memory: search, filter, sort, page. With
`DataProvider` it owns none of it — it describes what the user asked for as a
`DataRequest` and renders whatever comes back. Several features are meaningful
only in the first mode, and the table disables rather than mis-applies them:
inline editing, row reordering and the row-morph animations all require a plain
client-side list, and each one says so on the console when it is configured
where it cannot work.

Around that core sit the optional surfaces: a KPI summary bar, a toolbar with
search and export and a column-visibility menu, a bulk-action bar that appears
with the first selection, per-row detail rows, per-row action slots, grouping
headers, and a pagination footer. Each of them is absent from the markup
entirely when it is not configured.

## Public API

### Data

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Items` | `IEnumerable<TItem>` | `[]` | The rows, when no `DataProvider` is set. |
| `DataProvider` | `Func<DataRequest, CancellationToken, ValueTask<DataResult<TItem>>>?` | `null` | Server-side mode: the table asks, the delegate answers. |
| `Columns` | `RenderFragment?` | `null` | The `DrylColumn<TItem>` declarations. |
| `RowIdSelector` | `Func<TItem, object>?` | `null` | Stable per-row identity for the morph animations. |

### Slots

| Member | Type | Default | Purpose |
|---|---|---|---|
| `SummaryContent` | `RenderFragment?` | `null` | KPI bar above the toolbar. |
| `ToolbarContent` | `RenderFragment?` | `null` | Extra toolbar content. |
| `EmptyContent` | `RenderFragment?` | `null` | Shown instead of the default empty message. |
| `DetailTemplate` | `RenderFragment<TItem>?` | `null` | Expandable detail row per row. |
| `RowActions` | `RenderFragment<TItem>?` | `null` | Trailing per-row action cell. |
| `BulkActions` | `RenderFragment<IReadOnlySet<TItem>>?` | `null` | Content of the bulk-action bar. |

### Toolbar, search and export

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ShowToolbar` | `bool` | `false` | Renders the toolbar row. |
| `Searchable` | `bool` | `false` | Renders the global search input. |
| `SearchPlaceholder` | `string` | `"Search…"` | Placeholder and accessible label of the search input. |
| `SearchDebounceMs` | `int` | `200` | Debounce before a search is applied. |
| `SearchPredicate` | `Func<TItem, string, bool>?` | `null` | Replaces the per-column search entirely. |
| `ShowExport` | `bool` | `false` | Renders the CSV export button. |
| `ExportFileName` | `string` | `"export.csv"` | File name of the export. |
| `AllowColumnVisibility` | `bool` | `false` | Renders the column-visibility menu. |

### Paging and size

| Member | Type | Default | Purpose |
|---|---|---|---|
| `PageSize` | `int` | `0` | Rows per page. `0` disables paging. |
| `PageSizeChanged` | `EventCallback<int>` | — | Raised when the user picks a size. |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 20, 50, 100]` | Choices in the footer's size selector. |
| `Page` | `int` | `0` | Zero-indexed current page. |
| `PageChanged` | `EventCallback<int>` | — | Raised when the user navigates. |
| `Virtualize` | `bool` | `false` | Renders only the visible rows. Excludes paging and grouping. |
| `VirtualizeItemSize` | `float` | `44f` | Row height hint for virtualization. |
| `Height` | `string?` | `null` | Maximum height; makes the table scroll. |
| `StickyHeader` | `bool` | `true` | Keeps the header visible while scrolling. |

### Selection and interaction

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Selectable` | `bool` | `false` | Renders the checkbox column. |
| `SelectedItems` | `IReadOnlySet<TItem>?` | `null` | The selection, for two-way binding. |
| `SelectedItemsChanged` | `EventCallback<IReadOnlySet<TItem>>` | — | Raised on every selection change. |
| `OnRowClick` | `EventCallback<TItem>` | — | Raised when a row is clicked. |
| `GroupBy` | `Func<TItem, object?>?` | `null` | Groups rows under collapsible headers. |

### Reordering and editing

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Reorderable` | `bool` | `false` | Renders the drag grip column. |
| `OnRowReordered` | `EventCallback<RowReorderEventArgs>` | — | Raised with the moved row's old and new index. |
| `AnimateReorder` | `bool` | `false` | Morphs rows between positions with a morph. |
| `Editable` | `bool` | `false` | Enables inline editing. |
| `EditMode` | `TableEditMode` | `TableEditMode.Row` | Whole row or single cell. |
| `CloneRow` | `Func<TItem, TItem>?` | `null` | Supplies the working copy so cancel can revert. |
| `OnRowCommitted` | `EventCallback<RowEditEventArgs<TItem>>` | — | Raised with the original and the edited item. |
| `OnRowCancelled` | `EventCallback<TItem>` | — | Raised with the item whose edit was abandoned. |
| `ResizableColumns` | `bool` | `false` | Renders per-column resize handles. |
| `ReorderableColumns` | `bool` | `false` | Allows header drag and `Alt`+arrow column moves. |

### Presentation and state

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Loading` | `bool` | `false` | Renders the loading row instead of the data. |
| `Bordered` | `bool` | `true` | Renders the table on the library's card surface. |
| `AriaLabel` | `string?` | `null` | Accessible label for the table. |
| `PersistStateKey` | `string?` | `null` | Persists sort, filters, page, size, hidden columns, widths and order. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the table's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

### Public methods

| Member | Signature | Purpose |
|---|---|---|
| `BeginEdit` | `void BeginEdit(TItem item)` | Starts editing a row. |
| `BeginEditCell` | `void BeginEditCell(TItem item, string columnKey)` | Starts editing one cell. |
| `OnColumnResized` | `[JSInvokable] void OnColumnResized(string key, double widthPx)` | Called by the resize helper; not for consumer use. |

`DrylColumn<TItem>`'s own parameters, and every supporting type named in
`Source`, are set out in [`../_Api.md`](../_Api.md).
