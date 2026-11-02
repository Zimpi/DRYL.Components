import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// dryl.keynav: which keys lose their native default. DrylMultiSelect's search
// field (I43) passes its own list so the caret keys stay native.
const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

function load() {
    const window = { dryl: {} };
    const start = source.indexOf('window.dryl.keynav = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length), vm.createContext({ window, WeakMap, Set }));
    return window.dryl.keynav;
}

function element() {
    const listeners = new Set();
    return {
        listeners,
        addEventListener: (t, fn) => listeners.add(fn),
        removeEventListener: (t, fn) => listeners.delete(fn),
        press(key) { let prevented = false; for (const fn of listeners) fn({ key, preventDefault: () => { prevented = true; } }); return prevented; },
    };
}

test('without a key list the navigation keys lose their default and Enter keeps it', () => {
    const keynav = load(), el = element();
    keynav.attach(el);
    for (const k of ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']) assert.ok(el.press(k), k);
    for (const k of ['Enter', 'Tab', 'Escape', ' ', 'a']) assert.equal(el.press(k), false, k);
});

test('a key list replaces the default set for that element only', () => {
    const keynav = load(), field = element(), other = element();
    keynav.attach(field, ['ArrowUp', 'ArrowDown', 'Enter']);
    keynav.attach(other);
    for (const k of ['ArrowUp', 'ArrowDown', 'Enter']) assert.ok(field.press(k), k);
    for (const k of ['ArrowLeft', 'ArrowRight', 'Home', 'End', ' ']) assert.equal(field.press(k), false, k);
    assert.ok(other.press('Home'));
});

test('attaching again replaces the handler, and detach removes it', () => {
    const keynav = load(), el = element();
    keynav.attach(el); keynav.attach(el, ['Enter']);
    assert.equal(el.listeners.size, 1);
    assert.equal(el.press('Home'), false);
    keynav.detach(el); keynav.detach(el);
    assert.equal(el.listeners.size, 0);
});
