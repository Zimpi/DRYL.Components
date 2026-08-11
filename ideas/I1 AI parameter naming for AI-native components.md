# AI parameter naming for AI-native components

## Meta
- **State:** Draft

## Problem

`AI-03` in [`../harness/ai.md`](../harness/ai.md) states, without qualification, that
a component's AI-mode parameter is always named `Ai`, is of type `AiState`, and
defaults to `AiState.None` — so AI styling is opt-in and existing consumers see no
change. 36 declarations in the library follow this. Eight do not:

**Seven deviate on the name only** — the parameter is called `State` or `SettleTo`,
the default is correctly `AiState.None`:
`DrylToolCall`, `DrylToolCallGroup`, `DrylCanvas`, `DrylAiStream`, `DrylAiScope`,
`DrylAiGenerate`, `DrylAiBuild`.

**`DrylAiIndicator` deviates on both** — parameter `State`, default `AiState.Active`,
so it renders a visible AI pill out of the box.

Today the rule and the code disagree and nothing decides between them. That is
tolerable while it is written down, but it stops being tolerable in phase C: every
component spec documents its own public API, so 127 specs are about to record whichever
answer is true at the time. Deciding afterwards means rewriting specs and, past 1.0,
taking a MAJOR break (`REL-01` in [`../harness/releasing.md`](../harness/releasing.md))
with finished specs already naming the old parameter.

Without a decision, the likeliest outcome is drift: the next AI-native component copies
whichever neighbour its author happened to read.

## Solution Idea

*Open — this is the decision to be made. Two candidate directions, neither adopted:*

**A — Carve out AI-native components in `AI-03`.**
The argument: `AI-03`'s stated purpose is that AI styling is *opt-in* so existing
consumers see no change. For a component whose entire reason to exist is displaying AI
state, there is nothing to opt into, and `AiState.None` as a default would make
`DrylAiIndicator` render nothing at all. Under this direction the exemption must come
with a binding definition of "AI-native", or the exemption grows by itself — the
plausible test is a component that has no meaningful appearance with AI absent.

**B — Rename the eight to conform.**
The argument: one rule, no categories, nothing to adjudicate per component. The cost is
a MAJOR break for eight public parameters, and `DrylAiIndicator` still needs an answer
for its default, since `None` would make it useless.

A split is possible — the seven name-only cases and `DrylAiIndicator` are not the same
problem — but a split answer needs its own justification, not just convenience.

## Scope

- **In scope:** the name and default of the `AiState` parameter on the eight named
  components; the wording of `AI-03`; if a carve-out is chosen, the binding definition
  of "AI-native".
- **Out of scope:** the `AiState` enum itself and its five values (`AI-01`); the aura
  primitives (`AI-02`); the aura lifecycle (`AI-06`, `AI-07`); every other component's
  `Ai` parameter.

## Impact

- **Harness:** `AI-03` in `../harness/ai.md` — either gains a scoped exemption with a
  definition, or is left unchanged and the code moves to it. `AI-05` is untouched
  either way. No new token, animation, `AiState` value or dependency is involved, so no
  `IDEA-05` blocker applies.
- **Specs:** none exist yet. This decision determines what phase C records in the
  `## Public API` section of eight component specs, which is why it comes first.
- **Public API:** direction B renames eight public parameters — MAJOR under `REL-01`,
  with `@bind-` call sites in `DRYL.Website` to follow. Direction A changes no API.
- **Code:** direction A touches no component code. Direction B touches the eight
  components plus every consumer of them.

## Decisions

- 2026-08-11: Recorded as an idea rather than settled in `AI-03`. An implementer had
  written the exemption straight into the rule during the harness build; that was
  rejected in review as an implementer amending a binding rule to make a red check look
  green. The rule stays as the source states it and the deviations are recorded as
  pre-existing violations until the Product Owner decides.

## Open Points

- Direction A or B, or a justified split between the seven and `DrylAiIndicator`.
- If A: the binding definition of "AI-native" that keeps the exemption from spreading.
- If A: whether `DrylAiIndicator`'s `AiState.Active` default is right, or whether it
  should default to a different non-`None` value.
- If B: the target version for the break, and whether it lands before or after 1.0.
