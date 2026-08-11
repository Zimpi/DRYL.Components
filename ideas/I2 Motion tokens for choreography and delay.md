# Motion tokens for choreography and delay

## Meta
- **State:** Ready

## Problem

The triage of 2026-08-11 (`docs/2026-08-11-red-rule-triage.md`) narrowed `DESIGN-10` so
that continuous (`infinite`) motion is no longer bound by the three-duration scale. That
cleared 22 of 31 hits. Nine remain, and they cannot be cleared by rewording the rule —
they are genuine debt. But most of them cannot be fixed either, because the value they
need does not exist:

**Five durations sit above the 600 ms ceiling** (720, 900, 1100, 1300, 2000 ms). These
are one-shot choreographies, not continuous motion, so the exemption does not reach
them. Tokenising them at `--dur-slow` (420 ms) would not make them compliant, it would
make them different animations. `DESIGN-10` offers nothing between 420 ms and "free".

**Three delays are literals** (120, 220, 800 ms) and `DESIGN-10` does not say whether it
governs them at all. The rule names durations and easings. An `animation-delay` is
neither, yet it is just as much a design decision — it sets the rhythm of a
choreography, and 220 ms versus 500 ms before a toast catches its shine is exactly the
kind of value the rule exists to keep consistent.

Without both, `DESIGN-10` cannot go green, and every future choreography faces the same
dead end: the rule forbids the literal and offers no token, so the author writes the
literal anyway. That is how a red check stops being read.

## Solution Idea

**Add one duration token and two delay tokens, and state in `DESIGN-10` that delays are
governed.**

```css
--dur-choreo:   900ms;   /* multi-step one-shot choreography; not for transitions */
--delay-short:  200ms;   /* a beat's offset, so two things do not land at once */
--delay-long:   800ms;   /* a hold before something retires itself */
```

`--dur-choreo` is deliberately outside the transition scale and carries a scope comment,
exactly as `--ease-viscous` already does ("view-transition pseudo-elements only"). There
is precedent in `dryl.css` for a narrowly scoped fourth value; the discipline is in the
comment, not in refusing the value.

Two delay tokens rather than three: 120 ms and 220 ms are the same intent — a beat's
offset — and collapse to 200 ms. 800 ms is a different intent, a hold before something
removes itself, and keeps its own name. Names state the intent, matching
`--dur-fast/med/slow` rather than a numbered scale.

`DESIGN-10` gains one sentence: an `animation-delay` or `transition-delay` is a design
value and references a delay token.

**Resulting changes**, all in `code/DRYL.Components/wwwroot/dryl.css` unless noted:

| Selector / keyframe | Today | Becomes | Visible change |
|---|---|---|---|
| `.fade-in` | `fadeIn 480ms` | `var(--dur-slow)` | 60 ms quicker — imperceptible |
| `.stagger > *` | `rise 520ms` | `var(--dur-slow)` | 100 ms quicker |
| Toast shine | `toast-shine 1300ms … 220ms` | `var(--dur-choreo) … var(--delay-short)` | noticeably quicker; the shine sweep tightens |
| Toast icon pop | `var(--dur-slow) … 120ms` | `… var(--delay-short)` | 80 ms later — imperceptible |
| Progress bar | `transition: width 600ms` | `var(--dur-slow)` | 180 ms quicker; this is a *transition* and must come from the three-scale |
| `ai-generated-lift` | `720ms` | `var(--dur-choreo)` | 180 ms slower |
| `ai-aura-bloom` | `900ms` | `var(--dur-choreo)` | none |
| `ai-comet-retire` | `1100ms … 800ms` | `var(--dur-choreo) … var(--delay-long)` | 200 ms quicker, delay unchanged |
| `tbl-row-ai-flash` | `1600ms` | `var(--dur-choreo)` | 700 ms quicker; the row flash tightens noticeably |
| `DrylImage.razor.css` `img-sharpen` | `var(--img-blur-dur, 2000ms)` | `var(--img-blur-dur)` | none — see below |
| `drift-a/b/c`, `shimmer`, `skel` | bare `ease-in-out` | `var(--ease-in-out)` | a marginally different curve |

`DrylImage`'s `2000ms` fallback is dead code, not a design value. `--img-blur-dur` is set
inline whenever the animation runs — both are gated on `Ai == AiState.Streaming` — so
the fallback is unreachable. The `2000` is the C# default of the public `BlurDuration`
parameter, a consumer-facing knob, and stays where it is. The fallback is dropped.

## Scope

- **In scope:** the three new tokens and their values; the `DESIGN-10` sentence on
  delays; retokenising the nine `DESIGN-10` debts and the five bare `ease-in-out`
  keywords; documenting all three in `harness/tokens.md`.
- **Out of scope:** the three existing durations and three easings — unchanged.
  `--ease-viscous` — unchanged. Continuous motion, already settled by the triage.
  `prefers-reduced-motion` (`UX-06`) — unaffected. The `DESIGN-07` frost debts — a
  different rule, and component work.

## Impact

