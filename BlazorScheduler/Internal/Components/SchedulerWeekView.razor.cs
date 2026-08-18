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

    private IEnumerable<SchedulerPlacedItem<TItem>> VisibleAllDayItems =>
        _layout is null ? Enumerable.Empty<SchedulerPlacedItem<TItem>>() : _layout.AllDayItems.Where(item => item.Order <= MaxVisibleAppointmentsPerDay);

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
