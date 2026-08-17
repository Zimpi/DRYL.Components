# Keyboard access for the bare popover

## Meta
- **State:** Adopted

## Problem

A `DrylPopover` used directly is not operable by keyboard. Three findings, all
measured on `/components/popover` and all recorded as debt in
[`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md):

1. **`Escape` is inert for a popover nobody focused into.** `CloseOnEscape`
   defaults to `true` and reads as a promise, but the handler is bound to the
   panel. Measured: focus on the trigger, `Escape`, panel still open.
2. **The portalled panel drops out of the tab order.** Measured: with the panel
   open, `Tab` from the trigger moves to the next control on the page and the
   panel stays open behind it. The panel is the last child of `<body>` while
   portalled, so its content sits at the very end of the tab order.
3. **Focus is returned nowhere on close.** Measured: after an `Escape` that
   closed the panel from inside it, `document.activeElement` is `<body>`.

**Target role:** the Blazor developer who places a bare `DrylPopover` — a filter
panel, an info bubble, a settings popover — and the keyboard or screen-reader
user of whatever they build. Every *library* consumer of the popover already
covers all three in its own module, which is exactly why this went unnoticed:
the primitive is the only place where the gap is visible, and the primitive is
the one the library itself never uses bare.

**What happens today without the feature.** Nothing tells the developer. The
parameter's own doc comment says "when Escape is pressed inside it", which is
accurate and easy to read past; `CloseOnEscape="true"` looks like the job is
done. `UX-01` is then satisfied only by what each consumer adds, and two of the
library's own consumers got it wrong before it was caught.

## Solution Idea

Three findings, three separate decisions — they share a component, not a cause,
and each could go a different way. Options and a recommendation for each; the
Product Owner decides (`IDEA-02`).

### 1. `Escape` that works before focus enters the panel

- **A — A document-level `Escape` listener while open**, in `dryl.popover`
  beside the existing outside-press listener, invoking the same `Close`.
  Catches every case including focus parked somewhere unrelated. The cost is
  that the popover then competes for a key it does not own: a dialog above it
  and a popover below it would both take one `Escape`, and nothing in the
  library arbitrates that today.
- **B — Handle `Escape` on the anchor as well as the panel.** Covers the case
  that actually occurs — the user opened it from the trigger and never left —
  without claiming the key globally. **Precedent exists and is recent:** both
  pickers added exactly this in 2.23.0, "as a second line of defence `Escape`
  is now handled on the input as well, so the field is never a dead end".
- **C — Leave the behaviour, fix the promise.** Rename or re-document
  `CloseOnEscape` so it says what it does. Post-1.0 a rename is MAJOR
  (`REL-01`), so realistically this is a doc-comment change and a criterion.

**Recommendation: B.** It repeats a pattern the library chose four days ago for
the same class of bug, it is contained to this component, and it leaves the
global-`Escape` question — which is really a question about layering dialogs,
popovers and the command palette — unopened rather than answered by accident.

### 2. The panel's place in the tab order

- **A — Cycle `Tab` inside the panel while it is open.** `drylPanelKeys` in
  `dryl.js` already implements exactly this and is already installed on this
  component's panel node by both pickers; this would apply it one level down,
  for every popover, rather than per consumer. Right for a panel that is a
  `dialog`. Wrong for a small non-modal info bubble, where trapping the user is
  worse than the gap.
- **B — Linear order: `Tab` from the trigger moves into the panel, and `Tab`
  past its last control closes the popover and moves on.** This is what a
  non-modal popover should do and what a user expects. It needs "the next tab
  stop after the trigger" computed in JS, which is real work and has no
  precedent in the library.
- **C — Trap only when the panel has a container role** (`dialog`), leave it
  alone otherwise. Splits the difference along a line that already exists in
  the API (`PanelRole`), at the cost of two behaviours to document.

**Recommendation: C.** `PanelRole` is already the parameter that says what kind
of thing the panel is, and the library's own dialogs and pickers are exactly
the ones that want the trap. A roleless info bubble keeps today's behaviour,
which for it is not obviously wrong.

### 3. Focus returned on close

- **A — Remember what was focused when the popover opened, and restore it on
  close, but only if focus is still inside the panel at that moment.** The
  guard is the whole design: a user who clicked away has already decided where
  focus belongs, and the pickers were given precisely this rule in 2.23.0 —
  "on the click-outside path the focus is **not** taken back".
- **B — A `RestoreFocus` parameter.** Explicit, and one more thing to get
  right; a default has to be chosen anyway, so this mostly defers option A by
  one step.
- **C — Leave it to consumers.** Defensible: the component genuinely does not
  know which element deserves focus. It is also how both pickers got it wrong.

**Recommendation: A**, and specifically without a parameter. Every library
consumer that restores focus today restores it to its own trigger, which is
what was focused when the popover opened — so the guarded restore agrees with
all of them rather than fighting them, and the consumers' own calls become
redundant rather than contradictory.

## Scope

- **In scope:** the three behaviours above on `DrylPopover`; the criteria and
  the `Recorded debt` entries in `specs/E11 Surfaces/F1 DrylPopover.md` that
  each one retires; whatever `dryl.popover` needs for them; the effect on the
  six library consumers that already do this work themselves.
- **Out of scope:** who wins one `Escape` when a dialog, a popover and the
  command palette are open at once — a layering question that belongs to the
  whole overlay stack, not to this component. `DrylMenu`'s own `Tab` gap
  (`E10 Navigation/F1 DrylMenu`, `Recorded debt`), which is the menu's to fix.
  Any change to `DrylPresence`, the exit animation, or the portal itself.

## Impact

- **Harness:** no new token, animation, `AiState` or dependency, so no
  `IDEA-05` blocker on any of the three. `UX-01` is the rule each of them is
  about, and all three move it from red to green for the bare primitive.
- **Specs:** `specs/E11 Surfaces/F1 DrylPopover.md` — three `Recorded debt`
  entries retired and replaced by criteria. `specs/E10 Navigation/F1 DrylMenu.md`
  names the `Escape`/focus pairing as a convention it took on; option 3A would
  make it a mechanism, and that sentence changes with it.
- **Public API:** option 3B would add a parameter; the recommended options add
  none. No rename, so no `REL-01` MAJOR on the recommended path. What changes
  either way is behaviour six library components currently implement
  themselves — none of it contradictory under the recommendations, but all of
  it needing to be re-measured rather than assumed.
- **Code:** `DrylPopover.razor` (an anchor-level key handler for 1),
  `dryl.popover` (`drylPanelKeys` install/uninstall for 2, the focus memory for
  3) and `dryl.js`'s `drylPanelFocus` helper. Risks, in the order they are
  likely to bite: a double focus restore fighting a consumer's own (guarded by
  3A's condition); `drylPanelKeys` being installed twice on a picker's panel,
  which already installs it; and the exit window from `I4`, during which the
  panel is still on screen and focus must not be handed back too early.

## Decisions

- 2026-08-17: Raised as an idea rather than fixed alongside the exit animation.
  All three are behaviour changes to the primitive eight components stand on,
  and `I4`'s scope explicitly excluded them so that one animation decision would
  not carry three unrelated fixes.
- 2026-08-17: **Finding 1 — option B.** `Escape` is handled on the anchor as
  well as the panel. The global-`Escape` question stays closed.
- 2026-08-17: **Finding 2 — option C.** `Tab` cycles inside the panel only when
  `PanelRole` names a container role, and `dialog` is that role. A panel with no
  role, or with `menu` or `listbox`, keeps today's behaviour — those two are
  already handled by the components that set them, and trapping `Tab` under
  `DrylMenu`, which closes on `Tab`, would put two answers on one key.
- 2026-08-17: **Finding 3 — option A.** The popover remembers what was focused
  when it opened and hands it back on close, but only while focus is still
  inside the panel. No `RestoreFocus` parameter.
- 2026-08-17: Two things the options did not name, decided while checking the
  code they land in. `drylPanelKeys.install` refuses to install twice, so
  whichever of the popover and a picker calls it first decides whether the
  navigation keys are suppressed — the pickers happen to win today because a
  parent's `OnAfterRenderAsync` runs before its child's, which is a guarantee
  about Blazor, not about this feature. The helper is made order-independent
  instead. And the popover now **removes** that listener when it closes, which
  retires the `Recorded gap` about the library's one listener with no teardown
  path (`CODE-05`) rather than adding a second owner to it.
- 2026-08-17: The focus is handed back when the close is **requested**, not
  when the exit animation finishes. `I4` put roughly `--dur-fast` between those
  two moments, and a keyboard user should not wait it out with focus parked on
  a panel that is fading away.

- 2026-08-17: **Adopted.** Carried into
  [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md),
  where all three findings are now criteria and their `Recorded debt` entries
  are retired, and reconciled in
  [`../specs/E10 Navigation/F1 DrylMenu.md`](../specs/E10%20Navigation/F1%20DrylMenu.md),
  which had described the popover as returning focus nowhere. Implemented in
  `837b381`.
- 2026-08-17: One thing the options got wrong, recorded because it is the kind
  of mistake that repeats. Finding 2's option C was written as "cycle `Tab`
  inside the panel", and installing exactly that changed nothing at all when
  measured: a listener on the panel cannot see a key pressed on the trigger,
  and the finding was about a panel nobody had focused into. The trap was only
  ever half the answer; the entry is the other half. The option was reasoned
  from the mechanism that already existed rather than from the measurement that
  produced the finding.
- 2026-08-17: One new item of debt comes with finding 2 and is recorded in the
  spec rather than smoothed over: a `dialog` panel now traps `Tab`, and a
  consumer that sets `PanelRole="dialog"` on something that is not really a
  dialog gets that behaviour without being warned.

## Open Points

- None.
