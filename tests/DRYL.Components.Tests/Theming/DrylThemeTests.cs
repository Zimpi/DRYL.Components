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

    [Fact]
    public void ToCssVariables_includes_only_specified_chart_slots()
    {
        var theme = new DrylTheme
        {
            Accent = new DrylAccent("#7c5cff", "#22d3ee"),
            Charts = new DrylChartPalette { Series3 = "#0aa2b5" },
        };

        var css = theme.ToCssVariables();

        Assert.Contains("--chart-3:#0aa2b5;", css);
        Assert.DoesNotContain("--chart-1", css);
        Assert.DoesNotContain("--chart-2", css);
        Assert.DoesNotContain("--chart-4", css);
    }

    [Fact]
    public void ToCssVariables_omits_chart_slots_when_charts_null()
    {
        var theme = new DrylTheme { Accent = new DrylAccent("#7c5cff", "#22d3ee") };

        Assert.DoesNotContain("--chart", theme.ToCssVariables());
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
        Assert.Null(DrylThemes.Nebula.Charts);
    }

    [Fact]
    public void Presets_with_colliding_accent_hues_curate_chart_slots()
    {
        // Ember's derived series 1 is amber → the fixed amber anchor moves out.
        Assert.Equal("#0aa2b5", DrylThemes.Ember.Charts?.Series3);
        // Verdant's derived series 1 is green → the fixed green anchor moves out.
        Assert.Equal("#8b7cf8", DrylThemes.Verdant.Charts?.Series4);
        // Mono's slate seeds carry no usable hue → full validated palette pinned.
        Assert.Equal("#8b7cf8", DrylThemes.Mono.Charts?.Series1);
        Assert.Equal("#5583e3", DrylThemes.Mono.Charts?.Series6);
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
