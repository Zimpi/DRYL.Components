# Agent Inputs — Public API

Shared enums, parameter contracts and services of the Agent Inputs category — the
part of the data contract the 1.0 freeze binds.

**Source folder:** `code/DRYL.Components.Agents/Field/, code/DRYL.Components.Agents/CommandPalette/, code/DRYL.Components.Agents/Voice/, code/DRYL.Components.Agents/Generation/`

The voice types below record the existing public surface and the cancellation
contract adopted from [I13](../../ideas/I13%20Performance%20and%20hardening%20update.md).
The I13 lifecycle criteria await implementation and verification. This companion
file has no `Meta` block and claims no component coverage (`SPEC-03`). Shared
Field, CommandPalette and Generation types remain phase-C documentation debt.

## VoicePhase

Declared in `code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs`, in namespace
`DRYL.Components.Agents`. Members retain this declaration order:

| Member | Meaning |
|---|---|
| `Idle` | No active session. |
| `Connecting` | Startup, beginning before token minting and ending when the data channel opens. |
| `Live` | The connected conversation. |
| `Closing` | Session teardown. |

## VoiceActivity

Declared in `code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs`. Members retain
this declaration order: `Listening`, `UserSpeaking`, `Thinking`, `Speaking`.
Activity describes the conversation within `VoicePhase.Live`; it does not add
values to the shared `AiState` vocabulary (`AI-01`).

## VoiceRole

Declared in `code/DRYL.Components.Agents/Voice/DrylVoiceMessage.cs`. Members
retain this declaration order: `User`, `Assistant`.

## DrylVoiceMessage

`public sealed record DrylVoiceMessage(VoiceRole Role, string Text)`, declared
in `code/DRYL.Components.Agents/Voice/DrylVoiceMessage.cs`. `Role` identifies
the speaker; `Text` holds one completed transcript line or one history turn.

## VoiceTurnDetection

Declared in `code/DRYL.Components.Agents/Voice/DrylVoiceOptions.cs`. Members
retain this declaration order: `SemanticVad`, `ServerVad`.

## VoiceNoiseReduction

Declared in `code/DRYL.Components.Agents/Voice/DrylVoiceOptions.cs`. Members
retain this declaration order: `NearField`, `FarField`, `Off`.

## DrylVoiceOptions

`public sealed class DrylVoiceOptions`, declared in
`code/DRYL.Components.Agents/Voice/DrylVoiceOptions.cs`. These are the library's
configured defaults, not a claim about a provider's current model catalog.

| Property | Type | Default | Contract |
|---|---|---|---|
| `ApiKey` | `string` | `string.Empty` | Used by the token-minting HTTP request; never included in browser startup arguments. |
| `Model` | `string` | `"gpt-realtime-2.1"` | Realtime session model. |
| `Instructions` | `string?` | `null` | Session instructions. |
| `Voice` | `string` | `"marin"` | Session output voice. |
| `Speed` | `double` | `1.0` | Session speaking rate. |
| `TurnDetection` | `VoiceTurnDetection` | `SemanticVad` | Input turn detection. |
| `NoiseReduction` | `VoiceNoiseReduction` | `NearField` | Input noise reduction. |
| `ReasoningEffort` | `string?` | `null` | Optional provider reasoning effort. |
| `TranscriptionModel` | `string?` | `"gpt-4o-transcribe"` | Input transcription model; null or whitespace omits transcription. |
| `Language` | `string?` | `null` | Optional transcription language. |
| `Tools` | `IList<AITool>` | Empty `List<AITool>` | Only contained `AIFunction` instances are exposed and callable as voice tools. |
| `IdleTimeout` | `TimeSpan` | `TimeSpan.FromMinutes(2)` | Browser inactivity timeout; zero disables it. |
| `MaxDuration` | `TimeSpan` | `TimeSpan.FromMinutes(30)` | Browser session duration cap; non-positive values schedule no duration timeout. |
| `BaseUrl` | `string` | `"https://api.openai.com/v1"` | Base URL for token minting and the browser handshake. |
| `SafetyIdentifier` | `string?` | `null` | Optional `OpenAI-Safety-Identifier` header on token minting. |
| `IsConfigured` | `bool`, getter only | Derived | True exactly when `ApiKey` is not null or whitespace. |

