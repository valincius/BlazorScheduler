using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerWeek
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }
        
        [Parameter] public DateTime Start { get; set; }
        [Parameter] public DateTime End { get; set; }
        [Parameter] public IEnumerable<Appointment> Appointments { get; set; }

		private readonly Dictionary<Appointment, int> _orderings = new();
        private readonly Dictionary<Appointment, (int, int)> _startsAndEnds = new();
		private int _maxNumOfAppointmentsPerDay => Scheduler.Config.MaxVisibleAppointmentsPerDay;

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
            DayOfWeek schedStart = Scheduler.Config.StartDayOfWeek;
            DayOfWeek start = schedStart, end = schedStart + 6;

            if (appointment.Start.Between(Start, End))
            {
                start = appointment.Start.DayOfWeek;
                end = appointment.End.Between(Start, End) ? appointment.End.DayOfWeek : schedStart - 1;
            }
            else if (appointment.End.Between(Start, End))
            {
                start = schedStart;
                end = appointment.End.DayOfWeek;
            }

            return ((start - schedStart + 7) % 7, (end - schedStart + 7) % 7);
        }

        private int GetBestOrderingForAppointment(Appointment appointment)
        {
            if (ReferenceEquals(appointment, Scheduler.NewAppointment))
            {
                return -1;
            }

            var (start, end) = _startsAndEnds[appointment];
            return _orderings
                .Where(x => {
                    return !ReferenceEquals(x.Key, Scheduler.NewAppointment)
                    && (start, end).Overlaps(_startsAndEnds[x.Key]);
                })
                .OrderBy(x => x.Value)
                .TakeWhile((x, i) => x.Value == ++i)
                .LastOrDefault().Value + 1;
        }
    }
}
