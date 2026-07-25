using System.Threading.Tasks;
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// Tests for the Task 9 FLIP wiring on <see cref="DrylAiCanvas"/> — the canvas
/// body element is handed to <c>dryl.motion.autoFlip</c> so artifact reflows
/// (nodes appearing/disappearing/reordering) glide instead of snapping, and the
/// observer is torn down via <c>dryl.motion.stopAutoFlip</c> on dispose.
/// JSInterop is Loose because the canvas also renders other JS-interop-aware
/// components (DrylPresence, the AI aura).
/// </summary>
public class DrylAiCanvasFlipTests : BunitContext
{
    public DrylAiCanvasFlipTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();   // the canvas injects IDrylViewTransition
    }

    [Fact]
    public void AutoFlip_is_invoked_on_canvas_body_after_first_render()
    {
        var run = new DrylCanvasRun();

        Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.NotEmpty(JSInterop.Invocations["dryl.motion.autoFlip"]);
    }

    [Fact]
    public async Task StopAutoFlip_is_invoked_on_dispose()
    {
        var run = new DrylCanvasRun();
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        // The JS teardown lives on the inner core renderer now (A1) — DrylAiCanvas only
        // owns the run subscription and the aura.
        await cut.InvokeAsync(() => cut.FindComponent<DrylCanvas>().Instance.DisposeAsync().AsTask());

        Assert.NotEmpty(JSInterop.Invocations["dryl.motion.stopAutoFlip"]);
    }
}
