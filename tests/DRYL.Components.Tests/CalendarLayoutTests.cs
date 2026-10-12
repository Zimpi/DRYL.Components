using System.Globalization;
using DRYL.Components.Internal;

namespace DRYL.Components.Tests;

/// <summary>
/// Pure-arithmetic tests for the calendar's layout engine. Each test is named after
/// the acceptance criterion of <c>specs/E5 Data/F22 DrylCalendar</c> it pins.
/// </summary>
public class CalendarLayoutTests
{
    private static readonly CalendarGrid FullDay = new(TimeSpan.Zero, TimeSpan.FromHours(24), 30);
    private static readonly CalendarGrid Office = new(TimeSpan.FromHours(8), TimeSpan.FromHours(18), 30);

    // Monday 12 October 2026.
    private static readonly DateOnly Mon = new(2026, 10, 12);

    private static DateTime At(DateOnly day, int hour, int minute = 0)
        => day.ToDateTime(new TimeOnly(hour, minute));

    private static CalendarEvent Timed(string id, DateOnly day, int fromHour, int fromMinute, int toHour, int toMinute,
        params string[] people)
        => new() { Id = id, Title = id, Start = At(day, fromHour, fromMinute), End = At(day, toHour, toMinute), PersonIds = people };

    private static CalendarEvent AllDay(string id, DateOnly from, DateOnly endExclusive)
        => new() { Id = id, Title = id, AllDay = true, Start = from.ToDateTime(TimeOnly.MinValue), End = endExclusive.ToDateTime(TimeOnly.MinValue) };

    private static CalendarAvailability Span(CalendarAvailabilityKind kind, DateTime start, DateTime end, string? label = null)
        => new() { PersonId = "p", Kind = kind, Start = start, End = end, Label = label };

    private static CalendarSpan Hours(double from, double to) => new(TimeSpan.FromHours(from), TimeSpan.FromHours(to));

    // ── S1 · The visible range ──────────────────────────────────────────

