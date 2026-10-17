# A stream that knows it is paused

## Meta
- **State:** Adopted
- **Specs:** [`specs/E3 AI/F6 DrylAiStream.md`](../specs/E3%20AI/F6%20DrylAiStream.md)
- **Origin:** Trello #68

## Problem

`DrylAiStream` reports `AiState.Streaming` for as long as its `Source` is open —
also when no token has arrived for minutes. Since I21 that window is common: a
run waiting for a tool approval rests at `AiState.Proposed`, but
`DrylAgentRun.TextStream` stays open across the wait, so the answer text keeps
glowing cyan with a typing caret, and a `DrylAiScope` keyed to the stream keeps
reading as working. The state claims "the AI is writing" while the AI waits for
a human.

Target role: a Blazor developer who binds a run's text stream to the screen.
Today they bend the state themselves — the 3.8.0 changelog and the website demo
`Examples/Agents/ToolApproval.razor` bind
`Ai="@(_run.State == AiState.Proposed ? AiState.None : context.State)"`, which
fixes the text but not the stream's `Key` in `IDrylAiActivityService`.

## Solution Idea

`DrylAiStream` gets `Paused` (`bool`, default `false`). While it is `true` and
the stream is open, the live states the stream would report — `Thinking` before
the first token, `Streaming` after it — read as `AiState.None`: the context,
and the `Key` (cleared, as for any `None`). The accumulated text stays; tokens
that still arrive keep accumulating. Setting `Paused` back to `false` restores
the live state at once. Completion, failure and the settle are unaffected: a
source that completes while paused still reaches `Generated` and `SettleTo`.

## Scope
- **In scope:** the parameter on `DrylAiStream`, its effect on the context state
  and on the activity service, removing the workaround from the website demo.
- **Out of scope:** a time-based fallback (rejected, see Decisions); a pause
  channel on `DrylAgentRun` (rejected, see Decisions); changing
  `DrylAiGenerate` / `DrylAiBuild`, which bind structured runs rather than a
  text stream and have no reported case yet.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency. `None` is the
  state I21 already chose for surfaces without an accept action while a run is
  `Proposed`, so `AI-04` is not touched.
- **Specs:** `specs/E3 AI/F6 DrylAiStream.md` — a new parameter row and a
  "Pause" criteria block.
- **Public API:** one new `bool` parameter, default `false` — MINOR (`REL-01`).
- **Code:** `DrylAiStream.razor` only. Risk: a `Paused` change must republish
  the state to the service without restarting the run — `OnParametersSet`
  restarts only on a new `Source` instance, so the republish sits beside that
  check.

## Decisions
- 2026-10-17: Product Owner chose an explicit `Paused` parameter over (b) a
  fallback to `Thinking` after a token pause — a time threshold flickers on
  slow models and still lies, just later — and over (c) a pause channel on the
  run, which costs the most Agents API for a case one boolean expresses.
- 2026-10-17: Paused reads as `AiState.None`, not `Proposed`: the text carries
  no accept action (same reasoning as the run's `DrylAiScope` in I21).
- 2026-10-17: Paused masks only the live states. A stream that ends while
  paused still plays its `Generated` reveal — the end is news, the wait is not.

## Open Points
