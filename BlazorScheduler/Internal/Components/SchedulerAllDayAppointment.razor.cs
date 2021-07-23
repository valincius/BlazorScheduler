using BlazorScheduler.Core;
using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAllDayAppointment<T> where T : IAppointment, new()
    {
        [CascadingParameter] public Scheduler<T> Scheduler { get; set; }
        [Parameter] public T Appointment { get; set; }
        [Parameter] public int Start { get; set; }
        [Parameter] public int End { get; set; }
        [Parameter] public int Order { get; set; }

        private string BackgroundColor => Appointment.Color.ToRgbString();
        private string TextColor => Appointment.Color.GetAltColor().ToRgbString();

		private IEnumerable<string> Classes
		{
            get
            {
                if (ReferenceEquals(Appointment, Scheduler.NewAppointment))
                {
                    yield return "new-appointment";
                }
            }
        }
    }
}