All listed properties except `IsConfigured` have public getters and setters.
`public JsonNode ToSessionPayload()` returns the provider session object with
`type: realtime`, `model`, audio output modality and the configured audio input
and output settings. It leaves input/output formats to WebRTC negotiation.
`SemanticVad`/`ServerVad` serialize as `semantic_vad`/`server_vad`;
`NearField`/`FarField` serialize as `near_field`/`far_field`; `Off` omits
`noise_reduction`. Whitespace-only optional text is omitted. Tool schemas carry
the configured function's name, description and parameters; `tool_choice: auto`
is emitted only when at least one function is present. `ApiKey`, timeout values,
`BaseUrl` and `SafetyIdentifier` are not session-payload fields.

## DrylVoiceRunner

`public sealed class DrylVoiceRunner`, declared in
`code/DRYL.Components.Agents/Voice/DrylVoiceRunner.cs`.

| Public signature | Contract |
|---|---|
| `DrylVoiceRunner(IJSRuntime js, HttpClient? http = null)` | Uses the supplied JS runtime and HTTP client; an omitted client uses the existing shared client. |
| `DrylVoiceRun Create(DrylVoiceOptions options)` | Returns a new idle run holding those options; null options throw `ArgumentNullException`. |

`Create` performs no HTTP or browser work. Token minting remains internal. The
host owns each returned run and disposes it when that conversation owner ends;
a dock render or navigation that only replaces its view does not transfer that
ownership. See [`_Interop.md`](_Interop.md) for registration and cleanup.

## DrylVoiceRun

`public sealed class DrylVoiceRun : DrylRunBase`, declared in
`code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs`. It is a shared service
handle, not a Razor component (`SPEC-02`). Its constructor is internal.

| Property | Type | Initial value / contract |
|---|---|---|
| `Options` | `DrylVoiceOptions`, getter only | The options supplied to `Create`. |
| `Phase` | `VoicePhase`, public getter, private setter | `Idle`. |
| `Activity` | `VoiceActivity`, public getter, private setter | `Listening`. |
| `Transcript` | `IReadOnlyList<DrylVoiceMessage>`, getter only | Empty; completed lines in arrival order. |
| `IsActive` | `bool`, getter only | `Phase != VoicePhase.Idle`, including `Closing`. |
| `SeedHistory` | `IEnumerable<DrylVoiceMessage>?`, get/set | `null`; fallback history for `StartAsync`. |
| `ShouldContinue` | `Func<ValueTask<bool>>?`, get/set | `null`; optional decision after a speech-only response. |
| `MaxAutoContinuations` | `int`, get/set | `6`; cap on consecutive continuations without tool progress or user speech. |

The inherited public members remain those of `DrylRunBase` in
`code/DRYL.Components.Agents/Agents/DrylRunBase.cs`: `State`, `Text`, `Error`,
`Usage`, `ToolCalls`, `OnChange`, `TextStream`, `WaitForCompletionAsync()` and
`DisposeAsync()`. Voice initializes `State` to `AiState.None`. Audio samples
and meter levels remain in the browser and do not raise `OnChange`.

| Public signature | Contract |
|---|---|
| `Task StartAsync(IEnumerable<DrylVoiceMessage>? history = null, CancellationToken ct = default)` | Starts an idle, undisposed run. Null history falls back to `SeedHistory`; explicit empty history seeds nothing. Repeated calls while an attempt is active do not start another attempt. |
| `Task StopAsync()` | Invalidates the current attempt and releases its session resources. Idle stop is a no-op. The run remains reusable. |
| `override ValueTask DisposeAsync()` | Permanently invalidates the run and releases its owned resources. Repeated disposal is safe. |

These existing public `[JSInvokable]` signatures remain unchanged:

