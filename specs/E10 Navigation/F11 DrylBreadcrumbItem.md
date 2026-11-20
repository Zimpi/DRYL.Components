# DrylBreadcrumbItem

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylBreadcrumbItem.razor

## User Story

As a Blazor developer building a breadcrumb trail, I want to declare each level
as one child element of `DrylBreadcrumbs` — its target, icon and label — so
that the trail renders links, separators and the current page from my
declarations alone.

## Description

`DrylBreadcrumbItem` is a declaration, not a widget. It renders **no markup of
its own**: on initialisation it registers with the `DrylBreadcrumbs` it receives
as a cascading value, and the parent draws it as a link, as plain text, or — if
it is the last item — as the current page
([`F10 DrylBreadcrumbs.md`](F10%20DrylBreadcrumbs.md)). Placed anywhere else it
throws. On disposal it unregisters.

The parent reads the item's `Href`, `Icon` and `ChildContent` each time it
renders; the item does not tell its parent when they change.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Href` | `string?` | `null` | Navigation target. Without one, the crumb is plain text. Ignored on the last crumb, which is always the current page. |
| `Icon` | `string?` | `null` | `DrylIcon` name shown before the label. |
| `ChildContent` | `RenderFragment?` | `null` | The crumb's label. |

The parent reference is a private `[CascadingParameter]` and not part of the
public API.

## Acceptance Criteria

### Registration

- `DrylBreadcrumbItem` renders no element of its own.
- A `DrylBreadcrumbItem` outside a `DrylBreadcrumbs` throws
  `InvalidOperationException` on initialisation.
- On initialisation the item registers with its parent once.
- On disposal the item unregisters from its parent, and the trail no longer
  shows it.

### Rendering by the parent

- An item that is not last and has a non-empty `Href` is rendered as a link to
  that `Href`.
- An item that is not last and has `Href` `null` or empty is rendered as plain
  text without `aria-current`.
- The last item is rendered as plain text with `aria-current="page"`, whatever
  its `Href`.
- A non-empty `Icon` is rendered as a `DrylIcon` before the label.
- With `Icon` `null` or empty, no icon is rendered.
- `ChildContent` is rendered as the label.

## Recorded debt

- **Parameter changes are not pushed to the parent.** The item has no
  `OnParametersSet`, and the parent's `NotifyChanged` is never called, so a
  changed `Href`, `Icon` or `ChildContent` reaches the trail only when the parent
  re-renders for another reason.
- **Order follows registration, not markup.** An item mounted after the first
  render is appended to the end of the trail and becomes the current page,
  wherever it stands in the markup.
- **No per-item `aria-label` or `title`.** An icon-only crumb (an `Icon` with no
  `ChildContent`) produces a link with no accessible name (`UX-05`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — EYE-CHECK-PENDING on /components/breadcrumbs
- **Enter/exit animation** — `DrylBreadcrumbItem` renders nothing, so there is
  nothing of its own to animate; the trail's missing enter/exit is recorded in
  `F10 DrylBreadcrumbs`.
- **Keyboard and a11y** — rendered as a native link (reachable by `Tab`,
  activated by `Enter`) or as text; the last item carries `aria-current="page"`.
  The unnamed icon-only crumb is recorded above.
- **AI mode** — **no**, for the same reason as `DrylBreadcrumbs`: a crumb is a
  location in the app's hierarchy, not a surface an AI works on (`AI-05`).
- **Demo page** — `/components/breadcrumbs`; `Breadcrumbs/Icons` shows `Icon`,
  `Breadcrumbs/Basics` shows linked and current crumbs.
- **`ComponentCatalog`** — no row of its own, being usable only inside
  `DrylBreadcrumbs`; covered by the `"Breadcrumbs"` / `breadcrumbs` entry.
- **Tests** — none of its own; `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  renders only the parent.
