using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylGridTests : BunitContext
{
    [Fact]
    public void Autofit_is_default_and_emits_min_clamp()
    {
        var cut = Render<DrylGrid>(ps => ps.AddChildContent("<div>a</div>"));
        var style = cut.Find("div.grid").GetAttribute("style")!;
        Assert.Contains("repeat(auto-fit, minmax(min(20rem, 100%), 1fr))", style); // Md default
    }

    [Theory]
    [InlineData(DrylGrid.ItemWidth.Xs, "12rem")]
    [InlineData(DrylGrid.ItemWidth.Sm, "16rem")]
    [InlineData(DrylGrid.ItemWidth.Lg, "28rem")]
    public void MinItemWidth_maps_to_rem(DrylGrid.ItemWidth w, string expected)
    {
        var cut = Render<DrylGrid>(ps => ps.Add(p => p.MinItemWidth, w).AddChildContent("x"));
        Assert.Contains($"min({expected}, 100%)", cut.Find("div.grid").GetAttribute("style"));
    }

    [Fact]
    public void Columns_uses_responsive_utility_class_not_inline_template()
    {
        var cut = Render<DrylGrid>(ps => ps.Add(p => p.Columns, 3).AddChildContent("x"));
        var cls = cut.Find("div.grid").GetAttribute("class")!;
        Assert.Contains("cq", cls);
        Assert.Contains("grid-cols-3", cls);
        Assert.DoesNotContain("auto-fit", cut.Find("div.grid").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Gap_maps_to_spacing_token()
    {
        var cut = Render<DrylGrid>(ps => ps
            .Add(p => p.Gap, DrylStack.StackGap.Lg).AddChildContent("x"));
        Assert.Contains("gap: var(--sp-4)", cut.Find("div.grid").GetAttribute("style"));
    }

    [Fact]
    public void Merges_class_and_forwards_attributes()
    {
        var cut = Render<DrylGrid>(ps => ps
            .Add(p => p.Class, "mine").AddUnmatched("data-x", "1").AddChildContent("x"));
        var el = cut.Find("div.grid");
        Assert.Contains("mine", el.GetAttribute("class"));
        Assert.Equal("1", el.GetAttribute("data-x"));
    }
}
