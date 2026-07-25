using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Motion;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Phase W2/W3/W5 — the canvas's view-transition and header work: an artifact swap morphs
/// old into new (which is also how the old tree finally gets an exit), the fullscreen
/// expand grows through the same transition, and the header carries a live element count
/// plus a build line while the artifact streams.
/// </summary>
public class DrylAiCanvasWowTests : BunitContext
{
    /// <summary>Records how often a view transition was started and applies the mutation
    /// straight away, so tests see the resulting DOM without a real browser transition.</summary>
    private sealed class RecordingViewTransition : IDrylViewTransition
    {
        public int Started { get; private set; }
        public Task RunAsync(Action mutate) { Started++; mutate(); return Task.CompletedTask; }
        public async Task RunAsync(Func<Task> mutate) { Started++; await mutate(); }
        public void SignalRendered() { }
    }

    private readonly RecordingViewTransition _vt = new();

    public DrylAiCanvasWowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
        Services.AddSingleton<IDrylViewTransition>(_vt);   // last registration wins
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private const string FirstArtifact = """
        {"title":"Q2","root":{"id":"root","type":"stack","props":{},"children":[
            {"id":"m","type":"markdown","props":{"content":"Old Q2 text"}},
            {"id":"d","type":"divider"}]}}
        """;

    private const string SecondArtifact = """
        {"title":"Q3","root":{"id":"root","type":"stack","props":{},"children":[
            {"id":"m","type":"markdown","props":{"content":"New Q3 text"}},
            {"id":"d","type":"divider"}]}}
        """;

    // ---- W2: artifact swap ------------------------------------------------

    [Fact]
    public void Root_carries_a_view_transition_name_and_the_depth_marker()
    {
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        var root = cut.Find(".ai-canvas");
        Assert.Contains("view-transition-name: dryl-canvas-", root.GetAttribute("style"));
        Assert.Contains("view-transition-class: dryl-depth", root.GetAttribute("style"));
        Assert.NotNull(root.GetAttribute("data-vt-depth"));
    }

