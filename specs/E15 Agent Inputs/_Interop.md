# Agent Inputs — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

The voice contract below adopts H01 from
[I13](../../ideas/I13%20Performance%20and%20hardening%20update.md); its new
lifecycle criteria await implementation and verification. Other Agent Inputs
interop and services remain phase-C documentation debt. This companion file
claims no component coverage (`SPEC-03`).

## Interop

### Voice module

`DrylVoiceRun` imports
`./_content/DRYL.Components.Agents/js/dryl-voice.js`, backed by
`code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js`.

| Existing export | Caller | Contract |
|---|---|---|
| `start(token, config, dotNet)` | `DrylVoiceRun.StartAsync` | Acquires microphone audio, negotiates one WebRTC connection and reports session events. `token` is the minted client secret. |
| `stop()` | `DrylVoiceRun.StopAsync` / `DisposeAsync` | Tears down the browser session. |
| `attachOrb(element)` | `DrylVoiceOrb` | Selects the meter's element; null detaches it. Attachment resets `--voice-level` and does not start voice. |

Startup config carries `baseUrl`, `idleMs`, `maxMs` and `history` with
`{ role, text }` turns. The browser posts the local offer as SDP to
`{baseUrl}/realtime/calls` with the client secret as the bearer token. Remote
audio uses a hidden audio element; protocol events use the `oai-events` data
channel. Audio samples never pass through .NET. The meter writes the existing
`--voice-level` property directly on the attached orb.

The browser reports `OnConnected`, `OnActivity`, `OnTranscript`,
`OnToolCallAsync`, `OnTurnEndedAsync`, `OnFailed` and `OnClosed` with the
arguments and return types listed in [`_Api.md`](_Api.md).

### Attempt ownership — I13

An **attempt** begins when an idle run accepts `StartAsync`; it includes token
minting, module import, browser startup and the live conversation. It ends on
stop, startup cancellation, disposal, terminal failure or transport closure.
Its identity must survive the JS/.NET round trip, including callbacks already
dispatched before teardown. Phase alone is insufficient: an old and a new
attempt can both have been `Connecting` or `Live`.

The existing public .NET signatures and JS export names remain compatible.
An internal per-attempt interop target is the preferred way to bind callbacks
to the originating attempt while preserving the public `[JSInvokable]`
methods. An internal identity argument or config field may scope `start` and
`stop`; it is not a new consumer parameter. The implementation plan chooses the
mechanism before code changes.

- The page has at most one acquiring or connected browser voice session.
- A competing start acquires no second microphone and does not replace the
  session that already owns the page.
- Each stop/dispose request from .NET targets its own attempt; a stale teardown
  cannot close a newer session, including one created by another run instance.
- A cancelled attempt cannot claim the page again when its pending interop call
  finally reaches the browser.
- An obsolete callback arriving at .NET cannot mutate the originating run or a
  newer attempt on that run.
- Terminal notification is emitted at most once for an attempt.
- A cancellation-triggered rejection produces no stale failure notification.
- Rejected callback promises are handled, including notifications dispatched
  without awaiting them, so a dead circuit produces no unhandled rejection.

### Startup boundaries — I13

For every row below, stop, disposal and pending-start cancellation invalidate
the attempt. Check ownership after each deferred operation settles, before the
next operation or state mutation. Both late fulfillment and late rejection are
covered; neither may notify or damage a newer attempt.

| Deferred boundary | Required outcome after invalidation |
|---|---|
| Token request / response-body read in `DrylVoiceRunner.MintTokenAsync` | Propagate cancellation to HTTP; a late token or failure starts no module import. |
| JS module import | A late module result starts no browser session; release an unused owned reference without disposing a newer attempt's reference. |
| Invocation of browser `start` | A late dispatch or completion cannot acquire ownership for a cancelled attempt. |
| `navigator.mediaDevices.getUserMedia` | Stop every track of a late stream immediately; create no peer, audio element or meter from it. |
| `RTCPeerConnection.createOffer` | Do not call `setLocalDescription`. |
| `RTCPeerConnection.setLocalDescription` | Do not send the SDP fetch. |
| Handshake `fetch` | Abort the pending request where supported; do not begin response-body processing after cancellation. |
| Successful or rejected handshake response body `text()` | Do not set a remote description or publish a failure from the obsolete body. |
| `RTCPeerConnection.setRemoteDescription` | Do not install session timers or publish connection state after completion. |

