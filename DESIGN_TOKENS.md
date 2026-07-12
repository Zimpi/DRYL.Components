# DRYL — Design Tokens Reference

Every visual decision in DRYL points to one of the values below. **Never hardcode** — always reference the variable.

The source of truth is `dryl.css`. This file is the readable index.

---

## Color modes

DRYL renders in two color modes — **dark** and **light** — from one token system. The
default follows the operating system (`prefers-color-scheme`); an explicit mode is
forced by `data-dryl-mode="light|dark"` on `<html>` (set via `DrylThemeProvider` /
`IDrylThemeService.SetModeAsync`, persisted in localStorage as `dryl-color-mode`).

- Dark values live in `:root`; light values live in the **LIGHT-TOKEN-SET** block in
  `dryl.css`. That block exists **twice** (system media query + explicit attribute
  selector) and both copies must stay identical — `node scripts/check-light-sync.mjs`
  verifies it.
- The neutral tokens are registered `@property <color>` values, so a mode switch glides
  over `--dur-slow` like a theme change (instant under `prefers-reduced-motion`).
- Components never branch on the mode — they consume tokens. If a value must differ per
  mode, it becomes a token with both values in `dryl.css`; never a literal in component CSS.
- Light-palette contrast is guarded by `node scripts/validate-light-contrast.mjs`.

Tokens listed below with a single value are identical in both modes.

---

## Colors

### Surfaces (the ground)
| Token            | Dark                | Light               | Use                                                |
| ---------------- | ------------------- | ------------------- | -------------------------------------------------- |
| `--ground`       | `#000000`           | `#f2f2f9`           | The page background.                                |
| `--bg-0`         | `#000000`           | `#f2f2f9`           | Lowest surface — alias of `--ground`.              |
| `--bg-1`         | `#07070a`           | `#f5f5fa`           | Subtle lift from ground (e.g. sidebar).            |
| `--bg-2`         | `#0c0c12`           | `#f8f8fc`           | Solid card background when transparency isn't OK.  |
| `--bg-3`         | `#14141c`           | `#fbfbfe`           | Highest opaque surface (modals on solid bg).       |

### Glass (the layers above)
| Token            | Dark                       | Light                      | Use                                                |
| ---------------- | -------------------------- | -------------------------- | -------------------------------------------------- |
| `--glass-1`      | `rgba(255,255,255,0.03)`   | `rgba(255,255,255,0.55)`   | Default card / panel.                              |
| `--glass-2`      | `rgba(255,255,255,0.05)`   | `rgba(255,255,255,0.62)`   | Slightly elevated (hover, secondary button).       |
| `--glass-3`      | `rgba(255,255,255,0.08)`   | `rgba(255,255,255,0.72)`   | Top elevation (active state, popover).             |
| `--glass-blur`   | `18px`                     | (same)                     | Default `backdrop-filter` blur radius.             |

### Lines (the edges)
| Token            | Dark                       | Light                      | Use                                                |
| ---------------- | -------------------------- | -------------------------- | -------------------------------------------------- |
| `--line-soft`    | `rgba(255,255,255,0.04)`   | `rgba(18,22,40,0.05)`      | Whisper-quiet dividers.                            |
| `--line`         | `rgba(255,255,255,0.06)`   | `rgba(18,22,40,0.08)`      | Default 1px border on glass surfaces.              |
| `--line-strong`  | `rgba(255,255,255,0.12)`   | `rgba(18,22,40,0.14)`      | Hover state, form fields, table headers.          |
| `--line-hover`   | `rgba(255,255,255,0.18)`   | `rgba(18,22,40,0.24)`      | Border on hovered inputs/triggers.                 |

### Foreground (the text)
| Token            | Dark                       | Light                      | Use                                                |
| ---------------- | -------------------------- | -------------------------- | -------------------------------------------------- |
| `--fg`           | `#f4f4f7`                  | `#15151c`                  | Primary text, headings, active values.             |
| `--fg-muted`     | `rgba(244,244,247,0.62)`   | `rgba(21,21,28,0.62)`      | Body text, labels, secondary content.              |
| `--fg-dim`       | `rgba(244,244,247,0.38)`   | `rgba(21,21,28,0.38)`      | Captions, placeholders, helper text.               |
| `--fg-faint`     | `rgba(244,244,247,0.22)`   | `rgba(21,21,28,0.22)`      | Decorative-only — separators, metadata.            |

