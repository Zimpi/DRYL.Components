# DrylAiCanvas — Phase W: Wow-Effekte

**Datum:** 2026-07-25
**Status:** genehmigt, in Umsetzung
**Vorgänger:** Phase P (`2026-07-24-canvas-render-pipeline-design.md`, Core 2.10.1 / Agents 0.8.1), Phase Q (`2026-07-24-canvas-robustness-design.md`, Core 2.10.2 / Agents 0.8.2)
**Folgephasen:** F (Katalog & Werkzeuge), R (Responsive-Härtung) — eigene Specs.

## Auslöser

Die Potenzial-Analyse identifiziert sechs Wow-Kandidaten (W1–W6). Nach P (Render-Pipeline) und Q (Robustheit) steht das Fundament: Nodes sind versionsgestempelt und memoisiert, ungültige Nodes settlen ehrlich, Cancel/Form-State/Prop-Sync sind dicht. Phase W macht sichtbar, was die Pipeline bereits weiß — **jede Patch-Op bekommt genau eine visuelle Sprache**, und der Artefakt-Wechsel/Fullscreen nutzt die schon vorhandene View-Transition-Schicht (`IDrylViewTransition`, `dryl.viewTransition`).

Zusätzlich erbt W den in Q bewusst verschobenen **Q5** (Root-Swap ohne Exit-Animation) — er wird von W2 mit gelöst.

## Leitgedanke: eine Op — eine Bewegung

Heute hat der Update-Pfad drei Op-Typen, aber nur zwei sichtbare Bewegungen. Nach W ist die Choreografie vollständig und überschneidungsfrei:

| Op         | Bewegung                                  | Träger                        |
| ---------- | ----------------------------------------- | ----------------------------- |
| `insert`   | Enter (SlideUp)                           | `DrylPresence` (bestehend)    |
| `move`     | FLIP-Glide                                | `dryl.motion.autoFlip` (best.)|
| `remove`   | Exit (SlideUp rückwärts) + `Purge`        | `DrylPresence` (bestehend)    |
| `setProps` | **Change-Pulse** — Akzentring, einmalig   | **W1 (neu)**                  |

`setProps` ist die einzige Op, die heute unsichtbar bleibt. W1 schließt genau diese Lücke — und *nur* diese: `insert` und `move` bekommen bewusst **keinen** Puls obendrauf, sonst doppelt sich die Bedeutung.

## Items

### W1 — Change-Pulse auf `setProps`

**Problem:** `ChangedIds` wird getrackt, aber ein `setProps` ändert Inhalte lautlos. Bei einem Patch über mehrere Knoten verliert das Auge die Update-Choreografie.

**Design (Agents):**
- `DrylCanvasRun` führt einen **monotonen Änderungsstempel pro Node-Id**:
  `internal int ChangeTickOf(string id)` — 0 = nie geändert. `ApplyOp` vergibt bei erfolgreichem `setProps` den nächsten Stempel (`_changeSeq`) an `op.Id`. `insert`/`move` vergeben **keinen** (siehe Choreografie-Tabelle).
  Der Stempel ist monoton, nicht boolesch: zwei aufeinanderfolgende `setProps` auf denselben Knoten müssen zwei Pulse ergeben.
