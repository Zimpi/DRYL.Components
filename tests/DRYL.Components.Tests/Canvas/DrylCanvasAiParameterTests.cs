using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// API-freeze tests for the renamed opt-in parameter on <see cref="DrylCanvas"/>
/// (<c>Ai</c>, formerly <c>State</c>) and its obsolete alias, plus the property
/// that makes it an opt-in at all: the artifact renders in full with
/// <see cref="AiState.None"/>.
/// </summary>
public class DrylCanvasAiParameterTests : BunitContext
{
    public DrylCanvasAiParameterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasSpec Report() => Parse("""
        {"title":"Report","root":{"id":"root","type":"stack","children":[
            {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k"}}]}}
        """);

    [Fact]
    public void Ai_defaults_to_none()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Report()));

        Assert.Equal(AiState.None, cut.Instance.Ai);
    }

    [Fact]
    public void Artifact_renders_in_full_with_none()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Report())
            .Add(x => x.Ai, AiState.None));

        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Report", cut.Markup);
        Assert.Empty(cut.FindAll(".canvas-build"));
    }

    [Fact]
    public void Streaming_shows_the_build_line()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Report())
            .Add(x => x.Ai, AiState.Streaming));

        Assert.Single(cut.FindAll(".canvas-build"));
        Assert.Contains("Revenue", cut.Markup);
    }

    [Fact]
    public void Obsolete_State_alias_sets_Ai()
    {
#pragma warning disable CS0618 // the alias is exactly what this test pins
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Report())
            .Add(x => x.State, AiState.Thinking));

        Assert.Equal(AiState.Thinking, cut.Instance.Ai);
        Assert.Equal(AiState.Thinking, cut.Instance.State);
#pragma warning restore CS0618
        Assert.Single(cut.FindAll(".canvas-build"));
    }

    [Fact]
    public void Canvas_carries_no_aura()
    {
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Report())
            .Add(x => x.Ai, AiState.Streaming));

        Assert.DoesNotContain("ai-aura", cut.Find(".canvas").GetAttribute("class"));
    }
}
