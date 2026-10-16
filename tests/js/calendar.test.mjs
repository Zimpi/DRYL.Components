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

test('focus moves focus to the matching element and scrolls it into view', () => {
    const calendar = load();
    const calls = [];
    const doc = { activeElement: null };
    const target = {
        ownerDocument: doc,
        focus(options) { calls.push(['focus', options]); doc.activeElement = target; },
        scrollIntoView(options) { calls.push(['scroll', options]); }
    };
    const root = { nodeType: 1, querySelector: selector => (selector === '.cal-cursor' ? target : null) };

    calendar.focus(root, '.cal-cursor');
    calendar.focus(root, '.cal-cursor');

    assert.deepEqual(calls.map(c => c[0]), ['focus', 'scroll', 'scroll']);
    assert.equal(calls[0][1].preventScroll, true);
});

test('focus is best-effort: no root, no match or a bad selector does nothing', () => {
    const calendar = load();
    const root = { nodeType: 1, querySelector: () => { throw new Error('bad selector'); } };

    calendar.focus(null, '.cal-cursor');
    calendar.focus({ nodeType: 1, querySelector: () => null }, '.missing');
    calendar.focus(root, '[[');
});

// ── dryl.calendar.edit: the pure parts mirror CalendarEditing in C# ──

function loadEdit() {
    const window = { dryl: { calendar: {} } };
    const context = vm.createContext({ window, Math, Number, JSON, Array, Date, Intl, String, WeakMap, Object });
    const start = source.indexOf('window.dryl.calendar.edit = (() => {');
    assert.notEqual(start, -1, 'dryl.calendar.edit is defined');
    vm.runInContext(source.slice(start), context);
    return window.dryl.calendar.edit;
}

// An 08:00–18:00 grid over a seven-day range; minutes count from its first midnight.
const office = { days: 7, ds: 480, de: 1080 };

test('edit.limits.time keeps a move inside the range and the day grid', () => {
    const { limits } = loadEdit();
    const event = { s: 540, e: 600 };                         // Monday 09:00–10:00

    assert.equal(limits.time({ s: 600, e: 660 }, event, office), true);
    assert.equal(limits.time({ s: 450, e: 510 }, event, office), false);           // before 08:00
    assert.equal(limits.time({ s: 1050, e: 1110 }, event, office), false);         // past 18:00
    assert.equal(limits.time({ s: 540 - 1440, e: 600 - 1440 }, event, office), false); // before the range
    assert.equal(limits.time({ s: 540 + 7 * 1440, e: 600 + 7 * 1440 }, event, office), false); // after it
});

test('edit.limits.time lets a broken limit stay broken but never grow, and keeps the shape', () => {
    const { limits } = loadEdit();
    const early = { s: 420, e: 540 };                         // 07:00–09:00 on an 08:00 grid

    assert.equal(limits.time({ s: 450, e: 570 }, early, office), true);
    assert.equal(limits.time({ s: 390, e: 510 }, early, office), false);
    const full = { days: 7, ds: 0, de: 1440 };
    assert.equal(limits.time({ s: 540, e: 540 + 1440 }, { s: 540, e: 600 }, full), false);  // would become all-day
    assert.equal(limits.time({ s: 1380, e: 1440 }, { s: 1320, e: 1380 }, full), true);       // ends at midnight
});

test('edit.limits.days keeps a bar in the range unless it already continued out of it', () => {
    const { limits } = loadEdit();

    assert.equal(limits.days({ fd: 5, ld: 7 }, { fd: 4, ld: 6 }, office), true);
    assert.equal(limits.days({ fd: 6, ld: 8 }, { fd: 5, ld: 7 }, office), false);
    assert.equal(limits.days({ fd: -1, ld: 3 }, { fd: -2, ld: 2 }, office), true);
    assert.equal(limits.days({ fd: -3, ld: 1 }, { fd: -2, ld: 2 }, office), false);
    assert.equal(limits.days({ fd: 2, ld: 2 }, { fd: 2, ld: 3 }, office), false);
});

test('edit.reassign replaces the starting column\'s person, without duplicates', () => {
    const { reassign } = loadEdit();

    assert.deepEqual([...reassign(['ada', 'bo'], 'ada', 'cy')], ['cy', 'bo']);
    assert.deepEqual([...reassign(['ada', 'bo'], 'ada', 'bo')], ['bo']);
    assert.deepEqual([...reassign(['ada', 'bo'], 'ada', null)], ['bo']);
    assert.deepEqual([...reassign([], null, 'ada')], ['ada']);
});

test('edit.hasConflict detects overlap, absence and time outside working hours', () => {
    const { hasConflict } = loadEdit();
    const timed = (id, s, e, p = ['ada']) => ({ id, s, e, p, tp: true });
    const a = timed('a', 540, 600), b = timed('b', 570, 660), c = timed('c', 600, 660);

    assert.equal(hasConflict(a, [a, b], {}), true);
    assert.equal(hasConflict(a, [a, c], {}), false);                               // only touching
    assert.equal(hasConflict(a, [a, timed('d', 540, 600, ['bo'])], {}), false);    // another person
    assert.equal(hasConflict(a, [a, { ...b, tp: false }], {}), false);             // all-day takes no part
    assert.equal(hasConflict(a, [a], { ada: { abs: [[0, 1440]], work: [] } }), true);
    assert.equal(hasConflict(timed('e', 960, 1080), [], { ada: { abs: [], work: [[540, 1020]] } }), true);
    assert.equal(hasConflict(timed('f', 660, 840), [], { ada: { abs: [], work: [[540, 720], [720, 1020]] } }), false);
    assert.equal(hasConflict(timed('g', 1440 + 1200, 1440 + 1260), [], { ada: { abs: [], work: [[540, 1020]] } }), false);
});
