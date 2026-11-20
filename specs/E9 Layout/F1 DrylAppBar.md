# DrylAppBar

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylAppBar.razor
              code/DRYL.Components/Components/Layout/AppBarElevation.cs

## User Story

As a Blazor developer, I want a sticky top bar that lays out brand, title and
actions in balanced regions and offers the drawer and sidebar toggles on its
own, so that my app shell's chrome needs no hand-wired state.

## Description

`DrylAppBar` renders the application's top bar as a `header.topbar`. It sticks
to the top of its scroll container and sits in the `topbar` grid area when it
is a direct child of a `DrylLayout`.

It has two layout modes. With plain `ChildContent` it is a single flex row.
As soon as any of `Start`, `Center` or `End` is set it switches to three
regions, where `Start` and `End` share the free space equally so `Center`
stays optically centred.

Inside a `DrylLayout` it reads the cascaded `DrylLayoutContext` (see
[`_Api.md`](_Api.md)) and can render two toggles before the content: a
hamburger that opens and closes the sibling `DrylDrawer` overlay (shown only
below the desktop width), and a collapse button that folds a collapsible
drawer to its icon rail (shown only at desktop width). Outside a layout no
toggle renders. `Elevation` lifts the bar off the content.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Bar content in single-row mode; ignored once a slot is set. |
| `Start` | `RenderFragment?` | `null` | Leading region (brand, nav); switches to slotted layout. |
| `Center` | `RenderFragment?` | `null` | Optically centred region (title, breadcrumbs); switches to slotted layout. |
| `End` | `RenderFragment?` | `null` | Trailing region (actions, avatar); switches to slotted layout. |
| `Elevation` | `AppBarElevation` | `AppBarElevation.Flat` | Resting elevation of the bar. |
| `ShowDrawerToggle` | `bool` | `true` | Renders the hamburger when a drawer is registered in the layout. |
| `ShowSidebarToggle` | `bool` | `false` | Renders the collapse button when a collapsible drawer is registered. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |
| `Context` | `DrylLayoutContext?` | cascaded | `[CascadingParameter]` from `DrylLayout`; `null` outside a layout. |

`AppBarElevation` members: `Flat`, `Raised` (see [`_Api.md`](_Api.md)).

## Acceptance Criteria

### Structure

- The root renders as a `header` with class `topbar`.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- Without `Start`, `Center` and `End`, `ChildContent` renders directly inside
  the root, after any toggle.
- With any of `Start`, `Center` or `End` set, `ChildContent` is not rendered.
- In slotted layout the root renders `.topbar-start` and `.topbar-end`.
- In slotted layout `.topbar-center` renders only when `Center` is set.
- In slotted layout the toggles render first inside `.topbar-start`, before
  `Start`.
- `.topbar-start` and `.topbar-end` share the free width equally, so
  `.topbar-center` stays centred between them.
- The bar's height is `--appbar-h`.
- The bar is sticky at the top of its scroll container, stacked on
  `--z-sticky`.
- As a direct child of `DrylLayout`'s `.app-shell`, the bar occupies the
  `topbar` grid area.

### Elevation

- `Elevation` defaults to `AppBarElevation.Flat`.
- `AppBarElevation.Flat` renders no `is-raised` class; the bar fills with
  `--bar-bg` and has a `--line` bottom border.
- The bar's `backdrop-filter` is `--glass-fx-float`, the floating glass tier.
- `AppBarElevation.Raised` adds `is-raised` to the root.
- A raised bar fills with `--glass-2` and casts `--shadow-md`.

### Drawer toggle

- With `ShowDrawerToggle` true and a `DrylDrawer` registered in the cascaded
  context, the bar renders one `button.drawer-toggle` of `type="button"`.
- Without a cascaded context, no drawer toggle renders.
- With no drawer registered in the context, no drawer toggle renders.
- With `ShowDrawerToggle` false, no drawer toggle renders.
- The drawer toggle carries `aria-label="Toggle navigation"`.
- The drawer toggle's `aria-expanded` is `"true"` while the registered drawer
  is open and `"false"` otherwise.
