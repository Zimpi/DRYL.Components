using System.Text.Json;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Covers the create-streaming reveal choreography: nodes surface one complete unit at a time (leaf level,
/// recursively), the still-streaming tail is withheld, and already-revealed instances stay frozen.
/// </summary>
public class CanvasStreamRevealTests
{
    private static CanvasSpec Spec(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static IReadOnlyList<CanvasNode> Kids(DrylCanvasRun run) =>
        run.Spec?.Root?.Children ?? new List<CanvasNode>();

    private static CanvasNode? Child(DrylCanvasRun run, string id) =>
        Kids(run).FirstOrDefault(c => c.Id == id);

    // ---- a leaf tail is withheld until a sibling follows (or the stream ends) ----

    [Fact]
    public void Reveals_complete_children_and_withholds_the_streaming_leaf_tail()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        // Two stats present, but the second is still the streaming tail — only the first is complete.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat","props":{"label":"B","value":"2"}}]}}
            """));

        Assert.Single(Kids(run));
        Assert.Equal("a", Kids(run)[0].Id);   // "b" is the withheld tail

        // Stream ends: "b" is flushed in.
        run.CompleteReveal(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat","props":{"label":"B","value":"2"}}]}}
            """));

        Assert.Equal(2, Kids(run).Count);
        Assert.Equal(new[] { "a", "b" }, Kids(run).Select(c => c.Id).ToArray());
        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal(1, run.Round);
    }

    // ---- a revealed node is frozen: same instance across later snapshots ----

    [Fact]
    public void A_revealed_node_keeps_its_instance_across_later_snapshots()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat"}]}}
            """));
        var firstInstance = Child(run, "a");
        Assert.NotNull(firstInstance);

        // A later snapshot re-parses "a" identically (a brand-new object) while "c" streams in.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat","props":{"label":"B","value":"2"}},
                {"id":"c","type":"stat"}]}}
            """));

        Assert.Same(firstInstance, Child(run, "a"));   // frozen — Blazor can skip re-rendering it
        Assert.NotNull(Child(run, "b"));               // "b" now complete and revealed
    }

    // ---- leaf-level reveal inside a still-streaming container ----

    [Fact]
    public void Reveals_inner_children_of_a_streaming_container_tail_one_at_a_time()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        // Grid is the streaming tail of root, but its first stat is already complete (a sibling follows).
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"grid","type":"grid","props":{"columns":3},"children":[
                    {"id":"umsatz","type":"stat","props":{"label":"Umsatz","value":"1"}},
                    {"id":"neukunden","type":"stat"}]}]}}
            """));

        var grid = Child(run, "grid");
        Assert.NotNull(grid);                       // container shell surfaced early
        Assert.Single(grid!.Children!);             // only the completed inner stat
        Assert.Equal("umsatz", grid.Children![0].Id);

        // Second inner stat completes (third begins).
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"grid","type":"grid","props":{"columns":3},"children":[
                    {"id":"umsatz","type":"stat","props":{"label":"Umsatz","value":"1"}},
                    {"id":"neukunden","type":"stat","props":{"label":"Neukunden","value":"2"}},
                    {"id":"churn","type":"stat"}]}]}}
            """));

        Assert.Equal(2, Child(run, "grid")!.Children!.Count);
        Assert.Equal(new[] { "umsatz", "neukunden" }, Child(run, "grid")!.Children!.Select(c => c.Id).ToArray());

        // Stream ends: the whole tree flushes (third stat + the line chart that followed the grid).
        run.CompleteReveal(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"grid","type":"grid","props":{"columns":3},"children":[
                    {"id":"umsatz","type":"stat","props":{"label":"Umsatz","value":"1"}},
                    {"id":"neukunden","type":"stat","props":{"label":"Neukunden","value":"2"}},
                    {"id":"churn","type":"stat","props":{"label":"Churn","value":"3"}}]},
                {"id":"chart","type":"lineChart","props":{"labels":["x"],"series":[{"name":"s","data":[1]}]}}]}}
            """));

        Assert.Equal(new[] { "grid", "chart" }, Kids(run).Select(c => c.Id).ToArray());
        Assert.Equal(3, Child(run, "grid")!.Children!.Count);
    }

    // ---- raise economy: no re-render when nothing new is revealed ----

    [Fact]
    public void Does_not_raise_when_only_the_withheld_tail_changed()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat","props":{"label":"B"}}]}}
            """));

        var raises = 0;
        run.OnChange += () => raises++;

        // Only the withheld tail "b" grows its props — nothing new becomes complete.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
                {"id":"b","type":"stat","props":{"label":"B","value":"2"}}]}}
            """));

        Assert.Equal(0, raises);
    }

    // ---- create-vs-update contract: Spec stays null until the first snapshot ----

    [Fact]
    public void BeginCreate_leaves_spec_null_until_the_first_snapshot_arrives()
    {
        var run = new DrylCanvasRun();

        run.BeginCreate();

        // A consumer keys create-vs-update on `Spec is null` (e.g. picks the generation script by phase),
        // so BeginCreate must NOT establish a tree before the generator has even been asked to stream.
        Assert.Null(run.Spec);
        Assert.Equal(AiState.Streaming, run.State);

        run.RevealSnapshot(Spec("""{"root":{"id":"root","type":"stack","children":[]}}"""));
        Assert.NotNull(run.Spec);
    }

    // ---- a shell is never seeded from a half-streamed type string ----

    [Fact]
    public void Never_seeds_a_node_from_a_partial_type_string()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        // PartialJsonReader surfaces open strings character by character, so a snapshot can carry a
        // half-streamed type ("stac" for "stack") with no props/children yet. That must NOT be frozen as
        // the node's identity — the reveal waits until props/children prove the type is complete.
        run.RevealSnapshot(Spec("""{"root":{"id":"root","type":"stac"}}"""));
        Assert.Null(run.Spec!.Root);   // withheld — identity not yet sealed

        run.RevealSnapshot(Spec("""{"root":{"id":"root","type":"stack","children":[]}}"""));
        Assert.Equal("stack", run.Spec!.Root!.Type);   // sealed with the complete type
    }

    // ---- title streams into the header ----

    [Fact]
    public void Title_is_revealed_and_grows()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        run.RevealSnapshot(Spec("""{"title":"Sales","root":{"id":"root","type":"stack","children":[]}}"""));
        Assert.Equal("Sales", run.Spec!.Title);

        run.RevealSnapshot(Spec("""{"title":"Sales Q3","root":{"id":"root","type":"stack","children":[]}}"""));
        Assert.Equal("Sales Q3", run.Spec!.Title);
    }

    // ---- a former shell's own props final-sync when it enters the complete zone ----

    [Fact]
    public void Complete_zone_syncs_props_of_a_former_shell()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        // c1 is the streaming tail container -> revealed as a shell with partial props.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"A"},"children":[]}]}}
            """));

        // A later sibling starts -> c1 becomes complete; its props finished growing meanwhile.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"AB"},"children":[
                    {"id":"x","type":"divider"}]},
                {"id":"d2","type":"divider"}]}}
            """));

        var c1 = Child(run, "c1")!;
        Assert.Equal("AB", c1.Props!.Value.GetProperty("title").GetString());
    }

    // ---- the done flush syncs root props too ----

    [Fact]
    public void Done_flush_syncs_root_props()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{"gap":"sm"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """));

        run.CompleteReveal(Spec("""
            {"root":{"id":"root","type":"stack","props":{"gap":"lg"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """));

        Assert.Equal("lg", run.Spec!.Root!.Props!.Value.GetProperty("gap").GetString());
    }
}
