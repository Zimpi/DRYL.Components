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
- `DrylChipInput` — Free-text tag field; chips created on Enter / comma; Backspace removes last chip; `@bind-Tags` (`IReadOnlyList<string>`); `MaxTags`; AI-aware
- `DrylRating` — Star rating input inheriting `InputBase<int?>`; configurable `MaxStars`; hover preview; `AllowClear`; `ReadOnly`; keyboard navigation (arrows, Home, End); EditForm / DataAnnotations validation; AI-aware
- `DrylInputOtp` — Fixed-box OTP/2FA code entry inheriting `InputBase<string>`; configurable `Digits` (default 6); auto-focus advance; paste-to-fill via `dryl.otp` JS helper; AI-aware
- `DrylTimePicker` — Time-only picker inheriting `InputBase<TimeOnly?>`; scrollable hour/minute panel; `Min`/`Max`; `MinuteStep` (1, 5, 10, 15, 30…); Escape/Enter keyboard support; AI-aware
- `DrylInputMask` — Masked input inheriting `InputBase<string>`; predefined `MaskType` (Phone / Iban / PostalCode / CreditCard) or `CustomPattern` (`#` = digit, `A` = letter); formatting enforced via `dryl.inputmask` JS helper (input + paste); `LeadingIcon` slot; AI-aware
- `MaskType` enum — `Phone` / `Iban` / `PostalCode` / `CreditCard` / `Custom` for `DrylInputMask`
- `dryl.js` — Three new namespaces: `dryl.otp` (focusNext, focusPrev, attach/paste), `dryl.timepicker` (click-outside attach/detach, scrollToActive), `dryl.inputmask` (format-on-input attach/detach, paste)
- `DrylIcon` — Sechs neue Icons: `Circle` (lucide: circle), `Command` (lucide: command), `Hash` (lucide: hash), `List` (lucide: list), `Sliders` (lucide: sliders-horizontal), `Upload` (lucide: upload); werden in der Demo-Navigationsleiste verwendet

### Changed
- `DrylSelect` — Replaced native `<select>` element with a fully custom dropdown; API changed from `ChildContent` (`<option>` elements) to `Items` (`IEnumerable<SelectItem>`); panel and option styling now matches `DrylAutocomplete` (glass background, accent scrollbar, selected-item dot); `Placeholder` parameter added; click-outside detection via `dryl.menu.attach`; keyboard navigation (ArrowDown/Up, Enter, Space, Escape, Tab)
- `DrylNavGroup` — New `Collapsible` parameter (bool, default `false`) enables accordion-style sub-menus with CSS grid animate-in/out; `DefaultExpanded` (bool, default `true`) sets initial state; `Href` parameter makes the header a `NavLink` while a separate chevron button controls collapse; `Icon` parameter adds a leading icon to the collapsible header
- `DrylNavLink` — New `Sub` parameter (bool, default `false`) renders the item indented (`.nav-item--sub`) for use inside collapsible `DrylNavGroup` children
- `dryl.css` — New primitives for collapsible nav: `.nav-scroll` (scrollable sidebar middle area), `.nav-section-toggle`, `.nav-section-header`, `.nav-section-link`, `.nav-section-chevron-btn`, `.nav-section-chevron`, `.nav-children`, `.nav-children-inner`, `.nav-item--sub`

### Fixed
- `DrylTimePicker` — Time panel rendered outside the `.ai-aura` (`isolation:isolate`) wrapper so `backdrop-filter` blurs the page correctly instead of the parent's AI glow effects
- `DrylInputOtp` — AI aura now wraps each digit box individually (rotating gradient ring per box, box border hidden in AI mode) instead of spanning the entire group
- `DrylRating` — AI mode wrapper gets `background: var(--glass-1)` so the gradient ring frames a proper glass surface instead of floating around bare stars
- All `InputBase<T>`-derived components (`DrylInputText`, `DrylInputPassword`, `DrylTextarea`, `DrylInputNumber`, `DrylRating`, `DrylTimePicker`, `DrylInputOtp`, `DrylInputMask`, `DrylSlider`, `DrylToggle`, `DrylCheckbox`, `DrylRadioGroup`, `DrylSelect`, `DrylAutocomplete`) — overrode `SetParametersAsync` to supply a fallback `ValueExpression` when the component is used with one-way `Value="..."` or no value outside an `EditForm`; previously threw `InvalidOperationException: requires a value for the 'ValueExpression' parameter`
- `DrylIcon` — Added missing `ChevronUp` icon (lucide: chevron-up); was silently rendering an empty SVG when used in `DrylInputNumber`'s stepper
- `DrylInputNumber` — Stepper buttons are now flush with the input: wrapper uses `align-items: stretch` via `.has-stepper`, input squares off its right edge (`border-radius: var(--r-md) 0 0 var(--r-md); border-right: none`), stepper closes the shape with right-side radius; separator border syncs to input hover/focus state; buttons gain `:active` (glass-3 + accent-a) and `:focus-visible` ring; removed erroneous `has-trailing-icon` padding from the stepper mode
- `DrylDatePicker` — Empty calendar cells (leading/trailing padding days) no longer show a hover highlight; hover selector now excludes `.date-cell--empty`
- `DrylDrawer` — Sidebar navigation area is now scrollable when content overflows the viewport height; brand and Project footer remain pinned outside the scroll region

