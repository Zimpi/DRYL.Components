# Light Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DRYL renders in two equal color modes (dark + light "Aurora Light"), defaulting to the OS preference, switchable and persistable through the theme system, with all living docs reworded mode-neutrally.

**Architecture:** Dark stays the built-in token set in `:root`; a light token set applies via `@media (prefers-color-scheme: light)` (System, pure CSS) and `:root[data-dryl-mode="light"]` (explicit). ~150 hardcoded literals in `dryl.css` are lifted onto new semantic tokens first. `IDrylThemeService` gains `DrylColorMode` / `SetModeAsync`; `DrylThemeProvider` persists explicit choices in localStorage and restores them pre-paint. A new `DrylColorModeToggle` component cycles System → Light → Dark.

**Tech Stack:** Blazor (net8/9/10 multi-target), plain CSS custom properties + `@property`, vanilla JS in `dryl.js`, bUnit/xUnit tests, plain-node validation scripts (no npm deps).

**Spec:** `docs/superpowers/specs/2026-07-10-light-mode-design.md` — read it first.

## Global Constraints

- Branch: `feat/light-mode` (based on `feat/display-tools`). All repo paths below are relative to `c:\Users\janzi\Desktop\DRYL\DRYL.Components` unless prefixed `WEBSITE/` = `c:\Users\janzi\Desktop\DRYL\DRYL.Website`.
- Tokens, not literals (CLAUDE.md 2.1). Every value this plan introduces is a CSS variable.
- Motion vocabulary fixed: only `--dur-fast|med|slow`, `--ease-out|in-out|spring` (CLAUDE.md 2.5). Honour `prefers-reduced-motion`.
- No new runtime dependencies. Validation scripts are dev-only plain node (no `package.json`).
- Accents (`--accent-*`, `--ai-*`) are **mode-independent** — never fork them per mode.
- Literals on accent-gradient surfaces (white text/insets on the violet→cyan fill) are mode-independent → `--on-accent*` tokens defined once in `:root`, no light override.
- **Code surfaces stay dark in both modes** (deliberate design decision) → `--code-bg`/`--code-fg` tokens, syntax token colors unchanged.
- The light token set exists **twice** in `dryl.css` (media query + attribute selector). Both copies carry a `LIGHT-TOKEN-SET — copy 1/2` / `copy 2/2` marker comment and MUST stay identical (checked by `scripts/check-light-sync.mjs`, Task 1).
- Docs rewording: mode-neutral, "as if it was always this way". No "new!", no migration framing outside `CHANGELOG.md`. Historical files under `docs/superpowers/` are never edited.
- Test command: `dotnet test tests/DRYL.Components.Tests` (run from repo root). Build: `dotnet build`.
- Target version at the end: **2.0.0** (bumped only in Task 12, together with the changelog cut).
- German user communication; code, comments, and docs in English.

---

## Reference: the light token values

Single source for every task below. "Both" = defined once in `:root`, no light override.

### Overrides of existing tokens (go into the LIGHT-TOKEN-SET)

| Token | Dark (existing, stays in `:root`) | Light |
| --- | --- | --- |
| `color-scheme` | `dark` | `light` |
| `--ground` | `#000000` | `#f2f2f9` |
| `--bg-0` | `#000000` | `#f2f2f9` |
| `--bg-1` | `#07070a` | `#f5f5fa` |
| `--bg-2` | `#0c0c12` | `#f8f8fc` |
| `--bg-3` | `#14141c` | `#fbfbfe` |
| `--line` | `rgba(255,255,255,0.06)` | `rgba(18,22,40,0.08)` |
| `--line-strong` | `rgba(255,255,255,0.12)` | `rgba(18,22,40,0.14)` |
| `--line-soft` | `rgba(255,255,255,0.04)` | `rgba(18,22,40,0.05)` |
| `--glass-1` | `rgba(255,255,255,0.03)` | `rgba(255,255,255,0.55)` |
| `--glass-2` | `rgba(255,255,255,0.05)` | `rgba(255,255,255,0.62)` |
| `--glass-3` | `rgba(255,255,255,0.08)` | `rgba(255,255,255,0.72)` |
| `--fg` | `#f4f4f7` | `#15151c` |
| `--fg-muted` | `rgba(244,244,247,0.62)` | `rgba(21,21,28,0.62)` |
| `--fg-dim` | `rgba(244,244,247,0.38)` | `rgba(21,21,28,0.38)` |
| `--fg-faint` | `rgba(244,244,247,0.22)` | `rgba(21,21,28,0.22)` |
| `--success` | `#34d399` | `#0e8a4d` |
| `--warning` | `#fbbf24` | `#b45309` |
| `--danger` | `#f87171` | `#dc2626` |
| `--info` | `var(--accent-b)` | `#0e7490` |
| `--chart-1` | `oklch(from var(--accent-a) 0.65 clamp(0.1, c, 0.19) h)` | `oklch(from var(--accent-a) 0.52 clamp(0.1, c, 0.17) h)` |
| `--chart-2` | `oklch(from var(--accent-b) 0.65 clamp(0.1, c, 0.19) h)` | `oklch(from var(--accent-b) 0.52 clamp(0.1, c, 0.17) h)` |
| `--chart-3` | `#bd7a12` | `#96610e` |
| `--chart-4` | `#26a058` | `#1d7f46` |
| `--chart-5` | `#d6428e` | `#b0316f` |
| `--chart-6` | `#5583e3` | `#3a63c4` |
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.4)` | `0 1px 2px rgba(28,24,70,0.08)` |
| `--shadow-md` | `0 8px 24px rgba(0,0,0,0.45), 0 2px 6px rgba(0,0,0,0.35)` | `0 8px 24px rgba(28,24,70,0.10), 0 2px 6px rgba(28,24,70,0.06)` |
| `--shadow-lg` | `0 24px 64px rgba(0,0,0,0.55), 0 8px 16px rgba(0,0,0,0.35)` | `0 24px 64px rgba(28,24,70,0.15), 0 8px 16px rgba(28,24,70,0.08)` |
| `--glow-accent` | (existing 35 % / 18 % mixes) | same structure with `22%` / `12%` mixes |
| `--glow-soft` | (existing 18 % / 8 % mixes) | same structure with `12%` / `6%` mixes |
| `--grain-opacity` | `0.4` (new token, Task 1) | `0.25` |
| `--aurora-opacity` | `0.85` (new token, Task 1) | `0.5` |

### New tokens introduced by the literal sweep (Tasks 5–7)

| Token | Dark value | Light value |
| --- | --- | --- |
| `--edge-hi` | `rgba(255,255,255,0.06)` | `rgba(255,255,255,0.85)` |
| `--edge-hi-strong` | `rgba(255,255,255,0.18)` | `rgba(255,255,255,0.95)` |
| `--sheen-grad` | `linear-gradient(180deg, rgba(255,255,255,0.04), rgba(255,255,255,0.015))` | `linear-gradient(180deg, rgba(255,255,255,0.55), rgba(255,255,255,0.15))` |
| `--sheen-grad-soft` | `linear-gradient(180deg, rgba(255,255,255,0.02), rgba(255,255,255,0))` | `linear-gradient(180deg, rgba(255,255,255,0.35), rgba(255,255,255,0))` |
| `--shimmer` | `rgba(255,255,255,0.22)` | `rgba(255,255,255,0.8)` |
| `--shimmer-strong` | `rgba(255,255,255,0.4)` | `rgba(255,255,255,0.95)` |
| `--backdrop` | `rgba(0,0,0,0.6)` | `rgba(26,28,48,0.35)` |
| `--backdrop-soft` | `rgba(0,0,0,0.4)` | `rgba(26,28,48,0.22)` |
| `--hover-wash` | `rgba(255,255,255,0.02)` | `rgba(18,22,40,0.03)` |
| `--press-wash` | `rgba(0,0,0,0.25)` | `rgba(18,22,40,0.08)` |
| `--line-hover` | `rgba(255,255,255,0.18)` | `rgba(18,22,40,0.24)` |
| `--scrollbar-thumb` | `rgba(255,255,255,0.08)` | `rgba(18,22,40,0.18)` |
| `--scrollbar-thumb-hover` | `rgba(255,255,255,0.16)` | `rgba(18,22,40,0.3)` |
| `--accent-fg` | `#d6cbff` | `color-mix(in srgb, var(--accent-a) 72%, #10102a)` |
| `--danger-fg` | `#fca5a5` | `#b91c1c` |
| `--on-accent` | `#ffffff` | (both) |
| `--on-accent-line` | `rgba(255,255,255,0.12)` | (both) |
| `--on-accent-hi` | `rgba(255,255,255,0.2)` | (both) |
| `--knob` | `#ffffff` | (both) |
| `--code-bg` | `rgba(0,0,0,0.55)` | `#14141d` |
| `--code-fg` | `#f4f4f7` | (both) |
| `--success-hi` | `color-mix(in srgb, var(--success) 55%, white)` | (both — adapts through the seed) |
| `--warning-hi` | `color-mix(in srgb, var(--warning) 55%, white)` | (both) |
| `--danger-hi` | `color-mix(in srgb, var(--danger) 55%, white)` | (both) |
| `--info-hi` | `color-mix(in srgb, var(--info) 55%, white)` | (both) |
| `--depth-edge` | `rgba(255,255,255,0.18)` | `rgba(255,255,255,0.9)` |
| `--depth-shadow` | `rgba(0,0,0,0.42)` | `rgba(28,24,70,0.16)` |

