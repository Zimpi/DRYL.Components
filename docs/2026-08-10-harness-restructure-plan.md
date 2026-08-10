# Harness-Umbau Phase A — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Das Repository in `code/ · harness/ · specs/ · ideas/ · tests/ · docs/` umbauen und aus der 275-Zeilen-`CLAUDE.md` ein härtbares Regelwerk mit stabilen Regel-IDs machen.

**Architecture:** Zuerst der mechanische Umzug nach `code/` bis zu einem grünen Build — das ist der Rückfallpunkt. Danach der Harness-Aufbau: Referenzdateien verschieben, die Regeln aus `CLAUDE.md` in vier Themendateien mit IDs extrahieren, `requirements.md` und `ideas.md` aus der Fremdvorlage übersetzen und auf DRYL anpassen, zuletzt `CLAUDE.md` als Router neu schreiben. Ein Prüfskript hält die Querverweise ab dem ersten Tag konsistent.

**Tech Stack:** .NET 8/9/10, Blazor, MSBuild (`DRYL.slnx`), Node (ESM-Skripte unter `scripts/`), GitHub Actions, Markdown.

**Spec:** `docs/2026-08-10-harness-restructure.md`

## Global Constraints

- **Kein Bibliothekscode wird geändert.** Phase A verschiebt und formuliert um. Einzige neue Regel ist `SPEC-01`. Keine `.razor`, `.cs`, `.css` oder `.js` unter `code/` wird inhaltlich angefasst.
- **Kein `<Version>`-Bump.** Es wird kein shippable library code geändert — der Push nach `main` muss ein Publish-No-op bleiben (`REL-03`).
- **Alle Verschiebungen mit `git mv`.** Die Historie muss erhalten bleiben; sie ist Primärquelle für Phase C.
- **Alle Harness-Dateien auf Englisch.** Konsistent mit dem bestehenden `CLAUDE.md` und mit `REL-02`.
- **Dateinamen in `harness/` kleingeschrieben:** `code.md`, `design.md`, `uiux.md`, `ai.md`, `requirements.md`, `ideas.md`, `releasing.md`, `tokens.md`, `patterns.md`, `conventions.md`, `theming.md`.
- **`_content/DRYL.Components/…` niemals anfassen.** Das ist ein Blazor-Static-Web-Assets-Pfad, abgeleitet vom Assembly-Namen, nicht vom Ordner. Vorkommen in `README.md`, `PACKAGE.md` und `.claude/skills/verify/SKILL.md` bleiben unverändert.
- **`CHANGELOG.md` wird nicht rückwirkend umgeschrieben.** Alte Einträge nennen alte Pfade — das ist korrekt, ein Changelog dokumentiert den Stand zum Release-Zeitpunkt. Einzige Änderung ist ein neuer Eintrag unter `[Unreleased]` in Task 12.
- **Regel-IDs werden nie recycelt.** Nummernblöcke mit absichtlichen Lücken (`DESIGN-04`, `DESIGN-09` bleiben frei) trennen Themenblöcke, damit spätere Einschübe nicht umnummerieren.
- **Jede Regel trägt `Status:` (`binding` / `default` / `guidance`) und `Enforced:` (`script` / `grep` / `review`).**

---

### Task 1: Branch anlegen und Ausgangszustand sichern

**Files:**
- Keine

**Interfaces:**
- Produces: Branch `harness-restructure`, auf dem alle folgenden Tasks committen.

- [ ] **Step 1: Sicherstellen, dass der Arbeitsbaum bis auf die drei neuen Dateien sauber ist**

```bash
git status --short
```

Erwartet: nur `?? docs/2026-08-10-harness-restructure.md`, `?? docs/2026-08-10-harness-restructure-plan.md`, `?? ideas.md`, `?? requirements.md`. Falls andere Änderungen auftauchen: **stoppen** und den Maintainer fragen.

- [ ] **Step 2: Branch anlegen**

```bash
git checkout -b harness-restructure
```

- [ ] **Step 3: Spec und Plan committen**

```bash
git add docs/2026-08-10-harness-restructure.md docs/2026-08-10-harness-restructure-plan.md
git commit -m "docs: add harness restructure design and phase A plan"
```

- [ ] **Step 4: Referenz-Build zur Absicherung**

```bash
dotnet build DRYL.slnx -c Release
```

Erwartet: `Build succeeded`. Dieser Lauf beweist, dass ein späterer Fehlschlag vom Umzug kommt und nicht vorher schon bestand. Schlägt er fehl: **stoppen**, den Fehler dem Maintainer melden und nicht mit Task 2 beginnen.

---

### Task 2: Bibliotheksprojekte nach `code/` verschieben

**Files:**
- Move: `DRYL.Components/` → `code/DRYL.Components/`
- Move: `DRYL.Components.Agents/` → `code/DRYL.Components.Agents/`
- Modify: `DRYL.slnx`
- Modify: `tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj:19-20`
- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/publish.yml`
- Modify: `scripts/check-light-sync.mjs:4`
- Modify: `README.md:36-37`
- Modify: `CONTRIBUTING.md:13`
- Modify: `code/DRYL.Components/PACKAGE.md:34-35`

**Interfaces:**
- Consumes: Branch aus Task 1.
- Produces: Grüner Build unter der neuen Struktur. Alle folgenden Tasks setzen voraus, dass Bibliothekscode unter `code/` liegt.

- [ ] **Step 1: Build-Artefakte entfernen, damit sie nicht mitwandern**

```bash
rm -rf DRYL.Components/bin DRYL.Components/obj DRYL.Components.Agents/bin DRYL.Components.Agents/obj
```

- [ ] **Step 2: Ordner verschieben**

```bash
mkdir -p code
git mv DRYL.Components code/DRYL.Components
git mv DRYL.Components.Agents code/DRYL.Components.Agents
```

- [ ] **Step 3: Verschiebung prüfen**

```bash
git status --short | head -20
ls code/
```

Erwartet: `git status` zeigt die Einträge als `R` (renamed), nicht als `D` + `??`. `ls code/` zeigt `DRYL.Components` und `DRYL.Components.Agents`.

- [ ] **Step 4: `DRYL.slnx` anpassen**

Datei vollständig ersetzen durch:

```xml
<Solution>
  <Folder Name="/tests/">
    <Project Path="tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj" />
  </Folder>
  <Project Path="code/DRYL.Components/DRYL.Components.csproj" />
  <Project Path="code/DRYL.Components.Agents/DRYL.Components.Agents.csproj" />
</Solution>
```

- [ ] **Step 5: `tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj` anpassen**

Zeilen 19–20 ersetzen:

```xml
    <ProjectReference Include="..\..\code\DRYL.Components\DRYL.Components.csproj" />
    <ProjectReference Include="..\..\code\DRYL.Components.Agents\DRYL.Components.Agents.csproj" />
```

`code/DRYL.Components.Agents/DRYL.Components.Agents.csproj:42` (`..\DRYL.Components\…`) bleibt **unverändert** — beide Projekte sind gemeinsam umgezogen, der relative Pfad stimmt weiterhin.

- [ ] **Step 6: `.github/workflows/ci.yml` anpassen**

Zwei Pack-Schritte:

```yaml
      - name: Pack (validate packaging)
        run: dotnet pack code/DRYL.Components/DRYL.Components.csproj -c Release --no-build -o artifacts

      - name: Pack Agents (validate packaging)
        run: dotnet pack code/DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release --no-build -o artifacts
```

- [ ] **Step 7: `.github/workflows/publish.yml` anpassen**

Im Schritt „Read versions from csproj" die beiden `grep`-Zeilen und die beiden Fehlermeldungen:

```bash
          core=$(grep -oPm1 '(?<=<Version>)[^<]+' code/DRYL.Components/DRYL.Components.csproj)
          agents=$(grep -oPm1 '(?<=<Version>)[^<]+' code/DRYL.Components.Agents/DRYL.Components.Agents.csproj)
          if [ -z "$core" ]; then
            echo "::error::Could not read <Version> from code/DRYL.Components/DRYL.Components.csproj"
            exit 1
          fi
          if [ -z "$agents" ]; then
            echo "::error::Could not read <Version> from code/DRYL.Components.Agents/DRYL.Components.Agents.csproj"
            exit 1
          fi
