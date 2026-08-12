// Checks the harness for two invariants:
//   1. Every relative markdown link in CLAUDE.md and harness/*.md resolves to
//      an existing file.
//   2. Every rule ID referenced anywhere (CODE-01, DESIGN-07, SPEC-03, …)
//      exists as a heading in some harness file.
// Run: node scripts/check-harness-links.mjs
import { readFileSync, readdirSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const files = [
  "CLAUDE.md",
  ...readdirSync(join(root, "harness"))
    .filter((f) => f.endsWith(".md"))
    .map((f) => join("harness", f)),
];

const ID = /\b(CODE|DESIGN|UX|AI|SPEC|IDEA|REL)-\d{2}\b/g;
const LINK = /\[[^\]]*\]\(([^)#]+)\)/g;

const defined = new Set();
const referenced = new Map(); // id -> [file, …]
const brokenLinks = [];

for (const rel of files) {
  const text = readFileSync(join(root, rel), "utf8");

  for (const line of text.split("\n")) {
    // A heading that starts with an ID defines that ID.
    const heading = line.match(/^#{2,4}\s+((CODE|DESIGN|UX|AI|SPEC|IDEA|REL)-\d{2})\b/);
    if (heading) defined.add(heading[1]);
  }

  for (const m of text.matchAll(ID)) {
    if (!referenced.has(m[0])) referenced.set(m[0], []);
    referenced.get(m[0]).push(rel);
  }

  for (const m of text.matchAll(LINK)) {
    const target = m[1];
    if (/^[a-z]+:/.test(target)) continue; // http:, mailto:, …
    const abs = resolve(root, dirname(rel), target);
    if (!existsSync(abs)) brokenLinks.push(`${rel} → ${target}`);
  }
}

const undefinedIds = [...referenced.keys()].filter((id) => !defined.has(id));
const unreferenced = [...defined].filter(
  (id) => (referenced.get(id) ?? []).length < 2,
);

let failed = false;

if (brokenLinks.length) {
  console.error(`Broken links (${brokenLinks.length}):`);
  for (const l of brokenLinks) console.error(`  ${l}`);
  failed = true;
}

if (undefinedIds.length) {
  console.error(`Referenced but never defined (${undefinedIds.length}):`);
  for (const id of undefinedIds.sort()) {
    console.error(`  ${id} — cited in ${[...new Set(referenced.get(id))].join(", ")}`);
  }
  failed = true;
}

if (unreferenced.length) {
  console.warn(`Defined but never cited elsewhere (${unreferenced.length}):`);
  for (const id of unreferenced.sort()) console.warn(`  ${id}`);
}

if (failed) process.exit(1);
console.log(`OK — ${defined.size} rule IDs, ${files.length} files, no broken links.`);
