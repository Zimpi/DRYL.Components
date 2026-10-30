# DrylNavGroup

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylNavGroup.razor

## User Story

As a Blazor developer, I want to group sidebar links under a labelled section
that can optionally collapse, so that a long navigation stays scannable.

## Description

`DrylNavGroup` renders a section of `DrylNavLink` rows inside a `DrylDrawer`.
A static group shows a small title above its rows. A collapsible group turns
its header into a toggle and animates its rows open and closed; with an `Href`
the header is itself a link row with a separate chevron toggle. The active
indicator and hover light of the surrounding drawer cover the group's link
header like any other row (see [`F18 DrylDrawer`](../E9%20Layout/F18%20DrylDrawer.md)).

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Title` | `string?` | `null` | Section label; the toggle text of a collapsible group. |
| `ChildContent` | `RenderFragment?` | `null` | The group's rows. |
| `Collapsible` | `bool` | `false` | Makes the group collapsible. |
| `DefaultExpanded` | `bool` | `true` | Initial expansion of a collapsible group. |
| `Href` | `string?` | `null` | Route of a collapsible group's header link. |
| `Match` | `NavLinkMatch` | `NavLinkMatch.Prefix` | How the header link matches the current URL. |
| `Icon` | `string?` | `null` | `DrylIcon` name before a collapsible group's label. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

## Acceptance Criteria

### Static group

- The root renders with class `nav-section`.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- A static group with a nonempty `Title` renders it in `.nav-title`.
- A static group without `Title` renders no `.nav-title`.
- A static group renders `ChildContent` directly after its title.

### Collapsible group

- `Collapsible` defaults to `false`.
- `DefaultExpanded` defaults to `true`.
- Without `Href`, the header is a native `button` with class
  `nav-section-toggle` and `type="button"`.
- The toggle carries `aria-expanded` reflecting the expansion.
- With `Href`, the header renders a `NavLink` with class `nav-section-link` and a
  separate `button` with class `nav-section-chevron-btn`.
- The chevron button's `aria-label` reads `Collapse {Title}` while expanded and
  `Expand {Title}` while collapsed.
- The header link applies `Match`.
- The header link carries `active` when it matches the current URL.
- A nonempty `Icon` renders a `DrylIcon` before the label.
- Activating the toggle or chevron button inverts the expansion.
- The rows render inside `.nav-children`, which carries `is-open` while expanded.
- While collapsed, `.nav-children` carries `aria-hidden="true"`.
- Opening and closing animates the rows' height with `grid-template-rows` on
  `--dur-med` and `--ease-out`.
- The chevron turns a quarter while expanded, on `--dur-med` and `--ease-out`.

### Appearance

- Titles and toggle labels use `--font-mono` in `--fg-faint`.
- A hovered toggle label uses `--fg-dim`.
- An active header link uses the same active treatment as a `DrylNavLink` row,
  including the accent edge inside the row and the `--accent-ico` icon.
- Inside a `DrylDrawer` the drawer's gliding indicator and hover light apply to
  the header link (`F18`).
- Colors resolve from the same selectors in both color modes (`DESIGN-02`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** token-only colors, no mode selector. Verified in the
  browser on 2026-10-30 in dark and light on the docs website.
- **Enter/exit:** rows open and close with a height transition; the chevron
  turns; the drawer's indicator tracks and fades with the group. Verified in the
  browser on 2026-10-30.
- **Keyboard and a11y:** native buttons and links; `aria-expanded` on the
  toggle; a labelled chevron button.
- **AI decision:** no `Ai` parameter. A group is a structural label for
  navigation and never the place where AI work runs (`AI-05`).
- **Demo:** the docs website's own sidebar
  (`DRYL.Website/Components/Layout/NavMenu.razor`) and
  `/components/app-shell` (`AppShell/CollapsibleShell`).
- **Catalog:** covered by the `App Shell` entry (slug `app-shell`) in
  `DRYL.Website/Components/ComponentCatalog.cs` (`REL-04`).

## Recorded debt and scope limits

- **Hidden rows stay focusable:** a collapsed group sets `aria-hidden` but not
  `inert`, so its links can still be reached by Tab (`UX-01`).
- **Uncontrolled expansion:** expansion is internal state; there is no
  parameter or callback to bind it.
- **Literal geometry (`DESIGN-01`):** header paddings and the chevron button's
  size are literal lengths.
