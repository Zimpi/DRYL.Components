# RC Phase 1 — CONVENTIONS.md + Inputs API Audit — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce the binding `CONVENTIONS.md` that the whole 1.0 API freeze audits against, then run the first category audit (Inputs) to a concrete findings document — without yet changing component code.

**Architecture:** Two deliverables. (1) `CONVENTIONS.md` at the repo root, codifying the public-API naming rules the library already mostly follows (events, booleans, enums, AI, slots, pass-through), linked from `CLAUDE.md` and `CONTRIBUTING.md`. (2) An audit of the 23 Inputs components against those rules, captured as `docs/superpowers/audits/2026-06-10-inputs-api-audit.md`. Fixes are deliberately out of scope for this plan — they land in a follow-on plan informed by the findings, so we never pre-write rename code we haven't verified.

**Tech Stack:** Markdown docs, ripgrep/grep for the audit, git. No component code changes, no build/test runs required (nothing compiles differently).

**Scope note:** This is the first sub-project of the spec `docs/superpowers/specs/2026-06-10-v1-rc-release-design.md` (Phase 1 + the discovery half of Phase 2 Batch 1). Subsequent batches (Inputs fixes, then Data/Layout/Surfaces/Feedback/Navigation/Actions/AI) and Phases 3–4 get their own plans.

---

### Task 1: Verify the current event-naming pattern before codifying it

**Files:**
- None created/modified (read-only verification that the rules we will write match reality).

- [ ] **Step 1: List every EventCallback parameter name**

Run:
```bash
cd DRYL.Components
grep -rhoE "EventCallback(<[^>]+>)? [A-Za-z]+" --include="*.razor" --include="*.cs" . \
  | grep -oE "[A-Za-z]+$" | sort | uniq -c | sort -rn
```
Expected: a list where **two-way-binding** change events end in `Changed`
(`ValueChanged`, `OpenChanged`, `ExpandedChanged`, `CollapsedChanged`,
`ActiveChanged`, `SelectedValueChanged`, `PageSizeChanged`, …) and **action /
notification** events start with `On` (`OnClick`, `OnClose`, `OnSend`,
`OnRetry`, `OnRemove`, `OnDismiss`, `OnClear`, `OnRowClick`, …).

- [ ] **Step 2: Confirm the known deviations exist (these become future fix tickets, NOT this plan)**

Run:
```bash
cd DRYL.Components
grep -rln "IsOpenChanged" --include="*.razor" --include="*.cs" .   # expect: Components/Layout/DrylExpansion.razor
grep -rln "OnPageChanged\|OnPageSizeChanged\|PageSizeChanged" --include="*.razor" --include="*.cs" .  # expect: Data/DrylPagination.razor, Data/DrylTable.razor
```
Expected: `DrylExpansion` uses `IsOpenChanged` (violates the no-`Is` rule for the
`@bind-Open` pattern); pagination mixes `On`-prefixed and bare change events.
These confirm the rules below are needed. Do **not** fix them here.

- [ ] **Step 3: Confirm booleans currently avoid Is/Has prefixes**

Run:
```bash
cd DRYL.Components
grep -rhoE "public bool (Is|Has)[A-Za-z]+" --include="*.razor" --include="*.cs" . | sort -u
```
Expected: only `IsOpen` (DrylExpansion) shows up, confirming the rest of the
library already follows "plain adjective" booleans — so codifying that rule
matches existing practice rather than imposing churn.

---

### Task 2: Write CONVENTIONS.md

**Files:**
- Create: `CONVENTIONS.md`

- [ ] **Step 1: Create `CONVENTIONS.md` with this exact content**

````markdown
# DRYL Public API Conventions

These rules define the **public API surface** of every DRYL component. They are
binding for the 1.0 API freeze: after `1.0.0`, changing any of these on an
existing component is a breaking change (MAJOR bump). They codify patterns the
library already follows — see `CLAUDE.md` for the design-system rules and
`COMPONENT_PATTERNS.md` for component structure.

## 1. Naming

