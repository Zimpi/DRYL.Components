# Spec Rules

How specs under `specs/` are structured and written. DRYL is spec-driven:
`specs/` and `code/` are one artifact, kept in sync in both directions.

Idea intake happens before any spec exists — see `ideas.md`.

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. The `SPEC` block is currently contiguous — `SPEC-01` …
`SPEC-09`, no gaps — because it was written in one pass; later rules are
appended with the next unused number in sequence.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

Related rule files: [`code.md`](code.md), [`design.md`](design.md),
[`uiux.md`](uiux.md), [`ai.md`](ai.md), [`releasing.md`](releasing.md).
Reference documents without IDs: [`tokens.md`](tokens.md),
[`patterns.md`](patterns.md), [`conventions.md`](conventions.md),
[`theming.md`](theming.md).

---

### SPEC-01 — Specs and code are one artifact

Status: **binding** | Enforced: **review**

Every change to a component's behaviour or public API updates its spec in the
same commit. A spec that no longer matches its code goes back to
`State: Modified` — never leave it on `Implemented`. Do not write code for a
component whose spec you have not read.

Check: the component's spec file was touched in the same commit as its code

### SPEC-02 — Folder and file structure

Status: **binding** | Enforced: **script**

Specs live under `specs/` in a three-level hierarchy:

```
specs/
├── E{n} {Category}/
│   ├── _Api.md              shared enums, parameter contracts, services
│   ├── _Interop.md          JS interop surface, DI services, cleanup duties
│   ├── F{n} {DrylComponent}.md
│   └── F{n} {DrylComponent}/        only when one file is no longer enough
│       ├── _Component.md            the component's Meta block: State + Source
│       └── S{n} {Aspect}.md
```

- **E** = component **category**. The list is fixed below — do not invent one,
  and do not file a component outside it. Categories follow the folders under
  `code/` one-to-one, so a component's category is derivable from its path.
  Note what enforces which half: the script checks that `specs/` and the table
  agree on the category *list*, and that every component is claimed by exactly
  one spec. That a component sits in the category its path implies is
  **review-enforced** — the script reads the category names out of the table,
  never the source-folder column. Because the mapping is a convention rather
  than a check, a folder that misplaces a component quietly misplaces its spec
  too; that is what made the moves of 2026-08-11 worth doing before the specs
  were written.

| E | Category | Source folder | Components |
|---|---|---|---:|
| `E1` | Foundation | `code/DRYL.Components/Components/Providers/` | 5 |
| `E2` | Actions | `code/DRYL.Components/Components/Actions/` | 3 |
| `E3` | AI | `code/DRYL.Components/Components/AI/` | 8 |
| `E4` | Charts | `code/DRYL.Components/Components/Data/Charts/` | 4 |
| `E5` | Data | `code/DRYL.Components/Components/Data/` | 21 |
| `E6` | Dialogs | `code/DRYL.Components/Dialogs/` | 4 |
| `E7` | Feedback | `code/DRYL.Components/Components/Feedback/` | 8 |
| `E8` | Inputs | `code/DRYL.Components/Components/Inputs/` | 23 |
| `E9` | Layout | `code/DRYL.Components/Components/Layout/` | 17 |
| `E10` | Navigation | `code/DRYL.Components/Components/Navigation/` | 12 |
| `E11` | Surfaces | `code/DRYL.Components/Components/Surfaces/` | 8 |
| `E12` | Agent Runtime | `code/DRYL.Components.Agents/Agents/`, `/Display/` | 5 |
| `E13` | Agent Tools | `code/DRYL.Components.Agents/Tools/` | 3 |
| `E14` | Agent Canvas | `code/DRYL.Components.Agents/Canvas/` | 2 |
| `E15` | Agent Inputs | `code/DRYL.Components.Agents/Field/`, `/CommandPalette/`, `/Voice/`, `/Generation/` | 5 |
| | | **Total** | **128** |

