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
    - Optionally run `Install-Package BlazorScheduler.Core` in your shared project so you can have access to the interfaces there.
2. Add references to necessary js & css files in your `index.html`
    - Add `<link href="_content/BlazorScheduler/css/styles.css" rel="stylesheet" />` to the head
    - Add `<script src="_content/BlazorScheduler/js/scripts.js"></script>` to the body
3. Add `@using BlazorScheduler` to your page
    - Add `using BlazorScheduler.Core` wherever you are creating an implementation of `BlazorScheduler.Core.IAppointment`
4. Create an implementation of `IAppointment`
    ```c#
    public class Appointment : IAppointment
    {
        public string Title { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public Color Color { get; set; }
    }
    ```
5. Create a `List` of your appointments
    ```c#
    List<Appointment> Appointments = new();
    ```
5. Add the component to your view
    ```html
    <Scheduler T="Appointment" Appointments="Appointments" />
    ```

## Interactions
There are 3 callbacks that the scheduler provides.
- `Task OnAddingNewAppointment(T appointment)` - invoked when the user is done dragging to create a new appointment
- `Task OnAppointmentClick(T appointment, MouseEventArgs mouse)` - invoked when the user clicks on an appointment
- `Task OnOverflowAppointmentClick(IEnumerable<T> appointments, MouseEventArgs mouse)` - invoked when the user clicks on an "overflowing" appointment

See the demo [here](https://valincius.dev/BlazorScheduler/) for more information on usage
