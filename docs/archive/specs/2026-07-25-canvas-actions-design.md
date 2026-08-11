# Phase 2 — Canvas Actions

**Datum:** 2026-07-25
**Status:** freigegeben (Brainstorming abgeschlossen)
**Rahmen:** `2026-07-25-canvas-platform-roadmap.md` — Phase 2 von 6
**Baut auf:** Phase 1 (`2026-07-25-canvas-data-binding-design.md`) — gezielter Refresh nach einer Aktion
**Versionen:** Kern `2.12.0 → 2.13.0` · Agents `0.10.0 → 0.11.0`

---

## 1. Ziel

Ein Button im Artefakt löst einen **typisierten Host-Command** aus statt eine Chat-Nachricht.

Heute macht `CanvasInteraction.ToPromptMessage()` aus jedem Klick Prosa, die im nächsten Zug beim
Modell landet. Das ist für „zeig mir das als Balkendiagramm" richtig und für „Bestellung freigeben"
untauglich: die Freigabe darf nicht davon abhängen, ob ein Sprachmodell den Satz richtig versteht,
und sie darf nicht das Modell als Zwischeninstanz haben.

Nach dieser Phase gilt: **Die AI baut, beschriftet und belegt den Button vor — gedrückt wird er nur
vom Menschen** (A4). Der Handler ist gewöhnlicher C#-Code im Host, mit DI-Scope, typisierten
Argumenten und einem Ergebnis, das sagt, was der Canvas als Nächstes tun soll.

## 2. Entscheidungen dieser Phase

Zusätzlich zu den Roadmap-Entscheidungen A1–A9 wurden am 2026-07-25 mit dem Nutzer festgelegt:

| # | Entscheidung | Verworfene Alternativen |
| --- | --- | --- |
| **E1** | **Die Aktion steht auf Node-Ebene**, symmetrisch zu Phase 1: `"action": { "name", "args", "confirm" }` neben `"data"`. `kind: "danger"` bleibt Prop — es ist Darstellung. | Alles in die Button-Props (vermischt Darstellung und Verhalten und lässt sich in Phase 4 nicht auf einen `form`-Container heben) |
| **E2** | **`.AskAi(…)` läuft über den bestehenden `OnInteraction`-Weg.** `CanvasInteraction` bekommt eine optionale `Message`, die `ToPromptMessage()` wörtlich zurückgibt. | Ein zweiter `OnActionAsk`-Callback (jeder Host müsste ihn verdrahten, sonst schluckt der Canvas das AskAi still) |
| **E3** | **Erfolg ist ein Toast, Fehler bleibt inline am Button.** Erfolg ist flüchtig — das Artefakt refresht ohnehin sichtbar. Ein Fehler verlangt eine Reaktion und darf nicht nach vier Sekunden verschwinden. | Beides inline (Erfolgsmeldungen bräuchten eigene Verfallslogik); beides als Toast (genau die Meldung, die bleiben müsste, verschwindet) |
| **E4** | **`CanvasActionResult` darf Patch-Ops zurückgeben.** „Freigeben" setzt das Badge sofort auf grün — ohne AI-Runde, ohne Datenquelle. Läuft durch den vorhandenen `CanvasPatcher` und stempelt den vorhandenen Change-Pulse. | Auf Phase 5 verschieben |
| **E5** | **Argumente werden wie Phase-1-Parameter als C#-Record deklariert** und per Reflection beschrieben — `CanvasParamSchema` und `CanvasParamInfo` werden unverändert wiederverwendet. | Ein zweites, aktions-eigenes Schema |
| **E6** | **`Refresh` invalidiert global über `ICanvasDataService`, nicht nur den eigenen Binder.** Zwei Canvases, die dieselbe Quelle zeigen, sind nach der Freigabe beide aktuell. | Nur der auslösende Canvas lädt nach |
| **E7** | **`confirm` ohne verfügbaren `IDrylDialogService` führt die Aktion nicht aus**, sondern zeigt den Inline-Fehler. Eine bewusst als bestätigungspflichtig markierte Aktion darf nie unbestätigt laufen. | Ohne Dialog einfach durchlaufen; `window.confirm` per Interop |

## 3. Öffentliche API

### 3.1 Registrierung

