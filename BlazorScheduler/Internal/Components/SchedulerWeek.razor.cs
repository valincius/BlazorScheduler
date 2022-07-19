using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerWeek
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

        [Parameter] public DateTime Start { get; set; }
        [Parameter] public DateTime End { get; set; }
        [Parameter] public IEnumerable<Appointment> Appointments { get; set; } = null!;

		private int MaxNumOfAppointmentsPerDay => Scheduler.MaxVisibleAppointmentsPerDay;
        private int MaxVisibleAppointmentsThisWeek
        {
            get
            {
                int max = 0;
                for(var dt = Start; dt <= End; dt = dt.AddDays(1))
                {
                    var appCount = Appointments.Where(x => dt.Between(x.Start.Date, x.End.Date)).Count();
                    max = Math.Max(max, appCount);
                }
                return Math.Min(max, MaxNumOfAppointmentsPerDay);
            }
        }

        private readonly Dictionary<Appointment, int> _orderings = new();
        private readonly Dictionary<Appointment, (int, int)> _startsAndEnds = new();

        protected override void OnInitialized()
        {
            foreach (var app in Appointments)
            {
                _startsAndEnds[app] = GetStartAndEndDayForAppointment(app);
            }
            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            _orderings.Clear();
            foreach (var app in Appointments)
            {
                _orderings[app] = GetBestOrderingForAppointment(app);
            }

            base.OnParametersSet();
        }

        private (int, int) GetStartAndEndDayForAppointment(Appointment appointment)
        {
            DayOfWeek schedStart = Scheduler.StartDayOfWeek;
            DayOfWeek start = schedStart, end = schedStart + 6;

            if (!(appointment.Start.Date, appointment.End.Date).Overlaps((Start, End)))
                return ((int)appointment.Start.DayOfWeek, (int)appointment.End.DayOfWeek);

            if (appointment.Start.Date.Between(Start, End))
            {
                start = appointment.Start.DayOfWeek;
                end = appointment.End.Date.Between(Start, End) ? appointment.End.DayOfWeek : schedStart - 1;
            }
            else if (appointment.End.Date.Between(Start, End))
            {
                start = schedStart;
                end = appointment.End.DayOfWeek;
            }

            return ((start - schedStart + 7) % 7, (end - schedStart + 7) % 7);
        }

        private int GetBestOrderingForAppointment(Appointment appointment)
        {
            return _orderings
                .Where(x => x.Key != Scheduler.DraggingAppointment)
                .Where(x => _startsAndEnds[appointment].Overlaps(_startsAndEnds[x.Key]))
                .OrderBy(x => x.Value)
                .TakeWhile((x, i) => x.Value == ++i)
                .LastOrDefault().Value + 1;
        }
    }
}
