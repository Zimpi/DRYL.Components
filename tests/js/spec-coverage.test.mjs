import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { execFileSync, spawnSync } from "node:child_process";

const script = resolve(import.meta.dirname, "../../scripts/check-spec-coverage.mjs");
const baselinePath = "scripts/spec-coverage-baseline.json";
const category = "specs/E1 Inputs";
const component = name => `code/Components/Dryl${name}.razor`;
const specPath = name => `${category}/F${name === "Alpha" ? 1 : 2} Dryl${name}.md`;
const spec = (name, source = component(name)) => `# Dryl${name}\n\n## Meta\n- **State:** Modified\n- **Source:** ${source}\n`;

function fixture(t) {
  const tempParent = resolve(tmpdir());
  const root = mkdtempSync(join(tempParent, "dryl-spec-coverage-"));
  t.after(() => {
    // Teardown is restricted to the directory this fixture just created.
    assert.equal(dirname(resolve(root)), tempParent);
    assert.ok(root.startsWith(join(tempParent, "dryl-spec-coverage-")));
    rmSync(root, { recursive: true, force: true });
  });
  const write = (path, text) => {
    mkdirSync(dirname(join(root, path)), { recursive: true });
    writeFileSync(join(root, path), text, "utf8");
  };
  const remove = path => rmSync(join(root, path));
  const baseline = paths => write(baselinePath, JSON.stringify({ version: 1, uncovered: paths }, null, 2));
  const run = (...args) => {
    const result = spawnSync(process.execPath, [script, "--root", root, ...args], { encoding: "utf8" });
    assert.ifError(result.error);
    assert.equal(result.signal, null, result.stderr);
    return { status: result.status, output: result.stdout + result.stderr, stdout: result.stdout };
  };
  const git = (...args) => execFileSync("git", ["-C", root, ...args], {
    encoding: "utf8", stdio: ["pipe", "pipe", "pipe"],
  }).trim();
  const commit = () => {
    git("add", "--all");
    git("-c", "user.name=DRYL fixture", "-c", "user.email=fixture@example.invalid",
      "-c", "commit.gpgsign=false", "commit", "-m", "fixture base");
    return git("rev-parse", "HEAD");
  };
  const initGit = () => {
    git("init", "--template=");
    git("config", "core.autocrlf", "false");
    git("config", "core.hooksPath", join(root, "unused-hooks"));
  };
  write("harness/requirements.md", "# Requirements\n\n| E | Category | Source | Count |\n|---|---|---|---|\n| `E1` | Inputs | code/Components/ | 2 |\n");
  write(`${category}/_Api.md`, "# Inputs API\n");
  write(`${category}/_Interop.md`, "# Inputs Interop\n\n## Interop\nnone\n\n## Services\nnone\n\n## Cleanup\nnone\n");
  write(component("Alpha"), "<input />\n");
  write(component("Beta"), "<input />\n");
  write(specPath("Alpha"), spec("Alpha"));
  baseline([component("Beta")]);
  return { root, write, remove, baseline, run, git, commit, initGit };
}

function passed(result, match) {
  assert.equal(result.status, 0, result.output);
  if (match) assert.match(result.output, match);
}

function failed(result, match) {
  assert.equal(result.status, 1, result.output);
  assert.match(result.output, match);
}

test("strict default still fails uncovered components and passes complete coverage", t => {
  const f = fixture(t);
  failed(f.run(), /1\/2 components covered/);
  failed(f.run("--quiet"), /1\/2 components covered/);
  assert.doesNotMatch(f.run("--quiet").stdout, /without a spec/);
  f.write(specPath("Beta"), spec("Beta"));
  passed(f.run(), /2\/2 components covered/);
});

test("phase C permits only the exact recorded uncovered set", t => {
  const f = fixture(t);
  passed(f.run("--baseline", baselinePath), /Phase-C baseline: 1 recorded uncovered components/);
  failed(f.run(), /1\/2 components covered/);
});

test("phase C rejects a new component with no spec", t => {
  const f = fixture(t);
  f.write(component("Gamma"), "<input />\n");
  failed(f.run("--baseline", baselinePath), /DrylGamma\.razor: newly uncovered component/);
});

test("lost coverage cannot be hidden by an unchanged covered-component count", t => {
  const f = fixture(t);
  f.remove(specPath("Alpha"));
  f.write(specPath("Beta"), spec("Beta"));
  const result = f.run("--baseline", baselinePath);
  failed(result, /DrylAlpha\.razor: newly uncovered component/);
  assert.match(result.output, /DrylBeta\.razor: stale baseline entry/);
  assert.match(result.output, /1\/2 components covered/);
});

