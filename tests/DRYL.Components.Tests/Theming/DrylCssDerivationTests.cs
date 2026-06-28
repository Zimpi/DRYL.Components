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

    [Fact]
    public void Ai_indicator_icon_color_uses_ai_seed_not_brand_accent()
    {
        var css = ReadDrylCss();

        // Locate the .ai-indicator rule block (from ".ai-indicator {" up to the
        // next blank line after ".ai-indicator .ai-indicator-ico {").
        var indicatorIcoStart = css.IndexOf(".ai-indicator .ai-indicator-ico", StringComparison.Ordinal);
        Assert.True(indicatorIcoStart >= 0, ".ai-indicator .ai-indicator-ico rule not found in dryl.css");

        // Extract a reasonable window (200 chars) covering the ico rule body.
        var window = css.Substring(indicatorIcoStart, Math.Min(200, css.Length - indicatorIcoStart));

        Assert.Contains("var(--ai-b)", window);
    }
}
