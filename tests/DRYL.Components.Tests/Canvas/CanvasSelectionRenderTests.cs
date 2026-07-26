using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasSelectionRenderTests : BunitContext
{
    public CanvasSelectionRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "Revenue", "value": "1" } },
            { "id": "grp", "type": "card", "props": { "title": "Group" }, "children": [
                { "id": "b", "type": "stat", "props": { "label": "Inner", "value": "2" } } ] } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(CanvasSelection sel) =>
        Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec()).Add(x => x.Selection, sel));

    [Fact]
    public void Without_a_selection_object_nothing_changes_in_the_markup()
    {
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec()));

        Assert.Empty(cut.FindAll(".canvas-node[tabindex]"));
        Assert.Empty(cut.FindAll(".canvas-node-tools"));
    }

    [Fact]
    public void Clicking_a_node_selects_it()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='a']").Click();

        Assert.Equal("a", sel.Id);
        Assert.Equal("Revenue", sel.Label);
        Assert.Contains("is-selected", cut.Find("[data-cid='a']").GetAttribute("class"));
    }

    [Fact]
    public void Clicking_an_inner_node_selects_the_inner_node_not_its_container()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='b']").Click();

        Assert.Equal("b", sel.Id);
    }

    [Fact]
    public void The_root_node_is_not_selectable()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        Assert.Null(cut.Find("[data-cid='root']").GetAttribute("tabindex"));
    }

    [Fact]
    public void Exactly_one_node_carries_the_tab_stop()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        Assert.Single(cut.FindAll(".canvas-node[tabindex='0']"));
        Assert.Equal("a", cut.Find(".canvas-node[tabindex='0']").GetAttribute("data-cid"));

        cut.Find("[data-cid='b']").Click();

        Assert.Single(cut.FindAll(".canvas-node[tabindex='0']"));
        Assert.Equal("b", cut.Find(".canvas-node[tabindex='0']").GetAttribute("data-cid"));
    }

    [Theory]
    [InlineData("a", "ArrowDown", "grp")]
    [InlineData("grp", "ArrowUp", "a")]
    [InlineData("grp", "ArrowRight", "b")]
    [InlineData("b", "ArrowLeft", "grp")]
    [InlineData("grp", "Home", "a")]
    [InlineData("a", "End", "grp")]
    public void Arrow_keys_walk_the_tree(string from, string key, string expected)
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find($"[data-cid='{from}']").Focus();
        cut.Find($"[data-cid='{from}']").Click();

        cut.Find($"[data-cid='{from}']").KeyDown(key);

        Assert.Equal(expected, sel.Id);
    }

    [Fact]
    public void Arrow_keys_do_nothing_while_the_wrapper_has_no_focus()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Click();
        cut.Find("[data-cid='a']").Blur();

        cut.Find("[data-cid='a']").KeyDown("ArrowDown");

        Assert.Equal("a", sel.Id);
    }

    [Fact]
    public void Escape_clears_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Escape");

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Enter_asks_the_dock_for_a_prompt()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Enter");

        Assert.Equal(1, asked);
    }

    [Fact]
    public void A_new_spec_instance_drops_the_selection()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);
        cut.Find("[data-cid='a']").Click();

        cut.Render(p => p.Add(x => x.Spec, Spec()).Add(x => x.Selection, sel));

        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void The_node_announces_itself()
    {
        var sel = new CanvasSelection();
        var cut = Canvas(sel);

        cut.Find("[data-cid='a']").Click();

        Assert.Contains("Selected: Revenue, stat", cut.Markup);
    }
}
