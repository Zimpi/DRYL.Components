# Phase 1 — Canvas Data Binding

**Datum:** 2026-07-25
**Status:** freigegeben (Brainstorming abgeschlossen)
**Rahmen:** `2026-07-25-canvas-platform-roadmap.md` — Phase 1 von 6
**Versionen:** Kern `2.11.0 → 2.12.0` · Agents `0.9.0 → 0.10.0` (⚠ breaking, siehe §7)

---

## 1. Ziel

Ein Canvas-Node bezieht seine Werte aus einer registrierten Host-Datenquelle statt aus Prompt-Text.
Damit hören Zahlen auf zu halluzinieren, veralten nicht mehr in der Sekunde nach der Generierung,
und die Token-Kosten entkoppeln sich von der Datenmenge.

Diese Phase enthält außerdem den **A1-Umzug** des Renderers in den Kern (§6) — Phase 1 fasst den
Katalog ohnehin an, ein separater Umzugs-Release wäre reine Reibung.

## 2. Entscheidungen dieser Phase

Zusätzlich zu den Roadmap-Entscheidungen A1–A9 wurden am 2026-07-25 mit dem Nutzer festgelegt:

| # | Entscheidung | Verworfene Alternativen |
| --- | --- | --- |
| **D1** | **Typisierte Ergebnisformen.** Eine Quelle liefert `Scalar`, `Series`, `Segments` oder `Rows`; der Binder mappt Form → Node-Props. Der Host denkt in Daten, nicht in Widgets. | Rohe Props als JSON zurückgeben (koppelt Quelle an genau einen Node-Typ); POCOs mit Projektionsangaben im Spec (das Modell müsste Property-Namen erraten — genau die Halluzinationsquelle, die wir loswerden) |
| **D2** | **Refresh über drei Wege:** deklaratives Intervall, manueller Knopf im Header, `Invalidate` durch den Host. | Nur Intervall+manuell; nur manuell |
| **D3** | **Parameter dürfen auf Formularfelder verweisen** (`{ "$field": "…" }`). Ein Select oben, und die abhängigen Nodes folgen — ohne AI-Turn. | Nur Literale (jede Filteränderung bräuchte `update_artifact`) |
| **D4** | **Parameter werden als C#-Record deklariert**, das Modell-Schema wird daraus abgeleitet. Ein Ort der Wahrheit, IntelliSense im Handler. | Dictionary + Prosa-Beschreibung; beide Wege parallel |
| **D5** | **Mandant/Benutzer kommen aus dem DI-Scope des Handlers, nie aus dem Spec.** Damit braucht es keinen zweiten Verweistyp (`$context`) im Schema, und das Modell kann den Mandanten nicht manipulieren. | `$context`-Verweise im Bindungsschema |
| **D6** | **Die Ergebnisform steht statisch in der Registry** (inferiert aus dem Rückgabetyp des Handlers). Dadurch fallen Form-Fehler schon bei der Validierung auf und landen im Receipt, statt erst beim Rendern sichtbar zu werden. | Form erst zur Laufzeit aus dem Ergebnis ableiten |

## 3. Öffentliche API

### 3.1 Registrierung

```csharp
public sealed record SalesParams(int Year, string? Region = null);

builder.Services.AddDrylCanvasDataSource("sales.byMonth",
    "Umsatz je Monat in Tsd €.",
    async (SalesParams p, CanvasDataContext ctx, CancellationToken ct) =>
    {
        var db = ctx.Services.GetRequiredService<AppDb>();
        var rows = await db.SalesAsync(p.Year, p.Region, ct);
        return CanvasData.Series(rows.Select(r => r.Month), ("Umsatz", rows.Select(r => r.Total)));
    });

// parameterlose Quelle
builder.Services.AddDrylCanvasDataSource("orders.open", "Offene Aufträge.",
    async (CanvasDataContext ctx, CancellationToken ct) => CanvasData.Rows(columns, rows));
```

Signatur (die Generics werden aus dem Lambda inferiert, der Aufrufer schreibt sie nie hin):