The microphone permission API need not be abortable for cancellation to finish.
Teardown releases resources already acquired and arranges cleanup for anything
the cancelled acquisition returns later. Once stopped, a new attempt can start
without waiting for that old permission prompt, token response or import.

### Live callbacks and delayed work — I13

- A stale data-channel open event seeds no history and starts no timer.
- A stale peer track event attaches no audio stream and creates no meter.
- Stale message, close and ICE events do not alter the current session.
- A queued meter frame from an obsolete attempt does not change the current
  orb's `--voice-level` or schedule another frame.
- An obsolete idle, duration or retry timer cannot stop or continue a new
  session.
- Tool results send only to the same still-current session that requested them.
- Tool completion after invalidation sends no `function_call_output` or
  `response.create` message.
- A continuation decision resolved after invalidation sends no message or
  listening notification.
- User speech invalidates pending retry and response-continuation requests for
  the prior turn of the same session.
- A current-session tool result may still answer its original call after user
  speech, but it does not request a response over the user's newer turn.

## Services

`AddDrylAgents()` in
`code/DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`
registers `DrylVoiceRunner` scoped, resolving its `IJSRuntime` from the current
scope. `DrylVoiceRunner.Create` returns host-owned `DrylVoiceRun` instances.
The optional injected `HttpClient` is a constructor seam; disposing a run does
not dispose that client or the runner's shared client.

The internal token mint sends the options' session payload to
`{BaseUrl}/realtime/client_secrets` and forwards cancellation to the HTTP
request and response-body read. Missing configuration makes no HTTP request.
An unsuccessful current request exposes the provider's JSON error message or
the existing status-code fallback. A missing returned client secret fails the
current attempt. Only the minted secret, never `ApiKey`, is passed to JS.

## Cleanup

### Browser resources — I13

Teardown is safe before acquisition, during every startup boundary and after
connection. Repeated stop, disposal or transport-close events are idempotent.
Ownership is invalidated before resources are released, so release-triggered
events cannot restart teardown or report into another attempt (`CODE-05`).

- Every acquired microphone track is stopped, including tracks returned after
  teardown finished.
- The attempt's data channel is closed.
- The attempt's peer connection is closed.
- The attempt's audio element releases its stream reference and is removed.
- The attempt's Web Audio context is closed; rejection of `close()` is handled.
- The attempt's idle, maximum-duration and retry timers are cleared.
- The attempt's animation frame is cancelled.
- Event handlers retained by the attempt are detached or rendered inert before
  their resources are released.
- The current attempt's meter is reset to zero on teardown.
- Obsolete teardown does not reset a newer attempt's meter.
- The module's active-session reference is cleared only by the attempt that
  owns it.
- A cleanup operation failing does not skip cleanup of the remaining resources.

### .NET resources — I13

- Stop-owned cancellation reaches token minting and pending startup interop.
- Invalidating an attempt makes its retained interop target inert immediately.
- Every attempt-owned `DotNetObjectReference` is disposed when no longer needed.
- Module references acquired after invalidation are released if no current
  attempt owns them.
- Run disposal releases retained module references and inherited run resources.
- Disposing an unused run makes no JS call during static prerender.
- JS disconnection or already-completed teardown does not prevent .NET cleanup.
- A dock only removes its subscriptions when disposed; its host-owned run stays
  alive until the host stops or disposes it.

### Verification expectations

Use controlled promises for microphone acquisition, every WebRTC operation,
fetch and both success/error body reads. At each boundary, stop or dispose,
start a replacement, and then fulfill or reject the old promise. Assert that
the old attempt starts no later handshake step, makes no stale callback and
cannot close or clear the replacement. Cover delayed browser dispatch and two
different .NET run handles as well as restart on one handle.

Replay retained event handlers and timer/frame callbacks after teardown; hold
tool and continuation interop promises across stop/restart. Track live
microphone tracks, peers, channels, audio elements, audio contexts, timers and
frames so every completed teardown returns its owned resources to baseline.
Assert that rejected interop and audio-context promises are handled. Preserve a
successful connection, history seed, transcript, tool response, user
interruption and timeout case so cancellation guards do not disable valid work.

The deterministic JS suite runs without a browser installation. The separate
I13 browser host renders the actual dock and working-tree assets, with a fake
token provider and controlled microphone/WebRTC/network substitutes. Exercise
start, stop, restart and removal/re-mounting in both color modes. No physical
microphone or paid provider request is required. Existing .NET tests with
`NoopJsRuntime` establish only their named .NET assertions; they are not browser
cleanup evidence.
