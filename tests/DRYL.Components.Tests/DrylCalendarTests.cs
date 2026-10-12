using System.Globalization;
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Tests;

/// <summary>
/// Behavioural tests for <see cref="DrylCalendar"/>, grouped by the stories of
/// <c>specs/E5 Data/F22 DrylCalendar</c>. The arithmetic behind the positions is
/// pinned in <see cref="CalendarLayoutTests"/>; these tests pin what reaches the
/// markup and what the callbacks carry.
/// </summary>
public class DrylCalendarTests : BunitContext
{
    // Monday 12 October 2026, half past ten.
    private static readonly DateOnly Mon = new(2026, 10, 12);
    private static readonly DateTime Now = new(2026, 10, 12, 10, 30, 0);
    private static readonly CultureInfo EnUs = new("en-US");
    private static readonly CultureInfo DeDe = new("de-DE");

    public DrylCalendarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // A fixed clock, culture and week start, so no test depends on the machine.
    private IRenderedComponent<DrylCalendar> RenderCalendar(
        Action<ComponentParameterCollectionBuilder<DrylCalendar>>? configure = null,
        CultureInfo? culture = null,
        DayOfWeek? firstDay = DayOfWeek.Monday,
        DateTime? now = null)
        => Render<DrylCalendar>(p =>
        {
            p.Add(c => c.Now, now ?? Now);
            p.Add(c => c.FirstDayOfWeek, firstDay);
            p.Add(c => c.Culture, culture ?? EnUs);
            configure?.Invoke(p);
        });

    private static string Title(IRenderedComponent<DrylCalendar> cut) => cut.Find(".cal-title").TextContent.Trim();

    // ── S1 · The view ───────────────────────────────────────────────────

    [Fact]
    public void View_defaults_to_Week_and_renders_seven_days_as_a_time_grid()
    {
        var cut = RenderCalendar();

        Assert.Equal(7, cut.FindAll(".cal-col").Count);
        Assert.Equal(7, cut.FindAll(".cal-day-head").Count);
        Assert.Empty(cut.FindAll(".cal-month"));
    }

