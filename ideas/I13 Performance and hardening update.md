# Performance and hardening update

## Meta
- **State:** Adopted

## Problem

The maintainer wants DRYL's JavaScript and CSS checked against current browser
standards and established performance/hardening practices before an update.
The [2026-09-15 audit](../docs/2026-09-15-performance-hardening-audit.md) finds a
modern baseline but concrete lifecycle, motion, contrast and verification gaps.
The current 1,116 passing tests cannot establish browser rendering or JS cleanup.

**Target role:** the consuming Blazor developer who needs responsive, accessible
components and reliable upgrades; the maintainer who needs evidence before release.

## Solution Idea

Deliver the bounded H01–H06 package described below, including the shared exit
and focus cleanup that its motion fixes depend on. Give each repair a failing
reproduction and a regression test. The Product Owner confirmed this scope on
2026-09-15; the adopted contracts are linked below.

Asset delivery, rendering profiles, mobile viewport work and optional modern CSS
remain follow-up candidates. They are not prerequisites for this first package.

### Alternatives considered

1. **Hardening and evidence first (recommended):** fix demonstrated defects, then
   optimize measured bottlenecks. Lowest migration risk, useful verifiable outcomes.
2. **Modernization first:** introduce cascade layers, anchor positioning and broad
   module/style restructuring immediately. Potential maintenance benefit, but wider
   cascade/browser risk before a regression net exists.
3. **Measurement only:** establish benchmarks and CI before any fixes. Useful for
   prioritization, but delays already demonstrated cleanup/accessibility repairs.

New syntax alone is not evidence of better performance. Do not reverse the
maintainer's FLIP decision in `I12` merely to adopt View Transitions. Coordinate
appearance baselines with the existing Draft `I8`, while retaining separate live
motion and event-lifecycle tests.

## Scope

- **In scope:** voice startup/stop/disposal in .NET and JS (H01); active canvas
  reorder and table resize cleanup (H02); the audited motion selectors and shared
  exit completion (H03); semantic badge text (H04); the bounded focus family below
  (H05); deterministic checks and a live browser host gating CI and publication
  (H06). Complete the contracts for these changes first.
- **Deferred:** H07 asset freshness/compression and host upgrade recipes; H08
  pointer/scroll/glass profiling and optimization; H09 mobile shell sizing; H10
  CSP/theme-input API design. H10's shared exit/focus cleanup is pulled forward
  only where named below. Screenshot baselines and their approval policy stay in
  I8. Other semantic-text surfaces and isolated focus families remain follow-ups.
- **Out of scope:** a redesign, new public components, new AI states, runtime
  dependencies, new tokens or animation primitives, cascade-layer migration,
  changes to LIGHT-TOKEN-SET, a browser support-floor change, manual publishing,
  blanket modernization, or an unmeasured speedup promise.

### First package: desired behaviour

