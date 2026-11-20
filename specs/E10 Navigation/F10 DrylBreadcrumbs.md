# DrylBreadcrumbs

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylBreadcrumbs.razor
              code/DRYL.Components/Components/Navigation/DrylBreadcrumbs.razor.css

## User Story

As a Blazor developer building a page deep in a hierarchy, I want a breadcrumb
trail I fill with `DrylBreadcrumbItem` declarations, so that the user sees where
they are, can jump back up with a link, and a long trail collapses instead of
wrapping across the header.

## Description

`DrylBreadcrumbs` renders a `nav` landmark around an ordered list. Its
`DrylBreadcrumbItem` children render no markup of their own (see
[`F11 DrylBreadcrumbItem.md`](F11%20DrylBreadcrumbItem.md)): each registers with
the parent it receives as a fixed cascading value, and the parent draws every
crumb, every separator and the collapse marker.

**The last crumb is the current page.** Whatever the last registered item
declares, it is rendered as plain text with `aria-current="page"` — never as a
link, even when it carries an `Href`. Every other item with an `Href` is a link;
one without is plain text in the same style as the current crumb, but without
`aria-current`.

**A long trail collapses in the middle.** With `MaxItems` set to 2 or more and
exceeded, the trail shows the first crumb, then an ellipsis, then the last
`MaxItems - 1` crumbs. The ellipsis is a marker, not a control: it is hidden
from assistive technology and cannot be expanded.

**Separators are decoration.** Between two visible entries the component
renders a separator list item — `ChevronRight` by default, or the consumer's
`Separator` fragment — hidden from assistive technology.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `AriaLabel` | `string` | `"Breadcrumb"` | Accessible name of the `nav` landmark. |
| `Separator` | `RenderFragment?` | `null` | Custom separator between crumbs; `null` renders a chevron. |
| `MaxItems` | `int?` | `null` | Maximum crumbs shown before the middle collapses into an ellipsis; `null` shows all. |
| `ChildContent` | `RenderFragment?` | `null` | The `DrylBreadcrumbItem` declarations. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captures unmatched attributes; splatted onto the `nav`. |
| `Class` | `string?` | `null` | Merged onto the `nav`'s `breadcrumbs-nav` class. |

## Acceptance Criteria

### Structure

- The component renders one `nav` carrying `breadcrumbs-nav`.
- `Class` is appended to the `nav`'s classes; `breadcrumbs-nav` stays
  (`tests/DRYL.Components.Tests/ClassMergeTests.cs`,
  `Breadcrumbs_merges_consumer_class_without_clobbering`).
- `AdditionalAttributes` are applied to the `nav`.
- `AriaLabel` is rendered as the `nav`'s `aria-label`.
- `AriaLabel` defaults to `"Breadcrumb"`.
- The `nav` holds one `ol` carrying `.breadcrumbs`.
- Each visible crumb is one `li` carrying `.breadcrumb-item`, in registration
  order.
- With no items, the list is empty.
- `ChildContent` is rendered inside a fixed `CascadingValue` of the component
  itself, so `DrylBreadcrumbItem` declarations register without consumer
  wiring.

### Crumbs

- The last registered item renders as a `span.breadcrumb-current` carrying
  `aria-current="page"`.
- The last item renders as text even when it has an `Href`.
- Any other item with a non-empty `Href` renders as an `a.breadcrumb-link`
  whose `href` is that `Href`.
- Any other item with `Href` `null` or empty renders as a
  `span.breadcrumb-current` without `aria-current`.
- An item with a non-empty `Icon` renders a `DrylIcon` of that name before its
  content, in links and in text crumbs alike.
- An item's `ChildContent` is rendered as the crumb's label.

### Separators

- One `li.breadcrumb-sep` is rendered between every two visible entries, and
  none before the first.
- Every separator carries `aria-hidden="true"`.
- With `Separator` `null`, each separator renders a `ChevronRight` `DrylIcon`.
- With `Separator` set, each separator renders that fragment instead.
- Separators take no pointer events.

### Collapse

