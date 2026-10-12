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
| T3 — shell: views, toolbar, time grid | Complete | `02ee9dd` `feat(calendar): views, toolbar and the time grid`; gate passes (1445 tests, 51 new in `DrylCalendarTests`); coverage has no missing `Source` path left for `F22`. Looked at in the browser on a pulled-forward website demo (`DRYL.Website`, branch `feat/calendar`, uncommitted until T7): week view in light mode, month view in dark mode, no console warning, no horizontal page scroll at 1280px. The scroll to `InitialScrollTime` and `CollapseBelow` arrive with T6. |
| T4 — events | Complete | `45aa9b3` `feat(calendar): events in all three views`; gate passes (1473 tests, 28 new). Looked at on the website demo with sample events: week view in both colour modes, month view in light mode — tints over `--chart-1`…`--chart-6` hold up by eye in both modes, text stays readable. `People` came in with this task (colour and avatars need it); legend, hiding, columns and availability remain T5. Enter/exit motion of events remains T6. |
| T5 — people and availability | Complete | `1ad8662` `feat(calendar): people, columns and availability`; gate passes (1499 tests, 26 new). Looked at on the website demo (new example "Person columns and availability"): the day view with three person columns, an unassigned column, working hours, a labelled absence and a whole-day absence, in both colour modes. **Muted areas over `--line-soft`: clear in light mode, visible but faint in dark mode** (computed `rgba(255,255,255,0.04)` on the dark ground) — judged to hold up, flagged for the maintainer's eye rather than changed (`DESIGN-03`). |
| T6 — motion, narrow layouts, accessibility, interop | Complete | `c07663c` `feat(calendar): motion, narrow layouts and accessibility`; gate passes (1534 tests, 35 new), `node --test tests/js/*.test.mjs` passes (149, 5 new in `calendar.test.mjs`). Browser pass on the website demo, recorded under "T6 — what was looked at" below. |
| T7 — website demo and catalog | Complete | `DRYL.Website` branch `feat/calendar`, `84992ce` `feat: DrylCalendar demo page`: five examples, registered as `"Calendar"` / `calendar` under `Data`, not AI-capable. `dotnet build` and `dotnet test DRYL.Website.slnx` pass (36 tests). Not pushed; merges after the library release. |
| T8 — review, full verification, close the loop | Complete, except the pull request | Independent review by a separate agent: six confirmed findings, three plausible; five fixed with tests in `cddf1bc` `fix(calendar): findings of the independent review`, the rest recorded as gaps (see below). Specs to `Implemented` with `## Recorded gaps` in all four stories. Full evidence run on 2026-10-12: build 0 errors, 1543 tests pass, light sync in sync, contrast 50 PASS / 0 FAIL, harness links OK, motion tokens clean, JS tests 149 pass, coverage `65/131` with no structural error (the script exits non-zero by design). **Not done: push and pull request** — outward-facing, left to the maintainer's word. |

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

- **T4 — a segment's accessible name states the event's whole span** rather
  than a word for "continues": `CalendarLabels` has no such member. A segment of
  an event from Monday 22:00 to Tuesday 02:00 reads both days and both times in
  each of its two segments.
- **T4 — tab order is not strictly day-major in the week view.** The all-day row
  sits above the grid, so all all-day bars (in day order) are reached before the
  timed events (day by day, by start). In the month view a week row's day
  numbers come first, then its events by day, bars before lines. `S4` asks for
  day, then all-day before timed, then `Start`; a bar that spans days has one
  place in the DOM.
- **T4 — event avatars are `DrylAvatar`, scaled down inside the event** through
  `::deep` from the calendar's own wrapper, because the avatar's smallest size
  is taller than a month line. Where a box is too narrow, avatars and then the
  recurring mark are clipped before the title is.
- **T4 — a segment drawn shorter than 45 minutes is single-line** (`is-compact`),
  so its text is cut at the box and not through the middle of a second line.

- **T5 — with every person hidden, the unassigned column is always rendered**
  in person columns, with or without an unassigned event: `S3` asks for "the
  time axis and the unassigned column only", and a grid of no columns would
  show nothing at all.
- **T5 — "today" in person columns.** The day view has no day header there, so
  every person's column header carries `aria-current="date"` on today, and the
  current-time line is drawn across every column.
- **T5 — an absence without a `Label` mutes its time and names nothing**, in the
  column and in the all-day row alike.
- **T5 — a hidden legend entry is dimmed with `opacity: 0.6`** and a hollow
  colour dot; there is no opacity token, and existing components write the
  number the same way.

- **T6 — a view change fades the incoming grid in; the outgoing grid is removed
  at once.** `S4` asks for the outgoing grid to fade out as well. Keeping it
  mounted for its exit would leave its controls in the tab order while it fades
  (`UX-07`); `DrylPresence` has no way to make its child inert, and changing
  that primitive is outside this delivery. **A gap against `S4` "Motion".**
