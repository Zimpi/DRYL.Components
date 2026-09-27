# I15 — Muting the microphone: implementation plan

Source idea: [`ideas/I15 Muting the microphone in a voice session.md`](../ideas/I15%20Muting%20the%20microphone%20in%20a%20voice%20session.md).
Versions: `DRYL.Components` 3.1.0 → **3.2.0** (new icon), `DRYL.Components.Agents`
0.19.0 → **0.20.0** (new run API, dock parameters). Both 3.1.0 and 0.19.0 are
tagged, so both bump (`REL-01`).

Each task updates its spec in the same commit (`SPEC-01`) and carries its own
verification.

## T1 — Icon `MicrophoneOff` (core 3.2.0)

- `code/DRYL.Components/Components/Data/DrylIcon.razor` — add `MicrophoneOff`
  (lucide `mic-off`) next to `Microphone`.
- `specs/E5 Data/F10 DrylIcon.md` — only if it lists names (it describes the set).
- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>3.2.0</Version>`.
- `CHANGELOG.md` — new `[3.2.0]` block, `### Added`.
- Verify: `dotnet build DRYL.slnx -c Release`; `dotnet test … --filter DrylIcon`.

## T2 — Mute on the run and in the browser (Agents 0.20.0)

- `code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs` — `IsMuted`,
  `SetMutedAsync(bool)`: accepted in `Connecting` and `Live`, ignored in `Idle`,
  `Closing` and after dispose; raises `OnChange`; forwarded to the adopted JS
  handle, and re-applied right after adoption when set before it; reset in
  `MarkConnecting` and `SetClosed`.
- `code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js` — handle gains
  `setMuted(muted)`: stores `state.muted`, toggles `enabled` on the microphone's
  audio tracks; microphone acquisition applies the stored value.
- `specs/E15 Agent Inputs/_Api.md` (DrylVoiceRun members), `_Interop.md`
  (`setMuted` on the owned handle).
- `code/DRYL.Components.Agents/DRYL.Components.Agents.csproj` — 0.20.0.
- Tests: `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceCancellationTests.cs`
  (forwarding, early mute, reset, idle no-op, stale attempt);
  `tests/js/voice.test.mjs` (track `enabled`, early mute before `getUserMedia`).
- Verify: `dotnet test … --filter Voice`; `node --test tests/js/voice.test.mjs`.

## T3 — Orb and dock (Agents 0.20.0)

- `Voice/DrylVoiceOrb.razor(.css)` — `voice-orb--muted` while `Run.IsMuted`:
  ring and comet fade out (`--dur-slow`, `--ease-out`, then `visibility:hidden`
  so the spinning comet stops costing frames); core and glow stay.
- `Canvas/DrylCanvasDock.razor(.css)` — `.dock-voice-actions` row: icon-only
  toggle (`Microphone`/`MicrophoneOff`, `Pressed`, `DrylTooltip`, `AriaLabel`)
  left of the stop button; disabled while `Closing`. Parameters
  `VoiceMuteLabel` ("Mute microphone"), `VoiceUnmuteLabel` ("Unmute
  microphone"), `VoiceMutedText` ("Muted"). Status line: muted + `Live` +
  activity not `Thinking`/`Speaking` → `VoiceMutedText`, ahead of
  `VoiceStatusText` (which cannot see the mute), behind a failure and `Status`.
- Specs: `specs/E14 Agent Canvas/F1 DrylCanvasDock.md` (parameters, Voice,
  status, keyboard); orb criteria live in the E15 `_Api.md` / dock spec (the
  orb has no spec of its own).
- Tests: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasDockTests.cs`,
  `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOrbTests.cs`.
- Verify: `dotnet test … --filter "CanvasDock|VoiceOrb"`.

## T4 — Website, close the loop

- `../DRYL.Website/Components/Pages/DemoCanvasDock.razor` — German labels for
  the three new parameters; API text mentions the toggle.
- Idea → `Adopted` with spec links.
- Verify: full evidence list from `CLAUDE.md` step 5, `dotnet test
  DRYL.Website.slnx`, and the dock's voice takeover in both colour modes in the
  browser (muted and unmuted).
