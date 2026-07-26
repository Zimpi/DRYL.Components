using System.Text.Json;
using Bunit;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>The dock is a command bar, not a chat: one input, one status line, the log on demand.</summary>
public class DrylCanvasDockTests : BunitContext
{
    public DrylCanvasDockTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static DrylCanvasRun BuildingRun()
    {
        var spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "markdown", "props": { "content": "x" } },
                { "id": "b", "type": "markdown", "props": { "content": "y" } } ] } }
            """, CanvasJson.Options)!;

        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(spec);
        return run;
    }

    [Fact]
    public void The_status_line_reports_the_run()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Contains("Building", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void An_untouched_run_reads_as_idle()
    {
        // A canvas run exists long before anything asks it for an artifact — a freshly loaded
        // page must not claim the assistant is working.
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, new DrylCanvasRun()));

        Assert.Contains("Idle", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_restored_artifact_reads_as_ready_not_as_working()
    {
        // A document loaded from a store fills Spec without any generation ever running. Claiming
        // work would be a lie nothing can settle: no generation is coming to end it.
        var spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "title": "Report", "root": { "id": "root", "type": "stack", "children": [
                { "id": "a", "type": "markdown", "props": { "content": "x" } } ] } }
            """, CanvasJson.Options)!;

        var workspace = new CanvasWorkspace();
        workspace.Open("Übersicht").Spec = spec;
        var run = new DrylCanvasRun();
        run.UseWorkspace(workspace);

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, run));

        var status = cut.Find(".dock-status").TextContent;
        Assert.Contains("Ready", status);
        Assert.DoesNotContain("Working", status);
    }

    [Fact]
    public void Busy_alone_makes_the_dock_work()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, new DrylCanvasRun())
            .Add(x => x.Busy, true));

        Assert.Contains("Working", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void A_failed_run_puts_its_message_in_the_status_line()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.FailGeneration(new InvalidOperationException("generator gave up"));

        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, run));

        Assert.Contains("generator gave up", cut.Find(".dock-status").TextContent);
        Assert.Contains("is-error", cut.Find(".dock-status").GetAttribute("class"));
    }

    [Fact]
    public void An_explicit_status_wins()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Run, BuildingRun())
            .Add(x => x.Status, "Waiting for approval"));

        Assert.Contains("Waiting for approval", cut.Find(".dock-status").TextContent);
        Assert.DoesNotContain("Building", cut.Find(".dock-status").TextContent);
    }

    [Fact]
    public void Sending_raises_OnSend_with_the_draft()
    {
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.OnSend, EventCallback.Factory.Create<string>(this, s => sent = s)));

        var composer = cut.FindComponent<DrylChatComposer>();
        composer.Find("textarea").Input("open the order view");
        composer.Find("button").Click();

        Assert.Equal("open the order view", sent);
    }

    [Fact]
    public void Busy_disables_the_composer()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Busy, true));

        Assert.True(cut.FindComponent<DrylChatComposer>().Instance.Disabled);
    }

    [Fact]
    public void Collapsed_leaves_a_single_labelled_button()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Collapsed, true)
            .Add(x => x.Title, "Assistant"));

        Assert.Empty(cut.FindAll(".dock-panel"));
        Assert.Equal("Assistant", cut.Find(".dock-fab button").GetAttribute("aria-label"));

        cut.Find(".dock-fab button").Click();
        Assert.Single(cut.FindAll(".dock-panel"));
    }

    [Fact]
    public void Without_a_log_there_is_no_disclosure()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Run, BuildingRun()));

        Assert.Empty(cut.FindAll(".dock-log-toggle"));
        Assert.Empty(cut.FindAll(".dock-log"));
    }

    [Fact]
    public void The_log_slot_renders_and_toggles()
    {
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Log, (RenderFragment)(b => b.AddMarkupContent(0, "<p>turn one</p>"))));

        Assert.Contains("turn one", cut.Markup);
        Assert.Equal("true", cut.Find(".dock-log").GetAttribute("aria-hidden"));

        cut.Find(".dock-log-toggle").Click();
        Assert.Null(cut.Find(".dock-log").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void The_corner_becomes_a_class()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Corner, DockCorner.TopLeft));

        Assert.Contains("canvas-dock--tl", cut.Find(".canvas-dock").GetAttribute("class"));
    }

    [Fact]
    public void Host_actions_render_in_the_head()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Actions,
            (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"stop\">Stop</button>"))));

        Assert.NotNull(cut.Find(".dock-head #stop"));
    }

    [Fact]
    public void Suggestions_render_above_the_composer()
    {
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Suggestions,
            (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"chip\">Kennzahlen</button>"))));

        Assert.NotNull(cut.Find(".dock-suggestions #chip"));
    }

    [Fact]
    public void Without_the_slots_the_dock_grows_no_containers()
    {
        var cut = Render<DrylCanvasDock>();

        Assert.Empty(cut.FindAll(".dock-actions"));
        Assert.Empty(cut.FindAll(".dock-suggestions"));
    }
}