```

Die beiden Pack-Schritte:

```yaml
      - name: Pack DRYL.Components
        if: env.CORE_PUBLISH == 'true'
        run: >
          dotnet pack code/DRYL.Components/DRYL.Components.csproj
          -c Release --no-build
          -o artifacts/core

      - name: Pack DRYL.Components.Agents
        if: env.AGENTS_PUBLISH == 'true'
        run: >
          dotnet pack code/DRYL.Components.Agents/DRYL.Components.Agents.csproj
          -c Release --no-build
          -o artifacts/agents
```

Im Kommentarblock am Dateianfang außerdem die beiden Pfadangaben `DRYL.Components/DRYL.Components.csproj` und `DRYL.Components.Agents/DRYL.Components.Agents.csproj` auf `code/…` ändern. Der Kommentar ist die Erklärung des Mechanismus; ein falscher Pfad darin führt die nächste Person in die Irre.

- [ ] **Step 8: `scripts/check-light-sync.mjs:4` anpassen**

```js
const css = readFileSync(new URL("../code/DRYL.Components/wwwroot/dryl.css", import.meta.url), "utf8");
```

- [ ] **Step 9: Markdown-Links auf `dryl.css` / `dryl.js` anpassen**

`README.md` Zeilen 36–37 — nur die Link-Ziele in Klammern, der Text bleibt:

```markdown
- **One token file.** Every color, spacing, radius, shadow and duration is a CSS variable in [`dryl.css`](code/DRYL.Components/wwwroot/dryl.css).
- **~90 components, zero npm dependencies.** No JS framework underneath, no third-party package — just CSS, Razor, and a single hand-written interop file ([`dryl.js`](code/DRYL.Components/wwwroot/js/dryl.js)) for the DOM-level concerns Blazor can't do alone (focus traps, portals, clipboard).
```

`CONTRIBUTING.md` Zeile 13:

```markdown
  easing references a CSS variable from [`code/DRYL.Components/wwwroot/dryl.css`](code/DRYL.Components/wwwroot/dryl.css).