```csharp
public static IServiceCollection AddDrylCanvasDataSource<TParams, TData>(
    this IServiceCollection services,
    string name,
    string description,
    Func<TParams, CanvasDataContext, CancellationToken, Task<TData>> handler)
    where TParams : class
    where TData : CanvasData;

// parameterlose Überladung
public static IServiceCollection AddDrylCanvasDataSource<TData>(
    this IServiceCollection services,
    string name,
    string description,
    Func<CanvasDataContext, CancellationToken, Task<TData>> handler)
    where TData : CanvasData;
```

- **`ctx.Services`** ist der Scope des Circuits bzw. des Requests. Der Handler holt sich `DbContext`,
  `IHttpClientFactory` oder den angemeldeten Nutzer selbst (D5).
- **`name`** ist der Schlüssel im Spec. Konvention `bereich.sache` (kleingeschrieben, Punkt-getrennt);
  doppelte Registrierung wirft bei `AddDryl…` sofort, nicht erst zur Laufzeit.
- **`description`** ist reiner Modell-Text. Ein Satz, was die Quelle liefert und in welcher Einheit.
  Parameter werden **nicht** hier beschrieben — die kommen aus dem Record (D4).

### 3.2 Ergebnisformen

```csharp
public abstract class CanvasData
{
    public static CanvasScalarData  Scalar(double value, string? text = null,
                                           string? delta = null, string? direction = null);
    public static CanvasScalarData  Scalar(string text, string? delta = null, string? direction = null);
    public static CanvasSeriesData  Series(IEnumerable<string> labels,
                                           params (string Name, IEnumerable<double> Data)[] series);
    public static CanvasSegmentData Segments(IEnumerable<(string Label, double Value)> segments);
    public static CanvasRowData     Rows(IEnumerable<string> columns,
                                         IEnumerable<IEnumerable<string>> rows);
}
```

Mapping Form → Node-Typ → Props:

| Form | Zulässige Node-Typen | Gefüllte Props |
| --- | --- | --- |
| `CanvasScalarData` | `stat`, `badge`, `progress` | `stat`: `value`,`delta`,`direction` · `badge`: `text` · `progress`: `value` (0–100) |
| `CanvasSeriesData` | `lineChart`, `areaChart`, `barChart` | `labels`, `series` |
| `CanvasSegmentData` | `donutChart` | `segments` |
| `CanvasRowData` | `table` *(ab Phase 4: `dataGrid`, `list`)* | `columns`, `rows` |

Regeln:

- `Scalar` trägt Zahl **und** Text. `progress` verlangt die Zahl (die textlose Überladung an einem
  `progress` ist ein Validierungsfehler mit klarer Meldung); `badge` nimmt den Text; `stat` nimmt
  beides und tweent die Zahl über das vorhandene `DrylStat.CountUp`.
- Vom Modell im Spec gesetzte literale Props eines gebundenen Node werden vom Bindungsergebnis
  **überschrieben**, nicht gemerged. Nicht von der Form berührte Props (`title`, `valueFormat`,
  `label`) bleiben erhalten — sie sind Darstellung, nicht Daten.
- Ein `Rows`-Ergebnis mit mehr als 30 Zeilen wird am `table` abgeschnitten (die bestehende
  Katalog-Grenze) und der Node zeigt einen dezenten „gekürzt"-Hinweis. Ab Phase 4 löst `dataGrid`
  das mit Paging.

### 3.3 Bindung am Node

```json
{ "id": "chart", "type": "lineChart",
  "data": {
    "source": "sales.byMonth",
    "params": { "year": 2026, "region": { "$field": "region" } },
    "refresh": "interval:30s"
  } }
```

- `params`: Literal oder `{ "$field": "<name>" }` — Verweis auf ein interaktives Node desselben
  Artefakts (D3). Ein unbekannter Feldname ist ein Validierungsfehler.
- `refresh`: `"manual"` (Standard, weglassbar) oder `"interval:<n>s"` mit `n >= 5`. Kleinere Werte
  werden auf 5 s angehoben und im Receipt erwähnt.
- `data` ist optional. Ein Node ohne `data` verhält sich exakt wie heute (A2, Rückwärtskompatibilität).

Modell im Code (`DRYL.Components.Canvas`):

