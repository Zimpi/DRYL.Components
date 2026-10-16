import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

class El {
    constructor(classes = [], rect = { left: 0, top: 0, width: 0, height: 0 }, parent = null) {
        this.classes = new Set(classes); this.rect = rect; this.parent = parent; this.isConnected = true;
        this.props = new Map();
        this.classList = { contains: c => this.classes.has(c) };
        this.style = { setProperty: (k, v) => this.props.set(k, v), removeProperty: k => this.props.delete(k) };
    }
    getBoundingClientRect() { return this.rect; }
    closest(selector) {
        const need = selector.split('.').filter(Boolean);
        for (let n = this; n; n = n.parent) if (need.every(c => n.classes.has(c))) return n;
        return null;
    }
}

function fixture() {
    const listeners = new Map();
    const document = {
        activeElement: null,
        addEventListener(type, fn) { listeners.set(type, fn); },
    };
    const window = { dryl: {} };
    const context = vm.createContext({ window, document, console, Set });
    const start = source.indexOf('window.dryl.proposal = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), context);
    return {
        document, api: window.dryl.proposal,
        down: (target, x, y) => listeners.get('pointerdown')({ target, clientX: x, clientY: y }),
        key: k => listeners.get('keydown')({ key: k }),
        listeners,
    };
}

const card = () => new El(['ai-aura', 'ai-proposed'], { left: 100, top: 50, width: 300, height: 200 });

test('installs one capture listener per input kind on load', () => {
    const f = fixture();
    assert.deepEqual([...f.listeners.keys()].sort(), ['keydown', 'pointerdown']);
});

test('a pointer-down inside a proposed surface stamps its point relative to the surface', () => {
    const f = fixture(), host = card(), button = new El([], {}, host);
    f.down(button, 180, 210);
    assert.equal(host.props.get('--ai-wave-x'), '80px');
    assert.equal(host.props.get('--ai-wave-y'), '160px');
});

test('a pointer-down outside any proposed surface stamps nothing', () => {
    const f = fixture(), plain = new El(['ai-aura']);
    f.down(new El([], {}, plain), 10, 10);
    assert.equal(plain.props.size, 0);
    assert.equal(f.api.stamped.size, 0);
});

test('the next pointer-down elsewhere clears the previous stamp', () => {
    const f = fixture(), host = card();
    f.down(new El([], {}, host), 150, 60);
    f.down(new El(), 5, 5);
    assert.equal(host.props.size, 0);
});

test('a key press clears the stamp', () => {
    const f = fixture(), host = card();
    f.down(new El([], {}, host), 150, 60);
    f.key('a');
    assert.equal(host.props.size, 0);
});

test('Enter with focus inside a proposed surface stamps the focused element centre', () => {
    const f = fixture(), host = card();
    f.document.activeElement = new El([], { left: 120, top: 200, width: 40, height: 20 }, host);
    f.key('Enter');
    assert.equal(host.props.get('--ai-wave-x'), '40px');
    assert.equal(host.props.get('--ai-wave-y'), '160px');
});

test('Space with focus inside a proposed surface stamps too', () => {
    const f = fixture(), host = card();
    f.document.activeElement = new El([], { left: 100, top: 50, width: 0, height: 0 }, host);
    f.key(' ');
    assert.equal(host.props.get('--ai-wave-x'), '0px');
});

test('a stamp whose wave is in flight survives a clear and goes at the next one', () => {
    const f = fixture(), host = card();
    f.down(new El([], {}, host), 150, 60);
    host.classes.delete('ai-proposed'); host.classes.add('ai-from-proposed');
    f.down(new El(), 5, 5);
    assert.equal(host.props.get('--ai-wave-x'), '50px');
    host.classes.delete('ai-from-proposed');
    f.down(new El(), 5, 5);
    assert.equal(host.props.size, 0);
    assert.equal(f.api.stamped.size, 0);
});

test('a disconnected in-flight host is released', () => {
    const f = fixture(), host = card();
    f.down(new El([], {}, host), 150, 60);
    host.classes.add('ai-from-proposed'); host.isConnected = false;
    f.key('Tab');
    assert.equal(f.api.stamped.size, 0);
});
