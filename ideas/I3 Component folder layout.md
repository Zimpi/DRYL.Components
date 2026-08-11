# Component folder layout

## Meta
- **State:** Draft

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

*Open — this is the decision to be made.*

The finding that makes any of this possible: **all 111 components under
`Components/` declare `@namespace DRYL.Components`.** The folder does not
determine the namespace, so moving a file is not an API break — no `using`
changes for consumers, no MAJOR under `REL-01`. This is the unusual case where
the tidy-up is nearly free at the API level; the cost is a large rename commit
and `git blame` needing `--follow`.

Three candidate directions, none adopted:

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
  under an `Internal/` folder.
- **Out of scope:** the category *list* itself (fixed in `SPEC-02`, phase B);
  any rename of a component or a public parameter; the `@namespace` declaration;
  the `ComponentCatalog` grouping in `DRYL.Website`, which is a separate
  navigation decision.

## Impact

- **Harness:** `SPEC-02`'s category table names source folders and would need
  updating to match. `CODE-01` in [`../harness/code.md`](../harness/code.md)
  goes green once `CanvasNodeView` moves — that is the only rule whose hit count
  this changes. No new token, animation, `AiState` or dependency, so no
  `IDEA-05` blocker.
- **Specs:** none exist yet, which is exactly why this is worth deciding now.
  Every `Source` block records concrete paths; deciding after phase C means
  editing the specs of every component that moves.
- **Public API:** none under direction A — `@namespace DRYL.Components` is
  declared in each component, so the folder is not part of the contract. This
  must be re-verified before any move, not assumed: a `.razor.cs` codebehind
  with a folder-derived namespace would be an exception.
- **Code:** roughly 30 files move under direction A, plus their `.razor.css` and
  `.razor.cs` companions. `git blame` needs `--follow` afterwards. No behavioural
  change, so the existing test suite is the regression net.

## Decisions

- 2026-08-11: Recorded as an idea rather than fixed during phase B. The phase B
  plan is explicit that no code moves; and the restructure design requires
  deviations found while reverse-engineering to be filed under `ideas/`, not
  silently corrected.

## Open Points

- Direction A, B or C.
- If A: whether it lands before phase C starts (cheap) or is deferred (then
  every affected spec is edited twice).
- If A: the name and boundary of the folder the provider components move to.
- Whether `CanvasNodeView` is decided separately and moved now — it is the one
  item here that keeps a rule red, and it is a single file.
- Verify, before any move, that no component's namespace is folder-derived
  rather than declared.
