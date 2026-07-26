using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasDockSelectionTests : BunitContext
{
    public CanvasDockSelectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasNode Chart() => new()
    {
        Id = "c3",
        Type = "lineChart",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "title": "Revenue by month" }"""),
    };

    [Fact]
    public void Without_a_selection_the_dock_shows_no_chip()
    {
        var cut = Render<DrylCanvasDock>();

        Assert.Empty(cut.FindAll(".dock-context"));
    }

    [Fact]
    public void The_chip_names_the_selected_element()
    {
        var sel = new CanvasSelection();
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Selection, sel));

        cut.InvokeAsync(() => sel.Select(Chart()));

        Assert.Contains("Revenue by month", cut.Find(".dock-context").TextContent);
        Assert.Contains("lineChart", cut.Find(".dock-context").TextContent);
    }

    [Fact]
    public void The_chips_clear_button_drops_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Render<DrylCanvasDock>(p => p.Add(x => x.Selection, sel));
        cut.InvokeAsync(() => sel.Select(Chart()));

        cut.Find(".dock-context button[aria-label='Clear context']").Click();

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Sending_prefixes_the_text_with_the_element_reference()
    {
        var sel = new CanvasSelection();
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, sel)
            .Add(x => x.OnSend, EventCallback.Factory.Create<string>(this, t => sent = t)));
        cut.InvokeAsync(() => sel.Select(Chart()));

        var composer = cut.FindComponent<DrylChatComposer>();
        composer.Find("textarea").Input("make it a bar chart");
        composer.Find("button").Click();

        Assert.Equal(
            "Regarding the artifact element \"c3\" (lineChart, \"Revenue by month\"):\nmake it a bar chart",
            sent);
    }

    [Fact]
    public void Without_a_selection_the_text_goes_out_unchanged()
    {
        string? sent = null;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, new CanvasSelection())
            .Add(x => x.OnSend, EventCallback.Factory.Create<string>(this, t => sent = t)));

        var composer = cut.FindComponent<DrylChatComposer>();
        composer.Find("textarea").Input("build an overview");
        composer.Find("button").Click();

        Assert.Equal("build an overview", sent);
    }

    [Fact]
    public void A_prompt_request_expands_a_collapsed_dock()
    {
        var sel = new CanvasSelection();
        var collapsed = true;
        var cut = Render<DrylCanvasDock>(p => p
            .Add(x => x.Selection, sel)
            .Add(x => x.Collapsed, true)
            .Add(x => x.CollapsedChanged, EventCallback.Factory.Create<bool>(this, v => collapsed = v)));

        cut.InvokeAsync(() => sel.RequestPrompt());

        Assert.False(collapsed);
    }
}
