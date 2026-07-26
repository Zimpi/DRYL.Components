using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using DRYL.Components;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasNodeToolsTests : BunitContext
{
    public CanvasNodeToolsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "Revenue", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "Orders", "value": "2" } } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(
        CanvasSpec spec, CanvasSelection sel, List<CanvasEdit>? edits = null) =>
        Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, sel)
            .Add(x => x.OnEdit, e => edits?.Add(e)));

    private static IElement Tool(IRenderedComponent<DrylCanvas> cut, string label) =>
        cut.Find($".canvas-node-tools button[aria-label='{label}']");

    [Fact]
    public void The_toolbar_appears_only_on_the_selected_node()
    {
        var cut = Canvas(Spec(), new CanvasSelection());
        Assert.Empty(cut.FindAll(".canvas-node-tools"));

        cut.Find("[data-cid='a']").Click();

        Assert.Single(cut.FindAll(".canvas-node-tools"));
        Assert.Contains("canvas-node-tools", cut.Find("[data-cid='a']").InnerHtml);
    }

    [Fact]
    public void Pinning_locks_the_node_and_reports_an_edit()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Pin element").Click();

        Assert.True(spec.Root!.Children![0].Locked);
        Assert.True(sel.Locked);
        Assert.Equal(CanvasNodeCommand.TogglePin, edits[0].Command);
        Assert.Equal("Pinned Revenue", edits[0].Label);
        Assert.Contains("Revenue pinned.", cut.Markup);
    }

    [Fact]
    public void A_pinned_node_shows_its_mark_and_disables_the_destructive_tools()
    {
        var spec = Spec();
        spec.Root!.Children![0].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Click();

        Assert.Single(cut.FindAll(".canvas-node-pin"));
        Assert.True(Tool(cut, "Duplicate element").HasAttribute("disabled"));
        Assert.True(Tool(cut, "Remove element").HasAttribute("disabled"));
        Assert.False(Tool(cut, "Unpin element").HasAttribute("disabled"));
    }

    [Fact]
    public void Duplicating_inserts_a_fresh_copy_right_after_the_original()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Duplicate element").Click();

        Assert.Equal(3, spec.Root!.Children!.Count);
        Assert.Equal("a-2", spec.Root.Children[1].Id);
        Assert.Equal("a-2", sel.Id);                       // the copy is selected
        Assert.Equal("Duplicated Revenue", edits[0].Label);
    }

    [Fact]
    public void Removing_flags_the_node_for_its_exit_and_clears_the_selection()
    {
        var spec = Spec();
        var sel = new CanvasSelection();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, sel, edits);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Remove element").Click();

        Assert.True(spec.Root!.Children![0].Removing);
        Assert.False(sel.HasSelection);
        Assert.Equal("Removed Revenue", edits[0].Label);
    }

    [Fact]
    public void Delete_removes_the_focused_node()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Delete");

        Assert.True(spec.Root!.Children![0].Removing);
    }

    [Fact]
    public void Delete_refuses_a_pinned_node()
    {
        var spec = Spec();
        spec.Root!.Children![0].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown("Delete");

        Assert.False(spec.Root.Children[0].Removing);
    }

    [Fact]
    public void Without_a_purge_handler_the_canvas_drops_the_node_itself()
    {
        var spec = Spec();
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, new CanvasSelection()));
        cut.Find("[data-cid='a']").Click();
        cut.Find(".canvas-node-tools button[aria-label='Remove element']").Click();

        cut.InvokeAsync(() => cut.Instance.PurgeForTest("a"));

        Assert.Single(spec.Root!.Children!);
        Assert.Equal("b", spec.Root.Children[0].Id);
    }

    [Fact]
    public void The_prompt_tool_asks_for_a_prompt()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;
        var cut = Canvas(Spec(), sel);
        cut.Find("[data-cid='a']").Click();

        Tool(cut, "Prompt about this element").Click();

        Assert.Equal(1, asked);
    }
}
