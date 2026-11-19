# I45 — The aura frames a surface: plan

Idea: [`ideas/I45 The aura frames a surface, the content carries the rest.md`](../../ideas/I45%20The%20aura%20frames%20a%20surface,%20the%20content%20carries%20the%20rest.md) (direction A — the content carries the state).
Target: `release/3.17.0`, worked directly on the release branch.

## How it works

The eight components keep `AiAuraCss.Append` on the wrapper they have today, so
`.ai-aura` and the state classes still supply `--ai-hot` / `--ai-cool` and the
state's periods. What changes is what is drawn under them:

- Six components stop rendering the frame. Where `AiState.Proposed` is shown
  (`DrylRating`, `DrylSlider`, `DrylRadioGroup`) `DrylAuraElements` stays in the
  markup with the new `Frameless` parameter, so the screen-reader text of a
  proposal keeps its one source.
- A light ring around a small round mark is one shared primitive in `dryl.css`,
  `.ai-mark-ring`, with the keyframes `ai-mark-breathe` and `ai-mark-out`.
- No wrapper gains padding, radius or a fill in AI mode any more, so entering
  AI mode moves nothing.

## Task 1 — Library (DRYL.Components)

Files:
- `harness/ai.md` — `AI-08`, a pointer from `AI-02`; `harness/tokens.md` — the
  mark ring and the new periods.
- `Components/AI/DrylAuraElements.razor` — `Frameless`.
- `wwwroot/dryl.css` — `.ai-mark-ring` block; the progress AI rules; the radio
  AI rules; removal of the AI wrapper surfaces of rating and radio group.
- `Components/Feedback/DrylProgress.razor` — no frame, `ai-active` class.
- `Components/Feedback/DrylSkeleton.razor`, `.razor.css` — no frame, tint for
  `Active` and `Thinking`.
- `Components/Feedback/DrylSpinner.razor`, `.razor.css` — no frame, state hue.
- `Components/Inputs/DrylRating.razor`, `.razor.css` — no frame, the `Thinking`
  keyframe interpolates between two `color-mix` values.
- `Components/Inputs/DrylSlider.razor`, `.razor.css` — no frame, ring on the
  knob, sheen through the fill.
- `Components/Inputs/DrylRadioGroup.razor`, `DrylRadio.razor`,
  `RadioGroupContext` — no frame, ring on the selected option.
- `Components/Navigation/DrylStepper.razor`, `.razor.css` — aura on
  `.step-indicator`.
- `Components/Inputs/DrylInputOtp.razor`, `.razor.css` — walking light.
- Specs: `E3 AI/F7 DrylAuraElements.md`, `E7 Feedback/F3`, `F4`, `F5`,
  `E8 Inputs/F9`, `F10`, `E10 Navigation/F2`, `F3`; first specs for
  `DrylRating`, `DrylSlider`, `DrylInputOtp`, with their lines removed from
  `scripts/spec-coverage-baseline.json`.
- Tests: `tests/DRYL.Components.Tests/AiFramelessTests.cs`; existing tests that
  expect the frame on these components.
- `CHANGELOG.md` joins `[3.17.0]`; `<Version>` stays (`REL-01`). Idea → Adopted.

Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
`node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`,
`node scripts/check-motion-tokens.mjs`; on the docs website, both color modes,
by eye: each of the eight components in `Active`, `Thinking`, `Streaming`,
`Generated` and `Proposed`, no frame on the six, no layout shift when the state
changes, and `DrylTab`, `DrylTimelineItem` and an input unchanged.

## Task 2 — Website (DRYL.Website)

Files: the `AiMode` examples of the eight components where they describe the
frame or lack a state (`Progress`, `Skeleton`, `Spinner`, `Rating`, `Slider`,
`Radio`, `Stepper`, `InputOtp`).
Verify: `dotnet build`, `dotnet test DRYL.Website.slnx`; the eight pages render
in both modes.
