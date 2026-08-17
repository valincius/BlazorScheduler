namespace BlazorScheduler.Internal.Layout;

internal readonly record struct SchedulerLayoutInput<TItem>(
    TItem Item, object? Key, DateTime Start, DateTime End,
    string? Color, string? Class, string? Style, int SourceIndex);

internal sealed record SchedulerPlacedItem<TItem>(
    SchedulerLayoutInput<TItem> Input, int StartDay, int EndDay, int Order)
{
    public bool IsTimed => Input.Start.Date == Input.End.Date && Input.Start != Input.End;
}

internal sealed record SchedulerDayOverflow<TItem>(DateTime Day, IReadOnlyList<TItem> Items);

internal sealed record SchedulerWeekLayout<TItem>(
    DateTime Start, DateTime End, IReadOnlyList<SchedulerPlacedItem<TItem>> Items,
    IReadOnlyList<SchedulerDayOverflow<TItem>> Overflows, int VisibleRows);

internal static class SchedulerLayoutPlanner
{
    public static IReadOnlyList<SchedulerWeekLayout<TItem>> Build<TItem>(
        IReadOnlyList<SchedulerLayoutInput<TItem>> source,
        DateTime rangeStart,
        DateTime rangeEnd,
        int maxVisibleItems)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxVisibleItems, 1);

        var visible = source
            .Where(item => item.Start.Date <= rangeEnd && rangeStart <= item.End.Date)
            .OrderBy(item => item.Start)
            .ThenByDescending(item => item.End - item.Start)
            .ThenBy(item => item.SourceIndex)
            .ToArray();

        var weeks = new List<SchedulerWeekLayout<TItem>>();
        for (var weekStart = rangeStart.Date; weekStart <= rangeEnd.Date; weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(6);
            var occupancy = new List<byte>();
            var placed = new List<SchedulerPlacedItem<TItem>>();

            foreach (var item in visible)
            {
                if (item.Start.Date > weekEnd || item.End.Date < weekStart)
                {
                    continue;
                }

                var startDay = Math.Clamp((item.Start.Date - weekStart).Days, 0, 6);
                var endDay = Math.Clamp((item.End.Date - weekStart).Days, 0, 6);
                var mask = CreateMask(startDay, endDay);
                var slot = 0;
                while (slot < occupancy.Count && (occupancy[slot] & mask) != 0)
                {
                    slot++;
                }

                if (slot == occupancy.Count)
                {
                    occupancy.Add(0);
                }

                occupancy[slot] |= mask;
                placed.Add(new SchedulerPlacedItem<TItem>(item, startDay, endDay, slot + 1));
            }

            var overflows = new List<SchedulerDayOverflow<TItem>>();
            for (var day = 0; day < 7; day++)
            {
                var hidden = placed
                    .Where(item => item.Order > maxVisibleItems && day >= item.StartDay && day <= item.EndDay)
                    .Select(item => item.Input.Item)
                    .ToArray();
                if (hidden.Length > 0)
                {
                    overflows.Add(new SchedulerDayOverflow<TItem>(weekStart.AddDays(day), hidden));
                }
            }

            weeks.Add(new SchedulerWeekLayout<TItem>(
                weekStart, weekEnd, placed, overflows, Math.Min(occupancy.Count, maxVisibleItems)));
        }

        return weeks;
    }

    private static byte CreateMask(int startDay, int endDay)
    {
        var width = endDay - startDay + 1;
        return (byte)(((1 << width) - 1) << startDay);
    }
}