**A category may be componentless** — it then carries `_Api.md` and
`_Interop.md` and nothing else. No category is currently in that position, but
the structure allows it, because a category's companion files can be worth
having on their own.

`E1 Foundation` is the case that shows why. Its subject is not a family of
widgets but the library's own footing: the public surface that belongs to no
single component — the theming types, the DI registration, `AiState`/`AiAura`,
the motion primitives and the token surface of `dryl.css`. That surface is bound
by the 1.0 freeze and would otherwise have no place to be documented, and it
lives in the category's companion files rather than in any `F{n}`. Since
2026-08-11 the category *also* owns `Components/Providers/`: the five components
a consumer mounts once in the layout rather than places on a page
(`DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal`,
`DrylColorModeToggle`), which are footing in the same sense. The reasoning is in
`ideas/I3 Component folder layout.md`.

`E12`–`E15` bundle the agents package's eight folders by theme. Four of those
folders hold exactly one component; a category apiece, each with two companion
files reading "none", would be ceremony without return.

The component counts are a statement of fact at the time of writing, not a
budget — a new component raises its category's count and the total. Only the
**total** is re-derived rather than trusted: `scripts/check-spec-coverage.mjs`
counts `Dryl*.razor` under `code/` and reports `x/128`. The per-category counts
are maintained by hand and are documentation, so a move between categories
means editing this table in the same commit.
- **F** = **one component, one file**. A `Dryl*.razor` maps to exactly one
  spec file; that one-to-one mapping is what makes the sync checkable
  (`SPEC-03`).
- **S** = an aspect of a single component, used **only** when the component is
  too big for one file. Then `F{n} {DrylComponent}.md` becomes the folder
  `F{n} {DrylComponent}/`, which holds one `_Component.md` plus the `S{n}`
  story files. The candidates are the library's three largest components —
  `DrylTable` (`code/DRYL.Components/Components/Data/DrylTable.razor`),
  `DrylCommandPalette`
  (`code/DRYL.Components/Components/Navigation/DrylCommandPalette.razor`) and
  `DrylCanvas` (`code/DRYL.Components/Components/AI/DrylCanvas.razor`);
  nothing else is split without a reason stated in the PR.

Numbering starts at 1 per level (`F1`, `F2`, … per category; `S1`, `S2`, … per
component) and stays stable: new entries are appended at the end, never
inserted in between. The E/F/S numbering follows from the file path and is
**not** duplicated in the file's H1.

The template this convention was adapted from carried a third companion file,
`_Menüstruktur.md`, for menu placement. It is deliberately dropped: DRYL's
navigation is the `ComponentCatalog` in `DRYL.Website`, whose maintenance
[`releasing.md`](releasing.md) already requires under `REL-04`. A second,
hand-written navigation list would only drift from it.

Check: `node scripts/check-spec-coverage.mjs` — every directory under `specs/`
matches `E{n} {Category}` and appears in the table above, every category in the
table exists under `specs/`, every spec file matches `F{n} {DrylComponent}.md`
or `F{n} {DrylComponent}/S{n} {Aspect}.md`, every split folder carries exactly
one `_Component.md`, and each category carries `_Api.md` and `_Interop.md`.

### SPEC-03 — Every spec names its State and its Source

Status: **binding** | Enforced: **script**

Every **component spec** carries a `## Meta` block directly after its H1, with
exactly two mandatory fields:

```markdown
## Meta
- **State:** Modified | Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylPopover.razor
              code/DRYL.Components/Components/Surfaces/DrylPopover.razor.css
              code/DRYL.Components/Components/Surfaces/PopoverPlacement.cs
```

`Source` lists the code files the spec describes. Without it, "the spec
mirrors the code" is an assertion. With it, it is an invariant that is
checkable in **two directions**:

- Every path named in a `Source` block exists — no spec describes deleted
  code.
- Every `Dryl*.razor` under `code/` appears in the `Source` of **exactly one
  component spec** — that is, of one `F{n} {DrylComponent}.md` or one
  `F{n} {DrylComponent}/_Component.md`. No component without a spec, none
  captured twice.