### Accents (the glow)
| Token            | Value                                                   | Use                                                |
| ---------------- | ------------------------------------------------------- | -------------------------------------------------- |
| `--accent-a`     | `#7c5cff` (violet)                                      | Primary accent color.                              |
| `--accent-b`     | `#22d3ee` (cyan)                                        | Secondary accent.                                  |
| `--accent`       | alias of `--accent-a`                                   | Use when you need "the accent" in singular form.   |
| `--accent-grad`  | `linear-gradient(135deg, var(--accent-a), var(--accent-b))` | Primary buttons, active indicators, brand mark. |
| `--accent-grad-r`| same, reversed                                          | Used sparingly for contrast against `--accent-grad`. |
| `--accent-soft`  | `color-mix(in srgb, var(--accent-a) 18%, transparent)` | Soft accent fill (badges, alert icons). Derived.   |
| `--accent-line`  | `color-mix(in srgb, var(--accent-a) 45%, transparent)` | Accent border, focus ring. Derived.                |

### Semantic
| Token         | Dark       | Light      | Use                          |
| ------------- | ---------- | ---------- | ---------------------------- |
| `--success`   | `#34d399`  | `#0e8a4d`  | Healthy, succeeded, online.  |
| `--warning`   | `#fbbf24`  | `#b45309`  | Throttled, pending, near-limit. |
| `--danger`    | `#f87171`  | `#dc2626`  | Failed, destructive action.  |
| `--info`      | `#22d3ee` (alias of `--accent-b`) | `#0e7490` | Informational, neutral status. |

### Chart series palette
| Token       | Dark (default theme)                                 | Light                                  | Use                                    |
| ----------- | ---------------------------------------------------- | -------------------------------------- | -------------------------------------- |
| `--chart-1` | `oklch(from var(--accent-a) 0.65 clamp(0.1, c, 0.19) h)` | same at `L 0.52`, chroma cap `0.17` | Series 1 — follows the theme's A seed. |
| `--chart-2` | `oklch(from var(--accent-b) 0.65 clamp(0.1, c, 0.19) h)` | same at `L 0.52`, chroma cap `0.17` | Series 2 — follows the theme's B seed. |
| `--chart-3` | `#bd7a12`                                            | `#96610e`                              | Series 3 (amber, fixed anchor).        |
| `--chart-4` | `#26a058`                                            | `#1d7f46`                              | Series 4 (green, fixed anchor).        |
| `--chart-5` | `#d6428e`                                            | `#b0316f`                              | Series 5 (magenta, fixed anchor).      |
| `--chart-6` | `#5583e3`                                            | `#3a63c4`                              | Series 6 (blue, fixed anchor).         |

Series 1/2 are **theme-following**: hue from the accent seeds, lightness snapped to
the validated band (oklch L 0.65) and chroma clamped to 0.1–0.19, so any theme stays
chart-legible without per-theme tuning. Slots 3–6 are fixed hue anchors. Themes whose
accent hue collides with an anchor (or brands needing an exact palette) override
individual slots via `DrylTheme.Charts` (`DrylChartPalette`) — the presets Ember,
Verdant and Mono already ship curated overrides. All six tokens are registered
`@property <color>` values: overrides glide on theme change, and the registered
initial values (`#8b7cf8` / `#0aa2b5` / …) double as the fallback palette in engines
without relative color syntax.

Fixed order, assigned in sequence, **never cycled** — series 7+ renders `--fg-dim`
(reads as "other"). Palettes are validated per mode (lightness band, chroma floor,
adjacent-pair CVD ΔE ≥ 12, contrast ≥ 3:1 against the mode's surface; the light run
is scripted in `scripts/validate-light-contrast.mjs`). Never use `--success` /
`--warning` / `--danger` as series colors — status is reserved.

### Effect tokens (mode-dependent surfaces & details)

These carry the small optical details that must read differently per mode. Defined in
`:root` (dark) with light overrides in the LIGHT-TOKEN-SET; "(same)" = mode-independent.

