# Canvas Phase 5 — Canvas Document (Detail-Spec)

**Datum:** 2026-07-26
**Status:** freigegeben
**Rahmen:** Phase 5 aus `2026-07-25-canvas-platform-roadmap.md` — bindend sind A1–A9 und die
globalen Nicht-Ziele. Ausgangsstand: Kern **2.15.0**, Agents **0.13.0**.
**Zielversionen:** Kern → **2.16.0** (MINOR). **Agents unverändert** — Phase 5 fasst das
Agents-Paket nicht an, damit entfällt auch die Publish-Frage aus der Roadmap-Warnbox.

## 1. Ziel

Ein Dashboard überlebt Reload, Nutzerwechsel und Deployment — und der Nutzer kann einen Schritt
zurück.

Drei Bausteine, alle im Kern:

1. **`CanvasDocument`** — serialisierbarer Schnappschuss eines ganzen `CanvasWorkspace`
   (Schema-Version, Titel, alle Views mit Spec, aktive View).
2. **`CanvasHistory`** — Snapshot-Ring **pro View**: Undo, Redo, „auf Version zurück".
3. **`ICanvasDocumentStore`** + In-Memory-Implementierung — der Vertrag, kein Datenbankcode (A7).

Dazu die sichtbare Seite: `DrylCanvasWorkspace` bekommt optional Undo/Redo und einen
Verlaufs-Popover in seiner View-Leiste, plus optionales Autosave gegen den registrierten Store.

**Nicht-Ziele**

- Keine Dokument-Migration über Schemagrenzen — ein zu neues Dokument wird mit klarer Meldung
  abgelehnt (Roadmap).
- Kein Undo für Host-Commands (Phase 2) — Domänensache.
- Kein Multi-User, kein Konflikt-Merge, kein Routing-/URL-Sync.
- Kein Persistieren des **live getippten** Formularstands unter `DrylAiCanvas` — siehe §4.3.
- Keine neue Komponente, keine neuen Tokens, keine neuen Durations/Easings/Farben.

## 2. Befund vorab: es gibt keinen Op-Log

Die Roadmap nimmt an, die Versionshistorie ließe sich „über den ohnehin vorhandenen Op-Log"
bauen. Den gibt es nicht: `CanvasPatcher.Apply` mutiert den Spec **in place** und liefert nur
einen Fehlergrund zurück; der Create-Pfad (`DrylCanvasRun.CompleteGeneration`) ersetzt den Spec
komplett. Ein Op-Log müsste erst gebaut werden *und* bräuchte für den Create-Pfad trotzdem
Snapshots — zwei Mechanismen für ein Ergebnis.

**Entscheidung:** Snapshot-Ring. Nach jeder abgeschlossenen Runde legt der Workspace den Spec
der aktiven View als JSON in einen Ringpuffer. Undo/Redo/„auf Version zurück" spielen einen
Eintrag zurück. Das deckt Create- und Update-Pfad mit demselben Code, ist exakt, und ein
Eintrag ist ohnehin genau das, was ein Dokument speichert — beide Features teilen eine
Serialisierung.

## 3. `CanvasDocument` — was ein Dokument ist

Neue Datei `DRYL.Components/Canvas/CanvasDocument.cs`, Namensraum `DRYL.Components.Canvas`.

```csharp
public sealed class CanvasDocument
{
    public const int CurrentSchema = 1;

    public int Schema { get; set; } = CurrentSchema;
    public string? Id { get; set; }                       // Store-Schlüssel; null = noch nie gespeichert
    public string? Title { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public List<CanvasDocumentView>? Views { get; set; }
    public string? ActiveId { get; set; }
}

public sealed class CanvasDocumentView
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public CanvasSpec? Spec { get; set; }
}
```

Serialisiert wird mit `CanvasJson.Options` — dieselbe camelCase-Web-Konvention wie jeder Spec.
`CanvasNode.Removing` und `CanvasNode.Version` sind bereits `[JsonIgnore]`; Datenbindungen
(`data`) und Aktionsbindungen (`action`) **werden mitgespeichert** — sie sind Referenzen auf
registrierte Host-Namen, keine Daten (A2/A3). Ein geladenes Dokument holt sich seine Zahlen beim
Rendern selbst.

