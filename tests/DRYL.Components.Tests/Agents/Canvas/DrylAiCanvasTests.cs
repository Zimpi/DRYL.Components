using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class DrylAiCanvasTests : BunitContext
{
    public DrylAiCanvasTests()
    {
        // DrylPresence calls dryl.motion.* during its exit lifecycle; the canvas renders
        // several JS-interop-aware components. Loose mode lets those calls no-op in tests.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    [Fact]
    public void Renders_stat_node_after_snapshot()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"title":"Report","root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k","direction":"up","delta":"+5%"}}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Single(cut.FindAll(".stat"));
        Assert.Contains("Revenue", cut.Markup);
        Assert.Contains("Report", cut.Markup); // title in the header
    }

    [Fact]
    public void Chart_node_renders_its_title_caption()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"c1","type":"lineChart","props":{"title":"Umsatz nach Monat","labels":["Jan"],
                    "series":[{"name":"2025","data":[1]}]}}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        var caption = cut.Find(".canvas-chart-title");
        Assert.Equal("Umsatz nach Monat", caption.TextContent);
    }

    [Fact]
    public void Invalid_node_renders_skeleton_fallback()
    {
        var run = new DrylCanvasRun();
        // A stat with no value fails CanvasCatalog.Validate → same state as "still streaming".
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"bad","type":"stat","props":{"label":"only a label"}}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Contains("waiting for stat", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".skel-wrap"));
    }

    [Fact]
    public void Aria_live_announces_after_complete_generation()
    {
        var run = new DrylCanvasRun();
        var spec = Parse("""{"root":{"id":"root","type":"stack","children":[{"id":"d","type":"divider"}]}}""");

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        cut.InvokeAsync(() => run.CompleteGeneration(spec));

        cut.WaitForAssertion(() =>
        {
            var live = cut.Find(".canvas-live");
            Assert.Equal("polite", live.GetAttribute("aria-live"));
            Assert.Contains("ready", live.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Button_click_raises_interaction_with_current_form_values()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"q","type":"inputText","props":{"name":"query","label":"Query","value":"seed"}},
                {"id":"go","type":"button","props":{"label":"Send","intent":"submit","kind":"primary"}}]}}
            """));

        CanvasInteraction? captured = null;
        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, run)
            .Add(x => x.OnInteraction, i => captured = i));

        cut.Find("input").Input("hello");
        cut.Find("button").Click();

        Assert.NotNull(captured);
        Assert.Equal("submit", captured!.Intent);
        Assert.Equal("go", captured.NodeId);
        Assert.Equal("hello", captured.Values["query"]);
    }

    [Fact]
    public void Run_error_renders_danger_alert()
    {
        var run = new DrylCanvasRun();
        run.FailGeneration(new InvalidOperationException("boom"));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.NotEmpty(cut.FindAll(".alert"));
        Assert.Contains("Artifact failed", cut.Markup);
        Assert.Contains("boom", cut.Markup);
    }

    [Fact]
    public void Removing_node_plays_exit_then_purges()
    {
        var run = new DrylCanvasRun();
        var spec = Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"divider"},
                {"id":"b","type":"badge","props":{"text":"Hi"}}]}}
            """);
        run.ApplySnapshot(spec);

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("Hi", cut.Markup);

        var child = spec.Root!.Children!.First(c => c.Id == "b");
        cut.InvokeAsync(() => { child.Removing = true; run.Raise(); });

        // Exit animation is playing — the node is still mounted, marked exiting.
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".presence-exit")));

        // The root also wraps in a DrylPresence whose markup contains the descendant's exit
        // class; the exiting node is the innermost match, so take the last.
        var exiting = cut.FindComponents<DrylPresence>()
                         .Last(pc => pc.Markup.Contains("presence-exit"));
        cut.InvokeAsync(() => exiting.Instance.OnExitFinished());

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Hi", cut.Markup);
            Assert.Single(spec.Root!.Children!); // only "a" remains after Purge
        });
    }
}
