using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for the gliding active indicator of <see cref="DrylDrawer"/> (I29): the
/// drawer renders one inert ink element in its scrolling region and hands the
/// glide to <c>dryl.motion.navGlide</c>.
/// </summary>
public class DrylDrawerTests : BunitContext
{
    public DrylDrawerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Slotted_drawer_renders_the_ink_first_in_the_scrolling_region()
    {
        var cut = Render<DrylDrawer>(ps => ps
            .Add(p => p.Header, "<b>Brand</b>")
            .Add(p => p.Content, "<a class=\"nav-item\">Overview</a>"));

        var content = cut.Find(".sidebar-content");
        Assert.Equal("nav-ink", content.FirstElementChild!.ClassName);
        Assert.Single(cut.FindAll(".nav-ink"));
    }

    [Fact]
    public void Plain_drawer_renders_the_ink_directly_in_the_drawer()
    {
        var cut = Render<DrylDrawer>(ps => ps.AddChildContent("<a class=\"nav-item\">Overview</a>"));

        var aside = cut.Find("aside.sidebar");
        Assert.Equal("nav-ink", aside.FirstElementChild!.ClassName);
    }

    [Fact]
    public void Ink_is_hidden_from_assistive_technology()
    {
        var cut = Render<DrylDrawer>(ps => ps.AddChildContent("<a class=\"nav-item\">Overview</a>"));

        Assert.Equal("true", cut.Find(".nav-ink").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Glide_is_attached_once_after_the_first_render()
    {
        var cut = Render<DrylDrawer>(ps => ps.AddChildContent("<a class=\"nav-item\">Overview</a>"));

        cut.Render();

        Assert.Single(JSInterop.Invocations, i => i.Identifier == "dryl.motion.navGlide");
    }

    [Fact]
    public async Task Dispose_detaches_the_glide()
    {
        var cut = Render<DrylDrawer>(ps => ps.AddChildContent("<a class=\"nav-item\">Overview</a>"));

        await cut.Instance.DisposeAsync();

        Assert.Single(JSInterop.Invocations, i => i.Identifier == "dryl.motion.disposeNavGlide");
    }
}
