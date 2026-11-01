# A toast redesign

## Meta
- **State:** Draft
- **Specs:** none yet — `DrylToast` and `DrylToastProvider` have no spec
  (`specs/E7 Feedback` stops at F8 `DrylNotifications`)
- **Target release:** 3.16.0 (`release/3.16.0`, branch `feat/i34-toast-redesign`)

## Problem

The Product Owner questions the toasts as they are (screenshot of three stacked
toasts — Info, Error, Warning — in dark mode, 2026-11-01). The Tech Lead's
reading of what is wrong today, from the screenshot and the code
(`DrylToast.razor`, `DrylToastProvider.razor`, the `.toast*` rules in
`dryl.css`):

1. **Every toast shouts.** One toast carries five accent signals at once: a
   coloured 1px border, a 3px inset rail on the left, a 32–36px outer glow, a
   tinted icon chip and a coloured progress bar. Three stacked toasts read as a
   traffic light, not as glass with an accent that glows (`DESIGN-08`'s
   intent). Info and Error differ in hue only, not in weight.
2. **The stack has no shape.** Every toast is a full card; ten toasts are a
   wall. There is no limit, no collapse, no sense of "newest in front".
3. **A failure disappears on its own.** `ShowError` auto-dismisses after 5 s
   like everything else. The progress bar tells the reader they have five
   seconds to read about a failed payment.
4. **No action.** "Undo", "Retry", "Open" — the most common reason a toast
   exists — needs a custom body component (`Show<TComponent>`).
5. **Not AI-native in practice.** `Ai` exists and the reference can update it,
   but there is no toast that *follows* a task ("Generating report…" →
   "Report ready"), which is what an agent most often wants to say.
6. **Debt in the current code**, which a redesign would pay off rather than
   copy: hand-written `backdrop-filter` instead of `--panel-float` +
   `--glass-fx-float` (`DESIGN-06`/`DESIGN-07`); the progress bar runs on
   `linear` (`DESIGN-10`); the host (`aria-live="polite"`) nests every toast's
   own live region, so a toast can be announced twice; the close button is a
   12px glyph in the corner, misaligned with the first text line in the
   screenshot.

**Target role:** a Blazor developer giving short-lived feedback from a page or
a background task — and the person reading it.

## Solution Idea (options, Tech Lead)

- **A — Quiet glass, same model.** Keep the API; strip the visuals down to one
  accent signal per toast (icon chip and a thin gradient on the top edge, no
  rail, no coloured border, no glow), errors and warnings stay until closed,
  close button aligned to the first line, frost via the float tokens. Low risk,
  PATCH-to-MINOR, but the stack problem stays.
- **B — A living stack.** A, plus the stack becomes one object: toasts pile up
  behind the newest (scaled and offset, at most three visible), hovering or
  focusing the stack fans it open, a configurable `MaxVisible`. New: an
  optional action button (`ToastOptions.Action`), and a toast whose state the
  reference updates (`reference.Update(...)`) — the basis of a task toast.
- **C — B, AI-native.** B, plus a task toast: `Toasts.Track(task, ...)` shows a
  pending toast with the AI state, morphs into success or error when the task
  completes, and — opt-in — a toast that times out lands in the
  `DrylNotifications` inbox instead of being lost.

Recommendation: **B**, with C's task toast as a stretch if B fits comfortably
into 3.16.0. The inbox hand-off is worth its own idea.

## Scope
- **In scope (proposed):** visuals and motion of `DrylToast`; the stack
  behaviour of `DrylToastProvider`; dismiss rules per variant; an action
  button; the first spec for both components.
- **Out of scope (proposed):** `DrylNotifications` itself; `DrylAlert`
  (already redone in I28); a breaking change to `IDrylToastService`.

## Impact (first pass — to be completed in phase 3)

- **Harness:** a stacked/fanned pile may need new keyframes or a motion
  primitive → `DESIGN-10` sign-off. Possibly a token for the stack offset →
  `DESIGN-03` sign-off. No new `AiState` expected.
- **Specs:** new spec(s) under `specs/E7 Feedback` (or `E11 Surfaces`, where
  the code lives today) — raises the coverage meter.
- **Public API:** additive only (`ToastOptions.Action`, `MaxVisible`,
  `Update`), so MINOR; changing the error default from 5 s to "stay" is a
  behaviour change that needs a changelog note.
- **Code:** `DrylToast.razor`, `DrylToastProvider.razor`, `Toasts/*`,
  `.toast*` in `dryl.css` (light-sync check applies), `js/` toast interop
  (auto-dismiss timer and hover pause live there today).

## Decisions
- 2026-11-01 — Product Owner: question the toasts and think about a redesign,
  as the first feature of the next minor release (3.16.0).

- 2026-11-01 — Product Owner: the toasts already look good; the look stays.
  What is missing is a small morph. Asked for three motion variants as a
  browser prototype.
- 2026-11-01 — Tech Lead: prototype with three motion variants on the
  unchanged look — A *Capsule* (a pill around the icon unfolds into the card
  and folds back), B *Deck* (newest in front, older ones recede into a stack,
  hover fans it out and pauses every timer, an expired toast shrinks into its
  progress line), C *Origin and bell* (grows out of the button that raised it,
  an unread expired toast flies into the notification bell; a task toast morphs
  from "writing" to "ready"). Every variant lets neighbours glide (FLIP) and
  gives each toast its own countdown.
- 2026-11-01 — Product Owner prefers variant C and asked what happens when a
  toast has no origin (raised by an event, not a click). Tech Lead: without an
  origin the toast should not fall back to A or to today's slide-in, but come
  *out of* the bell it returns to on expiry — one path in and out. Where no
  bell exists, it falls back to A's capsule at the host. Prototype extended
  with an "Ereignis" button and a switch between the three fallbacks.

## Bug found on the way
- Product Owner: when several toasts are open and the first expires, the
  other toasts' progress bars restart — visually only, they still close on
  time. Tech Lead's likely cause: in `DrylToastProvider` the `@key` sits on the
  `.toast-slot` div *inside* `CascadingValue`, while the loop's top-level item
  is the `CascadingValue`, which Blazor diffs by position. Removing the first
  entry hands each position its successor's content and rebuilds the DOM below
  it; the JS timer in `dryl.toast` still holds the old slot, so the close stays
  on time while the new bar starts from full. Fix: move `@key` onto
  `CascadingValue`. Not yet verified in the browser; fix as its own branch into
  `release/3.16.0`.

## Open Points
- Variant C chosen in principle — confirm the no-origin fallback (bell, else
  capsule).
- How a toast learns its origin: an explicit `ToastOptions.Origin`
  (`ElementReference`), automatic capture of the last activated element in
  `dryl.js`, or both?
- How the toast finds the bell: does `DrylNotifications` register itself with
  the toast service, and does an expired toast become a notification entry?
- Superseded: Each needs `DESIGN-10` sign-off for its
  keyframes / Web Animations.
- Should errors and warnings stay until closed?
- `linear` on the countdown bar: a timer drawn eased would misstate the time
  left — exception like the spinner, or not?
