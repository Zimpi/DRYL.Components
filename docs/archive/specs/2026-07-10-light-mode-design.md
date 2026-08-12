# Light Mode — Design Spec

**Date:** 2026-07-10
**Status:** Approved by maintainer (visual direction, API surface, and architecture validated in brainstorming session)
**Scope:** DRYL.Components (library) + DRYL.Website (docs site). DRYL.Portfolio is explicitly out of scope.
**Target version:** 2.0.0 (from 1.5.0 — the default appearance changes for existing consumers)

---

## 1. Goal

DRYL gains a light color mode as a **fully equal peer** of the dark mode — same identity, same quality bar, same theming power. The mode defaults to the **operating-system preference** (`prefers-color-scheme`), can be forced at runtime through `IDrylThemeService`, persists an explicit user choice, and glides between modes with the same animated transition the theme system already uses for accent changes.

All living documentation is reworded so the library reads as if it always had two modes — no "newly added light theme" framing anywhere except the CHANGELOG (which stays a factual version log).

## 2. Visual language — "Aurora Light"

The chosen direction mirrors the dark identity instead of inventing a second one: **light, glassy, alive.**

| Aspect | Dark (existing) | Light (new) |
| --- | --- | --- |
| Ground | Pure black `#000000` | Tinted near-white, `#f2f2f9` family — never pure white |
| Ambient body radials | Accent tints at 6–12 % alpha | Same radials at 12–16 % alpha, so the color life stays visible on light |
| Glass surfaces `--glass-1/2/3` | `rgba(255,255,255,.03/.05/.08)` | Tinted white glass `rgba(255,255,255,.55/.62/.72)`, same `--glass-blur: 18px` |
| Glass edge | Hairline `rgba(255,255,255,.06/.12)` | White edge `rgba(255,255,255,.65)` + white inset highlight; hairlines from dark blue-gray `rgba(18,22,40,.08/.14/.05)` for `--line/--line-strong/--line-soft` |
| Text `--fg*` | `#f4f4f7` at alpha 1/.62/.38/.22 | Near-black `#15151c` at the **same alpha steps** — contrast logic unchanged |
| Shadows | Black-heavy stacks | Soft, faintly accent-tinted shadows (`rgba(90,70,190,.10–.14)` class) |
| Glows | `--glow-accent`, `--glow-soft` | Same structure, reduced alpha |
| Accents | Seed-driven (`--accent-a/b`) | **Identical.** A `DrylTheme` works in both modes with zero consumer effort |
| Semantics | `#34d399` etc. | Darker, light-validated defaults (contrast ≥ 3:1 on light ground) |
| Charts | Dark-validated palette + derived series 1/2 (L 0.65) | Light-validated overrides; the derivation formula gets a light variant with an adjusted lightness band |
| `color-scheme` | `dark` | Switches with the mode (native controls, scrollbars, autofill) |
| Film grain | White noise, `opacity .4` | Re-tuned for light ground (darker grain and/or lower opacity — determined visually during implementation) |

Exact light values above are the approved starting points from the mockup; final values are tuned during implementation with the same validation rigor as the chart-family project (contrast checks scripted, not eyeballed).

## 3. Token architecture

### 3.1 Literal-to-token refactor (prerequisite)

`dryl.css` contains ~150 hardcoded color literals outside `:root` (white insets, sheen gradients, backdrops, scrollbar thumbs, code-block token colors, `rgba(0,0,0,…)` overlays), plus 5 scoped `.razor.css` files with literals. All of them implicitly assume a dark ground and **must be lifted to new semantic tokens first** (e.g. `--sheen`, `--inset-highlight`, `--backdrop`, `--scrollbar-thumb`, `--code-tok-*`, `--on-accent`). This is the largest single work item and brings the stylesheet in line with rule 2.1 regardless of mode.

### 3.2 Mode switching (approved: attribute-based token overrides)

- `:root` keeps the dark token set as the built-in default.
- The **light token set** (~45 tokens) applies through two selectors:
  1. `@media (prefers-color-scheme: light) { :root:not([data-dryl-mode="dark"]) { … } }` — the System default works **in pure CSS**: no JS, no FOUC, live response to OS changes, and it works even without a `DrylThemeProvider` in the app.
  2. `:root[data-dryl-mode="light"] { … }` — explicit forcing.
- The light block therefore exists twice. This is a deliberate trade-off (~45 lines), kept as one co-located section in `dryl.css` with a loud keep-in-sync comment.
- The neutral **color** tokens are registered via `@property` so a mode switch **glides** over `--dur-slow` / `--ease-in-out`, exactly like today's accent-theme glide. Composite tokens (shadow stacks, gradients) switch instantly; the color glide carries the perceived transition. Reduced-motion users get an instant swap (existing mechanism).

