# DrylThemeProvider — Design Spec

**Date:** 2026-06-28
**Status:** Approved (design), proceeding to implementation plan
**Scope:** Customizable theming for DRYL.Components — accents + semantics — plus a holistic README rewrite.

---

## 1. Goal in one paragraph

Give DRYL consumers a first-class, strongly-typed way to re-skin the library's **accent and semantic colors** at app start and at runtime, without ever touching `dryl.css` or breaking the dark/glass identity. The differentiator from MudBlazor: instead of forcing the consumer to set ~30 colors by hand (and risk an incoherent result), the consumer sets a few **seeds** and the system **derives** the rest coherently in CSS via `color-mix()`. A dedicated **AI accent** can diverge from the brand accent, and runtime theme changes **glide** via registered `@property` color interpolation. The whole feature is opt-in and visually identical to today's default when no theme is supplied.

---

## 2. What is and isn't themeable

**Themeable (seeds):**

| Seed token            | Default                       | Drives (derived)                                                            |
| --------------------- | ----------------------------- | -------------------------------------------------------------------------- |
| `--accent-a`          | `#7c5cff`                     | `--accent-grad`, `--accent-soft`, `--accent-line`, `--glow-accent`, `--glow-soft`, body bg |
| `--accent-b`          | `#22d3ee`                     | `--accent-grad`, `--info`, `--glow-*`, body bg                             |
| `--ai-a`              | falls back to `var(--accent-a)` | `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`, `.ai-indicator`        |
| `--ai-b`              | falls back to `var(--accent-b)` | same as above                                                            |
| `--success`           | `#34d399`                     | direct                                                                     |
| `--warning`           | `#fbbf24`                     | direct                                                                     |
| `--danger`            | `#f87171`                     | direct                                                                     |
| `--info`              | `var(--accent-b)`             | direct (kept as alias unless overridden)                                   |

**Fixed (never themeable in this iteration):** the black ground (`--ground`, `--bg-*`), glass surfaces (`--glass-*`, `--glass-blur`), lines (`--line*`), foreground text ramp (`--fg*`), radii (`--r-*`), spacing (`--sp-*`), motion (`--dur-*`, `--ease-*`), typography. This protects the DRYL identity — only "the glow" and status colors move.

**AI accent is opt-in (rule 2.10):** `--ai-a/--ai-b` default to the brand accent, so absent an `AiAccent` the AI vocabulary is byte-for-byte unchanged.

---

## 3. The core innovation: CSS-side seed derivation

Derivation happens in `dryl.css`, not in C#. C# emits only the **seed** custom properties (~6–10); CSS computes everything downstream with `color-mix()`. This guarantees coherence (every derived value is mixed from the same seed) and keeps the C# surface tiny.

### 3.1 Derivation refactor in `dryl.css`

Today several derived values **hardcode** the default accent as literal `rgba(124,92,255,…)` / `rgba(34,211,238,…)`. These will not follow a theme. They are replaced with `color-mix()` expressions that are **pixel-identical** for the default Nebula theme (since `--accent-a` *is* `#7c5cff`) but now track the seed.

| Token / rule          | Before                                                   | After (derives from seed)                                                  |
| --------------------- | -------------------------------------------------------- | -------------------------------------------------------------------------- |
| `--accent-soft`       | `rgba(124,92,255,0.18)`                                  | `color-mix(in srgb, var(--accent-a) 18%, transparent)`                     |
| `--accent-line`       | `rgba(124,92,255,0.45)`                                  | `color-mix(in srgb, var(--accent-a) 45%, transparent)`                     |
| `--glow-accent`       | literal violet/cyan rgba                                 | `color-mix(...)` over `--accent-a` (35%) and `--accent-b` (18%)            |
| `--glow-soft`         | literal violet/cyan rgba                                 | `color-mix(...)` over `--accent-a` (18%) and `--accent-b` (8%)             |
| `body` radial bg      | literal violet/cyan rgba                                 | `color-mix(...)` over `--accent-a` / `--accent-b`                          |
| `.ai-aura-*` colors   | `var(--accent-a/-b)` directly                            | `var(--ai-a)` / `var(--ai-b)` (which default back to the brand accent)     |

