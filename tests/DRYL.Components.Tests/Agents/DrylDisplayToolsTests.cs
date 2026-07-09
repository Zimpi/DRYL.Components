using System.Text.Json;
using DRYL.Components.Agents.Tools;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Tests.Agents;

public class DrylDisplayToolsTests
{
    private static async Task<string?> InvokeAsync(AITool tool, Dictionary<string, object> args)
    {
        var fn = (AIFunction)tool;
        var result = await fn.InvokeAsync(new AIFunctionArguments(args));
        return result?.ToString();
    }

    // Model-shaped argument values: every value is a JsonElement, like a real provider sends.
    private static object Json(object shape) => JsonSerializer.SerializeToElement(shape);

    [Fact]
    public void All_contains_six_tools_with_show_names()
    {
        var tools = DrylDisplayTools.Create();
        Assert.Equal(6, tools.All.Count);
        Assert.All(tools.All, t => Assert.StartsWith("show_", t.Name));
    }

    [Fact]
    public async Task LineChart_acknowledges_valid_arguments()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.LineChart, new()
        {
            ["labels"] = Json(new[] { "Apr", "May" }),
            ["series"] = Json(new object[] { new { name = "Revenue", data = new[] { 1.0, 2.0 } } }),
        });
        Assert.Contains("shown to the user", answer);
        Assert.DoesNotContain("NOT shown", answer);
    }

    [Fact]
    public async Task LineChart_rejects_series_label_length_mismatch()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.LineChart, new()
        {
            ["labels"] = Json(new[] { "Apr", "May", "Jun" }),
            ["series"] = Json(new object[] { new { name = "Revenue", data = new[] { 1.0, 2.0 } } }),
        });
        Assert.Contains("NOT shown", answer);
        Assert.Contains("Revenue", answer);
    }

    [Fact]
    public async Task BarChart_rejects_invalid_value_format()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.BarChart, new()
        {
            ["labels"] = Json(new[] { "A" }),
            ["series"] = Json(new object[] { new { name = "S", data = new[] { 1.0 } } }),
            ["valueFormat"] = "D2", // int-only specifier — invalid for double
        });
        Assert.Contains("NOT shown", answer);
        Assert.Contains("valueFormat", answer);
    }

    [Fact]
    public async Task DonutChart_rejects_non_positive_segment()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.DonutChart, new()
        {
            ["segments"] = Json(new object[]
            {
                new { label = "EU", value = 10.0 },
                new { label = "US", value = 0.0 },
            }),
        });
        Assert.Contains("NOT shown", answer);
        Assert.Contains("US", answer);
    }

    [Fact]
    public async Task Stats_rejects_invalid_direction()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.Stats, new()
        {
            ["stats"] = Json(new object[]
            {
                new { label = "Revenue", value = "10", direction = "sideways" },
            }),
        });
        Assert.Contains("NOT shown", answer);
        Assert.Contains("sideways", answer);
    }

    [Fact]
    public async Task Timeline_acknowledges_valid_events()
    {
        var tools = DrylDisplayTools.Create();
        var answer = await InvokeAsync(tools.Timeline, new()
        {
            ["events"] = Json(new object[]
            {
                new { title = "Kickoff", kind = "success" },
                new { title = "Review", timestamp = "May 12", text = "All green." },
            }),
        });
        Assert.Contains("shown to the user", answer);
        Assert.DoesNotContain("NOT shown", answer);
    }
}
