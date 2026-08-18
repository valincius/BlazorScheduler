# BlazorScheduler

A lightweight month and week scheduler for Blazor with all-day and timed items, overflow handling, templates, appointment creation, and drag-to-reschedule support.

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

- `OnRangeChanged` receives the displayed `SchedulerRange` after initial interactive rendering, navigation, and view changes.
- `OnCreate` receives the dragged date range (a day span in the month view, a timed span in the week view).
- `OnItemClick` receives the selected item.
- `OnItemReschedule` receives the item and proposed start/end values.
- `OnOverflowClick` receives the day and hidden items.

## Views

The scheduler ships with a month view and a week view. Switch with the `View` parameter (default `SchedulerView.Month`), bind it for two-way updates, or use the built-in switcher in the default header:

```razor
<DataScheduler TItem="CalendarItem"
               Items="_items"
               ItemKey="item => item.Id"
               ItemStart="item => item.Start"
               ItemEnd="item => item.End"
               @bind-View="_view"
               StartDayOfWeek="DayOfWeek.Monday">
</DataScheduler>

@code {
    private SchedulerView _view = SchedulerView.Month;
}
```

- `StartDayOfWeek` (default `DayOfWeek.Sunday`) controls the first day of the week in both views. The displayed range always starts on it.
- The month view shows the full month with the day-number grid, timed items as dots, and multi-day items as bars. Drag across empty days to create an appointment, or drag an appointment to reschedule it. `MonthViewWeeks` (default `null`) optionally pins the grid to a fixed number of weeks (for example `6` for a classic six-week grid); when unset the grid auto-fits the month.
- The week view shows one week with an all-day strip on top (multi-day and zero-duration items) and a time grid below, where timed items are positioned by their start and end times. Drag vertically across an empty day column to create a timed appointment (positions snap to 15 minutes); item clicks work in both views.
- `ShowViewSwitcher` (default `true`) shows a Month/Week dropdown on the right of the default header. Supplying a custom `HeaderTemplate` replaces the header entirely, including the switcher; drive view changes through the `View` parameter then.

Week-view configuration:

- `WeekViewStartHour` / `WeekViewEndHour` (defaults `0` and `24`) define the visible hour window. Items are clipped at the edges; items entirely outside the window are hidden.
- `WeekViewHourHeight` (default `60`) sets the pixel height of one hour row.
- `Use24HourClock` (default `true`) renders the hour labels in 24-hour format (`00:00`); set to `false` for 12-hour labels (`12 AM`, `1 PM`).
- `WeekDayHeaderTemplate` customizes each week day header cell (it receives the `DateTime`).
- `MaxVisibleAppointmentsPerDay` (default `5`) caps the all-day strip rows and the per-day timed overlap columns; the hidden items are reported through `OnOverflowClick` and shown as a "+ {n} others" chip.

`DayTemplate` customizes the month day cells and does not apply to the week view.

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
node --test tests/js/scheduler.test.js
dotnet run --project BlazorScheduler.Demo/BlazorScheduler.Demo.csproj
```

The demo is deployed to GitHub Pages at [https://valincius.github.io/BlazorScheduler/](https://valincius.github.io/BlazorScheduler/) by the `GitHub Pages` workflow on every push to `main`. The workflow publishes the WebAssembly app and uploads its `wwwroot` output; the Pages source must be set to **GitHub Actions** in the repository settings.

See [docs/performance.md](docs/performance.md) for the layout profiling method and results.

## License

MIT
