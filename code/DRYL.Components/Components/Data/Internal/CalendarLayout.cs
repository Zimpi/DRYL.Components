using System.Globalization;

namespace DRYL.Components.Internal;

/// <summary>The normalised time grid of a day or week view.</summary>
/// <param name="DayStart">First time of day the grid shows.</param>
/// <param name="DayEnd">Time of day the grid ends at; at most twenty-four hours.</param>
/// <param name="SlotMinutes">Length of one slot, a divisor of sixty.</param>
internal readonly record struct CalendarGrid(TimeSpan DayStart, TimeSpan DayEnd, int SlotMinutes)
{
    /// <summary>The span of time the grid covers.</summary>
    public TimeSpan Length => DayEnd - DayStart;

    /// <summary>Number of slots, the last one possibly cut short by <see cref="DayEnd"/>.</summary>
    public int SlotCount => (int)Math.Ceiling(Length.TotalMinutes / SlotMinutes);

    /// <summary>Position of a time of day on the grid, as a fraction of its length.</summary>
    public double Fraction(TimeSpan timeOfDay) => (timeOfDay - DayStart).TotalMinutes / Length.TotalMinutes;
}

/// <summary>
/// One timed event in one day's column. Positions are fractions of the column
/// (0…1), never pixels — the stylesheet owns every length.
/// </summary>
internal sealed record CalendarSegment(
    CalendarEvent Event,
    DateOnly Day,
    double Top,
    double Height,
    bool ContinuesBefore,
    bool ContinuesAfter)
{
    /// <summary>Leading edge within the column, set by <see cref="CalendarLayout.Pack"/>.</summary>
    public double Left { get; init; }

    /// <summary>Width within the column, set by <see cref="CalendarLayout.Pack"/>.</summary>
    public double Width { get; init; } = 1;
}

/// <summary>An event drawn as a bar across whole days, in the all-day row or a month's week row.</summary>
/// <param name="Event">The event the bar belongs to.</param>
/// <param name="Column">Zero-based day column the bar starts in.</param>
/// <param name="Span">Number of day columns it covers.</param>
/// <param name="Lane">Zero-based lane; bars that share a day sit in different lanes.</param>
/// <param name="ContinuesBefore">The event starts before the first column.</param>
/// <param name="ContinuesAfter">The event ends after the last column.</param>
internal sealed record CalendarBar(
    CalendarEvent Event,
    int Column,
    int Span,
    int Lane,
    bool ContinuesBefore,
    bool ContinuesAfter);

/// <summary>A single-day timed event in a month cell.</summary>
internal sealed record CalendarMonthLine(CalendarEvent Event, int Column, int Lane);

/// <summary>One week row of the month view.</summary>
/// <param name="First">The first day of the week row.</param>
/// <param name="Bars">The bars that fit, in lane order.</param>
/// <param name="Lines">The lines that fit.</param>
/// <param name="Hidden">Per day column, how many events did not fit.</param>
/// <param name="Total">Per day column, how many events the day has.</param>
internal sealed record CalendarMonthWeek(
    DateOnly First,
    IReadOnlyList<CalendarBar> Bars,
    IReadOnlyList<CalendarMonthLine> Lines,
    IReadOnlyList<int> Hidden,
    IReadOnlyList<int> Total);

/// <summary>A span of one day, as times of day.</summary>
internal readonly record struct CalendarSpan(TimeSpan Start, TimeSpan End);

/// <summary>An absence clipped to one day's grid.</summary>
/// <param name="Source">The span the host supplied.</param>
/// <param name="Span">The part of it inside the day's grid.</param>
/// <param name="WholeDay">It covers the whole grid of that day.</param>
internal sealed record CalendarAbsence(CalendarAvailability Source, CalendarSpan Span, bool WholeDay);

/// <summary>
/// Pure layout arithmetic of <c>DrylCalendar</c>: ranges, navigation, placement,
/// packing, lanes, colour and availability. No Blazor dependency. Every
/// <see cref="DateTime"/> is read as a wall-clock time; its <see cref="DateTime.Kind"/>
/// has no effect, and every end is exclusive.
/// </summary>
internal static class CalendarLayout
{
    private const double Epsilon = 1e-9;
    private static readonly TimeSpan WholeDay = TimeSpan.FromHours(24);

