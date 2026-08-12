# Harness-Umbau Phase B — Implementation Plan

**Datum:** 2026-08-11
**Voraussetzung:** Phase A abgeschlossen (`7538b7a`), Triage der roten Regeln
abgeschlossen (`docs/2026-08-11-red-rule-triage.md`), die beiden vor Phase C fälligen
Produktentscheidungen getroffen und als `ideas/I1` und `ideas/I2` auf `Ready` erfasst.

**Ziel:** Phase C ist ohne Phase B nicht ausführbar — es gibt keine Kategoriestruktur,
in die eine Spec geschrieben werden könnte, und keinen Fortschrittsmesser, an dem
„x von 127 Komponenten" ablesbar wäre. Dieser Plan schafft beides.

## Global Constraints

- **Kein Bibliothekscode wird geändert.** Phase B fasst `specs/`, `ideas/`, `harness/`,
  `scripts/`, `docs/` und `.claude/` an — keine `.razor`, `.cs`, `.css` oder `.js` unter
  `code/`. Damit entfällt der `<Version>`-Bump nach `REL-01`; ein `CHANGELOG`-Eintrag
  wird trotzdem geschrieben, weil der Harness sich sichtbar ändert.
- **Alle Verschiebungen mit `git mv`.** Die 30+ Pläne unter `docs/superpowers/plans/`
  sind laut Design die Primärquelle für Phase C; ihre Historie muss erhalten bleiben.
- **Keine Spec wird geschrieben.** Phase B legt Gerüste an. Eine Spec mit Inhalt ist
  Phase C und folgt `SPEC-03` bis `SPEC-08`.
- **Ein Commit pro Task.**

## Die Kategorieliste

Beschlossen 2026-08-11: **1:1 zu den Code-Ordnern.** Die Zuordnung ist damit aus dem
Pfad prüfbar und das Coverage-Skript kann sie verifizieren. Die drei bekannten
fachlichen Fehlstellen des Ordnerschnitts werden **nicht** stillschweigend korrigiert,
sondern als Idee erfasst (Task 7).

| E | Kategorie | Quelle | Komponenten |
|---|---|---|---:|
| `E1` | Foundation | — (komponentenlos) | 0 |
| `E2` | Actions | `code/DRYL.Components/Components/Actions/` | 3 |
| `E3` | AI | `code/DRYL.Components/Components/AI/` | 8 |
| `E4` | Charts | `code/DRYL.Components/Components/Data/Charts/` | 4 |
| `E5` | Data | `code/DRYL.Components/Components/Data/` | 21 |
| `E6` | Dialogs | `code/DRYL.Components/Dialogs/` | 2 |
| `E7` | Feedback | `code/DRYL.Components/Components/Feedback/` | 8 |
| `E8` | Inputs | `code/DRYL.Components/Components/Inputs/` | 23 |
| `E9` | Layout | `code/DRYL.Components/Components/Layout/` | 22 |
| `E10` | Navigation | `code/DRYL.Components/Components/Navigation/` | 6 |
| `E11` | Surfaces | `code/DRYL.Components/Components/Surfaces/` | 15 |
| `E12` | Agent Runtime | `DRYL.Components.Agents/Agents/`, `/Display/` | 5 |
| `E13` | Agent Tools | `DRYL.Components.Agents/Tools/` | 3 |
| `E14` | Agent Canvas | `DRYL.Components.Agents/Canvas/` | 2 |
| `E15` | Agent Inputs | `DRYL.Components.Agents/Field/`, `/CommandPalette/`, `/Voice/`, `/Generation/` | 5 |
| | | **Summe** | **127** |

`E1 Foundation` ist komponentenlos und trägt nur `_Api.md` und `_Interop.md`: die
Theming-Typen, die DI-Registrierung, `AiState`/`AiAura`, die Motion-Primitiven und die
Token-Oberfläche aus `dryl.css`. Das ist öffentliche Oberfläche, die die 1.0-Freeze
bindet und heute in keiner Komponentenkategorie einen Ort hätte.

`E12`–`E15` bündeln die acht Ordner des Agents-Pakets thematisch, weil vier davon genau
eine Komponente enthalten und eine eigene Kategorie samt zwei Begleitdateien dafür
Zeremonie ohne Ertrag wäre.

---

## Task 1 — `SPEC-02` trägt die Kategorieliste

**Files:** `harness/requirements.md`

`SPEC-02` sagt heute „The category list is `defined in phase B` — do not invent one."
Der Platzhalter wird durch die Tabelle oben ersetzt (E-Nummer, Name, Quellordner).
Zusätzlich ein Satz, dass eine Kategorie komponentenlos sein darf und dann nur die
beiden Begleitdateien trägt — `E1 Foundation` ist der Fall, für den `SPEC-02` heute
keinen Platz hat.

Der `Check:` von `SPEC-02` wird um „jede Kategorie unter `specs/` steht in der Liste,
und jede Kategorie der Liste existiert" ergänzt.

