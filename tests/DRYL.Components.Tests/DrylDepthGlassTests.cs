using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylDepthGlass"/> — the glass surface that warps in 3D
/// toward the pointer. JSInterop is Loose because it wires dryl.depthglass on
/// render.
/// </summary>
public class DrylDepthGlassTests : BunitContext
{
    public DrylDepthGlassTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_glass_layers_and_content()
    {
        var cut = Render<DrylDepthGlass>(ps => ps.AddChildContent("<span>body</span>"));

        Assert.Single(cut.FindAll(".dg-warp"));
        Assert.Single(cut.FindAll(".dg-sheen"));
        Assert.Single(cut.FindAll(".dg-specular"));
        Assert.Contains("body", cut.Find(".dg-content").InnerHtml);
    }

    [Theory]
    [InlineData(DepthGlassIntensity.Subtle, "dg--subtle")]
    [InlineData(DepthGlassIntensity.Medium, "dg--medium")]
    [InlineData(DepthGlassIntensity.Strong, "dg--strong")]
    public void Intensity_maps_to_class(DepthGlassIntensity intensity, string cssClass)
    {
        var cut = Render<DrylDepthGlass>(ps => ps.Add(p => p.Intensity, intensity));

        Assert.Contains(cssClass, cut.Find(".dg-warp").GetAttribute("class"));
    }

    [Fact]
    public void Interactive_tracks_pointer()
    {
        Render<DrylDepthGlass>(ps => ps.Add(p => p.Interactive, true));

        Assert.NotEmpty(JSInterop.Invocations["dryl.depthglass.track"]);
    }

    [Fact]
    public void Non_interactive_does_not_track_pointer_and_marks_static()
    {
        var cut = Render<DrylDepthGlass>(ps => ps.Add(p => p.Interactive, false));

        Assert.Contains("dg--static", cut.Find(".dg-warp").GetAttribute("class"));
        Assert.Empty(JSInterop.Invocations["dryl.depthglass.track"]);
    }

    [Fact]
    public void DrylCard_Depth_adds_warp_layers_and_supersedes_spotlight()
    {
        var cut = Render<DrylCard>(ps => ps
            .Add(p => p.Depth, DepthGlassIntensity.Strong)
            .AddChildContent("<span>body</span>"));

        var card = cut.Find(".glass-card");
        Assert.Contains("dg-warp", card.GetAttribute("class"));
        Assert.Contains("dg--strong", card.GetAttribute("class"));
        Assert.Single(cut.FindAll(".dg-content"));
        Assert.Empty(cut.FindAll(".spotlight-fx")); // depth supersedes the spotlight
        Assert.NotEmpty(JSInterop.Invocations["dryl.depthglass.track"]);
    }
}
