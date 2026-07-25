# Canvas Phase 4 — Katalog-Ausbau (Detail-Spec)

**Datum:** 2026-07-25
**Status:** freigegeben
**Rahmen:** Phase 4 aus `2026-07-25-canvas-platform-roadmap.md` — bindend sind A1–A9 und die
globalen Nicht-Ziele. Ausgangsstand: Kern **2.14.1**, Agents **0.12.0**.
**Zielversionen:** Kern → **2.15.0** (MINOR), Agents → **0.13.0** (MINOR).

## 1. Ziel

Neun neue Node-Typen, damit ein echtes Fachanwendungs-Dashboard nichts vermisst:
`dataGrid`, `form`, `kpi`, `list`, `keyValue`, `accordion`, `image`, `code`, `emptyState`.
Alle rendern über bestehende DRYL-Komponenten — **keine neue öffentliche Komponente**, keine
neuen Tokens, keine neuen Animationen. Dazu die in der Roadmap geforderte Antwort auf das
Token-Budget des Schemas.

**Nicht-Ziele:** keine Karte/Map, kein Rich-Text-Editor, keine host-konfigurierbare
Katalog-Teilmenge (YAGNI, bis ein Host sie braucht), kein neues Daten-Shape.

## 2. Die neun Typen

Jeder Typ bekommt: Katalog-Eintrag (`AllTypes` + ggf. `ContainerTypes`), Prop-Klasse +
Validierung in `CanvasCatalog`, Render-Case in `CanvasNodeView`, eine Schema-Zeile
(§3), Tests. Container-/Interactive-Mitgliedschaft ist zentral in `CanvasCatalog` —
Patcher, StreamReveal und FLIP funktionieren damit automatisch.

### 2.1 `dataGrid` — der große Bruder von `table`

Rendert `DrylTable<TItem>` mit einem internen Zeilentyp (`CanvasGridRow`, Wrapper um
`IReadOnlyList<string>` + stabiler Index). Spalten entstehen deklarativ aus den Props
(`DrylColumn` je Spalte, `FieldGetter` per Zellenindex).

```json
{ "type": "dataGrid", "props": {
    "columns": ["Auftrag", "Kunde", "Status"],
    "rows": [["4711", "ACME", "offen"]],
    "sortable": true, "filterable": false, "searchable": false, "pageSize": 10 } }
```

- `columns: string[]` (Pflicht, 1–12), `rows?: string[][]` (Zellen = Spaltenzahl).
- `sortable?: bool` (default **true**), `filterable?: bool` (default false, Select-Filter je
  Spalte), `searchable?: bool` (default false, Toolbar-Suche), `pageSize?: number`
  (default 10; 0 = kein Paging; max 100).
- Literale Rows: max **100** (mehr → korrigierender Receipt wie bei `table`).
- Datenbindung: Shape **Rows**; der Mapper kappt bei **1000** Zeilen mit dem bestehenden
  Truncated-Hinweis. `MaxTableRows` (30) gilt weiterhin nur für `table`.
