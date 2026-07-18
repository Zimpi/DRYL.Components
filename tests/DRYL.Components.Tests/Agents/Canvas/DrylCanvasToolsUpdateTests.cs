using System.Text.Json;
using DRYL.Components.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class DrylCanvasToolsUpdateTests
{
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "B", "value": "2" } } ] } }
        """, CanvasJson.Options)!;

    private static async Task<string?> InvokeAsync(AITool tool, string brief)
    {
        var fn = (AIFunction)tool;
        var args = new Dictionary<string, object?> { ["brief"] = brief };
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

    // ---- malformed stream tail (real-model behavior) ----

    [Fact]
    public async Task Update_applies_last_parsed_ops_when_stream_tail_is_malformed()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        // Complete op list followed by a stray bracket — the strict final parse fails,
        // but the ops the tolerant reader already parsed must still land.
        var full = """{"ops":[{"op":"setProps","id":"a","props":{"value":"11"}}]}""";

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(full, "]"));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "bump a");

        Assert.Equal(AiState.Generated, run.State);
        Assert.Null(run.Error);
        Assert.Equal("11", run.Spec!.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
        Assert.Contains("1 changes applied", receipt);
        Assert.Contains("malformed", receipt);
    }

    // ---- staged application: only ops strictly before the last parsed one apply mid-stream ----

    [Fact]
    public async Task Update_applies_ops_strictly_before_the_last_parsed_one_while_streaming()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        // Snapshot the values at the moment the FIRST op actually lands, to prove the
        // second (still-last-parsed, possibly-truncated) op has not been applied yet.
        string? aAtFirstApply = null;
        string? bAtFirstApply = null;
        var firstApplySeen = false;
        run.OnChange += () =>
        {
            if (firstApplySeen || run.ChangedIds.Count == 0) return;
            firstApplySeen = true;
            aAtFirstApply = run.Spec!.Root!.Children![0].Props!.Value.GetProperty("value").GetString();
            bAtFirstApply = run.Spec.Root.Children![1].Props!.Value.GetProperty("value").GetString();
        };

        // Chunk 1: op "a" complete, array/doc still open -> Ops.Count == 1 (nothing applied yet,
        // the sole op is still "last parsed"). Chunk 2: op "b" arrives but its value string is
        // truncated -> Ops.Count == 2, so op "a" (now strictly-before-last) applies, op "b" doesn't.
        // Chunk 3: completes op "b" -> applied fully once the stream ends.
        var chunk1 = """{"ops":[{"op":"setProps","id":"a","props":{"value":"11"}}""";
        var chunk2 = """,{"op":"setProps","id":"b","props":{"value":"22""";
        var chunk3 = "\"}}]}";

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(chunk1, chunk2, chunk3));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "bump both values");

        Assert.True(firstApplySeen);
        Assert.Equal("11", aAtFirstApply);
        Assert.Equal("2", bAtFirstApply);   // op "b" not yet applied when op "a" landed

        Assert.Equal("11", run.Spec!.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
        Assert.Equal("22", run.Spec.Root.Children![1].Props!.Value.GetProperty("value").GetString());
        Assert.Contains("2 changes applied", receipt);
        Assert.Equal(AiState.Generated, run.State);
    }

    // ---- unknown id ----

    [Fact]
    public async Task Update_skips_op_on_unknown_id_and_reports_it_in_the_receipt()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var full = """{"ops":[{"op":"setProps","id":"zzz","props":{"value":"5"}}]}""";
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(full));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "bump zzz");

        Assert.Contains("skipped", receipt);
        Assert.Contains("0 changes applied", receipt);
        Assert.Empty(run.ChangedIds);
        Assert.Equal("1", run.Spec!.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
        Assert.Equal("2", run.Spec.Root.Children![1].Props!.Value.GetProperty("value").GetString());
        Assert.Equal(AiState.Generated, run.State);
    }

    // ---- no artifact yet ----

    [Fact]
    public async Task Update_without_prior_artifact_returns_corrective_receipt_without_state_change()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script("{}"));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "add something");

        Assert.Equal("There is no artifact yet — call create_artifact first.", receipt);
        Assert.Equal(AiState.Thinking, run.State);   // unchanged: BeginGeneration was never called
        Assert.Equal(0, changes);
        Assert.Null(run.Spec);
    }

    // ---- remove op ----

    [Fact]
    public async Task Update_remove_op_flags_node_removing_without_deleting_it()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var full = """{"ops":[{"op":"remove","id":"a"}]}""";
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(full));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "remove a");

        Assert.Contains("1 changes applied", receipt);
        Assert.Equal(2, run.Spec!.Root!.Children!.Count);   // still present
        var node = run.Spec.Root.Children!.First(c => c.Id == "a");
        Assert.True(node.Removing);
        Assert.Equal(AiState.Generated, run.State);
    }
}
