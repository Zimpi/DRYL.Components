import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// dryl.mdeditor (I54): the text edits behind DrylMarkdownEditor's toolbar, its
// shortcuts and the Enter key, and the listeners attach() owns.
const source = readFileSync(new URL('../../code/DRYL.Components/wwwroot/js/dryl.js', import.meta.url), 'utf8');

function load({ platform = 'Linux x86_64', exec = false } = {}) {
    const window = { dryl: {} };
    const document = { calls: [], execCommand(...args) { this.calls.push(args); return exec; } };
    class Event { constructor(type, init) { this.type = type; Object.assign(this, init); } }
    const start = source.indexOf('window.dryl.mdeditor = (() => {');
    vm.runInContext(source.slice(start, source.indexOf('\n})();', start) + '\n})();'.length),
        vm.createContext({ window, document, navigator: { platform }, WeakMap, Event }));
    return { md: window.dryl.mdeditor, document };
}

const { md } = load();

// "a|b|c": the bars mark the selection; one bar is a caret.
function parse(marked) {
    const s = marked.indexOf('|'), e = marked.lastIndexOf('|');
    return s === e
        ? { value: marked.replace('|', ''), start: s, end: s }
        : { value: marked.slice(0, s) + marked.slice(s + 1, e) + marked.slice(e + 1), start: s, end: e - 1 };
}
function show(value, start, end) {
    return start === end
        ? value.slice(0, start) + '|' + value.slice(start)
        : value.slice(0, start) + '|' + value.slice(start, end) + '|' + value.slice(end);
}
function done(value, edit) {
    const next = value.slice(0, edit.start) + edit.text + value.slice(edit.end);
    return show(next, edit.selStart, edit.selEnd);
}
function cmd(marked, command) {
    const { value, start, end } = parse(marked);
    return done(value, md.transform(value, start, end, command));
}
function enter(marked) {
    const { value, start } = parse(marked);
    const edit = md.enter(value, start);
    return edit ? done(value, edit) : null;
}

test('inline marks wrap the selection and keep it selected', () => {
    assert.equal(cmd('a |bold| c', 'bold'), 'a **|bold|** c');
    assert.equal(cmd('a |it| c', 'italic'), 'a _|it|_ c');
    assert.equal(cmd('a |gone| c', 'strike'), 'a ~~|gone|~~ c');
    assert.equal(cmd('a |x()| c', 'code'), 'a `|x()|` c');
});

test('an inline mark on a caret inserts the pair and puts the caret inside', () => {
    assert.equal(cmd('a |', 'bold'), 'a **|**');
    assert.equal(cmd('|', 'italic'), '_|_');
});

test('whitespace at the ends of a selection stays outside the marks', () => {
    assert.equal(cmd('a| bold |c', 'bold'), 'a **|bold|** c');
});

test('the same command takes the marks off again', () => {
    assert.equal(cmd('a **|bold|** c', 'bold'), 'a |bold| c');
    assert.equal(cmd('a |**bold**| c', 'bold'), 'a |bold| c');
    assert.equal(cmd('a **|** c', 'bold'), 'a | c');
    assert.equal(cmd('a ~~|x|~~', 'strike'), 'a |x|');
    assert.equal(cmd('_|x|_', 'italic'), '|x|');
    assert.equal(cmd('`|x|`', 'code'), '|x|');
});

test('code on a selection that spans lines becomes a fenced block on its own lines', () => {
    assert.equal(cmd('|a\nb|', 'code'), '```\n|a\nb|\n```');
    assert.equal(cmd('x |a\nb| y', 'code'), 'x \n```\n|a\nb|\n```\n y');
    assert.equal(cmd('|a\nb\n|c', 'code'), '```\n|a\nb|\n```\nc');
});

test('code on a fenced selection takes the fence off', () => {
    assert.equal(cmd('```\n|a\nb|\n```', 'code'), '|a\nb|');
});

test('link wraps the selection and selects the url; a caret lands in the brackets', () => {
    assert.equal(cmd('see |docs| now', 'link'), 'see [docs](|url|) now');
    assert.equal(cmd('see |', 'link'), 'see [|](url)');
});

