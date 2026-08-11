# Design Rules

Binding visual rules for DRYL. Token reference: [`tokens.md`](tokens.md).
Component anatomy: [`patterns.md`](patterns.md). Consumer theming:
[`theming.md`](theming.md).

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. Gaps between number blocks are intentional — they leave room
for later rules without renumbering.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

### DESIGN-01 — Tokens, not literals

Status: **binding** | Enforced: **grep**

Every color, every padding, every radius, every shadow, every duration and
every easing curve must reference a CSS variable. The full list lives in
[`tokens.md`](tokens.md).

✅ `background: var(--glass-1);`
❌ `background: rgba(255,255,255,0.03);`

**Alpha-channel contexts are exempt.** In `mask`, `-webkit-mask`, `mask-image`
and `clip-path`, a color is not a color — only its alpha is read. `#fff` means
"keep this area", `transparent` means "cut it out". Those are stencils, not
design decisions: no token could replace them, and they are identical in both
color modes.

Check: `rg -n '#[0-9a-fA-F]{3,8}\b|rgba?\(' code/*/**/*.razor.css | rg -v 'mask|clip-path'`
returns nothing — **clean**.

### DESIGN-02 — Two modes, one identity

Status: **binding** | Enforced: **script**

DRYL renders in two color modes — dark and light — driven entirely by the
token system. The default follows the operating system
(`prefers-color-scheme`); apps and users can force a mode through
`DrylThemeProvider` / `IDrylThemeService`.

- Components never branch on the mode. They consume tokens; the mode swaps
  the token values underneath them.
- Never write a mode-assuming literal (`rgba(255,255,255,…)`, hardcoded
  grays) in component CSS. If a value must differ per mode, it becomes a
  token with both values in `dryl.css` — added to **both** LIGHT-TOKEN-SET
  copies (`node scripts/check-light-sync.mjs` must stay green). See
  [`theming.md`](theming.md) for the full seed → derived model and how the
  glide transition works.
- Every new component is verified in **both** modes before it ships (flip
  `data-dryl-mode` on `<html>` in devtools).

Check: `node scripts/check-light-sync.mjs` — currently **green**
(`LIGHT-TOKEN-SET copies are in sync.`, exit 0).

### DESIGN-03 — A missing value becomes a token, never an inline value

Status: **binding** | Enforced: **review**

If a value is missing from `dryl.css` / [`tokens.md`](tokens.md), do not
invent it — propose adding it to `dryl.css` as a new token and ask the
maintainer to review.

Check: the new value exists in `dryl.css` and is documented in
[`tokens.md`](tokens.md) (reviewer confirms both before merge).

### DESIGN-05 — Glass surfaces, not solid blocks

Status: **binding** | Enforced: **review**

Cards, panels, modals → translucent. Never paint a solid hex on a card
background.

Check: no card/panel surface carries an opaque hex or solid-color background
(reviewer check — no automated scan documented yet).

### DESIGN-06 — Frost is charged per surface

Status: **binding** | Enforced: **review**

Frost is charged per surface, so it goes only where it can be seen:

- **Floating over content** (topbar, sidebar, popover, menu, tooltip, toast,
  dialog) → `background: var(--panel-float)` (or `--panel-grad`) +
  `backdrop-filter: var(--glass-fx-float)`. Content sliding underneath is the
  point — which also means the fill has to stay translucent. A frosted panel
  at 0.95 opacity is a solid block wearing a filter it cannot show.
- **In the flow** (card, expansion panel, alert, secondary button) →
  `backdrop-filter: var(--glass-fx-flow)`. Behind it is the page's own smooth
  background; blurring something smooth is invisible (measured: 0.84 of 255)
  and cost multiples of the page's GPU draw.
- **Opaque background** (anything on `--bg-2`, `--panel-sticky`) → no
  `backdrop-filter` at all. A surface you cannot see through can never show
  one.

