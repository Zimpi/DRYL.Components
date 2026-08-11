# update-docs

Update CHANGELOG.md and README.md after a change to the library.

## What to do

1. Open `CHANGELOG.md` and add the change under `[Unreleased]`.
   - New component or feature → `### Added`
   - Behaviour change without breaking the API → `### Changed`
   - Bug fix, visual regression, accessibility issue → `### Fixed`
   - Something deleted → `### Removed`
   - Still works but will be removed in a future MAJOR → `### Deprecated`

2. Open `README.md` and update the table in the **"What's in the box (today)"** section:
   - New component → add a row: `| \`DrylName\` | Category | ✅ or — | ✅ Done | short description (≤ 12 words) |`
   - Existing component changed → update the notes column if the change is user-visible
   - Component removed → delete the row

3. **Do not touch:**
   - `<Version>` in `DRYL.Components.csproj` — the maintainer sets this
   - Other rows in the README table — only update the affected component

## Entry format in CHANGELOG.md

```markdown
### Added
- `DrylName` — Short description; variants: X / Y / Z; AI-Mode (if applicable)

### Fixed
- `DrylCard` — Cursor spotlight not rendered under certain Safari versions
```

## What does NOT need a changelog entry

- Internal refactoring with no visible effect
- Changes to demo pages in `DRYL.Website` only
- Typo fixes in comments or XML doc strings
- CI/build configuration

## Checklist

- [ ] `CHANGELOG.md` — entry added under `[Unreleased]` with the correct sub-heading
- [ ] `README.md` — table row added / updated if public API is affected
