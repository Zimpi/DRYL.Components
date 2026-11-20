# DrylLayout

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylLayout.razor

## User Story

As a Blazor developer, I want one root component that arranges my app bar,
sidebar and main content into an application shell and lets them talk to each
other, so that the hamburger, the collapse button and the reflowing body work
without state I wire by hand.

## Description

`DrylLayout` renders the shell grid `div.app-shell` and cascades one
`DrylLayoutContext` (see [`_Api.md`](_Api.md)) to everything inside it. A
`DrylDrawer`, a `DrylAppBar` and a `DrylMainContent` placed as direct children
take the grid areas `drawer`, `topbar` and `main`.

The shell fills the viewport and never scrolls itself: sidebar and top bar
stay in place and only the main area scrolls. At desktop width the drawer is a
column of `--sidebar-w`; when a collapsible drawer folds to its rail, the
layout marks itself collapsed and the column glides to
`--sidebar-collapsed-w`, in step with the drawer's own width. Below the
desktop width the shell is a single column and the drawer becomes an overlay.

The context is how the parts coordinate: the drawer registers itself and its
collapse toggle, the app bar reads them to render its toggles, and the layout
re-renders whenever the context reports a change.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The shell's parts — normally `DrylAppBar`, `DrylDrawer`, `DrylMainContent`. |
| `SidebarWidth` | `string?` | `null` | Expanded drawer column as a CSS length; written to `--sidebar-w`. |
| `SidebarCollapsedWidth` | `string?` | `null` | Collapsed drawer column as a CSS length; written to `--sidebar-collapsed-w`. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

Cascades: one `DrylLayoutContext`, fixed for the layout's lifetime.

## Acceptance Criteria

### Structure

- The root renders as a `div` with class `app-shell`.
- `ChildContent` renders inside the root.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- The layout cascades one `DrylLayoutContext` instance to its descendants.
- The cascaded context instance stays the same across re-renders.

### Custom widths

- `SidebarWidth` is written to the root's `--sidebar-w` custom property when
  set.
- `SidebarCollapsedWidth` is written to the root's `--sidebar-collapsed-w`
  custom property when set.
- With neither set, the root renders no `style` attribute and the tokens keep
  their global values.

### Grid

- At desktop width (`Breakpoint.Lg` and wider, as a viewport width) the shell
  has two columns: the drawer column of `--sidebar-w` and the content column.
- The shell has two rows: the top bar row sized to its content and the main
  row taking the rest.
- A direct `.sidebar` child occupies the `drawer` area, spanning both rows.
- A direct `.topbar` child occupies the `topbar` area.
- A direct `.main` child occupies the `main` area.
- The shell is exactly one viewport tall and does not scroll itself.
- Below desktop width the shell has a single column holding the top bar and
  the main area.
- Below desktop width a direct `.sidebar` child leaves the grid and slides in
  from the inline start as an overlay while it is open.
- The overlay slides on `--dur-med` / `--ease-out`.

### Sidebar collapse

- While the context reports the sidebar collapsed, the root carries
  `is-sidebar-collapsed`.
- While the sidebar is not collapsed, the root carries no
  `is-sidebar-collapsed`.
- At desktop width a collapsed shell's drawer column is
  `--sidebar-collapsed-w`.
- The drawer column's width change transitions on `--dur-med` /
  `--ease-in-out`.
- Below desktop width `is-sidebar-collapsed` has no effect on the grid.

### Coordination

- When a drawer inside the layout opens, closes, collapses or expands, the
  layout re-renders.
- When a collapsible drawer unregisters its toggle, the context reports the
  sidebar as not collapsed and the root loses `is-sidebar-collapsed`.
- Disposing the layout unsubscribes it from its context.

## Recorded debt

- **Viewport, not container.** The shell switches between one and two columns
  on a viewport media query at the desktop width, not a container query; a
  shell inside a framed preview follows the window, not its frame.
- **`100vh`.** The shell's height is the viewport height in `vh`, which on
  mobile browsers with a collapsing address bar is taller than the visible
  area.
- **Literal overlay geometry.** Below desktop width the overlay drawer's width
  and stacking order are literals in `dryl.css`, not `--sidebar-w` and a
  `--z-*` token (`DESIGN-01`).
- **A consumer `style` replaces the widths.** The splatted attributes come
  after the computed `style`, so a consumer `style` drops `SidebarWidth` and
  `SidebarCollapsedWidth`.
- **Width strings are unchecked.** `SidebarWidth` and `SidebarCollapsedWidth`
  are written into the style as given.
- **No tests.** No bUnit test renders `DrylLayout`; the coordination is
  exercised only through the demo.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/app-shell` and in the website's own shell in dark and light (2026-11-20)
- **Enter/exit animation** — the shell mounts once and never conditionally,
  so it has no enter or exit of its own; its layout movement is animated — the
  drawer column glides on collapse and the overlay slides in and out
  (`DESIGN-11`).
- **Keyboard and a11y** — the layout adds no role and no focus stop; the
  landmarks come from its parts (`header` from `DrylAppBar`, `aside` from
  `DrylDrawer`, `main` from `DrylMainContent`), in source order.
- **AI mode** — **no** `Ai` parameter: the shell is application chrome; AI
  work belongs to the content inside it (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoAppShell.razor`, route
  `/components/app-shell`, example `AppShell/CollapsibleShell`. The website's
  own shell is a `DrylLayout` in `DRYL.Website/Components/Layout/MainLayout.razor`.
- **`ComponentCatalog`** — the shared `"App Shell"` / `app-shell` entry, group
  `Layout`, AI flag `false` (`REL-04`).
- **Tests** — none.
