namespace BlazorScheduler;

/// <summary>A date range displayed or selected by a scheduler.</summary>
public readonly record struct SchedulerRange(DateTime Start, DateTime End);

/// <summary>Template context for a scheduler item.</summary>
public readonly record struct SchedulerItemContext<TItem>(TItem Item, bool IsTimed);

/// <summary>Describes a requested item reschedule.</summary>
public readonly record struct SchedulerItemRescheduleEventArgs<TItem>(TItem Item, DateTime Start, DateTime End);

/// <summary>Describes the items hidden by a day's overflow indicator.</summary>
public sealed record SchedulerOverflowEventArgs<TItem>(DateTime Day, IReadOnlyList<TItem> Items);