The second direction is also the progress meter for phase C: it answers
"x of 128 components covered" directly — `scripts/check-spec-coverage.mjs`
prints exactly that line and exits non-zero until it reads `128/128`.

#### `Source` format

Written so a script can parse it without guessing:

- **One path per line.** The first path sits on the `- **Source:**` line
  itself; each further path is a continuation line, indented with whitespace
  and carrying nothing but the path — no bullet, no comma, no backticks.
- **Repo-root-relative, forward slashes**, no leading `./` and no leading `/`
  (`code/DRYL.Components/Components/Surfaces/DrylPopover.razor`).
- **Must be listed:** the component's `.razor` file, and its `.razor.cs`
  codebehind and `.razor.css` isolated stylesheet where they exist.
- **May be listed:** other files the spec describes and that no other spec
  claims — an enum or options type owned by the component (e.g.
  `PopoverPlacement.cs`), a service or reference type it is the spec for
  (e.g. `code/DRYL.Components/Dialogs/DrylDialogService.cs`).
- **Never listed:** `dryl.css`, `_Imports.razor` and anything else shared
  across components — a shared file claimed by one spec would falsely read as
  covered. Shared surface belongs in `_Api.md` or `_Interop.md`.

The order is `.razor` first, then the rest; the check is order-independent.

#### The component spec format

```markdown
# DrylPopover

## Meta
- **State:** Modified | Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylPopover.razor
              code/DRYL.Components/Components/Surfaces/DrylPopover.razor.css

## User Story
As a Blazor developer, I want …, so that ….

## Description
What the component does, what it is for, how it is used. No implementation.

## Public API
Parameters, enums, `EventCallback`s, `RenderFragment`s — the outward contract.

## Acceptance Criteria
- …
```

The role in the `## User Story` is the **consuming Blazor developer** — the
person who installs the NuGet package and places the component on a page — not
an end user and not a maintainer.

For large specs, acceptance criteria may be grouped into thematic `###`
subsections (e.g. "Focus handling", "Placement").

#### Split components

When a component is split into `F{n} {DrylComponent}/` (`SPEC-02`), `Source`
stays at the **component** level, never at the story level:

- `F{n} {DrylComponent}/_Component.md` carries the `## Meta` block with
  `Source` **and** the component's rolled-up `State`. It is the component
  spec for that component — the one place its files are claimed.
- Each `S{n} {Aspect}.md` inside carries a `## Meta` block with `State` only
  and **no `Source`**. Its state is per-aspect; `_Component.md` is
  `Implemented` only when every `S{n}` beside it is.

This keeps "exactly one spec claims each `Dryl*.razor`" true for split and
unsplit components alike.

#### The underscore-prefixed companion files

`_Api.md`, `_Interop.md` and `_Component.md` are **not** component specs. A
leading underscore marks a file that the coverage check does not treat as
claiming a component — except `_Component.md`, which is the component spec of
its own folder.

| File | Scope | Purpose | Meta block |
|---|---|---|---|
| `_Api.md` | category | Shared enums, parameter contracts and services of the category — the data contract of the library, and what the 1.0 freeze binds. Minimum structure: an H1 naming the category, then one `##` section per shared type, each listing its members with the exact spelling used in code. | **No** `Meta` block: no `State`, no `Source`. It is a reference for the specs around it, not a unit of implementation. |
| `_Interop.md` | category | The JS interop surface the category uses (`dryl.js` entry points), the DI services it registers, and the cleanup duties each imposes (`CODE-05` in [`code.md`](code.md)). Minimum structure: an H1, then `## Interop`, `## Services` and `## Cleanup` sections; empty sections are written as "none". | **No** `Meta` block: no `State`, no `Source`. |
| `_Component.md` | one component | The `Meta` block of a split component plus its `## Description` and `## Public API`; the acceptance criteria live in the `S{n}` files. | **Yes** — `State` **and** `Source`, exactly as an `F{n} {DrylComponent}.md`. |

