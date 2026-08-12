# Phase 3 — Workspace + Prompt Dock

**Datum:** 2026-07-25
**Status:** freigegeben (Brainstorming abgeschlossen)
**Rahmen:** `2026-07-25-canvas-platform-roadmap.md` — Phase 3 von 6
**Baut auf:** Phase 1 (`2026-07-25-canvas-data-binding-design.md`), Phase 2 (`2026-07-25-canvas-actions-design.md`)
**Versionen:** Kern `2.13.0 → 2.14.0` · Agents `0.11.0 → 0.12.0`

---

## 1. Ziel

Die Vision-Oberfläche der Roadmap wird zu **zwei Komponenten** statt zu Host-Code:

```razor
<DrylCanvasWorkspace Workspace="_ws">
    <View><DrylAiCanvas Run="_run" AllowExpand="false" /></View>
</DrylCanvasWorkspace>

<DrylCanvasDock Run="_run" Busy="Busy" OnSend="Send" Corner="DockCorner.BottomRight">
    <Log>@* die eigenen DrylMessages des Hosts *@</Log>
</DrylCanvasDock>
```

Heute schreibt jede Anwendung diese Fläche selbst — die beiden Website-Beispiele sind der Beleg:
`DrylChat` links, `DrylAiCanvas` rechts, Turn-Liste im `@code`-Block. Das ist eine Chat-Demo, keine
Fachanwendung. Nach dieser Phase gilt:

- **A5** — der Workspace hält benannte Views, genau eine ist groß sichtbar, der Wechsel morpht.
  Der Nutzer kommt zu dem zurück, was er vorhin hatte.
- **A6** — das Dock ist eine Befehlsleiste: Eingabe plus **eine** Zeile Live-Status. Das Artefakt
  ist die Antwort, nicht der Text. Der Verlauf klappt auf Abruf auf.

## 2. Entscheidungen dieser Phase

Zusätzlich zu den Roadmap-Entscheidungen A1–A9 am 2026-07-25 mit dem Nutzer festgelegt:

| # | Entscheidung | Verworfene Alternativen |
| --- | --- | --- |
| **F1** | **`CanvasWorkspace` ist ein beobachtbarer Zustandshalter im Kern**, neben `CanvasFormState` und `CanvasPulseTracker`. Die Komponente rendert ihn, die AI mutiert ihn. | Der Roadmap-Sketch `Views="_views" @bind-ActiveViewId="_active"` (rein präsentational). Dann bräuchte `open_view` einen zusätzlichen Host-Callback, den zu verdrahten jeder vergisst. **Die Roadmap-Zeile wird mit dieser Spec angepasst.** |
| **F2** | **Ein `DrylCanvasRun`, projiziert auf die aktive View.** `run.Spec` liest und schreibt die Spec der aktiven View. | Ein Run pro View: mehr Zustand, und der Kern-Workspace müsste einen Agents-Typ kennen (verletzt A1). |
| **F3** | **Der Workspace besitzt den Wechsel-Morph**, `DrylAiCanvas` überspringt seinen eigenen Swap-Morph für genau diesen Wechsel. | Beide morphen: `IDrylViewTransition` hat genau ein `_pending` — zwei verschachtelte `RunAsync` verlieren eine Mutation oder brechen die Transition ab. |
| **F4** | **Das Dock lebt im Top-Layer** (`popover="manual"` + `dryl.topLayer.show`), das Attribut wird erst nach erfolgreichem Interop gesetzt. | Nur `position: fixed`: jeder Ancestor mit `transform`/`backdrop-filter` wird Containing Block — das Dock klebt dann an der Karte statt am Viewport. |
| **F5** | **Der Verlauf kommt als `<Log>`-Slot vom Host.** Das Dock bringt kein eigenes Nachrichtenmodell mit. | Ein dock-eigener Nachrichtentyp — ein zweiter, konkurrierender Chat-Datentyp neben `DrylMessage`. |
| **F6** | **`open_view(name, brief)` aktiviert *und* baut** — dieselbe Create-Generierung wie `create_artifact`. | Nur aktivieren: zwei Modell-Runden für den Normalfall, und ein Modell, das die zweite vergisst, lässt eine leere View stehen. |
| **F7** | **Der Nutzer darf Views schließen, aber keine anlegen.** Neue Views entstehen durch die AI oder durch Host-Code. | Ein `+`-Knopf: eine leere View ohne Inhalt braucht einen eigenen Empty-State und hat keinen Zweck. |
| **F8** | **Ein Viewwechsel bumpt `ArtifactEpoch`** — die Formularwerte der alten View bleiben nicht im Canvas stehen. | Formularzustand pro View mitführen: `CanvasFormState` kommt als `IsFixed`-Cascade aus `DrylCanvas` und ließe sich nur durch Remount tauschen. Persistenz ist Phase 5. |

