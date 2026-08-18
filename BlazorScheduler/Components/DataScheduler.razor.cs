using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BlazorScheduler.Internal.Extensions;
using BlazorScheduler.Internal.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorScheduler;

/// <summary>A high-performance, data-driven scheduler component.</summary>
public partial class DataScheduler<TItem> : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter, EditorRequired] public Func<TItem, object?> ItemKey { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemStart { get; set; } = null!;
    [Parameter, EditorRequired] public Func<TItem, DateTime> ItemEnd { get; set; } = null!;
    [Parameter] public Func<TItem, string?>? ItemColor { get; set; }
    [Parameter] public Func<TItem, string?>? ItemClass { get; set; }
    [Parameter] public Func<TItem, string?>? ItemStyle { get; set; }
    [Parameter] public RenderFragment<SchedulerItemContext<TItem>>? ItemTemplate { get; set; }
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment<DateTime>? DayTemplate { get; set; }
    [Parameter] public EventCallback<SchedulerRange> OnRangeChanged { get; set; }
    [Parameter] public EventCallback<SchedulerRange> OnCreate { get; set; }
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<SchedulerItemRescheduleEventArgs<TItem>> OnItemReschedule { get; set; }
    [Parameter] public EventCallback<SchedulerOverflowEventArgs<TItem>> OnOverflowClick { get; set; }

    [Parameter] public bool AlwaysShowYear { get; set; } = true;
    [Parameter] public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
    [Parameter] public bool EnableDragging { get; set; } = true;
    [Parameter] public bool EnableAppointmentsCreationFromScheduler { get; set; } = true;
    [Parameter] public bool EnableRescheduling { get; set; }
    [Parameter] public string ThemeColor { get; set; } = "aqua";
    [Parameter] public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;
    [Parameter] public string TodayButtonText { get; set; } = "Today";
    [Parameter] public string PlusOthersText { get; set; } = "+ {n} others";
    [Parameter] public string NewAppointmentText { get; set; } = "New Appointment";
    [Parameter] public string NewAppointmentColor { get; set; } = "#bce";
    [Parameter] public string? RootDaysGroupInWeekStyle { get; set; }
    [Parameter] public string? RootAppointmentOverflowStyle { get; set; }
    [Parameter] public string? RootDayClass { get; set; }
    [Parameter] public string? RootDayStyle { get; set; }

    [Parameter] public SchedulerView View { get; set; } = SchedulerView.Month;
    [Parameter] public EventCallback<SchedulerView> ViewChanged { get; set; }
    [Parameter] public bool ShowViewSwitcher { get; set; } = true;
    [Parameter] public int WeekViewStartHour { get; set; }
    [Parameter] public int WeekViewEndHour { get; set; } = 24;
    [Parameter] public int WeekViewHourHeight { get; set; } = 60;
    [Parameter] public RenderFragment<DateTime>? WeekDayHeaderTemplate { get; set; }

    public DateTime CurrentDate { get; private set; } = DateTime.Today;