Rejected alternatives: CSS `light-dark()` (composite tokens can't use it → two mixed mechanisms; older enterprise browsers) and a second stylesheet (double maintenance, no glide, FOUC).

## 4. Mode API

### 4.1 `DrylColorMode`

New enum in `DRYL.Components.Theming`:

```csharp
public enum DrylColorMode { System, Dark, Light }
```

`System` is the default everywhere.

### 4.2 `IDrylThemeService` additions

```csharp
DrylColorMode CurrentMode { get; }            // starts as System
Task SetModeAsync(DrylColorMode mode);        // switch + notify
event Func<Task>? OnModeChanged;              // single-subscriber, like OnThemeChanged
```

`DrylTheme` itself stays **mode-agnostic** — it carries accent/AI/semantic/chart seeds that work in both modes. (If a consumer overrides `Semantic`/`Charts`, that override applies in both modes; per-mode seed overrides are out of scope — YAGNI.)

### 4.3 `DrylThemeProvider` additions

- New parameter `Mode` (`DrylColorMode`, default `System`) — the startup value, mirroring how `Theme` works today.
- On mode change: sets/removes `data-dryl-mode` on `document.documentElement` via a new `dryl.theme.applyMode` JS function; persists the choice.
- **Persistence:** localStorage key `dryl-color-mode`. Only explicit choices (`light`/`dark`) are stored; choosing `System` removes the key.
- **Pre-paint restore:** the provider renders a tiny inline `<script>` (alongside its existing inline `<style>`) that reads localStorage and sets the attribute during HTML parsing — before first paint, Blazor-Server-prerender-safe. No FOUC.
- Without a provider: the System default still works (pure CSS path). Only persistence and runtime switching require the provider — same "optional but recommended" contract the provider has today.
- Prerender/dispose safety follows the existing patterns (`_attached` flag, `JSDisconnectedException` guard).

## 5. `DrylColorModeToggle` (new component)

An animated icon button that cycles `System → Light → Dark`:

- SVG sun/moon/auto morph animated with `--dur-med` + `--ease-spring` (rule 2.12; enter/exit and state changes all transition).
- Internally wrapped in `DrylTooltip` (rule 2.11); `aria-label` mirrors the tooltip and updates with state; mode changes announced via `aria-live="polite"`.
- Keyboard reachable, `:focus-visible` accent ring, honours `prefers-reduced-motion`.
- Registered in the website `ComponentCatalog`; used in the docs-site header as the live demo of the feature.
- Parameters kept minimal: standard merged `Class`, optionally `Size` if the existing icon-button sizing pattern requires it. No AI parameter (rule 2.10 — not an AI surface).

## 6. Documentation rewording

Principle: living docs read as if DRYL always had two modes. No "new!", no migration framing outside the changelog.

- **CLAUDE.md** — rule 2.2 "Dark first, only dark" becomes "**Two modes, one identity**": both modes are driven exclusively by tokens; never write mode-specific literals in components; every new component is verified in both modes. The §4 "looking right" checklist gains a "reads correctly in both modes" item. Rules referencing "dark glass" identity are reworded mode-neutrally.
- **THEMING.md** — new mode chapter (System default, persistence, toggle, API); the "What's themeable" table updates (`Background`/`Surface` rows now point to the mode system instead of "No — by design").
- **DESIGN_TOKENS.md** — every neutral token documents both values (dark / light columns).
- **README.md, COMPONENT_PATTERNS.md, PACKAGE.md, website copy** — mode-neutral wording throughout.
- **Historical specs/plans** under `docs/superpowers/` are time-capsule documents and stay untouched.
- **CHANGELOG.md** — records the change factually under 2.0.0 (it is a version log, not marketing surface).

## 7. DRYL.Website

- `DrylColorModeToggle` in the site header.
- Theming docs page extended with the mode chapter; all dark-only phrasing removed.
- `ComponentCatalog` entry for `DrylColorModeToggle`.
- The site itself becomes the live showcase of both modes — its own custom CSS is audited for dark assumptions and lifted onto tokens where needed.

## 8. Versioning, testing, risks

### Versioning

- `DRYL.Components.csproj` → **2.0.0**. The default appearance changes from "always dark" to "system preference" — a visible behavior change for existing consumers on light-OS machines → MAJOR per §7.0. Changelog cut in the same commit that bumps the version.

### Testing

- **bUnit:** service (`SetModeAsync`, `CurrentMode`, event), provider (attribute application, persistence interop calls, inline script rendering, prerender safety), toggle component (cycle order, aria attributes, tooltip).
- **Contrast validation:** scripted checks for light-mode semantics and chart palette (same approach as the chart-family validation).
- **Visual verification:** `/verify` flow (docs website + Playwright) in **both modes**, desktop + 375 px, component-by-component during the literal refactor — not only at the end.

### Risks

- **Hidden dark assumptions:** 5,178 lines of `dryl.css` plus samples/website CSS contain literals whose intent (highlight vs. surface vs. overlay) must be judged one by one; a wrong token choice only shows up visually. Mitigation: componentwise visual verification in both modes during the refactor.
- **Duplicated light block drift:** mitigated by co-location + sync comment; a test can diff the two blocks' declarations.
- **Consumer breakage:** apps with their own dark-assuming chrome around DRYL will look mixed in light mode. Mitigated by the MAJOR bump and a THEMING.md note (consumers can pin `Mode="DrylColorMode.Dark"` for the old behavior — one line).

## 9. Out of scope

- DRYL.Portfolio (separate follow-up).
- Per-mode `DrylTheme` seed overrides.
- Any third mode (high-contrast, sepia, …).
- Rewriting historical specs/plans.
