using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural + API-freeze tests for <see cref="DrylExpansion"/>. Pins the
/// renamed public surface (<c>Open</c> / <c>OpenChanged</c>, formerly
/// <c>IsOpen</c> / <c>IsOpenChanged</c>) and the toggle semantics.
/// </summary>
public class DrylExpansionTests : BunitContext
{
    [Fact]
    public void Renders_title()
    {
        var cut = Render<DrylExpansion>(ps => ps.Add(p => p.Title, "Service health"));

        Assert.Contains("Service health", cut.Find(".expansion-title").TextContent);
    }

    [Fact]
    public void Closed_by_default()
    {
        var cut = Render<DrylExpansion>(ps => ps.Add(p => p.Title, "Panel"));

        Assert.DoesNotContain("is-open", cut.Find(".expansion").GetAttribute("class"));
        Assert.Equal("false", cut.Find("button.expansion-header").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Open_true_reflects_expanded_state()
    {
        var cut = Render<DrylExpansion>(ps => ps
            .Add(p => p.Title, "Panel")
            .Add(p => p.Open, true));

        Assert.Contains("is-open", cut.Find(".expansion").GetAttribute("class"));
        Assert.Equal("true", cut.Find("button.expansion-header").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Clicking_header_toggles_and_invokes_OpenChanged()
    {
        bool? captured = null;
        var cut = Render<DrylExpansion>(ps => ps
            .Add(p => p.Title, "Panel")
            .Add(p => p.OpenChanged, (bool v) => captured = v));

        cut.Find("button.expansion-header").Click();

        Assert.True(captured);  // OpenChanged fired with the new (open) state
        Assert.Equal("true", cut.Find("button.expansion-header").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Disabled_panel_does_not_toggle()
    {
        bool fired = false;
        var cut = Render<DrylExpansion>(ps => ps
            .Add(p => p.Title, "Panel")
            .Add(p => p.Disabled, true)
            .Add(p => p.OpenChanged, (bool _) => fired = true));

        cut.Find("button.expansion-header").Click();

        Assert.False(fired);
        Assert.Equal("false", cut.Find("button.expansion-header").GetAttribute("aria-expanded"));
    }
}