### Added
- `DrylInputPassword` — Password input with show/hide eye toggle; inherits `InputBase<string>`; EditForm / DataAnnotations validation; AI-aware
- `DrylInputNumber<TValue>` — Generic numeric input for `int`, `long`, `float`, `double`, `decimal` and nullable variants; optional `Min` / `Max` / `Step`; optional ± stepper buttons (`ShowStepper`); `inputmode="decimal"` for mobile keyboards; AI-aware; native spinners hidden in favour of custom stepper
- `DrylRadioGroup<TValue>` — Radio button group inheriting `InputBase<TValue>`; `Orientation` (`Vertical` / `Horizontal`); cascades `RadioGroupContext<TValue>` to children; EditForm validation; AI-aware (ring wraps the group)
- `DrylRadio<TValue>` — Single radio option inside `DrylRadioGroup`; receives group context via `[CascadingParameter]`; individual `Disabled` override; accessible `<label>` + visually-hidden `<input type="radio">` pattern
- `DrylMultiSelect` — Multi-selection dropdown; chip display for selected items with `MaxVisibleChips` overflow count; `@bind-SelectedValues` (`IReadOnlyList<string>`); same JS click-outside / keyboard pattern as `DrylSelect`; panel stays open on selection; AI-aware
- `DrylSlider` — Range slider inheriting `InputBase<double>`; `Min` / `Max` / `Step`; accent gradient fill tracks thumb via CSS custom property `--pct` (no JS); `ShowValue` label; AI-aware
- `DrylFileUpload` — Drag-and-drop / click-to-browse file picker built on Blazor `InputFile`; `Multiple` / `Accept` / `MaxFileSizeBytes`; drag-active glow via `dryl.fileupload.attach` JS helper; removable file list; `FilesChanged` event callback; AI-aware
- `RadioGroupOrientation` enum — `Vertical` / `Horizontal` for `DrylRadioGroup`
- `RadioGroupContext<TValue>` — Internal cascading context record used by `DrylRadioGroup` / `DrylRadio`
- `dryl.js` — `window.dryl.fileupload`: `attach` / `detach` for drag-enter/leave/over/drop event management with counter-based tracking to avoid false "drag leave" on child elements
- `dryl.css` — New primitives: `.radio-group` / `.radio-group--vertical` / `.radio-group--horizontal` / `.radio` / `.radio-input` / `.radio-control` / `.radio-label` / `.radio--disabled`; `.chip` / `.chip-text` / `.chip-remove` / `.chip-overflow` / `.multiselect-chips`; `.num-stepper` / `.num-step-btn`; `.file-drop` / `.file-drop--active` / `.file-drop--disabled` / `.file-drop-icon` / `.file-drop-title` / `.file-drop-sub` / `.file-list` / `.file-item` / `.file-item-icon` / `.file-item-name` / `.file-item-size` / `.file-item-remove`; `.slider-wrap` / `.slider-header` / `.slider-value`; native number spinner suppression (`input[type=number]::-webkit-inner-spin-button`)
- `DrylCommandPalette` — Full-screen command launcher overlay; accepts static `Items` or async `SearchProvider` (250 ms debounce); Ctrl+K / Cmd+K global hotkey; category grouping with `CommandItem.Category` (named categories alpha-sorted, ungrouped last); keyboard navigation (Arrow Up/Down, Enter, Escape); three item types: `Navigate` (router), `Action` (callback, closes palette), `AiIntent` (callback, keeps palette open); AI result panel via `Ai` parameter and `AiContent` slot; `@bind-Open` two-way binding; ARIA combobox + listbox pattern with `aria-activedescendant`, `aria-live` AI panel
- `CommandItem` / `CommandItemType` — Model classes for command palette entries (`Label`, `Description`, `Icon`, `Category`, `Type`, `Href`, `Action`, `AiAction`)
- `dryl.js` — `window.dryl.commandpalette`: `attachGlobal` / `detachGlobal` for per-instance Ctrl+K document listener (WeakMap-keyed, no leaks), `focusInput`, `scrollItemIntoView`
- `DrylAutocomplete<TItem>` — Generic combobox; `ItemsProvider` for server-side async search, `SearchFunc` for client-side filtering, `ItemTemplate` for custom option rendering, `DisplayText` converter; ARIA combobox pattern; AI-aware (`Ai` parameter signals model pre-filling the value)
- `DrylDatePicker` — Calendar panel bound to `DateOnly?`; keyboard-navigable ARIA grid (Arrow keys, PageUp/Down, Home/End, Enter/Escape); `Min` / `Max` constraints; optional date range mode via `Range` + `@bind-RangeStart` / `@bind-RangeEnd`; AI-aware
- `DrylStepper` — Multi-step wizard container (mirrors `DrylTabs` cascading pattern); variants: Horizontal / Vertical; `@bind-ActiveStep` two-way binding; compound with `DrylStep`
- `DrylStep` — Single step declaration inside `DrylStepper`; states: Pending / Active / Completed / Error; optional `Description`, `Icon` override; AI-aware (`Ai` parameter wraps the step header in the shared ai-aura ring vocabulary)
- `dryl.js` — `window.dryl.autocomplete.scrollOptionIntoView` and `window.dryl.datepicker.focusDay` helpers
- `StepperOrientation` enum — `Horizontal` / `Vertical` for `DrylStepper`
- `StepState` enum — `Pending` / `Active` / `Completed` / `Error` for `DrylStep`

### Changed
- `DrylSelect` — Now AI-aware: added `Ai` parameter (`AiState`, default `AiState.None`); native `<select>` is wrapped in `.input-wrapper` with the shared ai-aura ring, glow, and wash primitives

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
