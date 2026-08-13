# Escape erreicht die beiden Picker nie: Implementation Plan

**Datum:** 2026-08-13
**Vorgänger:** [`2026-08-13-popover-portal-focus-plan.md`](2026-08-13-popover-portal-focus-plan.md) —
dort wurde derselbe Fehlerbau am `DrylMenu` behoben. Der dortige Fix greift hier
**nicht**, siehe „Die Ursache".
**Ausgangsstand:** `<Version>` `2.23.0` (unveröffentlicht), `11/127`, 1037 Tests grün.

**Ziel:** `Escape` schließt das Panel von `DrylDatePicker` und `DrylTimePicker`,
und der Fokus kehrt danach auf das Eingabefeld zurück.

## Der Befund, am laufenden System gemessen

Docs-Website (`http://localhost:5044`), Playwright, jeweils Klick ins
Eingabefeld → Panel offen → `Escape`:

`/components/datepicker`

```
ENTER vis=hidden parent=DIV targetFound=true active=INPUT   ← dryl.datepicker.focusDay
EXIT  active=INPUT panelHasFocus=false
nach Escape: openPanels=1, active=INPUT
```

`/components/timepicker`

```
window.dryl.timepicker = { attach, detach, scrollToActive }   ← keine Fokusfunktion
beim Öffnen: panelHasFocus=false, active=INPUT, panelTabindex=-1
nach Escape: openPanels=1, active=INPUT
```

Beide Panels bleiben offen. `bUnit` sieht das nicht: es führt `dryl.js` nicht aus
und verwaltet keinen echten Fokus — deshalb sind die 1037 grünen Tests hier kein
Gegenbeweis, und ein bUnit-Test, der so täte, als decke er es ab, wäre schlimmer
als keiner.

## Die Ursache

Gemeinsame Bauform mit dem `DrylMenu`-Fehler: `Escape` hängt am Panel-Handler
(`@onkeydown` auf `.date-panel` bzw. `.time-panel`), `CloseOnEscape="false"` geht
an den `DrylPopover`, und der Fokus steht im Eingabefeld. Zwei **verschiedene**
Ursachen dahinter, keine davon vom Menü-Fix abgedeckt:

- **`DrylDatePicker`:** `OnAfterRenderAsync` ruft `dryl.datepicker.focusDay`. Der
  Aufruf findet statt und findet sein Ziel (`targetFound=true`), aber das Panel
  ist zu diesem Zeitpunkt noch `visibility: hidden` — `DrylPopover` setzt
  `.is-positioned` erst danach. `focus()` auf einem unsichtbaren Element ist ein
  stiller No-op. Der Pending-Focus-Kanal, den `dryl.popover.open` bereits
  auswertet, wird von `focusDay` nicht bespielt; er liegt in `dryl.menu`.
- **`DrylTimePicker`:** versucht überhaupt nicht zu fokussieren. Das Modul kennt
  nur `attach`, `detach`, `scrollToActive`.

## Der Fix

`dryl.popover.open`s Pending-Kanal (`panel.__drylPendingFocus`) ist bereits
allgemein und bleibt unverändert. Geändert werden nur die Konsumenten:

1. Die „versuche zu fokussieren, sonst parke die Anforderung"-Logik wird aus
   `dryl.menu` in einen **privaten, geteilten Helfer** in `dryl.js` gehoben, den
   `dryl.menu`, `dryl.datepicker` und `dryl.timepicker` benutzen. Keine dritte
   Kopie derselben sechs Zeilen. **Keine neue öffentliche API**, keine Änderung
   an `DrylPopover`s Parametern (1.0-Freeze).
2. `dryl.timepicker` bekommt `focusPanel(panel)`, das das Panel selbst fokussiert
   (`role="dialog"`, `tabindex="-1"` sind schon da). Bewusst **nicht** eine
   Zelle: das würde die Spalten-Scrollposition mitverschieben, die
   `scrollToActive` gerade gesetzt hat.
3. **Fokusrückgabe beim Schließen.** Erst dadurch, dass der Fokus künftig im
   Panel steht, entsteht sie als Pflicht: das Panel verschwindet, der Fokus fiele
   sonst auf `<body>`. Beide Picker geben ihn beim Schließen auf ihr
   Eingabefeld zurück.
   **Falle, die dabei zu entschärfen ist:** beide `<input>` tragen
   `@onfocus="Open"`. Ein naives `input.focus()` beim Schließen löst damit sofort
   wieder `Open` aus. Der Rückgabeweg braucht eine einmalige Unterdrückung, und
   genau dieser Fall ist im Browser zu messen (Escape darf nicht in ein
   Wieder-Öffnen laufen).

