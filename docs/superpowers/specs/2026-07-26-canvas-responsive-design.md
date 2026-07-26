# Sidequest R — Responsive-Härtung des Canvas — Design

**Datum:** 2026-07-26
**Status:** Freigegeben (Brainstorm), Plan folgt
**Scope:** Sidequest R aus `2026-07-25-canvas-platform-roadmap.md` — `.canvas-body` als
Container-Kontext, Donut responsiv, plus die Fixes, die ein gemessener 375-px-Durchlauf belegt.

---

## 1. Ziel

Ein Canvas-Artefakt bleibt in **jedem** Slot heil: im schmalen Chat-Strang, in einer Grid-Zelle,
auf 375 px. Reagiert wird auf die Breite des **Canvas**, nie auf die des Viewports — der Canvas
morpht zwischen inline und Vollbild, eine Media Query wäre dort strukturell falsch.

**Nicht-Ziele:** kein neuer Breakpoint, kein neues Token, keine neuen Node-Typen, keine
Layout-Neuerfindung. Fixes sind CSS-only; Markup wird nur angefasst, wo CSS es nachweislich nicht
kann.

## 2. Ausgangsbefund

| Befund | Stelle | Bewertung |
| --- | --- | --- |
| Der Donut ist immer 260 px **breit**, unabhängig vom Slot | `dryl.css` `.donut-box` (`height: var(--chart-h, 260px)`) + `.donut-slice` (`top/bottom: 0; aspect-ratio: 1`) | echte Bug-Klasse: jeder Container < 260 px bekommt ein überlaufendes Rad |
| `.canvas-body` ist kein Container-Kontext | `DrylCanvas.razor.css` | Canvas-eigene Regeln können nicht auf die Canvas-Breite reagieren |
| Tooltip / Popover portalisieren nach `<body>` | `dryl.js` `dryl.tooltip`, `dryl.popover` | entlastet das Hauptrisiko von `container-type` (siehe 3.1) |
| `DrylGrid`, `DrylStack`, `DrylPagination`, `DrylDescriptionList` bringen je einen eigenen `.cq`-Wrapper mit | `dryl.css` `.cq` | zwingt den Canvas-Kontext zu einem **Namen** (siehe 3.1) |

## 3. Bausteine

### 3.1 `.canvas-body` wird benannter Container

```css
.canvas-body { container: canvas / inline-size; }
```

**Benannt**, nicht anonym: eine anonyme `@container`-Abfrage trifft den *nächstgelegenen*
Vorfahr-Container. Da vier Primitive ihren eigenen `.cq`-Wrapper mitbringen, würde eine
Canvas-Regel in Node-Tiefe von diesem Wrapper abgefangen. `@container canvas (…)` greift
verlässlich über die Zwischenschicht hinweg.

**Risiko und warum es hier keines ist:** `container-type` erzeugt Containment und damit einen
enthaltenden Block für `position: fixed`-Nachfahren. Tooltip-Bubble und Popover-Panel werden
jedoch nach `<body>` portalisiert und liegen gar nicht unter `.canvas-body`. Das Vollbild
(`.canvas.is-expanded`, Top-Layer via `popover="manual"`) sitzt **über** dem Body, ist also
ebenfalls nicht betroffen. Beides wird in 3.3 gegengemessen, nicht nur behauptet.

Inline-Size-Containment heißt außerdem: die Breite von `.canvas-body` darf nicht mehr vom Inhalt
abhängen. Sie kommt aus der Flex-Spalte `.canvas` — unverändert.

### 3.2 Der Donut skaliert

```css
.chart-kind-donut { container-type: inline-size; }
.donut-box        { height: min(var(--chart-h, 260px), 100cqw); }
```

Das Rad ist damit `min(gewünschte Höhe, verfügbare Breite)` statt starr 260 px. Die Slices bleiben
unverändert (`aspect-ratio: 1` auf die neue Höhe), Tooltip-Anker (`--tip-top`/`--tip-left`) und
`--donut-hole` sind prozentual und wandern mit. `container-type` sitzt auf dem Chart-Root, nicht
auf `.donut-box` selbst — ein Element ist nie sein eigener Container.

Das ist eine **bibliotheksweite** Korrektur an `DrylDonutChart`, nicht nur an der Canvas-Instanz:
jeder Donut in einer schmalen Karte profitiert. Changelog-Rubrik `Fixed`.

