# JavaScript, CSS, performance and hardening audit

Date: 2026-09-15. Runtime baseline: `e640e8a` (DRYL.Components 3.0.0,
DRYL.Components.Agents 0.17.6). Existing-work commit: `d168bd8`.

## Assessment

DRYL already uses modern browser capabilities and has a strong component test
suite. The best next investment is reliable cancellation, real browser regression
coverage, accessible motion/contrast, and predictable asset delivery. Replacing
syntax simply because a newer feature exists would not establish a speed gain.

The maintainer confirmed **current Chrome, Edge, Firefox and Safari, with fallbacks
for newer features**. Exact minimum versions and the release test matrix remain to
be written. This audit covers both packages; it does not implement the update.
The proposal is tracked in [I13](../ideas/I13%20Performance%20and%20hardening%20update.md).

## Existing uncommitted work

The initial working tree contained only an untracked `AGENTS.md`; no staged files
or uncommitted runtime changes existed. It was reviewed and committed unchanged as
`d168bd8` — `docs: add Codex repository instructions` (156 added lines).

Its differences from `CLAUDE.md` are the assistant name in the heading and the
stale `x/127` coverage example; `CLAUDE.md` correctly says `x/129`. Both describe
the idea/spec/plan workflow, selective delegation and review, token and dependency
constraints, verification commands, and release duties. The working tree was clean
after that commit. No push or release was performed.

## Evidence and limits

| Check | Result |
|---|---|
| `dotnet build DRYL.slnx -c Release` | Passed; 80 warnings, 0 errors; both libraries built for net8.0, net9.0 and net10.0 |
| `dotnet test DRYL.slnx -c Release` | 1,116 passed, 0 failed, 0 skipped; test project targets net10.0 |
| `node scripts/check-light-sync.mjs` | Passed |
| `node scripts/validate-light-contrast.mjs` | All 10 configured checks passed; limited coverage, see H04 |
| `node scripts/check-harness-links.mjs` | Passed: 53 rule IDs, 12 files, no broken links |
| `node scripts/check-spec-coverage.mjs` | Exit 1: **54/129 components covered**, 75 missing specs; existing phase C debt |
| `node scripts/check-motion-tokens.mjs` | Passed; does not establish reduced-motion correctness |
| `node --check` on all four runtime JS files | Passed with Node v24.13.0 |
| `dotnet list DRYL.slnx package --vulnerable --include-transitive` | Exit 0; configured sources reported no vulnerable packages in any project |
| Representative light/dark visual check | Performed in the in-app Chromium browser using actual repository CSS and static fixture markup |

Build warnings include member hiding (`CS0108`), nullable-return warnings
(`CS8603`) and component-parameter assignment warnings in tests (`BL0005`). The
total includes repeated target-framework diagnostics; it is not 80 distinct bugs.
The vulnerability query is a point-in-time advisory check, not a security audit or
proof that every package is the latest version.

Independent CSS review and deterministic JS lifecycle reproductions were included.
The JS reproductions execute production source with mocked DOM, microphone, WebRTC,
network and timers. No microphone was used and no voice API request was sent.
Raw evidence is in local temporary files `dryl-audit-build.log`,
`dryl-audit-test.log`, `dryl-audit-vulnerable.log` and
`dryl-lifecycle-audit-20260915.mjs`; these are not durable CI artifacts.

The visual fixture checked controls, semantic badges, an Aurora surface and a
dialog surface. Badge foreground/background values were read from browser-computed
styles. This is **not** a full Blazor integration test, four-engine verification,
mobile Safari run, forced-colors session or reduced-motion emulation. Those cases
remain in the proposed verification work. No INP, heap growth, frame-time or GPU
saving has been measured; performance findings below are candidates to profile.

## Findings, ordered for the update

P1 = address in the first hardening scope. P2 = bounded follow-up work. These are
engineering priorities, not security vulnerability scores.

### H01 — P1: voice startup can outlive stop

**Evidence:** `code/DRYL.Components.Agents/wwwroot/js/dryl-voice.js`, `start`,
`stop`, `teardown`, `channel.onopen`; caller
`code/DRYL.Components.Agents/Voice/DrylVoiceRun.cs`, `StartAsync`, `StopAsync`,
`OnConnected`. The stop button in `DrylCanvasDock.razor` is available while connecting.

