using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The snapshot ring behind undo, redo and "back to version N".</summary>
public class CanvasHistoryTests
{
    private static CanvasSpec Spec(string title) =>
        new() { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };

    [Fact]
    public void Record_stores_an_entry_and_notifies()
    {
        var h = new CanvasHistory();
        var changes = 0;
        h.OnChange += () => changes++;

        Assert.True(h.Record(Spec("one"), "created"));

        Assert.Single(h.Entries);
        Assert.Equal("created", h.Entries[0].Label);
        Assert.Equal(0, h.Position);
        Assert.False(h.CanUndo);
        Assert.False(h.CanRedo);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Record_drops_a_snapshot_that_did_not_change_anything()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "created");
        var changes = 0;
        h.OnChange += () => changes++;

        Assert.False(h.Record(Spec("one"), "again"));

        Assert.Single(h.Entries);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Undo_walks_back_and_returns_a_fresh_instance()
    {
        var h = new CanvasHistory();
        var first = Spec("one");
        h.Record(first, "v1");
        h.Record(Spec("two"), "v2");

        var undone = h.Undo();

        Assert.NotNull(undone);
        Assert.Equal("one", undone!.Title);
        Assert.NotSame(first, undone);
        Assert.Equal(0, h.Position);
        Assert.False(h.CanUndo);
        Assert.True(h.CanRedo);
    }

    [Fact]
    public void Redo_walks_forward_again()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Undo();

        var redone = h.Redo();

        Assert.Equal("two", redone!.Title);
        Assert.Equal(1, h.Position);
        Assert.False(h.CanRedo);
    }

    [Fact]
    public void Undo_at_the_start_and_Redo_at_the_end_return_null()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");

        Assert.Null(h.Undo());
        Assert.Null(h.Redo());
        Assert.Equal(0, h.Position);
    }

    [Fact]
    public void Recording_after_an_undo_truncates_the_redo_branch()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");
        h.Undo();
        h.Undo();

        h.Record(Spec("other"), "v2b");

        Assert.Equal(2, h.Entries.Count);
        Assert.Equal("v1", h.Entries[0].Label);
        Assert.Equal("v2b", h.Entries[1].Label);
        Assert.Equal(1, h.Position);
        Assert.False(h.CanRedo);
    }

    [Fact]
    public void The_ring_drops_the_oldest_entry_when_it_overflows()
    {
        var h = new CanvasHistory(capacity: 3);
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");
        h.Record(Spec("four"), "v4");

        Assert.Equal(3, h.Entries.Count);
        Assert.Equal("v2", h.Entries[0].Label);
        Assert.Equal(2, h.Position);
    }

    [Fact]
    public void Capacity_is_clamped_to_a_sane_range()
    {
        Assert.Equal(2, new CanvasHistory(0).Capacity);
        Assert.Equal(200, new CanvasHistory(5000).Capacity);
    }

    [Fact]
    public void Restore_jumps_to_any_index_without_dropping_entries()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");
        h.Record(Spec("two"), "v2");
        h.Record(Spec("three"), "v3");

        var restored = h.Restore(0);

        Assert.Equal("one", restored!.Title);
        Assert.Equal(0, h.Position);
        Assert.Equal(3, h.Entries.Count);
        Assert.True(h.CanRedo);
        Assert.Null(h.Restore(9));
        Assert.Null(h.Restore(-1));
    }

    [Fact]
    public void An_empty_spec_is_a_legitimate_snapshot()
    {
        var h = new CanvasHistory();
        h.Record(null, "cleared");
        h.Record(Spec("one"), "v1");

        Assert.Null(h.Undo());   // "null" spec deserializes back to null
        Assert.Equal(0, h.Position);
    }

    [Fact]
    public void Clear_empties_the_ring()
    {
        var h = new CanvasHistory();
        h.Record(Spec("one"), "v1");

        h.Clear();

        Assert.Empty(h.Entries);
        Assert.Equal(-1, h.Position);
        Assert.False(h.CanUndo);
    }
}
