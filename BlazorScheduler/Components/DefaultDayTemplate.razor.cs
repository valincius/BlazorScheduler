using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;

namespace BlazorScheduler
{
    public partial class DefaultDayTemplate
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }
        [Parameter] public DateTime Day { get; set; }

        [Parameter] public Action OnClick { get; set; }

        private bool IsDiffMonth => Day.Month != Scheduler.CurrentDate.Month;
        private string DateText => (IsDiffMonth && Day.Day == 1) ? Day.ToString("MMM d") : Day.Day.ToString();

        private void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == 0)
            {
                OnClick?.Invoke();
            }
        }
    }
}