`color-mix(in srgb, X p%, transparent)` is mathematically equal to the old `rgba` with alpha `p/100`, so the default render is unchanged. Each replacement is verified against its prior literal during implementation.

### 3.2 New seed declarations + `@property` registration

In `:root`, add `--ai-a: var(--accent-a); --ai-b: var(--accent-b);` (opt-in defaults). Register the four color seeds via `@property` so they **interpolate** on change:

```css
@property --accent-a { syntax: "<color>"; inherits: true; initial-value: #7c5cff; }
@property --accent-b { syntax: "<color>"; inherits: true; initial-value: #22d3ee; }
@property --ai-a     { syntax: "<color>"; inherits: true; initial-value: #7c5cff; }
@property --ai-b     { syntax: "<color>"; inherits: true; initial-value: #22d3ee; }
```

Because these are registered animatable custom properties, any property that references them through `color-mix()`/gradients is recomputed per frame when they change — so the entire derived chain glides for free.

### 3.3 Live transition

A single transition declaration drives the glide, gated on motion preference:

```css
@media (prefers-reduced-motion: no-preference) {
  :root { transition: --accent-a var(--dur-slow) var(--ease-in-out),
                      --accent-b var(--dur-slow) var(--ease-in-out),
                      --ai-a var(--dur-slow) var(--ease-in-out),
                      --ai-b var(--dur-slow) var(--ease-in-out); }
}
```

Under `prefers-reduced-motion: reduce` the seeds swap instantly. Semantic seeds (`--success` etc.) are **not** registered/animated — they are plain values that switch instantly; status colors changing mid-glide adds no value and avoids registering four more properties.

---

## 4. C# API

New namespace `DRYL.Components.Theming`.

### 4.1 Theme model (immutable records)

```csharp
/// A two-stop accent (gradient endpoints). Either stop may be used alone.
public readonly record struct DrylAccent(string A, string B);

/// Optional semantic overrides; null members fall back to DRYL defaults.
public sealed record DrylSemantic
{
    public string? Success { get; init; }
    public string? Warning { get; init; }
    public string? Danger  { get; init; }
    public string? Info    { get; init; }
}

/// A complete DRYL theme. Only seeds — everything else is derived in CSS.
public sealed record DrylTheme
{
    public required DrylAccent Accent { get; init; }
    /// When null, AI surfaces reuse the brand Accent.
    public DrylAccent? AiAccent { get; init; }
    public DrylSemantic? Semantic { get; init; }

    /// Emits ONLY the seed custom properties as a "--k:v;--k:v;" string.
    /// AiAccent omitted when null (CSS default keeps brand accent).
    /// Semantic members omitted when null.
    internal string ToCssVariables();
}
```

`ToCssVariables()` uses `FormattableString.Invariant` / `CultureInfo.InvariantCulture` for any formatting to avoid German-locale `0,5` corruption. (Colors are passed through as authored strings — hex or any valid CSS color — but the rule is applied defensively for any numeric work.)

### 4.2 Presets

```csharp
public static class DrylThemes
{
    public static DrylTheme Nebula  { get; }  // default = today's DRYL (violet→cyan)
    public static DrylTheme Ember   { get; }  // warm amber→red
    public static DrylTheme Verdant { get; }  // green→teal
    public static DrylTheme Mono    { get; }  // desaturated, near-monochrome

    public static DrylTheme Default => Nebula;
}
```

Each preset sets only seeds (and, for non-Nebula, a coherent semantic set where it improves harmony). Exact hex values chosen during implementation and sanity-checked against DRYL's contrast guidance (body text stays on the fixed `--fg*` ramp, so accents only need to read as glow — low risk).

### 4.3 Runtime service

```csharp
public interface IDrylThemeService
{
    DrylTheme Current { get; }
    Task SetThemeAsync(DrylTheme theme);
    Task SetAccentAsync(string a, string b);          // convenience
    event Func<Task>? OnThemeChanged;
}
```

