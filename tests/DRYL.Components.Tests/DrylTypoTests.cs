using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The look rides the variant class (styled in dryl.css), the tag is separate.
/// What the class computes to is verified in the browser; here: the markup contract.
/// </summary>
public class DrylTypoTests : BunitContext
{
    [Fact]
    public void Variant_picks_tag_and_class()
    {
        var cut = Render<DrylTypo>(p => p
            .Add(x => x.Variant, DrylTypo.TypoVariant.Lead)
            .AddChildContent("Standfirst"));

        var el = cut.Find("p");
        Assert.Contains("typo", el.ClassList);
        Assert.Contains("typo-lead", el.ClassList);
    }

    [Fact]
    public void As_changes_the_tag_and_keeps_the_variant_class()
    {
        var cut = Render<DrylTypo>(p => p
            .Add(x => x.Variant, DrylTypo.TypoVariant.H2)
            .Add(x => x.As, "h1"));

        var el = cut.Find("h1");
        Assert.Contains("typo-h2", el.ClassList);
        Assert.DoesNotContain("typo-h1", el.ClassList);
    }

    [Fact]
    public void Gradient_adds_the_shared_class_and_suppresses_the_inline_colour()
    {
        var cut = Render<DrylTypo>(p => p
            .Add(x => x.Variant, DrylTypo.TypoVariant.H1)
            .Add(x => x.Color, DrylTypo.TypoColor.Muted)
            .Add(x => x.Gradient, true));

        var el = cut.Find("h1");
        Assert.Contains("gradient-text", el.ClassList);
        Assert.Null(el.GetAttribute("style"));
    }

    [Fact]
    public void The_variant_rules_precede_gradient_text_in_dryl_css()
    {
        // Equal specificity: .gradient-text must come later, or a variant's
        // colour would cover the gradient's transparent one.
        var css = File.ReadAllText(Path.Combine(RepoRoot(), "code", "DRYL.Components", "wwwroot", "dryl.css"));
        var typo = css.IndexOf(".typo-h1, .typo-h2, .typo-h3, .typo-h4 { color:", StringComparison.Ordinal);
        var gradient = css.IndexOf(".gradient-text {", StringComparison.Ordinal);

        Assert.True(typo >= 0, "variant colour rule not found");
        Assert.True(gradient > typo, ".gradient-text must follow the .typo-* rules");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DRYL.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DRYL.slnx not found above the test directory.");
    }
}
