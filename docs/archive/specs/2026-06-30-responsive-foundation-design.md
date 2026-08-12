# Responsive Foundation & Mobile Hardening — Design

**Date:** 2026-06-30
**Status:** Approved (brainstorm), pending implementation plan
**Scope:** New responsive layout primitives + a global safety layer + a staged audit/fix of existing components for mobile.

---

## 1. Problem & Goal

Several DRYL components are squeezed or clipped on phones. The most visible
offenders (from real screenshots of **DRYL.Website** at ~375px):

- **Cards** keep their desktop width and clip on the right — nothing inside
  wraps (badges `Done` / `Running`, trailing text get cut off). The single
  worst case.
- **AppBar / topbar** runs off the right edge — the `Ctrl+K` search pill and
  theme button are clipped.
- Demo wrappers (Preview/Code tabs) overflow horizontally.

**North star:** *If a consumer builds their UI exclusively with DRYL.Components,
the result should be responsive automatically* — without the consumer writing a
single media query.

Today there is **no systematic responsive foundation**: no breakpoint tokens,
no responsive props on `DrylStack`, no `DrylGrid`, and only a few hand-written
media queries (1023/1024px) in the layout/sidebar CSS.

---

## 2. Mechanism Decision

**Container-query-first.** Components react to the width of *their own slot*, not
the viewport. A card in a narrow column re-stacks itself. This is what makes
"build with DRYL = automatically responsive" true. Viewport media queries are
used only where genuinely page-level (the existing root layout/sidebar already
does this).

**Global safety layer is defensive + additive** — a small set of base rules
catches ~80% of the "nothing shrinks / clips" bug class, *without* an aggressive
reset that could disturb existing UIs.

---

## 3. Foundation (Phase 0)

### 3.1 Breakpoint scale (new, in `dryl.css`)

A fixed, documented scale. The px values live **only** in `dryl.css` — this is
the one allowed literal exception (same as the existing 1024px media queries),
because `var()` cannot be used inside `@container` / `@media` query conditions.
Consumers never write px; they pick an enum.

| Name | Width  | Intended for                                  |
| ---- | ------ | --------------------------------------------- |
| `Sm` | 480px  | phone landscape / small slots                 |
| `Md` | 768px  | tablet                                        |
| `Lg` | 1024px | desktop (matches existing sidebar query)      |
| `Xl` | 1280px | large                                         |

- C# `enum Breakpoint { Sm, Md, Lg, Xl }`, consumed by every responsive prop.
- Documented in `DESIGN_TOKENS.md` as the breakpoint scale.

### 3.2 Container-query mechanics

- Utility class `.cq` → `container-type: inline-size`. Makes an element a query
  container. Layout primitives (`DrylGrid`, `DrylContainer`, `DrylStack`) set
  this internally so children adapt to the slot, not the screen.

### 3.3 Global safety layer (in `dryl.css`)

Defensive, additive rules that kill the "nothing shrinks / clips" bug class:

- `img, svg, video, canvas { max-width: 100%; height: auto; }`
- `min-width: 0` on the flex primitives (`.stack`, `.row`, `.col`, `.between`,
  `.glass-card`) so children may shrink instead of overflowing.
- Long words/tokens: `overflow-wrap: anywhere` on text surfaces.
- `pre` / code blocks: `overflow-x: auto` instead of bursting the page width.

All rules are structural only — no new color/motion, and they honour
`prefers-reduced-motion`.

**Expectation:** Phase 0 alone removes most clipping bugs (img / min-width /
word-wrap) before any per-component work.

---

## 4. Layout Primitives (Phase 1)

All follow `CONVENTIONS.md`: enums (not strings), merged `Class`,
`AdditionalAttributes`, XML docs. None invents a new color or animation.

### 4.1 `DrylGrid` — the core

Default mode is **auto-fit** (needs no breakpoints → maximally automatic):

```razor
<DrylGrid MinItemWidth="GridItemWidth.Md" Gap="StackGap.Lg">…</DrylGrid>
```

- `MinItemWidth` (enum, e.g. `Xs≈12rem / Sm≈16rem / Md≈20rem / Lg≈28rem`) →
  `grid-template-columns: repeat(auto-fit, minmax(min(<w>, 100%), 1fr))`. Items
  wrap automatically, never overflow.
- `Columns` (`int?`, optional) → **fixed** column count, but with a
  `min(…, 100%)` floor so it still collapses on a narrow slot instead of
  clipping.
