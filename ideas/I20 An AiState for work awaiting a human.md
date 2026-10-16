# An AiState for work awaiting a human

## Meta
- **State:** Adopted

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

**For whom:** the end user of a DRYL application who has to decide on what the
AI produced; and the developer building that application, who today has no
shared way to show it.

## Solution Idea

A sixth `AiState`, **`Proposed`**: the AI has produced something and a human has
to accept or dismiss it.

- **Resting look — "still aurora".** The existing aurora edge
  (`.ai-aura--aurora`'s ring), frozen mid-drift: soft, asymmetric, slightly
  blurred, and motionless. Comet and breathing halo retire on entering the state.
  It fades in once (`--dur-slow`, `--ease-out`) and then nothing moves, so ten
  proposals on one screen stay calm.
- **The library never ends it.** `Proposed` stays until the host changes the
  state. Accept → `Generated`, dismiss → `None`. What dismissing means for the
  content (an element removed, a value restored) is the host's business.
- **Accept — "Welle".** On `Proposed` → `Generated` a wave of AI light spreads
  across the surface from where the human acted, and the aurora edge flares as
  the wave lands; one shot over `--dur-choreo`, then the existing `Generated`
  bloom, afterglow and self-removal. The element does not replay `Generated`'s
  entrance lift, because it is already on screen. Every other path into
  `Generated` is unchanged.
- **Where the wave starts.** The component remembers the last pointer-down on
  its own surface. If `Proposed` → `Generated` follows shortly after, the wave
  starts there; after keyboard use it starts at the focused element inside the
  surface; otherwise (e.g. "accept all" from elsewhere) at the centre. No new
  parameter.
- **Dismiss** uses the existing graceful exit (`.ai-aura--out`).
- **Screen readers.** A surface in `Proposed` carries a fixed description, read
  when the user reaches it: "AI suggestion, awaiting your decision" (English,
  like every default string in the library). There is no live announcement per
  element — seven proposals would be seven announcements in a row. A host that
  wants one ("7 suggestions") uses `DrylAiIndicator`, which gains a default
  label for `Proposed`.
- **Reduced motion:** no fade-in, no wave — straight to the calm `Generated`.

## Scope
- **In scope:** the enum member; the resting look and the accept wave in
  `dryl.css`; `AuraLifecycle` (a resting state, the hand-over to `Generated`
  without the entrance lift); the wave origin in `window.dryl.motion`; the
  description on the host; a `Proposed` label in `DrylAiIndicator`; the rule
  changes (`AI-01`, `AI-06`, `UX-04`) and the token documentation; a visit to
  every one of the 43 components with an `Ai` parameter — each either shows
  `Proposed` or its spec states that it ignores it.
- **Out of scope:**
  - a library-owned accept/dismiss API (no `OnProposalResolved`);
  - the Agents package (tool approval, canvas operations) — its own later step;
  - the calendar's use of `Proposed` — I19, stage three;
  - any change to the other paths into `Generated`, or to dismiss beyond the
    existing graceful exit;
  - localising the description — the library has no localisation story yet; its
    default strings are English throughout.

## Impact

First pass by the Tech Lead 2026-10-12; updated 2026-10-16 once the look was
chosen.

- **Harness:**
  - **New `AiState`** — signed off (Decisions, 2026-10-12). `AI-01` lists six
    members.
  - **New AI visuals (`AI-04`)** — the resting look needs no new keyframe or
    token: it is the existing aurora ring with its existing `ai-aura-drift`
    paused. The wave needs **two new keyframes**, `ai-proposed-wave` and
    `ai-proposed-flare` — signed off 2026-10-16, to be documented in
    `tokens.md`. Durations and easings are existing tokens only (`--dur-slow`,
    `--dur-choreo`, `--ease-out`).
  - **`AI-06`** gains an exception: `Proposed` is a resting state and may stay
    as long as a human has not decided.
  - **`AI-07`** is unchanged in substance; its Check gains the `Proposed` →
    `Generated` hand-over without the entrance lift.
  - **`UX-04`** gains the case of a resting state: described on the element,
    not announced live per element.
  - **Motion (`DESIGN-10`/`DESIGN-11`):** nothing loops in `Proposed`; the fade
    in and the wave are one-shots.
- **Specs:** `specs/E3 AI/` carries the primitives (`AiState`, the aura,
  `DrylAiIndicator`) and gets the new stories. Every spec of a component with an
  `Ai` parameter gains a line for `Proposed` — shown, or ignored.
- **Public API:** one new enum member, `AiState.Proposed` — additive, MINOR
  (`REL-01`). A consumer's exhaustive `switch` over `AiState` gains an unhandled
  case; a compiler warning, not a break, and it belongs in the changelog. No new
  parameters.
- **Code:** `AiState.cs`; `Ai/AuraLifecycle.cs` (a state that does not retire,
  the hand-over flag); `AiAuraCss` and `DrylAuraElements` (state class, the
  description, the wave element); `wwwroot/js/dryl.js` (`window.dryl.motion`:
  remember the pointer-down, compute the origin); `DrylAiIndicator`. 43
  components take an `Ai` parameter.
  - **Risk — silent gaps.** A component that maps states to classes by hand
    (`DrylImage`, `DrylNotifications`, `DrylTabs` draw their own aura markup)
    and does not know `Proposed` renders it as nothing. Every one is visited,
    not assumed.
  - **Risk — the origin heuristic.** "Shortly after the pointer-down" needs a
    window; it must not start the wave at a stale click. Covered by an
    acceptance criterion.

## Decisions

- 2026-10-12 (Product Owner): a **new, own `AiState`** for work awaiting a
  human, rather than an ordinary non-AI status per component.
- 2026-10-12 (Product Owner): it is named **`Proposed`**.
- 2026-10-12 (Product Owner, delegated to the Tech Lead): this is **its own
  idea**, separate from I19. It changes the whole library, and the calendar only
  needs it in its third stage.
- 2026-10-12 (Product Owner, decided for the calendar in I19): an accepted
  proposal goes to **`Generated`**, a dismissed one is removed, and **the host
  triggers both**.
- 2026-10-16 (Product Owner): **the library never ends `Proposed`.** It rests
  until the host changes the state: accept → `Generated`, dismiss → `None`.
  What dismissing means for the content — removing an element, restoring a
  previous value — is the host's business; the library cannot know it. `AI-06`
  gains an exception: `Proposed` is a resting state and may stay as long as a
  human has not decided. No library-owned accept/dismiss API.
  *Alternative rejected:* a per-component `OnProposalResolved` callback — 43
  components would gain API, and "dismiss" would need a meaning per component.
- 2026-10-16 (Product Owner): **first release** = the enum member, the
  primitive in `dryl.css`, `AuraLifecycle`, and the rule change. Every one of the
  43 components with an `Ai` parameter is visited: it either shows the primitive
  or its spec states that it ignores `Proposed` — no silent gaps. The Agents
  package (tool approval, canvas operations) follows in its own step.
  *Alternative rejected:* Agents in the same release — the strongest showcase,
  but a markedly larger and riskier release.
- 2026-10-16 (Product Owner): an acceptance criterion carries the anti-decoration
  guard — **`Proposed` is set only where an accept or dismiss action actually
  exists.** "The AI was here once" is not a proposal.
- 2026-10-16 (Product Owner): the look is decided **after seeing visual
  examples** in the browser, in both modes.
- 2026-10-16 (Product Owner): the resting look is **E, "still aurora"** — the
  existing `.ai-aura--aurora` edge, frozen mid-drift; comet and breathing halo
  retire on entering `Proposed`. No new token.
  *Alternatives rejected:* A (still hairline ring) and D (ring + "Vorschlag"
  pill) read too quietly in dark mode with many proposals on screen; B (ring +
  still halo) is more light for the same message; C (dashed gradient ring) needs
  a new primitive and is furthest from the glass vocabulary.
- 2026-10-16 (Product Owner): the accept transition `Proposed` → `Generated` is
  **"Welle"**: a wave of AI light (`--ai-a`/`--ai-b`, scaled by `--ai-strength`)
  spreads from where the human clicked across the surface; the aurora edge
  flares as the wave lands. One shot over `--dur-choreo` with `--ease-out`, then
  the existing `Generated` bloom, afterglow and self-removal. Only for
  `Proposed` → `Generated`; every other path into `Generated` is unchanged.
  Under `prefers-reduced-motion` it goes straight to the calm `Generated`.
  *Alternatives rejected:* Erwachen (thawing aurora), Scharfstellen (edge pulls
  into focus), Umlauf (one comet lap), Besiegeln (stamp press).
- 2026-10-16 (Tech Lead, from the variants): an element already on screen does
  **not** replay `Generated`'s entrance lift (fade from 0 + 8px) when it comes
  from `Proposed`.
- 2026-10-16 (Product Owner): **sign-off (`AI-04`)** for the two new keyframes
  `ai-proposed-wave` and `ai-proposed-flare`, documented in `tokens.md`.
- 2026-10-16 (Product Owner): **the wave's origin is found by the component**:
  the last pointer-down on its own surface shortly before the change, else the
  focused element inside it, else the centre. No new API; a small addition to
  `window.dryl.motion`.
  *Alternatives rejected:* the host passes the origin as a parameter (API on 43
  components, contradicts "no library-owned accept"); always the centre (loses
  what makes the wave worth having — the answer comes from your own hand).
- 2026-10-16 (Product Owner): **screen readers (`UX-04`)**: a fixed description
  on the element in `Proposed` ("AI suggestion, awaiting your decision"), no
  live announcement per element; a host-level announcement goes through
  `DrylAiIndicator`, which gains a `Proposed` default label. `UX-04` is extended
  by this case.
  *Alternatives rejected:* a live announcement per element (a flood with many
  proposals); no announcement of its own (invisible to screen-reader users
  whenever a host forgets it).

## Resulting specs

Specified on 2026-10-16:

- [`../specs/E1 Foundation/_Api.md`](../specs/E1%20Foundation/_Api.md) — `AiState`
  with its six members, the resting kind, the host-resolves rule and the
  anti-decoration guard.
- [`../specs/E3 AI/F7 DrylAuraElements.md`](../specs/E3%20AI/F7%20DrylAuraElements.md) —
  the resting look, the accept wave, the description for assistive technology.
- [`../specs/E3 AI/_Interop.md`](../specs/E3%20AI/_Interop.md) — how `dryl.js`
  aims the wave without an interop call.
- [`../specs/E3 AI/F4 DrylAiIndicator.md`](../specs/E3%20AI/F4%20DrylAiIndicator.md) —
  the "Proposed" label, the resting pill, the announcement route.
- One criterion block each in the 28 further component specs that carry an AI
  section. Components without a spec yet (phase C) get theirs when that spec is
  written; the implementation visits them all the same.

Three statements were sharpened by the spec rather than taken over as written:

- **The wave's origin needs no time window.** The idea said "shortly after the
  pointer-down". The spec uses the most recent pointer-down in the whole
  document, and only if it lies inside the surface and no key press followed. A
  stale click inside a card is overwritten by the next click anywhere — on an
  "accept all" button outside it, say — which is the case the window was meant
  to catch, without guessing a duration.
- **Three components ignore `Proposed`.** `DrylSpinner`, `DrylSkeleton` and
  `DrylProgress` report work in progress or stand in for content that has not
  arrived, so nothing on them can be accepted or dismissed. They render it like
  `None`.
- **`DrylNotifications` rows show the resting look but no wave**, for the same
  reason they play no completion wash today (a recorded gap of that spec).

## Open Points

*(none)*