    /// <summary>Invariant-culture number for CSS interpolation.</summary>
    public static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>A fraction (0…1) as an invariant-culture CSS percentage.</summary>
    public static string Pct(double fraction) => F(fraction * 100) + "%";

    // ── Range and navigation ────────────────────────────────────────────

    /// <summary>The first day of the week that contains <paramref name="date"/>.</summary>
    public static DateOnly StartOfWeek(DateOnly date, DayOfWeek firstDayOfWeek)
        => date.AddDays(-(((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7));

    /// <summary>
    /// The first visible day and the number of visible days. The month view shows
    /// every whole week that contains a day of the month.
    /// </summary>
    public static (DateOnly First, int Days) VisibleDays(CalendarView view, DateOnly date, DayOfWeek firstDayOfWeek)
    {
        switch (view)
        {
            case CalendarView.Day:
                return (date, 1);
            case CalendarView.Month:
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                var first = StartOfWeek(monthStart, firstDayOfWeek);
                var last = StartOfWeek(monthStart.AddMonths(1).AddDays(-1), firstDayOfWeek).AddDays(6);
                return (first, last.DayNumber - first.DayNumber + 1);
            default:
                return (StartOfWeek(date, firstDayOfWeek), 7);
        }
    }

    /// <summary>The visible range, from midnight of the first day to midnight after the last.</summary>
    public static CalendarRange VisibleRange(CalendarView view, DateOnly date, DayOfWeek firstDayOfWeek)
    {
        var (first, days) = VisibleDays(view, date, firstDayOfWeek);
        var start = first.ToDateTime(TimeOnly.MinValue);
        return new CalendarRange(start, start.AddDays(days));
    }

    /// <summary>
    /// The date one range before (<paramref name="direction"/> below zero) or after.
    /// A month step from a day the target month lacks lands on its last day.
    /// </summary>
    public static DateOnly Step(CalendarView view, DateOnly date, int direction)
    {
        var sign = direction < 0 ? -1 : 1;
        return view switch
        {
            CalendarView.Day => date.AddDays(sign),
            CalendarView.Month => date.AddMonths(sign),
            _ => date.AddDays(7 * sign)
        };
    }

    // ── The time grid ───────────────────────────────────────────────────

    /// <summary>
    /// Normalises the grid parameters. A <paramref name="dayEnd"/> that is not later
    /// than <paramref name="dayStart"/> (or lies beyond the following midnight)
    /// becomes the following midnight; a <paramref name="slotMinutes"/> that does not
    /// divide sixty becomes thirty. A <paramref name="dayStart"/> outside the day
    /// becomes midnight.
    /// </summary>
    public static CalendarGrid Grid(
        TimeSpan dayStart, TimeSpan dayEnd, int slotMinutes,
        out bool dayEndReplaced, out bool slotMinutesReplaced)
    {
        if (dayStart < TimeSpan.Zero || dayStart >= WholeDay) dayStart = TimeSpan.Zero;

        dayEndReplaced = dayEnd <= dayStart || dayEnd > WholeDay;
        if (dayEndReplaced) dayEnd = WholeDay;

        slotMinutesReplaced = slotMinutes <= 0 || 60 % slotMinutes != 0;
        if (slotMinutesReplaced) slotMinutes = 30;

        return new CalendarGrid(dayStart, dayEnd, slotMinutes);
    }

    // ── What an event's times mean ──────────────────────────────────────

    /// <summary>The event belongs in the all-day row: it is all-day, or lasts a day or longer.</summary>
    public static bool IsAllDayRow(CalendarEvent e) => e.AllDay || e.End - e.Start >= WholeDay;

    /// <summary>The month view draws the event as a bar: it is all-day, or touches more than one day.</summary>
    public static bool IsMonthBar(CalendarEvent e)
    {
        if (e.AllDay) return true;
        var (first, end) = DaySpan(e);
        return end.DayNumber - first.DayNumber > 1;
    }

    /// <summary>The days an event touches, the end exclusive.</summary>
    public static (DateOnly First, DateOnly End) DaySpan(CalendarEvent e)
    {
        var first = DateOnly.FromDateTime(e.Start);
        if (e.AllDay)
        {
            var allDayEnd = DateOnly.FromDateTime(e.End);
            return (first, allDayEnd > first ? allDayEnd : first.AddDays(1));
        }

        var end = e.End > e.Start ? e.End : e.Start;
        var endDay = DateOnly.FromDateTime(end);
        // An end at midnight belongs to the day before it — the end is exclusive.
        var exclusive = end.TimeOfDay == TimeSpan.Zero && endDay > first ? endDay : endDay.AddDays(1);
        return (first, exclusive);
    }

    /// <summary>The event touches at least one of the <paramref name="days"/> days from <paramref name="first"/>.</summary>
    public static bool Touches(CalendarEvent e, DateOnly first, int days)
    {
        var (start, end) = DaySpan(e);
        return start < first.AddDays(days) && end > first;
    }

    /// <summary>Keeps the first item of every id; reports whether a later one was dropped.</summary>
    public static List<T> DistinctById<T>(IEnumerable<T> items, Func<T, string> id, out bool hadDuplicates)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<T>();
        hadDuplicates = false;
        foreach (var item in items)
        {
            if (seen.Add(id(item))) result.Add(item);
            else hadDuplicates = true;
        }
        return result;
    }

    // ── Placement in the day and week views ─────────────────────────────

    /// <summary>
    /// The part of a timed event drawn in one day's column, or <c>null</c> when it
    /// lies wholly outside that day's grid. An event whose end is not later than
    /// its start, or that is shorter than a slot, is drawn one slot long.
    /// </summary>
    public static CalendarSegment? Segment(CalendarEvent e, DateOnly day, CalendarGrid grid)
    {
        var slot = TimeSpan.FromMinutes(grid.SlotMinutes);
        var start = e.Start;
        var end = e.End > e.Start ? e.End : e.Start + slot;

        var midnight = day.ToDateTime(TimeOnly.MinValue);
        var gridStart = midnight + grid.DayStart;
        var gridEnd = midnight + grid.DayEnd;
        if (end <= gridStart || start >= gridEnd) return null;

        var from = start < gridStart ? gridStart : start;
        var to = end > gridEnd ? gridEnd : end;
        var length = grid.Length.TotalMinutes;

        var height = Math.Min(Math.Max((to - from).TotalMinutes, grid.SlotMinutes) / length, 1);
        var top = (from - gridStart).TotalMinutes / length;
        // A short event at the very end keeps its slot height and moves up instead.
        if (top + height > 1) top = 1 - height;

        return new CalendarSegment(e, day, top, height,
            ContinuesBefore: start < midnight,
            ContinuesAfter: end > midnight.AddDays(1));
    }

    /// <summary>
    /// Lays the segments of one column side by side where they overlap. Ordered by
    /// start, longer first; each takes the first free position, and a group shares
    /// the column evenly. A segment that overlaps nothing keeps the full width.
    /// </summary>
    public static IReadOnlyList<CalendarSegment> Pack(IEnumerable<CalendarSegment> segments)
    {
        var ordered = segments
            .OrderBy(s => s.Top)
            .ThenByDescending(s => s.Height)
            .ThenBy(s => s.Event.Start)
            .ThenBy(s => s.Event.Id, StringComparer.Ordinal)
            .ToList();

        var result = new List<CalendarSegment>(ordered.Count);
        var group = new List<(CalendarSegment Segment, int Position)>();
        var positionEnds = new List<double>();
        var groupEnd = 0d;

        void Flush()
        {
            var count = positionEnds.Count;
            foreach (var (segment, position) in group)
                result.Add(segment with { Left = (double)position / count, Width = 1d / count });
            group.Clear();
            positionEnds.Clear();
        }

        foreach (var segment in ordered)
        {
            if (group.Count > 0 && segment.Top >= groupEnd - Epsilon) Flush();

            var position = positionEnds.FindIndex(end => end <= segment.Top + Epsilon);
            var bottom = segment.Top + segment.Height;
            if (position < 0)
            {
                position = positionEnds.Count;
                positionEnds.Add(bottom);
            }
            else
            {
                positionEnds[position] = bottom;
            }

            group.Add((segment, position));
            groupEnd = group.Count == 1 ? bottom : Math.Max(groupEnd, bottom);
        }

        if (group.Count > 0) Flush();
        return result;
    }

    /// <summary>The packed segments of one day's column.</summary>
    public static IReadOnlyList<CalendarSegment> Column(IEnumerable<CalendarEvent> events, DateOnly day, CalendarGrid grid)
        => Pack(events
            .Where(e => !IsAllDayRow(e))
            .Select(e => Segment(e, day, grid))
            .OfType<CalendarSegment>());

    // ── Bars: the all-day row and the month's week rows ─────────────────

    /// <summary>
    /// Places events as bars across <paramref name="days"/> day columns starting at
    /// <paramref name="first"/>, clipped to that span and stacked in lanes. Events
    /// outside the span are left out.
    /// </summary>
    public static IReadOnlyList<CalendarBar> Bars(IEnumerable<CalendarEvent> events, DateOnly first, int days)
    {
        var end = first.AddDays(days);
        var clipped = new List<(CalendarEvent Event, int Column, int Span, int FullSpan, bool Before, bool After)>();
        foreach (var e in events)
        {
            var (start, stop) = DaySpan(e);
            if (start >= end || stop <= first) continue;
            var from = start < first ? first : start;
            var to = stop > end ? end : stop;
            clipped.Add((e, from.DayNumber - first.DayNumber, to.DayNumber - from.DayNumber,
                stop.DayNumber - start.DayNumber, (start < first), (stop > end)));
        }

        var laneEnds = new List<int>();
        var result = new List<CalendarBar>(clipped.Count);
        foreach (var bar in clipped
                     .OrderBy(b => b.Column)
                     .ThenByDescending(b => b.FullSpan)
                     .ThenBy(b => b.Event.Start)
                     .ThenBy(b => b.Event.Id, StringComparer.Ordinal))
        {
            var lane = laneEnds.FindIndex(laneEnd => laneEnd <= bar.Column);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(0);
            }
            laneEnds[lane] = bar.Column + bar.Span;
            result.Add(new CalendarBar(bar.Event, bar.Column, bar.Span, lane, bar.Before, bar.After));
        }

        return result;
    }

