# GPT-Live voice delegation

## Meta
- **State:** Adopted

## Problem

The portfolio maintainer wants `gpt-live-1` to handle natural spoken interaction
while a separate Responses backend reasons, uses the existing portfolio functions
and researches the web. The current Realtime-only transport cannot use this model
by changing its model string: GPT-Live has a different handshake and event protocol.

## Solution Idea

Add opt-in GPT-Live Responses delegation to the existing voice run. Reuse the
dock, orb, tool trace, server-side functions and attempt ownership. The browser
forwards function events over its authenticated Blazor circuit; server HTTP
exchanges its SDP with `/live/sessions`, keeping the project key private.

Alternatives considered: client delegation preserves a custom agent loop but
requires reconstructing task intent from transcript fragments; a second, unrelated
voice UI duplicates the existing library lifecycle. Responses delegation matches
the maintainer's supplied Playground example and retains the function-tool contract.

## Scope

- **In scope:** additive Live options; split voice/backend prompts; server SDP
  exchange; hosted web search; nested Responses tool calls and continuations;
  cited backend-result notification; overlapping transcript fragments; graceful
  close and cancellation; configuration, regression tests and a consumer recipe.
- **Out of scope:** replacing existing Realtime consumers; a new visual component,
  token, animation, AI state or runtime dependency; SIP, audio via WebSocket,
  recordings, client delegation or automatic deployment.

## Impact

- **Harness:** no new design primitives or dependencies. Existing .NET HTTP/JSON,
  WebRTC and Blazor interop are sufficient.
- **Specs:** additive contracts in `specs/E15 Agent Inputs/_Api.md` and
  `specs/E15 Agent Inputs/_Interop.md`.
- **Public API:** `DrylVoiceOptions.Live` and `DrylLiveOptions` select the new
  protocol. Existing Realtime defaults/signatures remain. A backend-response
  callback exposes cited results; raw transcript fragments preserve overlap.
- **Code:** voice options, runner, run and JS module plus isolated tests. Main
  risks are startup cancellation, duplicate tool execution, incomplete forwarded
  lifecycle snapshots and treating transcript fragments as completed speech.

## Decisions

- 2026-09-17 — Jan explicitly requested implementation in Portfolio and necessary
  library changes, then clarified the exact target: “nicht realtime sondern
  gpt-live-1”. His Playground example shows Responses delegation, `gpt-5.6-terra`,
  medium reasoning, web search and function tools. This authorizes the bounded
  integration; no further product choice or new dependency approval is needed.
- 2026-09-17 — Technical recommendation: opt-in Responses delegation using the
  existing authenticated circuit. The official server-controls guide explicitly
  permits forwarding browser function events to an authenticated backend without
  a sideband connection.

## Open Points

None for the implementation scope. Actual model/account access and audible
conversation quality will be reported separately from deterministic test evidence.