test('heading cycles one to three and back to none', () => {
    assert.equal(cmd('Ti|tle', 'heading'), '# Ti|tle');
    assert.equal(cmd('# Ti|tle', 'heading'), '## Ti|tle');
    assert.equal(cmd('## Ti|tle', 'heading'), '### Ti|tle');
    assert.equal(cmd('### Ti|tle', 'heading'), 'Ti|tle');
    assert.equal(cmd('|', 'heading'), '# |');
});

test('a heading on a line inside the text touches only that line', () => {
    assert.equal(cmd('one\ntw|o\nthree', 'heading'), 'one\n# tw|o\nthree');
});

test('list commands prefix every selected line and select the block', () => {
    assert.equal(cmd('|a\nb|', 'bullet'), '|- a\n- b|');
    assert.equal(cmd('|a\nb\nc|', 'ordered'), '|1. a\n2. b\n3. c|');
    assert.equal(cmd('|a\nb|', 'task'), '|- [ ] a\n- [ ] b|');
    assert.equal(cmd('|a\nb|', 'quote'), '|> a\n> b|');
});

test('a list command on a caret keeps the caret on its character', () => {
    assert.equal(cmd('it|em', 'bullet'), '- it|em');
    assert.equal(cmd('|', 'bullet'), '- |');
    assert.equal(cmd('|', 'task'), '- [ ] |');
    assert.equal(cmd('- it|em', 'bullet'), 'it|em');
    assert.equal(cmd('-| item', 'bullet'), '|item');
});

test('the same list command removes the markers again', () => {
    assert.equal(cmd('|- a\n- b|', 'bullet'), '|a\nb|');
    assert.equal(cmd('|1. a\n2. b|', 'ordered'), '|a\nb|');
    assert.equal(cmd('|- [ ] a\n- [x] b|', 'task'), '|a\nb|');
    assert.equal(cmd('|> a\n> b|', 'quote'), '|a\nb|');
});

test('one kind of list replaces another instead of stacking on it', () => {
    assert.equal(cmd('|- a\n- b|', 'ordered'), '|1. a\n2. b|');
    assert.equal(cmd('|1. a\n2. b|', 'task'), '|- [ ] a\n- [ ] b|');
    assert.equal(cmd('|- [x] a|', 'bullet'), '|- a|');
});

test('a list keeps its indentation and skips blank lines; a quote covers them', () => {
    assert.equal(cmd('|  a\n\n  b|', 'bullet'), '|  - a\n\n  - b|');
    assert.equal(cmd('|a\n\nb|', 'quote'), '|> a\n>\n> b|');
    assert.equal(cmd('|> a\n>\n> b|', 'quote'), '|a\n\nb|');
});

test('a selection ending right after a line break leaves the next line alone', () => {
    assert.equal(cmd('|a\n|b', 'bullet'), '|- a|\nb');
});

test('an unknown command changes nothing', () => {
    assert.equal(md.transform('abc', 0, 3, 'table'), null);
});

test('Enter continues a bullet, a numbered and a task list and a quote', () => {
    assert.equal(enter('- one|'), '- one\n- |');
    assert.equal(enter('* one|'), '* one\n* |');
    assert.equal(enter('3. three|'), '3. three\n4. |');
    assert.equal(enter('- [x] done|'), '- [x] done\n- [ ] |');
    assert.equal(enter('> said|'), '> said\n> |');
    assert.equal(enter('  - nested|'), '  - nested\n  - |');
    assert.equal(enter('- on|e'), '- on\n- |e');
});

test('Enter on an empty item ends the list', () => {
    assert.equal(enter('- one\n- |'), '- one\n|');
    assert.equal(enter('1. |'), '|');
    assert.equal(enter('- [ ] |'), '|');
    assert.equal(enter('> |'), '|');
});

test('Enter elsewhere is left to the browser', () => {
    assert.equal(enter('plain|'), null);
    assert.equal(enter('|- one'), null);
    assert.equal(enter('**bold**|'), null);
    assert.equal(enter('---|'), null);
});

