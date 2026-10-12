# A scheduling calendar

## Meta
- **State:** Ready

## Problem

Raised by the Product Owner on 2026-10-12:

> "Ich würde gerne als nächstes eine Kalenderkomponente bauen, die schön
> individualisierbar ist und Wochenansicht, Monatsansicht, Tagesansicht,
> Kalender-Ereignisse erstellen, hin und her schieben, vergrößern, verkleinern.
> Teilnehmer, so ein richtiges Enterprise Kalender Control."

DRYL has no way to show or edit events on a time axis. The only calendar-shaped
thing in the library is the month grid inside `DrylDatePicker`, which picks a
date and knows nothing about events. A line-of-business application that plans
appointments, shifts or bookings has to bring a third-party scheduler — which
does not read DRYL's tokens, does not follow the two colour modes and has no
notion of `AiState`.

The target role is the Blazor developer building a line-of-business application
in which several people's time is planned; the people using that application
are the ones who drag the events.

## Solution Idea

A calendar component with day, week and month views for planning across several
people. The host supplies the events, the people and their availability; the
calendar renders them, lets the user create, move and resize events directly on
the grid, marks conflicts, and reports every change as a request the host
accepts or refuses. Several people appear overlaid in one grid or side by side
as columns. A default editor covers an event's details and can be replaced.

## Scope
- **In scope**, shipped in three stages, each its own release:
  1. **Show.** The event model; day, week and month view; all-day and multi-day
     events; recurring events marked as part of a series; several people
     overlaid or as columns; availability as muted areas; navigation between
     dates and views; templates for an event's content and a person's header;
     visible time range, slot length and first day of the week as parameters;
     the narrow layouts.
  2. **Edit.** Creating, moving and resizing an event by pointer, touch and
     keyboard; reassigning by moving across person columns; conflict detection,
     shown while dragging and on the dropped event; every change reported to the
     host as a request.
  3. **Editor and AI.** The default event editor and its replacement template;
     `Ai` on an event, including `Proposed` with accept and dismiss. This stage
     waits for I20.
- **Out of scope:** a horizontal timeline (people as rows); a recurrence rule
  editor and editing a whole series; time zone conversion; blocking a change
  because of a conflict; free colour values; storing events; agent tools and use
  inside the canvas.

## Impact

Second pass by the Tech Lead, 2026-10-12, against the scope decided so far.

- **Harness:**
  - **Motion — no blocker.** Following the pointer is direct manipulation, not
    an animation. Settling on release, switching views and an event entering or
    leaving fit the three durations and three easings (`DESIGN-10`) and
    `DrylPresence` (`DESIGN-12`).
  - **Colour — no new tokens.** `--chart-1..6` already is a validated
    categorical palette in both LIGHT-TOKEN-SET copies and follows the theme. An
    event is tinted with it and carries it as a small indicator, never as a
    solid fill (`DESIGN-08`). Six slots match the overlay view, which stops
    being readable beyond about five people; in the column view the column
    identifies the person, not the colour.
  - **AI — a conflict with the decision above.** `AiState` is
    `None / Active / Thinking / Streaming / Generated`. The three live states may
    only show while the AI is working (`AI-06`) and `Generated` is a one-shot
    that removes itself (`AI-07`). None of them can mark an event that *waits*
    to be accepted. A persistent "proposed by AI" look is a new AI visual
    (`AI-04`). Resolved by the Product Owner with a new state, `Proposed`,
    shaped in I20; the calendar's third stage depends on it.
  - **Dependencies — none** (`CODE-03`).
  - **Keyboard — binding, not optional.** Every pointer action must be reachable
    by keyboard (`UX-01`): creating on a focused slot, grabbing an event, moving
    and resizing it by slot with the arrow keys, dropping, cancelling. Results
    and conflicts are announced through `aria-live`.
- **Specs:** a new feature spec for `DrylCalendar`; its category follows its
  source folder (`SPEC-02`) — `Components/Data/`, so `specs/E5 Data/`, with
  `_Api.md` and `_Interop.md` of that category gaining its types and its bridge.
  No existing spec overlaps — `DrylDatePicker` in `E8 Inputs` picks a date and
  stays untouched.
- **Public API:** entirely new, so additive → MINOR per stage (`REL-01`). It
  consists of the component, an event type, a person type, an availability
  type, enums for the view, the way people are laid out and the colour slot
  (`CODE-02` — never a string), change-request callbacks, and the templates.
  The API is frozen once the first stage ships: the event model has to carry
  what stages two and three need (assignment to people, series membership, `Ai`)
  from the start, or later stages cannot add it without a MAJOR. The spec for
  stage one therefore defines the whole model.
