using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.AI;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The model may open a named view — and only ever builds into the one it opened.</summary>
public class CanvasOpenViewToolTests
{
    private const string ArtifactJson =
        """{"title":"Artifact","root":{"id":"root","type":"stack","children":[""" +
        """{"id":"m","type":"markdown","props":{"content":"open"}}]}}""";

    private static async IAsyncEnumerable<string> Script(string json)
    {
        await Task.Yield();
        yield return json;
    }

    private static async Task<string?> OpenAsync(AITool tool, string name, string brief)
    {
        var result = await ((AIFunction)tool).InvokeAsync(
            new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["name"] = name,
                ["brief"] = brief,
            }!));
        return result?.ToString();
    }

    [Fact]
    public void Without_a_workspace_there_is_no_open_view_tool()
    {
        var tools = DrylCanvasTools.CreateReplay(new DrylCanvasRun(), (_, _) => Script(ArtifactJson));

        Assert.Equal(2, tools.All.Count);
        Assert.Null(tools.OpenView);
    }

    [Fact]
    public void With_a_workspace_the_tool_set_grows_by_one()
    {
        var ws = new CanvasWorkspace();
        var tools = DrylCanvasTools.CreateReplay(
            new DrylCanvasRun(), (_, _) => Script(ArtifactJson), workspace: ws);

        Assert.Equal(3, tools.All.Count);
        Assert.NotNull(tools.OpenView);
        Assert.Equal("open_view", ((AIFunction)tools.OpenView!).Name);
    }

    [Fact]
    public async Task Open_view_creates_activates_and_fills_the_view()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(ArtifactJson), workspace: ws);

        var receipt = await OpenAsync(tools.OpenView!, "Order 4711", "Show order 4711.");

        Assert.Single(ws.Views);
        Assert.Equal("Order 4711", ws.Active!.Title);
        Assert.NotNull(ws.Active.Spec);
        Assert.Same(ws.Active.Spec, run.Spec);
        Assert.Contains("Order 4711", receipt);
        Assert.Contains("2 elements", receipt);
    }

    [Fact]
    public async Task Re_opening_a_view_builds_into_the_same_one()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(ArtifactJson), workspace: ws);

        await OpenAsync(tools.OpenView!, "Overview", "a");
        await OpenAsync(tools.OpenView!, "Order 4711", "b");
        await OpenAsync(tools.OpenView!, "Overview", "c");

        Assert.Equal(2, ws.Views.Count);
        Assert.Equal("overview", ws.ActiveId);
        Assert.NotNull(ws.Views[0].Spec);
        Assert.NotNull(ws.Views[1].Spec);
    }

    [Fact]
    public async Task Create_artifact_keeps_its_plain_receipt()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(ArtifactJson), workspace: ws);

        var receipt = await ((AIFunction)tools.CreateArtifact).InvokeAsync(
            new AIFunctionArguments(new Dictionary<string, object?> { ["brief"] = "b" }!));

        Assert.Contains("Artifact created: 2 elements", receipt?.ToString());
    }
}
