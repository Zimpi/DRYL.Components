namespace DRYL.Components.Tests;

/// <summary>
/// Pins what the API freeze binds in the calendar's types: the first member of each
/// enum (the value an unset parameter or property takes), the default texts of
/// <see cref="CalendarLabels"/>, and that the records are copied with <c>with</c>.
/// </summary>
public class CalendarTypesTests
{
    [Fact]
    public void CalendarView_defaults_to_Week()
    {
        Assert.Equal(CalendarView.Week, default(CalendarView));
        Assert.Equal(
            new[] { CalendarView.Week, CalendarView.Day, CalendarView.Month },
            Enum.GetValues<CalendarView>());
    }

    [Fact]
    public void CalendarPersonLayout_defaults_to_Overlay()
    {
        Assert.Equal(CalendarPersonLayout.Overlay, default(CalendarPersonLayout));
        Assert.Equal(
            new[] { CalendarPersonLayout.Overlay, CalendarPersonLayout.Columns },
            Enum.GetValues<CalendarPersonLayout>());
    }

    [Fact]
    public void CalendarColor_defaults_to_Auto_followed_by_six_slots()
    {
        Assert.Equal(CalendarColor.Auto, default(CalendarColor));
        Assert.Equal(
            new[]
            {
                CalendarColor.Auto, CalendarColor.Slot1, CalendarColor.Slot2, CalendarColor.Slot3,
                CalendarColor.Slot4, CalendarColor.Slot5, CalendarColor.Slot6
            },
            Enum.GetValues<CalendarColor>());
    }

    [Fact]
    public void CalendarAvailabilityKind_defaults_to_Working()
    {
        Assert.Equal(CalendarAvailabilityKind.Working, default(CalendarAvailabilityKind));
        Assert.Equal(
            new[] { CalendarAvailabilityKind.Working, CalendarAvailabilityKind.Absent },
            Enum.GetValues<CalendarAvailabilityKind>());
    }

    [Fact]
    public void CalendarLabels_default_to_English_texts()
    {
        var labels = new CalendarLabels();

        Assert.Equal("Today", labels.Today);
        Assert.Equal("Previous", labels.Previous);
        Assert.Equal("Next", labels.Next);
        Assert.Equal("Day", labels.Day);
        Assert.Equal("Week", labels.Week);
        Assert.Equal("Month", labels.Month);
        Assert.Equal("All day", labels.AllDay);
        Assert.Equal("+{0} more", labels.More);
        Assert.Equal("Unassigned", labels.Unassigned);
        Assert.Equal("Recurring", labels.Recurring);
        Assert.Equal("People", labels.People);
        Assert.Equal("Loading", labels.Loading);
        Assert.Equal("No events", labels.NoEvents);
        Assert.Equal("New event", labels.NewEvent);
        Assert.Equal("Arrow keys move, Shift and arrow keys resize, Enter drops, Escape cancels.", labels.EditHint);
        Assert.Equal("Enter creates an event here.", labels.CreateHint);
        Assert.Equal("Change requested: {0}", labels.Requested);
        Assert.Equal("Change cancelled.", labels.Cancelled);
        Assert.Equal("The change was not applied.", labels.NotApplied);
        Assert.Equal("Overlaps another event of {0}", labels.ConflictOverlap);
        Assert.Equal("{0} is absent", labels.ConflictAbsence);
        Assert.Equal("Outside the working hours of {0}", labels.ConflictOutsideWorkingHours);
    }

    [Fact]
    public void CalendarLabels_overrides_one_text_and_keeps_the_rest()
    {
        var labels = new CalendarLabels { Today = "Heute" };

        Assert.Equal("Heute", labels.Today);
        Assert.Equal("Previous", labels.Previous);
    }

