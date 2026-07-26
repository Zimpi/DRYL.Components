using System.Text.Json;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// The pin has to reach the model twice: as a rule in the prompt, and — when it tries anyway —
/// as a corrective sentence in the receipt it can act on next turn.
/// </summary>
public class CanvasPinReceiptTests
{
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "locked": true, "props": { "label": "A", "value": "1" } },
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

    [Fact]
    public void The_schema_tells_the_model_about_pinned_nodes()
    {
        Assert.Contains("\"locked\": true", CanvasPrompt.SchemaText);
        Assert.Contains("Never change, move or remove a pinned node", CanvasPrompt.SchemaText);
    }

    [Fact]
    public void The_update_prompt_repeats_the_rule_next_to_the_ops()
    {
        var prompt = CanvasPrompt.UpdatePrompt(
            "do something", """{"root":{"id":"r","type":"stack"}}""");

        Assert.Contains("pinned by the user", prompt);
    }

    [Fact]
    public async Task An_update_that_targets_a_pinned_node_is_skipped_with_a_reason()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var ops = """
            {"ops":[{"op":"setProps","id":"a","props":{"value":"999"}},
                    {"op":"remove","id":"a"}]}
            """;
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(ops));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "change the pinned stat");

        Assert.Contains("2 ops skipped", receipt);
        Assert.Contains("pinned by the user", receipt);
        Assert.Equal("1", run.Spec!.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
        Assert.False(run.Spec.Root.Children[0].Removing);
    }

    [Fact]
    public async Task An_unpinned_node_next_to_it_is_still_patched()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var ops = """{"ops":[{"op":"setProps","id":"b","props":{"value":"22"}}]}""";
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(ops));

        var receipt = await InvokeAsync(tools.UpdateArtifact, "bump b");

        Assert.Contains("1 changes applied", receipt);
        Assert.Equal("22", run.Spec!.Root!.Children![1].Props!.Value.GetProperty("value").GetString());
    }
}
