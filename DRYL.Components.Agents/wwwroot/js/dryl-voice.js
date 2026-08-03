// dryl-voice.js — the browser half of a DRYL voice session.
//
// Holds a WebRTC peer connection straight to the realtime API: the microphone track goes out,
// the model's voice comes back as a remote track, and a data channel called "oai-events"
// carries the JSON both ways. .NET never sees a byte of audio — it only learns who is talking
// and which tool was asked for.
//
// The levels are half the reason this file exists. Measuring them here and writing a CSS
// variable straight onto the orb keeps a per-frame signal out of the Blazor circuit; the same
// value shipped over interop would be sixty renders a second for a decoration.

let session = null;   // one voice session per page — a second microphone is not a feature
let orb = null;

/** Points the level meter at the orb element. Safe to call before or after start(). */
export function attachOrb(element) {
    orb = element || null;
    if (orb) orb.style.setProperty('--voice-level', '0');
}

/** Opens a session. `token` is the ephemeral ek_… secret; the session config rides inside it. */
export async function start(token, config, dotNet) {
    if (session) return;

    const state = {
        dotNet,
        pc: null,
        channel: null,
        mic: null,
        audio: null,
        ctx: null,
        raf: 0,
        idleMs: 0,
        idleTimer: 0,
        maxTimer: 0,
        closed: false,
        speaking: false,
        // Bumped whenever the user takes the floor. Anything that was decided before the bump
        // belongs to a conversation that has moved on and must not be sent.
        turn: 0,
        // Spoken answers arrive as deltas keyed by item; a turn is only worth a transcript line
        // once it is finished.
        answers: new Map(),
    };
    session = state;

    try {
        state.mic = await navigator.mediaDevices.getUserMedia({
            audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
        });
    } catch (err) {
        session = null;
        // A denied microphone is by far the most likely failure, and the browser's own message
        // ("Permission denied") tells the user nothing about what to do next.
        await report(dotNet, 'OnFailed', err && err.name === 'NotAllowedError'
            ? 'Kein Zugriff auf das Mikrofon. Erlaube ihn in den Browser-Einstellungen und starte neu.'
            : `Das Mikrofon ließ sich nicht öffnen: ${err?.message ?? err}`);
        return;
    }

    try {
        const pc = new RTCPeerConnection();
        state.pc = pc;

        // The model's voice. Out of the document flow — it is audio, it has nothing to show.
        const audio = document.createElement('audio');
        audio.autoplay = true;
        audio.style.display = 'none';
        document.body.appendChild(audio);
        state.audio = audio;

        pc.ontrack = (event) => {
            audio.srcObject = event.streams[0];
            // The microphone click is a user gesture, but this runs a server round trip later,
            // so the autoplay policy may still refuse. Sticky activation usually carries it —
            // this is the belt to that pair of braces.
            audio.play?.().catch(() => { /* the element autoplays or it does not */ });
            meter(state, event.streams[0], 'out');
        };

        pc.addTrack(state.mic.getAudioTracks()[0], state.mic);
        meter(state, state.mic, 'in');

        pc.oniceconnectionstatechange = () => {
            if (pc.iceConnectionState === 'failed' || pc.iceConnectionState === 'closed') {
                teardown(state, 'OnClosed');
            }
        };

        const channel = pc.createDataChannel('oai-events');
        state.channel = channel;
        channel.onmessage = (event) => handle(state, event.data);
        // The other half of not tearing down on every `error` event: a session that really is
        // over closes this channel, and that signal — unlike a complaint on it — cannot be
        // mistaken for something recoverable.
        channel.onclose = () => teardown(state, 'OnClosed');
        channel.onopen = async () => {
            seed(state, config.history);
            state.idleMs = config.idleMs ?? 0;
            touch(state);
            await report(state.dotNet, 'OnConnected');
        };

        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        const response = await fetch(`${config.baseUrl}/realtime/calls`, {
            method: 'POST',
            body: offer.sdp,
            headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/sdp' },
        });

        if (!response.ok) {
            // The API says why in the body; the status alone cannot tell an expired token from
            // an unknown model.
            const detail = await response.text().catch(() => '');
            throw new Error(
                `Die Verbindung wurde abgelehnt (${response.status}). ${detail}`.trim());
        }

        await pc.setRemoteDescription({ type: 'answer', sdp: await response.text() });

        if (config.maxMs > 0) {
            state.maxTimer = setTimeout(() => teardown(state, 'OnClosed'), config.maxMs);
        }
    } catch (err) {
        const message = err?.message ?? String(err);
        teardown(state, null);
        await report(dotNet, 'OnFailed', message);
    }
}