**Bewusst ohne Mindestgröße.** Ein sehr schmaler Slot bekommt ein kleines Rad — das ist die
ehrliche Antwort, besser als ein Überlauf oder eine Scrollbar. Die Legende umbricht bereits
(`.chart-legend` ist `flex-wrap: wrap`). `CenterContent` bleibt Sache des Konsumenten; die globale
Sicherheitsschicht (`overflow-wrap: anywhere`) fängt lange Wörter.

### 3.3 Der gemessene Durchlauf

Kein Augenmaß. Per Playwright wird der Canvas in zwei Engpass-Situationen vermessen:

- **A — Telefon:** Viewport 375 × 812, Canvas inline.
- **B — schmale Spalte:** Canvas in einem Slot von ~320 px bei desktopbreitem Viewport. Belegt,
  dass die Regeln am Container hängen und nicht am Viewport.

Gemessen wird pro Node-Typ des Katalogs:

1. `scrollWidth > clientWidth` auf `.canvas-body` und auf jedem `.canvas-node` (horizontaler
   Überlauf), mit der erlaubten Ausnahme `.tbl-wrap`, das absichtlich selbst scrollt.
2. Bounding-Box jedes `.canvas-node`-Kindes gegen den Rahmen von `.canvas-body` (seitliches
   Herausragen, das `overflow: hidden` am `.canvas` sonst still abschneidet).
3. Gegenprobe zu 3.1: ein geöffneter Tooltip und ein geöffnetes Select-Panel innerhalb des Canvas
   landen weiterhin an ihrem Trigger (Bounding-Boxen vergleichen).

Was überläuft, wird gefixt — mit `@container canvas (…)`, wo die Canvas-Breite die Ursache ist.
Verdächtige, die der Durchlauf bestätigen oder entlasten muss: die Node-Werkzeugleiste
(`.canvas-node-tools`, negativ versetzt in der Ecke), die Kopfzeile (`.canvas-head`, Titel +
Werkzeuge in einer `space-between`-Zeile), `dataGrid`, `timeline`, `keyValue`.

Der Durchlauf wird in **beiden Farbmodi** und mit `prefers-reduced-motion: reduce` wiederholt; die
Messwerte kommen als Tabelle in den Plan-Abschluss.

## 4. Tests

bUnit kann kein Layout messen — die Absicherung ist zweigeteilt:

- **CSS-Verträge** als xUnit-Assertions gegen `dryl.css` und `DrylCanvas.razor.css`, nach dem
  bestehenden Muster von `Theming/DrylCssDerivationTests`: der Container-**Name** `canvas`, die
  `min()`-Zeile am Donut und `container-type` am Donut-Root sind damit gegen stilles
  Wegrefaktorieren gesichert.
- **Layoutbeweis** durch den Playwright-Durchlauf aus 3.3, dokumentiert mit Zahlen.

Kein neuer bUnit-Test für ungeänderte Renderpfade.

## 5. Version, Pakete, DoD

- **Kern 2.17.0 → 2.17.1** (PATCH). Beide Bausteine liegen nach dem A1-Umzug im Kern
  (`Components/AI/DrylCanvas.razor.css`, `wwwroot/dryl.css`).
- **Agents unverändert** — damit stellt sich die `publish.yml`-Frage aus der Roadmap hier nicht.
  Falls der Durchlauf doch einen Bruch im `DrylCanvasDock` findet, kommt ein Agents-PATCH dazu und
  die Paketfrage in den Plan.
- Die Roadmap führt R als „Agents PATCH“; das stammt aus der Zeit vor dem Umzug. **Korrigiert:
  Kern-PATCH.** Die Roadmap-Tabelle wird entsprechend nachgezogen.
- `CHANGELOG.md`: `Fixed`-Einträge für `DrylDonutChart` und `DrylCanvas`, Release im selben Commit.
- Keine neuen Tokens → `scripts/check-light-sync.mjs` bleibt unberührt, wird aber gefahren.
- `ComponentCatalog`: keine neue Komponente, kein Eintrag nötig. Kein neues Sample — die
  bestehende Canvas-Demoseite ist die Messfläche.
- A8: die Änderung ist rein statisch (Größenbestimmung), keine neue Zustandsänderung, also keine
  neue Bewegung zu belegen.
- `project_ai_canvas` wird fortgeschrieben.