    /// <summary>
    /// Places one week row of the month view. Bars sit above lines; lines are
    /// ordered by start. A cell holds <paramref name="capacity"/> rows; a day with
    /// more gives its last row to the overflow control, and what does not fit is
    /// counted in <see cref="CalendarMonthWeek.Hidden"/>. A bar is hidden as a whole.
    /// </summary>
    public static CalendarMonthWeek MonthWeek(IEnumerable<CalendarEvent> events, DateOnly first, int capacity)
    {
        const int days = 7;
        capacity = Math.Max(capacity, 1);
        var inWeek = events.Where(e => Touches(e, first, days)).ToList();

        var bars = Bars(inWeek.Where(IsMonthBar), first, days);
        var taken = new HashSet<int>[days];
        for (var d = 0; d < days; d++) taken[d] = [];
        foreach (var bar in bars)
            for (var d = bar.Column; d < bar.Column + bar.Span; d++)
                taken[d].Add(bar.Lane);

        var lines = new List<CalendarMonthLine>();
        foreach (var e in inWeek.Where(e => !IsMonthBar(e))
                     .OrderBy(e => e.Start)
                     .ThenBy(e => e.Id, StringComparer.Ordinal))
        {
            var column = DateOnly.FromDateTime(e.Start).DayNumber - first.DayNumber;
            var lane = 0;
            while (!taken[column].Add(lane)) lane++;
            lines.Add(new CalendarMonthLine(e, column, lane));
        }

        // Hiding cascades: a day that hides anything needs its last row for the
        // overflow control, which hides whatever sat there — on a bar's every day.
        var barHidden = bars.Select(b => b.Lane >= capacity).ToArray();
        var lineHidden = lines.Select(l => l.Lane >= capacity).ToArray();
        var overflow = new bool[days];
        bool changed;
        do
        {
            changed = false;
            for (var i = 0; i < bars.Count; i++)
                if (barHidden[i])
                    for (var d = bars[i].Column; d < bars[i].Column + bars[i].Span; d++)
                        overflow[d] = true;
            for (var i = 0; i < lines.Count; i++)
                if (lineHidden[i]) overflow[lines[i].Column] = true;

            for (var i = 0; i < bars.Count; i++)
            {
                if (barHidden[i] || bars[i].Lane != capacity - 1) continue;
                for (var d = bars[i].Column; d < bars[i].Column + bars[i].Span; d++)
                    if (overflow[d]) { barHidden[i] = changed = true; break; }
            }
            for (var i = 0; i < lines.Count; i++)
            {
                if (lineHidden[i] || lines[i].Lane != capacity - 1 || !overflow[lines[i].Column]) continue;
                lineHidden[i] = changed = true;
            }
        } while (changed);

        var hidden = new int[days];
        var total = new int[days];
        for (var i = 0; i < bars.Count; i++)
            for (var d = bars[i].Column; d < bars[i].Column + bars[i].Span; d++)
            {
                total[d]++;
                if (barHidden[i]) hidden[d]++;
            }
        for (var i = 0; i < lines.Count; i++)
        {
            total[lines[i].Column]++;
            if (lineHidden[i]) hidden[lines[i].Column]++;
        }

        return new CalendarMonthWeek(first,
            bars.Where((_, i) => !barHidden[i]).ToList(),
            lines.Where((_, i) => !lineHidden[i]).ToList(),
            hidden, total);
    }

