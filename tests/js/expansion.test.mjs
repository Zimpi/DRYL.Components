import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import vm from "node:vm";

const root = resolve(import.meta.dirname, "../..");
const shared = readFileSync(resolve(root, "code/DRYL.Components/wwwroot/js/dryl.js"), "utf8");
const css = readFileSync(resolve(root, "code/DRYL.Components/wwwroot/dryl.css"), "utf8");
const source = shared.slice(shared.indexOf("window.dryl.expansion = "), shared.indexOf(" * Download — client-side"))
  .replace(/\/\* -+\s*$/, "");

class Node {
  constructor(left = 0, header = false) {
    this.left = left; this.header = header; this.listeners = new Map(); this.attrs = new Set(); this.props = new Map();
    this.style = { setProperty: (k, v) => this.props.set(k, v) };
  }
  closest(selector) { return selector === ".expansion-header" && this.header ? this : this.parent?.closest(selector) ?? null; }
  getBoundingClientRect() { return { left: this.left }; }
  addEventListener(type, fn) { this.listeners.set(type, fn); }
  removeEventListener(type) { this.listeners.delete(type); }
  hasAttribute(name) { return this.attrs.has(name); }
  setAttribute(name) { this.attrs.add(name); }
  removeAttribute(name) { this.attrs.delete(name); }
}

function setup() {
  const doc = new Map();
  const frames = [];
  const context = {
    window: { dryl: {} }, Element: Node,
    document: { addEventListener: (type, fn) => doc.set(type, fn) },
    requestAnimationFrame: (fn) => { frames.push(fn); return frames.length; },
  };
  vm.runInNewContext(source, context);
  const header = new Node(100, true);
  const body = new Node(100);
  const child = new Node();
  child.parent = header;
  header.nextElementSibling = body;
  const flush = () => { while (frames.length) frames.shift()(); };
  return { doc, frames, flush, header, body, child, context };
}

test("a mouse lights the header it moves over and writes the offset inside it", () => {
  const { doc, flush, header, child } = setup();
  doc.get("pointerover")({ pointerType: "mouse", target: child });
  header.listeners.get("pointermove")({ clientX: 340 });
  assert.equal(header.hasAttribute("data-spot"), false, "the marker waits for the frame");
  flush();
  assert.equal(header.props.get("--spot-x"), "240px");
  assert.equal(header.hasAttribute("data-spot"), true);
});

test("touch and pen never light a header", () => {
  const { doc, header, child } = setup();
  doc.get("pointerover")({ pointerType: "touch", target: child });
  doc.get("pointerover")({ pointerType: "pen", target: child });
  assert.equal(header.listeners.size, 0);
});

test("leaving a header clears its marker and its listeners", () => {
  const { doc, flush, header, child } = setup();
  doc.get("pointerover")({ pointerType: "mouse", target: child });
  header.listeners.get("pointermove")({ clientX: 340 });
  flush();
  header.listeners.get("pointerleave")();
  assert.equal(header.hasAttribute("data-spot"), false);
  assert.equal(header.listeners.size, 0);
});

test("moving straight to another header hands the light over", () => {
  const { doc, header, child } = setup();
  const other = new Node(0, true);
  doc.get("pointerover")({ pointerType: "mouse", target: child });
  doc.get("pointerover")({ pointerType: "mouse", target: other });
  assert.equal(header.listeners.size, 0);
  assert.equal(other.listeners.size, 2);
});

test("many moves in one frame cost one write, and a frame after leaving writes nothing", () => {
  const { doc, frames, flush, header, child } = setup();
  doc.get("pointerover")({ pointerType: "mouse", target: child });
  const move = header.listeners.get("pointermove");
  move({ clientX: 110 }); move({ clientX: 120 }); move({ clientX: 130 });
  assert.equal(frames.length, 1);
  header.listeners.get("pointerleave")();
  flush();
  assert.equal(header.props.has("--spot-x"), false);
  assert.equal(header.hasAttribute("data-spot"), false);
});

test("a click starts the seam at its point, a keyboard click in the middle", () => {
  const { doc, body, child } = setup();
  doc.get("click")({ target: child, detail: 1, clientX: 175 });
  assert.equal(body.props.get("--seam-x"), "75px");
  doc.get("click")({ target: child, detail: 0, clientX: 0 });
  assert.equal(body.props.get("--seam-x"), "50%");
});

test("a click or hover outside any header does nothing", () => {
  const { doc } = setup();
  const outside = new Node();
  assert.doesNotThrow(() => doc.get("click")({ target: outside, detail: 1, clientX: 1 }));
  assert.doesNotThrow(() => doc.get("pointerover")({ pointerType: "mouse", target: outside }));
});

test("a second load of the script does not bind the document twice", () => {
  const { doc, context } = setup();
  doc.clear();
  vm.runInNewContext(source, context);
  assert.equal(doc.size, 0);
});

test("the open body drops its mask only after the reveal", () => {
  const open = css.slice(css.indexOf(".expansion.is-open .expansion-body-inner {"));
  const rule = open.slice(0, open.indexOf("}"));
  assert.match(rule, /mask-image: none/);
  assert.match(rule, /mask-image var\(--dur-fast\) var\(--ease-out\) var\(--delay-long\)/);
  assert.match(rule, /transition-behavior: normal, allow-discrete/);
});
