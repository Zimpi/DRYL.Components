# Übergabe: Stand nach der Sitzung vom 13.–17.08.2026

**Branch:** `feature/picker-escape-and-popover-specs` (beide Repos)
**Stand `DRYL.Components`:** `<Version>` `2.23.0`, unveröffentlicht (kein
`v2.23.0`-Tag, nicht auf `origin/main`), `12/127`, **1042 Tests grün**.
**Stand `DRYL.Website`:** Branch `fix/dockerfile-after-code-move`, 36 Tests grün.

> `main` ist in beiden Repos **nicht** gepusht worden, und das ist Absicht: ein
> Push auf `main` in `DRYL.Components` veröffentlicht 2.23.0 (`REL-05`). Der
> Feature-Branch ist die sichere Übergabe.

## Was erledigt ist

| Thema | Commits |
|---|---|
| `Escape` erreicht `DrylDatePicker`/`DrylTimePicker`, plus alle Folgefehler | `dda0a47`, `bdf6b9d`, `130f483`, `1a13797` |
| `aria-haspopup`/`aria-expanded` am Trigger jedes Popovers | `1816f3f`, `086e65a`, `9060780`, `392f68a` |
| `REL-01`/`REL-02`: unveröffentlichter Stapel, Datum, Checklisten | `97a653d`, `f73cadd` |
| `DrylSplitButton`: Demo-Seite + Katalog (Website) | `5e01fc7`, hier `09b42f6`, `1c20d96`, `e99a47f` |
| `.btn-lg.btn-icon` — icon-only Button in Large zeigte kein Icon | `b1296ea` |
| Race in `CanvasDataRegistry` (geteilter `NullabilityInfoContext`) | `f130160` |
| Spec `E11 Surfaces/F1 DrylPopover` | `293a21e`, `ec12a2d` |
| Website: 92 tote „View source"-Links, Test dagegen, CI-Gate | `9d81d24`, `e09e547`, `9a7c315`, `4d67593` |

Jeder Fix ist im Browser gemessen worden, vorher rot und nachher grün; die
Messungen stehen in den Plänen unter `docs/2026-08-1*`.

## Was offen ist, nach Dringlichkeit

1. **`ideas/I4` braucht deine Entscheidung** — der `DrylPopover` animiert rein,
   nicht raus (`DESIGN-12`). Der naheliegende Fix wurde gebaut, gemessen und
   verworfen (115 ms leerer Glaskasten). Zwei Routen stehen dort, beide auf der
   Freigabestufe „neue Animation". Ohne dich geht es nicht weiter.
2. **Reviewbefunde an `F1 DrylPopover.md`**, noch nicht umgesetzt:
   - Abschnitt `## Deviations (State: Implemented)` umbenennen (z. B.
     „Recorded debt") und `Deviations: none` wieder eindeutig hinschreiben —
     `F3` benutzt dieselbe Überschrift für *nicht erfüllte* Kriterien, ein
     `SPEC-04`-Reviewer müsste den Merge sonst blockieren.
   - Drei Einträge gehören unter `Recorded gaps`: der nie abgehängte
     Panel-Key-Listener, das rohe `min-width`/`1px`, „no tests of its own".
   - Sieben Sammelkriterien verletzen `SPEC-06` (Liste im Reviewbericht; u. a.
     „Tearing the portal down survives …", „fill is `--panel-float` and its
     frost is …", „paints none of the above while `Surface` is `false`").
   - `State: Implemented` ist **richtig** und bleibt — geprüft und bestätigt.
3. **`E10 Navigation/F1 DrylMenu`** fehlt noch (Task 2 des Plans
   `docs/2026-08-14-e11-e10-popover-menu-plan.md`). `F3 DrylSplitButton` verweist
   ausdrücklich darauf, dass diese Hälfte offen ist. Danach `13/127`.
4. **Drei a11y-Befunde am blanken `DrylPopover`**, in der Spec als Schuld
   festgehalten, nicht behoben: `Escape` wirkt nicht, wenn niemand ins Panel
   fokussiert hat (obwohl `CloseOnEscape` per Default `true` ist); das portierte
   Panel fällt aus der Tab-Reihenfolge; beim Schließen wird der Fokus nirgendwohin
   zurückgegeben.
5. **`dryl.timepicker.scrollToActive`** ist am verborgenen Panel wirkungslos —
   dieselbe Ursache wie der behobene Fokusfehler, eigener Fix ausstehend.
6. **Website, aus dem Review offen:** `Block` und `MenuPlacement` sind auf der
   Split-Button-Seite undemonstriert, und elf Carets sagen einem Screenreader
   identisch „More actions".
7. **Aus der Vorsession weiterhin offen und hier nicht angefasst:** 94
   `WaitForAssertion`-Aufrufe ohne Timeout, das Agents-Paket (`0.17.4`).

## Zwei Dinge, die beim Weiterarbeiten Zeit sparen

- **Laufzeit-Verifikation nicht parallelisieren.** Zwei Agenten, die beide die
  Docs-Website auf Port 5044 brauchen, bringen sich gegenseitig um. In dieser
  Sitzung zweimal passiert.
- **Doc-Kommentare sind hier keine Quelle.** Fünf nachweislich falsche oder
  irreführende sind allein in dieser Sitzung gefunden worden. Beleg am Code oder
  im Browser.
