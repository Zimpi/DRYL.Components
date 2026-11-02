using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// The wrapper contract of <see cref="DrylTooltip"/>, as
/// <c>specs/E7 Feedback/F2 DrylTooltip.md</c> states it. The bubble itself is
/// driven by <c>dryl.tooltip</c> and verified in the browser.
/// </summary>
public class DrylTooltipTests : BunitContext
{
    [Fact]
    public void Wrapper_carries_text_and_placement_around_the_trigger()
    {
        var cut = Render<DrylTooltip>(ps => ps
            .Add(p => p.Text, "Settings")
            .Add(p => p.Placement, DrylTooltip.TooltipPlacement.Left)
            .AddChildContent("<button aria-label=\"Settings\"></button>"));

        var wrap = cut.Find(".tt-wrap");
        Assert.Equal("Settings", wrap.GetAttribute("data-tt"));
        Assert.Equal("left", wrap.GetAttribute("data-tt-placement"));
        Assert.Single(wrap.Children);
        Assert.Equal("BUTTON", wrap.Children[0].TagName);
    }

    [Theory]
    [InlineData(DrylTooltip.TooltipPlacement.Top, "top")]
    [InlineData(DrylTooltip.TooltipPlacement.Bottom, "bottom")]
    [InlineData(DrylTooltip.TooltipPlacement.Left, "left")]
    [InlineData(DrylTooltip.TooltipPlacement.Right, "right")]
    [InlineData((DrylTooltip.TooltipPlacement)99, "top")]
    public void Each_placement_maps_to_its_string(DrylTooltip.TooltipPlacement placement, string expected)
    {
        var cut = Render<DrylTooltip>(ps => ps.Add(p => p.Text, "x").Add(p => p.Placement, placement));

        Assert.Equal(expected, cut.Find(".tt-wrap").GetAttribute("data-tt-placement"));
    }

    [Fact]
    public void No_delay_renders_no_delay_attribute()
    {
        var cut = Render<DrylTooltip>(ps => ps.Add(p => p.Text, "x"));

        Assert.False(cut.Find(".tt-wrap").HasAttribute("data-tt-delay"));
    }

    [Fact]
    public void Delay_renders_as_whole_milliseconds()
    {
        var cut = Render<DrylTooltip>(ps => ps
            .Add(p => p.Text, "x")
            .Add(p => p.Delay, TimeSpan.FromMilliseconds(499.6)));

        Assert.Equal("500", cut.Find(".tt-wrap").GetAttribute("data-tt-delay"));
    }

    [Fact]
    public void Delay_is_culture_invariant()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            var cut = Render<DrylTooltip>(ps => ps
                .Add(p => p.Text, "x")
                .Add(p => p.Delay, TimeSpan.FromSeconds(1.5)));

            Assert.Equal("1500", cut.Find(".tt-wrap").GetAttribute("data-tt-delay"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Negative_delay_counts_as_none()
    {
        var cut = Render<DrylTooltip>(ps => ps
            .Add(p => p.Text, "x")
            .Add(p => p.Delay, TimeSpan.FromMilliseconds(-300)));

        Assert.False(cut.Find(".tt-wrap").HasAttribute("data-tt-delay"));
    }

    [Fact]
    public void Class_merges_and_attributes_pass_through()
    {
        var cut = Render<DrylTooltip>(ps => ps
            .Add(p => p.Text, "x")
            .Add(p => p.Class, "mine")
            .AddUnmatched("data-test", "t1"));

        var wrap = cut.Find(".tt-wrap");
        Assert.Contains("mine", wrap.ClassList);
        Assert.Equal("t1", wrap.GetAttribute("data-test"));
        Assert.False(wrap.HasAttribute("tabindex"));
    }
}