Values are approved starting points; Task 9 validates contrast, Task 12 verifies visually — small tuning is expected and must be applied to **both** copies of the light set.

---

### Task 1: Mode CSS foundation in `dryl.css`

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css` (token/`@property` region, lines ~1–200)
- Create: `scripts/check-light-sync.mjs`

**Interfaces:**
- Produces: the `data-dryl-mode` attribute contract (`light` / `dark` on `<html>`, absent = System) that Tasks 3, 4, 10 rely on; the LIGHT-TOKEN-SET markers Tasks 5–9 append to; `--grain-opacity`, `--aurora-opacity` tokens.

- [ ] **Step 1: Register neutral tokens as `@property`** — insert after the existing `--chart-6` registration (line ~21):

```css
/* Neutral tokens are registered so a color-mode switch glides exactly like
   an accent-theme switch. Initial values mirror the built-in dark set. */
@property --ground   { syntax: "<color>"; inherits: true; initial-value: #000000; }
@property --bg-0     { syntax: "<color>"; inherits: true; initial-value: #000000; }
@property --bg-1     { syntax: "<color>"; inherits: true; initial-value: #07070a; }
@property --bg-2     { syntax: "<color>"; inherits: true; initial-value: #0c0c12; }
@property --bg-3     { syntax: "<color>"; inherits: true; initial-value: #14141c; }
@property --line        { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.06); }
@property --line-strong { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.12); }
@property --line-soft   { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.04); }
@property --glass-1  { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.03); }
@property --glass-2  { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.05); }
@property --glass-3  { syntax: "<color>"; inherits: true; initial-value: rgba(255,255,255,0.08); }
@property --fg       { syntax: "<color>"; inherits: true; initial-value: #f4f4f7; }
@property --fg-muted { syntax: "<color>"; inherits: true; initial-value: rgba(244,244,247,0.62); }
@property --fg-dim   { syntax: "<color>"; inherits: true; initial-value: rgba(244,244,247,0.38); }
@property --fg-faint { syntax: "<color>"; inherits: true; initial-value: rgba(244,244,247,0.22); }
```

- [ ] **Step 2: Extend the `:root` transition list** (currently lines ~138–151) with the same 15 properties, e.g. `--ground var(--dur-slow) var(--ease-in-out),` … keep the existing accent/ai/chart entries; one property per line.

- [ ] **Step 3: Tokenize grain + aurora opacity.** In `:root` add `--grain-opacity: 0.4;` and `--aurora-opacity: 0.85;`. Change `body::before` `opacity: var(--grain-opacity, 0.4)` → `opacity: var(--grain-opacity)`; change `.aurora` `opacity: 0.85` → `opacity: var(--aurora-opacity)`.

- [ ] **Step 4: Append the light set** at the end of the token region (directly after the `:root` transition media block). Content = every row of "Overrides of existing tokens" above, written twice:

```css
/* =============================================================
   LIGHT MODE — token overrides ("Aurora Light").
   ⚠ THE BLOCK BELOW EXISTS TWICE (system media query + explicit
   attribute). Keep both copies IDENTICAL — checked by
   scripts/check-light-sync.mjs.
   ============================================================= */
@media (prefers-color-scheme: light) {
  :root:not([data-dryl-mode="dark"]) {
    /* LIGHT-TOKEN-SET — copy 1/2 */
    color-scheme: light;
    --ground:        #f2f2f9;
    --bg-0:          #f2f2f9;
    --bg-1:          #f5f5fa;
    --bg-2:          #f8f8fc;
    --bg-3:          #fbfbfe;
    --line:          rgba(18, 22, 40, 0.08);
    --line-strong:   rgba(18, 22, 40, 0.14);
    --line-soft:     rgba(18, 22, 40, 0.05);
    --glass-1:       rgba(255, 255, 255, 0.55);
    --glass-2:       rgba(255, 255, 255, 0.62);
    --glass-3:       rgba(255, 255, 255, 0.72);
    --fg:            #15151c;
    --fg-muted:      rgba(21, 21, 28, 0.62);
    --fg-dim:        rgba(21, 21, 28, 0.38);
    --fg-faint:      rgba(21, 21, 28, 0.22);
    --success:       #0e8a4d;
    --warning:       #b45309;
    --danger:        #dc2626;
    --info:          #0e7490;
    --chart-1:       oklch(from var(--accent-a) 0.52 clamp(0.1, c, 0.17) h);
    --chart-2:       oklch(from var(--accent-b) 0.52 clamp(0.1, c, 0.17) h);
    --chart-3:       #96610e;
    --chart-4:       #1d7f46;
    --chart-5:       #b0316f;
    --chart-6:       #3a63c4;
    --shadow-sm:  0 1px 2px rgba(28, 24, 70, 0.08);
    --shadow-md:  0 8px 24px rgba(28, 24, 70, 0.10), 0 2px 6px rgba(28, 24, 70, 0.06);
    --shadow-lg:  0 24px 64px rgba(28, 24, 70, 0.15), 0 8px 16px rgba(28, 24, 70, 0.08);
    --glow-accent: 0 0 0 1px var(--accent-line),
                   0 8px 32px color-mix(in srgb, var(--accent-a) 22%, transparent),
                   0 0 64px color-mix(in srgb, var(--accent-b) 12%, transparent);
    --glow-soft:   0 0 60px color-mix(in srgb, var(--accent-a) 12%, transparent),
                   0 0 120px color-mix(in srgb, var(--accent-b) 6%, transparent);
    --grain-opacity: 0.25;
    --aurora-opacity: 0.5;
  }
}
:root[data-dryl-mode="light"] {
  /* LIGHT-TOKEN-SET — copy 2/2 */
  /* …identical declarations as copy 1/2… (write them out in full) */
}
```

The `copy 2/2` block must contain the full identical declaration list — no shortcuts.

- [ ] **Step 5: Write the sync checker** `scripts/check-light-sync.mjs`:

```js
// Verifies the two LIGHT-TOKEN-SET copies in dryl.css are identical.
import { readFileSync } from "node:fs";

const css = readFileSync(new URL("../DRYL.Components/wwwroot/dryl.css", import.meta.url), "utf8");
const blocks = [];
const re = /LIGHT-TOKEN-SET — copy [12]\/2 \*\//g;
let m;
while ((m = re.exec(css)) !== null) {
  const start = m.index + m[0].length;
  let depth = 1, i = start;
  while (i < css.length && depth > 0) {
    if (css[i] === "{") depth++;
    else if (css[i] === "}") depth--;
    i++;
  }
  const body = css.slice(start, i - 1);
  blocks.push(body.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\s+/g, " ").trim());
}
if (blocks.length !== 2) { console.error(`Expected 2 LIGHT-TOKEN-SET blocks, found ${blocks.length}`); process.exit(1); }
if (blocks[0] !== blocks[1]) { console.error("LIGHT-TOKEN-SET copies differ!"); process.exit(1); }
console.log("LIGHT-TOKEN-SET copies are in sync.");
```

- [ ] **Step 6: Run the checker** — `node scripts/check-light-sync.mjs` → expected: `LIGHT-TOKEN-SET copies are in sync.`

- [ ] **Step 7: Visual smoke test.** Build and launch the docs website (see `/verify` skill; typically `dotnet run` in `WEBSITE/`). In browser devtools run `document.documentElement.setAttribute('data-dryl-mode','light')` — the page ground, text, and glass must flip light with a smooth glide (dark literals elsewhere will still look wrong; that is Tasks 5–8's job). Then remove the attribute; with OS set to dark, dark returns.

- [ ] **Step 8: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css scripts/check-light-sync.mjs
git commit -m "feat(theme): light token set + registered neutral tokens with mode glide"
```

