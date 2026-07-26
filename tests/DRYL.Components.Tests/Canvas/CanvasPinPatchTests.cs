using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// The pin is an instruction to the AI author, not a freeze of the widget: an op the user
/// triggered (a data refresh, an action result, the node toolbar) still goes through.
/// </summary>
public class CanvasPinPatchTests
{
    // "grp" is a pinned card holding "b"; "a" is a pinned stat next to it.
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "locked": true, "props": { "label": "A", "value": "1" } },
            { "id": "free", "type": "stat", "props": { "label": "B", "value": "2" } },
            { "id": "grp", "type": "card", "locked": true, "children": [
                { "id": "b", "type": "stat", "props": { "label": "C", "value": "3" } } ] } ] } }
        """, CanvasJson.Options)!;

    private static JsonElement Props(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public void Locked_survives_a_json_roundtrip_and_false_is_not_written()
    {
        var spec = Spec();
        var json = JsonSerializer.Serialize(spec, CanvasJson.Options);

        Assert.Contains("\"locked\":true", json);
        Assert.DoesNotContain("\"locked\":false", json);
        Assert.True(JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!
            .Root!.Children![0].Locked);
    }

    [Fact]
    public void Ai_setProps_on_a_pinned_node_is_refused_and_changes_nothing()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "999" }"""),
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'setProps': node 'a' is pinned by the user — leave it unchanged and say so if asked.", err);
        Assert.Equal("1", spec.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
    }

    [Fact]
    public void The_same_setProps_goes_through_for_the_user()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "999" }"""),
        });

        Assert.Null(err);
        Assert.Equal("999", spec.Root!.Children![0].Props!.Value.GetProperty("value").GetString());
    }

    [Fact]
    public void Ai_remove_of_a_pinned_node_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "a" },
                                      CanvasPatchAuthor.Ai);

        Assert.Equal("op 'remove': node 'a' is pinned by the user — it must stay.", err);
        Assert.False(spec.Root!.Children![0].Removing);
    }

    [Fact]
    public void Ai_move_of_a_pinned_node_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "a", Parent = "grp", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'a' is pinned by the user — its position must stay.", err);
        Assert.Equal("a", spec.Root!.Children![0].Id);
    }

    [Fact]
    public void Ai_move_into_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "free", Parent = "grp", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'grp' is pinned by the user — nothing may be moved out of or into it.", err);
        Assert.Equal(3, spec.Root!.Children!.Count);
    }

    [Fact]
    public void Ai_move_out_of_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "move", Id = "b", Parent = "root", Index = 0,
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'move': node 'grp' is pinned by the user — nothing may be moved out of or into it.", err);
        Assert.Single(spec.Root!.Children![2].Children!);
    }

    [Fact]
    public void Ai_insert_into_a_pinned_container_is_refused()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode { Id = "n", Type = "divider" },
        }, CanvasPatchAuthor.Ai);

        Assert.Equal("op 'insert': node 'grp' is pinned by the user — nothing may be added to it.", err);
        Assert.Single(spec.Root!.Children![2].Children!);
    }

    [Fact]
    public void A_child_of_a_pinned_container_stays_patchable_for_the_ai()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "b", Props = Props("""{ "value": "42" }"""),
        }, CanvasPatchAuthor.Ai);

        Assert.Null(err);
        Assert.Equal("42", spec.Root!.Children![2].Children![0].Props!.Value
            .GetProperty("value").GetString());
    }

    [Fact]
    public void An_unpinned_node_is_untouched_by_the_rule()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "free" },
                                      CanvasPatchAuthor.Ai);

        Assert.Null(err);
        Assert.True(spec.Root!.Children![1].Removing);
    }
}
