# I7 A — A toggle modifier that is relative to its variant

Idea: `ideas/I7 Two loose ends from the quiet Primary.md`, point **A**.
Spec: `specs/E2 Actions/F1 DrylButton.md`.
Part **B** (the `DrylCanvasDock` FAB) is not in this plan; it is judged by eye
first and sequenced separately.

## What is wrong

`.btn--active` is written for a variant that carries nothing at rest. It sets
`border-color` and replaces `box-shadow` wholesale, at a specificity every
variant rule ties with and at a position later in the file, so it wins against
all of them. Against the three variants that do carry something at rest the
result is wrong in three different ways:

- **Primary** — gains a flat `--accent-line` ring inside its gradient hairline
  and a resting glow it deliberately does not have, so switched on is louder
  than its own hover and louder than the untoggled action next to it.
- **Bold** — loses its four-layer glow and its inset highlight and keeps only
  the thin accent ring, so switched on is *quieter* than switched off.
- **Danger** — is ringed and glowed in `--accent-*`, so the toggle overwrites
  the one thing the variant exists to signal.

## The shape chosen

The modifier becomes **relative to its variant**: one step above that variant's
rest, never above its own hover. The base rule stays exactly as it is and now
means what it always meant — the treatment for the variants with no resting
accent, `Secondary` and `Ghost`. Three variant-relative overrides join it.

Every override is gated behind `:not(:disabled)` so `.btn:disabled` keeps
stripping the glow at its own, lower specificity, rather than being outranked.
Each override needs a `:hover` companion, because a bare `.btn-x.btn--active`
ties with `.btn-x:hover` and, sitting later, would otherwise freeze the hover.

## Tasks

### T1 — CSS

File: `code/DRYL.Components/wwwroot/dryl.css`, the `.btn--active` block.

- Retarget the existing comment: the base treatment is for the variants with no
  resting accent.
- `.btn-primary.btn--active:not(:disabled)` — no ring, no glow; the accent tint
  washes in and stays, below the hover's strength. Plus a `:hover` companion
  restoring `.btn-primary:hover`'s tint and glow.
- `.btn-bold.btn--active:not(:disabled)` — keeps Bold's own glow, marks on with
  an inset ring in `--on-accent-line`. Plus a `:hover` companion carrying the
  heavier hover glow.
- `.btn-danger.btn--active:not(:disabled)` — the base rule's shape drawn from
  `--danger`. Plus a `:hover` companion.

Verify: `node scripts/check-light-sync.mjs`, `node scripts/validate-light-contrast.mjs`,
`node scripts/check-motion-tokens.mjs`.

### T2 — Spec

File: `specs/E2 Actions/F1 DrylButton.md`.

- `## Meta` → `State: Modified` while the work is open, back to `Implemented`
  at the end of the task.
- "Toggle state" — replace the single `distinguishable` criterion with the
  relative rule and one criterion per variant.
- "Appearance" — the two `active modifier` lines become variant-aware.
- The `DESIGN-01` debt paragraph names the active modifier's literal shadow;
  extend the count to the three overrides rather than leaving it understated.

Verify: `node scripts/check-harness-links.mjs`, `node scripts/check-spec-coverage.mjs`.

### T3 — Tests

File: `tests/DRYL.Components.Tests/`.

There is no test today that touches `Pressed` at all. The class-level
criteria are assertable in bUnit even though the CSS is not: the active
modifier class present exactly while `Pressed` is `true`, and `aria-pressed`
following it. Add those; the visual criteria stay eye-verified, as they are
for every other rule in `dryl.css`.

Verify: `dotnet test DRYL.slnx -c Release`.

### T4 — Release

- `<Version>` `2.24.0` → `2.24.1` in `code/DRYL.Components/DRYL.Components.csproj`
  (patch: appearance behind an existing parameter, no API change).
- `CHANGELOG.md` — a `### Fixed` entry under a new `[2.24.1]` block.

Verify: `dotnet build DRYL.slnx -c Release`, then both color modes by eye on the
docs site with a toggled button in each of the five variants.
