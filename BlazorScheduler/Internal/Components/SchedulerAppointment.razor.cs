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

		private bool _drag;

		private void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == 0)
				_drag = false;
		}

		private void OnMouseUp(MouseEventArgs e)
		{
			if (_drag)
				return;

			Appointment.OnClick?.Invoke();
		}

		private void OnMouseMove(MouseEventArgs e)
		{
			_drag = true;

			if ((e.Buttons & 1) == 1)
				Scheduler.BeginDrag(Appointment);
		}
	}
}
