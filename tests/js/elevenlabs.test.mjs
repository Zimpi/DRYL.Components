import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// The ElevenLabs road through dryl-voice.js, against a fake client SDK: the module must translate
// the SDK's callbacks into the same reports an OpenAI session sends, and own nothing after stop.

const source = readFileSync(new URL('../../code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js', import.meta.url), 'utf8');
const deferred = () => { let resolve, reject; const promise = new Promise((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; };
const settle = async () => { for (let i = 0; i < 30; i++) await Promise.resolve(); };

function fixture({ preloaded = true, joinGate = null } = {}) {
    const reports = [], scripts = [], conversations = [], sessions = [];
    const timers = new Map(), frames = new Map(); let next = 0;

    const sdk = {
        VoiceConversation: {
            startSession(options) {
                sessions.push(options);
                const conversation = {
                    ended: 0, muted: [], input: 0, output: 0,
                    endSession() { this.ended++; return Promise.resolve(); },
                    setMicMuted(value) { this.muted.push(value); },
                    getInputVolume() { return this.input; },
                    getOutputVolume() { return this.output; },
                };
                conversations.push(conversation);
                return joinGate ? joinGate.promise.then(() => conversation) : Promise.resolve(conversation);
            },
        },
    };

    const sandbox = {
        console: { log() {}, warn() {} }, AbortController, Uint8Array,
        performance: { now: () => 0 },
        document: {
            createElement() { const script = {}; scripts.push(script); return script; },
            head: { appendChild() {} }, body: { appendChild() {} },
        },
        setTimeout: callback => { const id = ++next; timers.set(id, callback); return id; }, clearTimeout: id => timers.delete(id),
        requestAnimationFrame: callback => { const id = ++next; frames.set(id, callback); return id; }, cancelAnimationFrame: id => frames.delete(id),
    };
    sandbox.window = {};
    sandbox.globalThis = sandbox;
    if (preloaded) sandbox.ElevenLabsClient = sdk;

    const api = vm.runInNewContext(source.replace(/\bexport /g, '') + '\n;({ createSession })', sandbox);
    const dotNet = { invokeMethodAsync(method, ...args) { reports.push([method, ...args]); return Promise.resolve(method === 'OnToolCallAsync' ? '{"ok":true}' : undefined); } };
    const config = {
        elevenLabs: { scriptUrl: 'https://cdn.invalid/client.js', overrides: { agent: { firstMessage: 'Hola' } }, tools: ['show_map'] },
        idleMs: 100, maxMs: 200, history: [],
    };
    const open = () => api.createSession('el-token', config, dotNet);
    const clean = () => { assert.equal(timers.size, 0, 'timers'); assert.equal(frames.size, 0, 'frames'); };
    const loadScript = () => { sandbox.ElevenLabsClient = sdk; scripts.at(-1).onload(); };
    return { api, open, config, dotNet, reports, scripts, conversations, sessions, timers, frames, clean, loadScript };
}

const connect = async (f) => {
    const handle = f.open(); await handle.start();
    f.sessions[0].onConnect({ conversationId: 'conv_1' }); await settle();
    return handle;
};

test('the token, the overrides and one client tool per name reach the SDK over WebRTC', async () => {
    const f = fixture(); const handle = f.open(); await handle.start();
    const options = f.sessions[0];
    assert.equal(options.conversationToken, 'el-token');
    assert.equal(options.connectionType, 'webrtc');
    assert.equal(options.overrides.agent.firstMessage, 'Hola');
    assert.deepEqual(Object.keys(options.clientTools), ['show_map']);
    await handle.stop(); f.clean();
});

test('connect reports the conversation id before the session goes live', async () => {
    const f = fixture(); const handle = await connect(f);
    assert.deepEqual(f.reports.slice(0, 2), [['OnConversationStarted', 'conv_1'], ['OnConnected']]);
    await handle.stop(); f.clean();
});

test('messages become transcript lines of the right speaker', async () => {
    const f = fixture(); const handle = await connect(f);
    f.sessions[0].onMessage({ source: 'user', message: 'Hallo' });
    f.sessions[0].onMessage({ role: 'agent', message: 'Hola' });
    f.sessions[0].onMessage({ role: 'user', message: '' });
    await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnTranscript'), [['OnTranscript', 'User', 'Hallo'], ['OnTranscript', 'Assistant', 'Hola']]);
    await handle.stop(); f.clean();
});

test('a client tool runs on the circuit, shows as thinking, and returns the circuit result', async () => {
    const f = fixture(); const gate = deferred();
    const plain = f.dotNet.invokeMethodAsync;
    f.dotNet.invokeMethodAsync = (method, ...args) => method === 'OnToolCallAsync' ? (f.reports.push([method, ...args]), gate.promise) : plain(method, ...args);
    const handle = await connect(f);
    const result = f.sessions[0].clientTools.show_map({ place: 'Cusco' }); await settle();
    const call = f.reports.find(r => r[0] === 'OnToolCallAsync');
    assert.equal(call[2], 'show_map'); assert.equal(call[3], '{"place":"Cusco"}'); assert.match(call[1], /^el_/);
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnActivity').at(-1), ['OnActivity', 'Thinking']);
    gate.resolve('{"shown":true}');
    assert.equal(await result, '{"shown":true}'); await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnActivity').at(-1), ['OnActivity', 'Listening']);
    await handle.stop(); f.clean();
});

test('a tool that throws answers the agent with an error instead of hanging it', async () => {
    const f = fixture();
    f.dotNet.invokeMethodAsync = (method, ...args) => { f.reports.push([method, ...args]); return method === 'OnToolCallAsync' ? Promise.reject(new Error('boom')) : Promise.resolve(); };
    const handle = await connect(f);
    assert.deepEqual(JSON.parse(await f.sessions[0].clientTools.show_map({})), { error: 'boom' });
    await handle.stop(); f.clean();
});

test('speaking follows the SDK mode', async () => {
    const f = fixture(); const handle = await connect(f);
    f.sessions[0].onModeChange({ mode: 'speaking' }); await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnActivity').at(-1), ['OnActivity', 'Speaking']);
    f.sessions[0].onModeChange({ mode: 'listening' }); await settle();
    assert.deepEqual(f.reports.filter(r => r[0] === 'OnActivity').at(-1), ['OnActivity', 'Listening']);
    await handle.stop(); f.clean();
});

test('a mute set before the room is joined reaches the SDK microphone once it is', async () => {
    const f = fixture(); const handle = f.open();
    handle.setMuted(true); await handle.start();
    assert.deepEqual(f.conversations[0].muted, [true]);
    handle.setMuted(false); assert.deepEqual(f.conversations[0].muted, [true, false]);
    await handle.stop(); f.clean();
});

test('stop ends the conversation once and reports nothing further', async () => {
    const f = fixture(); const handle = await connect(f); const count = f.reports.length;
    await handle.stop(); await handle.stop();
    f.sessions[0].onDisconnect({ reason: 'user' });
    f.sessions[0].onMessage({ source: 'user', message: 'late' }); await settle();
    assert.equal(f.conversations[0].ended, 1); assert.equal(f.reports.length, count); f.clean();
});

test('a stop while the room is still being joined leaves it as soon as it is joined', async () => {
    const joinGate = deferred(); const f = fixture({ joinGate }); const handle = f.open();
    const started = handle.start(); await settle();
    await handle.stop(); joinGate.resolve(); await started; await settle();
    assert.equal(f.conversations[0].ended, 1); assert.deepEqual(f.reports, []); f.clean();
});

test('the agent hanging up closes the session, an error fails it', async () => {
    const closed = fixture(); await connect(closed);
    closed.sessions[0].onDisconnect({ reason: 'agent' }); await settle();
    assert.deepEqual(closed.reports.at(-1), ['OnClosed']); closed.clean();

    const failed = fixture(); await connect(failed);
    failed.sessions[0].onDisconnect({ reason: 'error', message: 'quota' }); await settle();
    assert.deepEqual(failed.reports.at(-1), ['OnFailed', 'quota']); failed.clean();
});

test('the client script loads once per URL and a failed load can be retried', async () => {
    const f = fixture({ preloaded: false });
    const first = f.open(); const started = first.start(); await settle();
    assert.equal(f.scripts.length, 1); assert.equal(f.scripts[0].src, 'https://cdn.invalid/client.js');
    f.scripts[0].onerror(); await started; await settle();
    assert.equal(f.reports.at(-1)[0], 'OnFailed');

    const second = f.open(); const again = second.start(); await settle();
    assert.equal(f.scripts.length, 2, 'a failed load is forgotten');
    f.loadScript(); await again; await settle();
    assert.equal(f.sessions.length, 1); await second.stop(); f.clean();
});
