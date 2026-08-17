using Bunit;

namespace BlazorScheduler.Tests;

public sealed class DataSchedulerTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void Render_ProducesCalendarAndItems()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = DateTime.Today;
        var items = new[] { new TestItem(1, "Review", today, today, "red") };

        var cut = _context.Render<DataScheduler<TestItem>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.ItemKey, item => item.Id)
            .Add(component => component.ItemStart, item => item.Start)
            .Add(component => component.ItemEnd, item => item.End)
            .Add(component => component.ItemColor, item => item.Color));

        Assert.Equal(42, cut.FindAll("[data-scheduler-day]").Count);
        Assert.Contains("Review", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_RejectsDuplicateKeys()
    {
        var items = new[]
        {
            new TestItem(1, "One", DateTime.Today, DateTime.Today, "red"),
            new TestItem(1, "Two", DateTime.Today, DateTime.Today, "blue")
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _context.Render<DataScheduler<TestItem>>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.ItemKey, item => item.Id)
                .Add(component => component.ItemStart, item => item.Start)
                .Add(component => component.ItemEnd, item => item.End)));

        Assert.Contains("unique ItemKey", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose() => _context.Dispose();

    private sealed record TestItem(int Id, string Title, DateTime Start, DateTime End, string Color)
    {
        public override string ToString() => Title;
    }
}
