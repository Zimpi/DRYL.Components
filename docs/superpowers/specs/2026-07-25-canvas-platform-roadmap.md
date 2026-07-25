# Canvas Platform — Roadmap & Architekturvertrag

**Datum:** 2026-07-25
**Status:** freigegeben (Brainstorming abgeschlossen)
**Art:** Nordstern-Dokument über sechs Phasen — **kein** Implementierungs-Spec

Dieses Dokument ist der geteilte Vertrag für den Ausbau von `DrylAiCanvas` zum zentralen Control
des DRYL-Agents-Frameworks. Jede der sechs Phasen bekommt ihre **eigene** Detail-Spec, ihren
eigenen Plan und ihren eigenen Chat. Was hier steht, ist der Rahmen, an dem sich jede dieser
Specs messen lassen muss.

Vorgänger-Specs (Phasen P/Q/W des Canvas selbst):
`2026-07-17-dryl-ai-canvas-design.md`, `2026-07-24-canvas-render-pipeline-design.md`,
`2026-07-24-canvas-robustness-design.md`, `2026-07-25-canvas-wow-design.md`.

---

## 1. Vision

> Eine Fachanwendungsseite besteht aus einem `DrylCanvasWorkspace`, der benannte Views hält, und
> einem `DrylCanvasDock` in der Ecke. Der Entwickler liefert Datenquellen und Commands — die
> Oberfläche entsteht.

Endbenutzer arbeiten in Businessanwendungen auf einer großen Canvas-Fläche und prompten über eine
Floating Card in der Ecke. Entwickler bauen keine Dashboards mehr von Hand; sie beschreiben, welche
Daten es gibt und welche Aktionen erlaubt sind.

Damit das ein *Standard* wird und kein Effekt, gilt: Der Canvas muss sich in jeder Anwendung gleich
anfühlen. Die Gewöhnung entsteht nicht aus der Featureliste, sondern aus **A8** (eine Zustands-
änderung = eine Bewegung) und aus der Verlässlichkeit von **A4** (die AI drückt nie selbst).

## 2. Ausgangslage (Stand 2026-07-25)

`DrylAiCanvas` ist ein exzellenter **Chat-Artifact-Renderer**: kuratierter Katalog (21 Typen),
Streaming-Reveal auf Leaf-Ebene, Patch-Ops mit FLIP-Glide, Change-Pulse, View-Transition-Morph,
Fullscreen über den Top-Layer, Layout-Budget nach gemessener Breite.

Er ist noch **keine Applikationsoberfläche**. Sechs Lücken trennen ihn davon:

1. **Daten sind Prompt-Text.** `CanvasPrompt.SchemaText` verlangt wörtlich, alle Zahlen in den Brief
   zu schreiben. Zahlen können halluzinieren, veralten sofort und skalieren nicht.
2. **Der Rückkanal ist Prosa.** `CanvasInteraction.ToPromptMessage()` macht aus jedem Klick eine
   Chat-Nachricht — untauglich für "Bestellung freigeben".
3. **Nichts überlebt einen Reload.** `Spec` lebt im RAM; kein Speichern, keine Version, kein Undo.
4. **Die Vision-Oberfläche existiert nicht als Komponente.** Workspace und Prompt-Dock sind heute
   Host-Code, den jeder Entwickler selbst schreibt.
5. **Der Katalog ist zu dünn.** `table` ist eine Markdown-Tabelle ohne Sortierung/Filter/Paging,
   max. 30 Zeilen. Es fehlen DataGrid, Formular, KPI-Reihe, Liste, Key-Value, Accordion, Bild, Code.
6. **Man kann nichts direkt anfassen.** Änderungen gehen nur über einen Prompt, der das ganze
   Artefakt meint — keine Selektion, kein Pin, kein Undo.

## 3. Querschnittliche Architekturentscheidungen

