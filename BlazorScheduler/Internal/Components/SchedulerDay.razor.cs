﻿using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using BlazorScheduler.Internal.Extensions;
using Microsoft.AspNetCore.Components.Web;
using BlazorScheduler.Core;

namespace BlazorScheduler.Internal.Components
{
    public partial class SchedulerDay<T> where T : IAppointment, new()
    {
        [CascadingParameter] public Scheduler<T> Scheduler { get; set; }

        [Parameter] public DateTime Day { get; set; }

        private bool IsDiffMonth => Day.Month != Scheduler.CurrentDate.Month;
        private string DateText => (IsDiffMonth && Day.Day == 1) ? Day.ToString("MMM d") : Day.Day.ToString();
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

        private void OnDayClick()
        {
            Scheduler?.OnDayClick(Day);
        }
    }
}
