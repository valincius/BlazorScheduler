namespace BlazorScheduler.Internal.Layout;

/// <summary>A timed appointment placed in a week-view day column.</summary>
internal sealed record SchedulerTimedItem<TItem>(
    SchedulerLayoutInput<TItem> Input,
    int Slot,
    int MaxSlots,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool ClippedAtStart,
    bool ClippedAtEnd);

/// <summary>The timed appointments and overflows for a single day column.</summary>
internal sealed record SchedulerTimedDayLayout<TItem>(
    DateTime Day,
    IReadOnlyList<SchedulerTimedItem<TItem>> Items,
    IReadOnlyList<SchedulerDayOverflow<TItem>> Overflows);

/// <summary>The complete layout for one week in the week view.</summary>
internal sealed record SchedulerWeekViewLayout<TItem>(
    DateTime Start,
    DateTime End,
    IReadOnlyList<SchedulerPlacedItem<TItem>> AllDayItems,
    IReadOnlyList<SchedulerDayOverflow<TItem>> AllDayOverflows,
    int AllDayVisibleRows,
    IReadOnlyList<SchedulerTimedDayLayout<TItem>> Days);

/// <summary>
/// Builds the layout for the week view: an all-day strip (reusing <see cref="SchedulerLayoutPlanner"/>)
/// plus per-day timed columns with overlap partitioning, view-window clipping, and overflow reporting.
/// </summary>
internal static class SchedulerWeekViewPlanner
{
    /// <summary>
    /// Builds the layout for a single week.
    /// Items are classified as timed (same-day, non-zero duration) or all-day (multi-day or zero-duration).
    /// </summary>
    public static SchedulerWeekViewLayout<TItem> Build<TItem>(
        IReadOnlyList<SchedulerLayoutInput<TItem>> source,
        DateTime weekStart,
        DateTime weekEnd,
        int maxAllDayRows,
        int maxTimedPerDay,
        TimeSpan viewStart,
        TimeSpan viewEnd)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllDayRows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTimedPerDay, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(viewStart, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(viewEnd, TimeSpan.FromHours(24));
        if (viewEnd <= viewStart)
        {
            throw new ArgumentOutOfRangeException(nameof(viewEnd), "The view window end must be after the view window start.");
        }

        var allDay = new List<SchedulerLayoutInput<TItem>>();
        var timed = new List<SchedulerLayoutInput<TItem>>();
        foreach (var item in source)
        {
            if (item.Start.Date == item.End.Date && item.Start != item.End)
            {
                timed.Add(item);
            }
            else
            {
                allDay.Add(item);
            }
        }

        var allDayWeek = allDay.Count == 0
            ? new SchedulerWeekLayout<TItem>(weekStart.Date, weekEnd.Date, Array.Empty<SchedulerPlacedItem<TItem>>(), Array.Empty<SchedulerDayOverflow<TItem>>(), 0)
            : SchedulerLayoutPlanner.Build(allDay, weekStart.Date, weekEnd.Date, maxAllDayRows).Single();

        var days = new SchedulerTimedDayLayout<TItem>[7];
        for (var offset = 0; offset < 7; offset++)
        {
            days[offset] = BuildDay(timed, weekStart.Date.AddDays(offset), maxTimedPerDay, viewStart, viewEnd);
        }

        return new SchedulerWeekViewLayout<TItem>(
            weekStart.Date,
            weekEnd.Date,
            allDayWeek.Items,
            allDayWeek.Overflows,
            allDayWeek.VisibleRows,
            days);
    }

    private static SchedulerTimedDayLayout<TItem> BuildDay<TItem>(
        IReadOnlyList<SchedulerLayoutInput<TItem>> timed,
        DateTime day,
        int maxTimedPerDay,
        TimeSpan viewStart,
        TimeSpan viewEnd)
    {
        var candidates = timed
            .Where(item => item.Start.Date == day)
            .OrderBy(item => item.Start)
            .ThenBy(item => item.SourceIndex)
            .ToArray();

        var slotEnds = new List<TimeSpan>();
        var assigned = new List<(SchedulerLayoutInput<TItem> Input, int Slot, TimeSpan Start, TimeSpan End, bool ClippedAtStart, bool ClippedAtEnd)>();
        var overflowItems = new List<TItem>();

        foreach (var item in candidates)
        {
            var start = item.Start.TimeOfDay;
            var end = item.End.TimeOfDay;
            if (end <= viewStart || start >= viewEnd)
            {
                continue;
            }

            var clippedStart = start < viewStart ? viewStart : start;
            var clippedEnd = end > viewEnd ? viewEnd : end;

            // Earliest-finish interval partitioning: reuse the first slot whose last item has ended.
            var slot = 0;
            while (slot < slotEnds.Count && slotEnds[slot] > start)
            {
                slot++;
            }
            if (slot == slotEnds.Count)
            {
                slotEnds.Add(end);
            }
            else
            {
                slotEnds[slot] = end;
            }

            if (slot < maxTimedPerDay)
            {
                assigned.Add((item, slot, clippedStart, clippedEnd, (start < viewStart), (end > viewEnd)));
            }
            else
            {
                overflowItems.Add(item.Item);
            }
        }

        var items = assigned
            .Select(entry => new SchedulerTimedItem<TItem>(
                entry.Input, entry.Slot, slotEnds.Count, entry.Start, entry.End, entry.ClippedAtStart, entry.ClippedAtEnd))
            .OrderBy(entry => entry.Slot)
            .ThenBy(entry => entry.StartTime)
            .ToArray();

        var overflows = overflowItems.Count == 0
            ? Array.Empty<SchedulerDayOverflow<TItem>>()
            : new[] { new SchedulerDayOverflow<TItem>(day, overflowItems.ToArray()) };

        return new SchedulerTimedDayLayout<TItem>(day, items, overflows);
    }
}
