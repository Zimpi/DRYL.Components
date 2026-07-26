using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasSelectionTests
{
    private static CanvasNode Chart() => new()
    {
        Id = "c1",
        Type = "lineChart",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "title": "Revenue" }"""),
    };

    [Fact]
    public void Select_records_id_type_label_and_lock()
    {
        var sel = new CanvasSelection();
        var node = Chart();
        node.Locked = true;

        sel.Select(node);

        Assert.True(sel.HasSelection);
        Assert.Equal("c1", sel.Id);
        Assert.Equal("lineChart", sel.Type);
        Assert.Equal("Revenue", sel.Label);
        Assert.True(sel.Locked);
    }

    [Fact]
    public void Select_raises_change_once_and_is_idempotent()
    {
        var sel = new CanvasSelection();
        var changes = 0;
        sel.OnChange += () => changes++;

        sel.Select(Chart());
        sel.Select(Chart());

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Select_raises_change_when_the_lock_flipped_on_the_same_node()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart());
        var changes = 0;
        sel.OnChange += () => changes++;

        var pinned = Chart();
        pinned.Locked = true;
        sel.Select(pinned);

        Assert.Equal(1, changes);
        Assert.True(sel.Locked);
    }

    [Fact]
    public void Select_with_focus_bumps_the_focus_tick_every_time()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart(), focus: true);
        var first = sel.FocusTick;
        sel.Select(Chart(), focus: true);

        Assert.True(sel.FocusTick > first);
    }

    [Fact]
    public void Clear_resets_everything_and_raises_once()
    {
        var sel = new CanvasSelection();
        sel.Select(Chart());
        var changes = 0;
        sel.OnChange += () => changes++;

        sel.Clear();
        sel.Clear();

        Assert.Equal(1, changes);
        Assert.False(sel.HasSelection);
        Assert.Null(sel.Id);
        Assert.Null(sel.Type);
        Assert.Null(sel.Label);
        Assert.False(sel.Locked);
    }

    [Fact]
    public void RovingId_is_the_selection_and_falls_back_to_the_registered_id()
    {
        var sel = new CanvasSelection();
        sel.SetFallback("first");
        Assert.Equal("first", sel.RovingId);

        sel.Select(Chart());
        Assert.Equal("c1", sel.RovingId);

        sel.Clear();
        Assert.Equal("first", sel.RovingId);
    }

    [Fact]
    public void RequestPrompt_raises_its_own_event()
    {
        var sel = new CanvasSelection();
        var asked = 0;
        sel.OnPromptRequested += () => asked++;

        sel.RequestPrompt();

        Assert.Equal(1, asked);
    }
}
