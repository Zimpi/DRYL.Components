# Phase C — offene Punkte aus E3 AI, dann Kategorie E2 Actions: Implementation Plan

**Datum:** 2026-08-13
**Vorgänger:** [`2026-08-12-e3-ai-specs-plan.md`](2026-08-12-e3-ai-specs-plan.md) — dort
entstanden `F4`–`F8` und die beiden Begleitdateien; Endstand `8/127`.
**Ausgangsstand:** `node scripts/check-spec-coverage.mjs` → `8/127 components covered`,
`DRYL.Components` `<Version>` `2.22.0`, `DRYL.Components.Agents` `0.17.4`.

**Ziel:** die vier Punkte abräumen, die der E3-Durchgang offen zurückgelassen hat, und
danach `E2 Actions` (drei Komponenten) spezifizieren. Zielstand: `11/127`.

## Global Constraints

- **Jede Verhaltensbehauptung am Code geprüft.** Doc-Kommentare sind kein Beleg. Ein
  Kriterium, das ich nicht an `.razor`, `.razor.css` oder `dryl.css` verifizieren kann,
  kommt nicht in die Spec. Der E3-Durchgang hat dreimal gezeigt, wohin das sonst führt
  (vier statt zwei Exception-Typen in `_Interop.md`, zwei zusätzlich veraltete Stellen
  im PR-Template).
- **Keine Token-Reinheit behaupten, die nicht gilt.** Der `DESIGN-01`-Check greppt
  Farben in `*.razor.css`. Er sieht literale Paddings, Höhen und Gaps in `dryl.css`
  nicht — und die drei Actions-Komponenten haben **gar keine** `.razor.css`, ihr CSS
  liegt vollständig in `dryl.css`. Für `E2` heißt das: der Check sagt über diese
  Kategorie strukturell nichts aus, und die Specs müssen ihre Appearance-Aussagen
  jeweils am gelesenen Selektor belegen und die Literale benennen.
- **Bibliothekscode ⇒ `<Version>` + CHANGELOG im selben Commit** (`REL-01`, `REL-02`).
  Das betrifft hier Task 1 und Task 3 — anders als im E3-Plan, der `code/` nicht anfasste.
  Beides sind Fixes ohne API-Änderung, also **PATCH**: `2.22.0` → `2.22.1`.
  Ein Bump, nicht zwei: siehe „Reihenfolge".
- **Ein Commit pro Task** (`SPEC-01`, `SPEC-04`): Spec und ggf. Code zusammen.
- **Specs auf Englisch** (`SPEC-08`), auch wenn dieser Plan deutsch ist.
- **Keine Zeilennummern in Regeldokumenten und Specs** (`SPEC-09`) — Selektoren,
  Symbolnamen, Dateipfade. Task 4 existiert, weil `harness/uiux.md` genau das verletzt.

## Vorab geprüft, damit die Tasks nicht darauf warten

- `DrylAuraElements.razor` rendert vier `div`s (`.ai-aura-ring`, `.ai-aura-comet`,
  `.ai-aura-glow`, `.ai-aura-wash`), **keins** trägt `aria-hidden`. Bestätigt.
- Das Markup wird von **45 Dateien** referenziert (`grep -rl`, ohne `obj/`), verteilt
  über beide Pakete — `DrylButton`, `DrylTable`, `DrylDialog`, alle vier Charts, sämtliche
  Inputs, `DrylAiCanvas` in `DRYL.Components.Agents`. Die „rund dreißig" aus dem
  E3-Plan waren die untere Schätzung; es sind mehr.
- `harness/uiux.md` zitiert unter `UX-04` `line 18` und `line 13`. Bestätigt, und es
  sind die einzigen zwei Zeilennummern-Zitate in der Datei.
