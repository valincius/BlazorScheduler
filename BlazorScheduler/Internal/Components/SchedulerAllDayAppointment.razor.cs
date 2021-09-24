using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAllDayAppointment
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }

        [Parameter] public Appointment Appointment { get; set; }
        [Parameter] public int Start { get; set; }
        [Parameter] public int End { get; set; }
        [Parameter] public int Order { get; set; }

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
