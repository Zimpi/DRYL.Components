import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import vm from "node:vm";

const root = resolve(import.meta.dirname, "../..");
const canvasSource = readFileSync(resolve(root, "code/DRYL.Components/wwwroot/js/dryl-canvas.js"), "utf8");
const sharedSource = readFileSync(resolve(root, "code/DRYL.Components/wwwroot/js/dryl.js"), "utf8");
const resizeSource = sharedSource.slice(sharedSource.indexOf("    initColumnResize(root, dotnet)"),
  sharedSource.indexOf("    // layoutPinned(root)"));

class Events {
  listeners = new Map();
  addEventListener(type, handler) {
    if (!this.listeners.has(type)) this.listeners.set(type, new Set());
    this.listeners.get(type).add(handler);
  }
  removeEventListener(type, handler) { this.listeners.get(type)?.delete(handler); }
  handlers(type) { return [...(this.listeners.get(type) ?? [])]; }
  fire(type, values = {}) {
    const event = { type, target: this, button: 0, pointerId: 1, clientX: 10, clientY: 10,
      preventDefault() { }, stopPropagation() { }, ...values };
    for (const handler of this.handlers(type)) handler(event);
    return event;
  }
  count() { return [...this.listeners.values()].reduce((count, set) => count + set.size, 0); }
}

class Style {
  values = new Map();
  getPropertyValue(name) { return this.values.get(name)?.value ?? ""; }
  getPropertyPriority(name) { return this.values.get(name)?.priority ?? ""; }
  setProperty(name, value, priority = "") {
    if (!value) this.values.delete(name);
    else this.values.set(name, { value, priority });
  }
  removeProperty(name) { const old = this.getPropertyValue(name); this.values.delete(name); return old; }
  get width() { return this.getPropertyValue("width"); }
  set width(value) { this.setProperty("width", value); }
  get transform() { return this.getPropertyValue("transform"); }
  set transform(value) { this.setProperty("transform", value); }
}

class Element extends Events {
  constructor(tag = "div", attrs = {}, classes = []) {
    super(); this.tag = tag; this.attrs = new Map(Object.entries(attrs)); this.children = [];
    this.style = new Style(); this.captured = new Set(); this.isConnected = true;
    const set = new Set(classes);
    this.classList = { add: (...names) => names.forEach(name => set.add(name)),
      remove: (...names) => names.forEach(name => set.delete(name)), contains: name => set.has(name) };
  }
  append(child) { this.children.push(child); child.parentElement = this; return child; }
  contains(node) { return node === this || this.children.some(child => child.contains(node)); }
  matches(selector) {
    if (selector.startsWith(".")) return this.classList.contains(selector.slice(1));
    const attr = selector.match(/^(\w+)?\[([^=\]]+)(?:="([^"]*)")?\]$/);
    if (attr) return (!attr[1] || this.tag === attr[1]) && this.attrs.has(attr[2]) &&
      (attr[3] === undefined || this.attrs.get(attr[2]) === attr[3]);
    return this.tag === selector;
  }
  closest(selector) { return this.matches(selector) ? this : this.parentElement?.closest(selector) ?? null; }
  querySelectorAll(selector) { return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]); }
  getAttribute(name) { return this.attrs.get(name) ?? null; }
  setAttribute(name, value) { this.attrs.set(name, value); }
  hasAttribute(name) { return this.attrs.has(name); }
  removeAttribute(name) { this.attrs.delete(name); }
  getBoundingClientRect() { return this.rect ?? { left: 0, top: 0, width: 100, height: 60 }; }
  setPointerCapture(id) { this.captured.add(id); }
  hasPointerCapture(id) { return this.captured.has(id); }
  releasePointerCapture(id) { this.captured.delete(id); }
}

function environment() {
  const window = new Events();
  window.CSS = { escape: value => value };
  window.dryl = {};
  const context = vm.createContext({ window, CSS: window.CSS });
  vm.runInContext(canvasSource.replace(/^export /gm, "") + "\nglobalThis.canvas = { initReorder, disposeReorder };", context);
  window.dryl.table = vm.runInContext(`({${resizeSource}})`, context);
  const calls = [];
  const dotnet = { invokeMethodAsync(...args) { calls.push(args); return Promise.resolve(); } };
  return { window, canvas: context.canvas, table: window.dryl.table, calls, dotnet };
}

function canvasFixture() {
  const env = environment();
  const root = new Element();
  const nodes = [0, 1, 2].map(i => {
    const node = root.append(new Element("div", { "data-cid": `node-${i}` }));
    node.rect = { left: 0, top: i * 100, width: 200, height: 60 };
    node.handle = node.append(new Element("button", { "data-drag-handle": "" }));
    return node;
  });
  env.canvas.initReorder(root, env.dotnet);
  const down = (index = 0, pointerId = 1) => root.fire("pointerdown", { target: nodes[index].handle, pointerId });
  const move = (pointerId = 1) => env.window.fire("pointermove", { clientX: 20, clientY: 240, pointerId });
  return { ...env, root, nodes, down, move };
}

