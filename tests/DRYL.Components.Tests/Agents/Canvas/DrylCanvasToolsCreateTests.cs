using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class DrylCanvasToolsCreateTests
{
    private static async Task<string?> InvokeAsync(
        AITool tool, string brief, string? title = null)
    {
        var fn = (AIFunction)tool;
        var args = new Dictionary<string, object?> { ["brief"] = brief };
        if (title is not null) args["title"] = title;
        var result = await fn.InvokeAsync(new AIFunctionArguments(args!));
        return result?.ToString();
    }

    private static async IAsyncEnumerable<string> Script(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    // ---- happy path ----

    [Fact]
    public async Task Create_streams_intermediate_snapshots_and_completes_with_receipt()
    {
        var run = new DrylCanvasRun();
        var childCounts = new List<int?>();
        run.OnChange += () => childCounts.Add(run.Spec?.Root?.Children?.Count);

        // Chunk 1 leaves the "children" array open-but-empty when repaired; chunk 2 completes
        // it with exactly one child node — 2 nodes total (root + child) once finished.
        var chunk1 = """{"title":"T","root":{"id":"root","type":"stack","children":[""";
        var chunk2 = """{"id":"a","type":"stat","props":{"label":"L","value":"1"}}]}}""";

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(chunk1, chunk2));

        var receipt = await InvokeAsync(tools.CreateArtifact, "show one stat");

        // Intermediate snapshot was observed: first with 0 children, growing to 1.
        Assert.Contains(0, childCounts);
        Assert.Single(run.Spec!.Root!.Children!);

        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal(1, run.Round);
        Assert.Null(run.Error);
        Assert.Contains("2 elements", receipt);
        Assert.Contains("0 inputs", receipt);
        Assert.DoesNotContain("placeholders", receipt);
    }

    // ---- invalid node in final spec ----

    [Fact]
    public async Task Create_reports_placeholders_receipt_when_final_spec_has_invalid_node()
    {
        var run = new DrylCanvasRun();
        // progress.value must be 0..100 — 150 is invalid, caught by CanvasCatalog.Validate.
        var full = """
            {"title":"T","root":{"id":"root","type":"stack","children":[
            {"id":"a","type":"progress","props":{"value":150}}]}}
            """;

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(full));

        var receipt = await InvokeAsync(tools.CreateArtifact, "show progress at 150 percent");

        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal(1, run.Round);
        Assert.Null(run.Error);
        Assert.Contains("placeholders", receipt);
        Assert.Contains("update_artifact", receipt);
        Assert.Contains("must be between 0 and 100", receipt);
    }

    // ---- generator throws ----

    [Fact]
    public async Task Create_fails_generation_when_generator_throws()
    {
        var run = new DrylCanvasRun();

        static async IAsyncEnumerable<string> Throwing()
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Throwing());

        var receipt = await InvokeAsync(tools.CreateArtifact, "anything");

        Assert.NotNull(run.Error);
        Assert.Contains("boom", run.Error!.Message);
        Assert.Equal(AiState.None, run.State);
        Assert.StartsWith("Artifact generation failed", receipt);
        Assert.Contains("boom", receipt);
    }

    // ---- cancellation must propagate, not become a failure receipt ----

    [Fact]
    public async Task Create_rethrows_cancellation_instead_of_returning_a_failure_receipt()
    {
        var run = new DrylCanvasRun();

        static async IAsyncEnumerable<string> Cancelling()
        {
            await Task.Yield();
            throw new OperationCanceledException();
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Cancelling());
        var fn = (AIFunction)tools.CreateArtifact;
        var args = new Dictionary<string, object?> { ["brief"] = "anything" };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fn.InvokeAsync(new AIFunctionArguments(args!)).AsTask());

        Assert.Null(run.Error);
    }

    // ---- registration ----

    [Fact]
    public void CreateArtifact_tool_is_named_create_artifact()
    {
        var run = new DrylCanvasRun();
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script("{}"));
        Assert.Equal("create_artifact", tools.CreateArtifact.Name);
    }

    [Fact]
    public void UpdateArtifact_tool_is_registered_and_included_in_All()
    {
        var run = new DrylCanvasRun();
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script("{}"));

        Assert.Equal("update_artifact", tools.UpdateArtifact.Name);
        Assert.Equal(2, tools.All.Count);
        Assert.Contains(tools.CreateArtifact, tools.All);
        Assert.Contains(tools.UpdateArtifact, tools.All);
    }
}
