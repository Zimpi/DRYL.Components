# Phase C, core batch: Surfaces, Navigation, Inputs — plan

Target: `release/3.17.0`, worked directly on the release branch.
Meter before: `84/131 components covered`. Meter after: `104/131`.

Why these: they are the components every consuming app places, and the three
`DESIGN-07` debt hits (`.chat`, `.reconnect-overlay`, `.validation-summary`) all
sit in components that had no spec. Layout (14), the remaining Providers and the
Agents package follow in a later batch.

Specs are reverse-engineered from the code as it stands (`SPEC-03`, `SPEC-05`,
`SPEC-06`, `SPEC-07`, `SPEC-08`, `SPEC-09`); format and depth follow
`specs/E8 Inputs/F11 DrylToggle.md`. A spec describes behaviour that exists; what
falls short of a rule goes into `## Recorded debt`, never into a criterion.
Spec-only tasks touch no library code, so they need no `<Version>` bump and no
changelog entry (`REL-01`).

Each task also removes its components from `scripts/spec-coverage-baseline.json`
in the same commit — a covered component left in the baseline fails the publish
gate (stale baseline entry).

## Task 1 — E11 Surfaces (6)

Files: `specs/E11 Surfaces/F3 DrylCard.md`, `F4 DrylChat.md`,
`F5 DrylChatComposer.md`, `F6 DrylDepthGlass.md`, `F7 DrylMarkdown.md`,
`F8 DrylMessage.md`; `specs/E11 Surfaces/_Api.md` (`MessageRole` and whatever
else the six own; drop the "partial by design" paragraph), `_Interop.md`.
Commit: `Specs: the six remaining Surfaces components`.

## Task 2 — E10 Navigation (4) + E1 DrylReconnectModal (1)

Files: `specs/E10 Navigation/F8 DrylTabs.md`, `F9 DrylTab.md`,
`F10 DrylBreadcrumbs.md`, `F11 DrylBreadcrumbItem.md`, `_Api.md`, `_Interop.md`;
`specs/E1 Foundation/F4 DrylReconnectModal.md` (next free number), `_Api.md` /
`_Interop.md` if it owns anything.
Commit: `Specs: Tabs, Breadcrumbs and the reconnect modal`.

## Task 3 — E8 Inputs (9)

Files: `specs/E8 Inputs/F15 DrylInputNumber.md`, `F16 DrylInputPassword.md`,
`F17 DrylInputMask.md`, `F18 DrylDatePicker.md`, `F19 DrylChipInput.md`,
`F20 DrylSegmentedControl.md`, `F21 DrylSegment.md`, `F22 DrylFormField.md`,
`F23 DrylValidationSummary.md`; `_Api.md`, `_Interop.md`.
Commit: `Specs: the nine remaining Inputs components`.

Tasks 1–3 touch disjoint folders and run in parallel (one subagent each);
the baseline edit is done by the main agent when committing each task.

## Task 4 — Eye check, both modes

Every demo page of the 20 components on `DRYL.Website` (`/components/<slug>`,
`http` launch profile, port 5044), dark and light, at desktop width and 375px.
The `Both color modes` evidence line of each spec is written only after this.
Findings become `## Recorded debt` entries or Task 5 fixes.

## Task 5 — Pay the DESIGN-07 debt

Replace the hand-written `backdrop-filter: blur(...)` in
`DrylChat.razor.css` (`.chat`), `DrylReconnectModal.razor.css`
(`.reconnect-overlay`) and `DrylValidationSummary.razor.css`
(`.validation-summary`) with the glass token that fits the surface
(`DESIGN-06`: in flow → `--glass-fx-flow`; floating → `--glass-fx-float`).
Update the three specs, the `DESIGN-07` Check line in `harness/design.md`, and
`CHANGELOG.md` `[3.17.0]` (`<Version>` stays — 3.17.0 is unshipped).
Commit: `Glass tokens for Chat, ReconnectModal and ValidationSummary (DESIGN-07)`.

## Verify (after each task)

`dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs --baseline scripts/spec-coverage-baseline.json`,
`node scripts/check-motion-tokens.mjs`,
`rg -n ':[0-9]+|line[s]? [0-9]+' specs/` (no line-number citations, `SPEC-09`).
