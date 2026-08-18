using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorScheduler.Tests;

public sealed class DataSchedulerViewTests : IDisposable
{
    private readonly BunitContext _context = new();

    public DataSchedulerViewTests()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _context.Dispose();

    private static IRenderedComponent<DataScheduler<TestItem>> Render(
        BunitContext context,
        IReadOnlyList<TestItem>? items = null,
        Action<ComponentParameterCollectionBuilder<DataScheduler<TestItem>>>? configure = null)
    {
        items ??= Array.Empty<TestItem>();
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

    private static async Task AnchorToAsync(IRenderedComponent<DataScheduler<TestItem>> cut, DateTime date)
        => await cut.InvokeAsync(() => cut.Instance.SetCurrentMonthAsync(date));

    private static string DayName(DayOfWeek day) => CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)day];

    [Fact]
    public void MonthView_Default_StartsWithSunday()
    {
        var cut = Render(_context);
        Assert.Equal(42, cut.FindAll("[data-scheduler-day]").Count);

        var range = cut.Instance.CurrentRange;
        Assert.Equal(DayOfWeek.Sunday, range.Start.DayOfWeek);
        Assert.Equal(
            range.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            cut.FindAll("[data-scheduler-day]")[0].GetAttribute("data-scheduler-day"));

        var headers = cut.FindAll(".week.header .full-dayname");
        Assert.Equal(7, headers.Count);
        Assert.Equal(DayName(DayOfWeek.Sunday), headers[0].TextContent);
        Assert.Equal(DayName(DayOfWeek.Saturday), headers[^1].TextContent);
    }

    [Fact]
    public async Task MonthView_MondayStart_StartsWithMonday()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.StartDayOfWeek, DayOfWeek.Monday));
        await AnchorToAsync(cut, new DateTime(2026, 6, 10));

        var range = cut.Instance.CurrentRange;
        Assert.Equal(DayOfWeek.Monday, range.Start.DayOfWeek);
        Assert.Equal(new DateTime(2026, 6, 1), range.Start);

        var cells = cut.FindAll("[data-scheduler-day]");
        Assert.Equal((range.End - range.Start).Days + 1, cells.Count);
        Assert.Equal(
            range.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            cells[0].GetAttribute("data-scheduler-day"));

        var headers = cut.FindAll(".week.header .full-dayname");
        Assert.Equal(DayName(DayOfWeek.Monday), headers[0].TextContent);
        Assert.Equal(DayName(DayOfWeek.Sunday), headers[^1].TextContent);
    }

    [Fact]
    public async Task MonthView_SaturdayStart_StartsWithSaturday()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.StartDayOfWeek, DayOfWeek.Saturday));
        await AnchorToAsync(cut, new DateTime(2026, 6, 10));

        var range = cut.Instance.CurrentRange;
        Assert.Equal(DayOfWeek.Saturday, range.Start.DayOfWeek);

        var cells = cut.FindAll("[data-scheduler-day]");
        Assert.Equal((range.End - range.Start).Days + 1, cells.Count);
        Assert.Equal(
            range.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            cells[0].GetAttribute("data-scheduler-day"));

        var headers = cut.FindAll(".week.header .full-dayname");
        Assert.Equal(DayName(DayOfWeek.Saturday), headers[0].TextContent);
        Assert.Equal(DayName(DayOfWeek.Friday), headers[^1].TextContent);
    }

    [Fact]
    public void WeekView_RendersSevenDayColumnsAndHourLabels()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));

        var columns = cut.FindAll("[data-scheduler-day]");
        Assert.Equal(7, columns.Count);

        var range = cut.Instance.CurrentRange;
        Assert.Equal(DayOfWeek.Sunday, range.Start.DayOfWeek);
        for (var index = 0; index < 7; index++)
        {
            Assert.Equal(
                range.Start.AddDays(index).ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                columns[index].GetAttribute("data-scheduler-day"));
        }

        var hours = cut.FindAll("[data-scheduler-hour]");
        Assert.Equal(24, hours.Count);
        Assert.Equal("0", hours[0].GetAttribute("data-scheduler-hour"));
        Assert.Equal("23", hours[^1].GetAttribute("data-scheduler-hour"));
    }

    [Fact]
    public async Task WeekView_TimedItem_PositionedByTime()
    {
        var day = new DateTime(2026, 8, 10);
        var cut = Render(_context, items: new[]
        {
            new TestItem(1, "Standup", day.AddHours(9), day.AddHours(10), "red")
        }, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));
        await AnchorToAsync(cut, day);

        var item = cut.Find("[data-scheduler-timed=\"true\"]");
        Assert.Equal("0", item.GetAttribute("data-scheduler-item"));

        var style = item.GetAttribute("style");
        Assert.Contains("--startMinutes:540;", style);
        Assert.Contains("--durationMinutes:60;", style);
        Assert.Contains("--top:", style);
        Assert.Contains("--height:", style);

        Assert.Equal(
            day.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            item.ParentElement!.GetAttribute("data-scheduler-day"));
    }

    [Fact]
    public async Task WeekView_HourLabels_DefaultTo24HourFormat()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));

        var labels = cut.FindAll(".hour-label");
        Assert.Equal(24, labels.Count);
        Assert.Equal("00:00", labels[0].TextContent);
        Assert.Equal("13:00", labels[13].TextContent);
        Assert.Equal("23:00", labels[^1].TextContent);
    }

    [Fact]
    public async Task WeekView_HourLabels_Use12HourFormat()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week)
            .Add(component => component.Use24HourClock, false));

        var labels = cut.FindAll(".hour-label");
        Assert.Equal(24, labels.Count);
        Assert.Equal("12 AM", labels[0].TextContent);
        Assert.Equal("9 AM", labels[9].TextContent);
        Assert.Equal("12 PM", labels[12].TextContent);
        Assert.Equal("1 PM", labels[13].TextContent);
        Assert.Equal("11 PM", labels[^1].TextContent);
    }

    [Fact]
    public async Task WeekView_CreateDrag_ShowsPreviewInAnchorColumn()
    {
        var day = new DateTime(2026, 8, 10);
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));
        await AnchorToAsync(cut, day);

        await cut.InvokeAsync(() => cut.Instance.BeginWeekDrag("20260810|540"));
        await cut.InvokeAsync(() => cut.Instance.DragWeekTo("600"));

        var preview = cut.Find(".time-grid .new-appointment");
        var style = preview.GetAttribute("style");
        Assert.Contains("--top:", style);
        Assert.Contains("--height:", style);
        Assert.Equal(
            day.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            preview.ParentElement!.GetAttribute("data-scheduler-day"));
    }

    [Fact]
    public async Task MonthView_FixedSixWeeks_RendersExactly42Cells()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.MonthViewWeeks, 6));
        await AnchorToAsync(cut, new DateTime(2026, 6, 10));

        var range = cut.Instance.CurrentRange;
        Assert.Equal(42, (range.End - range.Start).Days + 1);

        var cells = cut.FindAll("[data-scheduler-day]");
        Assert.Equal(42, cells.Count);
        Assert.Equal(
            range.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            cells[0].GetAttribute("data-scheduler-day"));
    }

    [Fact]
    public void MonthView_AutoWeeks_IsDefaultBehavior()
    {
        // The auto (null) setting keeps the existing range logic: the grid covers
        // the month plus the surrounding partial weeks. The length varies by month.
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.StartDayOfWeek, DayOfWeek.Monday));

        var range = cut.Instance.CurrentRange;
        var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var last = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));

        var expectedStart = first.AddDays(-((7 + (first.DayOfWeek - DayOfWeek.Monday)) % 7));
        var expectedEnd = last.AddDays((7 + (DayOfWeek.Sunday - last.DayOfWeek)) % 7);

        Assert.Equal(expectedStart, range.Start);
        Assert.Equal(expectedEnd, range.End);
        Assert.Equal(DayOfWeek.Monday, range.Start.DayOfWeek);
        Assert.Equal((range.End - range.Start).Days + 1, cut.FindAll("[data-scheduler-day]").Count);
    }

    [Fact]
    public async Task WeekView_AllDayItem_InAllDayStrip()
    {
        var start = new DateTime(2026, 8, 9);
        var cut = Render(_context, items: new[]
        {
            new TestItem(1, "Vacation", start, start.AddDays(3), "blue")
        }, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));
        await AnchorToAsync(cut, start.AddDays(1));

        var strip = cut.Find("[data-scheduler-all-day-strip]");
        Assert.Contains("data-scheduler-item=\"0\"", strip.InnerHtml);

        var bar = strip.QuerySelector(".appointment");
        Assert.NotNull(bar);
        var style = bar!.GetAttribute("style");
        Assert.Contains("--start:0;", style);
        Assert.Contains("--end:3;", style);
        Assert.Contains("--order:1;", style);

        Assert.Empty(cut.FindAll("[data-scheduler-timed=\"true\"]"));
    }

    [Fact]
    public async Task WeekView_ClipsItemsAtHourWindow()
    {
        var day = new DateTime(2026, 8, 10);
        var cut = Render(_context, items: new[]
        {
            new TestItem(1, "Early", day.AddHours(6), day.AddHours(10), "red"),
            new TestItem(2, "Out", day.AddHours(6), day.AddHours(7), "red")
        }, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week)
            .Add(component => component.WeekViewStartHour, 8)
            .Add(component => component.WeekViewEndHour, 18));
        await AnchorToAsync(cut, day);

        var timed = cut.FindAll("[data-scheduler-timed=\"true\"]");
        Assert.Single(timed);

        var style = timed[0].GetAttribute("style");
        Assert.Contains("--startMinutes:480;", style);
        Assert.Contains("--durationMinutes:120;", style);

        Assert.DoesNotContain("data-scheduler-item=\"1\"", cut.Markup);
    }

    [Fact]
    public async Task WeekView_OverlappingTimedItems_GetDistinctSlots()
    {
        var day = new DateTime(2026, 8, 10);
        var cut = Render(_context, items: new[]
        {
            new TestItem(1, "A", day.AddHours(9), day.AddHours(11), "red"),
            new TestItem(2, "B", day.AddHours(10), day.AddHours(12), "blue"),
            new TestItem(3, "C", day.AddHours(13), day.AddHours(14), "green")
        }, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));
        await AnchorToAsync(cut, day);

        var timed = cut.FindAll("[data-scheduler-timed=\"true\"]");
        Assert.Equal(3, timed.Count);

        var styles = timed.ToDictionary(
            element => element.GetAttribute("data-scheduler-item")!,
            element => element.GetAttribute("style") ?? "");

        // Keys are source indexes: A=0, B=1, C=2.
        Assert.Contains("--slot:0;", styles["0"]);
        Assert.Contains("--slot:1;", styles["1"]);
        Assert.Contains("--slot:0;", styles["2"]);
        Assert.Contains("--maxSlots:2", styles["0"]);
        Assert.Contains("--maxSlots:2", styles["1"]);
        Assert.Contains("--maxSlots:2", styles["2"]);
    }

    [Fact]
    public async Task WeekView_AllDayStripOverflow_ShowsChip()
    {
        var start = new DateTime(2026, 8, 10);
        var items = Enumerable.Range(0, 6)
            .Select(index => new TestItem(index, $"V{index}", start, start.AddDays(1), "red"))
            .ToArray();
        var cut = Render(_context, items: items, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week)
            .Add(component => component.MaxVisibleAppointmentsPerDay, 3));
        await AnchorToAsync(cut, start);

        var chips = cut.FindAll(".all-day-strip .scheduler-overflow");
        Assert.Equal(2, chips.Count);
        Assert.Equal("+ 3 others", chips[0].TextContent);
    }

    [Fact]
    public async Task WeekView_Navigation_MovesByWeek()
    {
        var anchor = new DateTime(2026, 8, 10);
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));
        await AnchorToAsync(cut, anchor);

        var initial = cut.Instance.CurrentRange.Start;
        Assert.Equal(DayOfWeek.Sunday, initial.DayOfWeek);

        await cut.Find("button[aria-label=\"Next week\"]").ClickAsync(new MouseEventArgs());
        Assert.Equal(initial.AddDays(7), cut.Instance.CurrentRange.Start);

        await cut.Find("button[aria-label=\"Previous week\"]").ClickAsync(new MouseEventArgs());
        Assert.Equal(initial, cut.Instance.CurrentRange.Start);
    }

    [Fact]
    public async Task MonthView_Navigation_MovesByMonthAndTodayResets()
    {
        var cut = Render(_context);
        await AnchorToAsync(cut, new DateTime(2026, 8, 10));
        Assert.Equal(8, cut.Instance.CurrentDate.Month);

        await cut.Find("button[aria-label=\"Next month\"]").ClickAsync(new MouseEventArgs());
        Assert.Equal(9, cut.Instance.CurrentDate.Month);

        await cut.Find("button[aria-label=\"Previous month\"]").ClickAsync(new MouseEventArgs());
        Assert.Equal(8, cut.Instance.CurrentDate.Month);

        await cut.Find("button.today").ClickAsync(new MouseEventArgs());
        Assert.Equal(DateTime.Today, cut.Instance.CurrentDate);
    }

    [Fact]
    public async Task ViewSwitcher_TogglesView_AndRaisesViewChanged()
    {
        SchedulerView? changed = null;
        var cut = _context.Render<DataScheduler<TestItem>>(parameters => parameters
            .Add(component => component.Items, Array.Empty<TestItem>())
            .Add(component => component.ItemKey, item => item.Id)
            .Add(component => component.ItemStart, item => item.Start)
            .Add(component => component.ItemEnd, item => item.End)
            .Add(component => component.ViewChanged, (SchedulerView view) => changed = view));

        // Default: month view with the built-in dropdown switcher.
        Assert.Equal(SchedulerView.Month, cut.Instance.View);
        Assert.Equal(42, cut.FindAll("[data-scheduler-day]").Count);

        var select = cut.Find("select.view-select");
        Assert.Equal(SchedulerView.Month.ToString(), select.GetAttribute("value"));

        await select.TriggerEventAsync("onchange", new ChangeEventArgs { Value = SchedulerView.Week.ToString() });

        Assert.Equal(SchedulerView.Week, changed);
        Assert.Equal(SchedulerView.Week, cut.Instance.View);
        Assert.Equal(7, cut.FindAll("[data-scheduler-day]").Count);

        await cut.Find("select.view-select").TriggerEventAsync("onchange", new ChangeEventArgs { Value = SchedulerView.Month.ToString() });

        Assert.Equal(SchedulerView.Month, changed);
        Assert.Equal(SchedulerView.Month, cut.Instance.View);
        Assert.Equal(42, cut.FindAll("[data-scheduler-day]").Count);
    }

    [Fact]
    public void ViewSwitcher_Select_ReflectsCurrentView()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week));

        var select = cut.Find("select.view-select");
        Assert.Equal(SchedulerView.Week.ToString(), select.GetAttribute("value"));
    }

    [Fact]
    public void Header_Controls_LiveOutsideTheMonthGrid()
    {
        // The pointer drag handler only engages inside the day grid, so the
        // header controls must live outside `.month`. This is the structural
        // invariant the JS regression tests rely on.
        var cut = Render(_context);

        var header = cut.Find(".header");
        Assert.Empty(header.QuerySelectorAll(".month"));
        Assert.NotNull(header.QuerySelector("button.today"));
        Assert.NotNull(header.QuerySelector("select.view-select"));

        var month = cut.Find(".month");
        Assert.Empty(month.QuerySelectorAll("button"));
        Assert.Equal(42, month.QuerySelectorAll("[data-scheduler-day]").Length);
    }

    [Fact]
    public void ShowViewSwitcherFalse_HidesSwitcher()
    {
        var cut = Render(_context, configure: parameters => parameters
            .Add(component => component.ShowViewSwitcher, false));
        Assert.Empty(cut.FindAll(".view-switcher"));
    }

    [Fact]
    public void InvalidWeekViewHours_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(_context, configure: parameters => parameters
            .Add(component => component.View, SchedulerView.Week)
            .Add(component => component.WeekViewStartHour, 18)
            .Add(component => component.WeekViewEndHour, 8)));
    }
}