    // ── Colour ──────────────────────────────────────────────────────────

    /// <summary>
    /// The colour slot of every person. An explicit slot is kept; <c>Auto</c> takes
    /// a slot by position in <paramref name="people"/>, cycling through the six —
    /// so hiding someone recolours nobody.
    /// </summary>
    public static IReadOnlyDictionary<string, CalendarColor> PersonColors(IReadOnlyList<CalendarPerson> people)
    {
        var colors = new Dictionary<string, CalendarColor>(people.Count, StringComparer.Ordinal);
        for (var i = 0; i < people.Count; i++)
        {
            colors[people[i].Id] = people[i].Color != CalendarColor.Auto
                ? people[i].Color
                : CalendarColor.Slot1 + i % 6;
        }
        return colors;
    }

    /// <summary>
    /// The colour slot of an event: its own, else that of its first person that is
    /// known, else the first slot.
    /// </summary>
    public static CalendarColor EventColor(CalendarEvent e, IReadOnlyDictionary<string, CalendarColor> personColors)
    {
        if (e.Color != CalendarColor.Auto) return e.Color;
        foreach (var id in e.PersonIds)
            if (personColors.TryGetValue(id, out var color)) return color;
        return CalendarColor.Slot1;
    }

    /// <summary>The number (1…6) of a resolved slot, for a <c>data-color</c> attribute.</summary>
    public static int SlotNumber(CalendarColor color)
        => color == CalendarColor.Auto ? 1 : (int)color - (int)CalendarColor.Slot1 + 1;

