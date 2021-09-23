using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorScheduler.Internal.Extensions;
using BlazorScheduler.Configuration;
using BlazorScheduler.Internal.Components;
namespace BlazorScheduler
{
    public partial class Scheduler
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment<DateTime> DayTemplate { get; set; }
        [Parameter] public Config Config { get; set; } = new();
        [Parameter] public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;

        [Parameter] public Func<DateTime, DateTime, Task> OnAddingNewAppointment { get; set; }
        [Parameter] public Func<DateTime, Task> OnOverflowAppointmentClick { get; set; }

        private readonly HashSet<Appointment> Appointments = new();
        private DotNetObjectReference<Scheduler> ObjectReference;
        private DateTime NewAppointmentAnchor;

        public DateTime CurrentDate { get; private set; }
        private string MonthDisplay
        {
            get
            {
                var res = CurrentDate.ToString("MMMM");
                if (Config.AlwaysShowYear || CurrentDate.Year != DateTime.Today.Year)
                {
                    return res += CurrentDate.ToString(" yyyy");
                }
                return res;
            }
        }

        public Appointment NewAppointment { get; private set; }
        private bool DoneDragging = false;

        protected override void OnInitialized()
        {
            ObjectReference = DotNetObjectReference.Create(this);
            CurrentDate = DateTime.Today;

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AttachMouseHandler();
            }
            base.OnAfterRender(firstRender);
        }

        public void AddAppointment(Appointment appointment)
        {
            Appointments.Add(appointment);
            StateHasChanged();
        }

        public void RemoveAppointment(Appointment appointment)
        {
            Appointments.Remove(appointment);
            StateHasChanged();
        }

        private async Task AttachMouseHandler()
        {
            await jsRuntime.InvokeVoidAsync("attachSchedulerMouseEventsHandler", ObjectReference);
        }

        private async Task ChangeMonth(int months = 0)
        {
            CurrentDate = months == 0 ? DateTime.Today : CurrentDate.AddMonths(months);
            await AttachMouseHandler();
        }

        private IEnumerable<DateTime> GetDateRange()
        {
            var startDate = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).GetPrevious(StartDayOfWeek);
            var endDate = new DateTime(CurrentDate.Year, CurrentDate.Month, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month)).GetNext((DayOfWeek)((int)(StartDayOfWeek - 1 + 7) % 7));

            return Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
              .Select(offset => startDate.AddDays(offset));
        }

        private IEnumerable<Appointment> GetAppointments(DateTime start, DateTime end)
        {
            var appointmentsInTimeframe = Appointments.Where(x => (start, end).Overlaps((x.Start, x.End))).ToList();
            if (NewAppointment is not null && (start, end).Overlaps((NewAppointment.Start, NewAppointment.End)))
            {
                appointmentsInTimeframe.Add(NewAppointment);
            }

            return appointmentsInTimeframe
                .OrderBy(x => x.Start)
                .ThenByDescending(x => (x.End - x.Start).Days);
        }

        public void BeginDrag(SchedulerDay day)
        {
            NewAppointment = new Appointment
            {
                ChildContent = new RenderFragment(builder => builder.AddContent(0, "New appointment")),
                Start = day.Day,
                End = day.Day,
                Color = Config.ThemeColor
            };
            DoneDragging = false;

            NewAppointmentAnchor = NewAppointment.Start;
            StateHasChanged();
        }

        [JSInvokable]
        public async Task OnMouseUp(int button)
        {
            if (button == 0)
            {
                if (NewAppointment is not null && !DoneDragging)
                {
                    DoneDragging = true;
                    await OnAddingNewAppointment?.Invoke(NewAppointment.Start, NewAppointment.End);
                    NewAppointment = default;
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public void OnMouseMove(string date)
        {
            if (NewAppointment is not null && !DoneDragging)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                (NewAppointment.Start, NewAppointment.End) = day < NewAppointmentAnchor ? (day, NewAppointmentAnchor) : (NewAppointmentAnchor, day);
                StateHasChanged();
            }
        }
    }
}
