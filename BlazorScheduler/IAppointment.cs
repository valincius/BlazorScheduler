namespace BlazorScheduler;

public interface IAppointment
{
    DateTime Start { get; set; }
    DateTime End { get; set; }
}