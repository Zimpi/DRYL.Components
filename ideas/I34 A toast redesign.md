# A toast redesign

## Meta
- **State:** Ready
- **Specs:** `specs/E11 Surfaces/F9 DrylToast.md`,
  `specs/E1 Foundation/F3 DrylToastProvider.md`,
  `specs/E7 Feedback/F8 DrylNotifications.md` (the bell marker)
- **Target release:** 3.16.0 (`release/3.16.0`, branch `feat/i34-toast-origin`)
- **Plan:** `docs/2026-11-01-i34-toast-origin-plan.md`

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
- **In scope:** the motion of service-driven toasts (`DrylToastProvider`):
  entry from an origin element, from the notification bell, or as an
  unfolding capsule; the return into the bell with an inbox entry; neighbours
  that glide; one countdown per toast (the reset bug). The marker on the
  `DrylNotifications` bell. The first specs for `DrylToast` and
  `DrylToastProvider`.
- **Out of scope:** the look of a toast (stays as it is); the deck of variant
  B; the task toast and `reference.Update`; an action button; errors that stay
  until closed; the double announcement of nested live regions; `linear` on
  the countdown bar (recorded as gaps in the specs, candidates for their own
  ideas); `DrylAlert`; any breaking change to `IDrylToastService`.

## Impact

- **Harness:** no new token, no new CSS keyframe, no new `AiState`, no new
  dependency. The choreography runs as Web Animations in `dryl.toast` on the
  existing `--dur-fast/med/slow/choreo`, `--ease-out`, `--ease-spring` and
  `--ease-viscous` (a shape morph, which is what that easing is for). The new
  motion itself needs `DESIGN-10` sign-off — given below.
- **Specs:** two new component specs (coverage 72 → 74), `DrylNotifications`
  gains one criterion for the bell marker; `E11`/`E1` `_Api.md` and
  `_Interop.md` gain the new options and the `dryl.toast` entry points.
- **Public API:** additive, so MINOR — `ToastOptions.Origin`
  (`ElementReference?`), `ToastOptions.KeepInInbox` (`bool`, default `true`).
  Behaviour change worth a changelog note: a toast without an origin that
  expires next to a service-driven bell now becomes an unread notification.
- **Code:** `ToastOptions.cs`, `DrylToastProvider.razor` (key fix, leave via
  JS, inbox entry), `DrylNotifications.razor` (marker), `dryl.toast` in
  `dryl.js` (entry, leave, FLIP), `.toast-host` rules in `dryl.css`. Risk: a
  toast is hidden until `dryl.toast` takes it over — a failed attach must
  reveal it from C#.

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

- 2026-11-01 — Product Owner confirmed the rule: with an origin the toast grows
  out of it, without one it comes out of the bell, without a bell it unfolds as
  a capsule. Asked to implement it straight into `release/3.16.0`. This is the
  `DESIGN-10` sign-off for the choreography.
- 2026-11-01 — Tech Lead, decided within that confirmation (open to revision
  by the Product Owner):
  - The origin is passed explicitly (`ToastOptions.Origin`); no automatic
    capture of the last clicked element, which would guess wrong after a slow
    server call.
  - The bell is found in the DOM: `DrylNotifications` marks its bell with
    `data-dryl-inbox` only in service-driven mode, the one mode where an entry
    the toast service adds actually appears behind that bell.
  - Only a toast that came out of the bell returns into it on expiry and
    becomes an inbox entry ("what came from the bell goes back to the bell").
    A toast raised by a button is direct feedback to that click; filing every
    "Saved" in the inbox would flood it. The prototype sent every expired
    toast into the bell — this narrows it. `KeepInInbox = false` opts a
    bell-born toast out.
  - Closing (×, `Close()`, `CloseAll()`) never files an entry; it keeps
    today's shrink-blur-fade.

## Bug found on the way
- Product Owner: when several toasts are open and the first expires, the
  other toasts' progress bars restart — visually only, they still close on
  time. Tech Lead's likely cause: in `DrylToastProvider` the `@key` sits on the
  `.toast-slot` div *inside* `CascadingValue`, while the loop's top-level item
  is the `CascadingValue`, which Blazor diffs by position. Removing the first
  entry hands each position its successor's content and rebuilds the DOM below
  it; the JS timer in `dryl.toast` still holds the old slot, so the close stays
  on time while the new bar starts from full. Fixed in this idea's
  implementation (`@key` on `CascadingValue`), verified in the browser.

## Open Points
- None.
