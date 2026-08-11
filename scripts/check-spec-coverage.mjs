// Checks specs/ against code/ — the invariant behind SPEC-02 and SPEC-03.
//
// Structure (SPEC-02):
//   1. Every directory under specs/ is "E{n} {Category}" and appears in the
//      category table in harness/requirements.md; every category in that table
//      exists under specs/.
//   2. Every category carries _Api.md and _Interop.md.
//   3. Every spec file is "F{n} {DrylComponent}.md", or a folder
//      "F{n} {DrylComponent}/" holding exactly one _Component.md plus
//      "S{n} {Aspect}.md" story files.
//
// Meta blocks (SPEC-03):
//   4. Every component spec (F{n}.md or F{n}/_Component.md) carries State and
//      Source; every S{n} file carries State and no Source; _Api.md and
//      _Interop.md carry neither.
//   5. Every Source path is repo-root-relative with forward slashes, and exists.
//
// Coverage (SPEC-03, both directions):
//   6. Every Dryl*.razor under code/ appears in exactly one Source block —
//      none uncovered, none claimed twice.
//
// Run: node scripts/check-spec-coverage.mjs
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join, resolve, relative, sep } from "node:path";

const root = resolve(import.meta.dirname, "..");
const specsDir = join(root, "specs");
const errors = [];

// ---------------------------------------------------------------- categories

// The table in SPEC-02 is the single source of truth for the category list.
// Rows look like: | `E1` | Foundation | — (no components) | 0 |
const requirements = readFileSync(join(root, "harness", "requirements.md"), "utf8");
const expectedCategories = [
  ...requirements.matchAll(/^\|\s*`(E\d+)`\s*\|\s*([^|]+?)\s*\|/gm),
].map(([, e, name]) => `${e} ${name}`);

if (expectedCategories.length === 0) {
  errors.push("harness/requirements.md: could not parse the SPEC-02 category table");
}

const actualCategories = readdirSync(specsDir, { withFileTypes: true })
  .filter((d) => d.isDirectory())
  .map((d) => d.name)
  .sort();

for (const dir of actualCategories) {
  if (!/^E\d+ .+/.test(dir)) {
    errors.push(`specs/${dir}: directory name does not match "E{n} {Category}" (SPEC-02)`);
  } else if (!expectedCategories.includes(dir)) {
    errors.push(`specs/${dir}: not listed in the SPEC-02 category table`);
  }
}

for (const cat of expectedCategories) {
  if (!actualCategories.includes(cat)) {
    errors.push(`specs/${cat}: listed in the SPEC-02 category table but does not exist`);
  }
}

// ------------------------------------------------------------------ metadata

