using BlazorScheduler.Internal.Layout;

namespace BlazorScheduler.Tests;

public sealed class SchedulerWeekViewPlannerTests
{
    private static readonly DateTime Start = new(2026, 8, 9); // Sunday

    [Fact]
    public void Build_ClassifiesTimedAndAllDay()
    {
        var inputs = new[]
        {
            Input(1, Start.AddHours(9), Start.AddHours(10)),   // timed (same-day, non-zero)
            Input(2, Start, Start.AddDays(2)),                 // all-day (multi-day)
            Input(3, Start.AddHours(9), Start.AddHours(9))     // all-day (zero-duration)
        };

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 5, 5, TimeSpan.Zero, TimeSpan.FromHours(24));

        Assert.Equal(2, layout.AllDayItems.Count);
        var timedItem = Assert.Single(layout.Days[0].Items);
        Assert.Equal(1, timedItem.Input.Item.Id);
    }

    [Fact]
    public void Build_ClassifiesOvernightTimedItemAsAllDay()
    {
        var inputs = new[] { Input(1, Start.AddHours(23), Start.AddDays(1).AddHours(1)) };

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 5, 5, TimeSpan.Zero, TimeSpan.FromHours(24));

        Assert.Single(layout.AllDayItems);
        Assert.Empty(layout.Days[0].Items);
        Assert.Empty(layout.Days[1].Items);
    }

    [Fact]
    public void Build_AssignsOverlappingTimedItemsToDistinctSlots()
    {
        var inputs = new[]
        {
            Input(1, Start.AddHours(9), Start.AddHours(11)),
            Input(2, Start.AddHours(10), Start.AddHours(12)),
            Input(3, Start.AddHours(13), Start.AddHours(14))
        };

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 5, 5, TimeSpan.Zero, TimeSpan.FromHours(24));

        var day = layout.Days[0];
        Assert.Equal(3, day.Items.Count);
        Assert.All(day.Items, item => Assert.Equal(2, item.MaxSlots));

        var slots = day.Items.ToDictionary(item => item.Input.Item.Id, item => item.Slot);
        Assert.Equal(0, slots[1]);
        Assert.Equal(1, slots[2]);
        Assert.Equal(0, slots[3]);
    }

    [Fact]
    public void Build_ClipsTimesToViewWindow()
    {
        var inputs = new[]
        {
            Input(1, Start.AddHours(6), Start.AddHours(10)),   // clipped at the start of the window
            Input(2, Start.AddHours(17), Start.AddHours(23)),  // clipped at the end of the window
            Input(3, Start.AddHours(6), Start.AddHours(7))     // entirely outside the window
        };

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 5, 5, TimeSpan.FromHours(8), TimeSpan.FromHours(18));

        var day = layout.Days[0];
        Assert.Equal(2, day.Items.Count);

        var item1 = day.Items.Single(item => item.Input.Item.Id == 1);
        Assert.Equal(TimeSpan.FromHours(8), item1.StartTime);
        Assert.True(item1.ClippedAtStart);
        Assert.Equal(TimeSpan.FromHours(10), item1.EndTime);
        Assert.False(item1.ClippedAtEnd);

        var item2 = day.Items.Single(item => item.Input.Item.Id == 2);
        Assert.Equal(TimeSpan.FromHours(17), item2.StartTime);
        Assert.False(item2.ClippedAtStart);
        Assert.Equal(TimeSpan.FromHours(18), item2.EndTime);
        Assert.True(item2.ClippedAtEnd);

        Assert.DoesNotContain(day.Items, item => item.Input.Item.Id == 3);
    }

    [Fact]
    public void Build_AllDayItemsUseRowOrderingWithOverflow()
    {
        var inputs = Enumerable.Range(0, 6)
            .Select(index => Input(index, Start.AddDays(1), Start.AddDays(2)))
            .ToArray();

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 3, 5, TimeSpan.Zero, TimeSpan.FromHours(24));

        Assert.Equal(3, layout.AllDayVisibleRows);
        // The layout retains every item; the view renders only orders up to the cap.
        Assert.Equal(6, layout.AllDayItems.Count);
        Assert.Equal(2, layout.AllDayOverflows.Count);
        var overflow = layout.AllDayOverflows[0];
        Assert.Equal(3, overflow.Items.Count);
        Assert.Equal(Start.AddDays(1), overflow.Day);
    }

    [Fact]
    public void Build_TimedOverflow_ReportsItemsBeyondCap()
    {
        var inputs = Enumerable.Range(0, 4)
            .Select(index => Input(index, Start.AddHours(9), Start.AddHours(10)))
            .ToArray();

        var layout = SchedulerWeekViewPlanner.Build(inputs, Start, Start.AddDays(6), 5, 2, TimeSpan.Zero, TimeSpan.FromHours(24));

        var day = layout.Days[0];
        Assert.Equal(2, day.Items.Count);
        var overflow = Assert.Single(day.Overflows);
        Assert.Equal(2, overflow.Items.Count);
    }

    [Fact]
    public void Build_EmptyWeek_ProducesEmptyLayout()
    {
        var layout = SchedulerWeekViewPlanner.Build<TestItem>([], Start, Start.AddDays(6), 5, 5, TimeSpan.Zero, TimeSpan.FromHours(24));

        Assert.Equal(7, layout.Days.Count);
        Assert.Empty(layout.AllDayItems);
        Assert.Empty(layout.AllDayOverflows);
        Assert.Equal(0, layout.AllDayVisibleRows);
        Assert.All(layout.Days, day =>
        {
            Assert.Empty(day.Items);
            Assert.Empty(day.Overflows);
        });
    }

    [Fact]
    public void Build_RejectsInvalidViewWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SchedulerWeekViewPlanner.Build<TestItem>([], Start, Start.AddDays(6), 5, 5, TimeSpan.FromHours(18), TimeSpan.FromHours(8)));
    }

    private static SchedulerLayoutInput<TestItem> Input(int id, DateTime start, DateTime end) =>
        new(new TestItem(id, $"Item {id}", start, end, null), id, start, end, null, null, null, id);
}
