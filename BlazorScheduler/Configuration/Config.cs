using System;

namespace BlazorScheduler.Configuration
{
    public class Config
    {
        public bool AlwaysShowYear { get; set; } = true;
        public int MaxVisibleAppointmentsPerDay { get; set; } = 5;
        public bool EnableDragging { get; set; } = true;
        public string ThemeColor { get; set; } = "aqua";
        public DayOfWeek StartDayOfWeek { get; set; } = DayOfWeek.Sunday;
        public string TodayButtonText { get; set; } = "Today";
        public string PlusOthersText { get; set; } = "+ {n} others";
    }
}