// SPEC-03's Source format, written so it parses without guessing: the first
// path sits on the "- **Source:**" line, each further path is a continuation
// line indented with whitespace and carrying nothing but the path.
function parseMeta(text) {
  const lines = text.split("\n");
  const meta = { state: null, sources: [], malformed: [] };

  for (let i = 0; i < lines.length; i++) {
    const state = lines[i].match(/^-\s+\*\*State:\*\*\s*(.+?)\s*$/);
    if (state) meta.state = state[1];

    const source = lines[i].match(/^-\s+\*\*Source:\*\*\s*(.*?)\s*$/);
    if (!source) continue;

    if (source[1]) meta.sources.push(source[1]);
    for (let j = i + 1; j < lines.length; j++) {
      const line = lines[j];
      if (!/^\s+\S/.test(line)) break; // no longer an indented continuation
      const path = line.trim();
      if (/^[-*#|]/.test(path) || /[`,]/.test(path) || /\s/.test(path)) {
        meta.malformed.push(path);
      } else {
        meta.sources.push(path);
      }
    }
  }
  return meta;
}

const claims = new Map(); // repo-relative path -> [spec file, …]

function claim(path, specFile) {
  if (!claims.has(path)) claims.set(path, []);
  claims.get(path).push(specFile);
}

function checkComponentSpec(absPath, relPath) {
  const meta = parseMeta(readFileSync(absPath, "utf8"));

  if (!["Modified", "Implemented"].includes(meta.state ?? "")) {
    errors.push(`${relPath}: State must be "Modified" or "Implemented" (SPEC-04), found ${meta.state === null ? "no State field" : `"${meta.state}"`}`);
  }
  if (meta.sources.length === 0) {
    errors.push(`${relPath}: component spec carries no Source block (SPEC-03)`);
  }
  for (const bad of meta.malformed) {
    errors.push(`${relPath}: malformed Source continuation line "${bad}" — one bare path per line (SPEC-03)`);
  }
  for (const path of meta.sources) {
    if (path.startsWith("/") || path.startsWith("./") || path.includes("\\")) {
      errors.push(`${relPath}: Source path "${path}" must be repo-root-relative with forward slashes (SPEC-03)`);
      continue;
    }
    if (!existsSync(join(root, path))) {
      errors.push(`${relPath}: Source path "${path}" does not exist (SPEC-03)`);
      continue;
    }
    claim(path, relPath);
  }
}

function checkStoryFile(absPath, relPath) {
  const meta = parseMeta(readFileSync(absPath, "utf8"));
  if (!["Modified", "Implemented"].includes(meta.state ?? "")) {
    errors.push(`${relPath}: State must be "Modified" or "Implemented" (SPEC-04), found ${meta.state === null ? "no State field" : `"${meta.state}"`}`);
  }
  if (meta.sources.length > 0) {
    errors.push(`${relPath}: an S{n} story file carries State only — Source belongs in _Component.md (SPEC-03)`);
  }
}

function checkCompanionFile(absPath, relPath) {
  const meta = parseMeta(readFileSync(absPath, "utf8"));
  if (meta.state !== null || meta.sources.length > 0) {
    errors.push(`${relPath}: _Api.md and _Interop.md carry no Meta block (SPEC-03)`);
  }
}

// ----------------------------------------------------------------- structure

for (const cat of actualCategories) {
  const catDir = join(specsDir, cat);

  for (const companion of ["_Api.md", "_Interop.md"]) {
    const abs = join(catDir, companion);
    if (!existsSync(abs)) {
      errors.push(`specs/${cat}/${companion}: missing — every category carries both companion files (SPEC-02)`);
    } else {
      checkCompanionFile(abs, `specs/${cat}/${companion}`);
    }
  }

  for (const entry of readdirSync(catDir, { withFileTypes: true })) {
    const name = entry.name;
    if (name === "_Api.md" || name === "_Interop.md") continue;

    if (entry.isFile()) {
      if (!/^F\d+ .+\.md$/.test(name)) {
        errors.push(`specs/${cat}/${name}: not a valid spec file name — expected "F{n} {DrylComponent}.md" (SPEC-02)`);
        continue;
      }
      checkComponentSpec(join(catDir, name), `specs/${cat}/${name}`);
      continue;
    }

    // A directory is a split component (SPEC-02).
    if (!/^F\d+ .+/.test(name)) {
      errors.push(`specs/${cat}/${name}/: not a valid split-component folder — expected "F{n} {DrylComponent}/" (SPEC-02)`);
      continue;
    }

    const splitDir = join(catDir, name);
    const inside = readdirSync(splitDir);
    if (!inside.includes("_Component.md")) {
      errors.push(`specs/${cat}/${name}/: split component carries no _Component.md (SPEC-02)`);
    } else {
      checkComponentSpec(join(splitDir, "_Component.md"), `specs/${cat}/${name}/_Component.md`);
    }

    for (const child of inside) {
      if (child === "_Component.md") continue;
      if (!/^S\d+ .+\.md$/.test(child)) {
        errors.push(`specs/${cat}/${name}/${child}: not a valid story file name — expected "S{n} {Aspect}.md" (SPEC-02)`);
        continue;
      }
      checkStoryFile(join(splitDir, child), `specs/${cat}/${name}/${child}`);
    }
  }
}

// ------------------------------------------------------------------ coverage

function findComponents(dir, found = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === "obj" || entry.name === "bin") continue;
    const abs = join(dir, entry.name);
    if (entry.isDirectory()) findComponents(abs, found);
    else if (/^Dryl.+\.razor$/.test(entry.name)) {
      found.push(relative(root, abs).split(sep).join("/"));
    }
  }
  return found;
}

const components = findComponents(join(root, "code")).sort();
const covered = components.filter((c) => (claims.get(c) ?? []).length === 1);
const uncovered = components.filter((c) => !claims.has(c));
const duplicated = components.filter((c) => (claims.get(c) ?? []).length > 1);

for (const c of duplicated) {
  errors.push(`${c}: claimed by ${claims.get(c).length} specs — ${claims.get(c).join(", ")} (SPEC-03)`);
}

// A Source path that is not a Dryl*.razor is legitimate (a codebehind, a
// stylesheet, an owned enum), but no two specs may claim the same file.
for (const [path, specFiles] of claims) {
  if (specFiles.length > 1 && !components.includes(path)) {
    errors.push(`${path}: claimed by ${specFiles.length} specs — ${specFiles.join(", ")} (SPEC-03)`);
  }
}

// ------------------------------------------------------------------- report

if (errors.length) {
  console.error(`Violations (${errors.length}):`);
  for (const e of errors) console.error(`  ${e}`);
  console.error("");
}

console.log(`${covered.length}/${components.length} components covered`);

if (uncovered.length && !process.argv.includes("--quiet")) {
  console.log(`${uncovered.length} without a spec:`);
  for (const c of uncovered) console.log(`  ${c}`);
}

if (errors.length || uncovered.length) process.exit(1);