- `Gap` (reuses the token-driven `StackGap` enum).
- Sets `.cq` internally. Reflow glides via `--dur-med`; items are
  `DrylReveal`-compatible.

### 4.2 `DrylContainer` — prevents edge-to-edge squeeze

```razor
<DrylContainer Size="ContainerSize.Lg">…</DrylContainer>
```

- `Size` (enum `Sm / Md / Lg / Xl / Full` = reading-width maxima) → centered,
  `max-width`.
- Responsive side padding via `clamp(var(--sp-4), 4vw, var(--sp-6))` — never
  edge-to-edge on a phone.

### 4.3 `DrylStack` responsive extension (additive, no new component)

- New prop `CollapseBelow` (`Breakpoint?`, default `null`). With
  `Direction="Horizontal"` and a value set, the stack flips to **vertical below
  that container width** — via a container-query utility class
  (`.stack-collapse-md` etc.), no inline `@media`.
- The direction change glides (satisfies rule 2.12). Existing API unchanged →
  **no breaking change**.

### 4.4 `DrylSpacer` — flexible spacer

- `Size` (`StackGap?`) → fixed gap from the `--sp` scale; without `Size` =
  `flex: 1` (pushes siblings apart, e.g. in a toolbar). Structural only.

### 4.5 `DrylAspectRatio` — ratio box for media/embeds

```razor
<DrylAspectRatio Ratio="AspectRatio.Video">…</DrylAspectRatio>
```

- `Ratio` (enum `Square / Video 16:9 / Photo 4:3 / Wide 21:9`, or `Custom` +
  `RatioValue` string) → `aspect-ratio` + `max-width: 100%`. Child fills via
  `object-fit: cover`.

### 4.6 Rule 2.12 (animation) note

`DrylContainer`, `DrylSpacer`, `DrylAspectRatio` are **purely structural**
primitives with no own state — their motion is delegated to children
(`DrylReveal`-compatible). `DrylGrid` (reflow glides) and `DrylStack` (collapse
glides) animate themselves. This will be called out explicitly in each PR
description as the exception permitted by rule 2.12.

---

## 5. Component Fix Batches (Phase 2)

Container-query-driven, batched by reported pain points. Each batch is a
self-contained unit (its own fixes + changelog entries) so we merge and verify
incrementally.

- **Batch A — Cards & Surfaces** (the "ganz schlimm" case): `glass-card`
  shrinks correctly; internal rows get `flex-wrap`; badges/slots wrap instead
  of clipping. Plus the website demo wrappers (Preview/Code tabs).
- **Batch B — AppBar / Topbar**: `topbar-start/center/end` collapse on narrow —
  search pill shrinks, optional items move behind a "more" menu or hide, theme /
  menu buttons stay visible. (Fixes screenshot 1.)
- **Batch C — Data**: `DrylTable` → horizontally scrollable wrapper on a narrow
  slot (instead of clipping); `DrylDescriptionList` → single column below `Md`;
  `DrylPagination` → compact variant (fewer buttons) when narrow.
- **Batch D — Navigation**: `DrylTabs` / `DrylStepper` → horizontal scroll /
  wrap; `DrylBreadcrumbs` → wrap / truncation.
- **Batch E — Overlays**: `DrylDialog` / `DrylPopover` / `DrylMenu` /
  `DrylToast` → width via `min(…, calc(100vw - sp))`, never leave the viewport.

---

## 6. Verification

Screenshots originate from **DRYL.Website**, so we verify there — component
pages at a ~375px viewport (Playwright is available).

**Acceptance per batch:** no horizontal overflow at 375px, nothing clipped on
the right, touch targets adequately sized.

---

## 7. Documentation & Catalog

Per `CLAUDE.md` §7:

- Each new component + each visible change → entry under `[Unreleased]` in
  `CHANGELOG.md`.
- Each new component registered in `ComponentCatalog` (DRYL.Website) so it
  appears on components.dryl.dev.
- No README component table.

Optionally: file the Phase 2 batches as DRYL Trello backlog cards.

---

## 8. Out of Scope (YAGNI)

- No light theme, no `prefers-color-scheme` (CLAUDE.md §2.2).
- No new colors, animations, durations, or easings.
- No viewport-based breakpoint props on components (container-query-first); the
  `Breakpoint` enum is used only for explicit collapse points (`DrylStack`) and
  page-level layout, not sprinkled across every component.
- No unrelated refactoring of components beyond what each fix batch requires.
