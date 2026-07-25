using Bunit;
using DRYL.Components;
using Xunit;

namespace DRYL.Components.Tests;

/// <summary>
/// Phase W4 — <see cref="DrylStat"/>'s opt-in count-up. The tween lives entirely in
/// <c>dryl.motion.countUp</c>: it rewrites the value span's text between renders and always
/// lands on the exact string Blazor rendered, so the markup is identical with and without
/// it. These tests pin the interop contract (opt-in, one call per value change) and that
/// the rendered value never depends on JS having run.
/// </summary>
public class DrylStatCountUpTests : BunitContext
{
    public DrylStatCountUpTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void CountUp_off_by_default_makes_no_interop_call()
    {
        Render<DrylStat>(p => p.Add(x => x.Label, "Revenue").Add(x => x.Value, "12,480"));

        Assert.Empty(JSInterop.Invocations["dryl.motion.countUp"]);
    }

    [Fact]
    public void CountUp_tweens_the_value_on_first_render()
    {
        var cut = Render<DrylStat>(p => p
            .Add(x => x.Label, "Revenue")
            .Add(x => x.Value, "12,480")
            .Add(x => x.CountUp, true));

        var call = Assert.Single(JSInterop.Invocations["dryl.motion.countUp"]);
        Assert.Equal("12,480", call.Arguments[1]);
        // The real value is in the DOM regardless — JS only animates the way there.
        Assert.Equal("12,480", cut.Find(".stat-value").TextContent);
    }

    [Fact]
    public void CountUp_tweens_again_only_when_the_value_actually_changes()
    {
        var cut = Render<DrylStat>(p => p
            .Add(x => x.Label, "Revenue")
            .Add(x => x.Value, "10")
            .Add(x => x.CountUp, true));
        Assert.Single(JSInterop.Invocations["dryl.motion.countUp"]);

        // An unrelated re-render must not restart the tween…
        cut.Render(p => p
            .Add(x => x.Label, "Net revenue")
            .Add(x => x.Value, "10")
            .Add(x => x.CountUp, true));
        Assert.Single(JSInterop.Invocations["dryl.motion.countUp"]);

        // …but a new value must.
        cut.Render(p => p
            .Add(x => x.Label, "Net revenue")
            .Add(x => x.Value, "42")
            .Add(x => x.CountUp, true));

        var calls = JSInterop.Invocations["dryl.motion.countUp"];
        Assert.Equal(2, calls.Count);
        Assert.Equal("42", calls[^1].Arguments[1]);
        Assert.Equal("42", cut.Find(".stat-value").TextContent);
    }
}
