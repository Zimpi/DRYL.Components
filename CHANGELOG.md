# Changelog

All notable changes to DRYL are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

Version bump guide:
- **MAJOR** (1.x.x) — Breaking changes to the public API
- **MINOR** (x.1.x) — New components or features, backwards-compatible
- **PATCH** (x.x.1) — Bug fixes, docs, visual tweaks with no API change

---

## [Unreleased]

### Added
- `DrylSkeleton` — AI-native content placeholder; variants: Line / Text / Avatar / Card / Image / Custom; sizes: Small / Medium / Large; `Lines` and `Width` parameters; `AiState.Streaming` shifts shimmer to violet-cyan gradient to signal AI writing into placeholder blocks; `AiState.Generated` fades blocks out to reveal real content
- `DrylIcon` — new `Blocks` icon (Lucide `layout-template`) for navigation / skeleton-related UI
- `DrylMenu` + `DrylMenuItem` — Dropdown action menu anchored to any trigger; `MenuPlacement` (BottomStart / BottomEnd / TopStart / TopEnd), `Block` mode; `DrylMenuItem` supports icons, keyboard-shortcut hints, `Danger` variant, separators and section headers; fully keyboard-navigable (Arrow keys, Home/End, ESC, Tab)
- `dryl.css` — Menu primitives: `.menu-anchor`, `.menu-panel` (+ `--end`, `--top` placement modifiers), `.menu-item` (+ `--danger`), `.menu-item-shortcut`, `.menu-separator`, `.menu-header`
- `dryl.js` — `window.dryl.menu` — click-outside detection via capture-phase `pointerdown`, `focusPanel`, `navigate`, `focusTrigger`
- `DrylSpinner` — New loading indicator; variants: Ring / Dots / Pulse; sizes: Small / Medium / Large; AI-aware (`Ai` parameter drives shared AI vocabulary — spinning rate responds to Thinking/Streaming states)
- `DrylTable` — `GroupBy` parameter clusters rows under collapsible mono-styled group headers with a per-group count badge
- `DrylTable` — `DetailTemplate` slot adds a leading expand-chevron column; clicking it toggles a glass detail panel under each row
- `DrylTable` — `RowActions` slot appends a trailing per-row actions column whose clicks don't propagate to the row click handler
- `DrylTable` — `BulkActions` slot renders a floating glass action bar above the toolbar while any row is selected, with a count chip and clear-selection close
- `DrylTable` — `Virtualize` + `VirtualizeItemSize` parameters render only visible rows via `Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize`; suppresses pagination, ignores grouping with a console warning
- `DrylTable` — `Height` parameter constrains the scrollable body (required for `Virtualize`)
- `DrylTable` — `StickyHeader` parameter (default `true`) keeps the header anchored to the top while the body scrolls
- `DrylTable` — `AllowColumnVisibility` parameter exposes a Settings-icon toolbar menu that toggles individual columns on and off
- `DrylTable` — `PersistStateKey` parameter persists sort, filters, page, page-size and column visibility in `localStorage` across reloads
- `DrylColumn` — `Hidden` parameter sets a column's initial visibility for the visibility menu
- `dryl.css` — Phase 3/4 primitives: `.tbl-group-header`, `.tbl-group-toggle`, `.tbl-group-chevron`, `.tbl-group-count`, `.tbl-col-expand`, `.tbl-expand-btn`, `.tbl-row-detail`, `.tbl-row-detail-inner`, `.tbl-col-actions`, `.tbl-row-actions`, `.tbl-bulk-bar` (+ `-info` / `-count` / `-label` / `-actions` / `-close`), `.tbl-wrap--scroll`, `.tbl-no-sticky`, `.tbl-col-menu-wrap`, `.tbl-col-menu-trigger`, `.tbl-col-menu` (+ `-header` / `-title` / `-close` / `-body` / `-option`)
- `dryl.js` — `window.dryl.storage` helper (`get` / `set` / `remove`) wrapping `localStorage` with graceful failure

### Changed
- `DrylTable` — Now implements `IAsyncDisposable` and uses `IJSRuntime` for the new state-persistence path

### Deprecated
<!-- Features that still work but will be removed in a future MAJOR go here -->

### Removed
<!-- Removed features go here -->

### Fixed
<!-- Bug fixes go here -->

---

## [0.1.0] — 2026-05-27

First documented state of the library. All components are in early-development status.

### Added

