# Motion-Token (I2) — Implementation Plan

**Datum:** 2026-08-11
**Voraussetzung:** `ideas/I2` auf `Ready`; der Ordnerschnitt aus `I3` ist umgesetzt
(`b1048e5`, `ddf1d35`, `2ccc3d7`).

**Ziel:** `DESIGN-10` von neun dokumentierten Schulden auf null bringen — und zwar
gegen einen Check, der auch die zwei Stellen sieht, die er heute verfehlt.

## Global Constraints

- **Kein Verhalten außer Timing ändert sich.** Keine Keyframe wird umgeschrieben, kein
  Selektor, kein Parameter. Nur Zeitwerte werden durch Token ersetzt.
- **`DESIGN-02`:** die drei neuen Token sind modusneutral und gehören in den geteilten
  Motion-Block, **nicht** in die LIGHT-TOKEN-SET-Kopien. `check-light-sync.mjs` muss
  grün bleiben.
- **Die Vokabel ist der Zweck.** Werte konvergieren auf `--dur-choreo`, auch wo das
  sichtbar ist. Kein fünfter Token, kein Kompromisswert.
- **Ein Commit pro Task.**

## Der Bestand — verifiziert, nicht aus I2 übernommen

Der `DESIGN-10`-Check meldet **9 Treffer**, exakt die in I2 gelisteten. Beim
Nachzählen kamen **zwei weitere echte Verstöße** dazu, die der Check verfehlt, weil
seine Regex `animation:` auf derselben Zeile wie das Literal verlangt:

| Ort | Wert | Vom Check gesehen |
|---|---|---|
| `dryl.css:2114` `tbl-row-ai-flash` | `1600ms` | **nein** — mehrzeilige Deklaration |
| `dryl.css:4291` `ai-comet-retire` | `1100ms … 800ms` | **nein** — mehrzeilige Deklaration |

Elf Aufrufstellen also, nicht neun. Nur die neun zu fixen hieße: Check grün, Schuld
bleibt — genau das, wovor `CLAUDE.md` warnt.

Nicht betroffen (bewusst, siehe `I2`, Decisions vom 2026-08-11): die acht
`.stagger > *:nth-child(n)`-Offsets, die fünf `calc(var(--i) * 30ms|40ms)`-Staffelungen
und die `0ms`-Werte. Staffelschritte kommen aus einem Schritt-Token
(`--reveal-step: 60ms`), nicht aus einem Delay-Token.

---

## Task 1 — Die drei Token anlegen

**Files:** `code/DRYL.Components/wwwroot/dryl.css`

In den `/* Motion */`-Block, hinter `--dur-slow` und vor `--reveal-step`:

```css
  --dur-choreo:  900ms;  /* multi-step one-shot choreography; not for transitions */
  --delay-short: 200ms;  /* a beat's offset, so two things do not land at once */
  --delay-long:  800ms;  /* a hold before something retires itself */
```

**Verification:**

```bash
node scripts/check-light-sync.mjs        # neue Token sind modusneutral -> weiter in sync
rg -n 'dur-choreo|delay-short|delay-long' code/DRYL.Components/wwwroot/dryl.css
```

Erwartet: `LIGHT-TOKEN-SET copies are in sync.`; die drei Token existieren genau
einmal, im geteilten Block.

## Task 2 — Die elf Aufrufstellen retokenisieren

**Files:** `code/DRYL.Components/wwwroot/dryl.css`,
`code/DRYL.Components/Components/Data/DrylImage.razor.css`

| Zeile | Heute | Wird |
|---|---|---|
| `dryl.css:1168` `.fade-in` | `fadeIn 480ms` | `var(--dur-slow)` |
| `dryl.css:1171` `.stagger > *` | `rise 520ms` | `var(--dur-slow)` |
| `dryl.css:2114` `tbl-row-ai-flash` | `1600ms` | `var(--dur-choreo)` |
| `dryl.css:3143` Toast-Shine | `1300ms … 220ms` | `var(--dur-choreo) … var(--delay-short)` |
| `dryl.css:3202` Toast-Icon-Pop | `… 120ms both` | `… var(--delay-short) both` |
| `dryl.css:3466` Progress-Bar | `width 600ms` | `var(--dur-slow)` |
| `dryl.css:4247` `ai-generated-lift` | `720ms` | `var(--dur-choreo)` |
| `dryl.css:4265` `ai-aura-bloom` | `900ms` | `var(--dur-choreo)` |
| `dryl.css:4281` `ai-comet-retire` | `1100ms … 800ms` | `var(--dur-choreo) … var(--delay-long)` |
| `dryl.css:4291` `ai-comet-retire` | `1100ms … 800ms` | `var(--dur-choreo) … var(--delay-long)` |
| `DrylImage.razor.css:107` | `var(--img-blur-dur, 2000ms)` | `var(--img-blur-dur)` |

