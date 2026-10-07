# ElevenLabs as a voice transport

## Meta
- **State:** Adopted
- **Specs:** [`specs/E15 Agent Inputs/_Api.md`](../specs/E15%20Agent%20Inputs/_Api.md),
  [`specs/E15 Agent Inputs/_Interop.md`](../specs/E15%20Agent%20Inputs/_Interop.md)

## Problem

The El Destino Hub wants to compare its GPT-Live voice with an ElevenLabs agent:
a designed voice with a stable accent instead of one described in a prompt, at
the cost of a cascaded pipeline (speech-to-text → LLM → text-to-speech). The dock,
the orb, the tool trace and the circuit-side tools must stay the same, so the
comparison is about the voice and not about a second UI.

Target role: the host developer who picks the voice provider for a DRYL-built
assistant; secondarily the end user, who hears the designed voice.

## Solution Idea

A third, opt-in transport on the existing `DrylVoiceRun`, next to Realtime and Live:

- `DrylVoiceOptions.ElevenLabs` (`DrylElevenLabsOptions`) names the agent, the key
  and the voice. The runner exchanges the key for a WebRTC conversation token on the
  server; the browser gets only the token.
- `dryl-voice.js` loads the official ElevenLabs client SDK and translates its
  callbacks into the existing reports: transcript lines, activity, tool calls. Every
  name in `Options.Tools` becomes an ElevenLabs *client tool* answered by
  `OnToolCallAsync` — the agent's tools run on the circuit, like every other DRYL tool.
- `Instructions` and `Language` become session overrides; the conversation so far is
  appended to the prompt, because an ElevenLabs session cannot be seeded with items.
- `ConversationId` lets the host fetch duration and cost once the session is over.

## Scope

- **In scope:** the transport on `DrylVoiceRun` — token exchange, session
  overrides, client tools, transcript, activity, levels, mute, idle and duration
  limits, `ConversationId`.
- **Out of scope:** configuring the agent itself (its LLM, voice model and tool
  registration live in the ElevenLabs workspace); fetching duration and cost
  (the host does that with `ConversationId`); any change to `DrylCanvasDock` or
  `DrylVoiceOrb`; ElevenLabs server tools or the WebSocket connection type.

## Impact

- **Harness:** a **new runtime dependency** — `@elevenlabs/client` (MIT), loaded
  from jsDelivr at a pinned version (`ClientScriptUrl`, overridable to self-host),
  only when the transport is configured. Signed off 2026-10-07 and recorded as the
  opt-in JS exception of `CODE-03` in `harness/code.md`.
- **Specs:** `specs/E15 Agent Inputs/_Api.md` (`DrylVoiceOptions`,
  `DrylElevenLabsOptions`, `DrylVoiceRun`) and `specs/E15 Agent Inputs/_Interop.md`
  (ElevenLabs transport).
- **Public API:** additive. `DrylElevenLabsOptions`, `DrylVoiceOptions.ElevenLabs`,
  `ToElevenLabsOverrides`, `DrylVoiceRun.ConversationId`,
  `DrylVoiceRun.OnConversationStarted`. Realtime and Live are unchanged;
  `IsConfigured` now reads the ElevenLabs key when that transport is set.
- **Code:** options, runner, run and JS module plus `DrylElevenLabsTests` and
  `tests/js/elevenlabs.test.mjs`.
- **Risks:** the SDK owns the microphone, so mute goes through `setMicMuted`; levels
  come from `getInputVolume`/`getOutputVolume`; the SDK hides the tool call id, so
  the JS invents one per call; activity has no "thinking" of its own and is derived
  from running client tools.

## Decisions

- 2026-10-06 — Jan asked whether Santiago could run on ElevenLabs "mit derselben
  Qualität … mit dem delegierten Model im Hintergrund … den ganzen Tool Calls", then
  approved a prototype ("Ein Prototyp klingt gut") and supplied a designed voice.
- 2026-10-07 — Jan decided the transport belongs in DRYL, accepted the prototype as
  it stands — CDN-loaded SDK with a self-host override — and asked to finish it
  ("Ja das soll in DRYL das passt so. Mach alles fertig."). This is the maintainer
  sign-off for the runtime dependency.

## Open Points
