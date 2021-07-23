using BlazorScheduler.Core;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointmentOverflow<T> where T : IAppointment, new()
	{
		[CascadingParameter] public Scheduler<T> Scheduler { get; set; }

		[Parameter] public IReadOnlyCollection<T> Appointments { get; set;}
		[Parameter] public int Start { get; set; }
		[Parameter] public int Order { get; set; }
	}
}
