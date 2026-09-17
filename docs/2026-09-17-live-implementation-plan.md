# GPT-Live implementation plan

Source: [I14](../ideas/I14%20GPT-Live%20voice%20delegation.md), requested by the
maintainer on 2026-09-17. I13 is completed and committed separately before touching
its voice lifecycle files.

## Contracts to implement

1. `DrylVoiceOptions.Live` is nullable (default null). Non-null selects GPT-Live;
   `Model` is then the Live voice model, explicitly `gpt-live-1` in Portfolio.
   `DrylLiveOptions` holds `BackendModel` (default `gpt-5.6-terra`),
   `BackendInstructions`, `ReasoningEffort` (default `medium`) and
   `EnableWebSearch` (default false). Existing `Tools` become Responses functions,
   with `strict:false` to preserve optional schemas; optional hosted `web_search`
   is added only in Live mode. `parallel_tool_calls:false` ensures ordered actions.
2. Live session JSON contains `model`, voice `instructions`, `audio.output.voice`,
   `store:false`, bounded text `input` history and `delegation:{type:"responses",
   responses:{model,instructions,reasoning,tools,tool_choice,max_output_tokens,
   parallel_tool_calls}}`. Omit Realtime audio/VAD/codec/turn settings.
3. Browser still uses `createSession(token,config,dotNet)`, with null token and
   `config.live:true`. After microphone/offer/local SDP and bounded ICE gathering,
   call attempt-owned `OnLiveOfferAsync(sdp)`; the server returns only the SDP
   answer. It holds the created session ID and cleans up stale created sessions.
   Wait for `session.started`, never send `session.start` or a greeting
   `response.create`. Live conversation style belongs in the short voice prompt.
4. Browser receives nested `response.event`, tracks delegation and response IDs,
   collects function items from `response.output_item.done`, and waits for a
   successful terminal response before executing collected calls. A terminal
   `output:[]` never erases those calls. Deduplicate call IDs for the session,
   execute sequentially, return all results using `response.item.create`, then
   issue exactly one `response.create` with no standalone Responses body.
   Stale/failed/incomplete responses never dispatch new actions. An ordinary
   user speech interruption does not cancel backend work or its continuation.
5. Completed backend messages, including URL annotations, are passed to the
   host through `DrylVoiceRun.BackendResponseReceived` as response JSON with the
   collected output items restored. Backend messages are not spoken transcripts.
6. Preserve every input/output transcript fragment with original text and
   timestamps. Group each speaker independently for display; no trimming or
   invented spaces, no inferred semantic turn endings. The run reports partial
   spoken text independently from tool progress. Live bypasses `ShouldContinue`.
7. Stop closes a ready session using `session.close`, stops microphone input,
   and drains final events for a bounded interval before releasing the peer.
   Record final usage/close reason when provided; cancellation, transport failure
   and disposal preserve I13 stale-attempt guards and release owned resources.

## Tasks and ownership

- .NET: `Voice/DrylLiveOptions.cs`, `DrylVoiceOptions.cs`, `DrylVoiceRunner.cs`,
  `DrylVoiceRun.cs`, tests under `tests/DRYL.Components.Tests/Agents/Voice/`.
  Verify payloads, real HTTP request seams, late handshake completion, duplicate
  offers, history bounds, transcripts, backend callback and cleanup.
- Browser: `wwwroot/js/dryl-voice.js` and `tests/js/live.test.mjs`. Verify transport
  startup, ICE cancellation, readiness, nested event aggregation, duplicate tools,
  failed responses, interrupted speech, stop/restart and resource cleanup.
- Consumer: Portfolio uses `gpt-live-1` and backend `gpt-5.6-terra`, medium effort,
  hosted web search plus its existing domain functions; displays cited backend
  responses. Retain the text agent's dedicated `web_research` function.
- Release: bump unpublished Agents to 0.18.0, keep I13 Core 3.0.1; update E15
  specs and English changelog in the implementation commit. Pack both artifacts,
  pin Portfolio CI to the final Components commit and run its tests.

## Verification

`node --test tests/js/*.test.mjs`; `dotnet build DRYL.slnx -c Release`;
`dotnet test DRYL.slnx -c Release`; the repository's token/contrast/harness/spec/
motion checks; applicable browser voice tests. Independent code review before
release. A paid live smoke check is separate and uses no user microphone unless
explicitly started by the user.

## Progress

Implementation complete. The client/server protocol is one coordinated release
unit because either half on its own cannot establish a Live session. Its API,
interop contracts, regressions, package notes and version ship together.

Independent review found and resolved: repeated stop/dispose losing the closing
owner; malformed SDP response shapes bypassing known-session cleanup; and the
Portfolio's cited result spanning multiple backend responses. Browser ICE waiting
and readiness now have tested cancellation and timeout bounds. Independent
re-review confirmed both .NET fixes and the Portfolio reset fix; no actionable
findings remain.

### Evidence — 2026-09-17

- Release build passed for net8/net9/net10 (63 existing warnings, no errors).
- Full .NET suite: 1,165 passed. Deterministic JS: 124 passed.
- Chromium voice integration: 20 passed, including Live in both light/dark modes
  and 18 existing Realtime cases. Real Blazor callbacks and the HTTP seam are used.
- Live browser cases also passed in Firefox (2/2) and WebKit (2/2), both modes.
- Token sync, 50 contrast compositions, harness links and motion checks passed.
  Strict coverage remains 60/129 with 69 existing phase-C gaps; the explicit
  baseline gate passed against `origin/main`. No new component coverage is claimed.
- Core 3.0.1 and Agents 0.18.0 nupkg/snupkg built under `artifacts/packages`.
  Both contain all three target frameworks; Agents references Core 3.0.1.
- Actual OpenAI WebRTC smoke using production JS and payload builder: HTTP 201
  with gpt-live-1, gpt-5.6-terra, Gleam, functions and web search. A synthetic spoken
  question executed `probe_status`, submitted its result and continued to hosted
  web search with a URL citation. Received 35 transcript fragments and final
  `session.closed` usage of 20 seconds. A separate active WebRTC session confirmed
  server `POST /live/sessions/{id}/hangup` returns HTTP 200 and closes transport.
  No physical microphone was used; this does not certify natural speech quality.

I13's separate full engine/native-browser results and visual inspection are in
`2026-09-15-i13-implementation-plan.md`. New Live code changes no visual design.
Native Safari/Firefox, Windows contrast-theme and physical-device audio acceptance
remain the documented manual limits. Publishing and deployment are separate from
these local package artifacts and user acceptance testing.