`DrylThemeService` (scoped) holds `Current`, raises `OnThemeChanged`. Registered in `AddDrylComponents()` with `Nebula` as the initial value. The service does **not** itself touch the DOM — the provider component owns rendering/interop and subscribes to the event. This keeps the service free of `IJSRuntime` and unit-testable.

---

## 5. `DrylThemeProvider` component

Mirrors the existing `DrylToastProvider` / `DrylDialogProvider` root-provider pattern.

```razor
@* Place once in the root layout, above other providers *@
<DrylThemeProvider Theme="@DrylThemes.Nebula" />
```

Responsibilities:

1. **No-flash static path (prerender-safe):** on render, emit the current theme's seeds into an inline `<style>` targeting `:root` (`:root{ --accent-a:…; … }`). This means first paint is correct on Blazor Server prerender and on WASM with **no JS required** for the static case. The block re-renders when the theme changes.
2. **Runtime path:** after first render, when `OnThemeChanged` fires, call `dryl.theme.apply(varsString)` which sets the seeds on `document.documentElement.style`. Because the seeds are registered `@property` colors with a `:root` transition, the change glides. Inline-`<style>` and `documentElement` both target `:root`; `documentElement` inline style wins specificity for runtime, the `<style>` covers prerender/no-JS — they agree on value so there is no conflict.
3. **Parameters:** `Theme` (`DrylTheme`, default `DrylThemes.Default`). If both `Theme` is set and the service has a different `Current`, the service is the source of truth after first init; `Theme` seeds the service's initial value on `OnInitialized`.
4. **Lifecycle / interop safety:** subscribe to `OnThemeChanged` in `OnInitialized`, unsubscribe in `Dispose`. JS interop guarded with an `_attached` flag so static prerender never invokes JS (per the prerender-dispose rule). `JSDisconnectedException` swallowed like the toast provider.

### 5.1 JS interop (`wwwroot/js/dryl.js`)

Add a small namespaced block:

```js
window.dryl.theme = {
  apply(vars) {
    // vars: "--accent-a:#..;--accent-b:#..;..."
    const root = document.documentElement;
    vars.split(';').forEach(pair => {
      const i = pair.indexOf(':');
      if (i > 0) root.style.setProperty(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
    });
  }
};
```

No new dependencies; consistent with the zero-JS-dependency rule.

---

## 6. DI registration

`AddDrylComponents()` adds:

```csharp
services.AddScoped<IDrylThemeService, DrylThemeService>();
```

Updated XML doc mentions placing `<DrylThemeProvider />` in the root layout.

---

## 7. Accessibility & motion

- Live transition gated behind `@media (prefers-reduced-motion: no-preference)`; reduced-motion users get an instant swap, fully usable.
- Theme changes are purely visual — no focus, ARIA, or keyboard-order impact.
- The fixed `--fg*` text ramp is unchanged, so text contrast is unaffected by accent choice (accents are glow/border/indicator only — rule 2.4). This is why a Contrast-Guard was intentionally descoped: text never sits on an accent fill.

---

## 8. Documentation changes (CLAUDE.md §7 — mandatory)

1. **`DESIGN_TOKENS.md`** — new "Theming & Seed Derivation" section: the seed vs derived split, the `color-mix` derivations, the `@property`/transition mechanism, the AI-accent fallback.
2. **`THEMING.md`** (new) — consumer guide: use a preset, switch at runtime via `IDrylThemeService`, build a custom theme, the seed→derived model, AI accent, reduced-motion behavior.
3. **`CHANGELOG.md`** — `[Unreleased] → Added`: `DrylThemeProvider`, `IDrylThemeService`, `DrylTheme`/`DrylThemes` presets, new `--ai-a`/`--ai-b` tokens, seed-derivation refactor (note: visually identical default).
4. **`samples/Pages/DemoTheming.razor`** — live preset switcher + custom-accent picker demonstrating the glide; linked from the samples nav.
5. **`README.md`** — holistic rewrite (see §9).

---

## 9. Holistic README rewrite

The README is currently ~500 lines with a full ~90-row component table and long per-component deep-dives (Table, Dialog). The maintainer wants it **shorter and more focused**, with the giant component table **removed** (the live catalog at **components.dryl.dev** is the source of truth), and **customizability promoted** as a headline feature.