Check: floating surfaces use `--panel-float` + `--glass-fx-float`; in-flow
surfaces use `--glass-fx-flow`; opaque surfaces carry no `backdrop-filter` at
all (reviewer check).

### DESIGN-07 — Never hand-write `backdrop-filter: blur(...)`

Status: **binding** | Enforced: **grep**

Never write `backdrop-filter: blur(...)` on a new in-flow surface — use the
token (`--glass-fx-flow` / `--glass-fx-float`).

Check: `rg -n 'backdrop-filter:\s*blur\(' code/` returns hits only in
`dryl.css` — currently **6 pre-existing hits outside `dryl.css`**, across
three components (`DrylChat.razor.css:9-10`,
`DrylReconnectModal.razor.css:23-24`, `DrylValidationSummary.razor.css:9-10`),
see phase C.

### DESIGN-08 — Accents glow, never scream

Status: **binding** | Enforced: **review**

Saturated accent colors are only ever used as:

- Gradients (`var(--accent-grad)`)
- 1px borders (`var(--accent-line)`)
- Glow rings (`box-shadow` with low alpha)
- Tiny dots and indicators

Never as a full background fill of a large surface.

Check: accent color appears only as a gradient, a 1px border, a glow ring, or
a small indicator — never as the fill of a large surface (reviewer check).

### DESIGN-10 — Fixed motion vocabulary

Status: **binding** | Enforced: **script**

This rule governs **transitions and one-shot animations** — anything that
moves the UI from one state to another and then stops.

Three durations: `--dur-fast` (140ms), `--dur-med` (240ms), `--dur-slow`
(420ms). Three easings: `--ease-out`, `--ease-in-out`, `--ease-spring`.

Don't invent new ones. Don't use `linear`. Don't use durations under 100ms
(feels glitchy) or over 600ms (feels broken).

**A delay is a design value too.** An `animation-delay` or `transition-delay`
inside an `animation` / `transition` shorthand reads `--delay-short` (200ms,
a beat's offset so two things do not land at once) or `--delay-long` (800ms, a
hold before something retires itself). It sets the rhythm of a choreography and
is chosen by eye exactly as a duration is. Two exceptions, both of them values
nobody chose: `0s` says "no delay" rather than "this much delay", and a stagger
step multiplied by an index — `calc(var(--reveal-step) * var(--i))` — is
arithmetic, not rhythm.

**One duration sits outside the scale.** `--dur-choreo` (900ms) is for
multi-step one-shot choreography — an aura bloom, a comet retiring, a row
flash — where the 600ms ceiling would not produce a faster version of the
gesture but a different one. It is **not** for transitions, which stay on the
three-step scale; the discipline is in the scope comment beside the token, the
same construction `--ease-viscous` already uses.

**Continuous motion is a different thing and is not bound by the duration
scale.** An `infinite` animation — a spinner, a skeleton shimmer, a breathing
pulse, the ambient aurora drift — has a rhythm that *is* the effect, measured
in seconds rather than milliseconds. Applying the transition scale to it
breaks it: a spinner needs `linear`, because rotation eased with `--ease-out`
stutters once per revolution, and a shimmer at 420ms is a strobe rather than a
sign of waiting. For continuous motion:

- The duration is free, chosen for the rhythm the effect needs.
- `linear` is allowed, and is required for anything that rotates.
- The easing **tokens** still bind wherever easing is applied at all — write
  `var(--ease-in-out)`, never the bare `ease-in-out` keyword; they are not the
  same curve.
- `prefers-reduced-motion` binds unchanged (`UX-06` in [`uiux.md`](uiux.md)).

Check: `node scripts/check-motion-tokens.mjs` — **currently clean**, and the
green is the whole point of how it reads. The check splits every
`transition`/`animation` shorthand into its comma-separated segments and judges
each on its own, because a single declaration may mix a continuous animation
with a one-shot: `.ai-aura.ai-generated .ai-aura-glow` does exactly that. The
grep this replaced tested whole lines, so that one-shot hid behind its
neighbour's `infinite` — and a second violation, `tbl-row-ai-flash` at 1600ms,
hid behind nothing more than a line break, since the regex wanted `animation:`
and the literal on the same line. Both were invisible while the rule reported
"9 pre-existing hits", which is why a hit count is never evidence of a clean
codebase.

