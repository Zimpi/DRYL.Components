# DrylNavLink

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylNavLink.razor

## User Story

As a Blazor developer, I want a sidebar link row with an icon that marks itself
when its route is current, so that users always see where they are.

## Description

`DrylNavLink` renders one navigation row: Blazor's `NavLink` with the
`.nav-item` visual and an optional leading icon, or a plain external link.
Inside a `DrylDrawer` its active state is drawn by the drawer's gliding
indicator and a hovered row is lit by the drawer's hover light (see
[`F18 DrylDrawer`](../E9%20Layout/F18%20DrylDrawer.md)); elsewhere it keeps a
static active treatment.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Href` | `string` | `""` | Navigation target; `[EditorRequired]`. |
| `Icon` | `string?` | `null` | `DrylIcon` name before the label. |
| `Match` | `NavLinkMatch` | `NavLinkMatch.Prefix` | How the URL is matched for the active state. |
| `External` | `bool` | `false` | Renders a plain `a` that opens in a new tab. |
| `Sub` | `bool` | `false` | Indented, lower child row inside a collapsible group. |
| `ChildContent` | `RenderFragment?` | `null` | Label. |
| `Class` | `string?` | `null` | Classes appended to the link. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the link. |

## Acceptance Criteria

### Structure

- The link carries class `nav-item`.
- `Sub` adds class `nav-item--sub`.
- `Class` is appended to the link's class list.
- Unmatched attributes are applied to the link.
- The label renders inside a `span`.
- A nonempty `Icon` renders a `DrylIcon` with class `ico` before the label.
- Without `Icon`, no icon renders.
- Without `External`, the link is a `NavLink` with `href` set to `Href`.
- Without `External`, the link applies `Match`.
- `Match` defaults to `NavLinkMatch.Prefix`.
- Without `External`, the link carries `active` while its route is current.
- Without `External`, the link carries `aria-current="page"` while its route is
  current.
- With `External`, the link is a plain `a` with `target="_blank"`.
- With `External`, the link carries `rel="noopener"`.
- With `External`, the link never carries `active`.

### Appearance

- A row uses `--fg-muted` text at rest.
- A hovered row uses `--glass-2` and `--fg`.
- An active row uses `--fg` text.
- An active row's icon uses `--accent-ico` at full opacity.
- The icon's color and opacity change with `--dur-med` and `--ease-out`.
- Outside a `DrylDrawer`'s indicator, an active row draws the accent gradient
  fill with an `--accent-line` hairline.
- Outside a `DrylDrawer`'s indicator, an active row draws an `--accent-grad`
  edge inside its left side.
- Inside a `DrylDrawer` the drawer's gliding indicator replaces the row's own
  active fill and edge (`F18`).
- Inside a `DrylDrawer` a mouse pointer over the row lights it with the
  drawer's hover light (`F18`).
- A `Sub` row is shorter and indented further than a regular row.
- On a collapsed `DrylDrawer` rail only the icon shows, centred.
- Colors resolve from the same selectors in both color modes (`DESIGN-02`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** token-only colors, no mode selector. Verified in the
  browser on 2026-10-30 in dark and light on the docs website.
- **Enter/exit:** a row has no mount animation of its own; it appears with its
  group or drawer. Its state change is animated by the drawer's indicator, its
  hover by the light and a `--dur-fast` background transition, its icon by a
  `--dur-med` color transition (`DESIGN-11`).
- **Keyboard and a11y:** a native link, reachable by Tab and activated by Enter.
  The current row carries `aria-current="page"` (set by `NavLink`).
- **AI decision:** no `Ai` parameter. A link row points at content; AI work
  belongs to that content (`AI-05`).
- **Demo:** the docs website's own sidebar
  (`DRYL.Website/Components/Layout/NavMenu.razor`) and
  `/components/app-shell` (`AppShell/CollapsibleShell`).
- **Catalog:** covered by the `App Shell` entry (slug `app-shell`) in
  `DRYL.Website/Components/ComponentCatalog.cs` (`REL-04`).

## Recorded debt and scope limits

- **External rows:** an `External` row gives no hint that it opens a new tab
  beyond the browser's own (`UX-05`).
- **Literal geometry (`DESIGN-01`):** row height, paddings, font sizes and the
  accent edge's size are literal lengths.
