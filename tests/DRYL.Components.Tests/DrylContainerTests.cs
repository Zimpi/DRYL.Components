using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylContainerTests : BunitContext
{
    [Fact]
    public void Default_size_is_lg_and_centers()
    {
        var cut = Render<DrylContainer>(ps => ps.AddChildContent("x"));
        var style = cut.Find("div.container").GetAttribute("style")!;
        Assert.Contains("max-width: 64rem", style);
        Assert.Contains("margin-inline: auto", style);
        Assert.Contains("clamp(var(--sp-4), 4vw, var(--sp-6))", style);
    }

    [Theory]
    [InlineData(DrylContainer.ContainerSize.Sm, "40rem")]
    [InlineData(DrylContainer.ContainerSize.Md, "52rem")]
    [InlineData(DrylContainer.ContainerSize.Xl, "80rem")]
    public void Size_maps_to_max_width(DrylContainer.ContainerSize s, string expected)
    {
        var cut = Render<DrylContainer>(ps => ps.Add(p => p.Size, s).AddChildContent("x"));
        Assert.Contains($"max-width: {expected}", cut.Find("div.container").GetAttribute("style"));
    }

    [Fact]
    public void Full_emits_no_max_width()
    {
        var cut = Render<DrylContainer>(ps => ps
            .Add(p => p.Size, DrylContainer.ContainerSize.Full).AddChildContent("x"));
        Assert.DoesNotContain("max-width", cut.Find("div.container").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Merges_class_and_forwards_attributes()
    {
        var cut = Render<DrylContainer>(ps => ps
            .Add(p => p.Class, "mine").AddUnmatched("data-x", "1").AddChildContent("x"));
        var el = cut.Find("div.container");
        Assert.Contains("mine", el.GetAttribute("class"));
        Assert.Equal("1", el.GetAttribute("data-x"));
    }
}
