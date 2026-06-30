using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylSpacerTests : BunitContext
{
    [Fact]
    public void Flexible_by_default()
    {
        var cut = Render<DrylSpacer>();
        var el = cut.Find("div.spacer");
        Assert.Contains("flex: 1 1 auto", el.GetAttribute("style"));
        Assert.Equal("true", el.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Fixed_size_maps_to_spacing_token()
    {
        var cut = Render<DrylSpacer>(ps => ps.Add(p => p.Size, DrylStack.StackGap.Xl));
        var style = cut.Find("div.spacer").GetAttribute("style")!;
        Assert.Contains("var(--sp-5)", style);   // Xl
        Assert.DoesNotContain("flex: 1", style);
    }
}
