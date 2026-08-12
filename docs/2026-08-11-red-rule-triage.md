# Triage der roten Regeln

**Datum:** 2026-08-11
**Anlass:** Phase A hat fünf Regeln mit bestehenden Verstößen dokumentiert. Vor Phase B
ist je Treffer zu entscheiden: ist die **Regel zu breit** oder der **Code schuldig**?
**Kein Code wurde geändert.** Regelkorrekturen sind angewendet, Code-Schulden sind hier
gelistet und gehören in Phase C zur jeweiligen Komponenten-Spec.

## Ergebnis in einer Zeile

Von 49 Treffern sind **25 Regel-Zuschnittfehler** und **24 echte Schulden**. Ein Harness,
dessen rote Checks zur Hälfte aus zu breit formulierten Regeln bestehen, erzieht dazu,
rote Checks zu ignorieren — deshalb steht diese Runde vor allem anderen.

| Regel | Treffer | Regel zu breit | Echte Schuld | Verbleibend rot |
|---|---:|---:|---:|---:|
| `DESIGN-01` Tokens statt Literale | 2 | 2 | 0 | **0 — grün** |
| `DESIGN-07` Frost nur wo sichtbar | 6 | 0 | 6 | 6 |
| `DESIGN-10` Motion-Vokabular | 31 | 22 | 9 | 9 |
| `CODE-01` `Dryl`-Präfix | 2 | 1 | 1 | 1 |
| `AI-03` Parameter heißt `Ai` | 8 | — | — | 8, offene Produktfrage |

---

## 1. `DESIGN-01` — Regel zu breit, beide Treffer entfallen

```
DrylSpinner.razor.css   mask: radial-gradient(…, transparent …, #fff …)
                        -webkit-mask: (dieselbe Zeile)
```

Das `#fff` in einer `mask` ist **keine Farbe**. Eine Maske wertet nur den Alphakanal
aus; `#fff` bedeutet „diesen Bereich behalten", `transparent` bedeutet „ausstanzen".
Der Wert ist eine Schablone, keine Gestaltungsentscheidung, und es gibt keinen Token,
der ihn ersetzen könnte oder sollte. In beiden Farbmodi ist er identisch.

**Regelkorrektur:** `DESIGN-01` nimmt Alphakanal-Kontexte aus — `mask`, `-webkit-mask`,
`mask-image` und `clip-path`. Dort sind `#fff`, `#000` und `transparent` strukturelle
Werte.

## 2. `DESIGN-07` — Regel korrekt, alle sechs Treffer sind echte Schuld

Der entscheidende Kontext steht in `dryl.css`: **`--glass-fx-flow` ist auf `none`
gesetzt.** Die auskommentierte Zeile daneben zeigt den früheren Wert
(`blur(var(--glass-blur)) saturate(140%)`). Frost im Fluss wurde bewusst abgeschaltet,
weil er dort unsichtbar ist und trotzdem GPU kostet — genau die Messung, die
`DESIGN-06` begründet.

| Komponente | Was dort steht | Warum es ein Verstoß ist |
|---|---|---|
| `DrylChat` `.chat` | `blur(var(--glass-blur))` | Fläche im Fluss. Umgeht `--glass-fx-flow: none` und zahlt 18px Blur für nichts. |
| `DrylValidationSummary` | `blur(var(--glass-blur))` | Alert im Fluss. Dasselbe. |
| `DrylReconnectModal` `.reconnect-overlay` | `blur(6px) saturate(120%)` | `position: fixed; inset: 0` — schwebend, gehört auf `var(--glass-fx-float)` (`blur(24px) saturate(160%)`). Stattdessen hartkodiert **und** ein anderer Look als jede andere schwebende Fläche. |

Die ersten beiden benutzen zwar einen Token, aber den falschen Mechanismus: sie
komponieren `backdrop-filter` selbst, statt die Budgetentscheidung über
`--glass-fx-flow` zu konsumieren. Deshalb hat die Abschaltung sie nicht erreicht.

Das ist die wertvollste Fundstelle der ganzen Runde: eine bewusste
Performance-Entscheidung, die an drei Stellen wirkungslos blieb, ohne dass es
jemandem auffiel.

**Keine Regeländerung.** Drei Komponenten, in Phase C mit ihrer Spec zu beheben.

## 3. `DESIGN-10` — Regel zu breit für Ambient-Animation, 9 echte Schulden bleiben

`DESIGN-10` verlangt drei Dauern (140/240/420 ms), drei Easings, kein `linear`, nichts
unter 100 ms oder über 600 ms. Diese Regel beschreibt **Übergänge**. Auf
Dauerbewegungen angewendet verbietet sie deren korrekte Umsetzung:

- Ein Spinner braucht `linear`. Ein Rotieren mit `--ease-out` stockt bei jeder Umdrehung.
- Ein Skeleton-Shimmer bei 420 ms ist ein Stroboskop, keine Wartezustandsanzeige.
- Das Aurora-Drift des Hintergrunds läuft über 22–34 s. Das ist der Effekt.

22 der 31 Treffer sind `infinite`-Animationen dieser Art. Die Regel hat sie nie gemeint.

**Regelkorrektur:** `DESIGN-10` gilt für Übergänge und einmalige Animationen.
Dauerbewegungen (`infinite`) bekommen einen eigenen Absatz: freie Dauer, weil ihr
Rhythmus zum Effekt gehört; `linear` ausdrücklich erlaubt und für Rotation erforderlich;
die Easing-Token bleiben Pflicht, wo überhaupt geeast wird; `prefers-reduced-motion`
gilt unverändert (`UX-06`).

