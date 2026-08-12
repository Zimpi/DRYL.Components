# `DrylSplitButton` — F3 von `Modified` auf `Implemented`: Implementation Plan

**Datum:** 2026-08-13
**Vorgänger:** [`2026-08-13-phase-c-e2-actions-plan.md`](2026-08-13-phase-c-e2-actions-plan.md) —
dort entstanden `F1`–`F3` und die beiden Begleitdateien; Endstand `11/127`.
**Ausgangsstand:** `node scripts/check-spec-coverage.mjs` → `11/127 components covered`
(Exit 1, Phase-C-Normalfall), `DRYL.Components` `<Version>` `2.22.1`,
`DRYL.Components.Agents` `0.17.4`, Arbeitsbaum sauber auf `main`.

**Ziel:** die zwei Abweichungen schließen, die `specs/E2 Actions/F3 DrylSplitButton.md`
unter `## Deviations` benennt, und die Spec damit auf `State: Implemented` bringen.
Die Coverage-Zahl ändert sich dabei **nicht** — `State` ist kein Zählkriterium.

## Die Entscheidung, die diesem Plan vorausging

Abweichung 2 bot die Spec mit zwei Wegen an. Einer davon ist **nicht implementierbar**,
und das ist vor dem Planen geprüft worden statt danach: `AiScope.Resolve` lautet
`explicitAi != AiState.None ? explicitAi : (scope?.State ?? AiState.None)` — es gibt
kein Off-Sentinel. `AiState.None` heißt dort „erbe den Scope", nicht „aus". Ein Caret,
der innerhalb eines `DrylAiScope` dunkel bleibt, ginge nur über eine Änderung an
`DrylAiAware`/`AiScope` selbst, also an einem Typ, den `E1 Foundation` trägt und den
die 1.0-Freeze bindet. Der Weg „den Caret auf `AiState.None` festnageln" entfällt
damit; die Spec sollte das nachtragen, statt eine Option stehen zu lassen, die es
nicht gibt.

Bleibt das Weiterreichen, und dort hat der Maintainer am 2026-08-13 entschieden:
**`DrylSplitButton` wird `DrylAiAware`.** Nicht die minimale Variante (`Ai` zusätzlich
an den Caret durchreichen, PATCH, `Aura`-Lücke bleibt offen), sondern die, die die
Komponente an die Form jeder anderen AI-aware Komponente der Bibliothek angleicht.

## Global Constraints

- **Bibliothekscode ⇒ `<Version>` + CHANGELOG im selben Commit** (`REL-01`, `REL-02`).
  Anders als der Vorgängerplan-Block 5–8 fasst dieser Plan `code/` an.
- **Ein Bump, nicht zwei.** `2.22.1` → `2.23.0`, angelegt in Task 1; Task 2 ergänzt
  denselben, noch nicht veröffentlichten Block um weitere Bullets. Die Begründung ist
  die aus dem Vorgängerplan: zwei Bumps für zwei Commits derselben unveröffentlichten
  Version sind genau die Buchhaltung, die der `2.22.0`-Eintrag als Sonderfall hat
  erklären müssen.
- **MINOR, nicht PATCH.** `releasing.md`: neuer Parameter → MINOR. `Aura` kommt neu
  dazu. `Ai` behält Namen und Default (`AiState.None`), wechselt nur von einem eigenen
  `[Parameter]` zu dem geerbten — für einen Konsumenten identisch, also **nicht**
  breaking, und das gehört so in den CHANGELOG-Eintrag geschrieben statt verschwiegen.
- **`SPEC-01`: Spec und Code im selben Commit.** Jeder der beiden Tasks fasst `F3` mit
  an. `State` wechselt erst in Task 2 auf `Implemented`, weil erst dann beide
  Abweichungen geschlossen sind — Task 1 lässt es ausdrücklich auf `Modified` stehen
  und streicht nur die eine Abweichung.
