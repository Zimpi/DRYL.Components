# DrylAiCanvas — Phase Q: Robustheit & Qualität

**Datum:** 2026-07-24
**Status:** genehmigt, Implementierung wartet auf GO
**Vorgänger:** Phase P (`2026-07-24-canvas-render-pipeline-design.md`, abgeschlossen: Core 2.10.1 / Agents 0.8.1)
**Folgephasen:** W (Wow & Werkzeuge), R (Responsive-Härtung) — eigene Specs.

## Auslöser

Vier Quellen: (1) die in der Potenzial-Analyse identifizierten Robustheitslücken Q1–Q4, (2) ein Nutzer-Func-Test-Befund — AI-generierte Charts zeigen wörtliche `{value}`-Platzhalter in Achsen/Tooltips („€{value} Tsd80", „{value}% · {value}% %"), (3) zwei Follow-ups aus der Phase-P-Final-Review, (4) die bewusste Verschiebung von Q5 (Root-Swap-Exit) in Phase W, wo er via View-Transition sauber gelöst wird.

## Items

### Q1 — Echte Fehlerzustände statt Dauer-Skeleton

Ein Knoten, der nach Abschluss der Generation ungültig bleibt, zeigt heute ewig Skeleton + „waiting for {type}…" — sieht aus wie Laden, ist aber fertig-kaputt.

**Design:** `CanvasNodeView` unterscheidet am gecascadeten `Run.State`:
- Fehler + `Streaming`/`Thinking` → Skeleton + „waiting…" (unverändert).
- Fehler + `Generated`/`None` (inkl. nach `FailGeneration`) → kompakter Platzhalter `.canvas-invalid`: gedachte Border (`--line-strong`), `--fg-dim`, Radius `--r-md`, Padding `--sp-3`; Inhalt ist der Validierungsfehler (modellseitige Formulierung, technisch ehrlich — passt zur Dev-Tool-Oberfläche).

Der `update_artifact`-Pfad kann keine Invaliden erzeugen (Ops werden validiert/rollbacked) — betroffen ist nur der Create-Pfad, und dort kippt der Zustand bei `CompleteReveal`/`FailGeneration` korrekt um.

### Q2 — ID-Uniqueness im Create-Pfad

Doppelte IDs werden nur bei der `insert`-Op validiert; im Create-Pfad nicht — `@key`-Kollisionen und verwirrte `ChangedIds` sind die Folge.

