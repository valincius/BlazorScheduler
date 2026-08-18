using Bunit;

namespace BlazorScheduler.Tests;

/// <summary>
/// Tests the .NET side of the double-click protocol: a double-click on an
/// empty day produces a full-day range (midnight to midnight of the next day)
/// through <c>OnDayDoubleClick</c>, and nothing when creation is disabled.
/// The browser side is covered by tests/js/scheduler.test.js.
/// </summary>
public sealed class DataSchedulerDoubleClickTests : IDisposable
{
    private readonly BunitContext _context = new();

    public DataSchedulerDoubleClickTests()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task DayDoubleClicked_RaisesFullDayRange()
    {
        SchedulerRange? created = null;
        var cut = _context.Render<DataScheduler<TestItem>>(parameters => parameters
            .Add(component => component.Items, Array.Empty<TestItem>())
            .Add(component => component.ItemKey, item => item.Id)
            .Add(component => component.ItemStart, item => item.Start)
            .Add(component => component.ItemEnd, item => item.End)
            .Add(component => component.OnDayDoubleClick, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.DayDoubleClicked("20260801"));

        Assert.NotNull(created);
        Assert.Equal(new DateTime(2026, 8, 1), created.Value.Start);
        Assert.Equal(new DateTime(2026, 8, 2), created.Value.End);
    }

    [Fact]
    public async Task DayDoubleClicked_CreationDisabled_DoesNotRaise()
    {
        SchedulerRange? created = null;
        var cut = _context.Render<DataScheduler<TestItem>>(parameters => parameters
            .Add(component => component.Items, Array.Empty<TestItem>())
            .Add(component => component.ItemKey, item => item.Id)
            .Add(component => component.ItemStart, item => item.Start)
            .Add(component => component.ItemEnd, item => item.End)
            .Add(component => component.EnableAppointmentsCreationFromScheduler, false)
            .Add(component => component.OnDayDoubleClick, (SchedulerRange range) => created = range));

        await cut.InvokeAsync(() => cut.Instance.DayDoubleClicked("20260801"));

        Assert.Null(created);
    }

    private sealed record TestItem(int Id, string Title, DateTime Start, DateTime End, string Color)
    {
        public override string ToString() => Title;
    }
}