---

### Task 2: `DrylColorMode` enum + service mode API

**Files:**
- Create: `DRYL.Components/Theming/DrylColorMode.cs`
- Modify: `DRYL.Components/Theming/IDrylThemeService.cs`
- Modify: `DRYL.Components/Theming/DrylThemeService.cs`
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeServiceModeTests.cs` (create)

**Interfaces:**
- Produces: `enum DrylColorMode { System, Dark, Light }`; `DrylColorMode IDrylThemeService.CurrentMode { get; }`; `Task SetModeAsync(DrylColorMode mode)`; `event Func<Task>? OnModeChanged` — consumed by Tasks 3, 4, 10.

- [ ] **Step 1: Write failing tests** in `tests/DRYL.Components.Tests/Theming/DrylThemeServiceModeTests.cs`:

```csharp
using DRYL.Components.Theming;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeServiceModeTests
{
    [Fact]
    public void CurrentMode_defaults_to_System()
    {
        var svc = new DrylThemeService();
        Assert.Equal(DrylColorMode.System, svc.CurrentMode);
    }

    [Fact]
    public async Task SetModeAsync_updates_CurrentMode_and_raises_event()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnModeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetModeAsync(DrylColorMode.Light);

        Assert.Equal(DrylColorMode.Light, svc.CurrentMode);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetModeAsync_is_a_noop_for_the_current_mode()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnModeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetModeAsync(DrylColorMode.System);

        Assert.Equal(0, raised);
    }
}
```

- [ ] **Step 2: Run to verify failure** — `dotnet test tests/DRYL.Components.Tests --filter DrylThemeServiceModeTests` → expected: compile error (`DrylColorMode` not defined).

- [ ] **Step 3: Implement.** New file `DRYL.Components/Theming/DrylColorMode.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <summary>
/// The color mode DRYL renders in. <see cref="System"/> (the default) follows
/// the operating-system preference via <c>prefers-color-scheme</c>;
/// <see cref="Dark"/> and <see cref="Light"/> force a mode explicitly.
/// </summary>
public enum DrylColorMode
{
    /// <summary>Follow the operating-system preference (default).</summary>
    System,
    /// <summary>Force the dark rendition.</summary>
    Dark,
    /// <summary>Force the light rendition.</summary>
    Light,
}
```

`IDrylThemeService.cs` — add below `OnThemeChanged` (mirror its doc style):

```csharp
    /// <summary>The currently chosen color mode. Starts as <see cref="DrylColorMode.System"/>.</summary>
    DrylColorMode CurrentMode { get; }

    /// <summary>Switch the color mode and notify listeners. Animates if motion is allowed.</summary>
    Task SetModeAsync(DrylColorMode mode);

    /// <summary>Raised after <see cref="CurrentMode"/> changes. Single-subscriber, like <see cref="OnThemeChanged"/>.</summary>
    event Func<Task>? OnModeChanged;
```

`DrylThemeService.cs` — add:

```csharp
    /// <inheritdoc/>
    public DrylColorMode CurrentMode { get; private set; } = DrylColorMode.System;

    /// <inheritdoc/>
    public event Func<Task>? OnModeChanged;

    /// <inheritdoc/>
    public async Task SetModeAsync(DrylColorMode mode)
    {
        if (mode == CurrentMode) return;
        CurrentMode = mode;
        if (OnModeChanged is { } handler)
            await handler.Invoke();
    }
```

- [ ] **Step 4: Run tests** — `dotnet test tests/DRYL.Components.Tests --filter DrylThemeServiceModeTests` → expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Theming tests/DRYL.Components.Tests/Theming/DrylThemeServiceModeTests.cs
git commit -m "feat(theme): DrylColorMode enum + SetModeAsync on IDrylThemeService"
```

---

### Task 3: JS mode application + `DrylThemeProvider` persistence