- **Components:** PascalCase, `Dryl` prefix — `DrylButton`, `DrylDataGrid`.
- **Enums:** `<Component><Concept>` — `ButtonVariant`, `ButtonSize`,
  `BadgeKind`. Declared next to the component (nested type or sibling file).
- **CSS classes:** kebab-case, no prefix — `.btn`, `.glass-card`.

## 2. Parameters

- **Variants / sizes / kinds are always `enum`, never `string`.** The first enum
  member is the sensible default, and the parameter defaults to it:
  `[Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;`
- **Boolean parameters use a plain adjective / state word — no `Is`/`Has`
  prefix — and default to `false`:** `Disabled`, `Loading`, `Open`, `Selected`,
  `Readonly`, `Required`.
- **Required values** are non-nullable and named for the thing
  (`Src`, `Alt`, `Text`); optional values are nullable (`string?`).
- **Pass-through HTML attributes** use exactly:
  `[Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object>? AdditionalAttributes { get; set; }`

## 3. Events

- **Two-way binding** uses the `Value` / `ValueChanged` / `ValueExpression`
  triple so consumers can write `@bind-Value`. For other bindable state, the
  change event is **`<Property>Changed`** (PascalCase property + `Changed`), with
  **no `On` prefix and no `Is` prefix** — e.g. `Open`/`OpenChanged`,
  `Expanded`/`ExpandedChanged`, `Active`/`ActiveChanged`, `PageSize`/`PageSizeChanged`.
- **Action & notification events** (one-way, "something happened") use the
  **`On<Verb>`** form: `OnClick`, `OnClose`, `OnSend`, `OnRetry`, `OnRemove`,
  `OnDismiss`, `OnClear`, `OnRowClick`.
- All events are `EventCallback` or `EventCallback<T>` — never `Action`/`Func`
  on the public surface.

## 4. AI

- The opt-in AI parameter is always named **`Ai`**, type `AiState`, default
  **`AiState.None`**. Never a per-component AI enum or a differently named
  parameter. See `CLAUDE.md` §2.10.

## 5. Slots

- The default slot is **`ChildContent`** (`RenderFragment?`).
- Named slots are PascalCase `RenderFragment?` — `Header`, `Footer`, `Start`,
  `End`, `Content`.
- Slots that take an item/context are `RenderFragment<T>` with a documented
  context type.

## 6. Form integration

- Input components that bind a single value derive from `InputBase<TValue>` and
  expose `@bind-Value`. They must override `SetParametersAsync` so `Value="..."`
  works outside an `EditForm` (avoids the `ValueExpression` `InvalidOperationException`).

## 7. Lifecycle / JS interop

- Components using `IJSRuntime` must be prerender-safe: no JS before the first
  interactive render, and `IAsyncDisposable` cleanup guarded by an `_attached`
  flag so static prerender disposal does not throw.

## Known deviations (to be fixed before 1.0)

