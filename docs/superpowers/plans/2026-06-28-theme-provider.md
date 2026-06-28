# DrylThemeProvider Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a strongly-typed, runtime-switchable theming system to DRYL that lets consumers re-skin accent + semantic colors from a few seeds, with CSS-side coherent derivation and an animated live transition — plus a holistic README rewrite that promotes it.

**Architecture:** C# emits only *seed* CSS custom properties (`--accent-a/-b`, optional `--ai-a/-b`, optional semantics). `dryl.css` derives everything downstream via `color-mix()` and registers the color seeds as animatable `@property`s so theme changes glide. A scoped `IDrylThemeService` holds the current theme and raises an event; a root `DrylThemeProvider` component renders the seeds into an inline `:root` `<style>` (prerender/no-flash) and pushes runtime changes to `document.documentElement` via tiny JS interop.

**Tech Stack:** Blazor (Server + WASM, .NET 8/9/10), C# records, bUnit + xUnit tests, CSS `color-mix()` / `@property`, vanilla JS interop under `window.dryl`.

## Global Constraints

- **Tokens, not literals** — every CSS value references a variable; new derivations use existing tokens/seeds only.
- **Dark only** — no light theme, no `prefers-color-scheme`.
- **No new runtime dependencies** — zero npm/JS libraries; interop is hand-written under `window.dryl`.
- **InvariantCulture for any numeric→string** — use `FormattableString.Invariant` / `CultureInfo.InvariantCulture` (German locale otherwise emits `0,5`).
- **AI mode is opt-in** — `--ai-a/--ai-b` default to the brand accent; absent an AI accent the AI vocabulary is byte-for-byte unchanged (rule 2.10).
- **Motion vocabulary fixed** — only `--dur-fast|med|slow`, `--ease-out|in-out|spring`; honour `prefers-reduced-motion: reduce`.
- **Default render unchanged** — the seed-derivation refactor must be pixel-identical for the default (Nebula) theme.
- **Naming** — components PascalCase `Dryl`-prefixed; CSS classes kebab-case; C# parameters strongly typed (enums/records, not strings).
- **Docs are mandatory** (CLAUDE.md §7) — CHANGELOG `[Unreleased]`, README, and reference docs updated in the same change.
- **Test project:** `tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`. Run all: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`. bUnit tests inherit `BunitContext` and render with `Render<T>(...)`.

---

## File Structure

**Create:**
- `DRYL.Components/Theming/DrylAccent.cs` — accent value type (two gradient stops).
- `DRYL.Components/Theming/DrylSemantic.cs` — optional semantic overrides.
- `DRYL.Components/Theming/DrylTheme.cs` — theme record + `ToCssVariables()`.
- `DRYL.Components/Theming/DrylThemes.cs` — curated presets.
- `DRYL.Components/Theming/IDrylThemeService.cs` — service contract.
- `DRYL.Components/Theming/DrylThemeService.cs` — service implementation.
- `DRYL.Components/Components/Surfaces/DrylThemeProvider.razor` — root provider component.
- `samples/Pages/DemoTheming.razor` — live demo.
- `THEMING.md` — consumer theming guide.
- `tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs`
- `tests/DRYL.Components.Tests/Theming/DrylThemeServiceTests.cs`
- `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs`
- `tests/DRYL.Components.Tests/Theming/DrylCssDerivationTests.cs`

**Modify:**
- `DRYL.Components/wwwroot/dryl.css` — seeds, `@property`, `color-mix` derivations, transition.
- `DRYL.Components/wwwroot/js/dryl.js` — `window.dryl.theme.apply`.
- `DRYL.Components/Extensions/ServiceCollectionExtensions.cs` — register service.
- `DRYL.Components/_Imports.razor` — `@using DRYL.Components.Theming`.
- `tests/DRYL.Components.Tests/ServiceRegistrationTests.cs` — assert new service.
- `DESIGN_TOKENS.md` — theming section.
- `CHANGELOG.md` — `[Unreleased] → Added`.
- `CLAUDE.md` — §7.2 note (table removed; point at website).
- `README.md` — holistic rewrite.

---

## Task 1: Theme model (`DrylAccent`, `DrylSemantic`, `DrylTheme`)

**Files:**
- Create: `DRYL.Components/Theming/DrylAccent.cs`
- Create: `DRYL.Components/Theming/DrylSemantic.cs`
- Create: `DRYL.Components/Theming/DrylTheme.cs`
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs`

**Interfaces:**
- Produces:
  - `readonly record struct DrylAccent(string A, string B)` in `DRYL.Components.Theming`
  - `sealed record DrylSemantic { string? Success; string? Warning; string? Danger; string? Info; }`
  - `sealed record DrylTheme { required DrylAccent Accent; DrylAccent? AiAccent; DrylSemantic? Semantic; internal string ToCssVariables(); }`
  - `ToCssVariables()` returns a `;`-separated seed list: always `--accent-a`/`--accent-b`; adds `--ai-a`/`--ai-b` only when `AiAccent` is set; adds each semantic only when non-null. No trailing logic depends on order, but emit in the order: accent-a, accent-b, ai-a, ai-b, success, warning, danger, info.

- [ ] **Step 1: Write the failing test**

Create `tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs`:

```csharp
using DRYL.Components.Theming;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeTests
{
    [Fact]
    public void ToCssVariables_emits_only_accent_seeds_when_minimal()
    {
        var theme = new DrylTheme { Accent = new DrylAccent("#7c5cff", "#22d3ee") };

        var css = theme.ToCssVariables();

        Assert.Equal("--accent-a:#7c5cff;--accent-b:#22d3ee;", css);
    }

    [Fact]
    public void ToCssVariables_includes_ai_seeds_when_ai_accent_set()
    {
        var theme = new DrylTheme
        {
            Accent = new DrylAccent("#7c5cff", "#22d3ee"),
            AiAccent = new DrylAccent("#ff7ad9", "#ffd166"),
        };

        var css = theme.ToCssVariables();

        Assert.Contains("--ai-a:#ff7ad9;", css);
        Assert.Contains("--ai-b:#ffd166;", css);
    }

    [Fact]
    public void ToCssVariables_omits_ai_seeds_when_ai_accent_null()
    {
        var theme = new DrylTheme { Accent = new DrylAccent("#7c5cff", "#22d3ee") };

        Assert.DoesNotContain("--ai-a", theme.ToCssVariables());
    }

    [Fact]
    public void ToCssVariables_includes_only_specified_semantics()
    {
        var theme = new DrylTheme
        {
            Accent = new DrylAccent("#7c5cff", "#22d3ee"),
            Semantic = new DrylSemantic { Danger = "#ff0000" },
        };

        var css = theme.ToCssVariables();

        Assert.Contains("--danger:#ff0000;", css);
        Assert.DoesNotContain("--success", css);
        Assert.DoesNotContain("--warning", css);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeTests"`
