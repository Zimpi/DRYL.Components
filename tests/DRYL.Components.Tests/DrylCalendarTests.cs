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

    // ── S2 · Events ─────────────────────────────────────────────────────

    private static readonly CalendarPerson Ada = new() { Id = "ada", Name = "Ada Lovelace" };
    private static readonly CalendarPerson Bob = new() { Id = "bob", Name = "Bob Ross" };
    private static readonly CalendarPerson Cy = new() { Id = "cy", Name = "Cy Twombly", Color = CalendarColor.Slot5 };

    private static DateTime At(DateOnly day, int hour, int minute = 0) => day.ToDateTime(new TimeOnly(hour, minute));

    private static CalendarEvent Timed(string id, DateOnly day, int fromHour, int toHour, params string[] people)
        => new() { Id = id, Title = id, Start = At(day, fromHour), End = At(day, toHour), PersonIds = people };

    private static CalendarEvent AllDay(string id, DateOnly from, DateOnly endExclusive)
        => new() { Id = id, Title = id, AllDay = true, Start = from.ToDateTime(TimeOnly.MinValue), End = endExclusive.ToDateTime(TimeOnly.MinValue) };

    private IRenderedComponent<DrylCalendar> RenderEvents(
        IEnumerable<CalendarEvent> events,
        Action<ComponentParameterCollectionBuilder<DrylCalendar>>? configure = null)
        => RenderCalendar(p =>
        {
            p.Add(c => c.Events, events);
            p.Add(c => c.People, new[] { Ada, Bob, Cy });
            configure?.Invoke(p);
        });

    private static string Text(AngleSharp.Dom.IElement element) => element.TextContent.Trim().Replace(' ', ' ');

    [Fact]
    public void A_timed_event_is_drawn_in_the_column_of_its_day_from_its_Start_to_its_End()
    {
        var cut = RenderEvents([Timed("standup", Mon.AddDays(2), 9, 12)]);

        var box = Assert.Single(cut.FindAll(".cal-event"));
        Assert.Equal("2026-10-14", box.ParentElement!.GetAttribute("data-date"));
        Assert.Equal("top:37.5%;height:12.5%;left:0%;width:100%", box.GetAttribute("style"));
        Assert.Contains("cal-event--timed", box.ClassList);
    }

    [Fact]
    public void An_event_whose_End_is_not_later_than_its_Start_is_drawn_one_slot_long()
    {
        var cut = RenderEvents([Timed("point", Mon, 12, 12)]);

        Assert.Equal("top:50%;height:2.0833%;left:0%;width:100%", cut.Find(".cal-event").GetAttribute("style"));
    }

    [Fact]
    public void An_event_wholly_outside_the_visible_range_is_not_rendered()
    {
        var cut = RenderEvents([Timed("next-week", Mon.AddDays(7), 9, 10), AllDay("last-week", Mon.AddDays(-3), Mon.AddDays(-1))]);

        Assert.Empty(cut.FindAll(".cal-event"));
    }

    [Fact]
    public void Of_two_events_with_the_same_Id_the_later_one_is_not_rendered_and_it_is_reported_once()
    {
        var original = Console.Out;
        var console = new StringWriter();
        Console.SetOut(console);
        IRenderedComponent<DrylCalendar> cut;
        try
        {
            cut = RenderEvents([Timed("dup", Mon, 9, 10), Timed("dup", Mon, 14, 15)]);
            cut.Render(p => p.Add(c => c.Loading, true));
        }
        finally
        {
            Console.SetOut(original);
        }

        var box = Assert.Single(cut.FindAll(".cal-event"));
        Assert.StartsWith("top:37.5%", box.GetAttribute("style"));
        Assert.Equal(1, Occurrences(console.ToString(), "DrylCalendar: two events share an Id"));
    }

    [Fact]
    public void An_event_crossing_midnight_is_one_segment_in_each_day_marked_as_continuing()
    {
        var late = new CalendarEvent { Id = "late", Title = "Late shift", Start = At(Mon, 22), End = At(Mon.AddDays(1), 2) };
        var clicked = new List<CalendarEvent>();
        var cut = RenderEvents([late], p => p.Add(c => c.OnEventClick, clicked.Add));

        var segments = cut.FindAll(".cal-event");
        Assert.Equal(2, segments.Count);
        Assert.Equal("2026-10-12", segments[0].ParentElement!.GetAttribute("data-date"));
        Assert.Contains("is-continued-after", segments[0].ClassList);
        Assert.DoesNotContain("is-continued-before", segments[0].ClassList);
        Assert.Equal("2026-10-13", segments[1].ParentElement!.GetAttribute("data-date"));
        Assert.Contains("is-continued-before", segments[1].ClassList);

        cut.FindAll(".cal-event")[0].Click();
        cut.FindAll(".cal-event")[1].Click();

        Assert.Equal(2, clicked.Count);
        Assert.All(clicked, e => Assert.Same(late, e));
        // A segment's name states the whole span, so it says where it continues.
        Assert.Equal(
            "Late shift, Monday, October 12, 2026 10:00 PM – Tuesday, October 13, 2026 2:00 AM",
            cut.FindAll(".cal-event")[1].GetAttribute("aria-label")!.Replace(' ', ' '));
    }

    [Fact]
    public void A_timed_event_of_a_day_or_longer_is_drawn_in_the_all_day_row_instead_of_the_grid()
    {
        var cut = RenderEvents([new CalendarEvent { Id = "trip", Title = "Trip", Start = At(Mon, 9), End = At(Mon.AddDays(2), 9) }]);

        var bar = Assert.Single(cut.FindAll(".cal-event"));
        Assert.Contains("cal-event--bar", bar.ClassList);
        Assert.Contains("cal-allday", bar.ParentElement!.ClassList);
        Assert.Equal("grid-column:2 / span 3;grid-row:1", bar.GetAttribute("style"));
    }

    [Fact]
    public void Overlapping_events_of_one_column_are_drawn_side_by_side()
    {
        var cut = RenderEvents([Timed("a", Mon, 9, 11), Timed("b", Mon, 10, 12), Timed("alone", Mon, 14, 15)]);

        Assert.EndsWith("left:0%;width:50%", cut.Find("[data-event-id='a']").GetAttribute("style"));
        Assert.EndsWith("left:50%;width:50%", cut.Find("[data-event-id='b']").GetAttribute("style"));
        Assert.EndsWith("left:0%;width:100%", cut.Find("[data-event-id='alone']").GetAttribute("style"));
    }

    [Fact]
    public void An_all_day_event_is_a_bar_spanning_its_days_in_the_all_day_row()
    {
        var cut = RenderEvents([AllDay("fair", Mon.AddDays(1), Mon.AddDays(4))]);

        var bar = cut.Find(".cal-allday .cal-event--bar");
        Assert.Equal("grid-column:3 / span 3;grid-row:1", bar.GetAttribute("style"));
        Assert.DoesNotContain("is-continued-before", bar.ClassList);
        Assert.DoesNotContain("is-continued-after", bar.ClassList);
        Assert.Equal("fair, Tuesday, October 13, 2026 – Thursday, October 15, 2026, All day", bar.GetAttribute("aria-label"));
    }

    [Fact]
    public void A_bar_that_leaves_the_visible_range_is_marked_as_continuing_at_that_edge()
    {
        var cut = RenderEvents([AllDay("before", Mon.AddDays(-2), Mon.AddDays(1)), AllDay("after", Mon.AddDays(6), Mon.AddDays(9))]);

        Assert.Contains("is-continued-before", cut.Find("[data-event-id='before']").ClassList);
        Assert.DoesNotContain("is-continued-after", cut.Find("[data-event-id='before']").ClassList);
        Assert.Contains("is-continued-after", cut.Find("[data-event-id='after']").ClassList);
    }

    [Fact]
    public void Bars_that_share_a_day_are_stacked_in_lanes_and_the_row_grows_to_fit_them()
    {
        var cut = RenderEvents([AllDay("a", Mon, Mon.AddDays(3)), AllDay("b", Mon.AddDays(2), Mon.AddDays(4))]);

        Assert.EndsWith("grid-row:1", cut.Find("[data-event-id='a']").GetAttribute("style"));
        Assert.EndsWith("grid-row:2", cut.Find("[data-event-id='b']").GetAttribute("style"));
        Assert.Equal("--cal-lanes:2", cut.Find(".cal-allday").GetAttribute("style"));
    }

    [Fact]
    public void The_all_day_row_is_rendered_empty_when_no_event_needs_it()
    {
        var cut = RenderEvents([Timed("a", Mon, 9, 10)]);

        Assert.Equal("--cal-lanes:1", cut.Find(".cal-allday").GetAttribute("style"));
        Assert.Empty(cut.FindAll(".cal-allday .cal-event"));
    }

    // ── S2 · The month view ─────────────────────────────────────────────

    private IRenderedComponent<DrylCalendar> RenderMonth(
        IEnumerable<CalendarEvent> events,
        Action<ComponentParameterCollectionBuilder<DrylCalendar>>? configure = null)
        => RenderEvents(events, p =>
        {
            p.Add(c => c.View, CalendarView.Month);
            configure?.Invoke(p);
        });

    [Fact]
    public void A_single_day_timed_event_is_a_line_with_its_start_time_before_its_title()
    {
        var cut = RenderMonth([Timed("review", Mon, 9, 10)]);

        var line = Assert.Single(cut.FindAll(".cal-event"));
        Assert.Contains("cal-event--line", line.ClassList);
        Assert.Equal("grid-column:1 / span 1;grid-row:2", line.GetAttribute("style"));
        Assert.Equal("2026-10-12", line.ParentElement!.QuerySelector(".cal-cell")!.GetAttribute("data-date"));
        var children = line.Children;
        Assert.Contains("cal-event-time", children[0].ClassList);
        Assert.Equal("9:00 AM", Text(children[0]));
        Assert.Equal("review", Text(children[1]));
    }

    [Fact]
    public void An_all_day_or_multi_day_event_is_a_bar_that_continues_in_the_next_week_row()
    {
        var cut = RenderMonth([AllDay("fair", Mon.AddDays(5), Mon.AddDays(9))]);   // Saturday to Tuesday

        var bars = cut.FindAll(".cal-event--bar");
        Assert.Equal(2, bars.Count);
        Assert.Equal("grid-column:6 / span 2;grid-row:2", bars[0].GetAttribute("style"));
        Assert.Contains("is-continued-after", bars[0].ClassList);
        Assert.Equal("grid-column:1 / span 2;grid-row:2", bars[1].GetAttribute("style"));
        Assert.Contains("is-continued-before", bars[1].ClassList);
    }

    [Fact]
    public void Within_a_cell_bars_are_placed_above_lines_and_lines_are_ordered_by_Start()
    {
        var cut = RenderMonth([Timed("late", Mon, 15, 16), Timed("early", Mon, 9, 10), AllDay("bar", Mon, Mon.AddDays(1))]);

        Assert.EndsWith("grid-row:2", cut.Find("[data-event-id='bar']").GetAttribute("style"));
        Assert.EndsWith("grid-row:3", cut.Find("[data-event-id='early']").GetAttribute("style"));
        Assert.EndsWith("grid-row:4", cut.Find("[data-event-id='late']").GetAttribute("style"));
        Assert.Equal(["bar", "early", "late"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));
        Assert.Empty(cut.FindAll(".cal-more"));
    }

    [Fact]
    public void A_cell_with_more_events_than_fit_renders_an_overflow_control_naming_how_many_are_hidden()
    {
        var cut = RenderMonth([Timed("a", Mon, 9, 10), Timed("b", Mon, 10, 11), Timed("c", Mon, 11, 12), Timed("d", Mon, 12, 13)]);

        Assert.Equal(["a", "b"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));
        var more = Assert.Single(cut.FindAll(".cal-more"));
        Assert.Equal("+2 more", more.TextContent.Trim());
        Assert.Equal("grid-column:1 / span 1;grid-row:4", more.GetAttribute("style"));
    }

    [Fact]
    public void The_overflow_control_uses_the_More_label()
    {
        var cut = RenderMonth(
            [Timed("a", Mon, 9, 10), Timed("b", Mon, 10, 11), Timed("c", Mon, 11, 12), Timed("d", Mon, 12, 13)],
            p => p.Add(c => c.Labels, new CalendarLabels { More = "{0} weitere" }));

        Assert.Equal("2 weitere", cut.Find(".cal-more").TextContent.Trim());
    }

    [Fact]
    public void Activating_the_overflow_control_switches_to_the_day_view_of_that_day()
    {
        var dates = new List<DateOnly>();
        var views = new List<CalendarView>();
        var cut = RenderMonth(
            [Timed("a", Mon.AddDays(3), 9, 10), Timed("b", Mon.AddDays(3), 10, 11), Timed("c", Mon.AddDays(3), 11, 12), Timed("d", Mon.AddDays(3), 12, 13)],
            p => p.Add(c => c.DateChanged, dates.Add).Add(c => c.ViewChanged, views.Add));

        cut.Find(".cal-more").Click();

        Assert.Equal(Mon.AddDays(3), Assert.Single(dates));
        Assert.Equal(CalendarView.Day, Assert.Single(views));
        Assert.Equal(4, cut.FindAll(".cal-event--timed").Count);
    }

    // ── S2 · What an event shows ────────────────────────────────────────

    [Fact]
    public void A_timed_event_renders_its_title_and_its_start_and_end_time()
    {
        var cut = RenderEvents([new CalendarEvent { Id = "e", Title = "Design review", Start = At(Mon, 9), End = At(Mon, 10, 30) }]);

        var box = cut.Find(".cal-event");
        Assert.Equal("Design review", Text(box.QuerySelector(".cal-event-title")!));
        Assert.Equal("9:00 AM – 10:30 AM", Text(box.QuerySelector(".cal-event-time")!));
        Assert.Null(box.QuerySelector(".cal-event-recurring"));
        Assert.Null(box.QuerySelector(".cal-event-people"));
    }

    [Fact]
    public void Times_are_formatted_with_Culture()
    {
        var cut = RenderCalendar(
            p => p.Add(c => c.Events, new[] { Timed("e", Mon, 9, 17) }),
            culture: DeDe);

        Assert.Equal("09:00 – 17:00", Text(cut.Find(".cal-event-time")));
    }

    [Fact]
    public void An_event_renders_its_people_as_avatars_in_the_order_of_PersonIds()
    {
        var cut = RenderEvents([Timed("e", Mon, 9, 10, "bob", "ghost", "ada")]);

        var avatars = cut.FindAll(".cal-event-people .avatar");
        Assert.Equal(2, avatars.Count);
        Assert.Equal("BR", avatars[0].TextContent.Trim());
        Assert.Equal("AL", avatars[1].TextContent.Trim());
    }

    [Fact]
    public void An_event_with_a_SeriesId_renders_a_recurring_mark_with_its_accessible_name()
    {
        var cut = RenderEvents(
        [
            Timed("mon", Mon, 9, 10) with { SeriesId = "weekly" },
            Timed("tue", Mon.AddDays(1), 9, 10) with { SeriesId = "weekly" },
            Timed("single", Mon.AddDays(2), 9, 10)
        ], p => p.Add(c => c.Labels, new CalendarLabels { Recurring = "Wiederkehrend" }));

        // Two occurrences are two independent events; nothing is derived from the series but the mark.
        Assert.Equal(3, cut.FindAll(".cal-event").Count);
        var marks = cut.FindAll(".cal-event-recurring svg");
        Assert.Equal(2, marks.Count);
        Assert.All(marks, m => Assert.Equal("Wiederkehrend", m.GetAttribute("aria-label")));
        Assert.EndsWith(", Wiederkehrend", cut.Find("[data-event-id='mon']").GetAttribute("aria-label"));
    }

    [Fact]
    public void EventTemplate_replaces_the_content_but_not_the_box()
    {
        var events = new[] { Timed("e", Mon, 9, 10, "cy") with { SeriesId = "s" } };
        var plain = RenderEvents(events);
        var templated = RenderEvents(events, p => p.Add(c => c.EventTemplate, e => $"<em class=\"mine\">{e.Id}!</em>"));

        var box = templated.Find(".cal-event");
        Assert.Equal("e!", box.QuerySelector(".mine")!.TextContent);
        Assert.Null(box.QuerySelector(".cal-event-title"));
        Assert.Null(box.QuerySelector(".cal-event-time"));
        Assert.Null(box.QuerySelector(".cal-event-people"));
        Assert.Null(box.QuerySelector(".cal-event-recurring"));

        var original = plain.Find(".cal-event");
        Assert.Equal("BUTTON", box.TagName);
        Assert.Equal(original.GetAttribute("style"), box.GetAttribute("style"));
        Assert.Equal(original.GetAttribute("data-color"), box.GetAttribute("data-color"));
        Assert.Equal(original.GetAttribute("class"), box.GetAttribute("class"));
        Assert.Equal(original.GetAttribute("aria-label"), box.GetAttribute("aria-label"));
    }

    [Fact]
    public void An_events_accessible_name_states_title_day_times_people_and_that_it_recurs()
    {
        var cut = RenderEvents([new CalendarEvent
        {
            Id = "e", Title = "Design review", Start = At(Mon, 9), End = At(Mon, 10),
            PersonIds = ["ada", "bob"], SeriesId = "weekly"
        }]);

        Assert.Equal(
            "Design review, Monday, October 12, 2026, 9:00 AM – 10:00 AM, Ada Lovelace, Bob Ross, Recurring",
            cut.Find(".cal-event").GetAttribute("aria-label")!.Replace(' ', ' '));
    }

    // ── S2 · Colour ─────────────────────────────────────────────────────

    [Fact]
    public void An_event_carries_its_colour_as_a_slot_attribute_never_as_an_inline_colour()
    {
        var cut = RenderEvents(
        [
            Timed("explicit", Mon, 8, 9, "ada") with { Color = CalendarColor.Slot4 },
            Timed("first-known", Mon, 10, 11, "ghost", "bob", "ada"),
            Timed("person-slot", Mon, 12, 13, "cy"),
            Timed("nobody", Mon, 14, 15)
        ]);

        Assert.Equal("4", cut.Find("[data-event-id='explicit']").GetAttribute("data-color"));
        Assert.Equal("2", cut.Find("[data-event-id='first-known']").GetAttribute("data-color"));
        Assert.Equal("5", cut.Find("[data-event-id='person-slot']").GetAttribute("data-color"));
        Assert.Equal("1", cut.Find("[data-event-id='nobody']").GetAttribute("data-color"));
        Assert.All(cut.FindAll(".cal-event"), e => Assert.DoesNotContain("color", e.GetAttribute("style")));
    }

    // ── S2 · Activating an event ────────────────────────────────────────

    [Fact]
    public void Every_rendered_event_is_a_button_and_activating_it_fires_OnEventClick_with_that_event()
    {
        var review = Timed("review", Mon, 9, 10);
        var fair = AllDay("fair", Mon, Mon.AddDays(2));
        var clicked = new List<CalendarEvent>();
        var cut = RenderEvents([review, fair], p => p.Add(c => c.OnEventClick, clicked.Add));

        Assert.All(cut.FindAll(".cal-event"), e =>
        {
            Assert.Equal("BUTTON", e.TagName);
            Assert.Equal("button", e.GetAttribute("type"));
        });

        cut.Find("[data-event-id='review']").Click();
        cut.Find("[data-event-id='fair']").Click();

        Assert.Equal(2, clicked.Count);
        Assert.Same(review, clicked[0]);
        Assert.Same(fair, clicked[1]);
    }

    [Fact]
    public void An_event_is_focusable_whether_or_not_OnEventClick_has_a_handler()
    {
        var cut = RenderEvents([Timed("review", Mon, 9, 10)]);

        var box = cut.Find(".cal-event");
        Assert.False(box.HasAttribute("disabled"));
        Assert.False(box.HasAttribute("tabindex"));
        box.Click();   // no handler — nothing to raise, nothing thrown
    }

    [Fact]
    public void Every_bar_of_one_event_in_the_month_view_fires_OnEventClick_with_the_same_event()
    {
        var fair = AllDay("fair", Mon.AddDays(5), Mon.AddDays(9));
        var clicked = new List<CalendarEvent>();
        var cut = RenderMonth([fair], p => p.Add(c => c.OnEventClick, clicked.Add));

        cut.FindAll(".cal-event--bar")[0].Click();
        cut.FindAll(".cal-event--bar")[1].Click();

        Assert.Equal(2, clicked.Count);
        Assert.All(clicked, e => Assert.Same(fair, e));
    }

    [Fact]
    public void Events_with_equal_content_and_different_ids_render_side_by_side_without_a_key_clash()
    {
        // Records compare by content; keying on the record itself would give two
        // siblings the same key. The key is the Id plus the segment's day.
        var a = new CalendarEvent { Id = "a", Title = "Same", Start = At(Mon, 9), End = At(Mon, 10) };
        var b = a with { Id = "b" };

        var cut = RenderEvents([a, b]);

        Assert.Equal(2, cut.FindAll(".cal-event").Count);
    }

    // ── S3 · People ─────────────────────────────────────────────────────

    private static CalendarAvailability Working(string person, DateOnly day, int from, int to)
        => new() { PersonId = person, Start = At(day, 0).AddHours(from), End = At(day, 0).AddHours(to) };

    private static CalendarAvailability Absent(string person, DateTime start, DateTime end, string? label = null)
        => new() { PersonId = person, Kind = CalendarAvailabilityKind.Absent, Start = start, End = end, Label = label };

    private IRenderedComponent<DrylCalendar> RenderColumns(
        IEnumerable<CalendarEvent> events,
        Action<ComponentParameterCollectionBuilder<DrylCalendar>>? configure = null)
        => RenderEvents(events, p =>
        {
            p.Add(c => c.View, CalendarView.Day);
            p.Add(c => c.PersonLayout, CalendarPersonLayout.Columns);
            configure?.Invoke(p);
        });

    private static IReadOnlySet<string> Hidden(params string[] ids) => new HashSet<string>(ids);

    [Fact]
    public void People_left_empty_renders_no_legend_no_person_columns_and_no_avatars()
    {
        var cut = RenderCalendar(p => p
            .Add(c => c.Events, new[] { Timed("e", Mon, 9, 10, "ada") })
            .Add(c => c.View, CalendarView.Day)
            .Add(c => c.PersonLayout, CalendarPersonLayout.Columns));

        Assert.Empty(cut.FindAll(".cal-legend"));
        Assert.Empty(cut.FindAll(".cal-person-head"));
        Assert.Empty(cut.FindAll(".avatar"));
        // Columns without people renders the day as the overlaid layout does.
        Assert.Single(cut.FindAll(".cal-col"));
        Assert.Single(cut.FindAll(".cal-day-head"));
        Assert.Single(cut.FindAll(".cal-event"));
    }

    [Fact]
    public void A_person_with_Auto_takes_a_slot_by_position_and_an_explicit_slot_is_kept()
    {
        var cut = RenderEvents([]);

        Assert.Equal(["1", "2", "5"], cut.FindAll(".cal-legend-item").Select(i => i.GetAttribute("data-color")));
    }

    [Fact]
    public void Of_two_people_with_the_same_Id_the_later_one_is_not_rendered_and_it_is_reported_once()
    {
        var original = Console.Out;
        var console = new StringWriter();
        Console.SetOut(console);
        IRenderedComponent<DrylCalendar> cut;
        try
        {
            cut = RenderCalendar(p => p.Add(c => c.People, new[] { Ada, Bob, new CalendarPerson { Id = "ada", Name = "Impostor" } }));
            cut.Render(p => p.Add(c => c.Loading, true));
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal(["Ada Lovelace", "Bob Ross"], cut.FindAll(".cal-legend-item .cal-person-name").Select(n => n.TextContent));
        Assert.Equal(1, Occurrences(console.ToString(), "DrylCalendar: two people share an Id"));
    }

    // ── S3 · The legend ─────────────────────────────────────────────────

    [Fact]
    public void ShowLegend_left_true_renders_an_entry_for_every_person_in_the_order_of_People()
    {
        var cut = RenderEvents([]);

        var legend = cut.Find(".cal-legend");
        Assert.Equal("People", legend.GetAttribute("aria-label"));
        var entries = legend.QuerySelectorAll(".cal-legend-item");
        Assert.Equal(["Ada Lovelace", "Bob Ross", "Cy Twombly"], entries.Select(e => e.QuerySelector(".cal-person-name")!.TextContent));
        Assert.All(entries, e =>
        {
            Assert.Equal("BUTTON", e.TagName);
            Assert.NotNull(e.QuerySelector(".avatar"));
            Assert.Equal("true", e.QuerySelector(".cal-legend-swatch")!.GetAttribute("aria-hidden"));
            Assert.Equal("true", e.GetAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void The_legend_sits_between_the_toolbar_and_the_grid()
    {
        var cut = RenderEvents([]);

        Assert.Equal(
            ["cal-toolbar", "cal-legend", "cal-body"],
            cut.Find(".cal-root").Children.Select(c => c.ClassList.First(n => n.StartsWith("cal-"))));
    }

    [Fact]
    public void ShowLegend_set_to_false_renders_no_legend_element_at_all()
    {
        var cut = RenderEvents([], p => p.Add(c => c.ShowLegend, false));

        Assert.Empty(cut.FindAll(".cal-legend"));
    }

    [Fact]
    public void PersonTemplate_replaces_the_avatar_and_the_name_of_a_legend_entry()
    {
        var cut = RenderEvents([], p => p.Add(c => c.PersonTemplate, person => $"<b class=\"mine\">{person.Id}</b>"));

        var entry = cut.FindAll(".cal-legend-item")[1];
        Assert.Equal("bob", entry.QuerySelector(".mine")!.TextContent);
        Assert.Null(entry.QuerySelector(".avatar"));
        Assert.Null(entry.QuerySelector(".cal-person-name"));
        Assert.NotNull(entry.QuerySelector(".cal-legend-swatch"));
    }

    [Fact]
    public void Activating_a_legend_entry_hides_a_shown_person_and_shows_a_hidden_one()
    {
        var sets = new List<IReadOnlySet<string>>();
        var cut = RenderEvents([Timed("a", Mon, 9, 10, "ada"), Timed("b", Mon, 11, 12, "bob")],
            p => p.Add(c => c.HiddenPersonIdsChanged, sets.Add));

        cut.Find(".cal-legend-item[data-person-id='ada']").Click();

        Assert.Equal(["ada"], sets[0]);
        Assert.Equal("false", cut.Find(".cal-legend-item[data-person-id='ada']").GetAttribute("aria-pressed"));
        Assert.Equal(["b"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));   // applied unbound

        cut.Find(".cal-legend-item[data-person-id='bob']").Click();
        cut.Find(".cal-legend-item[data-person-id='ada']").Click();

        Assert.Equal(3, sets.Count);
        Assert.Equal(["ada", "bob"], sets[1].Order());
        Assert.Equal(["bob"], sets[2]);
        Assert.Equal("true", cut.Find(".cal-legend-item[data-person-id='ada']").GetAttribute("aria-pressed"));
        Assert.Equal(["a"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));
    }

    [Fact]
    public void HiddenPersonIds_is_adopted_and_an_id_that_matches_no_person_has_no_effect()
    {
        var cut = RenderEvents([Timed("a", Mon, 9, 10, "ada"), Timed("b", Mon, 11, 12, "bob")],
            p => p.Add(c => c.HiddenPersonIds, Hidden("bob", "ghost")));

        Assert.Equal(["true", "false", "true"], cut.FindAll(".cal-legend-item").Select(i => i.GetAttribute("aria-pressed")));
        Assert.Equal(["a"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));

        cut.Render(p => p.Add(c => c.HiddenPersonIds, (IReadOnlySet<string>?)null));

        Assert.Equal(2, cut.FindAll(".cal-event").Count);
    }

    // ── S3 · Hidden people ──────────────────────────────────────────────

    [Fact]
    public void Hiding_decides_which_events_are_rendered_but_not_their_colours_or_avatars()
    {
        var cut = RenderEvents(
        [
            Timed("all-hidden", Mon, 8, 9, "ada"),
            Timed("one-shown", Mon, 10, 11, "ada", "bob"),
            Timed("unassigned", Mon, 12, 13, "ghost"),
            Timed("bobs", Mon, 14, 15, "bob")
        ], p => p.Add(c => c.HiddenPersonIds, Hidden("ada")));

        Assert.Equal(["one-shown", "unassigned", "bobs"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));
        // Bob keeps the slot of his position; hiding Ada recolours nobody.
        Assert.Equal("2", cut.Find("[data-event-id='bobs']").GetAttribute("data-color"));
        Assert.Equal("2", cut.Find(".cal-legend-item[data-person-id='bob']").GetAttribute("data-color"));
        // The event still takes its first person's colour and still shows her avatar.
        Assert.Equal("1", cut.Find("[data-event-id='one-shown']").GetAttribute("data-color"));
        Assert.Equal(["AL", "BR"], cut.FindAll("[data-event-id='one-shown'] .avatar").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void An_unassigned_event_is_rendered_even_with_every_person_hidden()
    {
        var cut = RenderMonth([Timed("orphan", Mon, 9, 10), Timed("adas", Mon, 11, 12, "ada")],
            p => p.Add(c => c.HiddenPersonIds, Hidden("ada", "bob", "cy")));

        Assert.Equal(["orphan"], cut.FindAll(".cal-event").Select(e => e.GetAttribute("data-event-id")));
    }

    // ── S3 · The overlaid layout and person columns ─────────────────────

    [Fact]
    public void PersonLayout_defaults_to_Overlay_and_every_shown_persons_events_share_the_days_column()
    {
        var cut = RenderEvents([Timed("a", Mon, 9, 10, "ada"), Timed("b", Mon, 11, 12, "bob")],
            p => p.Add(c => c.View, CalendarView.Day));

        var column = Assert.Single(cut.FindAll(".cal-col"));
        Assert.Equal(2, column.QuerySelectorAll(".cal-event").Length);
        Assert.Empty(cut.FindAll(".cal-person-head"));
    }

    [Theory]
    [InlineData(CalendarView.Week)]
    [InlineData(CalendarView.Month)]
    public void The_week_and_month_views_are_always_overlaid(CalendarView view)
    {
        var cut = RenderEvents([Timed("a", Mon, 9, 10, "ada")],
            p => p.Add(c => c.View, view).Add(c => c.PersonLayout, CalendarPersonLayout.Columns));

        Assert.Empty(cut.FindAll(".cal-person-head"));
        Assert.Equal(view == CalendarView.Week ? 7 : 0, cut.FindAll(".cal-col").Count);
        Assert.Single(cut.FindAll(".cal-event"));
    }

    [Fact]
    public void Columns_in_the_day_view_renders_one_column_per_shown_person_under_one_time_axis()
    {
        var cut = RenderColumns([Timed("a", Mon, 9, 10, "ada")], p => p.Add(c => c.HiddenPersonIds, Hidden("bob")));

        Assert.Equal(["ada", "cy"], cut.FindAll(".cal-col").Select(c => c.GetAttribute("data-person-id")));
        Assert.Equal(["Ada Lovelace", "Cy Twombly"], cut.FindAll(".cal-person-head .cal-person-name").Select(n => n.TextContent));
        Assert.All(cut.FindAll(".cal-person-head"), h => Assert.NotNull(h.QuerySelector(".avatar")));
        Assert.Single(cut.FindAll(".cal-axis"));
        Assert.Contains("--cal-cols:2", cut.Find(".cal-time").GetAttribute("style"));
        Assert.Empty(cut.FindAll(".cal-day-head"));
    }

    [Fact]
    public void PersonTemplate_replaces_the_avatar_and_the_name_of_a_column_header()
    {
        var cut = RenderColumns([], p => p.Add(c => c.PersonTemplate, person => $"<b class=\"mine\">{person.Id}</b>"));

        var head = cut.FindAll(".cal-person-head")[2];
        Assert.Equal("cy", head.QuerySelector(".mine")!.TextContent);
        Assert.Null(head.QuerySelector(".avatar"));
        Assert.Null(head.QuerySelector(".cal-person-name"));
    }

    [Fact]
    public void An_event_is_drawn_in_the_column_of_every_shown_person_assigned_to_it()
    {
        var shared = Timed("shared", Mon, 9, 10, "ada", "bob");
        var clicked = new List<CalendarEvent>();
        var cut = RenderColumns([shared, AllDay("fair", Mon, Mon.AddDays(1)) with { PersonIds = ["bob", "cy"] }],
            p => p.Add(c => c.OnEventClick, clicked.Add));

        Assert.Single(cut.FindAll(".cal-col[data-person-id='ada'] [data-event-id='shared']"));
        Assert.Single(cut.FindAll(".cal-col[data-person-id='bob'] [data-event-id='shared']"));
        Assert.Empty(cut.FindAll(".cal-col[data-person-id='cy'] .cal-event"));

        // The all-day event sits in the all-day row of Bob's and Cy's columns.
        var bars = cut.FindAll(".cal-allday [data-event-id='fair']");
        Assert.Equal(["grid-column:3 / span 1;grid-row:1", "grid-column:4 / span 1;grid-row:1"], bars.Select(b => b.GetAttribute("style")));

        cut.FindAll("[data-event-id='shared']")[0].Click();
        cut.FindAll("[data-event-id='shared']")[1].Click();
        Assert.All(clicked, e => Assert.Same(shared, e));
        Assert.Equal(2, clicked.Count);
    }

    [Fact]
    public void Overlaps_are_resolved_per_column()
    {
        // The two events overlap in time but belong to different people.
        var cut = RenderColumns([Timed("a", Mon, 9, 11, "ada"), Timed("b", Mon, 10, 12, "bob"), Timed("c", Mon, 10, 12, "ada")]);

        Assert.EndsWith("left:0%;width:100%", cut.Find(".cal-col[data-person-id='bob'] [data-event-id='b']").GetAttribute("style"));
        Assert.EndsWith("left:0%;width:50%", cut.Find(".cal-col[data-person-id='ada'] [data-event-id='a']").GetAttribute("style"));
        Assert.EndsWith("left:50%;width:50%", cut.Find(".cal-col[data-person-id='ada'] [data-event-id='c']").GetAttribute("style"));
    }

    [Fact]
    public void A_leading_unassigned_column_is_rendered_only_when_the_day_has_an_unassigned_event()
    {
        var without = RenderColumns([Timed("a", Mon, 9, 10, "ada"), Timed("tomorrow", Mon.AddDays(1), 9, 10)]);
        var with = RenderColumns([Timed("a", Mon, 9, 10, "ada"), Timed("orphan", Mon, 11, 12, "ghost")]);

        Assert.Equal(3, without.FindAll(".cal-col").Count);
        Assert.DoesNotContain(without.FindAll(".cal-person-head"), h => h.TextContent.Trim() == "Unassigned");

        var columns = with.FindAll(".cal-col");
        Assert.Equal(4, columns.Count);
        Assert.False(columns[0].HasAttribute("data-person-id"));
        Assert.Equal("orphan", columns[0].QuerySelector(".cal-event")!.GetAttribute("data-event-id"));
        Assert.Equal("Unassigned", with.FindAll(".cal-person-head")[0].TextContent.Trim());
    }

    [Fact]
    public void With_every_person_hidden_the_day_view_renders_its_time_axis_and_the_unassigned_column_only()
    {
        var cut = RenderColumns([Timed("a", Mon, 9, 10, "ada")], p => p.Add(c => c.HiddenPersonIds, Hidden("ada", "bob", "cy")));

        Assert.Single(cut.FindAll(".cal-axis"));
        var head = Assert.Single(cut.FindAll(".cal-person-head"));
        Assert.Equal("Unassigned", head.TextContent.Trim());
        Assert.Single(cut.FindAll(".cal-col"));
        Assert.Empty(cut.FindAll(".cal-event"));
    }

    // ── S3 · Availability ───────────────────────────────────────────────

    [Fact]
    public void In_a_persons_column_their_unavailable_time_is_a_muted_area_behind_the_events()
    {
        var cut = RenderColumns([Timed("a", Mon, 9, 10, "ada")], p => p.Add(c => c.Availability, new[]
        {
            Working("ada", Mon, 6, 18),
            Working("ghost", Mon, 0, 1)
        }));

        var column = cut.Find(".cal-col[data-person-id='ada']");
        var muted = column.QuerySelectorAll(".cal-muted");
        Assert.Equal(["top:0%;height:25%", "top:75%;height:25%"], muted.Select(m => m.GetAttribute("style")));
        Assert.All(muted, m =>
        {
            Assert.Equal("true", m.GetAttribute("aria-hidden"));
            Assert.Equal("DIV", m.TagName);
            Assert.False(m.HasAttribute("tabindex"));
        });
        // Behind the events: the muted areas come first in the column.
        Assert.Contains("cal-muted", column.Children[0].ClassList);
        Assert.Contains("cal-event", column.Children[^2].ClassList);
        // No working span, no unavailable time; an unknown person's span is ignored.
        Assert.Empty(cut.FindAll(".cal-col[data-person-id='bob'] .cal-muted"));
    }

    [Fact]
    public void A_working_span_is_not_repeated_on_other_days()
    {
        var cut = RenderColumns([], p => p
            .Add(c => c.Date, Mon.AddDays(1))
            .Add(c => c.Availability, new[] { Working("ada", Mon, 6, 18) }));

        Assert.Empty(cut.FindAll(".cal-muted"));
    }

    [Fact]
    public void An_absence_in_a_persons_column_renders_its_label_and_a_whole_day_is_named_in_the_all_day_row()
    {
        var cut = RenderColumns([], p => p.Add(c => c.Availability, new[]
        {
            Absent("ada", At(Mon, 12), At(Mon, 18), "Dentist"),
            Absent("bob", At(Mon.AddDays(-1), 0), At(Mon.AddDays(2), 0), "Holiday"),
            Absent("cy", At(Mon, 0), At(Mon, 6))
        }));

        var dentist = cut.Find(".cal-col[data-person-id='ada'] .cal-absence");
        Assert.Equal("Dentist", dentist.TextContent.Trim());
        Assert.Equal("top:50%;height:25%", dentist.GetAttribute("style"));
        Assert.Equal("top:50%;height:25%", cut.Find(".cal-col[data-person-id='ada'] .cal-muted").GetAttribute("style"));

        Assert.Equal("top:0%;height:100%", cut.Find(".cal-col[data-person-id='bob'] .cal-muted").GetAttribute("style"));
        var chip = Assert.Single(cut.FindAll(".cal-absence-chip"));
        Assert.Equal("Holiday", chip.TextContent.Trim());
        Assert.Equal("grid-column:3 / span 1;grid-row:1", chip.GetAttribute("style"));

        // An absence without a label mutes its time and names nothing.
        Assert.Single(cut.FindAll(".cal-col[data-person-id='cy'] .cal-muted"));
        Assert.Empty(cut.FindAll(".cal-col[data-person-id='cy'] .cal-absence"));
    }

    [Fact]
    public void Overlaid_a_time_is_muted_only_when_every_shown_person_is_unavailable_at_it()
    {
        var availability = new[]
        {
            Working("ada", Mon, 6, 18),
            Working("bob", Mon, 12, 24),
            Working("cy", Mon, 0, 24)
        };
        var cut = RenderEvents([], p => p.Add(c => c.Availability, availability));

        // Cy is available all day, so nothing is muted.
        Assert.Empty(cut.FindAll(".cal-muted"));

        cut.Render(p => p.Add(c => c.HiddenPersonIds, Hidden("cy")));

        // Ada is out before 6 and after 18, Bob before 12: both are out before 6.
        var muted = Assert.Single(cut.FindAll(".cal-muted"));
        Assert.Equal("top:0%;height:25%", muted.GetAttribute("style"));
        Assert.Equal("2026-10-12", muted.ParentElement!.GetAttribute("data-date"));
        Assert.Empty(cut.FindAll(".cal-absence"));
    }

    [Fact]
    public void Overlaid_no_muted_area_is_drawn_when_no_person_is_shown()
    {
        var cut = RenderEvents([], p => p
            .Add(c => c.Availability, new[] { Working("ada", Mon, 6, 18) })
            .Add(c => c.HiddenPersonIds, Hidden("ada", "bob", "cy")));

        Assert.Empty(cut.FindAll(".cal-muted"));
    }

    [Fact]
    public void The_month_view_draws_no_availability()
    {
        var cut = RenderMonth([], p => p.Add(c => c.Availability, new[]
        {
            Working("ada", Mon, 6, 18), Working("bob", Mon, 6, 18), Working("cy", Mon, 6, 18),
            Absent("ada", At(Mon, 0), At(Mon.AddDays(1), 0), "Holiday")
        }));

        Assert.Empty(cut.FindAll(".cal-muted"));
        Assert.Empty(cut.FindAll(".cal-absence"));
        Assert.Empty(cut.FindAll(".cal-absence-chip"));
    }
}
