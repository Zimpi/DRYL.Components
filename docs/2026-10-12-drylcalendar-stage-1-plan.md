# DrylCalendar stage one — implementation plan and progress

Scope: stage one ("show") of `ideas/I19 A scheduling calendar.md`, as specified
in `specs/E5 Data/F22 DrylCalendar/` (`_Component.md`, `S1`–`S4`). Branch:
`feat/calendar`. Stages two and three, `AiState.Proposed` (I20) and every token
or motion value not already in `dryl.css` are outside this delivery.

Release: `DRYL.Components` 3.4.0 → **3.5.0** (new component, MINOR — `REL-01`).
The bump happens in T1, the first commit that touches library code; every later
task adds its entry to the same unpublished `[3.5.0]` block and leaves
`<Version>` alone. The Agents package is not touched.

## Progress

| Task | State | Commit / evidence |
|---|---|---|
| T0 — spec | Complete | `f67f168`; harness links pass; coverage reports only the eleven not-yet-existing `Source` paths of `F22`. |
| T1 — types and version | Complete | `bcd3420` `feat(calendar): the calendar's types (3.5.0)`; gate passes (1305 tests, 10 new in `CalendarTypesTests`); coverage's missing `Source` paths of `F22` fall from eleven to the two component files. Doc comments name `DrylCalendar` as `<c>` rather than `<see cref>` until T3 creates the type — a cref to it does not resolve yet (CS1574). No browser pass: nothing renders yet. |
| T2 — layout engine | Complete | `8460167` `feat(calendar): the layout engine`; gate passes (1394 tests, 89 new in `CalendarLayoutTests`, every row of the T2 table and every named edge case covered). Written by the main agent, not a subagent. Limitations below. |
| T3 — shell: views, toolbar, time grid | Complete | `feat(calendar): views, toolbar and the time grid`; gate passes (1445 tests, 51 new in `DrylCalendarTests`); coverage has no missing `Source` path left for `F22`. Looked at in the browser on a pulled-forward website demo (`DRYL.Website`, branch `feat/calendar`, uncommitted until T7): week view in light mode, month view in dark mode, no console warning, no horizontal page scroll at 1280px. The scroll to `InitialScrollTime` and `CollapseBelow` arrive with T6. |
| T4 — events | Open | |
| T5 — people and availability | Open | |
| T6 — motion, narrow layouts, accessibility, interop | Open | |
| T7 — website demo and catalog | Open | |
| T8 — review, full verification, close the loop | Open | |

One verified commit per task. Only the named files are staged. Update this table
as each task completes and keep evidence and known limitations in this file. The
main agent owns commits, versioning and this record. T2 is self-contained and
may go to an implementation subagent; T3–T6 all edit `DrylCalendar.razor` and
its stylesheet and stay sequential on one working tree.

The per-task check, referred to below as **the gate**:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release --no-build
node scripts/check-motion-tokens.mjs
git diff --check
```

## Known limitations and decisions taken while implementing

Recorded here as they arise; T8 turns those that fall short of a criterion into
`## Recorded gaps` in the story concerned.

- **T2 — the spec is silent, the engine decides.** A `DayStart` outside the day
  becomes midnight without a report; a `DayEnd` beyond the following midnight is
  replaced and reported like one that is not later than `DayStart`. An
  availability span whose `End` is not later than its `Start` is ignored.
- **T2 — overlap is judged on the drawn box.** A short event is drawn one slot
  high (`S2`), and packing uses that drawn height, so two five-minute events ten
  minutes apart sit side by side instead of on top of each other.
- **T2 — a month cell's capacity is a number, not a measurement.** All layout is
  server-side C#, so "as many events as fit its height" (`S2`) is a row count
  the component passes to `CalendarLayout.MonthWeek`, not the cell's rendered
  height. A day that hides anything gives its last row to the overflow control;
  a bar hidden on one of its days is hidden, and counted, on all of them.