```csharp
public sealed record ApproveArgs(string OrderId, string? Note = null);

builder.Services.AddDrylCanvasAction("order.approve",
    "Gibt einen Auftrag frei.",
    async (ApproveArgs a, CanvasActionContext ctx, CancellationToken ct) =>
    {
        var orders = ctx.Services.GetRequiredService<IOrderService>();
        await orders.ApproveAsync(a.OrderId, a.Note, ct);
        return CanvasActionResult.Ok("Auftrag freigegeben").Refresh("orders.open");
    });

// argumentlose Aktion
builder.Services.AddDrylCanvasAction("cache.clear", "Leert den Berichts-Cache.",
    async (CanvasActionContext ctx, CancellationToken ct) =>
    {
        await ctx.Services.GetRequiredService<IReportCache>().ClearAsync(ct);
        return CanvasActionResult.Ok("Cache geleert");
    });
```

```csharp
public static IServiceCollection AddDrylCanvasAction<TArgs>(
    this IServiceCollection services,
    string name,
    string description,
    Func<TArgs, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
    where TArgs : class;

public static IServiceCollection AddDrylCanvasAction(
    this IServiceCollection services,
    string name,
    string description,
    Func<CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler);
```

Die Regeln sind wortgleich zu `AddDrylCanvasDataSource` — bewusst, denn ein Host lernt sie einmal:

- **`name`**: Schlüssel im Spec, Konvention `bereich.sache`. Doppelte Registrierung wirft sofort.
- **`description`**: ein Satz für das Modell. Argumente werden **nicht** hier beschrieben — die
  kommen aus dem Record (E5, wie D4).
- **`ctx.Services`**: der Circuit-/Request-Scope. Mandant und Nutzer holt sich der Handler dort,
  nie aus dem Spec (D5 gilt unverändert — sonst könnte das Modell den Mandanten setzen).
- **`TArgs`** akzeptiert dieselbe Typenliste wie `TParams` (`string`, `int`, `long`, `double`,
  `decimal`, `bool`, `DateOnly`, `DateTime`, `Guid`, `enum`, deren Nullables, `IReadOnlyList<T>`).
  Ein nicht unterstützter Typ wirft bei der Registrierung.

### 3.2 Kontext und Ergebnis

```csharp
public sealed class CanvasActionContext
{
    /// Der Circuit-/Request-Scope.
    public IServiceProvider Services { get; }

    /// Die Id des Buttons, der gedrückt wurde.
    public string NodeId { get; }

    /// Momentaufnahme *aller* interaktiven Felder des Artefakts — auch derer,
    /// die nicht in "args" stehen.
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// Bequemer Zugriff auf ein Feld aus Values; default(T) bei Fehlen oder Typmismatch.
    public T? Get<T>(string name);
}
```

```csharp
public sealed class CanvasActionResult
{
    public static CanvasActionResult Ok(string? message = null);
    public static CanvasActionResult Fail(string message);

    public bool Succeeded { get; }
    public string? Message { get; }
    public IReadOnlyList<CanvasInvalidation> Refreshes { get; }
    public IReadOnlyList<CanvasOp> Ops { get; }
    public string? Ask { get; }

    public CanvasActionResult Refresh(params string[] sources);
    public CanvasActionResult Refresh(string source, object parameters);
    public CanvasActionResult Patch(params CanvasOp[] ops);
    public CanvasActionResult AskAi(string message);   // standardmäßig aus (A4)
}
```

Fluent, mutierender Builder — jede Methode gibt `this` zurück. `Refresh` erzeugt intern die
bereits existierende `CanvasInvalidation`; ein neuer Typ dafür wäre reine Verdopplung.

`AskAi` ist **nicht** die AI, die eine Aktion auslöst — es ist die Aktion, die der AI *hinterher*
erzählt, was passiert ist. Die Richtung stimmt mit A4 überein; deshalb ist es opt-in und nicht der
Standardweg.

### 3.3 Bindung am Node

```json
{ "id": "approve", "type": "button",
  "props": { "label": "Freigeben", "kind": "danger" },
  "action": {
    "name": "order.approve",
    "args": { "orderId": { "$field": "order" }, "note": "Freigabe aus dem Dashboard" },
    "confirm": "Auftrag wirklich freigeben?"
  } }
```

