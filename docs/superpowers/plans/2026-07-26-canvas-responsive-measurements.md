# Sidequest R — Messprotokoll

**Datum:** 2026-07-26 · **Werkzeug:** Playwright gegen `http://localhost:5044`
**Messfläche:** `/dev/canvas-responsive` (Fixture aus Task 3, danach entfernt) mit zwei Canvas-
Instanzen derselben Spec: `#wide` in voller Breite, `#narrow` in einem 320-px-Slot.
Alle Werte in CSS-Pixeln, `getBoundingClientRect` bzw. `scrollWidth`/`clientWidth`.

## 1. Donut — vorher / nachher

Gemessen auf `/components/donut-chart`, Slot auf 200 px gezwungen, `--chart-h: 220px`.

| Zustand | Slot | Rad (B × H) | Überstand links | Überstand rechts |
| --- | --- | --- | --- | --- |
| vor dem Fix (Regel per JS zurückgesetzt) | 200 | 220 × 220 | **10** | **10** |
| nach dem Fix | 200 | **200 × 200** | 0 | 0 |

Im Canvas, `--chart-h: 260px`:

| Slot | Chart-Inhaltsbreite (Container) | `.donut-box` | Rad | Überstand |
| --- | --- | --- | --- | --- |
| `#wide` (Node 514) | 482 | 260 px | 260 × 260 | keiner |
| `#narrow` (Node 286) | **254** | **254 px** | 254 × 254 | keiner |

`min(260px, 100cqw)` löst im schmalen Slot auf die Containerbreite auf — genau wie entworfen.

## 2. Überlauf im Canvas — 375 × 812 und 320-px-Slot

**Vor dem Fix**, Nodes mit `scrollWidth > clientWidth` (ohne `.tbl-wrap`, das absichtlich scrollt):

| Slot | Node | scrollW | clientW | weitester Ausreißer |
| --- | --- | --- | --- | --- |
| wide | `root` / `cols` / `donut` | 314 | 297 | `.chart-tip.donut-tip`, 16,4 px über die Chart-Kante |
| narrow | `root` / `cols` / `donut` | 306 | 286 | `.chart-tip.donut-tip`, 20,0 px |
| narrow | `line` | 292 | 286 | `.chart-tip`, 6,1 px |

Und am Body selbst:

| Slot | `scrollWidth` | `clientWidth` | mit `.chart-tip { display:none }` |
| --- | --- | --- | --- |
| wide | 330 | 329 | **329 / 329** |
| narrow | 322 | 318 | **318 / 318** |

**Befund:** Chart-Tooltips werden auch im verborgenen Zustand gelayoutet und waren die
*alleinige* Ursache des horizontalen Scrollbereichs im Canvas-Body. Der Nutzer bekam eine
Scroll-Möglichkeit zu einer Blase, die der Canvas an seiner eigenen Kante ohnehin abschneidet.

**Fix:** `.canvas-body { overflow-x: hidden; overflow-y: auto; }` — der Canvas scrollt als
Ganzes nie seitwärts; ein Widget mit horizontalem Bedarf scrollt in sich selbst.

**Nach dem Fix** (`overflow-x` computed `hidden`, keine Scrollleiste, kein Node über der
Body-Kante):

| Viewport | Slot | clientW | Nodes über der Body-Kante | horizontale Scrollleiste |
| --- | --- | --- | --- | --- |
| 375 × 812 | wide | 329 | 0 | nein |
| 375 × 812 | narrow | 318 | 0 | nein |
| 1440 × 900 | wide | 1072 | 0 | nein |
| 1440 × 900 | narrow | 318 | 0 | nein |

Der 320-px-Slot misst bei beiden Viewports identisch (318/318) — die Regeln hängen am
Container, nicht am Viewport.

## 3. Gegenprobe zur Containment-Frage

