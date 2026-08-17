using System.Diagnostics;
using BlazorScheduler.Internal.Layout;

var start = new DateTime(2026, 8, 2);
Console.WriteLine("| Items | Legacy median | v5 median | Speedup | v5 allocation |");
Console.WriteLine("| ---: | ---: | ---: | ---: | ---: |");

foreach (var count in new[] { 50, 500, 5_000 })
{
    var inputs = CreateInputs(count, start);
    LegacyLayout(inputs, start);
    SchedulerLayoutPlanner.Build(inputs, start, start.AddDays(41), 5);

    var legacy = Measure(() => LegacyLayout(inputs, start));
    var modern = Measure(() => SchedulerLayoutPlanner.Build(inputs, start, start.AddDays(41), 5));
    Console.WriteLine($"| {count:N0} | {legacy.Elapsed.TotalMilliseconds:N2} ms | {modern.Elapsed.TotalMilliseconds:N2} ms | {legacy.Elapsed.TotalMilliseconds / modern.Elapsed.TotalMilliseconds:N1}× | {modern.AllocatedBytes / 1024d:N1} KiB |");
}

static (TimeSpan Elapsed, long AllocatedBytes) Measure(Action action)
{
    var samples = new (TimeSpan Elapsed, long AllocatedBytes)[5];
    for (var index = 0; index < samples.Length; index++)
    {
        GC.Collect();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        samples[index] = (stopwatch.Elapsed, GC.GetAllocatedBytesForCurrentThread() - allocated);
    }
    return samples.OrderBy(sample => sample.Elapsed).ElementAt(samples.Length / 2);
}

static SchedulerLayoutInput<Item>[] CreateInputs(int count, DateTime rangeStart)
{
    var random = new Random(42);
    return Enumerable.Range(0, count)
        .Select(index =>
        {
            var itemStart = rangeStart.AddDays(random.Next(42)).AddHours(index % 3 == 0 ? 9 : 0);
            var itemEnd = itemStart.AddDays(random.Next(8)).AddHours(index % 3 == 0 ? 1 : 0);
            return new SchedulerLayoutInput<Item>(new Item(index), index, itemStart, itemEnd, null, null, null, index);
        })
        .ToArray();
}

// Mirrors the v4 layout shape: each week repeatedly scans prior placements and
// then repeats overlap scans for item visibility and daily overflow.
static void LegacyLayout(IReadOnlyList<SchedulerLayoutInput<Item>> source, DateTime rangeStart)
{
    for (var weekStart = rangeStart; weekStart <= rangeStart.AddDays(41); weekStart = weekStart.AddDays(7))
    {
        var weekEnd = weekStart.AddDays(6);
        var candidates = source
            .Where(item => item.Start.Date <= weekEnd && weekStart <= item.End.Date)
            .OrderBy(item => item.Start)
            .ThenByDescending(item => item.End - item.Start)
            .ToArray();
        var placed = new List<(SchedulerLayoutInput<Item> Item, int Start, int End, int Order)>();
        foreach (var item in candidates)
        {
            var first = Math.Clamp((item.Start.Date - weekStart).Days, 0, 6);
            var last = Math.Clamp((item.End.Date - weekStart).Days, 0, 6);
            var order = placed
                .Where(other => first <= other.End && other.Start <= last)
                .OrderBy(other => other.Order)
                .TakeWhile((other, index) => other.Order == index + 1)
                .Select(other => other.Order)
                .LastOrDefault() + 1;
            placed.Add((item, first, last, order));
        }

        foreach (var item in placed)
        {
            _ = placed.Where(other => item.Start <= other.End && other.Start <= item.End).Max(other => other.Order);
        }
        for (var day = 0; day < 7; day++)
        {
            _ = placed.Count(item => day >= item.Start && day <= item.End && item.Order > 5);
        }
    }
}

internal sealed record Item(int Id);