- `args`: Literal oder `{ "$field": "<name>" }` — **exakt dieselbe Syntax wie `data.params`**.
  Der Auflöser wird geteilt (§4.1), nicht kopiert.
- `confirm`: optionaler Text. Vorhanden ⇒ vor der Ausführung ein `DrylDialog` über
  `IDrylDialogService.ShowConfirmAsync`.
- `action` ist optional. Ein Button ohne `action` verhält sich exakt wie heute — `intent` geht als
  `CanvasInteraction` an `OnInteraction` (A2-Denkweise, Rückwärtskompatibilität).

Modell im Code (`DRYL.Components.Canvas`):

```csharp
public sealed class CanvasActionBinding
{
    public string? Name { get; set; }
    public JsonElement? Args { get; set; }
    public string? Confirm { get; set; }
}
// CanvasNode bekommt: public CanvasActionBinding? Action { get; set; }
```

`ButtonNodeProps.Kind` erlaubt zusätzlich `"danger"` → `DrylButton.ButtonVariant.Danger`.
`ButtonNodeProps.Intent` wird **optional**, sobald eine `action` vorhanden ist (§5.2, Regel 6) —
eine Lockerung, kein Bruch.

### 3.4 Laufzeit-Dienst

```csharp
public interface ICanvasActionService                    // scoped
{
    IReadOnlyList<CanvasActionDescriptor> Descriptors { get; }

    /// Infrastruktur — ein gebundener Canvas ruft das, Hosts nicht.
    Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct);
}

public sealed record CanvasActionDescriptor(
    string Name, string Description, IReadOnlyList<CanvasParamInfo> Args);
```

`Descriptors` speist den Prompt-Block (§5.1) **und** die Validierung (§5.2) — ein Ort der Wahrheit,
genau wie `ICanvasDataService.Descriptors`. Der Dienst baut den `CanvasActionContext` aus dem
eigenen Scope; deshalb reicht der Aufrufer nur Rohdaten hinein.

### 3.5 Was der Host am Canvas sieht

`DrylCanvas` bekommt einen zusätzlichen Parameter:

```csharp
/// Läuft nach jeder abgeschlossenen Aktion — auch nach einer fehlgeschlagenen.
/// Für Protokollierung und eigene Reaktionen; der Canvas hat Toast, Patch und
/// Refresh zu diesem Zeitpunkt bereits selbst erledigt.
[Parameter] public EventCallback<CanvasActionOutcome> OnAction { get; set; }

public sealed record CanvasActionOutcome(
    string Action, string NodeId, bool Succeeded, string? Message);
```

`DrylAiCanvas` reicht ihn unverändert durch. Kein Host **muss** ihn verdrahten — der Canvas ist
ohne ihn vollständig funktionsfähig.

## 4. Der Runner

`CanvasActionRunner` gehört **einer Canvas-Instanz**, wie `CanvasDataBinder`, und hängt am selben
`CanvasContext`-Cascade. Er ist bewusst klein: er hält je Node einen Zustand und führt eine
Sequenz aus.

```csharp
public sealed class CanvasActionRunner
{
    public CanvasActionRunner(ICanvasActionService actions, ICanvasDataService? data,
                              CanvasFormState form, IServiceProvider services, ILogger? log = null);

    public CanvasActionState? StateOf(string nodeId);
    public Task InvokeAsync(string nodeId, CanvasActionBinding action);
    public event Action? OnChanged;
}

public sealed class CanvasActionState
{
    public bool Busy { get; }
    public string? Error { get; }
}
```

### 4.1 Geteilte Argument-Auflösung

Die `$field`-Auflösung aus `CanvasDataBinder.Resolve` wandert in einen internen Helfer
`CanvasArgs`, den Binder und Runner beide nutzen:

```csharp
internal static class CanvasArgs
{
    public static JsonElement? Resolve(JsonElement? raw, CanvasFormState form, out HashSet<string> fields);
    public static string? FieldReference(JsonElement value);
    public static bool HasFieldReference(JsonElement? args);
}
```

