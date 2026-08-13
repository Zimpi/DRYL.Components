# Der Caret sagt nicht, dass er ein Menü öffnet: Implementation Plan

**Datum:** 2026-08-13
**Ausgangsstand:** `<Version>` `2.23.0` (unveröffentlicht), `11/127`, 1037 Tests
grün, HEAD `250b637`.

**Ziel:** Der Caret des `DrylSplitButton` kündigt sein Menü an
(`aria-haspopup`) und meldet, ob es offen ist (`aria-expanded`) — ohne die
Konsumenten zu stören, die das längst selbst tun.

## Der Befund

Am Code belegt: `aria-haspopup` und `aria-expanded` kommen in
`DrylSplitButton.razor`, `DrylMenu.razor` und `DrylPopover.razor` an keiner
Stelle vor. `DrylSelect` und `DrylMultiSelect` emittieren
`aria-haspopup="listbox"` mit lebendem `aria-expanded`; `DrylDatePicker`,
`DrylTimePicker` und `DrylNotifications` emittieren `aria-haspopup="dialog"`
mit einem. Der Caret ist derselbe Bau von Trigger und emittiert keines von
beiden: einem Screenreader-Nutzer wird das zweite Segment als gewöhnliche
Schaltfläche angesagt, und dass ein Menü aufgegangen ist, erfährt er nie.

In `F3 DrylSplitButton` steht das als `Recorded gap`. Keine nummerierte Regel
verlangt es — der Grund, es trotzdem zu schließen, ist Bibliothekskonsistenz,
und der Befund benennt ihn selbst so.

## Warum das nicht in `DrylSplitButton` zu lösen ist

Der Caret ist ein `DrylButton` im `Trigger`-Fragment eines `DrylMenu`.
`aria-haspopup="menu"` könnte `DrylSplitButton` statisch mitgeben.
`aria-expanded` braucht den Offen-Zustand, und der liegt als privates `_isOpen`
in `DrylMenu`; dessen Parameterliste (`Trigger`, `Items`, `Label`, `Placement`,
`Block`, `Class`) gibt ihn nicht heraus. Über einen Cascading Value geht es
ebenfalls nicht: das Trigger-Fragment wird zur Laufzeit zwar unter `DrylMenu`
gerendert, seine Attributwerte stammen aber aus dem Kontext von
`DrylSplitButton` — der Caret kann die Kaskade nicht lesen. Der Code liegt also
in `DrylMenu`/`DrylPopover`, genau wie der Gap-Eintrag vermutet.

## Der Fix

**`dryl.popover.open` und `.close` setzen die beiden Attribute auf dem
fokussierbaren Element im Trigger.** Diese Stelle kennt es bereits:
`dryl.menu.focusTrigger` sucht dasselbe Element mit demselben Selektor.
`DrylPopover` kennt `PanelRole` und gibt ihn an den Aufruf mit.

**Strikt additiv, und das ist die tragende Bedingung:** gesetzt wird nur, wenn
das Element **kein** `aria-haspopup` hat. Wer es selbst schreibt — `DrylSelect`,
`DrylMultiSelect`, beide Picker, `DrylNotifications` — bleibt unberührt, und
niemand schreibt gegen Blazors Attribut-Diffing an. Das Eigentum wird am Knoten
vermerkt (Muster wie `__drylPendingFocus`, `__drylPanelKeys`), und nur wer es
hat, pflegt `aria-expanded` und räumt beim Schließen wieder auf.

**Keine neue öffentliche API.** Erwogen und verworfen: ein Parameter an
`DrylPopover`. Dieselbe Begründung wie beim verworfenen `OnPortaled` im
Popover-Plan — die 1.0-gebundene Oberfläche eines geteilten Primitivs wächst
nicht für etwas, das ohne sie geht.

**Zu prüfen, bevor gebaut wird** (Reihenfolge einhalten, dies ist die
Rot-Messung): trägt der Caret im Browser wirklich keines der beiden Attribute,
und tragen die fünf Vergleichskomponenten sie wirklich? Am laufenden System
ablesen, nicht aus dieser Datei übernehmen.

**Risiko, das die Messung klären muss:** ein Re-Render des Trigger-Inhalts
könnte ein per JS gesetztes Attribut verlieren, wenn Blazor den Knoten neu
aufbaut. Zu messen: Menü öffnen, einen Re-Render des Triggers auslösen
(`Disabled` umschalten oder Zustandswechsel auf der Demo-Seite), `aria-expanded`
danach ablesen. Geht es verloren, ist das zu **melden** — dann ist der Weg über
JS der falsche und der Parameter am `DrylPopover` doch die ehrlichere Lösung.

## Task 1 — Trigger-Zustand am Popover

**Dateien:** `code/DRYL.Components/wwwroot/js/dryl.js`,
`code/DRYL.Components/Components/Surfaces/DrylPopover.razor`,
`specs/E2 Actions/F3 DrylSplitButton.md`, `CHANGELOG.md`

**Verifikation.** Volle Evidenzliste aus `CLAUDE.md` Stufe 5, plus im Browser:

- `/components/button-group`: Caret trägt `aria-haspopup="menu"`, `aria-expanded`
  wechselt `false` → `true` → `false` über Öffnen und Schließen, und zwar bei
  Mausklick **und** bei `Escape`.
- `/components/menu`: derselbe Nachweis am schlichten Menü.
- Gegenprobe, dass nichts doppelt oder überschrieben wird: `/components/select`,
  `/components/multiselect`, `/components/datepicker`, `/components/timepicker`,
  `/components/notifications` — je genau **ein** `aria-haspopup` mit dem Wert von
  vorher, `aria-expanded` weiterhin lebend.
- Beide Farbmodi sind hier ohne Belang (kein CSS), das entfällt begründet.

**Spec.** `F3` beschreibt danach das neue Verhalten, und der Eintrag wandert aus
den `Recorded gaps` — im selben Commit (`SPEC-01`). `E10`/`E11` sind leer, für
`DrylMenu`/`DrylPopover` gibt es weiterhin nichts zu aktualisieren; das bleibt
die bekannte Lücke.

**Tests.** Hier ist bUnit ausnahmsweise **nicht** blind: setzt der Fix das
Attribut per JS, sieht bUnit es nicht — sagt das dann ehrlich und verlässt sich
auf die Browser-Messung. Nur was ohne `dryl.js` wahr ist, darf ein bUnit-Test
behaupten.

**Bump.** `2.23.0` ist unveröffentlicht: ein weiterer Bullet im selben Block,
kein neuer Bump.
