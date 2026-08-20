# Plan: the five defects the E7 specs turned up

**Branch:** `idea/i8-appearance-regression` (continuing) · **Base:** `96ceb11`
(`2.24.2`, **published** — `v2.24.2` is tagged and on `origin/main`)

Writing the Feedback specs meant reading eight components against their code
rather than their doc comments, and five things came out of that reading that
are defects rather than debt. This plan fixes them. One task per commit, each
with its own verification.

Because `2.24.2` is published, task 1 bumps `<Version>` to **`2.24.3`** and cuts
the changelog block; tasks 2–4 add their entries to that same block and leave
the version alone (`REL-01`). All four are fixes, so PATCH carries the stack.

Every task touches a component whose spec now exists, so every task updates that
spec in the same commit (`SPEC-01`) and leaves it on `Implemented` — spec and
code changed together in one session goes straight there, without `Modified` as
an intermediate state (`SPEC-04`).

The sixth item raised in the same conversation — giving `DrylTooltip` an `Ai`
parameter — is **not** in this plan. It is a new feature touching the AI visual
vocabulary, so it belongs in the idea stage (`IDEA-01`, `AI-04`), and it is
opened as `ideas/I9 AI tooltip.md` instead.

---

## Task 1 — `DrylProgress` reports the value it actually draws

- **Files:** `code/DRYL.Components/Components/Feedback/DrylProgress.razor` ·
  `specs/E7 Feedback/F5 DrylProgress.md` ·
  `code/DRYL.Components/DRYL.Components.csproj` · `CHANGELOG.md`
- **The defect:** the fill is clamped into 0…100 %, the reported ARIA value is
  not. `Value="120" Max="100"` draws a full bar and tells a screen reader
  "120 of 100"; `Value="-5"` draws an empty bar and reports `-5`. The two halves
  of the same component disagree about what is on screen, and the half a
  sighted user cannot check is the wrong one.
- **The fix:** report the clamped value. The clamp already exists for the
  percentage; the ARIA value is derived from the same clamped number instead of
  from the raw parameter. Formatting stays invariant-culture.
- **Not in scope:** `Max <= 0`. The bar renders empty for it today and will
  report `0` of that `Max`; changing what a non-positive scale means is a
  behaviour decision, not this fix.
- **Verify:** `dotnet build DRYL.slnx -c Release`; a new bUnit test asserting
  the reported value for an over-range, an in-range and a negative `Value`;
  `dotnet test DRYL.slnx -c Release`.

## Task 2 — the shimmer stops when the user asked for less motion

- **Files:** `code/DRYL.Components/wwwroot/dryl.css` ·
  `code/DRYL.Components/Components/Feedback/DrylSkeleton.razor.css` ·
  `specs/E7 Feedback/F4 DrylSkeleton.md` · `CHANGELOG.md`
- **The defect:** `DrylSkeleton`'s reduced-motion block calms the AI mutations
  and drops the stagger, and it was easy to read that as the component honouring
  `UX-06`. It does not: the `skel` primitive's own sweep lives in `dryl.css` and
  is untouched by any reduced-motion rule, so a user who asked for less motion
  gets a placeholder that is *entirely* moving — and on a loading page that is
  most of the screen.
- **Where it belongs:** in the primitive, not in the component. `DrylImage`
  renders a `DrylSkeleton` for its own loading state, so both are covered by one
  fix, and any future consumer of `skel` inherits it.
- **The fix:** under `prefers-reduced-motion: reduce`, the sliding strip is not
  painted at all, leaving the block as a flat token surface. The AI streaming
  state keeps its violet-cyan **color** as a static tint on the block itself, so
  the one thing the shimmer was saying — model output is arriving here — is not
  lost with the motion that said it.
- **Not in scope:** the base shimmer's rate with motion on. `DESIGN-10` leaves
  continuous motion free and 1.4 s is a chosen rhythm.
- **Verify:** `node scripts/check-motion-tokens.mjs`;
  `node scripts/check-light-sync.mjs`; both color modes with reduced motion
  forced in the browser, on `/components/skeleton` and `/components/image`.

## Task 3 — a controlled `DrylNotifications` stops writing to its input

- **Files:**
  `code/DRYL.Components/Components/Feedback/DrylNotifications.razor` ·
  `specs/E7 Feedback/F8 DrylNotifications.md` · `CHANGELOG.md` ·
  (`DRYL.Website`) `Components/Examples/Notifications/Controlled.razor`
