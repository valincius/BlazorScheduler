using System;
using System.Drawing;

namespace BlazorScheduler
{
	public interface IAppointment
	{
		string Title { get; set; }
		DateTime Start { get; set; }
		DateTime End { get; set; }
		Color Color { get; set; }
	}
}
