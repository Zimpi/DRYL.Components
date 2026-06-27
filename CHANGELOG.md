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
- `DRYL.Components.Agents` — New companion package integrating the Microsoft Agent Framework (`Microsoft.Agents.AI`). Experimental, independently versioned (0.1.0), decoupled from core. The core stays dependency-free
- `AddDrylAgents()` — DI extension registering `DrylAgentRunner` (scoped); call alongside `AddDrylComponents()`
- `DrylAgentRunner` — Starts agent runs and bridges them to DRYL's AI vocabulary; `Start(...)` returns an observable run, `GenerateStreamingAsync<T>(...)` streams typed structured output, `Replay(...)` drives a run from a pre-built update sequence (recorded runs / demos / tests)
- `DrylAgentRun` — Observable run handle (`State`, `Text`, `ToolCalls`, `TextStream`, `OnChange`); drives `AiState` automatically and feeds `DrylAiScope`
- `DrylToolInvocation` — Captured tool/function call with lifecycle-derived `AiState`; maps 1:1 onto the core `DrylToolCall`
- `DrylAgentToolCalls` — Renders an agent run's tool calls via the core `DrylToolCall` (full trace, or `ActiveOnly`)
- `PartialJsonReader<T>` / `JsonPartialRepair` — Tolerant partial-JSON snapshot engine for structured streaming (hold-last-good on parse failure)
- `DrylAiGenerate<T>` / `GenerationSnapshot<T>` — Streams a typed object from raw JSON tokens and renders progressive partial snapshots; mirrors `DrylAiStream`
- `DrylUiTools` — Factory for four human-in-the-loop `AIFunction` tools (`AskChoice`, `AskMultiChoice`, `RequestPermission`, `AskText`) backed by DRYL dialogs, plus an `All` collection
- `DrylAskChoiceDialog` / `DrylAskMultiChoiceDialog` / `DrylAskTextDialog` — Agent-question dialogs (Agents package) composed from core components; `RequestPermission` reuses the core `DrylConfirmDialog`
- `DrylAgentRunner.StartBuild<T>` — Starts a collaborative, iterative artifact build; framework-owned iteration guidance prompt + auto-injected `update_<T>` merge tool drive the model to refine `T` round-by-round via `DrylArtifactRun<T>`
- `DrylAgentRunner.CreateUpdateTool<T>` — Internal factory that generates the typed `update_<T>` (or custom-named) `AIFunction` tool, embedding `T`'s JSON schema in the description so the model knows the artifact shape
- `DrylArtifactRun<T>` — Observable handle for a collaborative build; live progressively-merged `Artifact` + `Round` counter atop the shared run surface
- `DrylBuildOptions` — `MaxRounds` safety cap (default 12), overridable `Guidance` prompt, custom `UpdateToolName`, and `RevealDuration` (per-round progressive-reveal target, default 1.2 s; `TimeSpan.Zero` = atomic merge)
- `DrylAiBuild<T>` / `ArtifactSnapshot<T>` — Renders the live artifact; each `update_<T>` round materializes progressively (Apple "guided generation" feel) over `DrylBuildOptions.RevealDuration` — the round's new/changed fields type in while earlier fields stay stable, with the `Streaming` aura shown during the reveal (parallel to `DrylAiGenerate<T>`)
- `JsonMerge` — Deep-merge engine for partial artifact patches (objects merge recursively, arrays/scalars replace, null/absent leaves existing)
- `DrylRunBase` — Shared run plumbing (text channel, completion, stable `TextStream`, `OnChange`) extracted from `DrylAgentRun`; base for `DrylAgentRun` and `DrylArtifactRun<T>` (public surface of `DrylAgentRun` unchanged)
- `DrylPresence` — New motion primitive; defers a child's unmount until its exit animation finishes (motion.dev-style AnimatePresence). `Transition`: Fade / Scale / SlideUp / SlideDown / SlideLeft / SlideRight; `Appear`, `OnExited`
- `DrylReveal` — New motion primitive; scroll-triggered staggered entrance via IntersectionObserver. `Transition`: Fade / Rise / ScaleIn; `Stagger`, `Once`, `Threshold`
- `dryl.motion` — New JS module (`onExit`, `moveIndicator`, `observe`) powering the motion primitives; reduced-motion aware
- `--reveal-step` — New motion token (60 ms) controlling `DrylReveal`'s per-child stagger step
- `DrylTabs` — New `AnimateIndicator` parameter (default true) to opt out of the gliding underline
- `DrylLiquidGlass` — New experimental glass surface that warps in 3D toward the pointer; perspective tilt + layered content/gloss parallax + travelling specular highlight + hover lift (visionOS-style depth, pure CSS transforms); `Intensity` (Subtle / Medium / Strong), `Interactive`; reduced-motion aware

### Changed
- `DrylTabs` — The active underline now glides between tabs on a spring instead of fading in per-tab (set `AnimateIndicator="false"` for the old behaviour)

### Fixed
- `DrylDialog` — Dialogs now animate out (scale + fade) on close instead of disappearing instantly; honours `prefers-reduced-motion`

## [1.0.0] — 2026-06-24