**Files:**
- Modify: `DRYL.Components/wwwroot/js/dryl.js` (`window.dryl.theme` object, lines ~1186–1203)
- Modify: `DRYL.Components/Components/Surfaces/DrylThemeProvider.razor`
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs` (extend)

**Interfaces:**
- Consumes: `DrylColorMode`, `SetModeAsync`, `OnModeChanged` (Task 2); `data-dryl-mode` contract (Task 1).
- Produces: `dryl.theme.applyMode(mode, persist)` and `dryl.theme.storedMode()` JS functions; `DrylThemeProvider.Mode` parameter (`DrylColorMode`, default `System`); localStorage key `dryl-color-mode`.

- [ ] **Step 1: Write failing bUnit tests** — append to `DrylThemeProviderTests.cs`:

```csharp
    [Fact]
    public void Renders_prepaint_mode_restore_script()
    {
        var cut = Render<DrylThemeProvider>();

        Assert.Contains("dryl-color-mode", cut.Markup);          // localStorage key
        Assert.Contains("data-dryl-mode", cut.Markup);           // attribute contract
    }

    [Fact]
    public void Mode_parameter_is_baked_into_the_restore_script()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Mode, DrylColorMode.Light));

        Assert.Contains("var p='light'", cut.Markup);
    }

    [Fact]
    public async Task Runtime_mode_change_invokes_applyMode_with_persist()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>();
        var invocation = JSInterop.SetupVoid("dryl.theme.applyMode", "dark", true);

        await cut.InvokeAsync(() => svc.SetModeAsync(DrylColorMode.Dark));

        invocation.VerifyInvoke("dryl.theme.applyMode");
    }
```

- [ ] **Step 2: Run to verify failure** — `dotnet test tests/DRYL.Components.Tests --filter DrylThemeProviderTests` → expected: compile error (`Mode` parameter missing).

- [ ] **Step 3: Extend `dryl.js`.** Inside the `window.dryl.theme = { … }` object add two members after `apply`:

```js
    /* Explicit color-mode forcing. mode: 'light' | 'dark' | 'system'.
       'system' removes the attribute so the prefers-color-scheme media
       query in dryl.css takes over (live, no JS listener needed). */
    applyMode(mode, persist) {
        const root = document.documentElement;
        try {
            if (mode === 'light' || mode === 'dark') {
                root.setAttribute('data-dryl-mode', mode);
                if (persist) localStorage.setItem('dryl-color-mode', mode);
            } else {
                root.removeAttribute('data-dryl-mode');
                if (persist) localStorage.removeItem('dryl-color-mode');
            }
        } catch { /* storage unavailable (private mode etc.) — attribute still applied */ }
    },
    /* The persisted explicit choice, or null when the user follows System. */
    storedMode() {
        try {
            const m = localStorage.getItem('dryl-color-mode');
            return (m === 'light' || m === 'dark') ? m : null;
        } catch { return null; }
    }
```

- [ ] **Step 4: Extend `DrylThemeProvider.razor`.** Changes (keep every existing behavior):

```razor
<style>@($":root {{ {_vars} }}")</style>
@((MarkupString)_modeScript)
```

```csharp
    /// <summary>
    /// The color mode to start in. Defaults to <see cref="DrylColorMode.System"/>
    /// (follow the OS). A persisted explicit user choice — made earlier via
    /// <see cref="IDrylThemeService.SetModeAsync"/> — wins over this parameter.
    /// </summary>
    [Parameter] public DrylColorMode Mode { get; set; } = DrylColorMode.System;

    private string _modeScript = "";
    private bool _persist = true;

    // In OnInitializedAsync, after the existing theme wiring:
    ThemeService.OnModeChanged += HandleModeChangedAsync;

    var p = Mode switch { DrylColorMode.Light => "light", DrylColorMode.Dark => "dark", _ => "" };
    _modeScript =
        "<script>(function(){try{var p='" + p + "';" +
        "var m=localStorage.getItem('dryl-color-mode');" +
        "m=(m==='light'||m==='dark')?m:p;" +
        "if(m){document.documentElement.setAttribute('data-dryl-mode',m);}}catch(e){}})();</script>";

    private async Task HandleModeChangedAsync()
    {
        await InvokeAsync(StateHasChanged);
        if (_attached)
        {
            var mode = ThemeService.CurrentMode switch
            {
                DrylColorMode.Light => "light",
                DrylColorMode.Dark => "dark",
                _ => "system",
            };
            try { await JS.InvokeVoidAsync("dryl.theme.applyMode", mode, _persist); }
            catch (JSDisconnectedException) { /* circuit gone */ }
        }
    }

    // Replace OnAfterRender with OnAfterRenderAsync; keep the _attached flip,
    // then sync the C# state with what the pre-paint script applied:
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _attached = true;

        string? stored = null;
        try { stored = await JS.InvokeAsync<string?>("dryl.theme.storedMode"); }
        catch (JSDisconnectedException) { return; }

        var startup = stored switch
        {
            "light" => DrylColorMode.Light,
            "dark" => DrylColorMode.Dark,
            _ => Mode,
        };
        if (startup != ThemeService.CurrentMode)
        {
            _persist = false;                         // startup sync is not a user choice
            try { await ThemeService.SetModeAsync(startup); }
            finally { _persist = true; }
        }
    }

    // Dispose: also unsubscribe OnModeChanged.
```

Note: `@((MarkupString)_modeScript)` is the supported way to emit a `<script>` from a component (the Razor compiler rejects literal script tags). The script runs during HTML parsing — before first paint — so a persisted choice never flashes.

- [ ] **Step 5: Run tests** — `dotnet test tests/DRYL.Components.Tests --filter "DrylThemeProviderTests|DrylThemeServiceModeTests"` → expected: all pass (JSInterop is `Loose`; `storedMode` returns default `null`).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/wwwroot/js/dryl.js DRYL.Components/Components/Surfaces/DrylThemeProvider.razor tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs
git commit -m "feat(theme): color-mode application, persistence and pre-paint restore in DrylThemeProvider"
```

---

### Task 4: `DrylColorModeToggle` component

**Files:**
- Create: `DRYL.Components/Components/Surfaces/DrylColorModeToggle.razor`
- Modify: `DRYL.Components/wwwroot/dryl.css` (append a `.color-mode-toggle` block in the buttons region)
- Test: `tests/DRYL.Components.Tests/Theming/DrylColorModeToggleTests.cs` (create)

**Interfaces:**
- Consumes: `IDrylThemeService.CurrentMode` / `SetModeAsync` (Task 2).
- Produces: `<DrylColorModeToggle />` (optional `Size` int, default 18; merged `Class`) — used by Task 10.

Before coding: open `DRYL.Components/Components/Data/DrylButton.razor` (or the nearest icon-button-like component) and mirror the established merged-`Class` parameter pattern exactly (a splatted `class` must merge, not clobber).

- [ ] **Step 1: Write failing tests** in `tests/DRYL.Components.Tests/Theming/DrylColorModeToggleTests.cs`:

```csharp
using Bunit;
using DRYL.Components;
using DRYL.Components.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Theming;

public class DrylColorModeToggleTests : BunitContext
{
    public DrylColorModeToggleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<IDrylThemeService, DrylThemeService>();
    }

    [Fact]
    public void Renders_button_with_mode_label()
    {
        var cut = Render<DrylColorModeToggle>();
        var btn = cut.Find("button");

        Assert.Contains("System", btn.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Click_cycles_System_Light_Dark_System()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylColorModeToggle>();

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.Light, svc.CurrentMode);

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.Dark, svc.CurrentMode);

        await cut.Find("button").ClickAsync(new());
        Assert.Equal(DrylColorMode.System, svc.CurrentMode);
    }

    [Fact]
    public async Task State_class_follows_the_chosen_mode()
    {
        var cut = Render<DrylColorModeToggle>();
        Assert.Contains("is-system", cut.Find("button").ClassList);

        await cut.Find("button").ClickAsync(new());
        Assert.Contains("is-light", cut.Find("button").ClassList);
    }
}
```

