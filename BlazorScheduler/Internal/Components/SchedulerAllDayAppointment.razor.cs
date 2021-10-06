using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace BlazorScheduler.Internal.Components
{
	public partial class SchedulerAllDayAppointment
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }

        [Parameter] public Appointment Appointment { get; set; }
        [Parameter] public int Start { get; set; }
        [Parameter] public int End { get; set; }
        [Parameter] public int Order { get; set; }

		private IEnumerable<string> Classes
		{
            get
            {
                if (ReferenceEquals(Appointment, Scheduler.NewAppointment))
                {
                    yield return "new-appointment";
                }
            }
        }
        
        private Appointment _previousAppointment;
        private int _previousStart, _previousEnd;
        private bool _shouldRender;

        protected override void OnParametersSet()
        {
            _shouldRender = true;
            _shouldRender &= _previousAppointment == Appointment;
            _shouldRender &= _previousStart == Start;
            _shouldRender &= _previousEnd == End;

            _previousAppointment = Appointment;
            _previousStart = Start;
            _previousEnd = End;
        }

        protected override bool ShouldRender() => _shouldRender;
    }
}
