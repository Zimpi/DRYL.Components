using DRYL.Components.Internal;

namespace DRYL.Components.Tests;

/// <summary>
/// Pure-arithmetic tests for the calendar's editing: the limits a change keeps to,
/// the steps, reassignment and conflict detection. Each test is named after the
/// acceptance criterion of <c>specs/E5 Data/F22 DrylCalendar/S6</c> or <c>S8</c> it pins.
/// </summary>
public class CalendarEditingTests
{
    private static readonly CalendarGrid Office = new(TimeSpan.FromHours(8), TimeSpan.FromHours(18), 30);
    private static readonly CalendarGrid FullDay = new(TimeSpan.Zero, TimeSpan.FromHours(24), 30);

    // Monday 12 October 2026; the visible week runs Monday to Sunday.
    private static readonly DateOnly Mon = new(2026, 10, 12);
    private static readonly CalendarBounds Week = new(At(Mon, 0), At(Mon.AddDays(7), 0), Office);

    private static DateTime At(DateOnly day, int hour, int minute = 0) => day.ToDateTime(TimeOnly.MinValue).AddHours(hour).AddMinutes(minute);

    private static CalendarEvent Timed(string id, DateOnly day, int fromHour, int fromMinute, int toHour, int toMinute, params string[] people)
        => new() { Id = id, Title = id, Start = At(day, fromHour, fromMinute), End = At(day, toHour, toMinute), PersonIds = people };

    private static CalendarEvent AllDay(string id, DateOnly from, DateOnly endExclusive, params string[] people)
        => new() { Id = id, AllDay = true, Start = from.ToDateTime(TimeOnly.MinValue), End = endExclusive.ToDateTime(TimeOnly.MinValue), PersonIds = people };

    private static CalendarAvailability Span(string person, CalendarAvailabilityKind kind, DateTime start, DateTime end)
        => new() { PersonId = person, Kind = kind, Start = start, End = end };

    private static readonly IReadOnlySet<string> Known = new HashSet<string>(["ada", "bo"]);

    private static Dictionary<string, List<CalendarAvailability>> Availability(params CalendarAvailability[] spans)
        => spans.GroupBy(s => s.PersonId).ToDictionary(g => g.Key, g => g.ToList());

    // ── Limits (S6 "Moving", "Resizing") ────────────────────────────────

    [Fact]
    public void A_move_within_the_grid_fits()
    {
        var e = Timed("a", Mon, 9, 0, 10, 0);
        Assert.True(CalendarEditing.Fits(CalendarEditing.Move(e, TimeSpan.FromHours(2)), e, CalendarSurface.Time, Week));
    }

