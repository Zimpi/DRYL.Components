# update-docs

Pflege CHANGELOG.md und README.md nach einer Änderung an der Bibliothek.

## Was zu tun ist

1. Öffne `CHANGELOG.md` und trage die Änderung unter `[Unreleased]` ein.
   - Neue Komponente oder Feature → `### Added`
   - Verhaltenänderung ohne Breaking Change → `### Changed`
   - Bugfix, visueller Fehler, Barrierefreiheitsproblem → `### Fixed`
   - Etwas entfernt → `### Removed`
   - Noch funktioniert aber wird demnächst entfernt → `### Deprecated`

2. Öffne `README.md` und aktualisiere die Tabelle im Abschnitt **"What's in the box (today)"**:
   - Neue Komponente → neue Zeile hinzufügen: `| \`DrylName\` | Kategorie | ✅ oder — | ✅ Done | kurze Beschreibung (≤ 12 Wörter) |`
   - Bestehende Komponente geändert → Notizen-Spalte aktualisieren, wenn die Änderung für den Nutzer sichtbar ist
   - Komponente entfernt → Zeile löschen

3. **Nicht anfassen:**
   - `<Version>` in `DRYL.Components.csproj` — das setzt der Maintainer
   - Andere Zeilen in der README-Tabelle — nur die betroffene Komponente anpassen

## Entry-Format in CHANGELOG.md

```markdown
### Added
- `DrylName` — Kurzbeschreibung; Varianten: X / Y / Z; AI-Mode (falls vorhanden)

### Fixed
- `DrylCard` — Cursor-Spotlight wurde unter bestimmten Safari-Versionen nicht gerendert
```

## Was KEIN Eintrag braucht

- Rein internes Refactoring ohne sichtbaren Effekt
- Änderungen ausschließlich an `samples/`-Demoseiten
- Tippfehler-Korrekturen in Kommentaren oder XML-Doc-Strings
- CI/Build-Konfiguration

## Checkliste

- [ ] `CHANGELOG.md` — Eintrag unter `[Unreleased]` mit korrekter Überschrift
- [ ] `README.md` — Tabellenzeile hinzugefügt / aktualisiert (falls öffentliche API betroffen)
