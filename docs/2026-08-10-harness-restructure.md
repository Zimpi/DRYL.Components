# Harness-Umbau: Struktur, Regelwerk, Spec-Driven Development

**Datum:** 2026-08-10
**Status:** Design freigegeben
**Repository:** DRYL.Components

Dieses Dokument ist ein einmaliger Migrationsentwurf. Es beschreibt den Umbau der
Repository-Struktur und die Einführung eines härtbaren Harness. Nach Abschluss von
Phase C hat es keine Funktion mehr und kann entfallen.

---

## 1. Ziel

Heute liegen Regeln, Referenzen und Community-Health-Dateien unsortiert im Root, und
die verbindlichen Regeln stehen als 275-Zeilen-Fließtext in `CLAUDE.md`. Es gibt keine
Möglichkeit, eine einzelne Regel zu referenzieren, ihren Verbindlichkeitsgrad zu
erhöhen oder ihre Einhaltung zu prüfen.

Ziel ist ein **Harness**: ein Regelwerk, das über die Zeit gehärtet werden kann, weil
jede Regel eine stabile Identität, einen expliziten Verbindlichkeitsgrad und einen
benannten Prüfweg hat. Zweites Ziel ist der Wechsel zu **spec-driven development**:
`specs/` und `code/` werden ein einziges Artefakt, das synchron gehalten werden muss.

Der Umbau erfolgt in drei Phasen:

| Phase | Inhalt |
|---|---|
| **A** | Ordnerstruktur, Harness-Dateien, `requirements.md` + `ideas.md`, neue `CLAUDE.md` |
| **B** | Spec-Gerüst unter `specs/`, Superpowers ablösen, `ideas.md` scharfstellen |
| **C** | Reverse-Engineering der Specs aus der Codebasis, 127 Komponenten |

Dieses Dokument spezifiziert **Phase A** vollständig und legt die Entscheidungen fest,
auf die B und C aufbauen.

---

## 2. Zielstruktur

```
DRYL.Components/
├── CLAUDE.md                       Router + Kernregeln, ~70 Zeilen
├── README.md  LICENSE  CHANGELOG.md  CONTRIBUTING.md
├── CODE_OF_CONDUCT.md  SECURITY.md  THIRD_PARTY_NOTICES.md
├── DRYL.slnx  Directory.Build.props
│
├── harness/
│   ├── code.md          CODE-*     Blazor-Naming, typisierte Parameter,
│   │                               Dependency-Bar, Bau-Checkliste
│   ├── design.md        DESIGN-*   Tokens, zwei Modi, Glas, Akzente,
│   │                               Motion-Vokabular, Animationspflicht
│   ├── uiux.md          UX-*       A11y, Tastatur, Fokus, Tooltips,
│   │                               reduced-motion
│   ├── ai.md            AI-*       AiState, Ai-Parameter, Aura-Lifecycle
│   ├── requirements.md  SPEC-*     Wie Specs strukturiert und formuliert werden
│   ├── ideas.md         IDEA-*     Wie aus einer Idee eine reife Idee wird
│   ├── releasing.md     REL-*      Versionierung, CHANGELOG, Publish
│   ├── tokens.md                   ← DESIGN_TOKENS.md (Referenz)
│   ├── patterns.md                 ← COMPONENT_PATTERNS.md (Referenz)
│   ├── conventions.md              ← CONVENTIONS.md (Referenz, API-Freeze)
│   └── theming.md                  ← THEMING.md (Referenz)
│
├── specs/                          Phase B/C
│   └── E{n} {Kategorie}/
│       ├── _Api.md
│       ├── _Interop.md
│       ├── F{n} {DrylKomponente}.md
│       └── F{n} {DrylKomponente}/
│           └── S{n} {Aspekt}.md
│
├── ideas/                          Phase B
│   └── I{n} {Ideen-Name}.md
│
├── code/
│   ├── DRYL.Components/
│   └── DRYL.Components.Agents/
│
├── tests/DRYL.Components.Tests/
├── docs/                           screenshots, gifs, archive
├── samples/  prototype/  scripts/
└── .github/  .claude/
```

