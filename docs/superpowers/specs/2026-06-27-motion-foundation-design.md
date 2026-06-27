# DRYL Motion Foundation — Design

**Date:** 2026-06-27
**Branch:** `feat/motion-foundation`
**Status:** Approved (user said "fang an")

## Goal

Give DRYL a small, reusable motion layer that delivers the "motion.dev feeling"
(weiche Enter/Exit-Übergänge, gleitende Layout-Bewegung, gestaffeltes Scroll-Reveal)
without breaking the library's hard rules: fixed token vocabulary, dark-only,
zero npm/JS dependencies, AI vocabulary untouched.

The three things plain CSS + Blazor cannot do today and that this layer adds:

1. **Exit animations** — Blazor removes a node instantly; there is no animate-out.
2. **Gliding layout movement** — an active indicator that slides between variable-width targets.
3. **Scroll-triggered staggered reveal** — IntersectionObserver-driven entrance.

## Architecture

One JS module, two new components, targeted retrofits. Everything reuses the
existing `window.dryl.*` conventions (attach/detach lifecycle, WeakMap state,
prerender-safe guards) and the existing motion tokens.

### 1. `window.dryl.motion` (new JS module, `wwwroot/js/dryl.js`)

- `onExit(el, ref)` / `clearExit(el)` — fires `OnExitFinished` via delegated
  `animationend` (generalises the existing toast exit pattern). Carries DrylPresence.
- `moveIndicator(container, opts)` — measures the active child's rect and sets the
  shared indicator element's `transform`/width; CSS transitions it on `--ease-spring`.
  Re-measures on resize via a ResizeObserver registered per container.
- `observe(el, ref, opts)` / `unobserve(el)` — IntersectionObserver that adds
  `.is-revealed` when an element scrolls into view (includes the `node.matches()`
  self-match fix learned in the portfolio). Carries DrylReveal.
- **Reduced motion:** every entry checks `matchMedia('(prefers-reduced-motion: reduce)')`
  and resolves instantly (no animation, indicator jumps, reveal shows immediately).
  This is an a11y feature, not a color-scheme toggle — does not violate "dark only".

### 2. `DrylPresence` (new, Surfaces)

Wrapper that defers unmount until the exit animation finishes (Blazor-AnimatePresence
for a single child).

```csharp
[Parameter] public bool Visible { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public PresenceTransition Transition { get; set; } = PresenceTransition.Fade;
// Fade | Scale | SlideUp | SlideDown | SlideLeft | SlideRight
[Parameter] public bool Appear { get; set; } = false;   // animate on first mount too
[Parameter] public EventCallback OnExited { get; set; } // fired after exit completes
```

Internal state machine: `Hidden → Entering → Shown → Exiting → Hidden`. While
`Exiting` the child keeps rendering with `.presence-exit`; `OnExitFinished` (from
`dryl.motion.onExit`) drops it and raises `OnExited`. CSS classes `.presence-*`
are token-driven (`--dur-med`, `--ease-spring`/`--ease-out`).

### 3. `DrylReveal` (new, Layout)

Scroll-triggered entrance via IntersectionObserver.

```csharp
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public RevealTransition Transition { get; set; } = RevealTransition.Rise;
// Fade | Rise | ScaleIn
[Parameter] public bool Stagger { get; set; } = false;  // stagger direct children
[Parameter] public bool Once { get; set; } = true;
[Parameter] public double Threshold { get; set; } = 0.15;
```

Renders a `.reveal reveal--{transition}` wrapper; on intersect JS adds `.is-revealed`.
Stagger applies `transition-delay: calc(var(--reveal-i) * var(--reveal-step))` per child.

## Retrofits

Scoped to clean cases only — no body-portal coordination (Menu/Popover deferred).

- **`DrylTabs`** — gliding shared indicator (`.tab-ink`) instead of the per-tab
  `::after`. Tabs are variable-width, so this needs `dryl.motion.moveIndicator`.
  New opt-out `[Parameter] bool AnimateIndicator = true` (default = new behaviour,
  non-breaking → CHANGELOG "Changed"). The component fulfils its own doc-comment,
  which already claims a "gliding gradient underline".
- **`DrylDialog`** — exit animation. The dialog is rendered inline by
  `DrylDialogProvider` (not portaled), so the provider gains an `IsExiting` flag per
  entry: on close it renders `.dialog--exit` + backdrop fade, waits for
  `dryl.motion.onExit`, then removes the entry. New `dialogOut` keyframe.
- **Opportunistic:** `DrylDrawer` (slide exit) and/or a dismissible `DrylAlert` if
  they retrofit cleanly without portal timing. Verified during implementation; if
  not clean, deferred to their own tickets.

**Already done / no work:** `DrylToast` (has exit), `DrylSegmentedControl`
(already glides via equal-width `translateX(index*100%)`), `DrylExpansion`
(grid-rows transition).

## Tokens

Reuse `--dur-med` / `--dur-slow` + `--ease-spring` / `--ease-out`. **One new token
proposed** (rule 2.1 — maintainer is the user, approved here):

- `--reveal-step: 60ms` — stagger step for `DrylReveal`, replacing the hardcoded
  `nth-child` delays currently in `.stagger`.

No new AI states, colors, gradients, or animations (rule 2.10). Presence/Reveal are
not AI surfaces → no `Ai` parameter.

## Accessibility

- `prefers-reduced-motion: reduce` neutralises every primitive (CSS `@media` block
  + JS short-circuit).
- Motion is decorative: focus order, keyboard nav, and ARIA on retrofitted
  components are unchanged. The gliding tab indicator is `aria-hidden`.

## Docs / definition of done (CLAUDE.md §7)

- CSS only token-driven in `dryl.css`; new `.presence-*`, `.reveal-*`, `.tab-ink`,
  `dialogOut` + a `@media (prefers-reduced-motion)` block.
- XML docs on every class and `[Parameter]`.
- Demo pages under `samples/` (or the website) for Presence, Reveal, gliding Tabs.
- `CHANGELOG.md` `[Unreleased]`: Added (DrylPresence, DrylReveal, `--reveal-step`,
  `dryl.motion`), Changed (DrylTabs), Fixed (DrylDialog exit).
- `README.md` component table rows for DrylPresence + DrylReveal.

## Out of scope (future tickets)

- Generic FLIP for list/table reordering.
- Menu/Popover exit animations (body-portal un-portal timing).
- Card→Dialog shared-element transition.
- Gesture springs (drag-to-dismiss, tilt, magnetic) — that's direction "B".