Expected: FAIL — `DrylTheme` / `DrylAccent` / `DrylSemantic` do not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

Create `DRYL.Components/Theming/DrylAccent.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <summary>
/// A two-stop accent: the endpoints of DRYL's accent gradient. Either stop
/// can be used on its own (e.g. as a solid accent), but together they form
/// <c>--accent-grad</c>.
/// </summary>
/// <param name="A">First gradient stop / primary accent (maps to <c>--accent-a</c>).</param>
/// <param name="B">Second gradient stop / secondary accent (maps to <c>--accent-b</c>).</param>
public readonly record struct DrylAccent(string A, string B);
```

Create `DRYL.Components/Theming/DrylSemantic.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <summary>
/// Optional overrides for DRYL's semantic status colors. Any member left
/// <c>null</c> falls back to the DRYL default for that token.
/// </summary>
public sealed record DrylSemantic
{
    /// <summary>Healthy / succeeded / online — maps to <c>--success</c>.</summary>
    public string? Success { get; init; }

    /// <summary>Pending / near-limit — maps to <c>--warning</c>.</summary>
    public string? Warning { get; init; }

    /// <summary>Failed / destructive — maps to <c>--danger</c>.</summary>
    public string? Danger { get; init; }

    /// <summary>Informational / neutral — maps to <c>--info</c>.</summary>
    public string? Info { get; init; }
}
```

Create `DRYL.Components/Theming/DrylTheme.cs`:

```csharp
using System.Text;

namespace DRYL.Components.Theming;

/// <summary>
/// A complete DRYL theme. A theme only carries <em>seed</em> values — the brand
/// accent, an optional separate AI accent, and optional semantic overrides.
/// Everything else (soft fills, accent lines, glows, the AI aura) is
/// <em>derived</em> from these seeds in <c>dryl.css</c> via <c>color-mix()</c>,
/// so a theme can never drift out of visual coherence.
/// </summary>
public sealed record DrylTheme
{
    /// <summary>The brand accent gradient endpoints. Required.</summary>
    public required DrylAccent Accent { get; init; }

    /// <summary>
    /// An optional accent used only for AI surfaces (aura, indicators). When
    /// <c>null</c>, AI surfaces reuse <see cref="Accent"/> — so AI styling is
    /// unchanged unless a consumer opts in.
    /// </summary>
    public DrylAccent? AiAccent { get; init; }

    /// <summary>Optional semantic status-color overrides.</summary>
    public DrylSemantic? Semantic { get; init; }

    /// <summary>
    /// Emits the theme's seed custom properties as a <c>";"</c>-separated
    /// <c>--key:value;</c> string suitable for an inline <c>:root</c> style or
    /// for <c>document.documentElement.style</c>. Omits AI seeds when
    /// <see cref="AiAccent"/> is <c>null</c> and omits any unset semantic.
    /// </summary>
    internal string ToCssVariables()
    {
        var sb = new StringBuilder();
        Append(sb, "--accent-a", Accent.A);
        Append(sb, "--accent-b", Accent.B);

        if (AiAccent is { } ai)
        {
            Append(sb, "--ai-a", ai.A);
            Append(sb, "--ai-b", ai.B);
        }

        if (Semantic is { } s)
        {
            Append(sb, "--success", s.Success);
            Append(sb, "--warning", s.Warning);
            Append(sb, "--danger", s.Danger);
            Append(sb, "--info", s.Info);
        }

        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append(key).Append(':').Append(value).Append(';');
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Theming/DrylAccent.cs DRYL.Components/Theming/DrylSemantic.cs DRYL.Components/Theming/DrylTheme.cs tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs
git commit -m "feat(theming): DrylTheme model with seed-only ToCssVariables()"
```

---

## Task 2: Curated presets (`DrylThemes`)

**Files:**
- Create: `DRYL.Components/Theming/DrylThemes.cs`
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs` (add to existing file)

**Interfaces:**
- Consumes: `DrylTheme`, `DrylAccent`, `DrylSemantic` (Task 1).
- Produces: `static class DrylThemes` with `DrylTheme Nebula`, `Ember`, `Verdant`, `Mono`, and `Default => Nebula`. `Nebula.Accent` is exactly `#7c5cff`/`#22d3ee` and `Nebula.AiAccent` is `null` and `Nebula.Semantic` is `null` (so it is byte-identical to today's default).

- [ ] **Step 1: Write the failing test**

Append to `tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs`:

```csharp
public class DrylThemesTests
{
    [Fact]
    public void Nebula_matches_current_default_accent_and_has_no_overrides()
    {
        Assert.Equal(new DrylAccent("#7c5cff", "#22d3ee"), DrylThemes.Nebula.Accent);
        Assert.Null(DrylThemes.Nebula.AiAccent);
        Assert.Null(DrylThemes.Nebula.Semantic);
    }

    [Fact]
    public void Default_is_Nebula()
    {
        Assert.Equal(DrylThemes.Nebula, DrylThemes.Default);
    }

    [Fact]
    public void Nebula_emits_only_accent_seeds()
    {
        // Byte-identical to the default :root — no extra seeds to override.
        Assert.Equal("--accent-a:#7c5cff;--accent-b:#22d3ee;", DrylThemes.Nebula.ToCssVariables());
    }

    [Theory]
    [InlineData("Ember")]
    [InlineData("Verdant")]
    [InlineData("Mono")]
    public void Alternative_presets_change_the_accent(string _)
    {
        // Each alternative differs from Nebula's accent.
        Assert.NotEqual(DrylThemes.Nebula.Accent, DrylThemes.Ember.Accent);
        Assert.NotEqual(DrylThemes.Nebula.Accent, DrylThemes.Verdant.Accent);
        Assert.NotEqual(DrylThemes.Nebula.Accent, DrylThemes.Mono.Accent);
    }
}
```

