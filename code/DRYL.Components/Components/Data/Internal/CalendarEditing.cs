namespace DRYL.Components.Internal;

/// <summary>
/// What a change may not leave: the visible range, and the time grid of each day.
/// </summary>
/// <param name="RangeStart">Midnight of the first visible day.</param>
/// <param name="RangeEnd">Midnight after the last visible day, exclusive.</param>
/// <param name="Grid">The normalised time grid.</param>
internal readonly record struct CalendarBounds(DateTime RangeStart, DateTime RangeEnd, CalendarGrid Grid);

/// <summary>Where a change is being made: the time grid, the all-day row, or the month view.</summary>
internal enum CalendarSurface { Time, AllDay, Month }

/// <summary>
/// Pure arithmetic of editing in <c>DrylCalendar</c>: the steps every gesture and
/// every key shares, the limits they keep to, reassignment across person columns,
/// and conflict detection. No Blazor dependency; <c>dryl.calendar.edit</c> mirrors
/// the limits and the conflict rules in the browser.
/// </summary>
internal static class CalendarEditing
{
    private static readonly TimeSpan WholeDay = TimeSpan.FromHours(24);

    // ── Limits ──────────────────────────────────────────────────────────

    /// <summary>
    /// The candidate keeps to the limits of a change made to <paramref name="original"/>:
    /// it does not leave the visible range, starts no earlier in its day than
    /// <c>DayStart</c>, ends no later than <c>DayEnd</c>, and keeps its shape. A limit
    /// the original already broke may stay broken — a change never takes an event
    /// further out than it was.
    /// </summary>
    public static bool Fits(CalendarEvent candidate, CalendarEvent original, CalendarSurface surface, CalendarBounds bounds)
    {
        if (candidate.End < candidate.Start) return false;
        if (Shape(candidate, surface) != Shape(original, surface)) return false;

        if (surface == CalendarSurface.Time)
        {
            if (candidate.Start < bounds.RangeStart && candidate.Start < original.Start) return false;
            if (candidate.End > bounds.RangeEnd && candidate.End > original.End) return false;

            var startOfDay = candidate.Start.TimeOfDay;
            if (startOfDay < bounds.Grid.DayStart && startOfDay < original.Start.TimeOfDay) return false;
            var endOfDay = EndOfDay(candidate.End);
            if (endOfDay > bounds.Grid.DayEnd && endOfDay > EndOfDay(original.End)) return false;
            return true;
        }

        var (first, end) = CalendarLayout.DaySpan(candidate);
        var (originalFirst, originalEnd) = CalendarLayout.DaySpan(original);
        var rangeFirst = DateOnly.FromDateTime(bounds.RangeStart);
        var rangeEnd = DateOnly.FromDateTime(bounds.RangeEnd);
        if (first < rangeFirst && first < originalFirst) return false;
        if (end > rangeEnd && end > originalEnd) return false;
        return true;
    }

    // A change keeps the event in the row it is drawn in: a timed event never
    // becomes an all-day one, nor the reverse (I19, 2026-10-16).
    private static bool Shape(CalendarEvent e, CalendarSurface surface) => surface switch
    {
        CalendarSurface.Month => CalendarLayout.IsMonthBar(e),
        _ => CalendarLayout.IsAllDayRow(e)
    };

    // An end at midnight is the end of the day before it: 24:00, not 00:00.
    private static TimeSpan EndOfDay(DateTime end) => end.TimeOfDay == TimeSpan.Zero ? WholeDay : end.TimeOfDay;

    // ── Steps ───────────────────────────────────────────────────────────

    /// <summary>The event moved by <paramref name="by"/>, its duration kept.</summary>
    public static CalendarEvent Move(CalendarEvent e, TimeSpan by)
        => e with { Start = e.Start + by, End = e.End + by };

    /// <summary>
    /// The event with its start or its end moved by <paramref name="by"/>, or <c>null</c>
    /// when that would leave it shorter than <paramref name="minimum"/>.
    /// </summary>
    public static CalendarEvent? Resize(CalendarEvent e, bool end, TimeSpan by, TimeSpan minimum)
    {
        var resized = end ? e with { End = e.End + by } : e with { Start = e.Start + by };
        return resized.End - resized.Start < minimum ? null : resized;
    }

    /// <summary>
    /// A bar with its first day (<paramref name="end"/> false) or its last day moved by
    /// <paramref name="days"/>, or <c>null</c> when it would cover less than one day.
    /// The time-of-day parts of an all-day event stay as they were.
    /// </summary>
    public static CalendarEvent? ResizeDays(CalendarEvent e, bool end, int days)
    {
        if (!e.AllDay) return Resize(e, end, TimeSpan.FromDays(days), WholeDay);

        var (first, endExclusive) = CalendarLayout.DaySpan(e);
        var newFirst = end ? first : first.AddDays(days);
        var newEnd = end ? endExclusive.AddDays(days) : endExclusive;
        if (newEnd <= newFirst) return null;
        return e with
        {
            Start = newFirst.ToDateTime(TimeOnly.FromTimeSpan(e.Start.TimeOfDay)),
            End = newEnd.ToDateTime(TimeOnly.MinValue)
        };
    }

    /// <summary>
    /// A new timed event from slot <paramref name="firstSlot"/> to slot
    /// <paramref name="lastSlot"/> of <paramref name="day"/>, both included; the end
    /// stops at <c>DayEnd</c>.
    /// </summary>
    public static (DateTime Start, DateTime End) TimedSpan(DateOnly day, int firstSlot, int lastSlot, CalendarGrid grid)
    {
        if (lastSlot < firstSlot) (firstSlot, lastSlot) = (lastSlot, firstSlot);
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        var slot = TimeSpan.FromMinutes(grid.SlotMinutes);
        var start = midnight + grid.DayStart + slot * firstSlot;
        var end = midnight + grid.DayStart + slot * (lastSlot + 1);
        var limit = midnight + grid.DayEnd;
        return (start, end > limit ? limit : end);
    }