| Token                    | Dark                                | Light                        | Use                                            |
| ------------------------ | ----------------------------------- | ---------------------------- | ----------------------------------------------- |
| `--edge-hi`              | `rgba(255,255,255,0.06)`            | `rgba(255,255,255,0.85)`     | 1px inset top highlight on glass.               |
| `--edge-hi-strong`       | `rgba(255,255,255,0.18)`            | `rgba(255,255,255,0.95)`     | Stronger glass edge (active/raised).            |
| `--sheen-grad`           | white 0.04 → 0.015 gradient         | white 0.55 → 0.15 gradient   | Top sheen on cards/buttons.                     |
| `--sheen-grad-soft`      | white 0.02 → 0 gradient             | white 0.35 → 0 gradient      | Fainter sheen (rows, rails).                    |
| `--shimmer`              | `rgba(255,255,255,0.22)`            | `rgba(255,255,255,0.8)`      | Moving shine sweep mid-stop.                    |
| `--shimmer-strong`       | `rgba(255,255,255,0.4)`             | `rgba(255,255,255,0.95)`     | Strong shine (primary button).                  |
| `--hover-wash`           | `rgba(255,255,255,0.02)`            | `rgba(18,22,40,0.03)`        | Row/list hover fill.                            |
| `--press-wash`           | `rgba(0,0,0,0.25)`                  | `rgba(18,22,40,0.08)`        | Pressed/close-chip fill.                        |
| `--backdrop`             | `rgba(0,0,0,0.6)`                   | `rgba(26,28,48,0.35)`        | Modal/drawer scrim.                             |
| `--backdrop-soft`        | `rgba(0,0,0,0.4)`                   | `rgba(26,28,48,0.22)`        | Lighter scrim / translucent bars.               |
| `--bar-bg`               | `rgba(0,0,0,0.4)`                   | `rgba(252,252,255,0.72)`     | `DrylAppBar` (flat) surface — override for a custom bar tint. |
| `--scrollbar-thumb`      | `rgba(255,255,255,0.08)`            | `rgba(18,22,40,0.18)`        | Scrollbar thumb.                                |
| `--scrollbar-thumb-hover`| `rgba(255,255,255,0.16)`            | `rgba(18,22,40,0.3)`         | Scrollbar thumb hover.                          |
| `--on-accent`            | `#ffffff`                           | (same)                       | Text/icons on accent-gradient fills.            |
| `--on-accent-line`       | `rgba(255,255,255,0.12)`            | (same)                       | Border on accent fills.                         |
| `--on-accent-hi`         | `rgba(255,255,255,0.2)`             | (same)                       | Inset highlight on accent fills.                |
| `--knob`                 | `#ffffff`                           | (same)                       | Slider/switch knob fill.                        |
| `--accent-fg`            | `#d6cbff`                           | accent-a 72% → near-black mix| Accent-tinted text on glass.                    |
| `--accent-ico`           | `#c4b5fd`                           | accent-a 78% → near-black mix| Accent icon chips (alerts).                     |
| `--danger-fg`            | `#fca5a5`                           | `#b91c1c`                    | Danger text on glass.                           |
| `--success-hi` … `--info-hi` | exact bright literals            | seed → white mixes           | Bright gradient endpoints (progress bars). Dark stays pixel-exact; light derives from the mode's semantic seeds. |
| `--depth-edge`           | `rgba(255,255,255,0.18)`            | `rgba(255,255,255,0.9)`      | DepthGlass reference edge.                      |
| `--depth-edge-strong`    | `rgba(255,255,255,0.55)`            | `rgba(255,255,255,0.95)`     | DepthGlass strongest inset.                     |
| `--depth-shadow`         | `rgba(0,0,0,0.42)`                  | `rgba(28,24,70,0.16)`        | DepthGlass / dialog drop shadow.                |
| `--code-bg`              | `rgba(0,0,0,0.55)`                  | `#14141d`                    | Markdown/chat code-fence surface — **stays dark in both modes** (by design; `DrylCodeBlock` itself follows the mode via its token-mapped colors). |
| `--code-fg`              | `#f4f4f7`                           | (same)                       | Code-fence text (light-on-dark in both modes).  |
| `--grain-opacity`        | `0.4`                               | `0.25`                       | Film-grain overlay strength.                    |
| `--aurora-opacity`       | `0.85`                              | `0.5`                        | Aurora orb strength.                            |

Shadows and glows are also mode-tuned: `--shadow-sm/md/lg` swap their black stacks for
soft indigo-tinted ones in light (`rgba(28,24,70,…)`), and `--glow-accent`/`--glow-soft`
reduce their mix percentages. Exact values: `dryl.css` LIGHT-TOKEN-SET.

---

## Theming & Seed Derivation

DRYL's theming system is built on a small set of **seed variables**. You only set a few values; `dryl.css` derives everything else automatically via `color-mix()`.

