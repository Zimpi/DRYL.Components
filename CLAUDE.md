# Instructions for Claude (and any AI agent) — DRYL Component Library

You are helping build **DRYL**, an open-source UI component library for Blazor
Server and Blazor WebAssembly.

DRYL is **glassy, alive — and AI-native**: translucent layers on a deep-dark or
luminous-light ground, following the user's system by default, with accents that
glow instead of shouting. Every component reads from CSS variables in
`code/DRYL.Components/wwwroot/dryl.css` — never a hardcoded color, size, radius,
shadow or duration. AI is a first-class state of the UI: a component that opts into
AI styling takes an `Ai` parameter of type `AiState` that drives one shared visual
vocabulary, so a user can feel where the AI is at work without reading a label.

DRYL is **spec-driven**: a component's spec and its code are one artifact.

---

## How work happens here — the order is binding

Never skip a stage, never start at a later one because the work "looks small".
Each stage has a rulebook; read it when you enter the stage.

**1. Idea** → [`harness/ideas.md`](harness/ideas.md)
A new feature, a larger change, anything not yet specified starts as a
dialogue, not as code. The Product Owner brings the idea; you challenge it,
check feasibility against `harness/`, the specs and `code/`, and maintain
`ideas/I{n} {Name}.md` while it matures. A new token, a new animation, a new
`AiState` or a new dependency is a **blocker needing maintainer sign-off** —
surface it here, not in the code. The idea leaves this stage as `Ready`.

**2. Spec** → [`harness/requirements.md`](harness/requirements.md)
A `Ready` idea becomes Epics/Features/Stories under `specs/`, with a `Meta`
block naming its `State` and its `Source` files, and INVEST acceptance
criteria. Nothing is implemented from an idea document — only from a spec.

**3. Plan**
Non-trivial work gets a written implementation plan before any edit: exact
files per task, exact commands, a verification step per task, and one commit
per task. A task is the smallest unit that carries its own verification.

**4. Implementation**
Work the plan task by task. Use the main agent by default and delegate
selectively when a task benefits from isolated context, independent reasoning,
or can be implemented as a clearly bounded unit.

- Do not dispatch a subagent mechanically for every task. Small, tightly
  coupled, or context-heavy changes should stay with the main agent.
- Implementation subagents are encouraged for self-contained tasks with clear
  boundaries, acceptance criteria, and interfaces.
- Give subagents the task, relevant interfaces, acceptance criteria, and
  binding constraints. Avoid unnecessary session history or unrelated plan
  details.
- Do not run multiple implementation agents against overlapping code on the
  same working tree. Parallelize only genuinely independent work.
- Use independent review for non-trivial, risky, or architecturally relevant
  changes. Trivial or mechanical changes do not require a separate reviewer.
- Prefer sending review findings back to the agent that implemented the change
  when substantial rework is required.
- Track progress externally for longer multi-step plans so completed work is
  not accidentally repeated.

**5. Verification, then the claim** — in that order
Never report work as done, fixed or passing before running the commands that
prove it and reading their output. Evidence first, assertion second. For this
repository the evidence is: `dotnet build DRYL.slnx -c Release`,
`dotnet test DRYL.slnx -c Release`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`,
`node scripts/check-motion-tokens.mjs`, and both color modes checked by eye.
The coverage check exits non-zero until every component has a spec; during
phase C its `x/128 components covered` line is the progress meter, and a rising
number is the evidence — not a green exit.
If a step was skipped, say so. If tests fail, say so with the output.

**6. Close the loop** → [`harness/releasing.md`](harness/releasing.md)
Spec `State` updated, `CHANGELOG.md` entry written, `<Version>` bumped,
`ComponentCatalog` in `DRYL.Website` registered.

---

## Read before you work

| What you are doing | Read first |
|---|---|
| A new idea, no spec yet | [`harness/ideas.md`](harness/ideas.md) |
| A new or changed component | [`harness/requirements.md`](harness/requirements.md) + that component's spec |
| Writing code | [`harness/code.md`](harness/code.md) |
| CSS, color, motion | [`harness/design.md`](harness/design.md) + [`harness/tokens.md`](harness/tokens.md) |
| Interaction, keyboard, a11y | [`harness/uiux.md`](harness/uiux.md) |
| AI behaviour | [`harness/ai.md`](harness/ai.md) |
| Version, changelog, release | [`harness/releasing.md`](harness/releasing.md) |
| Component anatomy | [`harness/patterns.md`](harness/patterns.md) |
| Public API naming | [`harness/conventions.md`](harness/conventions.md) |
| Consumer theming | [`harness/theming.md`](harness/theming.md) |

Every rule has a stable ID. Cite it when you flag a violation.

---

## The nine rules you may not break

1. **Tokens, not literals.** Every color, padding, radius, shadow, duration and
   easing references a CSS variable. → `DESIGN-01`
2. **Two modes, one identity.** Components never branch on the mode, and never
   write a mode-assuming value. A per-mode value becomes a token in both
   LIGHT-TOKEN-SET copies. → `DESIGN-02`
3. **Frost only where it can be seen.** Floating → `--panel-float` +
   `--glass-fx-float`. In the flow → `--glass-fx-flow`. On an opaque ground →
   none. Never hand-write `backdrop-filter: blur(...)` on a new in-flow
   surface. → `DESIGN-06`, `DESIGN-07`
4. **Accents glow, never scream.** Gradient, 1px border, glow ring or small
   indicator — never the fill of a large surface. → `DESIGN-08`
5. **Fixed motion vocabulary, and everything moves.** Three durations, three
   easings, no `linear`. Every component is deliberately animated; anything that
   mounts conditionally wraps in `DrylPresence`.
   → `DESIGN-10`, `DESIGN-11`, `DESIGN-12`
6. **`Dryl`-prefixed components, typed parameters.** `enum` for variants, never
   `string`. → `CODE-01`, `CODE-02`
7. **Zero external runtime dependencies.** No npm, no JS framework. `Markdig` is
   the one approved exception. → `CODE-03`
8. **Touching library code means bumping `<Version>` and writing a changelog
   entry, in the same commit** — unless `<Version>` already names a version that
   has not shipped yet, in which case the entry joins that block and the version
   stays put. → `REL-01`, `REL-02`
9. **Specs and code are one artifact.** Every change to a component's behaviour
   or public API updates its spec in the same commit. A spec that no longer
   matches its code goes back to `State: Modified` — never leave it on
   `Implemented`. Do not write code for a component whose spec you have not
   read. → `SPEC-01`

Evidence and references cite selectors, symbol names and file paths — never line
numbers (`SPEC-09`).

If a value, a state or a primitive you need does not exist: **do not invent it.**
Propose adding it and ask the maintainer. That is the bar for tokens
(`DESIGN-03`), motion (`DESIGN-10`), AI visuals (`AI-04`) and dependencies
(`CODE-03`) alike.

Several rules record **pre-existing violations** in their Check line. They are
documented debt, not permission: never add a new one, and never read a rule's
hit count as evidence that the codebase is clean.

---

## Repository layout

| Path | What lives there |
|---|---|
| `code/` | The two library projects (`DRYL.Components`, `DRYL.Components.Agents`) |
| `harness/` | The rules — this file routes to them |
| `specs/` | One spec per component; the contract. Fifteen categories, filled in phase C |
| `ideas/` | Ideas in dialogue, before they become specs |
| `tests/` | bUnit tests (`DRYL.Components.Tests`) |
| `scripts/` | Token sync, contrast, harness-link and spec-coverage checks |
| `docs/` | Screenshots, gifs, plans and archive |
