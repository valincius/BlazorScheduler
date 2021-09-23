using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorScheduler.Internal.Components
{
    public partial class SchedulerDay
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }

        [Parameter] public DateTime Day { get; set; }

        private bool IsDiffMonth => Day.Month != Scheduler.CurrentDate.Month;
        private IEnumerable<string> Classes
        {
            get
            {
                if (Day == DateTime.Today)
                    yield return "today";

                if (IsDiffMonth)
                    yield return "diff-month";
            }
        }

        private void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == 0 && !Scheduler.Config.DisableDragging)
            {
                Scheduler.BeginDrag(this);
            }
        }
    }
}
