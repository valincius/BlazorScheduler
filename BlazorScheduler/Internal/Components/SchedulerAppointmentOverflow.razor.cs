using Microsoft.AspNetCore.Components;
using System;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointmentOverflow
	{
		[CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

		[Parameter] public DateTime Day { get; set;}
		[Parameter] public int AppointmentCount { get; set;}
		[Parameter] public int Start { get; set; }
		[Parameter] public int Order { get; set; }
		[Parameter] public string? Style { get; set; }
	}
}
