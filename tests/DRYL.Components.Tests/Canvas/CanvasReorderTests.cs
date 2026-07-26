using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Canvas;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasReorderTests : BunitContext
{
    public CanvasReorderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "b", "type": "stat", "props": { "label": "B", "value": "2" } },
            { "id": "c", "type": "stat", "props": { "label": "C", "value": "3" } } ] } }
        """, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> Canvas(
        CanvasSpec spec, CanvasSelection sel, List<CanvasEdit>? edits = null) =>
        Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.Selection, sel)
            .Add(x => x.OnEdit, e => edits?.Add(e)));

    private static string[] Ids(CanvasSpec spec) =>
        spec.Root!.Children!.Select(c => c.Id).ToArray();

    [Fact]
    public void The_drop_reported_from_js_becomes_one_move_op()
    {
        var spec = Spec();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, new CanvasSelection(), edits);

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("c", 0));

        Assert.Equal(new[] { "c", "a", "b" }, Ids(spec));
        Assert.Equal(CanvasNodeCommand.MoveUp, edits[0].Command);
    }

    [Fact]
    public void A_drop_onto_the_same_slot_changes_nothing_and_reports_nothing()
    {
        var spec = Spec();
        var edits = new List<CanvasEdit>();
        var cut = Canvas(spec, new CanvasSelection(), edits);

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("a", 0));

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
        Assert.Empty(edits);
    }

    [Fact]
    public void A_drop_on_a_pinned_node_is_refused()
    {
        var spec = Spec();
        spec.Root!.Children![2].Locked = true;
        var cut = Canvas(spec, new CanvasSelection());

        cut.InvokeAsync(() => cut.Instance.OnNodeReorder("c", 0));

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
    }

    [Fact]
    public void Alt_arrow_moves_the_focused_node_one_slot()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='b']").Focus();
        cut.Find("[data-cid='b']").Click();

        cut.Find("[data-cid='b']").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        Assert.Equal(new[] { "b", "a", "c" }, Ids(spec));
    }

    [Fact]
    public void Alt_arrow_at_the_edge_does_nothing()
    {
        var spec = Spec();
        var cut = Canvas(spec, new CanvasSelection());
        cut.Find("[data-cid='a']").Focus();
        cut.Find("[data-cid='a']").Click();

        cut.Find("[data-cid='a']").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        Assert.Equal(new[] { "a", "b", "c" }, Ids(spec));
    }

    [Fact]
    public void The_grip_is_offered_only_when_the_node_has_siblings()
    {
        var cut = Canvas(Spec(), new CanvasSelection());
        cut.Find("[data-cid='a']").Click();
        Assert.Single(cut.FindAll("[data-drag-handle]"));

        var solo = JsonSerializer.Deserialize<CanvasSpec>("""
            { "root": { "id": "root", "type": "stack", "children": [
                { "id": "only", "type": "divider" } ] } }
            """, CanvasJson.Options)!;
        var second = Canvas(solo, new CanvasSelection());
        second.Find("[data-cid='only']").Click();

        Assert.Empty(second.FindAll("[data-drag-handle]"));
    }
}
