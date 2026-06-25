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
    public async Task ApplyPatchAsync_atomic_merges_and_counts_rounds()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var changes = 0;
        run.OnChange += () => changes++;

        await run.ApplyPatchAsync(El("""{"title":"Pasta"}"""), maxRounds: null, TimeSpan.Zero, default);
        await run.ApplyPatchAsync(El("""{"steps":["boil","drain"]}"""), maxRounds: null, TimeSpan.Zero, default);

        Assert.Equal("Pasta", run.Artifact!.Title);
        Assert.Equal(2, run.Artifact.Steps.Count);
        Assert.Equal(2, run.Round);
        Assert.Equal(2, changes);   // exactly one OnChange per atomic round
    }

    [Fact]
    public async Task ApplyPatchAsync_returns_a_receipt_below_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = await run.ApplyPatchAsync(El("""{"title":"X"}"""), maxRounds: 12, TimeSpan.Zero, default);
        Assert.Contains("round 1", receipt);
    }

    [Fact]
    public async Task ApplyPatchAsync_returns_a_finalize_nudge_at_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = await run.ApplyPatchAsync(El("""{"title":"X"}"""), maxRounds: 1, TimeSpan.Zero, default);
        Assert.Contains("Maximum refinement rounds reached", receipt);
    }
}