**Echte Schulden, die bleiben (9):**

| Ort | Wert | Art |
|---|---|---|
| `dryl.css` `.fade-in` | `fadeIn 480ms` | Einmal-Animation, Literal statt Token |
| `dryl.css` `.stagger > *` | `rise 520ms` | dito |
| `dryl.css` Toast-Shine | `toast-shine 1300ms … 220ms` | dito, Dauer **und** Verzögerung |
| `dryl.css` Toast-Icon-Pop | `var(--dur-slow) … 120ms` | Dauer getokent, **Verzögerung** literal |
| `dryl.css` Progressbar | `transition: width 600ms` | **Übergang** mit Literal — der klarste Verstoß |
| `dryl.css` `ai-generated-lift` | `720ms` | Einmal-Animation |
| `dryl.css` `ai-aura-bloom` | `900ms` | dito |
| `dryl.css` `ai-comet-retire` | `1100ms … 800ms` | dito |
| `DrylImage` `img-sharpen` | `var(--img-blur-dur, 2000ms)` | getokent mit literalem Fallback |

Sechs davon liegen über 600 ms und wären auch mit Token regelwidrig — die AI-Aura- und
Toast-Choreografien brauchen offenbar eine vierte Dauer. Das ist eine
Maintainer-Entscheidung nach `DESIGN-03`: entweder ein neuer Token (etwa
`--dur-choreo`), oder die Choreografien werden auf `--dur-slow` gekürzt. **Nicht
nebenbei zu entscheiden.**

Zusätzlich nutzen sechs Ambient-Animationen ein bloßes `ease-in-out` statt
`var(--ease-in-out)` (`drift-a/b/c`, `shimmer`, `skel`). Das bleibt auch nach der
Korrektur ein Verstoß und ist billig zu beheben.

## 4. `CODE-01` — Regel zu breit, aber nur ein Treffer entfällt

```
code/DRYL.Components/Canvas/CanvasNodeView.razor
code/DRYL.Components/Components/Data/Charts/Internal/ChartFrame.razor
```

Beides sind keine öffentlichen Komponenten. `ChartFrame` liegt unter `Internal/`.
`CanvasNodeView` nimmt ausschließlich `internal` Cascading Parameters (`CanvasContext`,
`CanvasFormScope`) und wird nur von `DrylCanvas` gerendert — ein Konsument kann es weder
platzieren noch parametrisieren.

Die Regel meinte „jede **öffentliche** Komponente trägt das `Dryl`-Präfix". Ein
Präfix ist Namensraum-Hygiene für fremden Code; auf internen Bausteinen erzeugt es nur
Rauschen und verschleiert die Grenze zwischen öffentlich und intern.

**Regelkorrektur:** `CODE-01` gilt für öffentliche Komponenten. Interne Bausteine
tragen bewusst **kein** `Dryl`-Präfix; das Fehlen ist das Signal.

**Aber die Regel wird dadurch nicht grün.** Ein Prüfkommando kann „intern" nur am
Ordner erkennen. `ChartFrame` liegt unter `Internal/` und entfällt damit;
`CanvasNodeView` ist genauso intern, liegt aber nicht dort — für das Kommando
ununterscheidbar von einer vergessenen öffentlichen Komponente. Der Check bleibt bei
**1 Treffer**. Die Korrektur ist ein Verschieben nach `Canvas/Internal/`, also eine
Code-Änderung für Phase C.

Das ist der ehrliche Ausgang, und er ist die Regel wert: die Konvention „intern lebt
unter `Internal/`" macht die Grenze prüfbar, statt sie der Erinnerung zu überlassen.

## 5. `AI-03` — Produktentscheidung, nicht triagierbar

Acht Abweichungen: sieben nur im Parameternamen (`State`/`SettleTo`, Default korrekt
`AiState.None`), `DrylAiIndicator` zusätzlich im Default (`AiState.Active`).

Das ist keine Frage von „Regel oder Code falsch", sondern ob AI-native Komponenten von
`AI-03` ausgenommen sind — und wenn ja, was „AI-native" bindend heißt. Nach 1.0 ist ein
Umbenennen ein MAJOR-Bruch (`REL-01`).

**Muss vor Phase C fallen**, weil jede Spec die Parameternamen ihrer Komponente
dokumentiert. Erfasst als `ideas/I1 AI-Parameter naming for AI-native components.md`,
State `Draft`. Die Regel bleibt bis zur Entscheidung unverändert.

---

## Was daraus folgt

**Vor Phase B, entschieden und angewendet:** die Zuschnitt-Korrekturen an `DESIGN-01`,
`DESIGN-10` und `CODE-01`. Zwei Regeln werden dadurch grün, ohne dass eine Zeile Code
angefasst wurde.

**Vor Phase C, vom Maintainer zu entscheiden:**
1. `AI-03` — Ausnahme für AI-native Komponenten oder Umbenennung (→ `ideas/I1`)
2. Eine vierte Dauer für Choreografien (`--dur-choreo`) oder Kürzung auf `--dur-slow`

**In Phase C, je Komponente mit ihrer Spec:** die 6 `DESIGN-07`-Treffer und die 9
`DESIGN-10`-Schulden. Eine Spec, die gegen verletzenden Code geschrieben wird, schreibt
den Verstoß fest — deshalb gehören diese Korrekturen in dieselbe Sitzung wie die Spec,
nicht davor und nicht danach.