### Begründung der Ablageorte

- **`specs/` und `ideas/` auf Root-Ebene**, nicht unter `docs/`. Sie sind Vertrag, nicht
  Dokumentation. `harness/requirements.md` verhält sich zu `specs/` wie
  `harness/code.md` zu `code/` — diese Symmetrie ist beabsichtigt und trägt das
  gesamte Modell.
- **Community-Health-Dateien bleiben im Root.** GitHub erkennt `CONTRIBUTING.md`,
  `SECURITY.md`, `CODE_OF_CONDUCT.md` und `LICENSE` nur dort; `README.md` und
  `CHANGELOG.md` gehen in die NuGet-Pakete ein.
- **`harness/` bekommt keine Index-Datei.** `CLAUDE.md` ist der Router. Ein zweiter
  Index würde davon abdriften.
- **`.claude/` bleibt getrennt von `harness/`.** `.claude/` konfiguriert das Werkzeug
  Claude Code, `harness/` ist das Regelwerk des Projekts und gilt unabhängig davon,
  wer oder was daran arbeitet.

---

## 3. Regelformat

Jede Regel in `code.md`, `design.md`, `uiux.md`, `ai.md`, `requirements.md`,
`ideas.md` und `releasing.md` folgt demselben Rumpf:

```markdown
### DESIGN-07 — Frost only on floating surfaces

Status: **binding** | Enforced: **script**

Floating over content (topbar, popover, dialog) → `--panel-float` +
`--glass-fx-float`. In the flow → `--glass-fx-flow`. On an opaque
background → no `backdrop-filter` at all.

❌ `backdrop-filter: blur(12px)` on a new in-flow surface.

Check: `rg 'backdrop-filter:\s*blur\(' code/` returns nothing outside dryl.css
```

### Die drei Felder

**ID** — stabil und nie recycelt. Wird eine Regel gestrichen, bleibt ihre Nummer
verbrannt. Damit bedeutet „verstößt gegen DESIGN-07" auch in einem Jahr noch dasselbe.
Vergabe in Themenblöcken mit Lücken (`CODE-01…09` Regeln, `CODE-20…` Prozess), damit
spätere Einschübe nicht umnummerieren.

**Status** — der Härtungsgrad der Regel selbst:

| Status | Bedeutung |
|---|---|
| `binding` | Ein Verstoß blockiert den Merge. Keine Ausnahme ohne Maintainer-Freigabe. |
| `default` | Gilt, solange nichts dagegen spricht. Abweichung wird im PR begründet. |
| `guidance` | Empfehlung. Kein Verstoß, wenn abgewichen wird. |

Eine Regel kann als `guidance` eingeführt und später hochgestuft werden, ohne neu
formuliert zu werden. Das ist der eigentliche Härtungspfad.

**Enforced** — wie die Einhaltung festgestellt wird: `script`, `grep` oder `review`.
Die Menge der `review`-Regeln ist das Härtungs-Backlog. Jede Regel, die von `review`
auf `grep` oder `script` wandert, hängt nicht mehr von Aufmerksamkeit ab.

`tokens.md`, `patterns.md`, `conventions.md` und `theming.md` bleiben
Referenzdokumente **ohne** IDs. Sie beschreiben, sie schreiben nicht vor.

---

## 4. Inhaltsverteilung der heutigen CLAUDE.md

