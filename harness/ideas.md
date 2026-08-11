# Idea Rules

> **Status: not yet active.** These rules take effect in phase B, once
> `specs/` holds real specs. The feasibility check in `IDEA-05` reads
> `specs/`; until there is something to read, the process would be empty
> ceremony. Until then this file documents the intended process.

How a rough feature idea becomes a mature, documented idea — **before** any
spec or code exists. Once an idea is `Ready`, it is carried over into specs
following [`requirements.md`](requirements.md).

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

### IDEA-01 — Scope: when this process applies

Status: **binding** | Enforced: **review**

The process applies whenever a new idea, a new feature or a larger change
comes up that does not yet exist as a spec. The Tech Lead does **not** start
with implementation and does **not** write specs — it begins the idea
dialogue instead.

Check: a spec or code change with no corresponding idea document under
`ideas/` (state `Ready` or `Adopted`) is questioned in review.

### IDEA-02 — Roles

Status: **binding** | Enforced: **review**

| Role | Who | Responsibility |
|---|---|---|
| **Product Owner** | Jan (DRYL) | Brings the idea, knows the goal, makes every final decision |
| **Tech Lead** | Claude | Challenges the idea, checks feasibility against `harness/`, `specs/` and `code/`, proposes 2–3 options with a recommendation, maintains the idea document |

The product responsibility sits with the maintainer; the Tech Lead's
contribution is technical feasibility and principled resistance, never the
product decision.

Check: the idea document's `## Decisions` entries are attributable to the
Product Owner; the `## Impact` analysis is attributable to the Tech Lead.

### IDEA-03 — Dialogue ground rules

Status: **binding** | Enforced: **review**

The Tech Lead runs an active, critical conversation — it **never simply nods
an idea through**. The dialogue runs in rounds until the Definition of Ready
(`IDEA-06`) is fully met.

- **Ask actively:** concrete questions per round — focused, at most about
  three at once, so a real conversation happens, not a questionnaire.
- **Stay critical:** challenge assumptions, surface alternatives, name edge
  cases and contradictions. A vague answer ("we'll figure it out somehow") is
  not accepted — it is made concrete.
- **Business before technical:** clarify the problem and the benefit first,
  then the solution, technology last.
- **Record decisions:** every decision made in the dialogue is logged in the
  idea document (`IDEA-07`'s format), so nothing is lost.
- **The Tech Lead proposes:** on open points, it proposes 2–3 concrete
  options with pros/cons and a recommendation, instead of only asking.

Check: the idea document's `## Decisions` section is non-empty for any idea
past `Draft`; a round with more than about three questions, or a `Ready` idea
with no recorded challenge, is flagged in review.

### IDEA-04 — The five phases

Status: **binding** | Enforced: **review**

1. **Understand** — What problem does the idea solve? For whom? What is the
   benefit? What happens today without the feature?
2. **Challenge** — Is the idea the best solution to the problem? What
   alternatives exist? What is explicitly **not** part of the scope? What
   edge cases exist?
3. **Check feasibility** — see `IDEA-05`.
4. **Refine** — open points from phases 1–3 are closed one by one. The
   dialogue continues until no open point remains.
5. **Close** — the Tech Lead summarizes the final idea, the Product Owner
   confirms it explicitly. Only then is the idea document filed as `Ready`.

Check: the idea document shows evidence of each phase (problem statement,
alternatives considered, `## Impact` filled in, empty `## Open Points`,
explicit Product Owner confirmation) before its state is set to `Ready`.

### IDEA-05 — The harness feasibility gate

Status: **binding** | Enforced: **review**

Phase 3 (`IDEA-04`) checks the idea against four sources. `harness/` comes
first and is checked before the other three:

- **Harness** — does the idea require a new token, a new animation/duration/
  easing, a new `AiState`, or a new runtime dependency? Each of these is a
  **blocker requiring maintainer sign-off** (`DESIGN-01`, `DESIGN-03`,
  `DESIGN-10`, `AI-04`, `CODE-03`), not a detail for later.
- **Specs** — does it fit the existing categories and components under
  `specs/`? Any overlap or contradiction with existing acceptance criteria?
- **Public API** — do the enums, parameters and services in the relevant
  `_Api.md` suffice? What would have to change? Post-1.0, a rename is MAJOR
  (`REL-01`).
- **Code** — is it buildable within the existing architecture under `code/`?
  Where are the touch points, where are the risks?

These four are the "don't invent" rules of the system. The idea dialogue is
the right place to surface them — discovered in code, they are found too
late.

Check: the idea document's `## Impact` section addresses all four sources;
every harness blocker it names carries either a resolution or an explicit
maintainer sign-off before the idea reaches `Ready` (`IDEA-06`).

### IDEA-06 — Definition of Ready

Status: **binding** | Enforced: **review**

An idea is only "perfect" (state `Ready`) once **all** points are met:

- [ ] Problem and benefit are clearly stated.
- [ ] The target role is named.
- [ ] Scope is bounded: what is in, what is explicitly out.
- [ ] The desired behaviour is concrete enough to derive INVEST acceptance
      criteria from it (see [`requirements.md`](requirements.md)).
- [ ] Feasibility checked against harness, specs, public API and code; the
      impact is documented concretely.
- [ ] Every harness blocker is either resolved or has maintainer sign-off.
- [ ] No open points remain.
- [ ] The Product Owner has explicitly confirmed the final version.

Check: the reviewer walks all eight points against the idea document before
accepting a state change to `Ready`.

### IDEA-07 — Filing, format and states

Status: **binding** | Enforced: **review**

Each idea is filed as its own file under `ideas/` (repo root):

```
ideas/I{n} {Name}.md
```

- **I** = idea, running number starting at 1; numbers stay stable, new ideas
  are appended at the end.
- The document is maintained **during** the dialogue (state `Draft`), not
  only at the end — an idea may mature over days.

#### Idea format

```markdown
# {Idea Title}

## Meta
- **State:** Draft | Ready | Adopted

## Problem
## Solution Idea
## Scope
- **In scope:** …
- **Out of scope:** …

## Impact
- **Harness:** new tokens / animations / AiStates / dependencies — and their sign-off
- **Specs:** affected or new categories, components, stories (with paths)
- **Public API:** new or changed parameters, enums, events, services
- **Code:** touch points and risks under `code/`

## Decisions
- {date}: {decision, with a short reason}

## Open Points
- … (empty once State is Ready)
```

#### State values

| State | Meaning |
|---|---|
| **`Draft`** | The idea is under discussion; open points remain. |
| **`Ready`** | Definition of Ready fully met, Product Owner has confirmed. Ready to be carried into specs. |
| **`Adopted`** | The idea has been carried into specs following [`requirements.md`](requirements.md). The document links the resulting spec paths and is no longer changed. |

Check: every file under `ideas/` matches `I{n} {Name}.md`, numbers are unique
and never reused, and an `Adopted` idea's document links at least one spec
path under `specs/`.
