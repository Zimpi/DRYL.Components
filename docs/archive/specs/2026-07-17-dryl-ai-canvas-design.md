# DrylAiCanvas — Design Spec

**Datum:** 2026-07-17
**Status:** Entwurf, vom Maintainer im Brainstorming abgesegnet
**Paket:** `DRYL.Components.Agents` → Release **0.6.0** (MINOR; Stand bei Planerstellung: 0.5.0)

## 1. Was es ist

DrylAiCanvas bringt das Chat-Artifacts-Muster als wiederverwendbare Blazor-Komponente in DRYL:
Neben einem `DrylChat` entsteht eine Glass-Fläche, auf der die AI während der Konversation
lebendige, **voll interaktive** Kompositionen aus DRYL-Komponenten baut und iteriert — ein
Dashboard baut sich sichtbar Karte für Karte auf, "mach das Chart größer" wird zum animierten
Morph statt Neurendering, und Interaktionen im Artifact fließen als strukturierte Events zurück
an die AI.

**Kernentscheidungen (Brainstorming 2026-07-17):**

| Frage | Entscheidung |
| --- | --- |
| Primärer Use Case | Chat-Artifacts (gekoppelt an `DrylChat`/`DrylAgentRun`) |
| Interaktivität | Voll interaktiv; Events fließen zurück an die AI |
| Vokabular | Kuratierter, versionierter Katalog (~19 Typen) |
| Iteration | Patch-Ops + animierter Morph (FLIP, Aura auf geänderten Knoten) |
| Architektur | Hybrid: Chat-Tool triggert Structured-Streaming-Sub-Run |

## 2. Öffentliche API

Alles liegt in `DRYL.Components.Agents`. Integrationsaufwand für den Entwickler: drei Zeilen.

```razor
<div class="workspace">
    <DrylChat Run="_run" ... />
    <DrylAiCanvas Run="_canvas" OnInteraction="..." />
</div>

@code {
    private DrylCanvasRun _canvas = new();

    void Start()
    {
        var tools = DrylCanvasTools.Create(_canvas /*, optional: Datenkontext */);
        _run = Runner.Start(agent, prompt, tools: tools.All);
    }
}
```

### Bausteine

1. **`DrylAiCanvas`** (Razor) — Glass-Panel, rendert die live `CanvasSpec` rekursiv über den
   kuratierten Katalog. Parameter: `Run` (Pflicht), `OnInteraction`
   (`EventCallback<CanvasInteraction>`, optionales Abfangen/Veto), übliche `Class`-Merge-Param.
2. **`DrylCanvasRun`** — observable Handle, erbt `DrylRunBase` (wie `DrylArtifactRun<T>`).
   Hält: live `CanvasSpec`, `AiState`, `Round`, `ChangedIds` (für Aura-Puls), `Error`.
3. **`DrylCanvasTools`** — Tools für den Chat-Agenten:
   - `create_artifact(brief, title)` — startet intern den Generator-Sub-Run, wartet auf
     Abschluss, gibt Receipt zurück ("Artifact erstellt: 7 Elemente, 1 Interaktion").
   - `update_artifact(brief)` — wie oben, streamt Patch-Ops statt einer neuen Spec.
   Der Chat-Agent sieht die Spec nie — er delegiert nur (validate-and-ack-Pattern wie
   `DrylDisplayTools`).
4. **Spec-Modell** — `CanvasSpec` / `CanvasNode` / `CanvasPatch` (POCOs + Validierung).

## 3. Spec-Modell

ID-adressierter Baum; stabile `id`s sind Pflicht (Anker für Patches, FLIP, Events).

```json
{
  "title": "Umsatzanalyse Q2",
  "root": {
    "id": "root", "type": "stack", "props": { "gap": "md" },
    "children": [
      { "id": "kpis", "type": "grid", "props": { "columns": 3 }, "children": [
        { "id": "rev", "type": "stat", "props": { "label": "Umsatz", "value": "48.2k", "delta": "+12%" } }
      ]},
      { "id": "trend", "type": "lineChart", "props": { "labels": ["Apr","Mai","Jun"], "series": [] } },
      { "id": "ask", "type": "button", "props": { "label": "Nach Region aufschlüsseln", "intent": "breakdown-by-region" } }
    ]
  }
}
```