// ── the listeners ────────────────────────────────────────────────────────────

function node() {
    const listeners = new Map();
    return {
        listeners,
        addEventListener(t, fn) { if (!listeners.has(t)) listeners.set(t, new Set()); listeners.get(t).add(fn); },
        removeEventListener(t, fn) { listeners.get(t)?.delete(fn); },
        emit(t, e = {}) {
            const event = { target: this, prevented: false, preventDefault() { this.prevented = true; }, ...e };
            for (const fn of [...listeners.get(t) ?? []]) fn(event);
            return event;
        },
        count() { return [...listeners.values()].reduce((n, set) => n + set.size, 0); },
    };
}

function editor(value = '', start = 0, end = start) {
    const el = Object.assign(node(), {
        value, selectionStart: start, selectionEnd: end, disabled: false, readOnly: false, focused: 0, inputs: 0,
        focus() { this.focused++; },
        setSelectionRange(s, e) { this.selectionStart = s; this.selectionEnd = e; },
        setRangeText(text, s, e) { this.value = this.value.slice(0, s) + text + this.value.slice(e); },
        dispatchEvent(ev) { if (ev.type === 'input') this.inputs++; },
    });
    const bar = node();
    const names = ['bold', 'italic', 'strike', 'heading'];
    const buttons = names.map((name, i) => ({
        dataset: { mdeCmd: name }, disabled: false, tabIndex: i === 0 ? 0 : -1, focused: 0,
        focus() { this.focused++; }, closest() { return this; },
    }));
    bar.querySelectorAll = () => buttons;
    bar.contains = (b) => buttons.includes(b);
    const root = {
        dataset: { mdeView: 'editor' },
        querySelector: (sel) => sel === '[data-mde-source]' ? el : sel === '[data-mde-toolbar]' ? bar : null,
    };
    return { root, el, bar, buttons, state: () => show(el.value, el.selectionStart, el.selectionEnd) };
}

test('a toolbar press edits the source, raises one input event and returns focus to it', () => {
    const { md } = load(), f = editor('a word', 2, 6);
    md.attach(f.root);
    f.bar.emit('click', { target: f.buttons[0] });
    assert.equal(f.state(), 'a **|word|**');
    assert.equal(f.el.inputs, 1);
    assert.equal(f.el.focused, 1);
});

test('where the browser inserts the text itself, no second input event is raised', () => {
    const { md, document } = load({ exec: true }), f = editor('word', 0, 4);
    document.execCommand = (name, _, text) => { f.el.setRangeText(text, f.el.selectionStart, f.el.selectionEnd); return true; };
    md.attach(f.root);
    f.bar.emit('click', { target: f.buttons[0] });
    assert.equal(f.state(), '**|word|**');
    assert.equal(f.el.inputs, 0);
});

test('a press on the toolbar does not take the caret out of the source', () => {
    const { md } = load(), f = editor();
    md.attach(f.root);
    assert.ok(f.bar.emit('mousedown', { target: f.buttons[1] }).prevented);
});

test('a disabled button, a read-only source and the preview all refuse a command', () => {
    const { md } = load(), f = editor('word', 0, 4);
    md.attach(f.root);
    f.buttons[0].disabled = true;
    f.bar.emit('click', { target: f.buttons[0] });
    f.buttons[0].disabled = false;
    f.el.readOnly = true;
    f.bar.emit('click', { target: f.buttons[0] });
    f.el.readOnly = false;
    f.root.dataset.mdeView = 'preview';
    f.bar.emit('click', { target: f.buttons[0] });
    assert.equal(f.state(), '|word|');
});

test('Ctrl+B, I, K and E run their commands and lose the browser default', () => {
    const { md } = load();
    for (const [key, expected] of [['b', '**|x|**'], ['i', '_|x|_'], ['k', '[x](|url|)'], ['e', '`|x|`'], ['B', '**|x|**']]) {
        const f = editor('x', 0, 1);
        md.attach(f.root);
        assert.ok(f.el.emit('keydown', { key, ctrlKey: true }).prevented, key);
        assert.equal(f.state(), expected, key);
    }
});

