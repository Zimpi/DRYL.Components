using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>The workspace state behind A5: named views, exactly one active.</summary>
public class CanvasWorkspaceTests
{
    [Fact]
    public void Open_creates_activates_and_notifies()
    {
        var ws = new CanvasWorkspace();
        var changes = 0;
        ws.OnChange += () => changes++;

        var view = ws.Open("Auftrag 4711");

        Assert.Equal("auftrag-4711", view.Id);
        Assert.Equal("Auftrag 4711", view.Title);
        Assert.Same(view, ws.Active);
        Assert.Single(ws.Views);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Open_with_a_known_title_activates_the_existing_view()
    {
        var ws = new CanvasWorkspace();
        var first = ws.Open("Overview");
        first.Spec = new CanvasSpec { Title = "Overview" };
        ws.Open("Auftrag 4711");

        var again = ws.Open("Overview");

        Assert.Same(first, again);
        Assert.Equal(2, ws.Views.Count);
        Assert.Same(first.Spec, ws.Active!.Spec);   // the spec survives re-opening
    }

    [Fact]
    public void Titles_that_share_a_slug_are_the_same_view()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Order 42");
        var b = ws.Open("Order/42");

        Assert.Equal("order-42", a.Id);
        Assert.Same(a, b);          // the slug IS the identity — "order 42" is one view
        Assert.Single(ws.Views);
    }

    [Fact]
    public void An_empty_title_still_yields_an_id()
    {
        var ws = new CanvasWorkspace();
        Assert.Equal("view", ws.Open("   ").Id);
    }

    [Fact]
    public void Activate_switches_and_notifies_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");
        var changes = 0;
        ws.OnChange += () => changes++;

        Assert.True(ws.Activate(a.Id));
        Assert.False(ws.Activate(a.Id));            // already active — no second event
        Assert.False(ws.Activate("nope"));
        Assert.Same(a, ws.Active);
        Assert.Equal(1, changes);
        Assert.NotSame(b, ws.Active);
    }

    [Fact]
    public void Close_flags_the_view_and_hands_the_active_slot_to_a_neighbour()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");   // active

        ws.Close(b.Id);

        Assert.True(b.Removing);
        Assert.Equal(2, ws.Views.Count);            // still there — the chip is animating out
        Assert.Same(a, ws.Active);                  // the body already shows the neighbour
    }

    [Fact]
    public void Closing_twice_notifies_once()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Close(a.Id);
        ws.Close(a.Id);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Remove_drops_the_view_and_the_last_one_leaves_nothing_active()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("A");
        var b = ws.Open("B");

        ws.Remove(b.Id);
        Assert.Same(a, ws.Active);
        Assert.Single(ws.Views);

        ws.Remove(a.Id);
        Assert.Null(ws.Active);
        Assert.Null(ws.ActiveId);
        Assert.Empty(ws.Views);
    }

    [Fact]
    public void Remove_of_an_unknown_id_is_a_no_op()
    {
        var ws = new CanvasWorkspace();
        ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Remove("nope");

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Clear_empties_the_workspace_once()
    {
        var ws = new CanvasWorkspace();
        ws.Open("A");
        var changes = 0;
        ws.OnChange += () => changes++;

        ws.Clear();
        ws.Clear();

        Assert.Empty(ws.Views);
        Assert.Null(ws.ActiveId);
        Assert.Equal(1, changes);
    }
}