function tableFixture() {
  const env = environment();
  const root = new Element();
  const table = root.append(new Element("table"));
  const th = table.append(new Element("th", { "data-col-key": "name" }));
  th.offsetWidth = 180;
  th.style.setProperty("width", "180px", "important");
  const grip = th.append(new Element("span", { "data-col-key": "name" }, ["tbl-col-resize"]));
  const cells = ["180px", "", "25%"].map(width => {
    const cell = table.append(new Element("td", { "data-col-key": "name" }));
    cell.style.width = width;
    return cell;
  });
  env.table.initColumnResize(root, env.dotnet);
  const down = (pointerId = 1) => root.fire("pointerdown", { target: grip, pointerId });
  const move = (pointerId = 1) => env.window.fire("pointermove", { clientX: 70, pointerId });
  const widths = () => [th, ...cells].map(cell => [cell.style.width, cell.style.getPropertyPriority("width")]);
  return { ...env, root, th, grip, cells, down, move, widths };
}

const cancellation = ["dispose", "reinitialize", "Escape", "pointercancel", "lostpointercapture"];
for (const reason of cancellation) {
  test(`canvas ${reason} restores decoration, releases capture and silences late events`, () => {
    const f = canvasFixture();
    f.nodes[0].style.setProperty("transform", "scale(0.9)", "important");
    f.down(); f.move();
    assert.ok(f.nodes[0].classList.contains("is-dragging"));
    assert.ok(f.nodes[2].hasAttribute("data-drop-after"));
    const lateMove = f.window.handlers("pointermove");
    const lateUp = f.window.handlers("pointerup");
    if (reason === "dispose") f.canvas.disposeReorder(f.root);
    else if (reason === "reinitialize") f.canvas.initReorder(f.root, f.dotnet);
    else if (reason === "Escape") f.window.fire("keydown", { key: "Escape" });
    else if (reason === "lostpointercapture") f.nodes[0].handle.fire(reason);
    else f.window.fire(reason);
    assert.equal(f.window.count(), 0, "active global listeners must be removed");
    assert.equal(f.nodes[0].handle.captured.size, 0);
    assert.equal(f.nodes[0].classList.contains("is-dragging"), false);
    assert.equal(f.nodes[0].style.transform, "scale(0.9)");
    assert.equal(f.nodes[0].style.getPropertyPriority("transform"), "important");
    assert.ok(f.nodes.every(node => !node.hasAttribute("data-drop-before") && !node.hasAttribute("data-drop-after")));
    for (const handler of lateMove) handler({ pointerId: 1, clientX: 999, clientY: 999 });
    for (const handler of lateUp) handler({ pointerId: 1 });
    assert.equal(f.nodes[0].style.transform, "scale(0.9)");
    assert.equal(f.calls.length, 0);
    f.canvas.disposeReorder(f.root); f.canvas.disposeReorder(f.root);
    assert.equal(f.root.count(), 0);
  });

  test(`table ${reason} restores exact inline widths without persisting a preview`, () => {
    const f = tableFixture();
    const before = f.widths();
    f.down(); f.move();
    assert.notDeepEqual(f.widths(), before);
    const lateMove = f.window.handlers("pointermove");
    const lateUp = f.window.handlers("pointerup");
    if (reason === "dispose") f.table.disposeColumnResize(f.root);
    else if (reason === "reinitialize") f.table.initColumnResize(f.root, f.dotnet);
    else if (reason === "Escape") f.window.fire("keydown", { key: "Escape" });
    else if (reason === "lostpointercapture") f.grip.fire(reason);
    else f.window.fire(reason);
    assert.equal(f.window.count(), 0, "active global listeners must be removed");
    assert.equal(f.grip.captured.size, 0);
    assert.equal(f.root.classList.contains("tbl-resizing"), false);
    assert.deepEqual(f.widths(), before);
    for (const handler of lateMove) handler({ pointerId: 1, clientX: 999 });
    for (const handler of lateUp) handler({ pointerId: 1 });
    assert.deepEqual(f.widths(), before);
    assert.equal(f.calls.length, 0);
    f.table.disposeColumnResize(f.root); f.table.disposeColumnResize(f.root);
    assert.equal(f.root.count(), 0);
  });
}

test("canvas ignores a foreign pointer and reports a changed drop once", () => {
  const f = canvasFixture();
  f.down();
  f.move(2); f.window.fire("pointerup", { pointerId: 2 }); f.window.fire("pointercancel", { pointerId: 2 });
  assert.equal(f.nodes[0].style.transform, "");
  assert.equal(f.calls.length, 0);
  f.move();
  const lateUp = f.window.handlers("pointerup");
  f.window.fire("pointerup");
  for (const handler of lateUp) handler({ pointerId: 1 });
  assert.deepEqual(f.calls, [["OnNodeReorder", "node-0", 2]]);
  assert.equal(f.window.count(), 0);
  assert.equal(f.nodes[0].handle.captured.size, 0);
});