- **Two defects, one component:**
  1. **It mutates the consumer's object.** Activating an unread row sets `Read`
     on the supplied `DrylNotification` *and then* raises `OnMarkRead`. A
     controlled component whose whole contract is "you own the state, I raise
     callbacks" writing to that state is a surprise, and a consumer holding a
     snapshot finds it changed under them. The website's own controlled example
     has a no-op `OnMarkRead` with a comment explaining that the component
     already did it — the demo documents the bug.
  2. **The unread state may never be announced.** The dot carries an
     `aria-label` on a bare `span` with no role. A generic element without one
     is not reliably named, so a screen-reader user hears a row's title and time
     and not that it is unread — the state the whole bell exists to convey
     (`UX-05`).
- **The fix:** in controlled mode the component raises `OnMarkRead` and writes
  nothing; service-driven mode is unchanged, because there the service *is* the
  state. The unread marker moves into the row's own accessible name as
  visually-hidden text, and the dot becomes decorative.
- **Consequence for the website:** the controlled example must now set `Read`
  itself. That is the point — it becomes an example of controlled mode instead
  of an example of relying on the component to cheat.
- **Verify:** `dotnet build`; new bUnit tests — controlled mode leaves `Read`
  untouched and raises the callback, service-driven mode still marks read; the
  unread row's accessible name contains the marker;
  `dotnet test DRYL.slnx -c Release`; then in `DRYL.Website`,
  `dotnet test DRYL.Website.slnx`.

## Task 4 — a dismiss button that dismisses

- **Files:** `code/DRYL.Components/Components/Feedback/DrylAlert.razor` ·
  `specs/E7 Feedback/F1 DrylAlert.md` · `CHANGELOG.md`
- **The defect:** `<DrylAlert Dismissible>` with no `OnDismiss` renders a
  button that is focusable, announced and inert. The alert never removes itself,
  so with nobody listening the control does nothing at all — a control that
  lies about being actionable.
- **The options considered:**
  1. *Render the button only when a handler is attached.* Rejected: it silently
     drops a control the consumer explicitly asked for, and `Dismissible` would
     mean two different things depending on an unrelated parameter.
  2. *Always self-hide, and also raise the callback.* Rejected: a host that
     unmounts the alert on `OnDismiss` gets the same effect twice, and an alert
     that hides itself has no way back — a host re-showing the same instance
     would find it invisible.
  3. **Self-hide only when no handler is attached.** Chosen. The rule is one
     sentence: if nobody is listening, the button still does the obvious thing;
     if someone is, they own the lifecycle exactly as today.
- **The fix:** option 3, plus the state resets when `Dismissible` is turned off
  and on again, so the alert is recoverable without remounting.
- **Verify:** `dotnet build`; new bUnit tests — dismiss with no handler removes
  the alert, dismiss with a handler raises it and leaves the alert mounted;
  `dotnet test DRYL.slnx -c Release`.

## Task 5 — the catalog stops calling the tooltip CSS-only

- **Files:** (`DRYL.Website`) `Components/ComponentCatalog.cs`
- **The defect:** the catalog's one-line description reads "CSS-only hover
  tooltip — 4 placements, wraps any trigger." The bubble has been a JS-driven
  body-level portal for a long time, which is exactly why it survives the glass
  cards it is used inside. The line is on the components overview and in the
  search index, so it is the first thing a reader is told about the component,
  and it is false.
- **The fix:** describe what it is. No library code, no version bump — this is
  `DRYL.Website` only.
- **Verify:** `dotnet test DRYL.Website.slnx`; the overview page and the search
  result read correctly in the browser.

---

## What is deliberately left alone

- **`DrylProgress` has no default `AriaLabel`.** A real gap, recorded in `F5`,
  but giving it a default is a behaviour decision about what a nameless bar
  should be called — not a defect with one obvious fix.
- **The fixed English strings** across five components. A localisation surface
  is a feature, not a patch.
- **`DrylNotifications` has no virtualisation and the service never trims.**
  Both are real and both are design decisions the maintainer should make.
- **The literal type sizes, chip diameters and panel widths** in six of the
  eight components. `DESIGN-01` does not cover type scale today; inventing a
  token set for it is a `DESIGN-03` proposal, not a fix.