    // ── People ──────────────────────────────────────────────────────────

    /// <summary>None of the event's person ids matches a known person.</summary>
    public static bool IsUnassigned(CalendarEvent e, IReadOnlySet<string> knownPersonIds)
    {
        foreach (var id in e.PersonIds)
            if (knownPersonIds.Contains(id)) return false;
        return true;
    }

    /// <summary>
    /// The event is rendered: it is unassigned, or at least one of its known people
    /// is not hidden.
    /// </summary>
    public static bool IsVisible(CalendarEvent e, IReadOnlySet<string> knownPersonIds, IReadOnlySet<string>? hiddenPersonIds)
    {
        var assigned = false;
        foreach (var id in e.PersonIds)
        {
            if (!knownPersonIds.Contains(id)) continue;
            assigned = true;
            if (hiddenPersonIds is null || !hiddenPersonIds.Contains(id)) return true;
        }
        return !assigned;
    }

    // ── Availability ────────────────────────────────────────────────────

    /// <summary>
    /// The unavailable time of one person within one day's grid, merged and in
    /// order. On a day with at least one working span, everything outside the
    /// working spans is unavailable; an absence is unavailable wherever it lies.
    /// </summary>
    /// <param name="spans">The spans of that one person.</param>
    /// <param name="day">The day whose grid is asked about.</param>
    /// <param name="grid">The normalised time grid.</param>
    public static IReadOnlyList<CalendarSpan> Unavailable(IEnumerable<CalendarAvailability> spans, DateOnly day, CalendarGrid grid)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        var nextMidnight = midnight.AddDays(1);
        var working = new List<CalendarSpan>();
        var unavailable = new List<CalendarSpan>();
        var hasWorking = false;

