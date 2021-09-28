# BlazorScheduler
[![Nuget](https://img.shields.io/nuget/v/BlazorScheduler)](https://www.nuget.org/packages/BlazorScheduler/)

A scheduler/calendar built with Blazor with zero dependancies.
![preview](https://user-images.githubusercontent.com/15176357/125132100-b1693b00-e0b8-11eb-9873-88a18973626b.png)
[Demo here](https://valincius.dev/BlazorScheduler/)

## Overview
BlazorScheduler is a component library that provides a single component, the scheduler.
The scheduler supports "all-day" appointments, appointments spanning multiple days/weeks/months, and timed appointments.
Also has support for dragging to create appointments.

## Usage
1. Run `Install-Package BlazorScheduler` in the package manager console to install the latest package in your frontend project.
2. Add references to necessary js & css files in your `index.html`
    - Add `<link href="_content/BlazorScheduler/css/styles.css" rel="stylesheet" />` to the head
    - Add `<script src="_content/BlazorScheduler/js/scripts.js"></script>` to the body
3. Add `@using BlazorScheduler` to your page
4. Create a `List` of your appointments
    ```c#
    List<AppointmentDto> _appointments = new();
    ```
5. Add the component to your view and build the appointments like so:
    ```c#
    <Scheduler>
        <Appointments>
            @foreach (var app in _appointments)
            {
                <Appointment Start="@app.Start" End="@app.End" Color="@app.Color">
                    @app.Title
                </Appointment>
            }
        </Appointments>
    </Scheduler>
    ```

## Interactions
There are 3 callbacks that the scheduler provides.
- `Task OnAddingNewAppointment(DateTime start, DateTime end)` - invoked when the user is done dragging to create a new appointment, the range is returned in the parameters
- `Task OnOverflowAppointmentClick(DateTime day)` - invoked when the user clicks on an "overflowing" appointment, the date of the overflow is returned in the parameters
- `Task OnRequestNewData(DateTime start, DateTime end)` - invoked on first render and when the month is changed, the range is returned in the parameters

See the demo [here](https://valincius.dev/BlazorScheduler/) for more information on usage