    /// <summary>A new all-day span from <paramref name="first"/> to <paramref name="last"/>, both included.</summary>
    public static (DateTime Start, DateTime End) AllDaySpan(DateOnly first, DateOnly last)
    {
        if (last < first) (first, last) = (last, first);
        return (first.ToDateTime(TimeOnly.MinValue), last.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// The people of an event moved from the column of <paramref name="from"/> to that of
    /// <paramref name="to"/>; <c>null</c> is the unassigned column. The person of the
    /// starting column is replaced, the others keep their order, and nobody appears twice.
    /// </summary>
    public static IReadOnlyList<string> Reassign(IReadOnlyList<string> personIds, string? from, string? to)
    {
        if (from == to) return personIds;
        var result = new List<string>(personIds.Count + 1);
        var placed = false;
        foreach (var id in personIds)
        {
            if (from is not null && id == from)
            {
                if (to is not null && !personIds.Contains(to) && !placed)
                {
                    result.Add(to);
                    placed = true;
                }
                continue;
            }
            result.Add(id);
        }
        if (from is null && to is not null && !result.Contains(to)) result.Add(to);
        return result;
    }

    // ── Conflicts ───────────────────────────────────────────────────────

    /// <summary>
    /// The conflicts of <paramref name="target"/> against <paramref name="others"/> and the
    /// availability of its people (<c>S8</c>). Only timed events take part, and only
    /// people in <paramref name="knownPersonIds"/>; an event with the target's
    /// <c>Id</c> among the others is skipped, so a moved event never conflicts with its
    /// own old place.
    /// </summary>
    public static IReadOnlyList<CalendarConflict> Conflicts(
        CalendarEvent target,
        IEnumerable<CalendarEvent> others,
        IReadOnlyDictionary<string, List<CalendarAvailability>> availability,
        IReadOnlySet<string> knownPersonIds)
    {
        if (!TakesPart(target)) return [];
        var result = new List<CalendarConflict>();
        var people = (target.PersonIds ?? []).Where(knownPersonIds.Contains).Distinct(StringComparer.Ordinal).ToList();
        if (people.Count == 0) return result;
        var otherList = others as IReadOnlyCollection<CalendarEvent> ?? others.ToList();

        foreach (var person in people)
        {
            foreach (var other in otherList)
            {
                if (other.Id == target.Id || !TakesPart(other)) continue;
                if (other.PersonIds?.Contains(person) != true) continue;
                if (Overlaps(target.Start, target.End, other.Start, other.End))
                    result.Add(new CalendarConflict { Kind = CalendarConflictKind.Overlap, PersonId = person, EventId = other.Id });
            }

            if (!availability.TryGetValue(person, out var spans)) continue;

            if (spans.Any(s => s.Kind == CalendarAvailabilityKind.Absent && s.End > s.Start
                               && Overlaps(target.Start, target.End, s.Start, s.End)))
                result.Add(new CalendarConflict { Kind = CalendarConflictKind.Absence, PersonId = person });

            if (OutsideWorkingHours(target, spans))
                result.Add(new CalendarConflict { Kind = CalendarConflictKind.OutsideWorkingHours, PersonId = person });
        }
        return result;
    }

    /// <summary>The conflicts of every event in <paramref name="events"/>, by <c>Id</c>; events without any are left out.</summary>
    public static Dictionary<string, IReadOnlyList<CalendarConflict>> AllConflicts(
        IReadOnlyList<CalendarEvent> events,
        IReadOnlyDictionary<string, List<CalendarAvailability>> availability,
        IReadOnlySet<string> knownPersonIds)
    {
        var result = new Dictionary<string, IReadOnlyList<CalendarConflict>>(StringComparer.Ordinal);
        foreach (var e in events)
        {
            var conflicts = Conflicts(e, events, availability, knownPersonIds);
            if (conflicts.Count > 0) result[e.Id] = conflicts;
        }
        return result;
    }

    /// <summary>Only timed events have conflicts and cause them: an all-day event takes no part.</summary>
    public static bool TakesPart(CalendarEvent e) => !CalendarLayout.IsAllDayRow(e) && e.End > e.Start;

    private static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd)
        => start < otherEnd && otherStart < end;

    // On every day the event touches for which the person has at least one working
    // span, the part of the event on that day must lie inside the working spans.
    private static bool OutsideWorkingHours(CalendarEvent e, List<CalendarAvailability> spans)
    {
        var working = spans.Where(s => s.Kind == CalendarAvailabilityKind.Working && s.End > s.Start).ToList();
        if (working.Count == 0) return false;

        for (var day = DateOnly.FromDateTime(e.Start); day.ToDateTime(TimeOnly.MinValue) < e.End; day = day.AddDays(1))
        {
            var midnight = day.ToDateTime(TimeOnly.MinValue);
            var next = midnight.AddDays(1);
            var onDay = working.Where(s => s.Start < next && s.End > midnight).OrderBy(s => s.Start).ToList();
            if (onDay.Count == 0) continue;

            var cursor = e.Start > midnight ? e.Start : midnight;
            var stop = e.End < next ? e.End : next;
            foreach (var span in onDay)
            {
                if (span.Start > cursor) return true;
                if (span.End > cursor) cursor = span.End;
                if (cursor >= stop) break;
            }
            if (cursor < stop) return true;
        }
        return false;
    }
}
