# DrylCalendar stage two — implementation plan and progress

Scope: stage two ("edit") of `ideas/I19 A scheduling calendar.md`, as specified
in `specs/E5 Data/F22 DrylCalendar/` (`S5`–`S8`, the stage-two rows of
`_Component.md`, the stage-two types in `../_Api.md`, the editing bridge in
`../_Interop.md`). Branch: `feat/calendar-stage-2`. Stage three, `AiState.Proposed`
(I20) and every token or motion value not already in `dryl.css` are outside this
delivery.

Release: `DRYL.Components` 3.5.1 → **3.6.0** (new feature, MINOR — `REL-01`).
The bump happens in T1, the first commit that touches library code; every later
task adds its entry to the same unpublished `[3.6.0]` block and leaves
`<Version>` alone. The Agents package is not touched.

## Progress

| Task | State | Commit / evidence |
|---|---|---|
| T0 — spec | Complete | `S5`–`S8`, PO-confirmed 2026-10-16 (I19 decisions). |
| T1 — types and version | Complete | Gate passes (1546 tests, 3 new in `CalendarTypesTests`; JS 149); coverage reports no missing `Source` path. S7 corrected: announcements get their own visually hidden live region, as the range title is visible. |
| T2 — conflict engine and edit arithmetic | Complete | Gate passes (1578 tests, 32 new in `CalendarEditingTests`). The arithmetic lives in a new `Internal/CalendarEditing.cs` beside `CalendarLayout.cs`, in the spec's `Source`. |
| T3 — server editing core: request, pending, conflict marks | Open | |
| T4 — keyboard editing and announcements | Open | |
| T5 — the pointer and touch bridge | Open | |
| T6 — website demo and catalog | Open | |
| T7 — review, full verification, close the loop | Open | |

One verified commit per task. T1–T5 all touch `DrylCalendar.razor` or its
types and stay sequential on one working tree, with the main agent; T7's review
goes to an independent agent.

The per-task check, **the gate**:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release --no-build
node --test tests/js/*.test.mjs
node scripts/check-motion-tokens.mjs
git diff --check
```

## Tasks

**T1 — types and version.** New `CalendarChangeKind.cs`, `CalendarConflictKind.cs`
beside the component; `Models/CalendarChangeRequest.cs`,
`Models/CalendarConflict.cs`; `CalendarEvent.ReadOnly`; the nine new
`CalendarLabels` members. `<Version>` 3.6.0, `[3.6.0]` changelog block, the new
files in `_Component.md` `Source`. Tests in `CalendarTypesTests.cs`.
Verification: the gate; coverage reports no missing `Source` path.

**T2 — conflict engine and edit arithmetic.** In `Internal/CalendarLayout.cs`:
`Conflicts(...)` (the three kinds of `S8`) and the pure step arithmetic every
gesture and key shares — move by minutes and days, resize an edge, create a span
— each clamped to the grid and the visible range (`S6` "Moving", "Resizing").
Tests in `CalendarLayoutTests.cs`. Verification: the gate.

**T3 — server editing core.** `Editable`, `OnEventChangeRequested`; building the
request from a completed change (`S5` "The request", "A new event"); the pending
state that draws the changed event at its proposed place while the host's task
runs; applied / not applied, the `NotApplied` announcement and the returning
transition; conflict marks and the conflict phrases in accessible names (`S8`);
resize handles in the markup; `ReadOnly`. Tests in `DrylCalendarTests.cs`.
Verification: the gate; looked at in the browser.

**T4 — keyboard editing.** The grid role with one roving cell (a virtual grid —
only the active cell is in the markup), creating by keyboard, moving and resizing
a focused event, hints, announcements in a visually hidden polite region, focus
after a change (`S7`). Tests in `DrylCalendarTests.cs`. Verification: the gate;
driven by keyboard in the browser.

**T5 — pointer and touch bridge.** `dryl.calendar.edit` in `dryl.js`: one
delegated `pointerdown`, the preview, snapping, click-versus-drag, the touch hold,
edge scrolling, the conflict check in the browser, one report on completion
(`S6`, `S8` "while editing"); `[JSInvokable]` entry on the component; attach and
detach with `Editable`; cleanup (`CODE-05`). Tests in `tests/js/calendar.test.mjs`
for the pure parts. Verification: the gate; driven by mouse and emulated touch
in the browser, both colour modes.

**T6 — website demo and catalog.** `DRYL.Website`: an editing example, the
catalog description. Verification: `dotnet test DRYL.Website.slnx`.

**T7 — close the loop.** Independent review; findings fixed or recorded as gaps;
`S5`–`S8` and `_Component.md` to `Implemented` with `## Recorded gaps`; full
evidence run; pull request.

## Known limitations and decisions taken while implementing

Recorded here as they arise; T7 turns those that fall short of a criterion into
`## Recorded gaps` in the story concerned.

- **T2 — "never further out" instead of "never out".** `S6` says a move never
  leaves the range, `DayStart` or `DayEnd`. An event the host already placed
  outside one of them (starting 07:00 on an 08:00 grid, a bar continuing from
  last week) could then not be moved at all. `CalendarEditing.Fits` lets a limit
  the original already broke stay broken, but never grow.
- **T2 — a change keeps the event's shape.** A timed event resized past
  twenty-four hours would jump into the all-day row; `Fits` refuses any change
  that alters the row an event is drawn in, which is also what keeps "a timed
  event never becomes an all-day event" true for resizes.