| Heute | Künftig |
|---|---|
| §1 System-Absatz | bleibt in `CLAUDE.md`, gekürzt |
| 2.1 Tokens statt Literale | `DESIGN-01` |
| 2.2 Zwei Modi, eine Identität | `DESIGN-02`, Detail → `theming.md` |
| 2.3 Glasflächen / Frost-Budget | `DESIGN-05` … `DESIGN-07` |
| 2.4 Akzente glühen | `DESIGN-08` |
| 2.5 Motion-Vokabular | `DESIGN-10` |
| 2.12 Animationspflicht | `DESIGN-11` … `DESIGN-13` |
| §4 „Looking right"-Checkliste | Abschlussabschnitt in `design.md` |
| 2.6 Blazor-Naming | `CODE-01` |
| 2.7 Typisierte Parameter | `CODE-02` |
| 2.8 Keine externen Runtime-Dependencies | `CODE-03` |
| §3 Bau-Checkliste | `CODE-20` |
| §5 Was vorher zu klären ist | `CODE-21` |
| 2.9 Accessibility | `UX-01` … `UX-04` |
| 2.11 Icon-only Tooltip | `UX-05` |
| 2.10 AI-Mode (7 Bullets) | `AI-01` … `AI-07` |
| §7 Versionierung, CHANGELOG, Catalog | `REL-01` … `REL-05` in `releasing.md` |
| §6 „Things you should never do" | aufgelöst — jede Zeile wird das ❌ ihrer Regel |

Die „never do"-Liste verschwindet als eigener Abschnitt. Sie ist heute eine zweite,
lose gekoppelte Kopie der Regeln und driftet deshalb. Jedes Verbot zieht zu der Regel,
die es negiert.

---

## 5. Die neue CLAUDE.md

Rund 70 Zeilen mit drei Teilen:

1. **Identität** — der System-Absatz aus §1, auf drei bis vier Sätze gekürzt.
2. **Routing-Tabelle** — was zu lesen ist, bevor gearbeitet wird:

   | Vorhaben | Zuerst lesen |
   |---|---|
   | Neue Idee, noch keine Spec | `harness/ideas.md` |
   | Neue oder geänderte Komponente | `harness/requirements.md` + die Spec der Komponente |
   | Code schreiben | `harness/code.md` |
   | CSS, Farben, Motion | `harness/design.md` + `harness/tokens.md` |
   | Interaktion, Tastatur, A11y | `harness/uiux.md` |
   | AI-Verhalten | `harness/ai.md` |
   | Version, CHANGELOG, Release | `harness/releasing.md` |

3. **Neun Kernregeln** als Einzeiler mit Verweis auf ihre ID. Aufgenommen wird, was
   bei Verstoß sofort sichtbaren Schaden macht:

   1. Tokens statt Literale → `DESIGN-01`
   2. Beide Farbmodi, nie modus-annehmende Werte → `DESIGN-02`
   3. Frost nur, wo er sichtbar ist → `DESIGN-07`
   4. Akzente nur als Gradient, 1px-Linie, Glow oder Indikator → `DESIGN-08`
   5. Festes Motion-Vokabular, jede Komponente ist animiert → `DESIGN-10`, `DESIGN-11`
   6. `Dryl`-Präfix, typisierte Enums statt Strings → `CODE-01`, `CODE-02`
   7. Keine npm/JS-Dependency → `CODE-03`
   8. Library-Änderung heißt `<Version>` bumpen + CHANGELOG-Eintrag → `REL-01`
   9. **Specs und Code sind ein Artefakt** → `SPEC-01`

Regel 9 im Wortlaut:

> **Specs and code are one artifact.** Every change to a component's behaviour or
> public API updates its spec in the same commit. A spec that no longer matches its
> code goes back to `State: Modified` — never leave it on `Implemented`. Do not write
> code for a component whose spec you have not read.

---

## 6. `harness/requirements.md`

Übersetzung und Anpassung der vorhandenen `requirements.md` (Vorlage aus einem
Fremdprojekt, deutsch, Business-App-Zuschnitt) auf eine Komponentenbibliothek.

### 6.1 Hierarchie

