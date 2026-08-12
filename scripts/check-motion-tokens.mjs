// Checks DESIGN-10 — the fixed motion vocabulary.
//
// Every `transition` / `animation` shorthand is split into its comma-separated
// segments and each segment is judged on its own. That matters: a declaration
// may mix a continuous animation with a one-shot, as `.ai-aura.ai-generated
// .ai-aura-glow` does. Judging the whole declaration — or the line, as the
// original grep did — lets the one-shot hide behind the other's `infinite`.
//
//   1. One-shot segments (no `infinite`) carry no literal duration or delay:
//      every time value comes from var(--dur-*) or var(--delay-*).
//   2. Continuous segments (`infinite`) may pick any duration and may use
//      `linear`, but the easing, where given, is a token — the bare
//      `ease-in-out` keyword is a different curve from var(--ease-in-out).
//
// A value multiplied by an index — calc(var(--i) * 30ms) — is a stagger step,
// not a beat chosen by eye, and is exempt under both rules.
//
// Run: node scripts/check-motion-tokens.mjs
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, resolve, relative, sep } from "node:path";

const root = resolve(import.meta.dirname, "..");
const violations = [];

function cssFiles(dir, found = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === "obj" || entry.name === "bin" || entry.name === "node_modules") continue;
    const abs = join(dir, entry.name);
    if (entry.isDirectory()) cssFiles(abs, found);
    else if (entry.name.endsWith(".css")) found.push(abs);
  }
  return found;
}

// Split on commas that sit at bracket depth zero, so calc(a, b) stays whole.
function topLevelSegments(value) {
  const out = [];
  let depth = 0;
  let current = "";
  for (const ch of value) {
    if (ch === "(") depth++;
    else if (ch === ")") depth--;
    if (ch === "," && depth === 0) {
      out.push(current);
      current = "";
    } else current += ch;
  }
  out.push(current);
  return out;
}

const BARE_EASINGS = /(?<![-\w(])(ease-in-out|ease-in|ease-out|ease)(?![-\w])/;
const TIME_LITERAL = /(?<![-\w(.])(\d+(?:\.\d+)?)(m?s)\b/g;

// Zero is not a design value: `0s` says "no delay", not "this much delay".
function hasNonZeroTime(segment) {
  return [...segment.matchAll(TIME_LITERAL)].some(([, n]) => parseFloat(n) !== 0);
}

for (const file of cssFiles(join(root, "code"))) {
  const rel = relative(root, file).split(sep).join("/");
  // Strip comments first: a duration named in prose is not a declaration.
  const text = readFileSync(file, "utf8").replace(/\/\*[\s\S]*?\*\//g, "");
  const lines = text.split("\n");

  for (const match of text.matchAll(/\b(transition|animation)\s*:([^;{}]*);/g)) {
    const [, property, value] = match;
    const line = lines.length - text.slice(match.index).split("\n").length + 1;

    for (const raw of topLevelSegments(value)) {
      // A stagger step is a step times an index, not a chosen value.
      const segment = raw.replace(/calc\([^)]*\)/g, "");
      const continuous = /\binfinite\b/.test(segment);

      if (!continuous && hasNonZeroTime(segment)) {
        violations.push(
          `${rel}:${line}: ${property} segment "${raw.trim()}" uses a literal time — ` +
            `one-shot motion reads var(--dur-*) / var(--delay-*) (DESIGN-10)`,
        );
      }
      if (BARE_EASINGS.test(segment)) {
        violations.push(
          `${rel}:${line}: ${property} segment "${raw.trim()}" uses a bare easing keyword — ` +
            `write var(--ease-*); the keyword is a different curve (DESIGN-10)`,
        );
      }
    }
  }
}

if (violations.length) {
  console.error(`DESIGN-10 violations (${violations.length}):`);
  for (const v of violations) console.error(`  ${v}`);
  process.exit(1);
}

console.log("DESIGN-10: no literal durations, delays or bare easings in motion shorthands.");