Diese neun Entscheidungen binden **alle** Phasen. Eine Phasen-Spec, die von ihnen abweichen will,
muss diese Roadmap ändern — nicht sich selbst eine Ausnahme schreiben.

### A1 — Der Renderer lebt im Kern

`CanvasSpec`, Katalog, `CanvasNodeView`, Patcher, Form-State und ein neuer, **dummer** `DrylCanvas`
(Spec rein, Interaktionen raus) ziehen nach `DRYL.Components`. `DRYL.Components.Agents` behält
`DrylAiCanvas` (= `DrylCanvas` + Run + Aura + Streaming + Fullscreen), `DrylCanvasTools`,
`CanvasPrompt`, `CanvasStreamReveal`, `DrylCanvasRun`.

*Konsequenz:* Eine Fachanwendung kann rendern, ohne das Agent-Framework zu laden. Ein `CanvasSpec`
darf aus Code, aus der Datenbank oder aus einem gespeicherten Dokument stammen — die AI ist ein
*Autor* des Specs, nicht seine Voraussetzung. Keine neue Abhängigkeit nötig: Markdig und die
Chart-Familie liegen bereits im Kern.

### A2 — Daten werden referenziert, nie inlined

Nodes tragen eine Bindung `data: { source, params, refresh }`. Der Host registriert benannte
Quellen; der Renderer löst sie auf und kann unabhängig von der AI refreshen.

*Rückwärtskompatibilität:* Literale Props (`labels`, `series`, `rows`, …) bleiben **gültig** —
sonst brechen alle bestehenden Chat-Artefakte und beide Website-Demos. Die Steuerung passiert im
**Prompt**: sobald der Host eine nicht-leere Quellen-Registry hat, wird das Modell angewiesen,
ausschließlich Quellen zu referenzieren. Registry ist die Regel für Fachanwendungen, inline bleibt
der Ad-hoc-Fall im Chat — ohne zwei konkurrierende Pfade in der Validierung.

### A3 — Das Modell sieht nie Rohdaten

Der Generator sieht Quellen-**Deskriptoren**: Name, Beschreibung, Parameter-Schema, Ergebnisform.
Nie Zeilen, nie Personendaten. Das ist zugleich ein Datenschutz- und ein Kostenargument und für
Fachanwendungen ein Verkaufsargument, kein Detail.

### A4 — Nur der Mensch löst aus

Die AI baut, beschriftet und belegt Buttons und Formulare vor — sie drückt sie nie. Schreibende
Commands laufen ausschließlich über eine bewusste Nutzeraktion im Artefakt. Nach einer Aktion
refresht der Canvas gezielt die betroffenen Quellen.

*Konsequenz:* klare Haftungsgrenze und eine Erwartung, die überall gleich gilt — was ich sehe,
passiert erst, wenn ich es will.

### A5 — Workspace = benannte Views, genau eine sichtbar

Der Workspace hält einen Satz benannter Artefakte ("Übersicht", "Auftrag 4711"); genau eines ist
groß sichtbar. Der Wechsel morpht über die bestehende View-Transition-Schicht. Der Nutzer kann zu
dem zurück, was er vorhin hatte — das ist der Unterschied zwischen Werkzeug und Chat-Spielzeug.

### A6 — Das Dock ist eine Befehlsleiste, kein Chat