| Ebene | Bedeutung | Beispiel |
|---|---|---|
| `E{n}` | Komponenten-**Kategorie** | `E4 Overlays` |
| `F{n}` | Eine **Komponente**, eine Datei | `F1 DrylDialog.md` |
| `S{n}` | Nur bei zu großen Komponenten: Aspekt | `F3 DrylTable/S2 Sorting.md` |

Bei 127 Komponenten ergibt das rund 127 Feature-Dateien und eine Handvoll aufgeteilter
Ordner. Eine `.razor` entspricht genau einer Spec — das macht den Sync prüfbar.
Kandidaten für die Aufteilung sind `DrylTable`, `DrylCommandPalette` und `DrylCanvas`
(die drei größten real vorhandenen Komponenten; `DrylDataGrid` und `DrylVoiceRun`
existieren in diesem Repository nicht — korrigiert nach Task 9).

Die Kategorien orientieren sich an der bestehenden Ordnerstruktur
(`Components/Actions`, `Data`, `Feedback`, `Inputs`, `Layout`, `Navigation`,
`Surfaces`, `AI`, plus `Canvas`, `Dialogs`, `Motion`, `Theming`, `Toasts`,
`Notifications` und die Agents-Kategorien). Die genaue Kategorieliste wird in Phase B
festgelegt.

Nummerierung startet pro Ebene bei 1 und bleibt stabil; Neues wird hinten angehängt,
nie dazwischen eingeschoben.

### 6.2 Begleitdateien je Kategorie

| Vorlage | DRYL | Inhalt |
|---|---|---|
| `_Datenstruktur.md` | `_Api.md` | Geteilte Enums, Parameter-Verträge, Services der Kategorie — der Datenvertrag einer Bibliothek und das, was der 1.0-Freeze bindet |
| `_Backend.md` | `_Interop.md` | JS-Interop-Oberfläche (`dryl.js`), DI-Services, Cleanup-Pflichten |
| `_Menüstruktur.md` | entfällt | Die Navigation ist der `ComponentCatalog` der Website; dessen Pflege verlangt bereits `REL-04` |

### 6.3 Spec-Format

```markdown
# DrylDialog

## Meta
- **State:** Modified | Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylPopover.razor
              code/DRYL.Components/Components/Surfaces/DrylPopover.razor.css
              code/DRYL.Components/Components/Surfaces/PopoverPlacement.cs

## User Story
As a Blazor developer, I want …, so that …

## Description
Fachliche Beschreibung: was die Komponente leistet, wofür sie gedacht ist,
wie sie eingesetzt wird. Keine Implementierung.

## Public API
Parameter, Enums, EventCallbacks, RenderFragments — der Vertrag nach außen.

## Acceptance Criteria
- …
```

### 6.4 Das `Source`-Feld

Ergänzung gegenüber der Vorlage. Ohne dieses Feld bleibt „die Spec bildet den Code ab"
eine Behauptung. Mit ihm wird daraus eine Invariante mit zwei Richtungen, beide per
Skript prüfbar:

- Jeder in `Source` genannte Pfad existiert → keine Spec beschreibt gelöschten Code.
- Jede `Dryl*.razor` unter `code/` erscheint in genau einer Spec → keine Komponente
  ohne Spec, keine doppelt erfasst.

`Source` sitzt auf **Komponenten**ebene, nie auf Story-Ebene: bei einer Komponente
in einer Datei in ihrer `F{n} X.md`, bei einer aufgeteilten Komponente in
`F{n} X/_Component.md`. Die `S{n}`-Dateien tragen nur ihren eigenen `State`.
Sonst erschiene eine `.razor` in drei Specs und die Invariante „genau eine"
wäre gebrochen (Befund aus Task 9).

Das Feld ist zugleich der Fortschrittsmesser für Phase C: die zweite Prüfung liefert
direkt „x von 127 Komponenten abgedeckt". Das zugehörige Prüfskript entsteht in
Phase B unter `scripts/check-spec-coverage.mjs`.

### 6.5 State

`Modified | Implemented`, aus der Vorlage unverändert übernommen, samt Pflegeregeln:

