using System;

namespace BlazorScheduler.Core
{
	public interface IAppointment
	{
		string Title { get; set; }
		DateTime Start { get; set; }
		DateTime End { get; set; }
		string Color { get; set; }
	}
}