```

`code/DRYL.Components/PACKAGE.md` Zeilen 34–35: dieselben zwei Links wie in `README.md`. **Achtung:** `PACKAGE.md` wird auf nuget.org gerendert, wo relative Repo-Pfade ohnehin nicht auflösen. Die Links waren schon vor dem Umzug tot. Ersetze sie deshalb hier durch absolute GitHub-URLs, statt einen weiteren toten relativen Pfad zu schreiben:

```markdown
- **One token file.** Every color, spacing, radius, shadow and duration is a CSS variable in [`dryl.css`](https://github.com/Zimpi/DRYL.Components/blob/main/code/DRYL.Components/wwwroot/dryl.css).
- **~90 components, zero npm dependencies.** No JS framework underneath, no third-party package — just CSS, Razor, and a single hand-written interop file ([`dryl.js`](https://github.com/Zimpi/DRYL.Components/blob/main/code/DRYL.Components/wwwroot/js/dryl.js)) for the DOM-level concerns Blazor can't do alone (focus traps, portals, clipboard).
```

- [ ] **Step 10: Prüfen, dass kein alter Pfad zurückblieb**

```bash
rg -n '(^|[^/])DRYL\.Components(\.Agents)?/(DRYL|wwwroot)' \
  --glob '!CHANGELOG.md' --glob '!docs/superpowers/**' --glob '!docs/2026-08-10-*' .
```

Erwartet: **keine Treffer**. Treffer in `CHANGELOG.md` und `docs/superpowers/**` sind ausgeschlossen und sollen dort bleiben (historische Einträge). Ein Treffer auf `_content/DRYL.Components/…` darf nicht auftauchen — das Muster verlangt `DRYL` oder `wwwroot` als nächstes Segment und schließt `_content` damit aus.

- [ ] **Step 11: Restore, Build, Test**

```bash
dotnet restore DRYL.slnx
dotnet build DRYL.slnx -c Release --no-restore
dotnet test DRYL.slnx -c Release --no-build --verbosity normal
```

Erwartet: alle drei erfolgreich, Testlauf ohne Fehlschläge. Bei `NU1105` oder „project not found": ein Pfad aus Step 4 oder 5 ist falsch.

- [ ] **Step 12: Beide Node-Skripte laufen lassen**

```bash
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
```

Erwartet: beide grün. `check-light-sync.mjs` beweist, dass der neue CSS-Pfad stimmt; `validate-light-contrast.mjs` hat keine Pfadabhängigkeit und dient als Gegenprobe.

- [ ] **Step 13: Pack-Schritt lokal gegenprüfen**

```bash
dotnet pack code/DRYL.Components/DRYL.Components.csproj -c Release --no-build -o /tmp/dryl-pack-check
dotnet pack code/DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release --no-build -o /tmp/dryl-pack-check
ls /tmp/dryl-pack-check
```

Erwartet: je eine `.nupkg` und `.snupkg` pro Paket. Das prüft die Pfade aus Step 6/7 lokal, bevor CI es tut.

- [ ] **Step 14: Commit**

```bash
git add -A
git commit -m "refactor: move library projects under code/

Moves DRYL.Components and DRYL.Components.Agents into code/ and updates
the solution, test project references, both workflows, check-light-sync
and the affected markdown links. No library code changed."
```

---

### Task 3: Referenzdateien nach `harness/` verschieben

**Files:**
- Move: `DESIGN_TOKENS.md` → `harness/tokens.md`
- Move: `COMPONENT_PATTERNS.md` → `harness/patterns.md`
- Move: `CONVENTIONS.md` → `harness/conventions.md`
- Move: `THEMING.md` → `harness/theming.md`
- Modify: `README.md:65`
- Modify: `CONTRIBUTING.md:14,23,51`
- Modify: `RELEASING.md:43`
- Modify: `harness/conventions.md:7`
- Modify: `harness/tokens.md:430`
- Modify: `code/DRYL.Components/PACKAGE.md:63`

**Interfaces:**
- Consumes: Struktur aus Task 2.
- Produces: `harness/` existiert mit vier Referenzdateien. Tasks 4–9 legen ihre Regeldateien daneben.

`RELEASING.md` bleibt in diesem Task noch im Root — es wird in Task 8 zu `harness/releasing.md`.

- [ ] **Step 1: Verschieben**

```bash
mkdir -p harness
git mv DESIGN_TOKENS.md harness/tokens.md
git mv COMPONENT_PATTERNS.md harness/patterns.md
git mv CONVENTIONS.md harness/conventions.md
git mv THEMING.md harness/theming.md
```

- [ ] **Step 2: Querlinks in den Root-Dateien anpassen**

`README.md:65`:

```markdown
look. Full guide: [`harness/theming.md`](harness/theming.md).
```

`CONTRIBUTING.md` Zeilen 14, 23, 51:

```markdown
  See [`harness/tokens.md`](harness/tokens.md).
```

```markdown
  must match [`harness/conventions.md`](harness/conventions.md). These are frozen at 1.0.
```

```markdown
   in [`harness/patterns.md`](harness/patterns.md).
```

`RELEASING.md:43`:

```markdown
The public API surface frozen at 1.0 is defined by [`harness/conventions.md`](harness/conventions.md).
```

- [ ] **Step 3: Querlinks innerhalb der verschobenen Dateien anpassen**

Die Dateien liegen jetzt nebeneinander in `harness/`, Verweise werden also dateiname-relativ.

`harness/conventions.md:7`:

```markdown
`patterns.md` for component structure.
```

`harness/tokens.md:430`:

```markdown
as the first child (see `patterns.md`). The raw markup it produces is:
```

- [ ] **Step 4: `code/DRYL.Components/PACKAGE.md:63` anpassen**

Auch hier eine absolute URL, aus demselben Grund wie in Task 2 Step 9:

```markdown
look. Full guide: [`theming.md`](https://github.com/Zimpi/DRYL.Components/blob/main/harness/theming.md).
```

- [ ] **Step 5: Prüfen, dass keine alten Dokumentnamen zurückblieben**

```bash
rg -n 'DESIGN_TOKENS\.md|COMPONENT_PATTERNS\.md|CONVENTIONS\.md|THEMING\.md' \
  --glob '!CHANGELOG.md' --glob '!docs/superpowers/**' --glob '!docs/2026-08-10-*' .
```

Erwartet: nur noch Treffer in `CLAUDE.md` (Zeilen 16–18, 32). Die werden in Task 10 mit der Neufassung entfernt — hier **nicht** anfassen, `CLAUDE.md` wird als Ganzes ersetzt.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: move reference docs into harness/"
```

---

### Task 4: `harness/design.md`

**Files:**
- Create: `harness/design.md`

**Interfaces:**
- Consumes: `harness/` aus Task 3; Quelltext ist `CLAUDE.md` §2.1–2.5, §2.12, §4 (im Repo noch unverändert vorhanden).
- Produces: Regel-IDs `DESIGN-01`…`DESIGN-13`, referenziert von `CLAUDE.md` (Task 10) und `harness/requirements.md` (Task 9).

- [ ] **Step 1: Datei mit diesem Kopf anlegen**

```markdown
# Design Rules

Binding visual rules for DRYL. Token reference: [`tokens.md`](tokens.md).
Component anatomy: [`patterns.md`](patterns.md). Consumer theming:
[`theming.md`](theming.md).

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. Gaps between number blocks are intentional — they leave room
for later rules without renumbering.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.
```

> **Wichtig zum Format aller folgenden Tasks:** Die Tabellen in den Steps sind die *Spezifikation*, nicht das Ausgabeformat. In der Datei wird jede Regel zu einem eigenen Abschnitt mit einer Überschrift der Form `### <ID> — <Titel>`, gefolgt von der `Status:`/`Enforced:`-Zeile, dem Regeltext und der `Check:`-Zeile. Das Prüfskript aus Task 12 erkennt eine Regel-ID **nur** an einer solchen Überschrift — eine Regel, die als Tabellenzeile abgelegt wird, gilt als nicht definiert und lässt den Check rot werden.

- [ ] **Step 2: Die Regeln aus `CLAUDE.md` übertragen — Block 01–03 (Tokens)**

Quelle: `CLAUDE.md` §2.1 und der Schlussabsatz von §1 („If a value is missing from those files, do not invent it"). Beispiel für die Zielform:

```markdown
### DESIGN-01 — Tokens, not literals

Status: **binding** | Enforced: **grep**

Every color, padding, radius, shadow, duration and easing references a CSS
variable. The full list lives in [`tokens.md`](tokens.md).

✅ `background: var(--glass-1);`
❌ `background: rgba(255,255,255,0.03);`

Check: `rg -n '#[0-9a-fA-F]{3,8}\b|rgba?\(' code/*/**/*.razor.css` returns nothing
```

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `DESIGN-01` | Tokens, not literals | binding | grep | `rg -n '#[0-9a-fA-F]{3,8}\b\|rgba?\(' code/*/**/*.razor.css` findet nichts |
| `DESIGN-02` | Two modes, one identity | binding | script | `node scripts/check-light-sync.mjs` |
| `DESIGN-03` | A missing value becomes a token, never an inline value | binding | review | Neuer Wert existiert in `dryl.css` und ist in `tokens.md` dokumentiert |

Inhalt wörtlich aus §2.1 und §2.2 übernehmen, einschließlich der ✅/❌-Paare und des Hinweises auf beide LIGHT-TOKEN-SET-Kopien. Bei `DESIGN-02` den Detailverweis auf `theming.md` ergänzen.

- [ ] **Step 3: Block 05–08 (Flächen und Farbe)**

Quelle: `CLAUDE.md` §2.3 und §2.4.

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `DESIGN-05` | Glass surfaces, not solid blocks | binding | review | Keine Kartenfläche mit deckendem Hex-Hintergrund |
| `DESIGN-06` | Frost is charged per surface | binding | review | Schwebend → `--panel-float` + `--glass-fx-float`; im Fluss → `--glass-fx-flow`; auf deckendem Grund → gar kein `backdrop-filter` |
| `DESIGN-07` | Never hand-write `backdrop-filter: blur(...)` | binding | grep | `rg -n 'backdrop-filter:\s*blur\(' code/` findet nur `dryl.css` |
| `DESIGN-08` | Accents glow, never scream | binding | review | Akzentfarbe nur als Gradient, 1px-Linie, Glow-Ring oder kleiner Indikator |

Die Messwerte aus §2.3 („measured: 0.84 of 255") mitnehmen — sie sind die Begründung dafür, dass die Regel nicht Geschmack ist.

- [ ] **Step 4: Block 10–13 (Motion)**

Quelle: `CLAUDE.md` §2.5 und §2.12.

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `DESIGN-10` | Fixed motion vocabulary | binding | grep | `rg -n 'transition:.*[0-9]+m?s\|animation:.*[0-9]+m?s' code/` nennt nur `--dur-*` / `--ease-*` |
| `DESIGN-11` | Every component is animated | binding | review | Enter/Exit, Zustandswechsel, Layout-Bewegung, Reveal — der zutreffende Teil davon |
| `DESIGN-12` | Conditional mount/unmount wraps in `DrylPresence` | binding | review | Kein `@if` um eine sichtbare Fläche ohne Presence-Wrapper |
| `DESIGN-13` | Reuse the shared motion primitives | default | review | Keine handgerollte Einmal-Animation, wo ein Primitive existiert |

Bei `DESIGN-10` die drei Dauern (140/240/420 ms), die drei Easings und die Verbote (`linear`, unter 100 ms, über 600 ms) wörtlich übernehmen. Bei `DESIGN-11` den motion.dev-Bezug und die Ausnahmeklausel („dann ausdrücklich in der PR-Beschreibung sagen") mitnehmen.

- [ ] **Step 5: Abschlussabschnitt „Does it look right?"**

`CLAUDE.md` §4 unverändert als Checkliste ans Dateiende, mit der Bewertungszeile („9/9 — ship it"). Jeden Punkt um die ID ergänzen, gegen die er prüft, z. B.:

```markdown
- [ ] Radii use `var(--r-xs|sm|md|lg|xl|pill)` — never an arbitrary px value (`DESIGN-01`)
```

- [ ] **Step 6: Die drei grep-Checks tatsächlich ausführen**

```bash
rg -n 'backdrop-filter:\s*blur\(' code/
rg -c 'var\(--dur-' code/DRYL.Components/wwwroot/dryl.css
```

Zweck: feststellen, ob die dokumentierten Checks auf dem heutigen Stand überhaupt grün sind. **Falls einer Treffer liefert:** die Regel trotzdem so dokumentieren, aber im `Check:`-Feld die Zahl der bestehenden Altfälle vermerken, z. B. „3 pre-existing hits, see Phase C". Nicht den Code reparieren — das verletzt die Global Constraint. Eine Regel, die als grün dokumentiert ist, es aber nicht ist, ist schlimmer als eine ehrlich rote.

- [ ] **Step 7: Commit**

```bash
git add harness/design.md
git commit -m "docs(harness): add design.md with DESIGN-01..13"
```

---

### Task 5: `harness/code.md`

**Files:**
- Create: `harness/code.md`

**Interfaces:**
- Consumes: Quelltext ist `CLAUDE.md` §2.6, §2.7, §2.8, §3, §5.
- Produces: Regel-IDs `CODE-01`…`CODE-05`, `CODE-20`, `CODE-21`.

- [ ] **Step 1: Dateikopf**

Denselben Kopf wie `harness/design.md` (Status-/Enforced-Erklärung), mit Verweisen auf [`patterns.md`](patterns.md) und [`conventions.md`](conventions.md).

- [ ] **Step 2: Block 01–05 (Regeln)**

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `CODE-01` | Naming: `Dryl`-prefixed PascalCase components, kebab-case CSS classes, `DRYL.Components` namespaces | binding | grep | `find code -name '*.razor' -not -name '_*' -not -path '*/obj/*' -not -path '*/bin/*' \| grep -v '/Dryl'` findet nichts |
| `CODE-02` | Strongly typed parameters: `enum` for variants, never `string`; sensible defaults | binding | review | Kein `[Parameter] public string Variant` |
| `CODE-03` | No external runtime dependencies | binding | grep | `rg -n '<PackageReference' code/*/**.csproj` — nur `Markdig` und Microsoft-Pakete |
| `CODE-04` | XML doc comment on the class and on every `[Parameter]` | binding | review | Dies ist eine Bibliothek; IntelliSense ist die Oberfläche |
| `CODE-05` | Timers and interop handles are disposed | binding | grep | `rg -n 'setTimeout\|setInterval' code/*/wwwroot/js/` hat für jeden Fall ein `clear*` |

Bei `CODE-01` die vollständige Aufstellung aus §2.6 übernehmen (Komponenten, CSS-Klassen, Dateien, Namespaces). Bei `CODE-02` das Codebeispiel mit `ButtonVariant` / `ButtonSize` mitnehmen. Bei `CODE-03` die dokumentierte Ausnahme `Markdig` (BSD-2-Clause, Maintainer-Freigabe, raw HTML deaktiviert) vollständig übernehmen — samt der Latte für künftige Dependencies: nur .NET NuGet, nie npm/JS, nur nach Freigabe.

- [ ] **Step 3: Block 20–21 (Prozess)**

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `CODE-20` | How to build a new component | default | review | Die achtstufige Checkliste aus `CLAUDE.md` §3 |
| `CODE-21` | What to clarify before writing code | default | review | Die sieben Punkte aus `CLAUDE.md` §5 |

Bei `CODE-20` Punkt 1 („Find the closest match in `examples/`") korrigieren: einen Ordner `examples/` gibt es im Repository nicht. Auf `code/DRYL.Components/Components/` verweisen und `DrylButton`, `DrylCard`, `DrylBadge` als Startpunkte nennen. Punkt 8 („Verify in the prototype") auf `prototype/DRYL Design System.html` belassen.

Bei `CODE-21` Punkt 5 (AI-Mode) um den Verweis auf `ai.md` ergänzen und Punkt 7 (Sample-Seite) mit `SPEC-05` verknüpfen.

- [ ] **Step 4: Prüfen, ob `examples/` wirklich fehlt**

```bash
ls examples 2>&1
ls code/DRYL.Components/Components/
```

Erwartet: `examples` existiert nicht; `Components/` zeigt `Actions Data Feedback Inputs Layout Navigation Surfaces AI`. Damit ist die Korrektur in Step 3 belegt.

- [ ] **Step 5: Commit**

```bash
git add harness/code.md
git commit -m "docs(harness): add code.md with CODE-01..05, CODE-20..21"
```

---

### Task 6: `harness/uiux.md`

**Files:**
- Create: `harness/uiux.md`

**Interfaces:**
- Consumes: Quelltext ist `CLAUDE.md` §2.9, §2.11 und die A11y-Klauseln aus §2.12.
- Produces: Regel-IDs `UX-01`…`UX-07`.

- [ ] **Step 1: Dateikopf**

Wie in Task 4 Step 1, ohne die Verweise auf `tokens.md`/`patterns.md`; stattdessen ein Verweis auf [`ai.md`](ai.md) für die AI-spezifischen Ansageregeln.

- [ ] **Step 2: Die sieben Regeln**

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `UX-01` | Every interactive element is keyboard-reachable | binding | review | Tab-Durchlauf in beiden Modi |
| `UX-02` | `:focus-visible` shows the accent ring | binding | grep | `rg -n 'outline:\s*none' code/` — jeder Treffer ersetzt den Ring nachweislich |
| `UX-03` | Contrast floor | binding | script | `node scripts/validate-light-contrast.mjs` |
| `UX-04` | AI activity is announced via `aria-live="polite"` | binding | review | Vorbild ist `DrylAiIndicator` |
| `UX-05` | Icon-only buttons always have a `DrylTooltip` and a matching `aria-label` | binding | review | Text im Tooltip und im `aria-label` sagen dasselbe |
| `UX-06` | `prefers-reduced-motion: reduce` is always honoured | binding | grep | `rg -c 'prefers-reduced-motion' code/DRYL.Components/wwwroot/dryl.css` > 0; eigene Komponenten-CSS spiegelt es |
| `UX-07` | Animation never changes focus order, keyboard reachability or ARIA semantics | binding | review | Bewegte Indikatoren sind `aria-hidden` |

Bei `UX-03` die konkreten Schwellen aus §2.9 übernehmen: Fließtext auf Glasflächen mindestens `var(--fg-muted)` (≈ 0,62 Alpha auf Weiß), axiale Infotexte nie unter `var(--fg-dim)`.

Bei `UX-05` beide Beispielzeilen aus §2.11 wörtlich mitnehmen (✅ mit `DrylTooltip`-Wrapper, ❌ ohne) und die Abgrenzung: ein Button mit sichtbarem Text neben dem Icon braucht keinen Tooltip.

- [ ] **Step 3: Die beiden greps ausführen**

```bash
rg -n 'outline:\s*none' code/
rg -c 'prefers-reduced-motion' code/DRYL.Components/wwwroot/dryl.css
```

Wie in Task 4 Step 6: bestehende Treffer im `Check:`-Feld als Altfälle vermerken, nicht reparieren.

- [ ] **Step 4: Commit**

```bash
git add harness/uiux.md
git commit -m "docs(harness): add uiux.md with UX-01..07"
```

---

### Task 7: `harness/ai.md`

**Files:**
- Create: `harness/ai.md`

**Interfaces:**
- Consumes: Quelltext ist `CLAUDE.md` §2.10 (sieben Bullets) und die vier AI-Zeilen aus §6.
- Produces: Regel-IDs `AI-01`…`AI-07`.

- [ ] **Step 1: Dateikopf**

Wie Task 4 Step 1, plus ein einleitender Absatz aus `CLAUDE.md` §1: AI ist ein erstklassiger Zustand der Oberfläche, ein gemeinsames visuelles Vokabular, das ohne Label lesbar ist.

- [ ] **Step 2: Die sieben Regeln**

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `AI-01` | One shared `AiState` enum (`None / Active / Thinking / Streaming / Generated`) | binding | grep | `rg -n 'enum \w*(Ai\|Loading\|Generating)\w*State' code/` findet nur `AiState` |
| `AI-02` | The visual comes from the existing primitives | binding | review | `.ai-aura`, `.ai-aura-ring`, `.ai-aura-comet`, `.ai-aura-glow`, optional `.ai-aura-wash`, `.ai-indicator` |
| `AI-03` | The opt-in parameter is always named `Ai` and defaults to `AiState.None` | binding | grep | `rg -n 'public AiState \w+' code/` — der Name ist überall `Ai`, der Default `AiState.None` |
| `AI-04` | Never invent a new AI animation, color, gradient or duration | binding | review | Neuer Wunsch → Vorschlag für `dryl.css` + Maintainer-Freigabe |
| `AI-05` | Components that cannot host AI mode do not get the parameter | binding | review | Kein `Ai` „auf Verdacht" (`DrylBadge`, `DrylToggle`) |
| `AI-06` | An aura runs only while the AI is actually working there | binding | review | `Active`/`Thinking`/`Streaming` sind Live-Zustände, nie dekorativ, nie zurückgelassen |
| `AI-07` | `Generated` is a one-shot; `AuraLifecycle` removes it | binding | review | Der Host meldet „fertig" und ist fertig |

Die Begründung aus §2.10 zu `AI-06` wörtlich mitnehmen — eine Fläche, die in einem Live-Zustand hängenbleibt, animiert für nichts.

- [ ] **Step 3: Vorhandene AI-Bausteine gegenprüfen**

```bash
ls code/DRYL.Components/Ai/
rg -n '\.ai-aura|\.ai-indicator' code/DRYL.Components/wwwroot/dryl.css | head -20
```

Zweck: die in `AI-02` genannten Klassennamen und die in `AI-07` genannte `AuraLifecycle` existieren wirklich. Weicht ein Name ab, gilt der Code — die Regel wird an ihn angepasst, nicht umgekehrt.

- [ ] **Step 4: Commit**

```bash
git add harness/ai.md
git commit -m "docs(harness): add ai.md with AI-01..07"
```

---

### Task 8: `harness/releasing.md`

**Files:**
- Move: `RELEASING.md` → `harness/releasing.md`
- Modify: `harness/releasing.md` (Regel-IDs ergänzen, §7 aus `CLAUDE.md` einarbeiten)
- Modify: `CONTRIBUTING.md:70`

**Interfaces:**
- Consumes: `RELEASING.md` (Prosa) und `CLAUDE.md` §7.0–§7.4 (Pflichten).
- Produces: Regel-IDs `REL-01`…`REL-05`, referenziert von `CLAUDE.md` (Task 10) und `harness/requirements.md` (Task 9).

- [ ] **Step 1: Verschieben**

```bash
git mv RELEASING.md harness/releasing.md
```

- [ ] **Step 2: Pfade und Links in der Datei anpassen**

Zeile 11: `**`code/DRYL.Components/DRYL.Components.csproj` → `<Version>`**`
Zeile 13: `[`Publish`](../.github/workflows/publish.yml)`
Zeile 32: `bump `<Version>` in `code/DRYL.Components/DRYL.Components.csproj``
Zeile 43: `[`conventions.md`](conventions.md)`
Zeile 59: `[`CHANGELOG.md`](../CHANGELOG.md)`
Zeile 69: `<Version>` in `code/DRYL.Components/DRYL.Components.csproj``

- [ ] **Step 3: Regelabschnitt einfügen**

Vor „Release checklist" einen Abschnitt „Rules" mit den fünf Regeln. Quelle ist `CLAUDE.md` §7.

| ID | Titel | Status | Enforced | Check |
|---|---|---|---|---|
| `REL-01` | You own the version — bump `<Version>` in the same commit as the change | binding | review | PATCH bei Fix, MINOR bei neuer Komponente/Parameter/Feature, MAJOR bei Bruch |
| `REL-02` | CHANGELOG and every consumer-facing artefact is written in English | binding | review | Gilt auch, wenn das Gespräch auf Deutsch lief |
| `REL-03` | Changes that do not touch shippable library code leave `<Version>` alone | default | review | Docs, Samples, CI, Tests — der Push ist ein No-op, und das ist richtig |
| `REL-04` | Register the component in `ComponentCatalog` (`DRYL.Website`) | binding | review | Es gibt keine Komponententabelle in `README.md` — keine anlegen |
| `REL-05` | Never publish by hand or push a `v*` tag yourself | binding | review | Der Workflow besitzt das Tagging |

`REL-02` ist die Begründung aus §7.1 wörtlich: `CHANGELOG.md` ist öffentlich, geht ins NuGet-Paket ein und wird zur GitHub-Release-Notiz.

- [ ] **Step 4: Changelog-Format aus §7.1 übernehmen**

Die Tabelle der Unterüberschriften (`Added` / `Changed` / `Deprecated` / `Removed` / `Fixed`), das Eintragsformat mit Backticks um den Komponentennamen und das Beispiel mit `DrylSpinner` / `DrylCard`. Ebenso die Liste aus §7.3, was **keinen** Eintrag braucht (internes Refactoring, reine `samples/`-Änderungen, Tippfehler in Kommentaren, CI-Konfiguration) — sie gehört unter `REL-03`.

- [ ] **Step 5: Abschluss-Checkliste aus §7.4 anhängen**

Als „Before you finish a task" ans Dateiende, ergänzt um die IDs:

```markdown
- [ ] `CHANGELOG.md` — entry under `[Unreleased]` with the correct sub-heading (`REL-02`)
- [ ] `ComponentCatalog` in `DRYL.Website` — registered or updated (`REL-04`)
- [ ] `<Version>` bumped and in lockstep with the changelog release you cut (`REL-01`)
- [ ] The component's spec updated and its `State` correct (`SPEC-01`, `SPEC-04`)
```

Die letzte Zeile ist neu — sie verankert die Spec-Pflicht dort, wo bisher die Doku-Pflicht endete.

- [ ] **Step 6: `CONTRIBUTING.md:70` anpassen**

```markdown
release flow is documented in [`harness/releasing.md`](harness/releasing.md).
```

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "docs(harness): move releasing.md into harness with REL-01..05"
```

---

### Task 9: `harness/requirements.md`

**Files:**
- Create: `harness/requirements.md`
- Delete: `requirements.md` (Vorlage im Root)

**Interfaces:**
- Consumes: `requirements.md` im Root (deutsche Fremdvorlage) als Strukturquelle; Spec §6 als Anpassungsvorgabe.
- Produces: Regel-IDs `SPEC-01`…`SPEC-08`. `SPEC-01` wird zur neunten Kernregel in `CLAUDE.md` (Task 10). Das Spec-Format aus `SPEC-03` ist die Grundlage für Phase C.

Die Vorlage stammt aus einer Business-App (Epics, BackOffice, Entitäten, Domain Events, Email-Logs). Struktur, Story-Format, State-Pflegeregeln und INVEST werden übernommen; die fachliche Ebene wird auf eine Komponentenbibliothek umgestellt. Vollständige Zuordnung: Spec §6.1–§6.8.

- [ ] **Step 1: Vorlage lesen und die Zuordnungstabelle danebenlegen**

Quellen: `requirements.md` (Root) und `docs/2026-08-10-harness-restructure.md` §6.

- [ ] **Step 2: Datei anlegen — Kopf und Hierarchie (`SPEC-02`)**

```markdown
# Spec Rules

How specs under `specs/` are structured and written. DRYL is spec-driven:
`specs/` and `code/` are one artifact, kept in sync in both directions.

Idea intake happens before any spec exists — see [`ideas.md`](ideas.md).
```

Dann `SPEC-02` mit der Ordnerstruktur:

```
specs/
├── E{n} {Category}/
│   ├── _Api.md              shared enums, parameter contracts, services
│   ├── _Interop.md          JS interop surface, DI services, cleanup duties
│   ├── F{n} {DrylComponent}.md
│   └── F{n} {DrylComponent}/        only when one file is no longer enough
│       └── S{n} {Aspect}.md
```

E = Kategorie, F = **eine Komponente = eine Datei**, S = nur bei zu großen Komponenten. Nummerierung startet je Ebene bei 1, Nummern bleiben stabil, Neues wird hinten angehängt. Kandidaten für die Aufteilung: `DrylDataGrid`, `DrylCanvas`, `DrylVoiceRun`. Die Kategorieliste wird in Phase B festgelegt — an dieser Stelle steht ausdrücklich `defined in phase B`, damit niemand rät.

`_Menüstruktur.md` der Vorlage entfällt; die Navigation ist der `ComponentCatalog` und wird bereits von `REL-04` verlangt.

- [ ] **Step 3: `SPEC-01` — die Kernregel**

```markdown
### SPEC-01 — Specs and code are one artifact

Status: **binding** | Enforced: **review**

Every change to a component's behaviour or public API updates its spec in the
same commit. A spec that no longer matches its code goes back to
`State: Modified` — never leave it on `Implemented`. Do not write code for a
component whose spec you have not read.

Check: the component's spec file was touched in the same commit as its code
```

- [ ] **Step 4: `SPEC-03` — Meta-Block mit `State` und `Source`**

```markdown
## Meta
- **State:** Modified | Implemented
- **Source:** code/DRYL.Components/Dialogs/DrylDialog.razor
              code/DRYL.Components/Dialogs/DrylDialog.razor.css
```

Begründung mitschreiben (Spec §6.4): `Source` macht aus einer Behauptung eine Invariante mit zwei Richtungen — jeder genannte Pfad existiert, und jede `Dryl*.razor` unter `code/` erscheint in genau einer Spec. Das Prüfskript `scripts/check-spec-coverage.mjs` entsteht in Phase B; bis dahin `Enforced: review`.

Das vollständige Spec-Format (H1, `## Meta`, `## User Story`, `## Description`, `## Public API`, `## Acceptance Criteria`) als Codeblock. Die Rolle in der User Story ist der konsumierende Blazor-Entwickler.

- [ ] **Step 5: `SPEC-04` — State-Pflegeregeln**

Aus der Vorlage inhaltlich unverändert, übersetzt:

- Jede inhaltliche Spec-Änderung an einer `Implemented`-Story setzt sie auf `Modified` — auch Verschärfungen einzelner Akzeptanzkriterien oder Edits an der Description.
- Sobald die Implementierung der Spec entspricht: `Implemented`, in derselben Session, in der abgeglichen wurde.
- Reine Code-Änderungen ohne Spec-Änderung (Bugfix, Refactoring, Performance) ändern den State nicht.
- Spec und Code koordiniert in einer Session geändert: direkt auf `Implemented`, ohne `Modified` als Zwischenstand.
- Bei Unklarheit konservativ `Modified`. Drift ist der Hauptfeind.

- [ ] **Step 6: `SPEC-05` — Querschnittsanforderungen je Komponente**

Ersetzt die Email-Logs-Konvention der Vorlage. Jede Komponenten-Spec belegt:

- beide Farbmodi verifiziert (`DESIGN-02`)
- Enter- und Exit-Animation vorhanden, oder die Ausnahme ausdrücklich begründet (`DESIGN-11`, `DESIGN-12`)
- Tastaturbedienbarkeit und A11y-Verhalten beschrieben (`UX-01`, `UX-05`)
- AI-Mode-Entscheidung **explizit** — auch ein „nein" wird mit Begründung notiert (`AI-05`)
- Sample-Seite unter `samples/` (`CODE-20`)
- Eintrag im `ComponentCatalog` (`REL-04`)

Der Satz aus der Vorlage, warum das kein optionales Extra ist, wird übernommen: Genau die Trennung „technisch schon da, sichtbar noch nicht" hat dort zu einer fehlenden Oberfläche geführt.

- [ ] **Step 7: `SPEC-06` — INVEST, `SPEC-07` — Verhalten statt Werte**

INVEST inhaltlich unverändert übernehmen (Independent, Negotiable, Valuable, Estimable, Small/atomar, Testable), einschließlich der Regel, dass zusammengesetzte Aussagen gesplittet werden.

Die Formulierungsbeispiele von `DisplayName`/Zertifikaten auf DRYL umschreiben:

```markdown
- ✅ "`Variant` defaults to `ButtonVariant.Primary`."
- ✅ "`Variant` accepts exactly the four values of `ButtonVariant`."
- ❌ "Variant: enum, four values, defaults to Primary." (not atomic)
- ❌ "All inputs are validated." (not testable — what is "all"?)
```

`SPEC-07` ersetzt die UI/Backend-Abgrenzung der Vorlage durch Verhalten/Darstellung:

```markdown
- ✅ "The border uses `--line-strong` on hover."
- ❌ "The border turns 1px #2a2a35 on hover."
```

Verb-Konventionen auf Bibliotheks-Vokabular übersetzen: „is visible / is disabled", „the `OnClosed` callback fires", „the aura is removed from the surface", „the component renders …", „matches the format `{…}`".

Feldnamen, Enum-Werte und Callback-Namen stehen in Backticks und in der Schreibweise aus `_Api.md`.

- [ ] **Step 8: `SPEC-08` — Sprache**

Englisch, mit derselben Begründung wie `REL-02`: Specs beschreiben eine öffentliche Bibliothek.

- [ ] **Step 9: Vorlage im Root löschen**

```bash
git rm requirements.md
```

- [ ] **Step 10: Prüfen, dass jede Vorlagen-Sektion adressiert ist**

Die Vorlage hatte acht Abschnitte: Ordnerstruktur, Story-Format, Meta-Block & State, Querschnitts-Features, INVEST, Formulierungs-Muster, Abgrenzung UI/Backend, Felder und Entitäten. Jeder muss in `SPEC-02`…`SPEC-08` wiederzufinden sein — entweder übernommen oder mit Begründung ersetzt. Fehlt einer: nachtragen.

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "docs(harness): add requirements.md with SPEC-01..08

Adapts the spec conventions from a business-app template to a component
library: epics become component categories, features become single
components, _Datenstruktur/_Backend become _Api/_Interop. Adds the Source
field so spec/code sync is mechanically checkable."
```

---

### Task 10: `harness/ideas.md`

**Files:**
- Create: `harness/ideas.md`
- Delete: `ideas.md` (Vorlage im Root)

**Interfaces:**
- Consumes: `ideas.md` im Root (deutsche Fremdvorlage); Spec §7 als Anpassungsvorgabe; `harness/requirements.md` aus Task 9 für die Verlinkung.
- Produces: Regel-IDs `IDEA-01`…`IDEA-07`. Wird von `CLAUDE.md` (Task 11) verlinkt, aber noch **nicht** als verbindlich markiert.

- [ ] **Step 1: Datei anlegen mit Aktivierungshinweis ganz oben**

```markdown
# Idea Rules

> **Status: not yet active.** These rules take effect in phase B, once
> `specs/` holds real specs. The feasibility check in `IDEA-05` reads
> `specs/`; until there is something to read, the process would be empty
> ceremony. Until then this file documents the intended process.

How a rough feature idea becomes a mature, documented idea — **before** any
spec or code exists. Once an idea is `Ready`, it is carried over into specs
following [`requirements.md`](requirements.md).
```

- [ ] **Step 2: `IDEA-01` — Geltungsbereich, `IDEA-02` — Rollen**

`IDEA-01`: Der Prozess gilt immer, wenn eine neue Idee, ein neues Feature oder eine größere Änderung aufkommt, die noch nicht als Spec existiert. Der Tech Lead beginnt dann **nicht** mit der Implementierung und schreibt **keine** Specs, sondern den Ideen-Dialog.

`IDEA-02` — die Rollentabelle. Die Vorlage hatte Stakeholder (Anwender) und Product Owner (Claude); hier liegt die Produktverantwortung beim Maintainer:

```markdown
| Role | Who | Responsibility |
|---|---|---|
| **Product Owner** | Jan (DRYL) | Brings the idea, knows the goal, makes every final decision |
| **Tech Lead** | Claude | Challenges the idea, checks feasibility against `harness/`, `specs/` and `code/`, proposes 2–3 options with a recommendation, maintains the idea document |
```

- [ ] **Step 3: `IDEA-03` — Dialogregeln, `IDEA-04` — Phasen**

`IDEA-03` aus der Vorlage: aktiv nachfragen (höchstens ca. drei Fragen je Runde, damit ein Gespräch entsteht und kein Fragebogen), kritisch bleiben, fachlich vor technisch, jede Entscheidung im Dokument protokollieren, bei offenen Punkten selbst 2–3 Optionen mit Vor-/Nachteilen und Empfehlung vorschlagen. Der Satz „nickt die Idee niemals einfach ab" wird übernommen.

`IDEA-04` — die fünf Phasen: Verstehen, Hinterfragen, Machbarkeit prüfen, Verfeinern, Abschließen.

- [ ] **Step 4: `IDEA-05` — die Harness-Machbarkeitsprüfung**

Das ist die DRYL-spezifische Erweiterung. Phase 3 prüft gegen vier Quellen:

```markdown
- **Harness** — does the idea require a new token, a new animation/duration/easing,
  a new `AiState`, or a new runtime dependency? Each of these is a **blocker
  requiring maintainer sign-off** (`DESIGN-01`, `DESIGN-03`, `DESIGN-10`,
  `AI-04`, `CODE-03`), not a detail for later.
- **Specs** — does it fit the existing categories and components under `specs/`?
  Any overlap or contradiction with existing acceptance criteria?
- **Public API** — do the enums, parameters and services in the relevant
  `_Api.md` suffice? What would have to change? Post-1.0, a rename is MAJOR
  (`REL-01`).
- **Code** — is it buildable within the existing architecture under `code/`?
  Where are the touch points, where are the risks?
```

Die Begründung mitschreiben: Diese vier sind die „nicht erfinden"-Regeln des Systems. Der Ideen-Dialog ist der richtige Ort, sie sichtbar zu machen — im Code entdeckt man sie zu spät.

- [ ] **Step 5: `IDEA-06` — Definition of Ready**

Aus der Vorlage, mit der `harness/`-Ergänzung:

```markdown
- [ ] Problem and benefit are clearly stated.
- [ ] The target role is named.
- [ ] Scope is bounded: what is in, what is explicitly out.
- [ ] The desired behaviour is concrete enough to derive INVEST acceptance
      criteria from it (see `requirements.md`).
- [ ] Feasibility checked against harness, specs, public API and code; the
      impact is documented concretely.
- [ ] Every harness blocker is either resolved or has maintainer sign-off.
- [ ] No open points remain.
- [ ] The Product Owner has explicitly confirmed the final version.
```

- [ ] **Step 6: `IDEA-07` — Ablage, Format, States**

Eine Datei je Idee unter `ideas/I{n} {Name}.md`, laufend ab 1, Nummern stabil, Neues hinten angehängt. Das Dokument wird **während** des Dialogs gepflegt (`Draft`), nicht erst am Ende — eine Idee darf über Tage reifen.

Format mit dem angepassten Impact-Abschnitt:

```markdown
# {Idea Title}

## Meta
- **State:** Draft | Ready | Adopted

## Problem
## Solution Idea
## Scope
- **In scope:** …
- **Out of scope:** …

## Impact
- **Harness:** new tokens / animations / AiStates / dependencies — and their sign-off
- **Specs:** affected or new categories, components, stories (with paths)
- **Public API:** new or changed parameters, enums, events, services
- **Code:** touch points and risks under `code/`

## Decisions
- {date}: {decision, with a short reason}

## Open Points
- … (empty once State is Ready)
```

States: `Draft` (in Diskussion) · `Ready` (Definition of Ready erfüllt, PO bestätigt) · `Adopted` (nach `requirements.md` in Specs überführt; das Dokument verlinkt die entstandenen Spec-Pfade und wird nicht mehr geändert).

- [ ] **Step 7: Vorlage im Root löschen**

```bash
git rm ideas.md
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "docs(harness): add ideas.md with IDEA-01..07 (not yet active)"
```

---

### Task 11: `CLAUDE.md` neu schreiben

**Files:**
- Modify: `CLAUDE.md` (vollständig ersetzt)

**Interfaces:**
- Consumes: Alle Regel-IDs aus Tasks 4–10. Jede in `CLAUDE.md` genannte ID muss dort als Überschrift existieren.
- Produces: Den Router, auf den Task 12 prüft.

- [ ] **Step 1: Datei vollständig ersetzen**

```markdown
# Instructions for Claude (and any AI agent) — DRYL Component Library

You are helping build **DRYL**, an open-source UI component library for Blazor
Server and Blazor WebAssembly.

DRYL is **glassy, alive — and AI-native**: translucent layers on a deep-dark or
luminous-light ground, following the user's system by default, with accents that
glow instead of shouting. Every component reads from CSS variables in
`code/DRYL.Components/wwwroot/dryl.css` — never a hardcoded color, size, radius,
shadow or duration. AI is a first-class state of the UI: AI-aware components take
an `AiState` parameter that drives one shared visual vocabulary, so a user can
feel where the AI is at work without reading a label.

DRYL is **spec-driven**. `specs/` and `code/` are one artifact.

---

## How work happens here — the order is binding

Never skip a stage, never start at a later one because the work "looks small".
Each stage has a rulebook; read it when you enter the stage.

**1. Idea** → `harness/ideas.md`
A new feature, a larger change, anything not yet in `specs/` starts as a
dialogue, not as code. The Product Owner brings the idea; you challenge it,
check feasibility against `harness/`, `specs/` and `code/`, and maintain
`ideas/I{n} …md` while it matures. A new token, a new animation, a new
`AiState` or a new dependency is a **blocker needing maintainer sign-off** —
surface it here, not in the code. The idea leaves this stage as `Ready`.

**2. Spec** → `harness/requirements.md`
A `Ready` idea becomes Epics/Features/Stories under `specs/`, with a `Meta`
block naming its `State` and its `Source` files, and INVEST acceptance
criteria. Nothing is implemented from an idea document — only from a spec.

**3. Plan**
Non-trivial work gets a written implementation plan before any edit: exact
files per task, exact commands, a verification step per task, and one commit
per task. A task is the smallest unit that carries its own verification.

**4. Implementation**
Work the plan task by task. For multi-task plans, dispatch **one fresh
subagent per task**:

- Hand the subagent only its own task, the interfaces it touches, and the
  binding constraints — never the whole plan and never the session's history.
- Never run two implementation subagents in parallel on the same branch.
- After each task, a **separate** reviewer checks two things: does it match
  the spec, and is it good work. The implementer's own self-review never
  replaces this.
- Track progress in a file, not only in your head. A lost controller
  re-running finished tasks is the most expensive failure mode there is.
- Findings go back to the implementer that wrote the code. Never fix them in
  the coordinating session — those fixes skip review.

**5. Verification, then the claim** — in that order
Never report work as done, fixed or passing before running the commands that
prove it and reading their output. Evidence first, assertion second. For this
repository the evidence is: `dotnet build DRYL.slnx -c Release`,
`dotnet test DRYL.slnx -c Release`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`, and both color modes checked by eye.
If a step was skipped, say so. If tests fail, say so with the output.

**6. Close the loop** → `harness/releasing.md`
Spec `State` updated, `CHANGELOG.md` entry written, `<Version>` bumped,
`ComponentCatalog` registered.

---

## Read before you work

| What you are doing | Read first |
|---|---|
| A new idea, no spec yet | `harness/ideas.md` |
| A new or changed component | `harness/requirements.md` + that component's spec |
| Writing code | `harness/code.md` |
| CSS, color, motion | `harness/design.md` + `harness/tokens.md` |
| Interaction, keyboard, a11y | `harness/uiux.md` |
| AI behaviour | `harness/ai.md` |
| Version, changelog, release | `harness/releasing.md` |
| Component anatomy | `harness/patterns.md` |
| Public API naming (1.0 freeze) | `harness/conventions.md` |
| Consumer theming | `harness/theming.md` |

Every rule has a stable ID. Cite it when you flag a violation.

---

## The nine rules you may not break

1. **Tokens, not literals.** Every color, padding, radius, shadow, duration and
   easing references a CSS variable. → `DESIGN-01`
2. **Two modes, one identity.** Never write a mode-assuming value. A per-mode
   value becomes a token in both LIGHT-TOKEN-SET copies. → `DESIGN-02`
3. **Frost only where it can be seen.** Floating → `--glass-fx-float`. In the
   flow → `--glass-fx-flow`. On an opaque ground → none. Never hand-write
   `backdrop-filter: blur(...)`. → `DESIGN-07`
4. **Accents glow, never scream.** Gradient, 1px border, glow ring or small
   indicator — never a large filled surface. → `DESIGN-08`
5. **Fixed motion vocabulary, and everything moves.** Three durations, three
   easings, no `linear`. Every component is deliberately animated; anything that
   mounts conditionally wraps in `DrylPresence`. → `DESIGN-10`, `DESIGN-11`
6. **`Dryl`-prefixed components, typed parameters.** `enum` for variants, never
   `string`. → `CODE-01`, `CODE-02`
7. **Zero external runtime dependencies.** No npm, no JS framework. `Markdig` is
   the one approved exception. → `CODE-03`
8. **Touching library code means bumping `<Version>` and writing a changelog
   entry, in the same commit.** → `REL-01`
9. **Specs and code are one artifact.** Every change to a component's behaviour
   or public API updates its spec in the same commit. A spec that no longer
   matches its code goes back to `State: Modified` — never leave it on
   `Implemented`. Do not write code for a component whose spec you have not
   read. → `SPEC-01`

If a value, a state or a primitive you need does not exist: **do not invent it.**
Propose adding it and ask the maintainer. That is the bar for tokens
(`DESIGN-03`), motion (`DESIGN-10`), AI visuals (`AI-04`) and dependencies
(`CODE-03`) alike.

---

## Repository layout

| Path | What lives there |
|---|---|
| `code/` | The two library projects |
| `harness/` | The rules — this file routes to them |
| `specs/` | One spec per component; the contract |
| `ideas/` | Ideas in dialogue, before they become specs |
| `tests/` | bUnit tests |
| `samples/` | Demo pages, one per component |
| `prototype/` | The visual reference (`DRYL Design System.html`) |
| `scripts/` | Token and contrast checks |
| `docs/` | Screenshots, gifs, archive |
```

- [ ] **Step 2: Zeilenzahl prüfen**

```bash
wc -l CLAUDE.md
```

Erwartet: unter 150 Zeilen (vorher 275). Der Workflow-Abschnitt ist bewusst in `CLAUDE.md` und nicht in einer eigenen Harness-Datei — er muss immer im Kontext liegen, weil er die Reihenfolge aller anderen Regeln bestimmt. Deutlich über 150 heißt, dass Regeldetail zurückgesickert ist, das in eine `harness/`-Datei gehört.

Prüfe zusätzlich, dass die sechs Stufen des Workflow-Abschnitts vollständig sind und in dieser Reihenfolge stehen: Idea · Spec · Plan · Implementation · Verification · Close the loop.

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: rewrite CLAUDE.md as a router with nine core rules"
```

---

### Task 12: Konsistenzprüfung, Gerüst, Changelog

**Files:**
- Create: `scripts/check-harness-links.mjs`
- Create: `specs/README.md`
- Create: `ideas/README.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: Alle Dateien aus Tasks 3–11.
- Produces: Ein Skript, das die Querverweise des Harness ab jetzt prüft. In Phase B kommt `scripts/check-spec-coverage.mjs` daneben.

Das Skript ist der Grund, warum dieser Task existiert: Ein Regelwerk mit stabilen IDs, das auf tote Links oder erfundene IDs verweist, verliert genau die Eigenschaft, für die es gebaut wurde.

- [ ] **Step 1: Skript schreiben**

Create: `scripts/check-harness-links.mjs`

```js
// Checks the harness for two invariants:
//   1. Every relative markdown link in CLAUDE.md and harness/*.md resolves to
//      an existing file.
//   2. Every rule ID referenced anywhere (CODE-01, DESIGN-07, SPEC-03, …)
//      exists as a heading in some harness file.
// Run: node scripts/check-harness-links.mjs
import { readFileSync, readdirSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const files = [
  "CLAUDE.md",
  ...readdirSync(join(root, "harness"))
    .filter((f) => f.endsWith(".md"))
    .map((f) => join("harness", f)),
];

const ID = /\b(CODE|DESIGN|UX|AI|SPEC|IDEA|REL)-\d{2}\b/g;
const LINK = /\[[^\]]*\]\(([^)#]+)\)/g;

const defined = new Set();
const referenced = new Map(); // id -> [file, …]
const brokenLinks = [];

for (const rel of files) {
  const text = readFileSync(join(root, rel), "utf8");

  for (const line of text.split("\n")) {
    // A heading that starts with an ID defines that ID.
    const heading = line.match(/^#{2,4}\s+((CODE|DESIGN|UX|AI|SPEC|IDEA|REL)-\d{2})\b/);
    if (heading) defined.add(heading[1]);
  }

  for (const m of text.matchAll(ID)) {
    if (!referenced.has(m[0])) referenced.set(m[0], []);
    referenced.get(m[0]).push(rel);
  }

  for (const m of text.matchAll(LINK)) {
    const target = m[1];
    if (/^[a-z]+:/.test(target)) continue; // http:, mailto:, …
    const abs = resolve(root, dirname(rel), target);
    if (!existsSync(abs)) brokenLinks.push(`${rel} → ${target}`);
  }
}

const undefinedIds = [...referenced.keys()].filter((id) => !defined.has(id));
const unreferenced = [...defined].filter(
  (id) => (referenced.get(id) ?? []).length < 2,
);

let failed = false;

if (brokenLinks.length) {
  console.error(`Broken links (${brokenLinks.length}):`);
  for (const l of brokenLinks) console.error(`  ${l}`);
  failed = true;
}

if (undefinedIds.length) {
  console.error(`Referenced but never defined (${undefinedIds.length}):`);
  for (const id of undefinedIds.sort()) {
    console.error(`  ${id} — cited in ${[...new Set(referenced.get(id))].join(", ")}`);
  }
  failed = true;
}

if (unreferenced.length) {
  console.warn(`Defined but never cited elsewhere (${unreferenced.length}):`);
  for (const id of unreferenced.sort()) console.warn(`  ${id}`);
}

if (failed) process.exit(1);
console.log(`OK — ${defined.size} rule IDs, ${files.length} files, no broken links.`);
```

- [ ] **Step 2: Skript ausführen**

```bash
node scripts/check-harness-links.mjs
```

Erwartet: `OK — <n> rule IDs, <n> files, no broken links.`

Bei „Referenced but never defined": In `CLAUDE.md` oder einer Regeldatei steht eine ID, die nirgends als Überschrift existiert — entweder ein Tippfehler oder eine vergessene Regel. **Beheben, nicht das Skript lockern.** Genau dafür ist es da.

Die Warnung „Defined but never cited elsewhere" ist kein Fehlschlag. Sie zeigt Regeln, auf die keine andere Datei verweist — das ist bei einigen normal und bei anderen ein Hinweis auf eine verwaiste Regel.

- [ ] **Step 3: `specs/README.md` anlegen**

```markdown
# Specs

One spec per component. `specs/` and `code/` are one artifact — see
[`../harness/requirements.md`](../harness/requirements.md) for the structure,
the `Meta` block and the state rules, and `SPEC-01` for the sync obligation.

Empty for now. The category structure is laid out in phase B; the specs
themselves are reverse-engineered from the codebase in phase C. Design:
[`../docs/2026-08-10-harness-restructure.md`](../docs/2026-08-10-harness-restructure.md).
```

- [ ] **Step 4: `ideas/README.md` anlegen**

```markdown
# Ideas

One file per idea, `I{n} {Name}.md`, maintained while the idea is still in
dialogue. Rules: [`../harness/ideas.md`](../harness/ideas.md).

Not yet active — the process starts in phase B, once `specs/` holds real specs.
```

- [ ] **Step 5: Skript erneut ausführen**

```bash
node scripts/check-harness-links.mjs
```

Die beiden neuen READMEs liegen außerhalb von `harness/` und werden nicht geprüft, aber ihre Ziele müssen existieren. Gegenprobe von Hand:

```bash
ls harness/requirements.md harness/ideas.md docs/2026-08-10-harness-restructure.md
```

- [ ] **Step 6: CHANGELOG-Eintrag**

Unter `[Unreleased]` in `CHANGELOG.md`, Unterüberschrift `Changed` (anlegen, falls nicht vorhanden):

```markdown
### Changed
- Repository layout — Library projects moved to `code/`, the rules split out of `CLAUDE.md` into `harness/` with stable rule IDs, and `specs/` + `ideas/` added for spec-driven development. Consumers are unaffected: package IDs, assembly names and the `_content/DRYL.Components/…` asset paths are unchanged
```

**Kein `<Version>`-Bump** — kein shippable library code wurde geändert (`REL-03`).

- [ ] **Step 7: Vollständige Verifikation**

```bash
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release --no-build --verbosity normal
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
git status --short
```

Erwartet: Build und Tests grün, alle drei Skripte grün, `git status` zeigt nur die Dateien dieses Tasks. Prüfe zusätzlich, dass `<Version>` in `code/DRYL.Components/DRYL.Components.csproj` unverändert ist:

```bash
git diff main --stat -- code/DRYL.Components/DRYL.Components.csproj
```

Erwartet: keine Ausgabe außer dem Rename.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "chore: add harness link checker, specs/ and ideas/ scaffolding, changelog entry"
```

- [ ] **Step 9: Endkontrolle der Struktur**

```bash
ls
ls harness/
```

Erwartet im Root: `CHANGELOG.md CLAUDE.md CODE_OF_CONDUCT.md CONTRIBUTING.md Directory.Build.props DRYL.slnx DRYL.Components.code-workspace LICENSE README.md SECURITY.md THIRD_PARTY_NOTICES.md code docs harness ideas prototype samples scripts specs tests` — und **keine** der Dateien `DESIGN_TOKENS.md`, `COMPONENT_PATTERNS.md`, `CONVENTIONS.md`, `THEMING.md`, `RELEASING.md`, `requirements.md`, `ideas.md` mehr.

Erwartet in `harness/`: `ai.md code.md conventions.md design.md ideas.md patterns.md releasing.md requirements.md theming.md tokens.md uiux.md` — elf Dateien.

---

## Nach Abschluss

Vor dem Merge nach `main` vom Maintainer zu prüfen:

- **Repository-Einstellungen auf GitHub** — Rulesets, Branch-Protection oder Path-Filter, die auf `DRYL.Components/**` zeigen, sind aus dem Arbeitsbaum nicht sichtbar und brechen still.
- **Offene Pull Requests** — der Umzug entwertet ihre Merge-Basis. Vor dem Merge schließen oder rebasen.
- **Der erste CI-Lauf auf dem Branch** — er beweist die Workflow-Pfade aus Task 2, Steps 6 und 7. Der lokale Pack-Check ersetzt ihn nicht vollständig.

Nicht Teil dieses Plans und Gegenstand von Phase B: Kategorieliste und `specs/E{n}`-Gerüst, `scripts/check-spec-coverage.mjs`, Superpowers projektlokal deaktivieren, `docs/superpowers/` → `docs/archive/`, `ideas.md` scharfstellen.
