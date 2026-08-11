# Component folder layout

## Meta
- **State:** Ready

## Problem

Phase B fixed the spec categories one-to-one to the folders under `code/`
(`SPEC-02`). That choice makes a component's category derivable from its path
and therefore checkable by `scripts/check-spec-coverage.mjs` — but it also means
the specs inherit whatever the folder layout gets wrong. Counting the 127
components for that table surfaced four things it gets wrong:

**1. `Components/Layout/` holds six navigation components.** `DrylNavGroup`,
`DrylNavLink`, `DrylTabs`, `DrylTab`, `DrylStepper` and `DrylStep` sit in
`Layout/` (22 components) while `Components/Navigation/` holds six. A consumer
looking for tabs looks in `Navigation/` first.

**2. `Components/Surfaces/` mixes three unrelated groups.** Surfaces proper
(`DrylCard`, `DrylPopover`, `DrylDepthGlass`, `DrylDialog`), the chat stack
(`DrylChat`, `DrylChatComposer`, `DrylMessage`, `DrylMarkdown`) and provider
infrastructure (`DrylThemeProvider`, `DrylToastProvider`, `DrylDialogProvider`,
`DrylPresence`, `DrylReconnectModal`, `DrylColorModeToggle`). The last group is
not a surface at all — it is plumbing a consumer mounts once in the layout.

**3. Dialogs live in two places.** `DrylDialog` and `DrylDialogProvider` are in
`Components/Surfaces/`; `DrylAlertDialog` and `DrylConfirmDialog` are in
`Dialogs/` alongside the dialog service. Two spec categories (`E6 Dialogs`,
`E11 Surfaces`) now describe one feature.

**4. `CanvasNodeView` is internal but not under `Internal/`.** Carried over from
the red-rule triage (`docs/2026-08-11-red-rule-triage.md`, `CODE-01`): it takes
only `internal` cascading parameters and is rendered solely by `DrylCanvas`, so
a consumer can neither place nor parameterise it — but the `CODE-01` check can
only recognise "internal" by the folder. `ChartFrame` is under
`Charts/Internal/` and is correctly exempt; `CanvasNodeView` is
indistinguishable from a forgotten public component. `CODE-01` stays at one hit
until this moves.

The cost of leaving it: 127 specs get written against this layout, and every
future reader of `specs/` learns that tabs are a layout concern.

## Solution Idea

**Direction A, taken before phase C, bounded by the fifteen fixed categories.**

Four moves, all of them between folders that a `SPEC-02` category already names
— or, in one case, a folder a category names but has never had:

| # | Move | From → To | Category effect |
|---|---|---|---|
| 1 | `DrylNavGroup`, `DrylNavLink`, `DrylTabs`, `DrylTab`, `DrylStepper`, `DrylStep` (+ `DrylStepper.razor.css`, `StepperOrientation.cs`, `StepState.cs`) | `Components/Layout/` → `Components/Navigation/` | `E9` 22 → 16, `E10` 6 → 12 |
| 2 | `DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal` (+ `.razor.css`), `DrylColorModeToggle` | `Components/Surfaces/` → `Components/Providers/` | `E11` 15 → 8, `E1` 0 → 5 |
| 3 | `DrylDialog`, `DrylDialogProvider` | `Components/Surfaces/` → `Dialogs/` | `E6` 2 → 4 |
| 4 | `CanvasNodeView.razor` | `Canvas/` → `Canvas/Internal/` | none — not one of the 127 |

The total stays at 127; only the distribution changes.

**Why `E1 Foundation` and not a sixteenth category.** The category *list* is out
of scope (`SPEC-02`, phase B), and the categories map one-to-one onto folders —
so a new `Components/Providers/` folder with no category would leave that
one-to-one claim false. Note what enforces it: `check-spec-coverage.mjs` reads
only the category *names* out of the `SPEC-02` table, never the source-folder
column. It verifies that `specs/` and the table agree and that every
`Dryl*.razor` is claimed exactly once — the path-to-category mapping itself is
`review`-enforced, not script-enforced. `E1 Foundation` is the
resolution: it already holds "the public surface that belongs to no single
component — the theming types, the DI registration, the motion primitives", which
is precisely what these five components are. It becomes the category's source
folder. The list stays at fifteen; what changes is `SPEC-02`'s claim that `E1`
carries no components.

**What direction A does *not* fix.** The chat stack (`DrylChat`,
`DrylChatComposer`, `DrylMessage`, `DrylMarkdown`) stays in `Components/Surfaces/`
— for the same reason the providers could not have their own folder: there is no
category for it, and creating one is out of scope. Problem 2 above is therefore
resolved for the provider group and left standing for the chat group. `E11` ends
at 8 components: four surfaces proper plus the four-part chat stack.

The finding that makes any of this possible: **all 111 components under
`Components/` declare `@namespace DRYL.Components`.** The folder does not
determine the namespace, so moving a file is not an API break — no `using`
changes for consumers, no MAJOR under `REL-01`. This is the unusual case where
the tidy-up is nearly free at the API level; the cost is a large rename commit
and `git blame` needing `--follow`.

Three candidate directions were weighed; **A was adopted** (see `## Decisions`):

**A — Move the code, keep categories one-to-one.** `Navigation/` gains the six
navigation components, `Surfaces/` sheds the providers into a new folder, the
dialog components consolidate, `CanvasNodeView` moves to `Canvas/Internal/`.
Specs and code stay aligned and the coverage check keeps verifying the mapping.
Costs one focused move commit, best taken *before* phase C writes 127 specs
naming the old paths in their `Source` blocks.

