using System.Globalization;
using BlazorScheduler.Internal.Layout;
using Microsoft.AspNetCore.Components;

namespace BlazorScheduler.Internal.Components;

/// <summary>Renders the week view for <see cref="DataScheduler{TItem}"/>.</summary>
public partial class SchedulerWeekView<TItem>
{
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter, EditorRequired] public Func<TItem, object?> ItemKey { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemStart { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemEnd { get; set; } = null!;
    [Parameter] public Func<TItem, string?>? ItemColor { get; set; }
    [Parameter] public Func<TItem, string?>? ItemClass { get; set; }
    [Parameter] public Func<TItem, string?>? ItemStyle { get; set; }
    [Parameter] public SchedulerRange Range { get; set; }
    [Parameter] public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
    [Parameter] public int WeekViewStartHour { get; set; }
    [Parameter] public int WeekViewEndHour { get; set; } = 24;
    [Parameter] public int WeekViewHourHeight { get; set; } = 60;
    [Parameter] public bool Use24HourClock { get; set; } = true;
    [Parameter] public int? DraggedIndex { get; set; }
    [Parameter] public DateTime? DragStart { get; set; }
    [Parameter] public DateTime? DragEnd { get; set; }
    [Parameter] public string NewAppointmentText { get; set; } = "New Appointment";
    [Parameter] public string PlusOthersText { get; set; } = "+ {n} others";
    [Parameter] public RenderFragment<SchedulerItemContext<TItem>>? ItemTemplate { get; set; }
    [Parameter] public RenderFragment<DateTime>? WeekDayHeaderTemplate { get; set; }
    [Parameter] public string ThemeColor { get; set; } = "aqua";
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<SchedulerOverflowEventArgs<TItem>> OnOverflowClick { get; set; }

    private SchedulerWeekViewLayout<TItem>? _layout;

    protected override void OnParametersSet()
    {
        var inputs = BuildInputs();
        _layout = SchedulerWeekViewPlanner.Build(
            inputs, Range.Start, Range.End, MaxVisibleAppointmentsPerDay, MaxVisibleAppointmentsPerDay,
            TimeSpan.FromHours(WeekViewStartHour), TimeSpan.FromHours(WeekViewEndHour));
    }

    private IEnumerable<SchedulerPlacedItem<TItem>> AllDayItems =>
        _layout is null ? Enumerable.Empty<SchedulerPlacedItem<TItem>>() : _layout.AllDayItems;

    private bool IsAllDayItemVisible(SchedulerPlacedItem<TItem> placed)
    {
        // Mirrors the month view: when any day spanned by the item shows an
        // overflow chip, the item yields its last row so the chip has a slot.
        var hasOverflowInSpan = _layout!.AllDayOverflows.Any(overflow =>
            overflow.Day.Date >= placed.Input.Start.Date && overflow.Day.Date <= placed.Input.End.Date);
        return placed.Order <= (hasOverflowInSpan ? MaxVisibleAppointmentsPerDay - 1 : MaxVisibleAppointmentsPerDay);
    }

    private string FormatHour(int hour) => Use24HourClock
        ? hour.ToString("00", CultureInfo.InvariantCulture) + ":00"
        : new DateTime(2000, 1, 1, hour, 0, 0).ToString("h tt", CultureInfo.InvariantCulture);

    /// <summary>
    /// Computes the CSS variables for the create-drag preview bar on the given day,
    /// or null when no create drag is over that day column.
    /// </summary>
    private string? CreatePreviewStyle(DateTime day)
    {
        if (DraggedIndex is not null || DragStart is not DateTime start || DragEnd is not DateTime end || start.Date != day.Date)
        {
            return null;
        }
        var totalMinutes = (WeekViewEndHour - WeekViewStartHour) * 60.0;
        var top = ((start.TimeOfDay.TotalMinutes - WeekViewStartHour * 60) / totalMinutes * 100.0)
            .ToString("0.####", CultureInfo.InvariantCulture);
        var height = ((end - start).TotalMinutes / totalMinutes * 100.0)
            .ToString("0.####", CultureInfo.InvariantCulture);
        return $"--top:{top}%;--height:{height}%;";
    }

    private SchedulerLayoutInput<TItem>[] BuildInputs()
    {
        var inputs = new SchedulerLayoutInput<TItem>[Items.Count];
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            inputs[index] = new SchedulerLayoutInput<TItem>(item, ItemKey(item), ItemStart(item), ItemEnd(item),
                ItemColor?.Invoke(item) ?? ThemeColor, ItemClass?.Invoke(item), ItemStyle?.Invoke(item), index);
        }
        return inputs;
    }

    private double TotalMinutes => (WeekViewEndHour - WeekViewStartHour) * 60.0;

    private string TimedStyle(SchedulerTimedItem<TItem> item)
    {
        var top = (item.StartTime.TotalMinutes / TotalMinutes * 100.0).ToString("0.####", CultureInfo.InvariantCulture);
        var height = ((item.EndTime - item.StartTime).TotalMinutes / TotalMinutes * 100.0).ToString("0.####", CultureInfo.InvariantCulture);
        return $"--startMinutes:{(int)item.StartTime.TotalMinutes};--durationMinutes:{(int)(item.EndTime - item.StartTime).TotalMinutes};--top:{top}%;--height:{height}%;--slot:{item.Slot};--maxSlots:{item.MaxSlots};background-color:{item.Input.Color};";
    }

    private Task ItemClickedAsync(TItem item) => OnItemClick.InvokeAsync(item);

    private Task OverflowClickedAsync(SchedulerDayOverflow<TItem> overflow) =>
        OnOverflowClick.InvokeAsync(new SchedulerOverflowEventArgs<TItem>(overflow.Day, overflow.Items));
}