Start awaits `getUserMedia`. A stop during that wait marks its state closed and
clears the session. When microphone acquisition later succeeds, startup continues
without checking cancellation. Cleanup subsequently returns early for the already
closed state; a later stop cannot find it.

**Reproduction:** start, stop, resolve the mocked microphone request, complete the
mock handshake, stop again. One microphone track remained unstopped; one peer,
audio context and audio element were created without release; a mock request and
`OnConnected` happened after stop. This can retain resources and report a live
connection after the user ended it.

**Action:** guard every asynchronous boundary with session identity/cancellation;
stop late-acquired tracks immediately; reject obsolete callbacks; abort the
handshake where possible. An `AbortController` can cancel fetch but does not itself
solve late microphone acquisition. Preserve idempotent cleanup. Relevant contract:
`CODE-05`, public `StopAsync`, and the Voice section of
`specs/E14 Agent Canvas/F1 DrylCanvasDock.md`. A dedicated voice component/lifecycle
spec is still missing. [MDN AbortController](https://developer.mozilla.org/en-US/docs/Web/API/AbortController).

### H02 — P2: active drag cleanup is incomplete

**Evidence:** `code/DRYL.Components/wwwroot/js/dryl-canvas.js`, `initReorder`,
local `finish`, `disposeReorder`. Disposal only removes the root `pointerdown`
handler. The gesture's four window handlers remain until a terminal gesture event.

**Reproduction:** after beginning a drag and disposing the canvas, one listener
each for `pointermove`, `pointerup`, `pointercancel` and `keydown` remained. The
dragging class, transform and drop marker remained. A later move changed the node
and a later pointer release invoked `OnNodeReorder` after disposal. That release
event finally removed the handlers; this is not an unconditional permanent leak.

The same ownership gap exists in `dryl.js`, `dryl.table.initColumnResize` and
`disposeColumnResize`: the disposal method cannot remove an active gesture's
window handlers. A third mock reproduction retained both window listeners and
`tbl-resizing`, ignored `pointercancel`, changed cell widths after disposal, and
called `OnColumnResized("name", 150)` on the later release.

**Action:** keep an active-gesture cancellation function in each root state; invoke
it during disposal and before another gesture starts. Cancel listeners, visual
state and capture together; suppress callbacks after disposal. Canvas explicitly
requires detachment in `specs/E3 AI/F3 DrylCanvas/S4 Interaction.md` (`CODE-05`).
Table changes also touch `specs/E5 Data/F16 DrylTable/S5 Columns.md` and its interop
contract.

### H03 — P1: reduced-motion rules lose in the cascade

**Evidence:** `code/DRYL.Components/wwwroot/dryl.css`. The early reduced-motion
rules for `.dialog`, `.dialog-backdrop` and `.dialog-layer.is-exiting .dialog` are
overridden by later normal rules with the same specificity. Aurora selectors such
as `.ai-aura--aurora .ai-aura-ring::before` outrank the reduced-motion selector
`.ai-aura-ring::before`. Generated glow/wash variants have a similar mismatch.
Additional gaps include the shared `.spinner`, `.fade-in`, `.stagger > *` and
ambient `.aurora` animations, plus `.step-panel` in `DrylStepper.razor.css`.

**Action:** explicitly gate motion on `no-preference` or place sufficiently specific
reduced-motion rules after all variants, preserving useful static end states.
Check pseudo-elements, enter/exit completion, and a preference change during an
animation. Existing checks scan token syntax, not the winning browser cascade.
This conflicts with `UX-06` and the motion contract in
`specs/E6 Dialogs/F1 DrylDialog.md`; shared aura behavior belongs with
`specs/E3 AI/F7 DrylAuraElements.md`.
[MDN specificity](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Cascade/Specificity),
[W3C reduced-motion technique](https://www.w3.org/WAI/WCAG21/Techniques/css/C39).

### H04 — P1: passing contrast checks miss small semantic text

**Evidence:** `.badge` in `dryl.css` renders 11px, weight 500 text. Browser-computed
light-mode values for `.badge-success`, `.badge-warning`, `.badge-danger` match
the semantic tokens and their 10% tinted backgrounds. Over `--bg-1` (#f5f5fa),
sRGB alpha compositing and luminance calculation give:

| Badge | Foreground | Background construction | Contrast |
|---|---|---|---:|
| Success | rgb(14,138,77) | 10% foreground over rgb(245,245,250) | 3.59:1 |
| Warning | rgb(180,83,9) | Same construction | 4.05:1 |
| Danger | rgb(220,38,38) | Same construction | 3.82:1 |

These are below 4.5:1 for ordinary small text. The current
`scripts/validate-light-contrast.mjs` checks manually duplicated color constants,
uses 3:1 for semantic tokens, and omits tinted/transparent background composition.
It can stay green even after the real CSS changes.

**Action:** test actual rendered foreground/background pairs in both modes and
separate text requirements from graphical indicators. Prefer existing legible text
tokens; any new semantic foreground token needs `DESIGN-03` sign-off. Update the
coverage claim in `specs/E5 Data/F3 DrylBadge.md` and the `UX-03` check definition
with the implementation. [W3C text contrast](https://www.w3.org/WAI/WCAG22/Techniques/general/G18).

### H05 — P1: shadow-only focus has no forced-colors fallback

**Evidence:** `code/DRYL.Components/Components/Navigation/DrylStepper.razor.css`,
`.step-header` removes the border; `.step-header:focus-visible` uses `outline:none`
and a shadow alone. There is no forced-colors override. The central input and
several navigation/close-button focus rules also suppress outlines.

Forced-colors mode removes shadows; the Stepper consequently has no explicit
surviving focus indicator. This conclusion follows from source and documented
browser behavior, not an OS contrast-mode run during this audit.

**Action:** retain an outline with a system-color-compatible forced-colors treatment;
test the whole focus family with keyboard input. Preserve `UX-01` and `UX-02`.
Stepper needs a component spec first. Proposing a new focus token requires
`DESIGN-03`; using existing tokens does not.
[MDN forced colors](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/forced-colors).

### H06 — P1: browser and CSS correctness are outside CI

**Evidence:** `.github/workflows/ci.yml` and `publish.yml` run restore, build, .NET
tests and packaging. Neither runs the five repository CSS/spec/harness scripts.
bUnit does not execute browser layout, real JS event handling or the CSS cascade.
`tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj` runs only on net10.0,
although both libraries compile for three frameworks.

**Action:** add currently passing deterministic checks to CI; treat the existing
54/129 spec coverage as a ratchet/report during phase C rather than installing an
unconditionally failing gate. Add browser lifecycle, motion, focus, contrast and
asset-upgrade scenarios. Coordinate screenshot baselines with the existing Draft
`ideas/I8 A regression net for what the library looks like.md`; that idea explicitly
distinguishes frozen appearance from live motion tests. Test-only .NET dependencies
are outside `CODE-03`'s shipped-runtime restriction, as I8 already explains.

### H07 — P2: asset freshness needs a consumer-facing contract

**Evidence:** `README.md` and `code/DRYL.Components/PACKAGE.md` show stable
`_content/DRYL.Components/dryl.css` and `js/dryl.js` URLs. `DrylCanvas.razor`,
`DrylAiField.razor`, `DrylVoiceRun.cs` and `DrylVoiceOrb.razor` import modules through
stable paths. No fingerprinting/cache integration guide accompanies them. This
does **not** prove that a deployed host serves stale files: host configuration,
import maps, content ETags and response headers determine that.

**Action:** document and test the host-specific asset paths. For modern Blazor Web
Apps use `MapStaticAssets`, `@Assets[...]` and generated import maps/module mappings
as appropriate. Verify the published output and an upgrade with a warm browser
cache. Provide a .NET 8 and standalone WASM recipe instead of copying a .NET 10
host example into every app. Use content-dependent URLs for immutable caching;
do not append a fresh timestamp on every load.
[Microsoft Blazor static assets](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0).

Include the consumer's generated `{App}.styles.css`: DRYL has substantial isolated
component CSS. `dryl.css` alone is not the complete stylesheet surface. RCL isolated
styles are included through the application's CSS-isolation bundle, so distinguish
those requirements clearly in installation examples.
[Microsoft CSS isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0).

### H08 — P2: avoidable work in pointer, scroll and glass paths

**Evidence:** `dryl.spotlight.track` and `dryl.depthglass.track` in `dryl.js` read
geometry and write custom properties for every pointer event. `dryl.popover.place`
measures after writing matched width and runs directly for captured scroll/resize
events. DepthGlass's `.dg-warp` keeps `will-change:transform` permanently; its
`.dg-specular` moves a radial-gradient center, which is a paint candidate.

**Action:** coalesce events into at most one scheduled update per frame, separate
reads from writes, avoid unchanged writes, cancel the frame on teardown and honor
reduced motion in JS too. For popovers, consider panel/anchor size observation and
visual viewport changes. Preserve existing focus, portal, scroll-position and exit
contracts. `requestAnimationFrame` is an organizing mechanism, not proof of lower
latency. Profile before/after on dense real scenes.
[web.dev layout work](https://web.dev/articles/avoid-large-complex-layouts-and-layout-thrashing),
[MDN will-change](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/will-change).

`DrylChat.razor.css` (`.chat`) and `DrylValidationSummary.razor.css`
(`.validation-summary`) bypass `--glass-fx-flow` with direct blur expressions.
These are documented `DESIGN-06`/`DESIGN-07` debts. Restore central flow/float
control and compare paint/compositing cost while retaining visible floating glass.
[web.dev animation performance](https://web.dev/articles/animations-guide).

### H09 — P2: mobile shell height is less modern than dialogs

**Evidence:** `.app-shell`, `.sidebar`, `.sidebar--flyout` and mobile shell sidebar
rules in `dryl.css` still use `height:100vh`; the shell clips overflow and scrolls
internally. Dialogs already use dynamic viewport units. Mobile browser chrome can
leave the shell/footer below the visible viewport.

**Action:** test a `100vh` fallback followed by `100dvh`, or `svh` where stable
height is preferable. Verify mobile address bars, rotation and the virtual keyboard
separately; `dvh` is not a universal keyboard fix. This remains a mobile reproduction
candidate. Layout/Drawer specs need completion before implementation.
[web.dev viewport units](https://web.dev/blog/viewport-units).

### H10 — P2: define CSP and theme-input hardening boundaries

**Evidence:** `DrylThemeProvider.razor` emits an inline `<style>` and `_modeScript`
without a nonce option. A strict host `script-src` policy blocks the inline startup
script unless it authorizes its bytes or nonce. Theming accepts authored CSS values;
`harness/theming.md` already requires developer-controlled colors and server-side
validation for untrusted input.

**Action:** define a supported CSP integration recipe and test prerender, startup
mode restoration and runtime theme changes. Consider an external bootstrap or
nonce-aware integration. Inventory style tags, style attributes and CSSOM writes
separately; their policy behavior differs. Decide whether the library should reject
untrusted color strings or retain a clearly enforced consumer boundary. This is a
hardening opportunity, not a demonstrated exploitable injection in a consuming app.
New public configuration would need its spec and version classification.
[MDN CSP](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP).

Other lifecycle follow-ups: `dryl.motion.onExit` relies on `animationend` and has
no general cancellation/zero-animation completion path; `DrylPresence` has no
watchdog equivalent to the popover's. Several `invokeMethodAsync` calls are wrapped
only in synchronous `try/catch`, which does not catch a rejected Promise. The
anonymous modal focus timeout cannot be cancelled on detach (already noted in
`CODE-05`). Exercise these with interrupted animations and circuit disconnection.
Microsoft recommends client-side DOM cleanup for cases where disposal interop can
no longer reach the browser; use appropriately scoped observation, not an expensive
document-wide observer per component.
[Microsoft interop disposal guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0).

## What “current CSS/JavaScript” means here

CSS modules have independent levels; there is no single CSS version switch to bump
in `dryl.css`. The [W3C CSS Snapshot 2026](https://www.w3.org/TR/css-2026/) describes
the standards landscape. Browser capability, library public contracts and cached
asset identity are three separate compatibility questions.

DRYL already uses `@property`, `color-mix()`, relative OKLCH colors, container
queries, container units, `:has()`, individual transform properties, dynamic
viewport units and the Popover top layer. Its JS uses async functions, optional
chaining, WeakMaps, observers, lazy ES modules, requestAnimationFrame and Web
Animations. No JS framework, TypeScript conversion or blanket polyfill layer is
needed to make those approaches current. The legacy clipboard fallback is isolated
behind the modern Clipboard API, rather than the default route.

| Candidate | Current assessment | Proposed use |
|---|---|---|
| CSS Anchor Positioning | `anchor-name` is newly Baseline since January 2026 | Prototype positioning for popovers; preserve JS fallback and existing focus/portal contracts. [MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/anchor-name) |
| `@starting-style` | Baseline since August 2024 | Evaluate top-layer entry/exit with discrete transitions; it does not replace Blazor unmount coordination. [MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@starting-style) |
| `content-visibility:auto` | Baseline since September 2024 | Measure on long independent sections. Containment can alter overlay and morph measurements. [MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/content-visibility) |
| `@layer` | Established cross-browser feature | Consider predictable override ordering; assess breaking changes to consumer cascade. No inherent speed claim. [MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@layer) |
| `light-dark()` | Baseline since May 2024 | Optional simplification, requiring a deliberate change to the two-copy LIGHT-TOKEN-SET contract. [MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/color_value/light-dark) |
| `interpolate-size`, scroll-driven timelines | Still limited availability in the checked references | Optional progressive enhancements only. [Intrinsic size](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/interpolate-size), [scroll timeline](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/animation-timeline/scroll) |

Baseline is a useful compatibility signal, not a performance or accessibility
certification. Verify the exact subfeature and a functional fallback in the agreed
browser matrix. [web.dev Baseline](https://web.dev/baseline).

Do not reintroduce View Transitions just to adopt a newer API: the maintainer
explicitly chose FLIP after visual problems, recorded in
`ideas/I12 Replace the View Transition API with FLIP.md`. Preserve that decision.

## Asset size baseline and version policy

Measured from local source/build output with Node's default gzip/Brotli compression:

| Asset | Raw bytes | gzip estimate | Brotli estimate |
|---|---:|---:|---:|
| Core `dryl.css` | 210,493 | 47,676 | 39,193 |
| Core `dryl.js` | 116,579 | 32,850 | 27,876 |
| Generated core CSS-isolation bundle, net10.0 | 142,769 | 26,476 | 21,906 |
| Core `dryl-canvas.js` | 7,257 | 2,652 | 2,221 |
| Agents `dryl-aifield.js` | 1,638 | 729 | 609 |
| Agents `dryl-voice.js` | 20,878 | 6,921 | 5,915 |

The main CSS + main JS + generated core isolated CSS total 469,841 raw bytes,
or 88,975 bytes in this Brotli estimate. Fonts, Agents isolated CSS, host assets
and Blazor runtime are additional. These estimates are not measured network
transfer sizes; published host compression settings and manifests must be checked.

Keep readable source. First establish compressed delivery and actual bundle usage;
then benchmark build-time minification or optional splitting. `MapStaticAssets`
provides compression/fingerprinting support, not minification. Avoid runtime build
dependencies and unsafe CSS purging of classes assembled by Razor or AI canvas
rendering. [Microsoft static file optimizations](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0).

Core and Agents have independent package versions and release tags, as implemented
by `.github/workflows/publish.yml`. Preserve `REL-01`/`REL-02`: classify actual fixes
as PATCH, new public capabilities as MINOR, and breaking API/consumer styling
contracts as MAJOR after review. A CSS-only fix still changes the shipped package.
Docs-only audit work leaves both versions unchanged (`REL-03`). Content hashes
handle browser cache identity; package SemVer handles consumer compatibility.

## Recommended delivery sequence

1. **Regression baseline and concrete hardening:** CI checks, voice cancellation,
   active gestures, reduced motion, small-text contrast and surviving focus.
2. **Measured delivery/rendering performance:** publish-host cache test, asset
   compression, pointer/scroll batching, flow frost and mobile shell sizing.
3. **Selective new browser features:** Anchor Positioning prototype, then only
   changes whose compatibility, maintenance or measured performance benefit is clear.

For each implementation task, first reconcile/create the affected component spec,
then write exact files, commands and verification into an implementation plan.
Use one verified commit per task. The present plan is an audit plan, not an
authorization to skip the idea/spec stages.

Proposed measurements: repeat mount/open/close/remove cycles and require resource
counts to return to baseline; cancellation must cause no late callback; profile a
dense pointer/scroll scene on representative hardware; compare cold and warm
published loads and an asset upgrade. Record median and tail frame/interaction
timings, layout/paint work and heap/resource trends under identical conditions.
Choose numeric performance budgets after the first real application baseline.

The report and I13 substitute for local capture only: no Trello cards were created
because the Trello connector was unavailable, and existing board duplicates could
not be checked.
