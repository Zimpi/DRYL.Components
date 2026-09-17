using Bunit;
using DRYL.Components;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylPresence"/> — the AnimatePresence-style
/// wrapper that defers a child's unmount until its exit animation finishes.
/// JSInterop is Loose because the component wires dryl.motion.onExit on render.
/// </summary>
public class DrylPresenceTests : BunitContext
{
    public DrylPresenceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Hidden_by_default_renders_nothing()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, false)
            .AddChildContent("<span>body</span>"));

        Assert.Empty(cut.FindAll(".presence"));
    }

    [Fact]
    public void Visible_renders_child_without_enter_class_by_default()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .AddChildContent("<span>body</span>"));

        var el = cut.Find(".presence");
        Assert.Contains("body", el.InnerHtml);
        Assert.DoesNotContain("presence-enter", el.GetAttribute("class"));
    }

    [Fact]
    public void Appear_adds_enter_animation_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .Add(p => p.Appear, true)
            .AddChildContent("<span>body</span>"));

        Assert.Contains("presence-enter", cut.Find(".presence").GetAttribute("class"));
    }

    [Fact]
    public void Transition_maps_to_variant_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .Add(p => p.Transition, PresenceTransition.Scale)
            .AddChildContent("<span>body</span>"));

        Assert.Contains("presence--scale", cut.Find(".presence").GetAttribute("class"));
    }

    [Fact]
    public void Hiding_keeps_child_mounted_with_exit_class()
    {
        var cut = Render<DrylPresence>(ps => ps
            .Add(p => p.Visible, true)
            .AddChildContent("<span>body</span>"));

        cut.Render(ps => ps.Add(p => p.Visible, false));

        // Still mounted (exit animation playing), now flagged as exiting.
        var el = cut.Find(".presence");
        Assert.Contains("presence-exit", el.GetAttribute("class"));
    }

    [Fact]
    public async Task A_queued_callback_from_an_abandoned_exit_cannot_finish_the_next_exit()
    {
        var exited = 0;
        var cut = Render<DrylPresence>(ps => ps.Add(p => p.Visible, true)
            .Add(p => p.OnExited, () => exited++).AddChildContent("<span>body</span>"));
        cut.Render(ps => ps.Add(p => p.Visible, false));
        var oldCallback = ExitCallback();

        cut.Render(ps => ps.Add(p => p.Visible, true));
        cut.Render(ps => ps.Add(p => p.Visible, false));
        var currentCallback = ExitCallback();
        await cut.InvokeAsync(() => CompleteExit(oldCallback));

        Assert.Single(cut.FindAll(".presence-exit"));
        Assert.Equal(0, exited);
        Assert.NotSame(oldCallback, currentCallback);
        await cut.InvokeAsync(() => CompleteExit(currentCallback));
        await cut.InvokeAsync(() => CompleteExit(currentCallback));
        Assert.Empty(cut.FindAll(".presence"));
        Assert.Equal(1, exited);
    }

    [Fact]
    public async Task A_callback_already_queued_before_disposal_is_inert()
    {
        var exited = 0;
        var cut = Render<DrylPresence>(ps => ps.Add(p => p.Visible, true)
            .Add(p => p.OnExited, () => exited++).AddChildContent("body"));
        cut.Render(ps => ps.Add(p => p.Visible, false));
        var callback = ExitCallback();
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        var cleanupCount = JSInterop.Invocations["dryl.motion.clearExit"].Count;

        await cut.InvokeAsync(() => CompleteExit(callback));
        await cut.InvokeAsync(() => cut.Instance.OnExitFinished());
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Equal(0, exited);
        Assert.Equal(cleanupCount, JSInterop.Invocations["dryl.motion.clearExit"].Count);
    }

    [Fact]
    public async Task The_public_compatibility_callback_completes_the_current_exit_once()
    {
        var exited = 0;
        var cut = Render<DrylPresence>(ps => ps.Add(p => p.Visible, true)
            .Add(p => p.OnExited, () => exited++).AddChildContent("body"));
        cut.Render(ps => ps.Add(p => p.Visible, false));

        await cut.InvokeAsync(() => cut.Instance.OnExitFinished());
        await cut.InvokeAsync(() => cut.Instance.OnExitFinished());

        Assert.Empty(cut.FindAll(".presence"));
        Assert.Equal(1, exited);
    }

    [Fact]
    public void A_rejected_exit_registration_does_not_leave_the_child_mounted()
    {
        JSInterop.SetupVoid("dryl.motion.onExit", _ => true)
            .SetException(new JSException("The element disappeared."));
        var exited = 0;
        var cut = Render<DrylPresence>(ps => ps.Add(p => p.Visible, true)
            .Add(p => p.OnExited, () => exited++).AddChildContent("body"));

        cut.Render(ps => ps.Add(p => p.Visible, false));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".presence")));
        Assert.Equal(1, exited);
    }

    [Fact]
    public async Task Disposal_clears_an_exit_whose_registration_is_still_pending()
    {
        var registration = JSInterop.SetupVoid("dryl.motion.onExit", _ => true);
        var exited = 0;
        var cut = Render<DrylPresence>(ps => ps.Add(p => p.Visible, true)
            .Add(p => p.OnExited, () => exited++).AddChildContent("body"));
        cut.Render(ps => ps.Add(p => p.Visible, false));
        var callback = ExitCallback();

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());

        Assert.Single(JSInterop.Invocations["dryl.motion.clearExit"]);
        registration.SetVoidResult();
        await cut.InvokeAsync(() => CompleteExit(callback));
        Assert.Equal(0, exited);
    }

    private object ExitCallback()
    {
        // Keep the callback value itself, just as an invocation queued before
        // DotNetObjectReference disposal already holds its callback target.
        var reference = JSInterop.Invocations["dryl.motion.onExit"].Last().Arguments[1]!;
        return reference.GetType().GetProperty("Value")!.GetValue(reference)!;
    }

    private static Task CompleteExit(object callback) =>
        (Task)callback.GetType().GetMethod("OnExitFinished")!.Invoke(callback, null)!;
}
