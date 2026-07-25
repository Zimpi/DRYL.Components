using System.Text.Json;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Phase W1 — the change-pulse. A <c>setProps</c> is the one patch op with no motion of
/// its own (an insert enters, a move glides, a remove exits), so the run stamps the node
/// and <c>CanvasNodeView</c> re-keys a one-shot accent ring over it. These tests pin the
/// stamp semantics (monotonic, setProps-only, reset on a fresh artifact) and that the
/// overlay actually lands on the patched node and only there.
/// </summary>
public class CanvasChangePulseTests : BunitContext
{
    public CanvasChangePulseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private static CanvasOp SetProps(string id, string props) => new()
    {
        Op = "setProps", Id = id, Props = JsonSerializer.Deserialize<JsonElement>(props),
    };

    private static CanvasSpec TwoStats() => Parse("""
        {"root":{"id":"root","type":"stack","children":[
            {"id":"a","type":"stat","props":{"label":"A","value":"1"}},
            {"id":"b","type":"stat","props":{"label":"B","value":"2"}}]}}
        """);

    [Fact]
    public void SetProps_stamps_the_patched_node()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());

        Assert.Equal(0, run.Pulse.TickOf("a"));

        Assert.Null(run.ApplyOp(SetProps("a", """{ "value": "9" }""")));

        Assert.True(run.Pulse.TickOf("a") > 0);
        Assert.Equal(0, run.Pulse.TickOf("b"));   // untouched node stays unstamped
    }

    [Fact]
    public void Repeated_setProps_produce_distinct_stamps()
    {
        // Monotonic, not boolean: two consecutive patches of the same node must read as
        // two separate pulses, otherwise the second change is silent.
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());

        run.ApplyOp(SetProps("a", """{ "value": "9" }"""));
        var first = run.Pulse.TickOf("a");
        run.ApplyOp(SetProps("a", """{ "value": "10" }"""));

        Assert.True(run.Pulse.TickOf("a") > first);
    }

    [Fact]
    public void Insert_and_move_do_not_stamp()
    {
        // They already have a motion — DrylPresence enter and the FLIP glide. A pulse on
        // top would double the meaning of a single op.
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());

        Assert.Null(run.ApplyOp(new CanvasOp
        {
            Op = "insert", Parent = "root", Index = 0,
            Node = new CanvasNode { Id = "c", Type = "divider" },
        }));
        Assert.Null(run.ApplyOp(new CanvasOp { Op = "move", Id = "c", Parent = "root", Index = 2 }));

        Assert.Equal(0, run.Pulse.TickOf("c"));
    }

    [Fact]
    public void Failed_setProps_does_not_stamp()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());

        Assert.NotNull(run.ApplyOp(SetProps("nope", """{ "value": "9" }""")));

        Assert.Equal(0, run.Pulse.TickOf("nope"));
    }

    [Fact]
    public void BeginCreate_clears_stamps()
    {
        // A fresh artifact recycles ids; a stale stamp would pulse a node that is in
        // fact brand new (and therefore already entering).
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());
        run.ApplyOp(SetProps("a", """{ "value": "9" }"""));
        Assert.True(run.Pulse.TickOf("a") > 0);

        run.BeginCreate();

        Assert.Equal(0, run.Pulse.TickOf("a"));
    }

    [Fact]
    public void Patched_node_renders_the_pulse_overlay_and_others_do_not()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(TwoStats());
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".canvas-pulse"));

        cut.InvokeAsync(() => run.ApplyOp(SetProps("a", """{ "value": "9" }""")));

        cut.WaitForAssertion(() =>
        {
            var pulses = cut.FindAll("[data-cid='a'] > .canvas-pulse");
            Assert.Single(pulses);
            Assert.Equal("true", pulses[0].GetAttribute("aria-hidden"));
            Assert.Empty(cut.FindAll("[data-cid='b'] > .canvas-pulse"));
        });
    }
}