- The drawer toggle renders the `Menu` icon.
- Clicking the drawer toggle toggles the registered drawer's open state.
- The drawer toggle is visible only below the desktop breakpoint
  (`Breakpoint.Lg`, as a viewport width).
- When the registered drawer opens or closes by any other route, the bar
  re-renders and the toggle's `aria-expanded` follows.

### Sidebar toggle

- With `ShowSidebarToggle` true and a collapsible drawer registered in the
  cascaded context, the bar renders one `button.sidebar-toggle` of
  `type="button"`.
- `ShowSidebarToggle` defaults to `false`.
- Without a registered collapsible drawer, no sidebar toggle renders.
- While the sidebar is expanded the toggle carries
  `aria-label="Collapse sidebar"`, `aria-pressed="false"` and the
  `ChevronsLeft` icon.
- While the sidebar is collapsed the toggle carries
  `aria-label="Expand sidebar"`, `aria-pressed="true"` and the
  `ChevronsRight` icon.
- Clicking the sidebar toggle collapses or expands the registered drawer.
- The sidebar toggle is hidden below the desktop breakpoint
  (`Breakpoint.Lg`, as a viewport width).
- When the drawer is collapsed by any other route, the toggle's label, pressed
  state and icon follow.

### Toggle appearance and motion

- Both toggles rest with a transparent fill, a `--line` border, `--r-sm`
  corners and `--fg-muted` icon color.
- On hover both toggles fill with `--glass-2`, take a `--line-strong` border
  and a `--fg` icon color.
- Background, border and color transition on `--dur-fast` / `--ease-out`.
- Keyboard focus shows an `--accent-line` border and an `--accent-soft` glow
  ring (`:focus-visible`).

### Lifecycle

- The bar subscribes to the cascaded context's `StateChanged` when it receives
  the context.
- When the cascaded context changes, the bar unsubscribes from the old one and
  subscribes to the new one.
- Disposing the bar unsubscribes it from the context.

## Recorded debt

- **A fill of its own.** `.topbar` fills with `--bar-bg` rather than
  `--panel-float`, the fill `DESIGN-06` names for a floating surface.
- **Icon-only toggles without a tooltip.** Both toggles carry an `aria-label`
  but no `DrylTooltip` (`UX-05`).
- **Fixed English labels.** `Toggle navigation`, `Collapse sidebar` and
  `Expand sidebar` cannot be localised or overridden.
- **Elevation snaps.** `.topbar` has no transition, so switching `Elevation`
  at runtime changes fill and shadow instantly (`DESIGN-11`).
- **Literal geometry.** The bar's horizontal padding and gap, the toggles'
  square size and the focus ring's spread are literal lengths (`DESIGN-01`).
- **Viewport, not container.** The toggles' visibility follows a viewport
  media query at the desktop width, not a container query, so a bar inside a
  narrow frame on a wide screen still shows the desktop toggle.
- **No tests.** No bUnit test renders `DrylAppBar`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/app-shell` and in the website's own shell in dark and light (2026-11-20)
- **Enter/exit animation** — **no**: the bar is persistent application chrome
  that mounts once with the shell and never mounts conditionally; the toggles
  animate their state changes as above (`DESIGN-11`). The missing elevation
  transition is recorded above.
- **Keyboard and a11y** — the root is a `header` landmark; both toggles are
  native buttons, reachable by Tab and operated by Enter and Space; the
  hamburger reports `aria-expanded`, the collapse button `aria-pressed`. The
  missing tooltip is recorded above.
- **AI mode** — **no** `Ai` parameter: the bar is application chrome; AI work
  happens in the content and controls placed inside it, which carry their own
  state (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoAppShell.razor`, route
  `/components/app-shell`, examples `AppShell/AppBarSlots` (slots, `Raised`)
  and `AppShell/CollapsibleShell` (`ShowSidebarToggle` inside a layout). The
  website's own chrome is a `DrylAppBar` in
  `DRYL.Website/Components/Layout/TopBar.razor`.
- **`ComponentCatalog`** — `DRYL.Website/Components/ComponentCatalog.cs`, the
  shared `"App Shell"` / `app-shell` entry, group `Layout`, AI flag `false`
  (`REL-04`).
- **Tests** — none.
