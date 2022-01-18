using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace BlazorScheduler
{
    public partial class Appointment : ComponentBase, IDisposable
    {
        [CascadingParameter] public Scheduler Scheduler { get; set; } = null!;

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter] public Func<Task>? OnClick { get; set; }
        [Parameter] public Func<DateTime, DateTime, Task>? OnReschedule { get; set; }

        [Parameter] public DateTime Start { get; set; }
        [Parameter] public DateTime End { get; set; }
        [Parameter] public string? Color { get; set; }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    StateHasChanged();
                }
            }
        }

        protected override void OnInitialized()
        {
            Scheduler.AddAppointment(this);
            Color ??= Scheduler.Config.ThemeColor;

            base.OnInitialized();
        }

        public void Click(MouseEventArgs _)
        {
            OnClick?.Invoke();
        }

        public void Dispose()
        {
            Scheduler.RemoveAppointment(this);
            GC.SuppressFinalize(this);
        }
    }
}
