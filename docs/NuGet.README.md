# BlazorScheduler

A lightweight, data-driven month and week scheduler component for Blazor. Add a calendar to your app in minutes with drag-to-create, double-click same-day creation, drag-to-reschedule, all-day and timed items, overflow handling, and full template support.

Supports .NET 8 and .NET 10. Works in Blazor Server, Blazor WebAssembly, and Blazor Web Apps (including static SSR with interactivity).

## Install

```powershell
dotnet add package BlazorScheduler
```

Reference the component stylesheet once, in the host page or `App.razor`:

```razor
<link rel="stylesheet" href="_content/BlazorScheduler/css/styles.css" />
```

The scheduler loads its isolated JavaScript module automatically — no script tag needed.

## Quick start

Define your own item type and pass it to the data-driven `DataScheduler<TItem>` component with selector parameters:

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

@code {
    private IReadOnlyList<CalendarItem> _items = new[]
    {
        new CalendarItem(1, "Planning", DateTime.Today.AddHours(9), DateTime.Today.AddHours(10), "#2563eb"),
    };

    private Task CreateAsync(SchedulerRange range) { /* add the appointment */ return Task.CompletedTask; }
    private Task RescheduleAsync(SchedulerItemRescheduleEventArgs<CalendarItem> args) { /* move it */ return Task.CompletedTask; }

    public sealed record CalendarItem(int Id, string Title, DateTime Start, DateTime End, string Color);
}
```

`ItemKey`, `ItemStart`, and `ItemEnd` are required. Keys must be non-null and unique, and an item's end must not precede its start.

## Features

- **Month view** — day-number grid with timed items as dots and multi-day items as bars; optional fixed week count (`MonthViewWeeks`).
- **Week view** — all-day strip plus a time grid with configurable hour window (`WeekViewStartHour` / `WeekViewEndHour`) and hour height; timed items positioned by start/end with 15-minute drag snapping.
- **Creation** — drag across empty days in the month view or drag vertically in a week day column to create an appointment; double-click an empty day in either view to create a same-day appointment (`OnDayDoubleClick`).
- **Rescheduling** — drag an appointment to move it (`EnableRescheduling` + `OnItemReschedule`).
- **Overflow handling** — `MaxVisibleAppointmentsPerDay` caps visible items; the rest collapse into a "+ {n} others" chip reported through `OnOverflowClick`.
- **Templates** — `ItemTemplate`, `DayTemplate`, `HeaderTemplate`, and `WeekDayHeaderTemplate` let you render your own markup.
- **Styling** — `ItemColor` / `ItemClass` / `ItemStyle` selectors, plus a `ThemeColor` accent used for the "today" marker and focus states.
- **Localization friendly** — month/day names and hour labels follow `CultureInfo.CurrentCulture`; `Use24HourClock` switches between 24-hour and 12-hour labels.

## Callbacks

| Callback | Receives |
| --- | --- |
| `OnRangeChanged` | The displayed `SchedulerRange` after navigation or view changes. |
| `OnCreate` | The dragged date range (day span in month view, timed span in week view). |
| `OnDayDoubleClick` | A full-day `SchedulerRange` (midnight to midnight) for the double-clicked day. |
| `OnItemClick` | The selected item. |
| `OnItemReschedule` | The item and its proposed start/end. |
| `OnOverflowClick` | The day and the hidden items. |

## Learn more

- Live demo: [https://valincius.github.io/BlazorScheduler/](https://valincius.github.io/BlazorScheduler/)
- Source and full documentation: [https://github.com/valincius/BlazorScheduler](https://github.com/valincius/BlazorScheduler)

## License

MIT
