# DRYL — Design Tokens Reference

Every visual decision in DRYL points to one of the values below. **Never hardcode** — always reference the variable.

The source of truth is `dryl.css`. This file is the readable index.

---

## Colors

### Surfaces (the ground)
| Token            | Value                          | Use                                                |
| ---------------- | ------------------------------ | -------------------------------------------------- |
| `--ground`       | `#000000`                      | The page background. Pure black, always.           |
| `--bg-0`         | `#000000`                      | Lowest surface — alias of `--ground`.              |
| `--bg-1`         | `#07070a`                      | Subtle lift from ground (e.g. sidebar).            |
| `--bg-2`         | `#0c0c12`                      | Solid card background when transparency isn't OK.  |
| `--bg-3`         | `#14141c`                      | Highest opaque surface (modals on solid bg).       |

### Glass (the layers above)
| Token            | Value                          | Use                                                |
| ---------------- | ------------------------------ | -------------------------------------------------- |
| `--glass-1`      | `rgba(255,255,255,0.03)`       | Default card / panel.                              |
| `--glass-2`      | `rgba(255,255,255,0.05)`       | Slightly elevated (hover, secondary button).       |
| `--glass-3`      | `rgba(255,255,255,0.08)`       | Top elevation (active state, popover).             |
| `--glass-blur`   | `18px`                         | Default `backdrop-filter` blur radius.             |

### Lines (the edges)
| Token            | Value                          | Use                                                |
| ---------------- | ------------------------------ | -------------------------------------------------- |
| `--line-soft`    | `rgba(255,255,255,0.04)`       | Whisper-quiet dividers.                            |
| `--line`         | `rgba(255,255,255,0.06)`       | Default 1px border on glass surfaces.              |
| `--line-strong`  | `rgba(255,255,255,0.12)`       | Hover state, form fields, table headers.          |

### Foreground (the text)
| Token            | Value                          | Use                                                |
| ---------------- | ------------------------------ | -------------------------------------------------- |
| `--fg`           | `#f4f4f7`                      | Primary text, headings, active values.             |
| `--fg-muted`     | `rgba(244,244,247,0.62)`       | Body text, labels, secondary content.              |
| `--fg-dim`       | `rgba(244,244,247,0.38)`       | Captions, placeholders, helper text.               |
| `--fg-faint`     | `rgba(244,244,247,0.22)`       | Decorative-only — separators, metadata.            |

### Accents (the glow)
| Token            | Value                                                   | Use                                                |
| ---------------- | ------------------------------------------------------- | -------------------------------------------------- |
| `--accent-a`     | `#7c5cff` (violet)                                      | Primary accent color.                              |
| `--accent-b`     | `#22d3ee` (cyan)                                        | Secondary accent.                                  |
| `--accent`       | alias of `--accent-a`                                   | Use when you need "the accent" in singular form.   |
| `--accent-grad`  | `linear-gradient(135deg, var(--accent-a), var(--accent-b))` | Primary buttons, active indicators, brand mark. |
| `--accent-grad-r`| same, reversed                                          | Used sparingly for contrast against `--accent-grad`. |
| `--accent-soft`  | `rgba(124,92,255,0.18)`                                 | Soft accent fill (badges, alert icons).            |
| `--accent-line`  | `rgba(124,92,255,0.45)`                                 | Accent border, focus ring.                         |

### Semantic
| Token         | Value      | Use                          |
| ------------- | ---------- | ---------------------------- |
| `--success`   | `#34d399`  | Healthy, succeeded, online.  |
| `--warning`   | `#fbbf24`  | Throttled, pending, near-limit. |
| `--danger`    | `#f87171`  | Failed, destructive action.  |
| `--info`      | `#22d3ee`  | Informational, neutral status. (alias of `--accent-b`) |

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
| `.ai-aura-ring`     | `--ai-a` / `--ai-b`       | Rotating gradient border on AI-active surfaces.        |
| `.ai-aura-glow`     | `--ai-a` / `--ai-b`       | Breathing box-shadow behind AI-active surfaces.        |

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
await ThemeService.SetAccentAsync(new DrylAccent("#a855f7", "#06b6d4"));
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

**Do not use `linear` for anything except progress bars and loaders.**

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
| `AiState.Active`     | Slow rotating gradient ring, breathing accent glow.      | Persistent AI surface (chat panel, model-backed card).     |
| `AiState.Thinking`   | Fast pulse on ring and glow.                             | A tool call is in flight.                                  |
| `AiState.Streaming`  | Moderate pulse; content updates incrementally.           | Tokens are arriving from the model.                        |
| `AiState.Generated`  | One-shot accent wash + soft lift, then settles.          | Reveal moment immediately after a generation completes.    |

### CSS primitives

| Class               | Role                                                                                                  |
| ------------------- | ----------------------------------------------------------------------------------------------------- |
| `.ai-aura`          | Marker on the host. Sets `position: relative; isolation: isolate;` so the children below can layer.   |
| `.ai-aura-ring`     | Rotating conic-gradient border (`--accent-a` ↔ `--accent-b`). Driven by `@property --ai-aura-angle`.   |
| `.ai-aura-glow`     | Breathing box-shadow glow behind the host (`--accent-line` + violet/cyan rings).                       |
| `.ai-aura-wash`     | One-shot gradient sweep used only with `.ai-generated`. Internally clipped to the host's bounds.       |
| `.ai-thinking`      | Modifier on `.ai-aura`: ring 1.8s, glow 1.4s.                                                          |
| `.ai-streaming`     | Modifier on `.ai-aura`: ring 3s, glow 2.6s.                                                            |
| `.ai-generated`     | Modifier on `.ai-aura`: triggers the wash + a one-shot lift on the host.                               |
| `.ai-indicator`     | Standalone status pill (`DrylAiIndicator`) — pulsing sparkle + shimmer sweep, adapts speed to state.   |

### Custom property

| Token                | Type      | Initial | Use                                                       |
| -------------------- | --------- | ------- | --------------------------------------------------------- |
| `--ai-aura-angle`    | `<angle>` | `0deg`  | Registered via `@property`. Animated to rotate the ring's conic-gradient without rotating its bounding box. |

### Durations

AI mode is the one place where ambient (looping) animations exceed the standard `--dur-fast / --dur-med / --dur-slow` scale — the same way `.aurora` drifts over 22 seconds. These long, continuous values are intentional:

| Animation        | Duration | Easing           |
| ---------------- | -------- | ---------------- |
| Ring rotation (Active)    | 6s    | `linear`         |
| Ring rotation (Thinking)  | 1.8s  | `linear`         |
| Ring rotation (Streaming) | 3s    | `linear`         |
| Glow breathe (Active)     | 4s    | `--ease-in-out`  |
| Glow breathe (Thinking)   | 1.4s  | `--ease-in-out`  |
| Glow breathe (Streaming)  | 2.6s  | `--ease-in-out`  |
| Wash sweep (Generated)    | 900ms | `--ease-out`     |
| Indicator pulse / shimmer | 2.4s / 3.6s (Active), 1s / 1.4s (Thinking), 1.6s / 2.2s (Streaming) | `--ease-in-out` |

All animations are suppressed under `prefers-reduced-motion: reduce` — the ring stays as a static gradient with reduced opacity.

### Recipe

```html
<div class="glass-card ai-aura ai-thinking">
    <div class="ai-aura-ring"></div>
    <div class="ai-aura-glow"></div>
    <!-- content -->
</div>
```

For the Generated reveal, add a sibling `<div class="ai-aura-wash"></div>` while `.ai-generated` is active (re-key it to replay on every transition).

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
