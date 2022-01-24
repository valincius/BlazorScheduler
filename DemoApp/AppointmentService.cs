using DemoApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DemoApp
{
    public class AppointmentService
    {
        public IEnumerable<AppointmentDto> GetAppointments(DateTime start, DateTime end)
        {
            return AllAppointments
                .Where(x => x.Start.Date <= end && start <= x.End.Date);
        }

        private readonly List<AppointmentDto> AllAppointments = new()
        {
            new AppointmentDto { Title = "Hello 1", Start = DateTime.Now, End = DateTime.Now.AddHours(1), Color = "yellow" },
            new AppointmentDto { Title = "Hello 2", Start = DateTime.Now.AddDays(3), End = DateTime.Now.AddDays(3).AddHours(1), Color = "red" },

            new AppointmentDto { Title = "Hello 3", Start = DateTime.Today, End = DateTime.Today.AddDays(1), Color = "yellow" },

            new AppointmentDto { Title = "Open Sourced Date", Start = new DateTime(2021, 7, 9), End = new DateTime(2021, 7, 9), Color = "green" },
            new AppointmentDto { Title = "Vacation", Start = DateTime.Today.AddDays(4), End = DateTime.Today.AddDays(14), Color = "pink" },
            new AppointmentDto { Title = "Really busy day 1", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 2", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 3", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 4", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 5", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 6", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },
            new AppointmentDto { Title = "Really busy day 7", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1), Color = "orange" },

            new AppointmentDto { Title = "New Year's Day", Start = new DateTime(DateTime.Today.Year, 1, 1), End = new DateTime(DateTime.Today.Year, 1, 1), Color = "blue" },
            new AppointmentDto { Title = "Groundhog Day", Start = new DateTime(DateTime.Today.Year, 2, 2), End = new DateTime(DateTime.Today.Year, 2, 2), Color = "blue" },
            new AppointmentDto { Title = "Valentine's Day", Start = new DateTime(DateTime.Today.Year, 2, 14), End = new DateTime(DateTime.Today.Year, 2, 14), Color = "blue" },
            new AppointmentDto { Title = "St. Patrick's Day", Start = new DateTime(DateTime.Today.Year, 3, 17), End = new DateTime(DateTime.Today.Year, 3, 17), Color = "blue" },
            new AppointmentDto { Title = "Earth Day", Start = new DateTime(DateTime.Today.Year, 4, 22), End = new DateTime(DateTime.Today.Year, 4, 22), Color = "blue" },
            new AppointmentDto { Title = "Independence Day", Start = new DateTime(DateTime.Today.Year, 7, 4), End = new DateTime(DateTime.Today.Year, 7, 4), Color = "blue" },
            new AppointmentDto { Title = "Patriot Day", Start = new DateTime(DateTime.Today.Year, 9, 11), End = new DateTime(DateTime.Today.Year, 9, 11), Color = "blue" },
            new AppointmentDto { Title = "Halloween", Start = new DateTime(DateTime.Today.Year, 10, 31), End = new DateTime(DateTime.Today.Year, 10, 31), Color = "blue" },
            new AppointmentDto { Title = "Veterans' Day", Start = new DateTime(DateTime.Today.Year, 11, 11), End = new DateTime(DateTime.Today.Year, 11, 11), Color = "blue" },
            new AppointmentDto { Title = "Pearl Harbor Day", Start = new DateTime(DateTime.Today.Year, 12, 7), End = new DateTime(DateTime.Today.Year, 12, 7), Color = "blue" },
            new AppointmentDto { Title = "Christmas Day", Start = new DateTime(DateTime.Today.Year, 12, 25), End = new DateTime(DateTime.Today.Year, 12, 25), Color = "blue" },
            new AppointmentDto { Title = "New Year's Eve", Start = new DateTime(DateTime.Today.Year, 12, 31), End = new DateTime(DateTime.Today.Year, 12, 31), Color = "blue" },
        };
    }
}
