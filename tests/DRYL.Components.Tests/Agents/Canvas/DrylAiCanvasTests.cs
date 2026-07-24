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
    public void Invalid_node_renders_skeleton_fallback_while_streaming()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        // An invalid node in the complete zone (a later sibling follows) mid-stream:
        // still shown as the "waiting" skeleton.
        run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"bad","type":"stat","props":{"label":"only a label"}},
                {"id":"ok","type":"divider"}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Contains("waiting for stat", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".skel-wrap"));
        Assert.Empty(cut.FindAll(".canvas-invalid"));
    }

    [Fact]
    public void Invalid_node_shows_error_placeholder_once_settled()
    {
        var run = new DrylCanvasRun();
        // A completed create settles the run at AiState.Generated with the final spec —
        // an invalid node is finished-broken, not streaming. (A bare ApplySnapshot would
        // NOT do: a fresh run starts at AiState.Thinking, which is still "in flight".)
        run.BeginCreate();
        run.CompleteReveal(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"bad","type":"stat","props":{"label":"only a label"}}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".canvas-waiting"));
        var placeholder = cut.Find(".canvas-invalid");
        Assert.Contains("value must be non-empty", placeholder.TextContent);
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

    [Fact]
    public void Patched_node_renders_its_new_props()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"s1","type":"stat","props":{"label":"Revenue","value":"€10k"}}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("€10k", cut.Markup);

        cut.InvokeAsync(() => run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "s1",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "value": "€12k" }"""),
        }));

        cut.WaitForAssertion(() => Assert.Contains("€12k", cut.Markup));
    }

    [Fact]
    public void Replaced_tree_with_same_ids_renders_new_content()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"m","type":"markdown","props":{"content":"Old Q2 text"}}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("Old Q2 text", cut.Markup);

        // Whole-tree replacement (second create) — same ids, same (zero) version stamps.
        cut.InvokeAsync(() => run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"m","type":"markdown","props":{"content":"New Q3 text"}}]}}
            """)));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("New Q3 text", cut.Markup);
            Assert.DoesNotContain("Old Q2 text", cut.Markup);
        });
    }

    [Fact]
    public void Tabs_shell_renders_once_its_children_stream_in()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        // The streaming tail is a tabs shell whose labels are known but whose
        // children have not started -> invalid (labels.Count != children.Count),
        // shown as a "waiting" skeleton.
        run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"t","type":"tabs","props":{"labels":["One","Two"]},"children":[]}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Contains("waiting for tabs", cut.Markup);

        cut.InvokeAsync(() => run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"t","type":"tabs","props":{"labels":["One","Two"]},"children":[
                    {"id":"t1","type":"divider"},
                    {"id":"t2","type":"divider"}]},
                {"id":"d2","type":"divider"}]}}
            """)));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".canvas-waiting"));
            Assert.Contains("One", cut.Markup);
        });
    }
}