## 3. Kern — der Workspace-Zustand

`DRYL.Components/Canvas/CanvasWorkspace.cs`, Namensraum `DRYL.Components.Canvas`.

```csharp
/// <summary>One named artifact in a workspace — the unit A5 lets the user come back to.</summary>
public sealed class CanvasView
{
    public string Id { get; init; } = string.Empty;   // stable key: slug of the title
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public CanvasSpec? Spec { get; set; }
    public bool Removing { get; internal set; }        // exit animation in flight
}

/// <summary>The named views of one canvas surface, exactly one of them active.</summary>
public sealed class CanvasWorkspace
{
    public IReadOnlyList<CanvasView> Views { get; }
    public string? ActiveId { get; }
    public CanvasView? Active { get; }
    public event Action? OnChange;

    /// <summary>Opens the view with this title (creating it if new) and activates it.</summary>
    public CanvasView Open(string title, string? icon = null);
    public bool Activate(string id);
    /// <summary>Flags the view for its exit animation; the bar calls <see cref="Remove"/> when it ends.</summary>
    public void Close(string id);
    public void Remove(string id);
    public void Clear();
}
```

Regeln:

- **Id** = kleingeschriebener Slug des Titels (`"Auftrag 4711"` → `auftrag-4711`), bei Kollision mit
  `-2`, `-3` … Damit findet `Open("Übersicht")` die bestehende View wieder, statt eine zweite anzulegen.
- `Open` auf eine bestehende View **aktiviert** sie und lässt ihre Spec unangetastet.
- `Remove` der aktiven View aktiviert die nachbarschaftlich nächste (rechts, sonst links); ist danach
  keine mehr da, ist `ActiveId` null.
- `Close` auf eine bereits schließende View ist ein No-op (Doppelklick auf das `x`).
- Jede Mutation, die etwas verändert hat, feuert **einmal** `OnChange`.
- Kein `INotifyPropertyChanged`, kein Threading-Schutz — dieselbe Konvention wie `DrylRunBase`
  (Blazor-Renderer-Thread; Aufrufer marshallt).

## 4. Kern — `DrylCanvasWorkspace`

`DRYL.Components/Components/AI/DrylCanvasWorkspace.razor` (+ `.razor.css`), Namensraum `DRYL.Components`.

### 4.1 Parameter

| Parameter | Typ | Bedeutung |
| --- | --- | --- |
| `Workspace` | `CanvasWorkspace?` | Der Zustand. Ohne ihn rendert die Komponente nur den Empty-State. |
| `View` | `RenderFragment<CanvasView>?` | Wie die aktive View gerendert wird. Ohne den Slot rendert der Workspace selbst ein `DrylCanvas` mit `view.Spec`. |
| `AllowClose` | `bool` = `true` | Ob die Chips ein `x` tragen. |
| `ShowBarWhenSingle` | `bool` = `false` | Ob die Leiste auch bei einer einzigen View sichtbar ist. |
| `EmptyText` | `string?` | Text des Empty-States, wenn keine View existiert. |
| `AriaLabel` | `string` = `"Views"` | Label der Leiste. |
| `Class`, `AdditionalAttributes` | | Haus-Konvention (Merge, siehe `feedback_class_splat_clobber`). |