- **Tests, bevor der Code stimmt** (`test-driven-development`). `DrylSplitButton` hat
  heute **null** Tests — das hat der F3-Durchgang belegt (`grep -rn "SplitButton"
  tests/` ohne `bin`/`obj` liefert nichts). Beide Tasks schreiben ihre Tests zuerst und
  zeigen sie rot, bevor sie grün werden.
- **Keine Zeilennummern** in Spec und Regeldokumenten (`SPEC-09`).
- **Ein Commit pro Task**, danach ein **getrennter** Reviewer (`CLAUDE.md` Stufe 4).
  Befunde gehen an den Implementierer zurück, nie in die koordinierende Session.

## Vorab geprüft, damit die Tasks nicht darauf warten

- `DrylTooltip` rendert `<span class="tt-wrap @Class" data-tt="@Text" …>` und ist rein
  CSS-getrieben — kein JS, kein Service, keine Cleanup-Pflicht. Die Blase ist
  dekorativ; der Kopfkommentar sagt ausdrücklich, dass der interaktive Trigger seinen
  eigenen `aria-label` mit demselben Text trägt. Ein Tooltip um den Caret bringt der
  Kategorie also **keinen** Interop-Eintrag — `_Interop.md` bleibt unangetastet.
- Der Tooltip bricht die Segment-Optik **nicht**: `.split-btn > .popover-anchor` greift
  weiter (der Tooltip säße unterhalb des Anchors, innerhalb von `<Trigger>`), und
  `.split-btn > .popover-anchor .btn` ist ein Nachfahren-Selektor, der durch den Span
  hindurch trifft. `.tt-wrap { display: inline-flex }` reicht das Flex-Layout durch.
  Der `tt-wrap`-Konflikt, den `F2 DrylButtonGroup.md` für die Gruppe festhält,
  wiederholt sich hier also nicht — das ist im F3-Durchgang belegt worden und der Grund,
  warum dieser Fix überhaupt unblockiert ist.
- `dryl.menu.focusTrigger` sucht mit `.popover-trigger button:not([disabled])`, ebenfalls
  ein Nachfahren-Selektor — die Fokusrückgabe an den Caret überlebt den Span.
- `DrylAiAware` liefert `Ai`, `Aura`, `[CascadingParameter] Scope`, `EffectiveAi` und
  `EffectiveAura`. `DrylSplitButton` erbt heute von nichts, deklariert also mit
  `@inherits DrylAiAware` keinen Basisklassen-Konflikt.
- `<Version>` steht auf `2.22.1`, der `## [Unreleased]`-Block im CHANGELOG ist leer.

---

## Task 1 — `UX-05`: Tooltip am Caret

**Dateien:** `code/DRYL.Components/Components/Actions/DrylSplitButton.razor`,
`specs/E2 Actions/F3 DrylSplitButton.md`,
`tests/DRYL.Components.Tests/DrylSplitButtonTests.cs` (neu), `CHANGELOG.md`,
`code/DRYL.Components/DRYL.Components.csproj`

**Änderung.** Der Caret-`DrylButton` im `<Trigger>`-Slot wird in einen `DrylTooltip`
gewickelt, dessen `Text` gleich `MenuAriaLabel` ist. Kein neuer Parameter: `UX-05`
verlangt Tooltip **und** passenden `aria-label`, und „passend" heißt hier gleichlautend
— ein eigener `MenuTooltip`-Parameter würde erlauben, die beiden auseinanderlaufen zu
lassen, was die Regel gerade verhindern will. Der `aria-label` bleibt, wo er ist, auf
dem Button; die Blase ist `aria-hidden` und verdoppelt die Ansage nicht.

**Zu verifizieren, nicht anzunehmen:** dass die Segment-Optik unverändert bleibt. Die
Selektor-Analyse oben sagt, dass sie es tut — der Beleg ist trotzdem der gerenderte
Baum, nicht die Überlegung. **Falls die Segmentierung doch bricht, ist der Fix falsch**
und der Task meldet das, statt CSS nachzuziehen, bis es passt.