### 3.1 API

```csharp
public static CanvasDocument Capture(CanvasWorkspace workspace,
                                     string? title = null,
                                     CanvasFormState? form = null);

public void Restore(CanvasWorkspace workspace);

public string ToJson();
public static bool TryFromJson(string json, out CanvasDocument? document, out string? error);

public CanvasDocument AsTemplate(string title);
```

- **`Capture`** kopiert jede View tief (JSON-Roundtrip über `CanvasJson`), damit das Dokument vom
  weiterlaufenden Workspace unabhängig ist. Views mit `Removing = true` werden übersprungen —
  was gerade rausanimiert, gehört nicht ins Dokument. `SavedAt = DateTimeOffset.UtcNow`,
  `Title = title ?? workspace.Active?.Title ?? "Canvas"`. Ist `form` gesetzt, werden die
  Live-Feldwerte in die `value`-Props der interaktiven Nodes der **aktiven** View gefaltet
  (§4.3).
- **`Restore`** leert den Workspace und baut ihn neu auf: `Open(title, icon)` je View (der Slug
  reproduziert die Id), `Spec` als Tiefkopie gesetzt, danach `Activate(ActiveId)` falls bekannt,
  sonst bleibt die zuletzt geöffnete aktiv. Eine einzige `OnChange`-Welle ist nicht garantiert
  (jede Workspace-Mutation feuert), aber das ist Bestand und für den Renderer harmlos.