Standard ist eine schlanke Eingabe plus **eine** Zeile Live-Status ("Baue Auftragsliste · 7
Elemente"). Das Artefakt ist die Antwort, nicht der Text. Der volle Verlauf klappt auf Abruf auf.
Der Blick bleibt auf dem Canvas.

### A7 — Persistenz ist Host-Sache; DRYL liefert den Vertrag

`ICanvasDocumentStore` + Serialisierung + eine In-Memory-Implementierung. **Kein Datenbankcode in
DRYL.** Der Host besitzt seine Daten weiterhin.

### A8 — Eine Op bleibt eine Bewegung

Die Leitregel aus Phase W gilt für alles Neue: Datenrefresh = Pulse/Count-Up statt Neuaufbau,
View-Wechsel = Morph, Aktion = ein Beat. Der Grund, warum sich das Ganze harmonisch anfühlt, ist
diese eine Regel — nicht die Summe der Features.

### A9 — Server *und* WASM

Datenquellen und Aktionen sind `Func<…, CancellationToken, Task<…>>` — auf WASM eben über HTTP.
Kein serverseitiges Konstrukt im Vertrag.

### Globale Nicht-Ziele

- Kein Drag-and-Drop-Designer als Ersatz fürs Prompten.
- Kein Code-Export ("gib mir das als Razor").
- Keine neuen Durations, Easings oder Farben — ausschließlich `--dur-fast|med|slow`,
  `--ease-out|in-out|spring|viscous` und die bestehende Akzent-/Glow-Familie.
- Keine npm-/JS-Abhängigkeit (CLAUDE.md §2.8).

---

## 4. Die sechs Phasen

```
1 Data Binding ──┬──> 2 Actions ──┬──> 3 Workspace + Dock ──> 6 Direct Manipulation
                 │                │                            ▲
                 └──> 4 Katalog ──┴──> 5 Document ─────────────┘
       (R Responsive — läuft unabhängig nebenher)
```

### Phase 1 — Canvas Data Binding

**Kern MINOR · Agents MINOR (breaking) · keine Abhängigkeit · enthält den A1-Umzug**

Ziel: Ein Node bezieht seine Werte aus einer registrierten Host-Quelle statt aus Prompt-Text.

```csharp
services.AddDrylCanvasDataSource("sales.byMonth",
    "Umsatz je Monat in Tsd €. Params: year (int, Pflicht), region (string?)",
    async (CanvasDataRequest req, CancellationToken ct) =>
        CanvasData.Series(labels, series));
```

```json
{ "id": "c1", "type": "lineChart",
  "data": { "source": "sales.byMonth", "params": { "year": 2026 }, "refresh": "interval:30s" } }
```

**Scope**

- `CanvasDataRegistry` + `CanvasDataBinder` (Kern).
- Drei katalognahe Ergebnisformen: `Scalar` (stat/progress/badge), `Series`/`Segments` (Charts),
  `Rows` (table/dataGrid).
- Bindungszustände: Loading (`DrylSkeleton`), Ready, Error (kompakter Inline-Fehler am Node —
  keine Alert-Wand, ein defektes Widget darf das Dashboard nicht sprengen).
- Dedupe: gleiche `source` + `params` innerhalb eines Artefakts ⇒ ein Aufruf.
- Refresh-Modi: manuell, Intervall, nach Aktion (Phase 2 zieht daran).
- Quellenkatalog wird in `CreatePrompt`/`UpdatePrompt` injiziert (Agents).
- Validierung unbekannter Quellen und falscher Parameter als korrigierender Receipt-Text
  (Bestandsmuster der Canvas-Tools).

**Nicht-Ziele:** keine Abfragesprache, keine Joins, keine Client-Aggregation. Der Host liefert
fertige Ergebnisse.

### Phase 2 — Canvas Actions

**Kern MINOR · Agents MINOR · braucht Phase 1 (gezielter Refresh)**

Ziel: `intent` wird ein typisierter Command statt Chat-Prosa.

```csharp
services.AddDrylCanvasAction("order.approve",
    "Gibt einen Auftrag frei. Args: orderId (string, Pflicht)",
    async (CanvasActionContext ctx, CancellationToken ct) => {
        await _orders.ApproveAsync(ctx.Get<string>("orderId"), ct);
        return CanvasActionResult.Ok("Auftrag freigegeben").Refresh("orders.open");
    });
```

**Scope**

- Button-Node bekommt `args` (statisch + aus dem Form-State), `confirm` (Text ⇒ `DrylDialog`),
  `kind: "danger"`.
- `CanvasActionResult`: Toast, Refresh-Liste, optionale Patch-Ops, optionales `.AskAi(…)`
  (**standardmäßig aus** — A4).
- Busy-Zustand am Button als ein Beat; Handler-Fehler landen inline, nie im Circuit.
- `OnInteraction` bleibt als Rückfallweg für Hosts ohne registrierte Aktion.

**Nicht-Ziele:** keine AI-ausgelösten Aktionen (A4); kein Undo für Commands — das ist Domänensache.

### Phase 3 — Workspace + Prompt Dock

**Workspace: Kern MINOR · Dock: Agents MINOR · braucht Phase 1+2**

Ziel: die Vision-Oberfläche als zwei Komponenten statt als Host-Code.

```razor
<DrylCanvasWorkspace Workspace="_ws">
    <View><DrylAiCanvas Run="_run" AllowExpand="false" /></View>
</DrylCanvasWorkspace>
<DrylCanvasDock Run="_run" Busy="Busy" OnSend="Send" Corner="DockCorner.BottomRight">
    <Log>@* die DrylMessages des Hosts *@</Log>
</DrylCanvasDock>
```

*(Präzisiert durch die Phase-3-Spec: die Views liegen in einem beobachtbaren `CanvasWorkspace`
statt in einer gebundenen Liste — sonst kann `open_view` sie nicht eröffnen; der Verlauf im Dock
kommt als Slot vom Host statt aus einem dock-eigenen Nachrichtenmodell.)*

**Scope**

- `DrylCanvasWorkspace`: benannte Views, genau eine sichtbar, Wechsel morpht über
  `IDrylViewTransition`; die View-Leiste erbt den gleitenden Indikator der Tabs.
- `DrylCanvasDock`: Floating Card auf Basis von `DrylChatComposer` + einzeiligem Live-Status aus dem
  Run + aufklappbarem `DrylChat`; kollabierbar zu einem Icon; auf Mobile volle Breite unten.
- Neues Tool `open_view(name, brief)`, damit die AI eine View eröffnen und aktivieren kann.

**Nicht-Ziele:** kein URL-/Routing-Sync (kann der Host), keine Mehrbenutzer-Synchronisierung.

### Phase 4 — Katalog-Ausbau

**Kern MINOR · Agents MINOR · braucht Phase 1; parallel zu 2/3 machbar**

Ziel: genug Bausteine, dass ein echtes Fachanwendungs-Dashboard nichts vermisst.

**Neue Typen:** `dataGrid` (datengebundenes `DrylTable` mit Sortierung, Filter, Paging — der große
Bruder des Markdown-`table`, das für kleine statische Tabellen bleibt) · `form` (Container mit
Submit auf **eine** Aktion, inkl. Validierung) · `kpi` (Reihe kompakter Stats) · `list` (Repeater
über `Rows`) · `keyValue` · `accordion` · `image` · `code` · `emptyState`.

**Nicht-Ziele:** keine Karte/Map (externe Abhängigkeit), kein Rich-Text-Editor.

**Risiko, das die Spec adressieren muss:** `CanvasPrompt.SchemaText` steht wörtlich in *jeder*
Generierung. Der Katalog verdoppelt sich nahezu — das Token-Budget braucht eine Antwort
(z.B. Kurzschema plus Detailbeschreibung nur für angeforderte Typen).

### Phase 5 — Canvas Document

**Kern MINOR · braucht Phase 4 (stabiles Schema)**

Ziel: Ein Dashboard überlebt Reload, Nutzerwechsel und Deployment.

**Scope**

- Serialisierung von Spec + View-Satz + Feld-Vorbelegungen, mit Schema-Version im Dokument.
- `ICanvasDocumentStore` (Save/Load/List/Delete) plus In-Memory-Implementierung — kein
  Datenbankcode in DRYL (A7).
- Versionshistorie über den ohnehin vorhandenen Op-Log; Undo/Redo; "auf Version zurück".
- Templates: ein gespeichertes Dokument als Startpunkt eines neuen — so liefert eine
  Fachanwendung ihre Standard-Dashboards aus.

**Nicht-Ziele:** keine Migration von Dokumenten über MAJOR-Schemagrenzen; ein zu altes Dokument
wird mit klarer Meldung abgelehnt.

### Phase 6 — Direct Manipulation

**Kern MINOR · Agents MINOR · braucht Phase 3+5**

Ziel: Der Nutzer kann anfassen statt nur beschreiben.

**Scope**

- Node-Selektion per Klick und Tastatur.
- Werkzeugleiste am selektierten Node: zu diesem Element prompten / anheften / duplizieren /
  entfernen.
- **Pin** (`locked: true`): der Patcher lehnt Ops auf angeheftete Nodes ab und sagt dem Modell im
  Receipt warum.
- Drag-Reorder über die bestehende FLIP-Schicht.
- Das Dock bekommt einen Kontext-Chip ("Umsatzchart"), der die Node-Id in den Brief legt.

**Nicht-Ziele:** kein freies Layout, kein Property-Inspector — geprompted wird weiterhin in Sprache.

### Sidequest R — Responsive

**Agents PATCH · unabhängig, jederzeit**

Der bereits geplante Rest aus der ursprünglichen Canvas-Roadmap: `.canvas-body` als
Container-Kontext, Donut responsiv.

---

## 5. Pakete, Versionen, Umzug

### 5.1 Paketgrenze nach dem A1-Umzug

Ausgangsstand: **Kern 2.11.0**, **Agents 0.9.0** (Agents referenziert den Kern per
`ProjectReference` und paketiert ihn als Dependency).

| `DRYL.Components` (Kern) | `DRYL.Components.Agents` |
| --- | --- |
| `Canvas/CanvasSpec.cs` (`CanvasSpec`, `CanvasNode`, `CanvasJson`) | `Canvas/DrylAiCanvas.razor` |
| `Canvas/CanvasCatalog.cs` + Prop-Typen | `Canvas/DrylCanvasRun.cs` |
| `Canvas/CanvasNodeView.razor` | `Canvas/DrylCanvasTools.cs` |
| `Canvas/CanvasPatch.cs`, `CanvasPatcher.cs` | `Canvas/CanvasPrompt.cs` |
| `Canvas/CanvasFormState.cs`, `CanvasInteraction.cs` | `Canvas/CanvasStreamReveal.cs` |
| **neu:** `DrylCanvas.razor` (dummer Renderer) | *(Phase 3)* `DrylCanvasDock` |
| *(Phase 1)* `CanvasDataRegistry`, `CanvasDataBinder` | |
| *(Phase 2)* `CanvasActionRegistry` | |
| *(Phase 3)* `DrylCanvasWorkspace` · *(5)* `ICanvasDocumentStore` | |

**Namensräume**, der bestehenden Konvention folgend: Komponenten in `DRYL.Components` (wie
`DrylTable`), das Typen-Subsystem in `DRYL.Components.Canvas` — analog zu `DRYL.Components.Motion`.
Das `_Imports.razor` des Agents-Pakets bekommt das `@using`.

**Ebenfalls mit umzuziehen:** `wwwroot/js/dryl-canvas.js`. Die Modul-URL ändert sich von
`_content/DRYL.Components.Agents/js/…` auf `_content/DRYL.Components/js/…`.

### 5.2 Der Umzug als Auftakt von Phase 1

Der Umzug ist **kein eigenes Projekt**, sondern Task 1 der Phase-1-Spec — Phase 1 fasst den Katalog
ohnehin an, ein separater Umzugs-Release wäre reine Reibung.

**Der Breaking Change wird angenommen, nicht kaschiert** (mit dem Nutzer am 2026-07-25 abgestimmt).
Öffentliche Typen wechseln Assembly *und* Namensraum; `[TypeForwardedTo]` hilft nicht, da es
gleiche Namen voraussetzt.

- **Agents 0.9.0 → 0.10.0.** In `0.x` ist MINOR die SemVer-Bruchstelle.
- **Kern 2.11.0 → 2.12.0** (MINOR, rein additiv — der Kern verliert nichts).
- **Migrationsnotiz** im `CHANGELOG.md` unter `Changed`, mit der einen Zeile, die Konsumenten
  brauchen: `using DRYL.Components.Canvas;` ergänzen.
- **Betroffene Aufrufstellen im eigenen Haus:** vier Dateien in `DRYL.Website`
  (`Components/ComponentCatalog.cs`, `Components/Pages/DemoAiCanvas.razor`,
  `Components/Examples/Agents/CanvasArtifacts.razor` + `OpenAiCanvasArtifacts.razor`) sowie
  `tests/DRYL.Components.Tests/Agents/`. Ein Commit, keine Migration.
- Bewusst **verworfen**: `[Obsolete]`-Aliase im Agents-Paket. Das Paket steht bei 0.9, hat wenige
  Konsumenten, und Alias-Schulden überleben erfahrungsgemäß bis 2.0.

### 5.3 Versionsfahrplan

| Phase | Kern | Agents |
| --- | --- | --- |
| 1 Data Binding *(inkl. Umzug)* | 2.11.0 → **2.12.0** | 0.9.0 → **0.10.0** ⚠ breaking |
| 2 Actions | → 2.13.0 | → 0.11.0 |
| 3 Workspace + Dock | → 2.14.0 | → 0.12.0 |
| 4 Katalog | → 2.15.0 | → 0.13.0 |
| 5 Document | → 2.16.0 | — |
| 6 Direct Manipulation | → 2.17.0 | → 0.14.0 |
| R Responsive | — | PATCH, wann immer |

Nach Phase 6 hat das Agents-Paket alles, was es für **1.0.0** braucht — das ist der natürliche
Schnitt.

> ⚠️ **In jede Phasen-Spec aufnehmen:** `publish.yml` publiziert das Agents-Paket nicht
> automatisch mit. Jede Phase, die Agents anfasst, muss klären, wie das Paket rausgeht — sonst
> liegen sechs Versionen ungeschnürt im Repo.

## 6. Definition of Done — gilt für jede Phase

1. `CHANGELOG.md` gepflegt, Release im selben Commit geschnitten, `<Version>` in beiden `.csproj`
   im Gleichschritt (CLAUDE.md §7.0/§7.1).
2. `ComponentCatalog` in `DRYL.Website` um neue Komponenten ergänzt.
3. Eine Demo-Seite, die die Phase **live** zeigt — Replay-Variante ohne Modell plus Live-Variante
   hinter dem bestehenden Umgebungs-Flag.
4. Tests: Unit für Registry-/Binder-/Patcher-Logik, bUnit für Renderpfade, Replay-Test für den
   Modell-Vertrag (inklusive der Receipt-Texte).
5. Beide Farbmodi geprüft, bei 375 px geprüft, `prefers-reduced-motion` geprüft.
6. `node scripts/check-light-sync.mjs` grün, falls Tokens dazukamen.
7. **A8 verifiziert:** jede neue Zustandsänderung ist eine Bewegung, kein Sprung.
8. Die Projektnotiz `project_ai_canvas` fortgeschrieben — sie ist der Gedächtnisfaden zwischen den
   Chats.

## 7. Startrezept für jeden Folge-Chat

> „Wir machen **Phase N — ⟨Name⟩** aus
> `docs/superpowers/specs/2026-07-25-canvas-platform-roadmap.md`. Lies die Roadmap und den
> bestehenden Canvas-Code, dann brainstorme die Detail-Spec."

Danach im selben Chat `/writing-plans`, und die Umsetzung in einem frischen Chat aus dem Plan
heraus.