```csharp
public sealed class CanvasDataBinding
{
    public string? Source { get; set; }
    public JsonElement? Params { get; set; }
    public string? Refresh { get; set; }
}
// CanvasNode bekommt: public CanvasDataBinding? Data { get; set; }
```

### 3.4 Laufzeit-Dienst

```csharp
public interface ICanvasDataService                      // scoped
{
    void Invalidate(string source);                       // alle Bindungen dieser Quelle
    void Invalidate(string source, object parameters);    // nur passende Parameter
    IReadOnlyList<CanvasDataDescriptor> Descriptors { get; }
}

public sealed record CanvasDataDescriptor(
    string Name, string Description, CanvasDataShape Shape, IReadOnlyList<CanvasParamInfo> Params);

public sealed record CanvasParamInfo(string Name, string TypeName, bool Required);

public enum CanvasDataShape { Scalar, Series, Segments, Rows }
```

`Descriptors` ist zugleich die Quelle des Prompt-Blocks (§5) — ein Ort der Wahrheit für Host,
Renderer und Modell. Die Ableitung aus `TParams` geschieht per Reflection über die
Primärkonstruktor-Parameter des Records: Nullable oder mit Default ⇒ `Required = false`.

Unterstützte Parametertypen in `TParams`: `string`, `int`, `long`, `double`, `decimal`, `bool`,
`DateOnly`, `DateTime`, `Guid`, `enum` sowie deren Nullable-Varianten und `IReadOnlyList<T>` davon.
Ein nicht unterstützter Typ wirft bei der Registrierung — nicht erst, wenn das Modell ihn trifft.

## 4. Der Binder

`CanvasDataBinder` gehört **einer Canvas-Instanz**, nicht der Anwendung — wie `CanvasFormState`.
Er wird über denselben `IsFixed`-Cascade an `CanvasNodeView` gereicht. Zwei Canvases auf einer
Seite teilen sich nichts.

### 4.1 Zustand je Bindung

| Feld | Inhalt |
| --- | --- |
| Schlüssel | `source` + aufgelöste Parameter als kanonisches JSON (ordinal, Property-Reihenfolge stabil sortiert) |
| Zustand | `Idle → Loading → Ready(data)` oder `Error(message)` |
| Node-Ids | alle Nodes auf diesem Schlüssel — **das ist das Dedupe** |
| Feldnamen | die `$field`-Verweise, von denen der Schlüssel abhängt |
| Sequenz | laufende Nummer der aktuellen Ladung |

Drei Stat-Nodes auf `orders.open` ergeben einen Schlüssel und **einen** Aufruf.

### 4.2 Die vier Auslöser

1. **Erstladung** — `CanvasNodeView` meldet seine Bindung beim ersten Render an; der Binder lädt.
2. **Feldänderung** — `CanvasFormState.OnChanged` meldet den geänderten Feldnamen; nur Bindungen mit
   diesem `$field`-Verweis laden neu. **Debounce 300 ms**, sonst feuert jede getippte Taste in einem
   `inputText` eine Datenbankabfrage. Bewusst kein `--dur-*`-Token: das ist eine Eingabe-Ruhepause,
   keine Animation.
3. **Intervall** — **ein** Timer pro Canvas, der fällige Bindungen einsammelt; nicht einer je Node.
   `IDisposable`, im `DisposeAsync` abgeräumt (CLAUDE.md §6). Der Timer läuft nur, solange mindestens
   eine Intervall-Bindung existiert.
4. **Invalidate** — der Host meldet eine Änderung; passende Schlüssel werden veraltet und geladen.

Gegen Ladelawinen und Wettläufe: je Schlüssel läuft **höchstens eine** Ladung; eine neue bricht die
alte per `CancellationToken` ab; ein verspätet zurückkehrendes Ergebnis mit alter Sequenznummer wird
verworfen. `DisposeAsync` bricht alle laufenden Ladungen ab.

### 4.3 A8 — ein Refresh ist eine Bewegung, kein Neuaufbau

