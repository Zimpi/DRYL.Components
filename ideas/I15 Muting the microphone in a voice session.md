# Muting the microphone in a voice session

## Meta
- **State:** Adopted
- **Specs:** [`specs/E14 Agent Canvas/F1 DrylCanvasDock.md`](../specs/E14%20Agent%20Canvas/F1%20DrylCanvasDock.md),
  [`specs/E15 Agent Inputs/_Api.md`](../specs/E15%20Agent%20Inputs/_Api.md),
  [`specs/E15 Agent Inputs/_Interop.md`](../specs/E15%20Agent%20Inputs/_Interop.md)

## Problem

During a live voice session in `DrylCanvasDock` the microphone is open for the
whole session. The only way to stop the model from hearing the room — a
colleague walks in, a phone rings, the user coughs or wants to think aloud — is
to end the session, which throws away the conversation's live context and costs
a reconnect. The user wants to mute their own microphone and keep the session.

Target role: the end user talking to a DRYL-built assistant; secondarily the
host developer who drives or mirrors the mute from their own UI.

## Solution Idea

A mute toggle for the voice session. Muting disables the outgoing microphone
track in the browser (`MediaStreamTrack.enabled = false`): the peer connection
stays up, the model keeps talking, but it receives silence. Unmuting re-enables
the same track — no renegotiation, no new permission prompt.

The mute state belongs to the session, not to the dock, so it lives on
`DrylVoiceRun`; the dock's button is one consumer of it.

- **Toggle:** an icon button left of "End voice session" in the takeover row,
  present whenever the takeover is (`Connecting`, `Live`, `Closing`). Icon
  `Microphone` while open, `MicrophoneOff` while muted; `aria-pressed`
  reflects the muted state; tooltip and `aria-label` from
  `VoiceMuteLabel` / `VoiceUnmuteLabel`. Tab + Enter/Space, no shortcut.
- **Early mute:** muting during `Connecting` is remembered and applied to the
  track as soon as the microphone is acquired.
- **Half a sentence:** muting mid-utterance only sends silence from then on;
  what was already said stays in the input and the model may answer it.
- **Orb:** while muted the orb fades its aura (ring and comet) out with the
  existing motion tokens; the core stays and keeps answering the model's
  voice. The user's voice no longer moves it because the track is silent.
- **Status line:** while muted and `Listening`, the line reads
  `VoiceMutedText` (default `"Muted"`); `Thinking` and `Speaking` lines stay
  as they are. A failure and `Status` still outrank it; `VoiceStatusText` does
  not, because its signature cannot see the mute.
- **Reset:** a session end (stop, failure, close) sets `IsMuted` back to
  `false`; a new session starts unmuted.

## Scope

- **In scope:** `DrylVoiceRun.IsMuted` + `SetMutedAsync(bool)`; JS track
  toggle for both the Realtime and the GPT-Live transport; the dock toggle,
  its labels and status line; the orb's muted look; the `MicrophoneOff` icon;
  specs, tests, changelog, website demo.
- **Out of scope:** push-to-talk; muting the model's output; input device
  selection; persisting mute across sessions; a keyboard shortcut; discarding
  the half-said input buffer on mute.

## Impact

- **Harness:** no new token, duration, easing, `AiState` or dependency. The
  orb's muted look uses existing motion tokens only (aura fade). One new icon,
  `MicrophoneOff` (lucide `mic-off`) — signed off by Jan 2026-09-27.
- **Specs:** `specs/E14 Agent Canvas/F1 DrylCanvasDock.md` (parameters, Voice,
  keyboard/a11y sections); `specs/E15 Agent Inputs/_Api.md` and
  `specs/E15 Agent Inputs/_Interop.md` (run members, JS `setMuted`); the
  `DrylVoiceOrb` spec (muted look); the `DrylIcon` spec (icon list).
- **Public API (additive):** `DrylVoiceRun.IsMuted`,
  `DrylVoiceRun.SetMutedAsync(bool)`; `DrylCanvasDock.VoiceMuteLabel`,
  `VoiceUnmuteLabel`, `VoiceMutedText`; icon name `MicrophoneOff`.
  MINOR bump for `DRYL.Components` (icon) and `DRYL.Components.Agents`.
- **Code:** `Voice/DrylVoiceRun.cs` (state, attempt ownership, reset),
  `wwwroot/js/dryl-voice.js` (`setMuted`, apply after `getUserMedia`),
  `Canvas/DrylCanvasDock.razor` + `.razor.css`, `Voice/DrylVoiceOrb.razor` +
  `.razor.css`, `Components/Data/DrylIcon.razor`. Risks: a stale attempt
  muting a newer session (guard by attempt as the other interop calls do);
  prerender / no JS module yet (store state, apply on attach); the orb's
  fade must stay compositor-only (opacity, no looped custom properties).

## Decisions

- 2026-09-27 — Jan: users need to be able to mute their microphone in the voice
  session of the canvas dock.
- 2026-09-27 — Jan: the toggle is an icon button beside "End voice session" in
  the takeover row. Rejected: clicking the orb (undiscoverable, the orb is a
  display today) and both (more API and test surface for a shortcut).
- 2026-09-27 — Jan: muted shows three ways — crossed-out microphone on the
  button, "Muted" in the status line, and an orb that stops answering the
  user's voice while still answering the model's. Rejected: icon and status
  line only (the orb would not carry the state).
- 2026-09-27 — Jan: the state is public on `DrylVoiceRun`
  (`IsMuted`, `SetMutedAsync(bool)`); the dock button uses exactly that, and a
  session end resets it. Rejected: dock-internal only.
- 2026-09-27 — Jan: the orb's muted look is token-free — the aura fades out,
  the core stays. Rejected: a new `--voice-muted-opacity` token.
- 2026-09-27 — Jan: sign-off for the new icon `MicrophoneOff` (lucide
  `mic-off`). Rejected: reusing `Microphone` with a pressed style only.
- 2026-09-27 — Jan: mute is available from `Connecting` on, no keyboard
  shortcut. Rejected: `Live` only; a shortcut (conflicts with input).
- 2026-09-27 — Jan: muting mid-sentence leaves the half-said input as it is.
  Rejected: clearing the input buffer on mute (Tech Lead's recommendation).
- 2026-09-27 — Jan confirmed the final summary and set the idea to `Ready`.
- 2026-09-27 — Tech Lead, during implementation: `VoiceMutedText` goes ahead of
  `VoiceStatusText` (the draft had it behind). A host that words its listening
  line would otherwise never show the mute; `VoiceMutedText` is the localisation
  point instead.

## Open Points
