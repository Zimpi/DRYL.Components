# Presentation, accessibility and AI

## Meta
- **State:** Implemented

## Acceptance Criteria

### The surface

- `Bordered` left `true` renders the table on the library's card surface, and
  its fill, border and frost are that shared treatment rather than the
  component's own (`DESIGN-06`).
- `Bordered` set to `false` renders a bare table with no surface of its own.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- `Height` set caps the table's height and makes it scroll.
- `Virtualize` set makes it scroll whether or not a height was given.
- `StickyHeader` left `true` keeps the header visible while the body scrolls.
- `StickyHeader` set to `false` adds the modifier that releases it.
- Every optional surface — the bulk bar, the summary bar, the toolbar, the
  footer — is absent from the markup entirely when it is not configured, rather
  than rendered empty.

### Tokens

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The header is set in `--fg-dim` and the cells in `--fg-muted`, so the data is
  louder than its labels.
- The header is set in `--font-mono`, upper-cased and letter-spaced, so it reads
  as a label row rather than as a first row of data.
- Every horizontal rule in the table is drawn in `--line`.
- The accent appears as the aura's ring and glow, the active sort and filter
  indicators, and the selected row's tint — never as the fill of the table
  (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Keyboard and accessibility

- The table carries `AriaLabel` as its accessible label.
- A sortable header is a tab stop; a plain header is not.
- A reorderable header is a tab stop even when it is not sortable, so the
  keyboard column move is reachable.
- Every control the table renders — the checkboxes, the expand, filter, group,
  grip, edit, commit, cancel, chip-remove and bulk-clear controls — carries an
  accessible label naming what it does rather than being announced as its icon.
- Every disclosure the table renders reports its expanded state.
- Every icon the table renders is decorative and stays out of its control's
  accessible name.
- The loading row is announced as a status.
- The bulk bar and the filter chips are announced as labelled regions.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The aura is rendered only on a bordered table, because a borderless one has no
  box for the ring and the glow to trace.
- The component renders the shared aura vocabulary through the shared helper
  rather than a table-specific AI treatment (`AI-02`).
- The aura's ring and glow are pulled inside the table's clipped bounds, because
  the surface clips to its rounded corners and would otherwise eat both.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- A row that arrives while streaming rises and flashes in, at `--dur-slow` and
  `--dur-choreo` with `--ease-out`.
- Under `prefers-reduced-motion: reduce` that row animation does not run.

### State persistence

- `PersistStateKey` set restores the sort, the filters, the page, the page size,
  the hidden columns, the column widths and the column order on the first
  render.
- Restoring rebuilds the view and re-renders.
- A missing or empty stored value leaves the defaults in place.
- A stored value that is not valid JSON, or that cannot be read at all, leaves
  the defaults in place rather than throwing — restoring is best-effort.
- The state is written back after a render in which something persistable
  changed, and only after a restore has been attempted, so the defaults cannot
  overwrite the stored state before it is read.
- Writing is best-effort in the same way as reading.
- `PersistStateKey` left `null` reads and writes nothing.

### Cleanup

- The search debounce is cancelled and disposed with the component.
- An in-flight data-provider request is cancelled and its source disposed with
  the component.
- The aura lifecycle's timer is disposed with the component.
- The view-transition helper is disposed with the component.
- The column-resize helper is detached, and its object reference disposed, with
  the component.
- Detaching is skipped when nothing was attached, and survives a disconnected
  circuit without throwing.
- Every interop call the table makes after a render is best-effort: a missing
  element or a closed circuit is swallowed rather than surfaced.

## Recorded gaps

- **The table claims the grid role without implementing the grid pattern.**
  That role promises two-dimensional keyboard navigation — arrow keys between
  cells, `Home` and `End` within a row, one tab stop for the whole widget. The
  table offers none of it: the tab order runs through each interactive element
  in turn, and no arrow key moves between cells. A screen-reader user is told
  they are in a grid and then finds none of the behaviour a grid implies. The
  plain table role would be the honest claim for what the component does today.
- **The loading indicator is not a `DrylSpinner`.** The loading row renders a
  bare span carrying the spinner class, inside an element with a hand-written
  inline style attribute — the one place in the component where layout is
  written inline rather than in the stylesheet. The padding inside it is a
  token; the rest is not.
- **The full-width rows span a hard-coded 99 columns.** The loading, empty,
  group-header and detail rows all declare a column span far larger than any
  table will have, rather than the number of columns the table actually
  rendered.
- **The table's own type sizes and paddings are literal.** The body and header
  type sizes and the cell paddings are written into the `.tbl` rules in
  `dryl.css` with no token behind them (`DESIGN-01`).
- **Persisted state is not versioned.** The stored shape has grown twice — the
  widths and the order were appended as optional members — and a future
  incompatible change has no version field to detect. A stored state from an
  older shape deserialises with its new members null, which happens to work, but
  only by luck of the shape's history.
- **Persisted state is per key, not per user.** It goes to the browser's storage
  under `PersistStateKey` alone, so two accounts on one browser share one
  table's layout.
- **`Bordered` silently disables the aura.** A consumer who sets `Ai` on a
  borderless table gets no aura at all and no indication why. The reason is
  sound; the silence is not.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg`, `--fg-muted`, `--fg-dim`,
  `--line` and the accent tokens are the mode-dependent ones; the component
  defines no mode-specific rule.
- **Enter/exit animation** — the table has the category's most developed motion
  and its most uneven coverage. Row moves, re-sorts and streaming inserts morph
  through view transitions, and a streaming row rises and flashes in. Nothing
  else moves: the bulk bar, the filter surface, the column menu, a group
  collapsing, a detail row expanding, a column moving and an edit starting all
  happen between two frames. Each is recorded in the `S{n}` file that owns it.
- **Keyboard and a11y** — the criteria above, plus the per-feature keyboard
  contracts in `S3`, `S5`, `S6` and `S7`. The substantive decisions are the
  labelled controls and the `Alt`-plus-arrow paths for both row and column
  moves; the substantive omission is the unearned grid role, recorded above.
- **AI mode** — yes, in two ways: the table carries the shared aura, and
  `AiState.Streaming` additionally turns on the row-glide and the per-row enter
  animation without any opt-in, because a table is the surface an agent most
  often fills a row at a time.
- **Demo page** — `DRYL.Website/Components/Pages/DemoTable.razor`, with the
  examples under `DRYL.Website/Components/Examples/Table/`.
- **`ComponentCatalog`** — registered as `"Table"` / `tables` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylTableTests.cs` guards row
  rendering, the sort cycle's first two steps, the selection callback, the two
  paging callbacks, the externally set page, and six aspects of the
  view-transition naming — that names are absent by default, present under
  `AnimateReorder`, present under `AiState.Streaming`, absent under
  `AiState.None`, sanitised to a valid identifier and scoped per instance — plus
  that a drag reorder and a sort still work with the morph active. Fourteen
  tests over the largest component in the library: the morph is well covered and
  almost nothing else is.
