using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointment<T> where T : IAppointment, new()
	{
		[CascadingParameter] public Scheduler<T> Scheduler { get; set; }

		[Parameter] public T Appointment { get; set; }
		[Parameter] public int Order { get; set; }

		private int Start => (int)Appointment.Start.DayOfWeek;
		private bool IsTimedAppointment => Appointment.Start.Date == Appointment.End.Date && Appointment.Start != Appointment.End;
		private string BackgroundColor => Appointment.Color.ToRgbString();
	}
}
