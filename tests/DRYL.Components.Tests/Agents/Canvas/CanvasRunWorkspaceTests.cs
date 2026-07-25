using System.Text.Json;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The run projected onto the workspace: one run, always the active view's spec.</summary>
public class CanvasRunWorkspaceTests
{
    private static CanvasSpec Spec(string title) =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""
            { "title": "{{title}}", "root": { "id": "root", "type": "stack" } }
            """,
            CanvasJson.Options)!;

    [Fact]
    public void A_generation_fills_the_active_view()
    {
        var ws = new CanvasWorkspace();
        var overview = ws.Open("Overview");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        run.BeginCreate();
        run.CompleteReveal(Spec("Overview"));

        Assert.NotNull(overview.Spec);
        Assert.Same(overview.Spec, run.Spec);
    }

    [Fact]
    public void Without_an_active_view_the_run_opens_one()
    {
        var ws = new CanvasWorkspace();
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        run.BeginCreate();
        run.CompleteReveal(Spec("Report"));

        Assert.Single(ws.Views);
        Assert.NotNull(ws.Active!.Spec);
        Assert.Same(ws.Active.Spec, run.Spec);
    }

    [Fact]
    public void Switching_views_switches_the_runs_spec()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        a.Spec = Spec("A");
        b.Spec = Spec("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        ws.Activate(a.Id);
        Assert.Same(a.Spec, run.Spec);

        ws.Activate(b.Id);
        Assert.Same(b.Spec, run.Spec);
    }

    [Fact]
    public void Switching_views_bumps_the_epoch_and_raises_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);
        var epoch = run.ArtifactEpoch;
        var raised = 0;
        run.OnChange += () => raised++;

        ws.Activate(a.Id);

        Assert.Equal(epoch + 1, run.ArtifactEpoch);
        Assert.Equal(1, raised);
        Assert.NotSame(b, ws.Active);
    }

    [Fact]
    public void The_swap_morph_is_suppressed_exactly_once_per_switch()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        ws.Open("B");
        var run = new DrylCanvasRun();
        run.UseWorkspace(ws);

        ws.Activate(a.Id);

        Assert.True(run.ConsumeSwapMorphSuppression());
        Assert.False(run.ConsumeSwapMorphSuppression());
    }

    [Fact]
    public void Without_a_workspace_the_run_keeps_its_own_spec()
    {
        var run = new DrylCanvasRun();

        run.BeginCreate();
        run.CompleteReveal(Spec("Solo"));

        Assert.NotNull(run.Spec);
        Assert.False(run.ConsumeSwapMorphSuppression());
    }
}