    [Fact]
    public void CalendarEvent_defaults_match_the_spec()
    {
        var ev = new CalendarEvent
        {
            Id = "e1",
            Start = new DateTime(2026, 10, 12, 9, 0, 0),
            End = new DateTime(2026, 10, 12, 10, 0, 0)
        };

        Assert.Equal("", ev.Title);
        Assert.False(ev.AllDay);
        Assert.Empty(ev.PersonIds);
        Assert.Equal(CalendarColor.Auto, ev.Color);
        Assert.Null(ev.SeriesId);
        Assert.Null(ev.Data);
        Assert.False(ev.ReadOnly);
    }

    [Fact]
    public void CalendarEvent_is_copied_with_with()
    {
        var data = new object();
        var ev = new CalendarEvent
        {
            Id = "e1",
            Title = "Stand-up",
            Start = new DateTime(2026, 10, 12, 9, 0, 0),
            End = new DateTime(2026, 10, 12, 9, 15, 0),
            PersonIds = ["p1"],
            Data = data
        };

        var moved = ev with { Start = ev.Start.AddHours(1), End = ev.End.AddHours(1) };

        Assert.Equal("e1", moved.Id);
        Assert.Equal("Stand-up", moved.Title);
        Assert.Equal(new DateTime(2026, 10, 12, 10, 0, 0), moved.Start);
        Assert.Same(data, moved.Data);
        Assert.Same(ev.PersonIds, moved.PersonIds);
        Assert.Equal(new DateTime(2026, 10, 12, 9, 0, 0), ev.Start);
    }

    [Fact]
    public void CalendarPerson_and_CalendarAvailability_defaults_match_the_spec()
    {
        var person = new CalendarPerson { Id = "p1", Name = "Ada" };
        var span = new CalendarAvailability
        {
            PersonId = "p1",
            Start = new DateTime(2026, 10, 12, 8, 0, 0),
            End = new DateTime(2026, 10, 12, 16, 0, 0)
        };

        Assert.Null(person.AvatarSrc);
        Assert.Equal(CalendarColor.Auto, person.Color);
        Assert.Equal(CalendarAvailabilityKind.Working, span.Kind);
        Assert.Null(span.Label);
    }

    [Fact]
    public void CalendarRange_is_a_value_of_start_and_end()
    {
        var start = new DateTime(2026, 10, 12);
        var range = new CalendarRange(start, start.AddDays(7));

        Assert.Equal(new CalendarRange(start, start.AddDays(7)), range);
        Assert.Equal(start.AddDays(7), range.End);
    }

    [Fact]
    public void CalendarChangeKind_defaults_to_Create()
    {
        Assert.Equal(CalendarChangeKind.Create, default(CalendarChangeKind));
        Assert.Equal(
            new[] { CalendarChangeKind.Create, CalendarChangeKind.Move, CalendarChangeKind.Resize },
            Enum.GetValues<CalendarChangeKind>());
    }

    [Fact]
    public void CalendarConflictKind_has_exactly_three_members()
    {
        Assert.Equal(CalendarConflictKind.Overlap, default(CalendarConflictKind));
        Assert.Equal(
            new[] { CalendarConflictKind.Overlap, CalendarConflictKind.Absence, CalendarConflictKind.OutsideWorkingHours },
            Enum.GetValues<CalendarConflictKind>());
    }

    [Fact]
    public void CalendarChangeRequest_and_CalendarConflict_defaults_match_the_spec()
    {
        var proposed = new CalendarEvent
        {
            Id = "new",
            Start = new DateTime(2026, 10, 12, 9, 0, 0),
            End = new DateTime(2026, 10, 12, 10, 0, 0)
        };
        var request = new CalendarChangeRequest { Kind = CalendarChangeKind.Create, Proposed = proposed };
        var conflict = new CalendarConflict { Kind = CalendarConflictKind.Absence, PersonId = "p1" };

        Assert.Null(request.Original);
        Assert.Same(proposed, request.Proposed);
        Assert.Empty(request.Conflicts);
        Assert.Null(conflict.EventId);
    }
}
