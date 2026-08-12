// Verifies the two LIGHT-TOKEN-SET copies in dryl.css are identical.
import { readFileSync } from "node:fs";

const css = readFileSync(new URL("../code/DRYL.Components/wwwroot/dryl.css", import.meta.url), "utf8");
const blocks = [];
const re = /LIGHT-TOKEN-SET — copy [12]\/2 \*\//g;
let m;
while ((m = re.exec(css)) !== null) {
  const start = m.index + m[0].length;
  let depth = 1, i = start;
  while (i < css.length && depth > 0) {
    if (css[i] === "{") depth++;
    else if (css[i] === "}") depth--;
    i++;
  }
  const body = css.slice(start, i - 1);
  blocks.push(body.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\s+/g, " ").trim());
}
if (blocks.length !== 2) { console.error(`Expected 2 LIGHT-TOKEN-SET blocks, found ${blocks.length}`); process.exit(1); }
if (blocks[0] !== blocks[1]) { console.error("LIGHT-TOKEN-SET copies differ!"); process.exit(1); }
console.log("LIGHT-TOKEN-SET copies are in sync.");