## Was der Fix nicht ist

Kein `@onkeydown` auf dem Eingabefeld. Das würde `Escape` heilen und die
eigentliche Störung — der Fokus kommt nie im Panel an — stehen lassen; für den
`DrylDatePicker` blieben `ArrowUp/Down`, `Home`, `End`, `PageUp/PageDown` und
`Enter` weiterhin tot, obwohl sie implementiert sind.

## Task 1 — Fokus erreicht die Picker-Panels, `Escape` schließt sie

**Dateien:** `code/DRYL.Components/wwwroot/js/dryl.js`,
`code/DRYL.Components/Components/Inputs/DrylDatePicker.razor`,
`code/DRYL.Components/Components/Inputs/DrylTimePicker.razor`, `CHANGELOG.md`

**Zuerst messen, dann ändern.** Die obige Instrumentierung ist vor der Änderung
zu wiederholen; widerspricht die Messung diesem Plan, ist das zu **melden**, statt
den Fix zu drehen. (Genau dieser Auftrag hat die erste Ursachenanalyse des
Vorgängerplans widerlegt.)

**Verifikation.** Volle Evidenzliste aus `CLAUDE.md` Stufe 5, plus Browser-Messung
auf `/components/datepicker` und `/components/timepicker`, je vorher rot und
nachher grün, protokolliert:

- Öffnen → `panelHasFocus=true`
- `Escape` → `openPanels=0` **und** Fokus zurück auf dem `INPUT` **und** das Panel
  bleibt zu (kein Wieder-Öffnen)
- `DrylDatePicker`: `ArrowRight` bewegt den fokussierten Tag, `Enter` wählt ihn
- Tag per Klick wählen → Panel zu, Fokus zurück auf dem `INPUT`
- beide Farbmodi per Auge (`DESIGN-02`)

**Tests.** Keine bUnit-Attrappe für die Fokusmechanik (Begründung oben). Was in
bUnit ehrlich prüfbar ist — dass `Escape` auf dem Panel-Handler den Zustand
schließt — darf ergänzt werden, aber nie als Beleg für diesen Fehler ausgegeben.

**Bump.** `2.23.0` ist unveröffentlicht: ein weiterer `Fixed`-Bullet im
**selben** Block, kein neuer Bump (`REL-01`, dieselbe Begründung wie in den
Vorgängerplänen — siehe auch die dafür vorgesehene Harness-Ergänzung).

## Nachtrag — Task 2: die Folgen des Fokus im Panel

Erst dadurch, dass der Fokus jetzt tatsächlich im Panel steht, sind Fälle
erreichbar, die vorher tot lagen. Zwei Reviews am laufenden System haben sie
gefunden; sie gehören zu diesem Fehlerbild und werden hier mitbehoben.

**HOCH A — `Enter`/`Space` auf den Monats-Chevrons wählt ein Datum aus.**
`HandleCalendarKeyDown` hängt am `.date-panel` und behandelt `Enter`/`Space`
unabhängig davon, welches Element den Fokus hat. Ein Mausklick auf einen Chevron
fokussiert diesen Button; ab da committet jede Bestätigungstaste einen Wert und
schließt den Picker. Gemessen: Klick auf „Next month", ein `Space` →
`values[0]="10.11.2026"`, `panels=0`. Im Range-Modus blättert derselbe Anschlag
den Monat **und** setzt den Start.

**Der naheliegende Fix ist der falsche.** `Enter`/`Space` einfach nicht mehr zu
behandeln und der Button-Aktivierung der Tagzelle zu überlassen, funktioniert
nicht: das statische `@onkeydown:preventDefault="true"` sitzt auf der Tagzelle
und unterdrückt genau die Klick-Erzeugung, die diesen Weg tragen müsste. Der
Panel-Handler muss die **Herkunft** prüfen und nur auf einer Tagzelle auswählen.

**Nachtrag zum Nachtrag, damit die Aktenlage stimmt:** der Absatz oben galt für
den Stand, auf dem er geschrieben wurde. Die umgesetzte Lösung hat das statische
`preventDefault` der Tagzelle entfernt — auf dem heutigen Stand *würde* „`Enter`
und `Space` gar nicht behandeln" also funktionieren. Die Herkunftsprüfung bleibt
trotzdem die bessere Wahl, weil sie zusätzlich die Monats-Chevrons und den
`Cancel`-Button des `DrylTimePicker` abdeckt, die eine reine Markup-Lösung nicht
erreicht. Sie liegt aus einem zwingenden Grund in `dryl.js` und nicht in .NET:
`KeyboardEventArgs` trägt kein Target, der Handler kann die Herkunft einer Taste
grundsätzlich nicht selbst feststellen.

