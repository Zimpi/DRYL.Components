# AiState.Proposed — implementation plan and progress

Scope: `ideas/I20 An AiState for work awaiting a human.md` (Adopted), as
specified in `specs/E1 Foundation/_Api.md` (`AiState`),
`specs/E3 AI/F7 DrylAuraElements.md`, `specs/E3 AI/_Interop.md` (the accept
wave), `specs/E3 AI/F4 DrylAiIndicator.md` and the `Proposed` criterion in every
component spec with an AI section. Branch: `feat/ai-proposed`. The Agents
package, the calendar's stage three and any library-owned accept/dismiss API are
outside this delivery.

Release: `DRYL.Components` 3.6.0 → **3.7.0** (new enum member, MINOR —
`REL-01`). The bump happens in T1, the first commit that touches library code;
every later task adds its entry to the same unpublished `[3.7.0]` block. The
Agents package is not touched: its AI-aware surfaces that compose
`AiAuraCss`/`DrylAuraElements` pick the state up from the core package.

## Progress

| Task | State | Commit / evidence |
|---|---|---|
| T0 — idea and spec | Complete | `357c191` — I20 adopted, specs written, PO-confirmed 2026-10-16. |
| T1 — the core: enum, lifecycle, aura, CSS, JS, rules | Complete | Gate passes (1657 tests, 26 new in `AiProposedTests`; JS 165, 9 new in `proposal.test.mjs`; browser `MotionTests` 12, now with `Proposed`). Browser pass on `DRYL.BrowserHost`, both modes: resting edge identical for Comet and Aurora, no running animation under `Proposed`, wave from the click (Comet field) and from the centre (Aurora field, no stamp), none under reduced motion. Found and fixed in the browser: the hidden comet kept spinning (now paused), and a paused drift froze Aurora wherever it had been (now a fixed offset). |
| T2 — `DrylAiIndicator` | Complete | Gate passes (1660 tests, 3 new). Spec `Implemented`. |
| T3 — the three components that ignore `Proposed` | Complete | Gate passes (1663 tests, 3 new: Proposed renders the same markup as None). Specs `Implemented`. |
| T4 — hosts that draw their own aura | Open | |
| T5 — sweep over every shared host, specs to `Implemented` | Open | |
| T6 — website demo | Open | |
| T7 — review, full verification, close the loop | Open | |

One verified commit per task, main agent; T7's review goes to an independent
agent.

