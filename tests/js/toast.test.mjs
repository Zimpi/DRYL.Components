import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// dryl.toast (idea I34): which way a toast comes in, whether it may return into
// the bell, and that its timer dies with it (CODE-05). The motion itself is
// checked in the browser; here every element is a stand-in.
const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

function element({ rect = { left: 0, top: 0, width: 40, height: 40 }, connected = true } = {}) {
    const el = {
        nodeType: 1, isConnected: connected, attributes: new Map(), listeners: new Map(), children: {},
        style: {}, parentElement: null, previousElementSibling: null, nextElementSibling: null,
        getBoundingClientRect: () => ({ ...rect, right: rect.left + rect.width, bottom: rect.top + rect.height }),
        setAttribute(k, v) { this.attributes.set(k, v); },
        getAttribute(k) { return this.attributes.get(k) ?? null; },
        hasAttribute(k) { return this.attributes.has(k); },
        addEventListener(t, fn) { this.listeners.set(t, fn); },
        removeEventListener(t) { this.listeners.delete(t); },
        querySelector(sel) { return this.children[sel] ?? null; },
        querySelectorAll() { return []; },
        closest() { return null; },
        animate() { return { finished: Promise.resolve() }; },
        classList: { contains: () => false },
    };
    return el;
}

function fixture({ reduce = true, bell = null } = {}) {
    const timers = new Map();
    let next = 0;
    const document = {
        documentElement: {},
        querySelectorAll: sel => (sel === '[data-dryl-inbox]' && bell ? [bell] : []),
    };
    const window = { dryl: { reduced: () => reduce }, innerWidth: 1200, innerHeight: 800 };
    const context = vm.createContext({
        window, document, console, Promise, Math, Date, isFinite, parseFloat,
        getComputedStyle: () => ({ getPropertyValue: () => '', borderTopLeftRadius: '14px', rowGap: '12px' }),
        setTimeout: (fn, delay) => { timers.set(++next, { fn, delay }); return next; },
        clearTimeout: id => timers.delete(id),
    });
    const start = source.indexOf('window.dryl.toast = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);

    const toast = element({ rect: { left: 800, top: 600, width: 340, height: 64 } });
    const progress = element();
    progress.setAttribute('style', 'animation-duration:5000ms;');
    toast.children['.toast-progress'] = progress;
    const slot = element({ rect: { left: 800, top: 600, width: 340, height: 64 } });
    slot.children['.toast'] = toast;
    const calls = [];
    const ref = { invokeMethodAsync: name => { calls.push(name); return Promise.resolve(); } };
    return { api: window.dryl.toast, slot, timers, calls, ref };
}

test('a toast with an origin on screen comes out of it', () => {
    const f = fixture();
    assert.equal(f.api.attach(f.slot, f.ref, element()), 'origin');
    assert.ok(f.slot.hasAttribute('data-dryl-entered'));
});

test('a toast without an origin comes out of a visible bell', () => {
    const f = fixture({ bell: element() });
    assert.equal(f.api.attach(f.slot, f.ref, null), 'bell');
});

test('an origin that is no longer connected counts as none', () => {
    const f = fixture({ bell: element() });
    assert.equal(f.api.attach(f.slot, f.ref, element({ connected: false })), 'bell');
});

test('without origin and bell the toast unfolds as a capsule', () => {
    const f = fixture({ bell: element({ rect: { left: 0, top: 0, width: 0, height: 0 } }) });
    assert.equal(f.api.attach(f.slot, f.ref, null), 'capsule');
});

test('leave reports the inbox only when asked and a bell is there', async () => {
    const withBell = fixture({ bell: element() });
    assert.equal(await withBell.api.leave(withBell.slot, true), 'inbox');
    assert.equal(await withBell.api.leave(withBell.slot, false), 'plain');
    const noBell = fixture();
    assert.equal(await noBell.api.leave(noBell.slot, true), 'plain');
});

test('the timer reports its expiry', () => {
    const f = fixture();
    f.api.attach(f.slot, f.ref, null);
    const [{ fn }] = f.timers.values();
    fn();
    assert.deepEqual(f.calls, ['OnExpired']);
});

test('the timer dies with detach, and a retained callback stays inert', () => {
    const f = fixture();
    f.api.attach(f.slot, f.ref, null);
    assert.equal(f.timers.size, 1);
    const [{ fn, delay }] = f.timers.values();
    assert.equal(delay, 5000);
    f.api.detach(f.slot);
    assert.equal(f.timers.size, 0);
    fn();
    assert.deepEqual(f.calls, []);
});

test('leave stops the countdown', async () => {
    const f = fixture();
    f.api.attach(f.slot, f.ref, null);
    await f.api.leave(f.slot, false);
    assert.equal(f.timers.size, 0);
});