**Design:** Der Abschluss-Walk in `CreateArtifactImpl` sammelt IDs in einem `HashSet<string>` (Ordinal); jedes Duplikat wird ein Eintrag in `problems` (modellseitiger Receipt: „duplicate id 'x' — ids must be unique across the artifact."). Bestehendes Muster: Receipt weist auf `update_artifact` zur Korrektur hin, kein harter Abbruch.

### Q3 — Cancel settle den Run

`catch (OperationCanceledException) { throw; }` in beiden Tool-Impls lässt den Run in `Streaming` hängen → die Aura zeigt ewig „Building".

**Design:** Neu `internal void CancelGeneration()` auf `DrylCanvasRun` (State → `None`, **kein** Error, `Raise()`); beide OCE-Catches rufen ihn vor dem Rethrow. Der Abbruch propagiert weiter an das Agent-Framework (bestehender Vertrag, siehe `Create_rethrows_cancellation_instead_of_returning_a_failure_receipt`).

### Q4 — Form-Reset bei neuem Artefakt

`_form` wird nur bei Run-Rebind zurückgesetzt; ein zweites `create_artifact` erbt alte Feldwerte, und weil `SeedFormOnce` nur seedet wenn der Name leer ist, **blockiert der Alt-Wert die KI-Vorgabe**. Zusatzfalle aus der P-Final-Review: `_seeded` (bool) überlebt Komponenten-Reuse bei Same-Id-Replacement — dieselbe Identitätsfalle wie beim Memo (Fix 6696ed7).

**Design:**
- `DrylCanvasRun`: `internal int ArtifactEpoch` (++ in `BeginCreate`).
- `CanvasFormState.Clear()` (**internal** — leert + feuert `OnChanged`; nur die Canvas ruft es, kein Public-API-Zuwachs → PATCH bleibt PATCH).
- `DrylAiCanvas` beobachtet die Epoch in `OnParametersSet` **und** `HandleChange` und ruft `_form.Clear()` bei Wechsel. Eine Form-Instanz bleibt (kein Ersatz — umgeht die `IsFixed`-Cascade-Falle).
- `SeedFormOnce`: `_seeded`-Bool → `_seededNode`-Referenz (`ReferenceEquals`), Seed läuft einmal **pro Knoten-Instanz**.

### Q5 — nach Phase W verschoben

Root-Swap-Exit-Animation wird von W2 (View-Transition beim Artefakt-Wechsel) abgedeckt. Keine Vorarbeit in Q.

### Q6 — `valueFormat` als `{value}`-Template (Func-Test-Befund)

`DrylChartBase.FormatValue` reicht `ValueFormat` blind an `double.ToString()` weiter. Modelle schicken Template-Strings (`"€{value} Tsd"`, `"{value}%"`) — Ergebnis: wörtliches `{value}` plus angehängte Zahl. Der Prompt dokumentiert `valueFormat` gar nicht.

**Design (Core):**
- `FormatValue` unterstützt Templates: enthält `ValueFormat` `{value}` → Ersetzung durch die formatierte Zahl; optionales Innenformat `{value:0.0}` (.NET-Formatstring). Ohne `{value}` → unverändert `v.ToString(ValueFormat)` (Back-Compat mit `"C0"`, `"N0"`). Kulturverhalten unverändert (culture-aware, wie das bestehende `"0.##"`). Malformed Templates (`{valueX}`, fehlende `}`) → Fallback auf `v.ToString(ValueFormat)`.
- `DrylDonutChart`: der **Prozentanteil** in Tooltip und aria-label wird nicht mehr über `ValueFormat` formatiert, sondern plain (`0.##`) — killt das `% %`-Doppel bei Prozent-Templates.

**Design (Agents):** `CanvasPrompt.SchemaText` dokumentiert `valueFormat` als Display-Template mit Beispielen („put `{value}` where the number goes, e.g. `€{value} Tsd` or `{value}%`") für beide Chart-Zeilen.

### Q7 — Shell-/Root-Props Final-Sync (P-Follow-up)

Eine als Shell enthüllte Tail-Node bekommt ihre eigenen Props beim `streamDone`-Flush nie final synchronisiert (nur Kinder werden geflusht); Root-Props sind an `!streamDone` gekoppelt. Zwischen letztem Streaming-Snapshot und striktem Final-Parse fertig gewordene Props (z.B. Card-Titel) bleiben alt.

**Design:**
- `CanvasStreamReveal.RevealChildren`, Complete-Zweig (`existing != null`): `PropsDiffer(existing.Props, s.Props)` → zuweisen + `Version++`. Modus-unabhängig sicher: per Referenz gefrorene Nodes sind dieselbe Instanz (PropsDiffer false → no-op); nur live-owned Shells abweichend.
- `Reveal`: Root-Props-Sync nicht mehr an `!streamDone` koppeln (sync + Bump wann immer abweichend).

## Nicht-Ziele

- Keine neuen Node-Typen, keine Toolbar/Wow-Effekte (Phase W), keine Responsive-Arbeit (Phase R).
- Kein harter Validierungs-Abbruch im Create-Pfad (Bestandsmuster: Receipt-Feedback).

## Tests

- **Q1:** bUnit — Invalider nach `CompleteReveal` zeigt `.canvas-invalid` (kein `.canvas-waiting`); invalider Komplett-Knoten während `Streaming` zeigt Skeleton. Bestandstest `Invalid_node_renders_skeleton_fallback` nutzt `ApplySnapshot` (State `None`) → wird auf den Reveal-Pfad umgestellt.
- **Q2:** Replay-Test — Create mit doppelter ID → Receipt enthält „duplicate id".
- **Q3:** Bestehende Cancel-Tests (Create + Update) asserten zusätzlich `run.State == AiState.None`.
- **Q4:** Unit — `BeginCreate` bumppt Epoch. bUnit — User-Edit → neues Create mit gleicher Node-Id/Feldname → AI-Vorgabe wird angezeigt (pinnt `Clear()` + Seed-Identität).
- **Q6:** Unit (Test-Subklasse von `DrylChartBase`) — `{value}`, `{value:0.0}`, Plain-Format, Malformed-Fallback. bUnit Donut — `ValueFormat="{value}%"` → kein `% %` in aria-label.
- **Q7:** Unit (CanvasStreamReveal) — Shell-Props werden im Complete-Zweig synchronisiert (+Bump); Root-Props beim Done-Flush.

## Versionierung & Doku

- Core **2.10.1 → 2.10.2** (Q6), Agents **0.8.1 → 0.8.2** (Q1–Q4, Q6-Prompt, Q7).
- `CHANGELOG.md`: Einträge unter `Fixed`/`Changed`, Release-Schnitt im selben Commit.
