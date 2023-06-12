using BlazorScheduler.Internal.Components;
using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorScheduler
{
    public partial class Scheduler : IAsyncDisposable
    {
        [Parameter] public RenderFragment Appointments { get; set; } = null!;
        [Parameter] public RenderFragment<Scheduler>? HeaderTemplate { get; set; }
        [Parameter] public RenderFragment<DateTime>? DayTemplate { get; set; }
        [Parameter] public string? RootDaysGroupInWeekStyle { get; set; }
        [Parameter] public string? RootAppointmentOverflowStyle { get; set; }
        [Parameter] public string? RootDayClass { get; set; }
        [Parameter] public string? RootDayStyle { get; set; }
        [Parameter] public Func<DateTime, DateTime, Task>? OnRequestNewData { get; set; }
        [Parameter] public Func<DateTime, DateTime, Task>? OnAddingNewAppointment { get; set; }
        [Parameter] public Func<DateTime, Task>? OnOverflowAppointmentClick { get; set; }

        #region Config
        [Parameter] public bool AlwaysShowYear { get; set; } = true;
        [Parameter] public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
        [Parameter] public bool EnableDragging { get; set; } = true;
        [Parameter] public bool EnableAppointmentsCreationFromScheduler { get; set; } = true;
        [Parameter] public bool EnableRescheduling { get; set; }
        [Parameter] public string ThemeColor { get; set; } = "aqua";
        [Parameter] public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;
        [Parameter] public string TodayButtonText { get; set; } = "Today";
        [Parameter] public string PlusOthersText { get; set; } = "+ {n} others";
        [Parameter] public string NewAppointmentText { get; set; } = "New Appointment";
        #endregion

        public DateTime CurrentDate { get; private set; }
        public (DateTime Start, DateTime End) CurrentRange
        {
            get
            {
                var startDate = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).GetPrevious(StartDayOfWeek);
                var endDate = new DateTime(CurrentDate.Year, CurrentDate.Month, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month))
                    .GetNext((DayOfWeek)((int)(StartDayOfWeek - 1 + 7) % 7));

                return (startDate, endDate);
            }
        }

        public Appointment? DraggingAppointment { get; private set; }

        private string MonthDisplay
        {
            get
            {
                var res = CurrentDate.ToString("MMMM");
                if (AlwaysShowYear || CurrentDate.Year != DateTime.Today.Year)
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
        private DateTime? _draggingAppointmentAnchor;
        private DateTime? _draggingStart, _draggingEnd;

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

            var (start, end) = CurrentRange;
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

        private IEnumerable<DateTime> GetDaysInRange()
        {
            var (start, end) = CurrentRange;
            return Enumerable
                .Range(0, 1 + end.Subtract(start).Days)
                .Select(offset => start.AddDays(offset));
        }

        private IEnumerable<Appointment> GetAppointmentsInRange(DateTime start, DateTime end)
        {
            var appointmentsInTimeframe = _appointments
                .Where(x => x.IsVisible)
                .Where(x => (start, end).Overlaps((x.Start.Date, x.End.Date)));

            return appointmentsInTimeframe
                .OrderBy(x => x.Start)
                .ThenByDescending(x => (x.End - x.Start).Days);
        }

        private Appointment? _reschedulingAppointment;
        public void BeginDrag(Appointment appointment)
        {
            if (!EnableRescheduling || _reschedulingAppointment is not null || _showNewAppointment)
                return;

            appointment.IsVisible = false;

            _reschedulingAppointment = appointment;
            _draggingStart = appointment.Start;
            _draggingEnd = appointment.End;
            _draggingAppointmentAnchor = null;

            StateHasChanged();
        }

        public void BeginDrag(SchedulerDay day)
        {
            if (!EnableAppointmentsCreationFromScheduler)
                return;

            _draggingStart = _draggingEnd = day.Day;
            _showNewAppointment = true;

            _draggingAppointmentAnchor = _draggingStart;
            StateHasChanged();
        }

        public bool IsDayBeingScheduled(Appointment appointment)
            => ReferenceEquals(appointment, DraggingAppointment) && _reschedulingAppointment is not null;

        [JSInvokable]
        public async Task OnMouseUp(int button)
        {
            if (button == 0 && _draggingStart is not null && _draggingEnd is not null)
            {
                if (_showNewAppointment)
                {
                    _showNewAppointment = false;
                    if (OnAddingNewAppointment is not null)
                        await OnAddingNewAppointment.Invoke(_draggingStart.Value, _draggingEnd.Value);

                    StateHasChanged();
                }

                if (_reschedulingAppointment is not null)
                {
                    var tempApp = _reschedulingAppointment;
                    _reschedulingAppointment = null;

                    if (tempApp.OnReschedule is not null)
                        await tempApp.OnReschedule.Invoke(_draggingStart.Value, _draggingEnd.Value);
                    else
                        throw new ArgumentNullException(nameof(Appointment.OnReschedule), $"{nameof(Appointment.OnReschedule)} must be defined on your Appointment component");

                    tempApp.IsVisible = true;

                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public void OnMouseMove(string date)
        {
            if (_showNewAppointment && EnableDragging)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                var anchor = _draggingAppointmentAnchor!.Value;
                (_draggingStart, _draggingEnd) = day < anchor ? (day, anchor) : (anchor, day);
                StateHasChanged();
            }

            if (_reschedulingAppointment is not null)
            {
                var day = DateTime.ParseExact(date, "yyyyMMdd", null);
                _draggingAppointmentAnchor ??= day;

                var diff = (day - _draggingAppointmentAnchor.Value).Days;

                _draggingStart = _reschedulingAppointment.Start.AddDays(diff);
                _draggingEnd = _reschedulingAppointment.End.AddDays(diff);

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
