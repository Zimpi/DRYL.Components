import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// dryl.motion.treeGlide / disposeTreeGlide (idea I41): which row the ink covers, how
// long each thread is, when the ink springs and when it tracks, and that a released
// tree keeps nothing behind (CODE-05). The look is checked in the browser; here every
// element is a stand-in with fixed offsets.
const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

class El {
    constructor(classes = '') {
        this.nodeType = 1; this.isConnected = true;
        this.classes = new Set(classes.split(' ').filter(Boolean));
        this.classList = { contains: c => this.classes.has(c) };
        this.attrs = new Map(); this.props = new Map(); this.listeners = new Map(); this.kids = [];
        this.parentElement = null; this.offsetParent = null;
        this.offsetLeft = 0; this.offsetTop = 0; this.offsetWidth = 200; this.offsetHeight = 30;
        this.style = { setProperty: (k, v) => this.props.set(k, v), removeProperty: k => this.props.delete(k) };
    }
    add(...kids) { for (const k of kids) { k.parentElement = this; this.kids.push(k); } return this; }
    setAttribute(k, v) { this.attrs.set(k, String(v)); }
    getAttribute(k) { return this.attrs.has(k) ? this.attrs.get(k) : null; }
    hasAttribute(k) { return this.attrs.has(k); }
    removeAttribute(k) { this.attrs.delete(k); }
    addEventListener(t, fn) { if (!this.listeners.has(t)) this.listeners.set(t, new Set()); this.listeners.get(t).add(fn); }
    removeEventListener(t, fn) { this.listeners.get(t)?.delete(fn); }
    emit(t, e = {}) { for (const fn of [...this.listeners.get(t) ?? []]) fn({ target: this, ...e }); }
    listenerCount() { return [...this.listeners.values()].reduce((n, s) => n + s.size, 0); }
    contains(el) { for (let n = el; n; n = n.parentElement) if (n === this) return true; return false; }
    * walk() { for (const k of this.kids) { yield k; yield* k.walk(); } }
    matchesSel(sel) {
        if (sel === '.tree-fold:not(.is-open)') return this.classes.has('tree-fold') && !this.classes.has('is-open');
        if (sel.startsWith('.') && !sel.includes(':')) return sel.slice(1).split('.').every(c => this.classes.has(c));
        return false;
    }
    closest(sel) { for (let n = this; n; n = n.parentElement) if (n.matchesSel(sel)) return n; return null; }
    querySelector(sel) {
        if (sel === ':scope > .tree-row') return this.kids.find(k => k.classes.has('tree-row')) ?? null;
        for (const n of this.walk()) if (n.matchesSel(sel)) return n;
        return null;
    }
}

// root (y 8)
//   docs     folder, open
//     guide  row in the group at y 0
//     api    folder, open: its row at y 30, its group's rows below
//       v1   row in the group at y 0
//       v2   row in the group at y 30
//   readme   row below
function tree() {
    const host = new El('tree');
    const row = (y, offsetParent) => { const r = new El('tree-row'); r.offsetTop = y; r.offsetParent = offsetParent; return r; };
    const folder = (y, offsetParent, parentGroup) => {
        const li = new El(), r = row(y, offsetParent), fold = new El('tree-fold is-open'), clip = new El(), group = new El('tree-group');
        group.offsetParent = offsetParent; group.offsetTop = y + 30;
        fold.add(clip.add(group)); li.add(r, fold);
        (parentGroup ?? host).add(li);
        return { r, fold, group };
    };
    const leaf = (y, offsetParent, parent) => { const li = new El(), r = row(y, offsetParent); li.add(r); parent.add(li); return r; };

    const docs = folder(8, host);
    const guide = leaf(0, docs.group, docs.group);
    const api = folder(30, docs.group, docs.group);
    const v1 = leaf(0, api.group, api.group);
    const v2 = leaf(30, api.group, api.group);
    const readme = leaf(120, host, host);
    return { host, docs, api, guide, v1, v2, readme };
}

