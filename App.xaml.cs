using Microsoft.Extensions.DependencyInjection;
using tutorial_demo_2.Services;
using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2
{
    public partial class App : Application
    {
		public static IServiceProvider Services { get; private set; } // Expose Services

		public App(IServiceProvider serviceProvider)
		{
			InitializeComponent();
			Services = serviceProvider; // Store the service provider

			// Resolve AppShell from DI
			MainPage = serviceProvider.GetRequiredService<AppShell>();
		}
		private void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<IAppStorageService, AppStorageService>();
			services.AddTransient<AttendanceViewModel>();
		}

	}


}