> Note: `ToCssVariables()` is `internal`; the test project already sees internals via the existing `InternalsVisibleTo` (the model test in Task 1 relies on the same access). If the build reports `ToCssVariables` inaccessible, add `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DRYL.Components.Tests")]` to `DRYL.Components` (check `DRYL.Components.csproj` / an `AssemblyInfo` first — only add if missing).

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemesTests"`
Expected: FAIL — `DrylThemes` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `DRYL.Components/Theming/DrylThemes.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <summary>
/// Curated, ready-to-use DRYL themes. Each preset sets only seeds; the rest of
/// the visual language is derived in <c>dryl.css</c>. Build your own by
/// constructing a <see cref="DrylTheme"/> directly.
/// </summary>
public static class DrylThemes
{
    /// <summary>
    /// The signature DRYL look — violet → cyan. Identical to the library's
    /// built-in default, so applying it changes nothing visually.
    /// </summary>
    public static DrylTheme Nebula { get; } = new()
    {
        Accent = new DrylAccent("#7c5cff", "#22d3ee"),
    };

    /// <summary>Warm amber → red. Energetic, product-launch feel.</summary>
    public static DrylTheme Ember { get; } = new()
    {
        Accent = new DrylAccent("#f59e0b", "#f43f5e"),
    };

    /// <summary>Green → teal. Calm, "systems healthy" feel.</summary>
    public static DrylTheme Verdant { get; } = new()
    {
        Accent = new DrylAccent("#34d399", "#22d3ee"),
    };

    /// <summary>Desaturated, near-monochrome — accent recedes to a cool slate.</summary>
    public static DrylTheme Mono { get; } = new()
    {
        Accent = new DrylAccent("#9aa4b2", "#cbd5e1"),
    };

    /// <summary>The default theme when none is supplied. Equal to <see cref="Nebula"/>.</summary>
    public static DrylTheme Default => Nebula;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemesTests"`
Expected: PASS (6 tests across the Theory).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Theming/DrylThemes.cs tests/DRYL.Components.Tests/Theming/DrylThemeTests.cs
git commit -m "feat(theming): DrylThemes presets (Nebula default + Ember/Verdant/Mono)"
```

---

## Task 3: Runtime service (`IDrylThemeService` / `DrylThemeService`)

**Files:**
- Create: `DRYL.Components/Theming/IDrylThemeService.cs`
- Create: `DRYL.Components/Theming/DrylThemeService.cs`
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeServiceTests.cs`

**Interfaces:**
- Consumes: `DrylTheme`, `DrylAccent`, `DrylThemes` (Tasks 1–2).
- Produces:
  - `interface IDrylThemeService { DrylTheme Current { get; } Task SetThemeAsync(DrylTheme theme); Task SetAccentAsync(string a, string b); event Func<Task>? OnThemeChanged; }`
  - `sealed class DrylThemeService : IDrylThemeService` — `Current` starts as `DrylThemes.Default`; `SetThemeAsync` assigns then awaits `OnThemeChanged`; `SetAccentAsync` is sugar over `SetThemeAsync(Current with { Accent = ... })`. The service never touches the DOM (no `IJSRuntime`).

- [ ] **Step 1: Write the failing test**

Create `tests/DRYL.Components.Tests/Theming/DrylThemeServiceTests.cs`:

```csharp
using DRYL.Components.Theming;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeServiceTests
{
    [Fact]
    public void Current_defaults_to_Nebula()
    {
        var svc = new DrylThemeService();
        Assert.Equal(DrylThemes.Nebula, svc.Current);
    }

    [Fact]
    public async Task SetThemeAsync_updates_current_and_raises_event()
    {
        var svc = new DrylThemeService();
        var raised = 0;
        svc.OnThemeChanged += () => { raised++; return Task.CompletedTask; };

        await svc.SetThemeAsync(DrylThemes.Ember);

        Assert.Equal(DrylThemes.Ember, svc.Current);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetAccentAsync_replaces_only_the_accent()
    {
        var svc = new DrylThemeService();
        await svc.SetThemeAsync(DrylThemes.Ember);

        await svc.SetAccentAsync("#111111", "#222222");

        Assert.Equal(new DrylAccent("#111111", "#222222"), svc.Current.Accent);
        // Ember had no semantic overrides, but the record-with preserves everything else.
        Assert.Equal(DrylThemes.Ember.Semantic, svc.Current.Semantic);
    }

    [Fact]
    public async Task SetThemeAsync_null_throws()
    {
        var svc = new DrylThemeService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SetThemeAsync(null!));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeServiceTests"`
Expected: FAIL — `DrylThemeService` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `DRYL.Components/Theming/IDrylThemeService.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <summary>
/// Holds the application's current <see cref="DrylTheme"/> and notifies the
/// <c>DrylThemeProvider</c> when it changes so the new seeds can be applied
/// (with the live transition). Registered as scoped by
/// <c>AddDrylComponents()</c>.
/// </summary>
public interface IDrylThemeService
{
    /// <summary>The currently active theme. Starts as <see cref="DrylThemes.Default"/>.</summary>
    DrylTheme Current { get; }

    /// <summary>Switch to a new theme and notify listeners. Animates if motion is allowed.</summary>
    Task SetThemeAsync(DrylTheme theme);

    /// <summary>Convenience: replace only the brand accent, keeping everything else.</summary>
    Task SetAccentAsync(string a, string b);

    /// <summary>Raised after <see cref="Current"/> changes. The provider subscribes to apply it.</summary>
    event Func<Task>? OnThemeChanged;
}
```

Create `DRYL.Components/Theming/DrylThemeService.cs`:

```csharp
namespace DRYL.Components.Theming;

/// <inheritdoc cref="IDrylThemeService"/>
public sealed class DrylThemeService : IDrylThemeService
{
    /// <inheritdoc/>
    public DrylTheme Current { get; private set; } = DrylThemes.Default;

    /// <inheritdoc/>
    public event Func<Task>? OnThemeChanged;

    /// <inheritdoc/>
    public async Task SetThemeAsync(DrylTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Current = theme;
        if (OnThemeChanged is { } handler)
            await handler.Invoke();
    }

    /// <inheritdoc/>
    public Task SetAccentAsync(string a, string b) =>
        SetThemeAsync(Current with { Accent = new DrylAccent(a, b) });
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeServiceTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Theming/IDrylThemeService.cs DRYL.Components/Theming/DrylThemeService.cs tests/DRYL.Components.Tests/Theming/DrylThemeServiceTests.cs
git commit -m "feat(theming): IDrylThemeService runtime theme switching"
```

---

## Task 4: DI registration

**Files:**
- Modify: `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/ServiceRegistrationTests.cs:17-20` (add an `[InlineData]`)

**Interfaces:**
- Consumes: `IDrylThemeService` / `DrylThemeService` (Task 3).
- Produces: `AddDrylComponents()` additionally registers `IDrylThemeService` as scoped.

- [ ] **Step 1: Write the failing test**

In `tests/DRYL.Components.Tests/ServiceRegistrationTests.cs`, add the using and a new `[InlineData]` line:

```csharp
using DRYL.Components.Theming;
```

```csharp
    [InlineData(typeof(IDrylDialogService))]
    [InlineData(typeof(IDrylToastService))]
    [InlineData(typeof(IDrylNotificationService))]
    [InlineData(typeof(IDrylAiActivityService))]
    [InlineData(typeof(IDrylThemeService))]
    public void AddDrylComponents_registers_service(Type serviceType)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ServiceRegistrationTests"`
Expected: FAIL — `IDrylThemeService` resolves to null (not registered).

- [ ] **Step 3: Write minimal implementation**

In `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`, add the using at the top and one registration line:

```csharp
using DRYL.Components.Theming;
```

```csharp
        services.AddScoped<IDrylDialogService, DrylDialogService>();
        services.AddScoped<IDrylToastService, DrylToastService>();
        services.AddScoped<IDrylNotificationService, DrylNotificationService>();
        services.AddScoped<IDrylAiActivityService, DrylAiActivityService>();
        services.AddScoped<IDrylThemeService, DrylThemeService>();
        return services;
```

Also extend the XML doc `<summary>` to mention `IDrylThemeService` and placing a `<DrylThemeProvider/>` in the root layout.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ServiceRegistrationTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Extensions/ServiceCollectionExtensions.cs tests/DRYL.Components.Tests/ServiceRegistrationTests.cs
git commit -m "feat(theming): register IDrylThemeService in AddDrylComponents"
```

---

## Task 5: `dryl.css` seed-derivation refactor

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css:6-97` (`:root`), the `--accent-soft`/`--accent-line`/`--glow-*` tokens, the `body` radial background, and the `.ai-aura-*` color references.
- Test: `tests/DRYL.Components.Tests/Theming/DrylCssDerivationTests.cs`

**Interfaces:**
- Consumes: nothing (pure CSS).
- Produces: a `:root` that (a) declares `--ai-a: var(--accent-a)` / `--ai-b: var(--accent-b)`, (b) registers `--accent-a/-b/--ai-a/-b` via `@property` as `<color>`, (c) derives `--accent-soft`/`--accent-line`/`--glow-accent`/`--glow-soft` and the body background via `color-mix()` from the seeds, (d) points `.ai-aura-*` color stops at `--ai-a/--ai-b`, (e) transitions the four seeds over `--dur-slow` under `prefers-reduced-motion: no-preference`.

**Note on equivalence:** `color-mix(in srgb, X p%, transparent)` equals `rgba(...)` of `X` at alpha `p/100`. Map each existing literal's alpha to the percentage. For the default seed `#7c5cff`/`#22d3ee` the output is identical.

- [ ] **Step 1: Write the failing test**

Create `tests/DRYL.Components.Tests/Theming/DrylCssDerivationTests.cs`:

```csharp
namespace DRYL.Components.Tests.Theming;

/// <summary>
/// Guards the seed-derivation refactor of dryl.css: the accent must no longer be
/// hardcoded in the derived tokens (so a theme propagates), the AI seeds must
/// exist and default to the brand accent, and the live transition must be present
/// and motion-gated.
/// </summary>
public class DrylCssDerivationTests
{
    private static string ReadDrylCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "DRYL.Components", "wwwroot", "dryl.css");
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException("dryl.css not found from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Accent_soft_and_line_are_derived_not_hardcoded()
    {
        var css = ReadDrylCss();
        Assert.Contains("--accent-soft:   color-mix(in srgb, var(--accent-a) 18%, transparent)", css);
        Assert.Contains("--accent-line:   color-mix(in srgb, var(--accent-a) 45%, transparent)", css);
    }

    [Fact]
    public void Ai_seeds_default_to_brand_accent()
    {
        var css = ReadDrylCss();
        Assert.Contains("--ai-a:", css);
        Assert.Contains("--ai-b:", css);
        Assert.Contains("--ai-a:          var(--accent-a)", css);
        Assert.Contains("--ai-b:          var(--accent-b)", css);
    }

    [Fact]
    public void Color_seeds_are_registered_as_animatable_properties()
    {
        var css = ReadDrylCss();
        Assert.Contains("@property --accent-a", css);
        Assert.Contains("@property --ai-a", css);
        Assert.Contains("syntax: \"<color>\"", css);
    }

    [Fact]
    public void Live_transition_is_motion_gated()
    {
        var css = ReadDrylCss();
        Assert.Contains("@media (prefers-reduced-motion: no-preference)", css);
        Assert.Contains("transition: --accent-a var(--dur-slow)", css);
    }

    [Fact]
    public void Derived_glow_tokens_no_longer_hardcode_the_default_violet()
    {
        var css = ReadDrylCss();
        // The literal default accent must not appear in --glow-accent / --glow-soft anymore.
        var glowAccentLine = css.Split('\n').First(l => l.Contains("--glow-accent:"));
        var glowSoftLine = css.Split('\n').First(l => l.Contains("--glow-soft:"));
        Assert.DoesNotContain("124, 92, 255", glowAccentLine);
        Assert.DoesNotContain("124, 92, 255", glowSoftLine);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCssDerivationTests"`
Expected: FAIL — the css still hardcodes the accent and has no `@property`/`--ai-*`/transition.