/** Ends the session and releases the microphone. */
export function stop() {
    if (session) teardown(session, null);
}

// ── events ───────────────────────────────────────────────────────────────────

function handle(state, raw) {
    let event;
    try { event = JSON.parse(raw); } catch { return; }

    switch (event.type) {
        case 'input_audio_buffer.speech_started':
            state.turn++;
            touch(state);
            report(state.dotNet, 'OnActivity', 'UserSpeaking');
            break;

        case 'input_audio_buffer.speech_stopped':
            report(state.dotNet, 'OnActivity', 'Thinking');
            break;

        case 'response.output_audio.delta':
            // Only the first delta of a turn is a state change; the rest are just audio.
            if (!state.speaking) {
                state.speaking = true;
                report(state.dotNet, 'OnActivity', 'Speaking');
            }
            break;

        case 'response.output_audio_transcript.delta':
            state.answers.set(
                event.item_id,
                (state.answers.get(event.item_id) ?? '') + (event.delta ?? ''));
            break;

        case 'response.output_audio_transcript.done':
            if (event.transcript) state.answers.set(event.item_id, event.transcript);
            break;

        case 'conversation.item.input_audio_transcription.completed':
            report(state.dotNet, 'OnTranscript', 'User', event.transcript ?? '');
            break;

        case 'response.done':
            state.speaking = false;
            flush(state, event);
            touch(state);
            // Only back to listening when the turn is actually over. With tool calls pending
            // the assistant is still working, and saying "listening" over the top of that is
            // the dock lying about what it is doing.
            if (!calls(state, event)) resume(state);
            break;

        case 'error':
            // A live session survives most of what arrives here — a response.create that raced
            // the user starting to speak is the common one, and it looks exactly like a dead
            // session does. Tearing the peer connection down for it threw away a conversation
            // that was working. A session that really is gone still closes: the transport says
            // so through oniceconnectionstatechange and the channel closing, which is the one
            // signal that cannot be mistaken for a recoverable complaint.
            console.warn(
                '[dryl-voice]',
                event.error?.code ?? 'error',
                event.error?.message ?? '');
            break;
    }
}

// Hands the finished spoken answer to .NET as one line. Prefers what the server said the
// transcript was; falls back to the deltas collected along the way.
function flush(state, event) {
    for (const item of event.response?.output ?? []) {
        if (item.type !== 'message') continue;

        const spoken = (item.content ?? [])
            .map((part) => part.transcript ?? part.text ?? '')
            .join('')
            .trim();
        const text = spoken || (state.answers.get(item.id) ?? '').trim();

        if (text) report(state.dotNet, 'OnTranscript', 'Assistant', text);
        state.answers.delete(item.id);
    }
}

// Every function call in a finished response, executed server-side and answered on the channel.
// Returns whether there was anything to run — the caller needs to know whether the turn is over.
function calls(state, event) {
    const pending = (event.response?.output ?? []).filter((item) => item.type === 'function_call');
    if (pending.length === 0) return false;

    report(state.dotNet, 'OnActivity', 'Thinking');
    const at = state.turn;

    Promise.all(pending.map(async (call) => {
        let output;
        try {
            output = await state.dotNet.invokeMethodAsync(
                'OnToolCallAsync', call.call_id, call.name, call.arguments ?? '{}');
        } catch (err) {
            // .NET itself fell over (circuit gone, serialisation). The model still needs an
            // answer, or the conversation stops dead with no way back.
            output = JSON.stringify({ error: err?.message ?? 'Der Werkzeugaufruf schlug fehl.' });
        }
        // The result goes back whatever else happened — it belongs to a call the model made, and
        // an unanswered call sits in the conversation forever.
        send(state, {
            type: 'conversation.item.create',
            item: { type: 'function_call_output', call_id: call.call_id, output },
        });
    })).then(() => request(state, at));

    return true;
}

