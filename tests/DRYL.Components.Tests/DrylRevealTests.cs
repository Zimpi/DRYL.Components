using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylReveal"/> — the scroll-triggered
/// staggered entrance wrapper. JSInterop is Loose because the component wires
/// dryl.motion.observe on first render.
/// </summary>
public class DrylRevealTests : BunitContext
{
    public DrylRevealTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_wrapper_with_rise_transition_by_default()
    {
        var cut = Render<DrylReveal>(ps => ps.AddChildContent("<span>body</span>"));

        var el = cut.Find(".reveal");
        Assert.Contains("reveal--rise", el.GetAttribute("class"));
        Assert.Contains("body", el.InnerHtml);
    }

    [Theory]
    [InlineData(RevealTransition.Fade, "reveal--fade")]
    [InlineData(RevealTransition.ScaleIn, "reveal--scale-in")]
    [InlineData(RevealTransition.Rise, "reveal--rise")]
    public void Transition_maps_to_variant_class(RevealTransition transition, string expected)
    {
        var cut = Render<DrylReveal>(ps => ps
            .Add(p => p.Transition, transition)
            .AddChildContent("<span>body</span>"));

        Assert.Contains(expected, cut.Find(".reveal").GetAttribute("class"));
    }

    [Fact]
    public void Observe_is_invoked_on_render()
    {
        Render<DrylReveal>(ps => ps
            .Add(p => p.Stagger, true)
            .AddChildContent("<span>body</span>"));

        Assert.NotEmpty(JSInterop.Invocations["dryl.motion.observe"]);
    }
}
