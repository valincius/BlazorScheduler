using BlazorScheduler.Internal.Layout;
using Microsoft.AspNetCore.Components;

namespace BlazorScheduler.Internal.Components;

/// <summary>Renders the month grid for <see cref="DataScheduler{TItem}"/>.</summary>
public partial class SchedulerMonthView<TItem>
{
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter, EditorRequired] public Func<TItem, object?> ItemKey { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemStart { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemEnd { get; set; } = null!;
    [Parameter] public Func<TItem, string?>? ItemColor { get; set; }
    [Parameter] public Func<TItem, string?>? ItemClass { get; set; }
    [Parameter] public Func<TItem, string?>? ItemStyle { get; set; }
    [Parameter] public SchedulerRange Range { get; set; }
    [Parameter] public DateTime CurrentDate { get; set; }
    [Parameter] public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
    [Parameter] public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;
    [Parameter] public int? DraggedIndex { get; set; }
    [Parameter] public DateTime? DragStart { get; set; }
    [Parameter] public DateTime? DragEnd { get; set; }
    [Parameter] public string NewAppointmentColor { get; set; } = "#bce";
    [Parameter] public string NewAppointmentText { get; set; } = "New Appointment";
    [Parameter] public string PlusOthersText { get; set; } = "+ {n} others";
    [Parameter] public string? RootDaysGroupInWeekStyle { get; set; }
    [Parameter] public string? RootAppointmentOverflowStyle { get; set; }
    [Parameter] public string? RootDayClass { get; set; }
    [Parameter] public string? RootDayStyle { get; set; }
    [Parameter] public RenderFragment<DateTime>? DayTemplate { get; set; }
    [Parameter] public RenderFragment<SchedulerItemContext<TItem>>? ItemTemplate { get; set; }
    [Parameter] public string ThemeColor { get; set; } = "aqua";
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<SchedulerOverflowEventArgs<TItem>> OnOverflowClick { get; set; }

    private IReadOnlyList<SchedulerWeekLayout<TItem>> _weeks = Array.Empty<SchedulerWeekLayout<TItem>>();

    protected override void OnParametersSet()
    {
        var inputs = BuildInputs();
        _weeks = SchedulerLayoutPlanner.Build(inputs, Range.Start, Range.End, MaxVisibleAppointmentsPerDay);
    }

    private SchedulerLayoutInput<TItem>[] BuildInputs()
    {
        var inputs = new SchedulerLayoutInput<TItem>[Items.Count];
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            var start = ItemStart(item);
            var end = ItemEnd(item);
            if (DraggedIndex == index && DragStart.HasValue && DragEnd.HasValue)
            {
                start = DragStart.Value;
                end = DragEnd.Value;
            }
            inputs[index] = new SchedulerLayoutInput<TItem>(item, ItemKey(item), start, end,
                ItemColor?.Invoke(item) ?? ThemeColor, ItemClass?.Invoke(item), ItemStyle?.Invoke(item), index);
        }
        return inputs;
    }

    private Task ItemClickedAsync(TItem item) => OnItemClick.InvokeAsync(item);

    private Task OverflowClickedAsync(SchedulerDayOverflow<TItem> overflow) =>
        OnOverflowClick.InvokeAsync(new SchedulerOverflowEventArgs<TItem>(overflow.Day, overflow.Items));
}
