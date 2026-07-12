# Handoff — AI-Aura rollout to the remaining hosts + release

**Date:** 2026-07-12
**Branch to continue on:** `feat/ai-aura-redesign` (already has Phase 1 + 2a committed)
**You own:** finishing the `Aura` + graceful-exit wiring on the remaining ~38
AI-aware hosts, then the version bump + release.

---

## What is already done (do not redo)

- **The new aura look is already live on every host** via `dryl.css` (Phase 1).
  You are only adding two *behaviours* per host: the `Aura` variant switch and
  the graceful exit.
- Core primitive + design: see `docs/superpowers/specs/2026-07-12-ai-aura-redesign-design.md`.
- Shared building blocks you will reuse everywhere:
  - `AiAura` enum (`Comet` / `Aurora`) — `DRYL.Components/AiAura.cs`.
  - `AuraLifecycle` (`DRYL.Components/Ai/AuraLifecycle.cs`) — mounts the aura and
    fades it out over `--dur-slow` on the way to `None`.
  - `<DrylAuraElements Aura="_aura" GenTick="_genTick" />` — the ring/glow/wash markup.
  - `AiAuraCss.Append(classes, aura, variant)` — the host-class builder.
  - `AiScope.ResolveAura(Aura, Scope)` + `DrylAiAware.EffectiveAura` /
    `DrylAiScope.Aura` (scope propagation) — already in place.
- **Canonical per-host recipe:** `COMPONENT_PATTERNS.md` → "AI-aware components"
  (already rewritten to the new pattern). Follow it exactly.
- Reference hosts already migrated (copy these): `DrylCard`, `DrylMarkdown`,
  `DrylToolCall`, `DrylToast`, `DrylMessage`, `DrylChat`.

## The per-host change (three archetypes)

For **each** host below, apply the recipe. There are three shapes:

1. **`@inherits DrylAiAware`** (e.g. `DrylMessage`, `DrylChat`) — `Ai`, `Aura`,
   `EffectiveAi`, `EffectiveAura` come from the base. Add `@implements IDisposable`,
   an `AuraLifecycle _aura` field, `_aura.Sync(EffectiveAi, () => InvokeAsync(StateHasChanged))`
   in `OnParametersSet`, `Dispose() => _aura.Dispose()`, replace the inline
   `@if (EffectiveAi != None){…ring/glow/wash…}` block with `<DrylAuraElements … />`,
   and replace the state-class `switch` with `AiAuraCss.Append(parts, _aura, EffectiveAura)`.

2. **`InputBase<T>` family** (most `Inputs/*`) — cannot change base. Add inline:
   `[CascadingParameter] AiScope? Scope; [Parameter] AiAura? Aura;` and
   `AiAura EffectiveAura => AiScope.ResolveAura(Aura, Scope);` (they already have
   `Ai` + `EffectiveAi` inline — mirror it). Then the same lifecycle + markup +
   class-helper edits as (1). **Keep the existing `SetParametersAsync` override**
   (needed for `ValueExpression` outside `EditForm`).

3. **Explicit-state hosts** (`DrylToolCall` used `State`, not `Ai`) — keep the
   state param as-is, but still add a scope-resolved `Aura` (see `DrylToolCall`
   for the exact shape) so `DrylAiScope Aura=` switches the variant.

## Remaining hosts to wire

Confirm each actually renders `.ai-aura-ring`/`.ai-aura-glow` (grep) before editing —
a few matches may be comments or unrelated. Group by archetype as you go.

- **Surfaces:** `DrylDialog`, `DrylChatComposer`
- **Inputs (InputBase family):** `DrylInputText`, `DrylInputNumber`, `DrylInputPassword`,
  `DrylInputMask`, `DrylInputOtp`, `DrylTextarea`, `DrylSelect`, `DrylMultiSelect`,
  `DrylAutocomplete`, `DrylChipInput`, `DrylDatePicker`, `DrylTimePicker`, `DrylSlider`,
  `DrylRadioGroup`, `DrylRating`, `DrylFileUpload`
- **Data:** `DrylTable`, `DrylStat`, `DrylImage`, `DrylCodeBlock`, `DrylTimelineItem`,
  `DrylDonutChart`, `DrylLineChart`, `DrylBarChart`, `DrylAreaChart`
- **Layout/Nav:** `DrylExpansion`, `DrylStepper`, `DrylStep`, `DrylCommandPalette`
- **Feedback:** `DrylAlert`, `DrylEmptyState`, `DrylProgress`, `DrylSpinner`,
  `DrylSkeleton`, `DrylNotifications`, `DrylErrorBoundary`

