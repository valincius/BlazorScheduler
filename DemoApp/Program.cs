using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoApp
{
    public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddSingleton<AppointmentService>();
			builder.Services.AddMudServices();

			await builder.Build().RunAsync();
		}
	}
}
