using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointmentOverflow
	{
		[CascadingParameter] public Scheduler Scheduler { get; set; }

		[Parameter] public IReadOnlyCollection<Appointment> Appointments { get; set;}
		[Parameter] public int Start { get; set; }
		[Parameter] public int Order { get; set; }
	}
}
