# Data source and paging

## Meta
- **State:** Implemented

## Acceptance Criteria

### Choosing a mode

- `DataProvider` left `null` puts the table in client mode: it reads `Items` and
  owns the whole pipeline.
- `DataProvider` set puts the table in server mode: it reads nothing from
  `Items` and renders whatever the delegate returns.
- The mode is decided per rebuild rather than once, so a consumer can supply a
  provider conditionally.

### The client pipeline

- The view is rebuilt whenever the `Items` reference changes.
- The change is detected by reference, so re-handing the same collection does
  not rebuild.
- The pipeline applies search, then filters, then sort, then paging, in that
  order.
- The reported total is the count after search and filters and before paging, so
  the footer reports what the filter matched rather than what the page shows.
- The pipeline is materialised once per rebuild, so each stage is not
  re-enumerated by the next.
- A `null` `Items` is treated as an empty sequence rather than throwing.

### The server pipeline

- A rebuild in server mode issues one `DataRequest` carrying the current skip,
  take, search text, sort descriptors and filter descriptors.
- The skip and take are derived from the current page and page size, and are
  zero and unbounded when paging is off.
- The returned items become the view and the returned total becomes the reported
  count, so the footer's paging is the server's arithmetic rather than the
  table's.
- A request in flight is cancelled when a newer one starts, so a fast typist
  does not render a stale page.
- A cancelled request neither replaces the view nor clears the loading state,
  because a newer request owns both.
- The loading state is raised before the request and cleared after it.

### Loading and emptiness

- `Loading` set renders a loading row instead of the data, whatever the view
  holds.
- A server request in flight renders the same loading row without `Loading`
  being set.
- An empty view renders `EmptyContent` when one is supplied.
- An empty view with no `EmptyContent` renders a default empty message rather
  than an empty table body.
- The loading and empty rows span the table's full width.

### Paging

- `PageSize` above zero renders the pagination footer.
- `PageSize` at zero renders no footer and no paging.
- `Virtualize` set renders no footer, because a virtualised table has one
  page.
- The current page is clamped to the last page that has rows, so shrinking the
  result set cannot leave the table on an empty page.
- Navigating raises `PageChanged` and rebuilds the view.
- Changing the page size raises `PageSizeChanged`, returns to the first page and
  rebuilds the view.
- A `Page` changed by the consumer is adopted, and one re-supplied unchanged is
  not, so the table's own navigation is not undone by the next render.
- `PageSize` follows the same rule.

### Virtualization

- `Virtualize` set renders only the rows in view.
- `Virtualize` set applies no paging, so the virtualised list is the whole
  filtered result.
- `Virtualize` and `GroupBy` are mutually exclusive; grouping is ignored and the
  conflict is reported once on the console.

## Recorded gaps

- **Misconfiguration is reported to the console, not to a logger.** All three
  conflict warnings — virtualize versus grouping, reorder versus its three
  exclusions, editing versus server mode — are written with `Console.WriteLine`.
  On Blazor Server that is the server's console, where no consumer looking at
  their browser will see it; there is no `ILogger` anywhere in the component.
- **The warnings are the only signal.** Nothing in the rendered UI indicates
  that a configured feature was silently dropped, so a table with `Reorderable`
  and `Virtualize` simply has no grip column and no explanation.
- **The client pipeline recomputes everything on every rebuild.** Search,
  filter and sort run over the whole collection each time any one of them
  changes, with no memoisation between stages. That is the right shape for the
  sizes the component is used at, and it is worth recording as the reason
  `Virtualize` exists.
- **Server mode cannot report its own errors.** A `DataProvider` that throws
  anything other than a cancellation propagates out of the render, because only
  `OperationCanceledException` is caught. There is no error slot and no failed
  state — the exception reaches the circuit's error boundary.
