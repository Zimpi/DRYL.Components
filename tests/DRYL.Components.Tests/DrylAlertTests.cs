using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylAlert"/>, centred on the question the
/// component answers per configuration: who owns the alert's lifetime when its
/// dismiss button is pressed.
/// </summary>
public class DrylAlertTests : BunitContext
{
    // Dismissing plays the alert's own exit through dryl.motion.collapse.
    public DrylAlertTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylAlert> RenderAlert(
        bool dismissible = true, Action? onDismiss = null) =>
        Render<DrylAlert>(ps =>
        {
            ps.Add(p => p.Dismissible, dismissible).AddChildContent("All changes were applied.");
            if (onDismiss is not null) ps.Add(p => p.OnDismiss, onDismiss);
        });

    [Fact]
    public void Dismiss_without_a_handler_removes_the_alert_after_its_exit()
    {
        // In the browser the exit is a real animation; bUnit's loose interop completes
        // the call at once, so what is visible here is the order: exit, then removal.
        var cut = RenderAlert();

        cut.Find(".alert-dismiss").Click();

        Assert.Single(JSInterop.Invocations, i => i.Identifier == "dryl.motion.collapse");
        Assert.Empty(cut.FindAll(".alert"));
    }

    [Fact]
    public void Dismiss_with_a_handler_plays_the_exit_before_raising_it()
    {
        var collapsedFirst = false;
        var cut = RenderAlert(onDismiss: () =>
            collapsedFirst = JSInterop.Invocations.Any(i => i.Identifier == "dryl.motion.collapse"));

        cut.Find(".alert-dismiss").Click();

        Assert.True(collapsedFirst);
    }

    [Fact]
    public void A_host_that_keeps_the_alert_gets_it_back()
    {
        // The handler ran but the host did not unmount: the exit plays in reverse.
        var cut = RenderAlert(onDismiss: () => { });

        cut.Find(".alert-dismiss").Click();

        cut.WaitForAssertion(() =>
            Assert.Single(JSInterop.Invocations, i => i.Identifier == "dryl.motion.restore"));
    }

    [Fact]
    public void Dismiss_with_a_handler_raises_it()
    {
        var raised = false;
        var cut = RenderAlert(onDismiss: () => raised = true);

        cut.Find(".alert-dismiss").Click();

        Assert.True(raised);
    }

    [Fact]
    public void Dismiss_with_a_handler_leaves_the_alert_mounted()
    {
        // The host owns the lifetime here: dismissing is a request, not a removal.
        var cut = RenderAlert(onDismiss: () => { });

        cut.Find(".alert-dismiss").Click();

        Assert.Single(cut.FindAll(".alert"));
    }

    [Fact]
    public void A_self_dismissing_alert_gains_no_wrapper_element()
    {
        // The exit is played on the alert itself, so no configuration adds an
        // element around it.
        var cut = RenderAlert();

        Assert.DoesNotContain("presence", cut.Markup);
        Assert.StartsWith("alert", cut.Find("*").GetAttribute("class"));
    }

    [Fact]
    public void An_alert_with_a_handler_gains_no_wrapper_element()
    {
        var cut = RenderAlert(onDismiss: () => { });

        Assert.DoesNotContain("presence", cut.Markup);
    }

    [Fact]
    public void A_non_dismissible_alert_gains_no_wrapper_element()
    {
        var cut = RenderAlert(dismissible: false);

        Assert.DoesNotContain("presence", cut.Markup);
        Assert.Empty(cut.FindAll(".alert-dismiss"));
    }

    [Fact]
    public void Turning_dismissible_off_brings_a_self_dismissed_alert_back()
    {
        var cut = RenderAlert();
        cut.Find(".alert-dismiss").Click();

        cut.Render(ps => ps.Add(p => p.Dismissible, false));

        Assert.Single(cut.FindAll(".alert"));
        Assert.DoesNotContain("presence", cut.Markup);
    }

    [Fact]
    public void Danger_is_announced_assertively()
    {
        var cut = Render<DrylAlert>(ps => ps
            .Add(p => p.Kind, DrylAlert.AlertKind.Danger)
            .AddChildContent("The upload failed."));

        var root = cut.Find(".alert");
        Assert.Equal("alert", root.GetAttribute("role"));
        Assert.Equal("assertive", root.GetAttribute("aria-live"));
    }

    [Fact]
    public void Info_is_announced_politely()
    {
        var cut = Render<DrylAlert>(ps => ps.AddChildContent("Two rows were skipped."));

        var root = cut.Find(".alert");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("polite", root.GetAttribute("aria-live"));
    }

    [Fact]
    public void An_empty_icon_suppresses_the_icon_chip()
    {
        var cut = Render<DrylAlert>(ps => ps
            .Add(p => p.Icon, string.Empty)
            .AddChildContent("No icon here."));

        Assert.Empty(cut.FindAll(".alert .ico"));
    }
}