### Seeds (what you set)

| Token        | Default          | What it represents                                              |
| ------------ | ---------------- | --------------------------------------------------------------- |
| `--accent-a` | `#7c5cff`        | Primary accent seed (violet by default).                        |
| `--accent-b` | `#22d3ee`        | Secondary accent seed (cyan by default).                        |
| `--ai-a`     | (= `--accent-a`) | AI accent primary seed. Defaults to the brand accent; set it to diverge AI surfaces from the UI accent. |
| `--ai-b`     | (= `--accent-b`) | AI accent secondary seed. Same opt-in divergence rule as `--ai-a`. |
| `--ai-core`  | `#eef3ff` (dark) | The aura comet's specular head — the hottest point. Mode-dependent: near-white on dark, a saturated accent core on light (white would be invisible on a light surface). |
| `--ai-strength` | `1` (dark) / `1.7` (light) | Aura-presence multiplier applied to the base saum + halo alphas, so translucent accents still read on a bright ground. |
| `--success`  | `#34d399`        | Semantic seed — success.                                        |
| `--warning`  | `#fbbf24`        | Semantic seed — warning.                                        |
| `--danger`   | `#f87171`        | Semantic seed — danger / destructive.                           |

### Derived (what `dryl.css` computes)

You never write these directly. They are generated from the seeds inside `dryl.css` using `color-mix()` so every derived value stays in harmony with whatever seeds the consumer provides:

| Derived token       | Derived from              | How it is used                                         |
| ------------------- | ------------------------- | ------------------------------------------------------ |
| `--accent-soft`     | `--accent-a` + alpha mix  | Soft accent fill (badges, focus ring background).      |
| `--accent-line`     | `--accent-a` + alpha mix  | Accent border and focus ring stroke.                   |
| `--glow-accent`     | `--accent-a` / `--accent-b` | Primary button hover glow, hero emphasis.            |
| `--glow-soft`       | `--accent-a` / `--accent-b` | Ambient lighting behind a section.                   |
| Body ambient glow   | `--accent-a` / `--accent-b` | The subtle background halo on the page root.         |
| `.ai-aura-ring`     | `--ai-a` / `--ai-b` / `--ai-core` | Even base saum + a travelling comet (Comet variant) or a flowing edge field (Aurora variant) on AI-active surfaces. |
| `.ai-aura-glow`     | `--ai-a` / `--ai-b`       | Breathing box-shadow halo behind AI-active surfaces.   |

### How seed changes transition

All seed tokens are registered as `@property` values with `syntax: "<color>"` in `dryl.css`. This makes them animatable: when the active theme changes (e.g. via `IDrylThemeService.SetThemeAsync`), every derived value transitions smoothly over `--dur-slow` (420 ms). Users with `prefers-reduced-motion: reduce` get an instant swap instead — the transition is gated by the same media query as every other DRYL animation.

### The AI accent opt-in

By default `--ai-a` and `--ai-b` resolve to the brand accent (`--accent-a` / `--accent-b`), so AI surfaces match the UI accent with no extra configuration. Setting `--ai-a` / `--ai-b` to different hues (e.g. a cooler blue-purple) lets an application give AI activity a visually distinct identity while keeping the rest of the accent palette untouched.

### How to set seeds

Always set seeds via `DrylTheme` / `DrylThemeProvider` or `IDrylThemeService` at runtime. Never edit `dryl.css` to hardcode a custom palette — the file is shared across all consumers and would be overwritten on the next package update.

```razor
@* Place once in your root layout *@
<DrylThemeProvider Theme="DrylThemes.Ember" />
```

```csharp
// Or switch at runtime from any component or service
await ThemeService.SetThemeAsync(DrylThemes.Verdant);
await ThemeService.SetAccentAsync("#a855f7", "#06b6d4");
```

---

## Typography

| Token         | Value                                                       |
| ------------- | ----------------------------------------------------------- |
| `--font-sans` | `'Inter', -apple-system, BlinkMacSystemFont, sans-serif`    |
| `--font-mono` | `'JetBrains Mono', ui-monospace, 'SF Mono', Menlo, monospace` |

### Type scale (no token — use these inline values, but consistently)

