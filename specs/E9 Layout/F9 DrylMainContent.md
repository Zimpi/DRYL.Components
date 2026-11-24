# DrylMainContent

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylMainContent.razor

## User Story

As a Blazor developer building an app shell, I want a page-body region that is
the shell's only scroll container and caps the reading width, so that the
sidebar and top bar stay put while the page scrolls with its scrollbar at the
viewport edge.

## Description

`DrylMainContent` renders the `main` landmark of a `DrylLayout` app shell. The
outer `main.main` spans the shell's `main` grid area at full width and is the
one region that scrolls; the inner `.main-inner` column centres the content at
a capped reading width and carries the page padding. Splitting the two keeps
the scrollbar flush with the viewport edge rather than beside the padded
content. Below the desktop breakpoint the inner padding tightens.

It is typically placed with `@Body` inside a consumer's layout, next to
`DrylDrawer` and `DrylAppBar`.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Page content, usually `@Body`. |
| `Class` | `string?` | `null` | Classes appended to the `main` root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the `main` root. |

## Acceptance Criteria

### Structure

- The root renders as a `main` element carrying `.main`.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- The root holds exactly one `div.main-inner`.
- `ChildContent` renders inside `.main-inner`.

### Layout

- Inside a `DrylLayout` (`.app-shell`), the root occupies the shell's `main`
  grid area.
- The root takes the full width of its track.
- The root scrolls vertically when its content is taller than it.
- The root can shrink below its content's height, so the scroll happens inside
  it rather than on the page.
- `.main-inner` centres itself horizontally inside the root.
- `.main-inner` caps the content width; the root itself carries no max width.
- `.main-inner`, not the root, carries the page padding.
- Below the desktop breakpoint, `.main-inner` uses a smaller padding.

### Keyboard and accessibility

- The page body is exposed as the `main` landmark.
- The component adds no focus stop of its own.

## Recorded debt

- **Literal geometry (`DESIGN-01`).** The reading-width cap and the desktop
  padding set of `.main-inner` are literal lengths, not `--sp-*` tokens; the
  smaller set below the desktop breakpoint reads `--sp-5`, `--sp-4` and
  `--sp-8`.
- **Viewport, not container.** The padding step-down is a viewport media query,
  not a container query, unlike the responsive foundation's container-first
  approach.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected as the website's own content area (`MainLayout.razor`) on every page visited, in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by written exception (`DESIGN-11`): the
  region is the always-mounted frame of the page and has nothing of its own to
  animate; page-to-page motion belongs to the content (`DrylRouteTransition`,
  `DrylReveal`, `DrylPresence`).
- **Keyboard and a11y** — `main` landmark; no focus stop.
- **AI mode** — **no** (`AI-05`): a structural landmark hosts no model-written
  content of its own.
- **Demo page** — no page of its own; shown on `/components/app-shell`
  (`DRYL.Website/Components/Pages/DemoAppShell.razor`), example
  `AppShell/CollapsibleShell`, and used by the website's own
  `DRYL.Website/Components/Layout/MainLayout.razor`.
- **`ComponentCatalog`** — no entry of its own; covered by the `"App Shell"` /
  `app-shell` entry (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylMainContentTests.cs` pins the
  stylesheet order: the smaller padding below the desktop breakpoint follows the
  base `.main-inner` rule, so it is not overridden by it.
