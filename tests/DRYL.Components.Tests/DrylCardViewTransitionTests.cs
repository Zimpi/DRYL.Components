using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylCard"/>'s view-transition morph-endpoint opt-in:
/// ViewTransitionName renders the CSS name; DepthGlass additionally tags the
/// transition class and the [data-vt-depth] marker the JS filter-injection keys on.
/// </summary>
public class DrylCardViewTransitionTests : BunitContext
{
    public DrylCardViewTransitionTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void No_name_renders_no_style_or_marker()
    {
        var cut = Render<DrylCard>(ps => ps.AddChildContent("x"));

        var root = cut.Find(".glass-card");
        Assert.Null(root.GetAttribute("style"));
        Assert.False(root.HasAttribute("data-vt-depth"));
    }

    [Fact]
    public void Name_renders_view_transition_name()
    {
        var cut = Render<DrylCard>(ps => ps
            .Add(p => p.ViewTransitionName, "hero-card")
            .AddChildContent("x"));

        var style = cut.Find(".glass-card").GetAttribute("style");
        Assert.Contains("view-transition-name: hero-card", style);
        Assert.DoesNotContain("dryl-depth", style);
    }

    [Fact]
    public void DepthGlass_adds_transition_class_and_marker()
    {
        var cut = Render<DrylCard>(ps => ps
            .Add(p => p.ViewTransitionName, "hero-card")
            .Add(p => p.ViewTransitionStyle, DrylViewTransitionStyle.DepthGlass)
            .AddChildContent("x"));

        var root = cut.Find(".glass-card");
        Assert.Contains("view-transition-class: dryl-depth", root.GetAttribute("style"));
        Assert.True(root.HasAttribute("data-vt-depth"));
    }
}
