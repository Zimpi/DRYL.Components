using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace DRYL.Components.Tests;

/// <summary>
/// Stage two of <see cref="DrylCalendar"/> by keyboard — <c>S7</c>: the slot grid,
/// creating, moving and resizing, cancelling and what is announced.
/// </summary>
public partial class DrylCalendarEditingTests
{
    private static KeyboardEventArgs Key(string key, bool shift = false) => new() { Key = key, ShiftKey = shift };

    private static string CursorName(IRenderedComponent<DrylCalendar> cut) => cut.Find(".cal-cursor").GetAttribute("aria-label")!;

    // ── S7 · The slot grid ──────────────────────────────────────────────

    [Fact]
    public void Editable_gives_the_time_grid_the_grid_role_with_one_focusable_cell()
    {
        var cut = RenderCalendar(new Host());

        var grid = cut.Find("[role='grid']");
        Assert.Contains("cal-time", grid.ClassList);
        Assert.Equal("49", grid.GetAttribute("aria-rowcount"));   // 48 slots and the all-day row
        Assert.Equal("7", grid.GetAttribute("aria-colcount"));
        var cell = Assert.Single(cut.FindAll("[role='gridcell']"));
        Assert.Equal("0", cell.GetAttribute("tabindex"));
        Assert.Equal("row", cell.ParentElement!.GetAttribute("role"));
        Assert.Equal(new CalendarLabels().CreateHint, cut.Find("#" + cell.GetAttribute("aria-describedby")).TextContent);
    }

    [Fact]
    public void Without_Editable_there_is_no_grid_role_and_no_cursor()
    {
        var cut = RenderCalendar(new Host(), editable: false);

        Assert.Empty(cut.FindAll("[role='grid']"));
        Assert.Empty(cut.FindAll(".cal-cursor"));
    }

    [Fact]
    public void The_cursor_starts_on_Date_at_InitialScrollTime()
    {
        var cut = RenderCalendar(new Host());

        Assert.Equal("Monday, October 12, 2026, 7:00 AM", CursorName(cut));
    }

    [Fact]
    public void The_arrow_keys_move_the_cursor_by_slot_and_by_day()
    {
        var cut = RenderCalendar(new Host());

        cut.Find(".cal-cursor").KeyDown(Key("ArrowDown"));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowRight"));

        Assert.Equal("Tuesday, October 13, 2026, 7:30 AM", CursorName(cut));
    }

    [Fact]
    public void ArrowUp_from_the_first_slot_enters_the_all_day_row_and_Home_End_cross_the_row()
    {
        var cut = RenderCalendar(new Host(), p => p.Add(c => c.InitialScrollTime, TimeSpan.Zero));

        cut.Find(".cal-cursor").KeyDown(Key("ArrowUp"));
        Assert.Equal("Monday, October 12, 2026, All day", CursorName(cut));
        Assert.NotNull(cut.Find(".cal-allday .cal-cursor"));

        cut.Find(".cal-cursor").KeyDown(Key("End"));
        Assert.Equal("Sunday, October 18, 2026, All day", CursorName(cut));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowRight"));
        Assert.Equal("Sunday, October 18, 2026, All day", CursorName(cut));
        cut.Find(".cal-cursor").KeyDown(Key("Home"));
        Assert.Equal("Monday, October 12, 2026, All day", CursorName(cut));
    }