- `MaxItems` defaults to `null`, and with `null` every item is shown.
- With `MaxItems` below 2, every item is shown.
- With no more items than `MaxItems`, every item is shown.
- With more items than `MaxItems` (≥ 2), the trail shows the first item, one
  ellipsis entry, then the last `MaxItems - 1` items.
- The ellipsis entry is an `li` carrying `.breadcrumb-item` and
  `.breadcrumb-ellipsis`, rendering a `MoreHorizontal` `DrylIcon`.
- The ellipsis entry carries `aria-hidden="true"`.
- Separators surround the ellipsis like any other entry.

### Appearance and motion

- A link reads `--fg-muted` with no underline and is rounded with `--r-xs`.
- Hovering a link turns its text `--fg` over a `--glass-2` fill.
- A link's colour and fill transition on `--dur-fast` / `--ease-out`.
- A text crumb reads `--fg`.
- A separator reads `--fg-faint`; the ellipsis reads `--fg-muted`.
- The list wraps onto further lines when it does not fit, with `--sp-1`
  between entries.
- A crumb's label does not wrap inside itself.
- A link's keyboard focus shows the library's global `:focus-visible` outline.
- The component branches on no colour mode and writes no mode-assuming value;
  both modes come from the token set (`DESIGN-02`).

### Lifecycle

- Registering an item re-renders the trail.
- Unregistering an item removes it from the trail.
- `Dispose` clears the registered items.

## Recorded debt

- **Collapsed crumbs are unreachable.** The ellipsis is `aria-hidden` and not
  interactive, so the crumbs it stands for cannot be reached by any user —
  pointer, keyboard or screen reader. The landmark announces fewer levels than
  the hierarchy has.
- **A link-less middle crumb looks current.** It uses the same
  `.breadcrumb-current` styling as the current page — `--fg`, heavier weight —
  and differs only in lacking `aria-current`.
- **No enter/exit animation (`DESIGN-11`, `DESIGN-12`).** Crumbs, separators and
  the ellipsis appear and disappear at once when items register or unregister,
  or when the trail collapses. Only the link hover is animated.
- **Order follows registration, not markup.** An item mounted after the first
  render is appended to the end of the trail — and so becomes the current page
  — wherever it stands in the markup.
- **Literal values in `DrylBreadcrumbs.razor.css` (`DESIGN-01`).** `.breadcrumbs`
  `font-size: 13px`; `.breadcrumb-link` and `.breadcrumb-current`
  `padding: 2px 6px`; `.breadcrumb-ellipsis` `padding: 2px 4px`;
  `.breadcrumb-current` `font-weight: 500`. The icon sizes `14` and `16` are
  literal `Size` arguments.
- **`NotifyChanged` is dead code.** `DrylBreadcrumbs.NotifyChanged` exists, but
  no item calls it (see `F11 DrylBreadcrumbItem`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — EYE-CHECK-PENDING on /components/breadcrumbs
- **Enter/exit animation** — **no**, recorded as debt above; the only motion is
  the link's hover transition.
- **Keyboard and a11y** — `nav` landmark with an accessible name, an ordered
  list, `aria-current="page"` on the last crumb, native links reachable by
  `Tab` and activated by `Enter`, separators and ellipsis `aria-hidden`. The
  unreachable collapsed crumbs are recorded above.
- **AI mode** — **no**, deliberately: the component has no `Ai` parameter. A
  breadcrumb trail reports where the user stands in the app's hierarchy; it is
  not a surface an AI works on, and its crumbs are links rather than content an
  AI produces (`AI-05`). The catalog row agrees (AI flag `false`).
- **Demo page** — `/components/breadcrumbs`, examples `Breadcrumbs/Basics`,
  `Breadcrumbs/Icons`, `Breadcrumbs/Collapsed` (`MaxItems="3"`) and
  `Breadcrumbs/CustomSeparator` under
  `DRYL.Website/Components/Examples/Breadcrumbs/`.
- **`ComponentCatalog`** — `"Breadcrumbs"` / `breadcrumbs` in
  `DRYL.Website/Components/ComponentCatalog.cs`, group `Navigation`,
  `ClassName` `"DrylBreadcrumbs"`, folder `Navigation`, AI flag `false`
  (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ClassMergeTests.cs` covers only the
  class merge; crumbs, separators and collapse have no test.
