// Loaded before Blazor. Only the voice endpoint and browser media/WebRTC/WebAudio
// constructors are replaced; DRYL's voice module and .NET interop remain real.
(() => {
    "use strict";

    const endpoint = "https://voice-fixture.invalid/v1/realtime/calls";
    const nativeFetch = window.fetch.bind(window);
    const nativeTimeout = window.setTimeout.bind(window);
    const nativeClearTimeout = window.clearTimeout.bind(window);
    const nativeFrame = window.requestAnimationFrame.bind(window);
    const nativeCancelFrame = window.cancelAnimationFrame.bind(window);
    const held = new Set(["media"]);
    const stages = new Set(["media", "offer", "local-description", "fetch", "answer", "remote-description", "channel-open"]);
    const pending = new Map();
    const tracks = [];
    const peers = [];
    const contexts = [];
    const timers = new Set();
    const frames = new Set();
    const calls = Object.fromEntries([...stages].map(stage => [stage, 0]));
    let sequence = 0;

    function validateStage(stage) {
        if (!stages.has(stage)) throw new Error(`Unknown voice fixture stage: ${stage}`);
    }

    function boundary(stage, value) {
        validateStage(stage);
        calls[stage]++;
        if (!held.has(stage)) return Promise.resolve().then(value);
        const id = ++sequence;
        return new Promise((resolve, reject) => pending.set(id, { id, stage, value, resolve, reject }));
    }

    function entry(stage, id) {
        validateStage(stage);
        const operation = id == null
            ? [...pending.values()].find(item => item.stage === stage)
            : pending.get(id);
        if (!operation || operation.stage !== stage) throw new Error(`No pending ${stage} operation (${id ?? "first"}).`);
        return operation;
    }

    function complete(stage, id) {
        const operation = entry(stage, id);
        pending.delete(operation.id);
        try { operation.resolve(operation.value()); }
        catch (error) { operation.reject(error); }
        return operation.id;
    }

    function reject(stage, id, message = "Fixture operation rejected") {
        const operation = entry(stage, id);
        pending.delete(operation.id);
        const error = new Error(message);
        if (stage === "media") error.name = "NotAllowedError";
        operation.reject(error);
        return operation.id;
    }

    function stream() {
        const track = {
            id: `fixture-track-${tracks.length + 1}`, kind: "audio", readyState: "live",
            stopCalls: 0,
            stop() { this.stopCalls++; this.readyState = "ended"; }
        };
        tracks.push(track);
        // An empty native stream satisfies the audio element's srcObject type;
        // its synthetic track never requests hardware or produces real audio.
        // Windows Playwright WebKit has no native MediaStream/WebRTC support.
        // This offline fixture only passes the stream to the fake peer/meter;
        // no remote track is emitted or assigned to an audio element.
        const result = typeof MediaStream === "function" ? new MediaStream() : {};
        Object.defineProperties(result, {
            getTracks: { value: () => [track] },
            getAudioTracks: { value: () => [track] }
        });
        return result;
    }

    if (!navigator.mediaDevices) Object.defineProperty(navigator, "mediaDevices", { value: {} });
    Object.defineProperty(navigator.mediaDevices, "getUserMedia", {
        configurable: true, value: () => boundary("media", stream)
    });

    class FixtureChannel extends EventTarget {
        constructor(peer) {
            super();
            this.peer = peer;
            this.label = "oai-events";
            this.readyState = "connecting";
            this.sent = [];
        }
        open() {
            if (this.readyState !== "connecting") return;
            this.readyState = "open";
            const event = new Event("open");
            this.onopen?.(event);
            this.dispatchEvent(event);
        }
        send(data) {
            if (this.readyState !== "open") throw new Error("Fixture channel is not open.");
            this.sent.push(JSON.parse(data));
        }
        close() {
            if (this.readyState === "closed") return;
            this.readyState = "closed";
            const event = new Event("close");
            this.onclose?.(event);
            this.dispatchEvent(event);
        }
        receive(payload) {
            const event = new MessageEvent("message", { data: JSON.stringify(payload) });
            this.onmessage?.(event);
            this.dispatchEvent(event);
        }
    }

    class FixturePeer extends EventTarget {
        constructor() {
            super();
            this.id = peers.length + 1;
            this.iceConnectionState = "new";
            this.iceGatheringState = "complete";
            this.connectionState = "new";
            this.closed = false;
            this.track = null;
            peers.push(this);
        }
        addTrack(track) { this.track = track; return { track }; }
        createDataChannel() { return this.channel = new FixtureChannel(this); }
        createOffer() { return boundary("offer", () => ({ type: "offer", sdp: "fixture-offer" })); }
        setLocalDescription(value) { return boundary("local-description", () => { this.localDescription = value; }); }
        async setRemoteDescription(value) {
            await boundary("remote-description", () => { this.remoteDescription = value; });
            if (this.closed) return;
            this.iceConnectionState = this.connectionState = "connected";
            // A real data channel opens independently of setRemoteDescription.
            boundary("channel-open", () => this.channel?.open()).catch(() => this.close());
        }
        close() {
            if (this.closed) return;
            this.closed = true;
            this.iceConnectionState = this.connectionState = "closed";
            this.channel?.close();
            this.oniceconnectionstatechange?.(new Event("iceconnectionstatechange"));
        }
    }
    window.RTCPeerConnection = FixturePeer;

    class FixtureAudioContext {
        constructor() { this.state = "running"; contexts.push(this); }
        resume() { this.state = "running"; return Promise.resolve(); }
        close() { this.state = "closed"; return Promise.resolve(); }
        createMediaStreamSource() { return { connect() {}, disconnect() {} }; }
        createAnalyser() {
            return { fftSize: 256, frequencyBinCount: 128, getByteTimeDomainData: values => values.fill(128) };
        }
    }
    window.AudioContext = FixtureAudioContext;
    window.webkitAudioContext = FixtureAudioContext;

    window.fetch = (input, options) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url === endpoint) {
            return boundary("fetch", () => ({
                ok: true, status: 200,
                text: () => boundary("answer", () => "fixture-answer")
            }));
        }
        if (url.includes("voice-fixture.invalid")) return Promise.reject(new Error(`Unexpected fixture URL: ${url}`));
        return nativeFetch(input, options);
    };

    // Count only scheduling calls originating in the actual DRYL voice module.
    // Other components and Blazor retain their native clocks and frame behavior.
    const fromVoice = () => new Error().stack?.includes("dryl-voice.js") === true;
    window.setTimeout = function (callback, delay, ...args) {
        if (!fromVoice()) return nativeTimeout(callback, delay, ...args);
        const id = nativeTimeout(() => { timers.delete(id); callback(...args); }, delay);
        timers.add(id);
        return id;
    };
    window.clearTimeout = id => { timers.delete(id); nativeClearTimeout(id); };
    window.requestAnimationFrame = callback => {
        if (!fromVoice()) return nativeFrame(callback);
        const id = nativeFrame(time => { frames.delete(id); callback(time); });
        frames.add(id);
        return id;
    };
    window.cancelAnimationFrame = id => { frames.delete(id); nativeCancelFrame(id); };

    window.voiceFixture = {
        hold(stage, enabled = true) { validateStage(stage); enabled ? held.add(stage) : held.delete(stage); },
        complete,
        reject,
        completeMedia: id => complete("media", id),
        rejectMedia: (id, message) => reject("media", id, message),
        pending: () => [...pending.values()].map(({ id, stage }) => ({ id, stage })),
        stats: () => ({
            calls: { ...calls }, pending: pending.size,
            tracksCreated: tracks.length,
            tracksStopped: tracks.filter(track => track.readyState === "ended").length,
            liveTracks: tracks.filter(track => track.readyState === "live").length,
            peersCreated: peers.length, livePeers: peers.filter(peer => !peer.closed).length,
            liveChannels: peers.filter(peer => peer.channel && peer.channel.readyState !== "closed").length,
            contextsCreated: contexts.length, liveContexts: contexts.filter(context => context.state !== "closed").length,
            audioElements: document.querySelectorAll("audio").length,
            timers: timers.size, frames: frames.size
        }),
        emit(payload, peerId) {
            const peer = peerId == null ? peers.at(-1) : peers.find(item => item.id === peerId);
            if (!peer?.channel) throw new Error("No fixture data channel.");
            peer.channel.receive(payload);
        },
        sent: peerId => (peerId == null ? peers.at(-1) : peers.find(peer => peer.id === peerId))?.channel?.sent ?? [],
        connection: () => {
            const peer = peers.at(-1);
            return peer ? { channelState: peer.channel?.readyState, localSdp: peer.localDescription?.sdp, remoteSdp: peer.remoteDescription?.sdp } : null;
        },
        // No reset method clears leaked resources: every test uses a fresh page,
        // and a stop/disposal regression must expose the resources it left alive.
        endpoint
    };
})();
