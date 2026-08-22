using Bunit;
using DRYL.Components;
using DRYL.Components.Motion;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylMorph"/>, the generic shared-element hull: it renders
/// one element of the requested tag, claims a morph name only
/// while it is named and active, tags the DepthGlass tier, and reports every
/// render back to <see cref="IDrylMorph"/> so a consumer never writes
/// <c>SignalRendered()</c>.
/// </summary>
public class DrylMorphTests : BunitContext
{
    /// <summary>Counts what the hull reports, and asserts nothing else — the real
    /// service's own contract is covered where it is implemented.</summary>
    private sealed class CountingMorph : IDrylMorph
    {
        public int Signals { get; private set; }
        public Task RunAsync(Action mutate) { mutate(); return Task.CompletedTask; }
        public Task RunAsync(Func<Task> mutate) => mutate();
        public void SignalRendered() => Signals++;
        public Task BeginNavigationAsync(TimeSpan timeout) => Task.CompletedTask;
    }

    private CountingMorph UseFakeTransition()
    {
        var fake = new CountingMorph();
        Services.AddSingleton<IDrylMorph>(fake);
        return fake;
    }

    // ---------------------------------------------------------------- element

    [Fact]
    public void Renders_a_single_div_by_default()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps.AddChildContent("<span>x</span>"));

        var root = cut.Nodes.OfType<AngleSharp.Dom.IElement>().Single();
        Assert.Equal("DIV", root.TagName);
        Assert.Equal("<span>x</span>", root.InnerHtml);
    }

    [Fact]
    public void As_chooses_the_rendered_tag()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.As, "article")
            .AddChildContent("x"));

        Assert.Equal("ARTICLE", cut.Nodes.OfType<AngleSharp.Dom.IElement>().Single().TagName);
    }

    [Fact]
    public void Renders_no_class_of_its_own()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps.AddChildContent("x"));

        Assert.False(cut.Find("div").HasAttribute("class"));
    }

    [Fact]
    public void Class_parameter_is_rendered()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Class, "col-span-2")
            .AddChildContent("x"));

        Assert.Equal("col-span-2", cut.Find("div").GetAttribute("class"));
    }

    [Fact]
    public void Splatted_class_binds_to_the_typed_parameter()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .AddUnmatched("class", "mt-4")
            .AddChildContent("x"));

        Assert.Equal("mt-4", cut.Find("div").GetAttribute("class"));
    }

    [Fact]
    public void Additional_attributes_are_splatted_onto_the_element()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .AddUnmatched("data-testid", "hull")
            .AddChildContent("x"));

        Assert.Equal("hull", cut.Find("div").GetAttribute("data-testid"));
    }

    // ------------------------------------------------------------ the name

    [Fact]
    public void Name_renders_view_transition_name()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.Equal("product-42", root.GetAttribute("data-dryl-morph"));
        Assert.False(root.HasAttribute("data-dryl-morph-depth"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_renders_no_style(string? name)
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, name)
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.False(root.HasAttribute("data-dryl-morph"));
        Assert.False(root.HasAttribute("data-dryl-morph-depth"));
    }

    [Fact]
    public void Inactive_renders_no_name_even_when_one_is_set()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .Add(p => p.Active, false)
            .Add(p => p.Style, DrylMorphStyle.DepthGlass)
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.False(root.HasAttribute("data-dryl-morph"));
        Assert.False(root.HasAttribute("data-dryl-morph-depth"));
    }

    [Fact]
    public void Activating_an_existing_instance_renders_the_name()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .Add(p => p.Active, false)
            .AddChildContent("x"));

        Assert.False(cut.Find("div").HasAttribute("data-dryl-morph"));

        cut.Render(ps => ps.Add(p => p.Active, true));

        Assert.Equal("product-42", cut.Find("div").GetAttribute("data-dryl-morph"));
    }

    // ------------------------------------------------------------- the tiers

    [Fact]
    public void DepthGlass_adds_the_transition_class_and_the_marker()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .Add(p => p.Style, DrylMorphStyle.DepthGlass)
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.Equal("product-42", root.GetAttribute("data-dryl-morph"));
        Assert.True(root.HasAttribute("data-dryl-morph-depth"));
    }

    [Fact]
    public void Glide_adds_neither_the_transition_class_nor_the_marker()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .Add(p => p.Style, DrylMorphStyle.Glide)
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.Equal("product-42", root.GetAttribute("data-dryl-morph"));
        Assert.False(root.HasAttribute("data-dryl-morph-depth"));
    }

    [Fact]
    public void DepthGlass_without_a_name_stays_inert()
    {
        UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Style, DrylMorphStyle.DepthGlass)
            .AddChildContent("x"));

        var root = cut.Find("div");
        Assert.False(root.HasAttribute("data-dryl-morph"));
        Assert.False(root.HasAttribute("data-dryl-morph-depth"));
    }

    // --------------------------------------------------------- the reporting

    [Fact]
    public void Every_render_is_reported_to_the_view_transition_service()
    {
        var fake = UseFakeTransition();

        var cut = Render<DrylMorph>(ps => ps
            .Add(p => p.Name, "product-42")
            .AddChildContent("x"));

        Assert.True(fake.Signals >= 1);

        var before = fake.Signals;
        cut.Render(ps => ps.Add(p => p.Name, "product-43"));

        Assert.True(fake.Signals > before);
    }

    [Fact]
    public void An_unnamed_hull_still_reports_its_render()
    {
        var fake = UseFakeTransition();

        Render<DrylMorph>(ps => ps.AddChildContent("x"));

        Assert.True(fake.Signals >= 1);
    }
}
