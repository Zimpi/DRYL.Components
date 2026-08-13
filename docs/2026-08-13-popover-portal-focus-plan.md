# Der Portal-Umzug wirft den Fokus weg: Implementation Plan

**Datum:** 2026-08-13
**Vorgänger:** [`2026-08-13-f3-splitbutton-reconcile-plan.md`](2026-08-13-f3-splitbutton-reconcile-plan.md) —
dort wurde der Befund gemessen, der diesen Plan auslöst.
**Ausgangsstand:** `<Version>` `2.23.0` (unveröffentlicht), `11/127`, 1037 Tests grün.

**Ziel:** `DrylMenu` bewegt den Fokus beim Öffnen tatsächlich in das Panel, und
`Escape` schließt das Menü wieder.

## Der Befund, am laufenden System gemessen

Auf `/components/button-group` und ebenso am schlichten Menü auf
`/components/menu` — der Split-Button und sein Tooltip sind damit entlastet:

- Öffnen lässt den Fokus auf dem Trigger stehen, statt ihn auf das erste Item zu
  setzen. Mit echtem Mausklick **und** mit `Enter` reproduziert.
- `dryl.menu.focusPanel` **wird** gerufen, mit dem richtigen Element
  (`DIV.popover-panel is-open …`, durch Instrumentierung der Funktion belegt).
  Der Aufruf findet also statt und wirkt nicht.
- `Escape` erreicht das Menü nie: kein `focusTrigger`-Aufruf, das Menü bleibt
  offen. Der Key-Handler hängt am Panel, und dort ist der Fokus nie angekommen.
- Konsole sauber, alle drei `dryl.menu`-Funktionen vorhanden.

## Die Ursache

**Dieser Abschnitt stand zuerst falsch hier, und die erste Fassung wird bewusst
mitdokumentiert, weil der Irrtum lehrreich ist.**

Beide Fassungen teilen die Reihenfolge, und die ist gemessen und richtig:

1. `DrylMenu.OnAfterRenderAsync` ruft `dryl.menu.focusPanel(panel)`.
2. Danach läuft `DrylPopover.OnAfterRenderAsync` und ruft `dryl.popover.open`,
   dessen erster Schritt `document.body.appendChild(panel)` ist.

`DrylMenu` ist die Elternkomponente von `DrylPopover`; Blazor ruft
`OnAfterRenderAsync` in Renderreihenfolge, also Eltern vor Kind.

**Die verworfene Erklärung:** das Umhängen des Knotens blurre den zuvor
gesetzten Fokus. Plausibel, und der Blur ist real — aber nicht die Ursache.

**Die gemessene Ursache:** zum Zeitpunkt von `focusPanel` ist das Panel noch
`visibility: hidden`. Es trägt `.is-open`, aber `DrylPopover.razor.css` schaltet
die Sichtbarkeit über den Doppelschlüssel `.is-open.is-positioned`, und
`.is-positioned` wird erst **innerhalb** von `dryl.popover.open` gesetzt, nach
dem Portal-Umzug. `focus()` auf einem unsichtbaren Element ist ein stiller
No-op. Der Fokus landet also nie im Panel — es gibt keinen, den das Umhängen
wegwerfen könnte.

Der Beleg ist die Instrumentierung beider Funktionen im Browser:

```
menu.focusPanel ENTER visibility=hidden  active=BUTTON.btn.btn-primary
menu.focusPanel EXIT  panelContainsActive=false      ← vor jedem appendChild
popover.open   ENTER  visibility=hidden
popover.open   EXIT   visibility=visible parent=BODY
```

`panelContainsActive=false` **vor** dem Umzug ist die Zeile, die die erste
Erklärung widerlegt. Der Fix, den dieser Plan zuerst vorschrieb — aktives
Element ums `appendChild` sichern und zurückgeben — hätte geprüft, ob der
Trigger-Button im Panel liegt, `false` bekommen und nichts getan.

Betroffen war nicht nur `Escape`: `ArrowUp`, `ArrowDown`, `Home` und `End`
hängen am selben Panel-Handler und kamen ebenso wenig an. Auch das ist am
vorherigen Zustand nachgestellt und gemessen worden, nicht gefolgert.

## Der Fix

**Ein privater Pending-Focus-Kanal innerhalb von `dryl.js`.** `focusPanel`
prüft, ob der Fokus tatsächlich angekommen ist; wenn nicht, parkt es eine
einmalige Anforderung am Panel-Knoten. `dryl.popover.open` wendet sie an,
unmittelbar nachdem `.is-positioned` gesetzt ist — also in dem Moment, in dem
das Panel fokussierbar wird. `dryl.popover.close` räumt eine nie eingelöste
Anforderung weg.