- **T2 — a timed event wholly outside `DayStart`–`DayEnd` on every day it
  touches** (22:00–02:00 on an 08:00–18:00 grid) appears in neither column, as
  `S2` "Placement" says, and so is invisible in the day and week views.

- **T3 — the view switch has no accessible group label.** `CalendarLabels` has
  no member for it and every text must come from `Labels` (`S4` "Labels"), so
  the radiogroup is unnamed; its three radios carry `Day` / `Week` / `Month`.
- **T3 — `Date` cannot say "unset" by its value.** The parameter is a
  `DateOnly`; the calendar notes in `SetParametersAsync` whether the host
  supplied one and otherwise starts on today. A host that reads `Date` back
  without binding it sees `default`.
- **T3 — the week title drops the weekday from the culture's long date pattern**
  by pattern surgery (`October 12 – October 18, 2026`,
  `12. Oktober – 18. Oktober 2026`); a culture whose pattern is nothing but a
  weekday falls back to the short date.

## Decisions carried from the spec

- `DrylCalendar` is not generic; a host's own object rides in
  `CalendarEvent.Data`.
- `End` is exclusive everywhere; all-day events count dates only.
- All layout arithmetic is server-side C#. JS does one thing: the initial
  scroll.
- No new token. Event colours draw from `--chart-1`…`--chart-6`; muted areas
  from `--line-soft`. **If either does not hold up by eye in both modes, stop
  and ask the maintainer (`DESIGN-03`) — do not add or inline a value.**
- No `Ai` / `Aura` parameter in this stage (`AI-05`, recorded in `S4`).

## T1 — types and version

Files:

- `code/DRYL.Components/Components/Data/CalendarView.cs`,
  `CalendarPersonLayout.cs`, `CalendarColor.cs`, `CalendarAvailabilityKind.cs`.
- `code/DRYL.Components/Components/Data/Models/CalendarEvent.cs`,
  `CalendarPerson.cs`, `CalendarAvailability.cs`, `CalendarRange.cs`,
  `CalendarLabels.cs`.
- `code/DRYL.Components/DRYL.Components.csproj` (`<Version>` 3.5.0).
- `CHANGELOG.md` (cut `## [3.5.0] — <date>` with an `Added` entry for
  `DrylCalendar`; fresh empty `[Unreleased]` above).

Exactly the members, types, defaults and order of
`specs/E5 Data/_Api.md` → "The calendar's types". Records are `sealed record`s
with `init` properties and **no positional constructor**; `Id`, `Name`,
`PersonId`, `Start` and `End` are `required`. `CalendarRange` is a
`readonly record struct`. XML doc comment on every type and member (`CODE-04`),
in English. Namespace `DRYL.Components`.

Verification: the gate. Additionally a test file
`tests/DRYL.Components.Tests/CalendarTypesTests.cs` pinning what the freeze
binds: the first member of each enum, `CalendarLabels` defaults, and that
`CalendarEvent` can be copied with `with`. Commit:
`feat(calendar): the calendar's types (3.5.0)`.

## T2 — layout engine

Files:

- `code/DRYL.Components/Components/Data/Internal/CalendarLayout.cs` (internal,
  static, no Blazor dependency).
- `tests/DRYL.Components.Tests/CalendarLayoutTests.cs`.
- `specs/E5 Data/F22 DrylCalendar/_Component.md` (add the file to `Source`).

Pure functions, each covered by tests named after the criterion they pin:

| Function | Covers |
|---|---|
| Visible range for a view, a date, a first day of week | `S1` "The visible range", including the month's whole weeks |
| Navigation step (previous/next per view, month-end clamp) | `S1` "Navigating" |
| Normalising `DayEnd` and `SlotMinutes` | `S1` "The time grid" |
| Splitting a timed event into per-day segments, clamped to `DayStart`–`DayEnd`, with continuation flags; ≥ 24 h goes to the all-day row | `S2` "Placement" |
| Overlap packing within one column (order, first free position, full width when alone) | `S2` "Overlapping events" |
| All-day lanes across a span of days, with continuation flags | `S2` "All-day and multi-day events" |
| Month placement: bars and lines per week row, capacity and overflow count | `S2` "The month view" |
| Colour resolution for people and events | `S2` "Colour", `S3` "People" |
| Visibility of an event under hidden people; unassigned detection | `S3` "Hidden people" |
| Unavailable spans per person and day; the all-unavailable intersection for the overlay | `S3` "Availability", "Drawing availability" |

Positions are returned as fractions (top, height, left, width in 0…1), never as
pixels, so the stylesheet owns every length. Numbers that reach markup are
formatted with `FormattableString.Invariant` — a German server otherwise writes
`0,5` into a style attribute.

Verification: the gate; every row of the table above has at least one test, and
the edge cases the spec names (zero-length event, event crossing midnight, event
outside the grid hours, duplicate ids, month of four and of six week rows,
February from the 31st) each have their own. Commit:
`feat(calendar): the layout engine`.

## T3 — shell: views, toolbar, time grid

Files:

- `code/DRYL.Components/Components/Data/DrylCalendar.razor`,
  `DrylCalendar.razor.css`.
- `tests/DRYL.Components.Tests/DrylCalendarTests.cs`.

Implements `S1` completely and the surface part of `S4`: the card surface
(`glass-card`, as `DrylTable` uses it), the toolbar composed from `DrylButton`,
`DrylTooltip` (icon-only previous/next, `UX-05`) and `DrylSegmentedControl` for
the view switch, the bound `View`/`Date` with internal fallback state when
unbound, `OnRangeChanged` (once after first render, then only on a real change),
the time axis, day headers, slots, today and the current-time line, `Loading`,
`Labels`, `Culture`, `Class` merge and `AdditionalAttributes`.

The current-time timer exists only while `Now` is `null`, ticks once a minute,
and is disposed with the component (`CODE-05`). Console reports (replaced
`DayEnd`/`SlotMinutes`) are written with `Console.WriteLine`, as `DrylTable`
does, once per instance.

Verification: the gate; bUnit tests for each `S1` section — rendered day count
per view, range callback arguments and its not firing on an unchanged range,
navigation steps, unbound operation, toolbar off, title format under `de-DE` and
`en-US`, hour labels under a twelve-hour culture, today marker and
`aria-current`, current-time line present/absent. Commit:
`feat(calendar): views, toolbar and the time grid`.

## T4 — events

Files: `DrylCalendar.razor`, `DrylCalendar.razor.css`, `DrylCalendarTests.cs`.

Implements `S2`: timed segments and the all-day row in day and week, bars and
lines and the overflow control in month, the default event content (title,
times, avatars through `DrylAvatar`, the recurring mark through `DrylIcon`),
`EventTemplate`, colour as tint plus edge indicator via a `data-color` slot
attribute (no inline colour), every event a `<button>`, `OnEventClick` by
pointer and by `Enter`/`Space`.

Each event is keyed by `Id` plus its segment's day — a `string` key. **Never key
on the `CalendarEvent` record itself**: equal content makes equal keys and kills
the circuit.

Verification: the gate; bUnit tests per `S2` section, including that every
segment of one event raises `OnEventClick` with the same instance, that the
template replaces content but not the box, and that a `SeriesId` renders the
mark with its accessible name. Commit: `feat(calendar): events in all three
views`.

## T5 — people and availability

Files: `DrylCalendar.razor`, `DrylCalendar.razor.css`, `DrylCalendarTests.cs`.

Implements `S3`: the legend as toggle buttons (`aria-pressed`), bound
`HiddenPersonIds` with internal fallback, person columns in the day view with
headers and the conditional unassigned column, `PersonTemplate`, muted areas for
unavailable time in columns and for the all-unavailable intersection in the
overlay, absence labels.