**B — Leave the code, re-cut only the spec categories.** No file moves, but
`SPEC-02`'s one-to-one property is lost and the coverage check can no longer
verify that a component sits in the category its path implies.

**C — Do nothing.** The layout is a naming inconvenience, not a defect; the
`ComponentCatalog` in `DRYL.Website` is what consumers actually browse.
`CanvasNodeView` would still need its own answer, since `CODE-01` stays red.

Timing matters more than the direction: every `Source` block written in phase C
names concrete paths, so a move afterwards means touching every spec it affects.

## Scope

- **In scope:** the folder location of the components named above; the
  `Source` paths that phase C will record for them; `CanvasNodeView`'s move
  under an `Internal/` folder; the `SPEC-02` table edits the moves force
  (six counts, `E1`'s source folder, `E1`'s componentless note).
- **Out of scope:** the category *list* itself (fixed in `SPEC-02`, phase B) —
  which is what rules out both a `E16 Providers` and a folder for the chat stack;
  any rename of a component or a public parameter; the `@namespace` declaration;
  the `ComponentCatalog` grouping in `DRYL.Website`, which is a separate
  navigation decision.

## Impact

- **Harness:** `SPEC-02`'s category table needs three edits — the six new counts,
  `Components/Providers/` as `E1 Foundation`'s source folder, and the paragraph
  stating that `E1` is componentless (it stays a *category that may be*
  componentless; it simply no longer is one). `CODE-01` in
  [`../harness/code.md`](../harness/code.md) goes green once `CanvasNodeView`
  moves — the only rule whose hit count this changes. No new token, animation,
  `AiState` or dependency, so no `IDEA-05` blocker.
- **Specs:** none exist yet, which is exactly why this is worth deciding now.
  Every `Source` block records concrete paths; deciding after phase C means
  editing the specs of every component that moves. `specs/E1 Foundation/` gains
  five `F{n}` files it would not otherwise have had. The per-category counts in
  the `SPEC-02` table are documentation and are maintained by hand — the script
  derives only the total (127) from the file system, by counting `Dryl*.razor`
  under `code/`; that total is unchanged by every move here.
- **Public API:** none. **Verified 2026-08-11, not assumed:** every `.razor`
  under `Components/` and `Dialogs/` declares `@namespace` explicitly, and every
  `.cs` beside them declares its own `namespace` line — 46 × `DRYL.Components`,
  9 × `DRYL.Components.Dialogs`, 3 × `DRYL.Components.Internal`. No namespace in
  either project is folder-derived, so a move is API-neutral as long as the
  moved `.cs` files keep their declaration. `_Imports.razor` lists namespaces,
  never folders, and is untouched. No `REL-01` MAJOR.
- **Code:** 18 files move, companions (`.razor.css`, owned enums) included — the
  early estimate of "roughly 30" counted companions that turned out not to exist.
  `git blame` needs `--follow` afterwards. No behavioural change, so the existing
  test suite is the regression net.

## Decisions

- 2026-08-11: Recorded as an idea rather than fixed during phase B. The phase B
  plan is explicit that no code moves; and the restructure design requires
  deviations found while reverse-engineering to be filed under `ideas/`, not
  silently corrected.
- 2026-08-11: **Direction A**, and it lands **before phase C**. Rationale: the
  move is API-neutral (verified above), so the only real cost is the rename
  commit — while deferring it means editing the `Source` block of every affected
  spec a second time after 127 are written.
- 2026-08-11: The provider group moves to `Components/Providers/` as the source
  folder of **`E1 Foundation`**, not into a sixteenth category. The category list
  is out of scope here, and `E1` already owns exactly this "belongs to no single
  component" surface. A folder without a category would not fail any script —
  the one-to-one mapping is review-enforced — but it would make `SPEC-02`'s
  wording untrue, which is the same debt this idea exists to remove. The
  Tech Lead raised the collision — direction A as originally written would have
  required opening the category list.
- 2026-08-11: The chat stack stays in `Components/Surfaces/`. Same constraint:
  no category exists for it, and creating one is out of scope. Problem 2 is
  therefore resolved only for the provider group; the chat grouping is left as
  known, accepted debt rather than silently folded into this move.
- 2026-08-11: `CanvasNodeView` **keeps `@namespace DRYL.Components.Canvas`** when
  it moves to `Canvas/Internal/`. `CODE-01` recognises "internal" by the folder,
  so the folder move alone turns it green; adopting `ChartFrame`'s
  `DRYL.Components.Internal` would add reference churn in `DrylCanvas` for no
  gain in rule compliance.
- 2026-08-11: **Implemented.** The 18 moves landed as pure renames in `b1048e5`
  (with `<Version>` 2.20.2 and the changelog release cut); `SPEC-02` and
  `specs/E1 Foundation/_Api.md` followed in `ddf1d35`. Evidence: build 0 errors,
  1004/1004 tests pass, `check-spec-coverage` reports no violations and still
  `0/127`, `check-light-sync` and `validate-light-contrast` pass,
  `check-harness-links` OK. Two `SPEC-02` sentences that overstated the script's
  enforcement were corrected in the same commit.
- 2026-08-11: `State` stays **`Ready`**, not `Adopted`, even though the code has
  moved. `IDEA-07` defines `Adopted` as "carried into specs" and requires the
  document to link at least one path under `specs/`. The component specs are
  written in phase C; this idea becomes `Adopted` then, not on implementation.

## Open Points

*None — the Product Owner confirmed direction A and both open sub-decisions on
2026-08-11.*
