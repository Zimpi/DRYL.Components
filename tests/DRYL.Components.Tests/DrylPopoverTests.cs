using AngleSharp.Dom;
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Tests for <see cref="DrylPopover"/>'s exit lifecycle — the one part of this
/// component bUnit can actually see.
///
/// Most of what the popover does is out of reach here: the portal to
/// <c>&lt;body&gt;</c>, the placement, the outside-press dismissal and the
/// trigger's ARIA claim all live in <c>dryl.js</c>, which bUnit never executes,
/// and there is no real focus to move. The exit is different: the state that
/// keeps the surface on screen while it animates away is C#'s, it is visible in
/// the rendered markup, and its watchdog is what finishes the exit precisely
/// when no <c>animationend</c> arrives — which is always the case here.
///
/// JSInterop is Loose because opening wires dryl.popover and the exit wires
/// dryl.motion.
/// </summary>
public class DrylPopoverTests : BunitContext
{
    private const string PanelSelector = ".popover-panel";
    private const string TriggerSelector = ".popover-trigger";

    // The component's watchdog is 400ms; give it room without making the suite
    // wait on the wall clock any longer than it has to.
    private static readonly TimeSpan PastTheWatchdog = TimeSpan.FromSeconds(2);

    public DrylPopoverTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<DrylPopover> RenderPopover() =>
        Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, b => b.AddMarkupContent(0, "<button>open</button>"))
            .Add(p => p.PanelContent, b => b.AddMarkupContent(0, "<span class=\"body\">panel body</span>")));

    [Fact]
    public void A_closed_popover_that_was_never_opened_is_neither_open_nor_exiting()
    {
        var cut = RenderPopover();

        var panel = cut.Find(PanelSelector);

        Assert.DoesNotContain("is-open", panel.ClassList);
        Assert.DoesNotContain("is-exiting", panel.ClassList);
        Assert.Empty(cut.FindAll(".body"));
    }

    [Fact]
    public void Opening_shows_the_panel_and_marks_it_open()
    {
        var cut = RenderPopover();

        cut.Find(TriggerSelector).Click();

        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-open", panel.ClassList);
        Assert.DoesNotContain("is-exiting", panel.ClassList);
        Assert.Single(cut.FindAll(".body"));
    }

    [Fact]
    public void Closing_marks_the_panel_exiting_but_keeps_it_open()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        // Both visibility keys stay while the exit runs — this is the whole
        // point: a surface that has already been hidden cannot be seen leaving.
        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-open", panel.ClassList);
        Assert.Contains("is-exiting", panel.ClassList);
    }

    [Fact]
    public void The_panel_content_stays_mounted_while_the_exit_runs()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        Assert.Single(cut.FindAll(".body"));
    }

    [Fact]
    public void The_watchdog_finishes_an_exit_no_animationend_ever_ends()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        // Nothing here dispatches animationend, which is exactly the case the
        // watchdog exists for: without it the panel would stay mounted and
        // portalled forever as an invisible, full-size overlay.
        cut.WaitForAssertion(() =>
        {
            var panel = cut.Find(PanelSelector);
            Assert.DoesNotContain("is-exiting", panel.ClassList);
            Assert.DoesNotContain("is-open", panel.ClassList);
        }, PastTheWatchdog);
    }

    [Fact]
    public void The_watchdog_unmounts_the_panel_content_when_it_finishes()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".body")), PastTheWatchdog);
    }

    [Fact]
    public void Re_opening_during_an_exit_calls_the_exit_off()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-open", panel.ClassList);
        Assert.DoesNotContain("is-exiting", panel.ClassList);
        Assert.Single(cut.FindAll(".body"));
    }

    [Fact]
    public void A_popover_re_opened_during_an_exit_is_not_closed_by_the_cancelled_watchdog()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        // The watchdog of the abandoned exit must not fire behind the re-open
        // and close a popover the user has just re-opened.
        Thread.Sleep(PastTheWatchdog);
        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-open", panel.ClassList);
        Assert.Single(cut.FindAll(".body"));
    }

    [Fact]
    public void Escape_on_the_anchor_closes_a_popover_nobody_focused_into()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        // The everyday case: opened from the trigger, focus never moved, so the
        // panel's own handler can never see the key.
        cut.Find(".popover-anchor").KeyDown(Key.Escape);

        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-exiting", panel.ClassList);
    }

    [Fact]
    public void Escape_on_the_anchor_does_nothing_while_CloseOnEscape_is_false()
    {
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, b => b.AddMarkupContent(0, "<button>open</button>"))
            .Add(p => p.PanelContent, b => b.AddMarkupContent(0, "<span class=\"body\">panel body</span>"))
            .Add(p => p.CloseOnEscape, false));
        cut.Find(TriggerSelector).Click();

        cut.Find(".popover-anchor").KeyDown(Key.Escape);

        // Every library consumer that implements Escape itself passes false
        // here; the anchor must leave the key alone for them too.
        var panel = cut.Find(PanelSelector);
        Assert.Contains("is-open", panel.ClassList);
        Assert.DoesNotContain("is-exiting", panel.ClassList);
    }

    [Fact]
    public void Escape_on_the_anchor_does_nothing_while_the_popover_is_closed()
    {
        var cut = RenderPopover();

        cut.Find(".popover-anchor").KeyDown(Key.Escape);

        var panel = cut.Find(PanelSelector);
        Assert.DoesNotContain("is-open", panel.ClassList);
        Assert.DoesNotContain("is-exiting", panel.ClassList);
    }

    [Fact]
    public void The_anchor_key_handler_does_not_raise_OnKeyDown()
    {
        var seen = 0;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, b => b.AddMarkupContent(0, "<button>open</button>"))
            .Add(p => p.PanelContent, b => b.AddMarkupContent(0, "<span class=\"body\">panel body</span>"))
            .Add(p => p.OnKeyDown, _ => seen++));
        cut.Find(TriggerSelector).Click();

        cut.Find(".popover-anchor").KeyDown(Key.Escape);

        // OnKeyDown is documented as every keydown the PANEL receives. A
        // consumer's keyboard handling belongs to the panel, and handing them
        // trigger keys they never asked for would widen that contract silently.
        Assert.Equal(0, seen);
    }

    [Fact]
    public void OnClose_fires_when_the_exit_starts_not_when_it_finishes()
    {
        var closed = 0;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, b => b.AddMarkupContent(0, "<button>open</button>"))
            .Add(p => p.PanelContent, b => b.AddMarkupContent(0, "<span class=\"body\">panel body</span>"))
            .Add(p => p.OnClose, () => closed++));
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        // The animation is presentation; a consumer's close handler must not
        // wait 140ms for it.
        Assert.Equal(1, closed);
    }

    [Fact]
    public void The_exit_leaves_a_two_way_bound_Open_false_immediately()
    {
        var open = false;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, b => b.AddMarkupContent(0, "<button>open</button>"))
            .Add(p => p.PanelContent, b => b.AddMarkupContent(0, "<span class=\"body\">panel body</span>"))
            .Bind(p => p.Open, open, v => open = v));
        cut.Find(TriggerSelector).Click();

        cut.Find(TriggerSelector).Click();

        Assert.False(open);
    }
}