test("table ignores a foreign pointer and commits the final preview width once", () => {
  const f = tableFixture();
  const before = f.widths();
  f.down();
  f.move(2); f.window.fire("pointerup", { pointerId: 2 }); f.window.fire("pointercancel", { pointerId: 2 });
  assert.deepEqual(f.widths(), before);
  assert.equal(f.calls.length, 0);
  f.move();
  const lateUp = f.window.handlers("pointerup");
  f.window.fire("pointerup");
  for (const handler of lateUp) handler({ pointerId: 1 });
  assert.deepEqual(f.calls, [["OnColumnResized", "name", 240]]);
  assert.ok(f.widths().every(([width]) => width === "240px"));
  assert.equal(f.window.count(), 0);
  assert.equal(f.grip.captured.size, 0);
});

test("replacement canvas gesture cancels the old pointer before beginning a new one", () => {
  const f = canvasFixture();
  f.down(); f.move();
  const oldMove = f.window.handlers("pointermove");
  const oldUp = f.window.handlers("pointerup");
  f.down(1, 2);
  assert.equal(f.nodes[0].style.transform, "");
  assert.equal(f.nodes[0].handle.captured.size, 0);
  for (const handler of oldMove) handler({ pointerId: 1, clientX: 900, clientY: 900 });
  for (const handler of oldUp) handler({ pointerId: 1 });
  assert.equal(f.calls.length, 0);
  f.move(2); f.window.fire("pointerup", { pointerId: 2 });
  assert.deepEqual(f.calls, [["OnNodeReorder", "node-1", 2]]);
});

test("replacement table gesture snapshots restored widths and owns only its callback", () => {
  const f = tableFixture();
  const before = f.widths();
  f.down(); f.move();
  const oldUp = f.window.handlers("pointerup");
  f.down(2);
  assert.deepEqual(f.widths(), before);
  for (const handler of oldUp) handler({ pointerId: 1 });
  assert.equal(f.calls.length, 0);
  f.move(2); f.window.fire("pointerup", { pointerId: 2 });
  assert.deepEqual(f.calls, [["OnColumnResized", "name", 240]]);
});

test("canvas unchanged drop does not report a reorder", () => {
  const f = canvasFixture();
  f.down(); f.window.fire("pointerup");
  assert.equal(f.calls.length, 0);
  assert.equal(f.window.count(), 0);
  assert.equal(f.nodes[0].handle.captured.size, 0);
});

test("horizontal canvas siblings retain their axis-specific drop behavior", () => {
  const f = canvasFixture();
  f.nodes.forEach((node, i) => { node.rect = { left: i * 100, top: 0, width: 60, height: 100 }; });
  f.down();
  f.window.fire("pointermove", { clientX: 240, clientY: 20 });
  f.window.fire("pointerup");
  assert.deepEqual(f.calls, [["OnNodeReorder", "node-0", 2]]);
});

test("non-primary buttons do not start a gesture or replace an active gesture", () => {
  const canvas = canvasFixture();
  canvas.root.fire("pointerdown", { target: canvas.nodes[0].handle, button: 2 });
  assert.equal(canvas.window.count(), 0);
  const table = tableFixture();
  table.root.fire("pointerdown", { target: table.grip, button: 2 });
  assert.equal(table.window.count(), 0);
  table.down(); table.move();
  const preview = table.widths();
  table.root.fire("pointerdown", { target: table.grip, button: 2, pointerId: 2 });
  assert.deepEqual(table.widths(), preview);
  table.window.fire("pointerup");
  assert.deepEqual(table.calls, [["OnColumnResized", "name", 240]]);
});

test("disconnect before the next event cancels both gesture families", () => {
  const canvas = canvasFixture();
  canvas.down(); canvas.move(); canvas.root.isConnected = false;
  canvas.window.fire("pointerup");
  assert.equal(canvas.calls.length, 0);
  assert.equal(canvas.nodes[0].style.transform, "");
  assert.equal(canvas.window.count(), 0);
  const table = tableFixture();
  const before = table.widths();
  table.down(); table.move(); table.root.isConnected = false;
  table.window.fire("pointerup");
  assert.equal(table.calls.length, 0);
  assert.deepEqual(table.widths(), before);
  assert.equal(table.window.count(), 0);
});

test("reinitializing replaces the .NET target and rejected commits are observed", async () => {
  for (const kind of ["canvas", "table"]) {
    const f = kind === "canvas" ? canvasFixture() : tableFixture();
    const calls = [];
    const replacement = { invokeMethodAsync(...args) { calls.push(args); return Promise.reject(new Error("closed circuit")); } };
    if (kind === "canvas") f.canvas.initReorder(f.root, replacement);
    else f.table.initColumnResize(f.root, replacement);
    f.down(); f.move(); f.window.fire("pointerup");
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(f.calls.length, 0);
    assert.equal(calls.length, 1);
  }
});
