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
