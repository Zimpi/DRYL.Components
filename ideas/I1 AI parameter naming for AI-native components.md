# AI parameter naming for AI-native components

## Meta
- **State:** Ready

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

**Direction C — narrow `AI-03` to the parameter it actually means, rather than exempting
a category of component.**

Reading the eight declarations shows they are not one deviation repeated eight times.
They are four different things, and only one of them is what `AI-03` governs:

| Component | Parameter | What it actually is |
|---|---|---|
| `DrylToolCall`, `DrylToolCallGroup`, `DrylCanvas` | `State`, default `None` | A genuine opt-in switch. Exactly what `AI-03` means. |
| `DrylAiStream`, `DrylAiGenerate`, `DrylAiBuild` | `SettleTo`, default `None` | Not a switch. The state to settle to *after* the `AiState.Generated` reveal; the live state comes from the stream itself. |
| `DrylAiScope` | `AiState? State`, no default | A broadcast override. `null` means "follow `IDrylAiActivityService`", `None` means "actively force AI off" — two different things. A default of `AiState.None` would break the component. |
| `DrylAiIndicator` | `State`, default `Active` | The value being displayed, not an opt-in. With `None` the component renders nothing at all. |

`AI-03` governs **the opt-in parameter** — the one that turns AI styling on for an
otherwise ordinary component. Where the parameter is something else, the rule was never
about it.

The binding test is a property of the component, checkable by reading it, not a
self-declared category:

> **Does the component still render something meaningful with `AiState.None`?**
> Yes → the parameter is an opt-in; it is named `Ai` and defaults to `AiState.None`.
> No → the value is the component's content or its control input; it carries its own
> descriptive name, and its default is that component's decision.

This is the test `I1` had already noted as plausible ("a component that has no
meaningful appearance with AI absent"), without the category "AI-native" that, as this
document argued, would grow by itself.

**Consequence:** five of the eight stop being violations because they are not opt-ins.
Three are genuine violations and are renamed `State` → `Ai`.

## Scope

- **In scope:** the name and default of the `AiState` parameter on the eight named
  components; the wording and the `Check:` line of `AI-03`; the binding test above.
- **Out of scope:** the `AiState` enum itself and its five values (`AI-01`); the aura
  primitives (`AI-02`); the aura lifecycle (`AI-06`, `AI-07`); every other component's
  `Ai` parameter.

## Impact

- **Harness:** `AI-03` in `../harness/ai.md` is narrowed to the opt-in parameter and
  gains the `AiState.None` test. Its `Check:` can no longer be pure `grep` — "is an
  opt-in" is not greppable — so, as with `CODE-01`, the check names the legitimate
  non-opt-ins explicitly and `Enforced` becomes `grep` + `review`. A named list is
  verifiable; a category is not. No new token, animation, `AiState` value or dependency
  is involved, so no `IDEA-05` blocker applies.
- **Specs:** none exist yet. This decision determines what phase C records in the
  `## Public API` section of eight component specs, which is why it comes first. The
  five non-opt-ins are documented with their own parameter names and a one-line reason.
- **Public API:** three renames (`DrylToolCall`, `DrylToolCallGroup`, `DrylCanvas`).
  `DRYL.Components` is at `2.20.1`, so a removal would be MAJOR under `REL-01`. It is
  therefore staged: `Ai` is added as the parameter, `State` stays as an `[Obsolete]`
  alias that delegates to it — MINOR now, removal in the next planned `3.0.0`.
  Consumers get a compiler warning naming the replacement instead of a break.
- **Code:** three components in `code/DRYL.Components/Components/AI/`, plus their
  `@bind-`/attribute call sites in `DRYL.Website`, which keep working through the alias.
  The other five components are not touched.

## Decisions

- 2026-08-11: Recorded as an idea rather than settled in `AI-03`. An implementer had
  written the exemption straight into the rule during the harness build; that was
  rejected in review as an implementer amending a binding rule to make a red check look
  green. The rule stays as the source states it and the deviations are recorded as
  pre-existing violations until the Product Owner decides.
- 2026-08-11: **Direction C adopted** over A (exempt AI-native components) and B (rename
  all eight). A was rejected because "AI-native" is a self-declared category with no
  checkable boundary — this document's own concern about the exemption spreading. B was
  rejected because renaming `SettleTo` to `Ai` would misstate what the parameter does,
  and `DrylAiScope` would still need a special case for its `null` default. C keeps one
  rule, adds no category, and its boundary is a property of the component.
- 2026-08-11: **The binding test is `AiState.None`.** A component that still renders
  meaningfully with `None` has an opt-in and must name it `Ai`. One that renders nothing
  does not, and names its parameter for what it is.
- 2026-08-11: **`DrylAiIndicator` keeps `State` and its `AiState.Active` default.** It
  renders nothing with `None`; the parameter is the value it displays, not a switch.
  Under the test it is not an `AI-03` case at all — no exemption is needed for it.
- 2026-08-11: **The three renames are staged, not immediate.** `Ai` is added and `State`
  becomes an `[Obsolete]` delegating alias — MINOR now, removal in the next planned
  `3.0.0`. A sole MAJOR spent on three parameter names is not worth forcing every
  consumer to migrate at once, and the compiler warning carries the migration hint.

## Open Points

*(none — Definition of Ready met; Product Owner confirmed 2026-08-11)*
