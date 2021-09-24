using System;

namespace BlazorScheduler.Configuration
{
    public class Config
    {
        public bool AlwaysShowYear { get; set; } = true;
        public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
        public bool DisableDragging { get; set; } = false;
        public string ThemeColor { get; set; } = "aqua";
        public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;
        public string PlusOthersText { get; set; } = "+ {n} others";
    }
}