Das ist keine Kür: zwei Kopien derselben Referenzsyntax würden garantiert auseinanderlaufen, und
der Prompt verspricht dem Modell ausdrücklich *dieselbe* Schreibweise an beiden Stellen.
`CanvasDataBinder.FieldReference` bleibt als internes Delegat bestehen, damit `CanvasCatalog`
seine bestehende Aufrufstelle behält.

### 4.2 Die Sequenz eines Klicks

1. **Doppelklick-Sperre.** Läuft für diesen Node bereits etwas, wird der Klick verworfen. (Der
   `DrylButton` ist im `Loading`-Zustand ohnehin `disabled`; die Sperre deckt den Rennfall ab.)
2. **Argumente auflösen** — Literale und `$field`-Verweise gegen den aktuellen `CanvasFormState`.
3. **Bestätigen**, falls `confirm` gesetzt ist: `ShowConfirmAsync(<Button-Label>, <confirm-Text>,
   confirmLabel: <Button-Label>)`. Abbruch ⇒ nichts passiert, kein Fehler, kein Toast.
   Kein `IDrylDialogService` auflösbar ⇒ Inline-Fehler „Confirmation is unavailable — the action
   was not run." und **Abbruch** (E7).
4. **Busy = true**, ein Render. Der Button geht in `Loading` — ein Beat.
5. **Handler ausführen** über `ICanvasActionService.InvokeAsync`, komplett in `try/catch`. Eine
   Ausnahme wird geloggt und zu `Fail("Action '<name>' failed.")` — sie erreicht nie den Renderer
   und nie den Circuit.
6. **Ergebnis verarbeiten, in dieser Reihenfolge:**
   1. `Ops` — über `CanvasPatcher` auf den aktuellen Spec, `setProps`-Ids stempeln den Pulse.
      **Alle Ops in einem Rutsch**, ohne den 260-ms-Stagger des AI-Pfads: eine Aktion ist *eine*
      Zustandsänderung, also eine Bewegung (A8). Ein übersprungener Op geht an `ILogger`.
   2. `Refreshes` — `ICanvasDataService.Invalidate(...)`, global (E6). Der Binder lädt und pulst,
      exakt auf dem Phase-1-Weg.
   3. `Message` bei Erfolg — `IDrylToastService.ShowSuccess`, falls auflösbar; sonst still
      (die Nachricht erreicht den Host weiterhin über `OnAction`).
   4. `Fail`-Meldung — `Error` am Node, inline (E3).
   5. `Ask` — `Ctx.Intent` mit `new CanvasInteraction(name, nodeId, values) { Message = ask }`.
7. **Busy = false**, `OnAction` feuert, ein Render.

Ein `Error` überlebt bis zum nächsten Versuch desselben Buttons — dann wird er zu Beginn von
Schritt 4 gelöscht.

### 4.3 A8 — eine Aktion ist eine Bewegung

| Situation | Was der Nutzer sieht |
| --- | --- |
| **Klick** | Der Button geht in `DrylButton.Loading` — Spinner statt Label-Wechsel, kein Overlay, keine Sperre über dem Artefakt |
| **Bestätigung nötig** | Der bestehende `DrylDialog` fährt über die vorhandene Dialog-Schicht auf und zu — kein eigener Weg |
| **Erfolg mit Patch-Ops** | Die Ops landen zusammen; die betroffenen Nodes pulsen — **dieselbe** Bewegung wie ein AI-`setProps` |
| **Erfolg mit Refresh** | Die betroffenen Bindungen laden; geänderte Werte pulsen und zählen hoch (Phase-1-Weg) |
| **Erfolg ohne beides** | Der Toast fährt ein. Er ist dann die einzige Bewegung — ohne ihn wäre der Klick folgenlos |
| **Fehler** | Der Button verlässt `Loading`, die Inline-Marke blendet über `DrylPresence` darunter ein — kein Sprung, keine Alert-Wand |

Der Nutzer soll nicht unterscheiden können, ob ein Wert sich geändert hat, weil er einen Knopf
gedrückt, die AI gepatcht oder ein Intervall nachgeladen hat. Es ist überall dieselbe Sprache.

### 4.4 Fehlertexte

