using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace BlazorScheduler.Internal.Components
{
    public partial class SchedulerDay
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }

        [Parameter] public DateTime Day { get; set; }
        [Parameter] public Func<DateTime, Task> OnClick { get; set; }

        private bool IsDiffMonth => Day.Month != Scheduler.CurrentDate.Month;
        private string DateText => (IsDiffMonth && Day.Day == 1) ? Day.ToString("MMM d") : Day.Day.ToString();
        private IEnumerable<string> Classes
        {
            get
            {
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