    [Fact]
    public void Two_canvases_get_distinct_view_transition_names()
    {
        // A duplicate name voids the whole transition, so per-instance uniqueness matters.
        var a = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));
        var b = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.NotEqual(a.Find(".ai-canvas").GetAttribute("style"),
                        b.Find(".ai-canvas").GetAttribute("style"));
    }

    [Fact]
    public void First_artifact_builds_without_a_view_transition()
    {
        // Nothing on screen to morph from — the node-by-node reveal choreography owns this.
        var run = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        cut.InvokeAsync(() => run.BeginCreate());
        cut.InvokeAsync(() => run.RevealSnapshot(Parse(FirstArtifact)));

        cut.WaitForAssertion(() => Assert.Contains("Old Q2 text", cut.Markup));
        Assert.Equal(0, _vt.Started);
    }

    [Fact]
    public void Second_artifact_swaps_through_a_view_transition()
    {
        var run = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        cut.InvokeAsync(() => run.BeginCreate());
        cut.InvokeAsync(() => run.RevealSnapshot(Parse(FirstArtifact)));
        cut.WaitForAssertion(() => Assert.Contains("Old Q2 text", cut.Markup));
        var afterFirst = _vt.Started;

        cut.InvokeAsync(() => run.BeginCreate());
        cut.InvokeAsync(() => run.RevealSnapshot(Parse(SecondArtifact)));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("New Q3 text", cut.Markup);
            Assert.DoesNotContain("Old Q2 text", cut.Markup);
            Assert.True(_vt.Started > afterFirst);
        });
    }

    [Fact]
    public void Patching_the_live_artifact_does_not_start_a_view_transition()
    {
        // Same spec instance — the pulse/glide vocabulary handles this, not a full morph.
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse(FirstArtifact));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        var before = _vt.Started;

        cut.InvokeAsync(() => run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "m",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "content": "Patched" }"""),
        }));

        cut.WaitForAssertion(() => Assert.Contains("Patched", cut.Markup));
        Assert.Equal(before, _vt.Started);
    }

    // ---- W3: fullscreen expand -------------------------------------------

    [Fact]
    public void Expand_button_toggles_fullscreen_through_a_view_transition()
    {
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        var button = cut.Find(".canvas-head-tools button");
        Assert.Equal("Expand artifact", button.GetAttribute("aria-label"));
        Assert.DoesNotContain("is-expanded", cut.Find(".ai-canvas").GetAttribute("class"));

        button.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("is-expanded", cut.Find(".ai-canvas").GetAttribute("class"));
            Assert.Equal("Exit fullscreen", cut.Find(".canvas-head-tools button").GetAttribute("aria-label"));
            Assert.True(_vt.Started > 0);
        });
    }

    [Fact]
    public void Escape_collapses_fullscreen()
    {
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));
        cut.Find(".canvas-head-tools button").Click();
        cut.WaitForAssertion(() => Assert.Contains("is-expanded", cut.Find(".ai-canvas").GetAttribute("class")));

        cut.Find(".ai-canvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("is-expanded", cut.Find(".ai-canvas").GetAttribute("class")));
    }

    [Fact]
    public void Expanding_promotes_the_canvas_to_the_top_layer()
    {
        // `position: fixed` alone measures against the nearest transformed/filtered ancestor,
        // so a "fullscreen" canvas would quietly fill a card. The top layer has no containing
        // block at all — that promotion is what makes fullscreen actually fullscreen.
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, new DrylCanvasRun()));
        Assert.Null(cut.Find(".ai-canvas").GetAttribute("popover"));

        cut.Find(".canvas-head-tools button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("manual", cut.Find(".ai-canvas").GetAttribute("popover"));
            Assert.NotEmpty(JSInterop.Invocations["dryl.topLayer.show"]);
        });

        cut.Find(".ai-canvas").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.Find(".ai-canvas").GetAttribute("popover"));
            Assert.NotEmpty(JSInterop.Invocations["dryl.topLayer.hide"]);
        });
    }

    [Fact]
    public void AllowExpand_false_renders_no_expand_button()
    {
        var cut = Render<DrylAiCanvas>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.AllowExpand, false));

        Assert.Empty(cut.FindAll(".canvas-head-tools button"));
    }

    // ---- W5: live build counter ------------------------------------------

    [Theory]
    [InlineData("""{"root":{"id":"r","type":"divider"}}""", 1)]
    [InlineData("""{"root":{"id":"r","type":"stack","children":[{"id":"a","type":"divider"},{"id":"b","type":"stack","children":[{"id":"c","type":"divider"}]}]}}""", 4)]
    public void NodeCount_counts_the_whole_tree(string json, int expected)
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse(json));

        Assert.Equal(expected, run.NodeCount);
    }

    [Fact]
    public void NodeCount_is_zero_without_an_artifact() =>
        Assert.Equal(0, new DrylCanvasRun().NodeCount);

    [Fact]
    public void Header_counts_elements_while_streaming_and_stops_when_ready()
    {
        var run = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        cut.InvokeAsync(() => run.BeginCreate());
        cut.InvokeAsync(() => run.RevealSnapshot(Parse(FirstArtifact)));

        cut.WaitForAssertion(() =>
        {
            var head = cut.Find(".canvas-head").TextContent;
            Assert.Contains("Building", head);
            Assert.Contains("element", head);
            Assert.NotEmpty(cut.FindAll(".canvas-build"));
        });

        cut.InvokeAsync(() => run.CompleteReveal(Parse(FirstArtifact)));

        cut.WaitForAssertion(() =>
        {
            var head = cut.Find(".canvas-head").TextContent;
            Assert.Contains("Ready", head);
            Assert.DoesNotContain("element", head);
        });
    }
}