Nutzerseitige Texte sind knapp und nennen die Aktion, nie den Stacktrace
(„Action *order.approve* failed."). Die Ausnahme geht an `ILogger`. Eine `Fail`-Meldung des Hosts
wird **wörtlich** angezeigt — der Host weiß besser als DRYL, was „Auftrag ist bereits freigegeben"
heißt.

## 5. Die AI-Seite

### 5.1 Prompt-Block

`CanvasActionPrompt.Block(descriptors)` — parallel zu `CanvasDataPrompt.Block`, leer bei leerer
Registry (dann bleibt der Vertrag exakt wie heute):

```
ACTIONS — wire buttons to these instead of inventing an intent:
- order.approve(orderId: string, note?: string) — "Gibt einen Auftrag frei."
- cache.clear() — "Leert den Berichts-Cache."
Wire like this: "action": { "name": "<name>", "args": { … }, "confirm": "<question>"? }
An arg is a literal, or { "$field": "<name of an interactive node in this artifact>" } —
the same reference syntax as a data param.
A button with an "action" may omit "intent".
Add "confirm" to anything destructive or irreversible, and set "kind": "danger" on the button.
You place the button and label it. You NEVER trigger an action — only the user presses it.
```

Der Block geht in `CreatePrompt` **und** `UpdatePrompt`, direkt hinter den Datenquellen-Block.

In `CanvasPrompt.SchemaText` ändert sich genau eine Zeile — der Button-Eintrag bekommt
`"danger"` als dritte `kind`-Variante. Alles andere über Aktionen steht im ACTIONS-Block, damit
das Token-Budget einer Anwendung **ohne** Aktionen unverändert bleibt (dasselbe Prinzip wie in
Phase 1).

### 5.2 Validierung

`CanvasValidationContext` bekommt ein zusätzliches Feld:

```csharp
public IReadOnlyList<CanvasActionDescriptor> Actions { get; init; } = Array.Empty<CanvasActionDescriptor>();
```

`CanvasCatalog.Validate(node, context)` wird umgebaut, damit Daten- **und** Aktionsbindung geprüft
werden (heute steigt es früh aus, sobald keine `data` vorhanden ist). Geprüft wird mit Kontext:

1. `action` steht nur auf einem `button`. Sonst: „a <type> node cannot carry an action — only a
   button can." *(Phase 4 erweitert das auf `form`.)*
2. Die Aktion existiert. Sonst: „unknown action '…' — available: …" (bis zu fünf Namen).
3. Pflichtargumente vorhanden, Typen konvertierbar, keine unbekannten Argumente — wortgleiche
   Meldungen wie bei den Datenparametern.
4. Jeder `$field`-Verweis zeigt auf ein `name` eines interaktiven Nodes **desselben** Artefakts.
5. `confirm` ist, wenn vorhanden, nicht leer.
6. Ein `button` braucht `intent` **oder** `action.name`. Sonst: „a button needs an intent or an
   action."

Alle Befunde landen wie bisher als korrigierende Sätze im Receipt von `create_artifact` /
`update_artifact`. **Kein harter Abbruch** — ein invalider Node rendert als Platzhalter, das
Modell repariert ihn im nächsten Zug.

`DrylCanvasTools.Create` / `CreateReplay` bekommen einen zusätzlichen optionalen Parameter
`ICanvasActionService? actions = null`; `ValidationContext(root)` füllt `Actions` daraus. Der
Kontext wird jetzt schon gebaut, wenn **Quellen oder Aktionen** registriert sind (heute: nur
Quellen).

### 5.3 Was die AI ausdrücklich nicht kann

Es gibt kein Tool `run_action`. Es gibt keinen Pfad, auf dem eine Modellausgabe einen Handler
auslöst. Der einzige Aufrufer von `ICanvasActionService.InvokeAsync` ist der `CanvasActionRunner`,
und der wird ausschließlich vom `@onclick` eines gerenderten Buttons erreicht. Das ist die
Haftungsgrenze aus A4, und sie ist eine Eigenschaft der Architektur, nicht eine Bitte im Prompt.

## 6. Render-Pfad

`CanvasNodeView`, Fall `"button"`:

```razor
case "button":
{
    var p = Props<ButtonNodeProps>();
    var state = Ctx.Actions?.StateOf(Node.Id);
    <DrylButton Variant="MapButtonVariant(p!.Kind)" Loading="state?.Busy == true"
                OnClick="@(_ => TriggerAsync(p))">
        @p.Label
    </DrylButton>
    <DrylPresence Visible="@(state?.Error is not null)" Transition="PresenceTransition.SlideUp"
                  Speed="PresenceSpeed.Fast">
        <span class="canvas-action-error">
            <DrylIcon Name="Alert" Size="14" /> @state?.Error
        </span>
    </DrylPresence>
    break;
}
```

`TriggerAsync` entscheidet zwischen den beiden Wegen:

| Lage | Weg |
| --- | --- |
| Kein `Node.Action` | `RaiseIntent(p.Intent)` — unverändert wie heute |
| `Node.Action`, aber `Ctx.Actions is null` (keine Aktion registriert) | `RaiseIntent(p.Intent)`, falls vorhanden — der Rückfallweg aus der Roadmap |
| `Node.Action` und Registry vorhanden | `Ctx.Actions.InvokeAsync(Node.Id, action)` |

Ein `action.name`, den es in einer vorhandenen Registry nicht gibt, ergibt den Inline-Fehler
„unknown action '…'" — das Modell hat ihn erfunden, und die Validierung sagt ihm das bereits im
Receipt.

`.canvas-action-error` ist ein Zwilling des vorhandenen `.canvas-data-error` (dieselben Tokens,
dieselbe Größe) und lebt in `DrylCanvas.razor.css`. **Kein neues Token, keine neue Farbe.**

`DrylCanvas.OnInitialized` legt den Runner an, sobald ein `ICanvasActionService` mit nicht-leeren
`Descriptors` auflösbar ist — spiegelbildlich zum Binder. `CanvasContext` bekommt
`public CanvasActionRunner? Actions { get; internal set; }` und
`internal Func<CanvasOp, string?>? Patch` (von `DrylCanvas` gesetzt, genau wie `Purge`).

## 7. Versionierung & Doku

- `DRYL.Components`: **2.12.0 → 2.13.0** (MINOR — rein additiv).
- `DRYL.Components.Agents`: **0.10.0 → 0.11.0** (MINOR; der zusätzliche `DrylCanvasTools`-Parameter
  ist optional, es bricht nichts — aber `0.x`-MINOR ist ohnehin die Release-Einheit).
- `CHANGELOG.md`: `Added` (Canvas Actions, `AddDrylCanvasAction`, `DrylCanvas.OnAction`,
  `CanvasInteraction.Message`, `kind: "danger"` am Button), Release im selben Commit geschnitten.
- `ComponentCatalog` in `DRYL.Website`: die `DrylCanvas`- und `DrylAiCanvas`-Einträge erwähnen
  Actions.
- ⚠ `publish.yml` publiziert beide Pakete (Kern `v<ver>`, Agents `agents-v<ver>`) beim Push auf
  `main`, sobald die `<Version>` neu ist. Kein Workflow-Eingriff nötig — nur verifizieren
  (Roadmap §5.3).

## 8. Nicht-Ziele

- **Keine AI-ausgelösten Aktionen** (A4). Kein `run_action`-Tool, in keiner Form.
- **Kein Undo für Commands** — das ist Domänensache. Undo für *Spec*-Änderungen kommt in Phase 5.
- **Kein `form`-Node mit Submit auf eine Aktion** — Phase 4.
- **Keine Aktionen auf anderen Node-Typen als `button`** — die Validierung lehnt sie ausdrücklich ab.
- **Keine parallele Mehrfachausführung** derselben Aktion am selben Node.
- **Keine Fortschrittsanzeige** langlaufender Aktionen jenseits des Busy-Buttons. Wer Fortschritt
  braucht, schreibt ihn per `Patch`-Op in einen `progress`-Node.
- **Keine neuen Durations, Easings, Farben oder Tokens.**

## 9. Tests

**Unit — Registry:** Argument-Deskriptor aus einem Record (Pflicht via Nullable/Default);
nicht unterstützter Argumenttyp wirft bei der Registrierung; doppelter Aktionsname wirft;
argumentlose Überladung ergibt einen leeren Argument-Satz.

**Unit — Prompt-Block:** enthält Name, Signatur und Beschreibung; leer bei leerer Registry;
enthält den Satz, dass das Modell Aktionen nie selbst auslöst.

**Unit — `CanvasArgs`:** Literale bleiben, `$field` wird gegen `CanvasFormState` aufgelöst,
fehlendes Feld ⇒ `null`; Binder und Runner erzeugen für dieselbe Eingabe dasselbe JSON.

**Unit — Runner:** Erfolg mit Nachricht ruft den Toast-Dienst; `Fail` setzt den Inline-Fehler und
**keinen** Toast; eine werfende Handler-Ausnahme wird zu `Fail` und verlässt den Runner nie;
`Refresh("x")` löst `Invalidate("x")` aus; `Refresh("x", new {…})` nur den passenden Schlüssel;
`Patch(op)` läuft durch den Patcher und stempelt den Pulse; `AskAi` feuert `Ctx.Intent` mit einer
`CanvasInteraction`, deren `ToPromptMessage()` genau den übergebenen Text liefert; ein zweiter
Klick während einer laufenden Aktion wird verworfen; `confirm` + abgelehnter Dialog ⇒ der Handler
läuft nicht; `confirm` ohne `IDrylDialogService` ⇒ Inline-Fehler und der Handler läuft nicht.

**Unit — Validierung:** die sechs Regeln aus §5.2, je mit der erwarteten Meldung; ein Button **ohne**
`action` bleibt mit `intent` allein gültig; `action` auf einem `stat` wird abgelehnt.

**bUnit:** Klick auf einen gebundenen Button zeigt `Loading` und danach wieder nicht; ein Fehler
rendert `.canvas-action-error` mit dem Text; ein Erfolg rendert ihn nicht; `kind: "danger"` ergibt
die Danger-Variante; ein Button ohne Registry fällt auf `OnInteraction` zurück; `OnAction` feuert
bei Erfolg **und** bei Fehlschlag; `CanvasInteraction.Message` erreicht den Host wörtlich.

**Replay (Modell-Vertrag):** unbekannte Aktion, fehlendes Pflichtargument, unbekanntes Argument,
`$field` auf ein nicht existierendes Feld, `action` auf einem Nicht-Button, Button ohne `intent`
und ohne `action` — jeder Fall erscheint im Receipt mit einem korrigierenden Satz, und das
Artefakt rendert trotzdem.

**Rückwärtskompatibilität:** die vollständige Phase-1-Testmenge bleibt unverändert grün; ein Test
pinnt, dass ein Artefakt ohne registrierte Aktionen weder den Prompt-Block noch ein verändertes
Button-Verhalten sieht.

## 10. Risiken

| Risiko | Gegenmaßnahme |
| --- | --- |
| **Doppelte Auslösung** — Netzwerklatenz, ungeduldiger Nutzer | `DrylButton` ist unter `Loading` `disabled`; der Runner verwirft zusätzlich jeden Klick auf einen bereits laufenden Node |
| **Handler wirft und reißt den Circuit** | Jeder Aufruf in `try/catch`; die Ausnahme wird zu `Fail` und geht an `ILogger`, nie an den Renderer |
| **Destruktive Aktion ohne Bestätigung** | `confirm` ist Vertragsbestandteil; ohne Dialog-Dienst wird die Aktion verweigert (E7); der Prompt weist das Modell an, alles Destruktive zu markieren |
| **Patch-Ops treffen einen inzwischen ersetzten Spec** (eine AI-Generierung lief parallel) | Der Patcher findet die Id nicht und überspringt den Op mit Grund → `ILogger`. Kein halb gepatchter Baum, keine Ausnahme |
| **Prompt-Wachstum** — viele Aktionen ⇒ langer Block in *jeder* Generierung | Eine Zeile je Aktion; dieselbe `CrowdedAt`-Warnung wie bei den Datenquellen; die eigentliche Antwort ist die Katalog-Kompression aus Phase 4 |
| **`$field`-Logik driftet zwischen Binder und Runner auseinander** | Geteilter `CanvasArgs`-Helfer (§4.1) plus ein Test, der beide Wege gegeneinander pinnt |
| **Toast-Dienst fehlt und die Erfolgsmeldung verschwindet lautlos** | `OnAction` liefert sie dem Host in jedem Fall; die Demo-Seite und die XML-Doku nennen `DrylToastProvider` als Voraussetzung |
