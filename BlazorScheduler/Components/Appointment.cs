using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace BlazorScheduler
{
    public partial class Appointment : ComponentBase, IDisposable
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public Func<Appointment, MouseEventArgs, Task> OnClick { get; set; }

        [Parameter] public DateTime Start { get; set; }
        [Parameter] public DateTime End { get; set; }
        [Parameter] public string Color { get; set; }

        protected override void OnInitialized()
        {
            Scheduler.AddAppointment(this);
            base.OnInitialized();
        }

        public void Click(MouseEventArgs e)
        {
            OnClick?.Invoke(this, e);
        }

        public void Dispose()
        {
            Scheduler.RemoveAppointment(this);
            GC.SuppressFinalize(this);
        }
    }
}