The nine known debts, plus those two, were retokenised on 2026-08-11; five
choreographies converged on `--dur-choreo` and their timing changed visibly, in
one case by 700ms. See `docs/2026-08-11-red-rule-triage.md` and
`ideas/I2 Motion tokens for choreography and delay.md`.

### DESIGN-11 — Every component is animated

Status: **binding** | Enforced: **review**

DRYL feels *alive*. **Every new component MUST be deliberately animated** —
never ship a component that just appears, snaps, or toggles with no
transition. Aim for the polish of [motion.dev](https://motion.dev): smooth,
physical, intentional.

Concretely, a new component must animate at least its relevant subset of:

- **Enter / exit** — appears and disappears with a transition, never
  instantly. Anything that mounts/unmounts conditionally (panels, overlays,
  list items, toasts) wraps in `DrylPresence` so it also animates *out*, not
  just in.
- **State changes** — hover, focus, active, selected, expanded, error →
  animated, not stepped (border-color, glow, transform).
- **Layout movement** — an active marker that moves between targets *glides*
  (use `dryl.motion.moveIndicator` / a shared indicator), it does not jump.
- **Reveal** — content-heavy or marketing surfaces use `DrylReveal` for
  staggered scroll-in where it fits.

If a component genuinely has nothing to animate, that is the rare exception —
say so explicitly in its PR description. The default is: it moves.

Check: enter/exit, state-change, layout-movement, reveal — whichever subset
applies to the component — is present (reviewer check); an explicit
exception is stated in the PR description when none apply.

### DESIGN-12 — Conditional mount/unmount wraps in `DrylPresence`

Status: **binding** | Enforced: **review**

Anything that mounts/unmounts conditionally (panels, overlays, list items,
toasts) wraps in `DrylPresence` so it also animates *out*, not just in.

Check: no `@if` around a visible surface without a `DrylPresence` wrapper
(reviewer check).

### DESIGN-13 — Reuse the shared motion primitives

Status: **default** | Enforced: **review**

Reuse the shared motion primitives (`DrylPresence`, `DrylReveal`,
`dryl.motion.*`, the `.presence-*` / `.reveal-*` / `.ai-aura*` classes). Do
not hand-roll a one-off animation when a primitive exists — extend the
primitive in `dryl.css` and ask the maintainer (same bar as `DESIGN-03`).

Check: no hand-rolled one-off animation exists where a shared primitive could
have been used (reviewer check).

---

## Does it look right?

If you're unsure whether a component "feels DRYL", check it against these:

- [ ] Background is translucent, not solid (`DESIGN-05`)
- [ ] Border is 1px and uses `var(--line)` or `var(--line-strong)` (`DESIGN-01`)
- [ ] Hover state changes border-color, lifts shadow, or activates a glow — never just darkens the background (`DESIGN-08`)
- [ ] Any animated property uses one of the three durations and one of the three easings (`DESIGN-10`)
- [ ] Accent color appears only in: a gradient, a 1px border, a glow ring, or a small indicator (`DESIGN-08`)
- [ ] Text on the surface uses `var(--fg)`, `var(--fg-muted)`, or `var(--fg-dim)` — never a hardcoded gray (`DESIGN-01`)
- [ ] Radii use `var(--r-xs|sm|md|lg|xl|pill)` — never an arbitrary px value (`DESIGN-01`)
- [ ] Padding uses `var(--sp-1..8)` — never an arbitrary px value (`DESIGN-01`)
- [ ] Component reads correctly in **both color modes** (flip `data-dryl-mode` on `<html>` in devtools) (`DESIGN-02`)

If 9/9 — ship it. If 7/9 — fix the two. If less — re-read this file.