Dazu die fünf bare `ease-in-out` auf `var(--ease-in-out)`: `dryl.css` Zeilen 507, 515,
523 (`drift-a/b/c`), 1622 (`shimmer`), 3437 (`skel`). Das sind `infinite`-Animationen —
die Dauer bleibt unangetastet, nur das Easing wird tokenisiert, wie `DESIGN-10` es für
kontinuierliche Bewegung ausdrücklich verlangt.

`DrylImage`s `2000ms`-Fallback ist toter Code: `--img-blur-dur` wird inline gesetzt,
wann immer die Animation läuft (beides an `Ai == AiState.Streaming` gebunden). Die
`2000` bleibt als C#-Default des öffentlichen `BlurDuration`-Parameters bestehen — nur
der unerreichbare CSS-Fallback entfällt.

**Verification:**

```bash
rg -n 'ease-in-out' code/ -g '*.css' | rg -v 'var\(--ease'   # nur die Definition bleibt
dotnet build DRYL.slnx -c Release
dotnet test  DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
```

## Task 3 — `DESIGN-10` nachziehen und den Check reparieren

**Files:** `harness/design.md`, `harness/tokens.md`

1. Der Check wird mehrzeilig, damit eine umgebrochene Deklaration nicht länger
   durchrutscht — die Fassung wird beim Umsetzen gegen den dann sauberen Baum
   entwickelt und muss **null** Treffer liefern und die beiden bisher unsichtbaren
   Stellen erfassen (durch Gegenprobe an `git stash`).
2. Der Satz zu Delays, ausdrücklich auf das `animation`/`transition`-Shorthand
   begrenzt, mit der Ausnahme für indexmultiplizierte Staffelschritte.
3. Die `--dur-choreo`-Notiz: außerhalb der Transition-Skala, nur für mehrstufige
   Einmal-Choreografien — dieselbe Konstruktion wie bei `--ease-viscous`.
4. Die `Check:`-Zeile von „9 pre-existing hits" auf null.
5. `harness/tokens.md` dokumentiert die drei Token.

**Verification:**

```bash
node scripts/check-harness-links.mjs
# der neue Check-Befehl aus DESIGN-10, wörtlich ausgeführt -> 0 Treffer
```

## Task 4 — Release

**Files:** `code/DRYL.Components/DRYL.Components.csproj`, `CHANGELOG.md`,
`ideas/I2 …md`

**MINOR** → `2.21.0`: drei neue Token sind konsumentensichtbare Oberfläche, die
`theming.md` zum Überschreiben freigibt (`REL-01`). Changelog-Release-Cut nach
`REL-02`, mit `Added` für die Token und `Changed` für die sichtbaren Timing-Änderungen.
`I2` bekommt seinen Umsetzungseintrag und bleibt auf `Ready` — `Adopted` verlangt nach
`IDEA-07` verlinkte Specs aus Phase C.

**Verification:** der volle Satz aus `CLAUDE.md` plus die Browser-Prüfung.

---

## Die Browser-Prüfung (aus `I2`, Decisions)

Fünf Stellen ändern ihr Timing sichtbar und werden vor Task 4 **in beiden Farbmodi**
angesehen:

| Was | Änderung |
|---|---|
| `tbl-row-ai-flash` | 700 ms kürzer — die größte Einzeländerung |
| Toast-Shine | 400 ms kürzer, der Sweep zieht an |
| Progress-Bar | 180 ms kürzer |
| `ai-generated-lift` | 180 ms **länger** |
| `ai-comet-retire` | 200 ms kürzer |

Unauffällig und ungeprüft bleiben: `.fade-in` (60 ms), `.stagger` (100 ms), der
Toast-Icon-Pop (80 ms später), `ai-aura-bloom` (unverändert) und die fünf Easing-Kurven.

## Was dieser Plan nicht tut

- **Keine Spec.** Acht der neun Schulden liegen in `dryl.css`, das zu keiner Komponente
  gehört. `CLAUDE.md` verlangt eine Spec vor der Implementierung — hier gibt es keine,
  gegen die man schreiben könnte, und `specs/E1 Foundation/_Api.md` hält ihren
  Token-Abschnitt ausdrücklich für Phase C zurück. Der Abschnitt wird dort geschrieben,
  wenn Phase C `E1` erreicht; bis dahin ist `harness/tokens.md` die Quelle.
- **Keine `DESIGN-07`-Frost-Arbeit.** Andere Regel, echte Komponentenarbeit, Phase C.
- **Keine Änderung an `--dur-fast/med/slow`, den drei Easings oder `--ease-viscous`.**
