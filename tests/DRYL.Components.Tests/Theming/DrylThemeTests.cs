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
