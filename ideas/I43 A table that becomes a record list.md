# A table that becomes a record list

## Meta
- **State:** Adopted
- **Specs:** `specs/E5 Data/F16 DrylTable/S10 Compact presentation.md`, with
  `_Component.md`, `S5`, `S8` and `S9` beside it and `specs/E5 Data/_Api.md`,
  `specs/E5 Data/_Interop.md`. Two things were built differently from the
  notes below, and the spec is what holds: the tap on a record is forwarded by
  a small browser helper, since nothing else can tell a compact table from a
  wide one at the moment of the tap; and the records beneath an unfolding one
  do not glide — the content fades and rises in, the rest is recorded as a gap.

## Problem

`DrylTable` has no narrow presentation. Below roughly 480px of available width
it keeps its columns and squeezes them: measured on the docs page at a 375px
viewport, a four-column table is laid out in 295px with no horizontal overflow,
so `auth-service` breaks into three lines mid-word and the header `ENVIRONMENT`
stacks into four. `.tbl-wrap` offers horizontal scroll, but the browser shrinks
the columns before it ever scrolls. The toolbar wraps without a plan, and the
header controls (sort, filter, resize) become touch targets a few pixels wide.

A grid is the wrong shape for a phone: comparing values down a column needs
width the device does not have. Whoever opens a DRYL data screen on a phone
wants to *find a record and read it*, not scan a column.

Target role: the Blazor developer who ships one data screen for desktop and
phone and does not want to write a second, hand-made list for the phone.

## Solution Idea

Below a container width, the table stops being a grid and becomes a **record
list**: one record per row, laid out as a small composed tile instead of a row
of cells. Three variants were put to the Product Owner:

- **A — Stacked cards.** Every record is a card showing every column as a
  label/value pair. Nothing hidden, but tall: four records fill a phone screen,
  the labels repeat on every card, and finding a record means scrolling.
- **B — Record list with a detail sheet.** Every record is a two-line tile
  (title, one trailing value, a muted meta line). A tap opens an overlay sheet
  with all fields and the row actions. Dense and familiar, but needs an overlay
  surface the table does not own today, and takes the user out of the list.
- **C — Record list that unfolds in place (recommended).** The same two-line
  tile as B; a tap unfolds the record in the list: the remaining columns as
  label/value pairs, the `DetailTemplate` below them, the row actions as a
  closing row. Dense at rest, complete on demand, no overlay, and the unfold is
  the motion the library already speaks (I41, I42).

Common to all three:

- **Column roles.** The table has to know which columns form the tile. The
  `Primary` column is the title. A column-level parameter assigns the others
  (trailing value, meta line, detail only, hidden when compact); with no
  assignment a heuristic applies (first non-primary column trailing, next two
  meta, the rest detail).
- **Header features move.** With no header row, sort and filter are reached
  through one control in the toolbar that lists the sortable and filterable
  columns. Column resize, column reorder and pinning have no meaning in a list
  and are inert while compact.
- **Everything else stays.** Search, selection and the bulk bar, grouping (as
  section headers), paging, inline editing (the editors appear in the unfolded
  record), streaming row entry and the aura keep working.
- **Container-query driven**, like the rest of the library: the switch follows
  the table's own available width, not the viewport, so a table in a narrow
  panel on a desktop becomes a list as well.

## Scope
- **In scope:** the compact presentation of `DrylTable`; the column role
  parameter; the toolbar's compact sort/filter control; the compact behaviour
  of every existing table feature; demo page, spec, tests.
- **Out of scope:** swipe gestures; touch drag for row reordering; a new
  bottom-sheet component; changes to the wide presentation; `DrylPagination`
  beyond what it already does at 480px.

## Impact
- **Harness:** no new `AiState`, no dependency. Motion: the unfold must use the
  existing durations and easings (`DESIGN-10`) — expected to fit, to be
  confirmed against I42's seam. Tokens: the tile should be buildable from
  existing spacing and type tokens; the table's own literal sizes are already
  recorded debt in `S9` and must not grow (`DESIGN-01`). Any gap found here is a
  blocker to bring back, not to invent.
- **Specs:** `specs/E5 Data/F16 DrylTable/` — a new `S10` for the compact
  presentation; `S5 Columns` (role parameter, inert resize/reorder/pin), `S2`
  and `S3` (the toolbar control), `S9` (roles and keyboard). `_Component.md` and
  `specs/E5 Data/_Api.md` gain the new members.
- **Public API:** additive only. A table parameter that names the breakpoint
  (the existing `Breakpoint` enum, as `DrylStack.CollapseBelow` does) and an
  opt-out; a `DrylColumn` parameter with a new enum for the role; possibly a
  template slot for consumers who want to draw the tile themselves.
