using Bunit;

namespace BlazorScheduler.Tests;

/// <summary>
/// Tests the .NET side of the drag protocol that the pointer module in
/// scheduler.js drives. They lock in the contract that a press without
/// movement is a click, never a create/reschedule, and that only a real
/// drag across days performs an action.
/// </summary>
public sealed class DataSchedulerDragTests : IDisposable
{
    private readonly BunitContext _context = new();

    public DataSchedulerDragTests()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _context.Dispose();

    private static IRenderedComponent<DataScheduler<TestItem>> Render(
        BunitContext context,
        IReadOnlyList<TestItem> items,
        Action<ComponentParameterCollectionBuilder<DataScheduler<TestItem>>>? configure = null)
    {
        return context.Render<DataScheduler<TestItem>>(parameters =>
        {
            parameters
                .Add(component => component.Items, items)
                .Add(component => component.ItemKey, item => item.Id)
                .Add(component => component.ItemStart, item => item.Start)
                .Add(component => component.ItemEnd, item => item.End)
                .Add(component => component.ItemColor, item => item.Color);
            configure?.Invoke(parameters);
        });
    }

    [Fact]
    public async Task DayPress_WithoutMovement_DoesNotCreate()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginDayDrag("20260801"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.Null(created);

        // Drag state was cleared: a later drag must not be affected.
        await cut.InvokeAsync(() => cut.Instance.DragTo("20260802"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());
        Assert.Null(created);
    }

    [Fact]
    public async Task DayDrag_AcrossDays_CreatesRange()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginDayDrag("20260801"));
        await cut.InvokeAsync(() => cut.Instance.DragTo("20260803"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.NotNull(created);
        Assert.Equal(new DateTime(2026, 8, 1), created.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 3), created.Value.End);
    }

    [Fact]
    public async Task ItemPress_WithoutMovement_DoesNotReschedule()
    {
        var day = new DateTime(2026, 8, 1);
        var item = new TestItem(1, "Planning", day, day.AddDays(1), "red");
        SchedulerItemRescheduleEventArgs<TestItem>? rescheduled = null;
        var cut = Render(_context, new[] { item }, parameters => parameters
            .Add(component => component.EnableRescheduling, true)
            .Add(component => component.OnItemReschedule, (SchedulerItemRescheduleEventArgs<TestItem> args) => rescheduled = args));

        await cut.InvokeAsync(() => cut.Instance.BeginItemDrag(0, "20260801"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.Null(rescheduled);
    }

    [Fact]
    public async Task ItemDrag_AcrossDays_Reschedules()
    {
        var day = new DateTime(2026, 8, 1);
        var item = new TestItem(1, "Planning", day, day.AddDays(1), "red");
        SchedulerItemRescheduleEventArgs<TestItem>? rescheduled = null;
        var cut = Render(_context, new[] { item }, parameters => parameters
            .Add(component => component.EnableRescheduling, true)
            .Add(component => component.OnItemReschedule, (SchedulerItemRescheduleEventArgs<TestItem> args) => rescheduled = args));

        await cut.InvokeAsync(() => cut.Instance.BeginItemDrag(0, "20260801"));
        await cut.InvokeAsync(() => cut.Instance.DragTo("20260803"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.NotNull(rescheduled);
        Assert.Same(item, rescheduled.Value.Item);
        Assert.Equal(new DateTime(2026, 8, 3), rescheduled.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 4), rescheduled.Value.End);
    }

    [Fact]
    public async Task Drag_Disabled_IgnoresPresses()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.EnableDragging, false)
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginDayDrag("20260801"));
        await cut.InvokeAsync(() => cut.Instance.DragTo("20260803"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.Null(created);
    }

    [Fact]
    public async Task WeekDayDrag_AcrossMinutes_CreatesTimedRange()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|540"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("600"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.NotNull(created);
        Assert.Equal(new DateTime(2026, 8, 10, 9, 0, 0), created.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 10, 10, 0, 0), created.Value.End);
    }

    [Fact]
    public async Task WeekDayDrag_BackwardAcrossMinutes_NormalizesRange()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|600"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("540"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.NotNull(created);
        Assert.Equal(new DateTime(2026, 8, 10, 9, 0, 0), created.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 10, 10, 0, 0), created.Value.End);
    }

    [Fact]
    public async Task WeekDayPress_WithoutMovement_DoesNotCreate()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|540"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.Null(created);
    }

    [Fact]
    public async Task WeekDrag_BackToAnchor_KeepsLastSpan()
    {
        // Dragging back to the anchor minute must not collapse the selection to
        // zero length (the planners classify zero-duration items as all-day).
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|540"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("600"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("540"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.NotNull(created);
        Assert.Equal(new DateTime(2026, 8, 10, 9, 0, 0), created.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 10, 10, 0, 0), created.Value.End);
    }

    [Fact]
    public async Task WeekDrag_Disabled_IgnoresPresses()
    {
        SchedulerRange? created = null;
        var cut = Render(_context, Array.Empty<TestItem>(), parameters => parameters
            .Add(component => component.EnableDragging, false)
            .Add(component => component.OnCreate, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|540"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("600"));
        await cut.InvokeAsync(() => cut.Instance.CompleteDrag());

        Assert.Null(created);
    }
}
