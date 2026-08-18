namespace BlazorScheduler.Tests;

internal sealed record TestItem(int Id, string Title, DateTime Start, DateTime End, string? Color = null)
{
    public override string ToString() => Title;
}
