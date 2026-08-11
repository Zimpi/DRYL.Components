# Ordnerschnitt (I3, Richtung A) — Implementation Plan

**Datum:** 2026-08-11
**Voraussetzung:** Phase B abgeschlossen (`86fbf25`), `ideas/I3` auf `Ready` (`c9296d6`)
mit Richtung A und beiden Unterentscheidungen bestätigt.

**Ziel:** Den Ordnerschnitt korrigieren, **bevor** Phase C 127 Specs schreibt, deren
`Source`-Blöcke konkrete Pfade nennen. Danach kostet jeder Umzug ein zweites Anfassen
jeder betroffenen Spec.

## Global Constraints

- **Alle Verschiebungen mit `git mv`.** Die Historie der bewegten Komponenten ist die
  Primärquelle für ihre Specs in Phase C; `git blame` braucht danach `--follow`.
- **Kein Verhalten ändert sich.** Kein Markup, kein Parameter, kein Selektor, keine
  `namespace`-Zeile wird angefasst — nur der Pfad der Datei. Die bestehende Testsuite
  ist damit das Regressionsnetz; ein grüner Lauf ist die vollständige Absicherung.
- **Keine `namespace`-Zeile wird an den neuen Ordner angepasst.** Genau das macht den
  Umzug API-neutral (`I3`, Impact/Public API). Eine Datei, die nach dem Move ihren
  Namespace ändert, ist ein Fehler, kein Aufräumen.
- **Keine Spec wird geschrieben.** Das ist Phase C.
- **Ein Commit pro Task.**

## Was sich bewegt

| Von | Nach | Dateien |
|---|---|---|
| `Components/Layout/` | `Components/Navigation/` | `DrylNavGroup.razor`, `DrylNavLink.razor`, `DrylTabs.razor`, `DrylTab.razor`, `DrylStepper.razor`, `DrylStepper.razor.css`, `DrylStep.razor`, `StepperOrientation.cs`, `StepState.cs` |
| `Components/Surfaces/` | `Components/Providers/` *(neu)* | `DrylThemeProvider.razor`, `DrylToastProvider.razor`, `DrylPresence.razor`, `DrylReconnectModal.razor`, `DrylReconnectModal.razor.css`, `DrylColorModeToggle.razor` |
| `Components/Surfaces/` | `Dialogs/` | `DrylDialog.razor`, `DrylDialogProvider.razor` |
| `Canvas/` | `Canvas/Internal/` *(neu)* | `CanvasNodeView.razor` |

Kategoriezahlen danach: `E1` 0 → 5, `E6` 2 → 4, `E9` 22 → 16, `E10` 6 → 12,
`E11` 15 → 8. Summe unverändert **127** — `CanvasNodeView` trägt kein `Dryl`-Präfix
und gehörte nie dazu.

## Zwei Entscheidungen, die dieser Plan trifft

**1. Die vier Moves sind ein Commit, nicht vier.** `REL-01` verlangt den
`<Version>`-Bump im selben Commit wie die Codeänderung; vier Move-Commits hießen vier
PATCH-Bumps für einen zusammenhängenden Vorgang. `I3` beschreibt den Umzug ohnehin als
„one focused move commit". Der Commit trägt seine eigene Verifikation (Build, Tests,
Coverage), ist also eine gültige Task-Einheit.

**2. `2.20.2` wird als Release geschnitten.** `REL-02` koppelt Bump und
Changelog-Release-Cut. Das nimmt die bereits unter `[Unreleased]` liegenden Einträge
mit — die Voice-Fixes (Agents 0.17.x) und die Phase-B-Harness-Zeilen. Das ist korrekt
nach `REL-02`, aber es ist eine sichtbare Folge, die der Product Owner kennen sollte:
`2.20.2` wird dadurch ein Sammelrelease und keine reine Umzugsversion.

PATCH ist die richtige Stufe: keine neue Komponente, kein neuer Parameter, keine
gebrochene API (`REL-01`).

---

## Task 1 — Die vier Verschiebungen

**Files:** die 18 Dateien der Tabelle oben, `code/DRYL.Components/DRYL.Components.csproj`,
`CHANGELOG.md`

```bash
cd code/DRYL.Components
mkdir -p Components/Providers Canvas/Internal

git mv Components/Layout/DrylNavGroup.razor        Components/Navigation/
git mv Components/Layout/DrylNavLink.razor         Components/Navigation/
git mv Components/Layout/DrylTabs.razor            Components/Navigation/
git mv Components/Layout/DrylTab.razor             Components/Navigation/
git mv Components/Layout/DrylStepper.razor         Components/Navigation/
git mv Components/Layout/DrylStepper.razor.css     Components/Navigation/
git mv Components/Layout/DrylStep.razor            Components/Navigation/
git mv Components/Layout/StepperOrientation.cs     Components/Navigation/
git mv Components/Layout/StepState.cs              Components/Navigation/

git mv Components/Surfaces/DrylThemeProvider.razor      Components/Providers/
git mv Components/Surfaces/DrylToastProvider.razor      Components/Providers/
git mv Components/Surfaces/DrylPresence.razor           Components/Providers/
git mv Components/Surfaces/DrylReconnectModal.razor     Components/Providers/
git mv Components/Surfaces/DrylReconnectModal.razor.css Components/Providers/
git mv Components/Surfaces/DrylColorModeToggle.razor    Components/Providers/

git mv Components/Surfaces/DrylDialog.razor         Dialogs/
git mv Components/Surfaces/DrylDialogProvider.razor Dialogs/

git mv Canvas/CanvasNodeView.razor Canvas/Internal/
```

