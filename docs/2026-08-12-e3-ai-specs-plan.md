# E3 AI — die fünf fehlenden Component-Specs: Implementation Plan

**Datum:** 2026-08-12
**Vorgänger:** [`2026-08-11-i1-plan.md`](2026-08-11-i1-plan.md) — dort entstanden
`F1 DrylToolCall`, `F2 DrylToolCallGroup` und `F3 DrylCanvas/`.
**Ausgangsstand:** `node scripts/check-spec-coverage.mjs` → `3/127 components covered`.

**Ziel:** Kategorie `E3 AI` vollständig spezifizieren. Acht Komponenten liegen unter
`code/DRYL.Components/Components/AI/`, drei haben eine Spec. Dieser Plan schreibt die
fünf fehlenden — `DrylAiIndicator`, `DrylAiScope`, `DrylAiStream`, `DrylAuraElements`,
`DrylCanvasWorkspace` — und zieht danach die beiden Begleitdateien der Kategorie nach.
Zielstand: `8/127`.

## Global Constraints

- **Spec-only, wo der Code stimmt.** Dieser Plan ist Phase C, nicht Phase B: er
  dokumentiert, was da ist. Code wird nur angefasst, wenn eine Spec sonst eine
  Unwahrheit behaupten müsste — und dann wird das im Task benannt, nicht nebenbei
  erledigt.
- **Jede Verhaltensbehauptung am Code geprüft.** Doc-Kommentare sind kein Beleg. Im
  letzten Durchgang standen mehrere Aussagen in `I1`, die weder im Code noch in der
  Realität ihre Entsprechung hatten — eine davon bis in `AI-03` hinein. Ein Kriterium,
  das ich nicht an `.razor`, `.razor.css` oder `dryl.css` verifizieren kann, kommt nicht
  in die Spec.
- **Keine Token-Reinheit behaupten, die nicht gilt.** `F1` und `F2` schreiben „Every
  radius, spacing, duration and easing resolves to a token" — für `DrylToolCall.razor.css`
  stimmt das (geprüft). Für `dryl.css` und für `DrylCanvasWorkspace.razor.css` stimmt es
  **nicht** durchweg: die Pille in `.ai-indicator` trägt `padding: 0 10px 0 8px`,
  `height: 22px`, `gap: 6px`, und `.ws-chips`/`.ws-ink` tragen `padding-bottom: 2px`
  bzw. `height: 2px`. Der `DESIGN-01`-Check greppt nur Farben in `*.razor.css` und
  findet davon nichts. Die Specs schreiben also, was gilt, und benennen die Ausnahmen.
