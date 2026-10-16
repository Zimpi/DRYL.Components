using System.Globalization;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for stage two of <see cref="DrylCalendar"/> — editing — grouped
/// by the stories <c>S5</c>–<c>S8</c> of <c>specs/E5 Data/F22 DrylCalendar</c>. The
/// arithmetic is pinned in <see cref="CalendarEditingTests"/>; a pointer gesture is
/// driven here through the bridge's entry point, <see cref="DrylCalendar.CompleteGesture"/>,
/// with the coordinates the bridge reports.
/// </summary>
public partial class DrylCalendarEditingTests : BunitContext
{
    // Monday 12 October 2026; the visible week runs from Monday midnight.
    private static readonly DateOnly Mon = new(2026, 10, 12);
    private static readonly DateTime Now = new(2026, 10, 12, 10, 30, 0);
    private static readonly CultureInfo EnUs = new("en-US");

    public DrylCalendarEditingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DateTime At(DateOnly day, int hour, int minute = 0) => day.ToDateTime(new TimeOnly(hour, minute));

    private static CalendarEvent Timed(string id, int fromHour, int toHour, params string[] people)
        => new() { Id = id, Title = id, Start = At(Mon, fromHour), End = At(Mon, toHour), PersonIds = people };

    private static readonly CalendarPerson Ada = new() { Id = "ada", Name = "Ada" };
    private static readonly CalendarPerson Bo = new() { Id = "bo", Name = "Bo" };

    // A host that keeps its events and applies (or refuses) every request.
    private sealed class Host
    {
        public List<CalendarEvent> Events = [];
        public List<CalendarChangeRequest> Requests = [];
        public bool Apply = true;
        public TaskCompletionSource? Gate;

        public async Task OnRequest(CalendarChangeRequest request)
        {
            Requests.Add(request);
            if (Gate is not null) await Gate.Task;
            if (!Apply) return;
            Events = request.Kind == CalendarChangeKind.Create
                ? [.. Events, request.Proposed]
                : Events.Select(e => e.Id == request.Original!.Id ? request.Proposed : e).ToList();
        }
    }

    private IRenderedComponent<DrylCalendar> RenderCalendar(Host host,
        Action<ComponentParameterCollectionBuilder<DrylCalendar>>? configure = null, bool editable = true,
        Func<IRenderedComponent<DrylCalendar>, CalendarChangeRequest, Task>? handler = null)
    {
        IRenderedComponent<DrylCalendar>? cut = null;
        cut = Render<DrylCalendar>(p =>
        {
            p.Add(c => c.Now, Now);
            p.Add(c => c.FirstDayOfWeek, DayOfWeek.Monday);
            p.Add(c => c.Culture, EnUs);
            p.Add(c => c.Editable, editable);
            p.Add(c => c.Events, host.Events);
            p.Add(c => c.OnEventChangeRequested, async (CalendarChangeRequest r) =>
            {
                if (handler is not null)
                {
                    await handler(cut!, r);
                    return;
                }
                await host.OnRequest(r);
                // The host's re-render: the new Events reach the calendar.
                cut!.Render(q => q.Add(c => c.Events, host.Events));
            });
            configure?.Invoke(p);
        });
        return cut;
    }

    private static Task Gesture(IRenderedComponent<DrylCalendar> cut, string kind, string surface, string? id,
        int a, int b = 0, string? edge = null, string? from = null, string? to = null)
        => cut.InvokeAsync(() => cut.Instance.CompleteGesture(kind, surface, id, edge, from, to, a, b));

    private static string Announced(IRenderedComponent<DrylCalendar> cut)
        => cut.Find(".visually-hidden[aria-live]").TextContent.Trim();

    // ── S5 · Switching editing on ───────────────────────────────────────

