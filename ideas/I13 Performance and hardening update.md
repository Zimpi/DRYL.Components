# Performance and hardening update

## Meta
- **State:** Draft

## Problem

The maintainer wants DRYL's JavaScript and CSS checked against current browser
standards and established performance/hardening practices before an update.
The [2026-09-15 audit](../docs/2026-09-15-performance-hardening-audit.md) finds a
modern baseline but concrete lifecycle, motion, contrast and verification gaps.
The current 1,116 passing tests cannot establish browser rendering or JS cleanup.

**Target role:** the consuming Blazor developer who needs responsive, accessible
components and reliable upgrades; the maintainer who needs evidence before release.

## Solution Idea

Deliver a measured hardening update in bounded tasks. Establish browser regression
evidence, fix cancellation and disposal, make reduced motion/focus/contrast reliable,
and document/test asset delivery. Profile pointer, scroll, glass and dense rendering
before adopting optional modern CSS features.

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

- **In scope:** core and Agents audit findings; voice cancellation; active canvas
  and table gestures; shared JS interop cleanup; motion, focus and semantic-text
  contrast; CI coverage; consumer asset freshness/compression; measured rendering
  and mobile viewport improvements; a fallback-preserving CSS feature evaluation.
- **Out of scope:** a redesign, new components unrelated to the findings, new AI
  states, runtime JS frameworks, automatic migration to every new browser feature,
  manual publishing, or an unmeasured speedup promise.

## Impact

- **Harness:** `CODE-05`, `UX-01`, `UX-02`, `UX-03`, `UX-06`, `DESIGN-06` and
  `DESIGN-07` guide concrete fixes. Start with existing tokens. Any new color/focus
  token or motion primitive needs maintainer sign-off (`DESIGN-03`, `DESIGN-10`,
  `DESIGN-13`). No new runtime dependency or AiState is proposed. Test-only .NET
  tooling is outside the shipped-runtime restriction, as I8 documents. Changing
  LIGHT-TOKEN-SET or consumer cascade ordering is a separate explicit decision.
- **Specs:** affected existing contracts include `specs/E3 AI/F3 DrylCanvas/S4 Interaction.md`,
  `specs/E3 AI/_Interop.md`, `specs/E5 Data/F16 DrylTable/S5 Columns.md`,
  `specs/E5 Data/F3 DrylBadge.md`, `specs/E6 Dialogs/F1 DrylDialog.md`,
  `specs/E3 AI/F7 DrylAuraElements.md`, `specs/E11 Surfaces/F1 DrylPopover.md`
  and `specs/E14 Agent Canvas/F1 DrylCanvasDock.md`. Foundation, Inputs, Layout,
  Navigation, Surfaces and Agent Inputs still lack several required component
  specs. Complete the contracts for the selected task before implementing it;
  the audit does not require completing all 75 missing specs first.
- **Public API:** aim to preserve current behavior and signatures for bug fixes.
  A CSP integration option, new token, changed CSS cascade contract or supported
  browser floor needs explicit classification. Core and Agents release independently.
- **Code:** `code/DRYL.Components/wwwroot/js/dryl.js`, `dryl-canvas.js`, `dryl.css`,
  affected isolated CSS and Razor callers; Agents `wwwroot/js/dryl-voice.js` and
  `Voice/DrylVoiceRun.cs`; theme provider/service; install documentation, scripts,
  tests and CI. Main risks: portal/focus order, cancellation races, visual identity,
  dynamic class generation and host-specific cache behavior.

## Decisions

- 2026-09-15 — Jan requested project analysis, review/commit of existing changes,
  and preparation for a performance and hardening update. The existing `AGENTS.md`
  was committed as `d168bd8`; no runtime changes were requested for this audit.
- 2026-09-15 — Jan confirmed current Chrome, Edge, Firefox and Safari with fallback
  behavior for newer capabilities. Older enterprise browsers are not a separate
  target for this proposal.

## Open Points

- Confirm the first delivery scope and order from the completed findings. The
  recommendation is H01–H06 first, followed by measured delivery/rendering work.
- Select the actual browser test host and ownership together with I8; choose
  minimum versions, mobile devices and application scenes for performance budgets.
- Define the supported CSP integration and whether theme colors can originate
  from untrusted end-user data in the intended consuming applications.
- If existing tokens cannot meet the contrast/focus target, present the concrete
  new-token proposal for maintainer sign-off before specifications/implementation.
- Obtain explicit Product Owner confirmation of the final scope before `Ready`
  (`IDEA-04`, `IDEA-06`). No new token, primitive or runtime dependency is approved
  by this Draft.
