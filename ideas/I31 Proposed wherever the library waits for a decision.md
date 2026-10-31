# Proposed wherever the library waits for a decision

## Meta
- **State:** Adopted
- **Specs:** `specs/E15 Agent Inputs/F1 DrylAiField.md`,
  `specs/E10 Navigation/F4 DrylCommandPalette/S4 Commands, arguments and the AI seam.md`,
  `specs/E1 Foundation/_Api.md`, `specs/E14 Agent Canvas/F1 DrylCanvasDock.md`,
  `specs/E14 Agent Canvas/F2 DrylAiCanvas.md`

## Problem

`AiState.Proposed` (I20, core 3.7.0) is the sixth member of the one AI vocabulary.
I20 visited every component with an `Ai` parameter in the core package, and I21
brought the state to tool approval in the Agents package. A sweep on 2026-10-31
found the places neither reached:

- **`DrylAiField` waits for a decision and says `Generated`.** After a run it
  offers "accept" and "reject" chips and rests until the user picks one — the
  exact case `Proposed` was made for. It sets `Generated` instead, so the
  suggestion blooms and retires as if it were already accepted, and accepting it
  plays nothing. Its review state is the library's own showcase of a human
  decision, and it shows the wrong one.
- **`IDrylAiOperation` cannot propose.** The handle `IDrylAiActivityService.Begin`
  returns offers `Thinking()`, `Streaming()` and `Generated()`. A host driving a
  flow through the service reaches `Proposed` only through `Set(key, ...)`,
  beside the handle it is holding.
- **`DrylCommandPalette`'s result panel ignores it.** The palette's shell aura
  shows `Proposed` through the shared primitive, but the panel below the list
  maps states by hand: under `Proposed` it appears without its reveal, and on
  accept (`Proposed` → `Generated`) it replays the entrance of content that is
  already on screen — what I20 rules out for every other surface.
- **Two Agents specs are silent.** `DrylAiCanvas` and `DrylCanvasDock` follow a
  `DrylCanvasRun`, which never reaches `Proposed`; their specs do not say so,
  which is the silent gap I20 set out to close.
- **Documentation lists five states** — `harness/patterns.md`, `harness/ai.md`,
  the navigation API — and the website's state demos show five.

## Solution Idea

1. **`DrylAiField` rests at `Proposed` in review.** Accept → `Generated`: the
   wrapped input plays the accept wave and the existing bloom, then retires.
   Reject → `None` through the existing graceful exit. Nothing else in the flow
   changes.
2. **`IDrylAiOperation.Proposed()`**, beside the other three. It ships as a
   default interface implementation that sets nothing, as `IDrylMorph` did with
   `BeginNavigation`, so an existing implementer keeps compiling; the library's
   own operation sets the key to `Proposed`.
3. **The palette panel learns `Proposed`**: it reveals once on entering it, and
   on `Proposed` → `Generated` it stays as it is instead of replaying.
4. **Specs say where `Proposed` cannot arise** — the canvas and the dock.
5. **Docs and demos list six states.**

## Scope

- **In scope:** the four points above in the library; `harness/patterns.md`,
  `harness/ai.md` and the navigation API in the docs; every website state demo
  with a state picker, plus one demo that walks propose → accept / dismiss.
- **Out of scope:**
  - `DrylHandoffTrace` and the orchestrated runs it shows — approval in
    `StartSequential`/`StartConcurrent` is out of scope since I21, so a step
    never legitimately rests at `Proposed`;
  - `DrylVoiceOrb` — a voice session has no approval step;
  - `DrylAiField`'s German default strings. They break the library's
    English-default rule, but changing them changes what every existing
    consumer shows; recorded in its spec as a limit, not fixed here;
  - any new visual — the look, the wave and the description are I20's.

## Impact

- **Harness:** no new token, keyframe, `AiState` or dependency. `AI-04` is not
  touched: every visual is the existing I20 primitive.
- **Public API:** one new interface member with a default implementation,
  `IDrylAiOperation.Proposed()` — additive, MINOR (`REL-01`).
- **Behaviour:** `DrylAiField` shows `Proposed` where it showed `Generated`; a
  consumer that styled the review state by `.ai-generated` sees `.ai-proposed`
  instead. Changelog entry under *Changed*. MINOR for the Agents package.
- **Specs:** `DrylAiField` has no spec; it gets its first one
  (`specs/E15 Agent Inputs/F1 DrylAiField.md`, and its baseline entry goes).
- **Code:** `Ai/IDrylAiOperation.cs`, `Ai/DrylAiActivityService.cs`,
  `DrylCommandPalette.razor` + `.razor.css`, `Field/DrylAiField.razor`; tests
  beside them.

## Decisions

- 2026-10-31 (Product Owner): the sweep's findings are adopted as a whole —
  docs, the website demos and the library gaps — "mach das alles fertig".
- 2026-10-31: **the `DrylAiField` review state is `Proposed`**, not a new state
  or parameter. Accept → `Generated`, reject → `None`, exactly as I20 defines the
  host's side.
- 2026-10-31: **`IDrylAiOperation.Proposed()` has a default implementation.**
  The interface has been public since 1.x and the API is frozen; a required
  member would break an implementer (MAJOR). The default sets nothing, matching
  the `IDrylMorph.BeginNavigation` precedent.
- 2026-10-31: **`DrylHandoffTrace` and `DrylVoiceOrb` stay as they are.** Neither
  can receive `Proposed` from a supported flow; teaching them a label for a state
  they never see would be code without a case.
- 2026-10-31: **German defaults of `DrylAiField` are recorded, not changed** —
  see Scope.

## Open Points

*(none)*