    [Fact]
    public void Editable_left_false_renders_stage_one_markup()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 9, 10, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }), editable: false);

        Assert.Empty(cut.FindAll("[data-editable]"));
        Assert.Empty(cut.FindAll(".cal-resize"));
        Assert.Empty(cut.FindAll(".cal-conflict"));
        Assert.Empty(cut.FindAll(".has-conflict"));
        Assert.Empty(cut.FindAll(".visually-hidden[aria-live]"));
        Assert.DoesNotContain("is-editable", cut.Find(".cal-root").ClassList);
    }

    [Fact]
    public async Task Editable_left_false_ignores_a_gesture()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host, editable: false);

        await Gesture(cut, "move", "time", "a", 60);

        Assert.Empty(host.Requests);
    }

    [Fact]
    public void Editable_marks_every_event_editable_and_describes_it()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        var button = cut.Find("[data-event-id='a']");
        Assert.Equal("true", button.GetAttribute("data-editable"));
        var hint = cut.Find("#" + button.GetAttribute("aria-describedby"));
        Assert.Equal(new CalendarLabels().EditHint, hint.TextContent);
        Assert.Contains("is-editable", cut.Find(".cal-root").ClassList);
    }

    [Fact]
    public async Task A_ReadOnly_event_cannot_be_moved_but_still_clicks()
    {
        var clicked = new List<CalendarEvent>();
        var locked = Timed("a", 9, 10) with { ReadOnly = true };
        var host = new Host { Events = [locked] };
        var cut = RenderCalendar(host, p => p.Add(c => c.OnEventClick, (CalendarEvent e) => clicked.Add(e)));

        var button = cut.Find("[data-event-id='a']");
        Assert.Null(button.GetAttribute("data-editable"));
        Assert.Empty(button.QuerySelectorAll(".cal-resize"));
        await Gesture(cut, "move", "time", "a", 60);
        button.Click();

        Assert.Empty(host.Requests);
        Assert.Same(locked, Assert.Single(clicked));
    }

    // ── S6 · Resize handles ─────────────────────────────────────────────

    [Fact]
    public void A_timed_event_has_both_handles_and_a_continuing_edge_has_none()
    {
        var overnight = new CalendarEvent { Id = "n", Title = "n", Start = At(Mon, 22), End = At(Mon.AddDays(1), 2) };
        var host = new Host { Events = [Timed("a", 9, 10), overnight] };
        var cut = RenderCalendar(host);

        var a = cut.Find("[data-event-id='a']");
        Assert.Single(a.QuerySelectorAll(".cal-resize--start"));
        Assert.Single(a.QuerySelectorAll(".cal-resize--end"));

        var segments = cut.FindAll("[data-event-id='n']");
        Assert.Equal(2, segments.Count);
        Assert.Empty(segments[0].QuerySelectorAll(".cal-resize--end"));
        Assert.Empty(segments[1].QuerySelectorAll(".cal-resize--start"));
    }

    [Fact]
    public void A_single_line_of_the_month_view_has_no_handle()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host, p => p.Add(c => c.View, CalendarView.Month));

        Assert.Empty(cut.Find("[data-event-id='a']").QuerySelectorAll(".cal-resize"));
    }

    // ── S5 · The request ────────────────────────────────────────────────

    [Fact]
    public async Task A_completed_move_fires_one_request_with_the_original_and_the_proposal()
    {
        var original = Timed("a", 9, 10, "ada") with { SeriesId = "s", Data = new object() };
        var host = new Host { Events = [original] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));

        await Gesture(cut, "move", "time", "a", 90);

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Move, request.Kind);
        Assert.Same(original, request.Original);
        Assert.Equal("a", request.Proposed.Id);
        Assert.Equal(At(Mon, 10, 30), request.Proposed.Start);
        Assert.Equal(At(Mon, 11, 30), request.Proposed.End);
        Assert.Equal(original with { Start = request.Proposed.Start, End = request.Proposed.End }, request.Proposed);
        Assert.Empty(request.Conflicts);
    }

    [Fact]
    public async Task A_change_that_ends_where_it_started_fires_nothing()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        await Gesture(cut, "move", "time", "a", 0);

        Assert.Empty(host.Requests);
    }

    [Fact]
    public async Task A_move_past_DayEnd_fires_nothing()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host, p => p.Add(c => c.DayEnd, TimeSpan.FromHours(18)));

        await Gesture(cut, "move", "time", "a", 9 * 60);

        Assert.Empty(host.Requests);
    }

    [Fact]
    public async Task A_resize_is_a_Resize_and_keeps_the_start()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        await Gesture(cut, "resize", "time", "a", 30, edge: "end");

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Resize, request.Kind);
        Assert.Equal(At(Mon, 9), request.Proposed.Start);
        Assert.Equal(At(Mon, 10, 30), request.Proposed.End);
    }

    [Fact]
    public async Task A_created_event_is_new_empty_and_on_slot_boundaries()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        await Gesture(cut, "create", "time", null, 13 * 60, 14 * 60 + 30);

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Create, request.Kind);
        Assert.Null(request.Original);
        Assert.False(string.IsNullOrEmpty(request.Proposed.Id));
        Assert.NotEqual("a", request.Proposed.Id);
        Assert.Equal("", request.Proposed.Title);
        Assert.Equal(CalendarColor.Auto, request.Proposed.Color);
        Assert.Null(request.Proposed.SeriesId);
        Assert.Null(request.Proposed.Data);
        Assert.Empty(request.Proposed.PersonIds);
        Assert.False(request.Proposed.AllDay);
        Assert.Equal(At(Mon, 13), request.Proposed.Start);
        Assert.Equal(At(Mon, 14, 30), request.Proposed.End);
    }

    [Fact]
    public async Task An_event_created_in_the_month_view_is_all_day()
    {
        var host = new Host();
        var cut = RenderCalendar(host, p => p.Add(c => c.View, CalendarView.Month));

        // The month view of October 2026 starts on Monday 28 September.
        await Gesture(cut, "create", "month", null, 15, 16);

        var proposed = Assert.Single(host.Requests).Proposed;
        Assert.True(proposed.AllDay);
        Assert.Equal(new DateTime(2026, 10, 13), proposed.Start);
        Assert.Equal(new DateTime(2026, 10, 15), proposed.End);
    }

    [Fact]
    public async Task An_event_created_in_a_persons_column_carries_that_person()
    {
        var host = new Host();
        var cut = RenderCalendar(host, p => p
            .Add(c => c.View, CalendarView.Day)
            .Add(c => c.PersonLayout, CalendarPersonLayout.Columns)
            .Add(c => c.People, new[] { Ada, Bo }));

        await Gesture(cut, "create", "time", null, 9 * 60, 10 * 60, to: "person|bo");

        Assert.Equal(["bo"], Assert.Single(host.Requests).Proposed.PersonIds);
    }

    [Fact]
    public async Task A_move_across_person_columns_reassigns_the_event()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada")] };
        var cut = RenderCalendar(host, p => p
            .Add(c => c.View, CalendarView.Day)
            .Add(c => c.PersonLayout, CalendarPersonLayout.Columns)
            .Add(c => c.People, new[] { Ada, Bo }));

        await Gesture(cut, "move", "time", "a", 0, from: "person|ada", to: "person|bo");

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Move, request.Kind);
        Assert.Equal(["bo"], request.Proposed.PersonIds);
        Assert.Equal(At(Mon, 9), request.Proposed.Start);
    }

    [Fact]
    public async Task A_change_to_an_occurrence_keeps_its_series()
    {
        var host = new Host { Events = [Timed("a", 9, 10) with { SeriesId = "weekly" }, Timed("b", 9, 10) with { SeriesId = "weekly" }] };
        var cut = RenderCalendar(host);

        await Gesture(cut, "move", "time", "a", 60);

        var request = Assert.Single(host.Requests);
        Assert.Equal("weekly", request.Proposed.SeriesId);
        Assert.Equal(At(Mon, 9), host.Events.Single(e => e.Id == "b").Start);
    }

    // ── S5 · While the host decides ─────────────────────────────────────

    [Fact]
    public async Task While_the_host_decides_the_event_is_drawn_where_it_was_dropped_and_busy()
    {
        var host = new Host { Events = [Timed("a", 9, 10)], Gate = new TaskCompletionSource() };
        var cut = RenderCalendar(host);
        var before = cut.Find(".cal-slot--timed").GetAttribute("style");

        var gesture = Gesture(cut, "move", "time", "a", 120);

        var button = cut.Find("[data-event-id='a']");
        Assert.NotEqual(before, cut.Find(".cal-slot--timed").GetAttribute("style"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.Contains("is-pending", button.ClassList);
        Assert.Null(button.GetAttribute("data-editable"));

        // A second change of the same event is refused while the first is decided.
        await Gesture(cut, "move", "time", "a", 30);
        Assert.Single(host.Requests);

        host.Gate.SetResult();
        await gesture;
        Assert.Null(cut.Find("[data-event-id='a']").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task While_the_host_decides_a_created_event_carries_the_NewEvent_title()
    {
        var host = new Host { Gate = new TaskCompletionSource() };
        var cut = RenderCalendar(host);

        var gesture = Gesture(cut, "create", "time", null, 9 * 60, 10 * 60);

        Assert.Contains("New event", cut.Find(".is-pending").TextContent);
        host.Gate.SetResult();
        await gesture;
    }

    [Fact]
    public async Task An_applied_change_stays_and_is_announced_as_requested()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        await Gesture(cut, "move", "time", "a", 60);

        Assert.Equal(At(Mon, 10), host.Events.Single().Start);
        Assert.Empty(cut.FindAll(".is-returning"));
        Assert.StartsWith("Change requested: Monday, October 12, 2026, 10:00", Announced(cut));
    }

    [Fact]
    public async Task A_change_the_host_did_not_apply_returns_and_is_announced()
    {
        var host = new Host { Events = [Timed("a", 9, 10)], Apply = false };
        var cut = RenderCalendar(host);
        var before = cut.Find(".cal-slot--timed").GetAttribute("style");

        await Gesture(cut, "move", "time", "a", 60);

        var slot = cut.Find(".cal-slot--timed");
        Assert.Equal(before, slot.GetAttribute("style"));
        Assert.Contains("is-returning", slot.ClassList);
        Assert.Equal("The change was not applied.", Announced(cut));
    }

    [Fact]
    public async Task A_created_event_the_host_did_not_apply_exits()
    {
        var host = new Host { Apply = false };
        var cut = RenderCalendar(host);

        await Gesture(cut, "create", "time", null, 9 * 60, 10 * 60);

        Assert.Contains("is-exiting", cut.Find(".cal-slot--timed").ClassList);
        Assert.Equal("The change was not applied.", Announced(cut));
    }

    [Fact]
    public async Task A_created_event_the_host_stored_under_its_own_id_counts_as_applied()
    {
        var host = new Host();
        var cut = RenderCalendar(host, handler: (c, r) =>
        {
            host.Events = [r.Proposed with { Id = "db-17", Title = "Stored" }];
            c.Render(q => q.Add(x => x.Events, host.Events));
            return Task.CompletedTask;
        });

        await Gesture(cut, "create", "time", null, 9 * 60, 10 * 60);

        Assert.DoesNotContain("not applied", Announced(cut));
        Assert.Single(cut.FindAll("[data-event-id='db-17']"));
    }

    [Fact]
    public async Task A_faulted_handler_is_not_applied_and_the_exception_is_not_swallowed()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host, handler: (_, _) => Task.FromException(new InvalidOperationException("store failed")));

        // The bridge's call returns to the browser; the fault goes to the renderer,
        // as an exception of the component's own event handler would.
        await Gesture(cut, "move", "time", "a", 60);
        var thrown = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("store failed", Assert.IsType<InvalidOperationException>(thrown).Message);
        Assert.Null(cut.Find("[data-event-id='a']").GetAttribute("aria-busy"));
    }

    // ── S8 · Conflicts ──────────────────────────────────────────────────

    [Fact]
    public void Overlapping_events_of_one_person_carry_the_conflict_mark_and_say_so()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 9, 11, "ada"), Timed("c", 12, 13, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));

        var a = cut.Find("[data-event-id='a']");
        Assert.Contains("has-conflict", a.ClassList);
        Assert.EndsWith("Overlaps another event of Ada", a.GetAttribute("aria-label"));
        Assert.Equal("true", a.QuerySelector(".cal-conflict")!.GetAttribute("aria-hidden"));
        Assert.DoesNotContain("has-conflict", cut.Find("[data-event-id='c']").ClassList);
        Assert.DoesNotContain("Overlaps", cut.Find("[data-event-id='c']").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_conflict_mark_is_drawn_with_an_EventTemplate()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 9, 11, "ada")] };
        var cut = RenderCalendar(host, p => p
            .Add(c => c.People, new[] { Ada })
            .Add(c => c.EventTemplate, e => b => b.AddContent(0, "custom " + e.Title)));

        Assert.Single(cut.Find("[data-event-id='a']").QuerySelectorAll(".cal-conflict"));
    }

    [Fact]
    public void An_absence_and_working_hours_are_named_in_the_accessible_name()
    {
        var host = new Host { Events = [Timed("a", 17, 19, "ada")] };
        var cut = RenderCalendar(host, p => p
            .Add(c => c.People, new[] { Ada })
            .Add(c => c.Availability, new[]
            {
                new CalendarAvailability { PersonId = "ada", Start = At(Mon, 9), End = At(Mon, 17) },
                new CalendarAvailability { PersonId = "ada", Kind = CalendarAvailabilityKind.Absent, Start = At(Mon, 18), End = At(Mon, 20) }
            }));

        Assert.EndsWith("Ada is absent. Outside the working hours of Ada", cut.Find("[data-event-id='a']").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task The_request_carries_the_conflicts_of_the_proposal()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 11, 12, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));

        await Gesture(cut, "move", "time", "a", 120);

        var conflict = Assert.Single(Assert.Single(host.Requests).Conflicts);
        Assert.Equal(CalendarConflictKind.Overlap, conflict.Kind);
        Assert.Equal("b", conflict.EventId);
        Assert.Contains("Overlaps another event of Ada", Announced(cut));
    }

    [Fact]
    public async Task A_conflict_never_prevents_a_drop()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 11, 12, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));

        await Gesture(cut, "move", "time", "a", 120);

        Assert.Equal(At(Mon, 11), host.Events.Single(e => e.Id == "a").Start);
        Assert.Contains("has-conflict", cut.Find("[data-event-id='a']").ClassList);
    }

    [Fact]
    public void All_day_events_carry_no_conflict_mark()
    {
        var allDay = new CalendarEvent { Id = "d", AllDay = true, Start = At(Mon, 0), End = At(Mon.AddDays(1), 0), PersonIds = ["ada"] };
        var host = new Host { Events = [allDay, Timed("a", 9, 10, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));

        Assert.Empty(cut.Find("[data-event-id='d']").QuerySelectorAll(".cal-conflict"));
    }
}