### 4.2 Markup

```razor
<div class="canvas-workspace @Class" @attributes="AdditionalAttributes">
    @if (ShowBar)
    {
        <div class="canvas-workspace-bar" role="tablist" aria-label="@AriaLabel" @ref="_bar">
            <div class="ws-ink" data-dryl-ink aria-hidden="true"></div>
            @foreach (var v in Workspace!.Views)
            {
                <DrylPresence @key="v.Id" Visible="@(!v.Removing)" Appear
                              Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Fast"
                              OnExited="() => Workspace.Remove(v.Id)">
                    <div class="ws-chip @(IsActive(v) ? "is-active" : null)"
                         data-dryl-ink-active="@(IsActive(v) ? "true" : null)">
                        <button type="button" role="tab" …>@v.Title</button>
                        @if (AllowClose) { <DrylTooltip …><DrylButton IconOnly …/></DrylTooltip> }
                    </div>
                </DrylPresence>
            }
        </div>
    }
    <div class="canvas-workspace-body">
        @if (Workspace?.Active is { } active) { @(View is not null ? View(active) : DefaultCanvas(active)) }
        else { <DrylEmptyState Title="No view yet">@EmptyText</DrylEmptyState> }
    </div>
</div>
```

- Der Gleiter ist derselbe wie bei `DrylTabs`: ein `[data-dryl-ink]`-Element plus
  `dryl.motion.moveIndicator(_bar)` nach jedem Render, `disposeIndicator` beim Entsorgen. Er ist
  `aria-hidden` (Regel 2.12).
- Der Body wird **nicht** gekeyt: die Canvas-Instanz bleibt über den Wechsel bestehen, nur ihre Spec
  wechselt — das ist die Voraussetzung dafür, dass der `view-transition-name` auf dem Canvas-Root
  einen Morph statt eines Austauschs ergibt.
- Chip-Aufbau: der Titel ist der `role="tab"`-Button, das `x` ist ein eigener Icon-Button **daneben**
  (nie verschachtelt) mit `DrylTooltip Text="Close view"` und gleichlautendem `aria-label` (Regel 2.11).

### 4.3 Wechsel, Tastatur, Motion

- **Wechsel:** `ViewTransition.RunAsync(() => { Workspace.Activate(id); StateHasChanged(); })`;
  `SignalRendered()` unbedingt in `OnAfterRender`. Damit morpht die Fläche (A8: eine Zustands-
  änderung = eine Bewegung), und die Leiste gleitet parallel über `moveIndicator`.
- **Tastatur:** ←/→ zwischen den Chips, Home/End an die Enden, Enter/Space aktiviert,
  `Delete`/`Backspace` schließt (nur wenn `AllowClose`). `tabindex` folgt dem Tabs-Muster
  (aktiver Chip 0, Rest −1).
- **Schließen:** `Close` setzt `Removing`; die `DrylPresence` des Chips spielt ihren Exit und ruft
  über `OnExited` `Remove` — dieselbe Choreografie wie beim Entfernen eines Canvas-Nodes.
- Bei genau einer View bleibt die Leiste aus (`ShowBarWhenSingle=false`): ein einzelnes Artefakt
  bekommt kein Chrome. Erscheint die zweite View, fährt die Leiste über `DrylPresence` ein.

### 4.4 CSS (`DrylCanvasWorkspace.razor.css`)

Nur Tokens, keine neuen. Leiste: `display:flex; gap: var(--sp-1); border-bottom: 1px solid var(--line);`
Chip: `border-radius: var(--r-md) var(--r-md) 0 0`, Ruhefarbe `var(--fg-muted)`, aktiv `var(--fg)`,
Hover ändert Rahmen/Glow statt Füllung. `.ws-ink`: 2px-Leiste mit `var(--accent-grad)`,
`transition: transform/width var(--dur-med) var(--ease-spring)`, aktiv erst ab `.is-ink-ready`.
Leiste horizontal scrollbar (`overflow-x:auto`), damit viele Views die Seite nicht umbrechen.
`prefers-reduced-motion: reduce` schaltet Gleiter- und Chip-Transitionen ab.