First stable release. The public API is now frozen: after 1.0.0, any rename of a
public parameter, event, enum or slot on an existing component — the surface
defined by `CONVENTIONS.md` — is a breaking change (MAJOR bump). 1.0.0 ships the
content of `1.0.0-rc.1` unchanged: the API-freeze event-name audit (board #39),
the JS-interop render-mode audit (#40) and the first wave of behavioural test
coverage for the complex/stateful surfaces (#41). A browsable per-component API
reference (#42) is published alongside it on the docs site.

### Added
- `DrylButton` — New `Class` parameter that merges extra CSS class(es) onto the button's own classes. This is also the fix for a class-clobber bug (see Fixed); a consumer's `class="..."` now binds to `Class` and is merged instead of overriding the button's identity classes. Establishes the library-wide convention (see `CONVENTIONS.md` §2) being rolled out to the remaining components
- **NuGet packaging** — `DRYL.Components` is now a publishable NuGet package with full metadata (id, description, tags, MIT license expression, project/repository URLs, icon, package README and release notes), a symbol package (`.snupkg`), XML documentation and SourceLink-enabled deterministic builds
- **Multi-target framework support** — the library now targets **net8.0, net9.0 and net10.0** (was net10.0 only), with the `Microsoft.AspNetCore.Components.Web` reference pinned per target framework
- `IDrylAiActivityService` / `DrylAiActivityService` — New scoped service (registered by `AddDrylComponents()`) that coordinates `AiState` across components keyed by operation, turning the shared AI vocabulary from a per-component visual into real orchestration. `Begin(key)` returns a disposable `IDrylAiOperation` handle (`Thinking()` / `Streaming()` / `Generated()`; `Dispose()` settles the key back to `None`); `GetState` / `Set` / `Clear` / `OnChanged` round it out. `StreamAsync(key, tokens, onToken, ct)` drives an `IAsyncEnumerable<string>` end-to-end (Thinking → Streaming on first token → Generated on completion) and always settles in a `finally`. Zero external dependency — you bring the token stream (e.g. from `Microsoft.Extensions.AI`); DRYL maps it to the existing `.ai-aura*` primitives. No new `AiState` values, colours, or `dryl.css` changes
- `IDrylAiOperation` — Disposable handle for an in-flight AI operation returned by `IDrylAiActivityService.Begin`
- `DrylAiScope` — New Intelligence component: wrap a region in `<DrylAiScope Key="...">` and every AI-aware component inside it inherits that operation's `AiState` automatically — a button, card and input light up in lockstep while the model works. Tracks `IDrylAiActivityService` by `Key`, or takes an explicit `State` override that needs no service. A component's own `Ai` parameter always wins over the scope
- `DrylAiStream` — New Intelligence component: binds an `IAsyncEnumerable<string>` token stream straight to the UI via a `RenderFragment<AiStreamContext>` (exposing `Text` + `State`), driving `AiState` automatically and settling to `SettleTo` (default `None`) after the `Generated` reveal. Optional `Key` pushes state to `IDrylAiActivityService` so a surrounding `DrylAiScope` reacts; `OnCompleted` fires with the full text. Cancels and restarts cleanly when `Source` changes (CancellationTokenSource disposed on teardown)
- `AiScope` — New cascaded context (Key + State) supplied by `DrylAiScope`, with a static `Resolve(explicitAi, scope)` that defines the one resolution rule (explicit `Ai` wins, otherwise inherit the scope) shared by every consumer
- `AiStreamContext` — New render context for `DrylAiStream`'s child content (`Text`, `State`)
- `DrylAiAware` — New base class (`@inherits DrylAiAware`) giving non-`InputBase` components the opt-in `Ai` parameter plus a cascaded `EffectiveAi` that resolves against a surrounding `DrylAiScope`
- `DrylImage` — New Data component: an intelligent, responsive image surface. Smart defaults remove the usual boilerplate — `loading="lazy"` + `decoding="async"`, an automatic `aspect-ratio` (from `Width`+`Height` or the `Ratio` enum: `Square`/`Video`/`Portrait`/`Wide`) that kills layout shift, `object-fit` cover, a shimmer skeleton while loading (reuses `DrylSkeleton`) and a stylised icon + alt fallback on error. Parameters: `Src`/`Alt` (required), `Width`/`Height`, `Fit` (`Cover`/`Contain`/`Fill`/`None`/`ScaleDown`), `Position` (`Center`/`Top`/`Bottom`/`Left`/`Right`), `Rounded` (`None`/`Sm`/`Md`/`Lg`/`Full`), `Ratio`, `Lazy`, `FallbackSrc`, `FallbackIcon`, `ShowSkeleton`, `Border`, `Shadow`. AI-native: with `Ai` set the **image area itself** reacts — `Active` washes a faint accent over it, `Thinking` drifts a violet→cyan cloud, `Streaming` sharpens from blur (drive it with `@bind`-style `Progress` 0–100 or let it run on a timer via `BlurDuration`), `Generated` reveals with a scale-in — all built only on the shared `.ai-aura*` primitives, no new colours/states/animations. `aria-live="polite"` announces state changes. Scoped CSS only
- `ImageFit` / `ImagePosition` / `ImageRounded` / `ImageRatio` enums — for `DrylImage`
- `DrylList` / `DrylListItem` — New Layout components: a token-driven replacement for ad-hoc `<ul>`/`<ol>` markup. `DrylList` chooses `Ordered` (ol vs ul), a marker `Variant` (`Default` DRYL dot / `Disc` / `Decimal` / `None` / `Dash`), a `Density` (`Compact`/`Default`/`Comfortable`) mapped to the spacing scale, and optional hairline `Dividers` between rows. `DrylListItem` takes an `Icon` (in place of the marker), `Start` / `End` slots (avatar/checkbox · badge/action), `Selected` (accent line + glass tint) and `Disabled` states, and becomes a keyboard-focusable button when given an `OnClick`. Nest a `DrylList` inside an item's content for indented sub-lists with a connector rail. Not AI-aware (a structural primitive, per CLAUDE.md §2.10)
- `ListVariant` / `ListDensity` enums — for `DrylList`
- `dryl.css` — New `.list` / `.list-item` primitives (markers via CSS counters for correct Decimal numbering across nested lists, density custom props, hairline dividers, selected / interactive / disabled states)
- `DrylIcon` — Two new icons: `Image` (lucide: image) and `ImageOff` (lucide: image-off, default `DrylImage` error fallback)
- `DrylAppBar` — New `Elevation` parameter (`Flat` default / `Raised`; Raised lifts the bar with `var(--shadow-md)` + a denser glass tint) and three optional layout slots `Start` / `Center` / `End` that switch the bar from a single flex row to a balanced three-region layout (Start and End flex equally so Center stays optically centred). New `ShowSidebarToggle` renders a desktop-visible button that collapses / expands the sidebar via the shared layout context. Fully backwards-compatible — plain `ChildContent` and the existing mobile hamburger are unchanged
- `DrylDrawer` — Upgraded to a full sidebar. New `Mode` (`SidebarMode`): `Auto` (default, the historical desktop-column / mobile-overlay behaviour), `Static` (always an in-flow column), `Collapsible` (desktop icon-rail collapse via `@bind-Collapsed`), `Pinnable` (collapse state persisted to `localStorage` via `PersistStateKey`) and `Flyout` (always an overlay; closes on `Esc` / backdrop and traps focus, reusing `dryl.modal`). The collapse is a desktop affordance — every non-`Static` mode still becomes the hamburger overlay (backdrop + focus, full labels) below 1024px. New `@bind-Collapsed`, `Width` / `CollapsedWidth` (CSS-length overrides) and pinned `Header` / `Content` / `Footer` slots (a scrolling nav area between a fixed header and footer). Backwards-compatible — `@bind-Open` + plain `ChildContent` keep working
- `SidebarMode` enum — `Auto` / `Static` / `Collapsible` / `Pinnable` / `Flyout` for `DrylDrawer.Mode`
- `AppBarElevation` enum — `Flat` / `Raised` for `DrylAppBar.Elevation`
- `DrylLayout` — New `SidebarWidth` / `SidebarCollapsedWidth` parameters (override the `--sidebar-w` / `--sidebar-collapsed-w` grid-column widths per layout) and app-shell-wide collapse coordination: it reflects the registered drawer's collapsed state onto the grid (`.is-sidebar-collapsed`) so the body reflows in step with the sidebar's icon-rail animation
- `DrylLayoutContext` — New `SidebarCollapsed`, `CanCollapseSidebar` and `ToggleSidebarAsync()` so a `DrylAppBar` button (or any consumer) can collapse the sidebar without wiring state by hand
- `dryl.css` — New app-chrome dimension tokens `--appbar-h` (60px), `--sidebar-w` (260px) and `--sidebar-collapsed-w` (56px), now consumed by `.topbar` / `.app-shell` / `.sidebar`. New `.topbar.is-raised` + `.topbar-start` / `-center` / `-end` slot primitives, `.app-shell.is-sidebar-collapsed`, and sidebar primitives `.sidebar--static` / `--flyout`, `.sidebar.is-collapsed` (icon rail), `.sidebar-header` / `-content` / `-footer`, `.sidebar-backdrop--flyout` and `.sidebar-toggle`
- `DrylNotifications` — New Feedback component: a bell trigger with an unread-count badge plus a popover inbox panel (built on `DrylPopover`) listing notifications with a leading icon chip, title, message, relative "x ago" time, unread dot, per-item dismiss, "Mark all read" and "Clear all". Empty state via `DrylEmptyState`. Works **service-driven** (bind to the new `IDrylNotificationService` — push entries from background jobs / AI completions and the badge updates live) or **controlled** (pass `Items` + `OnMarkRead` / `OnMarkAllRead` / `OnRemove` / `OnClear`). AI-aware per entry: a `DrylNotification` with `Ai != None` carries the shared `.ai-aura` (ideal for "Your report was generated" / "Agent task finished"). Scoped CSS only. Accessible bell (`aria-haspopup` / `aria-expanded` / live unread count) and `role="dialog"` panel
- `IDrylNotificationService` / `DrylNotificationService` — New scoped service (registered by `AddDrylComponents()`): `Add` / `MarkRead` / `MarkAllRead` / `Remove` / `Clear`, `Notifications`, `UnreadCount`, `OnChanged`
- `DrylNotification` — New model: `Id`, `Title`, `Message`, `Icon`, `Timestamp`, `Read`, `Ai`
- `DrylIcon` — Two new icons: `BellOff` (lucide: bell-off, notifications empty state) and `CheckCheck` (lucide: check-check, mark-all-read)
- `DrylTable` — Resizable, reorderable & pinned columns: new `ResizableColumns` adds a drag handle to each header's right edge (pointer-driven, widths reported back to .NET and persisted); `ReorderableColumns` lets users drag a header onto another — or focus it and press `Alt`+`Arrow Left`/`Right` — to reorder, with focus following the moved column. New `DrylColumn.Pinned` (`ColumnPin.Start`/`End`) freezes a column to an edge during horizontal scroll (sticky, opaque backing, edge rule), and per-column `Resizable` / `Reorderable` opt-outs (pinned columns never reorder). Reordering is confined to a pin group. Widths and order persist via `PersistStateKey`. New `dryl.table` helpers (`initColumnResize` / `disposeColumnResize` / `layoutPinned` / `focusHeader`) — no npm
- `ColumnPin` enum — `None` / `Start` / `End` for `DrylColumn.Pinned`
- `DrylColumn<TItem>` — New `Pinned`, `Resizable` and `Reorderable` parameters
- `dryl.css` — New `.tbl-pin` (+ `-start` / `-end`), `.tbl-col-resize`, `.tbl-resizing` and `.tbl-th--col-dragging` / `--col-drop-target` primitives for frozen columns, resize grips and column drag/drop
- `DrylTable` — Inline editing: new `Editable` (bool) plus a per-column `EditTemplate` (`RenderFragment<TItem>`) turn rows into inline editors that reuse the existing DRYL inputs. `EditMode` (`Row` default / `Cell`) chooses whether the whole row or a single cell edits; double-clicking a row/cell or pressing the pencil affordance starts editing, **Enter** commits and **Escape** cancels (handled on the row). Commits raise `OnRowCommitted` (`EventCallback<RowEditEventArgs<TItem>>`); `OnRowCancelled` carries the original row. An optional `CloneRow` (`Func<TItem,TItem>`) edits an isolated working copy so cancel reverts cleanly. Editable cells get the first editor auto-focused via a tiny `dryl.table.focusFirstEditor` helper (no npm); commit/cancel/pencil buttons carry `aria-label`s. Client-only — ignored (with a console warning) under `DataProvider`
- `TableEditMode` enum — `Row` / `Cell` granularity for `DrylTable` inline editing
- `RowEditEventArgs<TItem>` — New record (`Item` original / `EditedItem` working copy) carrying a committed `DrylTable` inline edit
- `DrylColumn<TItem>` — New `EditTemplate` parameter supplying the inline editor for a column
- `dryl.css` — New `.tbl-row--editing`, `.tbl-td-editing` and `.tbl-edit-btn` (+ `--commit` / `--cancel`) primitives for the inline-editing row, editor cell and commit/cancel/pencil affordances
- `dryl.js` — `dryl.table` gains `focusFirstEditor` (focuses + selects the first control in the editing row)
- `DrylSegmentedControl<TValue>` / `DrylSegment<TValue>` — New Inputs components: a compact iOS-style segmented switcher for exclusive view / mode selection in toolbars and headers (List / Board / Calendar, Day / Week / Month). A glass track holds equal-width segments (CSS grid, so widths stay equal even when the track shrink-wraps) with a single accent indicator that glides between them on `--ease-spring` — pure CSS, no JS. Lightweight `@bind-Value` (generic `TValue`, not an `EditForm` input) and carries no panel, unlike `DrylTabs`. `Size` (`Small`/`Medium`/`Large`), `Block`, per-segment `Icon` / `Label` / custom `ChildContent` / `Disabled`. Accessible `role="radiogroup"` + `role="radio"` with roving tabindex, Arrow/Home/End keyboard navigation (skips disabled), programmatic focus move, and a `:focus-visible` accent ring. Not AI-aware (a neutral mode switch, per CLAUDE.md §2.10). Scoped CSS only — no `dryl.css` changes
- `SegmentedSize` enum — `Small` / `Medium` / `Large` for `DrylSegmentedControl`
- `DrylTypo` — New Layout component: a strongly-typed typography primitive. `Variant` (`H1`/`H2`/`H3`/`H4`/`Lead`/`Body`/`Caption`/`Eyebrow`) drives the look while `As` independently chooses the rendered HTML tag, so an H2-styled heading can be a semantic `<h1>`. `Color` maps to the `--fg*` tokens, plus `Align` and a `Gradient` flag (reuses the shared `.gradient-text` primitive). Not AI-aware. New scoped `.typo-*` classes mirror the dryl.css type scale so the look rides a class, independent of the tag
- `DrylStack` — New Layout component: a flex layout primitive replacing ad-hoc `.row`/`.col`/`.between` markup. `Direction` (`Vertical`/`Horizontal`), token-driven `Gap` (`None`…`Xxl` → `--sp-*`), `Align`, `Justify` and `Wrap`. Token-only inline styling, no CSS. Not AI-aware
- `DrylDivider` — New Layout component: a thin separating rule. `Orientation` (`Horizontal`/`Vertical`, reusing the global `.divider`/`.divider-v` primitives) plus an optional centred label via `ChildContent` for the "— or —" pattern; `role="separator"`. Not AI-aware. New scoped `.divider-labelled*` classes for the labelled variant
- `dryl.css` — New `--z-popover: 150` layering token (between `--z-modal` and `--z-toast`) so portaled `DrylPopover` panels render above page content and modals, but below toasts
- `dryl.js` — New `dryl.popover` namespace (`open` / `close`): portals a popover panel to `<body>`, positions it with `position: fixed` against the viewport (placement, flip/clamp, reposition on scroll/resize) and handles click-outside accounting for the portaled panel. Replaces `DrylPopover`'s use of `dryl.menu.attach`
- `DrylButtonGroup` — New Actions component: visually joins related `DrylButton`s into one segmented control (flattened inner corners, merged 1px borders, outer radius preserved across sizes). Works as a clustered toolbar or, with each button's `Pressed`, an exclusive toggle group (reuses the shared `btn--active` surface — no new toggle state invented). `AriaLabel`, `Block`; `role="group"`
- `DrylSplitButton` — New Actions component: a primary action joined to a caret that opens a `DrylMenu` of secondary variants (the "Save ▾ / Save & new / Save & close" pattern), composed from `DrylButton` + `DrylMenu`. `Variant` / `Size` (shared by both segments), `LeadingIcon`, `OnClick`, `MenuItems` slot, `MenuPlacement`, `MenuLabel`, `MenuAriaLabel`, `Block`, AI-aware (`Ai` on the main button); the caret is a labelled icon button
- `dryl.css` — New `.btn-group` / `.btn-group--block` and `.split-btn` / `.split-btn--block` primitives that connect adjacent buttons into a segmented outline
- `DrylErrorBoundary` — New Feedback component: a glass error-fallback surface around Blazor's built-in `ErrorBoundary`. When the protected content throws during render/lifecycle, the default unstyled markup is replaced by a danger `DrylAlert` with `Title`, `Description`, an optional dev-only collapsible stack-trace toggle (`ShowDetails`) and a retry button that recovers the boundary (`ShowRetry` / `RetryText` / `OnRetry`). AI-aware via `Ai` (the fallback carries the shared aura — ideal for failed AI blocks); `FallbackContent` fully overrides the surface and receives the caught `Exception`; public `Recover()` for programmatic recovery
- `DrylIcon` — New `Refresh` icon (lucide: rotate-ccw) for the error-boundary retry action
- `DrylTable` — New `Reorderable` (bool, default `false`) and `OnRowReordered` (`EventCallback<RowReorderEventArgs>`) enable manual row reordering via a leading grip-handle column. Drag a handle to move a row, or focus it and press `Alt`+`Arrow Up`/`Arrow Down` for a keyboard-accessible move (focus follows the moved row). The table updates its displayed order immediately and raises `OnRowReordered` so consumers can persist the new order. Requires a plain client list — ignored (with a console warning) under `Virtualize`, `GroupBy` or `DataProvider`, and the handle is disabled while a sort is active. No npm — native HTML5 drag events plus a tiny `dryl.table.focusGrip` helper
- `RowReorderEventArgs` — New record (`OldIndex` / `NewIndex`, view-relative) carrying a `DrylTable` row move
- `DrylIcon` — New `GripVertical` icon (lucide: grip-vertical) for the table reorder handle
- `dryl.css` — New `.tbl-col-grip` / `.tbl-grip` primitives and `.tbl-row--dragging` / `.tbl-row--drop-target` row states for the reorder handle and drag affordance
- `dryl.js` — New `dryl.table` namespace (`focusGrip`) restores focus to the reorder handle after a keyboard row-move
- `DrylToolCall` — New AI component: visualises a single agent tool / function call — tool name, a live status pill (`DrylAiIndicator`) and a collapsible body holding arguments / result as JSON (`DrylCodeBlock`). Status uses the shared `AiState` vocabulary (`Thinking`=running, `Streaming`, `Generated`=done); `Error` shows a danger `DrylAlert`. Stack inside a `DrylTimeline` for a full agent trace
- `DrylCitation` — New Data component: inline source-attribution chip (`[n]`) that reveals title / URL / snippet in a `DrylPopover`; for verifiable RAG answers. `Index`, `Title`, `Url`, `Snippet`; accessible `<button>` trigger
- `DrylCitationList` / `DrylCitationListItem` — New Data components: the numbered source list that complements the inline chips (`<ol>` semantics, external links)
- `DrylMarkdown` — New Surfaces component: renders Markdown (CommonMark + GFM via Markdig) into the DRYL glass aesthetic. Fenced code blocks are delegated to `DrylCodeBlock`; all other content is rendered with **raw HTML disabled** so model-authored markup is escaped rather than executed. Re-renders as tokens arrive (streaming). `Content`, `Ai`. New global `.md` / `.md-content` CSS primitives
- `Markdig` — New (and only) external runtime dependency, added to power `DrylMarkdown`. Documented exception to CLAUDE.md rule 2.8; see `THIRD_PARTY_NOTICES.md` (BSD-2-Clause)
- `DrylCodeBlock` — New Data component: glass code surface with a language label and copy-to-clipboard button (`Code`, `Language`, `ShowLineNumbers`, `Ai`). Code is rendered text-only (HTML-encoded). Consumed by `DrylMarkdown` for fenced code blocks. AI-aware (Streaming glow)
- `dryl.js` — New `dryl.clipboard` namespace (`copy`): writes text to the clipboard via the async Clipboard API with an `execCommand` fallback; returns success so callers can show copied/failed feedback
- `DrylIcon` — Three new icons: `Link` (lucide: link), `Quote` (lucide: quote), `Wrench` (lucide: wrench) for the new AI components
- `dryl.js` — New `dryl.keynav` namespace (`attach` / `detach`): suppresses default page-scroll for navigation keys on a host element. `dryl.tree` is now a backwards-compatible alias of it; `DrylSelect` reuses it for its combobox trigger
- `DrylScrollArea` — New Layout component: a container-scoped scrollable region with the DRYL thin accent scrollbar (`MaxHeight`, `MaxWidth`, `Horizontal`). Pure CSS, no JS — for sidebars, log viewers, code blocks and long lists
- `DrylKbd` — New Data component for keyboard-shortcut display: renders semantic `<kbd>` chips; single key via content (`<DrylKbd>⌘K</DrylKbd>`) or a chord via `Keys` (`Keys="@(new[]{"Ctrl","K"})"`) joined by `Separator`. Token-based styling, no JS
- `DrylTable` — New `ShowExport` (bool, default `false`) and `ExportFileName` (string, default `"export.csv"`) parameters add a CSV export button to the toolbar. The export honours the active search, filters and sort, includes only the visible columns, and (in client mode) covers the full filtered result set across all pages. No npm — download is produced via a Blob URL (`dryl.download`); a UTF-8 BOM and `InvariantCulture` number formatting keep it Excel- and locale-safe
- `dryl.js` — New `dryl.download` namespace (`text` / `csv`) triggers a client-side file download via a transient Blob URL
- `dryl.css` — New `.tbl-toolbar-action` / `.tbl-toolbar-action--auto` primitives position toolbar action buttons (e.g. CSV export)
- `DrylEmptyState` — Now AI-aware: new `Ai` parameter (`AiState`, default `None`) drives the shared aura (ring / glow / Generated wash); in AI mode the placeholder gains a glass surface so the ring frames it. Demo page extended with Thinking / Streaming / Generated examples
- `DrylButton` — New `Pressed` parameter (`bool?`, default `null`) for toggle buttons (mute / bold / filter on-off): emits `aria-pressed` and highlights the button via the new `.btn--active` surface while pressed
- `dryl.css` — New `.btn--active` primitive (accent border + glow) for pressed/toggled buttons
- `DrylPopover` — Anchored floating-panel primitive; `@bind-Open`; `TriggerContent` / `PanelContent` slots; `Placement` (BottomStart / BottomEnd / TopStart / TopEnd); `MatchTriggerWidth`; `Block`; `CloseOnClickOutside` / `CloseOnEscape`; optional glass `Surface`; exposes `PanelElement` / `AnchorElement` for panel-scoped interop
- `DrylEmptyState` — "No data" placeholder; `Icon`, `Title`, `Description`, `ActionContent` slot; `Size` (Small / Medium)
- `DrylDescriptionList` — Semantic `<dl>` key/value view; `Layout` (Stacked / Inline); `Columns`
- `DrylDescriptionItem` — Single term/value pair; `Term`, `Icon`, value content
- `DrylFormField` — Generic `<TValue>` label + required marker + hint + inline validation wrapper for any input; `For` expression binds validation messages within an `EditForm`
- `DrylValidationSummary` — Glass-styled summary of all `EditContext` validation errors; subscribes to validation-state changes
- `PopoverPlacement` enum — `BottomStart` / `BottomEnd` / `TopStart` / `TopEnd` for `DrylPopover`
- `EmptyStateSize` enum — `Small` / `Medium` for `DrylEmptyState`
- `DescriptionLayout` enum — `Stacked` / `Inline` for `DrylDescriptionList`
- `DrylSparkline` — Tiny inline-SVG trend chart (zero JS); `Line` / `Area` / `Bar`; `Width` / `Height`; `ShowLastDot`; accent-gradient stroke/fill; all coordinates formatted with `InvariantCulture`
- `DrylStat` — KPI / metric card on a glass surface; `Label`, `Value`, `Icon`, `Delta` + `Direction` (Up / Down / Neutral) chip; `Sparkline` slot; AI-aware
- `DrylTimeline` — Vertical event sequence; draws the connecting rail for child `DrylTimelineItem`s; `role="list"`
- `DrylTimelineItem` — Single event; variant-tinted marker (Default / Accent / Success / Warning / Danger), `Title`, `Timestamp`, `Icon`, body; AI-aware marker (agent step traces)
- `DrylTreeView` — Hierarchical tree; declarative `DrylTreeNode` children; `@bind-SelectedValue`; roving-tabindex focus; full WAI-ARIA tree keyboard nav (arrows expand/collapse/move, Home/End, Enter/Space); `role="tree"`
- `DrylTreeNode` — Tree node; `Text`, `Icon`, `Value`, `@bind-Expanded`, `Disabled`; chevron toggle; nests further nodes
- `SparklineKind` enum — `Line` / `Area` / `Bar` for `DrylSparkline`
- `DeltaDirection` enum — `None` / `Up` / `Down` / `Neutral` for `DrylStat`
- `TimelineVariant` enum — `Default` / `Accent` / `Success` / `Warning` / `Danger` for `DrylTimelineItem`
- `dryl.js` — New `dryl.tree` namespace: `attach` / `detach` prevent default page-scroll for tree navigation keys (Tab left untouched)
- `DrylAvatar` — User / entity face; image with initials/icon/generic fallback; `Size` (Small / Medium / Large); `Shape` (Circle / Square); presence `Status` dot (Online / Busy / Away / Offline); initials derived from `Name`
- `DrylAvatarGroup` — Overlapping avatar stack; cascades `Size` to children; `Max` collapses overflow into a `+N` tile
- `DrylBreadcrumbs` — Hierarchical navigation trail; child `DrylBreadcrumbItem` registration; custom `Separator`; `MaxItems` collapses the middle into an ellipsis; `<nav>/<ol>` semantics, last crumb `aria-current="page"`
- `DrylBreadcrumbItem` — Single crumb; `Href` (link) or plain text; optional leading `Icon`
- `DrylProgress` — Linear progress bar; determinate or `Indeterminate` sweep; `Variant` (Accent / Success / Warning / Danger); `Size` (Small / Medium / Large); `ShowLabel` percentage; `role="progressbar"` ARIA; AI-aware
- `DrylChat` — Conversation surface; scrollable message log + pinned `Footer` composer slot; `Height`; `AutoScroll` via `dryl.chat.scrollToEnd`; `role="log"` + `aria-live="polite"`; AI-aware
- `DrylMessage` — Chat bubble; `Role` (User / Assistant / System) drives alignment & styling; `Author`, `Timestamp`, avatar slot, `Typing` dots; AI-aware
- `DrylChatComposer` — Chat input; `@bind-Value`; `OnSend`; Enter sends, Shift+Enter newline, auto-grow textarea via `dryl.chat.attachComposer`; AI-aware
- `AvatarSize` / `AvatarShape` / `AvatarStatus` enums for `DrylAvatar`
- `ProgressVariant` / `ProgressSize` enums for `DrylProgress`
- `MessageRole` enum — `User` / `Assistant` / `System` for `DrylMessage`
- `dryl.js` — New `dryl.chat` namespace: `scrollToEnd`, `attachComposer` (Enter-to-send + auto-grow), `detachComposer`, `resize`
- `DrylChipInput` — Free-text tag field; chips created on Enter / comma; Backspace removes last chip; `@bind-Tags` (`IReadOnlyList<string>`); `MaxTags`; AI-aware
- `DrylRating` — Star rating input inheriting `InputBase<int?>`; configurable `MaxStars`; hover preview; `AllowClear`; `ReadOnly`; keyboard navigation (arrows, Home, End); EditForm / DataAnnotations validation; AI-aware
- `DrylInputOtp` — Fixed-box OTP/2FA code entry inheriting `InputBase<string>`; configurable `Digits` (default 6); auto-focus advance; paste-to-fill via `dryl.otp` JS helper; AI-aware
- `DrylTimePicker` — Time-only picker inheriting `InputBase<TimeOnly?>`; scrollable hour/minute panel; `Min`/`Max`; `MinuteStep` (1, 5, 10, 15, 30…); Escape/Enter keyboard support; AI-aware
- `DrylInputMask` — Masked input inheriting `InputBase<string>`; predefined `MaskType` (Phone / Iban / PostalCode / CreditCard) or `CustomPattern` (`#` = digit, `A` = letter); formatting enforced via `dryl.inputmask` JS helper (input + paste); `LeadingIcon` slot; AI-aware
- `MaskType` enum — `Phone` / `Iban` / `PostalCode` / `CreditCard` / `Custom` for `DrylInputMask`
- `dryl.js` — Three new namespaces: `dryl.otp` (focusNext, focusPrev, attach/paste), `dryl.timepicker` (click-outside attach/detach, scrollToActive), `dryl.inputmask` (format-on-input attach/detach, paste)
- `DrylIcon` — Sechs neue Icons: `Circle` (lucide: circle), `Command` (lucide: command), `Hash` (lucide: hash), `List` (lucide: list), `Sliders` (lucide: sliders-horizontal), `Upload` (lucide: upload); werden in der Demo-Navigationsleiste verwendet

### Changed
- **BREAKING (API freeze)** `DrylExpansion` — Renamed `IsOpen` / `IsOpenChanged` → `Open` / `OpenChanged` to follow the no-`Is` boolean convention (`CONVENTIONS.md` §2/§3). Update `@bind-IsOpen` → `@bind-Open`. Resolves the last of the board #39 event-name deviations
- **BREAKING (API freeze)** `DrylPagination` — Renamed the page/size events to the bindable `<Property>Changed` form (`CONVENTIONS.md` §3): `OnPageChanged` → `CurrentPageChanged` and `OnPageSizeChanged` → `PageSizeChanged`. Both pair with their property for `@bind-CurrentPage` / `@bind-PageSize`
- **BREAKING (API freeze)** `DrylTable` — Normalised pagination to match `PageSize`/`PageSizeChanged`: the current page is now the two-way bindable `Page` / `PageChanged` (replaces the notification-style `OnPageChanged`). Use `@bind-Page` to control or observe the page; `PageSize`/`PageSizeChanged` are unchanged
- `DrylButton`, `DrylCard`, `DrylMessage`, `DrylChat`, `DrylInputText`, `DrylTextarea`, `DrylAutocomplete`, `DrylSelect` — These AI-aware components now inherit their `AiState` from a surrounding `DrylAiScope` when no explicit `Ai` is set, so a single operation can light them up together. An explicit `Ai` still wins, and with no scope present behaviour is unchanged. The four non-`InputBase` ones (`DrylButton`, `DrylCard`, `DrylMessage`, `DrylChat`) now derive from the new `DrylAiAware` base class. **No public API change**
- `DrylCodeBlock` — Now syntax-highlights code server-side via a tiny dependency-free C# tokenizer (no JS, no npm — CLAUDE.md rules 2.1 / 2.8). Token colors map only onto existing DRYL tokens (keyword→`--accent-a`, type→`--accent-b`, string→`--success`, number→`--warning`, comment→`--fg-faint`, punctuation→`--fg-muted`). Languages: `csharp`, `javascript`/`typescript`, `json`, `html`/`xml`, `css`, `bash`, `sql`, `python` (with common aliases); unknown languages fall back to plain text. Every token is HTML-encoded before wrapping, so model-authored code stays injection-safe. New `Highlight` parameter (bool, default `true`) opts out to verbatim plain text. Highlighting also flows automatically through `DrylMarkdown` fenced code blocks. **No breaking change**
- `DrylMessage` — New optional `Text` (string) and `Markdown` (bool) parameters: when `Text` is set it takes precedence over `ChildContent`, and with `Markdown="true"` it is rendered through `DrylMarkdown` (formatted Markdown + code blocks) — ideal for streaming LLM output. Defaults keep existing `ChildContent` usages unchanged. **No breaking change**
- `DrylButton` — Tactile "Sheen & Spring" interaction polish: a soft light reflection sweeps across the surface on hover (all variants except Ghost), the press now drops-and-shrinks with a spring-back release (`--ease-spring`), and icons animate on hover (trailing slides forward, leading pops, icon-only scales). Leading/trailing icons gain marker classes `btn-ico-lead` / `btn-ico-trail`. All token-driven; honours `prefers-reduced-motion`. **No public API change**
- `DrylMenu`, `DrylSelect`, `DrylAutocomplete`, `DrylTimePicker`, `DrylDatePicker` — Refactored onto the shared `DrylPopover` primitive for anchoring, positioning and click-outside dismissal; the duplicated panel-positioning CSS (`position:absolute; top:calc(100% + var(--sp-1)); …`) and per-component `dryl.menu.attach` boilerplate were removed. **No public API change** — parameters, keyboard navigation, ARIA and visuals are unchanged
- `dryl.js` — `dryl.menu.focusTrigger` now also matches a trigger inside `.popover-trigger` (used by the refactored dropdowns)
- `DrylSelect` — Replaced native `<select>` element with a fully custom dropdown; API changed from `ChildContent` (`<option>` elements) to `Items` (`IEnumerable<SelectItem>`); panel and option styling now matches `DrylAutocomplete` (glass background, accent scrollbar, selected-item dot); `Placeholder` parameter added; click-outside detection via `dryl.menu.attach`; keyboard navigation (ArrowDown/Up, Enter, Space, Escape, Tab)
- `DrylNavGroup` — New `Collapsible` parameter (bool, default `false`) enables accordion-style sub-menus with CSS grid animate-in/out; `DefaultExpanded` (bool, default `true`) sets initial state; `Href` parameter makes the header a `NavLink` while a separate chevron button controls collapse; `Icon` parameter adds a leading icon to the collapsible header
- `DrylNavLink` — New `Sub` parameter (bool, default `false`) renders the item indented (`.nav-item--sub`) for use inside collapsible `DrylNavGroup` children
- `dryl.css` — New primitives for collapsible nav: `.nav-scroll` (scrollable sidebar middle area), `.nav-section-toggle`, `.nav-section-header`, `.nav-section-link`, `.nav-section-chevron-btn`, `.nav-section-chevron`, `.nav-children`, `.nav-children-inner`, `.nav-item--sub`

### Fixed
- `DrylFileUpload`, `DrylMultiSelect` — `DisposeAsync` no longer throws a `JSDisconnectedException` when the Blazor Server circuit is already gone at teardown. Both guarded the prerender case (`_jsReady`) but called `dryl.*.detach` without catching a disconnected circuit; the detach is now wrapped in `try { … } catch (JSDisconnectedException) catch (JSException)` like the other interop components. Closes the render-mode audit (board #40): all 20 JS-interop components are now verified prerender-safe (no JS before first interactive render) with a guarded, disconnect-tolerant `DisposeAsync`
- Surfaces (`DrylCard`, `DrylChat`, `DrylDialog`, `DrylMarkdown`, `DrylMessage`, `DrylPopover`, `DrylToast`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter. With this, **all 61 components that accept pass-through attributes now merge a consumer `class` instead of clobbering their identity classes** — the library-wide convention (`CONVENTIONS.md` §2) is complete
- Navigation (`DrylBreadcrumbs`, `DrylMenu`, `DrylMenuItem`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylMenu` forwards it to its `DrylPopover`)
- Layout (`DrylDivider`, `DrylExpansion`, `DrylList`, `DrylListItem`, `DrylScrollArea`, `DrylStack`, `DrylTypo`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylListItem` additionally now splats its `AdditionalAttributes`, previously captured but never rendered)
- Inputs (`DrylChipInput`, `DrylFormField`, `DrylValidationSummary`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter applied to its field root
- Feedback components (`DrylAlert`, `DrylEmptyState`, `DrylErrorBoundary`, `DrylNotifications`, `DrylProgress`, `DrylSkeleton`, `DrylSpinner`, `DrylTooltip`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylErrorBoundary` forwards it to its fallback `DrylAlert`)
- Data components (`DrylAvatar`, `DrylAvatarGroup`, `DrylBadge`, `DrylCitation`, `DrylCitationList`, `DrylCitationListItem`, `DrylCodeBlock`, `DrylDescriptionList`, `DrylImage`, `DrylKbd`, `DrylPagination`, `DrylSparkline`, `DrylStat`, `DrylTable`, `DrylTableKpi`, `DrylTimeline`, `DrylTimelineItem`, `DrylTreeView`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter so a consumer's `class="..."` is folded into the component's own classes instead of overriding them. (`DrylTableKpi` additionally now splats its `AdditionalAttributes`, which were previously captured but never rendered)
- `DrylAiIndicator`, `DrylToolCall` — Same consumer-`class` clobber fix: both now expose a merged `Class` parameter
- `DrylButtonGroup`, `DrylSplitButton` — Same consumer-`class` clobber fix as `DrylButton`: both now expose a merged `Class` parameter so a consumer's `class="..."` is folded into the group/split-button classes instead of overriding them
- `DrylButton` — A consumer-supplied `class="..."` no longer wipes the button's own classes. Passing a class through the `@attributes` splat overrode the explicit `class="btn btn-primary …"` (Blazor's splat clobbers a same-element `class`, it does not merge), so `<DrylButton class="mt-4">` rendered `class="mt-4"` and lost all button styling. A new merged `Class` parameter now captures it (Blazor matches `class`→`Class` case-insensitively) and folds it in. The same convention is being rolled out to the other components that expose `AdditionalAttributes` without a `Class` parameter
- `DrylTable` — Persisted column widths are now formatted with `InvariantCulture`. A resized column wider than 999px previously rendered an invalid CSS width (e.g. `1.200px`) under group-separator locales such as German, collapsing the column; widths now always use a `.`-free integer pixel value
- `DrylButton` — A disabled button is now visually distinct from an enabled one. There was no `.btn:disabled` rule at all, so a disabled primary kept its full accent gradient and glow and read as active (WCAG 1.4.1 — state not conveyed). Disabled buttons are now dimmed (`opacity: 0.45`), desaturated, stripped of the accent glow, flattened (no lift) and show a `not-allowed` cursor, across all variants
- `DrylFileUpload` — AI mode (`Ai != None`) no longer draws a square-cornered ring/glow around the rounded drop zone. The wrapper had no radius, so the `.ai-aura-ring`/`.ai-aura-glow` `border-radius:inherit` resolved to `0`; the wrapper now carries `border-radius: var(--r-lg)` (matching `.file-drop`) via a stable `.file-upload-wrapper` class, so the rotating ring traces the rounded corners — mirroring the existing input/textarea/radio-group wrapper rules
- `DrylTable` — Global search bar no longer renders the magnifier icon floating outside a left-padded box (icon, gap, then placeholder text starting far inside). The `.input-icon` absolute-positioning rule was scoped to `.input-wrapper` only, so in the table toolbar's `.tbl-search` host the icon stayed inline while `.has-leading-icon` still reserved 38px of padding. `.tbl-search` now shares the same icon positioning, so the search reads as one cohesive field
- Inputs (`DrylInputText`, `DrylTextarea`, `DrylSelect`, `DrylChipInput`, `DrylInputOtp` and every component using the shared `.input`/`.textarea`/`.select` primitives) — Focused fields no longer snap their corners from `var(--r-md)` (14px) to a near-square 4px. The global `:focus-visible` rule was clobbering each component's `border-radius` with a forced `4px` (same specificity, declared later in the cascade); that declaration is removed and the outline now follows the element's own radius. The focus glow also gains a flush `0 0 0 1px var(--accent-line)` ring that fuses with the border into one crisp accent edge instead of a thin line with a detached halo
- `DrylPopover` — Panel is no longer clipped by an ancestor's `overflow: hidden` or `backdrop-filter` (e.g. a `DrylCard`/glass surface). It now portals to `<body>` and positions itself with `position: fixed` against the viewport (placement maths, viewport flip/clamp, reposition on scroll/resize in `dryl.popover`), so e.g. a `DrylCitation` chip inside a card opens fully visible above the prose. Fixes the same clipping for every consumer (`DrylMenu`, `DrylSelect`, `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker`) and lets a popover opened inside a `DrylDialog` render above the modal. The panel wrapper is now always present (content still gated by open state) so Blazor never removes a portaled node — avoiding a Blazor-Server crash on consumer-driven close. **No public API change**
- `DrylMarkdown` — AI mode (`Ai != None`) no longer draws a sharp-cornered rectangle hugging the text. The `.md` host had no radius/surface, so the rotating ring's `border-radius:inherit` resolved to `0`; it now gets `background: var(--glass-1)`, `border-radius: var(--r-lg)` and padding so the ring frames a proper glass panel, mirroring `DrylRating` / `DrylEmptyState`
- `DrylTable` — CSV export in server mode (`DataProvider`) now exports the **full filtered result set** across all pages instead of only the currently loaded page. The export issues a dedicated `DataProvider` call carrying the active search / sort / filters with `Skip=0` / `Take=int.MaxValue`; client mode is unchanged
- `DrylSelect` — Navigation keys (Arrow Up/Down, Home/End) no longer scroll the page behind the combobox while moving the option highlight. The trigger now attaches the shared `dryl.keynav` helper (preventDefault for nav keys only; Tab / Enter / Escape stay live so focus can leave the control). Mirrors the `DrylTreeView` behaviour
- `DrylPagination` — Active page button now carries `aria-current="page"` so screen readers can identify the current page (previously only `aria-label="Page N"`, indistinguishable from the others)
- `DrylEmptyState` — Root element is now a labelled `role="region"` (`aria-label` derived from `Title`, falling back to "Empty") so assistive tech can identify and navigate to the empty state
- `DrylSelect` — `ArrowUp` now opens the dropdown when the combobox is focused but closed (highlighting the selected option, or the last option if none), matching `ArrowDown` and the ARIA combobox pattern; previously `ArrowUp` did nothing while closed
- `DrylStepper` — Step headers are now real `<button>` elements: keyboard-focusable and activatable with Enter/Space (previously plain `<div @onclick>`, unreachable by keyboard — WCAG 2.1.1). Active header carries `aria-current="step"` and a visually-hidden `aria-live="polite"` region announces the active step
- `DrylCard` — Spotlight `mousemove` listener is now removed on dispose (`dryl.spotlight.untrack`); previously the handle was always `null` so the listener leaked on every navigation away from a page using a card
- `DrylInputNumber` — No longer emits spurious `min`/`max`/`step="0"` attributes for non-nullable value types (e.g. `TValue="int"`), which made the browser reject any positive value ("Value must be 0") and the stepper increment by 0. The native constraints are now emitted only when `Min`/`Max`/`Step` are explicitly set
- `DrylFormField` — No longer renders its own validation message (the wrapped DRYL input already shows one), so errors no longer appear twice; its hint is hidden while the field is invalid
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

[Unreleased]: https://github.com/Zimpi/DRYL.Components/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Zimpi/DRYL.Components/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/Zimpi/DRYL.Components/releases/tag/v0.1.0
