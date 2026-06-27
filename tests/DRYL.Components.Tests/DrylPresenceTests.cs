using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylPresence"/> — the AnimatePresence-style
/// wrapper that defers a child's unmount until its exit animation finishes.
/// JSInterop is Loose because the component wires dryl.motion.onExit on render.
/// </summary>
public class DrylPresenceTests : BunitContext
{
    public DrylPresenceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Hidden_by_default_renders_nothing()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, false)
            .AddChildContent("<span>body</span>"));

        Assert.Empty(cut.FindAll(".presence"));
    }

    [Fact]
    public void Visible_renders_child_without_enter_class_by_default()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .AddChildContent("<span>body</span>"));

        var el = cut.Find(".presence");
        Assert.Contains("body", el.InnerHtml);
        Assert.DoesNotContain("presence-enter", el.GetAttribute("class"));
    }

    [Fact]
    public void Appear_adds_enter_animation_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .Add(p => p.Appear, true)
            .AddChildContent("<span>body</span>"));

        Assert.Contains("presence-enter", cut.Find(".presence").GetAttribute("class"));
    }

    [Fact]
    public void Transition_maps_to_variant_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .Add(p => p.Transition, PresenceTransition.Scale)
            .AddChildContent("<span>body</span>"));

        Assert.Contains("presence--scale", cut.Find(".presence").GetAttribute("class"));
    }

    [Fact]
    public void Hiding_keeps_child_mounted_with_exit_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .AddChildContent("<span>body</span>"));

        cut.Render(ps => ps.Add(p => p.Visible, false));

        // Still mounted (exit animation playing), now flagged as exiting.
        var el = cut.Find(".presence");
        Assert.Contains("presence-exit", el.GetAttribute("class"));
    }
}