- Jede inhaltliche Spec-Änderung an einer `Implemented`-Story setzt sie auf `Modified`.
- Sobald die Implementierung der Spec entspricht, wird auf `Implemented` gesetzt.
- Reine Code-Änderungen ohne Spec-Änderung (Bugfix, Refactoring) ändern den State nicht.
- Spec und Code koordiniert in einer Session geändert → direkt `Implemented`.
- Bei Unklarheit konservativ `Modified`. Drift ist der Hauptfeind.

### 6.6 Querschnittsanforderungen

Die Vorlage kennt Querschnitts-*Features* je Epic (Email-Logs). Das DRYL-Äquivalent
sind Querschnitts-*Anforderungen* je Komponente. Jede Komponenten-Spec belegt:

- Beide Farbmodi verifiziert
- Enter- und Exit-Animation vorhanden (oder die Ausnahme ausdrücklich begründet)
- Tastaturbedienbarkeit und A11y-Verhalten beschrieben
- AI-Mode-Entscheidung **explizit** — auch ein „nein" wird notiert, mit Begründung
- Sample-Seite unter `samples/`
- Eintrag im `ComponentCatalog`

### 6.7 Akzeptanzkriterien

INVEST wird inhaltlich unverändert übernommen; die Beispiele werden von
`DisplayName`/Zertifikaten auf DRYL umgeschrieben (`Variant`, `AiState`, `OnClosed`,
`DrylTooltip`).

Die Abgrenzung **UI / Backend** der Vorlage wird ersetzt durch **Verhalten /
Darstellung**: Akzeptanzkriterien beschreiben beobachtbares Verhalten. Konkrete Werte
stehen nie im Kriterium, sondern nennen den Token.

- ✅ „The border uses `--line-strong` on hover."
- ❌ „The border turns 1px #2a2a35 on hover."

Die Verb-Konventionen werden auf Bibliotheks-Vokabular übersetzt: „is visible / is
disabled", „the `OnClosed` callback fires", „the aura is removed from the surface",
„the component renders …".

### 6.8 Sprache

Englisch, wie das gesamte Harness und aus demselben Grund wie `REL-02`: Specs
beschreiben eine öffentliche Bibliothek und werden von Menschen gelesen, die kein
Deutsch sprechen.

---

## 7. `harness/ideas.md`

Übersetzung und Anpassung der vorhandenen `ideas.md`. Struktur, Phasen und Definition
of Ready werden übernommen. Vier Anpassungen:

### 7.1 Rollen

| Rolle | Wer | Aufgabe |
|---|---|---|
| **Product Owner** | Jan (DRYL) | Bringt die Idee ein, kennt das Ziel, trifft alle finalen Entscheidungen |
| **Tech Lead** | Claude | Hinterfragt kritisch, prüft gegen `harness/`, `specs/` und `code/`, schlägt 2–3 Optionen mit Empfehlung vor, führt das Ideen-Dokument |

Die Vorlage hatte Stakeholder (Anwender) und Product Owner (Claude). Hier liegt die
Produktverantwortung beim Maintainer; Claudes Beitrag ist die technische
Machbarkeitsprüfung und der kritische Widerstand, nicht die Produktentscheidung.

### 7.2 Machbarkeitsprüfung gegen das Harness

Phase 3 des Dialogs prüft in der Vorlage gegen Specs, Datenstruktur, Backend und Code.
Für DRYL kommt `harness/` als vierte, vorrangige Quelle hinzu. Braucht eine Idee

- einen neuen Token,
- eine neue Animation, Dauer oder Easing,
- einen neuen `AiState`, oder
- eine neue Runtime-Dependency,

dann ist das ein **Blocker mit Maintainer-Freigabe**, kein Detail für später. Genau
diese vier sind die „nicht erfinden"-Regeln des Systems. Der Ideen-Dialog ist der
richtige Ort, sie sichtbar zu machen — im Code entdeckt man sie zu spät.