| Style      | Size  | Weight | Letter-spacing | Use                          |
| ---------- | ----- | ------ | -------------- | ---------------------------- |
| Display    | 56px  | 700    | -0.035em       | Hero headlines               |
| Title      | 32px  | 600    | -0.025em       | Page title (`h2`)            |
| Heading    | 20px  | 600    | -0.02em        | Section heading (`h3`)       |
| Subheading | 15px  | 600    | normal         | Card heading (`h4`)          |
| Body       | 14px  | 400    | normal         | Default text                 |
| Body Large | 17px  | 400    | normal         | Lead paragraph (`.lead`)     |
| Small      | 12px  | 400    | normal         | Helper text, metadata        |
| Eyebrow    | 11px  | 500    | 0.18em UPPER   | Section labels (`.eyebrow`)  |
| Mono       | 12.5px| 400    | normal         | Code, IDs, timestamps        |

---

## Spacing (4px scale)

| Token     | Value | Common use                                          |
| --------- | ----- | --------------------------------------------------- |
| `--sp-1`  | 4px   | Icon ↔ adjacent text, tightest gap                  |
| `--sp-2`  | 8px   | Item ↔ item in a tight row (e.g. button group)      |
| `--sp-3`  | 12px  | Default `row` and `col` gap                         |
| `--sp-4`  | 16px  | Card → card on a grid                               |
| `--sp-5`  | 24px  | Inside a card (`padding`)                           |
| `--sp-6`  | 32px  | Section → section                                   |
| `--sp-7`  | 48px  | Page padding (sides)                                |
| `--sp-8`  | 64px  | Hero blocks, major separations                      |

---

## Breakpoints (responsive scale)

DRYL is **container-query-first**: components react to the width of their own
slot, not the viewport. The breakpoint pixel values are intentionally **literal
and live only in `dryl.css`** — `var()` cannot be used inside `@container` /
`@media` query conditions. Consumers never write px; they pass the `Breakpoint`
enum (e.g. `DrylStack.CollapseBelow="Breakpoint.Md"`).

| Name | Width  | Intended for                                  |
| ---- | ------ | --------------------------------------------- |
| `Sm` | 480px  | phone landscape / small slots                 |
| `Md` | 768px  | tablet                                        |
| `Lg` | 1024px | desktop (matches the sidebar query)           |
| `Xl` | 1280px | large                                         |

**Mechanics & safety layer:**

- `.cq` → `container-type: inline-size`. Put it on a wrapper to make its inline
  size the query context for descendants. The layout primitives (`DrylGrid`,
  `DrylContainer`, responsive `DrylStack`) set this internally.
- A defensive **global safety layer** ships in `dryl.css`: `img/svg/video/canvas
  { max-width:100% }`, `min-width:0` on the flex primitives, and `overflow-wrap`
  on text surfaces — so content shrinks/wraps instead of clipping the page.

---

## Radii

| Token        | Value | Use                                            |
| ------------ | ----- | ---------------------------------------------- |
| `--r-xs`     | 6px   | Tiny chips, key shortcuts                      |
| `--r-sm`     | 10px  | Inputs (small), tabs, list items               |
| `--r-md`     | 14px  | Default buttons, inputs, code blocks           |
| `--r-lg`     | 20px  | Cards, panels, surfaces                        |
| `--r-xl`     | 28px  | Hero cards, large feature surfaces             |
| `--r-pill`   | 999px | Badges, toggles, progress bars                 |

---

## Shadows & glow

| Token            | Value                                                                     | Use                                          |
| ---------------- | ------------------------------------------------------------------------- | -------------------------------------------- |
| `--shadow-sm`    | `0 1px 2px rgba(0,0,0,0.4)`                                               | Subtle separation, sticky bars               |
| `--shadow-md`    | `0 8px 24px rgba(0,0,0,0.45), 0 2px 6px rgba(0,0,0,0.35)`                 | Cards, default panels                        |
| `--shadow-lg`    | `0 24px 64px rgba(0,0,0,0.55), 0 8px 16px rgba(0,0,0,0.35)`               | Modals, dropdowns                            |
| `--glow-accent`  | `0 0 0 1px var(--accent-line), 0 8px 32px rgba(124,92,255,0.35), 0 0 64px rgba(34,211,238,0.18)` | Primary button hover, active hero, focus emphasis |
| `--glow-soft`    | `0 0 60px rgba(124,92,255,0.18), 0 0 120px rgba(34,211,238,0.08)`         | Ambient lighting behind a section            |

---

## Motion

