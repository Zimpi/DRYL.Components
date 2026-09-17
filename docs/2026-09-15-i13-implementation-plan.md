# I13 implementation plan and progress

Scope: the first H01–H06 package explicitly confirmed by Jan on 2026-09-15 in
`ideas/I13 Performance and hardening update.md`. Branch: `codex/i13-hardening`.
H07–H10, I8 baselines and new tokens/public APIs remain outside this delivery.

## Progress

| Task | State | Commit / evidence |
|---|---|---|
| T0 — contracts and adoption | Complete | `17ed224`; coverage 60/129, no structural errors; harness links and whitespace check pass. |
| T1 — regression host and release gates | Complete | `a3173d1`; 45 Node CLI tests; 2 smoke cases per engine in Chromium/Firefox/WebKit; host/suite builds and phase-C gate pass. |
| T2 — voice cancellation | Complete | `b25ffcb`; owned-session/turn implementation, 25 JS voice cases, full .NET suite and final browser matrix below. |
| T3 — gesture ownership | Complete | `1173283`; 19 deterministic gesture cases and live pointer matrix in both modes. |
| T4 — exit ownership and reduced motion | Complete | `6d03892`; 10 deterministic motion cases and per-exit C# bridges; final browser matrix below. |
| T5 — badge contrast | Complete | `4127845`; 50 CSS-derived checks, 7 Node regressions and rendered contrast in all engines/both modes. |
| T6 — forced-colors focus | Implemented; native evidence pending | `65d25dd`; browser focus matrix passes including Chromium/Chrome/Edge forced colors. Native Windows contrast-theme inspection remains unavailable. |
| T7 — review, full verification and documentation | Complete within documented evidence limits | Independent review, isolated release checkout, package validation and evidence reconciliation below. |

One verified commit per task. Only the named files are staged. Update this table
as each task completes; keep evidence and known limitations in this file. Tasks
may use independent agents only on disjoint files. The main agent owns commits,
versioning and this progress record.

## T0 — contracts and adoption

Files:

- `ideas/I13 Performance and hardening update.md` (Ready then Adopted).
- `specs/E1 Foundation/F2 DrylPresence.md`, `_Api.md`, `_Interop.md`.
- `specs/E3 AI/F3 DrylCanvas/_Component.md`, `S4 Interaction.md`,
  `specs/E3 AI/F7 DrylAuraElements.md`, `specs/E3 AI/_Interop.md`.
- `specs/E5 Data/F3 DrylBadge.md`, `F16 DrylTable/_Component.md`,
  `F16 DrylTable/S5 Columns.md`, `specs/E5 Data/_Interop.md`.
- `specs/E6 Dialogs/F1 DrylDialog.md`, `F2 DrylDialogProvider.md`, `_Interop.md`.
- `specs/E8 Inputs/F1 DrylInputText.md`, `F2 DrylTextarea.md`,
  `F3 DrylSelect.md`, `_Api.md`, `_Interop.md`.
- `specs/E10 Navigation/F2 DrylStepper.md`, `F3 DrylStep.md`, `_Api.md`,
  `_Interop.md`.
- `specs/E14 Agent Canvas/F1 DrylCanvasDock.md`;
  `specs/E15 Agent Inputs/_Api.md`, `_Interop.md`.
- This plan. Reconcile the existing Popover contract if shared exit behaviour
  requires a substantive clarification: `specs/E11 Surfaces/F1 DrylPopover.md`.

Verification: `node scripts/check-spec-coverage.mjs --quiet` must report rising
coverage without structural errors (strict exit 1 remains expected); run
`node scripts/check-harness-links.mjs` and `git diff --check`. Read changed
contracts against actual parameters and callers. Commit: `spec: adopt I13
hardening contracts and implementation plan`.

## T1 — regression host and release gates (H06)

Files:

- `tests/DRYL.BrowserHost/DRYL.BrowserHost.csproj`, `Program.cs`,
  `Components/_Imports.razor`, `Components/App.razor`, `Components/Routes.razor`,
  `Components/Pages/Home.razor`, `Components/TestDialog.razor`,
  `wwwroot/fixtures.css`, `wwwroot/voice-fixture.js`, `FakeTokenHandler.cs`.
- `tests/DRYL.BrowserTests/DRYL.BrowserTests.csproj`, `BrowserFixture.cs`,
  `SmokeTests.cs`, `README.md`.
- `scripts/check-spec-coverage.mjs`, `scripts/spec-coverage-baseline.json`,
  `tests/js/spec-coverage.test.mjs`.
- `.github/workflows/verify.yml`, `ci.yml`, `publish.yml`, `.gitignore`.