- [ ] **Step 2: Run to verify failure** — `dotnet test tests/DRYL.Components.Tests --filter DrylColorModeToggleTests` → expected: compile error (component missing).

- [ ] **Step 3: Implement the component** `DrylColorModeToggle.razor`:

```razor
@namespace DRYL.Components
@using DRYL.Components.Theming
@inject IDrylThemeService ThemeService

@*  ─────────────────────────────────────────────────────────
    DrylColorModeToggle — cycles the color mode System → Light
    → Dark. The icon reflects the *chosen* mode (auto badge for
    System); the actual rendition is resolved by dryl.css.
    Like the theme switcher pattern, this component triggers the
    change itself and re-renders — it does not subscribe to
    OnModeChanged (that single-subscriber slot belongs to the
    provider).
    ───────────────────────────────────────────────────────── *@

<DrylTooltip Text="@Label">
    <button type="button"
            class="color-mode-toggle @StateClass @Class"
            aria-label="@Label"
            @attributes="AdditionalAttributes"
            @onclick="CycleAsync">
        <svg viewBox="0 0 24 24" width="@Size" height="@Size" fill="none"
             stroke="currentColor" stroke-width="1.75" stroke-linecap="round"
             stroke-linejoin="round" aria-hidden="true">
            <circle class="cmt-core" cx="12" cy="12" r="4" />
            <g class="cmt-rays">
                <line x1="12" y1="2.5" x2="12" y2="5" /><line x1="12" y1="19" x2="12" y2="21.5" />
                <line x1="2.5" y1="12" x2="5" y2="12" /><line x1="19" y1="12" x2="21.5" y2="12" />
                <line x1="5.3" y1="5.3" x2="7" y2="7" /><line x1="17" y1="17" x2="18.7" y2="18.7" />
                <line x1="5.3" y1="18.7" x2="7" y2="17" /><line x1="17" y1="7" x2="18.7" y2="5.3" />
            </g>
            <path class="cmt-moon" d="M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401" />
        </svg>
        <span class="cmt-auto" aria-hidden="true">A</span>
    </button>
</DrylTooltip>
<span class="visually-hidden" role="status" aria-live="polite">@Label</span>

@code {
    /// <summary>Icon size in pixels. Default 18.</summary>
    [Parameter] public int Size { get; set; } = 18;

    /// <summary>Additional CSS classes, merged with the component's own.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Splatted attributes (a lower-case <c>class</c> here is captured by <see cref="Class"/> — mirror the library pattern).</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string StateClass => ThemeService.CurrentMode switch
    {
        DrylColorMode.Light => "is-light",
        DrylColorMode.Dark => "is-dark",
        _ => "is-system",
    };

    private string Label => ThemeService.CurrentMode switch
    {
        DrylColorMode.Light => "Color mode: Light — switch to dark",
        DrylColorMode.Dark => "Color mode: Dark — follow system",
        _ => "Color mode: System — switch to light",
    };

    private Task CycleAsync() => ThemeService.SetModeAsync(ThemeService.CurrentMode switch
    {
        DrylColorMode.System => DrylColorMode.Light,
        DrylColorMode.Light => DrylColorMode.Dark,
        _ => DrylColorMode.System,
    });
}
```

Adapt the `Class`/`AdditionalAttributes` merge to the exact library pattern found before Step 1 (the splat must not clobber `class`). If `.visually-hidden` does not exist in `dryl.css`, use the library's existing screen-reader-only class (grep for `sr-only` / `visually-hidden`).

- [ ] **Step 4: Style it** — append to `dryl.css` next to the other button styles (tokens only; sun↔moon morph per rule 2.12):

```css
/* ---- Color-mode toggle ---------------------------------------- */
.color-mode-toggle {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px; height: 34px;
  border-radius: var(--r-sm);
  border: 1px solid var(--line);
  background: var(--glass-1);
  color: var(--fg-muted);
  cursor: pointer;
  transition: border-color var(--dur-fast) var(--ease-out),
              color var(--dur-fast) var(--ease-out),
              box-shadow var(--dur-fast) var(--ease-out);
}
.color-mode-toggle:hover { border-color: var(--line-hover); color: var(--fg); box-shadow: var(--shadow-sm); }
.color-mode-toggle .cmt-core,
.color-mode-toggle .cmt-rays,
.color-mode-toggle .cmt-moon {
  transform-origin: 12px 12px;
  transition: opacity var(--dur-med) var(--ease-out),
              transform var(--dur-med) var(--ease-spring);
}
/* chosen: light → sun */
.color-mode-toggle.is-light .cmt-moon { opacity: 0; transform: rotate(-40deg) scale(0.6); }
.color-mode-toggle.is-light .cmt-core,
.color-mode-toggle.is-light .cmt-rays { opacity: 1; transform: none; }
/* chosen: dark → moon */
.color-mode-toggle.is-dark .cmt-moon { opacity: 1; transform: none; }
.color-mode-toggle.is-dark .cmt-core,
.color-mode-toggle.is-dark .cmt-rays { opacity: 0; transform: rotate(40deg) scale(0.5); }
/* chosen: system → moon base + auto badge */
.color-mode-toggle.is-system .cmt-moon { opacity: 1; transform: none; }
.color-mode-toggle.is-system .cmt-core,
.color-mode-toggle.is-system .cmt-rays { opacity: 0; transform: rotate(40deg) scale(0.5); }
.color-mode-toggle .cmt-auto {
  position: absolute; right: -3px; bottom: -3px;
  font-size: 8px; font-weight: 700; line-height: 1;
  padding: 2px 3px; border-radius: var(--r-xs);
  background: var(--accent-grad); color: var(--on-accent);
  opacity: 0; transform: scale(0.5);
  transition: opacity var(--dur-med) var(--ease-out),
              transform var(--dur-med) var(--ease-spring);
}
.color-mode-toggle.is-system .cmt-auto { opacity: 1; transform: none; }
@media (prefers-reduced-motion: reduce) {
  .color-mode-toggle .cmt-core, .color-mode-toggle .cmt-rays,
  .color-mode-toggle .cmt-moon, .color-mode-toggle .cmt-auto { transition: none; }
}
```

(`--line-hover` and `--on-accent` exist after Tasks 5–7; if this task runs first, use `var(--line-strong)` / `#fff` and swap in the sweep. Preferred order is Tasks 5–7 before 4 only for these two variables — simply run the sweep grep afterwards to confirm no literal remains.)

- [ ] **Step 5: Run tests** — `dotnet test tests/DRYL.Components.Tests --filter DrylColorModeToggleTests` → expected: 3 passed.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Surfaces/DrylColorModeToggle.razor DRYL.Components/wwwroot/dryl.css tests/DRYL.Components.Tests/Theming/DrylColorModeToggleTests.cs
git commit -m "feat(theme): DrylColorModeToggle — animated System/Light/Dark cycle"
```

---

### Task 5: Literal sweep 1/3 — white highlights, sheens, shimmer, hover washes

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css` (whole file)