Verification: the gate; bUnit tests per `S3` section, including that hiding a
person does not recolour the others, that avatars still show hidden attendees,
that week and month ignore `Columns`, and the two empty cases (no people, all
hidden). Commit: `feat(calendar): people, columns and availability`.

## T6 — motion, narrow layouts, accessibility, interop

Files:

- `DrylCalendar.razor`, `DrylCalendar.razor.css`, `DrylCalendarTests.cs`.
- `code/DRYL.Components/wwwroot/js/dryl.js` (`window.dryl.calendar.scrollToTime`).
- `tests/js/calendar.test.mjs`.

Implements the rest of `S4`:

- **Motion** — view change and navigation transitions on the grid, enter/exit of
  single events through `DrylPresence` (`DESIGN-12`), hover/focus transitions.
  Only `--dur-fast`/`--dur-med` and `--ease-out`; a `prefers-reduced-motion`
  block that switches all of it off (`UX-06`). Animate `opacity` and `transform`
  only — no looping custom properties, nothing that leaves the compositor.
- **Narrow layouts** — a `.cq` wrapper **around** the element the `@container`
  rules apply to (a container cannot query itself), one modifier class per
  `Breakpoint`, as `DrylStack` does. The week strip, the month marks and the
  month's day list are in the markup at every width and switched by CSS, so
  bUnit can assert them without a browser.
- **Accessibility** — tab order, accessible names of events and segments, the
  polite live region for the range title, the loading status, `aria-hidden` on
  decoration.
- **Interop** — one best-effort `dryl.calendar.scrollToTime(el, fraction)` after
  the first interactive render, guarded for prerender and a closed circuit; no
  object reference handed out.

Verification: the gate, `node --test tests/js/*.test.mjs`, and the browser pass
through the `/verify` skill on the website demo (T7 may be pulled forward as far
as a throwaway page is needed): both colour modes, calendar at 375px and inside
a narrow column on a wide viewport, a full Tab pass, reduced motion on. Record
what was looked at here. Commit:
`feat(calendar): motion, narrow layouts and accessibility`.

## T7 — website demo and catalog

Repository `DRYL.Website` (separate repo, branch `feat/calendar`, consumes the
library through its `ProjectReference`).

Files: `Components/Pages/DemoCalendar.razor`,
`Components/Examples/Calendar/*.razor` (basic week, month, person columns with
availability, custom event template, German labels and culture),
`Components/ComponentCatalog.cs` (`"Calendar"` / `calendar`, section `Data`, not
flagged AI-capable).

Verification: `dotnet build` and `dotnet test DRYL.Website.slnx` in that
repository — the library's tests do not cover it. Commit there:
`feat: DrylCalendar demo page`. The website PR merges only after the library
release it depends on.

## T8 — review, full verification, close the loop

Files:

- `specs/E5 Data/F22 DrylCalendar/_Component.md`, `S1`–`S4` (`State:
  Implemented`, plus a `## Recorded gaps` section in any story the code falls
  short of — honest gaps, not silent ones).
- `specs/E5 Data/_Api.md`, `_Interop.md` (component and type counts, the interop
  table row, the cleanup table row, the "specified, not yet implemented" notes
  removed).
- `harness/requirements.md` (`E5` 21 → 22, total 130 → 131, and the three prose
  mentions of the total), `CLAUDE.md` (`x/130` → `x/131`).
- `CHANGELOG.md` (the `[3.5.0]` block complete, its date the day it ships).
- This plan.

Independent review of the whole branch before the full run — the component is
new public API under the freeze. Then the complete evidence of `CLAUDE.md`
stage 5:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs
node scripts/check-motion-tokens.mjs
node --test tests/js/*.test.mjs
```

The coverage line must read `65/131` with no structural error. Then the pull
request; merging publishes 3.5.0.

Commit: `docs(calendar): spec to Implemented, counts and changelog (3.5.0)`.