**Verify:** `node scripts/check-harness-links.mjs` grün; `rg -n 'defined in phase B'
harness/` liefert keinen Treffer mehr.

## Task 2 — Kategorieordner und Begleitdatei-Gerüste

**Files:** `specs/E1 Foundation/` … `specs/E15 Agent Inputs/`, je `_Api.md` und
`_Interop.md` (30 Dateien)

Gerüste, kein Inhalt. `_Api.md`: H1 mit dem Kategorienamen, darunter ein Hinweis, dass
die geteilten Typen in Phase C eingetragen werden. `_Interop.md`: H1 plus die drei von
`SPEC-03` geforderten Abschnitte `## Interop`, `## Services`, `## Cleanup`, jeweils mit
`none` gefüllt, bis Phase C sie belegt. Beide ohne `Meta`-Block — `SPEC-03` verbietet
ihn dort ausdrücklich.

**Verify:** 15 Ordner, 30 Dateien; `rg -n '^## Meta' specs/*/_Api.md specs/*/_Interop.md`
liefert keinen Treffer.

## Task 3 — `scripts/check-spec-coverage.mjs`

**Files:** `scripts/check-spec-coverage.mjs`

Die Prüfung aus `SPEC-03`, in beide Richtungen:

1. Jeder Pfad in einem `Source`-Block existiert.
2. Jede `Dryl*.razor` unter `code/` erscheint in genau einem `Source`-Block — keine
   ohne Spec, keine doppelt.

Dazu die strukturellen Prüfungen aus `SPEC-02`/`SPEC-03`, weil sie dieselben Dateien
lesen: Ordnernamen `E{n} {Kategorie}` gegen die Liste aus Task 1; Dateinamen
`F{n} {DrylComponent}.md` oder `F{n} {DrylComponent}/`; jeder Split-Ordner mit genau
einem `_Component.md`; jede Komponentenspec mit `State` **und** `Source`; jede
`S{n}`-Datei mit `State` und ohne `Source`; `_Api.md`/`_Interop.md` ohne beides;
`Source`-Pfade repo-root-relativ mit Forward Slashes.

Ausgabe: eine Zeile pro Verstoß, am Ende `x/127 components covered`, Exit-Code 1 bei
jedem Verstoß. Der Zähler ist der Fortschrittsmesser für Phase C.

Der `Source`-Parser folgt exakt dem Format aus `SPEC-03`: erster Pfad auf der
`- **Source:**`-Zeile, jeder weitere eine eingerückte Fortsetzungszeile mit nichts als
dem Pfad. Kein Erraten — was das Format verletzt, ist ein Verstoß, keine Kulanz.

**Verify:** `node scripts/check-spec-coverage.mjs` läuft und meldet
`0/127 components covered` mit Exit-Code 1 — der ehrliche Ausgangsstand, bevor Phase C
beginnt. Ein Negativtest mit einer weggeworfenen Wegwerf-Spec bestätigt, dass ein
erfundener `Source`-Pfad und eine doppelt beanspruchte Komponente je gemeldet werden.

## Task 4 — `SPEC-02`/`SPEC-03` von `review` auf `script` ziehen

**Files:** `harness/requirements.md`, `CLAUDE.md`

`SPEC-03` trägt heute „`scripts/check-spec-coverage.mjs` is built in **phase B**; until
it exists, this rule is `review`-enforced." Nach Task 3 existiert es: `Enforced` wird
`script`, die Übergangssätze in `SPEC-02` und `SPEC-03` entfallen. In `CLAUDE.md` kommt
das Skript in die Evidenzliste unter Stufe 5, neben `check-harness-links.mjs`.

**Verify:** `rg -n 'phase B' harness/requirements.md` liefert keinen Treffer mehr;
`node scripts/check-harness-links.mjs` grün.

## Task 5 — `ideas.md` scharfstellen

**Files:** `harness/ideas.md`, `ideas/README.md`, `specs/README.md`, `CLAUDE.md`

