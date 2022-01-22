using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAppointment
	{
		[CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

		[Parameter] public Appointment Appointment { get; set; } = null!;
		[Parameter] public int Order { get; set; }

		private int Start => (int)Appointment.Start.DayOfWeek;

		private void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == 0)
			{
				Scheduler.BeginDrag(Appointment);
			}
		}
	}
}
