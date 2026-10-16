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
| T3 — server editing core: request, pending, conflict marks | Complete | Gate passes (1606 tests, 28 new in `DrylCalendarEditingTests`). Browser look deferred to T5, where the grid can first be operated. |
| T4 — keyboard editing and announcements | Complete | Gate passes (1627 tests, 21 new keyboard tests in `DrylCalendarEditingTests.Keyboard.cs`; JS 151, 2 new for `dryl.calendar.focus`). Browser pass with T5. |
| T5 — the pointer and touch bridge | Complete | Gate passes (1627 tests; JS 156, 5 new for the bridge's limits, reassignment and conflicts). Browser pass on the website demo, both colour modes — see "T5 — what was looked at". |
| T6 — website demo and catalog | Complete | `DRYL.Website` branch `feat/calendar-stage-2`: example "Editing" (day view, person columns, `CollapseBelow="Breakpoint.Sm"` so week and month show wide on the docs page), catalog description. `dotnet test DRYL.Website.slnx` passes (36). Pulled forward before T5's browser pass. |
| T7 — review, full verification, close the loop | Complete | Independent review by a separate agent, eight findings. Fixed with tests in `DrylCalendarEditingTests.Review.cs`: a synchronous host's applied keyboard change reported as refused (the settle now yields first), focus going to the exiting ghost after a keyboard move, Tab swallowed during a keyboard change (the bridge now prevents only Enter and Space), timed-bar resize limits differing between JS and C#, a late release clearing the next gesture, `is-returning` sticking, conflicts computed against different event sets. Recorded as a gap: two creates in flight. Fixes re-checked in the browser. Full evidence run on 2026-10-16: build 0 errors, 1631 tests, JS 156, light sync, contrast, harness links and motion tokens clean, coverage `65/131`. Specs to `Implemented` with `## Recorded gaps`. |

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

- **T3 — "applied" is read from the next render.** After the host's task, the
  calendar compares `Events` with the proposal at the start of the following
  render: a move or resize counts as applied when the event with `Proposed.Id`
  sits where the proposal put it; a create counts as applied when any new id has
  appeared, so a host that stores the event under its own id is not told "not
  applied".
- **T3 — only a timed event glides back within its column.** A refused move
  that changed the day or the person changes the event's container; it then
  exits from the drop place and enters at its own, through `DrylPresence`. A bar
  changes its grid placement and jumps back.
- **T3 — the conflict mark sits in every event that can conflict**, hidden
  unless the event has a conflict, so the browser can switch it during a
  gesture without a render. Its tooltip is a `DrylTooltip`; its words are in
  the event's accessible name, the mark itself is `aria-hidden`.
- **T3 — a fault in the host's handler reaches the renderer** through
  `DispatchExceptionAsync`, as the bridge's call returns to the browser, where
  it would otherwise be swallowed.

- **T4 — the slot grid is virtual.** `S7` asks for a row per slot and a cell
  per day; the time grid's markup runs in columns, not rows. The grid carries
  `aria-rowcount` and `aria-colcount`, and only the one focusable cell is in the
  markup, inside a `role="row"` with its `aria-rowindex` and `aria-colindex`.
  Events stay buttons inside the columns, outside any row.
- **T4 — the cursor's place in the tab order.** It is the first stop of the
  row it sits in: in the time grid that is after the all-day bars, in the month
  view after the earlier weeks' day numbers and events. `S7` asks for "after the
  legend and before the events".
- **T4 — a keyboard move does not know the narrow week.** The server cannot see
  the container query, so in the narrow week view `ArrowLeft` and `ArrowRight`
  move the preview to a day that is not shown. `S6` forbids the day change only
  for the pointer, where the bridge can see it.
- **T4 — the preview of a timed change that crosses midnight shows its first
  day's segment only.**

### T5 — what was looked at (Chromium, website demo "Editing", 2026-10-16)

- Day view, person columns, dark and light: drag "Workshop" one hour down — the
  preview follows slot by slot in Ada's column, the conflict mark appears on it
  (lunch gap), one request, the host applies it, the event carries the mark and
  its accessible name ends with "Outside the working hours of Ada Lovelace".
- Drag across to Bob's column: the preview sits exactly in his column
  (measured: same x and width), the request reassigns the event to Bob.
- Resize "Design review" by its end handle; click an empty slot to create one
  slot; click an event without moving — no request.
- Keyboard: arrows on "Customer call", `Enter` — one request, focus stays on the
  event, the page does not scroll; `Escape` announces "Change cancelled."; the
  cursor names its day, time and person; `Enter`, `Shift`+`ArrowDown`, `Enter`
  creates two slots and focus moves to the new event.
- "Refuse changes with conflicts" on, drag "Retro": `aria-busy` while the host
  decides, then `is-returning` and "The change was not applied."
- Emulated touch (CDP): a swipe before the hold scrolls the grid and changes
  nothing; a hold grabs the event and a move of one hour is requested as one.
- Week view (wide): a drag one column to the right moves the event a day. Month
  view: a line dragged one row down marks the 22nd and moves it a week.
- At 400px: no horizontal page scroll.

Found and fixed during the pass: the preview spanned to the grid's last column
(an absolutely placed grid item with no named end line spans to the edge — now
`N / span 1` for the preview and the cursor), and the conflict mark sat on the
avatars (an event with a conflict now keeps room for it).

Observed, not changed: in the light mode the sticky header lets events show
faintly through it when the grid is scrolled; `--panel-sticky` is 92% opaque.
This is stage one's surface and a token value (`DESIGN-03`).