The per-task check, **the gate**:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release --no-build
node --test tests/js/*.test.mjs
node scripts/check-motion-tokens.mjs
node scripts/check-harness-links.mjs
git diff --check
```

## Design decisions taken while planning

- **The wave is aimed at input time, not at render time.** `dryl.js` listens for
  `pointerdown` and `keydown` on the document (capture, passive). A pointer-down
  inside a surface carrying `ai-proposed` writes the start point onto that
  surface as `--ai-wave-x` / `--ai-wave-y`; any later pointer-down or key press
  clears the previous stamp first, and an `Enter`/`Space` key press with focus
  inside a proposed surface stamps the focused element's centre. When the host
  then moves to `Generated`, the wave layer inherits the stamp and starts there
  from its first frame — no observer, no interop call, no round trip. Without a
  stamp the CSS falls back to the centre. The circle is sized in container units
  so it covers the farthest corner from any start point without JS measuring
  it. This replaces the observer described in the spec's first draft;
  `specs/E3 AI/_Interop.md` is corrected in T1. The observable criteria in
  `F7 DrylAuraElements.md` are unchanged.
- **`AuraLifecycle` records where `Generated` came from**: a new read-only
  `FromProposed`, true while the render state is `Generated` and it was entered
  directly from `Proposed`. `AiAuraCss.Append` turns it into
  `ai-from-proposed`; `DrylAuraElements` renders the wave layer from it.
- **Ignoring is done before the lifecycle**: the three ignoring components map
  `Proposed` to `None` before `Sync`, so the lifecycle never sees it.

## Tasks

**T1 — the core.**
`code/DRYL.Components/AiState.cs` (`Proposed`, appended);
`code/DRYL.Components/Ai/AuraLifecycle.cs` (`FromProposed`; `Proposed` is live
and never retires); `code/DRYL.Components/Ai/AiAuraCss.cs` (`ai-proposed`,
`ai-from-proposed`); `code/DRYL.Components/Components/AI/DrylAuraElements.razor`
(the wave layer, the visually hidden description);
`code/DRYL.Components/wwwroot/dryl.css` (the resting look on `.ai-aura.ai-proposed`,
`.ai-aura-wave` with `ai-proposed-wave`, the `ai-proposed-flare` keyframes, the
`ai-from-proposed` rules — no lift, frozen edge, wash offset by `--delay-long` —
and reduced motion); `code/DRYL.Components/wwwroot/js/dryl.js` (the stamp);
`harness/ai.md` (`AI-01` six members, `AI-06` resting exception, `AI-07` check
for the hand-over), `harness/uiux.md` (`UX-04` resting case),
`harness/tokens.md` (the two keyframes); `specs/E3 AI/_Interop.md` (stamp
mechanism); `<Version>` 3.7.0 and the `[3.7.0]` changelog block.
Tests: `tests/DRYL.Components.Tests/AuraLifecycleTests.cs` (new — `Proposed`
rests, `FromProposed` true only for a direct hand-over, cleared on the next
state), `AiAuraClassesTests.cs` (new — the classes), `DrylAuraElementsTests.cs`
(new — wave layer and description rendered exactly when specified);
`tests/js/` (new test for the stamp: inside, outside, key press clears,
keyboard stamps focus centre). Verification: the gate; both colour modes in
the browser on a host page with a card going None → Thinking → Proposed →
Generated and Proposed → None.

**T2 — `DrylAiIndicator`.** `DrylAiIndicator.razor` ("Proposed", `is-proposed`);
`dryl.css` (`.ai-indicator.is-proposed` stops pulse and shimmer). Tests extend
the indicator tests. Spec → `Implemented`. Verification: the gate.

**T3 — the components that ignore `Proposed`.** `DrylSpinner.razor`,
`DrylSkeleton.razor`, `DrylProgress.razor`: map `Proposed` to `None` before the
lifecycle. Tests: one per component (no aura, no description). Specs →
`Implemented`. Verification: the gate.

**T4 — hosts that draw their own aura.** `DrylCard` and `DrylMarkdown` move onto
`AiAuraCss.Append` + `DrylAuraElements` (their markup is the shared one written
out by hand). `DrylImage` keeps its own state classes and gains `ai-proposed`,
the wave and the description; its own live status label stays silent under
`Proposed`. `DrylCommandPalette`'s second, hand-written ring (the resolving
state) is checked against `Proposed`. `DrylNotifications` rows and `DrylTabs`
gain `ai-proposed` in their class switches (no wave — no lifecycle there).
Tests per component. Specs of those with a spec → `Implemented`. Verification:
the gate; the browser on Card, Image and a notification row, both modes.

**T5 — sweep.** A theory test over every component that composes the shared
aura, rendered with `Ai="AiState.Proposed"`: the host carries `ai-proposed` and
the description is present. Then every component spec touched in T0 whose code
now matches goes to `Implemented`. Verification: the gate; coverage unchanged
(`65/131`).

**T6 — website demo.** `DRYL.Website` branch `feat/ai-proposed`: the AI states
demo gains `Proposed` with accept and dismiss, and the lifecycle demo shows the
hand-over. `dotnet test DRYL.Website.slnx`. Verification: the browser, both
modes.

**T7 — review and close.** Independent review agent over the branch diff. Full
evidence run (`CLAUDE.md` stage 5), both modes by eye. Close the loop
(`harness/releasing.md`): specs `Implemented`, changelog complete, version
3.7.0.
