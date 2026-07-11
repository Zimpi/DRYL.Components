// dryl-aifield.js — DOM value bridge for DrylAiField (lazy ES module, no consumer wiring).
// Finds the first text-like field inside the wrapper, reads value + selection, and writes
// streamed text back by dispatching a native bubbling `input` event so the wrapped
// component's @bind-Value updates itself. Skips anything inside [data-aifield-ui]
// (the wrapper's own trigger / prompt UI).

const SELECTOR =
    'textarea, input:not([type]), input[type="text"], input[type="search"], ' +
    'input[type="email"], input[type="url"], input[type="tel"]';

function field(root) {
    if (!root) return null;
    for (const el of root.querySelectorAll(SELECTOR)) {
        if (el.disabled) continue;
        if (el.closest('[data-aifield-ui]')) continue;
        return el;
    }
    return null;
}

export function snapshot(root) {
    const el = field(root);
    if (!el) return { found: false, value: '', selStart: -1, selEnd: -1 };
    let selStart = -1, selEnd = -1;
    try {
        selStart = el.selectionStart ?? -1;
        selEnd = el.selectionEnd ?? -1;
    } catch { /* some input types throw on selection access */ }
    return { found: true, value: el.value ?? '', selStart, selEnd };
}

export function write(root, text) {
    const el = field(root);
    if (!el) return;
    el.value = text;
    el.dispatchEvent(new Event('input', { bubbles: true }));
}

export function setBusy(root, busy) {
    const el = field(root);
    if (el) el.readOnly = !!busy;
}

export function focusField(root) {
    const el = field(root);
    if (el) el.focus({ preventScroll: false });
}