Die **Entscheidung** zu fokussieren bleibt damit beim Menü, nur das **Timing**
wandert zum Portal. Das ist der Grund, warum `popover.open` nicht selbst
fokussiert: `DrylSelect`, `DrylDatePicker` und `DrylTimePicker` lassen den Fokus
absichtlich auf ihrem Trigger beziehungsweise Eingabefeld, ein pauschales
„fokussiere das Panel" wäre für sie eine Regression.

**Keine neue API.** Erwogen und verworfen: ein `OnPortaled`-`EventCallback` auf
`DrylPopover`, an dem `DrylMenu` fokussiert. Das wäre die sauberere Trennung,
erweitert aber die 1.0-gebundene Oberfläche eines geteilten Primitivs für einen
Fehler, der ohne API-Änderung behebbar ist. Der Pending-Kanal liegt im Geist
nahe daran, bleibt aber vollständig in `dryl.js` und benutzt das dort bereits
etablierte Muster für privaten Zustand am Knoten (`anchor.__drylMenu`,
`slot.__drylToast`, `el.__drylModal`).

**Der Blur beim Umhängen bleibt trotzdem behandelt** — als ausdrücklich so
kommentierte Härtung, nicht als der Fix. Er ist gemessen real
(`focusSurvivesReparent: false`), und sobald überhaupt Fokus in einem Panel
landen kann, liefe ein künftiger Konsument hinein, der vor dem Portal etwas
fokussiert.

**Bump:** `2.23.0` ist noch unveröffentlicht. Ein Bugfix ohne API-Änderung ist
PATCH; da `2.23.0` aber noch nicht draußen ist, bekommt er einen weiteren
`Fixed`-Bullet in **denselben** Block, keinen eigenen Bump — dieselbe
Begründung wie in den beiden Vorgängerplänen.

## Task 1 — Fokus überlebt den Portal-Umzug

**Dateien:** `code/DRYL.Components/wwwroot/js/dryl.js`, `CHANGELOG.md`,
`tests/DRYL.Components.Tests/` (siehe unten), ggf.
`specs/E2 Actions/F3 DrylSplitButton.md`

**Zuerst die Ursache belegen**, dann erst ändern: beide Aufrufe im Browser
instrumentieren und die Reihenfolge protokollieren. Genau das hat die erste
Fassung dieses Plans widerlegt — der Auftrag, bei widersprechender Messung zu
melden statt den Fix zu drehen, hat funktioniert und bleibt Bestandteil.

**Tests.** Hier ist ehrlich zu sein: bUnit rendert kein echtes DOM mit
Fokusverwaltung und führt `dryl.js` nicht aus — die bUnit-Suite **kann** diesen
Fehler nicht sehen, und das ist der Grund, warum er an 1037 grünen Tests
vorbeigekommen ist. Ein bUnit-Test, der so tut, als prüfe er das, wäre
schlimmer als keiner. Der Beleg ist deshalb die Messung im Browser, vorher rot
und nachher grün, protokolliert. Falls sich eine sinnvolle Regressionsschranke
in der vorhandenen Suite ziehen lässt, gern zusätzlich — aber nicht als Ersatz.

**Verifikation.** Volle Suite plus die Browser-Messung an **beiden** Stellen:
`/components/menu` (schlichtes Menü) und `/components/button-group`
(Split-Button), jeweils Öffnen → Fokus auf dem ersten Item, `Escape` → Menü zu
und Fokus zurück auf dem Trigger.

## Task 2 — `F3` nachziehen

Nach dem Fix stimmen die Kriterien wieder, die `40ca710` als falsch entfernt
hat. `F3` beschreibt dann das reparierte Verhalten, der Befund wandert aus den
`Recorded gaps`, und `State` geht zurück auf `Implemented` — sofern die Messung
das hergibt, und nur dann.

## Was dieser Plan nicht schließt

- **`DrylMenu` und `DrylPopover` haben keine Spec** (`E10`, `E11` sind leer).
  Dieser Plan ändert ihren Code, ohne dass `SPEC-01` eine Spec-Aktualisierung
  verlangen kann — es gibt nichts zu aktualisieren. Wenn die Kategorien an die
  Reihe kommen, muss die Spec den dann geltenden Stand beschreiben, samt dieser
  Fokus-Regel.
- **`DrylPopover` ohne `DrylPresence`** (`DESIGN-12`) bleibt offen.
- **Kein `aria-haspopup`/`aria-expanded`** am Caret bleibt offen.
