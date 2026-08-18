namespace BlazorScheduler;

/// <summary>The calendar view displayed by a scheduler.</summary>
public enum SchedulerView
{
    /// <summary>A month grid of weeks starting on <c>StartDayOfWeek</c>.</summary>
    Month,

    /// <summary>A single week with an all-day strip and a time grid.</summary>
    Week
}

/// <summary>A date range displayed or selected by a scheduler.</summary>
public readonly record struct SchedulerRange(DateTime Start, DateTime End);

/// <summary>Template context for a scheduler item.</summary>
public readonly record struct SchedulerItemContext<TItem>(TItem Item, bool IsTimed);

/// <summary>Describes a requested item reschedule.</summary>
public readonly record struct SchedulerItemRescheduleEventArgs<TItem>(TItem Item, DateTime Start, DateTime End);

/// <summary>Describes the items hidden by a day's overflow indicator.</summary>
public sealed record SchedulerOverflowEventArgs<TItem>(DateTime Day, IReadOnlyList<TItem> Items);