Use pinned `Microsoft.Playwright` 1.62.0 (NuGet index verified on 2026-09-15)
in the test project only. Keep both new projects outside `DRYL.slnx`. The host
uses actual project references, static assets and the generated isolation bundle.
Initial browser tests assert render/interop readiness, not known-broken cases.
Keep the strict coverage default; phase-C mode rejects both structural errors
and changes outside an explicit decreasing uncovered-component set.

Commands and verification:

1. `dotnet build tests/DRYL.BrowserHost/DRYL.BrowserHost.csproj -c Release`
2. `dotnet build tests/DRYL.BrowserTests/DRYL.BrowserTests.csproj -c Release`
3. `pwsh tests/DRYL.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium firefox webkit`
4. `node --test tests/js/spec-coverage.test.mjs`
5. `node scripts/check-spec-coverage.mjs --baseline scripts/spec-coverage-baseline.json`
6. `dotnet test tests/DRYL.BrowserTests/DRYL.BrowserTests.csproj -c Release`
   with `DRYL_BROWSER=chromium`, then `firefox`, then `webkit`.
7. Inspect workflow dependencies: `publish` requires the reusable verification
   result for the same checkout before login/upload; PR CI calls it too.
8. Keep failure trace/screenshot/log files under `artifacts/browser/<engine>/`
   and TRX results under `artifacts/test-results/<engine>/`; upload both with
   `if: always()` so failed jobs retain reviewable evidence.

Commit: `test: add Blazor browser regression host and publication gates`.

## T2 — voice cancellation (H01)

Files: `code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs`,
`code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js`,
`tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceCancellationTests.cs`,
`tests/js/voice.test.mjs`, `tests/DRYL.BrowserTests/VoiceTests.cs`, relevant E15
interop/API and Dock contract, Agents csproj/version and `CHANGELOG.md`.
Extend `tests/DRYL.BrowserHost/Components/Pages/Home.razor` with a real optional
`DrylCanvasDock` mount toggle; verify removing/remounting the dock leaves its
host-owned voice run alive and reattaches the orb.

Verify each deferred startup boundary and callbacks from obsolete attempts;
stop/dispose/restart and rejected media/network operations must release owned
resources and leave the current run untouched. Preserve existing public methods.
Read production callbacks as well as the direct start path.

Implementation choice: each .NET attempt owns an internal JS session handle,
created by a module factory before its `start` dispatch. Its `stop` makes late
`start` inert and cannot stop another handle's session. Existing module exports
and public callback signatures stay compatible; browser callbacks use an internal
per-attempt .NET bridge. Observe late resource-producing import/factory results
so their references can be disposed, rather than abandoning those awaits on
cancellation. HTTP and browser handshake cancellation still propagate normally.

Commands: `node --test tests/js/voice.test.mjs`;
`dotnet test DRYL.slnx -c Release --filter FullyQualifiedName~Voice`;
browser suite filtered to `VoiceTests`. Run reproduction before fixing and record
its failure. Agents PATCH bump shares one unpublished block across this stack.
Commit: `fix: cancel obsolete voice startup and callbacks`.

## T3 — gesture ownership (H02)

Files: core `wwwroot/js/dryl-canvas.js`, `wwwroot/js/dryl.js` (table resize only),
`tests/js/gestures.test.mjs`, `tests/DRYL.BrowserTests/GestureTests.cs`, Canvas
Interaction/Table Columns and their interop/rolled-up specs, core csproj/version
and `CHANGELOG.md`.

Commands: `node --test tests/js/gestures.test.mjs`; browser suite filtered to
`GestureTests`; `dotnet test DRYL.slnx -c Release --filter
"FullyQualifiedName~Canvas|FullyQualifiedName~Table"`.
Verify cancellation restores widths/markers/capture and listeners, pointer
identity, valid single commits and late-event silence. Core PATCH bump shares
one unpublished block across subsequent fixes.
Commit: `fix: cancel canvas and table gestures on disposal`.

## T4 — exit ownership and reduced motion (H03)

Files: core `wwwroot/js/dryl.js` (motion exit and modal focus),
`Components/Providers/DrylPresence.razor`, `wwwroot/dryl.css`,
`Components/Surfaces/DrylPopover.razor`,
`Components/Navigation/DrylStepper.razor.css`,
`tests/DRYL.Components.Tests/DrylPresenceTests.cs`, `tests/js/motion.test.mjs`,
`tests/DRYL.Components.Tests/DrylPopoverTests.cs`,
`tests/DRYL.BrowserTests/MotionTests.cs`, Foundation/Dialogs/Aura/Stepper and
Popover contract, `harness/code.md`, `harness/uiux.md`, changelog.

Independent T0 review found that Popover reuses an unversioned `ExitCallback`
and queues watchdog completion without an exit identity. Both must be scoped to
the current exit so close/reopen/close cannot accept an older completion. This
is within the confirmed shared-exit scope, not a new public capability.

