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
        constructor() { this.closed = false; this.iceGatheringState = 'complete'; this.localDescription = { sdp: 'gathered-offer' }; peers.push(this); }
        addTrack() {}
        addEventListener(name, handler) { this.listeners ??= new Map(); this.listeners.set(name, handler); }
        removeEventListener(name) { this.listeners?.delete(name); }
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
    const config = { live: true, baseUrl: 'https://fixture.invalid', idleMs: 100, maxMs: 200, history: [{ role: 'User', text: 'history' }] };
    const begin = () => api.start('token', config, dotNet);
    const finish = (reject = false) => { const item = pending.shift(); assert.ok(item, 'a boundary is pending'); reject ? item.gate.reject(new Error('late failure')) : item.gate.resolve(item.value()); };
    const clean = () => {
        assert.equal(tracks.filter(t => !t.stopped).length, 0, 'microphone tracks');
        assert.equal(peers.filter(p => !p.closed).length, 0, 'peers');
        assert.equal(contexts.filter(c => c.state !== 'closed').length, 0, 'contexts');
        assert.equal(audio.filter(a => !a.removed || a.srcObject).length, 0, 'audio elements');
        assert.equal(timers.size, 0, 'timers'); assert.equal(frames.size, 0, 'frames');
    };
    return { api, begin, config, dotNet, finish, pending, calls, reports, peers, tracks, contexts, audio, timers, frames, clean, hold: name => { held = name; } };
}

function emit(channel, event) { channel.onmessage({ data: JSON.stringify(event) }); }
function backend(channel, event, delegation = 'd') { emit(channel, { type: 'response.event', delegation_id: delegation, event }); }
async function connected(f) {
    await f.begin(); const channel = f.peers.at(-1).channel;
    channel.readyState = 'open'; await channel.onopen();
    emit(channel, { type: 'session.started' }); await settle(); return channel;
}
function functionResponse(channel, id = 'r', status = 'completed', calls = ['c']) {
    backend(channel, { type: 'response.created', response: { id } });
    calls.forEach((call, i) => backend(channel, { type: 'response.output_item.done', output_index: i,
        item: { id: `item_${call}`, type: 'function_call', call_id: call, name: 'lookup', arguments: '{"q":"yes"}' } }));
    backend(channel, { type: `response.${status}`, response: { id, status, output: [] } });
}
async function closed(f, channel) {
    const stop = f.api.stop(); emit(channel, { type: 'session.closed', usage: { seconds: 12 }, close_reason: 'client' });
    await stop; await settle(); f.clean();
}

test('Live offer stays on server interop; channel open is not readiness and sends no Realtime commands', async () => {
    const f = fixture(); await f.begin();
    assert.equal(f.calls.fetch ?? 0, 0);
    assert.deepEqual(f.reports.find(r => r[0] === 'OnLiveOfferAsync'), ['OnLiveOfferAsync', 'gathered-offer']);
    const channel = f.peers[0].channel; channel.readyState = 'open'; await channel.onopen();
    assert.equal(f.reports.some(r => r[0] === 'OnConnected'), false);
    assert.equal(channel.sent.length, 0);
    emit(channel, { type: 'session.started' }); emit(channel, { type: 'session.started' }); await settle();
    assert.equal(f.reports.filter(r => r[0] === 'OnConnected').length, 1);
    assert.equal(channel.sent.length, 0); await closed(f, channel);
});

test('Live functions come from output_item.done, execute once, and all outputs precede one continuation', async () => {
    const f = fixture(); const channel = await connected(f);
    functionResponse(channel, 'r', 'completed', ['one', 'two']); await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnToolCallAsync').map(r => r[1]), ['one', 'two']);
    assert.deepEqual(Array.from(channel.sent, e => e.type), ['response.item.create', 'response.item.create', 'response.create']);
    assert.ok(channel.sent.every(e => typeof e.event_id === 'string'));
    assert.equal(channel.sent[2].response, undefined);
    functionResponse(channel, 'r', 'completed', ['one', 'two']);
    functionResponse(channel, 'other', 'completed', ['one']); await settle();
    assert.equal(channel.sent.length, 3); await closed(f, channel);
});

for (const status of ['failed', 'incomplete', 'cancelled']) test(`Live ${status} backend cannot execute staged actions`, async () => {
    const f = fixture(); const channel = await connected(f); functionResponse(channel, 'r', status); await settle();
    assert.equal(f.reports.some(r => r[0] === 'OnToolCallAsync'), false);
    assert.equal(channel.sent.length, 0); await closed(f, channel);
});

test('speech overlaps backend work without suppressing its continuation; fragments stay exact', async () => {
    const f = fixture(); const gate = deferred();
    const original = f.dotNet.invokeMethodAsync;
    f.dotNet.invokeMethodAsync = (method, ...args) => method === 'OnToolCallAsync' ? gate.promise : original(method, ...args);
    const channel = await connected(f); functionResponse(channel); await settle();
    for (const [type, delta, start_ms, end_ms] of [
        ['session.input_transcript.delta', 'ja ', 10, 25], ['session.output_transcript.delta', 'Okay.', 20, 50],
        ['session.input_transcript.delta', 'ja', 25, 40]]) emit(channel, { type, delta, start_ms, end_ms });
    gate.resolve('{}'); await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnLiveTranscriptDelta').map(r => r.slice(1)),
        [['User', 'ja ', 10, 25], ['Assistant', 'Okay.', 20, 50], ['User', 'ja', 25, 40]]);
    assert.deepEqual(Array.from(channel.sent, e => e.type), ['response.item.create', 'response.create']);
    await closed(f, channel);
});