test("new coverage requires shrinking the baseline", t => {
  const f = fixture(t);
  f.write(specPath("Beta"), spec("Beta"));
  failed(f.run("--baseline", baselinePath), /DrylBeta\.razor: stale baseline entry; remove it/);
  f.baseline([]);
  passed(f.run("--baseline", baselinePath), /2\/2 components covered/);
});

test("removed components must also leave the debt list", t => {
  const f = fixture(t);
  f.remove(component("Beta"));
  failed(f.run("--baseline", baselinePath), /DrylBeta\.razor: stale baseline entry/);
  f.baseline([]);
  passed(f.run("--baseline", baselinePath), /1\/1 components covered/);
});

test("a baseline never suppresses structural or source violations", async t => {
  const cases = [
    ["missing companion", f => f.remove(`${category}/_Api.md`), /missing — every category carries both companion files/],
    ["invalid state", f => f.write(specPath("Alpha"), spec("Alpha").replace("Modified", "Draft")), /State must be/],
    ["missing Meta heading", f => f.write(specPath("Alpha"), spec("Alpha").replace("## Meta\n", "")), /Meta block must follow the H1/],
    ["Meta after prose", f => f.write(specPath("Alpha"), spec("Alpha").replace("## Meta", "Description before Meta.\n\n## Meta")), /Meta block must follow the H1/],
    ["duplicate State", f => f.write(specPath("Alpha"), `${spec("Alpha")}- **State:** Implemented\n`), /duplicate State fields/],
    ["duplicate Source", f => f.write(specPath("Alpha"), `${spec("Alpha")}- **Source:** ${component("Alpha")}\n`), /duplicate Source blocks/],
    ["Source outside Meta", f => f.write(specPath("Alpha"), spec("Alpha").replace("- **Source:**", "## Example\n\n- **Source:**")), /component spec carries no Source block/],
    ["empty companion Meta", f => f.write(`${category}/_Api.md`, "# Inputs\n\n## Meta\n"), /carry no Meta block/],
    ["missing Source block", f => f.write(specPath("Alpha"), "# DrylAlpha\n\n## Meta\n- **State:** Modified\n"), /component spec carries no Source block/],
    ["new missing Source file", f => f.write(specPath("Alpha"), `${spec("Alpha")}              code/Components/Deleted.cs\n`), /Deleted\.cs.*does not exist/],
    ["duplicate component claim", f => f.write(`${category}/F3 DrylDuplicate.md`, spec("Alpha")), /DrylAlpha\.razor: claimed by 2 specs/],
    ["duplicate non-component claim", f => {
      f.write("code/Components/Shared.cs", "// shared\n");
      f.write(specPath("Alpha"), `${spec("Alpha")}              code/Components/Shared.cs\n`);
      f.write(`${category}/F3 DrylOther.md`, spec("Other", "code/Components/Shared.cs"));
    }, /Shared\.cs: claimed by 2 specs/],
    ["malformed continuation", f => f.write(specPath("Alpha"), `${spec("Alpha")}              - code/Components/Other.cs\n`), /malformed Source continuation/],
    ["source path alias", f => f.write(specPath("Alpha"), spec("Alpha", "code/Components/../Components/DrylAlpha.razor")), /must be repo-root-relative/],
    ["source points to directory", f => f.write(specPath("Alpha"), spec("Alpha", "code/Components")), /does not exist/],
    ["invalid spec filename", f => f.write(`${category}/notes.md`, spec("Alpha")), /not a valid spec file name/],
    ["story claims Source", f => {
      f.write(`${category}/F3 DrylSplit/_Component.md`, spec("Split", "code/Components/Extra.cs"));
      f.write("code/Components/Extra.cs", "// extra\n");
      f.write(`${category}/F3 DrylSplit/S1 Behavior.md`, spec("Split", "code/Components/Extra.cs"));
    }, /story file carries State only/],
  ];
  for (const [name, mutate, message] of cases) await t.test(name, child => {
    const f = fixture(child);
    mutate(f);
    failed(f.run("--baseline", baselinePath), message);
  });
});