    [Fact]
    public void A_move_never_takes_an_event_before_DayStart()
    {
        var e = Timed("a", Mon, 8, 0, 9, 0);
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(e, TimeSpan.FromMinutes(-30)), e, CalendarSurface.Time, Week));
    }

    [Fact]
    public void A_move_never_takes_an_event_past_DayEnd()
    {
        var e = Timed("a", Mon, 17, 0, 18, 0);
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(e, TimeSpan.FromMinutes(30)), e, CalendarSurface.Time, Week));
    }

    [Fact]
    public void An_end_at_midnight_counts_as_the_end_of_the_day_before()
    {
        var bounds = Week with { Grid = FullDay };
        var e = Timed("a", Mon, 22, 0, 23, 0);
        var moved = CalendarEditing.Move(e, TimeSpan.FromHours(1));
        Assert.Equal(At(Mon.AddDays(1), 0), moved.End);
        Assert.True(CalendarEditing.Fits(moved, e, CalendarSurface.Time, bounds));
    }

    [Fact]
    public void A_move_never_leaves_the_visible_range()
    {
        var e = Timed("a", Mon.AddDays(6), 9, 0, 10, 0);
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(e, TimeSpan.FromDays(1)), e, CalendarSurface.Time, Week));
        var first = Timed("b", Mon, 9, 0, 10, 0);
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(first, TimeSpan.FromDays(-1)), first, CalendarSurface.Time, Week));
    }

    [Fact]
    public void A_limit_the_original_already_broke_may_stay_broken_but_not_grow()
    {
        var early = Timed("a", Mon, 7, 0, 9, 0);   // starts before DayStart 08:00
        Assert.True(CalendarEditing.Fits(CalendarEditing.Move(early, TimeSpan.FromMinutes(30)), early, CalendarSurface.Time, Week));
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(early, TimeSpan.FromMinutes(-30)), early, CalendarSurface.Time, Week));
    }

    [Fact]
    public void A_timed_event_never_becomes_an_all_day_event()
    {
        var e = Timed("a", Mon, 9, 0, 10, 0);
        var grown = CalendarEditing.Resize(e, end: true, TimeSpan.FromHours(24), TimeSpan.FromMinutes(30))!;
        Assert.False(CalendarEditing.Fits(grown, e, CalendarSurface.Time, Week with { Grid = FullDay }));
    }

    [Fact]
    public void A_bar_move_never_leaves_the_visible_range()
    {
        var bar = AllDay("a", Mon.AddDays(5), Mon.AddDays(7));
        Assert.True(CalendarEditing.Fits(CalendarEditing.Move(bar, TimeSpan.FromDays(-1)), bar, CalendarSurface.AllDay, Week));
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(bar, TimeSpan.FromDays(1)), bar, CalendarSurface.AllDay, Week));
    }

    [Fact]
    public void A_bar_that_continues_out_of_the_range_may_move_back_in()
    {
        var bar = AllDay("a", Mon.AddDays(-2), Mon.AddDays(2));
        Assert.True(CalendarEditing.Fits(CalendarEditing.Move(bar, TimeSpan.FromDays(1)), bar, CalendarSurface.AllDay, Week));
        Assert.False(CalendarEditing.Fits(CalendarEditing.Move(bar, TimeSpan.FromDays(-1)), bar, CalendarSurface.AllDay, Week));
    }

    // ── Steps ───────────────────────────────────────────────────────────

    [Fact]
    public void A_move_keeps_the_duration_and_the_minutes_past_the_slot()
    {
        var e = Timed("a", Mon, 9, 10, 9, 55);
        var moved = CalendarEditing.Move(e, TimeSpan.FromMinutes(30));
        Assert.Equal(At(Mon, 9, 40), moved.Start);
        Assert.Equal(e.End - e.Start, moved.End - moved.Start);
    }

    [Fact]
    public void A_resize_cannot_go_below_one_slot()
    {
        var e = Timed("a", Mon, 9, 0, 10, 0);
        Assert.Null(CalendarEditing.Resize(e, end: true, TimeSpan.FromMinutes(-60), TimeSpan.FromMinutes(30)));
        Assert.Equal(At(Mon, 9, 30), CalendarEditing.Resize(e, end: true, TimeSpan.FromMinutes(-30), TimeSpan.FromMinutes(30))!.End);
        Assert.Equal(At(Mon, 8, 30), CalendarEditing.Resize(e, end: false, TimeSpan.FromMinutes(-30), TimeSpan.FromMinutes(30))!.Start);
    }

    [Fact]
    public void A_bar_resize_changes_whole_days_and_cannot_go_below_one()
    {
        var bar = AllDay("a", Mon, Mon.AddDays(2));
        Assert.Equal(At(Mon.AddDays(3), 0), CalendarEditing.ResizeDays(bar, end: true, 1)!.End);
        Assert.Equal(At(Mon.AddDays(1), 0), CalendarEditing.ResizeDays(bar, end: false, 1)!.Start);
        Assert.Null(CalendarEditing.ResizeDays(bar, end: true, -2));
    }

    [Fact]
    public void A_single_day_all_day_event_whose_end_is_not_later_resizes_from_its_drawn_end()
    {
        var bar = new CalendarEvent { Id = "a", AllDay = true, Start = At(Mon, 0), End = At(Mon, 0) };
        var grown = CalendarEditing.ResizeDays(bar, end: true, 1)!;
        Assert.Equal(At(Mon.AddDays(2), 0), grown.End);
    }

    [Fact]
    public void A_created_timed_span_lies_on_slot_boundaries_and_stops_at_DayEnd()
    {
        Assert.Equal((At(Mon, 9, 0), At(Mon, 10, 30)), CalendarEditing.TimedSpan(Mon, 2, 4, Office));
        Assert.Equal((At(Mon, 9, 0), At(Mon, 10, 30)), CalendarEditing.TimedSpan(Mon, 4, 2, Office));
        var odd = new CalendarGrid(TimeSpan.FromHours(8), new TimeSpan(17, 45, 0), 30);
        Assert.Equal(At(Mon, 17, 45), CalendarEditing.TimedSpan(Mon, 19, 19, odd).End);
    }

    [Fact]
    public void A_created_all_day_span_covers_whole_days()
    {
        Assert.Equal((At(Mon, 0), At(Mon.AddDays(3), 0)), CalendarEditing.AllDaySpan(Mon.AddDays(2), Mon));
    }

    // ── Reassigning (S6 "Reassigning across person columns") ────────────

    [Fact]
    public void Reassigning_replaces_the_starting_columns_person_in_place()
        => Assert.Equal(["cy", "bo"], CalendarEditing.Reassign(["ada", "bo"], "ada", "cy"));

    [Fact]
    public void Reassigning_to_a_person_already_assigned_adds_no_duplicate()
        => Assert.Equal(["bo"], CalendarEditing.Reassign(["ada", "bo"], "ada", "bo"));

    [Fact]
    public void Moving_into_the_unassigned_column_removes_the_starting_person()
        => Assert.Equal(["bo"], CalendarEditing.Reassign(["ada", "bo"], "ada", null));

    [Fact]
    public void Moving_an_unassigned_event_into_a_column_adds_that_person()
        => Assert.Equal(["ada"], CalendarEditing.Reassign([], null, "ada"));

    // ── Conflicts (S8) ──────────────────────────────────────────────────

    [Fact]
    public void Two_overlapping_events_of_one_person_conflict_on_both()
    {
        var a = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var b = Timed("b", Mon, 9, 30, 11, 0, "ada");
        var all = CalendarEditing.AllConflicts([a, b], Availability(), Known);
        Assert.Equal("b", Assert.Single(all["a"]).EventId);
        Assert.Equal("a", Assert.Single(all["b"]).EventId);
        Assert.Equal(CalendarConflictKind.Overlap, all["a"][0].Kind);
        Assert.Equal("ada", all["a"][0].PersonId);
    }

    [Fact]
    public void Events_that_only_touch_do_not_overlap()
    {
        var a = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var b = Timed("b", Mon, 10, 0, 11, 0, "ada");
        Assert.Empty(CalendarEditing.AllConflicts([a, b], Availability(), Known));
    }

    [Fact]
    public void Events_of_different_people_do_not_conflict()
    {
        var a = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var b = Timed("b", Mon, 9, 0, 10, 0, "bo");
        Assert.Empty(CalendarEditing.AllConflicts([a, b], Availability(), Known));
    }

    [Fact]
    public void An_event_with_two_people_conflicts_only_for_the_one_concerned()
    {
        var a = Timed("a", Mon, 9, 0, 10, 0, "ada", "bo");
        var b = Timed("b", Mon, 9, 0, 10, 0, "bo");
        var conflict = Assert.Single(CalendarEditing.Conflicts(a, [a, b], Availability(), Known));
        Assert.Equal("bo", conflict.PersonId);
    }

    [Fact]
    public void An_event_overlapping_two_events_of_one_person_has_two_overlap_conflicts()
    {
        var a = Timed("a", Mon, 9, 0, 12, 0, "ada");
        var conflicts = CalendarEditing.Conflicts(a, [a, Timed("b", Mon, 9, 0, 10, 0, "ada"), Timed("c", Mon, 11, 0, 12, 0, "ada")], Availability(), Known);
        Assert.Equal(["b", "c"], conflicts.Select(c => c.EventId));
    }

    [Fact]
    public void All_day_and_unassigned_events_take_no_part()
    {
        var timed = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var allDay = AllDay("b", Mon, Mon.AddDays(1), "ada");
        var unassigned = Timed("c", Mon, 9, 0, 10, 0);
        var unknown = Timed("d", Mon, 9, 0, 10, 0, "nobody");
        Assert.Empty(CalendarEditing.AllConflicts([timed, allDay, unassigned, unknown], Availability(), Known));
    }

    [Fact]
    public void An_event_inside_an_absence_has_an_absence_conflict()
    {
        var a = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var conflict = Assert.Single(CalendarEditing.Conflicts(a, [a],
            Availability(Span("ada", CalendarAvailabilityKind.Absent, At(Mon, 0), At(Mon.AddDays(1), 0))), Known));
        Assert.Equal(CalendarConflictKind.Absence, conflict.Kind);
        Assert.Null(conflict.EventId);
    }

    [Fact]
    public void An_event_partly_outside_the_working_span_is_outside_working_hours()
    {
        var a = Timed("a", Mon, 16, 0, 18, 0, "ada");
        var conflict = Assert.Single(CalendarEditing.Conflicts(a, [a],
            Availability(Span("ada", CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17))), Known));
        Assert.Equal(CalendarConflictKind.OutsideWorkingHours, conflict.Kind);
    }

    [Fact]
    public void An_event_inside_two_adjoining_working_spans_is_not_outside()
    {
        var a = Timed("a", Mon, 11, 0, 14, 0, "ada");
        Assert.Empty(CalendarEditing.Conflicts(a, [a], Availability(
            Span("ada", CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 12)),
            Span("ada", CalendarAvailabilityKind.Working, At(Mon, 12), At(Mon, 17))), Known));
    }

    [Fact]
    public void An_event_in_a_lunch_gap_is_outside_working_hours()
    {
        var a = Timed("a", Mon, 12, 0, 12, 30, "ada");
        Assert.Single(CalendarEditing.Conflicts(a, [a], Availability(
            Span("ada", CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 12)),
            Span("ada", CalendarAvailabilityKind.Working, At(Mon, 13), At(Mon, 17))), Known));
    }

    [Fact]
    public void A_day_without_a_working_span_makes_nothing_outside_working_hours()
    {
        var a = Timed("a", Mon.AddDays(1), 20, 0, 21, 0, "ada");
        Assert.Empty(CalendarEditing.Conflicts(a, [a],
            Availability(Span("ada", CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17))), Known));
    }

    [Fact]
    public void An_absence_outside_working_hours_gives_both_conflicts()
    {
        var a = Timed("a", Mon, 18, 0, 19, 0, "ada");
        var kinds = CalendarEditing.Conflicts(a, [a], Availability(
            Span("ada", CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17)),
            Span("ada", CalendarAvailabilityKind.Absent, At(Mon, 17), At(Mon, 20))), Known).Select(c => c.Kind);
        Assert.Equal([CalendarConflictKind.Absence, CalendarConflictKind.OutsideWorkingHours], kinds);
    }

    [Fact]
    public void A_moved_event_never_conflicts_with_its_own_old_place()
    {
        var original = Timed("a", Mon, 9, 0, 10, 0, "ada");
        var proposed = CalendarEditing.Move(original, TimeSpan.FromMinutes(30));
        Assert.Empty(CalendarEditing.Conflicts(proposed, [original], Availability(), Known));
    }
}