Dann `<Version>` auf `2.20.2` und den Release-Cut in `CHANGELOG.md` (`[Unreleased]`
wird zu `## [2.20.2] — 2026-08-11`, ein frisches leeres `[Unreleased]` darüber), mit
einem `Changed`-Eintrag für den Umzug: Konsumenten sind nicht betroffen, weil
Namespaces und Assetpfade unverändert bleiben.

**Verification:**

Alles Weitere vom Repo-Root aus — dort liegt `DRYL.slnx`:

```bash
cd ../..
git diff --cached -M --stat                 # jede Zeile eine reine Umbenennung (R100)
grep -rn "^namespace\|@namespace" \
  code/DRYL.Components/Components/Navigation \
  code/DRYL.Components/Components/Providers \
  code/DRYL.Components/Dialogs \
  code/DRYL.Components/Canvas/Internal
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-spec-coverage.mjs        # weiterhin 0/127, keine Violations
```

Erwartet: `--stat -M` zeigt ausschließlich Renames ohne Inhaltsänderung; die
`namespace`-Prüfung zeigt keine Datei, deren Namespace ihren neuen Ordner spiegelt
(`DrylStepper` bleibt `DRYL.Components`, `CanvasNodeView` bleibt
`DRYL.Components.Canvas`); Build und Tests grün; das Coverage-Skript meldet weiterhin
`0/127` und **keine** Violation — die 127 sind pfadunabhängig, weil es nur
`Dryl*.razor` unter `code/` zählt.

## Task 2 — `SPEC-02` und die Kategorie-Begleitdateien nachziehen

**Files:** `harness/requirements.md`, `specs/E1 Foundation/_Api.md`,
`specs/E6 Dialogs/_Api.md`, `specs/E9 Layout/_Api.md`,
`specs/E10 Navigation/_Api.md`, `specs/E11 Surfaces/_Api.md`

Drei Änderungen an `SPEC-02`:

1. Die fünf geänderten Zeilen der Kategorietabelle (`E1`, `E6`, `E9`, `E10`, `E11`),
   inklusive `code/DRYL.Components/Components/Providers/` als Quellordner von `E1`.
2. Der Absatz „**A category may be componentless.** `E1 Foundation` carries no `F{n}`
   file at all …" — die Aussage bleibt als *Möglichkeit* bestehen (`SPEC-02` braucht
   den Fall weiterhin), aber `E1` ist nicht mehr ihr Beispiel. `E1` trägt jetzt fünf
   Komponenten **und** die componentenlose Oberfläche (Theming-Typen, DI-Registrierung,
   `AiState`/`AiAura`, Motion-Primitiven, Token-Oberfläche) in seinen Begleitdateien.
3. Die `**Source folder:**`-Zeile in den fünf betroffenen `_Api.md`.

Die Kategorie-*Liste* bleibt unangetastet bei fünfzehn — das ist die Grenze aus `I3`.

**Verification:**

```bash
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs
grep -n "Providers\|componentless" harness/requirements.md
```

Erwartet: Link-Check `OK`; das Coverage-Skript findet weiterhin alle fünfzehn
Kategorien in Tabelle *und* `specs/` (es liest nur die `E{n} {Name}`-Spalten, die sich
nicht ändern); der `grep` belegt beide Textstellen.

## Task 3 — `I3` schließen

**Files:** `ideas/I3 Component folder layout.md`

Ein `## Decisions`-Eintrag, dass Richtung A umgesetzt ist, mit dem Commit-Hash aus
Task 1. **`State` bleibt `Ready`, nicht `Adopted`:** `IDEA-07` definiert `Adopted` als
„in Specs überführt" und verlangt, dass das Dokument mindestens einen Spec-Pfad
verlinkt. Die Specs entstehen erst in Phase C — erst dann wird `I3` `Adopted`.

**Verification:** `node scripts/check-harness-links.mjs`

---

## Was danach ansteht

Phase C in der Reihenfolge aus der letzten Sitzung: `I2` (drei Motion-Token anlegen,
neun `DESIGN-10`-Schulden retokenisieren), dann `I1` (drei Umbenennungen), dann die
Specs kategorieweise — mit den drei `DESIGN-07`-Frost-Treffern als echter
Komponentenarbeit.

## Was dieser Plan nicht tut

- **Der Chat-Stack bleibt in `Components/Surfaces/`** (`DrylChat`, `DrylChatComposer`,
  `DrylMessage`, `DrylMarkdown`). Kein Kategorie-Slot, und die Liste ist out of scope
  (`I3`, Decisions). Bewusst akzeptierte Schuld.
- **Kein Rename einer Komponente oder eines Parameters.** Das ist `I1`.
- **Keine Änderung am `ComponentCatalog` in `DRYL.Website`.** Er gruppiert nach eigener
  Navigationslogik und referenziert Typen über den Namespace, der sich nicht ändert.
