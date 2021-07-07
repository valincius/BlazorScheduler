using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorScheduler.Internal.Extensions;
using BlazorScheduler.Internal.Components;
using System.Drawing;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorScheduler
{
	public partial class Scheduler<T> where T : IAppointment, new()
    {
        [Parameter] public List<T> Appointments { get; set; }
        [Parameter] public Func<T, Task> OnAddingNewAppointment { get; set; }
        [Parameter] public Func<T, MouseEventArgs, Task> OnAppointmentClick { get; set; }
        [Parameter] public Func<IEnumerable<T>, MouseEventArgs, Task> OnOverflowAppointmentClick { get; set; }
        [Parameter] public Color ThemeColor { get; set; } = Color.Aqua;

        private DotNetObjectReference<Scheduler<T>> ObjectReference;
        private DateTime NewAppointmentAnchor;

        public DateTime CurrentDate { get; private set; }
        public T NewAppointment { get; private set; }
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
            var startDate = new DateTime(CurrentDate.Year, CurrentDate.Month, 1).GetPrevious(DayOfWeek.Sunday);
            var endDate = new DateTime(CurrentDate.Year, CurrentDate.Month, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month)).GetNext(DayOfWeek.Saturday);
            return Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
              .Select(offset => startDate.AddDays(offset));
        }

        private IEnumerable<T> GetAppointments(DateTime start, DateTime end)
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

        public void BeginDrag(SchedulerDay<T> day)
        {
            NewAppointment = new T {
                Start = day.Day,
                End = day.Day,
                Title = "New Appointment"
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
                if (NewAppointment is not null)
                {
                    DoneDragging = true;
                    await OnAddingNewAppointment?.Invoke(NewAppointment);
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