**Tests, zuerst rot.** In `DrylSplitButtonTests.cs`: der Caret liegt innerhalb eines
`.tt-wrap`; dessen `data-tt` trägt den Default `"More actions"`; ein gesetztes
`MenuAriaLabel` erscheint in `data-tt` **und** im `aria-label` des Caret-Buttons; der
Caret ist weiterhin ein Nachfahre von `.popover-anchor`.

**Nebenbei, im selben Commit:** der Kopfkommentar von `DrylSplitButton.razor` zeigt
eine Form, die **nicht kompiliert** — ein blankes `Save` gefolgt von `<MenuItems>`,
obwohl `MenuItems` ein benanntes `RenderFragment` ist und das Label damit in
`<ChildContent>` gehört. `F3` hält das als Kommentardefekt fest. Der Task korrigiert
den Kommentar und streicht den Befund aus der Spec.

**Spec.** `F3` verliert Abweichung 1 und den Kommentar-Befund. **`State` bleibt
`Modified`** — Abweichung 2 steht noch. Das ist der Punkt, an dem die Versuchung
groß ist, schon umzustellen; `SPEC-04` bindet `Implemented` daran, dass **jedes**
Akzeptanzkriterium erfüllt ist.

**`REL-01`/`REL-02`.** `<Version>` `2.22.1` → `2.23.0`, neuer `## [2.23.0]`-Block,
Eintrag unter `Fixed` mit `UX-05` als Grund.

**Verifikation.**
```
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-spec-coverage.mjs      # bleibt 11/127
```

## Task 2 — `DrylSplitButton` wird `DrylAiAware`

**Dateien:** `code/DRYL.Components/Components/Actions/DrylSplitButton.razor`,
`specs/E2 Actions/F3 DrylSplitButton.md`, `specs/E2 Actions/_Api.md`,
`tests/DRYL.Components.Tests/DrylSplitButtonTests.cs`, `CHANGELOG.md`

**Änderung.** `@inherits DrylAiAware`; das eigene `[Parameter] public AiState Ai`
entfällt (es wird geerbt, gleicher Name, gleicher Default); beide Segmente bekommen
`Ai="EffectiveAi"` und `Aura="EffectiveAura"`.

**Warum das nicht breaking ist — und was trotzdem zu prüfen ist.** Für einen
Konsumenten bleibt `Ai` ein `AiState`-Parameter mit Default `AiState.None` am selben
Typ; ob er auf der Klasse oder auf der Basisklasse deklariert ist, sieht der
Razor-Aufrufer nicht. **Zu verifizieren, nicht anzunehmen:** dass `DrylSplitButton`
heute wirklich von nichts erbt (sonst Basisklassen-Konflikt) und dass kein Aufrufer im
Repo `Ai` per Reflection oder über `ComponentBase`-Typprüfung anfasst.

**Der eigentliche Gewinn, und wo er belegt wird.** Der Scope wird künftig **einmal**
aufgelöst, in `DrylSplitButton`, und das Ergebnis an beide Segmente gegeben — statt
zweimal unabhängig in zwei `DrylButton`s. Damit ist der Fall geschlossen, der die
Abweichung war: innerhalb eines `DrylAiScope` mit explizit gesetztem `Ai` zeigte der
Hauptbutton den expliziten, der Caret den Scope-Zustand.

**Tests, zuerst rot** — genau die drei Fälle, die `F3` benennt:
- außerhalb eines Scopes, `Ai` gesetzt → **beide** Segmente tragen die Aura-Klassen;
- innerhalb eines `DrylAiScope`, `Ai` nicht gesetzt → beide tragen dieselbe;
- innerhalb eines Scopes, `Ai` explizit **abweichend** gesetzt → beide tragen die
  explizite, nicht eines die eine und eines die andere. Das ist der Regressionstest
  für die Abweichung selbst und der wichtigste der drei.
- dazu: `Aura` explizit gesetzt schlägt auf beide Segmente durch.