## 5. Agents — Run-Projektion

`DrylCanvasRun` bekommt die Workspace-Bindung; alles andere (Pulse, Round, ChangedIds,
AvailableWidth) bleibt runweit, weil immer nur **eine** Generierung gleichzeitig läuft.

```csharp
/// <summary>Binds the run to a workspace: from here on the run reads and writes the
/// active view's spec, and create/open generations fill the active view.</summary>
public void UseWorkspace(CanvasWorkspace workspace);
```

- Das bestehende Feld hinter `Spec` wird zu einem privaten `_spec`; `Spec` liest
  `_workspace is null ? _spec : _workspace.Active?.Spec`, ein privates `SetSpec(value)` schreibt
  entsprechend. Ist ein Workspace gebunden und keine View aktiv, legt `SetSpec` implizit die View
  `"Artifact"` an — ein `create_artifact` ohne vorheriges `open_view` funktioniert unverändert.
- Der Run abonniert `workspace.OnChange`. Wechselt `ActiveId`:
  1. `_artifactEpoch++` (F8 — Formularwerte der alten View bleiben nicht stehen),
  2. `_suppressSwapMorph = true`,
  3. `Raise()`.
- `DrylAiCanvas.HandleChange` fragt `Run.ConsumeSwapMorphSuppression()` ab, bevor es entscheidet, ob
  ein Spec-Instanzwechsel einen Morph auslöst (F3). Beide Typen liegen in `DRYL.Components.Agents`,
  die Methode ist `internal` — Hosts sehen davon nichts.
- `Purge` und die Reveal-Pfade arbeiten weiter auf `Spec`, also automatisch auf der aktiven View.

## 6. Agents — `open_view`

`DrylCanvasTools.Create(...)` und `CreateReplay(...)` bekommen einen optionalen Parameter
`CanvasWorkspace? workspace = null`. Nur wenn er gesetzt ist, enthält `All` ein drittes Tool:

```
open_view(name, brief, title?)
```

> „Open a **named view** on the workspace and build its artifact. Use this when the user turns to a
> new subject (a specific order, a second report) and the current artifact should stay reachable —
> the view bar keeps both. Re-using an existing name activates that view and rebuilds it. Put ALL
> concrete data the artifact needs into the brief."

Implementierung: `workspace.Open(name)` (aktiviert), dann **derselbe** Rumpf wie
`CreateArtifactImpl(brief, title ?? name, ct)` — keine zweite Generierungslogik. Der Receipt nennt
die View: `Artifact created in view "Auftrag 4711": 7 elements, 2 inputs.`