Commands: `node --test tests/js/motion.test.mjs`;
`dotnet test DRYL.slnx -c Release --filter
"FullyQualifiedName~Presence|FullyQualifiedName~Dialog|FullyQualifiedName~Popover"`;
browser suite filtered to `MotionTests` in three engines; all existing motion
and light-sync scripts. Reproduce cancelled/missing animation, preference switch,
rapid reopening and disposed focus timer. Assert normal animations still run.
No new tokens, durations or keyframes.
Commit: `fix: complete interrupted exits and honor reduced motion`.

## T5 — badge contrast (H04)

Files: core `wwwroot/dryl.css`, `scripts/validate-light-contrast.mjs`,
`tests/js/contrast.test.mjs`, `tests/DRYL.BrowserTests/ContrastTests.cs`,
Badge spec, `harness/uiux.md`, changelog.

Commands: `node scripts/validate-light-contrast.mjs`;
`node --test tests/js/contrast.test.mjs`; browser suite filtered to
`ContrastTests` in three engines. Verify actual text/background composition in
both modes; keep semantic dot/border/fill while labels use `--fg`. Check normal
and floating/flow surface cases, alpha compositing and threshold failures.
Commit: `fix: make semantic badge labels meet text contrast`.

## T6 — forced-colors focus (H05)

Files: core `wwwroot/dryl.css`,
`Components/Navigation/DrylStepper.razor.css`,
`tests/DRYL.BrowserTests/FocusTests.cs`, E8/Stepper/Dialog contracts,
`harness/uiux.md`, changelog.

Commands: browser suite filtered to `FocusTests` in Chromium for forced colors
and all engines for ordinary keyboard behaviour; light-sync/motion scripts.
Inspect both modes and Windows contrast-theme focus where available. Use only
existing color tokens/browser-defined outline geometry. Preserve ordinary focus
and test the Select trigger with its real popover.
Commit: `fix: preserve focus outlines in forced colors`.

## T7 — review and release closure

Files: this plan, final states/evidence in affected specs, `harness/uiux.md`,
`harness/code.md`, `CHANGELOG.md`, `tests/DRYL.BrowserTests/README.md`, any fixes
required by independent review. In sibling `DRYL.Website`, verify the existing
catalog entries and update `Components/ComponentCatalog.cs` descriptions plus
relevant examples only if needed, following its own AGENTS.md. Do not include
its pre-existing untracked AGENTS.md in a commit.

Required commands, run against the final tree:

```
dotnet build DRYL.slnx -c Release
dotnet test DRYL.slnx -c Release
node scripts/check-light-sync.mjs
node scripts/validate-light-contrast.mjs
node scripts/check-harness-links.mjs
node scripts/check-spec-coverage.mjs
node scripts/check-motion-tokens.mjs
node --test tests/js/*.test.mjs
```

Also run the phase-C gate, all three browser engines, package validation with
`dotnet pack` for both libraries and independent code review. Inspect both modes
by eye. Record native-browser and Windows contrast-theme limits explicitly.
No manual publication, push or release tag. Commit:
`docs: record I13 hardening verification and release notes`.

## Evidence and limits

- Before work: core 3.0.0 and Agents 0.17.6 both have release tags.
- Website working tree initially has only an untracked `AGENTS.md`; preserve it.
- Native Safari and physical-device evidence require the corresponding platform;
  Playwright WebKit alone cannot establish that evidence.
- T1: Microsoft.Playwright 1.62.0 installed Chromium 151.0.7922.34, Firefox
  153.0 and WebKit 26.5 on Windows. Both modes and a real Stepper event/render
  roundtrip pass in each engine. Console errors are captured alongside page
  errors. Independent review verified the same-checkout publish dependency;
  its circuit-liveness and startup-diagnostic findings were addressed. All
  three workflow YAML files parse; hosted Actions execution is pending CI.
- T2 browser reproduction: 12/12 Chromium cases failed to start a replacement
  before the old startup settled. After the owned-session fix, 18/18 Firefox
  cases pass, including actual dock keyboard stop and live removal/remount.
- T4 reproduction: live and initial reduced-motion cases in both modes showed
  `ai-aura-drift`, spinner, fade/stagger and ambient motion continuing. The new
  rules keep a static aura cue and cover higher-specificity states/pseudo-elements.
- T5: the old light semantic badge labels measured 3.50–4.20:1 over the four
  test surfaces. The CSS-derived script now reads actual palette/rules, composes
  alpha colors and requires 4.5:1 for badge text; it keeps the original semantic
  indicator/chart checks as separate checks.

## Resumed verification — 2026-09-17

