using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylAspectRatioTests : BunitContext
{
    [Theory]
    [InlineData(DrylAspectRatio.AspectRatio.Square, "1 / 1")]
    [InlineData(DrylAspectRatio.AspectRatio.Video, "16 / 9")]
    [InlineData(DrylAspectRatio.AspectRatio.Photo, "4 / 3")]
    [InlineData(DrylAspectRatio.AspectRatio.Wide, "21 / 9")]
    public void Ratio_maps_to_css(DrylAspectRatio.AspectRatio r, string expected)
    {
        var cut = Render<DrylAspectRatio>(ps => ps.Add(p => p.Ratio, r).AddChildContent("x"));
        var style = cut.Find("div.aspect").GetAttribute("style")!;
        Assert.Contains($"aspect-ratio: {expected}", style);
        Assert.Contains("max-width: 100%", style);
    }

    [Fact]
    public void Custom_uses_ratio_value()
    {
        var cut = Render<DrylAspectRatio>(ps => ps
            .Add(p => p.Ratio, DrylAspectRatio.AspectRatio.Custom)
            .Add(p => p.RatioValue, "3 / 2").AddChildContent("x"));
        Assert.Contains("aspect-ratio: 3 / 2", cut.Find("div.aspect").GetAttribute("style"));
    }

    [Fact]
    public void Default_is_video()
    {
        var cut = Render<DrylAspectRatio>(ps => ps.AddChildContent("x"));
        Assert.Contains("aspect-ratio: 16 / 9", cut.Find("div.aspect").GetAttribute("style"));
    }
}
