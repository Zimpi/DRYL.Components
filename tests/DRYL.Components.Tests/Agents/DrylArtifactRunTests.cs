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

    [Fact]
    public async Task ApplyPatchAsync_reveals_a_field_progressively_keeping_the_base()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        // Commit a base round atomically.
        await run.ApplyPatchAsync(El("""{"title":"Soup","steps":["chop"]}"""), maxRounds: null, TimeSpan.Zero, default);

        var titles = new List<string?>();
        run.OnChange += () => titles.Add(run.Artifact?.Title);

        // Reveal a long new title over a short span; capture every snapshot.
        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup With Garlic Croutons"}"""),
            maxRounds: null, TimeSpan.FromMilliseconds(120), default);

        // The title length is non-decreasing and strictly increases across the reveal.
        var lengths = titles.Where(t => t is not null).Select(t => t!.Length).ToList();
        Assert.True(lengths.Count >= 3, $"expected several intermediate snapshots, got {lengths.Count}");
        for (var i = 1; i < lengths.Count; i++)
            Assert.True(lengths[i] >= lengths[i - 1], "title length must never shrink during the reveal");
        Assert.True(lengths[^1] > lengths[0], "title must grow across the reveal");

        // Final commit is exact, and the previously-committed base field is preserved throughout.
        Assert.Equal("Roasted Tomato Basil Soup With Garlic Croutons", run.Artifact!.Title);
        Assert.Single(run.Artifact.Steps);
        Assert.Equal("chop", run.Artifact.Steps[0]);
    }

    [Fact]
    public async Task ApplyPatchAsync_is_atomic_when_duration_is_zero()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var snapshots = 0;
        run.OnChange += () => snapshots++;

        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup With Garlic Croutons"}"""),
            maxRounds: null, TimeSpan.Zero, default);

        Assert.Equal(1, snapshots);   // exactly one effective update — no intermediate reveal snapshots
        Assert.Equal("Roasted Tomato Basil Soup With Garlic Croutons", run.Artifact!.Title);
        Assert.Equal(1, run.Round);
    }

    [Fact]
    public async Task ApplyPatchAsync_cancelled_midreveal_commits_the_final_artifact()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        using var cts = new CancellationTokenSource();

        // Cancel almost immediately so the reveal is interrupted partway through a long span.
        run.OnChange += () => cts.CancelAfter(0);

        // Must NOT throw — cancellation is absorbed and the final state committed.
        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup","steps":["chop","simmer","blend"]}"""),
            maxRounds: null, TimeSpan.FromSeconds(5), cts.Token);

        // Final state is the exact, full merge — never a half-revealed prefix.
        Assert.Equal("Roasted Tomato Basil Soup", run.Artifact!.Title);
        Assert.Equal(3, run.Artifact.Steps.Count);
        Assert.Equal(1, run.Round);
    }
}