- `E2 Actions` enthält genau drei Komponenten: `DrylButton.razor` (6.3 KB),
  `DrylButtonGroup.razor` (2.9 KB), `DrylSplitButton.razor` (4.6 KB). Keine ist ein
  `SPEC-02`-Split-Kandidat, keine hat eine eigene `.razor.css`.
- `specs/E2 Actions/_Api.md` und `_Interop.md` sind heute Scaffolds („*(phase C)*"),
  ohne `Meta`-Block — sie zählen nicht als Coverage.

---

## Task 1 — `aria-hidden` auf den vier Aura-Ebenen (`UX-07`(c))

**Dateien:** `code/DRYL.Components/Components/AI/DrylAuraElements.razor`,
`specs/E3 AI/F7 DrylAuraElements.md`, `CHANGELOG.md`,
`code/DRYL.Components/DRYL.Components.csproj`

**Änderung.** `aria-hidden="true"` auf `.ai-aura-ring`, `.ai-aura-comet`,
`.ai-aura-glow` und `.ai-aura-wash`. Nichts sonst: keine neue Klasse, kein
`pointer-events`, keine Änderung an `AiAuraCss` oder an der `AuraLifecycle`.

**Warum das trotz 45 Konsumenten sicher ist — und was ich trotzdem prüfe.** Die vier
Elemente sind leer, tragen keinen Text, sind nicht fokussierbar und liegen im DOM als
Geschwister vor dem Inhalt des Hosts. `aria-hidden` auf einem leeren, nicht
fokussierbaren Element entfernt nichts aus dem Accessibility-Tree, was vorher nutzbar
war. Das Risiko wäre ein fokussierbarer Nachfahre — die Elemente haben keine
Nachfahren. **Zu verifizieren, nicht anzunehmen:** dass kein Host eigene Kinder in
diese `div`s rendert (die Komponente nimmt kein `ChildContent`, also strukturell
ausgeschlossen — als Beleg zitiere ich die Parameterliste, nicht diesen Satz).

**Spec.** `F7` formuliert das Kriterium bereits, und zwar als eines, das der Code
**nicht** erfüllt („**The layers are exposed to assistive technology.**"). Dieser Task
dreht es um: das Kriterium wird zur erfüllten Anforderung umformuliert, der Absatz über
den offenen Befund entfällt, `.tab-ink`/`.ws-ink` bleiben als Präzedenz stehen.

**`State`: `Modified` → `Implemented`.** Das ist der Punkt, an dem ich aufpassen muss:
`F7` steht auf `Modified` aus **zwei** Gründen, nicht einem. Der zweite — keine
Demo-Seite, kein `ComponentCatalog`-Eintrag — bleibt bestehen und ist am 2026-08-12
bewusst so entschieden worden. Die Frage ist also nicht rhetorisch: **darf `F7` nach
diesem Fix auf `Implemented`?**

Ja, und der Aufhänger ist die Definition von `State` in `harness/requirements.md`, die
ich vor dem Umstellen lese und im Commit zitiere. `SPEC-01` bindet `Modified` an
*Spec und Code stimmen nicht überein*. Nach diesem Task tun sie das. Die
`SPEC-05`-Lücke ist eine Lücke in der Begleitdokumentation, kein Widerspruch zwischen
Spec und Code — sie steht in der Spec unter „Cross-cutting evidence" ausdrücklich als
Schuld benannt und bleibt dort wortgleich stehen. **Wenn `requirements.md` `State`
anders definiert als ich hier annehme, bleibt `F7` auf `Modified` und der Task meldet
das**, statt die Regel dem gewünschten Ergebnis anzupassen.

**`REL-01`/`REL-02`.** `<Version>` `2.22.0` → `2.22.1`, CHANGELOG unter `Fixed`:
`DrylAuraElements` — die vier Aura-Ebenen sind jetzt `aria-hidden`, mit `UX-07` als
Grund. Bump und Eintrag liegen in **diesem** Commit; Task 3 fasst dieselbe Version
danach nur noch mit einem weiteren `Fixed`-Bullet an (siehe „Reihenfolge").

**Verifikation.**
```
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-spec-coverage.mjs      # bleibt 8/127 — State ändert die Zahl nicht
```
Dazu die Sichtprüfung, die der E3-Plan sich ausdrücklich sparen durfte und dieser nicht
darf: **die Aura an einer laufenden Seite in beiden Farbmodi ansehen** (`Ai/Lifecycle`,
`Ai/States`) — dies ist der erste Task seit langem, der Markup ändert. Wenn die Aura
danach aussieht wie vorher, ist das das erwartete Ergebnis und wird als solches notiert.

## Task 2 — `DrylAiStreamTests.Smooth_mode_reveals_a_burst_gradually_and_completely` entflaken

**Dateien:** `tests/DRYL.Components.Tests/DrylAiStreamTests.cs`

**Kein Bibliothekscode, kein Bump, kein CHANGELOG** (`REL-03`).

**Die Ursache, am Code gelesen — nicht geraten.** Der Test schreibt 600 Zeichen in
einem Chunk, wartet mit `WaitForAssertion` darauf, dass *irgendetwas* sichtbar wird,
und **samplet danach** `cut.Markup`, um `midway < 600` zu behaupten. Die Enthüllung
läuft über `RevealTickMs = 16` und `RevealTake(backlog) = max(4, backlog/100)`, bei 600
Zeichen Rückstand also 6 Zeichen pro Tick — rund 100 Ticks, ~1.6 s bis zum vollen Text.
Das Zeitfenster für das Sample ist damit weit; nur: `WaitForAssertion` pollt. Unter Last
kann der erste erfolgreiche Poll erst nach Abschluss der gesamten Enthüllung laufen,
und dann ist `midway == 600`. Der Test misst also nicht „wurde schrittweise
enthüllt", sondern „hat der Poller schnell genug zugeschlagen". Das ist die Flakiness,
und sie sitzt in der Testmethode, nicht in `DrylAiStream`.

**Der Fix: das Verhalten beobachten statt es abtasten.** Statt nach der Tatsache zu
samplen, zeichnet der Test **jede Zwischenrenderung** auf. `DrylAiStream` ruft pro Tick
`Apply(...)` → `InvokeAsync(StateHasChanged)`; über eine `ChildContent`-`RenderFragment`
liegt bei jeder Renderung der `AiStreamContext` vor. Der Test hängt dort ein
`captured.Add(context.Text.Length)` ein — `Text` ist ein `string`, also ein Snapshot,
während `_context` selbst eine wiederverwendete Instanz ist (deshalb wird die **Länge**
festgehalten, nicht der Context).

Danach ohne jedes Zeitfenster:
- `WaitForAssertion`, bis die volle Länge 600 erreicht ist (Timeout großzügig, da nur
  noch Obergrenze, nicht Messgröße).
- Assertion gegen die **aufgezeichnete Liste**: sie enthält mindestens einen Wert echt
  zwischen 0 und 600. Diese Aussage kann nicht mehr verfallen — ein Wert, der einmal
  aufgezeichnet wurde, verschwindet nicht, egal wie langsam der Poller ist.

Damit prüft der Test dasselbe wie vorher — „paced, not dumped" — aber als Historie
statt als Momentaufnahme. Der Direct-Mode-Gegentest bleibt unangetastet.

**Zu verifizieren, nicht anzunehmen:** dass Blazor die Ticks nicht zu einem einzigen
Render-Batch zusammenzieht. Jeder Tick liegt hinter einem eigenen
`await Task.Delay(RevealTickMs, ct)`, also einem eigenen Renderer-Durchlauf — aber das
ist eine Erwartung an den Renderer, und der Beleg ist der Lauf, nicht die Überlegung.
**Falls die Aufzeichnung wider Erwarten nur 0 und 600 enthält, ist der Fix falsch** und
der Task meldet das, statt eine Toleranz einzubauen, bis es grün wird.

**Verifikation.** Nicht ein Lauf, sondern zwanzig — die Flakiness lag bei 1 von 5:
```
for i in $(seq 1 20); do dotnet test DRYL.slnx -c Release \
  --filter FullyQualifiedName~DrylAiStreamTests || break; done
```
Erst wenn zwanzig Läufe grün sind, gilt der Punkt als erledigt; ein einzelner grüner
Lauf ist bei einem Flake **kein** Beleg. Zusätzlich einmal die volle Suite.

## Task 3 — Deutsche Endnutzer-Strings im Core-Paket

**Dateien:** `code/DRYL.Components/Components/Navigation/DrylCommandPalette.razor`,
`code/DRYL.Components/Components/Feedback/DrylAlert.razor`,
`code/DRYL.Components/Components/Feedback/DrylTooltip.razor`, `CHANGELOG.md`

**Umfang — vom Maintainer am 2026-08-13 entschieden: alles im Core-Paket.** Punkt 3 der
Session nannte „Denkt…"; die Prüfung fand mehr. `REL-02` bindet nicht nur den
CHANGELOG, sondern „every other artefact a consumer of the library sees", und nennt
XML-Doc-Kommentare ausdrücklich mit.

| Ort | Heute | Art |
|---|---|---|
| `DrylCommandPalette` — AI-Pille | `Denkt…` | gerendert |
| `DrylCommandPalette` — Args-Kopf, `DrylTooltip` + `AriaLabel` | `Zurück` (2×) | gerendert |
| `DrylCommandPalette` — `ShowConfirmAsync` | `Aktion bestätigen`, `Ausführen`, `Abbrechen` | gerendert, Dialog |
| `DrylAlert` — Dismiss-Button | `aria-label="Benachrichtigung schließen"` | gerendert, a11y-Name |
| `DrylTooltip` — Usage-Block im Kopfkommentar | `Einstellungen öffnen`, `Eintrag löschen` | Doc-Kommentar |

**Ausdrücklich nicht in diesem Task:** `DRYL.Components.Agents/Field/DrylAiField.razor`
(`TriggerLabel`, `CancelLabel`, `AcceptLabel` und zwei Live-Region-Texte). Das sind
Default-Werte öffentlicher `[Parameter]` und zwei angesagte Strings, also eine
sichtbare API-Änderung in einem Paket mit eigenem Versionsstrang (`0.17.4`). Der Task
fasst sie nicht an und **hält den Befund am Ende dieses Dokuments schriftlich fest**,
damit er nicht verloren geht.

**Übersetzungen** (englisch, Ton der umgebenden Strings): `Thinking…`, `Back`,
`Confirm action` / `Run` / `Cancel`, `Dismiss notification`, `Open settings` /
`Delete entry`.

**Zu prüfen beim Umschreiben:** ob eine der Zeichenketten irgendwo als Selektor,
Testerwartung oder Katalogtext gegengelesen wird —
`grep -rn "Denkt\|Zurück\|Benachrichtigung" tests/ ../DRYL.Website/` vor der Änderung.
Trifft es zu, wandert die Anpassung in denselben Commit (Tests) bzw. wird als
`DRYL.Website`-Folgearbeit notiert (eigenes Repository, eigener Commit).

**`REL-01`/`REL-02`.** Kein zweiter Bump: dieser Task läuft nach Task 1 und trägt
lediglich einen weiteren `Fixed`-Bullet in denselben, noch nicht veröffentlichten
`2.22.1`-Block ein. Der CHANGELOG-Eintrag nennt die Verhaltensänderung ehrlich —
sichtbare Beschriftungen und ein `aria-label` ändern sich, was für einen Konsumenten
mit deutscher Oberfläche eine Regression sein kann; das gehört in den Eintrag, nicht
in eine Fußnote.

**Keine Spec betroffen:** `DrylCommandPalette`, `DrylAlert` und `DrylTooltip` haben in
Phase C noch keine Spec (`E7`, `E10` sind leer). Nichts, was auf `Modified` fallen
könnte — geprüft, nicht angenommen.

**Verifikation.** `dotnet build`/`dotnet test` wie oben, plus
`grep -rnE '(ä|ö|ü|ß|Ä|Ö|Ü)' code/DRYL.Components --include="*.razor" --include="*.cs"`
→ erwartet: keine Treffer mehr im Core-Paket.

## Task 4 — `harness/uiux.md`: Zeilennummern unter `UX-04` (`SPEC-09`)

**Dateien:** `harness/uiux.md`

Die Check-Zeile von `UX-04` belegt `aria-live="polite"` mit „confirmed: line 18, with a
supporting doc comment on line 13". `SPEC-09` verlangt Selektoren, Symbolnamen und
Pfade statt Zeilennummern; ein Regeldokument, das seine eigene Beweisführung an
Zeilennummern hängt, veraltet beim nächsten Edit still.

Ersetzt durch den Selektor-Beleg: das `role="status"`-Element von `DrylAiIndicator`
trägt `aria-live="polite"`, der stützende Doc-Kommentar steht im Kopfblock derselben
Datei. Der Rest der Check-Zeile — Reviewer-Prüfung, kein automatisierter Scan — bleibt
unverändert.

**Kein Bibliothekscode** → kein Bump, kein CHANGELOG (`REL-03`).

**Verifikation.** `node scripts/check-harness-links.mjs` grün; Gegenlesen des neuen
Belegs an `DrylAiIndicator.razor`, damit die Ersatzformulierung nicht ihrerseits etwas
behauptet, was dort nicht steht.

---

## Task 5 — `specs/E2 Actions/F1 DrylButton.md`

**Dateien:** `specs/E2 Actions/F1 DrylButton.md` (neu)

Muster: `specs/E3 AI/F4`–`F8` — dieselbe Gliederung (`Meta`, `User Story`,
`Description`, `Public API` als Tabelle, `Acceptance Criteria` in thematischen `###`,
`Cross-cutting evidence (SPEC-05)`) und dieselbe Kriterien-Granularität: ein Kriterium
pro prüfbarer Aussage, jede am Code belegt.

Belegquellen: `DrylButton.razor`, die `.btn*`-Regeln in
`code/DRYL.Components/wwwroot/dryl.css`, `specs/E2 Actions/_Api.md` (Scaffold, wird in
Task 8 gefüllt), und die bestehenden Tests, soweit vorhanden.

Am Code zu belegende Punkte — die Liste ist bewusst als **Fragenkatalog** formuliert,
nicht als Ergebnis, weil ich `DrylButton.razor` für diesen Plan nur in seiner Größe
geprüft habe und nicht Zeile für Zeile:

- Varianten und Größen als `enum` (`CODE-02`), mit den exakten Membernamen aus der
  Deklaration — nicht aus dem Doc-Kommentar.
- Ob `Ai` der Opt-in-Parameter ist und ob er `AI-03` erfüllt (ein Schalter, in der
  aktuellen `AI-03`-Fassung — **nicht** die alte „rendert mit `AiState.None` etwas
  Sinnvolles"-Formulierung). `DrylButton` referenziert `DrylAuraElements`, ist also
  AI-aware; ob der Parameter heute `Ai` oder noch `State` heißt, ist zu prüfen und
  entscheidet über `State: Implemented` vs. `Modified`.
- Loading-/Disabled-Verhalten und was davon `aria-disabled` vs. `disabled` ist.
- Icon-only + `UX-05` (Tooltip **und** passendes `aria-label`) — falls die Komponente
  einen `IconOnly`-Modus hat, ist das ein Kriterium mit Zähnen.
- Fokus-Ring, Tastaturbedienung, `prefers-reduced-motion` (`UX-06`).
- Appearance **ehrlich**: `DrylButton` hat keine `.razor.css`; alle Regeln liegen in
  `dryl.css`, wo der `DESIGN-01`-Check nicht hinsieht. Farben, Radien und Dauern werden
  einzeln am Selektor gelesen, und jedes Literal wird benannt statt Token-Reinheit zu
  behaupten.
- `SPEC-05`: Demo-Seite(n) und `ComponentCatalog`-Eintrag in `DRYL.Website` — beide per
  `grep` belegt, nicht vermutet.

**`State`:** ergibt sich aus der Prüfung; `Implemented` nur, wenn Spec und Code
übereinstimmen.

**Verifikation:** `node scripts/check-spec-coverage.mjs` zeigt `9/127`.

## Task 6 — `specs/E2 Actions/F2 DrylButtonGroup.md`

**Dateien:** `specs/E2 Actions/F2 DrylButtonGroup.md` (neu)

Die kleinste der drei (2.9 KB). Belegquellen: `DrylButtonGroup.razor`, die
`.btn-group*`-Regeln in `dryl.css`.

Zu klären und zu belegen: ob die Gruppe ein eigenes Element mit `role="group"` rendert
oder nur ein Layout-Wrapper ist; wie sie die Radien der Randkinder behandelt (das
klassische `:first-child`/`:last-child`-Muster) und ob sie dafür auf Kinder-Selektoren
angewiesen ist — das wäre ein Vertrag gegenüber dem Konsumenten und gehört dann in die
Acceptance Criteria, so wie `F7` die `AiAuraCss`-Paarung dort hat. Ob sie einen
`AiState`-Parameter trägt; falls nicht, wird das mit derselben Ehrlichkeit wie in `F5`
formuliert („kein `AiState`-Parameter, `AI-03` hat hier kein Subjekt").
`DESIGN-11`/`DESIGN-12`: falls es nichts zu animieren gibt, die ausgeschriebene
Ausnahme, nicht das Weglassen.

**Verifikation:** Coverage zeigt `10/127`.

## Task 7 — `specs/E2 Actions/F3 DrylSplitButton.md`

**Dateien:** `specs/E2 Actions/F3 DrylSplitButton.md` (neu)

Belegquellen: `DrylSplitButton.razor`, `.btn-split*` in `dryl.css`, dazu das
Menü-Primitiv, auf das die Komponente zurückgreift (zu ermitteln — vermutlich
`DrylMenu`/`DrylPopover`; die Spec zitiert, was der Code benutzt, und erfindet nichts).

Schwerpunkt Tastatur und ARIA, weil das die interaktionsreichste der drei ist:
Haupt-Aktion vs. Umschalter, `aria-haspopup`/`aria-expanded`, Fokusführung beim Öffnen
und Schließen, <kbd>Escape</kbd>, und ob der Umschalter icon-only ist — dann greift
`UX-05` mit Tooltip **und** `aria-label`. `DESIGN-12`: das Menü montiert bedingt, also
ist `DrylPresence` zu erwarten; ob es tatsächlich benutzt wird, ist zu prüfen und im
Negativfall ein Befund, der `State: Modified` trägt — nicht etwas, das dieser Task
nebenbei repariert.

**Verifikation:** Coverage zeigt `11/127`.

## Task 8 — `specs/E2 Actions/_Api.md` und `_Interop.md` füllen

**Dateien:** `specs/E2 Actions/_Api.md`, `specs/E2 Actions/_Interop.md`

Beide sind heute Scaffolds mit „*(phase C)*". Nach Task 7 ist bekannt, was
tatsächlich geteilt wird: die Varianten- und Größen-`enum`s, der AI-Opt-in samt seiner
`AI-03`-Begründung, und — für `_Interop.md` — ob die Kategorie überhaupt JS-Interop
benutzt oder einen Service konsumiert. Die Antwort „none" ist erlaubt; sie muss nur
belegt und nicht länger mit „*(phase C)*" markiert sein. Genau hier ist im E3-Durchgang
der Fehler entstanden, den der Vorgängerplan dokumentiert: eine Begleitdatei, die
mehr behauptete als der Code hergab.

**Verifikation:** Coverage bleibt `11/127` (Begleitdateien zählen nicht, solange sie
keinen `Meta`-Block tragen); `node scripts/check-harness-links.mjs` grün.

---

## Reihenfolge und Abhängigkeiten

**Task 1 zuerst**, weil er den `2.22.1`-Block in `CHANGELOG.md` und den `<Version>`-Bump
anlegt; **Task 3 danach**, weil er in denselben Block nur noch einen Bullet ergänzt.
Zwei Bumps für zwei Fixes derselben unveröffentlichten Version wären genau die
Buchhaltung, die der `2.22.0`-Eintrag im CHANGELOG als Sonderfall hat erklären müssen —
das wiederholen wir nicht. Beide fassen `CHANGELOG.md` an, laufen also **nicht**
parallel.

Task 2 und Task 4 hängen an nichts und an niemandem.

Tasks 5–7 sind untereinander unabhängig (keine geteilte Datei). Task 8 hängt an 5–7.

Die Blöcke 1–4 und 5–8 sind gegeneinander unabhängig; die Session arbeitet sie
trotzdem in dieser Reihenfolge ab, weil Block 1–4 der einzige ist, der Bibliothekscode
anfasst, und ein grüner Testlauf danach die Grundlage für alles Weitere ist.

Pro Task ein frischer Subagent, danach ein **getrennter** Reviewer (`CLAUDE.md`
Stufe 4). Nie zwei Implementierungs-Subagents parallel auf demselben Branch. Befunde
gehen an den Implementierer zurück, nicht in die koordinierende Session.

## Abschluss-Verifikation (`CLAUDE.md` Stufe 5)

Nach Task 8, in dieser Reihenfolge, mit **gelesener** Ausgabe:

```
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs      # erwartet: 11/127 components covered
node scripts/check-motion-tokens.mjs
```

`check-spec-coverage.mjs` endet weiterhin non-zero — Phase-C-Normalfall. Der Beleg ist
die gestiegene Zahl, nicht der grüne Exit.

Dazu, anders als im E3-Durchgang: **beide Farbmodi am laufenden Docs-Website
angesehen**. Dieser Plan ändert Markup (Task 1) und sichtbare Beschriftungen (Task 3);
die Begründung „es gibt nichts Neues anzusehen" gilt hier nicht.

## Offene Punkte, die dieser Plan bewusst nicht schließt

- **`DrylAiField` im Agents-Paket** trägt deutsche Defaults öffentlicher Parameter
  (`TriggerLabel` = „Mit AI ausfüllen", `CancelLabel`, `AcceptLabel`) und zwei deutsche
  Live-Region-Texte („AI-Vorschlag bereit — übernehmen oder verwerfen.",
  „Vorschlag übernommen."). `REL-02` gilt dort genauso. Nicht in Task 3, weil es
  sichtbare Defaults öffentlicher API in einem Paket mit eigenem Versionsstrang
  (`0.17.4`) sind — eine eigene Entscheidung mit eigenem Bump, kein Nebeneffekt eines
  Sprach-Fixes im Core.
- **Die `SPEC-05`-Lücke von `DrylAuraElements`** (keine Demo, kein Katalogeintrag)
  bleibt offen und in `F7` dokumentiert, wie am 2026-08-12 entschieden.
- **`DrylTooltip`, `DrylAlert`, `DrylCommandPalette` haben keine Spec.** Task 3 ändert
  ihren Code, ohne dass `SPEC-01` eine Spec-Aktualisierung verlangen kann — es gibt
  nichts zu aktualisieren. Wenn `E7` und `E10` an der Reihe sind, müssen die Specs den
  dann geltenden, englischen Stand beschreiben.