Check: every component spec — every `F{n} {DrylComponent}.md` and every
`F{n} {DrylComponent}/_Component.md` — has an H1 followed by a `## Meta` block
carrying both `State` and `Source`; every `S{n}` file carries `State` and no
`Source`; `_Api.md` and `_Interop.md` carry neither; every `Source` path
exists and is repo-root-relative; every `Dryl*.razor` under `code/` appears in
exactly one `Source` block — `node scripts/check-spec-coverage.mjs`.

### SPEC-04 — Keeping `State` honest

Status: **binding** | Enforced: **review**

`State` makes visible, at a glance, which specs currently match the code
(`Implemented`) and which do not (`Modified` — the spec was changed, or was
never implemented).

| State | Meaning |
|---|---|
| **`Modified`** | The spec was newly written, or changed in substance since the last implementation; the code does **not** (or no longer) reflect it. |
| **`Implemented`** | Spec and code are in sync — every acceptance criterion is implemented. |

Maintenance rules:

- **Every change in substance** to an `Implemented` spec sets it back to
  `Modified` — including tightening a single acceptance criterion or editing
  the `## Description`.
- **As soon as the implementation matches the spec**, the state is set to
  `Implemented` — in the same session in which the code was reconciled with
  the spec.
- **Code-only changes without a spec change** (bug fixes, refactoring,
  performance work) do **not** change the state.
- **Spec and code changed together in one session**: go straight to
  `Implemented`, without writing `Modified` as an intermediate state.
- `State` is the source of truth about the sync status and is checked
  explicitly on every spec edit. When in doubt, set `Modified` — drift is the
  main enemy.

Check: the reviewer confirms the `State` value against the diff — a spec whose
body changed in substance and whose state is still `Implemented` blocks the
merge.

### SPEC-05 — Cross-cutting requirements per component

Status: **binding** | Enforced: **review**

Some requirements repeat across every component. They are **not an optional
extra**; they are part of the minimum delivery for each one, and this list is
walked whenever a component spec is written. Every component spec evidences:

- **Both color modes** verified (`DESIGN-02` in [`design.md`](design.md)).
- **Enter and exit animation** present, or the exception explicitly justified
  (`DESIGN-11`, `DESIGN-12`).
- **Keyboard operation and a11y behaviour** described (`UX-01`, `UX-05` in
  [`uiux.md`](uiux.md)).
- **AI-mode decision made explicitly** — a "no" is written down with its
  reason, exactly like a "yes" (`AI-05` in [`ai.md`](ai.md)).
- **A demo page in `DRYL.Website`** (`CODE-20` in [`code.md`](code.md)) — demos live in the website, not in this repository.
- **An entry in the `ComponentCatalog`** (`REL-04` in
  [`releasing.md`](releasing.md)).

The template this list replaces made the same point about outgoing-email
logging, and the reason carries over unchanged: **the mechanism existing
technically does not satisfy the requirement.** In that project the backend
endpoints and workflows were long in place while the surface a user could
actually reach was still missing, because "technically already there, visible
not yet" was allowed to pass as done. The same split here would be an
`AiState` wired up with no aura on screen, or a component with no catalog
entry: shipped in the assembly, absent from the library.

Check: the reviewer walks all six points against the spec. Five of them —
both color modes, keyboard and a11y behaviour, the AI-mode decision, the
sample page, the `ComponentCatalog` entry — must be **evidenced in the spec
text**; a spec missing any of them is incomplete and blocks the merge. There
is no written-exception route for them: `DESIGN-02` and `REL-04` are binding
without an exception clause, and an explicit, reasoned "this component gets no
`Ai` parameter" **is** compliance with `AI-05`, not a waiver of it. Only the
enter/exit animation point may instead carry a written exception, and only on
the terms `DESIGN-11` already sets: the rare component with genuinely nothing
to animate, said so explicitly.

### SPEC-06 — Acceptance criteria follow INVEST

Status: **binding** | Enforced: **review**