**Spec.** `F3` verliert Abweichung 2 und die notierte `Aura`-Lücke; das Kriterium
„Both segments of one split button render the same effective AI state." ist dann
erfüllt. **`State` → `Implemented`**, mit derselben Sorgfalt wie bei `F7` im
E3-Durchgang: `SPEC-04` vorher lesen und im Commit zitieren. Die beiden verbleibenden
notierten Gaps — kein `aria-haspopup`/`aria-expanded`, `DrylPopover` ohne
`DrylPresence` — **bleiben stehen** und stehen `Implemented` nicht entgegen: keiner
bricht ein Kriterium dieser Spec, und der zweite gehört einer anderen Komponente.
Ebenfalls nachzutragen: dass die Spec unter Abweichung 2 einen Weg angeboten hat, den
`AiScope.Resolve` nicht hergibt.

**`_Api.md` mit anfassen.** Die Datei beschreibt heute drei AI-Opt-in-Formen der
Kategorie; nach diesem Task sind es zwei (`DrylButton` und `DrylSplitButton` beide
`DrylAiAware`, `DrylButtonGroup` gar keine). `SPEC-01` verlangt die Aktualisierung im
selben Commit.

**`REL-01`/`REL-02`.** Kein zweiter Bump. In den `2.23.0`-Block: ein `Added`-Bullet für
`Aura`, ein `Fixed`-Bullet für die Zustandsspaltung, und die ausdrückliche Notiz, dass
`Ai` nicht breaking ist.

**Verifikation.** Wie Task 1, plus die volle Suite.

---

## Reihenfolge und Abhängigkeiten

Task 1 zuerst, weil er den `2.23.0`-Block anlegt und die Testdatei erzeugt, in die
Task 2 nur noch Fälle ergänzt. Beide fassen `DrylSplitButton.razor`, `F3` und
`CHANGELOG.md` an, laufen also **nicht** parallel. Pro Task ein frischer Subagent,
danach ein getrennter Reviewer.

## Abschluss-Verifikation (`CLAUDE.md` Stufe 5)

Nach Task 2, in dieser Reihenfolge, mit **gelesener** Ausgabe:

```
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs      # erwartet: unverändert 11/127
node scripts/check-motion-tokens.mjs
```

Dazu, und hier ohne die Ausrede des Vorgängerblocks: **beide Farbmodi an der laufenden
Docs-Website angesehen.** Dieser Plan ändert Markup und fügt einem gemeinsam
gezeichneten Steuerelement eine zweite Aura hinzu — „es gibt nichts Neues anzusehen"
gilt nicht. Anzusehen ist die Split-Button-Demo auf `/components/button-group`, mit
Tooltip am Caret und mit einem AI-Zustand auf beiden Segmenten.

## Was dieser Plan bewusst nicht schließt

- **`REL-04` für `DrylSplitButton`** — keine eigene Demo-Seite, kein
  `ComponentCatalog`-Eintrag. Beides liegt in `DRYL.Website`, einem eigenen
  Repository mit eigenem Commit, und `F3` hält es fest.
- **Kein `aria-haspopup`/`aria-expanded` am Caret.** Library-Konsistenzlücke gegenüber
  `DrylSelect`, `DrylMultiSelect`, `DrylDatePicker`, `DrylTimePicker` und
  `DrylNotifications`; keine nummerierte Regel verlangt es, und der Code läge in
  `DrylMenu`/`DrylPopover`, nicht hier.
- **`DrylPopover` ohne `DrylPresence`** (`DESIGN-12`) — gehört in die Spec von
  `DrylPopover`, die es noch nicht gibt (`E11 Surfaces` ist leer).
- **Der `UX-05`/`tt-wrap`-Konflikt in `DrylButtonGroup`** — dort bricht ein Tooltip die
  Segmentierung tatsächlich, weil alle Regeln der Gruppe `> .btn` sind. In `F2`
  festgehalten, hier nicht angefasst; die Lösung wäre eine Änderung an `DrylTooltip`
  oder an den Gruppen-Selektoren und damit ein eigener Vorgang.