### Durations
| Token         | Value | Use                                          |
| ------------- | ----- | -------------------------------------------- |
| `--dur-fast`  | 140ms | Hover state, button press                    |
| `--dur-med`   | 240ms | Most transitions (background, color, border) |
| `--dur-slow`  | 420ms | Layout shifts, modal entry, large reveals    |

### Easing curves
| Token            | Value                          | Use                                |
| ---------------- | ------------------------------ | ---------------------------------- |
| `--ease-out`     | `cubic-bezier(0.16, 1, 0.3, 1)`| Exits, fade-ins, default reveal    |
| `--ease-in-out`  | `cubic-bezier(0.65, 0, 0.35, 1)`| Layout shifts, tab content swap   |
| `--ease-spring`  | `cubic-bezier(0.34, 1.56, 0.64, 1)`| Toggles, indicator pings        |
| `--ease-viscous` | `cubic-bezier(0.45, 0.05, 0.15, 1)`| **View-transition pseudo-elements only** — viscous morph settle |

**Do not use `linear` for anything except progress bars and loaders.**

**`--ease-viscous` is scoped to view transitions.** It models a viscous, syrup-like
settle — resists starting, then glides with weight, no overshoot. It exists because
none of the other three fit a morph: `--ease-spring` bounces (the opposite of
viscous), `--ease-out` is snappy, `--ease-in-out` is thin. Do not use it for
hover states, indicators or presence animations — those keep the three core curves.

### Reveal animations

Two utility classes drive every page-level entry animation. Use them; don't write per-component `@keyframes`.

| Class       | Behavior                                                                                                  | Use                                                                                                    |
| ----------- | --------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `.fade-in`  | 480ms `--ease-out`, opacity 0→1 + `translateY(6px)→0`                                                     | Apply to the **outer wrapper** of every page so content rises softly into view on navigation.          |
| `.stagger`  | 520ms `--ease-out` per child via `rise` keyframe, with delays `0, 60, 120 … 420ms` for children 1 through 8 | Apply to a **container of card/grid items** so they cascade in. Children 9+ all fire at delay 0 — keep groups small (≤ 8 visually significant items). |

```html
<div class="col fade-in">
    <h2>Components</h2>
    <div class="stagger" style="display: grid; grid-template-columns: ...">
        <Card />
        <Card />
        <Card />
    </div>
</div>
```

---

## AI Mode

AI is a first-class state of the UI in DRYL. The system has **one** ambient vocabulary — the same border, glow and reveal animations are reused across every AI-aware component, so a user learns the language once.

### The state enum

| State            | Visual signal                                            | When the consumer sets it                                  |
| ---------------- | -------------------------------------------------------- | ---------------------------------------------------------- |
| `AiState.None`       | No AI styling.                                           | Default. Surface renders normally.                         |
| `AiState.Active`     | Even saum + a slow travelling comet; gentle breathing halo. | Persistent AI surface (chat panel, model-backed card).  |
| `AiState.Thinking`   | Violet-dominant, fast comet + tight fast halo pulse.     | A tool call is in flight.                                  |
| `AiState.Streaming`  | Cyan-dominant comet + a directional sheen sweeping in reading direction. | Tokens are arriving from the model.       |
| `AiState.Generated`  | One-shot bloom + soft lift, then the comet retires to a calm afterglow. | Reveal moment right after generation completes.  |

The **variant** is orthogonal to the state — the `Aura` parameter (`AiAura`):
`Comet` (default, an even saum + a luminous travelling comet) or `Aurora` (a
soft, blurred, flowing edge field for dense AI pages). Both share the same
states, colour weighting and Generated reveal.

### CSS primitives

| Class               | Role                                                                                                  |
| ------------------- | ----------------------------------------------------------------------------------------------------- |
| `.ai-aura`          | Marker on the host. Sets `position: relative; isolation: isolate;` so the children below can layer.   |
| `.ai-aura--aurora`  | Variant modifier on the host: swaps the comet ring for the soft Aurora edge field.                    |
| `.ai-aura--out`     | Applied by the host during the graceful exit; dissolves the aura over `--dur-slow` instead of snapping. |
| `.ai-aura-ring`     | Even base saum (`--ai-a` ↔ `--ai-b`) + a travelling comet with an `--ai-core` specular head (its `::before`). Comet position is driven by `@property --ai-aura-angle`. |
| `.ai-aura-glow`     | Breathing box-shadow halo behind the host; its `::before` carries the Streaming sheen sweep.          |
| `.ai-aura-wash`     | One-shot Generated bloom; rendered only while `.ai-generated`.                                         |
| `.ai-thinking` / `.ai-streaming` / `.ai-generated` | State modifiers on `.ai-aura`: reset a handful of CSS vars (colour weighting, comet/halo speed, sheen). |
| `.ai-indicator`     | Standalone status pill (`DrylAiIndicator`) — pulsing sparkle + shimmer sweep, adapts speed to state.   |