- **`_Api.md` wird zitiert, nicht neu erfunden.** Vier der fünf Komponenten sind dort
  schon als Nicht-Opt-ins begründet (`DrylAiIndicator`, `DrylAiStream`, `DrylAiScope` in
  der Tabelle; `DrylAuraElements` und `DrylCanvasWorkspace` als „kein `AiState`-Parameter
  überhaupt"). Diese Begründungen werden in die Specs übernommen, in denselben Worten.
- **`AI-03` in der aktuellen Fassung.** Der bindende Test fragt, ob der Parameter ein
  Schalter ist — nicht mehr, ob die Komponente mit `AiState.None` etwas Sinnvolles
  rendert. Keine Spec darf die alte Formulierung wiederbeleben.
- **Specs auf Englisch** (`SPEC-08`), auch wenn dieser Plan deutsch ist.
- **Ein Commit pro Task.** `DRYL.Website` ist ein eigenes Repository (Branch
  `fix/dockerfile-after-code-move`) und bekommt seinen eigenen Commit.
- **Kein `<Version>`-Bump, kein Changelog-Eintrag.** `REL-01`/`REL-02` binden das an
  Änderungen an Bibliothekscode. Dieser Plan fasst `code/` nicht an. Ändert ein Task das
  doch, zieht er beides in denselben Commit.

## Die eine offene Entscheidung — entschieden

`DrylAuraElements` hat weder Demo-Seite noch `ComponentCatalog`-Eintrag. Verifiziert:
der Bezeichner kommt in `DRYL.Website/` nirgends vor, sondern ausschließlich in der
Markup anderer Komponenten (`DrylToolCall`, `DrylButton`, `DrylTable`, `DrylAiCanvas`
und rund dreißig weitere) sowie in `AiAuraCss.cs`. `SPEC-05` verlangt beides und lässt
für diese zwei Punkte ausdrücklich keinen schriftlichen Ausnahmeweg zu.

**Entscheidung des Maintainers (2026-08-12): die Lücke wird dokumentiert.** Die Spec
nennt beide Punkte als fehlend, begründet warum (ein Kompositionsbaustein, den kein
Konsument selbst platziert), und bleibt auf `State: Modified` statt `Implemented`. Kein
Code in `DRYL.Website`, keine Änderung an `SPEC-05`. Die Coverage steigt trotzdem, weil
`check-spec-coverage.mjs` den `Source`-Block zählt, nicht den `State`.

`DrylAiStream` ist der mildere Fall und braucht keine Entscheidung: Demos existieren
(`AiActivity/Streaming.razor`, dazu neun Beispiele unter `Agents/`), nur der
`ComponentCatalog`-Eintrag ist mit `DrylAiScope` geteilt (`"AI Activity"` /
`ai-activity`, `ClassName` `DrylAiScope`). Der Katalog kennt geteilte Einträge — 95
Einträge auf 127 Komponenten —, also wird das als Tatsache dokumentiert, nicht als
Mangel.

---

## Task 1 — `F4 DrylAiIndicator.md`

**Dateien:** `specs/E3 AI/F4 DrylAiIndicator.md` (neu)

**Inhalt.** Die Pille, deren Parameter ihr Inhalt ist. Belegquellen:
`code/DRYL.Components/Components/AI/DrylAiIndicator.razor` und der Block
`/* ====== AI INDICATOR — small status pill ====== */` in
`code/DRYL.Components/wwwroot/dryl.css`.

Am Code zu belegende Punkte, die nicht aus dem Doc-Kommentar stammen dürfen:

- `State` ist `AiState`, Default `AiState.Active` — der Doc-Kommentar der Komponente
  nennt den Default nicht, die Deklaration schon.
- Das Label ist `DefaultLabel`: `Thinking` → „Thinking…", `Streaming` → „Streaming…",
  `Generated` → „Generated", alles andere (`None` **und** `Active`) → „AI". Der
  Usage-Kommentar zeigt `<DrylAiIndicator State="AiState.Generated">Done</...>` — das
  ist ein `ChildContent`-Override, nicht das Default-Label.
- Die Klassen sind `is-thinking` und `is-streaming`; `Active`, `Generated` und `None`
  bekommen keine Modifier-Klasse. Der Unterschied zwischen `Active` und `None` ist damit
  **kein** visueller, sondern gar keiner — beides ist die ruhige Pille mit Label „AI".
  Das gehört in die Spec, weil es aus der Parameter-Tabelle sonst nicht hervorgeht.
- `role="status"` **und** `aria-live="polite"` — `UX-04` nennt genau diese Komponente
  als Präzedenzfall.
- Reduced Motion: `.ai-indicator .ai-indicator-ico` und `.ai-indicator::after`
  bekommen `animation: none`.
- Motion: Puls und Shimmer laufen `infinite`. `DESIGN-10` lässt einem
  kontinuierlichen Segment die eigene Periode; das Easing ist `var(--ease-in-out)`.
  Die Spec nennt das so und behauptet keine Dauer-Tokens.
- Appearance ehrlich: Farben und Radius sind Tokens (`--accent-fg`, `--accent-line`,
  `--accent-soft`, `--ai-b`, `--shimmer`, `--r-pill`, `--font-mono`); `height`,
  `padding` und `gap` der Pille sind Literale in `dryl.css`, `font-size` und
  `letter-spacing` ebenfalls — letztere regelt `DESIGN-01` nicht, erstere schon,
  und der Check greppt sie nicht.
- SPEC-05: Demo `Ai/Indicator.razor` und `Ai/Lifecycle.razor`, Katalog `"AI Mode"` /
  `ai`. Enter/Exit: die Pille selbst montiert nicht animiert — sie ist ein Inline-
  Element, das der Host platziert; die Bewegung ist die dauernde. Das wird als die
  von `DESIGN-11` erlaubte, ausgeschriebene Ausnahme formuliert.

**State:** `Implemented` — Spec und Code stimmen überein, kein Code-Change.

**Verifikation:** `node scripts/check-spec-coverage.mjs` zeigt `4/127`;
`node scripts/check-harness-links.mjs` bleibt grün.

## Task 2 — `F5 DrylAiScope.md`

**Dateien:** `specs/E3 AI/F5 DrylAiScope.md` (neu)

**Inhalt.** Der Broadcast-Scope. Belegquellen: `DrylAiScope.razor`,
`code/DRYL.Components/Ai/AiScope.cs`, `IDrylAiActivityService.cs`.

Am Code zu belegende Punkte:

- `State` ist `AiState?` **ohne** Default. `null` = dem Service folgen,
  `AiState.None` = AI aktiv aus. `_Api.md` begründet das bereits; die Spec übernimmt es.
- `Rebuild()` löst auf: `State ?? (Service und Key vorhanden ? GetState(Key) :
  AiState.None)`. Ein Scope ohne `Key` und ohne `State` broadcastet `None`.
- Der Service ist **optional**: `Services.GetService<IDrylAiActivityService>()`, nicht
  `GetRequiredService`. Ein Scope mit explizitem `State` funktioniert ohne
  `AddDrylComponents()`.
- `OnServiceChanged` verlässt sich auf `key != Key` → ein Scope mit `Key == null`
  reagiert nie auf den Service, auch wenn er registriert ist.
- `OnServiceChanged` ignoriert den Service, solange `State` gesetzt ist.
- `_scope` wird bei jedem `OnParametersSet` als **neue Instanz** gebaut, damit
  `CascadingValue` (`IsFixed="false"`) die Abonnenten benachrichtigt.
- Die Komponente rendert **kein eigenes Element** — nur `CascadingValue` um
  `ChildContent`. Daraus folgt für `SPEC-05` ehrlich: keine Farbe, kein Fokus, kein
  Keyboard, keine Animation. Das ist die von `DESIGN-11` gemeinte „genuinely nothing
  to animate"-Ausnahme und wird als solche ausgeschrieben, nicht weggelassen.
- `IDisposable`: `OnChanged` wird im `Dispose` abgemeldet (`CODE-05`).
- Wichtig für `AI-03`: `AiScope.Resolve` wird von `DrylAiAware` benutzt — **nicht** von
  `DrylToolCall`, `DrylToolCallGroup` und `DrylCanvas`, die nur die Aura erben. Die
  Spec verweist dafür auf `_Api.md` statt die Aussage zu duplizieren.
- SPEC-05: Demo `AiActivity/ScopeCoordination.razor`, Katalog `"AI Activity"` /
  `ai-activity`.

**State:** `Implemented`.

**Verifikation:** Coverage zeigt `5/127`.

## Task 3 — `F6 DrylAiStream.md`

**Dateien:** `specs/E3 AI/F6 DrylAiStream.md` (neu)

**Inhalt.** Der Token-Stream. Belegquellen: `DrylAiStream.razor`,
`code/DRYL.Components/Ai/AiStreamContext.cs`, dazu die bestehenden Tests in
`tests/DRYL.Components.Tests/DrylAiStreamTests.cs` — die Tests sind Beleg für
Verhalten, das ich sonst nur behaupten könnte.

Am Code zu belegende Punkte:

- Der Zustandsverlauf ist `Thinking` → (erster Token) `Streaming` → `Generated` →
  nach `SettleDelayMs` → `SettleTo`. `SettleDelayMs` ist eine `const int` von 1200 —
  eine logische Pause, ausdrücklich keine CSS-Dauer, also **kein** `DESIGN-01`-Fall.
- `Thinking` wird gesetzt, **bevor** geprüft wird, ob `Source` null ist; ein null-Source
  fällt sofort auf `None` zurück.
- Neustart hängt an `ReferenceEquals(Source, _current)` — dieselbe Sequenz erneut zu
  übergeben startet nichts neu, eine neue Instanz mit gleichem Inhalt schon.
- `OnCompleted` feuert nur bei Erfolg, **vor** dem Settle-Delay, mit dem vollen Text.
- Ein Fehler im Stream setzt `None` und **behält** den bis dahin gesammelten Text.
- Abbruch (`OperationCanceledException`) setzt gar nichts — der Nachfolger übernimmt.
- `Key` + registrierter Service: `Set(Key, state)` für jeden Zustand außer `None`,
  `Clear(Key)` für `None`.
- `Smooth` ist ein Produzent/Reveal-Paar über einen `StringBuilder`-Backlog;
  `RevealTickMs` = 16, `RevealTake(backlog)` = `max(4, backlog/100)`. Die Spec
  beschreibt das als beobachtbares Verhalten („die Enthüllungsrate wächst mit dem
  Rückstand"), nicht als Konstanten-Dump — `SPEC-06`, Negotiable.
- `SettleTo` ist kein Opt-in (`AI-03`), Begründung wörtlich aus `_Api.md`.
- Die Komponente rendert **kein eigenes Element**: `ChildContent(_context)` oder der
  rohe Text. Für `UX-04` heißt das ehrlich: der Stream selbst trägt kein
  `aria-live` — die Ansage gehört dem, was den Text rendert. Das wird als bewusste
  Aufteilung formuliert und dem Konsumenten als Pflicht genannt.
- `IDisposable` bricht den laufenden Stream ab (`CODE-05`).
- SPEC-05: Demo `AiActivity/Streaming.razor`; Katalog geteilt unter `"AI Activity"`,
  mit der oben festgehaltenen Begründung.

**State:** `Implemented`.

**Verifikation:** Coverage zeigt `6/127`; `dotnet test DRYL.slnx -c Release` bestätigt,
dass die Kriterien, die ich aus `DrylAiStreamTests.cs` gezogen habe, heute grün sind.

## Task 4 — `F7 DrylAuraElements.md`

**Dateien:** `specs/E3 AI/F7 DrylAuraElements.md` (neu)

**Inhalt.** Die geteilte Aura-Markup. Belegquellen: `DrylAuraElements.razor`,
`code/DRYL.Components/Ai/AuraLifecycle.cs`, `code/DRYL.Components/Ai/AiAuraCss.cs`, die
`.ai-aura*`-Regeln in `dryl.css`.

Am Code zu belegende Punkte:

- `Aura` ist ein `AuraLifecycle`, **kein** `AiAura` — der Namensgleichklang mit dem
  `Aura`-Parameter von `DrylToolCall` ist eine Falle und wird ausdrücklich benannt
  (`_Api.md` tut das bereits).
- `[EditorRequired]`.
- Gerendert wird nur, solange `Aura.Present` — also `RenderState != None` **oder**
  `Exiting`. Das ist der Grund, warum die Aura einen Ausgang hat (`DESIGN-12`).
- Der `.ai-aura-wash` erscheint nur bei `RenderState == AiState.Generated && !Exiting`
  und trägt `@key="GenTick"`, damit die Bloom-Animation pro Übergang neu abspielt
  (`AI-07`).
- Die drei Basiselemente `.ai-aura-ring`, `.ai-aura-comet`, `.ai-aura-glow` sind die
  Primitive aus `AI-02`; die Komponente zeichnet nichts selbst.
- Der Host muss die passenden `ai-aura*`-Klassen selbst setzen (`AiAuraCss.Append`) —
  ohne sie rendert die Komponente drei wirkungslose `div`s. Das ist ein Vertrag, kein
  Implementierungsdetail, und gehört in die Acceptance Criteria.
- `UX-07`: die Aura-Elemente sind dekorativ und dürfen die Fokusreihenfolge nicht
  ändern — sie sind `pointer-events: none` und tragen keinen Text. **Zu prüfen beim
  Schreiben:** ob sie `aria-hidden` tragen. Im Markup steht keins. Falls nicht, ist das
  ein echter Befund gegen `UX-07`(c) — er wird in der Spec als Kriterium formuliert,
  das der Code noch nicht erfüllt, und stützt zusätzlich `State: Modified`. Er wird in
  diesem Task **nicht** stillschweigend gefixt; ein Fix an geteiltem Aura-Markup trifft
  gut dreißig Komponenten und ist eine eigene Entscheidung.
- SPEC-05: weder Demo noch Katalogeintrag — dokumentiert wie oben entschieden.

**State:** `Modified`.

**Verifikation:** Coverage zeigt `7/127`.

## Task 5 — `F8 DrylCanvasWorkspace.md`

**Dateien:** `specs/E3 AI/F8 DrylCanvasWorkspace.md` (neu)

**Inhalt.** Die größte der fünf. Belegquellen: `DrylCanvasWorkspace.razor`,
`DrylCanvasWorkspace.razor.css`, `code/DRYL.Components/Canvas/CanvasWorkspace.cs`,
`CanvasHistory.cs`, `CanvasDocument.cs`, `CanvasDocumentStore.cs`.

**Vorab zu klären: Split oder nicht.** `SPEC-02` nennt genau drei Split-Kandidaten —
`DrylTable`, `DrylCommandPalette`, `DrylCanvas` — und verlangt für alles andere einen im
PR genannten Grund. `DrylCanvasWorkspace.razor` hat 18 KB gegen 28 KB bei `DrylCanvas`.
Die Spec bleibt deshalb **eine Datei**, mit thematischen `###`-Abschnitten, die
`SPEC-03` für große Specs ausdrücklich erlaubt. Kein Split, keine Regeländerung.

Abschnitte und die am Code zu belegenden Punkte:

- **Views und Chips.** `ShowBar` ist `Views.Count > 0 && (ShowBarWhenSingle ||
  ShowHistory || Views.Count > 1)` — Historie erzwingt die Leiste auch bei einer
  einzigen View. Ohne aktive View rendert der Body `DrylEmptyState`.
- **Wechsel.** `ActivateAsync` läuft durch `IDrylViewTransition.RunAsync`; die
  Begründung im Code (verschachtelte View-Transitions verlieren eine Mutation,
  `DrylCanvasRun.ConsumeSwapMorphSuppression`) gehört in die Description, nicht in ein
  Kriterium.
- **Slots.** Mit `View` rendert der Body `View(active)`, ohne ihn einen `DrylCanvas`
  über `active.Spec`. Das ist die Stelle, an der `_Api.md` sagt: der Workspace nimmt
  **keinen** `AiState`-Parameter und überlässt AI dem, was ihn umgibt.
- **Historie.** `Revision`-Änderung → `Workspace.Commit(RevisionLabel ?? "Version {n}")`
  → `ScheduleSave()`. `Commit` verwirft laut `CanvasWorkspace.Commit` eine Momentaufnahme,
  die nichts geändert hat. Undo/Redo/Restore laufen alle durch `HistoryStep`, das die
  View-Transition benutzt und **nur bei tatsächlicher Bewegung** ansagt.
- **Autosave.** Nur wenn `AutoSave` **und** ein `ICanvasDocumentStore` registriert ist;
  debounced über `AutoSaveDelayMs` (Default 1500) mit einer `CancellationTokenSource`;
  `DocumentIdChanged` feuert nur, wenn sich die Id ändert; ein werfender Store wird
  geschluckt, damit ein kaputtes Backend kein laufendes Dashboard mitnimmt.
- **Keyboard.** `role="tablist"`/`role="tab"`, Roving `tabindex` (aktiv `0`, sonst `-1`),
  <kbd>←</kbd>/<kbd>→</kbd>/<kbd>Home</kbd>/<kbd>End</kbd> wechseln, <kbd>Enter</kbd>
  und <kbd>Space</kbd> aktivieren, <kbd>Delete</kbd>/<kbd>Backspace</kbd> schließen —
  letzteres nur bei `AllowClose`. Navigation überspringt Views mit `Removing`.
- **Ansage.** `.ws-live` ist `aria-live="polite"` und visuell versteckt (`clip-path`).
- **Motion.** Die Leiste über `DrylPresence` (`SlideDown`, `Fast`), jeder Chip über
  `DrylPresence` (`Scale`, `Fast`, `Appear`) mit `OnExited` → `Workspace.Remove` —
  d.h. das Entfernen wartet auf die Exit-Animation. Die Tinte gleitet über
  `dryl.motion.moveIndicator`, mit `is-ink-ready` gegen das Einfliegen von x=0.
- **Interop und Cleanup.** `dryl.motion.moveIndicator` in `OnAfterRenderAsync`,
  `dryl.motion.disposeIndicator` in `DisposeAsync`; vier gefangene Ausnahmetypen mit
  je eigenem Grund (`JSDisconnectedException`, `InvalidOperationException`,
  `JSException`). `IAsyncDisposable` meldet außerdem `OnChange` ab und bricht den
  Autosave ab.
- **Appearance, ehrlich.** Farben, Radien, Dauern und Easings sind Tokens. Ausnahmen,
  die benannt werden: `padding-bottom: 2px` auf `.ws-chips`, `height: 2px` auf
  `.ws-ink`, `gap: 2px` in `.ws-versions`, `min-width: 200px` / `max-height: 280px`
  der Versionsliste. Reduced Motion schaltet die vier Transitions ab.
- **SPEC-05.** Demos `CanvasWorkspace/Basic.razor`, `.../Direct.razor`,
  `.../Document.razor`; Katalog `"Canvas Workspace"` / `canvas-workspace`. AI-Modus:
  **nein**, mit der Begründung aus `_Api.md` — der Workspace rendert einen schlichten
  `DrylCanvas` und überlässt AI dem Wrapper.

**State:** `Implemented`, sofern beim Schreiben kein Widerspruch auffällt.

**Verifikation:** Coverage zeigt `8/127`.

## Task 6 — `_Api.md` und `_Interop.md` der Kategorie nachziehen

**Dateien:** `specs/E3 AI/_Interop.md`, `specs/E3 AI/_Api.md`

`_Interop.md` sagt heute dreimal „none *(phase C)*". Nach Task 5 ist das falsch: die
Kategorie benutzt `dryl.motion.moveIndicator` und `dryl.motion.disposeIndicator`,
registriert bzw. konsumiert `IDrylAiActivityService`, `IDrylViewTransition` und
`ICanvasDocumentStore`, und hat drei Cleanup-Pflichten (`DrylAiScope.Dispose`,
`DrylAiStream.Dispose`, `DrylCanvasWorkspace.DisposeAsync`). Eine Spec, die Interop
beschreibt, neben einer Begleitdatei, die „none" sagt, ist genau die Drift, gegen die
`SPEC-01` geschrieben ist.

`_Api.md` bekommt den kleinen Nachtrag, den die neuen Specs brauchen: `AiStreamContext`
in der Sektion „Remaining shared types" von der Liste in eine echte Beschreibung, und
`AuraLifecycle` als das, worauf `DrylAuraElements`' `Aura` zeigt. Die Canvas-Typen
(`CanvasSpec`, `CanvasSelection`, …) bleiben offen — sie gehören zu `F3` und zu `E14`,
nicht zu diesem Plan.

**Verifikation:** `node scripts/check-spec-coverage.mjs` bleibt bei `8/127` (die
Begleitdateien zählen nicht), `node scripts/check-harness-links.mjs` grün.

## Task 7 — PR-Template: „Dark-only" streichen

**Dateien:** `.github/PULL_REQUEST_TEMPLATE.md`

Die Checkliste hakt heute „Dark-only; no light-theme additions" ab. Seit `DESIGN-02`
(„Two modes, one identity") ist das die Umkehrung der geltenden Regel: Komponenten
verzweigen nicht auf den Modus, und ein modusabhängiger Wert wird zum Token in beiden
LIGHT-TOKEN-SET-Kopien. Die Zeile wird durch die geltende Anforderung ersetzt und die
beiden Modus-Skripte (`check-light-sync.mjs`, `validate-light-contrast.mjs`) in den
Verifikationsblock aufgenommen, der sie heute nicht nennt.

**Verifikation:** `node scripts/check-harness-links.mjs` grün (das Template liegt
außerhalb von `harness/`, der Lauf belegt nur, dass nichts kaputt ging); Sichtprüfung
gegen `harness/design.md`.

## Task 8 — `DRYL.Website/CLAUDE.md`: NuGet → ProjectReference

**Dateien:** `../DRYL.Website/CLAUDE.md` (eigenes Repository, eigener Commit)

Zwei Stellen behaupten NuGet-Konsum: die Einleitung („consumes `DRYL.Components` via
NuGet") und Abschnitt 2 („There is no local `ProjectReference`. To update the library
version, bump the `<PackageReference>`"). `DRYL.Website.csproj` enthält tatsächlich zwei
`<ProjectReference>` auf `../DRYL.Components/code/DRYL.Components` und
`.../DRYL.Components.Agents` und keinen einzigen `<PackageReference>` auf DRYL —
geprüft. Beide Stellen werden korrigiert, inklusive der Folgeaussage über den
Versions-Bump, die dann keinen Gegenstand mehr hat.

**Verifikation:** `grep -n "ProjectReference" DRYL.Website/DRYL.Website.csproj` gegen den
neuen Text; `dotnet build` der Website ist nicht Teil dieses Tasks (reine Doku).

---

## Reihenfolge und Abhängigkeiten

Tasks 1–5 sind untereinander unabhängig; sie teilen keine Datei. Task 6 hängt an 1–5,
weil er ihr Ergebnis zusammenfasst. Tasks 7 und 8 hängen an nichts.

## Abschluss-Verifikation (`CLAUDE.md` Stufe 5)

Nach Task 6, in dieser Reihenfolge, mit gelesener Ausgabe:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs      # erwartet: 8/127 components covered
node scripts/check-motion-tokens.mjs
```

`check-spec-coverage.mjs` endet weiterhin mit einem Fehlercode — das ist der
Phase-C-Normalfall. Der Beleg ist die gestiegene Zahl, nicht der grüne Exit.

Build und Tests laufen, obwohl dieser Plan `code/` nicht anfassen soll: sie sind der
Nachweis, dass er es tatsächlich nicht getan hat.
