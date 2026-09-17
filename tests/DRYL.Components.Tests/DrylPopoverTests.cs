using AngleSharp.Dom;
using Bunit;
using DRYL.Components;
using System.Reflection;
using Microsoft.JSInterop;

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

    [Fact]
    public async Task An_old_browser_callback_cannot_finish_the_next_close()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["dryl.motion.onExit"]));
        var oldCallback = ExitCallback();
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations["dryl.motion.onExit"].Count));
        var currentCallback = ExitCallback();

        await cut.InvokeAsync(() => CompleteExit(oldCallback));

        Assert.Contains("is-exiting", cut.Find(PanelSelector).ClassList);
        Assert.Single(cut.FindAll(".body"));
        Assert.NotSame(oldCallback, currentCallback);
        await cut.InvokeAsync(() => CompleteExit(currentCallback));
        await cut.InvokeAsync(() => CompleteExit(currentCallback));
        Assert.Empty(cut.FindAll(".body"));
        Assert.Single(JSInterop.Invocations["dryl.popover.close"]);
    }

    [Fact]
    public async Task Bound_Open_changes_use_the_same_exit_and_reopen_ownership()
    {
        var cut = Render<DrylPopover>(ps => ps.Add(p => p.Open, true)
            .Add(p => p.PanelContent, "<span class=\"body\">body</span>"));

        cut.Render(ps => ps.Add(p => p.Open, false));

        Assert.Contains("is-exiting", cut.Find(PanelSelector).ClassList);
        Assert.Single(cut.FindAll(".body"));
        var oldCallback = ExitCallback();
        cut.Render(ps => ps.Add(p => p.Open, true));
        Assert.DoesNotContain("is-exiting", cut.Find(PanelSelector).ClassList);
        cut.Render(ps => ps.Add(p => p.Open, false));
        var currentCallback = ExitCallback();

        await cut.InvokeAsync(() => CompleteExit(oldCallback));
        Assert.Contains("is-exiting", cut.Find(PanelSelector).ClassList);
        Assert.Single(cut.FindAll(".body"));
        await cut.InvokeAsync(() => CompleteExit(currentCallback));
        Assert.Empty(cut.FindAll(".body"));
    }

    [Fact]
    public async Task A_watchdog_already_queued_before_cancellation_cannot_finish_the_next_close()
    {
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();
        using var cancellation = new CancellationTokenSource();
        var queue = new QueuedContinuationContext();

        // Control only scheduling: park the real watchdog's post-delay
        // continuation, then deliver it after a later close has begun.
        var watchdog = await Task.Factory.StartNew(() =>
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(queue);
            try
            {
                return (Task)typeof(DrylPopover).GetMethod("RunExitWatchdogAsync",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(cut.Instance, [cancellation.Token])!;
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        var resume = await queue.Continuation.Task.WaitAsync(PastTheWatchdog);

        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();
        cancellation.Cancel();
        await cut.InvokeAsync(resume);
        await watchdog;

        Assert.Contains("is-exiting", cut.Find(PanelSelector).ClassList);
        Assert.Single(cut.FindAll(".body"));
        await cut.InvokeAsync(() => CompleteExit(ExitCallback()));
        Assert.Empty(cut.FindAll(".body"));
    }

    [Fact]
    public async Task Disposal_makes_pending_exit_and_outside_dismissal_callbacks_inert()
    {
        var closed = 0;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, "<button>open</button>")
            .Add(p => p.PanelContent, "<span class=\"body\">body</span>")
            .Add(p => p.OnClose, () => closed++));
        cut.Find(TriggerSelector).Click();
        cut.Find(TriggerSelector).Click();
        var oldCallback = ExitCallback();
        cut.Find(TriggerSelector).Click();
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        var cleanupCount = JSInterop.Invocations["dryl.popover.close"].Count;

        await cut.InvokeAsync(() => CompleteExit(oldCallback));
        await cut.InvokeAsync(() => cut.Instance.Close());
        await cut.InvokeAsync(() => cut.Instance.SetOpenAsync(false));
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Equal(1, closed);
        Assert.Equal(cleanupCount, JSInterop.Invocations["dryl.popover.close"].Count);
    }

    [Fact]
    public async Task Rejected_exit_cleanup_still_releases_the_portal()
    {
        JSInterop.SetupVoid("dryl.motion.clearExit", _ => true)
            .SetException(new JSException("The exit element disappeared."));
        var cut = RenderPopover();
        cut.Find(TriggerSelector).Click();

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Single(JSInterop.Invocations["dryl.popover.close"]);
    }

    [Fact]
    public async Task Disposal_during_focus_release_does_not_resume_the_close_request()
    {
        var release = JSInterop.SetupVoid("dryl.popover.releaseFocus", _ => true);
        var closed = 0;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, "<button>open</button>")
            .Add(p => p.PanelContent, "body")
            .Add(p => p.OnClose, () => closed++));
        cut.Find(TriggerSelector).Click();
        var closing = cut.InvokeAsync(() => cut.Instance.SetOpenAsync(false));
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["dryl.popover.releaseFocus"]));

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        release.SetVoidResult();
        await closing;

        Assert.Equal(0, closed);
        Assert.Empty(JSInterop.Invocations["dryl.motion.onExit"]);
    }

    [Fact]
    public async Task Exit_completion_during_focus_release_does_not_swallow_OnClose()
    {
        var release = JSInterop.SetupVoid("dryl.popover.releaseFocus", _ => true);
        var closed = 0;
        var cut = Render<DrylPopover>(ps => ps
            .Add(p => p.TriggerContent, "<button>open</button>")
            .Add(p => p.PanelContent, "<span class=\"body\">body</span>")
            .Add(p => p.OnClose, () => closed++));
        cut.Find(TriggerSelector).Click();
        var closing = cut.InvokeAsync(() => cut.Instance.SetOpenAsync(false));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".body")), PastTheWatchdog);
        release.SetVoidResult();
        await closing;

        Assert.Equal(1, closed);
    }

    private object ExitCallback()
    {
        var reference = JSInterop.Invocations["dryl.motion.onExit"].Last().Arguments[1]!;
        return reference.GetType().GetProperty("Value")!.GetValue(reference)!;
    }

    private static Task CompleteExit(object callback) =>
        (Task)callback.GetType().GetMethod("OnExitFinished")!.Invoke(callback, null)!;

    private sealed class QueuedContinuationContext : SynchronizationContext
    {
        public TaskCompletionSource<Action> Continuation { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Post(SendOrPostCallback callback, object? state) =>
            Continuation.TrySetResult(() => callback(state));
    }
}
