# DrylDrawer

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylDrawer.razor
              code/DRYL.Components/Components/Layout/SidebarMode.cs

## User Story

As a Blazor developer, I want one sidebar component that can be a fixed column,
a collapsible icon rail or an overlay, with a pinned header and footer around a
scrolling navigation, so that my app shell's navigation behaves well at every
width and shows the user where they are.

## Description

`DrylDrawer` renders the application sidebar as an `aside.sidebar`. Inside a
`DrylLayout` it occupies the shell's drawer column and coordinates its open and
collapsed state with the layout and a `DrylAppBar` toggle; standalone it is a
plain sidebar. `Mode` selects how it occupies space. Content is either plain
`ChildContent` or the `Header` / `Content` / `Footer` slots, where only
`Content` scrolls.

The drawer also owns the motion of the navigation inside it
([`I29`](../../ideas/I29%20A%20sidebar%20with%20gliding%20glass.md)): one
shared indicator glides to the active row, and a soft light follows the mouse
across hovered rows. The rows themselves are `DrylNavLink` and `DrylNavGroup`
(see [`F6 DrylNavGroup`](../E10%20Navigation/F6%20DrylNavGroup.md),
[`F7 DrylNavLink`](../E10%20Navigation/F7%20DrylNavLink.md)).

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Mode` | `SidebarMode` | `SidebarMode.Auto` | How the sidebar occupies space. |
| `Open` | `bool` | `false` | Overlay open state (`Auto` on mobile, `Flyout` at every width); supports `@bind-Open`. |
| `OpenChanged` | `EventCallback<bool>` | Unset | Reports overlay open-state changes. |
| `Collapsed` | `bool` | `false` | Icon-rail state for `Collapsible` and `Pinnable`; supports `@bind-Collapsed`. |
| `CollapsedChanged` | `EventCallback<bool>` | Unset | Reports collapsed-state changes. |
| `Width` | `string?` | `null` | Expanded width as a CSS length; written to `--sidebar-w`. |
| `CollapsedWidth` | `string?` | `null` | Rail width as a CSS length; written to `--sidebar-collapsed-w`. |
| `PersistStateKey` | `string?` | `null` | localStorage key under which `Pinnable` persists its collapsed state. |
| `Header` | `RenderFragment?` | `null` | Pinned region above the scrolling content. |
| `Content` | `RenderFragment?` | `null` | Scrolling region; falls back to `ChildContent`. |
| `Footer` | `RenderFragment?` | `null` | Pinned region below the scrolling content. |
| `ChildContent` | `RenderFragment?` | `null` | Body when no slot is used. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

Public methods: `ToggleCollapseAsync()` toggles `Collapsed` (persisting it under
`Pinnable`); `OnEscape()` is `[JSInvokable]` for the flyout's Escape handling.
`DrylDrawer` implements `IAsyncDisposable`. `DrylLayoutContext` is cascaded in
from `DrylLayout`.

`SidebarMode` members: `Auto`, `Static`, `Collapsible`, `Pinnable`, `Flyout`.

## Acceptance Criteria

### Structure

- The root renders as an `aside` with class `sidebar`.
- The root carries `tabindex="-1"`.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- `Width` is written to the root's `--sidebar-w` custom property when set.
- `CollapsedWidth` is written to the root's `--sidebar-collapsed-w` custom
  property when set.
- With any of `Header`, `Content` or `Footer` set, the drawer renders
  `.sidebar-header`, `.sidebar-content` and `.sidebar-footer` for the slots that
  are set; `.sidebar-content` is always rendered in this layout.
- `.sidebar-content` renders `Content`, or `ChildContent` when `Content` is null.
- Without any slot, `ChildContent` renders directly inside the root.
- Only `.sidebar-content` scrolls; header and footer stay in place.

### Modes

- `Mode` defaults to `SidebarMode.Auto`.
- `SidebarMode.Static` adds `sidebar--static` and renders no backdrop.
- `SidebarMode.Flyout` adds `sidebar--flyout`.
- Every mode except `SidebarMode.Static` renders a `.sidebar-backdrop` before the
  root.
- While `Open` is true in a mode with a backdrop, root and backdrop carry
  `is-open`.
- Clicking the backdrop sets `Open` to false and fires `OpenChanged` with `false`.
- A location change while `Open` is true closes the drawer.
- Under `SidebarMode.Collapsible` and `SidebarMode.Pinnable`, `Collapsed` adds
  `is-collapsed` to the root.
- `Collapsed` has no effect on the class list in the other modes.
- `ToggleCollapseAsync()` inverts `Collapsed` and fires `CollapsedChanged` with
  the new value.
- Inside a `DrylLayout`, a collapsible drawer registers its toggle so the
  layout's sidebar toggle drives it.
- Under `SidebarMode.Pinnable` with a `PersistStateKey`, a collapse toggled by
  the user is written to localStorage under that key.
- Under `SidebarMode.Pinnable` with a `PersistStateKey`, the stored state is
  restored after the first interactive render.
- While a `SidebarMode.Flyout` drawer is open, focus is trapped inside it.
- While a `SidebarMode.Flyout` drawer is open, Escape closes it.

### Gliding indicator — I29

- The drawer renders exactly one `.nav-ink` element, as the first child of
  `.sidebar-content`, or of the root when no slot is used.
- `.nav-ink` carries `aria-hidden="true"`.
- After the first interactive render the drawer attaches
  `dryl.motion.navGlide` to its root.
- Once attached, the indicator lies under the active `DrylNavLink` or
  `DrylNavGroup` link row in the scrolling region and matches its box.
- With several active rows, the indicator lies under the one with the longest
  `href`.
- The covered row carries `data-nav-ink-target` and draws no fill, border or
  accent edge of its own.
- Active rows not covered by the indicator keep the static active treatment.
- On its first placement the indicator appears in place without travel.
- When the active row changes, the indicator travels to the new row with
  `--dur-slow` and `--ease-spring`.
- While a group above the active row opens or closes, the indicator follows the
  row frame by frame.
- While the drawer's width changes (collapsing to the rail and back), the
  indicator follows the row frame by frame.
- On the collapsed rail the indicator matches the active row's icon square.
- When the active row's group is closed, the indicator fades out with
  `--dur-med` and `--ease-out`.
- When that group opens again, the indicator fades back in under the row.
- An active row that sits inside a scroller of its own within the region keeps
  the static active treatment and the indicator stays hidden.
- The indicator's state is carried by `data-nav-ink` on its parent (`place`,
  `glide`, `track`); the drawer's rendered `class` attribute is not modified by
  script.
- The indicator uses the same gradient fill, `--accent-line` hairline and
  `--accent-grad` edge as the static active row.
- Under `prefers-reduced-motion: reduce` the indicator moves to the new row
  without travel.
- Disposing the drawer calls `dryl.motion.disposeNavGlide` when it was attached.
- During prerender and static rendering no indicator is shown and active rows
  keep the static treatment.

### Hover light — I29

- Moving a mouse pointer over a nav row inside the drawer lights the row with
  a soft accent glow centred on the pointer.
- The glow follows the pointer by `transform` only.
- The glow fades in and out with `--dur-med` and `--ease-out`.
- The glow stays inside the row's rounded box.
- The glow sits under the row's label and icon.
- Touch and pen input never light a row.
- Under `prefers-reduced-motion: reduce` no row is lit.

### Scrolling region

- `.sidebar-content` fades out over `--sp-3` at its top and bottom edges.
- `.sidebar-content` is padded by `--sp-3` at top and bottom, so rows at rest
  are never dimmed by the fade.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** every color comes from tokens (`--accent-a`,
  `--accent-b`, `--accent-line`, `--accent-grad`, `--accent-ico`) with no mode
  selector. Verified in the browser on 2026-10-30 on the docs website in dark
  and light: indicator, inside accent edge, active icon, hover light and edge
  fade.
- **Enter/exit:** the overlay slides in and out with `--dur-med`; the rail
  collapses with a width transition; the indicator glides, fades and tracks as
  above. Verified in the browser on 2026-10-30: spring travel between rows,
  frame-by-frame tracking while a group closes, fade in a closed group, rail
  square, no travel under reduced motion.
- **Keyboard and a11y:** the root is focusable programmatically
  (`tabindex="-1"`); the flyout traps focus and closes on Escape; the indicator
  and the hover light are decorative and hidden from assistive technology. Rows
  remain native links and buttons (see `F6`, `F7`).
- **AI decision:** no `Ai` parameter. The drawer is application chrome; AI work
  belongs to the content it navigates to (`AI-05`).
- **Demo:** `DRYL.Website/Components/Pages/DemoAppShell.razor`, route
  `/components/app-shell`, with `AppShell/AppBarSlots` and
  `AppShell/CollapsibleShell`. The website's own layout is a `Pinnable` drawer
  with all three slots.
- **Catalog:** `DRYL.Website/Components/ComponentCatalog.cs` — `App Shell`,
  slug `app-shell`, group `Layout`, category `Layout`, AI flag `false`
  (`REL-04`).

## Recorded debt and scope limits

- **Literal geometry (`DESIGN-01`):** the accent edge's width, height and glow
  radius, the active row's inner glow and the indicator's copy of them are
  literal lengths, shared with the static active row.
- **Prefix matching:** a parent route left active by `NavLinkMatch.Prefix` keeps
  its static pill beside the indicator; the consumer resolves it with
  `NavLinkMatch.All` on the parent link.
- **Verification gaps:** the glide and hover light are verified in the browser,
  not by an automated browser test; bUnit covers the rendered indicator and the
  interop calls (`tests/DRYL.Components.Tests/DrylDrawerTests.cs`).
