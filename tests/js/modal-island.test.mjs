import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// F6 moves between the open dialog and an island (the canvas dock); Tab stays inside
// whichever holds focus; without a dialog nothing is intercepted.

const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

class Node {
    constructor(tag = 'div', doc) {
        this.tag = tag; this.doc = doc; this.children = []; this.parent = null;
        this.attrs = {}; this.listeners = new Map(); this.isConnected = true;
        this.offsetParent = {}; this.disabled = false;
    }
    append(...kids) { for (const k of kids) { k.parent = this; this.children.push(k); } return this; }
    setAttribute(n, v) { this.attrs[n] = v; }
    addEventListener(t, fn) { if (!this.listeners.has(t)) this.listeners.set(t, new Set()); this.listeners.get(t).add(fn); }
    removeEventListener(t, fn) { this.listeners.get(t)?.delete(fn); }
    contains(n) { for (let c = n; c; c = c.parent) if (c === this) return true; return false; }
    descendants() { return this.children.flatMap(c => [c, ...c.descendants()]); }
    querySelectorAll() { return this.descendants().filter(n => ['button', 'textarea', 'input'].includes(n.tag) && !n.disabled); }
    matches(sel) { return sel.split(',').map(x => x.trim()).includes(this.tag); }
    getClientRects() { return this.hidden ? [] : [{}]; }
    focus() { this.doc.activeElement = this; }
}

function fixture() {
    const docListeners = new Map();
    const document = {
        activeElement: null,
        body: null,
        addEventListener(t, fn) { if (!docListeners.has(t)) docListeners.set(t, new Set()); docListeners.get(t).add(fn); },
        removeEventListener(t, fn) { docListeners.get(t)?.delete(fn); },
        querySelectorAll(sel) {
            assert.equal(sel, '[data-dryl-modal-island]');
            return document.body.descendants().filter(n => 'data-dryl-modal-island' in n.attrs);
        },
    };
    const body = new Node('body', document); body.classList = { add() {}, remove() {} };
    document.body = body; document.activeElement = body;
    const make = tag => new Node(tag, document);

    const dialog = make('div'), ok = make('button'), cancel = make('button');
    dialog.append(ok, cancel);
    const dock = make('div'), collapse = make('button'), composer = make('textarea'), send = make('button');
    dock.setAttribute('data-dryl-modal-island', '');
    dock.append(collapse, composer, send);
    const page = make('button');
    body.append(page, dock, dialog);

    const timers = [];
    const context = vm.createContext({
        window: { dryl: {} }, document, console, Promise, WeakMap, Map, Array,
        setTimeout: fn => { timers.push(fn); return timers.length; }, clearTimeout() {},
    });
    const start = source.indexOf('window.dryl.modal = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);

    const key = (k, shiftKey = false) => {
        const e = { key: k, shiftKey, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; } };
        for (const fn of [...docListeners.get('keydown') ?? []]) fn(e);
        return e;
    };
    const ref = { invokeMethodAsync: () => Promise.resolve() };
    return { modal: context.window.dryl.modal, document, dialog, ok, cancel, dock, collapse, composer, send, page, key, ref,
        listeners: () => docListeners.get('keydown')?.size ?? 0, tick: () => timers.splice(0).forEach(fn => fn()) };
}

test('without an open dialog F6 is left to the browser', () => {
    const f = fixture();
    assert.equal(f.listeners(), 0);
});

test('F6 moves focus from the dialog into the island text field', () => {
    const f = fixture();
    f.modal.attach(f.dialog, f.ref, {}); f.tick();
    f.cancel.focus();
    const e = f.key('F6');
    assert.equal(e.defaultPrevented, true);
    assert.equal(f.document.activeElement, f.composer);
});

test('F6 from the island returns to where focus left the dialog', () => {
    const f = fixture();
    f.modal.attach(f.dialog, f.ref, {}); f.tick();
    f.cancel.focus(); f.key('F6');
    f.key('F6');
    assert.equal(f.document.activeElement, f.cancel);
});

test('Tab inside the island wraps instead of walking onto the page', () => {
    const f = fixture();
    f.modal.attach(f.dialog, f.ref, {}); f.tick();
    f.send.focus();
    const e = f.key('Tab');
    assert.equal(e.defaultPrevented, true);
    assert.equal(f.document.activeElement, f.collapse);
    f.collapse.focus();
    f.key('Tab', true);
    assert.equal(f.document.activeElement, f.send);
});

test('a hidden island is not a target and F6 stays the browser\'s', () => {
    const f = fixture();
    f.dock.hidden = true;
    f.modal.attach(f.dialog, f.ref, {}); f.tick();
    f.ok.focus();
    const e = f.key('F6');
    assert.equal(e.defaultPrevented, false);
    assert.equal(f.document.activeElement, f.ok);
});

test('closing the last dialog removes the document listener', () => {
    const f = fixture();
    f.modal.attach(f.dialog, f.ref, {});
    assert.equal(f.listeners(), 1);
    f.modal.detach(f.dialog);
    assert.equal(f.listeners(), 0);
});

test('closing the dialog while focus sits in the island leaves it there', () => {
    // The user is typing in the dock while the assistant closes the map: focus stays put.
    const f = fixture();
    f.page.focus();
    f.modal.attach(f.dialog, f.ref, {}); f.tick();
    f.key('F6');
    assert.equal(f.document.activeElement, f.composer);
    f.modal.detach(f.dialog);
    assert.equal(f.document.activeElement, f.composer);
});
