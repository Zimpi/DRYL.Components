# I6 — implementation plan: the quiet Primary and the new Bold

Idea: `ideas/I6 A restrained Primary button, with the show on interaction.md` (Adopted)
Spec: `specs/E2 Actions/F1 DrylButton.md` (`State: Modified`)

One commit per task. Each task carries its own verification, run and read before
the task is called done.

---

## T1 — Specs: the `DrylSplitButton` default

The Product Owner decided that `DrylSplitButton.Variant` follows `Primary`, on the
grounds that the argument for `Secondary` no longer holds once `Primary` is no
longer the filled treatment.

- `specs/E2 Actions/F3 DrylSplitButton.md` — default changes to `Primary`; the
  "unsettled" paragraph is replaced by the decision and its reason;
  `State: Implemented` → `Modified`; the `Public API` table and any acceptance
  criterion naming the default are updated.
- `specs/E2 Actions/_Api.md` — the `Secondary` row and the defaults paragraph.

Verify: `node scripts/check-spec-coverage.mjs` still reports `14/127`;
`node scripts/check-harness-links.mjs` exits 0; no acceptance criterion in the
touched files carries a literal color, length, duration or easing (`SPEC-07`).

## T2 — CSS: redefine `.btn-primary`, add `.btn-bold`

`code/DRYL.Components/wwwroot/dryl.css`, the `.btn` family.

- `.btn-bold` is added carrying the rules `.btn-primary` holds today, unchanged —
  fill, label, border, inset highlight, resting glow, hover glow, hover lift.
- `.btn-primary` is rewritten: `--glass-2` surface, `--glass-fx-flow`, `--fg`
  label, a masked `::before` hairline from `--accent-grad`, no resting shadow.
  Its hover adds the `color-mix` tint over `transparent`, the glow from
  `--accent-a`, and the lift.
- The resting tint is declared as the same gradient shape at zero accent, so the
  hover interpolates two images rather than swapping one in.
- The sheen selector `.btn:not(.btn-ghost)::after` already covers both, since
  neither is `.btn-ghost`.

Verify: `node scripts/check-motion-tokens.mjs`;
`node scripts/check-light-sync.mjs`; `node scripts/validate-light-contrast.mjs`.

## T3 — Razor: the enum member and its class

`code/DRYL.Components/Components/Actions/DrylButton.razor`.

- `ButtonVariant` gains `Bold`, appended after `Danger` so existing ordinals hold.
- The `CssClass` switch maps `ButtonVariant.Bold` to `btn-bold`; the `_ =>`
  fallback stays `btn-primary`.
- The XML doc on `Variant` names the five values.

Verify: `dotnet build DRYL.slnx -c Release`.

## T4 — Call sites: which of the 15 want `Bold`

The fifteen `ButtonVariant.Primary` call sites across eleven files are read one by
one. The default assumption is that they stay `Primary`: the quiet variant is what
they should have had. A call site moves to `Bold` only where it is the single hero
action of a surface that has nothing else competing with it.

Expected outcome: the dialogs (`DrylConfirmDialog`, `DrylAlertDialog`, the three
agent ask-dialogs) stay `Primary` — a confirm button paired with a Ghost cancel is
exactly the case the quiet variant is designed for.

Verify: `dotnet build DRYL.slnx -c Release`; `dotnet test DRYL.slnx -c Release`.

### Outcome — no call site changes

All fifteen stay `ButtonVariant.Primary`. Thirteen are the case the quiet variant
was designed for: a confirm or submit paired with a Ghost cancel, a send button
inside a composer, a run button in the command palette's argument row, a submit in
a canvas form. One is a usage comment in `DrylButton.razor` itself, and one is the
`_ =>` fallback of `CanvasNodeView`'s string-to-variant mapping.

**`DrylCanvasDock`'s collapsed FAB was the one genuine `Bold` candidate and was
still left alone.** It floats over application content with nothing competing
against it, which is exactly the hero case — but it lives in
`code/DRYL.Components.Agents/Canvas/`, whose category `specs/E14 Agent Canvas/`
holds `_Api.md` and `_Interop.md` and no component spec at all. `SPEC-01` forbids
writing code for a component whose spec has not been read, and a spec that does not
exist cannot be read. Changing its appearance here would be exactly the drift the
rule exists to prevent.

Two facts make leaving it defensible rather than merely rule-abiding: the FAB
already carries `Ai="@DockAi"`, so in an AI state its edge is the aura's rather
than the variant's; and it sits inside a `DrylTooltip`, so its affordance does not
rest on fill alone. It is verified by eye in T7 and raised for a decision if it
reads weak.

A second thing found and deliberately not done: `CanvasNodeView` maps canvas-
authored action styles from strings — `"secondary"`, `"danger"`, and everything
else to `Primary`. A `"bold"` string would be the natural companion to this change,
but it is a behaviour addition to `DrylAiCanvas`'s action contract, not to
`DrylButton`, and belongs to that component's own spec.

## T5 — SplitButton: the default in code

`code/DRYL.Components/Components/Actions/DrylSplitButton.razor` — the `Variant`
parameter default and its XML doc.

Verify: `dotnet build DRYL.slnx -c Release`; `dotnet test DRYL.slnx -c Release`.

## T6 — Release bookkeeping

- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>` 2.23.0 → 2.24.0.
  MINOR: a new enum value is additive (`REL-01`).
- `CHANGELOG.md` — a `2.24.0` block. The appearance change to `Primary` is called
  out in its own right: no version number expresses "your buttons look different
  after this upgrade", which is exactly what `harness/patterns.md` now requires.

Verify: `dotnet build DRYL.slnx -c Release`.

## T7 — Close the loop

- `specs/E2 Actions/F1 DrylButton.md` → `State: Implemented`.
- `specs/E2 Actions/F3 DrylSplitButton.md` → `State: Implemented`.
- Both color modes checked by eye in the running site.

Verify: the full evidence set — `dotnet build DRYL.slnx -c Release`,
`dotnet test DRYL.slnx -c Release`, `node scripts/check-light-sync.mjs`,
`node scripts/validate-light-contrast.mjs`, `node scripts/check-harness-links.mjs`,
`node scripts/check-spec-coverage.mjs`, `node scripts/check-motion-tokens.mjs`.

---

## Out of scope

- `DRYL.Website` — separate repository. The demo page must gain a `Bold` example
  and the variants example must show five; that is a follow-up there.
- The pointer-positioned press ripple, rejected in I6.
- Any further variant, including the `Success`/`Warning` question raised and
  closed on 2026-08-17 without an idea.

## Process note

Worked inline, by the main agent, with no implementation subagents. CLAUDE.md
Stage 4 asks for the main agent by default and selective delegation where a task
benefits from isolated context or is a clearly bounded unit; these tasks are the
opposite of that — a stylesheet rewrite, one enum member, a call-site sweep and
the bookkeeping around them, all tightly coupled to one contract that already
exists. Recorded rather than left implicit.