- `Bordered=true` (Glass-Card), `RowIdSelector` = Zeilenindex, kein Virtualize/GroupBy.
- Sortieren/Filtern/Suchen ist rein clientseitig im Renderer — der Host liefert fertige
  Ergebnisse (Roadmap-Nichtziel „keine Abfragesprache" bleibt unberührt).
- A8: ein Daten-Refresh ersetzt die `Items`-Referenz; die Node-Identität bleibt, der
  Change-Pulse trägt die Bewegung — kein Neuaufbau des Grids.

### 2.2 `form` — Container mit Submit auf eine Aktion

Container (neu in `ContainerTypes`); Kinder sind beliebige Nodes, typisch die interaktiven.
Die Aktion sitzt als bestehendes `action`-Binding **am form-Node selbst**; der Renderer
zeichnet unter den Kindern einen Submit-`DrylButton`, der durch den vorhandenen
`CanvasActionRunner` läuft (Busy-Beat, Confirm-Dialog, Inline-Fehler — alles Bestand).

```json
{ "type": "form", "props": { "submitLabel": "Auftrag anlegen", "required": ["customer"] },
  "action": { "name": "order.create", "args": { "customer": { "$field": "customer" } } },
  "children": [ { "type": "inputText", "props": { "name": "customer", "label": "Kunde" } } ] }
```

- `submitLabel: string` (Pflicht), `required?: string[]` (Feldnamen).
- Validierung: `Validate` prüft, dass ein `action`-Binding existiert und jeder
  `required`-Eintrag ein interaktiver Node **innerhalb dieses form-Subtrees** ist.
- Submit mit leerem Pflichtfeld: kein Invoke; die betroffenen Felder zeigen einen
  Inline-Hinweis (`.canvas-field-required`, per `DrylPresence` eingeblendet); der erste
  ungültige Zustand ist ein Beat, kein Alert.
- `CanvasCatalog.ValidateAction` wird von „nur button" auf „button oder form" geweitet —
  genau die in Phase 2 angekündigte Weitung; der Trigger bleibt eine bewusste Nutzeraktion (A4).
- Kein `intent`-Fallback am form-Node: ohne registrierte Aktion ist das form-Binding invalide
  (Receipt sagt warum). Buttons behalten ihren Bestand.

### 2.3 `kpi` — Reihe kompakter Stats

Rendert eine responsive Reihe von `DrylStat` (CountUp, `Ai=Generated` wie `stat`).

- `items: [{ label, value, delta?, direction? }]`, 1–6 Einträge; gleiche Feldregeln wie `stat`.
- Layout: Flex-Reihe mit Umbruch (`.canvas-kpi`), Container-Query-sicher — bei schmaler
  Fläche stapeln die Kacheln.
- **Keine** Datenbindung: gebundene KPIs sind ein `grid` aus gebundenen `stat`-Nodes
  (ein Scalar je Quelle) — zwei Wege für dasselbe wären Validierungs-Doppelpfad.

### 2.4 `list` — Repeater über Rows

Rendert `DrylList`/`DrylListItem` (Icon-Slot, Titel + Sekundärtext).

- Literal: `items: [{ title, text?, icon? }]`, 1–50; `icon` muss ein bekannter
  `DrylIcon`-Name sein, sonst wird er still weggelassen (kein Receipt-Fehler — Ikonografie
  ist Deko).
- Datenbindung: Shape **Rows** — Spalte 0 → `title`, Spalte 1 (falls vorhanden) → `text`,
  weitere Spalten werden ignoriert; Kappung bei 50 mit Truncated-Hinweis.

### 2.5 `keyValue` — Beschriftungspaare

Rendert `DrylDescriptionList`/`DrylDescriptionItem`.

- `pairs: [{ key, value }]`, 1–20; `columns?: 1|2` (default 1).
- Datenbindung: Shape **Rows** mit **genau 2 Spalten** (sonst korrigierender Receipt:
  „a keyValue needs a 2-column rows source"); Zeile → Paar, Kappung bei 20.

### 2.6 `accordion` — Container mit auf-/zuklappenden Abschnitten

Wie `tabs`: `labels: string[]`, genau ein Kind je Label. Jeder Abschnitt ist eine
`DrylExpansion` (bringt die Höhen-Animation mit, `prefers-reduced-motion` inklusive).

- `labels: string[]` (≥1, Count == Children), `open?: number` (Index des initial offenen
  Abschnitts; default alle zu; außerhalb des Bereichs → Receipt).
- Auf/Zu ist lokaler UI-Zustand des Renderers (wie der aktive Tab) — er wandert nicht in
  den Spec und übersteht Patches an anderen Nodes.

### 2.7 `image`

Rendert `DrylImage` (Skeleton beim Laden, Fallback-Icon bei Fehler — beides Bestand).

- `src: string` (Pflicht), `alt: string` (Pflicht — a11y ist nicht optional),
  `ratio?: "auto"|"1:1"|"16:9"|"21:9"` (default auto; die vier `ImageRatio`-Werte von
  `DrylImage`), `fit?: "cover"|"contain"`
  (default cover), `caption?: string` (als `<figcaption>`-artige `--fg-dim`-Zeile).
- **URL-Sicherheit:** `src` muss mit `https://`, `/` oder `data:image/` beginnen;
  alles andere (insb. `javascript:`, `http:`) → korrigierender Receipt. Das ist
  Modell-Output, der im DOM landet — die Validierung ist die Grenze.

### 2.8 `code`

Rendert `DrylCodeBlock` (Highlighting, Copy-Button — Bestand).

- `code: string` (Pflicht), `language?: string`, `lineNumbers?: bool` (default false).

### 2.9 `emptyState`

Rendert `DrylEmptyState`.

- `title: string` (Pflicht), `description?: string`, `icon?: string` (unbekannter
  Icon-Name → Default-Icon, kein Fehler).
- Zweck im Prompt verankert: der leere Zustand einer View („Noch keine Aufträge") statt
  eines leeren `markdown`.

### Skeleton-Zuordnung (Streaming)

`SkeletonFor`: `dataGrid`, `list`, `keyValue`, `image`, `code`, `accordion` → `Card`;
`kpi`, `form`, `emptyState` → `Text`.

## 3. Schema & Token-Budget (das benannte Risiko)

`CanvasPrompt.SchemaText` steht wörtlich in jeder Generierung; der Katalog wächst von 21
auf 30 Typen. Antwort in drei Teilen:

1. **Ein-Zeilen-Disziplin bleibt.** Jeder neue Typ bekommt genau eine Schema-Zeile im
   bestehenden Kompaktformat. Gemessen: das Schema wächst von ~2,3k auf ~3,4k Zeichen
   (~+280 Tokens) — spürbar, aber kein Kurzschema-Mechanismus nötig, solange die Zeilen
   diszipliniert bleiben.
2. **Budget-Wächter als Test.** Ein Unit-Test bricht, wenn (a) ein Katalogtyp ohne
   Schema-Zeile existiert oder umgekehrt (Abgleich `CanvasCatalog.AllTypes` ↔ SchemaText)
   oder (b) `SchemaText.Length` **4500 Zeichen** übersteigt. Wer den Katalog erweitert,
   verhandelt ab dann explizit mit diesem Test — das ist die Stelle, an der ein künftiges
   Kurzschema einzöge.
3. **Wahlhilfe statt Detailprosa.** Zwei Regelzeilen im Schema steuern die teuren
   Entscheidungen: `table` für kleine statische Tabellen (≤30 Zeilen), `dataGrid` für
   gebundene/größere Daten; `form` um Eingaben auf **eine** Aktion zu bündeln, statt
   einzelner Buttons je Feld.

Dazu: der Shape-Map-Satz in `CanvasDataPrompt` wird `rows -> table|dataGrid|list|keyValue`;
`LayoutBudget` bekommt `dataGrid`-Spaltenregeln (<480: max 3, <900: max 5, sonst max 8 —
identisch zu `table`) und „kpi: max 2 Kacheln unter 480px" als eine Zeile je Stufe.

## 4. Datenbindung (Kern)

`CanvasDataMapper`:

- `Allows(Rows)` → `table`, `dataGrid`, `list`, `keyValue` (keyValue zusätzlich mit
  2-Spalten-Prüfung in `Apply`, Fehlertext s. §2.5).
- Kappungsgrenzen je Typ: `table` 30 (Bestand), `dataGrid` 1000, `list` 50, `keyValue` 20 —
  alle mit dem bestehenden `truncated`-Hinweis.
- `Sample`/`ExpectedShape`/`AllowedTypes` entsprechend erweitert.

`CanvasDataBinder` bleibt unberührt — er kennt keine Typen, nur Keys.

## 5. Bewegung (A8-Verifikation)

- Alle neuen Nodes laufen durch `CanvasNodeView`s `DrylPresence` (Enter/Exit) — geschenkt.
- `dataGrid`-Refresh: Identität bleibt, neue `Items`-Referenz, Change-Pulse — kein Remount.
- `accordion`: Auf/Zu animiert `DrylExpansion`; `form`-Submit ist der Button-Beat aus
  Phase 2; `image` blendet über den vorhandenen Skeleton→Bild-Übergang ein.
- Keine neuen Animationen, Durations oder Easings.

## 6. Tests

- **Catalog:** je Typ gültig/ungültig (Pflichtfelder, Bereiche, Container-Regeln,
  `form`-required-Prüfung, `image`-src-Schutz, `accordion`-Label/Child-Parität).
- **Mapper:** Rows→dataGrid/list/keyValue inkl. Kappung + 2-Spalten-Regel.
- **Render (bUnit):** je Typ ein Happy-Path; `form`-Submit (Pflichtfeld leer → kein Invoke,
  gefüllt → Runner läuft); `dataGrid` sortiert/paged.
- **Prompt:** Schema↔Katalog-Abgleich, Budgetgrenze, Shape-Map-Zeile, LayoutBudget-Zeilen.
- **Replay:** ein Replay-Artefakt, das alle neun Typen enthält, rendert ohne
  Receipt-Fehler durch (Modell-Vertrag).

## 7. Website & Auslieferung

- `DemoAiCanvas`/Workspace-Demo: Replay-Beispiel „Auftrags-Cockpit" mit dataGrid, kpi,
  form + Aktion, accordion — Replay ohne Modell, Live-Variante hinter dem bestehenden Flag.
- `ComponentCatalog`: Beschreibungen der Canvas-Einträge um die neuen Typen ergänzt
  (keine neuen Komponenten-Einträge — es gibt keine neue öffentliche Komponente).
- `CHANGELOG.md`: `Added` unter Kern (Katalogtypen, Mapper-Erweiterung) und Agents
  (Schema-Zeilen, LayoutBudget); Release im selben Commit geschnitten.
- Versionen: Kern 2.14.1 → **2.15.0**, Agents 0.12.0 → **0.13.0**. Publishing: `publish.yml`
  veröffentlicht beide Pakete (Tags `v<ver>` + `agents-v<ver>`) — die Roadmap-Warnung ist
  seit dem Doppel-Publish-Workflow erledigt, kein Handlungsbedarf.
- DoD der Roadmap gilt vollständig (beide Farbmodi, 375 px, reduced-motion,
  `project_ai_canvas` fortschreiben).