### Katalog (v1, ~19 Typen)

| Kategorie | Typen | Rendert als |
| --- | --- | --- |
| Layout | `stack`, `grid`, `card`, `divider`, `tabs` | `DrylStack`, `DrylGrid`, `DrylCard`, `DrylDivider`, `DrylTabs` |
| Anzeige | `markdown`, `stat`, `table`, `timeline`, `badge`, `progress` | `DrylMarkdown`, `DrylStat`, `DrylTable`, `DrylTimeline`, `DrylBadge`, `DrylProgress` |
| Charts | `lineChart`, `areaChart`, `barChart`, `donutChart` | Chart-Familie; Prop-Specs wiederverwendet aus `DisplaySpecs` |
| Interaktiv | `inputText`, `select`, `slider`, `toggle`, `button` | entsprechende DRYL-Inputs / `DrylButton` |

- Jeder Typ hat eine enge, validierte Prop-Spec.
- Unbekannter Typ / invalide Props → **Fallback-Karte** im Canvas + Fehler-Receipt an den
  Generator (Selbstkorrektur im nächsten Patch).
- `button` verlangt `label` (keine icon-only Buttons im Katalog — Regel 2.11 bleibt trivial erfüllt).

### Patch-Modell

`update_artifact` streamt eine Op-Liste; genau vier Op-Typen:

```json
{ "ops": [
  { "op": "setProps", "id": "trend", "props": { "height": "lg" } },
  { "op": "insert", "parent": "kpis", "index": 2, "node": {} },
  { "op": "remove", "id": "ask" },
  { "op": "move", "id": "trend", "parent": "root", "index": 0 }
]}
```

Jede Op wird **erst wenn sie vollständig ist** angewendet (nie halbe Ops), sequenziell mit
kurzem Stagger. Ops auf unbekannte IDs werden übersprungen und gesammelt ins Receipt
geschrieben.

## 4. Pipeline

### Create

1. `create_artifact(brief)` baut intern einen Generator-Agenten: System-Prompt = kompaktes
   Katalog-Schema (~1–2k Tokens, knappe Notation statt volles JSON-Schema) + Brief + optionaler
   Datenkontext vom Entwickler.
2. Sub-Run streamt `CanvasSpec` als Structured Output; der bestehende
   `PartialJsonReader`/`JsonPartialRepair`/`JsonMerge`-Stack erzeugt progressive Snapshots
   (Mechanik von `DrylAiBuild<T>`/`DrylArtifactRun`).
3. Renderer materialisiert Knoten, sobald `id` + `type` vollständig sind: bis die Props valide
   sind, steht dort ein `DrylSkeleton`-Shimmer in Zielgröße, dann morpht der Inhalt hinein.
4. Nach Abschluss: Receipt ans Tool, Chat-Turn läuft weiter. Snapshots werden gethrottlet wie
   bei `DrylAiBuild` (Reveal-Mechanik existiert).

### Update

Wie Create, aber Ziel-Schema ist die Op-Liste; Anwendung sequenziell-animiert (siehe Motion).
Geänderte Node-IDs landen in `ChangedIds` und pulsieren einmal mit der Aura.

### Interaktions-Loop

- Input-Knoten halten ihren Wert lokal im Renderer (`nodeId → value`).
- `button`-Klick erzeugt `CanvasInteraction { Intent, NodeId, Values }` (Values = Snapshot
  aller Input-Werte des Artifacts).
- **Default:** `DrylAiCanvas` speist das Event automatisch als strukturierte Nachricht in den
  laufenden Chat-Run ein; die AI antwortet und patcht typischerweise das Artifact. Währenddessen
  trägt das Artifact die Thinking-Aura.
