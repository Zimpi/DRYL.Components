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
// Strict (default): node scripts/check-spec-coverage.mjs
// Phase C: add --baseline scripts/spec-coverage-baseline.json
// CI: add --base-ref <commit> to reject additions to approved debt.
// --root <directory> runs these same checks against an isolated fixture.
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join, resolve, relative, sep, posix } from "node:path";

function argumentsFrom(args) {
  const result = { root: resolve(import.meta.dirname, ".."), quiet: false };
  const seen = new Set();
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (seen.has(arg)) throw new Error(`Duplicate option: ${arg}`);
    seen.add(arg);
    if (arg === "--quiet") result.quiet = true;
    else if (["--root", "--baseline", "--base-ref"].includes(arg)) {
      const value = args[++i];
      if (!value || value.startsWith("--")) throw new Error(`${arg} requires a value`);
      result[{ "--root": "root", "--baseline": "baseline", "--base-ref": "baseRef" }[arg]] = value;
    } else throw new Error(`Unknown option: ${arg}`);
  }
  result.root = resolve(result.root);
  if (result.baseRef && !result.baseline) throw new Error("--base-ref requires --baseline");
  if (result.baseline) {
    result.baseline = relative(result.root, resolve(result.root, result.baseline)).split(sep).join("/");
    if (!canonicalPath(result.baseline)) throw new Error("--baseline must name a file within --root");
  }
  return result;
}

