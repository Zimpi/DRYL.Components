using System.Text.Json;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests.Agents;

public class DrylArtifactRunTests
{
    private sealed class Dish
    {
        public string? Title { get; set; }
        public List<string> Steps { get; set; } = new();
    }

    private static JsonElement El(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void ApplyPatch_merges_progressively_and_counts_rounds()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var changes = 0;
        run.OnChange += () => changes++;

        run.ApplyPatch(El("""{"title":"Pasta"}"""), maxRounds: null);
        run.ApplyPatch(El("""{"steps":["boil","drain"]}"""), maxRounds: null);

        Assert.Equal("Pasta", run.Artifact!.Title);
        Assert.Equal(2, run.Artifact.Steps.Count);
        Assert.Equal(2, run.Round);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ApplyPatch_returns_a_receipt_below_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = run.ApplyPatch(El("""{"title":"X"}"""), maxRounds: 12);
        Assert.Contains("round 1", receipt);
    }

    [Fact]
    public void ApplyPatch_returns_a_finalize_nudge_at_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = run.ApplyPatch(El("""{"title":"X"}"""), maxRounds: 1);
        Assert.Contains("Maximum refinement rounds reached", receipt);
    }
}