- **Opt-out:** `OnInteraction` erlaubt Abfangen/Veto, bevor das Event zur AI geht.
- Kein HITL-Gate: Interaktionen sind nutzerinitiiert.

## 5. Motion & AI-Vokabular (Regeln 2.10 / 2.12 — nur Bestehendes)

- **Enter/Exit** pro Knoten über die Presence-Primitives (`--dur-med`, `--ease-spring`),
  beim Initial-Stream gestaggert.
- **Move-Ops** als FLIP-Glide über `dryl.motion`.
- **setProps** → einmaliger Aura-Puls; Charts animieren Werteübergänge ohnehin.
- **Canvas-Panel:** `Streaming` → `.ai-aura`-Ring + Comet; Abschluss → `Generated`
  One-Shot-Reveal; `prefers-reduced-motion` erledigen die Primitives.
- Keine neuen CSS-Primitives/Tokens erwartet; falls doch nötig → Vorschlag in `dryl.css`,
  Maintainer-Review (Regel 2.1).

## 6. Accessibility

- `aria-live="polite"` am Canvas: "Artifact wird erstellt", "Artifact aktualisiert, N Änderungen".
- Generierte Interaktiv-Elemente sind echte DRYL-Komponenten → keyboard-reachable, Focus-Ring.
- Buttons brauchen `label` (keine icon-only Fälle).

## 7. Fehlerbehandlung

- Sub-Run-Fehler → `Error != null` + `AiState.None` (bestehende Konvention); Canvas zeigt
  `DrylAlert` im Panel; Tool-Receipt meldet den Fehler an den Chat-Agenten.
- Invalide Knoten → Fallback-Karte + Selbstkorrektur-Receipt.
- Markdown-Knoten rendern durch `DrylMarkdown` (Surrogate-Stripping inklusive).
- Abbruch des Chat-Runs cancelt den Sub-Run mit.

## 8. Tests (`tests/DRYL.Components.Tests`)

Logik-lastig:

- Patch-Anwendung: alle vier Ops, unbekannte IDs, Reihenfolge.
- Node-Materialisierung aus partiellem JSON (Skeleton→Inhalt-Schwellen).
- Prop-Validierung + Fallback pro Katalogtyp.
- Interaction-Value-Snapshots.
- Fehler-Konvention (`Error != null` + `AiState.None`).

Dazu wenige bUnit-Rendertests: Knoten erscheint, Fallback-Karte, `aria-live`.

## 9. Demo & Doku

- Showcase auf der Agents-Seite der Website (Stil: Structured-Streaming-Showcase): Chat links,
  Canvas rechts, Szenario "Umsatzanalyse" mit klickbarem `intent`-Button; voller Loop
  (bauen → interagieren → morphen) als Replay ohne API-Key erlebbar.
- `ComponentCatalog`-Eintrag, Changelog unter `[Unreleased]`.

## 10. Versionierung

- Agents-Paket → **0.6.0**. Achtung: `publish.yml` veröffentlicht das Agents-Paket **nicht**;
  Release läuft separat wie bei den Vorversionen.
- Kern-Bibliothek (`DRYL.Components`) bekommt **eine** neue Motion-Primitive
  (`dryl.motion.autoFlip` für den Move-Glide) → Kern-MINOR-Bump.

## 11. Bewusst NICHT in V1 (YAGNI)

- Versionshistorie/Blättern zwischen Artifact-Ständen (natürlicher V2-Schritt).
- Mehrere Artifacts pro Canvas (ein `DrylCanvasRun` = ein Artifact; zwei gewünscht → zwei Runs).
- Entwickler-erweiterbarer Katalog (eigene Node-Typen) — größter V2-Hebel.
- Export (Spec → Razor-Code).

## 12. Risiken

1. **Prompt-Größe des Katalog-Schemas** — kompakt halten (~1–2k Tokens), knappe Notation.
2. **Generator-Disziplin bei IDs** — Sicherheitsnetz: Receipt-Feedback über übersprungene Ops.
3. **Blazor-Server-Renderlast beim Streaming** — Snapshot-Throttling wie bei `DrylAiBuild`.
