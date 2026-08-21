# Plan: Phase C — the `E5 Data` specs

**Branch:** `spec/phase-c-e5-data` · **Base:** `60b2016`
(`2.24.3`, **published** — `v2.24.3` is tagged and on `origin/main`)

`E5 Data` is the largest unwritten category: 21 components, none of them
covered today. `specs/E5 Data/` holds nothing but the two phase-C scaffolds.
Writing it moves the coverage meter from `31/127` to `52/127`.

The work is spec-only, so no `<Version>` bump and no changelog entry belong to
it (`REL-01` binds *library code*, and a spec is not that). If reading a
component against its code turns up a defect — as the `E7` pass turned up five —
the defect is **not** fixed in this branch. It is written down under
`## Recorded gaps` in the spec that found it and collected in a follow-up plan,
so that the specs land as one reviewable artifact and the fixes as another.

## Order of work

One commit per task. Every task ends with `node scripts/check-spec-coverage.mjs`
and `node scripts/check-harness-links.mjs`; the coverage number is the evidence,
not a green exit (the check stays non-zero until `127/127`).

| # | Task | Spec files | Covers |
|---|---|---|---:|
| 1 | Category companions | `_Api.md`, `_Interop.md` | 0 |
| 2 | Avatar family | `F1 DrylAvatar`, `F2 DrylAvatarGroup` | 2 |
| 3 | The three inline marks | `F3 DrylBadge`, `F10 DrylIcon`, `F12 DrylKbd` | 3 |
| 4 | Citation family | `F4 DrylCitation`, `F5 DrylCitationList`, `F6 DrylCitationListItem` | 3 |
| 5 | Code block | `F7 DrylCodeBlock` | 1 |
| 6 | Description family | `F8 DrylDescriptionList`, `F9 DrylDescriptionItem` | 2 |
| 7 | Image | `F11 DrylImage` | 1 |
| 8 | Pagination | `F13 DrylPagination` | 1 |
| 9 | Sparkline | `F14 DrylSparkline` | 1 |
| 10 | The two number tiles | `F15 DrylStat`, `F17 DrylTableKpi` | 2 |
| 11 | Timeline family | `F18 DrylTimeline`, `F19 DrylTimelineItem` | 2 |
| 12 | Tree family | `F20 DrylTreeView`, `F21 DrylTreeNode` | 2 |
| 13 | Table | `F16 DrylTable/` (split) | 1 |
| | | **Total** | **21** |

Numbering is assigned up front and stays stable (`SPEC-02`); the tasks are
ordered by family rather than by number, so `F16` is written last.

## Why `DrylTable` is split

`SPEC-02` names it as one of the three components allowed the `F{n} {Name}/`
form, and the code agrees: `DrylTable.razor` is 2115 lines, an order of
magnitude past anything else in the category. It becomes
`F16 DrylTable/_Component.md` plus one `S{n}` per aspect — data source and
paging, sorting, filtering and search, selection, column mechanics
(pin/resize/reorder/visibility), inline editing, row reordering, and
presentation. `DrylColumn.cs` and the types under `Models/` are named in its
`Source`; `Internal/` is not, being implementation.

## What is not in scope

- **No fixes.** See above. `## Recorded gaps` is where the reading's findings
  land.
- **`E4 Charts` is a separate category** and already written, so nothing under
  `Components/Data/Charts/` is touched — `DrylSparkline` sits in `Data/`
  itself and belongs here.
- **`Internal/SyntaxHighlighter.cs`** is implementation of `DrylCodeBlock`, not
  public surface; it is described in prose, not claimed in `Source`.

## Method

Each component is read against **its code**, not its doc comments: the `E7`
pass found five doc comments that were wrong or misleading, and this category
carries the same risk (`AvatarSize` documents "24px / 28px / 40px" in its XML
doc — the tokens decide, and the spec names the token, never the value,
`SPEC-07`).

For every component the six cross-cutting points of `SPEC-05` are walked and
evidenced in the spec text, including the demo page and `ComponentCatalog`
entry, which live in the `DRYL.Website` repository next door.