| Situation | Was der Nutzer sieht |
| --- | --- |
| **Erstladung** | `DrylSkeleton` in der Form des Node-Typs, dann Einblenden — wie beim Streaming |
| **Refresh mit neuen Werten** | **Kein Skeleton.** Der Node behält seine Identität, die Props werden gesetzt, der vorhandene **Change-Pulse** feuert — dieselbe Bewegung wie bei einem `setProps` der AI. Zahlen in `stat` zählen über `CountUp` hoch |
| **Refresh ohne Änderung** | Gar nichts. Kein Pulse, kein Render — sonst blinkt das Dashboard alle 30 s ohne Grund. Vergleich über das kanonische JSON des Ergebnisses |
| **Fehler, aber es gab schon einen guten Wert** | Der letzte gute Wert **bleibt stehen**, dazu eine kleine Fehlermarkierung mit Tooltip. „Kurz gestört" ist nicht „kaputt" |
| **Fehler ohne je einen guten Wert** | Kompakter Inline-Fehler an der Stelle des Inhalts. Ein defektes Widget darf nie das Dashboard sprengen |

Damit sieht eine Datenaktualisierung **identisch** aus wie eine AI-Änderung — eine einzige Sprache
für „hier hat sich etwas verändert", unabhängig davon, wer es verändert hat.