### Special cases

- **`DrylButton` / `DrylTab` (and `DrylSplitButton`):** these do **not** render the
  ring/glow divs — they have their own compact `.btn.ai-aura::before` /
  `.tab.ai-aura::before` conic ring in `dryl.css`. Per the design, they keep their
  compact treatment and **ignore the `Aura` variant** (Aurora is meaningless at
  that size). Decision to confirm with the maintainer: either (a) leave them
  entirely as-is (simplest — no `Aura` param), or (b) accept an `Aura` param for
  API symmetry but keep the visual compact. Do **not** try to make Aurora work on them.
- **`DrylTable`, charts, radio-group, file-upload, inputs** have CSS adaptations in
  `dryl.css` (`.tbl-root .ai-aura-ring{inset:0}`, `.chart.ai-aura`, `.input-wrapper.ai-aura`
  radius, etc.). Those still apply — you only change the `.razor`, not those rules.
  Verify the comet/aurora reads on their specific radius/shape; if a host needs a
  different corner radius for the ring, that's a `dryl.css` tweak (keep it token-only,
  both LIGHT-TOKEN-SET copies in sync).
- **Charts** are ultra-wide sometimes; the conic comet is slightly non-uniform on
  extreme aspect ratios (noted, acceptable). If a chart looks bad, the fallback is
  Aurora or a subtler saum — do not invent a new mechanism.

## Verification (per batch, not just at the end)

1. `dotnet build DRYL.Components/DRYL.Components.csproj` → 0 errors.
2. `node scripts/check-light-sync.mjs` → green (only if you touched `dryl.css`).
3. Runtime via the **`verify` skill**: run the docs website, drive the relevant
   demo pages, screenshot **both variants × states × light + dark**. Confirm:
   the comet/aurora reads, `DrylAiScope Aura="Aurora"` switches the subtree, and
   leaving AI mode **dissolves** (no snap). A throwaway `/_aura-scratch` matrix
   page is a good harness (delete it before finishing — it must not ship).
4. `dotnet test tests/DRYL.Components.Tests` — add a bUnit test for
   `AiScope.ResolveAura` (explicit wins → scope → Comet) mirroring the existing
   `AiScope.Resolve` tests, if not already present.

## Release (you own this — it was deliberately left undone)

Only after the rollout is complete and verified:

1. **Register in `ComponentCatalog`** (DRYL.Website) — no new *component*, but if you
   surface an Aura toggle in a demo, wire that. No new catalog entry is required.
2. **`CHANGELOG.md`** — the `[Unreleased]` block already describes Phase 1 + 2a.
   Add your rollout entry (Changed: "`Aura` variant + graceful exit now on all
   AI-aware hosts"), then **cut the release**: rename `[Unreleased]` →
   `## [2.6.0] — <today>` and open a fresh empty `[Unreleased]`.
3. **`DRYL.Components/DRYL.Components.csproj`** — bump `<Version>` `2.5.0` → **`2.6.0`**
   (MINOR: additive `Aura` API + new visuals, no breaking change). Keep it in
   lockstep with the changelog release you just cut.
4. Commit, then merge `feat/ai-aura-redesign` → `main` (or open the PR). The push
   to `main` with the bumped version triggers `publish.yml` (build → test → pack →
   nuget.org → tag `v2.6.0` → GitHub Release). **Never tag or publish by hand.**

## Gotchas (learned this session)

- **`AiAura?` nullable = "inherit".** `Comet` (0) is a real default, not "unset", so
  `Aura` is nullable; resolve via `ResolveAura`. Only fixed/scope-less surfaces
  (e.g. `DrylToast`) use a non-nullable `AiAura Aura = Comet`.
- **Dispose the lifecycle.** Every wired host must `_aura.Dispose()` (add
  `@implements IDisposable`, or fold into an existing `DisposeAsync`).
- **Prerender-safe.** `AuraLifecycle` touches no JS; fine under static prerender.
  Keep any existing JS-dispose `_attached` guards untouched.
- **Don't clobber `Class`.** Keep the merged typed `Class` param; append aura
  classes via `AiAuraCss.Append`, don't rely on the `@attributes` splat.
- **Existing exit animations coexist.** `DrylToast.is-leaving`, `DrylDialog` exit,
  `DrylPresence` etc. are separate from the aura exit — don't remove them; the
  aura `--dur-slow` dissolve is additive.
- **`--ai-strength` / `--ai-core`** are mode tokens in **both** LIGHT-TOKEN-SET
  copies. Any new per-mode aura value goes in both copies (sync guard).
