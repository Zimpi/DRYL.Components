import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js', import.meta.url), 'utf8');
const deferred = () => { let resolve, reject; const promise = new Promise((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; };
const settle = async () => { for (let i = 0; i < 30; i++) await Promise.resolve(); };

function fixture(held = null, failedResponse = false) {
    const pending = [], calls = {}, reports = [], peers = [], tracks = [], contexts = [], audio = [];
    const timers = new Map(), frames = new Map(); let next = 0;
    const boundary = (name, value) => {
        calls[name] = (calls[name] ?? 0) + 1;
        if (held !== name) return Promise.resolve().then(value);
        const gate = deferred(); pending.push({ name, gate, value }); return gate.promise;
    };
    const stream = () => { const track = { stopped: false, stop() { this.stopped = true; } }; tracks.push(track); return { getTracks: () => [track], getAudioTracks: () => [track] }; };
    class Peer {
        constructor() { this.closed = false; peers.push(this); }
        addTrack() {}
        createOffer() { return boundary('offer', () => ({ sdp: 'offer' })); }
        setLocalDescription() { return boundary('local', () => {}); }
        setRemoteDescription() { return boundary('remote', () => {}); }
        createDataChannel() {
            return this.channel = { readyState: 'connecting', sent: [], send(value) { this.sent.push(JSON.parse(value)); }, close() { this.readyState = 'closed'; this.onclose?.(); } };
        }
        close() { this.closed = true; this.iceConnectionState = 'closed'; this.oniceconnectionstatechange?.(); }
    }
    class Context {
        constructor() { this.state = 'running'; contexts.push(this); }
        close() { this.state = 'closed'; return Promise.resolve(); }
        createAnalyser() { return { frequencyBinCount: 2, getByteTimeDomainData(a) { a.fill(128); } }; }
        createMediaStreamSource() { return { connect() {}, disconnect() {} }; }
    }
    const sandbox = {
        console: { log() {}, warn() {} }, AbortController, Uint8Array,
        performance: { now: () => 0 },
        navigator: { mediaDevices: { getUserMedia: () => boundary('media', stream) } },
        RTCPeerConnection: Peer,
        document: { createElement() { const element = { style: {}, srcObject: null, removed: false, remove() { this.removed = true; }, play: () => Promise.resolve(), pause() {} }; audio.push(element); return element; }, body: { appendChild() {} } },
        fetch: (_url, options) => { calls.signal = options.signal; return boundary('fetch', () => ({ ok: !failedResponse, status: failedResponse ? 500 : 200, text: () => boundary('body', () => 'answer') })); },
        setTimeout: callback => { const id = ++next; timers.set(id, callback); return id; }, clearTimeout: id => timers.delete(id),
        requestAnimationFrame: callback => { const id = ++next; frames.set(id, callback); return id; }, cancelAnimationFrame: id => frames.delete(id),
    };
    sandbox.window = { AudioContext: Context };
    const api = vm.runInNewContext(source.replace(/\bexport /g, '') + '\n;({ start, stop, attachOrb, createSession: typeof createSession === "function" ? createSession : null })', sandbox);
    const dotNet = { invokeMethodAsync(method, ...args) { reports.push([method, ...args]); return Promise.resolve(method === 'OnTurnEndedAsync' ? false : '{}'); } };
    const config = { baseUrl: 'https://fixture.invalid', idleMs: 100, maxMs: 200, history: [{ role: 'User', text: 'history' }] };
    const begin = () => api.start('token', config, dotNet);
    const finish = (reject = false) => { const item = pending.shift(); assert.ok(item, 'a boundary is pending'); reject ? item.gate.reject(new Error('late failure')) : item.gate.resolve(item.value()); };
    const clean = () => {
        assert.equal(tracks.filter(t => !t.stopped).length, 0, 'microphone tracks');
        assert.equal(peers.filter(p => !p.closed).length, 0, 'peers');
        assert.equal(contexts.filter(c => c.state !== 'closed').length, 0, 'contexts');
        assert.equal(audio.filter(a => !a.removed || a.srcObject).length, 0, 'audio elements');
        assert.equal(timers.size, 0, 'timers'); assert.equal(frames.size, 0, 'frames');
    };
    return { api, begin, config, dotNet, finish, pending, calls, reports, peers, contexts, audio, timers, frames, clean, hold: name => { held = name; } };
}

for (const stage of ['media', 'offer', 'local', 'fetch', 'body', 'remote']) {
    for (const reject of [false, true]) {
        test(`stop during ${stage}: late ${reject ? 'rejection' : 'success'} cannot resume handshake`, async () => {
            const f = fixture(stage); const start = f.begin(); await settle();
            f.api.stop(); const snapshot = Object.fromEntries(Object.entries(f.calls).filter(([k]) => k !== 'signal'));
            f.finish(reject); await start; await settle();
            assert.deepEqual(Object.fromEntries(Object.entries(f.calls).filter(([k]) => k !== 'signal')), snapshot);
            assert.deepEqual(f.reports, []); f.clean();
        });
    }
}

test('a cancelled error response body cannot publish a failure', async () => {
    const f = fixture('body', true); const start = f.begin(); await settle(); f.api.stop(); f.finish(); await start;
    assert.deepEqual(f.reports, []); f.clean();
});

test('old microphone rejection cannot clear a replacement session', async () => {
    const f = fixture('media'); const old = f.begin(); await settle(); f.api.stop();
    const current = f.begin(); await settle(); f.finish(true); await old;
    assert.deepEqual(f.reports, []); f.finish(); await current; f.api.stop(); f.clean();
});

test('owned handle stop before delayed start is inert and cannot stop another handle', async () => {
    const f = fixture(); assert.equal(typeof f.api.createSession, 'function');
    const old = f.api.createSession('old', f.config, f.dotNet); old.stop(); await old.start();
    assert.equal(f.calls.media ?? 0, 0);
    const current = f.api.createSession('new', f.config, f.dotNet); await current.start(); old.stop();
    assert.equal(f.peers[0].closed, false); current.stop(); f.clean();
});

test('one page owner: competing handle never acquires another microphone', async () => {
    const f = fixture(); assert.equal(typeof f.api.createSession, 'function');
    const one = f.api.createSession('one', f.config, f.dotNet); await one.start();
    const two = f.api.createSession('two', f.config, f.dotNet); await two.start(); two.stop();
    assert.equal(f.calls.media, 1); assert.equal(f.peers[0].closed, false); one.stop(); f.clean();
});

test('retained events and queued frames cannot act after teardown or touch a replacement', async () => {
    const f = fixture(); await f.begin(); const peer = f.peers[0];
    const events = [peer.ontrack, peer.oniceconnectionstatechange, peer.channel.onopen, peer.channel.onmessage, peer.channel.onclose];
    const frames = [...f.frames.values()]; const timers = [...f.timers.values()];
    f.api.stop(); await f.begin(); const reportCount = f.reports.length;
    const resourceCount = f.contexts.length;
    events[0]?.({ streams: [{}] }); events[1]?.(); await events[2]?.();
    events[3]?.({ data: JSON.stringify({ type: 'conversation.item.input_audio_transcription.completed', transcript: 'old' }) }); events[4]?.();
    frames.forEach(fn => fn()); timers.forEach(fn => fn()); await settle();
    assert.equal(f.reports.length, reportCount); assert.equal(f.contexts.length, resourceCount);
    assert.equal(f.peers[1].closed, false); f.api.stop(); f.clean();
});

for (const tool of [false, true]) test(`pending ${tool ? 'tool' : 'continuation'} result is silent after stop`, async () => {
    const f = fixture(); const gate = deferred();
    f.dotNet.invokeMethodAsync = (method, ...args) => { f.reports.push([method, ...args]); return method === (tool ? 'OnToolCallAsync' : 'OnTurnEndedAsync') ? gate.promise : Promise.resolve(); };
    await f.begin(); const channel = f.peers[0].channel; channel.readyState = 'open';
    channel.onmessage({ data: JSON.stringify({ type: 'response.done', response: { output: tool ? [{ type: 'function_call', call_id: 'c', name: 'tool' }] : [] } }) });
    await settle(); f.api.stop(); const count = f.reports.length; gate.resolve(tool ? '{}' : true); await settle();
    assert.equal(f.reports.length, count); assert.equal(channel.sent.length, 0); f.clean();
});

test('normal connection seeds history, reports transcripts and closes once', async () => {
    const f = fixture(); await f.begin(); const peer = f.peers[0]; peer.channel.readyState = 'open'; await peer.channel.onopen();
    assert.equal(peer.channel.sent[0].item.content[0].text, 'history');
    peer.channel.onmessage({ data: JSON.stringify({ type: 'conversation.item.input_audio_transcription.completed', transcript: 'hello' }) });
    peer.channel.close(); await settle();
    assert.equal(f.reports.filter(r => r[0] === 'OnConnected').length, 1);
    assert.equal(f.reports.filter(r => r[0] === 'OnTranscript').length, 1);
    assert.equal(f.reports.filter(r => r[0] === 'OnClosed').length, 1); f.clean();
});

test('pending handshake fetch receives abort on stop', async () => {
    const f = fixture('fetch'); const pending = f.begin(); await settle();
    assert.equal(f.calls.signal.aborted, false); f.api.stop(); assert.equal(f.calls.signal.aborted, true);
    f.finish(true); await pending; f.clean();
});

test('cleanup continues when channel close throws and audio context close rejects', async () => {
    const f = fixture(); await f.begin();
    f.peers[0].channel.close = () => { throw new Error('channel gone'); };
    f.contexts[0].close = function () { this.state = 'closed'; return Promise.reject(new Error('context gone')); };
    f.audio[0].srcObject = {};
    f.audio[0].pause = () => { throw new Error('audio gone'); };
    f.api.stop(); await settle(); f.clean();
});

test('rejected notifications and tool interop do not become unhandled promises', async () => {
    const f = fixture(); f.dotNet.invokeMethodAsync = () => Promise.reject(new Error('circuit gone'));
    await f.begin(); const peer = f.peers[0]; peer.channel.readyState = 'open'; await peer.channel.onopen();
    peer.channel.onmessage({ data: JSON.stringify({ type: 'response.done', response: { output: [{ type: 'function_call', call_id: 'c', name: 'tool' }] } }) });
    await settle(); assert.equal(peer.channel.sent.at(-1).type, 'response.create');
    peer.channel.close(); await settle(); f.clean();
});

test('user speech suppresses an old continuation while the live session remains usable', async () => {
    const f = fixture(); const gate = deferred();
    f.dotNet.invokeMethodAsync = method => method === 'OnTurnEndedAsync' ? gate.promise : Promise.resolve();
    await f.begin(); const channel = f.peers[0].channel; channel.readyState = 'open';
    channel.onmessage({ data: JSON.stringify({ type: 'response.done', response: { output: [] } }) });
    channel.onmessage({ data: JSON.stringify({ type: 'input_audio_buffer.speech_started' }) });
    gate.resolve(true); await settle(); assert.equal(channel.sent.length, 0);
    assert.equal(f.peers[0].closed, false); f.api.stop(); f.clean();
});

test('current tool results survive user interruption without starting another response', async () => {
    const f = fixture(); const gate = deferred();
    f.dotNet.invokeMethodAsync = method => method === 'OnToolCallAsync' ? gate.promise : Promise.resolve();
    await f.begin(); const channel = f.peers[0].channel; channel.readyState = 'open';
    channel.onmessage({ data: JSON.stringify({ type: 'response.done', response: { output: [{ type: 'function_call', call_id: 'c', name: 'tool' }] } }) });
    channel.onmessage({ data: JSON.stringify({ type: 'input_audio_buffer.speech_started' }) });
    gate.resolve('{}'); await settle(); assert.equal(channel.sent.length, 1);
    assert.equal(channel.sent[0].item.type, 'function_call_output'); f.api.stop(); f.clean();
});
