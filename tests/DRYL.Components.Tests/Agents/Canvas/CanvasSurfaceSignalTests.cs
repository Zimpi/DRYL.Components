using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// A host that opens the canvas only when the assistant starts drawing can wait for it to be
/// measured instead of guessing a delay — so the first generation gets a real layout budget.
/// </summary>
public class CanvasSurfaceSignalTests : BunitContext
{
    public CanvasSurfaceSignalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    [Fact]
    public async Task WaitForSurface_completes_on_the_first_measurement()
    {
        var run = new DrylCanvasRun();
        var wait = run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5));
        Assert.False(wait.IsCompleted);

        run.ReportWidth(640);

        Assert.True(await wait);
    }

    [Fact]
    public async Task WaitForSurface_returns_at_once_when_a_width_is_known()
    {
        var run = new DrylCanvasRun();
        run.ReportWidth(640);

        var wait = run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5));

        Assert.True(wait.IsCompleted);
        Assert.True(await wait);
    }

    [Fact]
    public async Task WaitForSurface_gives_up_after_the_timeout_without_a_surface()
    {
        var run = new DrylCanvasRun();

        Assert.False(await run.WaitForSurfaceAsync(TimeSpan.FromMilliseconds(30)));
        Assert.Null(run.AvailableWidth);
    }

    [Fact]
    public async Task WaitForSurface_honours_cancellation()
    {
        var run = new DrylCanvasRun();
        using var cts = new CancellationTokenSource();
        var wait = run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5), cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }

    [Fact]
    public void OnWidthReported_fires_with_every_measurement_but_not_with_nonsense()
    {
        var run = new DrylCanvasRun();
        var widths = new List<int>();
        run.OnWidthReported += widths.Add;

        run.ReportWidth(390);
        run.ReportWidth(0);
        run.ReportWidth(1024);

        Assert.Equal([390, 1024], widths);
    }

    [Fact]
    public async Task A_mounted_canvas_completes_the_wait()
    {
        var run = new DrylCanvasRun();
        var wait = run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        cut.FindComponent<DrylCanvas>().Instance.OnWidthMeasured(480);

        Assert.True(await wait.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.Equal(480, run.AvailableWidth);
    }

    [Fact]
    public void Unmounting_the_canvas_forgets_its_width()
    {
        // A dialog closed: the next generation must not be authored for a panel that is gone,
        // and the next wait must wait for the new surface.
        var run = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        cut.FindComponent<DrylCanvas>().Instance.OnWidthMeasured(480);

        cut.Instance.Dispose();

        Assert.Null(run.AvailableWidth);
        Assert.False(run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5)).IsCompleted);
    }

    [Fact]
    public void Rebinding_the_canvas_forgets_the_width_on_the_old_run()
    {
        var first = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, first));
        cut.FindComponent<DrylCanvas>().Instance.OnWidthMeasured(480);

        var second = new DrylCanvasRun();
        cut.Render(p => p.Add(x => x.Run, second));

        Assert.Null(first.AvailableWidth);
        Assert.Equal(480, second.AvailableWidth);
    }

    [Fact]
    public async Task BeforeGenerate_runs_before_the_generation_reads_the_width()
    {
        var run = new DrylCanvasRun();
        var prompts = new List<string>();
        var tools = DrylCanvasTools.CreateReplay(run, (prompt, _) =>
        {
            prompts.Add(prompt);
            return Deltas("""{"title":"T","root":{"id":"r","type":"divider"}}""");
        });

        // The host opens its dialog here; the canvas inside measures itself.
        tools.BeforeGenerate = async ct =>
        {
            _ = Task.Run(async () => { await Task.Delay(20); run.ReportWidth(720); });
            Assert.True(await run.WaitForSurfaceAsync(TimeSpan.FromSeconds(5), ct));
        };

        await Invoke(tools.CreateArtifact);

        Assert.Contains("720px", Assert.Single(prompts));
    }

    [Fact]
    public async Task BeforeGenerate_also_runs_for_an_update()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(JsonSerializer.Deserialize<CanvasSpec>(
            """{"root":{"id":"r","type":"stack","children":[]}}""", CanvasJson.Options)!);
        var calls = 0;
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Deltas("""{"ops":[]}"""));
        tools.BeforeGenerate = _ => { calls++; return Task.CompletedTask; };

        await Invoke(tools.UpdateArtifact);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task BeforeGenerate_does_not_run_for_an_update_without_an_artifact()
    {
        var run = new DrylCanvasRun();
        var calls = 0;
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Deltas("""{"ops":[]}"""));
        tools.BeforeGenerate = _ => { calls++; return Task.CompletedTask; };

        await Invoke(tools.UpdateArtifact);

        Assert.Equal(0, calls);
    }

    private static Task<object?> Invoke(Microsoft.Extensions.AI.AITool tool) =>
        ((Microsoft.Extensions.AI.AIFunction)tool).InvokeAsync(
            new Microsoft.Extensions.AI.AIFunctionArguments { ["brief"] = "anything" }).AsTask();

    private static async IAsyncEnumerable<string> Deltas(string json)
    {
        await Task.Yield();
        yield return json;
    }
}
