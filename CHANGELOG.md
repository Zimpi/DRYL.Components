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
<!-- New components, features or tokens go here -->

### Changed
<!-- Changes to existing components go here -->

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
