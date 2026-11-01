# Übergabe: Stand nach der Sitzung vom 01.11.2026

**Branch:** `release/3.16.0` — sammelt die Features der nächsten Minor-Version.
**Stand:** `<Version>` `3.16.0`, unveröffentlicht (kein `v3.16.0`-Tag, nicht
auf `origin/main`); Agents unverändert `0.25.0`. `74/131` Specs,
**1947 bUnit- und 178 JS-Tests grün**, alle Prüfskripte grün.

> `release/3.16.0` ist **nicht** nach `main` gemergt, und das ist Absicht: ein
> Push auf `main` veröffentlicht 3.16.0 (`REL-05`).

## Arbeitsweise für dieses Release

- Jede Neuerung bekommt einen eigenen Branch von `release/3.16.0`
  (`feat/…` oder `fix/…`) und wird danach dort hineingemergt.
- Neue Einträge kommen in den offenen Block `[3.16.0]` im `CHANGELOG.md`,
  nicht unter `[Unreleased]`; `<Version>` bleibt stehen (`REL-01`, `REL-02`).
- Der Einleitungssatz des Blocks wird erst im Release-Commit geschrieben.

## Was erledigt ist

| Thema | Wo |
|---|---|
| Release-Branch eröffnet, Version 3.16.0 | `97f059e` |
| Idee I34: Toasts wachsen aus ihrem Ursprung oder aus der Glocke, Rückkehr in die Inbox | `ideas/I34 A toast redesign.md` (Ready), Plan `docs/2026-11-01-i34-toast-origin-plan.md` |
| Umsetzung I34, inkl. `ToastOptions.Origin`, `ToastOptions.KeepInInbox`, `DrylButton.Element` | `c0ede80`, gemergt in `0594292` |
| Fix: Fortschrittsbalken der übrigen Toasts starteten neu (`@key` in `DrylToastProvider`) | `c0ede80`, im Browser vorher/nachher gemessen |
| Erste Specs für `DrylToast` (E11 F2) und `DrylToastProvider` (E1 F3) | `c0ede80` |

Der Prototyp mit den drei Varianten liegt als Artifact „DRYL Toast-Morphs“
vor (claude.ai, privat).

## Was offen ist

1. **Entscheidung des Product Owners:** Nur Toasts, die aus der Glocke kamen,
   kehren beim Ablaufen in die Inbox zurück (im Prototyp taten es alle). Siehe
   `## Decisions` in I34.
2. **`DRYL.Website`:** Demo und `ComponentCatalog` für Toast-Ursprung und
   Glocke nachziehen (`REL-04`) — das Repo war in dieser Sitzung nicht
   verfügbar.
3. **Kandidaten für eigene Ideen** (als Lücken in den Toast-Specs notiert):
   Fehler und Warnungen, die stehen bleiben, bis man sie schließt; doppelte
   Ansage durch verschachtelte Live-Regionen; `ToastOptions.PauseOnHover`
   wird nirgends gelesen; `linear` am Countdown-Balken (`DESIGN-10`).
4. **Flaky Test:** `DrylAiFieldTests` (`The_review_chips_and_announcement_come_from_Texts`)
   schlug einmal im Komplettlauf fehl, allein und danach immer grün — bereits
   vor jeder Änderung dieser Sitzung.
5. **Lokales Bauen:** Das Ubuntu-SDK 10.0.114 baut `DrylCommandPalette` nicht
   (`@section.Title` wird als Direktive gelesen). Mit SDK 10.0.403 (wie CI,
   `10.0.x`) baut alles.
