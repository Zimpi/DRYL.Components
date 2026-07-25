using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Undo/redo as the workspace exposes it: always about the active view.</summary>
public class CanvasWorkspaceHistoryTests
{
    private static CanvasSpec Spec(string title) =>
        new() { Title = title, Root = new CanvasNode { Id = "r", Type = "stack" } };

    [Fact]
    public void Commit_records_the_active_view_and_notifies()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Commit("created"));

        Assert.Single(view.History.Entries);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Commit_without_an_active_view_does_nothing()
    {
        var ws = new CanvasWorkspace();

        Assert.False(ws.Commit("created"));
        Assert.False(ws.CanUndo);
        Assert.False(ws.Undo());
    }

    [Fact]
    public void Commit_of_an_unchanged_spec_is_dropped()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");

        Assert.False(ws.Commit("v1 again"));
        Assert.Single(view.History.Entries);
    }

    [Fact]
    public void Undo_puts_the_previous_spec_back_on_the_view()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");
        view.Spec = Spec("two");
        ws.Commit("v2");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Undo());

        Assert.Equal("one", view.Spec!.Title);
        Assert.Equal(1, changes);
        Assert.True(ws.CanRedo);

        Assert.True(ws.Redo());
        Assert.Equal("two", view.Spec!.Title);
    }

    [Fact]
    public void RestoreVersion_jumps_to_a_named_entry()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Overview");
        view.Spec = Spec("one");
        ws.Commit("v1");
        view.Spec = Spec("two");
        ws.Commit("v2");
        view.Spec = Spec("three");
        ws.Commit("v3");

        Assert.True(ws.RestoreVersion(0));
        Assert.Equal("one", view.Spec!.Title);
        Assert.False(ws.RestoreVersion(42));
    }

    [Fact]
    public void Each_view_keeps_its_own_history()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        a.Spec = Spec("a1");
        ws.Commit("a1");
        var b = ws.Open("B");          // Open activates B
        b.Spec = Spec("b1");
        ws.Commit("b1");

        Assert.Single(a.History.Entries);
        Assert.Single(b.History.Entries);
        Assert.False(ws.CanUndo);      // B has one entry only

        ws.Activate(a.Id);
        Assert.False(ws.CanUndo);
    }
}
