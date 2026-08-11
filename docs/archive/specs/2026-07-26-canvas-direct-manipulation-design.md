# Canvas Phase 6 — Direct Manipulation (Detail-Spec)

**Datum:** 2026-07-26
**Status:** freigegeben
**Rahmen:** Phase 6 aus `2026-07-25-canvas-platform-roadmap.md` — bindend sind A1–A9 und die
globalen Nicht-Ziele. Ausgangsstand: Kern **2.16.1**, Agents **0.13.0**.
**Zielversionen:** Kern → **2.17.0** (MINOR), Agents → **0.14.0** (MINOR).

## 1. Ziel

Der Nutzer kann ein Element **anfassen** statt es nur zu beschreiben: es auswählen, dazu prompten,
es anheften, duplizieren, entfernen und innerhalb seines Containers umsortieren.

Damit schließt sich die letzte Lücke aus §2 der Roadmap („Man kann nichts direkt anfassen"). Nach
Phase 6 hat das Agents-Paket alles, was es für 1.0.0 braucht.

Fünf Bausteine:

1. **`CanvasSelection`** (Kern) — der geteilte, beobachtbare Selektionszustand einer Canvas-Fläche.
   Genau ein Node ist ausgewählt; Renderer und Dock lesen dasselbe Objekt.
2. **Selektion im Renderer** — `CanvasNodeView` wird klick- und tastaturbedienbar, `DrylCanvas`
   löst Navigation und Kommandos gegen den Baum auf.
3. **Werkzeugleiste am Node** — Prompten / Anheften / Duplizieren / Entfernen, plus Drag-Griff.
4. **Pin** — `CanvasNode.Locked`; der Patcher lehnt **AI**-Ops auf angehefteten Nodes ab und sagt
   im Receipt warum.
5. **Kontext-Chip im Dock** (Agents) — die Selektion reist als kompakte Referenzzeile in den Brief.

**Nicht-Ziele**

- Kein freies Layout, kein Property-Inspector — geprompted wird weiterhin in Sprache (Roadmap).
- Kein Drag **zwischen** Containern (§8), kein Drag-and-Drop-Designer.
- Keine Mehrfachselektion.
- Kein Undo speziell für Direktmanipulation — sie nutzt die Historie aus Phase 5 (§12).
- Keine neuen Tokens, Farben, Durations oder Easings; kein neues Icon (§13).
- Kein neuer Node-Typ, keine Katalog-Erweiterung.

## 2. Die fünf tragenden Entscheidungen

| # | Frage | Entscheidung |
| --- | --- | --- |
| E1 | Was ist selektierbar? | **Jeder Node außer dem Root.** Der innerste Node unter dem Zeiger gewinnt; ein Container wird über seinen eigenen Rand/Padding getroffen. |
| E2 | Wie weit reicht ein Pin? | **Der Node und seine Kinderliste.** Abgelehnt: `setProps`/`remove`/`move` auf den Node selbst sowie `insert`/`move` in seine Kinderliste hinein oder aus ihr heraus. Die Nachfahren selbst bleiben editierbar. |
| E3 | Wie weit geht Drag-Reorder? | **Nur innerhalb desselben Containers** — Geschwister umsortieren, keine Drop-Zonen zwischen Containern. |
| E4 | Wie kommt die Selektion in den Prompt? | **Das Dock präfixt automatisch** eine kompakte Referenzzeile; der Chip ist per X abwählbar. |
| E5 | Wen bindet der Pin? | **Nur den AI-Autor.** Datenrefresh, Action-Ergebnisse und die eigenen Werkzeugleisten-Kommandos des Nutzers gehen durch (A4: was der Mensch auslöst, passiert). |

E5 ist der einzige Punkt, den die Roadmap offen ließ. Begründung: Der Pin ist eine Ansage an das
Modell („fass das nicht an"), keine Einfrierung des Widgets. Ein gepinnter Umsatz-Chart soll
weiterhin seine Zahlen aktualisieren, sonst pinnt niemand etwas.

## 3. `CanvasSelection` — der geteilte Zustand

Neue Datei `DRYL.Components/Canvas/CanvasSelection.cs`, Namensraum `DRYL.Components.Canvas`.
Beobachtbarer Renderer-Thread-Zustand wie `CanvasWorkspace` — kein Locking, kein
`INotifyPropertyChanged`, ein `OnChange` pro echter Änderung.

```csharp
public sealed class CanvasSelection
{
    public string? Id { get; private set; }          // null = nichts ausgewählt
    public string? Type { get; private set; }        // Katalogtyp des ausgewählten Nodes
    public string? Label { get; private set; }       // sprechende Kurzbezeichnung (§3.1)
    public bool Locked { get; private set; }         // Pin-Zustand des ausgewählten Nodes

    public bool HasSelection => Id is not null;

    public event Action? OnChange;
    public event Action? OnPromptRequested;

    public void Select(CanvasNode node, bool focus = false);
    public void Clear();
    public void RequestPrompt();                     // "zu diesem Element prompten"

    internal int FocusTick { get; }                  // §4.3
}
```

- **`Select`** ist idempotent: derselbe Node mit denselben Werten löst kein `OnChange` aus. `focus:
  true` erhöht zusätzlich `FocusTick`, worauf der zugehörige `CanvasNodeView` sich beim nächsten
  Render den Fokus holt (Tastaturnavigation, §4.3). Ein Klick selektiert mit `focus: false` — der
  Browser hat den Fokus dann schon gesetzt.
- **`Clear`** setzt alle vier Werte auf `null`/`false`.
- **`RequestPrompt`** feuert `OnPromptRequested`; das Dock klappt auf und fokussiert seinen
  Composer (§9). Ohne Dock ist es ein No-op — die Selektion allein ist bereits nutzbar.
- `Locked` wird bei jedem `Select` mitgeführt und **nach jedem Pin-Toggle neu gesetzt**, damit die
  Werkzeugleiste und der Chip denselben Zustand sehen.

### 3.1 `CanvasLabel` — die sprechende Kurzbezeichnung

Neue Datei `DRYL.Components/Canvas/CanvasLabel.cs`. Eine statische Funktion, die aus einem Node den
Text macht, den Mensch **und** Modell verstehen:

```csharp
public static string For(CanvasNode node);   // z.B. "Umsatz je Monat", "Auftrag freigeben", "Grid"
```

Reihenfolge der Quellen aus den Props, erste nicht-leere gewinnt:
`title` → `label` → `text` → `submitLabel` → `name` → `content` (erste Zeile, max. 40 Zeichen).
Fällt alles aus, liefert sie einen lesbaren Typnamen (`"lineChart"` → `"Line chart"`,
`"keyValue"` → `"Key value"`). Ergebnis wird auf **60 Zeichen** gekappt (mit `…`).

Die Funktion ist `public`, weil das Dock (anderes Paket) sie für den Chip braucht.

## 4. Selektion im Renderer

### 4.1 Aktivierung

`DrylCanvas` bekommt **einen** neuen Parameter:

```csharp
[Parameter] public CanvasSelection? Selection { get; set; }
```

Ein gesetztes `Selection` **ist** der Schalter — kein zusätzliches `AllowSelection`-Flag. Ohne
Selection ändert sich an einem bestehenden Canvas kein Attribut, kein Tabstopp, kein Pixel.
`DrylAiCanvas` reicht den Parameter unverändert an `DrylCanvas` durch.

`CanvasContext` bekommt `public CanvasSelection? Selection { get; internal set; }` sowie zwei
interne Delegates, die `DrylCanvas` setzt (nur es kennt den ganzen Baum und besitzt den Spec):

```csharp
internal Func<string, CanvasNav, bool>? Navigate { get; set; }   // Tastaturnavigation
internal Func<string, CanvasNodeCommand, Task>? Command { get; set; }  // Pin/Duplizieren/Entfernen/Reorder
```

```csharp
public enum CanvasNav { Previous, Next, Parent, FirstChild, First, Last }
public enum CanvasNodeCommand { TogglePin, Duplicate, Remove, MoveUp, MoveDown }
```

### 4.2 Trefferregel (E1)

`CanvasNodeView` rendert den Wrapper künftig als:

```razor
<div class="canvas-node @SelectionCss" data-cid="@Node.Id"
     tabindex="@TabIndex" aria-label="@AriaLabel"
     @onclick="SelectSelf" @onclick:stopPropagation="true"
     @onfocus="() => _focused = true" @onblur="() => _focused = false"
     @onkeydown="OnNodeKeyDown">
```

- `@onclick:stopPropagation` liefert E1 „innerster gewinnt" ohne jede Trefferrechnung: der Klick
  erreicht den äußeren Container nur, wenn er dessen eigenes Padding traf.
- **Der Root-Node bekommt weder `tabindex` noch Klick-Handler** (E1) — er ist das Artefakt selbst.
- `tabindex` ist ein **Roving Tabindex**: genau ein Node im Baum trägt `0` — der ausgewählte, oder
  (ohne Auswahl) das erste Kind des Roots. Alle anderen tragen `-1`. Der ganze Artefaktbaum kostet
  damit **einen** zusätzlichen Tabstopp, nicht vierzig.
- Interaktive Inhalte (Inputs, Buttons, Charts) bleiben unverändert erreichbar; der Wrapper liegt
  im Tab-Fluss davor.
- `aria-label` ist `"{CanvasLabel.For(node)}, {type}"` plus `", pinned"` wenn `Locked`.

Ein Klick auf ein Bedienelement **innerhalb** eines Nodes (Button, Input, Tab) selektiert den Node
nicht: das Bedienelement stoppt die Propagation ohnehin nicht, aber der Wrapper prüft vor dem
Selektieren nichts — gewollt. Wer im Formular tippt, hat den Node vorher fokussiert; die Selektion
ist eine harmlose Nebenwirkung und macht die Werkzeugleiste genau dort sichtbar, wo gearbeitet wird.

### 4.3 Tastatur

Der Wrapper verarbeitet Tasten **nur, wenn er selbst den Fokus hat** (`_focused`, gesetzt über
`@onfocus`/`@onblur` — `focus` bubblet nicht, ein Input im Node kann die Navigation also nicht
kapern):

| Taste | Wirkung |
| --- | --- |
| `ArrowUp` / `ArrowDown` | vorheriges / nächstes Geschwister (`CanvasNav.Previous/Next`) |
| `ArrowLeft` | Elternknoten (nie der Root) |
| `ArrowRight` | erstes Kind (nur Container) |
| `Home` / `End` | erstes / letztes Geschwister |
| `Alt` + `ArrowUp`/`ArrowDown` | Node **verschieben** — die tastaturbedienbare Form von Drag-Reorder (§8) |
| `Enter` | `Selection.RequestPrompt()` — zu diesem Element prompten |
| `Delete` / `Backspace` | Node entfernen (gesperrt bei `Locked`) |
| `Escape` | Auswahl aufheben |

Jede Navigation ruft `Ctx.Navigate(Node.Id, dir)`; `DrylCanvas` löst das Ziel gegen den Spec-Baum
auf und ruft `Selection.Select(target, focus: true)`. Der `CanvasNodeView` des Ziels sieht beim
nächsten Render einen neuen `FocusTick` und ruft in `OnAfterRenderAsync` `_el.FocusAsync()`.

**Scrollen unterdrücken:** `@onkeydown:preventDefault` ist ein pro Render ausgewerteter Ausdruck,
kein Rückgabewert des Handlers. Der Wrapper trägt deshalb
`@onkeydown:preventDefault="@_ownsKeys"` mit `_ownsKeys = _focused && Selectable` — genau dann,
wenn dieser Node den Fokus hat, sind Pfeiltasten Navigations- und keine Scroll-Tasten. Ein Input
im Node hat den Fokus selbst, `_focused` ist dort `false`, und der Event erreicht den Wrapper mit
unverändertem Default-Verhalten.

### 4.4 Ankündigung

`DrylCanvas` bekommt eine **zweite** visually-hidden Live-Region neben der bestehenden:

```razor
<div class="canvas-live" aria-live="polite">@_selectionAnnouncement</div>
```

Getrennt von `Announcement`, damit eine Selektionsmeldung keine AI-Meldung überschreibt. Texte
(englisch wie die ganze Bibliothek): `"Selected: {label}, {type}"`, `"Selection cleared."`,
`"{label} pinned."` / `"{label} unpinned."`, `"{label} duplicated."`, `"{label} removed."`,
`"{label} moved to position {n} of {m}."`

### 4.5 Selektion und Lebenszyklus

- Ein Spec-**Instanzwechsel** (neues Artefakt, View-Wechsel, Undo/Restore) löscht die Selektion —
  `DrylCanvas` erkennt das bereits über `_boundSpec` (`OnParametersSet`).
- Wird der ausgewählte Node entfernt (durch den Nutzer, einen AI-Patch oder `Purge`), löscht
  `DrylCanvas` die Selektion.
- Zwei Canvas-Flächen auf einer Seite teilen sich nichts: die Selektion ist ein Host-Objekt pro
  Fläche, genau wie der `CanvasWorkspace`.

## 5. Die Werkzeugleiste am Node

Gerendert **innerhalb** des ausgewählten `.canvas-node`, absolut positioniert an dessen oberer
rechter Ecke, in einem `DrylPresence` (`Scale`, `Fast`) — sie erscheint und verschwindet mit
Bewegung (Regel 2.12). Weil sie im `[data-cid]`-Element liegt, reitet sie bei FLIP-Glides mit.

Fünf Bedienelemente, alle **icon-only** und damit alle in einem `DrylTooltip` mit gleichlautendem
`AriaLabel` (Regel 2.11):

| Icon | Tooltip / aria-label | Wirkung |
| --- | --- | --- |
| `Sparkle` | „Prompt about this element" | `Selection.RequestPrompt()` |
| `Lock` (`Pressed="Locked"`) | „Pin element" / „Unpin element" | `CanvasNodeCommand.TogglePin` |
| `Copy` | „Duplicate element" | `CanvasNodeCommand.Duplicate` |
| `Trash` (`Variant="Danger"`) | „Remove element" | `CanvasNodeCommand.Remove` |
| `GripVertical` | „Reorder element" | Drag-Griff (§8), `data-drag-handle` |

Regeln:

- Bei `Locked` sind **Duplizieren, Entfernen und der Drag-Griff deaktiviert** (`Disabled`) — E2
  wirkt sichtbar, nicht nur im Receipt. Der Pin-Knopf selbst bleibt bedienbar.
- Hat der Node **kein** Geschwister, entfällt der Drag-Griff.
- Ohne `DrylTooltip`-tauglichen Kontext (Touch) greift die bestehende Tooltip-Mechanik unverändert.
- Ein angehefteter Node zeigt sein Schloss **auch ohne Selektion**: ein 12px `Lock`-Icon in
  `var(--fg-dim)` an derselben Ecke (`.canvas-node-pin`, `aria-hidden` — der Zustand steht bereits
  im `aria-label` des Wrappers).

## 6. Pin — `CanvasNode.Locked` und die Patcher-Regel

### 6.1 Das Feld

```csharp
/// <summary>Pinned by the user: the AI author may not change, move or remove this node …</summary>
public bool Locked { get; set; }
```

Serialisiert als `"locked": true` (kein `[JsonIgnore]` — anders als `Removing`/`Version`). Damit
überlebt ein Pin Speichern und Laden (Phase 5) **ohne Schemabruch**: ein zusätzliches optionales
Feld lässt `CanvasDocument.CurrentSchema` bei `1`, ältere Dokumente lesen sich unverändert.

`false` wird nicht serialisiert (Default-Ignorierung ist in `CanvasJson.Options` nicht aktiv →
explizit `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]`), damit der
Spec-JSON-Text im Update-Prompt nicht um ein `"locked": false` pro Node wächst.

### 6.2 Die Regel im Patcher

`CanvasPatcher.Apply` bekommt einen dritten Parameter:

```csharp
public enum CanvasPatchAuthor { User, Ai }

public static string? Apply(CanvasSpec spec, CanvasOp op,
                            CanvasPatchAuthor author = CanvasPatchAuthor.User);
```

Default `User` = bestehendes Verhalten; **nur** `DrylCanvasRun.ApplyOp` (der AI-Pfad) übergibt
`Ai`. Damit gilt E5 ohne Sonderfälle: Action-Ergebnisse, Datenrefresh und die
Werkzeugleisten-Kommandos gehen unverändert durch.

Für `author == Ai` prüft der Patcher **vor** jeder Mutation (E2):

| Op | abgelehnt wenn |
| --- | --- |
| `setProps` | Zielnode ist `Locked` |
| `remove` | Zielnode ist `Locked` |
| `move` | Zielnode ist `Locked` **oder** alter Elternknoten ist `Locked` **oder** neuer Elternknoten ist `Locked` |
| `insert` | Elternknoten ist `Locked` |

Ein `Locked`-Container schützt also seine Zusammensetzung und seine Position; was **in** seinen
Kindern passiert, bleibt frei (E2).

Skip-Gründe, modellgerichtet und korrigierend wie alle bestehenden:

- `op 'setProps': node 'c3' is pinned by the user — leave it unchanged and say so if asked.`
- `op 'remove': node 'c3' is pinned by the user — it must stay.`
- `op 'move': node 'c3' is pinned by the user — its position must stay.`
- `op 'move': node 'c7' is pinned by the user — nothing may be moved out of or into it.`
- `op 'insert': node 'c7' is pinned by the user — nothing may be added to it.`

Sie fließen über den vorhandenen `skipped`-Pfad in den `update_artifact`-Receipt (`DrylCanvasTools`,
unverändert) und damit in den nächsten Modellzug.

### 6.3 Pin setzen

`CanvasNodeCommand.TogglePin` läuft **nicht** über den Patcher — es ist eine Nutzeraktion auf einem
Metadatenfeld, kein Inhaltspatch. `DrylCanvas` setzt `node.Locked` direkt, erhöht `node.Version`
(damit die Memoisierung im View neu greift), aktualisiert `Selection` und kündigt an. Kein
`Pulse.Stamp` — der Pin ändert keinen Inhalt (A8: die Bewegung ist die Werkzeugleiste, die ihren
Zustand wechselt).

## 7. Duplizieren und Entfernen

### 7.1 `CanvasNodeClone`

Neue Datei `DRYL.Components/Canvas/CanvasNodeClone.cs`:

```csharp
public static CanvasNode Duplicate(CanvasNode node, IReadOnlySet<string> existingIds);
```

- Tiefkopie über `CanvasJson` (Roundtrip), damit Props, `data`- und `action`-Bindungen mitkommen.
- **Ids werden neu vergeben**: `"{id}-2"`, `"{id}-3"`, … bis frei, rekursiv für den ganzen Teilbaum;
  gegen `existingIds` **und** gegen die bereits im Klon vergebenen Ids geprüft.
- **`Locked` wird nicht kopiert** — eine Kopie startet ungepinnt. Wer sie schützen will, pinnt sie.
- **Interaktive Nodes bekommen einen neuen `name`** nach derselben Regel (`"{name}-2"`), sonst
  teilen sich Original und Kopie einen Formularwert. Betroffen: `inputText`, `select`, `slider`,
  `toggle` (`CanvasCatalog.IsInteractive`).

`DrylCanvas` fügt den Klon per `insert`-Op direkt hinter dem Original ein (`parent` = Elternknoten,
`index` = Originalindex + 1) und selektiert ihn. Die Bewegung liefert die bestehende
Presence-Enter-/FLIP-Schicht (A8).

### 7.2 Entfernen

`CanvasNodeCommand.Remove` erzeugt eine `remove`-Op → `Removing = true` → Exit-Animation →
`OnExited` → `Ctx.Purge`.

**Fix nebenbei:** Heute setzt `DrylCanvas` `_ctx.Purge = id => OnPurge.InvokeAsync(id)` — hat der
Host keinen Handler (jeder `DrylCanvas` ohne Run), bleibt der Node für immer als unsichtbares
`Removing`-Element im Baum. Künftig: ohne `OnPurge.HasDelegate` entfernt `DrylCanvas` den Node
selbst aus `Spec`. Changelog unter `Fixed`.

## 8. Drag-Reorder (E3)

Nur zwischen **Geschwistern desselben Elternknotens**. Umgesetzt in JS, weil ein Zeigergeste-Loop
über den Circuit nicht funktioniert; das Ergebnis ist genau **eine** `move`-Op in .NET.

### 8.1 `dryl-canvas.js`

Zwei neue Exports neben `observe`/`unobserve`:

```js
export function initReorder(root, dotnet) { … }
export function disposeReorder(root) { … }
```

- Ein delegierter `pointerdown` auf `root`, gefiltert auf `[data-drag-handle]`.
- Der gezogene Node ist `handle.closest('[data-cid]')`. **Geschwister** sind alle `[data-cid]`
  im `root`, deren nächster `[data-cid]`-Vorfahre derselbe ist — dieselbe Ankerlogik, die
  `dryl.motion.autoFlip` benutzt, und damit robust gegen die `DrylPresence`-Wrapper zwischen
  Node und Node.
- Während der Geste: `pointercapture`, der gezogene Node bekommt `.is-dragging` und folgt dem
  Zeiger über `transform` (compositor-only). Kein Blazor-Render, kein DOM-Umbau.
- **Zielindex** aus den Mittelpunkten der Geschwister entlang der Achse mit der größeren Streuung
  (deckt Stack **und** Grid ab).
- **Einfügemarke** ohne DOM-Mutation: JS setzt `data-drop-before` bzw. `data-drop-after` auf das
  betroffene Geschwister; die Marke zeichnet CSS als `::before`/`::after` (§13).
- `pointerup`: Attribute und Transform zurück, und **nur bei geändertem Index**
  `dotnet.invokeMethodAsync('OnNodeReorder', cid, index)`.
- `Escape` oder `pointercancel` bricht ohne Callback ab.
- `touch-action: none` auf dem Griff (§13), damit Touch-Drag nicht scrollt.

### 8.2 .NET-Seite

```csharp
[JSInvokable] public Task OnNodeReorder(string id, int index)
```

auf `DrylCanvas`; wendet eine `move`-Op (`parent` = aktueller Elternknoten, `index`) als
`CanvasPatchAuthor.User` an. Danach glidet die bestehende `autoFlip`-Schicht alle betroffenen
Geschwister an ihre neue Position — eine Op, eine Bewegung (A8). `Alt`+Pfeil (§4.3) erzeugt
dieselbe Op und ist damit der tastaturbedienbare Zwilling.

Init/Dispose laufen im vorhandenen `OnAfterRenderAsync`/`DisposeAsync`-Block von `DrylCanvas`,
**nur wenn `Selection` gesetzt ist**.

## 9. Der Kontext-Chip im Dock (Agents, E4)

`DrylCanvasDock` bekommt:

```csharp
[Parameter] public CanvasSelection? Selection { get; set; }
```

- Ohne Selection ändert sich nichts am Dock.
- Mit Selection **und** aktiver Auswahl erscheint über dem Composer ein Chip in einem
  `DrylPresence` (`SlideUp`, `Fast`): das Node-Label, sein Typ als `DrylBadge`, und ein
  icon-only `X`-Knopf („Clear context", mit Tooltip) → `Selection.Clear()`.
- Das Dock abonniert `Selection.OnChange` (Neurender) und `Selection.OnPromptRequested`: letzteres
  klappt das Dock auf (`Collapsed = false`) und fokussiert den Composer.
- **Präfix beim Senden** (E4). `SendAsync` stellt dem Text genau eine Zeile voran:

  ```
  Regarding the artifact element "c3" (lineChart, "Revenue by month"):
  <Text des Nutzers>
  ```

  Ohne Auswahl geht der Text unverändert raus. Die Referenz nennt die **Id**, weil das Modell in
  `update_artifact` genau damit patcht, und Typ + Label, damit es die Id nicht raten muss.
- Nach dem Senden bleibt der Chip stehen — eine Folgeanweisung meint fast immer dasselbe Element.

### 9.1 `DrylChatComposer.FocusAsync`

Der Composer bekommt eine öffentliche Methode:

```csharp
public ValueTask FocusAsync();   // fokussiert das <textarea>
```

Additiv, Kern-MINOR. Das Dock hält per `@ref` einen Verweis und ruft sie bei
`OnPromptRequested`.

## 10. Der Prompt-Vertrag

Zwei minimale Ergänzungen in `CanvasPrompt` (Agents) — das Token-Budget aus Phase 4 verträgt keine
Absätze:

1. **`SchemaText`**, eine Zeile am Ende des Node-Blocks:

   ```
   - Any node may carry "locked": true — the user pinned it. Never change, move or remove a pinned node, and add nothing to it.
   ```

2. **`UpdatePrompt`**, eine Zeile im Op-Block:

   ```
   Nodes marked "locked": true are pinned by the user — no op may target them; report that instead of trying.
   ```

Mehr nicht: die Durchsetzung liegt im Patcher, die Korrektur im Receipt. Der Prompt spart dem
Modell nur den vermeidbaren Fehlversuch.

## 11. Persistenz

`locked` ist Teil des Nodes und wird von `CanvasDocument.Capture`/`Restore` (Phase 5) automatisch
mitgeführt — kein Code, kein Schemabruch (§6.1). Die **Selektion** wird nicht persistiert: sie ist
Sitzungszustand wie der Fokus.

## 12. Historie und Autosave

Eine Direktmanipulation ist eine Zustandsänderung des Specs und gehört damit in die Historie aus
Phase 5. `DrylCanvasWorkspace` committet heute nur, wenn der Host `Revision` erhöht (nach einer
AI-Runde). Damit eine Nutzeränderung nicht durchs Raster fällt, bekommt `DrylCanvas`:

```csharp
[Parameter] public EventCallback<CanvasEdit> OnEdit { get; set; }
```

```csharp
public readonly record struct CanvasEdit(string NodeId, CanvasNodeCommand Command, string Label);
```

Ausgelöst nach jedem **erfolgreichen** Werkzeugleisten-Kommando und nach jedem Reorder.
`DrylAiCanvas` reicht es durch. Der Host erhöht darauf seinen `Revision`-Zähler und setzt
`RevisionLabel` (z.B. `"Removed Revenue chart"`) — der Workspace committet die Version und das
Autosave läuft an. Genau ein neuer Parameter, keine verdeckte Kopplung zwischen zwei Komponenten,
die einander nicht kennen. Die Demo (§15) zeigt den Dreizeiler.

## 13. CSS, Tokens, Motion

Alles in `DrylCanvas.razor.css` (die Node-Klassen liegen dort bereits hinter `::deep`).
**Keine neuen Tokens**, kein neues Icon.

| Selektor | Gestalt |
| --- | --- |
| `::deep .canvas-node.is-selected` | `box-shadow: 0 0 0 1px var(--accent-line), var(--glow-accent)`; `border-radius: var(--r-lg)`; Übergang `box-shadow var(--dur-fast) var(--ease-out)` |
| `::deep .canvas-node:focus-visible` | der bestehende Akzent-Ring aus `dryl.css`, nicht überschrieben |
| `::deep .canvas-node-tools` | absolut, `top/right: calc(var(--sp-1) * -1)`, `gap: var(--sp-1)`, `glass-card`-Fläche, `border-radius: var(--r-pill)` |
| `::deep .canvas-node-pin` | absolut, dieselbe Ecke, `color: var(--fg-dim)` |
| `::deep .canvas-node.is-dragging` | `box-shadow: var(--shadow-lg)`, `opacity: .9`, `z-index: 1`, `cursor: grabbing` |
| `::deep [data-drag-handle]` | `cursor: grab`, `touch-action: none` |
| `::deep .canvas-node[data-drop-before]::before` | 2px Linie in `var(--accent-line)` an der Oberkante, `opacity`-Übergang über `--dur-fast` |
| `::deep .canvas-node[data-drop-after]::after` | dieselbe Linie an der Unterkante |

`prefers-reduced-motion: reduce`: die Presence-Primitiven regeln sich selbst; zusätzlich entfällt
die Übergangsdauer der Selektionsring-Transition (Ring erscheint sofort), und `autoFlip` ist dort
ohnehin ein No-op — der Drop bleibt also bedienbar, nur ohne Glide.

Beide Farbmodi: alle verwendeten Werte sind bestehende Tokens, die in beiden LIGHT-TOKEN-SETs
existieren. `node scripts/check-light-sync.mjs` bleibt grün, weil kein Token dazukommt.

## 14. Tests

`tests/DRYL.Components.Tests/`

**Unit**

- `CanvasSelection`: `Select`/`Clear`/Idempotenz/`OnChange`-Anzahl/`FocusTick`.
- `CanvasLabel.For`: jede Quelle der Reihenfolge, Kappung, Typ-Fallback.
- `CanvasNodeClone.Duplicate`: frische Ids im ganzen Teilbaum, keine Kollision mit bestehenden,
  `name`-Neuvergabe bei interaktiven Nodes, `Locked` nicht kopiert, `data`/`action` kopiert.
- `CanvasPatcher` mit `CanvasPatchAuthor.Ai`: je ein Test für die fünf Ablehnungen aus §6.2 —
  inklusive **Wortlaut** des Skip-Grunds — plus je ein Gegentest, dass dieselbe Op als `User`
  durchgeht, und dass ein Kind eines gepinnten Containers weiterhin patchbar ist.
- `CanvasNode.Locked`: Serialisierungs-Roundtrip; `false` erscheint **nicht** im JSON.

**bUnit**

- `DrylCanvas` ohne `Selection`: kein `tabindex`, keine Werkzeugleiste — Beweis der
  Rückwärtskompatibilität.
- Klick auf einen Node selektiert ihn; Klick auf ein Kind selektiert das Kind (E1).
- Roving Tabindex: genau ein `tabindex="0"` im gerenderten Baum.
- Werkzeugleiste: bei `Locked` sind Duplizieren/Entfernen/Griff `disabled`.
- Duplizieren fügt hinter dem Original ein; Entfernen setzt `Removing`; Purge-Fallback ohne
  `OnPurge`-Handler entfernt den Node.
- `OnEdit` feuert je Kommando genau einmal mit dem richtigen Label.
- `DrylCanvasDock` mit Selektion: Chip sichtbar, Sendetext trägt die Referenzzeile; ohne Selektion
  unverändert.

**Replay (Modellvertrag)**

- Ein Replay-Lauf, dessen `update_artifact` eine `setProps`- und eine `remove`-Op auf einen
  gepinnten Node schickt: der Spec bleibt unverändert und der Receipt enthält beide Skip-Gründe.

## 15. Demo

`DRYL.Website`: die bestehende Canvas-Workspace-Demo bekommt Direktmanipulation, keine neue Seite —
Phase 6 ist eine Eigenschaft der Fläche, keine zweite Fläche.

- Host hält `CanvasSelection _sel = new();`, gibt es an `DrylAiCanvas` **und** `DrylCanvasDock`.
- `OnEdit` erhöht `_revision` und setzt `RevisionLabel` → Version + Autosave (§12).
- Eine kurze Hinweiszeile über der Fläche: „Click an element to select it — pin, duplicate, remove
  or prompt about it." (Discoverability, sonst findet die Werkzeugleiste niemand.)
- Die Replay-Variante zeigt zusätzlich einen vorgepinnten Node, dessen Update-Runde am Pin
  abprallt — sichtbar im Log-Receipt.
- `ComponentCatalog`: die Einträge für `DrylCanvas`, `DrylAiCanvas` und `DrylCanvasDock` werden um
  die neuen Parameter ergänzt; **keine neue Komponente** kommt hinzu.

## 16. Paketgrenze und Publish

| `DRYL.Components` (Kern) → **2.17.0** | `DRYL.Components.Agents` → **0.14.0** |
| --- | --- |
| `Canvas/CanvasSelection.cs` *(neu)* | `Canvas/DrylCanvasDock.razor` (Chip, Präfix) |
| `Canvas/CanvasLabel.cs` *(neu)* | `Canvas/CanvasPrompt.cs` (zwei Zeilen) |
| `Canvas/CanvasNodeClone.cs` *(neu)* | `Canvas/DrylCanvasRun.cs` (`ApplyOp` → `CanvasPatchAuthor.Ai`) |
| `Canvas/CanvasSpec.cs` (`Locked`) | |
| `Canvas/CanvasPatcher.cs` (Autor + Pin-Regel) | |
| `Canvas/CanvasContext.cs` (Selection + Delegates) | |
| `Canvas/CanvasNodeView.razor` (Selektion, Werkzeugleiste) | |
| `Components/AI/DrylCanvas.razor(.css)` (Navigation, Kommandos, Reorder, `OnEdit`) | |
| `Components/Surfaces/DrylChatComposer.razor` (`FocusAsync`) | |
| `wwwroot/js/dryl-canvas.js` (`initReorder`) | |

**Publish:** Die Warnbox der Roadmap ist erledigt — `.github/workflows/publish.yml` liest heute
`<Version>` aus **beiden** `.csproj`, packt und pusht beide Pakete und legt für jedes ein eigenes
Tag und Release an. Es genügt, beide Versionen im selben Commit zu erhöhen.

## 17. Definition of Done

1. `CHANGELOG.md` gepflegt, Release im selben Commit geschnitten, `<Version>` in **beiden**
   `.csproj` erhöht (2.17.0 / 0.14.0).
2. `ComponentCatalog` in `DRYL.Website` aktualisiert (§15).
3. Demo zeigt Phase 6 live — Replay-Variante plus Live-Variante hinter dem Umgebungs-Flag.
4. Tests aus §14 grün; `dotnet test` vollständig grün.
5. Beide Farbmodi geprüft, 375 px geprüft, `prefers-reduced-motion` geprüft.
6. `node scripts/check-light-sync.mjs` grün (kein neues Token, muss trivial grün bleiben).
7. **A8 verifiziert:** Selektionsring blendet ein, Werkzeugleiste skaliert ein, Duplikat gleitet
   ein, Entfernen animiert aus, Reorder glidet — kein Sprung.
8. Projektnotiz `project_ai_canvas` fortgeschrieben.
