# DrylAiCanvas — Phase P: Render-Pipeline & Performance

**Datum:** 2026-07-24
**Status:** genehmigt (Approach A1), bereit zur Implementierung
**Scope:** Nur Phase P. Folgephasen Q (Robustheit), W (Wow & Werkzeuge), R (Responsive-Härtung) bekommen eigene Specs.

## Problem

`CanvasNodeView` führt bei **jedem Render** drei teure Operationen pro Knoten aus:

1. `CanvasCatalog.Validate(Node)` ([CanvasNodeView.razor:30](../../DRYL.Components.Agents/Canvas/CanvasNodeView.razor)) — voller JSON-Parse + Regelprüfung.
2. `Props<T>()` — `JsonElement.GetRawText()` + `JsonSerializer.Deserialize` pro Aufruf.
3. Abgeleitete Daten (`ToSeries`/`ToSegments`/`ToItems`/`BuildTable`) werden pro Render neu allokiert.

Während des Streamings feuert `DrylCanvasRun.Raise()` pro Delta → alle bereits gerenderten Knoten parsen und validieren erneut: **O(Nodes × Deltas)** JSON-Parses. Zusätzlich parst `DrylMarkdown` (Core) in `OnParametersSet` bei jedem Parent-Render seinen kompletten Content neu durch Markdig — ohne Memo.

## Ziel

JSON-Parse, Validierung und Markdig laufen nur noch bei echter Mutation eines Knotens bzw. echter Content-Änderung — nie mehr pro Render. Keine öffentliche API-Änderung; das Frozen-Identity-Design (Presence-Keys, FLIP-Anker) bleibt unangetastet.

## Design

### 1. `CanvasNode.Version` (Agents, internal)

Neuer `internal int Version`-Zähler auf `CanvasNode`. Jede Mutation bumppt den betroffenen Knoten:

| Stelle | Mutation | Wer bumppt |
|---|---|---|
| `CanvasPatcher.ApplySetProps` | Props geändert | der Knoten |
| `CanvasPatcher.ApplyInsert` | Kinderliste geändert | der Elternknoten |
| `CanvasPatcher.ApplyRemove` | `Removing = true` | der Knoten |
| `CanvasPatcher.ApplyMove` | zwei Kinderlisten | alter **und** neuer Elternknoten |
| `DrylCanvasRun.Purge` | Kind fällt nach Exit-Animation aus der Liste | der Elternknoten |
| `CanvasStreamReveal.Reveal` (Root-Props) | Props-Zuweisung | `live.Root` |
| `CanvasStreamReveal.RevealChildren` (Tail-Props) | Props-Zuweisung | `liveTail` |
| `CanvasStreamReveal.RevealChildren` (beide `Children.Add`-Stellen) | Kind in Live-Liste aufgenommen | `liveParent` |

Eltern-Bump bei Kinderlisten-Änderung ist Pflicht: die `tabs`-Validierung (`labels.Count == children.Count`) hängt von den Kindern ab, nicht nur von den eigenen Props. Das gilt auch im Create-Streaming: ein als Shell enthüllter `tabs`-Container validiert bei 0 Kindern als ungültig (Skeleton) — ohne Bump beim ersten `Children.Add` bliebe er durch das Memo für immer im Skeleton stecken.

Der Bump erfolgt nur auf dem **Erfolgspfad** (nach bestandener Validierung) — ein zurückgerollter Op ändert nichts am Baum und darf keine Memos invalidieren.

### 2. Memo in `CanvasNodeView`

Private Felder:

```csharp
private int _memoVersion = -1;
private string? _memoError;      // Validierungsergebnis
private Type? _memoPropsType;
private object? _memoProps;      // deserialisierte Props
```

- `Validate(Node)` → nur wenn `Node.Version != _memoVersion`; Ergebnis in `_memoError`.
- `Props<T>()` → nur wenn Version oder angefragter Typ wechselt; Ergebnis in `_memoProps`.
- `ToSeries`/`ToSegments`/`ToItems`/`BuildTable` werden pro Render aus den **memoisierten** Props neu berechnet — billiges LINQ ohne JSON-Parse. Kein eigenes Memo für abgeleitete Daten (hält die Invalidierungslogik einfach).

### 3. Content-Memo in `DrylMarkdown` (Core)

`BuildSegments()` wird übersprungen, wenn `Content` seit dem letzten Aufruf unverändert ist (`_lastContent`-Feld, String-Vergleich — identische Referenz trifft den Schnellpfad). Profitieren: Canvas-`markdown`- und `table`-Knoten sowie jeder Chat-/Streaming-Host, der DrylMarkdown in re-rendernden Listen einsetzt.

### 4. Gestrichen: autoFlip-Coalescing

Der MutationObserver bündelt bereits pro Microtask; Streaming-Deltas treffen netzwerkbedingt >16 ms auseinander. Kein messbarer Gewinn → YAGNI.

## Tests

**Unit (Patcher/Reveal-Ebene):**
- `setProps` auf Knoten A bumppt `A.Version`, lässt `B.Version` unverändert.
- `insert`/`move` bumppt die Eltern-Version(en); `remove` die des entfernten Knotens; `Purge` die des Elternknotens.
- Zurückgerollter Op (Validierung schlägt fehl) bumppt **nichts**.
- `RevealSnapshot` mit geänderten Tail-Props bumppt die Live-Knoten-Version; unveränderte Props bumpen nicht; `Children.Add` im Reveal bumppt den Live-Elternknoten.

**bUnit (Renderer-Ebene):**
- Ein `setProps`-Patch auf Knoten A führt nicht zum Re-Parse von Knoten B (beobachtbar über unveränderte `Version` von B plus bestehendem Verhalten; die Memo-Logik selbst ist privat und wird über Versions-Semantik abgesichert).
- `DrylMarkdown`: zweites Rendern mit identischem `Content` parst nicht erneut (Seam: interner Parse-Zähler oder Verhaltensvergleich der Segmente).

**Regression:** Alle bestehenden Canvas-/Markdown-Tests bleiben grün.

## Versionierung & Doku

- `DRYL.Components.Agents`: **0.8.0 → 0.8.1** (PATCH — interne Optimierung, keine API-Änderung).
- `DRYL.Components`: **2.10.0 → 2.10.1** (PATCH — DrylMarkdown-Memo).
- `CHANGELOG.md`: Einträge für beide Pakete (`Changed`), Release-Schnitt im selben Commit.
- `ComponentCatalog` (DRYL.Website): keine Änderung — nicht nutzersichtbar.

## Nicht-Ziele

- Keine neuen Node-Typen (Phase F — verworfen zugunsten von R).
- Keine Fehlerzustands-Visuals (Phase Q).
- Kein Change-Pulse / View-Transitions / Toolbar (Phase W).
- Keine Donut-/Container-Query-Arbeit (Phase R).
