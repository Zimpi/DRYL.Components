using Bunit;
using DRYL.Components;
using DRYL.Components.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylToastProvider"/>'s hand-off to <c>dryl.toast</c>
/// (idea I34): where a toast comes from, when it returns into the bell, and what lands in
/// the inbox. The motion itself is JS and is checked in the browser.
/// </summary>
public class DrylToastProviderTests : BunitContext
{
    private const string Attach = "dryl.toast.attach";
    private const string Leave = "dryl.toast.leave";

    private readonly IDrylToastService _toasts;
    private readonly IDrylNotificationService _inbox;

    public DrylToastProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
        _toasts = Services.GetRequiredService<IDrylToastService>();
        _inbox = Services.GetRequiredService<IDrylNotificationService>();
    }

    private IRenderedComponent<DrylToastProvider> RenderProvider(string enteredFrom = DrylToastProvider.FromBell, string leftAs = DrylToastProvider.LeftIntoInbox)
    {
        JSInterop.Setup<string?>(Attach, _ => true).SetResult(enteredFrom);
        JSInterop.Setup<string>(Leave, _ => true).SetResult(leftAs);
        return Render<DrylToastProvider>();
    }

    private void Expire(IRenderedComponent<DrylToastProvider> cut, int index = 0)
    {
        var callbacks = (DotNetObjectReference<DrylToastProvider.ToastCallbacks>)
            JSInterop.Invocations[Attach][index].Arguments[1]!;
        cut.InvokeAsync(() => callbacks.Value.OnExpired()).GetAwaiter().GetResult();
    }

    private bool LeftIntoInbox(int index = 0) => (bool)JSInterop.Invocations[Leave][index].Arguments[1]!;

    // ── The countdown bug: one key per toast at the loop's top level ─────────

    [Fact]
    public void Removing_the_first_toast_keeps_the_others_elements()
    {
        var cut = RenderProvider(leftAs: "plain");
        _toasts.Show("one");
        var two = _toasts.Show("two");
        _toasts.Show("three");
        cut.WaitForAssertion(() => Assert.Equal(3, JSInterop.Invocations[Attach].Count));
        var attachedAs = (ElementReference)JSInterop.Invocations[Attach][1].Arguments[0]!;

        Expire(cut, 0);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".toast-slot").Count));
        cut.InvokeAsync(two.Close);

        // A slot Blazor rebuilt would be captured again under a new element id — and its
        // progress bar would start over, which is the bug this pins.
        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations[Leave].Count));
        var leftAs = (ElementReference)JSInterop.Invocations[Leave][1].Arguments[0]!;
        Assert.Equal(attachedAs.Id, leftAs.Id);
    }

    // ── Entry ────────────────────────────────────────────────────────────────

    [Fact]
    public void Attach_receives_the_origin()
    {
        var cut = RenderProvider();
        var origin = new ElementReference("origin-1");
        _toasts.Show("Saved.", new ToastOptions { Origin = origin });

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));
        Assert.Equal(origin, JSInterop.Invocations[Attach][0].Arguments[2]);
    }

    [Fact]
    public void Attach_receives_no_origin_when_none_is_given()
    {
        var cut = RenderProvider();
        _toasts.Show("Synced.");

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));
        Assert.Null(JSInterop.Invocations[Attach][0].Arguments[2]);
    }

    [Fact]
    public void A_toast_is_rendered_hidden_until_dryl_toast_takes_it_over()
    {
        var cut = RenderProvider();
        _toasts.Show("Synced.");

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));
        Assert.False(cut.Find(".toast-slot").HasAttribute("data-dryl-entered"));
    }

    [Fact]
    public void A_failed_attach_reveals_the_toast_from_csharp()
    {
        JSInterop.Setup<string?>(Attach, _ => true).SetException(new JSException("no dryl.js"));
        var cut = Render<DrylToastProvider>();
        _toasts.Show("Synced.");

        cut.WaitForAssertion(() => Assert.True(cut.Find(".toast-slot").HasAttribute("data-dryl-entered")));
    }

    // ── Back into the bell ───────────────────────────────────────────────────

    [Fact]
    public void A_bell_toast_that_expires_returns_into_the_inbox()
    {
        var cut = RenderProvider();
        _toasts.ShowSuccess("2,481 rows written.", "Nightly export finished");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Single(_inbox.Notifications));
        Assert.True(LeftIntoInbox());
        var entry = _inbox.Notifications[0];
        Assert.Equal("Nightly export finished", entry.Title);
        Assert.Equal("2,481 rows written.", entry.Message);
        Assert.Equal("Check", entry.Icon);
        Assert.False(entry.Read);
    }

    [Fact]
    public void An_entry_without_a_title_takes_the_message_as_its_title()
    {
        var cut = RenderProvider();
        _toasts.ShowInfo("Synced with the server.");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Single(_inbox.Notifications));
        Assert.Equal("Synced with the server.", _inbox.Notifications[0].Title);
        Assert.Null(_inbox.Notifications[0].Message);
        Assert.Equal("Info", _inbox.Notifications[0].Icon);
    }

    [Fact]
    public void The_entry_keeps_the_toasts_ai_state()
    {
        var cut = RenderProvider();
        _toasts.Show("Report ready.", new ToastOptions { Ai = AiState.Generated });
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Single(_inbox.Notifications));
        Assert.Equal(AiState.Generated, _inbox.Notifications[0].Ai);
    }

    [Fact]
    public void A_toast_with_an_origin_never_returns_into_the_inbox()
    {
        var cut = RenderProvider(enteredFrom: DrylToastProvider.FromOrigin);
        _toasts.Show("Saved.", new ToastOptions { Origin = new ElementReference("save") });
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.False(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }

    [Fact]
    public void A_capsule_toast_never_returns_into_the_inbox()
    {
        var cut = RenderProvider(enteredFrom: DrylToastProvider.FromCapsule);
        _toasts.Show("Synced.");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.False(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }

    [Fact]
    public void A_closed_toast_never_returns_into_the_inbox()
    {
        var cut = RenderProvider();
        var toast = _toasts.Show("Synced.");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        cut.InvokeAsync(toast.Close);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.False(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }

    [Fact]
    public void KeepInInbox_false_keeps_an_expired_bell_toast_out_of_the_inbox()
    {
        var cut = RenderProvider();
        _toasts.Show("Welcome back.", new ToastOptions { KeepInInbox = false });
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.False(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }

    [Fact]
    public void No_entry_is_filed_when_no_bell_was_left_to_fly_into()
    {
        var cut = RenderProvider(leftAs: "plain");
        _toasts.Show("Synced.");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.True(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }

    [Fact]
    public void A_bell_toast_is_filed_when_the_leave_reply_never_comes()
    {
        JSInterop.Setup<string?>(Attach, _ => true).SetResult(DrylToastProvider.FromBell);
        JSInterop.Setup<string>(Leave, _ => true); // never answers
        var cut = Render<DrylToastProvider>();
        cut.Instance.LeaveWatchdogMs = 50;
        _toasts.ShowInfo("Synced with the server.");
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.Single(_inbox.Notifications);
    }

    [Fact]
    public void A_toast_without_text_never_returns_into_the_inbox()
    {
        var cut = RenderProvider();
        _toasts.Show<DrylBadge>();
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations[Attach]));

        Expire(cut);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-slot")));
        Assert.False(LeftIntoInbox());
        Assert.Empty(_inbox.Notifications);
    }
}