### Target structure (lean)

1. **Header** — badges, tagline ("Dark. Glassy. Alive — and AI-native."), one-line pitch, install command.
2. **Hero screenshot** — keep.
3. **Status line** — keep (1.0, frozen API).
4. **Why DRYL** — condensed bullets (AI-native, dark-only, glow accents, intentional motion, zero JS deps, accessible). Trim prose.
5. **Make it yours — Theming** *(new, prominent, near the top)* — seed-derivation pitch, a `DrylThemes.Ember` one-liner, runtime `IDrylThemeService` switch, the glide, "set a few seeds, stay coherent" vs hand-tuning 30 colors. This is the new selling point.
6. **Quick start** — install / `AddDrylComponents()` / stylesheet link / providers (now incl. `<DrylThemeProvider />`) / first component.
7. **AI Mode** — keep but condense to the five-state table + one wiring snippet (it's the brand differentiator).
8. **Where to go deeper** — replaces the component table and the long Table/Dialog sections: a short paragraph pointing to **components.dryl.dev** for every component/variant/state, plus links to `DESIGN_TOKENS.md`, `THEMING.md`, `COMPONENT_PATTERNS.md`. Keep a one-line teaser that DRYL ships ~90 components across 8 categories incl. a dedicated Intelligence set — without enumerating them.
9. **Roadmap teaser** — one line: customizability deepens in future releases (theming is step one).
10. **Contributing / Support / Credits / License** — keep, lightly trimmed.

**Removed:** the full component table (§"What's in the box"), the long `DrylTable` walkthrough, the long `Dialog & DialogService` walkthrough (compressed to a mention + website link). **Net effect:** roughly half the length, theming surfaced high, website positioned as the live reference.

This rewrite touches only `README.md`; it does not change the §7.2 rule for *future* changes except that the component table no longer exists to maintain — `THEMING.md` and the website become the references. (Note for implementation: update CLAUDE.md §7.2's "component table" instruction to point at the website instead, so future agents don't try to re-add the table.)

---

## 10. Units & boundaries (for the plan)

| Unit                              | Responsibility                                          | Depends on                  |
| --------------------------------- | ------------------------------------------------------- | --------------------------- |
| `dryl.css` derivation refactor    | Seeds, `@property`, `color-mix` derivations, transition | nothing                     |
| `DrylTheme` / `DrylAccent` / `DrylSemantic` / `DrylThemes` | Theme model + presets + `ToCssVariables()` | nothing                |
| `IDrylThemeService` / `DrylThemeService` | Hold current theme, raise change event           | theme model                 |
| `DrylThemeProvider.razor`         | Render seeds (static + runtime), interop                | service, JS, theme model    |
| `dryl.theme` JS                   | Set seeds on `documentElement`                          | nothing                     |
| DI registration                   | Wire the service                                        | service                     |
| Docs (TOKENS, THEMING, CHANGELOG, README rewrite, sample) | Reference + promote          | all of the above            |

Each unit is independently testable: the model/presets via pure unit tests (`ToCssVariables()` output), the service via event-raise tests, the CSS via visual diff of the default theme (must be unchanged), the provider via bUnit render of the inline `<style>`.

---

## 11. Risks & decisions

- **Touching the core `dryl.css` glows/body.** Necessary for theming to propagate; mitigated by per-token equivalence checks (default theme must render identically). Explicitly called out so review verifies "no visual change at default".
- **`@property` + `color-mix` browser support.** Both are baseline in evergreen browsers by 2026; DRYL is dark-only/modern already (uses `backdrop-filter`, `@property --ai-aura-angle`). Acceptable.
- **Specificity of inline `<style>` vs `documentElement` style.** Both resolve to the same `:root`/value, so no conflict; documented in §5.
- **Semantic seeds not animated.** Deliberate (instant swap) to avoid registering four more `@property`s for no UX gain.
- **Descoped:** Contrast-Guard (text never sits on accent), full-palette/shape/motion theming (identity protection), scoped per-island themes (global + runtime chosen).