### 7.3 Impact-Abschnitt

Statt Entitäten und Endpunkten: **Specs · Public API · Tokens & Motion · Code**.

### 7.4 Ablage und Aktivierung

Eine Datei je Idee unter `ideas/I{n} {Name}.md`, laufend nummeriert, Nummern stabil.
Das Dokument wird **während** des Dialogs gepflegt, nicht erst am Ende — eine Idee darf
über Tage reifen. States: `Draft | Ready | Adopted`.

`ideas.md` entsteht bereits in Phase A, wird aber erst in Phase B verbindlich: In
Phase A trägt die Datei `Status: not yet active`, und `CLAUDE.md` verweist auf sie
ohne Pflichtcharakter. Grund: idea-driven zu arbeiten setzt voraus, dass die
Machbarkeitsprüfung gegen `specs/` etwas vorfindet. Solange `specs/` leer ist, wäre
die Pflicht ein leeres Ritual.

---

## 8. Migration Phase A

### 8.1 Betroffene Pfadstellen

Der Umzug der beiden Bibliotheksprojekte nach `code/` bricht acht Dateien. Alle sind
im Arbeitsbaum verifiziert:

| Datei | Änderung |
|---|---|
| `DRYL.slnx` | 2 Projektpfade |
| `tests/DRYL.Components.Tests/*.csproj` | 2 ProjectReferences (`..\..\` → `..\..\code\`) |
| `.github/workflows/ci.yml` | 2 Pack-Pfade |
| `.github/workflows/publish.yml` | 2 Version-Greps, 2 Pack-Pfade |
| `scripts/check-light-sync.mjs` | CSS-Pfad (Zeile 4) |
| `README.md` | Zeilen 36, 37 — Links auf `dryl.css` / `dryl.js` |
| `CONTRIBUTING.md` | Zeile 13 — Link auf `dryl.css` |
| `DRYL.Components/PACKAGE.md` | Zeilen 34, 35 — dieselben Links |

Dazu die Umbenennungs-Links nach dem Harness-Umzug: `README.md:65`,
`CONTRIBUTING.md:14,23,51,70`, `RELEASING.md:43`, `CONVENTIONS.md:7`,
`DESIGN_TOKENS.md:430`.

**Nicht betroffen** — im Arbeitsbaum verifiziert:

- `DRYL.Components.Agents.csproj` referenziert `..\DRYL.Components\`; beide Projekte
  ziehen gemeinsam um, der relative Pfad bleibt gültig.
- `.gitignore` und `.dockerignore` nutzen tiefenunabhängige Muster (`[Bb]in/`,
  `**/obj/`). Keine Änderung.
- `.claude/commands/update-docs.md` und `.claude/skills/verify/SKILL.md` verweisen nur
  auf `samples/`, `tests/` und `_content/…` — alle unverändert. Keine Änderung.
- `scripts/validate-light-contrast.mjs` hat keine Pfadabhängigkeit (Werte inline).
- `samples/` enthält keine `.csproj`.
- `DRYL.Components.code-workspace` verweist nur auf Geschwisterordner.
- `CHANGELOG.md` nennt alte Pfade in historischen Einträgen. Diese bleiben stehen —
  ein Changelog dokumentiert den Stand zum Zeitpunkt des Release.

**Achtung, keine Fundstelle:** `_content/DRYL.Components/dryl.css` in `README.md`,
`PACKAGE.md` und der `verify`-Skill ist ein Blazor-Static-Web-Assets-Pfad. Er leitet
sich vom **Assembly-Namen** ab, nicht vom Ordner. Er darf **nicht** angefasst werden.

### 8.2 Reihenfolge

1. **Umzug.** Alle Verschiebungen mit `git mv`, damit die Historie erhalten bleibt.
2. **Pfade fixen** — die acht Stellen aus 8.1.
3. **Verifizieren.** `dotnet restore`, `dotnet build -c Release`, `dotnet test` auf
   `DRYL.slnx`, plus `node scripts/check-light-sync.mjs` und
   `node scripts/validate-light-contrast.mjs`. Erst wenn alles grün ist, geht es
   weiter. Dieser Stand ist der Rückfallpunkt.
4. **Harness bauen.** Referenzdateien verschieben und umbenennen, die vier Regeldateien
   aus der alten `CLAUDE.md` extrahieren, `releasing.md` aus `RELEASING.md` und §7
   zusammenführen, `requirements.md` und `ideas.md` übersetzen und anpassen.
5. **`CLAUDE.md` neu schreiben.**
6. **Gerüst anlegen.** `specs/` und `ideas/` mit je einer `README.md`, die auf die
   zuständige Harness-Datei verweist, plus `.gitkeep`.
7. **Abschluss.** CHANGELOG-Eintrag unter `[Unreleased]`. Kein `<Version>`-Bump —
   es wird kein Bibliothekscode geändert (heutiges §7.3, künftig `REL-03`).

### 8.3 Abgrenzung

In Phase A **nicht** enthalten:

- Inhaltliche Änderungen an den Regeln. Der Umbau formt um, er entscheidet nicht neu.
  Einzige Ausnahme ist die neue Regel `SPEC-01`.
- Änderungen an den NuGet-Paketinhalten. `PACKAGE.md` bleibt beim jeweiligen Projekt.
- Superpowers-Deaktivierung und Plan-Archivierung — das ist Phase B.
- Anlegen echter Specs — das ist Phase C.

### 8.4 Risiko

Ein Restrisiko liegt außerhalb des Repositories: GitHub-Path-Filter, Rulesets oder
Branch-Protection-Regeln, die auf `DRYL.Components/**` zeigen, sind aus dem Arbeitsbaum
nicht sichtbar. Der Maintainer prüft die Repository-Einstellungen nach dem Umzug.

Zweitens verlieren offene Pull Requests und Branches durch den Umzug ihre
Merge-Basis. Der Umzug sollte auf einem Stand ohne offene PRs erfolgen.

---

## 9. Phase B und C im Überblick

Nicht Teil dieses Designs, aber festgehalten, damit Phase A darauf hinarbeitet.

**Phase B**

- Kategorieliste festlegen und `specs/E{n} …`-Ordner mit `_Api.md`- und
  `_Interop.md`-Gerüsten anlegen
- `scripts/check-spec-coverage.mjs` schreiben (Prüfung aus 6.4)
- Superpowers projektlokal deaktivieren:
  `.claude/settings.local.json` → `enabledPlugins: { "superpowers@…": false }`.
  Bewusst nicht global — `DRYL.Website` und `DRYL.Portfolio` behalten es, bis auch
  dort ein Harness steht.
- `docs/superpowers/plans/` → `docs/archive/plans/`. Die 30+ Pläne sind die einzige
  Aufzeichnung, warum Canvas, Voice und Light-Mode so gebaut sind, und damit
  Primärquelle für Phase C.
- `.claude/commands/update-docs.md` und `.claude/skills/verify/SKILL.md` auf das neue
  Modell ziehen
- `ideas.md` scharfstellen, `CLAUDE.md`-Verweis auf verbindlich setzen

**Phase C**

Reverse-Engineering kategorieweise, gemessen an der Coverage-Prüfung aus 6.4.

Ein Hinweis, der in die Planung gehört: Eine aus Code rekonstruierte Spec beschreibt
den Code — einschließlich seiner Fehler. Der Wert entsteht erst durch die Frage „ist
das so gewollt?" bei jeder einzelnen Spec. Phase C wird Bugs und
API-Inkonsistenzen aufdecken. Das ist kein Nebeneffekt, sondern der eigentliche
Ertrag. Gefundene Abweichungen werden als Idee unter `ideas/` erfasst, nicht
stillschweigend in die Spec geschrieben.
