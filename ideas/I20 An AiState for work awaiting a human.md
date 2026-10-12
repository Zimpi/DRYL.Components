# An AiState for work awaiting a human

## Meta
- **State:** Draft

## Problem

Raised by the Product Owner on 2026-10-12, while shaping the calendar (I19):

> "Zu dem AiState fände ich einen neuen eigenen schon sinnvoll… gerade was so
> Human in The Loop angeht… 'to be proofed' oder sowas."

`AiState` is `None / Active / Thinking / Streaming / Generated`. Three of them
say "the AI is working here" and may only show while it is (`AI-06`); `Generated`
is a one-shot that removes itself (`AI-07`). Nothing says **"the AI is done, and
this now waits for you."**

That moment exists all over an AI-native application: an event the AI proposes
in a calendar, a tool call that needs approval, a canvas operation awaiting
confirmation, a form value the AI filled in. Today a host can only show it with
its own badge or border — outside the shared vocabulary that lets a user feel
where the AI is at work without reading a label.

## Solution Idea

A sixth `AiState`, **`Proposed`**: the AI has produced something and a human has
to accept or dismiss it. It has its own resting look — recognisably AI, but
still, so that ten proposals on one screen do not flicker.

*Its look and its exits are not yet shaped.*

## Scope
- **In scope:** the new enum member; its visual in the shared AI primitives; the
  rule change that allows a resting AI state.
- **Out of scope:** *(not yet bounded)*

## Impact

First pass by the Tech Lead, 2026-10-12.

- **Harness:**
  - **A new `AiState` and a new AI visual** — both need maintainer sign-off
    (`AI-01`, `AI-04`). The direction is given (see Decisions); the look itself
    is not signed off and should be judged in the browser, in both modes.
  - **`AI-06` has to change.** It says an aura runs only while the AI is working
    there. `Proposed` is by definition a state in which it is not.
  - **`UX-04`:** entering `Proposed` is AI activity a screen reader user has to
    learn about; the announcement needs wording.
  - **Motion:** a resting state must not loop. Whether it needs anything beyond
    the existing durations and easings depends on the look (`DESIGN-10`).
- **Specs:** every spec of a component with an `Ai` parameter describes its
  states; each either gains `Proposed` or states that it ignores it. `specs/E3
  AI/` carries the primitives.
- **Public API:** one new enum member — additive, MINOR (`REL-01`). A consumer's
  exhaustive `switch` over `AiState` gains an unhandled case; that is a compiler
  warning, not a break, and belongs in the changelog.
- **Code:** `AiState.cs`; 43 components take an `Ai` parameter and 73 files
  compare against enum members. `AuraLifecycle` decides what a state settles to.
  - **Risk — silent gaps.** A component that maps states to classes by hand and
    does not know `Proposed` renders it as nothing. Every one has to be visited,
    not assumed.

## Decisions

- 2026-10-12 (Product Owner): a **new, own `AiState`** for work awaiting a
  human, rather than an ordinary non-AI status per component.
- 2026-10-12 (Product Owner): it is named **`Proposed`**.
- 2026-10-12 (Product Owner, delegated to the Tech Lead): this is **its own
  idea**, separate from I19. It changes the whole library, and the calendar only
  needs it in its third stage.
- 2026-10-12 (Product Owner, decided for the calendar in I19): an accepted
  proposal goes to **`Generated`**, a dismissed one is removed, and **the host
  triggers both**. Whether this holds for every component is still open.

## Open Points

- The exits beyond the calendar: whether accept → `Generated` and dismiss →
  removed is the rule for every component, and what "removed" means where the
  content is a value rather than an element.
- The look, in both modes, and whether it needs a new token.
- Which components show it in the first release, and which only later.
- Whether the Agents package (tool approval, canvas operations) adopts it in the
  same step.
