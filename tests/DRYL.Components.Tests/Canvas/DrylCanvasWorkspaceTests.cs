using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Bunit;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The workspace surface: named views, exactly one rendered, the rest one click away.</summary>
public class DrylCanvasWorkspaceTests : BunitContext
{
    public DrylCanvasWorkspaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    // Props is a JsonElement — specs are authored as JSON everywhere in this suite.
    private static CanvasSpec Spec(string title, string id, string text) =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""
            { "title": "{{title}}", "root": { "id": "{{id}}", "type": "markdown", "props": { "content": "{{text}}" } } }
            """,
            CanvasJson.Options)!;

    private static CanvasWorkspace TwoViews()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Overview");
        a.Spec = Spec("Overview", "r1", "first view");
        var b = ws.Open("Order 4711");
        b.Spec = Spec("Order 4711", "r2", "second view");
        ws.Activate(a.Id);
        return ws;
    }

    [Fact]
    public void Renders_a_chip_per_view_and_marks_the_active_one()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, TwoViews()));

        Assert.Equal(2, cut.FindAll(".ws-chip").Count);
        Assert.Contains("Order 4711", cut.Markup);

        var active = cut.Find("[data-dryl-ink-active='true']");
        Assert.Contains("Overview", active.TextContent);
        Assert.Single(cut.FindAll("[role='tab'][aria-selected='true']"));
    }

    [Fact]
    public void Renders_only_the_active_view()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, TwoViews()));

        Assert.Contains("first view", cut.Markup);
        Assert.DoesNotContain("second view", cut.Markup);
    }

    [Fact]
    public void Clicking_a_chip_activates_that_view()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll("[role='tab']")[1].Click();

        Assert.Equal("order-4711", ws.ActiveId);
        Assert.Contains("second view", cut.Markup);
    }

    [Fact]
    public void A_single_view_gets_no_bar_unless_asked_for()
    {
        var ws = new CanvasWorkspace();
        ws.Open("Only");

        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));
        Assert.Empty(cut.FindAll(".ws-chip"));

        cut.Render(p => p.Add(x => x.Workspace, ws).Add(x => x.ShowBarWhenSingle, true));
        Assert.Single(cut.FindAll(".ws-chip"));
    }

    [Fact]
    public void The_View_slot_receives_the_active_view()
    {
        var ws = TwoViews();
        RenderFragment<CanvasView> slot = view => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "slot:" + view.Title);
            builder.CloseElement();
        };

        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, ws)
            .Add(x => x.View, slot));

        Assert.Contains("slot:Overview", cut.Markup);
        Assert.DoesNotContain("first view", cut.Markup);   // the slot replaces the default canvas
    }

    [Fact]
    public void Closing_a_chip_flags_the_view_for_its_exit()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll(".ws-chip-close")[1].Click();

        Assert.True(ws.Views[1].Removing);
        Assert.Equal(2, ws.Views.Count);   // removal waits for the exit animation
    }

    [Fact]
    public void Close_buttons_disappear_when_AllowClose_is_off()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, TwoViews())
            .Add(x => x.AllowClose, false));

        Assert.Empty(cut.FindAll(".ws-chip-close"));
    }

    [Fact]
    public void Arrow_keys_walk_between_views()
    {
        var ws = TwoViews();
        var cut = Render<DrylCanvasWorkspace>(p => p.Add(x => x.Workspace, ws));

        cut.FindAll("[role='tab']")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Equal("order-4711", ws.ActiveId));

        cut.FindAll("[role='tab']")[1].KeyDown(new KeyboardEventArgs { Key = "Home" });
        cut.WaitForAssertion(() => Assert.Equal("overview", ws.ActiveId));
    }

    [Fact]
    public void An_empty_workspace_shows_the_empty_state()
    {
        var cut = Render<DrylCanvasWorkspace>(p => p
            .Add(x => x.Workspace, new CanvasWorkspace())
            .Add(x => x.EmptyText, "Ask for a view."));

        Assert.Contains("Ask for a view.", cut.Markup);
        Assert.Empty(cut.FindAll(".ws-chip"));
    }
}
