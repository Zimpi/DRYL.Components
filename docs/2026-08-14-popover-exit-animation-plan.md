# Der Popover animiert rein, aber nicht raus: Implementation Plan

**Datum:** 2026-08-14
**Ausgangsstand:** `<Version>` `2.23.0` (unveröffentlicht), `11/127`, 1040 Tests
grün, HEAD `392f68a`.

**Ziel:** `DrylPopover` verschwindet animiert statt zu springen — mit dem
vorhandenen Primitiv, nicht mit einer erfundenen Animation.

## Der Befund, am Code belegt

- Das Panel-**Element** wird *nicht* bedingt gemountet. Es muss stehenbleiben:
  `dryl.popover.open` portiert genau diesen Knoten ans `<body>` und
  `dryl.popover.close` gibt ihn zurück.
- Bedingt gemountet ist der Panel-**Inhalt**: `@if (Open) { @PanelContent }`,
  ohne `DrylPresence`.
- Sichtbar wird die Fläche über den Doppelschlüssel `.is-open.is-positioned` in
  `DrylPopover.razor.css`; daran hängt auch die Eintrittsanimation
  (`animation: popover-in var(--dur-fast) var(--ease-out)`). Ein Gegenstück für
  den Austritt gibt es nicht — beim Schließen fallen Klasse und Inhalt
  gleichzeitig weg, die Fläche ist sofort verschwunden.

## Die Regellage — und warum sie hier eng ist

`DESIGN-12` prüft wörtlich „kein `@if` um eine sichtbare Fläche ohne
`DrylPresence`-Wrapper". Der Verstoß besteht.

`DESIGN-13` verlangt, die geteilten Primitive zu benutzen und **keine**
Einzelanimation zu schnitzen; eine Erweiterung des Primitivs liegt auf der
Freigabestufe von `DESIGN-03`. `CLAUDE.md` Stufe 1 macht **eine neue Animation
ausdrücklich zum Blocker mit Maintainer-Freigabe**.

Daraus folgt der Zuschnitt dieser Aufgabe: **ein vorhandenes Primitiv anwenden,
nichts erfinden.**

## Der Fix

`DrylPresence` mit einer **bestehenden** `PresenceTransition` um den
Panel-Inhalt, und der eigentliche Schließvorgang wandert hinter das Ende der
Austrittsanimation:

1. Schließen angefordert → `Open` wird `false`.
2. **Sofort, nicht verzögert:** `aria-expanded="false"` am Trigger. Der
   angesagte Zustand darf einer Animation nie hinterherlaufen — sonst meldet der
   Screenreader „aufgeklappt", während es zugeht.
3. Die Fläche bleibt sichtbar (`.is-open.is-positioned` bleibt, der Knoten
   bleibt portiert), während die Austrittsanimation läuft.
4. `OnExited` → jetzt erst `dryl.popover.close`: Klassen weg, Knoten zurück.

`DrylPresence` bringt `prefers-reduced-motion` von sich aus mit; ohne Bewegung
darf nichts hängenbleiben.

## Die Abbruchbedingung — sie gilt, und sie ist keine Formalie

Zeigt die Messung, dass es **nur** dann richtig aussieht, wenn die
Panel-Fläche selbst eigene Keyframes bekommt (weil `DrylPresence` innerhalb der
Fläche animiert und der Glasrahmen darum stehen bleibt), dann ist das der Punkt,
an dem **gemeldet statt erfunden** wird. Neue Keyframes sind eine neue Animation
und brauchen Maintainer-Freigabe (`DESIGN-13`, `CLAUDE.md` Stufe 1). In dem Fall
endet die Aufgabe mit einem Bericht und ohne Commit am Code.

## Task 1 — Austritt über `DrylPresence`

**Dateien:** `code/DRYL.Components/Components/Surfaces/DrylPopover.razor`,
ggf. `code/DRYL.Components/wwwroot/js/dryl.js`, `CHANGELOG.md`

**Was dabei nicht kaputtgehen darf** — alles in dieser Sitzung frisch gebaut und
gemessen, alles hängt am Schließpfad, den diese Aufgabe verändert:

- Der Pending-Focus-Kanal (`panel.__drylPendingFocus`) und sein Aufräumen in
  `close`.
- Die Tastenbehandlung am Panel (`__drylPanelKeys`) samt Tab-Umlauf in beiden
  Pickern.
- Die Fokusrückgabe der beiden Picker und des Menüs auf Trigger bzw.
  Eingabefeld — sie darf **nicht** erst nach der Animation kommen.
- Der ARIA-Claim (`__drylTriggerHasPopup`, `__drylTriggerExpanded`) und sein
  `release`.

**Verifikation.** Volle Evidenzliste aus `CLAUDE.md` Stufe 5, plus im Browser:

- `/components/popover`, `/components/menu`, `/components/button-group`:
  Austritt sichtbar, keine hängende Fläche, kein Sprung.
- Schnelles Auf-Zu-Auf, zweites Popover öffnen während das erste austritt,
  Außenklick während des Austritts, `Escape` während des Austritts.
- `prefers-reduced-motion: reduce` — sofort weg, nichts bleibt stehen.
- Beide Farbmodi (`DESIGN-02`), da die Fläche selbst betroffen ist.
- Die vier Punkte oben je einmal nachgemessen, nicht erschlossen.
- `dryl-scroll-locked` und Aufräum-Invarianten aus dem `verify`-Skill.

**Spec.** `E11 Surfaces` ist ein Scaffold — nichts zu aktualisieren, dieselbe
bekannte Lücke. `F3 DrylSplitButton` beschreibt Popover-Verhalten bewusst nur
referenzierend; prüfen, ob eine Aussage dort dadurch falsch wird.

**Bump.** `2.23.0` ist unveröffentlicht: ein Bullet im selben Block, kein Bump.
