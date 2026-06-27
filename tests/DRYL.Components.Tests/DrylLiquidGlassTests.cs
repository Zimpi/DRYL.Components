using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylLiquidGlass"/> — the glass surface that warps in 3D
/// toward the pointer. JSInterop is Loose because it wires dryl.liquidglass on
/// render.
/// </summary>
public class DrylLiquidGlassTests : BunitContext
{
    public DrylLiquidGlassTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_glass_layers_and_content()
    {
        var cut = Render<DrylLiquidGlass>(ps => ps.AddChildContent("<span>body</span>"));

        Assert.Single(cut.FindAll(".lg"));
        Assert.Single(cut.FindAll(".lg-sheen"));
        Assert.Single(cut.FindAll(".lg-specular"));
        Assert.Contains("body", cut.Find(".lg-content").InnerHtml);
    }

    [Theory]
    [InlineData(LiquidGlassIntensity.Subtle, "lg--subtle")]
    [InlineData(LiquidGlassIntensity.Medium, "lg--medium")]
    [InlineData(LiquidGlassIntensity.Strong, "lg--strong")]
    public void Intensity_maps_to_class(LiquidGlassIntensity intensity, string cssClass)
    {
        var cut = Render<DrylLiquidGlass>(ps => ps.Add(p => p.Intensity, intensity));

        Assert.Contains(cssClass, cut.Find(".lg").GetAttribute("class"));
    }

    [Fact]
    public void Interactive_tracks_pointer()
    {
        Render<DrylLiquidGlass>(ps => ps.Add(p => p.Interactive, true));

        Assert.NotEmpty(JSInterop.Invocations["dryl.liquidglass.track"]);
    }

    [Fact]
    public void Non_interactive_does_not_track_pointer_and_marks_static()
    {
        var cut = Render<DrylLiquidGlass>(ps => ps.Add(p => p.Interactive, false));

        Assert.Contains("lg--static", cut.Find(".lg").GetAttribute("class"));
        Assert.Empty(JSInterop.Invocations["dryl.liquidglass.track"]);
    }
}
