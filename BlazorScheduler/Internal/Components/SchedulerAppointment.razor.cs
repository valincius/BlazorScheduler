using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointment
	{
		[CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

		[Parameter] public Appointment Appointment { get; set; } = null!;
		[Parameter] public int Order { get; set; }

		private int Start => (int)Appointment.Start.DayOfWeek;

		private IEnumerable<string> Classes
		{
			get
			{
				if (ReferenceEquals(Appointment, Scheduler.DraggingAppointment))
				{
					yield return "new-appointment";
				}
			}
		}

		private void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == 0)
			{
				Scheduler.BeginDrag(Appointment);
			}
		}
	}
}
