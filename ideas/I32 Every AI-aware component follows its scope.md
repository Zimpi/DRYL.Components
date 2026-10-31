# Every AI-aware component follows its scope

## Meta
- **State:** Adopted
- **Specs:** `specs/E3 AI/F5 DrylAiScope.md`, `specs/E5 Data/F15 DrylStat.md`,
  `specs/E5 Data/F16 DrylTable/S9 Presentation, accessibility and AI.md`,
  `specs/E5 Data/F7 DrylCodeBlock.md`, `specs/E5 Data/F11 DrylImage.md`,
  `specs/E5 Data/F19 DrylTimelineItem.md`, `specs/E7 Feedback/F1 DrylAlert.md`,
  `specs/E7 Feedback/F6 DrylEmptyState.md`, `specs/E7 Feedback/F5 DrylProgress.md`,
  `specs/E7 Feedback/F4 DrylSkeleton.md`, `specs/E7 Feedback/F3 DrylSpinner.md`,
  `specs/E8 Inputs/F4 DrylFileUpload.md`, `specs/E10 Navigation/F3 DrylStep.md`,
  `specs/E7 Feedback/F8 DrylNotifications.md`, `specs/E6 Dialogs/F1 DrylDialog.md`,
  `specs/E10 Navigation/F4 DrylCommandPalette/_Component.md`

## Problem

`DrylAiScope` promises its descendants the scope's state: "a descendant whose `Ai`
is `AiState.None` inherits the scope's state" (`specs/E3 AI/F5 DrylAiScope.md`,
*Resolution*), and `harness/patterns.md` (*AI-aware components → Rules → Resolve
through the scope*) tells every AI-aware component to drive its markup from
`EffectiveAi` **and** `EffectiveAura`.

Most components only do half of that. A sweep on 2026-10-31 (every file under
`code/` that cascades `AiScope`, compared on `AiScope.Resolve` / `EffectiveAi`
against `AiScope.ResolveAura` / `EffectiveAura`) found:

- **Follow state and aura (as promised):** `DrylButton`, `DrylSplitButton`,
  `DrylCard`, `DrylChat`, `DrylMessage`, `DrylMarkdown`, `DrylDialog`,
  `DrylInputText`, `DrylTextarea`, `DrylSelect`, `DrylAutocomplete`, the chart
  family through `DrylChartBase`.
