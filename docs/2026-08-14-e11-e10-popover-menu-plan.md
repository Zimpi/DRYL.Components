# `DrylPopover` und `DrylMenu` bekommen eine Spec: Implementation Plan

**Datum:** 2026-08-14
**Ausgangsstand:** `<Version>` `2.23.0` (unveröffentlicht), `11/127`, 1040 Tests
grün, HEAD `1c20d96`.

**Ziel:** Das Fokus-, Tastatur- und Portalverhalten von `DrylPopover` und
`DrylMenu` ist geschrieben, prüfbar und an genau einer Stelle nachlesbar.

## Warum ausgerechnet diese beiden

`E10 Navigation` (12 Komponenten) und `E11 Surfaces` (8) sind beide Scaffolds.
Diese Sitzung hat zweimal vorgeführt, was das kostet:

- Der `DrylMenu`-Fokusfehler und die beiden Picker-Fehler betrafen alle
  denselben Mechanismus, und es gab **keinen Text**, gegen den man das erwartete
  Verhalten hätte prüfen können. `SPEC-01` musste beide Male tatenlos zusehen,
  weil es nichts zu aktualisieren gab.
- `F3 DrylSplitButton` ist derzeit die **einzige** schriftliche Beschreibung von
  Verhalten, auf das jeder Split-Button-Konsument angewiesen ist — in der Spec
  einer Komponente, der dieser Code gar nicht gehört. Sie sagt das selbst.

**Umfang, bewusst eng:** dieser Plan schreibt zwei Komponenten-Specs, nicht
zwanzig. `DrylPopover` zuerst, weil Menü, Select, Autocomplete, MultiSelect,
beide Picker, `DrylNotifications` und `DrylCitation` darauf stehen. Die
Abdeckung geht von `11/127` auf `13/127`. Die übrigen 18 Komponenten der beiden
Kategorien bleiben offen und sind kein Versäumnis dieses Plans.

## Was die Specs beschreiben müssen — und woher es kommt

Alles hier ist in dieser Sitzung am laufenden System gemessen worden, nicht aus
Doc-Kommentaren übernommen (die waren mehrfach nachweislich falsch — siehe die
zwei in [`../ideas/I4 An exit animation for the popover surface.md`](../ideas/I4%20An%20exit%20animation%20for%20the%20popover%20surface.md)
festgehaltenen Fälle):

- **Der Portal-Umzug.** `dryl.popover.open` hängt das Panel ans `<body>`,
  positioniert es und gibt es beim Schließen zurück. Das Panel-Element bleibt
  dabei durchgehend gemountet; nur sein Inhalt hängt an `@if (Open)`.
- **Der Zwei-Schlüssel-Sichtbarkeitsgate** `.is-open.is-positioned`. `.is-positioned`
  setzt erst JS nach dem Platzieren — deshalb ist ein `focus()` davor ein
  stiller No-op, und deshalb existiert der Pending-Focus-Kanal.
- **Der Pending-Focus-Kanal** (`panel.__drylPendingFocus`): der Konsument
  entscheidet, ob fokussiert wird, das Portal entscheidet wann. Wer nichts
  anfordert, behält seinen Fokus — das ist der Grund, warum `DrylSelect` und
  `DrylAutocomplete` unberührt bleiben.
- **Die Tastenpolitik am Panel** (`__drylPanelKeys`): Herkunft entscheidet, weil
  `KeyboardEventArgs` kein Target trägt; `Tab` läuft im Panel um; Defaults
  werden nur für tatsächlich behandelte Tasten unterdrückt.
- **Der ARIA-Claim am Trigger** (`__drylTriggerHasPopup`, `__drylTriggerExpanded`):
  pro Attribut additiv, ab dem ersten Render, mit der Zielwahl „Element mit
  eigenem `aria-haspopup`, sonst der flachste Kandidat".
- **`CloseOnEscape`** und die Arbeitsteilung, die daraus folgt: wer `false`
  setzt, übernimmt Escape selbst — und muss dafür sorgen, dass der Fokus dort
  ankommt, wo sein Handler hängt. Genau das war der Fehler in beiden Pickern.

## Was als Schuld hineingehört, nicht als Erfolg

- **`DESIGN-12`**: das Panel animiert rein, nicht raus. Gemessen, Ursache
  verstanden, Entscheidung offen — die Spec verweist auf `I4` und trägt es als
  Abweichung, nicht als Lücke im Verhalten.
- **`dryl.timepicker.scrollToActive`** ist am verborgenen Panel wirkungslos.
  Gehört zur Picker-Spec (`E8`, Scaffold), aber die Ursache liegt im Portal und
  wird hier benannt.
- Der Listener aus `__drylPanelKeys` wird nie abgehängt. Begründet, geprüft,
  aber es ist der erste Listener der Bibliothek ohne `detach`-Gegenstück.

## Task 1 — `E11 Surfaces`: `F1 DrylPopover.md`

**Dateien:** `specs/E11 Surfaces/F1 DrylPopover.md`, `specs/E11 Surfaces/_Api.md`,
`specs/E11 Surfaces/_Interop.md`

`SPEC-05` ist vollständig zu belegen, alle sechs Punkte, mit den beiden
Ausnahmen, die begründet werden müssen: die fehlende Austrittsanimation
(`DESIGN-12` → `I4`) und die AI-Entscheidung (`DrylPopover` hat kein
`Ai`-Parameter — das ist eine Entscheidung und wird als solche geschrieben,
`AI-05`).

## Task 2 — `E10 Navigation`: `F1 DrylMenu.md`

**Dateien:** `specs/E10 Navigation/F1 DrylMenu.md`, `specs/E10 Navigation/_Api.md`,
`specs/E10 Navigation/_Interop.md`

Verweist auf `E11` statt nachzuerzählen — dieselbe Trennung, die `F3` sich
erarbeitet hat und dort ausdrücklich begründet ist. Was `F3` an
`DrylMenu`-Verhalten beschreibt, wird geprüft: was jetzt hier steht, gehört
dort referenziert.

## Verifikation für beide Tasks

- Jede Verhaltensaussage am Code oder im Browser belegt. Keine Zeilennummern
  (`SPEC-09`), Englisch (`SPEC-08`), `Meta` mit `State` und `Source` (`SPEC-03`).
- `node scripts/check-spec-coverage.mjs` — die Zahl muss von `11/127` auf
  `13/127` steigen. Das ist der Beleg, nicht der Exit-Code.
- `node scripts/check-harness-links.mjs` grün.
- Reine Spec-Arbeit: **kein** `<Version>`-Bump, **kein** CHANGELOG-Eintrag
  (`REL-03`).