- [ ] **Step 3: Write minimal implementation**

In `DRYL.Components/wwwroot/dryl.css`, **above** the `:root` block (line 6) add the `@property` registrations:

```css
/* Registered so theme seeds interpolate — the derived color-mix chain glides
   on a theme change. Initial values mirror the default Nebula accent. */
@property --accent-a { syntax: "<color>"; inherits: true; initial-value: #7c5cff; }
@property --accent-b { syntax: "<color>"; inherits: true; initial-value: #22d3ee; }
@property --ai-a     { syntax: "<color>"; inherits: true; initial-value: #7c5cff; }
@property --ai-b     { syntax: "<color>"; inherits: true; initial-value: #22d3ee; }
```

In the `:root` accent block (lines 32-39), replace the derived literals and add the AI seeds:

```css
  /* Accent — gradient violet → cyan (themeable seeds) */
  --accent-a:      #7c5cff;
  --accent-b:      #22d3ee;
  --accent:        var(--accent-a);
  --accent-soft:   color-mix(in srgb, var(--accent-a) 18%, transparent);
  --accent-line:   color-mix(in srgb, var(--accent-a) 45%, transparent);
  --accent-grad:   linear-gradient(135deg, var(--accent-a) 0%, var(--accent-b) 100%);
  --accent-grad-r: linear-gradient(135deg, var(--accent-b) 0%, var(--accent-a) 100%);

  /* AI accent — defaults to the brand accent (opt-in divergence via DrylTheme.AiAccent) */
  --ai-a:          var(--accent-a);
  --ai-b:          var(--accent-b);
```

Replace the glow tokens (lines 73-74) with derived forms:

```css
  --glow-accent: 0 0 0 1px var(--accent-line),
                 0 8px 32px color-mix(in srgb, var(--accent-a) 35%, transparent),
                 0 0 64px color-mix(in srgb, var(--accent-b) 18%, transparent);
  --glow-soft:   0 0 60px color-mix(in srgb, var(--accent-a) 18%, transparent),
                 0 0 120px color-mix(in srgb, var(--accent-b) 8%, transparent);
```

Replace the `body` radial background (lines 116-123) literals with derived mixes (keep the geometry identical):

```css
body {
  background:
    radial-gradient(1200px 800px at 15% -10%, color-mix(in srgb, var(--accent-a) 12%, transparent), transparent 60%),
    radial-gradient(1000px 700px at 100% 0%, color-mix(in srgb, var(--accent-b) 8%, transparent), transparent 60%),
    radial-gradient(900px 700px at 50% 110%, color-mix(in srgb, var(--accent-a) 6%, transparent), transparent 60%),
    var(--ground);
  background-attachment: fixed;
}
```

Add the live transition immediately after the `:root` closing brace (after line 97):

```css
/* Theme seeds glide on change; instant for reduced-motion users. */
@media (prefers-reduced-motion: no-preference) {
  :root {
    transition:
      --accent-a var(--dur-slow) var(--ease-in-out),
      --accent-b var(--dur-slow) var(--ease-in-out),
      --ai-a var(--dur-slow) var(--ease-in-out),
      --ai-b var(--dur-slow) var(--ease-in-out);
  }
}
```

Finally, in the `.ai-aura-ring` / `.ai-aura-glow` / `.ai-aura-wash` rules, replace `var(--accent-a)` → `var(--ai-a)` and `var(--accent-b)` → `var(--ai-b)` (search the AI Mode section). Use `Grep` for `accent-a` / `accent-b` inside the `.ai-aura` rules and swap each to the `--ai-*` seed. Do **not** change `--accent-*` usages outside the AI-aura primitives.

> After editing, re-grep the whole file for `124, 92, 255` and `34, 211, 238`: any remaining occurrences must be intentional (none should remain in `--glow-*`, `--accent-soft`, `--accent-line`, or `body`). The `.ai-aura` rules now reference `--ai-*`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCssDerivationTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Visual equivalence check (manual)**

Open `prototype/DRYL Design System.html` (or the samples app) and confirm the default theme looks unchanged: accent buttons, focus rings, AI-aura, and the page's ambient glow must be identical to before. This is the "default render unchanged" gate.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css tests/DRYL.Components.Tests/Theming/DrylCssDerivationTests.cs
git commit -m "feat(theming): derive accent tokens from seeds via color-mix + @property"
```

---

## Task 6: `DrylThemeProvider` component + JS interop + `_Imports`

**Files:**
- Create: `DRYL.Components/Components/Surfaces/DrylThemeProvider.razor`
- Modify: `DRYL.Components/wwwroot/js/dryl.js` (append `window.dryl.theme`)
- Modify: `DRYL.Components/_Imports.razor:6-9` (add `@using DRYL.Components.Theming`)
- Test: `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs`

**Interfaces:**
- Consumes: `IDrylThemeService`, `DrylTheme`, `DrylThemes` (Tasks 1–3); `ToCssVariables()` (internal).
- Produces: `<DrylThemeProvider Theme="DrylTheme?" />` in namespace `DRYL.Components`. Renders `<style>:root { <seeds> }</style>`. On runtime theme change, calls `dryl.theme.apply(varsString)`. JS guarded by an `_attached` flag set on first render.

- [ ] **Step 1: Write the failing test**

Create `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs`:

```csharp
using Bunit;
using DRYL.Components;
using DRYL.Components.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Theming;