    [Fact]
    public void Day_range_is_the_day_of_Date()
    {
        var range = CalendarLayout.VisibleRange(CalendarView.Day, Mon.AddDays(2), DayOfWeek.Monday);

        Assert.Equal(new DateTime(2026, 10, 14), range.Start);
        Assert.Equal(new DateTime(2026, 10, 15), range.End);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 12)]
    [InlineData(DayOfWeek.Sunday, 11)]
    [InlineData(DayOfWeek.Saturday, 10)]
    [InlineData(DayOfWeek.Thursday, 15)]
    public void Week_range_is_the_seven_days_from_the_first_day_of_week(DayOfWeek first, int expectedDay)
    {
        // Thursday 15 October 2026.
        var (start, days) = CalendarLayout.VisibleDays(CalendarView.Week, new DateOnly(2026, 10, 15), first);

        Assert.Equal(new DateOnly(2026, 10, expectedDay), start);
        Assert.Equal(7, days);
    }

    [Fact]
    public void Range_starts_at_midnight_and_ends_at_the_midnight_after_the_last_day()
    {
        var range = CalendarLayout.VisibleRange(CalendarView.Week, new DateOnly(2026, 10, 15), DayOfWeek.Monday);

        Assert.Equal(new DateTime(2026, 10, 12, 0, 0, 0), range.Start);
        Assert.Equal(new DateTime(2026, 10, 19, 0, 0, 0), range.End);
    }

    [Fact]
    public void Month_range_is_every_whole_week_that_contains_a_day_of_the_month()
    {
        // October 2026: Thursday the 1st to Saturday the 31st.
        var (first, days) = CalendarLayout.VisibleDays(CalendarView.Month, new DateOnly(2026, 10, 20), DayOfWeek.Monday);

        Assert.Equal(new DateOnly(2026, 9, 28), first);
        Assert.Equal(35, days);
    }

    [Fact]
    public void Month_of_four_week_rows()
    {
        // February 2026 starts on a Sunday and has 28 days.
        var (first, days) = CalendarLayout.VisibleDays(CalendarView.Month, new DateOnly(2026, 2, 10), DayOfWeek.Sunday);

        Assert.Equal(new DateOnly(2026, 2, 1), first);
        Assert.Equal(28, days);
    }

    [Fact]
    public void Month_of_six_week_rows()
    {
        // August 2026 starts on a Saturday and has 31 days.
        var (first, days) = CalendarLayout.VisibleDays(CalendarView.Month, new DateOnly(2026, 8, 15), DayOfWeek.Monday);

        Assert.Equal(new DateOnly(2026, 7, 27), first);
        Assert.Equal(42, days);
    }

    // ── S1 · Navigating ─────────────────────────────────────────────────

    [Fact]
    public void Previous_and_next_move_by_one_day_in_the_day_view()
    {
        Assert.Equal(Mon.AddDays(1), CalendarLayout.Step(CalendarView.Day, Mon, 1));
        Assert.Equal(Mon.AddDays(-1), CalendarLayout.Step(CalendarView.Day, Mon, -1));
    }

    [Fact]
    public void Previous_and_next_move_by_seven_days_in_the_week_view()
    {
        Assert.Equal(Mon.AddDays(7), CalendarLayout.Step(CalendarView.Week, Mon, 1));
        Assert.Equal(Mon.AddDays(-7), CalendarLayout.Step(CalendarView.Week, Mon, -1));
    }

    [Fact]
    public void Previous_and_next_move_by_one_month_in_the_month_view()
    {
        Assert.Equal(new DateOnly(2026, 11, 12), CalendarLayout.Step(CalendarView.Month, Mon, 1));
        Assert.Equal(new DateOnly(2026, 9, 12), CalendarLayout.Step(CalendarView.Month, Mon, -1));
    }

    [Fact]
    public void A_month_move_from_the_31st_lands_on_Februarys_last_day()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), CalendarLayout.Step(CalendarView.Month, new DateOnly(2026, 1, 31), 1));
        Assert.Equal(new DateOnly(2028, 2, 29), CalendarLayout.Step(CalendarView.Month, new DateOnly(2028, 3, 31), -1));
    }

    // ── S1 · The time grid ──────────────────────────────────────────────

    [Fact]
    public void A_DayEnd_not_later_than_DayStart_is_replaced_by_the_following_midnight()
    {
        var grid = CalendarLayout.Grid(TimeSpan.FromHours(9), TimeSpan.FromHours(9), 30, out var endReplaced, out var slotReplaced);

        Assert.Equal(TimeSpan.FromHours(24), grid.DayEnd);
        Assert.True(endReplaced);
        Assert.False(slotReplaced);
    }

    [Fact]
    public void A_valid_grid_is_kept_and_reports_nothing()
    {
        var grid = CalendarLayout.Grid(TimeSpan.FromHours(8), TimeSpan.FromHours(18), 15, out var endReplaced, out var slotReplaced);

        Assert.Equal(new CalendarGrid(TimeSpan.FromHours(8), TimeSpan.FromHours(18), 15), grid);
        Assert.False(endReplaced);
        Assert.False(slotReplaced);
        Assert.Equal(40, grid.SlotCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    [InlineData(7)]
    [InlineData(45)]
    [InlineData(90)]
    public void A_SlotMinutes_that_does_not_divide_sixty_is_replaced_by_30(int slotMinutes)
    {
        var grid = CalendarLayout.Grid(TimeSpan.Zero, TimeSpan.FromHours(24), slotMinutes, out _, out var slotReplaced);

        Assert.Equal(30, grid.SlotMinutes);
        Assert.True(slotReplaced);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(60)]
    public void A_SlotMinutes_that_divides_sixty_is_kept(int slotMinutes)
    {
        var grid = CalendarLayout.Grid(TimeSpan.Zero, TimeSpan.FromHours(24), slotMinutes, out _, out var slotReplaced);

        Assert.Equal(slotMinutes, grid.SlotMinutes);
        Assert.False(slotReplaced);
    }

    // ── S2 · What an event's times mean ─────────────────────────────────

    [Fact]
    public void DateTime_Kind_has_no_effect_on_where_the_event_is_drawn()
    {
        var local = new CalendarEvent { Id = "a", Start = DateTime.SpecifyKind(At(Mon, 9), DateTimeKind.Local), End = DateTime.SpecifyKind(At(Mon, 10), DateTimeKind.Local) };
        var utc = new CalendarEvent { Id = "b", Start = DateTime.SpecifyKind(At(Mon, 9), DateTimeKind.Utc), End = DateTime.SpecifyKind(At(Mon, 10), DateTimeKind.Utc) };

        var a = CalendarLayout.Segment(local, Mon, FullDay)!;
        var b = CalendarLayout.Segment(utc, Mon, FullDay)!;

        Assert.Equal(a.Top, b.Top);
        Assert.Equal(a.Height, b.Height);
    }

    [Fact]
    public void A_zero_length_event_is_drawn_one_slot_long()
    {
        var e = Timed("e", Mon, 10, 0, 10, 0);

        var segment = CalendarLayout.Segment(e, Mon, FullDay)!;

        Assert.Equal(10d / 24, segment.Top, 9);
        Assert.Equal(0.5 / 24, segment.Height, 9);
    }

    [Fact]
    public void An_event_ending_before_it_starts_is_drawn_one_slot_long()
    {
        var e = new CalendarEvent { Id = "e", Start = At(Mon, 10), End = At(Mon, 8) };

        var segment = CalendarLayout.Segment(e, Mon, FullDay)!;

        Assert.Equal(10d / 24, segment.Top, 9);
        Assert.Equal(0.5 / 24, segment.Height, 9);
    }

    [Fact]
    public void An_all_day_event_covers_its_start_date_up_to_but_not_including_its_end_date()
    {
        var (first, end) = CalendarLayout.DaySpan(AllDay("e", Mon, Mon.AddDays(3)));

        Assert.Equal(Mon, first);
        Assert.Equal(Mon.AddDays(3), end);
    }

    [Fact]
    public void An_all_day_event_whose_end_date_is_not_later_covers_its_start_date_alone()
    {
        var (first, end) = CalendarLayout.DaySpan(AllDay("e", Mon, Mon.AddDays(-2)));

        Assert.Equal(Mon, first);
        Assert.Equal(Mon.AddDays(1), end);
    }

    [Fact]
    public void The_time_of_day_of_an_all_day_event_has_no_effect()
    {
        var e = new CalendarEvent { Id = "e", AllDay = true, Start = At(Mon, 17, 30), End = At(Mon.AddDays(2), 3) };

        Assert.Equal((Mon, Mon.AddDays(2)), CalendarLayout.DaySpan(e));
    }

    [Fact]
    public void An_event_wholly_outside_the_visible_range_does_not_touch_it()
    {
        Assert.False(CalendarLayout.Touches(Timed("before", Mon.AddDays(-1), 9, 0, 10, 0), Mon, 7));
        Assert.False(CalendarLayout.Touches(Timed("after", Mon.AddDays(7), 9, 0, 10, 0), Mon, 7));
        Assert.True(CalendarLayout.Touches(Timed("in", Mon.AddDays(6), 23, 0, 23, 30), Mon, 7));
    }

    [Fact]
    public void A_timed_event_ending_at_midnight_does_not_touch_the_next_day()
    {
        var e = new CalendarEvent { Id = "e", Start = At(Mon, 22), End = At(Mon.AddDays(1), 0) };

        Assert.Equal((Mon, Mon.AddDays(1)), CalendarLayout.DaySpan(e));
        Assert.False(CalendarLayout.IsMonthBar(e));
    }

    [Fact]
    public void Of_two_events_with_the_same_Id_the_later_one_is_dropped_and_reported()
    {
        var first = Timed("dup", Mon, 9, 0, 10, 0);
        var second = Timed("dup", Mon, 11, 0, 12, 0);
        var other = Timed("other", Mon, 9, 0, 10, 0);

        var kept = CalendarLayout.DistinctById([first, other, second], e => e.Id, out var hadDuplicates);

        Assert.True(hadDuplicates);
        Assert.Equal(2, kept.Count);
        Assert.Same(first, kept[0]);
        Assert.Same(other, kept[1]);
    }

    [Fact]
    public void Distinct_ids_report_no_duplicate()
    {
        CalendarLayout.DistinctById([Timed("a", Mon, 9, 0, 10, 0), Timed("b", Mon, 9, 0, 10, 0)], e => e.Id, out var hadDuplicates);

        Assert.False(hadDuplicates);
    }

    // ── S2 · Placement in the day and week views ────────────────────────

    [Fact]
    public void A_timed_event_starts_at_its_Start_and_its_height_is_proportional_to_its_duration()
    {
        var segment = CalendarLayout.Segment(Timed("e", Mon, 10, 0, 12, 30), Mon, Office)!;

        Assert.Equal(0.2, segment.Top, 9);       // two of ten hours down
        Assert.Equal(0.25, segment.Height, 9);   // two and a half of ten hours
        Assert.False(segment.ContinuesBefore);
        Assert.False(segment.ContinuesAfter);
    }

    [Fact]
    public void An_event_shorter_than_one_slot_is_drawn_one_slot_high()
    {
        var segment = CalendarLayout.Segment(Timed("e", Mon, 10, 0, 10, 5), Mon, Office)!;

        Assert.Equal(0.05, segment.Height, 9);
    }

    [Fact]
    public void A_short_event_at_the_end_of_the_grid_stays_inside_it()
    {
        var segment = CalendarLayout.Segment(Timed("e", Mon, 17, 50, 18, 0), Mon, Office)!;

        Assert.Equal(0.05, segment.Height, 9);
        Assert.Equal(1d, segment.Top + segment.Height, 9);
    }

    [Fact]
    public void An_event_that_starts_before_DayStart_is_drawn_from_the_top()
    {
        var segment = CalendarLayout.Segment(Timed("e", Mon, 6, 0, 9, 0), Mon, Office)!;

        Assert.Equal(0d, segment.Top, 9);
        Assert.Equal(0.1, segment.Height, 9);
        Assert.False(segment.ContinuesBefore);   // same day — not a continuation
    }

    [Fact]
    public void An_event_that_ends_after_DayEnd_is_drawn_to_the_bottom()
    {
        var segment = CalendarLayout.Segment(Timed("e", Mon, 17, 0, 20, 0), Mon, Office)!;

        Assert.Equal(0.9, segment.Top, 9);
        Assert.Equal(1d, segment.Top + segment.Height, 9);
    }

    [Fact]
    public void An_event_outside_the_grid_hours_is_not_drawn_in_that_days_column()
    {
        Assert.Null(CalendarLayout.Segment(Timed("early", Mon, 5, 0, 8, 0), Mon, Office));
        Assert.Null(CalendarLayout.Segment(Timed("late", Mon, 18, 0, 19, 0), Mon, Office));
        Assert.Null(CalendarLayout.Segment(Timed("other", Mon, 9, 0, 10, 0), Mon.AddDays(1), Office));
    }

    [Fact]
    public void An_event_crossing_midnight_is_one_segment_in_each_day_it_touches()
    {
        var e = new CalendarEvent { Id = "e", Start = At(Mon, 22), End = At(Mon.AddDays(1), 2) };

        var first = CalendarLayout.Segment(e, Mon, FullDay)!;
        var second = CalendarLayout.Segment(e, Mon.AddDays(1), FullDay)!;

        Assert.Equal(22d / 24, first.Top, 9);
        Assert.Equal(2d / 24, first.Height, 9);
        Assert.False(first.ContinuesBefore);
        Assert.True(first.ContinuesAfter);

        Assert.Equal(0d, second.Top, 9);
        Assert.Equal(2d / 24, second.Height, 9);
        Assert.True(second.ContinuesBefore);
        Assert.False(second.ContinuesAfter);

        Assert.Null(CalendarLayout.Segment(e, Mon.AddDays(2), FullDay));
        Assert.Same(e, first.Event);
        Assert.Same(e, second.Event);
    }

    [Fact]
    public void A_timed_event_of_twenty_four_hours_or_more_goes_to_the_all_day_row()
    {
        var day = new CalendarEvent { Id = "day", Start = At(Mon, 9), End = At(Mon.AddDays(1), 9) };
        var almost = new CalendarEvent { Id = "almost", Start = At(Mon, 9), End = At(Mon.AddDays(1), 8, 59) };

        Assert.True(CalendarLayout.IsAllDayRow(day));
        Assert.False(CalendarLayout.IsAllDayRow(almost));
        Assert.Empty(CalendarLayout.Column([day], Mon, FullDay));
        Assert.Single(CalendarLayout.Column([almost], Mon, FullDay));
    }

    // ── S2 · Overlapping events ─────────────────────────────────────────

    [Fact]
    public void Overlapping_events_are_drawn_side_by_side()
    {
        var packed = CalendarLayout.Column([Timed("a", Mon, 9, 0, 11, 0), Timed("b", Mon, 10, 0, 12, 0)], Mon, FullDay);

        Assert.Equal(["a", "b"], packed.Select(s => s.Event.Id));
        Assert.Equal(0d, packed[0].Left, 9);
        Assert.Equal(0.5, packed[0].Width, 9);
        Assert.Equal(0.5, packed[1].Left, 9);
        Assert.Equal(0.5, packed[1].Width, 9);
    }

    [Fact]
    public void An_event_that_overlaps_no_other_uses_the_full_width()
    {
        var packed = CalendarLayout.Column(
            [Timed("a", Mon, 9, 0, 11, 0), Timed("b", Mon, 10, 0, 12, 0), Timed("alone", Mon, 14, 0, 15, 0)], Mon, FullDay);

        var alone = packed.Single(s => s.Event.Id == "alone");
        Assert.Equal(0d, alone.Left, 9);
        Assert.Equal(1d, alone.Width, 9);
    }

    [Fact]
    public void An_event_ending_when_another_starts_does_not_overlap_it()
    {
        var packed = CalendarLayout.Column([Timed("a", Mon, 9, 0, 10, 0), Timed("b", Mon, 10, 0, 11, 0)], Mon, FullDay);

        Assert.All(packed, s => Assert.Equal(1d, s.Width, 9));
    }

    [Fact]
    public void Side_by_side_events_are_ordered_by_Start_then_longer_duration_first()
    {
        var packed = CalendarLayout.Column(
            [Timed("short", Mon, 9, 0, 10, 0), Timed("later", Mon, 9, 30, 10, 30), Timed("long", Mon, 9, 0, 12, 0)], Mon, FullDay);

        Assert.Equal(["long", "short", "later"], packed.Select(s => s.Event.Id));
        Assert.Equal(0d, packed[0].Left, 9);
        Assert.Equal(1d / 3, packed[1].Left, 9);
        Assert.Equal(2d / 3, packed[2].Left, 9);
        Assert.All(packed, s => Assert.Equal(1d / 3, s.Width, 9));
    }

    [Fact]
    public void An_event_that_overlaps_a_group_only_later_takes_the_first_free_position()
    {
        // a: 9–12, b: 9–10, c: 10–11 — c overlaps a but not b, so it reuses b's position.
        var packed = CalendarLayout.Column(
            [Timed("a", Mon, 9, 0, 12, 0), Timed("b", Mon, 9, 0, 10, 0), Timed("c", Mon, 10, 0, 11, 0)], Mon, FullDay);

        var b = packed.Single(s => s.Event.Id == "b");
        var c = packed.Single(s => s.Event.Id == "c");
        Assert.Equal(0.5, b.Left, 9);
        Assert.Equal(0.5, c.Left, 9);
        Assert.All(packed, s => Assert.Equal(0.5, s.Width, 9));
    }

    [Fact]
    public void Events_starting_together_with_the_same_duration_keep_a_stable_order()
    {
        var packed = CalendarLayout.Column([Timed("b", Mon, 9, 0, 10, 0), Timed("a", Mon, 9, 0, 10, 0)], Mon, FullDay);

        Assert.Equal(["a", "b"], packed.Select(s => s.Event.Id));
    }

    // ── S2 · All-day and multi-day events ───────────────────────────────

    [Fact]
    public void An_all_day_event_is_a_bar_spanning_its_days()
    {
        var bar = Assert.Single(CalendarLayout.Bars([AllDay("e", Mon.AddDays(1), Mon.AddDays(4))], Mon, 7));

        Assert.Equal(1, bar.Column);
        Assert.Equal(3, bar.Span);
        Assert.Equal(0, bar.Lane);
        Assert.False(bar.ContinuesBefore);
        Assert.False(bar.ContinuesAfter);
    }

    [Fact]
    public void A_bar_that_starts_before_the_visible_range_continues_at_its_leading_edge()
    {
        var bar = Assert.Single(CalendarLayout.Bars([AllDay("e", Mon.AddDays(-2), Mon.AddDays(2))], Mon, 7));

        Assert.Equal(0, bar.Column);
        Assert.Equal(2, bar.Span);
        Assert.True(bar.ContinuesBefore);
        Assert.False(bar.ContinuesAfter);
    }

    [Fact]
    public void A_bar_that_ends_after_the_visible_range_continues_at_its_trailing_edge()
    {
        var bar = Assert.Single(CalendarLayout.Bars([AllDay("e", Mon.AddDays(5), Mon.AddDays(9))], Mon, 7));

        Assert.Equal(5, bar.Column);
        Assert.Equal(2, bar.Span);
        Assert.False(bar.ContinuesBefore);
        Assert.True(bar.ContinuesAfter);
    }

    [Fact]
    public void Bars_that_share_a_day_are_stacked_in_separate_lanes()
    {
        var bars = CalendarLayout.Bars(
            [AllDay("a", Mon, Mon.AddDays(3)), AllDay("b", Mon.AddDays(2), Mon.AddDays(4)), AllDay("c", Mon.AddDays(3), Mon.AddDays(5))], Mon, 7);

        Assert.Equal(0, bars.Single(b => b.Event.Id == "a").Lane);
        Assert.Equal(1, bars.Single(b => b.Event.Id == "b").Lane);
        Assert.Equal(0, bars.Single(b => b.Event.Id == "c").Lane);   // a has ended; its lane is free again
    }

    [Fact]
    public void A_bar_outside_the_span_is_left_out()
    {
        Assert.Empty(CalendarLayout.Bars([AllDay("e", Mon.AddDays(7), Mon.AddDays(8))], Mon, 7));
    }

    [Fact]
    public void A_long_timed_event_is_a_bar_over_the_days_it_touches()
    {
        var e = new CalendarEvent { Id = "e", Start = At(Mon, 18), End = At(Mon.AddDays(2), 9) };

        var bar = Assert.Single(CalendarLayout.Bars([e], Mon, 7));

        Assert.Equal(0, bar.Column);
        Assert.Equal(3, bar.Span);
    }

    // ── S2 · The month view ─────────────────────────────────────────────

    [Fact]
    public void A_single_day_timed_event_is_a_line_and_an_all_day_or_multi_day_event_is_a_bar()
    {
        var overnight = new CalendarEvent { Id = "overnight", Start = At(Mon.AddDays(3), 23), End = At(Mon.AddDays(4), 1) };

        var week = CalendarLayout.MonthWeek(
            [Timed("line", Mon, 9, 0, 10, 0), AllDay("allday", Mon.AddDays(1), Mon.AddDays(2)), overnight], Mon, 4);

        Assert.Equal("line", Assert.Single(week.Lines).Event.Id);
        Assert.Equal(["allday", "overnight"], week.Bars.Select(b => b.Event.Id).Order());
        Assert.Equal(2, week.Bars.Single(b => b.Event.Id == "overnight").Span);
    }

    [Fact]
    public void A_bar_that_crosses_the_end_of_a_week_row_continues_in_the_next_row()
    {
        var e = AllDay("e", Mon.AddDays(5), Mon.AddDays(9));

        var first = Assert.Single(CalendarLayout.MonthWeek([e], Mon, 4).Bars);
        var second = Assert.Single(CalendarLayout.MonthWeek([e], Mon.AddDays(7), 4).Bars);

        Assert.Equal((5, 2), (first.Column, first.Span));
        Assert.True(first.ContinuesAfter);
        Assert.Equal((0, 2), (second.Column, second.Span));
        Assert.True(second.ContinuesBefore);
        Assert.Same(first.Event, second.Event);
    }

    [Fact]
    public void Within_a_cell_bars_are_placed_above_lines_and_lines_are_ordered_by_Start()
    {
        var week = CalendarLayout.MonthWeek(
            [Timed("late", Mon, 15, 0, 16, 0), Timed("early", Mon, 9, 0, 10, 0), AllDay("bar", Mon, Mon.AddDays(1))], Mon, 4);

        Assert.Equal(0, Assert.Single(week.Bars).Lane);
        Assert.Equal(1, week.Lines.Single(l => l.Event.Id == "early").Lane);
        Assert.Equal(2, week.Lines.Single(l => l.Event.Id == "late").Lane);
    }

    [Fact]
    public void A_line_fills_the_lane_a_bar_leaves_free_in_its_day()
    {
        // The bar on Tuesday sits in lane 0; Monday's line may use lane 0 too.
        var week = CalendarLayout.MonthWeek(
            [AllDay("bar", Mon.AddDays(1), Mon.AddDays(2)), Timed("line", Mon, 9, 0, 10, 0)], Mon, 4);

        Assert.Equal(0, Assert.Single(week.Lines).Lane);
    }

    [Fact]
    public void A_cell_with_as_many_events_as_fit_shows_them_all()
    {
        var week = CalendarLayout.MonthWeek(
            [Timed("a", Mon, 9, 0, 10, 0), Timed("b", Mon, 10, 0, 11, 0), Timed("c", Mon, 11, 0, 12, 0)], Mon, 3);

        Assert.Equal(3, week.Lines.Count);
        Assert.Equal(0, week.Hidden[0]);
        Assert.Equal(3, week.Total[0]);
    }

    [Fact]
    public void A_cell_with_more_events_than_fit_gives_its_last_row_to_the_overflow_and_counts_the_hidden()
    {
        var week = CalendarLayout.MonthWeek(
            [Timed("a", Mon, 9, 0, 10, 0), Timed("b", Mon, 10, 0, 11, 0), Timed("c", Mon, 11, 0, 12, 0), Timed("d", Mon, 12, 0, 13, 0)], Mon, 3);

        Assert.Equal(["a", "b"], week.Lines.Select(l => l.Event.Id));
        Assert.Equal(2, week.Hidden[0]);
        Assert.Equal(4, week.Total[0]);
        Assert.Equal(0, week.Hidden[1]);
    }

    [Fact]
    public void A_bar_hidden_on_one_day_is_hidden_and_counted_on_all_its_days()
    {
        // Monday: two longer bars in lanes 0 and 1, "bar" (Monday and Tuesday) in the
        // last row, and one line too many. The overflow control takes Monday's last
        // row, so "bar" goes — and Tuesday, where it would have fitted, names it too.
        var week = CalendarLayout.MonthWeek(
        [
            AllDay("a", Mon.AddDays(-2), Mon.AddDays(1)),
            AllDay("b", Mon.AddDays(-2), Mon.AddDays(1)),
            AllDay("bar", Mon, Mon.AddDays(2)),
            Timed("extra", Mon, 9, 0, 10, 0),
            Timed("tue", Mon.AddDays(1), 9, 0, 10, 0)
        ], Mon, 3);

        Assert.Equal(["a", "b"], week.Bars.Select(b => b.Event.Id));
        Assert.Equal("tue", Assert.Single(week.Lines).Event.Id);
        Assert.Equal(4, week.Total[0]);
        Assert.Equal(2, week.Hidden[0]);
        Assert.Equal(2, week.Total[1]);
        Assert.Equal(1, week.Hidden[1]);
    }

    [Fact]
    public void Events_of_another_week_are_not_placed()
    {
        var week = CalendarLayout.MonthWeek([Timed("next", Mon.AddDays(7), 9, 0, 10, 0)], Mon, 3);

        Assert.Empty(week.Lines);
        Assert.Empty(week.Bars);
        Assert.All(week.Total, t => Assert.Equal(0, t));
    }

    // ── S2 · Colour, S3 · People ────────────────────────────────────────

    [Fact]
    public void A_person_with_Auto_is_given_a_slot_by_position_cycling_through_the_six()
    {
        var people = Enumerable.Range(0, 8).Select(i => new CalendarPerson { Id = $"p{i}", Name = $"P{i}" }).ToList();

        var colors = CalendarLayout.PersonColors(people);

        Assert.Equal(CalendarColor.Slot1, colors["p0"]);
        Assert.Equal(CalendarColor.Slot6, colors["p5"]);
        Assert.Equal(CalendarColor.Slot1, colors["p6"]);
        Assert.Equal(CalendarColor.Slot2, colors["p7"]);
    }

    [Fact]
    public void A_person_with_an_explicit_slot_uses_it_and_does_not_shift_the_others()
    {
        var colors = CalendarLayout.PersonColors(
        [
            new CalendarPerson { Id = "a", Name = "A", Color = CalendarColor.Slot5 },
            new CalendarPerson { Id = "b", Name = "B" }
        ]);

        Assert.Equal(CalendarColor.Slot5, colors["a"]);
        Assert.Equal(CalendarColor.Slot2, colors["b"]);
    }

    [Fact]
    public void An_event_with_an_explicit_slot_uses_that_slot()
    {
        var colors = CalendarLayout.PersonColors([new CalendarPerson { Id = "a", Name = "A" }]);
        var e = Timed("e", Mon, 9, 0, 10, 0, "a") with { Color = CalendarColor.Slot4 };

        Assert.Equal(CalendarColor.Slot4, CalendarLayout.EventColor(e, colors));
    }

    [Fact]
    public void An_Auto_event_uses_the_colour_of_its_first_person_that_is_in_People()
    {
        var colors = CalendarLayout.PersonColors(
            [new CalendarPerson { Id = "a", Name = "A" }, new CalendarPerson { Id = "b", Name = "B" }]);

        Assert.Equal(CalendarColor.Slot2, CalendarLayout.EventColor(Timed("e", Mon, 9, 0, 10, 0, "ghost", "b", "a"), colors));
    }

    [Fact]
    public void An_Auto_event_with_no_known_person_uses_Slot1()
    {
        var colors = CalendarLayout.PersonColors([new CalendarPerson { Id = "a", Name = "A", Color = CalendarColor.Slot3 }]);

        Assert.Equal(CalendarColor.Slot1, CalendarLayout.EventColor(Timed("e", Mon, 9, 0, 10, 0, "ghost"), colors));
        Assert.Equal(CalendarColor.Slot1, CalendarLayout.EventColor(Timed("e", Mon, 9, 0, 10, 0), colors));
    }

    [Fact]
    public void Slots_are_numbered_one_to_six()
    {
        Assert.Equal(1, CalendarLayout.SlotNumber(CalendarColor.Slot1));
        Assert.Equal(6, CalendarLayout.SlotNumber(CalendarColor.Slot6));
    }

    // ── S3 · Hidden people ──────────────────────────────────────────────

    private static readonly HashSet<string> Known = ["a", "b"];

    [Fact]
    public void An_event_none_of_whose_PersonIds_matches_a_person_is_unassigned()
    {
        Assert.True(CalendarLayout.IsUnassigned(Timed("e", Mon, 9, 0, 10, 0), Known));
        Assert.True(CalendarLayout.IsUnassigned(Timed("e", Mon, 9, 0, 10, 0, "ghost"), Known));
        Assert.False(CalendarLayout.IsUnassigned(Timed("e", Mon, 9, 0, 10, 0, "ghost", "a"), Known));
    }

    [Fact]
    public void An_event_all_of_whose_assigned_people_are_hidden_is_not_rendered()
    {
        Assert.False(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0, "a", "b"), Known, new HashSet<string> { "a", "b" }));
        Assert.False(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0, "a", "ghost"), Known, new HashSet<string> { "a" }));
    }

    [Fact]
    public void An_event_with_at_least_one_shown_person_is_rendered()
    {
        Assert.True(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0, "a", "b"), Known, new HashSet<string> { "a" }));
        Assert.True(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0, "a"), Known, null));
    }

    [Fact]
    public void An_unassigned_event_is_rendered_regardless_of_HiddenPersonIds()
    {
        var hidden = new HashSet<string> { "a", "b", "ghost" };

        Assert.True(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0), Known, hidden));
        Assert.True(CalendarLayout.IsVisible(Timed("e", Mon, 9, 0, 10, 0, "ghost"), Known, hidden));
    }

    // ── S3 · Availability ───────────────────────────────────────────────

    [Fact]
    public void On_a_day_with_a_working_span_the_time_outside_it_is_unavailable()
    {
        var spans = new[] { Span(CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17)) };

        Assert.Equal([Hours(0, 9), Hours(17, 24)], CalendarLayout.Unavailable(spans, Mon, FullDay));
        Assert.Equal([Hours(8, 9), Hours(17, 18)], CalendarLayout.Unavailable(spans, Mon, Office));
    }

    [Fact]
    public void Two_working_spans_leave_the_break_between_them_unavailable()
    {
        var spans = new[]
        {
            Span(CalendarAvailabilityKind.Working, At(Mon, 13), At(Mon, 17)),
            Span(CalendarAvailabilityKind.Working, At(Mon, 8), At(Mon, 12))
        };

        Assert.Equal([Hours(12, 13), Hours(17, 18)], CalendarLayout.Unavailable(spans, Mon, Office));
    }

    [Fact]
    public void On_a_day_without_a_working_span_nothing_is_unavailable_for_lack_of_one()
    {
        var spans = new[] { Span(CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17)) };

        Assert.Empty(CalendarLayout.Unavailable(spans, Mon.AddDays(1), FullDay));
        Assert.Empty(CalendarLayout.Unavailable([], Mon, FullDay));
    }

    [Fact]
    public void An_availability_End_is_exclusive()
    {
        // A span ending at Tuesday's midnight is not a working span of Tuesday.
        var spans = new[] { Span(CalendarAvailabilityKind.Working, At(Mon, 20), At(Mon.AddDays(1), 0)) };

        Assert.Empty(CalendarLayout.Unavailable(spans, Mon.AddDays(1), FullDay));
    }

    [Fact]
    public void Time_inside_an_absence_is_unavailable_whether_or_not_it_lies_in_a_working_span()
    {
        var spans = new[]
        {
            Span(CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17)),
            Span(CalendarAvailabilityKind.Absent, At(Mon, 11), At(Mon, 12))
        };

        Assert.Equal([Hours(8, 9), Hours(11, 12), Hours(17, 18)], CalendarLayout.Unavailable(spans, Mon, Office));
        Assert.Equal([Hours(11, 12)], CalendarLayout.Unavailable(spans.Skip(1), Mon, Office));
    }

    [Fact]
    public void An_absence_touching_unavailable_time_merges_with_it()
    {
        var spans = new[]
        {
            Span(CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17)),
            Span(CalendarAvailabilityKind.Absent, At(Mon, 15), At(Mon, 20))
        };

        Assert.Equal([Hours(8, 9), Hours(15, 18)], CalendarLayout.Unavailable(spans, Mon, Office));
    }

    [Fact]
    public void A_working_span_outside_the_grid_hours_makes_the_whole_grid_unavailable()
    {
        var spans = new[] { Span(CalendarAvailabilityKind.Working, At(Mon, 20), At(Mon, 22)) };

        Assert.Equal([Hours(8, 18)], CalendarLayout.Unavailable(spans, Mon, Office));
    }

    [Fact]
    public void A_span_that_ends_before_it_starts_is_ignored()
    {
        var spans = new[] { Span(CalendarAvailabilityKind.Absent, At(Mon, 12), At(Mon, 11)) };

        Assert.Empty(CalendarLayout.Unavailable(spans, Mon, FullDay));
        Assert.Empty(CalendarLayout.Absences(spans, Mon, FullDay));
    }

    [Fact]
    public void An_absence_keeps_its_label_and_says_when_it_covers_the_whole_visible_day()
    {
        var spans = new[]
        {
            Span(CalendarAvailabilityKind.Absent, At(Mon, 0), At(Mon.AddDays(2), 0), "Holiday"),
            Span(CalendarAvailabilityKind.Absent, At(Mon.AddDays(2), 10), At(Mon.AddDays(2), 12), "Dentist"),
            Span(CalendarAvailabilityKind.Working, At(Mon, 9), At(Mon, 17))
        };

        var monday = Assert.Single(CalendarLayout.Absences(spans, Mon, Office));
        var wednesday = Assert.Single(CalendarLayout.Absences(spans, Mon.AddDays(2), Office));

        Assert.Equal("Holiday", monday.Source.Label);
        Assert.True(monday.WholeDay);
        Assert.Equal(Hours(8, 18), monday.Span);
        Assert.Equal("Dentist", wednesday.Source.Label);
        Assert.False(wednesday.WholeDay);
        Assert.Equal(Hours(10, 12), wednesday.Span);
    }

    // ── S3 · Drawing availability ───────────────────────────────────────

    [Fact]
    public void The_overlay_mutes_a_time_only_when_every_shown_person_is_unavailable_at_it()
    {
        IReadOnlyList<CalendarSpan> a = [Hours(0, 9), Hours(17, 24)];
        IReadOnlyList<CalendarSpan> b = [Hours(0, 8), Hours(12, 13), Hours(16, 24)];

        Assert.Equal([Hours(0, 8), Hours(17, 24)], CalendarLayout.Intersect([a, b]));
    }

    [Fact]
    public void The_overlay_mutes_nothing_when_one_shown_person_has_no_unavailable_time()
    {
        IReadOnlyList<CalendarSpan> a = [Hours(0, 9)];

        Assert.Empty(CalendarLayout.Intersect([a, []]));
    }

    [Fact]
    public void The_overlay_mutes_nothing_when_no_person_is_shown()
    {
        Assert.Empty(CalendarLayout.Intersect([]));
    }

    [Fact]
    public void The_overlay_of_one_person_is_that_persons_unavailable_time()
    {
        IReadOnlyList<CalendarSpan> a = [Hours(0, 9), Hours(17, 24)];

        Assert.Equal(a, CalendarLayout.Intersect([a]));
    }

    // ── Fractions that reach markup ─────────────────────────────────────

    [Fact]
    public void Fractions_are_formatted_invariantly_under_a_German_culture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("0.5", CalendarLayout.F(0.5));
            Assert.Equal("12.5%", CalendarLayout.Pct(0.125));
            Assert.Equal("33.3333%", CalendarLayout.Pct(1d / 3));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void A_time_of_day_maps_to_a_fraction_of_the_grid()
    {
        Assert.Equal(0d, Office.Fraction(TimeSpan.FromHours(8)), 9);
        Assert.Equal(0.5, Office.Fraction(TimeSpan.FromHours(13)), 9);
        Assert.Equal(1d, Office.Fraction(TimeSpan.FromHours(18)), 9);
    }
}