- **Harness:** three new tokens, a maintainer blocker under `DESIGN-03` and `DESIGN-10`
  — signed off 2026-08-11 (see `## Decisions`). `DESIGN-10` gains the delay sentence
  (scoped to the shorthand), the `--dur-choreo` scope note, and a repaired multi-line
  check; its `Check:` line drops from 9 pre-existing hits to 0 — against the *fixed*
  check, which sees eleven call sites rather than nine.
  `harness/tokens.md` documents all three. No new `AiState`, no new dependency.
- **Specs:** none exist yet. Eight of the nine debts live in `dryl.css`, which belongs to
  no component and therefore to no spec — so, unlike the `DESIGN-07` debts, this work is
  not phase-C material and does not wait for one. `DrylImage` is the only
  component-scoped change and it is a dead-code removal.
- **Public API:** none. No parameter, enum or event changes. `BlurDuration` keeps its
  `2000` default. Consumers overriding `--dur-*` in their own theme (`theming.md`) gain
  three more knobs and lose none.
- **Code:** `wwwroot/dryl.css` (token block plus nine call sites) and
  `Components/Data/DrylImage.razor.css` (one fallback). `<Version>` bump and `CHANGELOG`
  entry under `REL-01`/`REL-02` — MINOR, since new tokens are consumer-visible surface.
  `node scripts/check-light-sync.mjs` must stay green: the new tokens are mode-neutral
  and belong in the shared block, not in the LIGHT-TOKEN-SET copies (`DESIGN-02`).

## Decisions

- 2026-08-11: **A fourth duration is added rather than shortening the choreographies to
  `--dur-slow`.** Compressing a 900 ms aura bloom into 420 ms is a different gesture, not
  a compliant version of the same one. The alternative — leaving them as literals — was
  what made `DESIGN-10` red in the first place.
- 2026-08-11: **One `--dur-choreo` at 900 ms, and the five values converge on it.** Two
  tokens (`--dur-slower` + `--dur-choreo`) would have preserved every current value
  exactly, but doubles the growth of the motion vocabulary to avoid changes of 180–400 ms
  that only one of them (the toast shine) makes conspicuous. The vocabulary is the point
  of the rule.
- 2026-08-11: **`animation-delay` is governed by `DESIGN-10`.** A delay sets the rhythm
  of a choreography and is a design decision in the same sense a duration is. Leaving it
  free would have cleared three hits by declaring them out of scope, which is the move
  the triage exists to prevent.
- 2026-08-11: **Two delay tokens, named for intent.** 120 ms and 220 ms are one
  intent — a beat's offset — and collapse to `--delay-short: 200ms`. `--delay-long:
  800ms` is a hold before retirement. Numbered tokens (`--delay-1/2/3`) would have
  preserved every value but carry no intent, unlike `--dur-fast/med/slow`.
- 2026-08-11: **The conspicuous timing changes are reviewed in the browser before the
  commit** — toast shine, progress bar, `ai-generated-lift`, `ai-comet-retire` and
  `tbl-row-ai-flash` — in both colour modes, per the standing verification bar in
  `CLAUDE.md`.
- 2026-08-11: **`tbl-row-ai-flash` (1600 ms) converges on `--dur-choreo` too**, found
  while verifying the inventory before planning. It is a sixth over-600 ms one-shot that
  this document did not list, because the `DESIGN-10` check misses it (below). 700 ms is
  the largest single change in this work and joins the browser review. Raising
  `--dur-choreo` to meet it halfway was rejected for the same reason a fifth token was:
  the vocabulary is the point.
- 2026-08-11: **The `DESIGN-10` check has a hole and is fixed as part of this work.** Its
  regex requires `animation:` on the same line as the literal, so a wrapped multi-line
  declaration slips through — which is exactly why `tbl-row-ai-flash` and the second
  `ai-comet-retire` call site (`dryl.css:4291`) never appeared in the count of nine.
  Retokenising only the nine would have turned the check green while leaving real debt
  behind, which is the failure mode `CLAUDE.md` warns about when it says never to read a
  hit count as evidence of a clean codebase.
- 2026-08-11: **The delay sentence governs delays in the `animation`/`transition`
  shorthand only.** Read literally it would also bind the eight `.stagger >
  *:nth-child(n)` offsets (60–420 ms) and five `calc(var(--i) * 30ms)` staggers — but
  those are arithmetic progressions built from a step, not beats chosen by eye, and
  `--reveal-step: 60ms` already exists as the token for that. A step multiplied by an
  index stays free; `0ms` is never a design value. This keeps the change at the three
  delays the rule actually counts.

## Open Points

*(none — Definition of Ready met; Product Owner confirmed 2026-08-11)*

## Correction to the triage document

`docs/2026-08-11-red-rule-triage.md` states "six ambient animations use a bare
`ease-in-out`" and then names five (`drift-a/b/c`, `shimmer`, `skel`). Five is correct;
a grep across all CSS under `code/`, excluding `obj/`, finds no sixth. The remaining
bare keywords are `linear` on `infinite` rotations (`spin`, `dryl-spin`,
`reconnect-spin`, `ai-comet-spin`), which the corrected `DESIGN-10` explicitly permits.