test('other keys, Shift and Alt combinations stay with the browser', () => {
    const { md } = load(), f = editor('x', 0, 1);
    md.attach(f.root);
    assert.equal(f.el.emit('keydown', { key: 'u', ctrlKey: true }).prevented, false);
    assert.equal(f.el.emit('keydown', { key: 'b', ctrlKey: true, shiftKey: true }).prevented, false);
    assert.equal(f.el.emit('keydown', { key: 'b', ctrlKey: true, altKey: true }).prevented, false);
    assert.equal(f.el.emit('keydown', { key: 'b' }).prevented, false);
    assert.equal(f.el.emit('keydown', { key: 'Tab' }).prevented, false);
    assert.equal(f.state(), '|x|');
});

test('on Apple platforms only Cmd is a shortcut; Ctrl keeps moving the caret', () => {
    const { md } = load({ platform: 'MacIntel' }), f = editor('x', 0, 1);
    md.attach(f.root);
    assert.equal(f.el.emit('keydown', { key: 'e', ctrlKey: true }).prevented, false);
    assert.equal(f.state(), '|x|');
    assert.ok(f.el.emit('keydown', { key: 'b', metaKey: true }).prevented);
    assert.equal(f.state(), '**|x|**');
});

test('Enter continues a list through the same edit path and is left alone elsewhere', () => {
    const { md } = load(), f = editor('- one', 5);
    md.attach(f.root);
    assert.ok(f.el.emit('keydown', { key: 'Enter' }).prevented);
    assert.equal(f.state(), '- one\n- |');
    assert.equal(f.el.emit('keydown', { key: 'Enter', shiftKey: true }).prevented, false);
    assert.equal(f.el.emit('keydown', { key: 'Enter', isComposing: true }).prevented, false);
    const plain = editor('plain', 5);
    md.attach(plain.root);
    assert.equal(plain.el.emit('keydown', { key: 'Enter' }).prevented, false);
});

test('arrow keys, Home and End move one tab stop through the enabled buttons', () => {
    const { md } = load(), f = editor();
    md.attach(f.root);
    f.buttons[1].disabled = true;
    assert.ok(f.bar.emit('keydown', { key: 'ArrowRight', target: f.buttons[0] }).prevented);
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [-1, -1, 0, -1]);
    assert.equal(f.buttons[2].focused, 1);
    f.bar.emit('keydown', { key: 'End', target: f.buttons[2] });
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [-1, -1, -1, 0]);
    f.bar.emit('keydown', { key: 'ArrowRight', target: f.buttons[3] });
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [0, -1, -1, -1]);
    f.bar.emit('keydown', { key: 'ArrowLeft', target: f.buttons[0] });
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [-1, -1, -1, 0]);
    f.bar.emit('keydown', { key: 'Home', target: f.buttons[3] });
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [0, -1, -1, -1]);
    assert.equal(f.bar.emit('keydown', { key: 'Tab', target: f.buttons[0] }).prevented, false);
});

test('sync leaves exactly one enabled button in the tab order', () => {
    const { md } = load(), f = editor();
    md.attach(f.root);
    f.buttons[0].disabled = true;
    md.sync(f.root);
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [-1, 0, -1, -1]);
    for (const b of f.buttons) b.disabled = true;
    md.sync(f.root);
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [-1, -1, -1, -1]);
    for (const b of f.buttons) b.disabled = false;
    md.sync(f.root);
    assert.deepEqual(f.buttons.map(b => b.tabIndex), [0, -1, -1, -1]);
});

test('attaching again replaces the listeners, and detach removes every one', () => {
    const { md } = load(), f = editor('x', 0, 1);
    md.attach(f.root); md.attach(f.root);
    assert.equal(f.el.count(), 1);
    assert.equal(f.bar.count(), 4);
    md.detach(f.root); md.detach(f.root);
    assert.equal(f.el.count() + f.bar.count(), 0);
    f.el.emit('keydown', { key: 'b', ctrlKey: true });
    assert.equal(f.state(), '|x|');
});
