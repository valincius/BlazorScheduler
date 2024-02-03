using Microsoft.AspNetCore.Components;

namespace BlazorScheduler;

public enum SchedulerView
{
    Day,
    Week,
    Month
}

public partial class Scheduler<T> where T : IAppointment
{
    [Parameter]
    public SchedulerView View { get; set; } = SchedulerView.Week;

    [Parameter]
    public required IQueryable<T> Appointments { get; set; }

    [Parameter]
    public RenderFragment<T>? AppointmentTemplate { get; set; }

    [Parameter]
    public DateTime DisplayStartDate { get; set; } = DateTime.Today;

    private IEnumerable<T> VisibleAppointments => Appointments.Where(ShouldRenderAppointment);

    private bool ShouldRenderAppointment(T appointment) => View switch
    {
        SchedulerView.Day => appointment.Start.Date == DisplayStartDate.Date || appointment.End.Date == DisplayStartDate.Date,
        SchedulerView.Week => appointment.Start.Date >= DisplayStartDate.Date && appointment.End.Date <= DisplayStartDate.AddDays(7).Date,
        SchedulerView.Month => appointment.Start.Month == DisplayStartDate.Month && appointment.Start.Year == DisplayStartDate.Year,
        _ => throw new NotImplementedException()
    };

    private RenderFragment RenderMonthView()
    {
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "month-view");
            builder.AddContent(2, $"{DisplayStartDate:MMMM yyyy}");

            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "class", "month-grid");

            foreach (var day in Enumerable.Range(1, DateTime.DaysInMonth(DisplayStartDate.Year, DisplayStartDate.Month)))
            {
                var date = new DateTime(DisplayStartDate.Year, DisplayStartDate.Month, day);
                builder.OpenElement(5, "div");
                builder.AddAttribute(6, "class", "day");
                builder.AddContent(7, date.Day);
                builder.CloseElement();
            }
            builder.CloseElement();

            builder.OpenElement(8, "div");
            builder.AddAttribute(9, "class", "month-grid");
            foreach (var appointment in VisibleAppointments)
            {
                var span = appointment.End - appointment.Start;

                builder.OpenElement(10, "div");
                builder.AddMultipleAttributes(11, new Dictionary<string, object>
                {
                    ["class"] = "appointment",
                    ["style"] = $"width: {100 * span.TotalDays}%"
                });
                builder.AddContent(12, RenderAppointment(appointment));
                builder.CloseElement();
            }

            builder.CloseElement();
            builder.CloseElement();
        };
    }

    private RenderFragment RenderAppointment(T appointment)
    {
        return AppointmentTemplate?.Invoke(appointment) ?? DefaultAppointmentTemplate(appointment);
    }

    private static RenderFragment DefaultAppointmentTemplate(T appointment)
    {
        return builder =>
        {
            builder.OpenElement(0, "div");

            builder.OpenElement(1, "span");
            builder.AddContent(2, appointment.Start.ToString("t"));
            builder.CloseElement();

            builder.AddContent(3, " - ");

            builder.OpenElement(4, "span");
            builder.AddContent(5, appointment.End.ToString("t"));
            builder.CloseElement();

            builder.CloseElement();
        };
    }
}