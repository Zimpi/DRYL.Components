using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Agents.Generation;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests.Agents;

public class DrylAiBuildTests : BunitContext
{
    private sealed class Dish { public string? Title { get; set; } }

    [Fact]
    public void Renders_the_current_artifact_snapshot()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        run.ApplyPatch(JsonDocument.Parse("""{"title":"Pasta"}""").RootElement, maxRounds: null);

        var cut = Render<DrylAiBuild<Dish>>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ChildContent, (RenderFragment<ArtifactSnapshot<Dish>>)(snap => builder =>
                builder.AddContent(0, snap.Artifact?.Title))));

        Assert.Contains("Pasta", cut.Markup);
    }

    [Fact]
    public void Re_renders_when_the_run_raises_a_change()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());

        var cut = Render<DrylAiBuild<Dish>>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.ChildContent, (RenderFragment<ArtifactSnapshot<Dish>>)(snap => builder =>
                builder.AddContent(0, snap.Artifact?.Title ?? "empty"))));

        Assert.Contains("empty", cut.Markup);

        cut.InvokeAsync(() => run.ApplyPatch(JsonDocument.Parse("""{"title":"Risotto"}""").RootElement, null));

        Assert.Contains("Risotto", cut.Markup);
    }
}