        foreach (var span in spans)
        {
            if (span.End <= span.Start || span.End <= midnight || span.Start >= nextMidnight) continue;
            var clipped = Clip(span, midnight, grid);
            if (span.Kind == CalendarAvailabilityKind.Working)
            {
                hasWorking = true;
                if (clipped is { } w) working.Add(w);
            }
            else if (clipped is { } a)
            {
                unavailable.Add(a);
            }
        }

        if (hasWorking)
        {
            var cursor = grid.DayStart;
            foreach (var w in Merge(working))
            {
                if (w.Start > cursor) unavailable.Add(new CalendarSpan(cursor, w.Start));
                cursor = w.End;
            }
            if (cursor < grid.DayEnd) unavailable.Add(new CalendarSpan(cursor, grid.DayEnd));
        }

        return Merge(unavailable);
    }

    /// <summary>The absences of one person within one day's grid, in order of start.</summary>
    /// <param name="spans">The spans of that one person.</param>
    /// <param name="day">The day whose grid is asked about.</param>
    /// <param name="grid">The normalised time grid.</param>
    public static IReadOnlyList<CalendarAbsence> Absences(IEnumerable<CalendarAvailability> spans, DateOnly day, CalendarGrid grid)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        var result = new List<CalendarAbsence>();
        foreach (var span in spans)
        {
            if (span.Kind != CalendarAvailabilityKind.Absent || span.End <= span.Start) continue;
            if (Clip(span, midnight, grid) is not { } clipped) continue;
            result.Add(new CalendarAbsence(span, clipped,
                WholeDay: clipped.Start == grid.DayStart && clipped.End == grid.DayEnd));
        }
        return result.OrderBy(a => a.Span.Start).ToList();
    }

    /// <summary>
    /// The time at which every one of the given people is unavailable. No people
    /// gives no time.
    /// </summary>
    public static IReadOnlyList<CalendarSpan> Intersect(IReadOnlyList<IReadOnlyList<CalendarSpan>> perPerson)
    {
        if (perPerson.Count == 0) return [];
        var current = perPerson[0];
        for (var p = 1; p < perPerson.Count && current.Count > 0; p++)
        {
            var other = perPerson[p];
            var next = new List<CalendarSpan>();
            int i = 0, j = 0;
            while (i < current.Count && j < other.Count)
            {
                var start = current[i].Start > other[j].Start ? current[i].Start : other[j].Start;
                var end = current[i].End < other[j].End ? current[i].End : other[j].End;
                if (start < end) next.Add(new CalendarSpan(start, end));
                if (current[i].End < other[j].End) i++; else j++;
            }
            current = next;
        }
        return current;
    }

    private static CalendarSpan? Clip(CalendarAvailability span, DateTime midnight, CalendarGrid grid)
    {
        var start = span.Start - midnight;
        var end = span.End - midnight;
        if (start < grid.DayStart) start = grid.DayStart;
        if (end > grid.DayEnd) end = grid.DayEnd;
        return start < end ? new CalendarSpan(start, end) : null;
    }

    private static List<CalendarSpan> Merge(List<CalendarSpan> spans)
    {
        var merged = new List<CalendarSpan>(spans.Count);
        foreach (var span in spans.OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && span.Start <= merged[^1].End)
            {
                if (span.End > merged[^1].End) merged[^1] = merged[^1] with { End = span.End };
            }
            else
            {
                merged.Add(span);
            }
        }
        return merged;
    }
}