**Interfaces:**
- Consumes: LIGHT-TOKEN-SET markers (Task 1).
- Produces: tokens `--edge-hi`, `--edge-hi-strong`, `--sheen-grad`, `--sheen-grad-soft`, `--shimmer`, `--shimmer-strong`, `--hover-wash`, `--line-hover`, `--on-accent`, `--on-accent-line`, `--on-accent-hi`, `--knob` (dark values in `:root`, light values in **both** LIGHT-TOKEN-SET copies, per the reference table).

- [ ] **Step 1: Define the new tokens.** Add the dark values to `:root` (new subsection `/* Mode-dependent effect tokens */`) and the light values to both LIGHT-TOKEN-SET copies — values verbatim from the reference table. `--on-accent*` and `--knob` go into `:root` only (mode-independent).

- [ ] **Step 2: Enumerate the candidates:**

```bash
grep -nE 'rgba\(255, ?255, ?255|rgba\(255,255,255' DRYL.Components/wwwroot/dryl.css | grep -v 'LIGHT-TOKEN-SET' | grep -v -- '--edge\|--sheen\|--shimmer\|--hover\|--line\|--glass\|--scrollbar\|--on-accent\|initial-value'
```

- [ ] **Step 3: Replace every hit** using these rules (judge each occurrence, don't blind-replace):
  - `inset 0 1px 0 rgba(255,255,255,0.04–0.08)` → `inset 0 1px 0 var(--edge-hi)`
  - `inset 0 1px 0 rgba(255,255,255,0.18–0.22)` **on accent-gradient surfaces** (primary buttons) → `var(--on-accent-hi)`; on glass surfaces → `var(--edge-hi-strong)`
  - top-sheen `linear-gradient(180deg, rgba(255,255,255,.04), rgba(255,255,255,.01/.015))` → `var(--sheen-grad)`; the fainter `.02 → 0` variant → `var(--sheen-grad-soft)`
  - shimmer sweeps (`transparent 30%, rgba(255,255,255,.22–.4) 50%, transparent 70%`) → `var(--shimmer)` / `var(--shimmer-strong)` as the middle stop
  - `color: white` / `color: #fff` on accent fills → `var(--on-accent)`; `border-color: rgba(255,255,255,0.12)` on accent fills → `var(--on-accent-line)`
  - `border: 2px solid white`, `background: white` (slider/switch knobs) → `var(--knob)`
  - `.input:hover … rgba(255,255,255,0.18)` and similar hover borders → `var(--line-hover)`
  - `.tbl tr:hover … rgba(255,255,255,0.02)` and similar row washes → `var(--hover-wash)`
  - DrylDepthGlass block (lines ~940–1000): leave for Task 7.
  - `white-space: nowrap` matches are noise — skip.

- [ ] **Step 4: Verify sweep completeness** — rerun the Step 2 grep; expected: only hits inside the DepthGlass block (Task 7) and `@property` initial values remain. Run `node scripts/check-light-sync.mjs` → in sync.

- [ ] **Step 5: Visual spot-check** both modes on the running website (buttons, cards, inputs, tables pages) — flip `data-dryl-mode` in devtools.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css
git commit -m "refactor(css): lift white highlight/sheen/shimmer literals onto mode-aware tokens"
```

---

### Task 6: Literal sweep 2/3 — black overlays, backdrops, scrollbars, code surfaces

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css`

**Interfaces:**
- Consumes: Task 5 conventions.
- Produces: tokens `--backdrop`, `--backdrop-soft`, `--press-wash`, `--scrollbar-thumb`, `--scrollbar-thumb-hover`, `--code-bg`, `--code-fg` (reference table values; light values into **both** copies; `--code-fg` `:root` only).

- [ ] **Step 1: Define the tokens** (same placement pattern as Task 5).

- [ ] **Step 2: Enumerate:**

```bash
grep -nE 'rgba\(0, ?0, ?0|rgba\(0,0,0' DRYL.Components/wwwroot/dryl.css | grep -v 'LIGHT-TOKEN-SET'
```

- [ ] **Step 3: Replace by rule:**
  - modal/drawer backdrops (`rgba(0,0,0,0.6)`) → `var(--backdrop)`; lighter overlays (`0.4`) → `var(--backdrop-soft)`
  - small pressed/hover darkenings (`rgba(0,0,0,0.25)`) → `var(--press-wash)`
  - `.code-block` background `rgba(0,0,0,0.55)` → `var(--code-bg)`; add `color: var(--code-fg);` to `.code-block` so code text stays light in both modes; the `.tok-*` colors (lines ~1597–1602) stay **unchanged** (code is dark in both modes — documented decision)
  - black shadow stacks inside component rules: if the stack matches an existing `--shadow-sm/md/lg` shape, use the token; genuinely bespoke stacks (e.g. dialog `0 22px 50px rgba(0,0,0,0.42)`) → leave for Task 7's `--depth-shadow` or fold into `--shadow-lg` when visually equivalent — judge per case, prefer existing tokens
  - `::-webkit-scrollbar-thumb` `rgba(255,255,255,0.08/0.16)` → `var(--scrollbar-thumb)` / `var(--scrollbar-thumb-hover)`

- [ ] **Step 4: Verify** — rerun grep from Step 2: only DepthGlass-block hits (Task 7) may remain. `node scripts/check-light-sync.mjs` → in sync. Check a dialog + code block + scrollbar visually in both modes.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css
git commit -m "refactor(css): tokenize backdrops, scrollbars and always-dark code surfaces"
```

---

### Task 7: Literal sweep 3/3 — semantic-derived colors, accent text, DepthGlass

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css`

**Interfaces:**
- Consumes: Tasks 5–6 conventions.
- Produces: `--accent-fg`, `--danger-fg`, `--success-hi`, `--warning-hi`, `--danger-hi`, `--info-hi`, `--depth-edge`, `--depth-shadow` (reference table).

- [ ] **Step 1: Define the tokens.** `--*-hi` are single `color-mix` definitions in `:root` (they adapt through the semantic seeds — no light copy). `--accent-fg`, `--danger-fg`, `--depth-edge`, `--depth-shadow` get light overrides in both copies.

- [ ] **Step 2: Enumerate remaining hex literals:**

```bash
grep -nE '#[0-9a-fA-F]{3,8}' DRYL.Components/wwwroot/dryl.css | grep -vE 'LIGHT-TOKEN-SET|initial-value|@property|tok-|--chart|--accent|--ai-|--success|--warning|--danger|--info|--fg|--ground|--bg-|url\('
```

- [ ] **Step 3: Replace by rule:**
  - `#d6cbff` (accent-tinted text, ~lines 802, 1581) → `var(--accent-fg)`
  - `#fca5a5` as danger text (~line 437) → `var(--danger-fg)`
  - progress/status gradient endpoints `#6ee7b7 / #fcd34d / #fca5a5 / #67e8f9` → `var(--success-hi)` / `var(--warning-hi)` / `var(--danger-hi)` / `var(--info-hi)`
  - DepthGlass block (~940–1000): white insets → `var(--depth-edge)` (scale the alpha steps against it with `color-mix(in srgb, var(--depth-edge) N%, transparent)` where the original alphas differ); `rgba(0,0,0,0.42/0.32)` drop shadows → `var(--depth-shadow)`
  - `.tok-*` colors and `@property` initial values stay.

- [ ] **Step 4: Final sweep audit** — expected **zero** unexplained literals:

```bash
grep -nE 'rgba\(255, ?255, ?255|rgba\(0, ?0, ?0|#[0-9a-fA-F]{6}' DRYL.Components/wwwroot/dryl.css | grep -vE 'LIGHT-TOKEN-SET|initial-value|tok-|url\(' | grep -vE '^\s*[0-9]+:\s*--'
```

Every remaining hit must be individually justifiable (document why in the commit message if any). `node scripts/check-light-sync.mjs` → in sync.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css
git commit -m "refactor(css): tokenize semantic-derived colors and DepthGlass — dryl.css literal-free"
```

---

### Task 8: Scoped `.razor.css` literals

**Files:**
- Modify: `DRYL.Components/Components/Inputs/DrylChipInput.razor.css`
- Modify: `DRYL.Components/Components/Inputs/DrylInputOtp.razor.css`
- Modify: `DRYL.Components/Components/Inputs/DrylMultiSelect.razor.css`
- Modify: `DRYL.Components/Components/Inputs/DrylRating.razor.css`
- Modify: `DRYL.Components/Components/Inputs/DrylSlider.razor.css`

**Interfaces:**
- Consumes: all tokens from Tasks 1, 5–7.

- [ ] **Step 1: Enumerate:**

```bash
grep -nE 'rgba\(255, ?255, ?255|rgba\(0, ?0, ?0|#[0-9a-fA-F]{6}' DRYL.Components/Components --include='*.razor.css' -r
```

- [ ] **Step 2: Replace each hit** with the matching token from the reference table (same rules as Tasks 5–7; knobs → `var(--knob)`, highlights → `var(--edge-hi*)`, etc.). If a hit fits no existing token, prefer an existing token that is visually equivalent over inventing a new one; a genuinely new token goes through the same dual-copy light-set procedure.

- [ ] **Step 3: Verify** — rerun Step 1 grep → zero hits. `dotnet build` → success. Visual check of the five inputs in both modes.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/Components
git commit -m "refactor(css): tokenize remaining literals in scoped component styles"
```

---

### Task 9: Contrast validation for the light palette

**Files:**
- Create: `scripts/validate-light-contrast.mjs`
- Possibly modify: both LIGHT-TOKEN-SET copies in `DRYL.Components/wwwroot/dryl.css` (value tuning)

**Interfaces:**
- Consumes: light values from Task 1.

- [ ] **Step 1: Write the validator** (plain node, WCAG 2.x relative luminance):

```js
// Validates light-mode semantic + chart colors: contrast >= 3.0 against the
// light elevated surface (--bg-1 #f5f5fa), and the fg scale >= 4.5 for text.
const surface = "f5f5fa";

const lum = (hex) => {
  const c = [0, 2, 4].map(i => parseInt(hex.slice(i, i + 2), 16) / 255)
    .map(v => v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4));
  return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
};
const ratio = (a, b) => {
  const [l1, l2] = [lum(a), lum(b)].sort((x, y) => y - x);
  return (l1 + 0.05) / (l2 + 0.05);
};

const checks = [
  // name, hex (no #), minimum ratio vs light surface
  ["--fg (text)",      "15151c", 4.5],
  ["--success",        "0e8a4d", 3.0],
  ["--warning",        "b45309", 3.0],
  ["--danger",         "dc2626", 3.0],
  ["--info",           "0e7490", 3.0],
  ["--chart-3",        "96610e", 3.0],
  ["--chart-4",        "1d7f46", 3.0],
  ["--chart-5",        "b0316f", 3.0],
  ["--chart-6",        "3a63c4", 3.0],
  ["--danger-fg",      "b91c1c", 4.5],
];

let failed = false;
for (const [name, hex, min] of checks) {
  const r = ratio(hex, surface);
  const ok = r >= min;
  if (!ok) failed = true;
  console.log(`${ok ? "PASS" : "FAIL"}  ${name.padEnd(18)} ${r.toFixed(2)}:1  (min ${min}:1)`);
}
process.exit(failed ? 1 : 0);
```

- [ ] **Step 2: Run it** — `node scripts/validate-light-contrast.mjs` → expected: all PASS. If any FAIL, darken that color until it passes (keep the hue, drop the lightness), update **both** LIGHT-TOKEN-SET copies **and** this script's value, rerun.

- [ ] **Step 3: Adjacent-series sanity for charts.** Visually inspect the charts demo page in light mode: six series must stay distinguishable (the dark palette was CVD-validated; the light values keep the same hue spacing, so distinctness carries over — confirm by eye, adjust lightness only if two series blur).

- [ ] **Step 4: Run the sync checker** — `node scripts/check-light-sync.mjs` → in sync.

- [ ] **Step 5: Commit**

```bash
git add scripts/validate-light-contrast.mjs DRYL.Components/wwwroot/dryl.css
git commit -m "test(theme): scripted contrast validation for the light palette"
```

---

### Task 10: Website integration (DRYL.Website)

**Files:**
- Modify: `WEBSITE/Components/Layout/TopBar.razor` (~line 32)
- Modify: `WEBSITE/Components/ComponentCatalog.cs` (Customization section, ~line 145)
- Create: `WEBSITE/Components/Pages/DemoColorMode.razor`
- Modify: `WEBSITE/Components/Pages/DemoTheming.razor` (cross-link the mode page)
- Modify: `WEBSITE/wwwroot/app.css` (literal audit)

**Interfaces:**
- Consumes: `<DrylColorModeToggle />` (Task 4), mode API (Tasks 2–3).

- [ ] **Step 1: Read first.** Open `WEBSITE/CLAUDE.md`, `DemoTheming.razor`, and one recent demo page to mirror the page structure and the `DemoExample` embedded-source framework exactly.

- [ ] **Step 2: TopBar.** In `TopBar.razor`, directly before `<ThemeSwitcher />`:

```razor
    <DrylColorModeToggle />
```

- [ ] **Step 3: Catalog entry.** In `ComponentCatalog.cs`, Customization section, after the Theming entry:

```csharp
        new("Color Mode", "color-mode", "Customization", "DrylColorModeToggle", "Surfaces", false, "System / Light / Dark — persisted choice, animated toggle.", "Moon"),
```

- [ ] **Step 4: Demo page.** Create `DemoColorMode.razor` at route `/components/color-mode`, mirroring the structure of `DemoTheming.razor`: an intro paragraph (mode follows the OS by default; explicit choices persist), a live `<DrylColorModeToggle />` example via the `DemoExample` framework, a snippet showing `DrylThemeProvider Mode=` and `IDrylThemeService.SetModeAsync`, and a note that accent themes are mode-independent. Cross-link from `DemoTheming.razor` ("Color mode ↔ accent themes are orthogonal — see /components/color-mode").

- [ ] **Step 5: Site CSS audit.** `grep -nE 'rgba\(255|rgba\(0, ?0, ?0|#[0-9a-fA-F]{6}' WEBSITE/wwwroot/app.css` — replace dark-assuming literals with library tokens (same rules as Tasks 5–7). Site-brand exceptions (e.g. hero art) may stay if they read correctly in both modes — check visually.

- [ ] **Step 6: Copy audit.** `grep -rinE 'dark[- ]?(only|first|theme)|no light' WEBSITE/Components WEBSITE/README.md` — reword every hit mode-neutrally (no "new" framing).

- [ ] **Step 7: Verify.** Run the website, click through: toggle in the top bar cycles modes with the glide; choice survives a reload; System follows the OS; `/components/color-mode` renders; search finds "Color Mode".

- [ ] **Step 8: Commit** (in the website repo)

```bash
git -C ../DRYL.Website add -A && git -C ../DRYL.Website commit -m "feat: color-mode toggle in top bar + color-mode docs page"
```

(If DRYL.Website consumes DRYL.Components as a NuGet package rather than a ProjectReference, switch it to the local ProjectReference for development or pack a local 2.0.0-dev package first — check `WEBSITE/DRYL.Website.csproj` at the start of this task.)

---

### Task 11: Documentation rewording (library repo)

**Files:**
- Modify: `CLAUDE.md`, `THEMING.md`, `DESIGN_TOKENS.md`, `README.md`, `COMPONENT_PATTERNS.md`, `DRYL.Components/PACKAGE.md`

**Interfaces:** none (text only). Historical files under `docs/superpowers/` stay untouched.

- [ ] **Step 1: CLAUDE.md.** Replace rule 2.2 entirely:

```markdown
### 2.2 Two modes, one identity
DRYL renders in two color modes — dark and light — driven entirely by the token
system. The default follows the operating system (`prefers-color-scheme`); apps
and users can force a mode through `DrylThemeProvider` / `IDrylThemeService`.

- Components never branch on the mode. They consume tokens; the mode swaps the
  token values underneath them.
- Never write a mode-assuming literal (`rgba(255,255,255,…)`, hardcoded grays)
  in component CSS. If a value must differ per mode, it becomes a token with
  both values in `dryl.css` (both light-set copies!).
- Every new component is verified in **both** modes before it ships.
```

Also: §1 paragraph — reword "dark, glassy, alive" to "glassy, alive — in a deep-dark and a luminous-light rendition of one identity" and adjust the surrounding sentences (surfaces stack on the mode's ground, not "pure black"). §4 checklist — add `- [ ] Component reads correctly in both color modes (flip data-dryl-mode in devtools)` and reword the "translucent, not solid" items mode-neutrally where they say "dark". §6 — remove "Add a light theme" from the never-do list; replace with "❌ Hardcode a mode-assuming color instead of a token".

- [ ] **Step 2: THEMING.md.** Rewrite the intro ("dark glass core" → "glass core rendered in two color modes"); add a "Color mode" chapter after Quick start:

```markdown
## Color mode

DRYL follows the operating-system preference (`prefers-color-scheme`) out of the
box — no setup required. To force a mode at startup:

```razor
<DrylThemeProvider Mode="DrylColorMode.Dark" />
```

Switch at runtime via the same service that switches themes:

```csharp
await ThemeService.SetModeAsync(DrylColorMode.Light);
```

An explicit choice is persisted (localStorage, key `dryl-color-mode`) and
restored before first paint on the next visit; `DrylColorMode.System` clears it.
Drop `<DrylColorModeToggle />` into your app bar for a ready-made, animated
switcher. Accent themes are mode-independent — a `DrylTheme` looks right in
both modes without extra work.
```

Update the "What's themeable" table: `Background` and `Surface translucency` rows now say "Per mode — dark and light ship as one token system; the mode is user-switchable". Text scale row: "Both modes ship tuned contrast ratios".

- [ ] **Step 3: DESIGN_TOKENS.md.** For every token the light set overrides, document both values (Dark / Light columns or a per-token pair). Source the values from `dryl.css` (single source of truth) — do not retype from this plan. Add the new effect tokens (`--edge-hi`, `--sheen-grad`, `--backdrop`, `--code-bg`, …) with usage guidance, and document the `data-dryl-mode` contract + the two-copy sync rule + `scripts/check-light-sync.mjs`.

- [ ] **Step 4: README.md / COMPONENT_PATTERNS.md / PACKAGE.md.** `grep -inE 'dark[- ]?(only|first|theme)|no light|pure black' <file>` — reword each hit mode-neutrally ("dark, glassy" → "glassy, alive in both color modes" etc.). Feature bullets mention "Light & dark, system-following, user-switchable" as an ordinary capability, not a novelty.

- [ ] **Step 5: Verify** — `grep -rinE 'dark.?only|only.?dark|no light theme' CLAUDE.md THEMING.md DESIGN_TOKENS.md README.md COMPONENT_PATTERNS.md DRYL.Components/PACKAGE.md` → zero hits.

- [ ] **Step 6: Commit**

```bash
git add CLAUDE.md THEMING.md DESIGN_TOKENS.md README.md COMPONENT_PATTERNS.md DRYL.Components/PACKAGE.md
git commit -m "docs: mode-neutral wording across living docs; color-mode chapter in THEMING"
```

---

### Task 12: Version 2.0.0, changelog cut, full verification

**Files:**
- Modify: `DRYL.Components/DRYL.Components.csproj` (`<Version>`)
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Full test run** — `dotnet test tests/DRYL.Components.Tests` → all green. `node scripts/check-light-sync.mjs` and `node scripts/validate-light-contrast.mjs` → pass.

- [ ] **Step 2: End-to-end verification with the `/verify` skill** (docs website + Playwright), in **both** modes, desktop + 375 px: buttons, cards, inputs, dialogs (backdrop!), tables, charts, code blocks (stay dark), AI aura surfaces, toggle cycle + persistence (reload keeps the choice), reduced-motion (instant swap).

- [ ] **Step 3: Bump version** — `DRYL.Components.csproj`: `<Version>1.5.0</Version>` → `<Version>2.0.0</Version>`. (Confirm the file still says 1.5.0; if the branch moved, MAJOR from whatever it says.)

- [ ] **Step 4: Cut the changelog.** In `CHANGELOG.md`, rename `[Unreleased]` to `## [2.0.0] - 2026-07-10` (use the actual date), fresh empty `[Unreleased]` above, with entries:

```markdown
### Added
- `DrylColorMode` — `System / Dark / Light`; `IDrylThemeService.SetModeAsync`, `CurrentMode`, `OnModeChanged`
- `DrylThemeProvider` — new `Mode` parameter; persists explicit choices (localStorage) and restores them before first paint
- `DrylColorModeToggle` — animated System / Light / Dark switcher
- Light color rendition of the full token system, including light-validated semantic and chart palettes
- `scripts/check-light-sync.mjs`, `scripts/validate-light-contrast.mjs` — dev-time guards for the light token set

### Changed
- **BREAKING:** the default color mode now follows the operating system (`prefers-color-scheme`). Apps that must stay dark regardless of OS pin `<DrylThemeProvider Mode="DrylColorMode.Dark" />`
- All remaining hardcoded color literals in `dryl.css` and scoped styles lifted onto semantic tokens (`--edge-hi`, `--sheen-grad`, `--backdrop`, `--code-bg`, …)
- Code surfaces (`.code-block`) render dark in both modes by design
```

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/DRYL.Components.csproj CHANGELOG.md
git commit -m "docs: cut 2.0.0 — light color mode, DrylColorModeToggle, mode API"
```

- [ ] **Step 6: Do not push a tag** — the publish workflow owns tagging when the branch reaches `main`.