| Finding | Bounded change | Evidence required before calling it fixed |
|---|---|---|
| H06 — regression foundation | A real Blazor host renders the working-tree libraries and their actual global and isolated CSS. Separate browser and deterministic JS tests own the failures below. | Ordinary solution tests need no browser download. The separate browser job installs its declared dependencies, fails if they are missing, and keeps traces/results on failure. Each defect has a failing reproduction before its repair. |
| H01 — voice cancellation | Stop and disposal invalidate startup at token minting, module import, microphone acquisition and every later handshake boundary. A stale attempt cannot clear, start or notify a newer attempt. | Deferred operations cover stop/dispose at each boundary, failure after stop, restart and callbacks already in flight. Late microphone tracks stop immediately; peers, audio elements/contexts, timers and frames return to baseline; no handshake starts after cancellation. .NET phase, transcript and continuation state do not revive. |
| H02 — gestures | Disposal, `pointercancel`, `Escape` and replacement of an active gesture cancel it. Only its own pointer can move or complete it. | Canvas transforms/markers, table preview widths, capture and window listeners are restored/released on cancellation. Later events cannot mutate the removed instance or report `OnNodeReorder`/`OnColumnResized`. A valid release commits once; repeated initialization/disposal is safe. |
| H03 — motion and exit | Reduced motion wins for dialog/backdrop enter/exit, both aura variants and their state/pseudo-element combinations, `.spinner`, `.fade-in`, `.stagger > *`, ambient `.aurora` and Stepper panels. Repair existing `dryl.motion.onExit`/`clearExit` ownership and the modal's pending focus timeout. | Computed styles and live animations show static, usable end states under reduction. Preference changes during exit, cancelled/absent animations, rapid close/reopen and disposal cannot leave mounted exits, stale callbacks or delayed focus stealing. Normal motion still plays and finishes. Rejected interop promises on these touched paths are handled. |
| H04 — semantic badges | Success, warning and danger labels use existing `--fg`; tint, border and optional dot retain the existing semantic token. Neutral and accent badges remain in the test matrix. | Rendered foreground/background composition reaches 4.5:1 for the small label in both modes on the default page, `--bg-1`, and representative flow/floating surfaces. Test text separately from graphical marks; replace duplicated color constants in the contrast check with CSS-derived evidence. |
| H05 — visible focus | Restore a forced-colors focus outline for Stepper headers, shared `.input`/`.textarea`/`.select` controls and `.dialog-close`. Preserve normal accent treatment and keyboard order. | Tab/Shift+Tab and each control's keyboard actions work in both modes. Forced-colors emulation checks the surviving outline, supplemented by a Windows contrast-theme pass. The Select scene includes its real popover and focus return. This does not claim every isolated focus override is repaired. |

Cancellation of table resize restores the pre-gesture inline widths, including an
unset width, without persisting a partial drag. Write this distinction from a
completed resize into the Columns contract.

Voice tests use controlled microphone/WebRTC/network substitutes and a fake token
provider: no paid voice request or physical microphone. Already-running host tools
cannot have their external effects undone by stopping voice; obsolete results
must not restart or mutate the voice session. Keep the page's one-session policy.

### Browser host and evidence ownership

- **Owner:** `DRYL.Components`. Proposed test-only projects:
  `tests/DRYL.BrowserHost/` (minimal .NET 10 interactive Blazor Server host) and
  `tests/DRYL.BrowserTests/` (Microsoft.Playwright for .NET). Both remain outside
  `DRYL.slnx`; neither ships in a package. Use project references to both
  libraries, with no dependency on a moving website checkout.
- **Scenes:** voice start/stop/restart; canvas reorder; table resize; presence
  and dialog close/reopen; Comet/Aurora state transitions; badge variants;
  Stepper and text input/textarea/Select keyboard interaction. Exercise removal
  and re-mounting, not only a static page.
- **Automation:** Chromium, Firefox and WebKit, with package/browser versions
  pinned and logged per run; both color modes and normal/reduced motion.
  Forced colors runs on the supporting browser. Keep engine-specific exclusions
  explicit. Failure screenshots are evidence, not I8 baselines.
