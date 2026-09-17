# I13 implementation plan and progress

Scope: the first H01–H06 package explicitly confirmed by Jan on 2026-09-15 in
`ideas/I13 Performance and hardening update.md`. Branch: `codex/i13-hardening`.
H07–H10, I8 baselines and new tokens/public APIs remain outside this delivery.

## Progress

| Task | State | Commit / evidence |
|---|---|---|
| T0 — contracts and adoption | Complete | `17ed224`; coverage 60/129, no structural errors; harness links and whitespace check pass. |
| T1 — regression host and release gates | Complete | `a3173d1`; 45 Node CLI tests; 2 smoke cases per engine in Chromium/Firefox/WebKit; host/suite builds and phase-C gate pass. |
| T2 — voice cancellation | Complete | Owned-session/turn implementation; 25 JS voice cases and full 1,146-case .NET suite pass. Chromium/Firefox voice scenarios pass; final matrix below. |
| T3 — gesture ownership | Complete | 19 deterministic gesture cases; Chromium live pointer matrix passes. Final cross-engine results below. |
| T4 — exit ownership and reduced motion | In progress | JS 8/10 failing before, 10/10 after; reduced-motion browser cases 4/4 fail before CSS fix. Per-exit C# bridge implementation in progress. |
| T5 — badge contrast | In progress | CSS-derived checks reproduced 12 light badge failures; all 50 checks and 7 Node cases pass after label fix. Rendered verification pending. |
| T6 — forced-colors focus | Pending | |
| T7 — review, full verification and documentation | Pending | |

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
