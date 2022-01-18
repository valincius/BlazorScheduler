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
    public partial class Scheduler : IAsyncDisposable
    {
        [Parameter] public RenderFragment Appointments { get; set; } = null!;
        [Parameter] public RenderFragment<Scheduler>? HeaderTemplate { get; set; }
        [Parameter] public RenderFragment<DateTime>? DayTemplate { get; set; }

        [Parameter] public Func<DateTime, DateTime, Task>? OnRequestNewData { get; set; }
        [Parameter] public Func<DateTime, DateTime, Task>? OnAddingNewAppointment { get; set; }
        [Parameter] public Func<DateTime, Task>? OnOverflowAppointmentClick { get; set; }
        
        [Parameter] public Config Config { get; set; } = new();

        public DateTime CurrentDate { get; private set; }
        public Appointment? DraggingAppointment { get; private set; }

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
        private DotNetObjectReference<Scheduler> _objReference = null!;
        private bool _loading = false;

        public bool _showNewAppointment;
        private DateTime _draggingAppointmentAnchor;
        private DateTime _draggingStart, _draggingEnd;

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
            await jsRuntime.InvokeVoidAsync("BlazorScheduler.attachSchedulerMouseEventsHandler", _objReference);
        }
        private async Task DestroyMouseHandler()
        {
            await jsRuntime.InvokeVoidAsync("BlazorScheduler.destroySchedulerMouseEventsHandler");
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
            var appointmentsInTimeframe = _appointments
                .Where(x => x.IsVisible)
                .Where(x => (start, end).Overlaps((x.Start, x.End)));

            return appointmentsInTimeframe
                .OrderBy(x => x.Start)
                .ThenByDescending(x => (x.End - x.Start).Days);
        }

        SchedulerAllDayAppointment? _draggingAppointment;
        public void BeginDrag(SchedulerAllDayAppointment appointment)
        {
            appointment.Appointment.IsVisible = false;

            _draggingAppointment = appointment;
            _draggingStart = appointment.Appointment.Start;
            _draggingEnd = appointment.Appointment.End;

            StateHasChanged();
        }

        public void BeginDrag(SchedulerDay day)
        {
            _draggingStart = _draggingEnd = day.Day;
            _showNewAppointment = true;

            _draggingAppointmentAnchor = _draggingStart;
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
                    if (OnAddingNewAppointment is not null)
                        await OnAddingNewAppointment.Invoke(_draggingStart, _draggingEnd);

                    StateHasChanged();
                }

                if (_draggingAppointment is not null)
                {
                    if (_draggingAppointment.Appointment.OnReschedule is not null)
                        await _draggingAppointment.Appointment.OnReschedule.Invoke(_draggingStart, _draggingEnd);

                    _draggingAppointment.Appointment.IsVisible = true;
                    _draggingAppointment = null;

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
                (_draggingStart, _draggingEnd) = day < _draggingAppointmentAnchor ? (day, _draggingAppointmentAnchor) : (_draggingAppointmentAnchor, day);
                StateHasChanged();
            }

            if (_draggingAppointment is not null)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                _draggingStart = day;
                _draggingEnd = day.AddDays(_draggingAppointment.Appointment.End.DayOfYear - _draggingAppointment.Appointment.Start.DayOfYear);

                StateHasChanged();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DestroyMouseHandler();
            _objReference.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