test("malformed baselines fail closed", async t => {
  const cases = [
    ["invalid JSON", "{", /malformed baseline JSON/],
    ["wrong version", { version: 2, uncovered: [] }, /expected baseline/],
    ["missing version", { uncovered: [] }, /expected baseline/],
    ["unknown field", { version: 1, uncovered: [], allowAll: true }, /expected baseline/],
    ["wrong list shape", { version: 1, uncovered: "all" }, /expected baseline/],
    ["null identity", { version: 1, uncovered: [null] }, /canonical code/],
    ["path traversal", { version: 1, uncovered: ["code/../DrylBeta.razor"] }, /canonical code/],
    ["backslash path", { version: 1, uncovered: ["code\\Components\\DrylBeta.razor"] }, /canonical code/],
    ["newline identity", { version: 1, uncovered: [`${component("Beta")}\n`] }, /canonical code/],
    ["not a component", { version: 1, uncovered: ["code/Components/Helper.cs"] }, /canonical code/],
    ["duplicate identity", { version: 1, uncovered: [component("Beta"), component("Beta")] }, /duplicate baseline identities/],
    ["unsorted identities", { version: 1, uncovered: [component("Beta"), component("Alpha")] }, /must be sorted/],
  ];
  for (const [name, data, message] of cases) await t.test(name, child => {
    const f = fixture(child);
    f.write(baselinePath, typeof data === "string" ? data : JSON.stringify(data));
    failed(f.run("--baseline", baselinePath), message);
  });
});

test("Git guard rejects expanding an existing baseline even when the current set matches", t => {
  const f = fixture(t);
  f.initGit();
  const base = f.commit();
  f.write(component("Gamma"), "<input />\n");
  f.baseline([component("Beta"), component("Gamma")]);
  passed(f.run("--baseline", baselinePath));
  failed(f.run("--baseline", baselinePath, "--base-ref", base), /DrylGamma\.razor: baseline addition is not approved/);
});

test("Git guard permits shrinking the baseline with new coverage", t => {
  const f = fixture(t);
  f.initGit();
  const base = f.commit();
  f.write(specPath("Beta"), spec("Beta"));
  f.baseline([]);
  passed(f.run("--baseline", baselinePath, "--base-ref", base), /2\/2 components covered/);
});

test("first introduction bootstraps actual uncovered identities from the base commit", t => {
  const f = fixture(t);
  f.remove(baselinePath);
  f.initGit();
  const base = f.commit();
  f.baseline([component("Beta")]);
  passed(f.run("--baseline", baselinePath, "--base-ref", base), /1\/2 components covered/);
  f.remove(specPath("Alpha"));
  f.write(specPath("Beta"), spec("Beta"));
  f.baseline([component("Alpha")]);
  passed(f.run("--baseline", baselinePath));
  failed(f.run("--baseline", baselinePath, "--base-ref", base), /DrylAlpha\.razor: baseline addition is not approved/);
});

test("first introduction cannot record a newly added uncovered component", t => {
  const f = fixture(t);
  f.remove(baselinePath);
  f.initGit();
  const base = f.commit();
  f.write(component("Gamma"), "<input />\n");
  f.baseline([component("Beta"), component("Gamma")]);
  failed(f.run("--baseline", baselinePath, "--base-ref", base), /DrylGamma\.razor: baseline addition is not approved/);
});

test("malformed historical metadata cannot bootstrap approval", t => {
  const f = fixture(t);
  f.remove(baselinePath);
  f.write(specPath("Alpha"), spec("Alpha").replace("Modified", "Draft"));
  f.initGit();
  const base = f.commit();
  f.write(specPath("Alpha"), spec("Alpha"));
  f.baseline([component("Beta")]);
  failed(f.run("--baseline", baselinePath, "--base-ref", base), /cannot bootstrap baseline from malformed component spec/);
});

test("malformed historical baseline and unavailable refs fail closed", t => {
  const f = fixture(t);
  f.write(baselinePath, "{");
  f.initGit();
  const base = f.commit();
  f.baseline([component("Beta")]);
  failed(f.run("--baseline", baselinePath, "--base-ref", base), /malformed baseline JSON/);
  failed(f.run("--baseline", baselinePath, "--base-ref", "no-such-ref"), /Spec coverage check failed/);
});

test("CLI rejects incomplete or ambiguous arguments", t => {
  const f = fixture(t);
  failed(f.run("--baseline"), /requires a value/);
  failed(f.run("--base-ref", "HEAD"), /requires --baseline/);
  failed(f.run("--unknown"), /Unknown option/);
  failed(f.run("--quiet", "--quiet"), /Duplicate option/);
  failed(f.run("--baseline", "../outside.json"), /must name a file within --root/);
});

test("fixture paths can contain spaces and the check does not rewrite the baseline", t => {
  const f = fixture(t);
  const text = readFileSync(join(f.root, baselinePath), "utf8");
  f.write("scripts/recorded debt.json", text);
  passed(f.run("--baseline", "scripts/recorded debt.json"));
  assert.equal(readFileSync(join(f.root, baselinePath), "utf8"), text);
});