    [Fact]
    public void In_the_month_view_the_cursor_moves_by_day_and_by_week()
    {
        var cut = RenderCalendar(new Host(), p => p.Add(c => c.View, CalendarView.Month));

        Assert.Equal("Monday, October 12, 2026", CursorName(cut));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowDown"));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowLeft"));
        Assert.Equal("Sunday, October 18, 2026", CursorName(cut));
    }

    [Fact]
    public void A_cell_in_person_columns_names_its_person()
    {
        var cut = RenderCalendar(new Host(), p => p
            .Add(c => c.View, CalendarView.Day)
            .Add(c => c.PersonLayout, CalendarPersonLayout.Columns)
            .Add(c => c.People, new[] { Ada, Bo }));

        cut.Find(".cal-cursor").KeyDown(Key("ArrowRight"));

        Assert.Equal("Monday, October 12, 2026, 7:00 AM, Bo", CursorName(cut));
    }

    // ── S7 · Creating by keyboard ───────────────────────────────────────

    [Fact]
    public void Enter_on_a_cell_starts_a_create_that_Shift_ArrowDown_extends_and_Enter_requests()
    {
        var host = new Host();
        var cut = RenderCalendar(host);

        cut.Find(".cal-cursor").KeyDown(Key("Enter"));
        Assert.Contains("is-active", cut.Find(".cal-preview").ClassList);
        cut.Find(".cal-cursor").KeyDown(Key("ArrowDown", shift: true));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowDown", shift: true));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowUp", shift: true));
        Assert.Empty(host.Requests);
        Assert.Equal("Monday, October 12, 2026, 7:00 AM – 8:00 AM", Announced(cut));
        cut.Find(".cal-cursor").KeyDown(Key("Enter"));

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Create, request.Kind);
        Assert.Equal(At(Mon, 7), request.Proposed.Start);
        Assert.Equal(At(Mon, 8), request.Proposed.End);
        Assert.DoesNotContain("is-active", cut.Find(".cal-preview").ClassList);
    }

    [Fact]
    public void A_keyboard_create_cannot_shrink_below_one_slot()
    {
        var host = new Host();
        var cut = RenderCalendar(host);

        cut.Find(".cal-cursor").KeyDown(Key("Enter"));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowUp", shift: true));
        cut.Find(".cal-cursor").KeyDown(Key("Enter"));

        var proposed = Assert.Single(host.Requests).Proposed;
        Assert.Equal(TimeSpan.FromMinutes(30), proposed.End - proposed.Start);
    }

    [Fact]
    public void Escape_and_leaving_the_cell_cancel_a_create()
    {
        var host = new Host();
        var cut = RenderCalendar(host);

        cut.Find(".cal-cursor").KeyDown(Key("Enter"));
        cut.Find(".cal-cursor").KeyDown(Key("Escape"));
        Assert.Equal("Change cancelled.", Announced(cut));
        cut.Find(".cal-cursor").KeyDown(Key("Enter"));
        cut.Find(".cal-cursor").FocusOut();
        Assert.DoesNotContain("is-active", cut.Find(".cal-preview").ClassList);

        Assert.Empty(host.Requests);
    }

    [Fact]
    public void A_create_in_the_month_view_covers_whole_days_and_Shift_ArrowRight_adds_one()
    {
        var host = new Host();
        var cut = RenderCalendar(host, p => p.Add(c => c.View, CalendarView.Month));

        cut.Find(".cal-cursor").KeyDown(Key("Enter"));
        cut.Find(".cal-cursor").KeyDown(Key("ArrowRight", shift: true));
        Assert.Equal(2, cut.FindAll(".cal-cell.is-preview").Count);
        cut.Find(".cal-cursor").KeyDown(Key("Enter"));

        var proposed = Assert.Single(host.Requests).Proposed;
        Assert.True(proposed.AllDay);
        Assert.Equal(At(Mon, 0), proposed.Start);
        Assert.Equal(At(Mon.AddDays(2), 0), proposed.End);
    }

    // ── S7 · Moving and resizing an event by keyboard ───────────────────

    [Fact]
    public void An_arrow_key_starts_a_move_and_Enter_requests_it_once()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        Assert.Contains("is-source", cut.Find("[data-event-id='a']").ClassList);
        Assert.Equal("9:30 AM – 10:30 AM", cut.Find(".cal-preview").GetAttribute("data-time"));
        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowRight"));
        Assert.Empty(host.Requests);
        Assert.Equal("Tuesday, October 13, 2026, 10:00 AM – 11:00 AM", Announced(cut));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Move, request.Kind);
        Assert.Equal(At(Mon.AddDays(1), 10), request.Proposed.Start);
    }

    [Fact]
    public void Shift_ArrowDown_resizes_the_end()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown", shift: true));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Resize, request.Kind);
        Assert.Equal(At(Mon, 9), request.Proposed.Start);
        Assert.Equal(At(Mon, 10, 30), request.Proposed.End);
    }

    [Fact]
    public void Escape_and_focus_leaving_cancel_a_keyboard_move()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Escape"));
        Assert.Equal("Change cancelled.", Announced(cut));
        Assert.DoesNotContain("is-source", cut.Find("[data-event-id='a']").ClassList);
        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        cut.Find("[data-event-id='a']").FocusOut();
        Assert.DoesNotContain("is-source", cut.Find("[data-event-id='a']").ClassList);

        Assert.Empty(host.Requests);
    }

    [Fact]
    public void A_keyboard_move_stops_at_the_edge_of_the_grid()
    {
        var host = new Host { Events = [Timed("a", 8, 9)] };
        var cut = RenderCalendar(host, p => p.Add(c => c.DayStart, TimeSpan.FromHours(8)));

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowUp"));
        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowLeft"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        Assert.Empty(host.Requests);
    }

    [Fact]
    public void ArrowRight_in_person_columns_reassigns_and_announces_the_person()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada")] };
        var cut = RenderCalendar(host, p => p
            .Add(c => c.View, CalendarView.Day)
            .Add(c => c.PersonLayout, CalendarPersonLayout.Columns)
            .Add(c => c.People, new[] { Ada, Bo }));

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowRight"));
        Assert.Equal("Monday, October 12, 2026, 9:00 AM – 10:00 AM, Bo", Announced(cut));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        Assert.Equal(["bo"], Assert.Single(host.Requests).Proposed.PersonIds);
    }

    [Fact]
    public void An_arrow_key_on_a_ReadOnly_event_does_nothing()
    {
        var host = new Host { Events = [Timed("a", 9, 10) with { ReadOnly = true }] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        Assert.DoesNotContain("is-source", cut.Find("[data-event-id='a']").ClassList);
        Assert.Empty(host.Requests);
    }

    [Fact]
    public void In_the_month_view_ArrowDown_moves_an_event_by_a_week_keeping_its_time()
    {
        var host = new Host { Events = [Timed("a", 9, 10)] };
        var cut = RenderCalendar(host, p => p.Add(c => c.View, CalendarView.Month));

        cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));
        Assert.Equal("2026-10-19", Assert.Single(cut.FindAll(".cal-cell.is-preview")).GetAttribute("data-date"));
        cut.Find("[data-event-id='a']").KeyDown(Key("Enter"));

        Assert.Equal(At(Mon.AddDays(7), 9), Assert.Single(host.Requests).Proposed.Start);
    }

    [Fact]
    public void Shift_ArrowRight_on_a_bar_adds_a_day()
    {
        var bar = new CalendarEvent { Id = "d", Title = "d", AllDay = true, Start = At(Mon, 0), End = At(Mon.AddDays(1), 0) };
        var host = new Host { Events = [bar] };
        var cut = RenderCalendar(host);

        cut.Find("[data-event-id='d']").KeyDown(Key("ArrowRight", shift: true));
        Assert.Equal(2, cut.FindAll(".cal-allday-cell.is-preview").Count);
        cut.Find("[data-event-id='d']").KeyDown(Key("Enter"));

        var request = Assert.Single(host.Requests);
        Assert.Equal(CalendarChangeKind.Resize, request.Kind);
        Assert.Equal(At(Mon.AddDays(2), 0), request.Proposed.End);
    }

    [Fact]
    public void A_step_announces_the_conflicts_of_the_preview_and_others_show_the_mark()
    {
        var host = new Host { Events = [Timed("a", 9, 10, "ada"), Timed("b", 11, 12, "ada")] };
        var cut = RenderCalendar(host, p => p.Add(c => c.People, new[] { Ada }));
        Assert.DoesNotContain("has-conflict", cut.Find("[data-event-id='b']").ClassList);

        for (var i = 0; i < 4; i++) cut.Find("[data-event-id='a']").KeyDown(Key("ArrowDown"));

        Assert.EndsWith("Overlaps another event of Ada", Announced(cut));
        Assert.Contains("has-conflict", cut.Find("[data-event-id='b']").ClassList);
        Assert.Equal("1", cut.Find(".cal-preview").GetAttribute("data-pv-conflict"));
    }

    [Fact]
    public void An_editable_event_is_described_by_the_edit_hint_and_a_ReadOnly_one_is_not()
    {
        var host = new Host { Events = [Timed("a", 9, 10), Timed("b", 11, 12) with { ReadOnly = true }] };
        var cut = RenderCalendar(host);

        Assert.NotNull(cut.Find("[data-event-id='a']").GetAttribute("aria-describedby"));
        Assert.Null(cut.Find("[data-event-id='b']").GetAttribute("aria-describedby"));
    }
}
