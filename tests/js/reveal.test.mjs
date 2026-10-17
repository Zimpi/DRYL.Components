import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

class Element {
    constructor(parent = null) {
        this.nodeType = 1; this.parentNode = parent; this.children = []; this.attrs = new Map(); this.vars = new Map();
        const classes = new Set();
        this.classList = { add: c => classes.add(c), remove: c => classes.delete(c), contains: c => classes.has(c),
            toggle: (c, on) => (on ? classes.add(c) : classes.delete(c)) };
        this.style = { setProperty: (k, v) => this.vars.set(k, String(v)), getPropertyValue: k => this.vars.get(k) ?? '' };
    }
    setAttribute(k, v) { this.attrs.set(k, v); }
    removeAttribute(k) { this.attrs.delete(k); }
    hasAttribute(k) { return this.attrs.has(k); }
    append(n = 1) { const added = Array.from({ length: n }, () => new Element(this)); this.children.push(...added); return added; }
    compareDocumentPosition(other) {
        const order = this.parentNode.children;
        return order.indexOf(other) > order.indexOf(this) ? 4 : 2;
    }
}

function fixture(reduce = false) {
    const observers = [], mutations = [], frames = new Map();
    let next = 0;
    class IntersectionObserver {
        constructor(cb, opts) { this.cb = cb; this.opts = opts; this.targets = new Set(); observers.push(this); }
        observe(t) { this.targets.add(t); }
        unobserve(t) { this.targets.delete(t); }
        disconnect() { this.targets.clear(); this.disconnected = true; }
    }
    class MutationObserver {
        constructor(cb) { this.cb = cb; mutations.push(this); }
        observe() {}
        disconnect() { this.disconnected = true; }
    }
    const window = { dryl: { reduced: () => reduce }, IntersectionObserver };
    const context = vm.createContext({ window, document: {}, console, WeakMap, Map, Set, Array, Math,
        IntersectionObserver, MutationObserver, Node: { DOCUMENT_POSITION_FOLLOWING: 4 },
        requestAnimationFrame: fn => { frames.set(++next, fn); return next; },
        cancelAnimationFrame: id => frames.delete(id) });
    const start = source.indexOf('window.dryl.motion = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);

    const [ratio, reach] = [() => observers.at(-2), () => observers.at(-1)];
    const fire = (io, target, isIntersecting, intersectionRatio = isIntersecting ? 1 : 0) =>
        io.cb([{ target, isIntersecting, intersectionRatio }]);
    return { motion: window.dryl.motion, observers, mutations, ratio, reach, fire,
        frame() { const list = [...frames.values()]; frames.clear(); list.forEach(fn => fn()); } };
}

test('a target taller than viewport / threshold reveals once it reaches up the viewport', () => {
    const f = fixture(), el = new Element();
    f.motion.observe(el, null, { threshold: 0.15 });
    assert.equal(f.reach().opts.rootMargin, '0px 0px -15% 0px');

    // Ratio stays below the threshold however far you scroll — the old observer stopped here.
    f.fire(f.ratio(), el, true, 0.08);
    f.frame();
    assert.equal(el.classList.contains('is-revealed'), false);

    f.fire(f.reach(), el, true);
    f.frame();
    assert.equal(el.classList.contains('is-revealed'), true);
    assert.equal(f.ratio().targets.has(el), false, 'once: no longer watched');
});

test('stagger watches each child and counts the delay within its batch', () => {
    const f = fixture(), el = new Element(), tiles = el.append(30);
    f.motion.observe(el, null, { stagger: true });
    assert.equal(f.ratio().targets.size, 30);
    assert.equal(el.classList.contains('is-revealed'), false);

    f.fire(f.ratio(), tiles[1], true);
    f.fire(f.ratio(), tiles[0], true);
    f.frame();
    assert.deepEqual([0, 1].map(i => tiles[i].hasAttribute('data-revealed')), [true, true]);
    assert.deepEqual([0, 1].map(i => tiles[i].style.getPropertyValue('--reveal-i')), ['0', '1']);
    assert.equal(tiles[29].hasAttribute('data-revealed'), false);

    for (const t of tiles.slice(10)) f.fire(f.reach(), t, true);
    f.frame();
    assert.equal(tiles[10].style.getPropertyValue('--reveal-i'), '0');
    assert.equal(tiles[29].style.getPropertyValue('--reveal-i'), '10', 'capped');
});

test('a child added later is watched, a removed one forgotten', () => {
    const f = fixture(), el = new Element();
    el.append(2);
    f.motion.observe(el, null, { stagger: true });
    const [added] = el.append(1), removed = el.children[0];
    f.mutations[0].cb([{ addedNodes: [added], removedNodes: [removed] }]);
    assert.equal(f.ratio().targets.has(added), true);
    assert.equal(f.ratio().targets.has(removed), false);
});

test('once = false hides again only when neither half sees it', () => {
    const f = fixture(), el = new Element();
    f.motion.observe(el, null, { once: false });
    f.fire(f.ratio(), el, true, 0.5);
    f.fire(f.reach(), el, true);
    f.frame();
    f.fire(f.ratio(), el, true, 0.05);
    assert.equal(el.classList.contains('is-revealed'), true);
    f.fire(f.reach(), el, false);
    assert.equal(el.classList.contains('is-revealed'), false);
});

test('reduced motion shows at once; unobserve releases everything', () => {
    const quiet = fixture(true), a = new Element();
    quiet.motion.observe(a, null, { stagger: true });
    assert.equal(a.classList.contains('is-revealed'), true);
    assert.equal(quiet.observers.length, 0);

    const f = fixture(), el = new Element();
    el.append(3);
    f.motion.observe(el, null, { stagger: true });
    f.fire(f.ratio(), el.children[0], true);
    f.motion.unobserve(el);
    f.frame();
    assert.ok(f.observers.every(io => io.disconnected) && f.mutations[0].disconnected);
    assert.equal(el.children[0].hasAttribute('data-revealed'), false, 'pending frame cancelled');
});
