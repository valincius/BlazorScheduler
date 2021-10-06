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
        [Parameter] public RenderFragment Appointments { get; set; }
        [Parameter] public RenderFragment<Scheduler> HeaderTemplate { get; set; }
        [Parameter] public RenderFragment<DateTime> DayTemplate { get; set; }

        [Parameter] public Func<DateTime, DateTime, Task> OnRequestNewData { get; set; }
        [Parameter] public Func<DateTime, DateTime, Task> OnAddingNewAppointment { get; set; }
        [Parameter] public Func<DateTime, Task> OnOverflowAppointmentClick { get; set; }
        
        [Parameter] public Config Config { get; set; } = new();

        public DateTime CurrentDate { get; private set; }
        public Appointment NewAppointment { get; private set; }

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

        private readonly HashSet<Appointment> _appointments = new();
        private DotNetObjectReference<Scheduler> _objReference;
        private DateTime _draggingAppointmentAnchor;
        private bool _doneDragging = false;
        private bool _loading = false;

        protected override async Task OnInitializedAsync()
        {
            _objReference = DotNetObjectReference.Create(this);
            await SetCurrentMonth(DateTime.Today);

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AttachMouseHandler();
            }
            base.OnAfterRender(firstRender);
        }

        internal void AddAppointment(Appointment appointment)
        {
            _appointments.Add(appointment);
            StateHasChanged();
        }

        internal void RemoveAppointment(Appointment appointment)
        {
            _appointments.Remove(appointment);
            StateHasChanged();
        }

        public async Task SetCurrentMonth(DateTime date)
        {
            CurrentDate = date;
            await AttachMouseHandler();
            var (start, end) = GetDateRangeForCurrentMonth();
            if (OnRequestNewData != null)
            {
                _loading = true;
                StateHasChanged();
                await OnRequestNewData(start, end);
                _loading = false;
            }
            StateHasChanged();
        }

        private async Task AttachMouseHandler()
        {
            await jsRuntime.InvokeVoidAsync("attachSchedulerMouseEventsHandler", _objReference);
        }

        private async Task ChangeMonth(int months = 0)
        {
            await SetCurrentMonth(months == 0 ? DateTime.Today : CurrentDate.AddMonths(months));
        }

        private (DateTime, DateTime) GetDateRangeForCurrentMonth()
        {
            var startDate = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).GetPrevious(Config.StartDayOfWeek);
            var endDate = new DateTime(CurrentDate.Year, CurrentDate.Month, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month))
                .GetNext((DayOfWeek)((int)(Config.StartDayOfWeek - 1 + 7) % 7));

            return (startDate, endDate);
        }

        private IEnumerable<DateTime> GetDaysInRange()
        {
            var (start, end) = GetDateRangeForCurrentMonth();
            return Enumerable
                .Range(0, 1 + end.Subtract(start).Days)
                .Select(offset => start.AddDays(offset));
        }

        private IEnumerable<Appointment> GetAppointmentsInRange(DateTime start, DateTime end)
        {
            var appointmentsInTimeframe = _appointments.Where(x => (start, end).Overlaps((x.Start, x.End))).ToList();
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
            _doneDragging = false;

            _draggingAppointmentAnchor = NewAppointment.Start;
            StateHasChanged();
        }

        [JSInvokable]
        public async Task OnMouseUp(int button)
        {
            if (button == 0)
            {
                if (NewAppointment is not null && !_doneDragging)
                {
                    _doneDragging = true;
                    await OnAddingNewAppointment?.Invoke(NewAppointment.Start, NewAppointment.End);
                    NewAppointment = default;
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public void OnMouseMove(string date)
        {
            if (NewAppointment is not null && !_doneDragging)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                (NewAppointment.Start, NewAppointment.End) = day < _draggingAppointmentAnchor ? (day, _draggingAppointmentAnchor) : (_draggingAppointmentAnchor, day);
                StateHasChanged();
            }
        }
    }
}
