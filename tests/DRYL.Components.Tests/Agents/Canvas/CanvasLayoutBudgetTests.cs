using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// The generator is stateless and never sees the page, so the only thing that stops it from
/// authoring a three-column dashboard for a 360px phone is the layout budget carried in the
/// prompt. These tests pin that the budget is derived from the measured width and that it
/// reaches BOTH generations.
/// </summary>
public class CanvasLayoutBudgetTests : BunitContext
{
    public CanvasLayoutBudgetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    [Fact]
    public void Unknown_width_adds_no_budget()
    {
        Assert.Equal(string.Empty, CanvasPrompt.LayoutBudget(null));
        Assert.Equal(string.Empty, CanvasPrompt.LayoutBudget(0));
        Assert.DoesNotContain("LAYOUT BUDGET", CanvasPrompt.CreatePrompt("brief", null));
    }

    [Fact]
    public void Narrow_width_forbids_multi_column_grids()
    {
        var budget = CanvasPrompt.LayoutBudget(360);
        Assert.Contains("360px", budget);
        Assert.Contains("\"columns\": 1 only", budget);
    }

    [Theory]
    [InlineData(700, "at most 2 columns")]
    [InlineData(1200, "up to 3 columns")]
    public void Wider_widths_relax_the_grid_budget(int width, string expected)
    {
        var budget = CanvasPrompt.LayoutBudget(width);
        Assert.Contains(expected, budget);
        Assert.DoesNotContain("\"columns\": 1 only", budget);
    }

    [Fact]
    public void Create_and_update_prompts_both_carry_the_budget()
    {
        Assert.Contains("LAYOUT BUDGET", CanvasPrompt.CreatePrompt("brief", "Title", 360));
        Assert.Contains("LAYOUT BUDGET", CanvasPrompt.UpdatePrompt("brief", "{}", 360));
    }

    [Fact]
    public void Run_keeps_the_reported_width_and_ignores_nonsense()
    {
        var run = new DrylCanvasRun();
        Assert.Null(run.AvailableWidth);

        run.ReportWidth(412);
        Assert.Equal(412, run.AvailableWidth);

        run.ReportWidth(0);
        run.ReportWidth(-5);
        Assert.Equal(412, run.AvailableWidth);
    }

    [Fact]
    public async Task Create_generation_sends_the_runs_current_width()
    {
        var run = new DrylCanvasRun();
        run.ReportWidth(360);

        var prompts = new List<string>();
        var tools = DrylCanvasTools.CreateReplay(run, (prompt, _) =>
        {
            prompts.Add(prompt);
            return Deltas("""{"title":"T","root":{"id":"r","type":"divider"}}""");
        });

        await Invoke(tools.CreateArtifact);

        Assert.Contains("360px", Assert.Single(prompts));
    }

    // A width measured after the run was created must still reach the next generation:
    // the tools read DrylCanvasRun.AvailableWidth per call, not once at construction.
    [Fact]
    public async Task Update_generation_reads_the_width_measured_after_construction()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""{"root":{"id":"r","type":"stack","children":[]}}"""));

        var prompts = new List<string>();
        var tools = DrylCanvasTools.CreateReplay(run, (prompt, _) =>
        {
            prompts.Add(prompt);
            return Deltas("""{"ops":[]}""");
        });

        run.ReportWidth(1440);

        await Invoke(tools.UpdateArtifact);

        Assert.Contains("1440px", Assert.Single(prompts));
    }

    [Fact]
    public void Canvas_forwards_a_measured_width_to_a_later_run()
    {
        var first = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, first));

        cut.FindComponent<DrylCanvas>().Instance.OnWidthMeasured(390);
        Assert.Equal(390, first.AvailableWidth);

        // Rebinding to a fresh run must not make the canvas width-blind again.
        var second = new DrylCanvasRun();
        cut.Render(p => p.Add(x => x.Run, second));
        Assert.Equal(390, second.AvailableWidth);
    }

    // AITool has no InvokeAsync of its own, and bUnit's IRenderedComponent extension of the
    // same name wins overload resolution — cast to the function first.
    private static Task<object?> Invoke(Microsoft.Extensions.AI.AITool tool) =>
        ((Microsoft.Extensions.AI.AIFunction)tool).InvokeAsync(
            new Microsoft.Extensions.AI.AIFunctionArguments { ["brief"] = "anything" }).AsTask();

    private static async IAsyncEnumerable<string> Deltas(string json)
    {
        await Task.Yield();
        yield return json;
    }
}