Acceptance criteria satisfy the INVEST principles:

- **I**ndependent — worded as self-contained as possible; references to other
  specs only when materially unavoidable (then as "see `S{n}`").
- **N**egotiable — criteria describe behaviour, not implementation.
- **V**aluable — every criterion delivers value to the consuming developer or
  the end user of their app.
- **E**stimable — concrete enough that the effort can be estimated.
- **S**mall — **atomic**: one criterion states exactly one checkable fact.
  Compound statements are split.
- **T**estable — every criterion has an unambiguous pass/fail.

```markdown
- ✅ "`Variant` defaults to `ButtonVariant.Primary`."
- ✅ "`Variant` accepts exactly the four values of `ButtonVariant`."
- ❌ "Variant: enum, four values, defaults to Primary." (not atomic)
- ❌ "All inputs are validated." (not testable — what is "all"?)
```

Check: the reviewer reads each criterion against the six letters; a compound
or untestable criterion blocks the merge.

### SPEC-07 — Behaviour, not appearance: name the token, never the value

Status: **binding** | Enforced: **review**

The template this convention was adapted from delimited **UI against
backend** — business logic belongs to the backend, the UI only displays its
result. A component library has no backend, so that delimitation is replaced
by **behaviour against appearance**: acceptance criteria describe observable
behaviour, and where appearance is part of the behaviour they name the design
token, never the literal value. Literal values live in `dryl.css` and are
documented in [`tokens.md`](tokens.md); repeating one in a spec creates a
second source of truth that silently goes stale (`DESIGN-01`).

```markdown
- ✅ "The border uses `--line-strong` on hover."
- ❌ "The border turns 1px #2a2a35 on hover."
```

Verb conventions:

- "is visible / is disabled / is focused" — for component states.
- "the `OnClose` callback fires" — for `EventCallback`s.
- "the aura is removed from the surface" — for AI state transitions.
- "the component renders …" — for markup and slot output.
- "matches the format `{…}`" — for string, URL or file formats.

Parameter names, enum values, callback names and CSS custom property names
stand in backticks and in the exact spelling used in the category's `_Api.md`
(e.g. `Variant`, `ButtonVariant.Primary`, `AiState.Streaming`, `OnClose`,
`DrylTooltip`, `--line-strong`).

Check: no acceptance criterion contains a literal color, length, duration or
easing value; every API name appears in backticks and matches its spelling in
`_Api.md` (reviewer check).

### SPEC-08 — Specs are written in English

Status: **binding** | Enforced: **review**

Specs are written in English, without exception, and regardless of the
language of the conversation that produced them. The reason is the one behind
`REL-02` in [`releasing.md`](releasing.md): specs describe a public library
and are read by people who do not speak German. This covers spec bodies,
acceptance criteria, `_Api.md` and `_Interop.md`.

Check: the reviewer confirms the spec contains no non-English prose.

### SPEC-09 — Cite selectors, symbols and paths — never line numbers

Status: **binding** | Enforced: **review**

Evidence and references in a spec — or in any harness rule document — cite
CSS selectors, symbol names, type names and file paths. They never cite line
numbers. A line number is silently false after the next edit above it: nothing
fails, nothing warns, and the citation now points at unrelated code while
still reading as precise. Two attempts at building this harness failed exactly
this way, on rule documents that cited line numbers into a source file.

```markdown
- ✅ "the `.ai-aura-comet` rule and the `ai-comet-spin` keyframes in `dryl.css`"
- ✅ "`DrylPopover.Close` in `code/DRYL.Components/Components/Surfaces/DrylPopover.razor`"
- ❌ "dryl.css:4128"
- ❌ "see DrylPopover.razor lines 148–174"
```

A range that genuinely needs to be pointed at is quoted instead: paste the
selector or the signature, so the reference carries its own proof of what it
meant.

Check: `rg -n ':[0-9]+|line[s]? [0-9]+' harness/ specs/` returns no citation
of a source location by line number (reviewer confirms remaining hits are not
line-number citations).
