using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Structure of the organic <see cref="DrylSpinner"/> variants (I34), as
/// <c>specs/E7 Feedback/F3 DrylSpinner.md</c> states it.
/// </summary>
public class DrylSpinnerTests : BunitContext
{
    [Fact]
    public void Ring_renders_one_liquid_torus()
    {
        var cut = Render<DrylSpinner>();

        Assert.Contains("v-ring", cut.Find("[role=status]").ClassList);
        Assert.Single(cut.FindAll(".spinner-liquid .spinner-liquid-torus"));
    }

    [Fact]
    public void Unmapped_variant_falls_back_to_the_ring()
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, (DrylSpinner.SpinnerVariant)99));

        Assert.Contains("v-ring", cut.Find("[role=status]").ClassList);
        Assert.Single(cut.FindAll(".spinner-liquid-torus"));
    }

    [Fact]
    public void Dots_renders_a_core_and_three_droplets_inside_its_own_goo_filter()
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, DrylSpinner.SpinnerVariant.Dots));

        Assert.Single(cut.FindAll(".spinner-goo .spinner-goo-core"));
        Assert.Equal(3, cut.FindAll(".spinner-goo .spinner-goo-drop").Count);

        var filterId = cut.Find("filter").GetAttribute("id");
        Assert.False(string.IsNullOrEmpty(filterId));
        Assert.Equal($"url(#{filterId})", cut.Find("g").GetAttribute("filter"));
    }

    [Fact]
    public void Two_goo_spinners_never_share_a_filter_id()
    {
        var a = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, DrylSpinner.SpinnerVariant.Dots));
        var b = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, DrylSpinner.SpinnerVariant.Dots));

        Assert.NotEqual(a.Find("filter").GetAttribute("id"), b.Find("filter").GetAttribute("id"));
    }

    [Fact]
    public void Pulse_renders_three_membranes_and_one_core()
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, DrylSpinner.SpinnerVariant.Pulse));

        Assert.Equal(3, cut.FindAll(".spinner-bloom .spinner-bloom-membrane").Count);
        Assert.Single(cut.FindAll(".spinner-bloom .spinner-bloom-core"));
    }

    [Theory]
    [InlineData(DrylSpinner.SpinnerVariant.Ring)]
    [InlineData(DrylSpinner.SpinnerVariant.Dots)]
    [InlineData(DrylSpinner.SpinnerVariant.Pulse)]
    public void Moving_parts_are_hidden_from_assistive_technology(DrylSpinner.SpinnerVariant variant)
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Variant, variant));

        var body = cut.Find(".spinner-body");
        Assert.Equal("true", body.GetAttribute("aria-hidden"));
        Assert.Equal("Loading", cut.Find("[role=status]").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(DrylSpinner.SpinnerSize.Small, "sz-sm")]
    [InlineData(DrylSpinner.SpinnerSize.Medium, "sz-md")]
    [InlineData(DrylSpinner.SpinnerSize.Large, "sz-lg")]
    public void Size_maps_to_modifier_class(DrylSpinner.SpinnerSize size, string expected)
    {
        var cut = Render<DrylSpinner>(ps => ps.Add(p => p.Size, size));

        Assert.Contains(expected, cut.Find("[role=status]").ClassList);
    }
}
