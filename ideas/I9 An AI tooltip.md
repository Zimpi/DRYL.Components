# An AI tooltip

## Meta
- **State:** Draft

## Problem

Raised by the Product Owner on 2026-08-20, alongside the five defects the
`E7 Feedback` specs turned up:

> "Außerdem soll ToolTip Ai und Aura bekommen. Angenommen man möchte mit KI zu
> einem Element was erklären? Dann wäre ein ToolTip ja eigentlich Top dafür
> oder?"

`DrylTooltip` is the only one of the eight Feedback components that takes
neither `Ai` nor `Aura`. The `F2` spec records that as a deliberate decision
rather than an omission, and the Product Owner is questioning the decision — on
a concrete use case: **an AI explaining an element of the UI to the user.**

The use case is real and the library has nowhere good to put it today. A short
explanation attached to a specific control, produced by a model, is exactly the
kind of thing DRYL claims to be for.

## The Tech Lead's reading

The instinct is right and the target is wrong, and the two halves need
separating before anything is built.

**Where the instinct is right:** attaching an explanation to the element it is
about, rather than to a panel somewhere else on the page, is the correct shape.
And there *is* a cheap, honest piece of this: marking a tooltip as
AI-provenance, so the user knows that the words they are about to read were
written by a model and not by the application's authors. That is what the aura
vocabulary exists for.

**Where the target is wrong:** the thing being aimed at is `DrylTooltip`'s
bubble, and the bubble is the wrong surface for AI content — for three reasons
that are properties of what a tooltip *is*, not bugs to be fixed.

1. **The bubble is shared and singular.** There is exactly one `.tt-portal`
   element per page, created lazily by `dryl.tooltip` and reused by every
   tooltip on it (see [`../specs/E7 Feedback/_Interop.md`](../specs/E7%20Feedback/_Interop.md)).
   That is the design that makes a toolbar of thirty triggers cost nothing. But
   the AI aura is per-surface DOM — `.ai-aura-ring`, `.ai-aura-comet`,
   `.ai-aura-glow` as children of the surface — and a single shared bubble
   cannot carry a per-trigger aura without JS building that markup itself. The
   aura vocabulary would then exist in a second place, written in JavaScript,
   which is exactly what `AI-02` and `DESIGN-13` are there to prevent.

2. **The bubble is decorative and transient — by contract.** It is
   `aria-hidden`, it is `pointer-events: none`, its content is set as
   `textContent`, and it hides on `pointerout`, on `pointerdown` and on scroll.
   An AI explanation put there is: never announced to a screen reader, never
   selectable, never copyable, and gone the moment the pointer moves. It also
   fails WCAG 1.4.13 (*Content on Hover or Focus*), which requires such content
   to be hoverable, persistent and dismissible — today's bubble is none of the
   three. The `F2` spec already records that a tooltip must never be the only
   place a piece of information exists; AI-generated explanation is by
   definition information that exists nowhere else.

3. **Streaming into it cannot work.** The placement routine measures the bubble
   once, flips it once and clamps it once. A bubble that grows token by token
   would resize and reposition under a pointer that has to stay still to keep it
   open. This is not a matter of effort — a growing tooltip is a bad
   interaction even when implemented perfectly.

There is also a fourth point that is about product rather than mechanics: a
tooltip is a *label*. It says what a control is. An explanation is a different
speech act, it is longer, and users expect to be able to keep it open while they
read it.

## Solution Idea

Split the idea in two, and decide them separately.

### Option A — `Ai` and `Aura` on `DrylTooltip`, provenance only

The trigger wrapper takes the aura, the bubble takes a static provenance mark
(the sparkle glyph and an accent-tinted border) driven by a data attribute the
JS copies onto the shared bubble while that trigger owns it.

- **Pro:** cheap; no new vocabulary; makes the eighth Feedback component
  consistent with the other seven; answers "was this written by AI?" honestly.
- **Pro:** the aura on the wrapper is real per-instance DOM, so it uses the
  shared vocabulary unchanged.
- **Con:** the bubble's own treatment is a static mark rather than the living
  aura, so it is *less* than what `Ai` means everywhere else — a parameter that
  under-delivers relative to its name.
- **Con:** does not address the actual use case at all.

### Option B — an explanation surface, built on `DrylPopover`

The use case gets the component it actually needs: a small affordance next to an
element that opens a **popover** with the model's explanation. `DrylPopover`
already portals, positions, flips, traps focus, closes on `Escape` and animates
out; the content can be `DrylMarkdown`, can stream, can be selected and copied,
and can carry the full aura because it is a real per-instance surface.

- **Pro:** every objection above disappears, because the surface is built for
  content rather than for labels.
- **Pro:** reuses two existing components rather than inventing a mechanism.
- **Con:** a new component — a spec, a demo page, a catalog entry, a name.
- **Con:** more than the Product Owner asked for.

### Option C — both, in that order

Option A closes the consistency gap on `DrylTooltip` and is small. Option B
serves the use case that prompted the question. They do not conflict: a control
can have an ordinary label on hover *and* an explanation on demand.

**The Tech Lead recommends C, with B carrying the weight** — and explicitly
recommends **against** putting the explanation itself in the tooltip bubble
under any option.