Der `Status: not yet active`-Block in `harness/ideas.md` entfällt; die Begründung dort
(„`IDEA-05` liest `specs/`; bis es etwas zu lesen gibt, wäre der Prozess leere
Zeremonie") ist mit Task 2 hinfällig. `ideas/README.md` und `specs/README.md` verlieren
ihre „not yet active" / „Empty for now"-Sätze und beschreiben den Ist-Stand.
`CLAUDE.md` verweist auf `harness/ideas.md` ohne Vorbehalt.

**Verify:** `rg -n 'not yet active|Empty for now' harness/ ideas/ specs/ CLAUDE.md`
liefert keinen Treffer; `node scripts/check-harness-links.mjs` grün.

## Task 6 — Superpowers deaktivieren, Pläne archivieren

**Files:** `.claude/settings.local.json`, `docs/superpowers/` → `docs/archive/`

Superpowers wird **projektlokal** deaktiviert (`enabledPlugins`), bewusst nicht global —
`DRYL.Website` und `DRYL.Portfolio` behalten es, bis auch dort ein Harness steht. Der
Grund steht im Phase-A-Design: zwei konkurrierende Prozessvorschriften in einer Sitzung
sind schlimmer als jede der beiden allein.

`docs/superpowers/plans/` wandert per `git mv` nach `docs/archive/plans/`; `audits/` und
`specs/` darunter ebenso, damit nichts zurückbleibt. Ein `docs/archive/README.md`
benennt, was dort liegt und warum es aufgehoben wird: Primärquelle für Phase C, nicht
mehr gültige Prozessdokumentation.

**Verify:** `git log --follow` auf einer beliebigen verschobenen Datei zeigt die
Historie vor dem Umzug; `.claude/settings.local.json` ist gültiges JSON; kein Verweis
im Harness zeigt noch auf `docs/superpowers/`
(`node scripts/check-harness-links.mjs` grün).

## Task 7 — Die drei Ordnerschnitt-Fehlstellen als Idee erfassen

**Files:** `ideas/I3 Component folder layout.md`

Beschlossen ist der Kategorieschnitt 1:1 zum Ordner. Damit erben die Specs drei
Fehlstellen, die beim Auszählen sichtbar wurden und die nach dem Design als Idee
gehören, nicht als stille Korrektur:

- `Components/Layout/` enthält sechs Navigationsbausteine (`DrylNavGroup`,
  `DrylNavLink`, `DrylTabs`, `DrylTab`, `DrylStepper`, `DrylStep`), während
  `Components/Navigation/` nur sechs Komponenten hat.
- `Components/Surfaces/` mischt Flächen, den Chat-Stapel und die Provider-Infrastruktur.
- Dialoge liegen in zwei Ordnern (`Components/Surfaces/DrylDialog`, `Dialogs/`).

Dazu der `CODE-01`-Restbefund aus dem Triage: `CanvasNodeView` ist intern, liegt aber
nicht unter `Internal/`, und ist für das Prüfkommando darum von einer vergessenen
öffentlichen Komponente ununterscheidbar.

Der Befund, der die Idee überhaupt möglich macht: alle 111 Komponenten deklarieren
`@namespace DRYL.Components`. Der Ordner bestimmt den Namespace nicht — ein Umzug wäre
kein API-Bruch. State `Draft`; die Entscheidung gehört nicht in Phase B.

**Verify:** Datei folgt dem Format aus `IDEA-07`; `node scripts/check-harness-links.mjs`
grün.

## Task 8 — `update-docs.md` und die `verify`-Skill auf das neue Modell ziehen

**Files:** `.claude/commands/update-docs.md`, `.claude/skills/verify/SKILL.md`

Beide stammen aus der Zeit vor dem Harness und verweisen auf die alte Struktur. Sie
werden auf `harness/`, `specs/` und die Evidenzliste aus `CLAUDE.md` Stufe 5 gezogen.
Umfang erst nach dem Lesen beider Dateien festlegbar — falls einer der beiden
inhaltlich überholt ist, ist Löschen die richtige Antwort, nicht Umschreiben.

**Verify:** kein Verweis auf entfallene Pfade
(`rg -n 'docs/superpowers|prototype/|samples/' .claude/`).

## Task 9 — Changelog und Abschluss

**Files:** `CHANGELOG.md`

Ein `### Changed`-Eintrag unter `[Unreleased]`: Kategoriestruktur festgelegt,
Coverage-Prüfung aktiv, Ideenprozess verbindlich, Superpowers projektlokal aus. Kein
`<Version>`-Bump — Phase B fasst keinen Bibliothekscode an.

**Verify:** die volle Evidenzliste aus `CLAUDE.md` Stufe 5, obwohl kein Code geändert
wurde — `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`. Der
Zweck ist der Ausgangsstand für Phase C: wenn etwas schon vorher rot ist, will ich das
jetzt wissen und nicht mitten in der ersten Spec.

---

## Was Phase B ausdrücklich nicht tut

- **Keine Spec schreiben.** Das ist Phase C, kategorieweise.
- **Die Entscheidungen aus `I1` und `I2` nicht umsetzen.** Beide fassen Code oder den
  Regeltext an (`AI-03` präzisieren und drei Parameter umbenennen; drei Token anlegen
  und neun Fundstellen retokenisieren). Sie sind entschieden und dokumentiert; die
  Umsetzung ist der erste Schritt von Phase C, mit Version-Bump und Changelog.
- **Die sechs `DESIGN-07`-Treffer nicht beheben.** Drei Komponenten, die mit ihrer Spec
  zusammen bearbeitet werden — das ist der Kern der Triage-Empfehlung.
- **Keinen Code verschieben.** Auch nicht `CanvasNodeView`; das hängt an `I3`.