test('hosted web search items and citations survive empty terminal output', async () => {
    const f = fixture(); const channel = await connected(f);
    backend(channel, { type: 'response.created', response: { id: 'r' } });
    const items = [{ id: 'web', type: 'web_search_call', status: 'completed' },
        { id: 'msg', type: 'message', content: [{ type: 'output_text', text: 'Answer', annotations: [{ type: 'url_citation', url: 'https://openai.com' }] }] }];
    items.forEach(item => backend(channel, { type: 'response.output_item.done', item }));
    backend(channel, { type: 'response.completed', response: { id: 'r', status: 'completed', output: [], usage: { total_tokens: 23 } } }); await settle();
    const response = f.reports.find(r => r[0] === 'OnBackendResponseAsync')[1];
    assert.deepEqual(JSON.parse(JSON.stringify(response.output)), items);
    assert.equal(response.usage.total_tokens, 23); assert.equal(channel.sent.length, 0); await closed(f, channel);
});

test('stop cuts microphone immediately, drains final transcript and usage, and suppresses late tool completion', async () => {
    const f = fixture(); const gate = deferred();
    const original = f.dotNet.invokeMethodAsync;
    f.dotNet.invokeMethodAsync = (method, ...args) => method === 'OnToolCallAsync' ? gate.promise : original(method, ...args);
    const channel = await connected(f); functionResponse(channel); await settle();
    const stop = f.api.stop(); assert.equal(f.peers[0].closed, false);
    assert.ok(f.tracks.every(track => track.stopped), 'capture stops before waiting for final events');
    assert.equal(channel.sent[0].type, 'session.close');
    assert.equal(f.api.stop(), stop);
    emit(channel, { type: 'session.output_transcript.delta', delta: 'Bye', start_ms: 40, end_ms: 70 });
    emit(channel, { type: 'session.usage.updated', usage: { seconds: 9 } });
    gate.resolve('{}'); await settle(); assert.equal(channel.sent.length, 1);
    emit(channel, { type: 'session.closed', usage: { seconds: 12 }, close_reason: 'client' }); await stop; await settle();
    assert.ok(f.reports.some(r => r[0] === 'OnLiveSessionClosed' && r[1].usage.seconds === 12));
    assert.ok(f.reports.some(r => r[0] === 'OnLiveUsage' && r[1] === 9));
    f.clean();
});

test('graceful close has a bounded fallback and releases page ownership', async () => {
    const f = fixture(); await connected(f); const stop = f.api.stop();
    [...f.timers.values()].forEach(fn => fn()); await stop; f.clean();
    const channel = await connected(f); await closed(f, channel);
});

for (const stage of ['media', 'offer', 'local', 'remote']) test(`Live stop during ${stage} cannot resume cancelled acquisition`, async () => {
    const f = fixture(stage); const start = f.begin(); await settle(); f.api.stop();
    const count = f.reports.length; f.finish(); await start; await settle();
    assert.equal(f.reports.length, count); f.clean();
});

test('late server answer cannot set a remote description after stop or clear replacement owner', async () => {
    const f = fixture(); const gate = deferred(); let offered = false;
    const original = f.dotNet.invokeMethodAsync;
    f.dotNet.invokeMethodAsync = (method, ...args) => {
        if (method === 'OnLiveOfferAsync' && !offered) { offered = true; return gate.promise; }
        return original(method, ...args);
    };
    const old = f.begin(); await settle(); f.api.stop(); const channel = await connected(f);
    gate.resolve('old-answer'); await old;
    assert.equal(f.calls.remote, 1); assert.equal(f.peers[1].closed, false); await closed(f, channel);
});

for (const outcome of ['complete', 'cancel', 'timeout']) test(`ICE gathering ${outcome} obeys startup ownership and bounds`, async () => {
    const f = fixture('local'); const start = f.begin(); await settle();
    const peer = f.peers[0]; peer.iceGatheringState = 'gathering'; f.finish(); await settle();
    assert.equal(f.reports.some(r => r[0] === 'OnLiveOfferAsync'), false);
    assert.equal(peer.listeners.size, 1);
    if (outcome === 'complete') {
        peer.iceGatheringState = 'complete'; peer.listeners.get('icegatheringstatechange')(); await start;
        assert.equal(f.reports.filter(r => r[0] === 'OnLiveOfferAsync').length, 1); f.api.stop();
    } else if (outcome === 'cancel') {
        f.api.stop(); await start; assert.equal(f.reports.length, 0);
    } else {
        [...f.timers.values()].forEach(fn => fn()); await start;
        assert.equal(f.reports.filter(r => r[0] === 'OnFailed').length, 1);
    }
    assert.equal(peer.listeners.size, 0); f.clean();
});

test('missing session.started is bounded and releases microphone and page ownership', async () => {
    const f = fixture(); await f.begin(); const timers = [...f.timers.values()];
    timers.forEach(fn => fn()); await settle();
    assert.equal(f.reports.filter(r => r[0] === 'OnFailed').length, 1); f.clean();
});
