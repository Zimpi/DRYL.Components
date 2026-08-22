# Grouping, detail rows and export

## Meta
- **State:** Implemented

## Acceptance Criteria

### Grouping

- `GroupBy` set groups the current view by its result and renders a header row
  before each group.
- A group header names the group and reports how many rows it holds.
- A group with a null key is labelled with an explicit placeholder rather than
  being left blank.
- A group header is a control that collapses and expands its group.
- A collapsed group renders its header and none of its rows.
- The header reports its expanded state to assistive technology.
- Grouping is applied after paging, so a group header describes the rows on the
  current page.
- `GroupBy` and `Virtualize` are mutually exclusive; grouping is ignored when
  virtualization is on.
- `GroupBy` suppresses row reordering, because a manual position across groups
  has no meaning.

### Detail rows

- `DetailTemplate` set renders a leading expand column with a control per row.
- Pressing the control expands or collapses that row's detail.
- The control reports its expanded state to assistive technology and carries a
  label naming what it will do.
- An expanded row renders one further row beneath it holding the template's
  output, spanning the table's full width.
- Several rows can be expanded at once.
- Clicking the expand cell does not also raise `OnRowClick`.
- `DetailTemplate` left `null` renders no expand column at all.

### CSV export

- `ShowExport` set renders an export control in the toolbar.
- The export covers the current search, filters and sort — and **all** pages,
  not the page in view.
- In server mode the export issues its own request with the current search, sort
  and filters and an unbounded take, so it exports what the user filtered rather
  than what they are looking at.
- The export includes only the visible columns, in their current order.
- The first line is the columns' display titles.
- A field containing a comma, a quote or a line break is quoted, and an inner
  quote is doubled.
- A null value is exported as an empty field.
- A formattable value is written with invariant culture, so the file is the same
  whatever the server's locale.
- The file is prefixed with a UTF-8 byte-order mark, so a spreadsheet opens it
  in the right encoding.
- The file is named `ExportFileName`.

## Recorded gaps

- **A group header is not a group.** The headers are ordinary rows carrying a
  toggle; the rows beneath them are not associated with their header by any
  ARIA relationship, so a screen-reader user hears a row that says "Production,
  12" and then twelve rows that do not say they belong to it.
- **Collapsing a group hides rows that stay selected.** A collapsed group's rows
  remain in the selection and in the export, which is correct — but the header
  checkbox's "all selected" state is computed over the whole view including
  them, so the header can appear unchecked with no visible unchecked row.
- **Group state is keyed by the group's text.** A group is identified by its
  key's string form, so two distinct keys with the same text collapse together,
  and a collapsed group's state does not survive the key's type changing.
- **Expanded rows are keyed by hash code.** The detail row's key is built from
  the item's hash code, so two rows sharing a hash share a key — which in
  Blazor's diff means the wrong detail row can be reused.
- **Neither grouping nor expansion is animated.** Collapsing a group and
  expanding a detail row both change the DOM between two frames, with no
  `DrylPresence` and no height transition (`DESIGN-11`, `DESIGN-12`). Expansion
  is the most-used gesture the table has after sorting.
- **The export's line ending follows the host.** Rows are appended with the
  environment's newline, so the same application produces CRLF-terminated files
  on Windows and LF-terminated ones on Linux. RFC 4180, which the quoting rules
  do follow, specifies CRLF.
- **The export is built in memory as one string.** Every row of every page is
  formatted into a single `StringBuilder` and handed to JS as one argument, so
  a large export is bounded by what the circuit will carry rather than streamed.
- **The unbounded server export is documented but not enforced.** The export's
  request asks for every matching row; a backend that honours it literally will
  materialise the whole table.
