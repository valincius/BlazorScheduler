﻿using Microsoft.AspNetCore.Components;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointment
	{
		[CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

		[Parameter] public Appointment Appointment { get; set; } = null!;
		[Parameter] public int Order { get; set; }

		private int Start => (int)Appointment.Start.DayOfWeek;
		private bool IsTimedAppointment => Appointment.Start.Date == Appointment.End.Date && Appointment.Start != Appointment.End;
	}
}
