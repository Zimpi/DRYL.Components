using Bunit;
using DRYL.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylNotifications"/>, pinning the two things the
/// component's contract turns on: which of its two modes owns the state, and that the
/// unread state is carried by text rather than by a coloured dot alone.
/// </summary>
public class DrylNotificationsTests : BunitContext
{
    // The panel is a DrylPopover, and opening it wires dryl.popover.
    public DrylNotificationsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static DrylNotification Unread(string title = "Report ready") =>
        new() { Title = title, Timestamp = DateTimeOffset.Now };

    private IRenderedComponent<DrylNotifications> RenderControlled(
        IReadOnlyList<DrylNotification> items,
        Action<DrylNotification>? onMarkRead = null) =>
        Render<DrylNotifications>(ps =>
        {
            ps.Add(p => p.Items, items);
            if (onMarkRead is not null) ps.Add(p => p.OnMarkRead, onMarkRead);
        });

    private static void OpenPanel(IRenderedComponent<DrylNotifications> cut) =>
        cut.Find(".notif-bell").Click();

    // ── Controlled mode owns its own state ──────────────────────────────────

    [Fact]
    public void Controlled_click_does_not_write_to_the_callers_object()
    {
        var item = Unread();
        var cut = RenderControlled([item]);
        OpenPanel(cut);

        cut.Find(".notif-item-main").Click();

        Assert.False(item.Read);
    }

    [Fact]
    public void Controlled_click_raises_mark_read_with_the_row()
    {
        var item = Unread();
        DrylNotification? seen = null;
        var cut = RenderControlled([item], n => seen = n);
        OpenPanel(cut);

        cut.Find(".notif-item-main").Click();

        Assert.Same(item, seen);
    }

    [Fact]
    public void Controlled_click_on_a_read_row_raises_no_mark_read()
    {
        var item = new DrylNotification { Title = "Build passed", Read = true };
        var raised = false;
        var cut = RenderControlled([item], _ => raised = true);
        OpenPanel(cut);

        cut.Find(".notif-item-main").Click();

        Assert.False(raised);
    }

    // ── Service-driven mode: the service is the state ───────────────────────

    [Fact]
    public void Service_driven_click_marks_the_entry_read_in_the_service()
    {
        Services.AddScoped<IDrylNotificationService, TestNotificationService>();
        var service = Services.GetRequiredService<IDrylNotificationService>();
        var item = service.Add(Unread());

        var cut = Render<DrylNotifications>();
        OpenPanel(cut);
        cut.Find(".notif-item-main").Click();

        Assert.True(item.Read);
        Assert.Equal(0, service.UnreadCount);
    }

    [Fact]
    public void Renders_an_empty_inbox_when_no_service_is_registered()
    {
        var cut = Render<DrylNotifications>();
        OpenPanel(cut);

        Assert.Empty(cut.FindAll(".notif-item"));
        Assert.Contains("All caught up", cut.Markup);
    }

    // ── The unread state is announced, not only coloured ────────────────────

    [Fact]
    public void Unread_row_carries_the_state_as_text_inside_its_button()
    {
        var cut = RenderControlled([Unread()]);
        OpenPanel(cut);

        var row = cut.Find(".notif-item-main");
        Assert.Contains("Unread", row.TextContent);
    }

    [Fact]
    public void Unread_dot_is_decorative()
    {
        var cut = RenderControlled([Unread()]);
        OpenPanel(cut);

        Assert.Equal("true", cut.Find(".notif-item-dot").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Read_row_carries_no_unread_text()
    {
        var cut = RenderControlled([new DrylNotification { Title = "Build passed", Read = true }]);
        OpenPanel(cut);

        Assert.DoesNotContain("Unread", cut.Find(".notif-item-main").TextContent);
    }

    /// <summary>
    /// The library's own implementation is internal, so the test drives the interface
    /// through a minimal stand-in with the same semantics.
    /// </summary>
    private sealed class TestNotificationService : IDrylNotificationService
    {
        private readonly List<DrylNotification> _items = [];

        public IReadOnlyList<DrylNotification> Notifications => _items;
        public int UnreadCount => _items.Count(n => !n.Read);

        public DrylNotification Add(DrylNotification notification)
        {
            _items.Insert(0, notification);
            OnChanged?.Invoke();
            return notification;
        }

        public DrylNotification Add(string title, string? message = null, string? icon = null, AiState ai = AiState.None) =>
            Add(new DrylNotification { Title = title, Message = message, Icon = icon, Ai = ai });

        public void MarkRead(string id)
        {
            var item = _items.FirstOrDefault(n => n.Id == id);
            if (item is { Read: false }) { item.Read = true; OnChanged?.Invoke(); }
        }

        public void MarkAllRead()
        {
            foreach (var n in _items) n.Read = true;
            OnChanged?.Invoke();
        }

        public void Remove(string id)
        {
            if (_items.RemoveAll(n => n.Id == id) > 0) OnChanged?.Invoke();
        }

        public void Clear()
        {
            _items.Clear();
            OnChanged?.Invoke();
        }

        public event Action? OnChanged;
    }
}