    public SchedulerRange CurrentRange
    {
        get
        {
            if (View == SchedulerView.Week)
            {
                var start = CurrentDate.Date.GetPrevious(StartDayOfWeek);
                return new SchedulerRange(start, start.AddDays(6));
            }

            var first = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
            var monthStart = first.GetPrevious(StartDayOfWeek);
            var last = new DateTime(CurrentDate.Year, CurrentDate.Month, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month));
            var endDay = (DayOfWeek)(((int)StartDayOfWeek + 6) % 7);
            return new SchedulerRange(monthStart, last.GetNext(endDay));
        }
    }

    private ElementReference _root;
    private IJSObjectReference? _module;
    private IJSObjectReference? _jsInstance;
    private DotNetObjectReference<DataScheduler<TItem>>? _objectReference;
    private bool _loading;
    private bool _interactiveInitialized;
    private int _requestVersion;
    private int? _draggedIndex;
    private DateTime? _dragAnchor;
    private DateTime? _dragStart;
    private DateTime? _dragEnd;
    private bool _didDrag;

    private string DisplayText
    {
        get
        {
            if (View == SchedulerView.Week)
            {
                var (start, end) = (CurrentRange.Start, CurrentRange.End);
                if (start.Year != end.Year)
                {
                    return $"{start.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)} – {end.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)}";
                }
                if (start.Month != end.Month)
                {
                    return $"{start.ToString("MMM d", CultureInfo.CurrentCulture)} – {end.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)}";
                }
                return $"{start.ToString("MMM d", CultureInfo.CurrentCulture)} – {end.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)}";
            }

            return AlwaysShowYear || CurrentDate.Year != DateTime.Today.Year
                ? CurrentDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
                : CurrentDate.ToString("MMMM", CultureInfo.CurrentCulture);
        }
    }

    private string PreviousLabel => View == SchedulerView.Week ? "Previous week" : "Previous month";
    private string NextLabel => View == SchedulerView.Week ? "Next week" : "Next month";

    protected override void OnParametersSet()
    {
        ValidateParameters();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorScheduler/js/scheduler.js");
            _objectReference = DotNetObjectReference.Create(this);
            _jsInstance = await _module.InvokeAsync<IJSObjectReference>("create", _root, _objectReference);
            _interactiveInitialized = true;
            await RequestRangeAsync();
        }
        catch (InvalidOperationException)
        {
            // Static prerendering has no JavaScript runtime. The interactive instance initializes later.
        }
    }

    /// <summary>Moves the scheduler to the given anchor date and refreshes the displayed range.</summary>
    public Task SetCurrentMonthAsync(DateTime date) => ChangeRangeAsync(date);

    private Task GoToTodayAsync() => ChangeRangeAsync(DateTime.Today);
    private Task PreviousAsync() => ChangeRangeAsync(View == SchedulerView.Week ? CurrentDate.AddDays(-7) : CurrentDate.AddMonths(-1));
    private Task NextAsync() => ChangeRangeAsync(View == SchedulerView.Week ? CurrentDate.AddDays(7) : CurrentDate.AddMonths(1));

    private async Task ChangeRangeAsync(DateTime date)
    {
        CurrentDate = date;
        await RequestRangeAsync();
    }

    private async Task SetViewAsync(SchedulerView view)
    {
        if (view == View)
        {
            return;
        }
        View = view;
        await ViewChanged.InvokeAsync(view);
        await RequestRangeAsync();
    }

    private async Task RequestRangeAsync()
    {
        var request = ++_requestVersion;
        _loading = true;
        StateHasChanged();
        try
        {
            await OnRangeChanged.InvokeAsync(CurrentRange);
        }
        finally
        {
            if (request == _requestVersion)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    private void ValidateParameters()
    {
        if (ItemKey is null || ItemStart is null || ItemEnd is null)
        {
            throw new InvalidOperationException("ItemKey, ItemStart, and ItemEnd are required.");
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxVisibleAppointmentsPerDay, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(WeekViewStartHour, 23);
        ArgumentOutOfRangeException.ThrowIfLessThan(WeekViewEndHour, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(WeekViewEndHour, 24);
        if (WeekViewStartHour >= WeekViewEndHour)
        {
            throw new ArgumentOutOfRangeException(nameof(WeekViewStartHour), "WeekViewStartHour must be less than WeekViewEndHour.");
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(WeekViewHourHeight, 1);
        if (!Enum.IsDefined(View))
        {
            throw new ArgumentOutOfRangeException(nameof(View), View, "View must be a defined SchedulerView value.");
        }

        var keys = new HashSet<object?>();
        foreach (var item in Items)
        {
            var key = ItemKey(item);
            if (key is null || !keys.Add(key))
            {
                throw new InvalidOperationException("Every scheduler item must have a non-null, unique ItemKey.");
            }
            if (ItemEnd(item) < ItemStart(item))
            {
                throw new InvalidOperationException($"Scheduler item '{key}' ends before it starts.");
            }
        }
    }

    private async Task ItemClickedAsync(TItem item)
    {
        if (_didDrag)
        {
            _didDrag = false;
            return;
        }
        await OnItemClick.InvokeAsync(item);
    }

    private Task WeekItemClickedAsync(TItem item) => OnItemClick.InvokeAsync(item);

    private Task MonthOverflowClickedAsync(SchedulerOverflowEventArgs<TItem> args) => OnOverflowClick.InvokeAsync(args);

    private Task WeekOverflowClickedAsync(SchedulerOverflowEventArgs<TItem> args) => OnOverflowClick.InvokeAsync(args);

    [JSInvokable]
    public void BeginDayDrag(string value)
    {
        if (!EnableDragging || !EnableAppointmentsCreationFromScheduler)
        {
            return;
        }
        var day = ParseDay(value);
        _draggedIndex = null;
        _dragAnchor = _dragStart = _dragEnd = day;
        _didDrag = false;
    }

    [JSInvokable]
    public void BeginItemDrag(int sourceIndex, string value)
    {
        if (!EnableDragging || !EnableRescheduling || sourceIndex < 0 || sourceIndex >= Items.Count)
        {
            return;
        }
        _draggedIndex = sourceIndex;
        _dragAnchor = ParseDay(value);
        _dragStart = ItemStart(Items[sourceIndex]);
        _dragEnd = ItemEnd(Items[sourceIndex]);
        _didDrag = false;
    }

    [JSInvokable]
    public void DragTo(string value)
    {
        if (!_dragAnchor.HasValue)
        {
            return;
        }
        var day = ParseDay(value);
        _didDrag = true;
        if (_draggedIndex.HasValue)
        {
            var item = Items[_draggedIndex.Value];
            var difference = (day - _dragAnchor.Value).Days;
            _dragStart = ItemStart(item).AddDays(difference);
            _dragEnd = ItemEnd(item).AddDays(difference);
        }
        else
        {
            if (day < _dragAnchor.Value)
            {
                _dragStart = day;
                _dragEnd = _dragAnchor.Value;
            }
            else
            {
                _dragStart = _dragAnchor.Value;
                _dragEnd = day;
            }
        }
        StateHasChanged();
    }

    [JSInvokable]
    public async Task CompleteDrag()
    {
        if (!_dragStart.HasValue || !_dragEnd.HasValue)
        {
            ClearDrag();
            return;
        }
        if (_draggedIndex.HasValue && !_didDrag)
        {
            ClearDrag();
            return;
        }
        if (_draggedIndex.HasValue)
        {
            var item = Items[_draggedIndex.Value];
            await OnItemReschedule.InvokeAsync(new SchedulerItemRescheduleEventArgs<TItem>(item, _dragStart.Value, _dragEnd.Value));
        }
        else
        {
            await OnCreate.InvokeAsync(new SchedulerRange(_dragStart.Value, _dragEnd.Value));
        }
        ClearDrag();
        StateHasChanged();
    }

    private void ClearDrag()
    {
        _draggedIndex = null;
        _dragAnchor = _dragStart = _dragEnd = null;
    }

    private static DateTime ParseDay(string value) => DateTime.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture);

    public async ValueTask DisposeAsync()
    {
        if (_jsInstance is not null && _interactiveInitialized)
        {
            try
            {
                await _jsInstance.InvokeVoidAsync("dispose");
                await _jsInstance.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (InvalidOperationException) { }
        }
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        _objectReference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