## Scope

Settled on 2026-08-20: **Option A only.** The idea is now exactly "`DrylTooltip`
gains `Ai` and `Aura`, and says who wrote the words".

- **In scope:**
  - `Ai` and `Aura` parameters on `DrylTooltip`, with the same types and
    defaults every other AI-capable component uses.
  - The living aura on the **trigger wrapper**, which is real per-instance DOM
    and therefore uses the shared vocabulary unchanged.
  - A **static provenance mark** on the bubble — the sparkle glyph and an
    accent-tinted border — driven by a data attribute the shared bubble picks up
    from the trigger that currently owns it.
  - Clearing that attribute on hide, so no tooltip inherits the previous one's
    provenance.
- **Out of scope:**
  - AI-generated explanation *content* in the tooltip bubble, streamed or
    otherwise. The bubble stays a label.
  - Making the bubble hoverable, persistent, selectable or announced.
  - A second implementation of the aura vocabulary in JavaScript.
  - The explanation surface (Option B). Not rejected — deferred, and its shape
    is already decided for whenever it is raised: **its own component**, not a
    mode of `DrylPopover`, so a primitive is not loaded up with subject matter
    it deliberately has none of. That will be its own idea document.

## Impact

*(Tech Lead, `IDEA-05`. Option B's rows are kept for whenever it is raised, but
only Option A is in scope.)*

### Harness

- **Option A:** no new token, no new animation, no new `AiState`, no new
  dependency. The static provenance mark reuses `--accent-line`, `--accent-soft`
  and the existing sparkle icon. **No blocker.** The one judgment call it did
  raise — whether a surface carrying a *reduced* AI treatment is acceptable
  under `AI-02`'s "one shared visual vocabulary", or whether that makes the
  vocabulary two things — was put to the maintainer as an `AI-04`-shaped
  question even though no new visual is invented, and **signed off on
  2026-08-20**: the mark is acceptable precisely because it is not a second
  vocabulary but a smaller statement in the same one, and because the
  alternative would have put the real vocabulary in a second implementation.
- **Option B:** no new token or animation expected; it composes `DrylPopover`,
  `DrylMarkdown` and the existing aura. **No blocker expected**, to be
  re-checked once its behaviour is concrete.

### Specs

- **Option A:** rewrites the AI-mode decision in
  [`../specs/E7 Feedback/F2 DrylTooltip.md`](../specs/E7%20Feedback/F2%20DrylTooltip.md),
  which currently records the *opposite* decision with its reasoning, and adds
  criteria to its "Appearance" and a new "AI mode" section. Touches
  [`../specs/E7 Feedback/_Api.md`](../specs/E7%20Feedback/_Api.md) (the "AI
  parameters" section counts the six components that carry both) and
  [`../specs/E7 Feedback/_Interop.md`](../specs/E7%20Feedback/_Interop.md) (the
  shared bubble would gain a per-trigger attribute).
- **Option B:** a new `F{n}` in a category to be decided. `E3 AI` is the
  likely home if it is an AI-native component in its own right; `E11 Surfaces`
  if it is a popover variant. That choice follows the source folder
  (`SPEC-02`), so it is a code-layout decision as much as a spec one.

### Public API

- **Option A:** two new parameters on `DrylTooltip` — `Ai` and `Aura`, with the
  types and defaults every other component uses. Additive, so MINOR
  (`REL-01`). No existing member changes.
- **Option B:** a new component with its own surface. Additive, MINOR.

### Code

- **Option A:** `DrylTooltip.razor` (two parameters, the aura lifecycle and its
  `IDisposable`, a data attribute), `dryl.js` (`dryl.tooltip` copies the
  provenance attribute onto the shared bubble on show and clears it on hide),
  `dryl.css` (the bubble's provenance treatment). The risk sits in the JS: the
  bubble is shared, so a stale attribute left behind on hide would mark an
  unrelated tooltip as AI-written. That is a correctness bug that would be
  invisible in a screenshot and needs a test.
- **Option B:** a new component under `code/DRYL.Components/`, composing
  existing ones. The known risks are `DrylPopover`'s recorded debt, which it
  would inherit — see
  [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md).

## Decisions

- 2026-08-20 (Tech Lead): the idea is **not** nodded through as "add `Ai` to
  `DrylTooltip`". The use case that motivates it and the consistency gap it
  names are two different things and are separated above.
- 2026-08-20 (Product Owner): **Option A only.** `DrylTooltip` gets `Ai` and
  `Aura`; the explanation surface is not built now. The consistency gap is worth
  closing on its own, and the use case can wait for a surface built for it.
- 2026-08-20 (Product Owner): **the static provenance mark is enough.** The
  bubble marks who wrote the words; it does not carry the living aura. The
  JS-side reimplementation the alternative would need is refused, so the aura
  vocabulary stays in exactly one place (`AI-02`, `DESIGN-13`).
- 2026-08-20 (Product Owner): **if the explanation surface is ever built, it is
  its own component**, not a mode of `DrylPopover`. Recorded here so the
  decision is not re-litigated when it is raised.

## Open Points

*(none — awaiting the Product Owner's explicit confirmation of this final
version, the last box of `IDEA-06`, before the state moves to `Ready`.)*