- `BeginCreate` leert die Stempel (frisches Artefakt recycelt Ids — ein Alt-Stempel dürfte nicht als „geändert" durchschlagen). `BeginGeneration` leert sie **nicht** (die Update-Runde soll gerade pulsen).
- `CanvasNodeView` vergleicht den Stempel bei jedem `OnParametersSet` gegen den zuletzt gesehenen; bei Abweichung wird ein interner `_pulseKey` hochgezählt.

**Design (Motion):** Kein Timer, kein Interop. Das Overlay ist ein `@key`-getaggtes Element:

```razor
<span class="canvas-pulse" @key="_pulseKey" aria-hidden="true"></span>
```

Ein neuer Key ⇒ Blazor ersetzt das Element ⇒ dessen CSS-Animation läuft frisch an. Kein `setTimeout`, kein `IDisposable`, kein Zustand, der leaken kann. Das Element bleibt danach (mit `opacity: 0`) im DOM stehen — es ist `pointer-events: none` und `aria-hidden`.

**Design (CSS, scoped in `DrylAiCanvas.razor.css` via `::deep`):**
- `.canvas-node { position: relative }` (Anker für das Overlay).
- `.canvas-pulse`: absolut, `inset: calc(var(--sp-1) * -1)`, `border-radius: var(--r-lg)`, `box-shadow: var(--glow-accent)`, `border: 1px solid var(--accent-line)`.
- Keyframes `canvas-pulse` animieren **ausschließlich `opacity` + `transform`** (compositor-sicher, siehe Compositor-Regeln): 0 → sichtbar (35 %) → 0, `var(--dur-slow) var(--ease-out) both`.
- `prefers-reduced-motion: reduce` → `animation: none` (das Overlay bleibt unsichtbar, weil `opacity: 0` der Ruhezustand ist).

Der Puls liegt bewusst auf `.canvas-node` und damit über dem gesamten Teilbaum: ein `setProps` auf einen Container bedeutet inhaltlich „dieser Bereich hat sich geändert".

**FLIP-Verträglichkeit:** Der MutationObserver in `dryl.motion.autoFlip` triggert nur auf `[data-cid]`-Knoten — das Puls-Element ist keiner, löst also keinen Reflow-Glide aus.

### W2 — View-Transition beim Artefakt-Wechsel (löst Q5)

**Problem:** Ein neues `create_artifact` ersetzt `live.Root` hart (`CanvasStreamReveal`); der alte Baum verschwindet ohne Exit. Verstoß gegen Regel 2.12.

**Design:** Nicht im Reveal-Layer lösen (dort existiert der alte Baum in C# schon nicht mehr), sondern **im Rendering-Layer**. Der entscheidende Umstand: wenn `OnChange` feuert, ist der C#-State schon neu, **das DOM aber noch alt** — Blazor rendert erst bei `StateHasChanged`. Genau das braucht `document.startViewTransition`.

- `DrylAiCanvas` injiziert `IDrylViewTransition` (public, scoped registriert in `AddDrylComponents`) und merkt sich die zuletzt **gerenderte** `CanvasSpec`-Instanz (`_renderedSpec`).
- In `HandleChange`: `!ReferenceEquals(_renderedSpec, Run?.Spec) && _renderedSpec is not null` ⇒ Artefakt-Swap ⇒ der Render läuft in `RunAsync(...)`. Sonst normaler `StateHasChanged`.
  Die `_renderedSpec is not null`-Bedingung ist wichtig: der allererste Artefakt-Aufbau (aus dem Empty-State heraus) hat kein „altes" Bild, das gemorpht werden könnte — dort bleibt die bestehende Node-für-Node-Reveal-Choreografie ungestört.
- `OnAfterRender` ruft unbedingt `SignalRendered()` (Vertrag von `IDrylViewTransition`).
- Der Canvas-Root trägt einen **pro Instanz stabilen** `view-transition-name` (`dryl-canvas-<n>`, aus einem statischen Zähler) plus `view-transition-class: dryl-depth` und den `data-vt-depth`-Marker — dieselbe Mechanik wie `DrylCard.ViewTransitionStyle=DepthGlass`. Damit bekommt der Artefakt-Wechsel den Mercury-Merge, nicht nur einen Cross-Fade.
- Fallback ist geschenkt: `dryl.viewTransition.start` wendet die Änderung ohne Morph an, wenn die API fehlt, der Nutzer Reduced-Motion will oder es Prerender ist.

**Ein Name, nicht mehrere:** Der `view-transition-name` sitzt nur auf dem Root, nicht zusätzlich auf `.canvas-body`. Verschachtelte Namen sind erlaubt, aber jeder zusätzliche Name ist ein weiterer Abbruchgrund („duplicate view-transition-name") und bringt hier keinen Mehrwert — der Root-Morph transportiert Größe *und* Inhalt.

### W3 — Fullscreen-Expand

**Design (Agents):**
- Neuer Parameter `[Parameter] public bool AllowExpand { get; set; } = true`. Der Header bekommt einen Icon-Only-Button, **in `DrylTooltip` gewickelt** (Regel 2.11) mit gleichlautendem `AriaLabel` (Regel 2.9): „Expand artifact" / „Exit fullscreen".
- Der Toggle läuft durch denselben `IDrylViewTransition` wie W2 — das Artefakt *wächst* ins Overlay statt umzuspringen. Da der Root bereits einen `view-transition-name` trägt (W2), ist das nahezu geschenkt.
- `Escape` schließt Fullscreen: `@onkeydown` auf dem Root (`tabindex="-1"`, damit der Root Ziel sein kann; der geklickte Button liegt ohnehin innerhalb, das Event bubbelt).
- CSS: `.ai-canvas.is-expanded { position: fixed; inset: var(--sp-4); z-index: var(--z-modal); }` — Token-only. Kein eigener Backdrop: der Canvas ist ein Werkzeug, kein Modal; er blockt die Seite nicht und fängt keinen Fokus ein.

**Top-Layer statt reinem `position: fixed` (Befund aus der Runtime-Verifikation).** `fixed` misst gegen den nächsten Ancestor mit `transform`/`filter`/`backdrop-filter`/Containment — und den gibt es in echten Apps fast immer (Seiten-Fade-in-Wrapper, Glass-Card, Tilt-Fläche). Der erste Durchlauf füllte deshalb 1078×624 statt des Viewports. Der Top-Layer hat *gar keinen* Containing Block:

- Neuer Core-Helper `dryl.topLayer.show/hide(el)` (Popover API). Der Canvas rendert `popover="manual"` nur im expandierten Zustand und ruft show/hide in `OnAfterRender` — **vor** `SignalRendered()`, damit die fertige Geometrie im „new"-Snapshot der View-Transition steht. Das eine Frame, in dem das Attribut da, der Popover aber noch nicht offen ist (`display: none`), paintet nie: während des VT-Update-Callbacks hält der Browser das Rendering an.
- `manual`, nicht `auto`: kein Light-Dismiss hinter dem Rücken der Komponente — `Escape` gehört uns und muss mit `_expanded` synchron bleiben.
- Progressiv: ein Browser ohne Popover API ignoriert das unbekannte Attribut, die JS-Aufrufe sind No-ops, und es bleibt beim `position: fixed`-Verhalten.
- Das CSS muss die UA-Popover-Defaults neutralisieren (`fit-content`-Sizing, `margin: auto`, Border, Systemfarben) — sonst überlebt die Glas-Oberfläche die Promotion nicht. Ebenso `min-width/min-height: 0`, damit eine Consumer-Sizing-Klasse den Fullscreen-Kasten nicht überstimmt.
- Scrollen: `.canvas-body` bekommt im expandierten Zustand `flex: 1 1 auto; min-height: 0; overscroll-behavior: contain`. `min-height: 0` ist tragend — ohne es weigert sich das Flex-Kind, unter seine Inhaltshöhe zu schrumpfen, und ein hohes Artefakt schöbe den Canvas über den Viewport hinaus, ganz ohne Scrollbar. Verifiziert bei 1280×520: Body scrollt (1069px Inhalt in 423px), der Header bleibt stehen, und Weiterscrollen am Ende bewegt die Seite dahinter nicht.

**Neue Icons (Core):** `Maximize` und `Minimize` (Lucide `maximize-2` / `minimize-2`) ergänzen `DrylIcon.Icons` — der Satz hat bisher kein Fullscreen-Paar.

### W4 — Count-Up für `DrylStat`

**Problem:** `DrylStat` rendert `Value` statisch. Ein `setProps` auf einen `stat`-Knoten tauscht die Zahl hart aus.

**Design (Core):**
- `[Parameter] public bool CountUp { get; set; }` — **opt-in**, Default `false` (kein Verhaltenswechsel für bestehende Consumer).
- `Value` ist ein vorformatierter `string`. Die JS-Seite (`dryl.motion.countUp(el, text)`) extrahiert die **erste Zahl** aus dem Zieltext, tweent von der zuletzt gelandeten Zahl (initial 0) dorthin und schreibt Präfix/Suffix unverändert mit. Der **letzte Frame schreibt immer den Zieltext wörtlich** — eine Fehlinterpretation von Gruppierungs-/Dezimaltrennern ist damit maximal ein Zwischenframe-Kosmetikfehler, nie ein falsches Endergebnis.
- Kein `Value` mit Zahl ⇒ Text wird direkt gesetzt (kein Tween). Reduced-Motion ⇒ direkt gesetzt.
- Blazor-Verträglichkeit: JS schreibt `textContent` des `.stat-value`-Spans. Blazor patcht diesen Textknoten nur, wenn sich der *virtuelle* Wert ändert — ein unbeteiligter Re-Render fasst ihn nicht an. Der nächste echte Wertwechsel setzt ihn auf den neuen Zieltext, und der Tween startet von der zuletzt gelandeten Zahl.
- Dauer/Easing aus dem Token-Vokabular: `--dur-slow` wird zur Laufzeit aus `:root` gelesen (Regel 2.1 — kein Literal im JS), das Easing spiegelt `--ease-out`.
- Interop-Disziplin: `try/catch` gegen `JSDisconnectedException`/`InvalidOperationException` (Prerender-Guard-Muster). Ein laufender `rAF` bricht ab, sobald das Element `!isConnected` ist; ein neuer Aufruf storniert den vorherigen für dasselbe Element.

**Design (Agents):** Der `stat`-Case in `CanvasNodeView` setzt `CountUp` — im Canvas ist jede Zahl AI-generiert, dort ist der Tween die Regel, nicht die Ausnahme.

### W5 — Live-Bauzähler im Header

**Design (Agents):**
- `DrylCanvasRun.NodeCount` (public, `int`) — Knoten im Live-Baum, 0 ohne Artefakt. Iterativer Walk, keine Allokation im Normalfall.
- Der Status-Text im `DrylAiIndicator` wird während `Streaming` zu „Building · 14 elements" (Singular „1 element"), formatiert mit `FormattableString.Invariant` (Locale-Regel).
- Unter der Kopfzeile läuft eine dünne Fortschrittslinie: **`<DrylProgress Indeterminate Size="Small" />`** — kein handgerollter Balken. Die bestehende `progress-indeterminate`-Animation ist bereits transform-only und Reduced-Motion-fest.
- Enter/Exit über `DrylPresence` (Fade, Fast) — Regel 2.12 für bedingt gemountete Elemente.
- Der Zähler ist rein dekorativ; die Screenreader-Ansage bleibt die bestehende `aria-live`-Region (die Linie ist `aria-hidden` über `DrylProgress`' eigenes Markup bzw. den Wrapper).

### W6 — nicht in dieser Phase

Chart-Datenübergänge (Line/Area-Morph bei Datenänderung) gehören zur Chart-Familie, nicht zum Canvas. Eigenes Projekt, eigene Spec.

## Nicht-Ziele

- Keine neuen Node-Typen (Phase F), keine Undo/Copy/Download-Werkzeuge (F2), kein Export/Import (F3).
- Kein Backdrop / Fokus-Falle für Fullscreen — der Canvas bleibt ein Werkzeug, kein Dialog.
- Keine neuen Durations/Easings/Farben. W nutzt ausschließlich `--dur-fast|med|slow`, `--ease-out|in-out|spring|viscous` und die bestehende Akzent-/Glow-Token-Familie.

## Tests

- **W1:** Unit — `ApplyOp(setProps)` bumppt `ChangeTickOf`, zweimal ⇒ zwei verschiedene Stempel; `insert`/`move` bumppen nicht; `BeginCreate` setzt zurück. bUnit — nach einem `setProps` trägt der betroffene `[data-cid]`-Knoten ein `.canvas-pulse`, ein unbeteiligter nicht.
- **W2:** bUnit — `_renderedSpec`-Pfad: Erst-Aufbau läuft **ohne** View-Transition, ein zweites Create **mit** (verifiziert über einen Fake-`IDrylViewTransition`). Root trägt `view-transition-name` + `data-vt-depth`.
- **W3:** bUnit — Expand-Button vorhanden (mit `aria-label`), Klick setzt `.is-expanded`, Escape entfernt sie; `AllowExpand=false` rendert keinen Button.
- **W4:** bUnit — `CountUp` ändert das gerenderte Markup nicht (der Wert steht weiter im DOM, der Tween ist reines JS); Interop-Aufruf erfolgt (JSInterop-Mode `Loose`/verifizierter Invocation-Record).
- **W5:** Unit — `NodeCount` zählt verschachtelte Bäume korrekt, 0 ohne Spec. bUnit — während `Streaming` steht „element(s)" im Header und die Progress-Linie ist da; nach `CompleteReveal` nicht mehr.

## Versionierung & Doku

- Core **2.10.2 → 2.11.0** (neuer `DrylStat.CountUp`-Parameter, neue Icons, `dryl.motion.countUp`) — MINOR.
- Agents **0.8.2 → 0.9.0** (neuer `AllowExpand`-Parameter, `NodeCount`, Change-Pulse, View-Transitions) — MINOR.
- `CHANGELOG.md`: Einträge unter `Added`/`Changed`, Release-Schnitt im selben Commit.
- `ComponentCatalog` (DRYL.Website): Beschreibung des AI-Canvas-Eintrags bleibt gültig; die Demo-Seite `DemoAiCanvas.razor` erwähnt die neue Choreografie.