`create_artifact` bleibt unverändert und meint immer die **aktive** View („ersetze das hier").
Die Tool-Beschreibung von `create_artifact` bekommt einen Satz, der beide auseinanderhält.

## 7. Agents — `DrylCanvasDock`

`DRYL.Components.Agents/Canvas/DrylCanvasDock.razor` (+ `.razor.css`), Namensraum `DRYL.Components.Agents`.

### 7.1 Parameter

| Parameter | Typ | Bedeutung |
| --- | --- | --- |
| `Run` | `DrylCanvasRun?` | Quelle der Statuszeile und des Aura-Zustands. |
| `Busy` | `bool` | Der Host ist beschäftigt (Chat-Turn läuft) — sperrt den Composer. |
| `OnSend` | `EventCallback<string>` | Der abgeschickte Text. |
| `Corner` | `DockCorner` = `BottomRight` | `BottomRight / BottomLeft / TopRight / TopLeft`. |
| `Placeholder` | `string?` = `"Ask for a view…"` | Composer-Platzhalter. |
| `Status` | `string?` | Überschreibt die abgeleitete Statuszeile. |
| `Log` | `RenderFragment?` | Der aufklappbare Verlauf (F5). Ohne ihn entfällt der Aufklapp-Knopf. |
| `Collapsed` + `CollapsedChanged` | `bool` | Zweiweg — das Dock kollabiert zu einem Icon. |
| `Title` | `string` = `"Assistant"` | Label des kollabierten Knopfes. |
| `Class`, `AdditionalAttributes` | | Haus-Konvention. |

`DockCorner` ist ein neues `enum` in `DRYL.Components.Agents`.

### 7.2 Aufbau

```razor
<div class="canvas-dock canvas-dock--@CornerCss @(Collapsed ? "is-collapsed" : null)"
     popover="@PopoverMode" @ref="_el">
    <DrylPresence Visible="@Collapsed" Transition="PresenceTransition.Scale" Speed="PresenceSpeed.Fast">
        <DrylTooltip Text="@Title">
            <DrylButton IconOnly AriaLabel="@Title" Ai="@DockAi" OnClick="Expand"><DrylIcon Name="Sparkle"/></DrylButton>
        </DrylTooltip>
    </DrylPresence>

    <DrylPresence Visible="@(!Collapsed)" Transition="PresenceTransition.Scale" Speed="PresenceSpeed.Fast">
        <div class="dock-panel glass-card">
            <div class="dock-head">
                <DrylAiIndicator State="@DockAi" />
                <span class="dock-status" aria-live="polite">…Statuszeile…</span>
                @if (Log is not null) { …Aufklapp-Knopf… }
                …Kollabieren-Knopf…
            </div>
            <div class="dock-log" role="log" aria-hidden="@(_logOpen ? null : "true")" @ref="_logEl">
                <div class="dock-log-inner"><div class="dock-log-content">@Log</div></div>
            </div>
            <DrylChatComposer @bind-Value="_draft" OnSend="Send" Disabled="@Busy"
                              Placeholder="@Placeholder" Ai="@DockAi" />
        </div>
    </DrylPresence>
</div>
```

- **Statuszeile (A6).** Genau eine Zeile, abgeleitet aus dem Run — `Status` schlägt sie:
  | Zustand | Text |
  | --- | --- |
  | `Run.Error is not null` | die Fehlermeldung, Ton `var(--danger)` |
  | `Run.State == Streaming` | `Building · {NodeCount} elements` (Singular sauber) |
  | `Run.State == Thinking` oder `Busy` | `Working…` |
  | `Run.State == Generated` | `Ready · {NodeCount} elements` |
  | sonst | `Idle` |
  Der Textwechsel ist eine Bewegung, kein Sprung: der Text steckt in einer `DrylPresence`
  (`Fade`, `Fast`), die über `@key="StatusText"` neu montiert wird — dasselbe Re-Key-Mittel wie beim
  Change-Pulse. `aria-live="polite"` (Regel 2.9).
- **Verlauf.** `0fr → 1fr`-Disclosure exakt wie `DrylToolCallGroup` (Inhalt wird nie gequetscht,
  bleibt im DOM), `max-height: 42vh`, `overflow-y:auto`, nach jedem Render beim Öffnen
  `dryl.chat.scrollToEnd(_logEl)` — derselbe Helfer, den `DrylChat` benutzt.
- **Kollabieren.** Ein Beat: Panel skaliert zur Ecke weg, der Icon-Knopf skaliert auf
  (`PresenceTransition.Scale`, `--ease-spring` steckt in der Primitive). Beschäftigt der Run, trägt
  auch der kollabierte Knopf die Aura — man sieht am Rand, dass gearbeitet wird.
- **Icon-only-Knöpfe** (Aufklappen, Kollabieren, kollabiertes Dock) tragen `DrylTooltip` **und**
  gleichlautendes `aria-label` (Regel 2.11).

### 7.3 Layer und Positionierung (F4)

- Root ist `position: fixed`, Ecke über `--dock-inset: var(--sp-4)` und Klassen
  `.canvas-dock--br|bl|tr|tl` (`inset: auto`, dann die zwei relevanten Kanten). `popover`-Defaults
  (`margin:auto; inset:0`) werden explizit überschrieben.
- Nach dem ersten Render: `_topLayer = true` → Re-Render mit `popover="manual"` → im nächsten
  `OnAfterRender` `dryl.topLayer.show(_el)`. Schlägt der Interop fehl (Prerender, kein JS, altes
  Browser-Ziel), bleibt das Attribut aus und das Dock steht als gewöhnliches `fixed`-Element da —
  genau der Fallback, den `dryl.topLayer` beschreibt. `hide` beim Entsorgen.
- Unter 640 px (`@media`, weil das Dock am Viewport hängt und nicht an einem Container): volle
  Breite unten, `inset: auto var(--sp-3) var(--sp-3)`, Log `max-height: 50vh`.
- **Zu prüfen (siehe §11):** `DrylTooltip` portiert nach `<body>` und liegt damit unter dem
  Top-Layer. Dasselbe gilt heute schon für die Kopfknöpfe des ausgeklappten `DrylCanvas`; wenn die
  Tooltips dort sichtbar sind, sind sie es hier auch.

## 8. Demo, Katalog, Doku

- **Neue Seite** `DRYL.Website/Components/Pages/DemoCanvasWorkspace.razor` unter
  `/components/canvas-workspace`:
  - **Replay-Variante** (`Components/Examples/Agents/CanvasWorkspaceDemo.razor`): ein Skript aus drei
    Zügen über `DrylCanvasTools.CreateReplay` — „Übersicht" bauen, per `open_view` „Auftrag 4711"
    eröffnen, zurück auf „Übersicht" wechseln. Zeigt Leiste, Gleiter, Morph, Dock-Status, Schließen.
  - **Live-Variante** hinter `OpenAi.IsConfigured`, gebaut wie `OpenAiCanvasArtifacts`, aber mit
    Workspace + Dock statt `DrylChat`-Spalte.
- **ComponentCatalog:** zwei Einträge (`DrylCanvasWorkspace`, `DrylCanvasDock`), beide auf diese
  Seite; der `DrylAiCanvas`-Eintrag bleibt, wie er ist.
- **CHANGELOG.md:** `[Unreleased]` → Release `2.14.0` im selben Commit, `Added` für
  `DrylCanvasWorkspace`, `CanvasWorkspace`/`CanvasView`, `DrylCanvasDock`, `open_view`,
  `DrylCanvasRun.UseWorkspace`.
- **Versionen:** `DRYL.Components.csproj` `2.13.0 → 2.14.0`, `DRYL.Components.Agents.csproj`
  `0.11.0 → 0.12.0`. `publish.yml` liest beide Versionen und taggt `v2.14.0` bzw. `agents-v0.12.0` —
  die Roadmap-Warnung zu Phase-Releases ist damit erledigt, es ist kein Handgriff nötig.
- **Projektnotiz** `project_ai_canvas` / `project_canvas_platform` fortschreiben (DoD 8).

## 9. Nicht-Ziele

- Kein URL-/Routing-Sync der aktiven View — der Host kann das über `Workspace.OnChange` selbst.
- Keine Mehrbenutzer-Synchronisierung.
- **Kein Weiterbauen in einer unsichtbaren View**: `open_view` aktiviert, bevor es baut. Es gibt zu
  jedem Zeitpunkt genau eine Generierung.
- Keine viewübergreifende Persistenz von Formularwerten — Phase 5.
- Kein Anlegen leerer Views durch den Nutzer (F7).
- Kein eigenes Nachrichtenmodell im Dock (F5); keine neuen Tokens, Durations oder Easings.

## 10. Tests

`tests/DRYL.Components.Tests`:

**Unit — `CanvasWorkspaceTests`**
1. `Open` legt an, aktiviert und feuert `OnChange`; `Open` mit demselben Titel aktiviert die
   bestehende View und legt keine zweite an.
2. Slug-Kollision bekommt `-2`.
3. `Remove` der aktiven View aktiviert die nächste; die letzte zu entfernen lässt `ActiveId` null.
4. `Close` setzt `Removing`, entfernt aber noch nichts; zweimal `Close` feuert nur einmal.

**Unit — `CanvasRunWorkspaceTests`**
5. Nach `UseWorkspace` schreibt eine Create-Generierung in die aktive View; ohne aktive View entsteht
   die View `"Artifact"`.
6. Ein `Activate` bumpt `ArtifactEpoch` und feuert `OnChange`.
7. `ConsumeSwapMorphSuppression` liefert genau einmal `true` und danach `false`.

**bUnit — `DrylCanvasWorkspaceTests`**
8. Zwei Views → Leiste mit zwei Chips, der aktive trägt `aria-selected="true"` und
   `data-dryl-ink-active`.
9. Eine View → keine Leiste; mit `ShowBarWhenSingle` → Leiste.
10. Klick auf einen Chip aktiviert die View und rendert deren Spec; `View`-Slot wird mit der aktiven
    View aufgerufen.
11. Klick auf `x` setzt `Removing` (View noch da) — der Ausbau folgt erst nach dem Exit.
12. ←/→/Home/End bewegen die Auswahl.

**bUnit — `DrylCanvasDockTests`**
13. Statuszeile spiegelt den Run (Streaming → `Building · 3 elements`; Error → Fehlertext).
14. `OnSend` feuert mit dem Text des Composers; `Busy` sperrt ihn.
15. Kollabiert rendert nur den Icon-Knopf mit `aria-label`; Ausklappen stellt das Panel her.
16. Ohne `Log` gibt es keinen Aufklapp-Knopf.

**Replay — `CanvasOpenViewToolTests`**
17. Ohne Workspace enthält `All` zwei Tools, mit Workspace drei.
18. `open_view` legt die View an, aktiviert sie, füllt ihre Spec und nennt sie im Receipt; ein
    zweiter Aufruf mit demselben Namen baut in dieselbe View.

Prerender/JS-Interop nach `feedback_prerender_js_dispose` absichern (`_attached`-Flag am Dock und am
Gleiter des Workspace).

## 11. Manuelle Prüfung (DoD 5/7)

- Beide Farbmodi, 375 px, `prefers-reduced-motion: reduce`.
- Der Wechsel morpht **einmal** (kein Doppel-Morph, kein Sprung) — A8.
- Das Dock liegt über einer Seite mit `backdrop-filter`-Karten wirklich am Viewport (F4).
- Tooltips der Dock-Knöpfe sind im Top-Layer sichtbar (§7.3); falls nicht, wird das als eigener
  Befund an `DrylTooltip` notiert, nicht im Dock umgangen.
- Tastaturweg: Composer → Aufklappen → Chips → Canvas, ohne Falle.

## 12. Risiken

| Risiko | Antwort |
| --- | --- |
| Doppelte View-Transition (Workspace **und** `DrylAiCanvas`) verschluckt eine Mutation | F3 — die Unterdrückung ist ein `internal` Einmal-Flag am Run und wird in Test 7 festgenagelt. |
| Top-Layer-Dock verdeckt Seiteninhalt oder frisst Klicks | Der Root ist nur so groß wie das Panel (`inset:auto`, kein `width:100%`), kollabiert nur so groß wie der Knopf; kein Backdrop. |
| `moveIndicator` misst eine Leiste, die gerade erst einfährt | `is-ink-ready` schaltet die Transition erst nach der ersten Platzierung frei — dasselbe Verhalten wie bei `DrylTabs`. |
| `open_view` als drittes Tool kostet Prompt-Budget in jeder Runde | Es wird nur registriert, wenn ein Workspace übergeben wurde; Chat-Artefakte ohne Workspace sehen es nie. |