The ring/glow/wash markup is emitted by the shared `<DrylAuraElements/>` component,
driven by an `AuraLifecycle` (which keeps the aura mounted for one `--dur-slow`
beat after leaving AI mode so it can fade out). The host-class combination is built
by `AiAuraCss.Append(classes, aura, variant)`.

### Custom property

| Token                | Type      | Initial | Use                                                       |
| -------------------- | --------- | ------- | --------------------------------------------------------- |
| `--ai-aura-angle`    | `<angle>` | `0deg`  | Registered via `@property`. Animated to travel the comet head around the perimeter without rotating its bounding box. |

### Durations

AI mode is the one place where ambient (looping) animations exceed the standard `--dur-fast / --dur-med / --dur-slow` scale — the same way `.aurora` drifts over 22 seconds. These long, continuous values are intentional (set per state via CSS vars):

| Animation        | Duration | Easing           |
| ---------------- | -------- | ---------------- |
| Comet orbit (Active / Thinking / Streaming / Generated) | 9s / 3.2s / 5s / 6s | `linear` |
| Halo breathe (Active / Thinking / Streaming / Generated) | 6s / 1.6s / 2.6s / 5s | `--ease-in-out` |
| Streaming sheen sweep     | 2.4s  | `--ease-in-out`  |
| Generated bloom + lift    | 900ms / 720ms | `--ease-out` |
| Comet afterglow retire    | 1.1s (800ms delay) | `--ease-out` |
| Graceful exit dissolve    | `--dur-slow` (420ms) | `--ease-out` |
| Indicator pulse / shimmer | 2.4s / 3.6s (Active), 1s / 1.4s (Thinking), 1.6s / 2.2s (Streaming) | `--ease-in-out` |

All animations are suppressed under `prefers-reduced-motion: reduce` — the aura stays as a static even saum with the travelling comet hidden.

### Recipe

Prefer the component wiring: on the host root add the classes from
`AiAuraCss.Append(...)` and drop `<DrylAuraElements Aura="_aura" GenTick="_genTick" />`
as the first child (see `COMPONENT_PATTERNS.md`). The raw markup it produces is:

```html
<div class="glass-card ai-aura ai-thinking">   <!-- + ai-aura--aurora for the Aurora variant -->
    <div class="ai-aura-ring"></div>
    <div class="ai-aura-glow"></div>
    <!-- <div class="ai-aura-wash"></div> only while .ai-generated -->
    <!-- content -->
</div>
```

> **Don't invent new AI states, animations, or colors.** If you need something that doesn't fit, propose extending the primitives above in `dryl.css` — same rule as every other token.

---

## Composition recipes

A few proven combinations — use these as starting points, don't recreate them.

### "Default card"
```css
background: var(--glass-1);
border: 1px solid var(--line);
border-radius: var(--r-lg);
backdrop-filter: blur(var(--glass-blur)) saturate(140%);
padding: var(--sp-5);
box-shadow: var(--shadow-md);
transition: border-color var(--dur-med) var(--ease-out),
            box-shadow var(--dur-med) var(--ease-out);
```

### "Card hover"
```css
border-color: var(--line-strong);
/* + optional radial glow behind, see .glass-card::after in dryl.css */
```

### "Primary button"
```css
background: var(--accent-grad);
color: white;
border: 1px solid rgba(255,255,255,0.12);
border-radius: var(--r-md);
box-shadow:
  0 1px 0 rgba(255,255,255,0.18) inset,
  0 0 0 1px var(--accent-line),
  0 6px 24px rgba(124,92,255,0.35),
  0 0 48px rgba(34,211,238,0.18);
```

### "Focus ring" (already on every input)
```css
border-color: var(--accent-line);
box-shadow: 0 0 0 4px var(--accent-soft), 0 0 24px rgba(124,92,255,0.18);
```

### "Status dot" (in a badge)
```css
width: 6px; height: 6px; border-radius: 50%;
background: currentColor;
box-shadow: 0 0 8px currentColor;
```