function canonicalPath(path) {
  return typeof path === "string" && path.length > 0 && !/[\\`,:\u0000-\u001f\u007f]/.test(path) &&
    !path.startsWith("/") && path.split("/").every(part => part !== "" && part !== "." && part !== "..");
}

function componentPath(path) {
  return canonicalPath(path) && path.startsWith("code/") && /^Dryl.+\.razor$/.test(posix.basename(path)) &&
    !path.split("/").some(part => part === "obj" || part === "bin");
}

function readBaseline(text, name) {
  let data;
  try { data = JSON.parse(text); }
  catch { throw new Error(`${name}: malformed baseline JSON`); }
  if (!data || Array.isArray(data) || data.version !== 1 || !Array.isArray(data.uncovered) ||
      Object.keys(data).some(key => !["version", "uncovered"].includes(key))) {
    throw new Error(`${name}: expected baseline { "version": 1, "uncovered": [component paths] }`);
  }
  if (data.uncovered.some(path => !componentPath(path))) throw new Error(`${name}: baseline entries must be canonical code/.../Dryl*.razor paths`);
  if (new Set(data.uncovered).size !== data.uncovered.length) throw new Error(`${name}: duplicate baseline identities`);
  if (data.uncovered.some((path, i) => i > 0 && data.uncovered[i - 1] > path)) throw new Error(`${name}: baseline identities must be sorted`);
  return data.uncovered;
}

try {
const options = argumentsFrom(process.argv.slice(2));

const root = options.root;
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
  const lines = text.replace(/^\uFEFF/, "").split(/\r?\n/);
  const heading = lines.findIndex(line => line.trim());
  const following = lines.findIndex((line, i) => i > heading && line.trim());
  const metaHeadings = lines.filter(line => /^## Meta\s*$/.test(line)).length;
  const meta = {
    state: null, sources: [], malformed: [], stateFields: 0, sourceFields: 0,
    metaHeadings,
    validPlacement: /^# \S/.test(lines[heading] ?? "") && /^## Meta\s*$/.test(lines[following] ?? "") && metaHeadings === 1,
  };
  // Only fields inside Meta count as a component contract. A Source-looking
  // example later in the prose must not conceal a missing contract.
  const start = lines.findIndex(line => /^## Meta\s*$/.test(line));
  const nextHeading = lines.findIndex((line, i) => i > start && /^#{1,2} /.test(line));
  const end = nextHeading < 0 ? lines.length : nextHeading;

  for (let i = start + 1; start >= 0 && i < end; i++) {
    const state = lines[i].match(/^-\s+\*\*State:\*\*\s*(.*?)\s*$/);
    if (state) { meta.state = state[1]; meta.stateFields++; }

    const source = lines[i].match(/^-\s+\*\*Source:\*\*\s*(.*?)\s*$/);
    if (!source) continue;
    meta.sourceFields++;

    if (source[1]) meta.sources.push(source[1]);
    for (let j = i + 1; j < end; j++) {
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

  if (!meta.validPlacement) errors.push(`${relPath}: exactly one Meta block must follow the H1 (SPEC-03)`);
  if (meta.stateFields > 1) errors.push(`${relPath}: duplicate State fields (SPEC-03)`);
  if (meta.sourceFields > 1) errors.push(`${relPath}: duplicate Source blocks (SPEC-03)`);
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
    if (!canonicalPath(path)) {
      errors.push(`${relPath}: Source path "${path}" must be repo-root-relative with forward slashes (SPEC-03)`);
      continue;
    }
    if (!existsSync(join(root, path)) || !statSync(join(root, path)).isFile()) {
      errors.push(`${relPath}: Source path "${path}" does not exist (SPEC-03)`);
      continue;
    }
    claim(path, relPath);
  }
}

function checkStoryFile(absPath, relPath) {
  const meta = parseMeta(readFileSync(absPath, "utf8"));
  if (!meta.validPlacement) errors.push(`${relPath}: exactly one Meta block must follow the H1 (SPEC-03)`);
  if (meta.stateFields > 1) errors.push(`${relPath}: duplicate State fields (SPEC-03)`);
  if (!["Modified", "Implemented"].includes(meta.state ?? "")) {
    errors.push(`${relPath}: State must be "Modified" or "Implemented" (SPEC-04), found ${meta.state === null ? "no State field" : `"${meta.state}"`}`);
  }
  if (meta.sourceFields > 0) {
    errors.push(`${relPath}: an S{n} story file carries State only — Source belongs in _Component.md (SPEC-03)`);
  }
}

function checkCompanionFile(absPath, relPath) {
  const text = readFileSync(absPath, "utf8");
  const meta = parseMeta(text);
  if (meta.metaHeadings || /^-\s+\*\*(?:State|Source):\*\*/m.test(text)) {
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

if (options.baseline) {
  const allowed = readBaseline(readFileSync(join(root, options.baseline), "utf8"), options.baseline);
  const allowedSet = new Set(allowed);
  const uncoveredSet = new Set(uncovered);
  for (const path of uncovered) {
    if (!allowedSet.has(path)) errors.push(`${path}: newly uncovered component; add or restore its spec instead of expanding the baseline`);
  }
  for (const path of allowed) {
    if (!uncoveredSet.has(path)) errors.push(`${path}: stale baseline entry; remove it because it is now covered or no longer a component`);
  }

  if (options.baseRef) {
    const git = args => execFileSync("git", ["-C", root, ...args], {
      encoding: "utf8", maxBuffer: 32 * 1024 * 1024, stdio: ["pipe", "pipe", "pipe"],
    });
    const gitRoot = resolve(git(["rev-parse", "--show-toplevel"]).trim());
    const sameRoot = process.platform === "win32" ? gitRoot.toLowerCase() === root.toLowerCase() : gitRoot === root;
    if (!sameRoot) throw new Error("--base-ref requires --root to be the Git repository root");
    const commit = git(["rev-parse", "--verify", "--end-of-options", `${options.baseRef}^{commit}`]).trim();
    const previousFiles = new Set(git(["ls-tree", "-r", "--name-only", "-z", commit]).split("\0").filter(Boolean));
    const readPrevious = path => git(["show", `${commit}:${path}`]);
    let previouslyAllowed;
    if (previousFiles.has(options.baseline)) {
      previouslyAllowed = readBaseline(readPrevious(options.baseline), `${options.baseline} at ${options.baseRef}`);
    } else {
      // First introduction: derive debt from the base commit's real component
      // identities and valid Source claims. No count-based or permissive bootstrap.
      const previousClaims = new Map();
      for (const path of previousFiles) {
        if (!/^specs\/E\d+ [^/]+\/F\d+ [^/]+(?:\.md|\/_Component\.md)$/.test(path)) continue;
        const meta = parseMeta(readPrevious(path));
        if (!meta.validPlacement || meta.stateFields !== 1 || meta.sourceFields !== 1 ||
            !["Modified", "Implemented"].includes(meta.state) || !meta.sources.length || meta.malformed.length ||
            meta.sources.some(source => !canonicalPath(source) || !previousFiles.has(source))) {
          throw new Error(`${path}: cannot bootstrap baseline from malformed component spec at ${options.baseRef}`);
        }
        for (const source of meta.sources) previousClaims.set(source, (previousClaims.get(source) ?? 0) + 1);
      }
      for (const [path, count] of previousClaims) {
        if (count > 1) throw new Error(`${path}: cannot bootstrap baseline from duplicate Source claims at ${options.baseRef}`);
      }
      previouslyAllowed = [...previousFiles].filter(path => componentPath(path) && !previousClaims.has(path));
    }
    const previousSet = new Set(previouslyAllowed);
    for (const path of allowed) {
      if (!previousSet.has(path)) errors.push(`${path}: baseline addition is not approved by base ref ${options.baseRef}`);
    }
  }
  console.log(`Phase-C baseline: ${allowed.length} recorded uncovered components.`);
}

if (errors.length) {
  console.error(`Violations (${errors.length}):`);
  for (const e of errors) console.error(`  ${e}`);
  console.error("");
}

console.log(`${covered.length}/${components.length} components covered`);

if (uncovered.length && !options.quiet) {
  console.log(`${uncovered.length} without a spec:`);
  for (const c of uncovered) console.log(`  ${c}`);
}

if (errors.length || (!options.baseline && uncovered.length)) process.exitCode = 1;
} catch (error) {
  console.error(`Spec coverage check failed: ${error.message}`);
  process.exitCode = 1;
}