`container-type` macht `.canvas-body` zum enthaltenden Block für `position: fixed`-Nachfahren.
Direkt geprüft, mit einer `position: fixed; top: 0; left: 0`-Sonde im Body:

| Sonde | erwartet ohne Containment | gemessen |
| --- | --- | --- |
| `left` | 0 | **66** (= linke Kante des Body) |

Das Risiko ist also **real** — und trifft die beiden Overlays der Bibliothek trotzdem nicht,
weil sie keine Nachfahren des Body sind:

| Overlay | Elternknoten | Position | Sitz relativ zum Trigger |
| --- | --- | --- | --- |
| `DrylTooltip` (`.tt-portal`) | `BODY` | fixed | Mitte auf Mitte (Δx 0), 8,5 px darüber |
| `DrylSelect`-Panel (`.popover-panel.is-open`) | `BODY` | fixed | Δx 0, 4 px unter dem Trigger, Breite gleich (263,2) |

Beide gemessen in einem Canvas bei 375 px, mit aktivem Container-Kontext.

## 4. Node-Werkzeugleiste

Die Leiste erscheint nur mit `CanvasSelection`, die die Fixture nicht setzt; ihre Geometrie
wurde deshalb mit einer Sonde derselben Klasse (`.canvas-node-tools.glass-card`, 132 × 32) an
einem Node gemessen:

| Slot | rechte Kante der Leiste | rechte Kante des Body | vom Canvas beschnitten |
| --- | --- | --- | --- |
| wide | 366,2 | 378,2 | nein (12 px Luft) |
| narrow | 355,0 | 367,0 | nein (12 px Luft) |

Der Überhang von `--sp-1` landet im `--sp-4`-Padding des Body — kein Fund, keine Regel nötig.

## 5. Farbmodi und `prefers-reduced-motion`

Derselbe Lauf bei 375 px in dunkel, hell und mit `prefers-reduced-motion: reduce`:

| Lauf | wide (clientW / Nodes über Kante / Rad) | narrow |
| --- | --- | --- |
| dunkel | 329 / 0 / 260 | 318 / 0 / 254 |
| hell | 329 / 0 / 260 | 318 / 0 / 254 |
| reduced motion | 329 / 0 / 260 | 318 / 0 / 254 |

Identisch — die Fixes sind Layout, nicht Farbe und nicht Bewegung.

## 6. Beobachtet, nicht gefixt

- **`table` staucht, `dataGrid` scrollt.** Der `table`-Node bei 375 px: fünf Spalten auf
  47–88 px, Zeilenhöhe 103 px statt 81 px — der Text bricht mehrzeilig um, aber nichts läuft
  über. `dataGrid` hat für genau diesen Fall eine Spaltenuntergrenze
  (`.canvas-grid .tbl { min-width: cols × 110px }`) und scrollt in `.tbl-wrap`; der einfache
  `table`-Node hat keinen solchen Wrapper. Die Asymmetrie ist echt, aber sie ist kein Überlauf
  und ihre Behebung bräuchte einen Scroll-Wrapper in `DrylTable` — eigenes Thema, eigener PR.
- **Chart-Tooltips ragen weiterhin über ihre Chart-Kante** (bis 20 px auf einem 286-px-Donut).
  Sichtbar nur im Hover, und der Canvas beschneidet die Blase an derselben Linie wie zuvor.
  Eine Platzierung, die die Blase in den Chart zurückholt, ist Arbeit an der Chart-Familie
  (Anker in Chart-Koordinaten statt in Slice-Koordinaten) — kein PATCH-Thema.
- **Kein Fund im `DrylCanvasDock`.** Bei 375 px auf `/components/canvas-dock` gemessen: das Dock
  ist 351 px breit und bleibt mit 12 px Abstand innerhalb des Viewports, `dock-panel`
  350 / 349, Kopfzeile und beide Icon-Buttons innerhalb der Kante. Das Agents-Paket bleibt
  unangetastet bei 0.14.0.
