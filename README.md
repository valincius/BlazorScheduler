# BlazorScheduler

A lightweight month scheduler for Blazor with all-day and timed items, overflow handling, templates, appointment creation, and drag-to-reschedule support.

Version 5 targets .NET 8 and .NET 10. The included demo is a standalone .NET 10 Blazor WebAssembly app, deployed as static files to GitHub Pages.

## Install

```powershell
dotnet add package BlazorScheduler
```

Reference the component stylesheet in the host page or `App.razor`:

```razor
<link rel="stylesheet" href="_content/BlazorScheduler/css/styles.css" />
```

The scheduler loads its isolated JavaScript module automatically. No script tag is required for the v5 data API.

## Data-driven API

```razor
<DataScheduler TItem="CalendarItem"
               Items="_items"
               ItemKey="item => item.Id"
               ItemStart="item => item.Start"
               ItemEnd="item => item.End"
               ItemColor="item => item.Color"
               EnableRescheduling="true"
               OnCreate="CreateAsync"
               OnItemReschedule="RescheduleAsync">
    <ItemTemplate Context="context">
        @context.Item.Title
    </ItemTemplate>
</DataScheduler>
```

`ItemKey`, `ItemStart`, and `ItemEnd` are required. Keys must be non-null and unique, and an item's end must not precede its start. Optional selectors provide color, CSS class, and inline style without requiring a scheduler-specific model type.

The primary callbacks use `EventCallback<T>`:

- `OnRangeChanged` receives the displayed `SchedulerRange` after initial interactive rendering and month navigation.
- `OnCreate` receives the dragged date range.
- `OnItemClick` receives the selected item.
- `OnItemReschedule` receives the item and proposed start/end values.
- `OnOverflowClick` receives the day and hidden items.

## v5 migration

The v4 child-component API remains functional throughout v5 but is marked obsolete with diagnostic `BZS001`:

```razor
<Scheduler>
    <Appointments>
        @foreach (var item in _items)
        {
            <Appointment Start="item.Start" End="item.End">@item.Title</Appointment>
        }
    </Appointments>
</Scheduler>
```

Migrate by replacing the child loop with `Items` and selector parameters, moving appointment markup into `ItemTemplate`, and changing `Func<Task>` handlers to the typed callbacks shown above. The legacy API may be removed in v6.

Legacy markup still requires `_content/BlazorScheduler/js/scripts.js`. New code should not reference that script.

## Develop and run

```powershell
dotnet build BlazorScheduler.sln -c Release
dotnet test BlazorScheduler.Tests/BlazorScheduler.Tests.csproj -c Release
dotnet run --project BlazorScheduler.Demo/BlazorScheduler.Demo.csproj
```

The demo is deployed to GitHub Pages at [https://valincius.github.io/BlazorScheduler/](https://valincius.github.io/BlazorScheduler/) by the `GitHub Pages` workflow on every push to `main`. The workflow publishes the WebAssembly app and uploads its `wwwroot` output; the Pages source must be set to **GitHub Actions** in the repository settings.

See [docs/performance.md](docs/performance.md) for the layout profiling method and results.

## License

MIT
