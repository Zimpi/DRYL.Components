# DRYL — Theming Guide

DRYL ships with a **glass core rendered in two color modes** — a deep-dark and a luminous-light rendition of the same identity. Surfaces stay translucent, text colors stay on the `--fg*` scale, and radii and motion tokens are fixed; the mode swaps the neutral values underneath and follows the user's operating system by default. What you theme on top are the accent colors that make the library feel like yours: the violet-to-cyan gradient, its derived fills and glows, an optional separate AI accent, and the semantic status colors — all of which read correctly in both modes.

---

## Quick start

Place `DrylThemeProvider` once in your root layout (e.g. `MainLayout.razor`). That is the only change required to activate a non-default theme.

```razor
@* MainLayout.razor *@
<DrylThemeProvider Theme="DrylThemes.Ember" />

@Body
```

The provider renders a `<style>` block into `:root` on first paint (Blazor Server prerender-safe — no flash) and wires up the runtime-switch service automatically. Remove it entirely and the library falls back to the default Nebula palette.

---

## Color mode

DRYL follows the operating-system preference (`prefers-color-scheme`) out of the
box — no setup required, live response to OS changes, no flash of the wrong mode.
To start an app in a fixed mode, pass `Mode` to the provider (a startup value —
after startup, switch via the service):

```razor
<DrylThemeProvider Mode="DrylColorMode.Dark" />
```

Switch at runtime via the same service that switches themes:

```csharp
await ThemeService.SetModeAsync(DrylColorMode.Light);
```

An explicit choice is persisted (localStorage, key `dryl-color-mode`) and restored
before first paint on the next visit; `DrylColorMode.System` clears it and follows
the OS again. Drop `<DrylColorModeToggle />` into your app bar for a ready-made,
animated switcher (cycles System → Light → Dark).

Because the neutral tokens are registered `@property` colors, a mode switch **glides**
over `--dur-slow` exactly like an accent change — instant for reduced-motion users.
Accent themes are mode-independent: any `DrylTheme` looks right in both modes without
extra work. One deliberate exception to the swap: **markdown/chat code fences keep a
dark surface in both modes** (`--code-bg` / `--code-fg`); `DrylCodeBlock` follows the
mode with its token-mapped syntax colors.

---

## Built-in presets

| Name                  | Primary accent      | Secondary accent | Character                                   |
| --------------------- | ------------------- | ---------------- | ------------------------------------------- |
| `DrylThemes.Nebula`   | Violet `#7c5cff`    | Cyan `#22d3ee`   | Default. The DRYL brand palette.            |
| `DrylThemes.Ember`    | Amber `#f59e0b`     | Rose `#f43f5e`   | Warm, energetic — dashboards and alerts.    |
| `DrylThemes.Verdant`  | Emerald `#10b981`   | Teal `#14b8a6`   | Cool and calm — data-heavy, health, finance.|
| `DrylThemes.Mono`     | White `#f4f4f7`     | Slate `#94a3b8`  | Neutral, high-contrast — minimal products. |

Pass any preset as the `Theme` parameter of `DrylThemeProvider`, or hand it to `IDrylThemeService.SetThemeAsync` at runtime.

---

## Switch at runtime

Inject `IDrylThemeService` (registered by `AddDrylComponents()`) and call `SetThemeAsync` or `SetAccentAsync`. The change glides — because the seed tokens are registered `@property` values, every derived color transitions smoothly over `--dur-slow` (420 ms). Users with `prefers-reduced-motion: reduce` get an instant swap.

```csharp
@inject IDrylThemeService ThemeService

// Switch to a preset
await ThemeService.SetThemeAsync(DrylThemes.Ember);

// Or change only the accent, keeping everything else
await ThemeService.SetAccentAsync("#a855f7", "#06b6d4");
```

You can also pass an `AiAccent` to diverge the AI surfaces from the UI accent (see "What's themeable" below):

```csharp
await ThemeService.SetThemeAsync(new DrylTheme
{
    Accent   = new DrylAccent("#a855f7", "#06b6d4"),
    AiAccent = new DrylAccent("#3b82f6", "#8b5cf6"),
});
```

> **Security note — user-supplied colors.** Theme seed values are injected into a `<style>` block as-authored (e.g. `--accent-a:#a855f7`). The strings must be **developer-controlled** CSS color values (hex, `rgb()`, named colors, etc.). If you ever source an accent from end-user input — a settings panel, a query parameter, a database field — **validate it as a CSS color on the server before passing it to `SetAccentAsync` or `DrylTheme`**. An unvalidated string could be used to inject arbitrary CSS. DRYL does not perform this validation for you.

---

## Build your own

Construct a `DrylTheme` record with as few or as many seeds as you need. DRYL derives all other values automatically.

```csharp
var myTheme = new DrylTheme
{
    // Required: the two raw accent hues
    Accent = new DrylAccent("#a855f7", "#06b6d4"),

    // Optional: give AI surfaces a different hue family
    AiAccent = new DrylAccent("#3b82f6", "#8b5cf6"),

    // Optional: override semantic status colors
    Semantic = new DrylSemantic
    {
        Success = "#22c55e",
        Warning = "#eab308",
        Danger  = "#ef4444",
    },
};

// Or switch later
await ThemeService.SetThemeAsync(myTheme);
```

```razor
@* Apply at startup — place once in your root layout *@
<DrylThemeProvider Theme="myTheme" />
```

`DrylTheme` is a plain C# record — store it in a config file, load it from a database, or let users build it in a settings panel.

---

## What's themeable

| Aspect                      | Configurable? | How                                               |
| --------------------------- | ------------- | ------------------------------------------------- |
| Accent colors (`--accent-a`, `--accent-b`) | Yes | `DrylTheme.Accent`                 |
| AI accent (`--ai-a`, `--ai-b`)             | Yes (opt-in) | `DrylTheme.AiAccent`               |
| Semantic colors (`--success`, `--warning`, `--danger`) | Yes | `DrylTheme.Semantic`   |
| Color mode (dark / light / system)         | Yes | `DrylThemeProvider.Mode`, `SetModeAsync`, `DrylColorModeToggle` |
| Surface translucency (glass values)        | Per mode | Both modes ship tuned glass values; not freely configurable |
| Background (page / `--ground`)             | Per mode | Deep-dark or luminous-light ground — the mode decides |
| Foreground / text scale                    | Per mode | Both modes ship tuned contrast ratios              |
| Radii, spacing, shadows, motion            | No — by design | Use the design tokens as written   |

If you find yourself wanting to override something in the "No" column, reach for a CSS layer override in your own stylesheet rather than through the theme system — and know that it will not be supported.

---

## The seed → derived model

You provide a handful of hue seeds (`--accent-a`, `--accent-b`, and optionally `--ai-a`, `--ai-b`, plus the semantic status seeds). `dryl.css` derives every dependent value — `--accent-soft`, `--accent-line`, `--glow-accent`, `--glow-soft`, the body ambient glow, and the AI aura colors — through `color-mix()` against those seeds. This means that changing two numbers gives you a fully harmonious theme: every fill, border, glow ring and AI visual updates in step because they all point back to the same seeds, and there are no magic hardcoded values to hunt down and replace.

---

## Reduced motion

Theme and color-mode transitions (the animated glide between palettes) honour `prefers-reduced-motion: reduce`. When the operating system reports a reduced-motion preference, the `--dur-slow` transition on the `@property` seed variables collapses to `0s`, so the palette swap is instant. The component system is fully usable either way — motion is always decorative.
