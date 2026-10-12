import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

function load() {
    const window = { dryl: {} };
    const context = vm.createContext({ window, Math, Number });
    const start = source.indexOf('window.dryl.calendar = {');
    assert.notEqual(start, -1, 'dryl.calendar is defined');
    vm.runInContext(source.slice(start, source.indexOf('\n};', start) + '\n};'.length), context);
    return window.dryl.calendar;
}

function scroller(gridHeight) {
    const grid = gridHeight === null ? null : { offsetHeight: gridHeight };
    return { nodeType: 1, scrollTop: 0, querySelector: selector => (selector === '.cal-grid' ? grid : null) };
}

test('scrollToTime puts the fraction of the grid at the top of the scroller', () => {
    const calendar = load();
    const el = scroller(1152);          // 24 hours at 48px

    calendar.scrollToTime(el, 7 / 24);

    assert.equal(el.scrollTop, 336);    // seven hours down
});

test('scrollToTime clamps a fraction outside the grid', () => {
    const calendar = load();
    const above = scroller(1000), below = scroller(1000);

    calendar.scrollToTime(above, 1.5);
    calendar.scrollToTime(below, -0.25);

    assert.equal(above.scrollTop, 1000);
    assert.equal(below.scrollTop, 0);
});

test('scrollToTime treats a fraction that is not a number as the top', () => {
    const calendar = load();
    const el = scroller(1000);
    el.scrollTop = 40;

    calendar.scrollToTime(el, Number.NaN);

    assert.equal(el.scrollTop, 0);
});

test('scrollToTime is a no-op without an element or without a grid', () => {
    const calendar = load();
    const month = scroller(null);       // the month view has no time grid
    month.scrollTop = 12;

    assert.doesNotThrow(() => calendar.scrollToTime(null, 0.5));
    assert.doesNotThrow(() => calendar.scrollToTime(undefined, 0.5));
    assert.doesNotThrow(() => calendar.scrollToTime({ nodeType: 3 }, 0.5));
    calendar.scrollToTime(month, 0.5);

    assert.equal(month.scrollTop, 12);
});

test('scrollToTime keeps nothing: no listener, no timer, no reference', () => {
    const start = source.indexOf('window.dryl.calendar = {');
    const body = source.slice(start, source.indexOf('\n};', start));

    assert.doesNotMatch(body, /addEventListener|setTimeout|setInterval|requestAnimationFrame|WeakMap|invokeMethodAsync/);
});