// A turn that ended without a tool call is where a spoken agent quietly gives up. Nothing in the
// protocol starts another one, so "ich schaue mal eben nach" becomes the last thing that ever
// happens and the user has to ask whether it is still working. .NET owns the decision, because
// it is the side that knows whether there is anything left on the plan.
async function resume(state) {
    const at = state.turn;

    let more = false;
    try { more = await state.dotNet.invokeMethodAsync('OnTurnEndedAsync'); }
    catch { /* circuit gone — the page is on its way out anyway */ }

    if (state.closed || state.turn !== at) return;   // the user took over while .NET decided

    if (more) send(state, { type: 'response.create' });
    else report(state.dotNet, 'OnActivity', 'Listening');
}

// Asks the model for another turn, unless the floor changed hands since `at`. Sending on top of
// a response the user's own speech already started is the collision the API rejects.
function request(state, at) {
    if (state.closed || state.turn !== at) return;
    send(state, { type: 'response.create' });
}

// Replays the earlier conversation into the session so the voice knows what was written.
function seed(state, history) {
    for (const turn of history ?? []) {
        if (!turn.text) continue;
        const mine = turn.role === 'User';
        send(state, {
            type: 'conversation.item.create',
            item: {
                type: 'message',
                role: mine ? 'user' : 'assistant',
                content: [{ type: mine ? 'input_text' : 'output_text', text: turn.text }],
            },
        });
    }
}

function send(state, payload) {
    if (state.channel?.readyState === 'open') state.channel.send(JSON.stringify(payload));
}

async function report(dotNet, method, ...args) {
    try { await dotNet.invokeMethodAsync(method, ...args); }
    catch { /* circuit gone — the page is on its way out anyway */ }
}

// ── levels ───────────────────────────────────────────────────────────────────

// One AnalyserNode per direction, both feeding the same CSS variable on the orb: the louder of
// the two wins, because at any moment only one side is really talking.
function meter(state, stream, direction) {
    try {
        state.ctx ??= new (window.AudioContext || window.webkitAudioContext)();
        // A context created outside a gesture starts suspended, and a suspended analyser reads
        // pure silence — the orb would sit perfectly still through the whole conversation.
        if (state.ctx.state === 'suspended') state.ctx.resume().catch(() => { /* stays still */ });

        const analyser = state.ctx.createAnalyser();
        analyser.fftSize = 256;
        state.ctx.createMediaStreamSource(stream).connect(analyser);

        state[direction] = {
            analyser,
            buffer: new Uint8Array(analyser.frequencyBinCount),
            level: 0,
        };

        if (!state.raf) state.raf = requestAnimationFrame(() => tick(state));
    } catch {
        // No Web Audio — the orb simply breathes without a level.
    }
}

function tick(state) {
    if (state.closed) return;

    for (const direction of ['in', 'out']) {
        const meterState = state[direction];
        if (!meterState) continue;

        meterState.analyser.getByteTimeDomainData(meterState.buffer);
        let peak = 0;
        for (const sample of meterState.buffer) peak = Math.max(peak, Math.abs(sample - 128));

        // Smoothed: a raw peak flickers, and a flickering orb reads as broken rather than alive.
        meterState.level += (Math.min(1, peak / 48) - meterState.level) * 0.25;
    }

    if (orb) {
        const level = Math.max(state.in?.level ?? 0, state.out?.level ?? 0);
        orb.style.setProperty('--voice-level', level.toFixed(3));
    }

    state.raf = requestAnimationFrame(() => tick(state));
}

// ── lifetime ─────────────────────────────────────────────────────────────────

function touch(state) {
    if (!state.idleMs) return;
    clearTimeout(state.idleTimer);
    state.idleTimer = setTimeout(() => teardown(state, 'OnClosed'), state.idleMs);
}

function teardown(state, notify) {
    if (state.closed) return;
    state.closed = true;

    clearTimeout(state.idleTimer);
    clearTimeout(state.maxTimer);
    if (state.raf) cancelAnimationFrame(state.raf);

    try { state.channel?.close(); } catch { /* already gone */ }
    try { state.pc?.close(); } catch { /* already gone */ }
    for (const track of state.mic?.getTracks() ?? []) track.stop();
    try { state.ctx?.close(); } catch { /* already gone */ }
    state.audio?.remove();
    orb?.style.setProperty('--voice-level', '0');

    if (session === state) session = null;
    if (notify) report(state.dotNet, notify);
}