**MITTEL B — die Monats-Chevrons sind per Tastatur unerreichbar (`UX-01`).**
Der Fokus betritt das Panel ausschließlich auf einer Tagzelle, und seit
`bdf6b9d` schließen `Tab` und `Shift+Tab` das Panel. Die *Aktion* Monatswechsel
bleibt über `PageUp`/`PageDown` erreichbar, die *Elemente* sind es nicht mehr.

**MITTEL C — `Shift+Tab` strandet im `DrylTimePicker`.** Dort blieb das alte
Verhalten: Fokus landet im Seitenfuß, das Panel bleibt offen, `Escape` ist
wieder tot. Dazu die Drift: `Tab` schließt im einen Picker und navigiert im
anderen.

**MITTEL D — das statische `preventDefault` verschluckt auch `Ctrl+F`.** Es ist
bedingungslos; gemessen `defaultPrevented=true` für `Control` und für `Ctrl+F`,
solange eine Tagzelle den Fokus hat.

**Die Entscheidung, die B, C und D zusammen auflöst:** `Tab` wird in beiden
Panels **umlaufend gehalten**, statt zu schließen. Beide Panels sind
`role="dialog"`; damit sind die Chevrons wieder erreichbar (`UX-01`), beide
Picker bekommen dasselbe Tastaturmodell, und `Escape` bleibt der einzige
Ausstieg — den es seit Task 1 an Panel *und* Eingabefeld gibt. Das
bedingungslose `preventDefault` in der Markup weicht dabei einer Unterdrückung
**nur der tatsächlich behandelten Tasten**; wo diese Logik liegt (Blazor-Latch
ist ausgeschlossen — er ist gemessen einen Render versetzt), entscheidet der
Implementierer und begründet es.

Verifikation wie in Task 1, zusätzlich: Chevron fokussieren und `Enter`/`Space`
drücken (darf nur blättern), `Tab` mehrfach durch beide Panels (bleibt drin,
erreicht die Chevrons bzw. Cancel/Done), `Ctrl+F` bei fokussierter Tagzelle
(`defaultPrevented=false`), und beide Picker gegeneinander auf gleiches
Verhalten.

## Ergebnis

Umgesetzt in `dda0a47`, `bdf6b9d`, `130f483`, `1a13797`, dazwischen zwei
getrennte Reviews am laufenden System. Elf Befunde aus beiden Reviewrunden sind
einzeln nachgemessen erledigt. Drei davon waren schwer, und alle drei entstanden
erst dadurch, dass der Fokus nun wirklich im Panel steht — vorher lagen diese
Wege tot. Das ist die Lehre dieses Plans: **wer Fokus in eine Fläche bringt,
erbt jede Taste, die dort ankommt.**

Offen und ausdrücklich nicht Teil davon:

- `dryl.timepicker.scrollToActive` ist derselbe stille No-op am noch verborgenen
  Panel — der Portal-`appendChild` setzt den `scrollTop` der Spalten zurück, die
  gewählte Stunde steht außerhalb des Sichtfelds. Vorbestehend, gemessen, eigene
  Aufgabe, derselbe Pending-Kanal ist der Kandidat.
- Ist der fokussierte Tag durch `Min`/`Max` gesperrt, schrumpft der Tab-Ring auf
  die beiden Chevrons. Kein Trap: eine Pfeiltaste holt den Fokus zurück,
  `Escape` wirkt immer.
- Keine Pfeilnavigation durch die Zeitspalten des `DrylTimePicker`. Die Tasten
  sind jetzt nur noch wirkungslos statt schädlich; echte Navigation wäre neues
  Verhalten und liefe über Ideenstufe und Spec.

## Was dieser Plan nicht schließt

- **`E8 Inputs` ist ein Scaffold** (`_Api.md`, `_Interop.md` leer, keine
  Story für die beiden Picker). `SPEC-01` hat hier nichts zu aktualisieren —
  dieselbe Lücke wie beim Menü. Wenn `E8` an die Reihe kommt, muss die Spec den
  dann geltenden Fokus- und Tastaturstand beschreiben, samt der neuen
  Fokusrückgabe.
- `DrylPopover` ohne `DrylPresence` (`DESIGN-12`) bleibt offen.
- `aria-haspopup`/`aria-expanded` am `DrylSplitButton`-Caret bleibt offen.