These exist today and are tracked for the API-freeze audit (board #39):

- `DrylExpansion` — `IsOpen` / `IsOpenChanged` → should be `Open` / `OpenChanged`.
- `DrylPagination` / `DrylTable` — pagination events mix `On`-prefixed and bare
  forms; normalise to the rules in §3.
````

- [ ] **Step 2: Verify the doc's claims match the code one more time**

Run:
```bash
cd DRYL.Components
grep -rhoE "EventCallback(<[^>]+>)? On[A-Za-z]+" --include="*.razor" --include="*.cs" . | sort -u | head
```
Expected: every `On`-prefixed callback is an action/notification (Click, Close,
Send, …), none is a two-way-binding change event — confirming §3 is accurate.

- [ ] **Step 3: Commit**

```bash
git add CONVENTIONS.md
git commit -m "docs: add CONVENTIONS.md (public API naming rules for 1.0 freeze)"
```

---

### Task 3: Link CONVENTIONS.md from CLAUDE.md and CONTRIBUTING.md

**Files:**
- Modify: `CLAUDE.md` (section 1, the "three files" list)
- Modify: `CONTRIBUTING.md` (the "Ground rules" section)

- [ ] **Step 1: Add CONVENTIONS.md to the design-system file list in CLAUDE.md**

In `CLAUDE.md`, find the bulleted list under section 1 that names
`dryl.css`, `DESIGN_TOKENS.md`, `COMPONENT_PATTERNS.md`. Add one line:

```markdown
- `CONVENTIONS.md` — the binding public-API naming rules (events, parameters, enums, slots) enforced for the 1.0 freeze
```

- [ ] **Step 2: Reference it from CONTRIBUTING.md ground rules**

In `CONTRIBUTING.md`, in the "Ground rules" section, add a bullet:

```markdown
- **Follow the API conventions.** Public parameter / event / enum / slot naming
  must match [`CONVENTIONS.md`](CONVENTIONS.md). These are frozen at 1.0.
```

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md CONTRIBUTING.md
git commit -m "docs: link CONVENTIONS.md from CLAUDE.md and CONTRIBUTING.md"
```

---

### Task 4: Audit the Inputs category against CONVENTIONS.md (discovery only)

**Files:**
- Create: `docs/superpowers/audits/2026-06-10-inputs-api-audit.md`

- [ ] **Step 1: Gather the Inputs public surface**

Run:
```bash
cd DRYL.Components/Components/Inputs
grep -rEn "\[Parameter\][^]]*\]?\s*public [^=;{]+" *.razor *.razor.cs 2>/dev/null
```
Expected: a list of every `[Parameter]` declaration across the 23 Inputs
components. (Some components keep parameters in a `.razor.cs`; the glob covers both.)

- [ ] **Step 2: Create the findings document with this structure, filled from Step 1**

Create `docs/superpowers/audits/2026-06-10-inputs-api-audit.md`:

```markdown
# Inputs API Audit (vs CONVENTIONS.md) — 2026-06-10

Category: Inputs (23 components). Audited against `CONVENTIONS.md`.
Status legend: ✅ conforms · ⚠️ deviation (needs fix) · 🔵 intentional (documented).

| Component | Parameter / Event | Rule (§) | Status | Proposed change |
| --- | --- | --- | --- | --- |
| DrylInputText | Value/ValueChanged | §3 | ✅ | — |
| … one row per public parameter or event that is non-trivial … | | | | |

## Deviations to fix (feeds the Inputs fix plan)
- (list each ⚠️ row as a one-line actionable item, or "none found")

## Intentional exceptions
- (list each 🔵 with a one-sentence justification)
```

Fill one row per public parameter/event that touches a rule in §2–§6 (you do not
need a row for trivially-correct pass-through attributes). For each, mark ✅/⚠️/🔵
and, for ⚠️, write the concrete proposed rename/retype.

- [ ] **Step 3: Sanity-check the Inputs event names specifically**

Run:
```bash
cd DRYL.Components/Components/Inputs
grep -rhoE "EventCallback(<[^>]+>)? [A-Za-z]+" *.razor *.razor.cs 2>/dev/null | sort -u
```
Expected: confirm each event is either a `*Changed` binding event or an
`On<Verb>` action event; record any that are neither as ⚠️ in the table.

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/audits/2026-06-10-inputs-api-audit.md
git commit -m "docs: audit Inputs API against CONVENTIONS.md (findings only)"
```

---

## Definition of done for this plan

- `CONVENTIONS.md` exists, committed, and linked from `CLAUDE.md` + `CONTRIBUTING.md`.
- The documented rules were each verified against the current code (Tasks 1–2).
- `docs/superpowers/audits/2026-06-10-inputs-api-audit.md` lists every Inputs
  deviation with a concrete proposed change (or records "none found").
- No component code changed; the build is untouched.

## Next plan

`2026-06-10-v1-rc-inputs-fixes.md` (to be written from the audit findings):
apply each ⚠️ change as a TDD micro-cycle (write/adjust bUnit test → rename →
green → update CHANGELOG `Changed` + README), one reviewable commit per fix.