The interrupted working tree was not release-ready: the first full .NET run
failed 3 of 1,146 cases, and the uncommitted gesture browser fixtures also
failed. The resumed work fixed a real same-session voice race by invalidating
pending `ShouldContinue` decisions when user speech takes the floor. The two
Popover-related test failures needed lifecycle-aware assertions: await the
new exit callback registration and allow the prompt's real exit window.
Independent review of voice resources/turn ownership, gesture rollback, modal
focus timers and Presence/Popover generations found no remaining actionable
code findings after that repair.

Browser fixture reconciliation retained real Blazor events, library assets and
native pointer capture. Coordinates cross the Playwright bridge as supported
numbers; Canvas setup waits for its toolbar animation and keeps the drop target
inside the viewport. Reopening is requested in the browser turn that observes
the running exit, and keyboard dialog opening establishes an explicit focus
return target on WebKit. Dock keyboard input waits for actual top-layer
promotion. Windows WebKit lacks native MediaStream, so the already-offline
voice fixture supplies an inert object only for the fake peer/meter. The host
now displays error messages and declares an empty favicon to avoid unrelated
404 errors in branded browsers. None of these fixtures verifies real audio.

The final code was built independently of the concurrent I14 work in
`artifacts/i13-release` (detached I13 checkout plus the final test-only fixture
repairs). The following results apply to I13's runtime, not later Live changes:

| Verification | Result |
|---|---|
| `dotnet build DRYL.slnx -c Release` | Pass for net8/net9/net10; 80 existing warnings, 0 errors. |
| `dotnet test DRYL.slnx -c Release --no-build` | 1,146 passed, 0 failed/skipped. |
| `node --test tests/js/*.test.mjs` | 106 passed, 0 failed/skipped. |
| Light token sync / motion tokens / harness links | Pass; no broken harness links. |
| CSS-derived contrast | 50/50 pass, including all 40 badge label compositions at 4.5:1. |
| Strict spec coverage | Expected exit 1: 60/129 covered; the remaining 69 are existing phase-C debt. |
| Phase-C coverage gate | Pass against the explicit 69-component baseline, no structural errors. |
| Chromium 151.0.7922.34 | 62/62 passed, both modes including forced colors. |
| Playwright Firefox 153.0 | 60/60 passed, both modes. |
| Playwright WebKit 26.5 | 60/60 passed, both modes. |
| Installed Chrome 152.0.7977.83 | 6/6 smoke/focus cases passed, both modes and forced colors. |
| Installed Edge 153.0.4234.32 | 6/6 smoke/focus cases passed, both modes and forced colors. |
| `dotnet pack` for both projects | Core 3.0.1 and Agents 0.17.7 nupkg/snupkg created; all three framework assemblies present; Agents depends on Core 3.0.1 in all frameworks. |

Playwright is 1.62.0.0; host OS is Windows NT 10.0.26200.0. Machine-readable
results, browser versions, host logs and screenshots live in
`artifacts/i13-release/artifacts/`; the native Chrome/Edge smoke/focus evidence
lives in the main checkout's `artifacts/`. Test fixtures shut down their hosts
and browsers on completion. The initially failing runs were used to diagnose
and fix the resumed implementation/fixtures and are not reported as passes.

Visual inspection of rendered badge and focus scenes in dark/light modes
confirmed readable labels, semantic dots/tints and preserved focus indicators;
forced-color screenshots show the surviving browser outlines. This is scoped
visual QA, not the deferred I8 screenshot-baseline approval. Existing website
catalog routes for Presence, Popover, Badge, Table, Canvas, Dialog, Inputs,
Form Controls, Stepper and Canvas Dock were verified in the sibling checkout;
no new component requires registration and their descriptions remain accurate.
The already documented AuraElements/shared-controls catalog debt is retained.

### Remaining evidence limits

- Native Safari and native Firefox smoke were not run. Playwright WebKit and
  Firefox are patched test engines and are not claimed as native evidence.
- A real Windows contrast-theme inspection remains outstanding. Browser forced
  colors emulation, including installed Chrome/Edge, does not replace it.
- No real microphone, paid speech connection or physical mobile device was
  used. H08/H09 performance/device work and I8 appearance baselines remain
  outside I13. No measured speedup is claimed.
- The existing Canvas toolbar hit-target layering gap remains documented in
  `specs/E3 AI/F3 DrylCanvas/S4 Interaction.md`; ownership tests use a genuinely
  exposed part of the grip without force-clicks or stylesheet overrides.
- Hosted GitHub Actions, remote push, release tagging and publication were not
  performed. The reusable same-checkout verification dependency gates upload
  when publication is later authorized. Package files are local validation
  artifacts; this session did not publish Core 3.0.1 or Agents 0.17.7.
