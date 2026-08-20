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
    // The self-dismissing configuration wraps in DrylPresence, which wires dryl.motion.
    public DrylAlertTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylAlert> RenderAlert(
        bool dismissible = true, Action? onDismiss = null) =>
        Render<DrylAlert>(ps =>
        {
            ps.Add(p => p.Dismissible, dismissible).AddChildContent("All changes were applied.");
            if (onDismiss is not null) ps.Add(p => p.OnDismiss, onDismiss);
        });

    [Fact]
    public void Dismiss_without_a_handler_starts_removing_the_alert()
    {
        // The removal itself finishes when DrylPresence's exit animation ends, which is
        // driven by dryl.motion and therefore out of bUnit's reach — see DrylPresenceTests
        // for the same boundary. What is visible here is that the alert answered the press
        // by beginning to leave, which is exactly what it did not do before.
        var cut = RenderAlert();

        cut.Find(".alert-dismiss").Click();

        Assert.Contains("presence-exit", cut.Find(".presence").GetAttribute("class"));
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
    public void An_alert_with_a_handler_gains_no_wrapper_element()
    {
        // The DrylPresence wrapper exists only in the self-dismissing configuration,
        // so an alert that worked before does not change shape.
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
