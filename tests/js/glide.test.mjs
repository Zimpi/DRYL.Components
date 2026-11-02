import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// dryl.motion.glideScan / glideRelease (idea I38): which row the list ink covers,
// when it travels and when it only appears, and that a released list keeps
// nothing behind (CODE-05). The look is checked in the browser; here every
// element is a stand-in with fixed offsets.
const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

class Node {
    constructor() { this.nodeType = 1; this.isConnected = true; this.listeners = new Map(); this.attrs = new Map(); }
    addEventListener(t, fn) { if (!this.listeners.has(t)) this.listeners.set(t, new Set()); this.listeners.get(t).add(fn); }
    removeEventListener(t, fn) { this.listeners.get(t)?.delete(fn); }
    emit(t, e = {}) { for (const fn of [...this.listeners.get(t) ?? []]) fn({ target: this, ...e }); }
    count() { return [...this.listeners.values()].reduce((n, s) => n + s.size, 0); }
    setAttribute(k, v) { this.attrs.set(k, String(v)); }
    getAttribute(k) { return this.attrs.has(k) ? this.attrs.get(k) : null; }
    hasAttribute(k) { return this.attrs.has(k); }
    removeAttribute(k) { this.attrs.delete(k); }
}

class Row extends Node {
    constructor(host, i, { danger = false, disabled = false } = {}) {
        super();
        this.host = host; this.offsetParent = host;
        this.offsetLeft = 4; this.offsetTop = 4 + i * 34; this.offsetWidth = 200; this.offsetHeight = 34;
        this.classes = new Set(danger ? ['menu-item', 'menu-item--danger'] : ['menu-item']);
        this.classList = { contains: c => this.classes.has(c) };
        this.disabled = disabled;
    }
    matches() { return !this.disabled; }
    closest() { return this; }
    getBoundingClientRect() { return { left: 100 + this.offsetLeft, top: 100 + this.offsetTop, width: this.offsetWidth, height: this.offsetHeight }; }
}

class Host extends Node {
    constructor() {
        super();
        this.props = new Map();
        this.style = { setProperty: (k, v) => this.props.set(k, v), removeProperty: k => this.props.delete(k) };
        this.rows = [];
    }
    matches(sel) { return sel.includes('[role="menu"]'); }
    contains(el) { return el === this || this.rows.includes(el); }
    querySelector(sel) { return sel === '.is-highlighted' ? this.rows.find(r => r.classes.has('is-highlighted')) ?? null : null; }
    querySelectorAll() { return []; }
    closest() { return null; }
}

function fixture() {
    const frames = new Map(), observers = [];
    let next = 0;
    const document = { activeElement: null };
    class Observer { constructor(fn) { this.fn = fn; this.live = false; observers.push(this); } observe() { this.live = true; } disconnect() { this.live = false; } }
    const window = { dryl: { reduced: () => false } };
    const context = vm.createContext({
        window, document, console, Promise, WeakMap, Map, Set,
        MutationObserver: Observer, ResizeObserver: Observer,
        requestAnimationFrame: fn => { frames.set(++next, fn); return next; },
        cancelAnimationFrame: id => frames.delete(id),
        getComputedStyle: () => ({ overflowY: 'visible' }),
        setTimeout: () => 0, clearTimeout: () => {},
    });
    const start = source.indexOf('window.dryl.motion = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);

    const host = new Host();
    host.rows = [new Row(host, 0), new Row(host, 1), new Row(host, 2, { danger: true })];
    return {
        motion: window.dryl.motion, document, host, observers,
        frame() { const list = [...frames.values()]; frames.clear(); list.forEach(fn => fn()); },
        frames,
        mouse: (type, row) => (type === 'pointerleave' ? host : host).emit(type, { target: row ?? host, pointerType: 'mouse', clientX: 140 }),
    };
}

test('a menu opens with the ink on its focused row, placed rather than flown in', () => {
    const f = fixture();
    f.document.activeElement = f.host.rows[0];
    f.motion.glideScan(f.host);

    assert.equal(f.host.getAttribute('data-glide'), 'place');
    assert.ok(f.host.hasAttribute('data-glide-on'));
    assert.equal(f.host.props.get('--glide-y'), '4px');
    assert.equal(f.host.props.get('--glide-h'), '34px');
    f.frame();
    assert.equal(f.host.getAttribute('data-glide'), 'glide');
});

test('the mouse moves the ink with the spring, and a danger row recolours it', () => {
    const f = fixture();
    f.document.activeElement = f.host.rows[0];
    f.motion.glideScan(f.host); f.frame();

    f.mouse('pointerover', f.host.rows[2]);
    assert.equal(f.host.getAttribute('data-glide'), 'glide');
    assert.equal(f.host.props.get('--glide-y'), '72px');
    assert.equal(f.host.getAttribute('data-glide-tone'), 'danger');

    f.mouse('pointerover', f.host.rows[1]);
    assert.equal(f.host.getAttribute('data-glide-tone'), null);
});

test('leaving falls back to the focused row, and without one the ink fades', () => {
    const f = fixture();
    f.document.activeElement = f.host.rows[0];
    f.motion.glideScan(f.host); f.frame();
    f.mouse('pointerover', f.host.rows[1]);
    f.mouse('pointerleave');
    assert.equal(f.host.props.get('--glide-y'), '4px');

    f.document.activeElement = null;
    f.mouse('pointerover', f.host.rows[1]);
    f.mouse('pointerleave');
    assert.equal(f.host.hasAttribute('data-glide-on'), false);
});

test('touch never moves the ink', () => {
    const f = fixture();
    f.motion.glideScan(f.host);
    f.host.emit('pointerover', { target: f.host.rows[1], pointerType: 'touch' });
    assert.equal(f.host.hasAttribute('data-glide-on'), false);
});

test('a listbox highlight moved by the keyboard carries the ink with it', () => {
    const f = fixture();
    f.host.rows[0].classes.add('is-highlighted');
    f.motion.glideScan(f.host); f.frame();
    assert.equal(f.host.props.get('--glide-y'), '4px');

    f.host.rows[0].classes.delete('is-highlighted');
    f.host.rows[1].classes.add('is-highlighted');
    f.observers[0].fn(); // the host's MutationObserver — Blazor rewrote a class
    assert.equal(f.host.props.get('--glide-y'), '38px');
    assert.equal(f.host.getAttribute('data-glide'), 'glide');
});

test('a disabled row is never covered', () => {
    const f = fixture();
    f.host.rows[1].disabled = true;
    f.motion.glideScan(f.host); f.frame();
    f.mouse('pointerover', f.host.rows[1]);
    assert.equal(f.host.hasAttribute('data-glide-on'), false);
});

test('release leaves no attribute, property, listener, observer or frame behind', () => {
    const f = fixture();
    f.document.activeElement = f.host.rows[0];
    f.motion.glideScan(f.host);
    f.host.emit('pointermove', { target: f.host.rows[0], pointerType: 'mouse', clientX: 140 });
    f.motion.glideRelease(f.host);

    assert.equal(f.host.attrs.size, 0);
    assert.equal(f.host.props.size, 0);
    assert.equal(f.host.count(), 0);
    assert.equal(f.observers.filter(o => o.live).length, 0);
    assert.equal(f.frames.size, 0);
});