#### Design System
- `dryl.css` — Complete token system: colors, spacing, radii, shadows, transitions, typography
- AI-mode primitives: `.ai-aura`, `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`, `.ai-indicator`
- `AiState` enum — Shared AI state (`None / Active / Thinking / Streaming / Generated`)
- `DESIGN_TOKENS.md` — Full token reference
- `COMPONENT_PATTERNS.md` — Component anatomy and folder conventions
- `CLAUDE.md` — Contribution rules for AI agents and human contributors

#### Actions
- `DrylButton` — Primary interaction surface; variants: Primary / Secondary / Ghost / Danger; sizes: Small / Medium / Large; states: Loading, Disabled; leading and trailing icon slots; AI-Mode

#### Surfaces
- `DrylCard` — Glass surface with optional cursor spotlight; AI-Mode with rotating gradient border
- `DrylDialog` — Service-driven glass dialog; focus trap; sizes: Small / Medium / Large / FullScreen; AI-Mode (Human in the Middle)
- `DrylDialogProvider` — Root provider; placed once in `App.razor`
- `DrylToast` — Service-driven toast stack; variants: Info / Success / Warning / Danger / Ai; 6 positions; auto-dismiss with progress bar; hover-pause; AI-Mode

#### Intelligence (AI)
- `DrylAiIndicator` — Pulsing status pill; label and pulse speed adapt to `AiState`

#### Data
- `DrylBadge` — Inline status label; variants: Neutral / Accent / Success / Warning / Danger; optional dot
- `DrylIcon` — Lucide-based icon set; used by Button, Badge and others
- `DrylTable<TItem>` — Declarative data grid; global search, sort (multi-sort via Shift-click), column filters (Text / Select), pagination, row selection, KPI summary bar; optional `DataProvider` for server-side loading; AI-Mode
- `DrylColumn<TItem>` — Declarative column for `DrylTable`; `Sortable`, `Searchable`, `Filterable`; custom `CellTemplate` / `HeaderTemplate`; alignment; width
- `DrylTableKpi` — KPI summary bar for `DrylTable`
- `DrylPagination` — Standalone page navigator; First / Prev / numbers (smart-ellipsis) / Next / Last; page-size selector; "Showing X–Y of Z"

#### Inputs
- `DrylInputText` — Form-bound text input; leading and trailing icon slots; AI-Mode
- `DrylTextarea` — Auto-resizable textarea; AI-Mode
- `DrylCheckbox` — Accessible checkbox with label
- `DrylSelect` — Styled select bound to `EditForm`
- `DrylToggle` — On/off toggle switch

#### Layout
- `DrylLayout` — Root shell; CSS grid with sidebar and topbar slots; cascades layout context
- `DrylAppBar` — Sticky top bar; optional responsive drawer-toggle hamburger
- `DrylDrawer` — Sidebar; always-visible column on desktop, overlay on mobile (`@bind-Open`)
- `DrylMainContent` — Main content slot inside `DrylLayout`; handles scroll and padding
- `DrylNavGroup` — Labelled group of nav links inside `DrylDrawer`
- `DrylNavLink` — Single nav row with icon and active highlighting; supports external links
- `DrylExpansion` — Collapsible glass panel; stacked panels share borders and detach on open; AI-Mode
- `DrylTab` / `DrylTabs` — Tab bar with glass panel content

#### Feedback
- `DrylAlert` — Feedback banner; variants: Info / Success / Warning / Danger / Ai; optional title; dismissible; AI-Mode
- `DrylTooltip` — CSS-only hover tooltip; 4 placements: Top / Bottom / Left / Right

#### Services & Extensions
- `IDrylDialogService` / `DrylDialogService` — Service-driven dialog control; `ShowAsync<T>`, `ShowConfirmAsync`, `ShowAlertAsync`
- `IDrylToastService` / `DrylToastService` — Service-driven toast control
- `AddDrylComponents()` — `IServiceCollection` extension method

#### Data Models
- `SortDescriptor`, `FilterDescriptor`, `DataRequest`, `DataResult<TItem>` — Models for `DrylTable` DataProvider
- `ColumnAlign`, `ColumnFilterType` — Enums for `DrylColumn`
- `DialogOptions`, `DialogParameters`, `DialogResult`, `DialogSize` — Models for `DrylDialog`
- `ToastOptions`, `ToastParameters`, `ToastVariant`, `ToastPosition` — Models for `DrylToast`
- `InputState` — Shared state enum for input components

---

[Unreleased]: https://github.com/Zimpi/DRYL.Components/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Zimpi/DRYL.Components/releases/tag/v0.1.0
