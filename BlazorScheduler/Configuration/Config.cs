namespace BlazorScheduler.Configuration
{
    public class Config
    {
        public bool AlwaysShowYear { get; set; } = true;
        public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
        public bool DisableDragging { get; set; } = false;
    }
}
