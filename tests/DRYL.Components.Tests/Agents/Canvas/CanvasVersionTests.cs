using System.Text.Json;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Version-stamp tests: every successful tree mutation must bump
/// <c>CanvasNode.Version</c> on exactly the nodes it touches (renderers memoize
/// parse + validation work on that stamp). Rolled-back ops bump nothing.
/// </summary>
public class CanvasVersionTests
{
    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasSpec Spec() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "grp", "type": "card", "children": [
                { "id": "b", "type": "divider" } ] } ] } }
        """);

    private static CanvasSpec MoveSpec() => Parse("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "g1", "type": "card", "children": [ { "id": "m", "type": "divider" } ] },
            { "id": "g2", "type": "card", "children": [] } ] } }
        """);

    private static JsonElement Props(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public void SetProps_bumps_only_the_target_node()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var grp = spec.Root!.Children![1];
        var rootV = spec.Root.Version;
        var aV = a.Version;
        var grpV = grp.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "delta": "+5%" }"""),
        });

        Assert.Null(err);
        Assert.Equal(aV + 1, a.Version);
        Assert.Equal(grpV, grp.Version);
        Assert.Equal(rootV, spec.Root.Version);
    }

    [Fact]
    public void Rolled_back_setProps_bumps_nothing()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var aV = a.Version;

        // value becomes empty -> invalid per CanvasCatalog "stat" rules -> rollback
        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "setProps", Id = "a", Props = Props("""{ "value": "" }"""),
        });

        Assert.NotNull(err);
        Assert.Equal(aV, a.Version);
    }

    [Fact]
    public void Insert_bumps_the_parent()
    {
        var spec = Spec();
        var grp = spec.Root!.Children![1];
        var grpV = grp.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp
        {
            Op = "insert", Parent = "grp",
            Node = new CanvasNode { Id = "n1", Type = "divider" },
        });

        Assert.Null(err);
        Assert.Equal(grpV + 1, grp.Version);
    }

    [Fact]
    public void Remove_bumps_the_removed_node()
    {
        var spec = Spec();
        var a = spec.Root!.Children![0];
        var aV = a.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "remove", Id = "a" });

        Assert.Null(err);
        Assert.True(a.Removing);
        Assert.Equal(aV + 1, a.Version);
    }

    [Fact]
    public void Move_bumps_both_parents()
    {
        var spec = MoveSpec();
        var g1 = spec.Root!.Children![0];
        var g2 = spec.Root!.Children![1];
        var g1V = g1.Version;
        var g2V = g2.Version;

        var err = CanvasPatcher.Apply(spec, new CanvasOp { Op = "move", Id = "m", Parent = "g2" });

        Assert.Null(err);
        Assert.Equal(g1V + 1, g1.Version);
        Assert.Equal(g2V + 1, g2.Version);
    }

    [Fact]
    public void Purge_bumps_the_parent()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        var grp = run.Spec!.Root!.Children![1];
        var grpV = grp.Version;

        run.Purge("b");

        Assert.Empty(grp.Children!);
        Assert.Equal(grpV + 1, grp.Version);
    }

    [Fact]
    public void Reveal_adding_a_child_bumps_the_live_parent()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);
        var root = live.Root!;
        Assert.Single(root.Children!);   // d2 (streaming tail leaf) is withheld
        var v = root.Version;

        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"},
                {"id":"d3","type":"divider"}]}}
            """), streamDone: false);

        Assert.Equal(2, root.Children!.Count);
        Assert.Equal(v + 1, root.Version);
    }

    [Fact]
    public void Reveal_updating_tail_props_bumps_the_live_tail()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"A"},"children":[]}]}}
            """), streamDone: false);
        var tail = live.Root!.Children![1];
        var v = tail.Version;

        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"AB"},"children":[]}]}}
            """), streamDone: false);

        Assert.Equal(v + 1, tail.Version);
        Assert.Equal("AB", tail.Props!.Value.GetProperty("title").GetString());
    }

    [Fact]
    public void Reveal_updating_root_props_bumps_the_root()
    {
        var live = new CanvasSpec();
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{"gap":"sm"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);
        var root = live.Root!;
        var v = root.Version;

        // Same children, only the root's props change -> exactly one bump.
        CanvasStreamReveal.Reveal(live, Parse("""
            {"root":{"id":"root","type":"stack","props":{"gap":"md"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """), streamDone: false);

        Assert.Equal(v + 1, root.Version);
    }
}