- **Code:** touch points under `code/DRYL.Components/Components/` and
  `wwwroot/js/dryl.js`.
  - **Risk — dragging on Blazor Server.** Moving and resizing cannot round-trip
    per pointer move. The gesture has to run in the browser and report once, on
    release — the shape `initColumnResize` in `dryl.js` already has (one
    delegated `pointerdown`, pointer capture, one callback).
  - **Risk — drag without a pointer.** Every gesture needs a keyboard path
    (`UX` rules); a calendar that can only be edited by dragging fails that.
  - **Risk — size.** This would be the largest single component in the library.
    It needs staging, not one release.
  - **Risk — touch.** A drag that starts on touch-down would swallow scrolling;
    the grab needs a hold, and the page must not scroll while an event is held.
  - **Risk — the narrow layout.** It keys on the calendar's own width, which
    needs a container-query ancestor around the element the rules apply to.

## Decisions

- 2026-10-12 (Product Owner): the calendar is **built for several people**, not
  for one person's own appointments.
- 2026-10-12 (Product Owner): it **knows availability and shows conflicts**;
  attendees are more than names on an event.
- 2026-10-12 (Product Owner, delegated to the Tech Lead): **the host owns the
  data.** Events come in as a parameter; the calendar reports a requested change
  (create, move, resize) and the host accepts, rejects and stores it. A calendar
  that kept its own events could not let a line-of-business application refuse a
  move, and would have to be synchronised with the host's store.
- 2026-10-12 (Product Owner): several people are shown **overlaid** (one grid,
  a colour per person, people can be hidden) and **as columns** (day view, one
  column per person; moving an event across columns reassigns it). A horizontal
  **timeline is out** of this idea — a later component on the same event model.
- 2026-10-12 (Product Owner): the first stage carries **all-day and multi-day
  events**. **Recurring events are display only**: the host supplies the
  occurrences, the calendar marks them as part of a series; a rule editor and
  "this one or all" come later. **Time zones are out**: the calendar shows one
  zone, the host converts.
- 2026-10-12 (Product Owner): **the host supplies availability** per person
  (working hours, absences), drawn as muted areas. The calendar detects
  conflicts itself — two events of one person overlapping, or an event inside an
  absence — and shows them already while dragging.
- 2026-10-12 (Product Owner): **a conflict warns, it never blocks.** The event
  can be dropped and is marked as conflicting; refusing it is the host's
  business logic. Double bookings are often intended.
- 2026-10-12 (Product Owner): event colours come from a **fixed, validated
  palette**, not from free colour values — a free value guarantees no contrast
  in either mode and bypasses `DESIGN-01`. **New tokens for a calendar palette
  are approved in principle** (`DESIGN-03`); their number and values are put to
  the Product Owner before anything is built.
- 2026-10-12 (Product Owner): the calendar ships a **default event editor**
  built from existing DRYL inputs (title, time, all-day, attendees, colour),
  **replaceable by a host template**. The same holds for an event's content in
  the grid and for a person's header. Visible time range, slot length and first
  day of the week are parameters.
- 2026-10-12 (Product Owner): **an event can carry an `AiState`.** An event
  proposed by AI shows as a proposal in the existing AI vocabulary and can be
  accepted or dismissed. Agent tools ("find a slot for these four people") and
  use inside the canvas are a separate, later idea in the Agents package.
- 2026-10-12 (Product Owner): the calendar lives in the **core package**.
- 2026-10-12 (Product Owner): an event that waits to be accepted is shown with a
  **new `AiState`, `Proposed`** — not with an ordinary "tentative" status. No
  existing state fits: the live ones may only show while the AI works (`AI-06`),
  `Generated` removes itself (`AI-07`). The state is shaped in its own idea,
  [I20](I20%20An%20AiState%20for%20work%20awaiting%20a%20human.md), because it
  changes the whole library. This supersedes "in the existing AI vocabulary"
  in the decision above.
- 2026-10-12 (Product Owner): event colours use the **six existing `--chart-*`
  slots**; no new token is added. The approval in principle stays unused.
- 2026-10-12 (Product Owner): the component is named **`DrylCalendar`** and
  lives under `Components/Data/`. `DrylScheduler` stays free for the later
  timeline.
- 2026-10-12 (Product Owner): it ships in **three stages, each its own
  release** — show, edit, editor and AI — as listed under Scope.
- 2026-10-12 (Product Owner): a proposed event that is **accepted goes to
  `Generated`**; one that is **dismissed is removed**. The host triggers both;
  the calendar never decides.
- 2026-10-12 (Product Owner): on **touch, an event is grabbed by holding it**,
  so a swipe keeps scrolling; resize handles appear on the selected event only.
- 2026-10-12 (Product Owner): when the **calendar itself is narrow** — its own
  width, not the screen's — the week view shows one day with a week strip to
  switch, the month view shows dots with the selected day's events listed
  below, and person columns scroll horizontally beside a fixed time axis.

- 2026-10-12 (Product Owner): the calendar is built as a **general library
  component**; the **demo page on the docs website is its first consumer**.
  ElDestinoHub may become a second one, but not yet, and does not shape the
  first stages.
- 2026-10-12 (Product Owner): the final version — problem, the three stages,
  what is out of scope, and the impact above — is **confirmed**. The idea is
  `Ready`.

## Open Points

*(none.)*
