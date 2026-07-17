using System.Text.Json;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasPatcherTests
{
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "grp", "type": "card", "children": [
                { "id": "b", "type": "divider" } ] } ] } }
        """, CanvasJson.Options)!;

    // ---- setProps ----

    [Fact]
    public void SetProps_merges_shallow()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }"""),
        });
        Assert.Null(err);
        var a = spec.Root!.Children![0];
        Assert.Equal("+5%", a.Props!.Value.GetProperty("delta").GetString());
        Assert.Equal("A", a.Props!.Value.GetProperty("label").GetString());   // kept
    }

    [Fact]
    public void SetProps_invalid_result_rolls_back()
    {
        var spec = Spec();
        var before = spec.Root!.Children![0].Props;
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a",
            // value becomes empty -> invalid per CanvasCatalog "stat" rules
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "value": "" }"""),
        });
        Assert.NotNull(err);
        var a = spec.Root!.Children![0];
        Assert.Equal(before!.Value.GetRawText(), a.Props!.Value.GetRawText());
    }

    [Fact]
    public void SetProps_unknown_id_returns_reason()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "zzz",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }"""),
        });
        Assert.NotNull(err);
    }

    // ---- insert ----

    [Fact]
    public void Insert_adds_at_index()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode { Id = "c", Type = "divider" },
        });
        Assert.Null(err);
        var grp = spec.Root!.Children![1];
        Assert.Equal(2, grp.Children!.Count);
        Assert.Equal("c", grp.Children[0].Id);   // inserted before "b"
        Assert.Equal("b", grp.Children[1].Id);
    }

    [Fact]
    public void Insert_invalid_child_in_subtree_is_skipped_and_spec_untouched()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode
            {
                Id = "c", Type = "card",
                Children = new List<CanvasNode>
                {
                    // invalid: stat with no label/value
                    new() { Id = "c1", Type = "stat", Props = JsonSerializer.Deserialize<JsonElement>("{}") },
                },
            },
        });
        Assert.NotNull(err);
        var grp = spec.Root!.Children![1];
        Assert.Single(grp.Children!);
        Assert.Equal("b", grp.Children![0].Id);
    }

    [Fact]
    public void Insert_duplicate_id_is_skipped()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode { Id = "a", Type = "divider" },   // "a" already exists at root level
        });
        Assert.NotNull(err);
        var grp = spec.Root!.Children![1];
        Assert.Single(grp.Children!);
    }

    // ---- remove ----

    [Fact]
    public void Remove_marks_removing_node_still_present()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "a" });
        Assert.Null(err);
        Assert.Equal(2, spec.Root!.Children!.Count);
        Assert.True(spec.Root.Children[0].Removing);
    }

    [Fact]
    public void Remove_unknown_id_returns_reason()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "zzz" });
        Assert.NotNull(err);
    }

    [Fact]
    public void Remove_root_is_skipped()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "root" });
        Assert.NotNull(err);
        Assert.False(spec.Root!.Removing);
    }

    // ---- move ----

    [Fact]
    public void Move_reorders_within_parent()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "grp", Parent = "root", Index = 0 });
        Assert.Null(err);
        Assert.Equal("grp", spec.Root!.Children![0].Id);
        Assert.Equal("a", spec.Root.Children[1].Id);
    }

    [Fact]
    public void Move_across_parents()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "a", Parent = "grp", Index = 0 });
        Assert.Null(err);
        Assert.Single(spec.Root!.Children!);
        Assert.Equal("grp", spec.Root.Children![0].Id);
        var grp = spec.Root.Children[0];
        Assert.Equal(2, grp.Children!.Count);
        Assert.Equal("a", grp.Children[0].Id);
        Assert.Equal("b", grp.Children[1].Id);
    }

    [Fact]
    public void Move_into_own_subtree_is_skipped()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "grp", Parent = "b", Index = 0 });
        Assert.NotNull(err);
        // spec untouched
        Assert.Equal(2, spec.Root!.Children!.Count);
        var grp = spec.Root.Children[1];
        Assert.Single(grp.Children!);
    }

    [Fact]
    public void Move_index_is_clamped_when_out_of_range()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "a", Parent = "grp", Index = 99 });
        Assert.Null(err);
        var grp = spec.Root!.Children![0];
        Assert.Equal(2, grp.Children!.Count);
        Assert.Equal("a", grp.Children[^1].Id);   // appended at the end
    }

    [Fact]
    public void Move_unknown_id_returns_reason()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "zzz", Parent = "grp", Index = 0 });
        Assert.NotNull(err);
    }

    [Fact]
    public void Move_to_unknown_parent_returns_reason()
    {
        var spec = Spec();
        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "a", Parent = "zzz", Index = 0 });
        Assert.NotNull(err);
    }
}
