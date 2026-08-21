# DrylPagination

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylPagination.razor

## User Story

As a Blazor developer, I want a page navigator I can bind to any paged list —
not only to a `DrylTable` — so that a card grid, a gallery or a search result
page gets the same navigation, the same keyboard behaviour and the same summary
line without me rebuilding it.

## Description

`DrylPagination` is a bar in three parts: a summary of which items are showing,
an optional page-size selector, and the page controls themselves — first,
previous, a numbered range, next, last.

It is **fully controlled**. It stores no page and no size of its own; it renders
what `CurrentPage` and `PageSize` say and raises a change when the user asks for
a different one. That is what lets the same component serve a table whose paging
happens in memory and a list whose paging is a server request.

Pages are **zero-indexed on the wire and 1-based on screen**. `CurrentPage` is
`0` for the first page; the labels and the accessible names say "1". The
component owns that conversion so a consumer's collection maths and the user's
mental model can each stay natural.

The numbered range is elided rather than complete: past a handful of pages it
shows the first, the last, a window around the current page, and ellipses for
what it left out — so a hundred pages fit in the same width as five. On a narrow
container the numbers drop out entirely and the arrow controls carry the
navigation, measured against the bar's own container rather than the viewport.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `CurrentPage` | `int` | `0` | Zero-indexed current page. |
| `CurrentPageChanged` | `EventCallback<int>` | — | Raised with the new zero-indexed page. Pairs for `@bind-CurrentPage`. |
| `PageSize` | `int` | `20` | Items per page. |
| `PageSizeChanged` | `EventCallback<int>` | — | Raised with the new size. Pairs for `@bind-PageSize`. |
| `TotalCount` | `int` | `0` | Total items across all pages. |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 20, 50, 100]` | Choices in the size selector. |
| `ShowPageSize` | `bool` | `true` | Renders the size selector. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the bar's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the bar. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a bar holding the summary, the optional size selector
  and the page controls.
- The bar is wrapped in one element that establishes the size containment the
  narrow layout is measured against.
- `Class` is merged onto the bar's own class rather than replacing it.
- `AdditionalAttributes` are applied to the bar.
- The bar wraps onto a second line rather than overflowing when its content does
  not fit.
- The page controls are pushed to the trailing edge of the bar.

### The summary

- `TotalCount` above zero renders a summary naming the first item, the last item
  and the total on the current page.
- The summary's item numbers are 1-based.
- The last number of the range never exceeds `TotalCount`, so the final page
  reports its real end rather than a full page's worth.
- `TotalCount` at zero renders an explicit empty message instead of a range.

### Paging

- The page count is `TotalCount` divided by `PageSize`, rounded up.
- The page count is at least one, so an empty list still renders one page rather
  than none.
- A `PageSize` of zero or less yields one page rather than a division by zero.
- Pressing a page control raises `CurrentPageChanged` with the target page.
- The target page is clamped into the valid range before it is raised, so "last
  page" on a short list cannot ask for a page that does not exist.
- A control that would raise the page already shown raises nothing, so a
  redundant press costs no round trip.
- The first and previous controls are disabled on the first page.
- The next and last controls are disabled on the last page.
- The component stores no page of its own: what it renders is `CurrentPage`
  until the consumer changes it.

### The numbered range

- Seven pages or fewer are all listed, with no ellipsis.
- More than seven pages always list the first page and the last page.
- A current page near the start lists the pages after it and one trailing
  ellipsis.
- A current page near the end lists the pages before it and one leading
  ellipsis.
- A current page in the middle lists it with one page either side, between two
  ellipses.
- The page shown as current is rendered in a different button variant from the
  others, so it is distinguishable without colour alone.
- The numbered range is hidden when the bar's container is narrow, leaving the
  summary and the arrow controls.
- The narrow layout is measured against the bar's container and not against the
  viewport, so a bar inside a narrow panel collapses on a wide screen.

### The page-size selector

- `ShowPageSize` left `true` renders a size selector listing `PageSizeOptions`.
- `ShowPageSize` set to `false` renders no selector.
- The option matching `PageSize` is the selected one.
- Picking a size raises `PageSizeChanged` with that size.
- A size that does not parse as a number raises nothing.
- A size of zero or less raises nothing, so the bar cannot ask its host for an
  impossible page size.
- The selector's `label` is associated with it by a per-instance identifier, so
  two bars on one page do not share a label.

### Keyboard and accessibility

- The page controls sit in a landmark labelled as pagination, so a screen-reader
  user can jump to them.
- Every arrow control carries an accessible label naming what it does rather
  than being announced as its icon.
- Every numbered control carries an accessible label naming its 1-based page
  number.
- The control for the page currently shown is marked as the current page for
  assistive technology.
- The ellipsis is hidden from assistive technology, so the range is announced as
  its pages.
- The ellipsis is not selectable, so selecting the bar's text does not pick it
  up.
- Every control is a real button, so the bar is fully operable by `Tab`, `Enter`
  and `Space` without a key handler of its own.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The summary and the selector's label are set in `--fg-muted`, quieter than the
  controls.
- The summary is set in `--font-mono`, so its numbers do not shift the bar as
  the page changes.
- The ellipsis is set in `--fg-dim` and in `--font-mono`.
- The bar paints no surface of its own — no fill, no border, no frost — so it
  inherits the ground it is placed on, whether that is a table's footer or a
  page (`DESIGN-06`).
- The component renders no accent of its own; the current page's emphasis comes
  from the button variant (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): paging is navigation, and navigation is
  the user's action rather than a model's. Where the *content* being paged is
  generated, the surface holding it carries the `Ai` state; an aura on the
  navigator would say a model was working on the page numbers.

## Recorded gaps

- **The size selector is a raw `select`, not a `DrylSelect`.** It carries the
  library's `select` class rather than the library's select component, so it
  does not get that component's keyboard behaviour, its panel, its frost or its
  animation — and it is the one control in the bar that does not look like the
  rest of DRYL. It also carries both a visible `label` and an `aria-label`; the
  latter wins, so the visible label is announced to nobody.
- **The summary trusts `CurrentPage`.** The range is computed from the parameter
  without clamping, so a consumer who passes a page beyond the end renders a
  summary like "Showing 261–247 of 247" while the controls behave correctly. The
  clamp exists in the navigation path and not in the display path.
- **The elision window is written in literals.** The seven-page threshold, the
  four pages listed near an edge and the one-page window around the current page
  are hand-picked numbers in `BuildPageList`, with nothing relating them to each
  other. Changing the window means changing three of them consistently.
- **The collapse breakpoint is a literal, and a private one.** The width at
  which the numbers disappear is written into the container query in `dryl.css`
  as a raw length, with no token behind it and no relation to the `Breakpoint`
  scale the rest of the library uses (`DESIGN-01`) — the same gap `F8` records.
- **Nothing is animated.** The page controls animate because `DrylButton` does;
  the bar itself has no transition, and the numbered range appearing or
  disappearing at the breakpoint snaps (`DESIGN-11`, `DESIGN-12`). Nothing moves
  when the current page changes, which is the one moment the component exists
  for.
- **The bar's own type sizes are literal**, as are the selector's paddings and
  minimum width and the gaps between the page buttons (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg-muted` and `--fg-dim` are
  the mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — **absent** for the bar itself, and recorded above
  as debt rather than as an exception; its buttons carry `DrylButton`'s motion.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decisions are the pagination landmark, the per-control labels and
  the current-page marking; the substantive omission is the selector's competing
  labels, recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoPagination.razor`, with the
  examples `Components/Examples/Pagination/Full.razor`, `.../Minimal.razor`,
  `.../ManyPages.razor`, `.../CustomSizes.razor` and `.../Empty.razor`.
- **`ComponentCatalog`** — registered as `"Pagination"` / `pagination` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylPaginationTests.cs` guards the
  summary, the empty message, the disabled edge controls, the next and last
  controls' raised values including the clamp, a numbered control's raised
  value, the size change and the hidden selector. It is the second-best-covered
  component in the category, and it exists because the two callbacks were
  renamed — the file pins the current names against the 1.0 freeze.
