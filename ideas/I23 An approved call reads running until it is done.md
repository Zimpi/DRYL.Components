# An approved call reads running until it is done

## Meta
- **State:** Adopted
- **Specs:** [`specs/E3 AI/F1 DrylToolCall.md`](../specs/E3%20AI/F1%20DrylToolCall.md),
  [`specs/E12 Agent Runtime/F1 DrylAgentToolCalls.md`](../specs/E12%20Agent%20Runtime/F1%20DrylAgentToolCalls.md)
- **Origin:** Trello #69, follow-up of I21

## Problem

Since Agents 0.22.0 (I21) an approved call moves straight from
`AiState.Proposed` to `AiState.Generated`, so its card plays the accept wave —
and its status pill reads "Done" while the tool has not run yet. The tool runs
only when the run resumes, at the start of the continuation. With a slow tool
the user sees "Done" for something that is still happening. The independent
I21 review flagged the pill as misleading; the product decision was left open.

Target role: the end user approving a tool call in a chat built on
`DrylAgentToolCalls`.

## Solution Idea

The wave plays as today; the card stays at `AiState.Generated`. A new `Running`
flag on `DrylToolCall` makes the status pill read "Running", with its indicator
at `AiState.Thinking`, until the host clears it. `DrylAgentToolCalls` sets it
for an approved call while it executes: once no call of the run is pending
(the run resumes only then) and until its `Result` or `Error` is in.
`DrylToolInvocation.Running` exposes "not finished yet" for custom renderers.

## Scope
- **In scope:** `DrylToolCall.Running`, `DrylToolInvocation.Running`, the
  wiring in `DrylAgentToolCalls` (cards, group state, `ActiveOnly`).
- **Out of scope:** an "Approved" label for an approved call that waits for
  its siblings' decisions — that window is user-driven (the group reads
  "awaiting approval" for the rest) and keeps today's "Done"; a new core
  transition `Generated → Thinking` (rejected, see Decisions).

## Impact
- **Harness:** no new token, animation, `AiState` or dependency. The pill's
  indicator uses the existing `Thinking` look; the card's aura is untouched.
- **Specs:** `specs/E3 AI/F1 DrylToolCall.md` (new parameter and criteria),
  `specs/E12 Agent Runtime/F1 DrylAgentToolCalls.md` (description, invocation
  and group criteria, `ActiveOnly`).
- **Public API:** `DrylToolCall.Running` (`bool`, default `false`) and
  `DrylToolInvocation.Running` (derived `bool`) — MINOR in both packages.
- **Code:** `DrylToolCall.razor`, `DrylToolInvocation.cs`,
  `DrylAgentToolCalls.razor`. Risk: a tool whose result never arrives would read
  "Running" forever — covered, because I21 already gives such a call an `Error`
  on cancellation and failure.

## Decisions
- 2026-10-17: Product Owner chose "wave + Running pill" over leaving the
  behaviour as is (the pill would keep claiming "Done") and over a core
  transition back to `Thinking` after the wave (needs a new transition and
  maintainer sign-off for little gain).
- 2026-10-17: Running starts only once nothing is pending. An approved call
  whose siblings still await a decision has not run either; showing "Running"
  there would repeat the lie #68 removed from the text stream.

## Open Points