- **`TryFromJson`** ist die einzige erlaubte Eingangstür. Fehlertexte englisch wie überall in der
  Bibliothek, adressiert an den Host (nicht an ein Modell):
  - malformed JSON → `"The document could not be read: it is not valid JSON."`
  - `Schema <= 0` → `"The document has no schema version."`
  - `Schema > CurrentSchema` → `"This document was written by a newer version of DRYL (schema {n}, this build reads up to {CurrentSchema})."`
  - Views leer/null → `"The document contains no views."`
  Ältere Schemaversionen: solange `Schema <= CurrentSchema`, wird gelesen. Es gibt heute nur
  Version 1; ein späteres MAJOR darf ablehnen (Roadmap-Nichtziel „keine Migration").
- **`AsTemplate`** liefert eine Tiefkopie mit `Id = null`, neuem `Title` und `SavedAt = default`.
  Damit ist „gespeichertes Dokument als Startpunkt eines neuen" ein Einzeiler, ohne
  Template-Sonderobjekt.

## 4. `CanvasHistory` — Snapshot-Ring pro View

Neue Datei `DRYL.Components/Canvas/CanvasHistory.cs`.

```csharp
public sealed record CanvasHistoryEntry(string Label, DateTimeOffset At, string Json);

public sealed class CanvasHistory
{
    public CanvasHistory(int capacity = 20);

    public int Capacity { get; }
    public IReadOnlyList<CanvasHistoryEntry> Entries { get; }  // ältester → neuester
    public int Position { get; }        // Index des aktuell gezeigten Eintrags, -1 wenn leer
    public bool CanUndo { get; }        // Position > 0
    public bool CanRedo { get; }        // Position < Entries.Count - 1

    public event Action? OnChange;

    public bool Record(CanvasSpec? spec, string label);
    public CanvasSpec? Undo();
    public CanvasSpec? Redo();
    public CanvasSpec? Restore(int index);
    public void Clear();
}
```

**Verhaltensregeln**

- `Record` serialisiert den Spec und **verwirft ihn stillschweigend, wenn das JSON dem Eintrag
  an `Position` gleicht** (Rückgabe `false`). Das ist der Schutz gegen Streaming: eine Runde,
  die nichts geändert hat, füllt den Ring nicht.
- `Record` nach einem Undo **schneidet den Redo-Ast ab** (alles hinter `Position` fällt weg) —
  Standardverhalten, keine Baumhistorie.
- Läuft der Ring über, fällt der älteste Eintrag; `Position` wandert mit.
- `spec = null` wird als leerer Eintrag (`"null"`) aufgenommen — eine geleerte View ist ein
  legitimer Zustand, zu dem man zurück können muss.
- `Undo`/`Redo`/`Restore` liefern eine **frisch deserialisierte** `CanvasSpec` (nie eine
  geteilte Instanz) und verschieben nur `Position` — die Einträge bleiben, „auf Version zurück"
  ist derselbe Mechanismus wie Undo mit freiem Index.
- `Capacity` wird auf `[2, 200]` geklemmt.

### 4.1 Anbindung an View und Workspace

`CanvasView` bekommt eine faul erzeugte Historie; `CanvasWorkspace` die vier Verben, die die
aktive View betreffen — so bleibt die Komponente dünn und die Logik testbar ohne Renderer.

```csharp
// CanvasView
public CanvasHistory History { get; } = new();

// CanvasWorkspace
public bool Commit(string label);        // aktive View: History.Record(view.Spec, label)
public bool Undo();                      // aktive View: Spec = History.Undo(), OnChange
public bool Redo();
public bool RestoreVersion(int index);
public bool CanUndo { get; }             // Active?.History.CanUndo == true
public bool CanRedo { get; }
```

Alle vier geben `false` zurück, wenn nichts passiert ist, und feuern `OnChange` genau dann,
wenn sie `true` liefern — dieselbe Konvention wie `Activate`.

### 4.2 Wer committet

Kern-only, ohne Kenntnis vom Agents-Run: `DrylCanvasWorkspace` bekommt

```csharp
[Parameter] public int Revision { get; set; }          // typisch: _run.Round
[Parameter] public string? RevisionLabel { get; set; } // typisch: der letzte Prompt
```

Ändert sich `Revision` gegenüber dem zuletzt gesehenen Wert, committet die Komponente die aktive
View mit `RevisionLabel ?? "Version {n}"`. Weil `Record` identisches JSON verwirft, ist ein
überflüssiger Commit gratis. Hosts, die keinen Run haben (code-erzeugte Specs), rufen
`workspace.Commit("…")` direkt — die Komponente ist nicht der einzige Weg.

### 4.3 Feldwerte

Interaktive Nodes seeden ihren Formularwert bereits aus dem `value`-Prop und nur, solange das
Feld leer ist (`CanvasNodeView.SeedFormOnce`). Daraus folgt der billigste korrekte Weg:
**Feldstände werden in die `value`-Props des gespeicherten Specs gefaltet**, nicht als zweiter
Zustandssack neben dem Spec geführt. Ein geladenes Dokument füllt die Felder dann von allein —
ohne eine einzige neue Zeile im Ladepfad.

Gefaltet wird für `inputText`, `select`, `slider`, `toggle` (die vier interaktiven Typen), und
nur, wenn `Capture` eine `CanvasFormState` bekommt.

Woher kommt die `CanvasFormState`? **`DrylCanvas.Context` ist bereits öffentlich** („Exposed so a
wrapper can read the live field values") — es braucht also **keinen neuen Parameter**. Ein Host
mit `@ref` auf sein `DrylCanvas` übergibt `_canvas.Context.Form`; `DrylCanvasWorkspace` hält für
sein internes `DrylCanvas` selbst einen `@ref` und benutzt ihn beim Autosave.

**`DrylAiCanvas` reicht `Context` nicht nach außen** und bekommt in dieser Phase auch keine
Durchreiche — das wäre eine Agents-Änderung, und Phase 6 fasst Agents ohnehin an. Konsequenz und
bewusst akzeptiert: unter `DrylAiCanvas` persistiert Phase 5 die vom Modell **vorbelegten** Werte
(die stehen im Spec), nicht den live getippten Stand. Als Nicht-Ziel oben notiert und als
Folgepunkt für Phase 6 zu führen.

## 5. `ICanvasDocumentStore`

Neue Datei `DRYL.Components/Canvas/CanvasDocumentStore.cs`.

```csharp
public sealed record CanvasDocumentInfo(string Id, string Title, DateTimeOffset SavedAt, int ViewCount);

public interface ICanvasDocumentStore
{
    Task<string> SaveAsync(CanvasDocument document, CancellationToken ct = default);
    Task<CanvasDocument?> LoadAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<CanvasDocumentInfo>> ListAsync(CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
```

- `SaveAsync` mit `document.Id == null` vergibt eine neue Id (`Guid.NewGuid().ToString("n")`),
  schreibt sie auf das übergebene Dokument zurück und liefert sie; mit gesetzter Id überschreibt
  es. `SavedAt` wird beim Speichern auf „jetzt" gesetzt.
- `ListAsync` sortiert neueste zuerst.
- `InMemoryCanvasDocumentStore` speichert das **JSON**, nicht das Objekt — so kann ein Aufrufer
  ein geladenes Dokument nicht versehentlich in den Store hineinmutieren, und der Serialisier-Pfad
  wird bei jedem Test mitgelaufen. `ConcurrentDictionary`, thread-safe, keine Größenbegrenzung.
- A9: rein `Task`-basiert, kein serverseitiges Konstrukt — auf WASM implementiert der Host das
  Interface gegen HTTP oder `localStorage`.

DI in `CanvasServiceCollectionExtensions`:

```csharp
public static IServiceCollection AddDrylCanvasDocumentStore(this IServiceCollection services);
public static IServiceCollection AddDrylCanvasDocumentStore<TStore>(this IServiceCollection services)
    where TStore : class, ICanvasDocumentStore;
```

Beide registrieren als **Singleton** (wie Data- und Action-Registry) via `TryAddSingleton`, damit
ein Host-Store gewinnt, wenn er zuerst registriert wurde.

## 6. Die sichtbare Seite: `DrylCanvasWorkspace`

Alles Neue ist **opt-in** und ändert an bestehenden Nutzungen nichts.

### 6.1 Neue Parameter

| Parameter | Typ | Default | Wirkung |
| --- | --- | --- | --- |
| `ShowHistory` | `bool` | `false` | Undo/Redo/Verlauf in der Leiste |
| `Revision` | `int` | `0` | Änderung ⇒ Commit der aktiven View |
| `RevisionLabel` | `string?` | `null` | Beschriftung des nächsten Commits |
| `AutoSave` | `bool` | `false` | Debounced Speichern gegen den registrierten Store |
| `DocumentId` | `string?` | `null` | Zieldokument für Autosave; wird nach dem ersten Save gesetzt |
| `DocumentIdChanged` | `EventCallback<string>` | — | meldet die vergebene Id zurück |
| `DocumentTitle` | `string?` | `null` | Titel für Autosave-Dokumente |
| `OnSaved` | `EventCallback<CanvasDocumentInfo>` | — | nach jedem Autosave |

`ICanvasDocumentStore` wird per `[Inject(...)] ICanvasDocumentStore? Store` optional gezogen
(`IServiceProvider.GetService`), damit die Komponente ohne registrierten Store weiterhin lädt.
Ist `AutoSave` gesetzt und kein Store da, tut die Komponente nichts — kein Wurf.

### 6.2 Leiste

Rechts in der `ws-bar` eine Werkzeuggruppe `.ws-tools`, die von den Chips durch `margin-left:auto`
getrennt ist. Die Chips-Zeile scrollt horizontal (`overflow-x: auto`), die Werkzeuge bleiben
stehen — bei 375 px darf die Leiste nicht umbrechen und der Body nie seitwärts scrollen.

- **Undo** — `DrylTooltip` „Undo", `DrylIcon Name="Undo2"`, `Disabled` bei `!CanUndo`.
- **Redo** — analog, `Redo2`.
- **Verlauf** — `DrylPopover` mit der Eintragsliste (neueste zuerst): Label, Relativzeit, der
  aktuelle Eintrag markiert. Klick = `RestoreVersion(index)`. Icon `History`, Tooltip „Version
  history".

Alle drei sind `IconOnly` ⇒ Tooltip **und** `AriaLabel` (Regeln 2.9/2.11). Die Gruppe erscheint
über `DrylPresence` (`SlideDown`, `Fast`) wie die Leiste selbst, also nie mit einem Sprung.
Die Leiste zeigt sich, sobald `ShowHistory` an ist — auch bei einer einzigen View, sonst hätte
ein Ein-View-Dashboard kein Undo (`ShowBar` wird entsprechend erweitert).

### 6.3 A8 — eine Op bleibt eine Bewegung

Undo, Redo und „auf Version zurück" laufen durch `ViewTransition.RunAsync`, genau wie
`ActivateAsync`. Der Spec-Wechsel morpht damit über dieselbe Schicht wie ein View-Wechsel; die
neuen Node-Instanzen bekommen ihre Bewegung aus dem bestehenden FLIP-/Change-Pulse-Pfad. Kein
Neuaufbau-Blitz, kein Sprung.

`prefers-reduced-motion` ist in `IDrylViewTransition` und in `DrylPresence` bereits behandelt —
es kommt keine eigene CSS-Animation dazu.

### 6.4 Ansage

Nach Undo/Redo/Restore setzt die Komponente einen `aria-live="polite"`-Text
(„Undone: Umsatzchart hinzugefügt" / „Restored version 3 of 8"), gerendert als visuell
verborgene Zeile in der Leiste. Kein neues Muster — dieselbe Mechanik wie `DrylAiIndicator`.

### 6.5 Autosave

Ein `System.Timers.Timer` (oder `CancellationTokenSource`-Debounce) mit **1500 ms**: jede
`Revision`-Änderung stößt ihn neu an, beim Ablauf wird `CanvasDocument.Capture` gemacht und
gespeichert. Der Timer wird in `DisposeAsync` sauber abgeräumt (CLAUDE.md: kein `setTimeout`
und kein Timer ohne Cleanup). Speicherfehler werden geschluckt und über `OnSaved` schlicht nicht
gemeldet — ein defekter Store darf ein laufendes Dashboard nicht abschießen (dieselbe Haltung
wie beim Node-Bindungsfehler in Phase 1).

## 7. Tests

`tests/DRYL.Components.Tests/Canvas/`

**Unit**

- `CanvasHistoryTests` — Ringkapazität und Verdrängung; identisches JSON wird verworfen;
  Undo/Redo-Positionen; Redo-Ast wird von `Record` abgeschnitten; `Restore(index)`;
  `Undo` liefert eine neue Instanz (nicht dieselbe Referenz); leerer Spec (`null`);
  `OnChange` feuert genau bei echten Änderungen.
- `CanvasDocumentTests` — Roundtrip erhält Titel/Views/ActiveId/Bindungen; `Capture` ist eine
  Tiefkopie (Mutation des Workspace ändert das Dokument nicht und umgekehrt);
  `Removing`-Views fehlen; `TryFromJson` lehnt Müll, Schema 0 und Schema `CurrentSchema + 1`
  mit den spezifizierten Texten ab; `AsTemplate` löscht die Id; `Capture` mit `CanvasFormState`
  faltet Werte in `value`-Props aller vier interaktiven Typen.
- `CanvasWorkspaceHistoryTests` — `Commit`/`Undo`/`Redo`/`RestoreVersion` inkl. `OnChange`-Zählung;
  Historien zweier Views sind unabhängig; `Undo` ohne aktive View ist `false`.
- `InMemoryCanvasDocumentStoreTests` — Save vergibt Id und schreibt sie zurück; Save mit Id
  überschreibt; `ListAsync` neueste zuerst; Load unbekannt = `null`; Delete; gespeichertes
  Dokument ist gegen Mutation des Aufrufers immun.

**bUnit** (`DrylCanvasWorkspaceHistoryTests`)

- Ohne `ShowHistory` kein einziger Werkzeug-Button (Bestandsnutzungen unverändert).
- Mit `ShowHistory`: Undo/Redo initial disabled; nach zwei Commits Undo enabled; Klick auf Undo
  setzt `Active.Spec` auf den Vorgänger.
- `Revision`-Wechsel committet genau einmal; ein zweiter Render mit gleicher `Revision` nicht.
- Verlaufs-Popover listet die Einträge neueste zuerst und markiert den aktuellen.
- Leiste erscheint bei einer einzigen View, sobald `ShowHistory` an ist.

**Replay** — der Modellvertrag ändert sich nicht (kein neues Tool, kein neues Schema); der
bestehende Replay-Test bleibt grün und wird nicht erweitert.

## 8. Demo

Kern-only ⇒ die Demo gehört auf die Workspace-Seite der Website, nicht auf die Agents-Seite.

- Neues Beispiel `Components/Examples/Canvas/CanvasDocument.razor`, eingebunden auf
  `Components/Pages/DemoCanvasWorkspace.razor`: ein Workspace mit zwei code-erzeugten Views,
  Buttons „Ändern" (mutiert den Spec + `Commit`), Undo/Redo/Verlauf in der Leiste, „Speichern",
  „Laden", „Als Vorlage" gegen den `InMemoryCanvasDocumentStore`. Kein Modell nötig — das ist
  die von der DoD geforderte Replay-Variante.
- Die Live-Variante ist `Components/Examples/Agents/OpenAiCanvasWorkspace.razor` hinter dem
  bestehenden Umgebungs-Flag: dort werden `ShowHistory`, `Revision="_run.Round"` und `AutoSave`
  ergänzt. Reine Website-Änderung, keine Agents-Bibliotheksänderung.
- `ComponentCatalog`: `DrylCanvasWorkspace` existiert bereits — der Eintrag wird um die neuen
  Fähigkeiten (Undo/Redo/Verlauf, Dokument-Store) ergänzt. Keine neue Komponente zu registrieren.

## 9. Dateien

**Neu (Kern)**

- `DRYL.Components/Canvas/CanvasDocument.cs`
- `DRYL.Components/Canvas/CanvasHistory.cs`
- `DRYL.Components/Canvas/CanvasDocumentStore.cs`

**Geändert (Kern)**

- `DRYL.Components/Canvas/CanvasWorkspace.cs` — `CanvasView.History`, `Commit/Undo/Redo/RestoreVersion/CanUndo/CanRedo`
- `DRYL.Components/Components/AI/DrylCanvasWorkspace.razor` (+ `.css`) — Werkzeugleiste, Commit, Autosave
- `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs` — `AddDrylCanvasDocumentStore`

**Geändert (Website)** — `DemoCanvasWorkspace.razor`, neues Beispiel, `OpenAiCanvasWorkspace.razor`,
`ComponentCatalog.cs`

**Unverändert:** das gesamte Agents-Paket.

## 10. Definition of Done

1. `CHANGELOG.md` gepflegt, Release im selben Commit geschnitten, Kern `<Version>` 2.15.0 →
   **2.16.0**; Agents-Version **bleibt** 0.13.0.
2. `ComponentCatalog`-Eintrag von `DrylCanvasWorkspace` aktualisiert.
3. Demo-Seite zeigt Speichern/Laden/Vorlage/Undo/Redo/Verlauf ohne Modell.
4. Tests aus §7 grün (`dotnet test tests/DRYL.Components.Tests`).
5. Beide Farbmodi, 375 px und `prefers-reduced-motion` geprüft.
6. `node scripts/check-light-sync.mjs` — es kommen keine Tokens dazu, der Lauf bleibt trotzdem
   Pflichtprüfung.
7. A8 verifiziert: Undo/Redo/Restore morphen über `IDrylViewTransition`, kein Sprung.
8. `project_ai_canvas` / `project_canvas_platform` fortgeschrieben.