Fehlertexte für den Nutzer sind knapp und nennen die Quelle, nicht den Stacktrace
(„Quelle *sales.byMonth* nicht erreichbar"). Die Ausnahme selbst geht an `ILogger`.

### 4.4 Pulse-Vereinheitlichung

Der Change-Pulse hängt heute an `DrylCanvasRun.ChangeTickOf(id)`; die Klasse bleibt im Agents-Paket,
während `CanvasNodeView` in den Kern zieht. Nach dem Umzug gibt es **zwei** Pulse-Quellen (AI-Patch
und Daten-Refresh).

Lösung: **`CanvasPulseTracker`** im Kern, im Besitz von `DrylCanvas`. Sowohl der Binder als auch —
über den `DrylAiCanvas`-Wrapper — der Run melden ihre Stempel dorthin; `CanvasNodeView` liest nur
noch den Tracker. Eine Quelle der Wahrheit für „dieser Node hat sich gerade geändert", und der Kern
muss nichts vom Agents-Paket wissen.

### 4.5 Aktualisieren-Knopf

Der Canvas-Header bekommt neben Expand einen `↻`-Knopf, der alle Bindungen neu lädt — icon-only,
also mit `DrylTooltip` und `aria-label` (Regel 2.11), während des Laufs im Busy-Zustand. Er
erscheint nur, wenn das Artefakt mindestens eine Bindung hat.

## 5. Die AI-Seite

### 5.1 Prompt-Block

`CanvasPrompt` bekommt einen aus `Descriptors` erzeugten Block, der **nur** dann eingesetzt wird,
wenn die Registry nicht leer ist (A2 — sonst bleibt alles wie heute):

```
DATA SOURCES — bind nodes to these instead of writing numbers yourself:
- sales.byMonth(year: int, region?: string) -> series — "Umsatz je Monat in Tsd €."
- orders.open() -> rows — "Offene Aufträge."
Bind like this: "data": { "source": "<name>", "params": { … }, "refresh": "interval:30s"? }
A param is a literal, or { "$field": "<name of an interactive node in this artifact>" }.
Shapes map to types: scalar -> stat|badge|progress, series -> lineChart|areaChart|barChart,
segments -> donutChart, rows -> table.
Do NOT invent numbers when a matching source exists.
```

Der Block geht in `CreatePrompt` **und** `UpdatePrompt`.

### 5.2 Validierung

`CanvasCatalog.Validate` bekommt eine additive Überladung mit Kontext — die bestehende
parameterlose Signatur bleibt gültig:

```csharp
public static string? Validate(CanvasNode node);
public static string? Validate(CanvasNode node, CanvasValidationContext? context);
```

Geprüft wird, wenn ein Kontext vorliegt und der Node eine `data`-Bindung hat:

1. Quelle existiert. Sonst: „unknown data source '…' — available: …" (bis zu fünf Namen).
2. Form passt zum Node-Typ (Tabelle §3.2). Sonst: „source '…' returns rows, but a lineChart needs series."
3. Pflichtparameter vorhanden, Typen konvertierbar, keine unbekannten Parameter.
4. Jeder `$field`-Verweis zeigt auf ein `name` eines interaktiven Nodes **desselben** Artefakts.
5. `refresh` ist syntaktisch gültig und `>= 5s`.

Alle Befunde landen wie bisher als korrigierende Sätze im Receipt von `create_artifact` /
`update_artifact` — das Modell repariert sie im nächsten Zug selbst. Der bestehende Grundsatz gilt
weiter: **kein harter Abbruch**, ein invalider Node rendert als Platzhalter.

Da `Walk` in `DrylCanvasTools` bereits jeden Node validiert, ist der Einbau dort eine Zeile — der
Kontext wird aus `ICanvasDataService.Descriptors` plus den im Artefakt gefundenen Feldnamen gebaut.

## 6. Der A1-Umzug

Erster Task der Phase, **rein mechanisch**, in einem eigenen Commit, mit vorher/nachher identischer
Testmenge.

**In den Kern (`DRYL.Components/Canvas/`, Namespace `DRYL.Components.Canvas`):**
`CanvasSpec.cs` (`CanvasSpec`, `CanvasNode`, `CanvasJson`), `CanvasCatalog.cs` + Prop-Typen,
`CanvasNodeView.razor`, `CanvasPatch.cs`, `CanvasPatcher.cs`, `CanvasFormState.cs`,
`CanvasInteraction.cs`.

**Neu im Kern:** `DrylCanvas.razor` (Namespace `DRYL.Components`) — der dumme Renderer:
Root, Glass-Card, Kopfzeile (Titel · `HeaderTools`-Slot · Refresh · Expand), Body, die
Cascades (Spec, Form, Binder, Pulse-Tracker, Intent), `DrylEmptyState`, Fehler-Alert,
Fullscreen über `dryl.topLayer`.
Parameter: `Spec`, `OnInteraction`, `AllowExpand`, `Class`, `HeaderTools`, `AdditionalAttributes`.

**Bleibt im Agents-Paket:** `DrylAiCanvas.razor` — umhüllt `DrylCanvas` und ergänzt Run-Abo,
AI-Aura, `DrylAiIndicator`, Build-Progress, `aria-live`, Streaming-Reveal, View-Transition beim
Artefakt-Swap, Weiterleitung der Run-Stempel an den Pulse-Tracker.
Ebenso: `DrylCanvasRun`, `DrylCanvasTools`, `CanvasPrompt`, `CanvasStreamReveal`.

**Ebenfalls umziehen:** `wwwroot/js/dryl-canvas.js` → `_content/DRYL.Components/js/dryl-canvas.js`;
die Modul-URL in der Komponente mitziehen.

**Anzupassende Aufrufstellen:** `DRYL.Website/Components/ComponentCatalog.cs`,
`Components/Pages/DemoAiCanvas.razor`, `Components/Examples/Agents/CanvasArtifacts.razor`,
`Components/Examples/Agents/OpenAiCanvasArtifacts.razor` sowie `tests/DRYL.Components.Tests/Agents/`.
Das `_Imports.razor` des Agents-Pakets bekommt `@using DRYL.Components.Canvas`.

`[Obsolete]`-Aliase werden bewusst **nicht** angelegt (Roadmap §5.2, mit dem Nutzer abgestimmt).

## 7. Versionierung & Doku

- `DRYL.Components`: **2.11.0 → 2.12.0** (MINOR — rein additiv).
- `DRYL.Components.Agents`: **0.9.0 → 0.10.0** (MINOR = Bruchstelle in `0.x`).
- `CHANGELOG.md`: Einträge für beide Pakete unter `Added` (Data Binding) und `Changed`
  (Umzug, mit der Migrationszeile `using DRYL.Components.Canvas;`), Release im selben Commit
  geschnitten.
- `ComponentCatalog` in `DRYL.Website`: `DrylCanvas` als eigener Eintrag ergänzen,
  `DrylAiCanvas`-Eintrag um die Datenbindung erweitern.
- ⚠ `publish.yml` publiziert das Agents-Paket nicht automatisch mit — der Plan muss einen Task dafür
  vorsehen (Roadmap §5.3).

## 8. Nicht-Ziele

- Keine Abfragesprache, keine Joins, keine Client-Aggregation — der Host liefert fertige Ergebnisse.
- Keine gebundenen **Container** (Repeater über `Rows`). Das ist der `list`-Typ in Phase 4.
- Kein TTL-Cache. Es gibt Dedupe je Schlüssel und explizite Refresh-Wege, sonst nichts.
- Keine Aktionen/Commands — Phase 2.
- Keine neuen Durations, Easings oder Farben; der Refresh nutzt ausschließlich den bestehenden
  Change-Pulse und `CountUp`.

## 9. Tests

**Unit — Registry:** Deskriptor-Ableitung aus einem Record (Pflicht vs. optional über Nullable und
Default); nicht unterstützter Parametertyp wirft bei der Registrierung; doppelter Quellenname wirft;
erzeugter Prompt-Block enthält Name, Signatur, Form und Beschreibung.

**Unit — Binder:** Dedupe (zwei Nodes, gleicher Schlüssel ⇒ ein Handler-Aufruf); Feldabhängigkeit
(Änderung von `region` lädt nur die abhängige Bindung); Debounce fasst schnelle Änderungen zusammen;
verspätetes Ergebnis mit alter Sequenznummer wird verworfen; `Invalidate(source)` trifft nur passende
Schlüssel, `Invalidate(source, parameters)` nur den einen; Intervall-Timer wird bei `DisposeAsync`
abgeräumt; laufende Ladungen werden abgebrochen.

**Unit — Mapping:** jede Form auf jeden zulässigen Node-Typ (Props korrekt gefüllt); jede unzulässige
Kombination liefert die erwartete Fehlermeldung; `Scalar` ohne Zahl an `progress` schlägt fehl;
`Rows` über 30 Zeilen wird gekürzt.

**bUnit:** Erstladung zeigt Skeleton, danach Inhalt; Refresh mit neuen Werten zeigt **kein** Skeleton,
aber einen Pulse; Refresh ohne Änderung erzeugt weder Pulse noch Render; Fehler nach gutem Wert
behält den Wert und zeigt die Markierung; Fehler ohne guten Wert zeigt den Inline-Fehler;
Refresh-Knopf nur bei vorhandener Bindung, mit `aria-label` und Tooltip; `$field`-Änderung lädt genau
den abhängigen Node neu.

**Replay (Modell-Vertrag):** unbekannte Quelle, falsche Form am Node-Typ, fehlender Pflichtparameter,
`$field` auf ein nicht existierendes Feld, `interval:1s` — jeder Fall erscheint im Receipt mit einem
korrigierenden Satz, und das Artefakt rendert trotzdem.

**Umzug:** die bestehende Canvas-Testmenge läuft nach dem Namespace-Wechsel unverändert grün;
ein Test pinnt, dass `DrylCanvas` ohne jede Agents-Referenz rendert.

## 10. Risiken

| Risiko | Gegenmaßnahme |
| --- | --- |
| **Prompt-Wachstum** — viele Quellen ergeben einen langen Block in *jeder* Generierung | Deskriptor knapp halten (eine Zeile je Quelle); ab 40 Quellen eine `ILogger`-Warnung; die eigentliche Lösung ist die Katalog-Kompression aus Phase 4 |
| **Ladelawine** — Feldverweis, Intervall und `Invalidate` überlagern sich | Debounce, Dedupe je Schlüssel, höchstens eine laufende Ladung je Schlüssel, Sequenznummern |
| **Der Umzug berührt viel Oberfläche** | Eigener erster Commit, rein mechanisch, identische Testmenge vorher/nachher |
| **Langsame Quelle blockiert das Artefakt** | Jede Ladung läuft eigenständig und asynchron; ein hängender Node bleibt im Skeleton, alle anderen rendern normal |
| **Handler wirft und reißt den Circuit** | Jeder Handler-Aufruf ist in `try/catch` gekapselt; die Ausnahme wird zum Bindungsfehler und geht an `ILogger`, nie an den Renderer |
