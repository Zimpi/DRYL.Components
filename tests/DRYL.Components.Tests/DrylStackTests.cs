using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylStackTests : BunitContext
{
    [Fact]
    public void Without_collapse_emits_single_div_with_inline_direction()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Horizontal)
            .AddChildContent("x"));
        Assert.Single(cut.FindAll("div.stack"));
        Assert.Contains("flex-direction: row", cut.Find("div.stack").GetAttribute("style"));
        Assert.DoesNotContain("stack-collapse", cut.Markup);
    }

    [Fact]
    public void CollapseBelow_wraps_in_cq_container_and_omits_inline_direction()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Horizontal)
            .Add(p => p.CollapseBelow, Breakpoint.Md)
            .AddChildContent("x"));
        Assert.NotNull(cut.Find("div.cq"));
        var inner = cut.Find("div.stack");
        Assert.Contains("stack-collapse-md", inner.GetAttribute("class"));
        Assert.DoesNotContain("flex-direction", inner.GetAttribute("style") ?? "");
    }

    [Fact]
    public void CollapseBelow_ignored_when_vertical()
    {
        var cut = Render<DrylStack>(ps => ps
            .Add(p => p.Direction, DrylStack.StackDirection.Vertical)
            .Add(p => p.CollapseBelow, Breakpoint.Md)
            .AddChildContent("x"));
        Assert.Empty(cut.FindAll("div.cq"));
        Assert.Contains("flex-direction: column", cut.Find("div.stack").GetAttribute("style"));
    }
}