- **Support policy:** retain Jan's current stable Chrome, Edge, Firefox and
  Safari target. Record actual browser/OS versions for release evidence,
  including native Chrome/Edge/Firefox/Safari smoke checks. Playwright's
  Firefox/WebKit are patched test builds, not native-browser evidence; device
  emulation is not a physical-device check. Unavailable native checks remain
  explicit release-evidence gaps, never claimed passes. See
  [Playwright browser support](https://playwright.dev/dotnet/docs/browsers).
- **Performance boundaries:** desktop scenes establish correctness. Physical
  phones, address bars, rotation, virtual keyboard behaviour and numeric
  performance budgets belong to H08/H09's measured follow-up scope.
- **Release gate:** `.github/workflows/ci.yml` and
  `.github/workflows/publish.yml` must depend on the same checks for the commit
  they build. Publication currently runs independently: making CI red alone
  does not prevent upload. Configure the existing workflow without publishing
  a package during implementation.
- **Spec coverage:** keep the strict default checker. Add a phase-C gate that
  rejects malformed/duplicate/missing-source contracts and newly uncovered
  components, while reporting the existing uncovered set. Compare component
  identities, not just a count that could conceal lost coverage. Remeasure the
  audit's 54/129 baseline before implementation and shrink the recorded debt
  as contracts land. Never hide all failures with `continue-on-error`.

### Delivery order after confirmation

1. Carry this scope into affected specs and shared contracts. Mark changed
   contracts `Modified`; keep unrelated recorded debt visible.
2. Write the implementation plan: exact files, commands and verification per
   task. Start with the test host, deterministic checks and publication gate,
   then H01, H02, H03, H04 and H05. Add each regression with its repair so an
   intermediate verified commit does not leave the suite intentionally red.
3. Use one verified commit per task, independent review of lifecycle/shared CSS
   changes, and external progress tracking for the task sequence.
4. Run the complete repository verification set plus the new JS/browser cases;
   inspect both modes by eye. Reconcile spec states, release notes, independently
   versioned packages and existing website catalog/demo coverage before claiming
   implementation complete.

## Impact

- **Harness:** `CODE-05`, `UX-01`, `UX-02`, `UX-03` and `UX-06` guide the first
  package. Existing `--fg` and semantic tokens support the badge proposal. For
  forced colors, restore a browser-provided outline with existing color tokens
  and browser-defined geometry; do not invent focus dimensions or system-color
  tokens. Forced colors removes shadows and can remap outline colors; see
  [MDN forced colors](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/forced-colors).
  Verify the visible result, including the non-native Select trigger. Repair
  existing motion primitives without adding keyframes, durations or easings.
  No new runtime dependency or AiState is proposed. Test-only .NET tooling is
  outside `CODE-03`'s shipped-runtime restriction, as I8 documents. If existing
  values cannot meet the tests, return with a concrete proposal under
  `DESIGN-03`/`DESIGN-13`; this idea approves no substitute token.
- **Specs:** affected existing contracts include `specs/E3 AI/F3 DrylCanvas/S4 Interaction.md`,
  `specs/E3 AI/_Interop.md`, `specs/E5 Data/F16 DrylTable/S5 Columns.md`,
  `specs/E5 Data/F3 DrylBadge.md`, `specs/E6 Dialogs/F1 DrylDialog.md`,
  `specs/E3 AI/F7 DrylAuraElements.md`, `specs/E11 Surfaces/F1 DrylPopover.md`
  and `specs/E14 Agent Canvas/F1 DrylCanvasDock.md`. Also reconcile
  `specs/E6 Dialogs/F2 DrylDialogProvider.md` and Data/Dialogs `_Interop.md`.
  New component contracts are needed for `DrylPresence` in E1, `DrylInputText`,
  `DrylTextarea` and `DrylSelect` in E8, and `DrylStepper` plus its `DrylStep`
  child in E10. The voice run belongs in `specs/E15 Agent Inputs/_Api.md` and
  `_Interop.md` as a shared service, not an invented component; add
  `DrylVoiceOrb`'s component contract if its implementation is touched. Shared
  CSS/motion contracts belong in E1's companion files. Update the Badge spec's
  explicit text/dot color contract and the evidence descriptions for `UX-02`,
  `UX-03` and `UX-06`: token synchronization proves no contrast or motion result.
  The audit does not require completing all missing component specs first.
- **Public API:** preserve caller-facing names, signatures, enum values, theme
  seeds and CSS class names. Session ownership must extend across the JS/.NET
  boundary for callbacks already in flight; checking `Phase == Connecting` alone
  cannot distinguish two attempts. Prefer a per-attempt internal interop target
  while preserving the existing public `JSInvokable` methods. Any unavoidable
  public break returns to the idea stage. Bug fixes target PATCH releases; core
  and Agents release independently.
- **Code:** `code/DRYL.Components/wwwroot/js/dryl.js`, `dryl-canvas.js`, `dryl.css`,
  affected isolated CSS and Razor callers; Agents `wwwroot/js/dryl-voice.js` and
  `Voice/DrylVoiceRun.cs`; scripts, tests and both CI workflows. .NET currently
  awaits `MintTokenAsync` and module import without a stop-owned cancellation
  generation; fixing only `getUserMedia` leaves startup races. `onExit` records
  only an animation-end handler and does not own its reduced-motion frame, so
  `clearExit` cannot cancel that completion. The host must load its generated
  CSS-isolation bundle as well as `dryl.css`. Main risks: in-flight callback
  ownership, focus restoration across dialog handoff, shared cascade effects
  and preserving normal motion. Theme/CSP, asset delivery and rendering
  optimization code stay in follow-up scope.

## Decisions

- 2026-09-15 — Jan requested project analysis, review/commit of existing changes,
  and preparation for a performance and hardening update. The existing `AGENTS.md`
  was committed as `d168bd8`; no runtime changes were requested for this audit.
- 2026-09-15 — Jan confirmed current Chrome, Edge, Firefox and Safari with fallback
  behavior for newer capabilities. Older enterprise browsers are not a separate
  target for this proposal.
- 2026-09-15 — Jan asked to start work on I13 and then to continue. The Tech Lead
  refined the first package against the harness, affected contracts and current
  JS/.NET/CI code. The proposal now names its host, scenarios, cancellation
  boundaries, visual treatment and deferred findings. The request to proceed is
  recorded separately from confirmation of the final scope below.
- 2026-09-15 — Jan explicitly confirmed the final first-package scope: bounded
  H01–H06, shared exit/focus cleanup, separate local browser host, publication
  gate, existing-token visual fixes and named deferrals. The idea is `Ready`.

## Readiness review

- Problem, target role, desired behaviour and bounded scope are concrete above.
- Feasibility has been checked against harness, specs, public API and code.
- No new token, runtime dependency, animation primitive, AiState or public API
  break is part of the proposed package.
- I8's baseline policy, H10's CSP/trust-boundary decision, physical mobile
  devices and numeric performance budgets are outside this delivery.
- The Product Owner explicitly confirmed this version on 2026-09-15.

## Adopted contracts

- [Foundation API](../specs/E1%20Foundation/_Api.md) and
  [interop / regression verification](../specs/E1%20Foundation/_Interop.md).
- [DrylPresence](../specs/E1%20Foundation/F2%20DrylPresence.md).
- [Canvas interaction](../specs/E3%20AI/F3%20DrylCanvas/S4%20Interaction.md)
  and [AI interop](../specs/E3%20AI/_Interop.md).
- [Table columns](../specs/E5%20Data/F16%20DrylTable/S5%20Columns.md)
  and [Data interop](../specs/E5%20Data/_Interop.md).
- [Aura layers](../specs/E3%20AI/F7%20DrylAuraElements.md) and
  [Badge](../specs/E5%20Data/F3%20DrylBadge.md).
- [Dialog](../specs/E6%20Dialogs/F1%20DrylDialog.md),
  [Dialog provider](../specs/E6%20Dialogs/F2%20DrylDialogProvider.md)
  and [Dialogs interop](../specs/E6%20Dialogs/_Interop.md).
- [Text input](../specs/E8%20Inputs/F1%20DrylInputText.md),
  [Textarea](../specs/E8%20Inputs/F2%20DrylTextarea.md),
  [Select](../specs/E8%20Inputs/F3%20DrylSelect.md).
- [Stepper](../specs/E10%20Navigation/F2%20DrylStepper.md) and
  [Step](../specs/E10%20Navigation/F3%20DrylStep.md).
- [Voice API](../specs/E15%20Agent%20Inputs/_Api.md),
  [Voice interop](../specs/E15%20Agent%20Inputs/_Interop.md), and
  [Canvas Dock](../specs/E14%20Agent%20Canvas/F1%20DrylCanvasDock.md).

The implementation sequence and its verification live in
[the implementation plan](../docs/2026-09-15-i13-implementation-plan.md).

## Open Points
