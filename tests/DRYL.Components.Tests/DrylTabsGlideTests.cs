using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for the gliding-underline retrofit on <see cref="DrylTabs"/>: the
/// shared <c>.tab-ink</c> indicator (default) vs. the per-tab fallback.
/// JSInterop is Loose because the indicator is measured via dryl.motion.
/// </summary>
public class DrylTabsGlideTests : BunitContext
{
    public DrylTabsGlideTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylTabs> RenderTabs(bool? animateIndicator = null)
        => Render<DrylTabs>(ps =>
        {
            if (animateIndicator is { } v) ps.Add(p => p.AnimateIndicator, v);
            ps.AddChildContent<DrylTab>(tp => tp.Add(t => t.Id, "a").Add(t => t.Label, "A"));
            ps.AddChildContent<DrylTab>(tp => tp.Add(t => t.Id, "b").Add(t => t.Label, "B"));
        });

    [Fact]
    public void Glide_indicator_is_rendered_by_default()
    {
        var cut = RenderTabs();

        Assert.Contains("tabs--glide", cut.Find(".tabs").GetAttribute("class"));
        Assert.Single(cut.FindAll(".tab-ink"));
    }

    [Fact]
    public void Active_tab_is_marked_for_the_indicator()
    {
        var cut = RenderTabs();

        var active = cut.Find("[data-dryl-ink-active=\"true\"]");
        Assert.Equal("A", active.TextContent.Trim());
    }

    [Fact]
    public void Disabling_indicator_falls_back_to_per_tab_underline()
    {
        var cut = RenderTabs(animateIndicator: false);

        Assert.DoesNotContain("tabs--glide", cut.Find(".tabs").GetAttribute("class"));
        Assert.Empty(cut.FindAll(".tab-ink"));
    }
}
