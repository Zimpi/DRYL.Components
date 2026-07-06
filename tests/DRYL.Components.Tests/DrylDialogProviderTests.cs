using Bunit;
using DRYL.Components;
using DRYL.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylDialogProvider"/> — the service-driven
/// dialog host. Focus: the multi-dialog lifecycle (shared backdrop, handoff
/// choreography when dialogs are opened in quick succession, e.g. by an AI
/// agent) and the exit watchdog that guarantees a closing dialog can never get
/// stuck as an invisible, click-eating overlay when its animationend is lost.
/// JSInterop is Loose because the provider wires dryl.modal / dryl.motion.
/// </summary>
public class DrylDialogProviderTests : BunitContext
{
    private readonly IDrylDialogService _service;

    public DrylDialogProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
        _service = Services.GetRequiredService<IDrylDialogService>();
    }

    /// <summary>Minimal dialog body used as the DynamicComponent target.</summary>
    private sealed class TestDialog : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddContent(0, "test-dialog-body");
    }

    [Fact]
    public void No_dialogs_renders_nothing()
    {
        var cut = Render<DrylDialogProvider>();
        Assert.Empty(cut.FindAll(".dialog-backdrop"));
    }

    [Fact]
    public async Task Show_renders_backdrop_with_one_layer()
    {
        var cut = Render<DrylDialogProvider>();
        await _service.ShowAsync<TestDialog>("First");

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".dialog-backdrop"));
            Assert.Single(cut.FindAll(".dialog-layer"));
            Assert.Contains("test-dialog-body", cut.Markup);
        });
    }

    [Fact]
    public async Task Stacked_dialogs_share_a_single_backdrop()
    {
        var cut = Render<DrylDialogProvider>();
        await _service.ShowAsync<TestDialog>("First");
        await _service.ShowAsync<TestDialog>("Second");

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".dialog-backdrop"));
            Assert.Equal(2, cut.FindAll(".dialog-layer").Count);
        });
    }

    [Fact]
    public async Task Closing_last_dialog_marks_layer_and_backdrop_exiting()
    {
        var cut = Render<DrylDialogProvider>();
        var dialog = await _service.ShowAsync<TestDialog>("First");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".dialog-layer")));

        dialog.Cancel();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("is-exiting", cut.Find(".dialog-layer").GetAttribute("class"));
            Assert.Contains("is-exiting", cut.Find(".dialog-backdrop").GetAttribute("class"));
        });
    }

    [Fact]
    public async Task Dialog_opened_during_exit_keeps_backdrop_alive_and_gets_handoff()
    {
        var cut = Render<DrylDialogProvider>();
        var first = await _service.ShowAsync<TestDialog>("First");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".dialog-layer")));

        // Agent pattern: close the current dialog and immediately open the next.
        first.Cancel();
        await _service.ShowAsync<TestDialog>("Second");

        cut.WaitForAssertion(() =>
        {
            var layers = cut.FindAll(".dialog-layer");
            Assert.Equal(2, layers.Count);
            Assert.Contains("is-exiting", layers[0].GetAttribute("class"));
            Assert.Contains("is-handoff", layers[1].GetAttribute("class"));
            // A live dialog exists → the shared backdrop must not fade out.
            Assert.DoesNotContain("is-exiting", cut.Find(".dialog-backdrop").GetAttribute("class"));
        });
    }

    [Fact]
    public async Task Lost_exit_animation_is_finalized_by_watchdog()
    {
        // JSInterop never invokes OnExitFinished here — exactly the failure mode
        // where animationend is missed. The watchdog must still remove the entry
        // so no invisible backdrop keeps blocking the page.
        var cut = Render<DrylDialogProvider>();
        var dialog = await _service.ShowAsync<TestDialog>("First");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".dialog-layer")));

        dialog.Cancel();

        cut.WaitForAssertion(
            () => Assert.Empty(cut.FindAll(".dialog-backdrop")),
            timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Rapid_sequential_dialogs_leave_no_orphaned_layers()
    {
        var cut = Render<DrylDialogProvider>();

        // Three chained tool-call dialogs, each closed right before the next opens.
        for (var i = 0; i < 3; i++)
        {
            var reference = await _service.ShowAsync<TestDialog>($"Dialog {i}");
            reference.Cancel();
        }

        cut.WaitForAssertion(
            () => Assert.Empty(cut.FindAll(".dialog-layer")),
            timeout: TimeSpan.FromSeconds(3));
    }
}
