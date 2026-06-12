using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Rendering tests for <see cref="DrylBadge"/>. These double as a worked
/// example of how to bUnit-test a DRYL component: render it, assert on the
/// emitted markup / CSS classes.
/// </summary>
public class DrylBadgeTests : BunitContext
{
    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<DrylBadge>(ps => ps.AddChildContent("Healthy"));

        Assert.Contains("Healthy", cut.Markup);
    }

    [Fact]
    public void Default_kind_has_only_base_class()
    {
        var cut = Render<DrylBadge>(ps => ps.AddChildContent("Neutral"));

        var span = cut.Find("span");
        Assert.Equal("badge", span.GetAttribute("class"));
    }

    [Theory]
    [InlineData(DrylBadge.BadgeKind.Accent, "badge-accent")]
    [InlineData(DrylBadge.BadgeKind.Success, "badge-success")]
    [InlineData(DrylBadge.BadgeKind.Warning, "badge-warning")]
    [InlineData(DrylBadge.BadgeKind.Danger, "badge-danger")]
    public void Kind_maps_to_modifier_class(DrylBadge.BadgeKind kind, string expected)
    {
        var cut = Render<DrylBadge>(ps => ps
            .Add(p => p.Kind, kind)
            .AddChildContent("x"));

        Assert.Contains(expected, cut.Find("span").GetAttribute("class"));
    }

    [Fact]
    public void Dot_adds_dot_modifier()
    {
        var cut = Render<DrylBadge>(ps => ps
            .Add(p => p.Dot, true)
            .AddChildContent("live"));

        Assert.Contains("badge-dot", cut.Find("span").GetAttribute("class"));
    }

    [Fact]
    public void Forwards_unmatched_attributes()
    {
        var cut = Render<DrylBadge>(ps => ps
            .AddChildContent("x")
            .AddUnmatched("data-test", "yes"));

        Assert.Equal("yes", cut.Find("span").GetAttribute("data-test"));
    }
}
