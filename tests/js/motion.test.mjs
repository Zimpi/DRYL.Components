import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');
class Element {
    constructor() { this.listeners = new Map(); this.animations = []; this.children = []; this.isConnected = true; this.nodeType = 1; }
    addEventListener(type, fn) { if (!this.listeners.has(type)) this.listeners.set(type, new Set()); this.listeners.get(type).add(fn); }
    removeEventListener(type, fn) { this.listeners.get(type)?.delete(fn); }
    emit(type, args = {}) { for (const fn of [...this.listeners.get(type) ?? []]) fn({ target: this, ...args }); }
    getAnimations() { return this.animations; }
    querySelectorAll() { return this.children; }
    contains(el) { return this === el || this.children.includes(el); }
    count() { return [...this.listeners.values()].reduce((sum, set) => sum + set.size, 0); }
}
function animation(target, name = 'presence-out-scale', remaining = 180) {
    let resolve, reject;
    const finished = new Promise((a, b) => { resolve = a; reject = b; });
    return { animationName: name, currentTime: 20, effect: { target, getComputedTiming: () => ({ endTime: remaining + 20 }) }, finished, resolve, reject };
}
function fixture(reduce = false) {
    const frames = new Map(), timers = new Map(), media = new Element();
    media.matches = reduce;
    let next = 0;
    const body = new Element(); body.classList = { add() {}, remove() {} };
    const document = { body, activeElement: body, addEventListener() {}, removeEventListener() {}, querySelectorAll: () => [] };
    const window = { dryl: { reduced: () => media.matches }, matchMedia: () => media };
    const context = vm.createContext({ window, document, console, Promise, WeakMap, Map,
        requestAnimationFrame: fn => { frames.set(++next, fn); return next; },
        cancelAnimationFrame: id => frames.delete(id),
        setTimeout: (fn, delay) => { timers.set(++next, { fn, delay }); return next; },
        clearTimeout: id => timers.delete(id),
    });
    for (const name of ['modal', 'motion']) {
        const start = source.indexOf(`window.dryl.${name} = (() => {`);
        vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);
    }
    const calls = [];
    const ref = { invokeMethodAsync: name => { calls.push(name); return Promise.resolve(); } };
    return { ...window.dryl, frames, timers, media, document, calls, ref,
        frame() { const list = [...frames.values()]; frames.clear(); list.forEach(fn => fn()); },
        tick() { const list = [...timers.values()]; timers.clear(); list.forEach(({ fn }) => fn()); },
        reduce() { media.matches = true; media.emit('change', { matches: true }); },
    };
}
const flush = async () => { await Promise.resolve(); await Promise.resolve(); };

test('normal matching exit completes once and clears owned work', async () => {
    const f = fixture(), el = new Element(), active = animation(el);
    el.animations = [active];
    f.motion.onExit(el, f.ref); f.frame();
    assert.deepEqual(f.calls, []);
    el.emit('animationend', { animationName: 'unrelated' });
    el.emit('animationend', { animationName: active.animationName, target: new Element() });
    assert.deepEqual(f.calls, []);
    el.emit('animationend', { animationName: active.animationName }); active.resolve(); await flush();
    assert.deepEqual(f.calls, ['OnExitFinished']);
    assert.equal(el.count() + f.frames.size + f.timers.size + f.media.count(), 0);
});
test('absent animation completes asynchronously', () => {
    const f = fixture(), el = new Element();
    f.motion.onExit(el, f.ref); assert.equal(f.calls.length, 0); f.frame();
    assert.deepEqual(f.calls, ['OnExitFinished']);
    assert.equal(el.count(), 0);
});
test('cancelled animation completes without animationend', async () => {
    const f = fixture(), el = new Element(), active = animation(el);
    el.animations = [active]; f.motion.onExit(el, f.ref); f.frame();
    el.emit('animationcancel', { animationName: active.animationName });
    active.reject(new Error('cancelled')); await flush();
    assert.deepEqual(f.calls, ['OnExitFinished']);
    assert.equal(el.count() + f.timers.size + f.media.count(), 0);
});
test('preference switching settles current exit and unregisters its media listener', () => {
    const f = fixture(), el = new Element(); el.animations = [animation(el)];
    f.motion.onExit(el, f.ref); f.frame(); f.reduce(); f.frame();
    assert.deepEqual(f.calls, ['OnExitFinished']);
    assert.equal(el.count() + f.frames.size + f.timers.size + f.media.count(), 0);
});
test('clearExit cancels a queued reduced-motion frame', () => {
    const f = fixture(true), el = new Element();
    f.motion.onExit(el, f.ref); const queued = [...f.frames.values()];
    f.motion.clearExit(el); f.motion.clearExit(el); queued.forEach(fn => fn()); f.frame();
    assert.deepEqual(f.calls, []);
    assert.equal(el.count() + f.frames.size + f.timers.size + f.media.count(), 0);
});
test('a cleared generation cannot complete a replacement exit', async () => {
    const f = fixture(), el = new Element(), old = animation(el);
    el.animations = [old]; f.motion.onExit(el, f.ref); f.frame();
    const queued = [...f.timers.values()];
    f.motion.clearExit(el);
    const current = animation(el); el.animations = [current];
    f.motion.onExit(el, { invokeMethodAsync: () => { f.calls.push('new'); return Promise.resolve(); } }); f.frame();
    old.resolve(); queued.forEach(({ fn }) => fn()); await flush();
    assert.deepEqual(f.calls, []);
    current.resolve(); await flush();
    assert.deepEqual(f.calls, ['new']);
});
test('dialog completion accepts its matching descendant and derived deadline', () => {
    const f = fixture(), el = new Element(), child = new Element();
    el.animations = [animation(child, 'dialogOut', 320)];
    f.motion.onExit(el, f.ref, { name: 'dialogOut', self: false }); f.frame();
    assert.deepEqual([...f.timers.values()].map(timer => timer.delay), [320]);
    f.tick(); assert.deepEqual(f.calls, ['OnExitFinished']);
    assert.equal(el.count() + f.media.count(), 0);
});
test('an animation promise rejection from a dead circuit is handled', async () => {
    const f = fixture(true), el = new Element();
    f.motion.onExit(el, { invokeMethodAsync: () => Promise.reject(new Error('circuit gone')) });
    f.frame(); await flush();
    assert.equal(el.count() + f.media.count(), 0);
});
test('modal detach cancels scheduled focus even if its callback was already queued', () => {
    const f = fixture(), el = new Element();
    let focused = 0; el.focus = () => focused++;
    f.modal.attach(el, f.ref); const queued = [...f.timers.values()];
    f.modal.detach(el); queued.forEach(({ fn }) => fn()); f.tick();
    assert.equal(focused, 0); assert.equal(el.count() + f.timers.size, 0);
});
test('only the current modal attachment may apply initial focus', () => {
    const f = fixture(), el = new Element();
    let focused = 0; el.focus = () => focused++;
    f.modal.attach(el, f.ref); const old = [...f.timers.values()];
    f.modal.detach(el); f.modal.attach(el, f.ref);
    old.forEach(({ fn }) => fn()); assert.equal(focused, 0);
    f.tick(); assert.equal(focused, 1); f.modal.detach(el);
});
