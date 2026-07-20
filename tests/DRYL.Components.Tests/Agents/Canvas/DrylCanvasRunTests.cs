using System.Text.Json;
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class DrylCanvasRunTests
{
    private static CanvasSpec Spec() => JsonSerializer.Deserialize<CanvasSpec>("""
        { "root": { "id": "root", "type": "stack", "children": [
            { "id": "a", "type": "stat", "props": { "label": "A", "value": "1" } },
            { "id": "grp", "type": "card", "children": [
                { "id": "b", "type": "divider" } ] } ] } }
        """, CanvasJson.Options)!;

    // ---- initial state ----

    [Fact]
    public void Initial_state_is_thinking_with_no_spec()
    {
        var run = new DrylCanvasRun();
        Assert.Equal(AiState.Thinking, run.State);
        Assert.Null(run.Spec);
        Assert.Equal(0, run.Round);
        Assert.Empty(run.ChangedIds);
    }

    // ---- BeginGeneration ----

    [Fact]
    public void BeginGeneration_sets_streaming_and_raises()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        run.BeginGeneration();

        Assert.Equal(AiState.Streaming, run.State);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void BeginGeneration_clears_changed_ids()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "a",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }"""),
        });
        Assert.NotEmpty(run.ChangedIds);

        run.BeginGeneration();

        Assert.Empty(run.ChangedIds);
    }

    // ---- ApplySnapshot ----

    [Fact]
    public void ApplySnapshot_sets_spec_and_raises()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        var spec = Spec();
        run.ApplySnapshot(spec);

        Assert.Same(spec, run.Spec);
        Assert.Equal(1, changes);
    }

    // ---- ApplyOp ----

    [Fact]
    public void ApplyOp_null_spec_returns_skip_reason_without_throwing()
    {
        var run = new DrylCanvasRun();
        var reason = run.ApplyOp(new CanvasOp { Op = "setProps", Id = "a" });

        Assert.NotNull(reason);
        Assert.Contains("no artifact to patch", reason);
    }

    [Fact]
    public void ApplyOp_null_spec_does_not_raise_onchange()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        run.ApplyOp(new CanvasOp { Op = "setProps", Id = "a" });

        Assert.Equal(0, changes);
    }

    [Fact]
    public void ApplyOp_setProps_routes_to_patcher_and_records_changed_id()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        var changes = 0;
        run.OnChange += () => changes++;

        var reason = run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "a",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }"""),
        });

        Assert.Null(reason);
        Assert.Equal("+5%", run.Spec!.Root!.Children![0].Props!.Value.GetProperty("delta").GetString());
        Assert.Contains("a", run.ChangedIds);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void ApplyOp_move_records_id()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var reason = run.ApplyOp(new CanvasOp { Op = "move", Id = "a", Parent = "grp", Index = 0 });

        Assert.Null(reason);
        Assert.Contains("a", run.ChangedIds);
    }

    [Fact]
    public void ApplyOp_insert_records_node_id_not_parent_id()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var reason = run.ApplyOp(new CanvasOp
        {
            Op = "insert", Parent = "grp", Index = 0,
            Node = new CanvasNode { Id = "c", Type = "divider" },
        });

        Assert.Null(reason);
        Assert.Contains("c", run.ChangedIds);
        Assert.DoesNotContain("grp", run.ChangedIds);
    }

    [Fact]
    public void ApplyOp_remove_records_nothing()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var reason = run.ApplyOp(new CanvasOp { Op = "remove", Id = "a" });

        Assert.Null(reason);
        Assert.Empty(run.ChangedIds);
        Assert.True(run.Spec!.Root!.Children![0].Removing);
    }

    [Fact]
    public void ApplyOp_failure_does_not_record_changed_id()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        var reason = run.ApplyOp(new CanvasOp { Op = "setProps", Id = "zzz" });

        Assert.NotNull(reason);
        Assert.Empty(run.ChangedIds);
    }

    [Fact]
    public void ApplyOp_failure_does_not_raise_onchange()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        var changes = 0;
        run.OnChange += () => changes++;

        run.ApplyOp(new CanvasOp { Op = "setProps", Id = "zzz" });

        Assert.Equal(0, changes);
    }

    // ---- CompleteGeneration ----

    [Fact]
    public void CompleteGeneration_noarg_marks_generated_and_increments_round_without_replacing_spec()
    {
        var run = new DrylCanvasRun();
        var spec = Spec();
        run.ApplySnapshot(spec);
        run.ApplyOp(new CanvasOp
        {
            Op = "setProps", Id = "a",
            Props = JsonSerializer.Deserialize<JsonElement>("""{ "delta": "+5%" }"""),
        });
        var changes = 0;
        run.OnChange += () => changes++;

        run.CompleteGeneration();

        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal(1, run.Round);
        Assert.Same(spec, run.Spec);   // update path: spec reference unchanged, ops already mutated it
        Assert.Equal(1, changes);
    }

    [Fact]
    public void CompleteGeneration_with_final_spec_sets_spec_and_increments_round()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        var final = Spec();
        run.CompleteGeneration(final);

        Assert.Equal(AiState.Generated, run.State);
        Assert.Equal(1, run.Round);
        Assert.Same(final, run.Spec);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void CompleteGeneration_increments_round_across_multiple_generations()
    {
        var run = new DrylCanvasRun();
        run.CompleteGeneration(Spec());
        run.CompleteGeneration();
        Assert.Equal(2, run.Round);
    }

    // ---- FailGeneration ----

    [Fact]
    public void FailGeneration_sets_error_and_none_state_without_marking_completed()
    {
        var run = new DrylCanvasRun();
        var changes = 0;
        run.OnChange += () => changes++;

        run.FailGeneration(new InvalidOperationException("boom"));

        Assert.NotNull(run.Error);
        Assert.Equal("boom", run.Error!.Message);
        Assert.Equal(AiState.None, run.State);
        Assert.Equal(1, changes);

        // The run must stay alive — WaitForCompletionAsync must NOT have completed synchronously.
        Assert.False(run.WaitForCompletionAsync().IsCompleted);
    }

    [Fact]
    public void BeginGeneration_after_failure_clears_error()
    {
        var run = new DrylCanvasRun();
        run.FailGeneration(new InvalidOperationException("boom"));
        Assert.NotNull(run.Error);

        run.BeginGeneration();

        Assert.Null(run.Error);
    }

    // ---- Purge ----

    [Fact]
    public void Purge_removes_node_from_parent_children_and_raises()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        run.ApplyOp(new CanvasOp { Op = "remove", Id = "a" });
        Assert.True(run.Spec!.Root!.Children![0].Removing);

        var changes = 0;
        run.OnChange += () => changes++;

        run.Purge("a");

        Assert.Single(run.Spec!.Root!.Children!);
        Assert.Equal("grp", run.Spec.Root.Children![0].Id);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Purge_unknown_id_is_a_noop()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());
        var before = run.Spec!.Root!.Children!.Count;

        run.Purge("zzz");

        Assert.Equal(before, run.Spec.Root.Children!.Count);
    }

    [Fact]
    public void Purge_root_id_is_a_noop()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Spec());

        run.Purge("root");

        Assert.NotNull(run.Spec!.Root);
        Assert.Equal("root", run.Spec.Root!.Id);
    }
}