- **Follow the aura only — the scope's state never arrives (27):**
  - Data: `DrylStat`, `DrylTable`, `DrylCodeBlock`, `DrylImage`, `DrylTimelineItem`
  - Feedback: `DrylAlert`, `DrylEmptyState`, `DrylNotifications`, `DrylProgress`,
    `DrylSkeleton`, `DrylSpinner`
  - Inputs: `DrylChipInput`, `DrylDatePicker`, `DrylTimePicker`, `DrylFileUpload`,
    `DrylInputMask`, `DrylInputNumber`, `DrylInputOtp`, `DrylInputPassword`,
    `DrylMultiSelect`, `DrylRadioGroup`, `DrylRating`, `DrylSlider`
  - Others: `DrylExpansion`, `DrylChatComposer`, `DrylStep`, `DrylCommandPalette`
  - plus `DrylToolCall`, `DrylToolCallGroup` — but these two are **deliberate**
    (their specs: "the AI state is never inherited … a tool call's status is a
    fact about that call").
- **Takes no scope at all:** `DrylToast` — rendered by the notification host,
  outside any scope, by construction.

Each of the 27 resolves `Aura` through the scope but hands the raw `Ai` to
`AuraLifecycle.Sync` and the `Generated` tick. None of their specs says the state
is *not* inherited — they are silent, except `DrylStep`, which records it as a
limit ("automatic scope-state inheritance is not implemented").

The visible symptom: the landing page (`DRYL.Website`, `Pages/Landing.razor`,
section *One state, every surface*) wraps a `DrylTextarea` and a `DrylStat` in one
scope and drives it from a state picker. The textarea lights up, the stat tile
stays dark. The copy beside it claims the signal shows "in inputs, stats, tables,
toasts and code blocks" — true today for some inputs only; stats, tables and code
blocks ignore the scope state, and toasts cannot receive it.

## Solution Idea

Three options:

- **A — Fix the library (recommended).** The 27 components resolve the state
  through the scope like the other twelve: `EffectiveAi` feeds `AuraLifecycle.Sync`
  and the `Generated` tick. The demo then is right as written; its copy loses
  "toasts".
  *Pro:* the library keeps the promise its own scope spec and pattern rule make;
  the fix is mechanical (one expression per component, the `InputBase` family
  replicates `EffectiveAi` inline as `DrylAiAware` documents).
  *Con:* a visible behaviour change — a component that sat dark inside a scope
  starts to glow. 27 components, many without a spec (`SPEC-01`: each touched
  component needs a spec in the same commit, or at least its existing spec
  updated).
- **B — Fix the demo, document the limit.** Swap the `DrylStat` in the demo for a
  component that follows (e.g. a `DrylCard` around the stat), correct the copy,
  and write the aura-only behaviour into the 27 specs as a recorded limit.
  *Pro:* no library change. *Con:* turns a gap into the contract; contradicts the
  scope spec and the pattern rule, which would then have to name 27 exceptions.
  The scope's marketing claim ("one state, every surface") stops being true.
- **C — A then B's copy fix.** A, plus the landing copy corrected to what is true
  after A ("toasts" goes).

Recommendation: **C** — A in the library, the copy fix in the website.

### Deliberate exceptions under A

Not every aura-only component should start inheriting:

- `DrylToolCall`, `DrylToolCallGroup` — stay as specified.
- `DrylCommandPalette` — its `Ai` is not decoration: `Ai != None` **mounts the
  AI result panel** with the host's `ResultContent`. Inheriting would open an
  empty result panel in any palette inside an active scope. Proposed: stays
  explicit-only, and its spec says so.
- `DrylNotifications` — the host sits at layout root; inheriting would light the
  whole toast stack when a page-wide scope is active. Open point.
- `DrylStep` — today a recorded limit; under A it would inherit like the others,
  and the limit goes.

## Scope

- **In scope:** the 27 (minus the agreed exceptions) in `DRYL.Components`; specs
  of the touched components; the `DrylAiScope` spec naming the exceptions; the
  landing demo copy in `DRYL.Website`; tests per component (inherits / explicit
  wins / `Generated` fires once on an inherited transition).
- **Out of scope:**
  - `DrylToast` — receiving a scope would need the toast to carry a key from the
    call site; a different idea.
  - Any new visual, token or state.
  - The aura *variant* resolution — already correct everywhere.

## Impact

- **Harness:** no new token, keyframe, `AiState` or dependency. Brings code in
  line with `harness/patterns.md`'s existing rule; no rule text changes, except
  possibly naming the exceptions there.
- **Specs:** `DrylStat`, `DrylTable`, `DrylCodeBlock`, `DrylImage`,
  `DrylTimelineItem`, `DrylAlert`, `DrylEmptyState`, `DrylNotifications`,
  `DrylProgress`, `DrylSkeleton`, `DrylSpinner`, `DrylFileUpload`, `DrylStep`,
  `DrylCommandPalette` gain an inheritance AC (or an explicit exception);
  `specs/E3 AI/F5 DrylAiScope.md` names the components that do not inherit.
  **Thirteen touched components have no spec** (`DrylChipInput`, `DrylDatePicker`,
  `DrylTimePicker`, `DrylInputMask`, `DrylInputNumber`, `DrylInputOtp`,
  `DrylInputPassword`, `DrylMultiSelect`, `DrylRadioGroup`, `DrylRating`,
  `DrylSlider`, `DrylExpansion`, `DrylChatComposer`) — `SPEC-01` forbids changing
  their behaviour without reading/writing a spec first. That is the real cost of A.
- **Public API:** none. No parameter, enum or type changes.
- **Behaviour:** visible change — components inside an active scope now show it.
  Core MINOR under *Changed* (`REL-01`); the scope spec already promised it, so it
  reads as a fix against the contract, not a new feature.
- **Code:** one line per component (`_aura.Sync(Ai, …)` → `EffectiveAi`, the
  `Generated` tick likewise); the `InputBase` family adds the inline
  `EffectiveAi`. Risk: a component that already compares `Ai` elsewhere (e.g. to
  gate content) must be read individually — the palette is the known case.

## Decisions

The Product Owner delegated every open point to the Tech Lead on 2026-10-31
("Beantworte die ganzen Fragen bitte selbst … mach das alles fertig"); the
decisions below were taken under that delegation.

- 2026-10-31: **Option C** — the library is fixed, and the landing copy loses
  "toasts". Turning a broken promise into the contract (B) would have needed
  27 exceptions in a rule that exists to have none.
- 2026-10-31: **Split.** The twelve components with a spec inherit now; the
  thirteen without one stay on the list in `AiScopeInheritanceTests` and in the
  `DrylAiScope` spec as debt, each fixed together with its first spec
  (`SPEC-01`). Writing thirteen specs as a side effect of a one-line fix would
  have produced specs nobody read the components for.
- 2026-10-31: **Exceptions, each recorded in its spec:** `DrylToolCall`,
  `DrylToolCallGroup` (unchanged), `DrylCommandPalette` (its `Ai` mounts
  content), `DrylNotifications` (layout root; a page-wide scope must not light
  the stack). The sweep also surfaced `DrylDialog`, whose state comes from its
  instance and which renders at layout root — recorded the same way.
- 2026-10-31: **A guard instead of a list in a document.** `AiScopeInheritanceTests`
  fails when a component resolves the aura variant through the scope but not the
  state and is not named with a reason — and when a named one has been fixed and
  is still on the list.

## Open Points

*(none)*