| Signature | Current-attempt contract |
|---|---|
| `void OnConnected()` | Enters `Live` with `Listening` activity. |
| `void OnActivity(string activity)` | Accepts case-insensitive `VoiceActivity` names while live; an unrecognized name retains the activity. |
| `void OnTranscript(string role, string text)` | Ignores whitespace-only lines; trims retained text; parses the role case-insensitively and falls back to `Assistant`. |
| `Task<string> OnToolCallAsync(string callId, string name, string argumentsJson)` | Invokes only a configured function with an ordinally matching name and records its trace. String results are returned directly; other results are JSON-serialized. Unknown tools, invalid arguments and tool failures return JSON containing `error` rather than escaping as exceptions. |
| `Task<bool> OnTurnEndedAsync()` | Returns whether the current live conversation may continue. |
| `void OnFailed(string message)` | Records a `DrylRunError` for the current attempt and settles idle. |
| `void OnClosed()` | Settles the current attempt idle without adding an error. |

Public callback availability is preserved for compatibility. Browser callbacks
must also carry internal attempt ownership across interop; their unversioned
public signatures alone do not establish that ownership. The internal protocol
is specified in [`_Interop.md`](_Interop.md).

### Lifecycle acceptance criteria — I13

- A newly accepted start clears the previous error.
- A newly accepted start clears the previous transcript.
- A newly accepted start clears the previous tool trace.
- A newly accepted start resets the automatic-continuation budget.
- Startup enters `Connecting` before token minting begins.
- Stopping an attempt invalidates it before awaiting browser teardown.
- Disposing a run invalidates its attempt before awaiting browser teardown.
- Cancellation of `ct` while `StartAsync` is pending invalidates that attempt.
- An invalidated attempt starts no later token, import or browser handshake
  operation when a pending operation completes.
- Cancellation settles the affected run at `Idle` without adding an error.
- `Closing` and `Idle` use `AiState.None` (`AI-06`).
- `Connecting` uses `AiState.Thinking`.
- Live `Listening` and `UserSpeaking` use `AiState.Active`.
- Live `Thinking` uses `AiState.Thinking`.
- Live `Speaking` uses `AiState.Streaming`.
- Stop preserves the completed transcript for the host to read.
- Stop preserves the existing tool trace for the host to read.
- A new start can be accepted after stop finishes even if an obsolete token,
  import or microphone operation has not completed.
- Disposing a run prevents any later `StartAsync` call from acquiring resources.
- A failure belonging to the current, uncancelled attempt records its message.
- A success, failure or finalizer belonging to an obsolete attempt cannot change
  the newer attempt's phase, activity, error, transcript or continuation budget.
- An obsolete callback cannot add to or alter the run's tool trace.
- An obsolete callback emits no `OnChange` notification.
- Callback ownership is checked again after awaiting a host tool or
  `ShouldContinue`, so invalidation during that wait has the same effect as
  invalidation before the callback arrived.
- A tool callback first received after invalidation does not invoke a host tool.
- A tool already executing at invalidation may finish its external effects;
  its obsolete result does not alter the run or trigger another voice response.
- An obsolete `OnTurnEndedAsync` continuation returns `false`.
- A missing, false or throwing `ShouldContinue` decision starts no automatic
  response.
- A true current-attempt decision enters `Thinking` only within the remaining
  `MaxAutoContinuations` budget.
- Reaching the continuation cap hands the floor back to the user.
- User speech resets the continuation budget.
- A current-attempt tool invocation resets the continuation budget.

### Verification expectations

Extend `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceRunTests.cs` with
controllable token/interop operations and host callbacks. Cover stop and dispose
during token minting and import, cancellation of `ct`, late success and failure,
restart before old work resolves, and disposed-run reuse. Hold a tool and a
`ShouldContinue` decision across stop/restart, then assert the new run's state,
trace, transcript, budget and notification count are unchanged by old work.
Exercise callbacks through their attempt-owned interop target as well as the
preserved public methods; direct calls alone cannot prove cross-attempt isolation.
Use the runner's injected `HttpClient` seam and fake JS references. These tests
make no paid request and need neither a microphone nor a browser download.