public class DrylThemeProviderTests : BunitContext
{
    public DrylThemeProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<IDrylThemeService, DrylThemeService>();
    }

    [Fact]
    public void Renders_default_seeds_into_root_style_when_no_theme()
    {
        var cut = Render<DrylThemeProvider>();

        var style = cut.Find("style");
        Assert.Contains(":root", style.TextContent);
        Assert.Contains("--accent-a:#7c5cff;", style.TextContent);
        Assert.Contains("--accent-b:#22d3ee;", style.TextContent);
    }

    [Fact]
    public void Applies_supplied_theme_seeds()
    {
        var cut = Render<DrylThemeProvider>(ps => ps.Add(p => p.Theme, DrylThemes.Ember));

        var style = cut.Find("style");
        Assert.Contains("--accent-a:#f59e0b;", style.TextContent);
        Assert.Contains("--accent-b:#f43f5e;", style.TextContent);
    }

    [Fact]
    public async Task Reacts_to_runtime_theme_change()
    {
        var svc = Services.GetRequiredService<IDrylThemeService>();
        var cut = Render<DrylThemeProvider>();

        await cut.InvokeAsync(() => svc.SetThemeAsync(DrylThemes.Verdant));

        var style = cut.Find("style");
        Assert.Contains("--accent-a:#34d399;", style.TextContent);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeProviderTests"`
Expected: FAIL — `DrylThemeProvider` does not exist.

- [ ] **Step 3: Write minimal implementation**

Add `@using DRYL.Components.Theming` to `DRYL.Components/_Imports.razor` (after line 9):

```razor
@using DRYL.Components.Theming
```

Append to `DRYL.Components/wwwroot/js/dryl.js`:

```js
/* --------------------------------------------------------------
 * Theme — apply DRYL theme seed variables to the document root.
 * Called by DrylThemeProvider on runtime theme changes; the
 * registered @property color seeds + :root transition make the
 * derived color-mix chain glide. `vars` is "--k:v;--k:v;".
 * -------------------------------------------------------------- */
window.dryl.theme = {
    apply(vars) {
        const root = document.documentElement;
        (vars || '').split(';').forEach(pair => {
            const i = pair.indexOf(':');
            if (i > 0) {
                root.style.setProperty(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
            }
        });
    }
};
```

Create `DRYL.Components/Components/Surfaces/DrylThemeProvider.razor`:

```razor
@namespace DRYL.Components
@using DRYL.Components.Theming
@inject IDrylThemeService ThemeService
@inject IJSRuntime JS
@implements IDisposable

@*  ─────────────────────────────────────────────────────────
    DrylThemeProvider — applies the active DrylTheme's seed
    custom properties to :root.

    Place once in your root layout, above the other providers:
        <DrylThemeProvider Theme="DrylThemes.Nebula" />

    On render it writes the seeds into an inline <style> for
    :root, so first paint (incl. Blazor Server prerender) is
    correct with no JS. Runtime theme changes are pushed to
    document.documentElement via dryl.theme.apply, where the
    registered @property color seeds + :root transition make
    the derived color-mix chain glide.
    ───────────────────────────────────────────────────────── *@

<style>@($":root {{ {_vars} }}")</style>

@code {
    /// <summary>
    /// The theme to apply on startup. When omitted, <see cref="DrylThemes.Default"/>
    /// (the signature DRYL look) is used. After startup, switch themes at runtime
    /// via <see cref="IDrylThemeService"/>.
    /// </summary>
    [Parameter] public DrylTheme? Theme { get; set; }

    private string _vars = "";
    private bool _attached;

    protected override async Task OnInitializedAsync()
    {
        ThemeService.OnThemeChanged += HandleChangedAsync;

        if (Theme is not null && !ReferenceEquals(Theme, ThemeService.Current))
        {
            // Updates _vars through the handler; JS is skipped because we are not
            // attached yet, so the inline <style> below carries the initial paint.
            await ThemeService.SetThemeAsync(Theme);
        }

        _vars = ThemeService.Current.ToCssVariables();
    }

    private async Task HandleChangedAsync()
    {
        _vars = ThemeService.Current.ToCssVariables();
        await InvokeAsync(StateHasChanged);

        if (_attached)
        {
            try { await JS.InvokeVoidAsync("dryl.theme.apply", _vars); }
            catch (JSDisconnectedException) { /* circuit gone */ }
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // After the first render the inline <style> has painted; subsequent
        // changes go through JS so the @property transition can interpolate.
        if (firstRender) _attached = true;
    }

    public void Dispose() => ThemeService.OnThemeChanged -= HandleChangedAsync;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylThemeProviderTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Surfaces/DrylThemeProvider.razor DRYL.Components/wwwroot/js/dryl.js DRYL.Components/_Imports.razor tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs
git commit -m "feat(theming): DrylThemeProvider with no-flash :root style + runtime apply"
```

---

## Task 7: Sample demo page

**Files:**
- Create: `samples/Pages/DemoTheming.razor`

**Interfaces:**
- Consumes: `DrylThemeProvider`, `IDrylThemeService`, `DrylThemes`, `DrylTheme`, `DrylAccent` (Tasks 1–6), plus existing DRYL components (`DrylCard`, `DrylButton`, `DrylSegmentedControl` or `DrylSelect`, `DrylAiIndicator`).
- Produces: a one-page demo at route `/theming` with a preset switcher and a custom-accent picker that call `IDrylThemeService` and visibly glide.

> No automated test (samples are demo-only per CLAUDE.md §7.3). Verify by running the samples app.

- [ ] **Step 1: Locate the samples nav + a similar page**

Find how existing demo pages register a route and appear in nav (search `samples/Pages` for `@page` and the nav list). Mirror that registration for `/theming`. Confirm `<DrylThemeProvider />` is present in the samples root layout — if not, add it once there.

- [ ] **Step 2: Write the page**

Create `samples/Pages/DemoTheming.razor`:

```razor
@page "/theming"
@using DRYL.Components.Theming
@inject IDrylThemeService Theme

<div class="col fade-in" style="gap: var(--sp-5);">
    <div class="col" style="gap: var(--sp-2);">
        <h2>Theming</h2>
        <p class="lead">
            Set a few seeds — DRYL derives the rest. Switch a preset and watch the
            accent, glows, focus rings and AI aura glide together.
        </p>
    </div>

    <DrylCard>
        <div class="row" style="gap: var(--sp-3); flex-wrap: wrap;">
            <DrylButton Variant="ButtonVariant.Primary" OnClick="@(() => Apply(DrylThemes.Nebula))">Nebula</DrylButton>
            <DrylButton Variant="ButtonVariant.Secondary" OnClick="@(() => Apply(DrylThemes.Ember))">Ember</DrylButton>
            <DrylButton Variant="ButtonVariant.Secondary" OnClick="@(() => Apply(DrylThemes.Verdant))">Verdant</DrylButton>
            <DrylButton Variant="ButtonVariant.Secondary" OnClick="@(() => Apply(DrylThemes.Mono))">Mono</DrylButton>
        </div>
    </DrylCard>

    <DrylCard>
        <div class="col" style="gap: var(--sp-4);">
            <h4>Live preview</h4>
            <div class="row" style="gap: var(--sp-3); flex-wrap: wrap; align-items: center;">
                <DrylButton Variant="ButtonVariant.Primary">Primary</DrylButton>
                <DrylBadge Variant="BadgeVariant.Accent">Accent</DrylBadge>
                <DrylBadge Variant="BadgeVariant.Success">Success</DrylBadge>
                <DrylBadge Variant="BadgeVariant.Warning">Warning</DrylBadge>
                <DrylBadge Variant="BadgeVariant.Danger">Danger</DrylBadge>
                <DrylAiIndicator State="AiState.Streaming" />
            </div>
            <DrylInputText Label="Focus me to see the accent ring" />
            <DrylCard Ai="AiState.Active">
                <p>An AI-aware surface — its aura uses the AI accent (or the brand accent by default).</p>
            </DrylCard>
        </div>
    </DrylCard>

    <DrylCard>
        <div class="col" style="gap: var(--sp-3);">
            <h4>Custom accent</h4>
            <div class="row" style="gap: var(--sp-3); align-items: center; flex-wrap: wrap;">
                <input type="color" @bind="_a" @bind:event="oninput" aria-label="Accent A" />
                <input type="color" @bind="_b" @bind:event="oninput" aria-label="Accent B" />
                <DrylButton Variant="ButtonVariant.Primary" OnClick="ApplyCustom">Apply</DrylButton>
            </div>
        </div>
    </DrylCard>
</div>

@code {
    private string _a = "#7c5cff";
    private string _b = "#22d3ee";

    private Task Apply(DrylTheme theme) => Theme.SetThemeAsync(theme);
    private Task ApplyCustom() => Theme.SetAccentAsync(_a, _b);
}
```

> If any referenced component/enum name differs in this repo (e.g. `BadgeVariant`, `ButtonVariant`), adjust to the actual names — check a sibling demo page. The page's value is demonstrating the glide, not exact widget choice.

- [ ] **Step 3: Run the samples app and verify**

Build and run the samples app, navigate to `/theming`, click presets, confirm the accent/glow/AI-aura glide smoothly and the custom picker works.
Run: `dotnet build` (solution) to ensure the page compiles.
Expected: builds; page switches themes with a visible transition.

- [ ] **Step 4: Commit**

```bash
git add samples/Pages/DemoTheming.razor
git commit -m "docs(samples): DemoTheming page with live preset + custom accent switcher"
```

---

## Task 8: Reference docs (DESIGN_TOKENS, THEMING, CHANGELOG, CLAUDE.md)

**Files:**
- Modify: `DESIGN_TOKENS.md`
- Create: `THEMING.md`
- Modify: `CHANGELOG.md`
- Modify: `CLAUDE.md` (§7.2)

**Interfaces:** none (docs only).

- [ ] **Step 1: Add a "Theming & Seed Derivation" section to `DESIGN_TOKENS.md`**

After the "Semantic" colors table, add a section explaining: the seed vs derived split (seeds = `--accent-a/-b`, `--ai-a/-b`, semantics; derived = `--accent-soft/-line`, `--glow-*`, body bg, `.ai-aura-*`); that derivation is done in `dryl.css` via `color-mix()`; that `--ai-a/--ai-b` default to the brand accent (opt-in divergence); and that the seeds are registered `@property` colors so theme changes transition over `--dur-slow` (reduced-motion → instant). Note that consumers set seeds via `DrylTheme` / `DrylThemeProvider`, never by editing `dryl.css`.

- [ ] **Step 2: Create `THEMING.md`**

Write a consumer guide with these sections, using real snippets from this plan:
- *Quick start* — add `<DrylThemeProvider Theme="DrylThemes.Ember" />` to the root layout.
- *Built-in presets* — Nebula (default), Ember, Verdant, Mono.
- *Switch at runtime* — inject `IDrylThemeService`, call `SetThemeAsync` / `SetAccentAsync`; the change glides.
- *Build your own* — construct a `DrylTheme` with `Accent`, optional `AiAccent`, optional `Semantic`.
- *What's themeable* — accents + AI accent + semantics; the dark glass core stays fixed (by design).
- *The seed→derived model* — one paragraph on why you only set a few values.
- *Reduced motion* — transitions are gated; reduced-motion users get an instant swap.

- [ ] **Step 3: Add a CHANGELOG entry**

In `CHANGELOG.md` under `[Unreleased] → Added` (create the `Added` sub-heading if absent):

```markdown
### Added
- `DrylThemeProvider` — Root provider that applies a customizable color theme; place once in the root layout
- `IDrylThemeService` — Runtime theme switching (`SetThemeAsync` / `SetAccentAsync`) with an animated transition
- `DrylTheme` / `DrylThemes` — Strongly-typed themes + curated presets (Nebula default, Ember, Verdant, Mono); set a few seeds, DRYL derives the rest
- `--ai-a` / `--ai-b` CSS tokens — Optional separate AI accent; defaults to the brand accent (opt-in divergence)
```

```markdown
### Changed
- `dryl.css` — Accent-derived tokens (`--accent-soft`, `--accent-line`, `--glow-accent`, `--glow-soft`, body ambient glow, AI aura) now derive from seed variables via `color-mix()`; the default theme is visually unchanged
```

- [ ] **Step 4: Update `CLAUDE.md` §7.2**

The README component table is being removed (Task 10). Update §7.2 so future agents don't try to re-add it: replace the "component table" instructions with a note that the canonical component list lives at **components.dryl.dev** (driven by the website's `ComponentCatalog`), and that a new component is registered there + in the changelog — not in a README table. Keep the rest of §7 intact.