- **Code:** `DrylTable.razor`, `DrylColumn.cs`, the `.tbl*` block in
  `dryl.css`. The central decision is *one DOM restyled by a container query*
  versus *a second render path chosen in C#*. C# cannot read a container query
  without JS, and a JS-measured switch flashes the grid after prerender, so the
  working assumption is one DOM: cells carry their role and label, the query
  turns the row into a tile, and the unfolded state is ordinary component
  state. Risks: table semantics once rows stop being `display: table-row`
  (explicit roles needed; `S9` already records the unearned `grid` role);
  `VirtualizeItemSize` no longer matching the tile height; sticky header and
  pinned-column layout code running against a list.

  **Spike, 2026-11-19 (Tech Lead).** A throwaway stylesheet injected into the
  running docs page at a 375px viewport, with the role and label attributes the
  component would render set by script; nothing in the repository was changed.
  One container query on `.tbl-wrap` turned the existing markup into the tile
  on every demo table: plain, selectable with toolbar, grouped with detail
  chevron, editable with the actions cell, reorderable, and the six-column
  pinned table (two columns unfolding as label/value pairs). No wrapper element
  and no second render path was needed; rows came out 66–70px tall. Findings:
  - The order-based heuristic is a fallback, not a design: it put `Environment`
    in the trailing slot and the status badge on the meta line, and made a SKU
    the title where the product name reads better. The per-column override is
    required, not optional.
  - The unfold is a `display` switch on cells, so the reveal itself can only
    fade and rise; the rows below have to be moved by the row morph the table
    already owns. Not yet built or measured.
  - A row in edit mode does not fit the tile: the editors land in the title,
    trailing and meta slots and are cramped to 42px where the slot is narrow.
    An editing record needs its own layout — every editable cell full width
    under its label, the commit and cancel controls closing it — which the
    same query can supply from the existing `tbl-td-editing` class.
  - Table semantics do not survive: once the row is a grid container the
    browser exposes its cells as generic elements. The compact presentation
    needs explicit roles on the row and its cells, and `S9`'s recorded `grid`
    role gap has to be settled with it rather than around it.
  - Light mode renders the tile from the same rules; no mode-specific value was
    needed.
  - Not covered by the spike: `Virtualize`, and the unfold animation.

## Decisions
- 2026-11-19: Work happens directly on `release/3.17.0` (Product Owner).
- 2026-11-19: Variant C — the record list that unfolds in place (Product
  Owner). A shows too few records per screen; B needs an overlay surface the
  table does not own and hides the list.
- 2026-11-19: The record list is the default for every table below
  `Breakpoint.Sm`, with an opt-out per table (Product Owner). The current
  narrow rendering is a defect, not a look anyone depends on.
- 2026-11-19: Column roles are assigned by a heuristic and overridable per
  column: title, trailing, meta, detail only, hidden while compact (Product
  Owner). The spike showed the heuristic alone misplaces columns.
- 2026-11-19: A table with sortable or filterable columns but no toolbar shows
  a slim control row carrying only the sort/filter control while compact,
  rather than losing sorting (Product Owner).
- 2026-11-19: Every record has a chevron button that unfolds it, the same one
  that expands the `DetailTemplate`. A tap on the record raises `OnRowClick`
  when one is set and unfolds the record when none is (Product Owner), so a
  consumer who navigates on row click keeps that behaviour.
- 2026-11-19: The reorder grip stays while compact; touch drag is out of scope
  (Product Owner). A narrow panel on a desktop still has a mouse and
  `Alt`+arrow.
- 2026-11-19: Virtualized tables get the record list too; the imprecise scroll
  estimate while compact is recorded as a known gap in the spec rather than
  excluding them (Product Owner). The component cannot know the tile height
  because it cannot know it is compact.
- 2026-11-19: The final shape below is confirmed; the idea is `Ready` (Product
  Owner).

## Consequences of the one-DOM approach (Tech Lead)

The switch is made by CSS, so the component never knows which presentation is
showing. Three things follow, and they are constraints rather than choices:

- **Roles are static.** The markup cannot change its roles with the query, so
  the rows and cells carry explicit table roles in both presentations, and the
  root claims `table` instead of `grid` — which also closes the gap `S9`
  records. While compact the header row is not rendered to the accessibility
  tree; each unfolded cell is named by its visible label.
- **Compact-only controls are always in the markup.** The sort/filter control
  and the unfold control are rendered at every width and shown by the query.
- **Unfolded is ordinary component state**, tracked at every width and visible
  only while compact.

The unfold animation is planned from what exists: the rows below glide through
the row morph the table already owns, and the unfolding cells fade and rise at
`--dur-med` with `--ease-out`, using the discrete-transition technique I42
introduced. If the build shows it needs a value outside the vocabulary, that
returns here as a blocker (`DESIGN-10`).

## Final shape

Below `Breakpoint.Sm` of its own available width, every `DrylTable` is a record
list unless the table opts out. A record at rest is a two-line tile: the title
(the `Primary` column unless a column claims the role), one trailing value, and
a meta line of up to two values. A chevron button unfolds the record in place:
the detail-only columns as label/value pairs, then the `DetailTemplate`, then
the row actions. A tap on the record itself raises `OnRowClick` when one is
set and unfolds the record when none is. A record in edit mode shows every
editable cell full width under its label, closed by commit and cancel.

Sort and filter are reached through one control in the toolbar, or in a slim
control row when the table has no toolbar. Search, selection and the bulk bar,
grouping, paging, streaming row entry, the aura and the reorder grip keep
working; column resize, column reorder and pinning are inert while compact.

Working names for the additive API, to be settled in the spec against
`harness/conventions.md`: a `Breakpoint?` parameter on the table in the manner
of `DrylStack.CollapseBelow`, defaulting to `Breakpoint.Sm`, with `null` as the
opt-out; a role parameter on `DrylColumn` backed by a new enum.

## Open Points

