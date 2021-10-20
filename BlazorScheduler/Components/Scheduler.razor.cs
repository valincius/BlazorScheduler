using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorScheduler.Internal.Extensions;
using BlazorScheduler.Configuration;
using BlazorScheduler.Internal.Components;
using System.Collections.ObjectModel;

namespace BlazorScheduler
{
    public partial class Scheduler : IDisposable
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

        private readonly ObservableCollection<Appointment> _appointments = new();
        private DotNetObjectReference<Scheduler> _objReference;
        private bool _loading = false;

        public bool _showNewAppointment;
        private DateTime _draggingAppointmentAnchor;
        private DateTime _newAppointmentStart, _newAppointmentEnd;

        protected override async Task OnInitializedAsync()
        {
            _objReference = DotNetObjectReference.Create(this);
            await SetCurrentMonth(DateTime.Today, true);

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

        public async Task SetCurrentMonth(DateTime date, bool skipJsInvoke = false)
        {
            CurrentDate = date;
            if (!skipJsInvoke)
            {
                await AttachMouseHandler();
            }
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
            var appointmentsInTimeframe = _appointments.Where(x => (start, end).Overlaps((x.Start, x.End)));
            return appointmentsInTimeframe
                .OrderBy(x => x.Start)
                .ThenByDescending(x => (x.End - x.Start).Days);
        }

        public void BeginDrag(SchedulerDay day)
        {
            _newAppointmentStart = _newAppointmentEnd = day.Day;
            _showNewAppointment = true;

            _draggingAppointmentAnchor = _newAppointmentStart;
            StateHasChanged();
        }

        [JSInvokable]
        public async Task OnMouseUp(int button)
        {
            if (button == 0)
            {
                if (_showNewAppointment)
                {
                    _showNewAppointment = false;
                    await OnAddingNewAppointment?.Invoke(_newAppointmentStart, _newAppointmentEnd);
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public void OnMouseMove(string date)
        {
            if (_showNewAppointment)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                (_newAppointmentStart, _newAppointmentEnd) = day < _draggingAppointmentAnchor ? (day, _draggingAppointmentAnchor) : (_draggingAppointmentAnchor, day);
                StateHasChanged();
            }
        }

        public void Dispose()
        {
            _objReference.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