- **T6 — events sit in a slot.** `DrylPresence` renders a wrapper that takes a
  class but no style, so each event is `slot (position) > presence > button`.
  An event that has left stays as a ghost in its slot until `OnExited`, with
  `aria-hidden` and `tabindex="-1"`. In person columns, hiding a person removes
  their whole column at once; their events do not play an exit.
- **T6 — events loaded after a navigation enter together, a beat after their
  grid.** A host that answers `OnRangeChanged` asynchronously hands the events
  over in a later render; they then enter with the event transition, all at
  once, not staggered. Events already in `Events` when the range changes arrive
  with the grid and play no enter of their own.
- **T6 — the time grid is scrolled whenever it is newly shown**, i.e. on the
  first render and again when the user comes back from the month view (the
  scroller is new DOM and would otherwise sit at midnight). Navigation and
  day/week switches keep the scroller and its position.
- **T6 — narrow and wide controls exist side by side in the markup** and are
  switched by CSS: the week's header buttons (open the day) and its strip
  buttons (select the day), the month's day number (opens the day) and its
  picker (selects the day). The server cannot know the width.
- **T6 — the strip's and the month's "has events" mark is visual only**
  (`aria-hidden`); `CalendarLabels` has no text for it.
- **T6 — in a box narrower than 120px the avatars and the recurring mark are
  not shown**, and a single-line segment narrower than 150px drops its times
  (container queries on the slot). The accessible name states all of it at any
  width.
- **T6 — the default `CollapseBelow` of `Md` is 768px of the calendar's own
  width**, so a calendar in a 650px card is narrow on any screen. The website's
  week, month and German demos set `Breakpoint.Sm` to show the full layouts.

## T8 — the independent review

Read-only review of the branch by a separate agent. What it found and what
became of it:

| Finding | Outcome |
|---|---|
| An event whose `PersonIds` is `null` throws during render. | Fixed — read as unassigned; tests in both suites. |
| A bound `Date` still holding `default` renders the year one and throws with a Sunday-first culture. | Fixed — the default date is read as unset; `StartOfWeek` and `Step` no longer leave the calendar's ends. Recorded in `S1`. |
| An event ending at `DateTime.MaxValue` throws in `DaySpan`. | Fixed, with tests. |
| A zero-length event just before midnight is drawn on two days. | Fixed — its one slot stops at midnight. |
| A month line can sit above a bar where bars leave a lane free. | Fixed — lines go below every bar of their day. |
| Opening a day raised the date before the view, so an asynchronous handler saw a stray range. | Fixed — both change before either callback runs. |
| The overflow control, muted areas, absence labels, the current-time line and the unassigned column mount without `DrylPresence`. | Recorded in `S4`. |
| Narrow, the week's all-day row keeps the lanes of the whole week. | Recorded in `S4`. |
| A slot key could collide for a person id containing `|col|person|`. | Left — not a realistic id. |

No finding against the public API, the token rules, the motion vocabulary or
the doc comments.

A consequence of the month-line fix worth knowing: a line now sits below a
bar's lane even when that bar is hidden by another day's overflow, so such a
day can show `+2 more` with a row to spare. It is counted, never dropped.

## T6 — what was looked at

On `http://localhost:5044/components/calendar` (website branch `feat/calendar`),
Chromium through Playwright, 2026-10-12:

- **Both colour modes** — week, month and person columns in light and dark.
- **Narrow inside a wide viewport** — at 1280px the demo card is 656px wide, so
  the default `Md` collapsed the week to strip + one column and the month to
  marks + day list; with `Sm` the same card shows the full layouts.
- **375px viewport** — no horizontal page scroll (`scrollWidth - clientWidth`
  = 0), calendars 297px wide, toolbar wraps, week strip shown; person columns
  scroll sideways (576px in 285px) with the axis `position: sticky`. This pass
  found the sticky header painting under the events; fixed (`.cal-head`
  `z-index`), not re-photographed.
- **Tab pass** (person columns): Previous, Today, Next, view switch, the three
  legend entries, then the events column by column; every stop showed the
  focus ring (the view switch its own).
- **Motion** — Next/Previous: `presence-in-left` / `presence-in-right` at
  0.24s on the grid, sticky header intact afterwards. Hiding a person: two
  slots `is-exiting` with `presence-out-scale`, gone after the exit; showing
  them again restored all thirteen.
- **Reduced motion** — grid animation `none`, event and legend transitions
  `0s`, exits finished at once. This pass found the week's day-header button
  still transitioning (selector specificity); fixed, not re-measured.
- **Not looked at:** a real screen reader, forced-colors, Firefox and Safari.

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
