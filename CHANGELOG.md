# Changelog

Alle nennenswerten Änderungen an DRYL werden in dieser Datei dokumentiert.

Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/).
Die Versionierung folgt [Semantic Versioning](https://semver.org/lang/de/).

Bedeutung der Versionsschritte:
- **MAJOR** (1.x.x) — Breaking Changes an der öffentlichen API
- **MINOR** (x.1.x) — Neue Komponenten oder Features, abwärtskompatibel
- **PATCH** (x.x.1) — Bugfixes, Dokumentation, Style-Korrekturen ohne API-Änderung

---

## [Unreleased]

### Hinzugefügt
<!-- Neue Komponenten, Features oder Token hier eintragen -->

### Geändert
<!-- Änderungen an bestehenden Komponenten hier eintragen -->

### Veraltet
<!-- Demnächst entfernte Features hier ankündigen -->

### Entfernt
<!-- Entfernte Features hier eintragen -->

### Behoben
<!-- Bugfixes hier eintragen -->

---

## [0.1.0] — 2026-05-27

Erster dokumentierter Stand der Bibliothek. Alle Komponenten befinden sich im Early-Development-Status.

### Hinzugefügt

#### Design System
- `dryl.css` — Komplettes Token-System: Farben, Abstände, Radien, Schatten, Übergänge, Typografie
- AI-Mode-Primitive: `.ai-aura`, `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`, `.ai-indicator`
- `AiState` enum — Gemeinsamer KI-Zustand (`None / Active / Thinking / Streaming / Generated`)
- `DESIGN_TOKENS.md` — Vollständige Token-Referenz
- `COMPONENT_PATTERNS.md` — Bau- und Konventionsregeln für Komponenten
- `CLAUDE.md` — Beitragsregeln für KI-Agenten und menschliche Beitragende

#### Actions
- `DrylButton` — Primäre Interaktionsfläche; Varianten: Primary / Secondary / Ghost / Danger; Größen: Small / Medium / Large; Zustände: Loading, Disabled; Leading- und Trailing-Icon-Slots; AI-Mode

#### Surfaces
- `DrylCard` — Glasoberfläche mit optionalem Cursor-Spotlight; AI-Mode mit rotierendem Gradient-Rahmen
- `DrylDialog` — Servicebetriebener Glasdialog; Fokus-Trap; Größen: Small / Medium / Large / FullScreen; AI-Mode (Human in the Middle)
- `DrylDialogProvider` — Root-Provider; einmalig in `App.razor` einzubinden
- `DrylToast` — Servicebetriebener Toast-Stack; Varianten: Info / Success / Warning / Danger / Ai; 6 Positionen; Auto-Dismiss mit Progress Bar; Hover-Pause; AI-Mode

#### Intelligence (AI)
- `DrylAiIndicator` — Pulsierendes Status-Pill; Label und Pulsgeschwindigkeit passen sich an `AiState` an

#### Data
- `DrylBadge` — Inline-Status-Label; Varianten: Neutral / Accent / Success / Warning / Danger; optionaler Dot
- `DrylIcon` — Lucide-basierter Icon-Satz; verwendet von Button, Badge und weiteren
- `DrylTable<TItem>` — Deklaratives Datengitter; globale Suche, Sortierung (Multi-Sort via Shift-Click), Spaltenfilter (Text / Select), Paginierung, Zeilenauswahl, KPI-Zusammenfassungsleiste; optionaler `DataProvider` für serverseitiges Laden; AI-Mode
- `DrylColumn<TItem>` — Deklarative Spalte für `DrylTable`; `Sortable`, `Searchable`, `Filterable`; custom `CellTemplate` / `HeaderTemplate`; Ausrichtung; Breite
- `DrylTableKpi` — KPI-Zusammenfassungsleiste für `DrylTable`
- `DrylPagination` — Eigenständiger Seitennavigator; First / Prev / Zahlen (Smart-Ellipsis) / Next / Last; Seitengrößen-Selector; "Zeige X–Y von Z"

#### Inputs
- `DrylInputText` — Formular-gebundenes Texteingabefeld; Leading- und Trailing-Icon-Slots; AI-Mode
- `DrylTextarea` — Automatisch größenverstellbares Textareafeld; AI-Mode
- `DrylCheckbox` — Barrierefreie Checkbox mit Label
- `DrylSelect` — Gestyltes Select, eingebunden in `EditForm`
- `DrylToggle` — Ein/Aus-Schalter

#### Layout
- `DrylLayout` — Root-Shell; CSS-Grid mit Sidebar- und Topbar-Slots; kaskadiert Layout-Kontext
- `DrylAppBar` — Klebriger Topbar; optionaler responsiver Drawer-Toggle-Hamburger
- `DrylDrawer` — Seitenleiste; immer sichtbare Spalte auf Desktop, Overlay auf Mobilgerät (`@bind-Open`)
- `DrylMainContent` — Haupt-Content-Slot innerhalb `DrylLayout`; übernimmt Scroll und Padding
- `DrylNavGroup` — Beschriftete Gruppe von Nav-Links innerhalb `DrylDrawer`
- `DrylNavLink` — Einzelne Nav-Zeile mit Icon und Aktiv-Hervorhebung; unterstützt externe Links
- `DrylExpansion` — Einklappbares Glaspanel; gestapelte Panels teilen Rahmen und trennen sich beim Öffnen; AI-Mode
- `DrylTab` / `DrylTabs` — Tab-Leiste mit Glaspanel-Inhalten

#### Feedback
- `DrylAlert` — Feedback-Banner; Varianten: Info / Success / Warning / Danger / Ai; optionaler Titel; Dismissible; AI-Mode
- `DrylTooltip` — CSS-only Hover-Tooltip; 4 Positionen: Top / Bottom / Left / Right

#### Services & Extensions
- `IDrylDialogService` / `DrylDialogService` — Servicebetriebene Dialog-Steuerung; `ShowAsync<T>`, `ShowConfirmAsync`, `ShowAlertAsync`
- `IDrylToastService` / `DrylToastService` — Servicebetriebene Toast-Steuerung
- `AddDrylComponents()` — Extension-Methode für `IServiceCollection`

#### Data Models
- `SortDescriptor`, `FilterDescriptor`, `DataRequest`, `DataResult<TItem>` — Modelle für `DrylTable` DataProvider
- `ColumnAlign`, `ColumnFilterType` — Enums für `DrylColumn`
- `DialogOptions`, `DialogParameters`, `DialogResult`, `DialogSize` — Modelle für `DrylDialog`
- `ToastOptions`, `ToastParameters`, `ToastVariant`, `ToastPosition` — Modelle für `DrylToast`
- `InputState` — Gemeinsamer Zustand für Eingabekomponenten

---

[Unreleased]: https://github.com/Zimpi/DRYL.Components/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Zimpi/DRYL.Components/releases/tag/v0.1.0