- [ ] **Step 5: Commit**

```bash
git add DESIGN_TOKENS.md THEMING.md CHANGELOG.md CLAUDE.md
git commit -m "docs(theming): DESIGN_TOKENS theming section, THEMING guide, changelog, CLAUDE note"
```

---

## Task 9: Holistic README rewrite

**Files:**
- Modify: `README.md` (full rewrite)

**Interfaces:** none (docs only).

**Goal:** roughly half the length; remove the full component table and the long `DrylTable` / `Dialog` deep-dives; promote theming high; point to **components.dryl.dev** as the live component reference.

- [ ] **Step 1: Rewrite `README.md` to this structure**

Preserve verbatim: the badge block (lines 3-9), the hero screenshot + caption (lines 18-20), the status blockquote (line 22), and the Contributing / Support / Credits / License sections (they stay, lightly trimmed). Replace the middle with the lean structure below.

1. **Header** — keep badges, tagline `**Dark. Glassy. Alive — and AI-native.**`, the one-line pitch, and the `dotnet add package DRYL.Components` block.
2. **Hero screenshot** — keep as-is.
3. **Status** — keep as-is.
4. **Why DRYL** — condense to a short intro line + the existing bullet list (AI-native, dark only, accents glow, motion intentional, one token file, ~90 components/zero JS deps, accessible). Trim the long prose paragraphs to 1–2 sentences each.
5. **Make it yours — Theming** *(new section, placed right after "Why DRYL")*:

```markdown
## Make it yours — theming

Most libraries make you hand-tune dozens of colors and hope they stay coherent.
DRYL flips that: you set a few **seeds** and the system **derives** the rest —
gradient, soft fills, accent lines, glow rings and the AI aura all stay in sync.

```razor
@* One line in your root layout *@
<DrylThemeProvider Theme="DrylThemes.Ember" />
```

Switch at runtime — the whole accent chain *glides* (and respects
`prefers-reduced-motion`):

```csharp
@inject IDrylThemeService Theme

await Theme.SetAccentAsync("#f59e0b", "#f43f5e"); // or Theme.SetThemeAsync(DrylThemes.Verdant)
```

Ships with curated presets — **Nebula** (default), **Ember**, **Verdant**,
**Mono** — and a dedicated, opt-in **AI accent** so AI moments can glow in their
own color. The dark glass core stays fixed by design, so a theme can't break the
look. Full guide: [`THEMING.md`](THEMING.md).
```

6. **Quick start** — keep the 5-step install/register/stylesheet/providers/use flow; add `<DrylThemeProvider />` to the providers step.
7. **AI Mode** — keep but condense to the five-state table + one short wiring snippet; drop the long per-component list (point to the website).
8. **Where to go deeper** *(replaces the component table + the long Table/Dialog sections)*:

```markdown
## Where to go deeper

DRYL ships **~90 components across 8 categories** — actions, surfaces,
navigation, data, inputs, layout, feedback, and a dedicated **Intelligence**
set for agent UIs (token streams, tool-call traces, RAG citations,
human-in-the-middle review).

The complete, interactive reference — every component, variant and AI state —
lives at **[components.dryl.dev](https://components.dryl.dev/)**.

For the design language and customization model:
[`DESIGN_TOKENS.md`](DESIGN_TOKENS.md) ·
[`THEMING.md`](THEMING.md) ·
[`COMPONENT_PATTERNS.md`](COMPONENT_PATTERNS.md).
```

9. **Roadmap teaser** — one line, e.g. `> Customization is just getting started — theming is step one toward a fully tunable DRYL.`
10. **Contributing / Support / Credits / License** — keep, lightly trimmed.

Remove entirely: the `## What's in the box` table (lines 356-458), the `## DrylTable — declarative data grid` section (lines 233-352), and the `## Dialog & DialogService` section (lines 148-231) — the website is now their home (keep a one-line dialog/table mention under AI Mode or "Where to go deeper" if it reads naturally).

- [ ] **Step 2: Verify links and length**

Confirm: no broken relative links; the component table is gone; theming appears above the fold-ish (right after Why DRYL); the file is meaningfully shorter (target ≲ 260 lines). Skim once for tone consistency.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs(readme): holistic rewrite — promote theming, drop component table (→ components.dryl.dev)"
```

---

## Task 10: Full suite + final verification

**Files:** none (verification only).

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: PASS — all existing tests plus the new Theming tests (model, presets, service, registration, css derivation, provider). No regressions.

- [ ] **Step 2: Build the whole solution**

Run: `dotnet build DRYL.slnx -c Release`
Expected: build succeeds (library, samples, tests) on the repo's target frameworks.

- [ ] **Step 3: Manual default-render gate**

Confirm again (samples app or prototype) that with no theme / `DrylThemes.Nebula` the UI is visually identical to before this change. Then switch to `Ember`/`Verdant`/`Mono` and confirm a smooth glide and coherent result (no stray violet left behind in glows/aura).

- [ ] **Step 4: Confirm docs checklist (CLAUDE.md §7.4)**

Verify: `CHANGELOG.md` has the `[Unreleased]` entries; `README.md` rewritten with theming promoted and table removed; `DESIGN_TOKENS.md` + `THEMING.md` present; `DRYL.Components.csproj` `<Version>` untouched (maintainer-owned).

---

## Self-Review (completed during planning)

**Spec coverage:**
- §2 themeable seeds → Tasks 1 (model), 5 (css `--ai-*`, semantics).
- §3 CSS derivation + `@property` + transition → Task 5.
- §4 C# API (model, presets, service) → Tasks 1, 2, 3.
- §5 provider (no-flash + runtime + interop safety) → Task 6.
- §6 DI → Task 4.
- §7 a11y/motion gating → Task 5 (media query), Task 6 (provider is visual-only).
- §8 docs (TOKENS, THEMING, CHANGELOG, sample, README) → Tasks 7, 8, 9.
- §9 README holistic rewrite → Task 9.
- §10 unit boundaries → reflected in task split.
- §11 risks (default unchanged, specificity, semantics-not-animated) → Task 5 equivalence note + Step 5 visual gate, Task 10 gate.

**Placeholder scan:** no TBD/TODO; every code step shows full code; the one explicit "adjust to actual names" note (Task 7) is a demo-only widget-name caveat, not a logic gap.

**Type consistency:** `DrylAccent(A,B)`, `DrylSemantic{Success,Warning,Danger,Info}`, `DrylTheme{Accent,AiAccent,Semantic,ToCssVariables()}`, `IDrylThemeService{Current,SetThemeAsync,SetAccentAsync,OnThemeChanged}`, `DrylThemes{Nebula,Ember,Verdant,Mono,Default}` — used consistently across Tasks 1–7. JS `window.dryl.theme.apply(vars)` matches the provider's `JS.InvokeVoidAsync("dryl.theme.apply", _vars)`.
