using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasNodeCloneTests
{
    private static CanvasNode Parse(string json) =>
        JsonSerializer.Deserialize<CanvasNode>(json, CanvasJson.Options)!;

    private static readonly IReadOnlySet<string> Taken =
        new HashSet<string>(StringComparer.Ordinal) { "card", "in", "card-2" };

    [Fact]
    public void Gives_every_node_of_the_subtree_a_free_id()
    {
        var node = Parse("""
            { "id": "card", "type": "card", "props": { "title": "Order" }, "children": [
                { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } } ] }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("card-3", copy.Id);              // card-2 was taken
        Assert.Equal("in-2", copy.Children![0].Id);
    }

    [Fact]
    public void Renames_interactive_fields_so_the_copy_owns_its_own_value()
    {
        var node = Parse("""
            { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("qty-2", copy.Props!.Value.GetProperty("name").GetString());
        Assert.Equal("Qty", copy.Props!.Value.GetProperty("label").GetString());
    }

    [Fact]
    public void Keeps_data_and_action_bindings()
    {
        var node = Parse("""
            { "id": "c", "type": "lineChart",
              "data": { "source": "sales.byMonth", "params": { "year": 2026 } } }
            """);

        var copy = CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("sales.byMonth", copy.Data!.Source);
        Assert.Equal(2026, copy.Data!.Params!.Value.GetProperty("year").GetInt32());
    }

    [Fact]
    public void A_copy_starts_unpinned()
    {
        var node = Parse("""{ "id": "c", "type": "divider", "locked": true }""");

        Assert.False(CanvasNodeClone.Duplicate(node, Taken).Locked);
    }

    [Fact]
    public void Leaves_the_original_untouched()
    {
        var node = Parse("""
            { "id": "in", "type": "inputText", "props": { "name": "qty", "label": "Qty" } }
            """);

        CanvasNodeClone.Duplicate(node, Taken);

        Assert.Equal("in", node.Id);
        Assert.Equal("qty", node.Props!.Value.GetProperty("name").GetString());
    }

    [Fact]
    public void Two_copies_in_a_row_do_not_collide()
    {
        var node = Parse("""{ "id": "d", "type": "divider" }""");
        var ids = new HashSet<string>(StringComparer.Ordinal) { "d" };

        var first = CanvasNodeClone.Duplicate(node, ids);
        ids.Add(first.Id);
        var second = CanvasNodeClone.Duplicate(node, ids);

        Assert.Equal("d-2", first.Id);
        Assert.Equal("d-3", second.Id);
    }
}
