using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace DRYL.Components.Tests;

/// <summary>
/// Regressions found by the independent review of stage two: a real parent host
/// whose handler completes at once, focus after a keyboard move, and keys during
/// a keyboard change.
/// </summary>
public partial class DrylCalendarEditingTests
{
    // A plain host: it keeps its events and applies every request synchronously,
    // re-rendering through its own EventCallback like any page would.
    private sealed class SyncHost : ComponentBase
    {
        [Parameter] public List<CalendarEvent> Events { get; set; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DrylCalendar>(0);
            builder.AddAttribute(1, nameof(DrylCalendar.Now), (DateTime?)Now);
            builder.AddAttribute(2, nameof(DrylCalendar.FirstDayOfWeek), (DayOfWeek?)DayOfWeek.Monday);
            builder.AddAttribute(3, nameof(DrylCalendar.Culture), EnUs);
            builder.AddAttribute(4, nameof(DrylCalendar.Editable), true);
            builder.AddAttribute(5, nameof(DrylCalendar.Events), Events);
            builder.AddAttribute(6, nameof(DrylCalendar.OnEventChangeRequested),
                EventCallback.Factory.Create<CalendarChangeRequest>(this, (Action<CalendarChangeRequest>)Apply));
            builder.CloseComponent();
        }

        private void Apply(CalendarChangeRequest r)
            => Events = Events.Select(e => e.Id == r.Original?.Id ? r.Proposed : e).ToList();
    }

    [Fact]
    public void A_keyboard_change_a_synchronous_host_applied_is_not_reported_as_refused()
    {
        var cut = Render<SyncHost>(p => p.Add(h => h.Events, new List<CalendarEvent> { Timed("a", 9, 10) }));

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        cut.WaitForAssertion(() => Assert.StartsWith("Change requested", cut.Find(".visually-hidden[aria-live]").TextContent.Trim()));
        Assert.Equal(At(Mon, 9, 30), cut.Instance.Events.Single().Start);
        Assert.Empty(cut.FindAll(".is-returning"));
    }

    [Fact]
    public void Focus_after_a_keyboard_move_never_goes_to_the_exiting_ghost()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowRight"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        cut.WaitForAssertion(() =>
        {
            var focus = JSInterop.Invocations.Last(i => i.Identifier == "dryl.calendar.focus");
            Assert.Equal("[data-event-id=\"a\"]:not([aria-hidden])", focus.Arguments[1]);
        });
    }

    [Fact]
    public void Only_the_event_under_a_keyboard_change_is_marked_for_the_bridge()
    {
        var host = new Host { Events = [Timed("a", 9, 10), Timed("b", 11, 12)] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));

        Assert.Equal("true", cut.Find("[data-event-id='a']").GetAttribute("data-key-editing"));
        Assert.Null(cut.Find("[data-event-id='b']").GetAttribute("data-key-editing"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Escape"));
        Assert.Null(cut.Find("[data-event-id='a']").GetAttribute("data-key-editing"));
    }

    [Fact]
    public async Task A_refused_event_stops_gliding_once_the_host_moves_it_itself()
    {
        var host = new Host { Events = [Timed("a", 9, 10)], Apply = false };
        var cut = RenderCalendar(host);

        await Gesture(cut, "move", "time", "a", 60);
        Assert.Contains("is-returning", cut.Find(".cal-slot--timed").ClassList);

        cut.Render(p => p.Add(c => c.Events, new[] { Timed("a", 13, 14) }));
        Assert.DoesNotContain("is-returning", cut.Find(".cal-slot--timed").ClassList);
    }
}