function fixture() {
    const frames = new Map(), observers = [];
    let next = 0;
    class Observer { constructor(fn) { this.fn = fn; this.live = false; observers.push(this); } observe() { this.live = true; } disconnect() { this.live = false; } }
    const window = { dryl: { reduced: () => false } };
    const context = vm.createContext({
        window, document: { activeElement: null }, console, Promise, WeakMap, Map, Set,
        MutationObserver: Observer, ResizeObserver: Observer,
        requestAnimationFrame: fn => { frames.set(++next, fn); return next; },
        cancelAnimationFrame: id => frames.delete(id),
        getComputedStyle: () => ({ overflowY: 'visible' }),
        setTimeout: () => 0, clearTimeout: () => {},
    });
    const start = source.indexOf('window.dryl.motion = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);
    const t = tree();
    return {
        ...t, motion: window.dryl.motion, frames, observers,
        frame() { const list = [...frames.values()]; frames.clear(); list.forEach(fn => fn()); },
        changed() { observers[0].fn(); },
        select(row) { for (const n of t.host.walk()) n.classes.delete('is-selected'); row.classes.add('is-selected'); },
    };
}

test('the ink starts on the selected row, placed rather than flown in, then arms the spring', () => {
    const f = fixture();
    f.select(f.readme);
    f.motion.treeGlide(f.host);

    assert.equal(f.host.getAttribute('data-tree-ink'), 'place');
    assert.ok(f.host.hasAttribute('data-tree-ink-on'));
    assert.equal(f.host.props.get('--tree-ink-y'), '120px');
    f.frame();
    assert.equal(f.host.getAttribute('data-tree-ink'), 'glide');
});

test('with nothing selected the ink stays off', () => {
    const f = fixture();
    f.motion.treeGlide(f.host);

    assert.equal(f.host.hasAttribute('data-tree-ink-on'), false);
    assert.equal(f.host.getAttribute('data-tree-ink'), 'place');
});

test('a deep selection runs a thread through every level, each to the row on the path', () => {
    const f = fixture();
    f.select(f.v2);
    f.motion.treeGlide(f.host);

    assert.equal(f.host.props.get('--tree-ink-y'), (38 + 60 + 30) + 'px');
    assert.equal(f.api.group.props.get('--tree-thread'), '45px');
    assert.equal(f.docs.group.props.get('--tree-thread'), '45px');
    assert.ok(f.api.group.hasAttribute('data-tree-path'));
    assert.ok(f.docs.group.hasAttribute('data-tree-path'));
    assert.ok(f.api.r.hasAttribute('data-tree-path'));
    assert.ok(f.docs.r.hasAttribute('data-tree-path'));
});

test('moving the selection withdraws the threads that are no longer on the path', () => {
    const f = fixture();
    f.select(f.v2);
    f.motion.treeGlide(f.host); f.frame();

    f.select(f.guide); f.changed();

    assert.equal(f.host.getAttribute('data-tree-ink'), 'glide');
    assert.equal(f.api.group.hasAttribute('data-tree-path'), false);
    assert.equal(f.api.group.props.get('--tree-thread'), '0px');
    assert.equal(f.api.r.hasAttribute('data-tree-path'), false);
    assert.equal(f.docs.group.props.get('--tree-thread'), '15px');
});

test('closing a folder around the selection retreats the ink onto the outermost closed folder', () => {
    const f = fixture();
    f.select(f.v1);
    f.motion.treeGlide(f.host); f.frame();

    f.api.fold.classes.delete('is-open'); f.changed();
    assert.ok(f.host.hasAttribute('data-tree-ink-retreat'));
    assert.equal(f.host.props.get('--tree-ink-y'), (38 + 30) + 'px');

    f.docs.fold.classes.delete('is-open'); f.changed();
    assert.equal(f.host.props.get('--tree-ink-y'), '8px');
    assert.equal(f.docs.group.hasAttribute('data-tree-path'), false);

    f.docs.fold.classes.add('is-open'); f.api.fold.classes.add('is-open'); f.changed();
    assert.equal(f.host.hasAttribute('data-tree-ink-retreat'), false);
    assert.equal(f.host.props.get('--tree-ink-y'), (38 + 60) + 'px');
});

test('a folder that moves the covered row is tracked, then the spring comes back', () => {
    const f = fixture();
    f.select(f.readme);
    f.motion.treeGlide(f.host); f.frame();

    const fold = f.api.fold;
    f.host.emit('transitionrun', { target: fold, propertyName: 'grid-template-rows' });
    assert.equal(f.host.getAttribute('data-tree-ink'), 'track');

    f.readme.offsetTop = 180;
    f.frame();
    assert.equal(f.host.props.get('--tree-ink-y'), '180px');

    f.host.emit('transitionend', { target: fold, propertyName: 'grid-template-rows' });
    f.frame(); f.frame(); f.frame();
    assert.equal(f.host.getAttribute('data-tree-ink'), 'glide');
    assert.equal(f.frames.size, 0);
});

test('a folder that retargets the ink keeps it on the spring through its own transition', () => {
    const f = fixture();
    f.select(f.v1);
    f.motion.treeGlide(f.host); f.frame();

    f.api.fold.classes.delete('is-open'); f.changed();
    f.host.emit('transitionrun', { target: f.api.fold, propertyName: 'grid-template-rows' });

    assert.equal(f.host.getAttribute('data-tree-ink'), 'glide');
});

test('other transitions on the way do not start the track', () => {
    const f = fixture();
    f.select(f.readme);
    f.motion.treeGlide(f.host); f.frame();

    f.host.emit('transitionrun', { target: f.readme, propertyName: 'color' });

    assert.equal(f.host.getAttribute('data-tree-ink'), 'glide');
});

test('dispose leaves nothing behind', () => {
    const f = fixture();
    f.select(f.v2);
    f.motion.treeGlide(f.host);
    f.motion.disposeTreeGlide(f.host);

    assert.equal(f.host.listenerCount(), 0);
    assert.ok(f.observers.every(o => !o.live));
    assert.equal(f.frames.size, 0);
    assert.equal(f.host.props.size, 0);
    assert.equal(f.host.attrs.size, 0);
    assert.equal(f.api.group.props.size, 0);
    assert.equal(f.api.group.attrs.size, 0);
    assert.equal(f.docs.r.attrs.size, 0);
});

test('attaching twice keeps one set of listeners, and disposing an unknown root is safe', () => {
    const f = fixture();
    f.select(f.readme);
    f.motion.treeGlide(f.host);
    const count = f.host.listenerCount();
    f.motion.treeGlide(f.host);

    assert.equal(f.host.listenerCount(), count);
    f.motion.disposeTreeGlide(new El());
    f.motion.disposeTreeGlide(null);
});