    [Fact]
    public void Day_view_renders_one_day_as_a_time_grid()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Day));

        var column = Assert.Single(cut.FindAll(".cal-col"));
        Assert.Equal("2026-10-12", column.GetAttribute("data-date"));
        Assert.Empty(cut.FindAll(".cal-month"));
    }

    [Fact]
    public void Month_view_renders_a_grid_of_days_one_row_per_week()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Month));

        Assert.Equal(5, cut.FindAll(".cal-week").Count);
        Assert.Equal(35, cut.FindAll(".cal-cell").Count);
        Assert.Empty(cut.FindAll(".cal-col"));
        Assert.Empty(cut.FindAll(".cal-grid"));
    }

    // ── S1 · The visible range ──────────────────────────────────────────

    [Fact]
    public void Week_starts_on_FirstDayOfWeek_when_it_is_set()
    {
        var cut = RenderCalendar(firstDay: DayOfWeek.Sunday);

        Assert.Equal("2026-10-11", cut.FindAll(".cal-col")[0].GetAttribute("data-date"));
    }

    [Fact]
    public void Week_starts_on_the_first_day_of_the_culture_when_FirstDayOfWeek_is_null()
    {
        var german = RenderCalendar(firstDay: null, culture: DeDe);
        var american = RenderCalendar(firstDay: null, culture: EnUs);

        Assert.Equal("2026-10-12", german.FindAll(".cal-col")[0].GetAttribute("data-date"));
        Assert.Equal("2026-10-11", american.FindAll(".cal-col")[0].GetAttribute("data-date"));
    }

    [Fact]
    public void A_day_of_a_neighbouring_month_is_rendered_and_marked_as_outside()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Month));

        var cells = cut.FindAll(".cal-cell");
        Assert.Equal("2026-09-28", cells[0].GetAttribute("data-date"));
        Assert.Contains("is-outside", cells[0].ClassList);
        Assert.DoesNotContain("is-outside", cells[3].ClassList);   // 1 October
        Assert.Contains("is-outside", cells[34].ClassList);        // 1 November
    }

    [Fact]
    public void Date_defaults_to_the_date_of_Now()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Day), now: new DateTime(2026, 3, 5, 9, 0, 0));

        Assert.Equal("2026-03-05", cut.Find(".cal-col").GetAttribute("data-date"));
    }

    [Fact]
    public void OnRangeChanged_fires_once_after_the_first_render_with_the_visible_range()
    {
        var ranges = new List<CalendarRange>();

        RenderCalendar(p => p.Add(c => c.OnRangeChanged, ranges.Add));

        var range = Assert.Single(ranges);
        Assert.Equal(new DateTime(2026, 10, 12, 0, 0, 0), range.Start);
        Assert.Equal(new DateTime(2026, 10, 19, 0, 0, 0), range.End);
    }

    [Fact]
    public void OnRangeChanged_fires_when_View_Date_or_FirstDayOfWeek_changes_the_range()
    {
        var ranges = new List<CalendarRange>();
        var cut = RenderCalendar(p => p.Add(c => c.OnRangeChanged, ranges.Add).Add(c => c.Date, Mon));

        cut.Render(p => p.Add(c => c.View, CalendarView.Day));
        cut.Render(p => p.Add(c => c.Date, Mon.AddDays(1)));
        cut.Render(p => p.Add(c => c.View, CalendarView.Week).Add(c => c.FirstDayOfWeek, DayOfWeek.Sunday));

        Assert.Equal(4, ranges.Count);
        Assert.Equal(new CalendarRange(new DateTime(2026, 10, 12), new DateTime(2026, 10, 13)), ranges[1]);
        Assert.Equal(new CalendarRange(new DateTime(2026, 10, 13), new DateTime(2026, 10, 14)), ranges[2]);
        Assert.Equal(new CalendarRange(new DateTime(2026, 10, 11), new DateTime(2026, 10, 18)), ranges[3]);
    }

    [Fact]
    public void OnRangeChanged_does_not_fire_when_a_parameter_changes_and_the_range_stays_the_same()
    {
        var ranges = new List<CalendarRange>();
        var cut = RenderCalendar(p => p.Add(c => c.OnRangeChanged, ranges.Add).Add(c => c.Date, Mon));

        cut.Render(p => p.Add(c => c.Loading, true));
        cut.Render(p => p.Add(c => c.Date, Mon.AddDays(3)));   // same week
        cut.Render(p => p.Add(c => c.SlotMinutes, 15));

        Assert.Single(ranges);
    }

    // ── S1 · The toolbar ────────────────────────────────────────────────

    [Fact]
    public void ShowToolbar_left_true_renders_the_toolbar_above_the_grid_with_its_controls()
    {
        var cut = RenderCalendar(p => p.Add(c => c.ToolbarContent, "<span class=\"extra\">Extra</span>"));

        var toolbar = cut.Find(".cal-toolbar");
        Assert.Contains("cal-toolbar", cut.Find(".cal-root").FirstElementChild!.ClassList);
        Assert.NotNull(toolbar.QuerySelector("button[aria-label='Previous']"));
        Assert.NotNull(toolbar.QuerySelector("button[aria-label='Next']"));
        Assert.Equal("Today", toolbar.QuerySelector("button.cal-today")!.TextContent.Trim());
        Assert.Equal(["Day", "Week", "Month"], toolbar.QuerySelectorAll("[role='radio']").Select(r => r.TextContent.Trim()));
        Assert.Equal("Extra", toolbar.QuerySelector(".cal-toolbar-end .extra")!.TextContent);
        Assert.Contains("extra", toolbar.QuerySelector(".cal-toolbar-end")!.LastElementChild!.ClassList);
    }

    [Fact]
    public void ShowToolbar_set_to_false_renders_no_toolbar_element_at_all()
    {
        var cut = RenderCalendar(p => p.Add(c => c.ShowToolbar, false));

        Assert.Empty(cut.FindAll(".cal-toolbar"));
        Assert.Equal(7, cut.FindAll(".cal-col").Count);
    }

    [Fact]
    public void The_previous_and_next_controls_are_icon_only_inside_a_tooltip_of_the_same_text()
    {
        var cut = RenderCalendar();

        foreach (var name in new[] { "Previous", "Next" })
        {
            var button = cut.Find($"button[aria-label='{name}']");
            Assert.Equal("", button.TextContent.Trim());
            Assert.Equal(name, button.Closest(".tt-wrap")!.GetAttribute("data-tt"));
        }
    }

    [Fact]
    public void The_title_is_formatted_with_Culture_in_English()
    {
        Assert.Equal("Monday, October 12, 2026", Title(RenderCalendar(p => p.Add(c => c.View, CalendarView.Day))));
        Assert.Equal("October 12 – October 18, 2026", Title(RenderCalendar()));
        Assert.Equal("October 2026", Title(RenderCalendar(p => p.Add(c => c.View, CalendarView.Month))));
    }

    [Fact]
    public void The_title_is_formatted_with_Culture_in_German()
    {
        Assert.Equal("Montag, 12. Oktober 2026", Title(RenderCalendar(p => p.Add(c => c.View, CalendarView.Day), culture: DeDe)));
        Assert.Equal("12. Oktober – 18. Oktober 2026", Title(RenderCalendar(culture: DeDe)));
        Assert.Equal("Oktober 2026", Title(RenderCalendar(p => p.Add(c => c.View, CalendarView.Month), culture: DeDe)));
    }

    // ── S1 · Navigating ─────────────────────────────────────────────────

    [Theory]
    [InlineData(CalendarView.Day, "2026-10-13", "2026-10-11")]
    [InlineData(CalendarView.Week, "2026-10-19", "2026-10-05")]
    [InlineData(CalendarView.Month, "2026-11-12", "2026-09-12")]
    public void Previous_and_next_move_Date_by_one_range_and_fire_DateChanged(CalendarView view, string next, string previous)
    {
        var dates = new List<DateOnly>();
        var cut = RenderCalendar(p => p.Add(c => c.View, view).Add(c => c.DateChanged, dates.Add));

        cut.Find("button[aria-label='Next']").Click();
        cut.Find("button[aria-label='Previous']").Click();
        cut.Find("button[aria-label='Previous']").Click();

        Assert.Equal([DateOnly.Parse(next), Mon, DateOnly.Parse(previous)], dates);
    }

    [Fact]
    public void A_month_move_from_a_day_the_target_month_lacks_lands_on_its_last_day()
    {
        var dates = new List<DateOnly>();
        var cut = RenderCalendar(p => p
            .Add(c => c.View, CalendarView.Month)
            .Add(c => c.Date, new DateOnly(2026, 1, 31))
            .Add(c => c.DateChanged, dates.Add));

        cut.Find("button[aria-label='Next']").Click();

        Assert.Equal(new DateOnly(2026, 2, 28), Assert.Single(dates));
        Assert.Equal("February 2026", Title(cut));
    }

    [Fact]
    public void The_calendar_shows_the_new_range_after_a_navigation_when_Date_is_not_bound()
    {
        var ranges = new List<CalendarRange>();
        var cut = RenderCalendar(p => p.Add(c => c.OnRangeChanged, ranges.Add));

        cut.Find("button[aria-label='Next']").Click();

        Assert.Equal("2026-10-19", cut.FindAll(".cal-col")[0].GetAttribute("data-date"));
        Assert.Equal("October 19 – October 25, 2026", Title(cut));
        Assert.Equal(new DateTime(2026, 10, 19), ranges[^1].Start);
    }

    [Fact]
    public void The_today_control_sets_Date_to_the_date_of_Now()
    {
        var dates = new List<DateOnly>();
        var cut = RenderCalendar(p => p.Add(c => c.Date, new DateOnly(2026, 7, 1)).Add(c => c.DateChanged, dates.Add));

        cut.Find("button.cal-today").Click();

        Assert.Equal(Mon, Assert.Single(dates));
        Assert.Equal("2026-10-12", cut.FindAll(".cal-col")[0].GetAttribute("data-date"));
    }

    [Fact]
    public void Selecting_a_view_fires_ViewChanged_and_shows_it_when_View_is_not_bound()
    {
        var views = new List<CalendarView>();
        var cut = RenderCalendar(p => p.Add(c => c.ViewChanged, views.Add));

        cut.FindAll("[role='radio']").Single(r => r.TextContent.Trim() == "Month").Click();

        Assert.Equal(CalendarView.Month, Assert.Single(views));
        Assert.NotEmpty(cut.FindAll(".cal-month"));
        Assert.Equal("true", cut.FindAll("[role='radio']").Single(r => r.TextContent.Trim() == "Month").GetAttribute("aria-checked"));
    }

    [Fact]
    public void A_changed_View_parameter_is_adopted()
    {
        var cut = RenderCalendar();

        cut.Render(p => p.Add(c => c.View, CalendarView.Month));

        Assert.NotEmpty(cut.FindAll(".cal-month"));
    }

    [Fact]
    public void Activating_a_day_header_in_the_week_view_switches_to_the_day_view_of_that_day()
    {
        var dates = new List<DateOnly>();
        var views = new List<CalendarView>();
        var cut = RenderCalendar(p => p.Add(c => c.DateChanged, dates.Add).Add(c => c.ViewChanged, views.Add));

        cut.Find("button.cal-day-head[data-date='2026-10-15']").Click();

        Assert.Equal(new DateOnly(2026, 10, 15), Assert.Single(dates));
        Assert.Equal(CalendarView.Day, Assert.Single(views));
        Assert.Equal("2026-10-15", Assert.Single(cut.FindAll(".cal-col")).GetAttribute("data-date"));
    }

    [Fact]
    public void A_day_header_in_the_day_view_is_not_a_control()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Day));

        Assert.Equal("DIV", cut.Find(".cal-day-head").TagName);
    }

    [Fact]
    public void Activating_a_day_number_in_the_month_view_switches_to_the_day_view_of_that_day()
    {
        var dates = new List<DateOnly>();
        var views = new List<CalendarView>();
        var cut = RenderCalendar(p => p
            .Add(c => c.View, CalendarView.Month)
            .Add(c => c.DateChanged, dates.Add)
            .Add(c => c.ViewChanged, views.Add));

        cut.Find(".cal-cell[data-date='2026-10-20'] .cal-cell-num").Click();

        Assert.Equal(new DateOnly(2026, 10, 20), Assert.Single(dates));
        Assert.Equal(CalendarView.Day, Assert.Single(views));
        Assert.Equal("2026-10-20", Assert.Single(cut.FindAll(".cal-col")).GetAttribute("data-date"));
    }

    // ── S1 · The time grid ──────────────────────────────────────────────

    [Fact]
    public void The_grid_shows_the_whole_day_by_default_and_labels_every_full_hour()
    {
        var cut = RenderCalendar();

        var hours = cut.FindAll(".cal-hour");
        Assert.Equal(24, hours.Count);
        Assert.Contains("--cal-hours:24", cut.Find(".cal-time").GetAttribute("style"));
        Assert.Equal("top:0%", hours[0].GetAttribute("style"));
        Assert.Equal("top:50%", hours[12].GetAttribute("style"));
    }

    [Fact]
    public void The_grid_starts_at_DayStart_and_ends_at_DayEnd()
    {
        var cut = RenderCalendar(p => p.Add(c => c.DayStart, TimeSpan.FromHours(8)).Add(c => c.DayEnd, TimeSpan.FromHours(18)));

        var hours = cut.FindAll(".cal-hour");
        Assert.Equal(10, hours.Count);
        Assert.Equal("8:00 AM", hours[0].TextContent.Trim().Replace(' ', ' '));
        Assert.Equal("5:00 PM", hours[^1].TextContent.Trim().Replace(' ', ' '));
        Assert.Contains("--cal-hours:10", cut.Find(".cal-time").GetAttribute("style"));
    }

    [Fact]
    public void Hour_labels_follow_a_twenty_four_hour_culture()
    {
        var cut = RenderCalendar(culture: DeDe);

        Assert.Equal("00:00", cut.FindAll(".cal-hour")[0].TextContent.Trim());
        Assert.Equal("13:00", cut.FindAll(".cal-hour")[13].TextContent.Trim());
    }

    [Fact]
    public void The_grid_is_divided_into_slots_and_the_hours_read_first()
    {
        var cut = RenderCalendar();

        Assert.Equal(47, cut.FindAll(".cal-line").Count);            // 48 slots, the top edge is the header's
        Assert.Equal(23, cut.FindAll(".cal-line--hour").Count);
        Assert.Equal("true", cut.Find(".cal-lines").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void SlotMinutes_sets_the_slot_length()
    {
        var cut = RenderCalendar(p => p.Add(c => c.SlotMinutes, 15));

        Assert.Equal(95, cut.FindAll(".cal-line").Count);
        Assert.Equal(23, cut.FindAll(".cal-line--hour").Count);
    }

    [Fact]
    public void A_replaced_DayEnd_or_SlotMinutes_is_reported_once_on_the_console()
    {
        var original = Console.Out;
        var console = new StringWriter();
        Console.SetOut(console);
        try
        {
            var cut = RenderCalendar(p => p
                .Add(c => c.DayStart, TimeSpan.FromHours(9))
                .Add(c => c.DayEnd, TimeSpan.FromHours(9))
                .Add(c => c.SlotMinutes, 7));
            cut.Render(p => p.Add(c => c.Loading, true));
            cut.Render(p => p.Add(c => c.Loading, false));

            Assert.Equal(15, cut.FindAll(".cal-hour").Count);             // 9:00 to the following midnight
            Assert.Equal(29, cut.FindAll(".cal-line").Count);             // thirty-minute slots
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = console.ToString();
        Assert.Equal(1, Occurrences(output, "DrylCalendar: DayEnd"));
        Assert.Equal(1, Occurrences(output, "DrylCalendar: SlotMinutes"));
    }

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    [Fact]
    public void Height_gives_the_calendar_that_height_and_a_scrolling_grid()
    {
        var cut = RenderCalendar(p => p.Add(c => c.Height, "480px"));

        var root = cut.Find(".cal-root");
        Assert.Equal("height: 480px", root.GetAttribute("style"));
        Assert.Contains("cal-root--fixed", root.ClassList);
        // The headers and the all-day row sit inside the scroller as its sticky head.
        Assert.NotNull(cut.Find(".cal-scroll .cal-head .cal-allday"));
    }

    [Fact]
    public void The_all_day_row_is_rendered_empty_and_carries_its_label()
    {
        var cut = RenderCalendar();

        Assert.Equal("All day", cut.Find(".cal-allday-label").TextContent.Trim());
        Assert.Equal(7, cut.FindAll(".cal-allday-cell").Count);
    }

    // ── S1 · Today and the current time ─────────────────────────────────

    [Fact]
    public void Today_is_marked_in_the_week_view_and_carries_aria_current()
    {
        var cut = RenderCalendar();

        var today = Assert.Single(cut.FindAll(".cal-day-head.is-today"));
        Assert.Equal("2026-10-12", today.GetAttribute("data-date"));
        Assert.Equal("date", today.GetAttribute("aria-current"));
        Assert.Single(cut.FindAll("[aria-current='date']"));
        Assert.Single(cut.FindAll(".cal-col.is-today"));
    }

    [Fact]
    public void Today_is_marked_in_the_month_view_and_carries_aria_current()
    {
        var cut = RenderCalendar(p => p.Add(c => c.View, CalendarView.Month));

        var today = Assert.Single(cut.FindAll(".cal-cell.is-today"));
        Assert.Equal("2026-10-12", today.GetAttribute("data-date"));
        Assert.Equal("date", today.GetAttribute("aria-current"));
    }

    [Fact]
    public void The_current_time_line_is_drawn_across_todays_column_at_the_time_of_Now()
    {
        var cut = RenderCalendar();

        var line = Assert.Single(cut.FindAll(".cal-now"));
        Assert.Equal("top:43.75%", line.GetAttribute("style"));   // 10:30 of 24 hours
        Assert.Equal("true", line.GetAttribute("aria-hidden"));
        Assert.Contains("is-today", line.ParentElement!.ClassList);
    }

    [Fact]
    public void No_current_time_line_is_drawn_when_today_is_outside_the_visible_range()
    {
        var cut = RenderCalendar(p => p.Add(c => c.Date, Mon.AddDays(7)));

        Assert.Empty(cut.FindAll(".cal-now"));
        Assert.Empty(cut.FindAll(".is-today"));
    }

    [Fact]
    public void No_current_time_line_is_drawn_when_the_time_of_Now_is_outside_the_grid()
    {
        var cut = RenderCalendar(p => p.Add(c => c.DayStart, TimeSpan.FromHours(11)).Add(c => c.DayEnd, TimeSpan.FromHours(18)));

        Assert.Empty(cut.FindAll(".cal-now"));
        Assert.Single(cut.FindAll(".cal-col.is-today"));
    }

    [Fact]
    public void With_Now_set_the_line_follows_the_parameter_and_no_timer_runs()
    {
        var cut = RenderCalendar();

        Assert.False(cut.Instance.ClockRunning);

        cut.Render(p => p.Add(c => c.Now, Now.AddHours(1.5)));

        Assert.Equal("top:50%", cut.Find(".cal-now").GetAttribute("style"));
    }

    [Fact]
    public void With_Now_left_null_a_timer_advances_the_line_and_is_disposed_with_the_component()
    {
        var cut = Render<DrylCalendar>(p => p.Add(c => c.View, CalendarView.Day));

        Assert.True(cut.Instance.ClockRunning);
        Assert.Single(cut.FindAll(".cal-now"));   // the whole day is shown, so the server's time is on the grid

        cut.Render(p => p.Add(c => c.Now, Now));
        Assert.False(cut.Instance.ClockRunning);

        cut.Render(p => p.Add(c => c.Now, (DateTime?)null));
        Assert.True(cut.Instance.ClockRunning);

        cut.Instance.Dispose();
        Assert.False(cut.Instance.ClockRunning);
    }

    // ── S1 · Loading ────────────────────────────────────────────────────

    [Fact]
    public void Loading_marks_the_calendar_as_busy_and_renders_a_status_in_the_toolbar()
    {
        var cut = RenderCalendar(p => p.Add(c => c.Loading, true));

        Assert.Equal("true", cut.Find(".cal-root").GetAttribute("aria-busy"));
        var status = cut.Find(".cal-toolbar [role='status']");
        Assert.Equal("Loading", status.GetAttribute("aria-label"));
        Assert.Equal(7, cut.FindAll(".cal-col").Count);   // the grid stays in place
    }

    [Fact]
    public void Without_Loading_the_calendar_is_not_busy()
    {
        var cut = RenderCalendar();

        Assert.False(cut.Find(".cal-root").HasAttribute("aria-busy"));
        Assert.Empty(cut.FindAll(".cal-toolbar [role='status']"));
    }

    // ── S4 · The surface, labels ────────────────────────────────────────

    [Fact]
    public void The_calendar_renders_on_the_card_surface()
    {
        var cut = RenderCalendar();

        Assert.Contains("glass-card", cut.Find(".cal-root").ClassList);
    }

    [Fact]
    public void Class_is_merged_onto_the_roots_own_classes()
    {
        var cut = RenderCalendar(p => p.Add(c => c.Class, "my-calendar wide"));

        var classes = cut.Find(".cal-root").ClassList;
        Assert.Contains("glass-card", classes);
        Assert.Contains("my-calendar", classes);
        Assert.Contains("wide", classes);
    }

    [Fact]
    public void A_splatted_class_binds_Class_and_does_not_clobber_the_roots_classes()
    {
        var cut = RenderCalendar(p => p.AddUnmatched("class", "splatted"));

        var classes = cut.Find(".cal-root").ClassList;
        Assert.Contains("cal-root", classes);
        Assert.Contains("splatted", classes);
    }

    [Fact]
    public void AdditionalAttributes_are_applied_to_the_root()
    {
        var cut = RenderCalendar(p => p.AddUnmatched("data-testid", "team").AddUnmatched("id", "cal"));

        var root = cut.Find(".cal-root");
        Assert.Equal("team", root.GetAttribute("data-testid"));
        Assert.Equal("cal", root.GetAttribute("id"));
    }

    [Fact]
    public void The_calendar_carries_AriaLabel_as_its_accessible_label()
    {
        var cut = RenderCalendar(p => p.Add(c => c.AriaLabel, "Team calendar"));

        Assert.Equal("Team calendar", cut.Find(".cal-root").GetAttribute("aria-label"));
        Assert.Equal("group", cut.Find(".cal-root").GetAttribute("role"));
    }

    [Fact]
    public void An_overridden_label_replaces_its_text_everywhere_it_is_used()
    {
        var labels = new CalendarLabels
        {
            Today = "Heute", Previous = "Zurück", Next = "Weiter",
            Day = "Tag", Week = "Woche", Month = "Monat", AllDay = "Ganztägig", Loading = "Lädt"
        };

        var cut = RenderCalendar(p => p.Add(c => c.Labels, labels).Add(c => c.Loading, true));

        Assert.Equal("Heute", cut.Find("button.cal-today").TextContent.Trim());
        Assert.Equal("Zurück", cut.Find("button[aria-label='Zurück']").Closest(".tt-wrap")!.GetAttribute("data-tt"));
        Assert.Equal("Weiter", cut.Find("button[aria-label='Weiter']").Closest(".tt-wrap")!.GetAttribute("data-tt"));
        Assert.Equal(["Tag", "Woche", "Monat"], cut.FindAll("[role='radio']").Select(r => r.TextContent.Trim()));
        Assert.Equal("Ganztägig", cut.Find(".cal-allday-label").TextContent.Trim());
        Assert.Equal("Lädt", cut.Find(".cal-toolbar [role='status']").GetAttribute("aria-label"));
    }

    [Fact]
    public void Day_names_follow_Culture_not_Labels()
    {
        var cut = RenderCalendar(culture: DeDe);

        Assert.StartsWith("Mo", cut.FindAll(".cal-day-name")[0].TextContent.Trim());
        Assert.Equal("Montag, 12. Oktober 2026", cut.FindAll(".cal-day-head")[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void The_title_sits_in_a_polite_live_region()
    {
        var cut = RenderCalendar();

        Assert.Equal("polite", cut.Find(".cal-title").GetAttribute("aria-live"));
    }
}
