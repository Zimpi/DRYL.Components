using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylLiquidGlass"/> — the experimental refractive glass
/// surface. JSInterop is Loose because it wires dryl.liquidglass on render.
/// </summary>
public class DrylLiquidGlassTests : BunitContext
{
    public DrylLiquidGlassTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_glass_layers_with_per_instance_filter()
    {
        var cut = Render<DrylLiquidGlass>(ps => ps.AddChildContent("<span>body</span>"));

        Assert.Single(cut.FindAll(".lg"));
        Assert.Single(cut.FindAll(".lg-refract"));
        Assert.Single(cut.FindAll(".lg-specular"));

        // The refract layer references the instance's own SVG filter.
        var filterId = cut.Find(".lg-defs filter").GetAttribute("id");
        Assert.StartsWith("lg-", filterId);
        Assert.Contains($"url(#{filterId})", cut.Find(".lg-refract").GetAttribute("style"));
        Assert.Contains("body", cut.Find(".lg-content").InnerHtml);
    }

    [Theory]
    [InlineData(LiquidGlassIntensity.Subtle, "lg--subtle", "40")]
    [InlineData(LiquidGlassIntensity.Medium, "lg--medium", "80")]
    [InlineData(LiquidGlassIntensity.Strong, "lg--strong", "140")]
    public void Intensity_drives_class_and_displacement_scale(
        LiquidGlassIntensity intensity, string cssClass, string scale)
    {
        var cut = Render<DrylLiquidGlass>(ps => ps.Add(p => p.Intensity, intensity));

        Assert.Contains(cssClass, cut.Find(".lg").GetAttribute("class"));
        Assert.Equal(scale, cut.Find("feDisplacementMap").GetAttribute("scale"));
    }

    [Fact]
    public void Non_interactive_does_not_track_pointer()
    {
        Render<DrylLiquidGlass>(ps => ps.Add(p => p.Interactive, false));

        Assert.Empty(JSInterop.Invocations["dryl.liquidglass.track"]);
    }
}
