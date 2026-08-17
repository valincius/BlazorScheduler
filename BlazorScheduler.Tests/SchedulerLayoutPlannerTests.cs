using BlazorScheduler.Internal.Layout;

namespace BlazorScheduler.Tests;

public sealed class SchedulerLayoutPlannerTests
{
    private static readonly DateTime Start = new(2026, 8, 2);

    [Fact]
    public void Build_CreatesSixWeeksForTypicalMonthRange()
    {
        var result = SchedulerLayoutPlanner.Build<TestItem>([], Start, Start.AddDays(41), 5);
        Assert.Equal(6, result.Count);
        Assert.All(result, week => Assert.Equal(6, (week.End - week.Start).Days));
    }

    [Fact]
    public void Build_AssignsOverlappingItemsToDifferentRows()
    {
        var inputs = new[]
        {
            Input(1, Start.AddDays(1), Start.AddDays(3)),
            Input(2, Start.AddDays(2), Start.AddDays(4)),
            Input(3, Start.AddDays(4), Start.AddDays(5))
        };

        var week = Assert.Single(SchedulerLayoutPlanner.Build(inputs, Start, Start.AddDays(6), 5));
        Assert.Equal(1, week.Items.Single(item => item.Input.Item.Id == 1).Order);
        Assert.Equal(2, week.Items.Single(item => item.Input.Item.Id == 2).Order);
        Assert.Equal(1, week.Items.Single(item => item.Input.Item.Id == 3).Order);
    }

    [Fact]
    public void Build_ClipsSpanningItemsAtWeekEdges()
    {
        var input = Input(1, Start.AddDays(-4), Start.AddDays(10));
        var weeks = SchedulerLayoutPlanner.Build([input], Start, Start.AddDays(13), 5);

        Assert.Equal((0, 6), (weeks[0].Items[0].StartDay, weeks[0].Items[0].EndDay));
        Assert.Equal((0, 3), (weeks[1].Items[0].StartDay, weeks[1].Items[0].EndDay));
    }

    [Fact]
    public void Build_ReportsOnlyRowsBeyondVisibleLimitAsOverflow()
    {
        var inputs = Enumerable.Range(0, 5).Select(index => Input(index, Start, Start)).ToArray();
        var week = Assert.Single(SchedulerLayoutPlanner.Build(inputs, Start, Start.AddDays(6), 3));

        var overflow = Assert.Single(week.Overflows);
        Assert.Equal(2, overflow.Items.Count);
        Assert.Equal(3, week.VisibleRows);
    }

    [Fact]
    public void Build_OrdersLongerItemsFirstForStableLayout()
    {
        var inputs = new[] { Input(1, Start, Start), Input(2, Start, Start.AddDays(3)) };
        var week = Assert.Single(SchedulerLayoutPlanner.Build(inputs, Start, Start.AddDays(6), 5));
        Assert.Equal(2, week.Items[0].Input.Item.Id);
    }

    private static SchedulerLayoutInput<TestItem> Input(int id, DateTime start, DateTime end) =>
        new(new TestItem(id), id, start, end, null, null, null, id);

    private sealed record TestItem(int Id);
}

