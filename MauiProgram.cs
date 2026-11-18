
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using tutorial_demo_2.Pages;
using tutorial_demo_2.Services;
using tutorial_demo_2.ViewModels;
using System.Net.Http;
using System.Net.Http.Headers;

namespace tutorial_demo_2
{
    public static class MauiProgram
    {
        public static MauiApp App { get; private set; } = null!;

        public static IServiceProvider Services => App.Services;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // MAUI application configuration
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(
                    isAndroidForegroundServiceEnabled: false)
                .UseMauiCommunityToolkitCamera()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            // HTTP Client configuration
            builder.Services
                .AddHttpClient("MyClient", client =>
                {
                    client.BaseAddress = new Uri(ApiUrls.BaseUrl);

                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(
                            "application/json"));
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            (message, cert, chain, errors) => true
                    });

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddSingleton<AttendanceViewModel>();
            builder.Services.AddTransient<WorkerListViewModel>();
            builder.Services.AddTransient<AttendanceHistoryViewModel>();
            builder.Services.AddTransient<SiteListViewModel>();
            builder.Services.AddTransient<ChangePasswordViewModel>();

            // Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<AttendancePage>();
            builder.Services.AddTransient<WorkerListPage>();
            builder.Services.AddTransient<SelfiePage>();
            builder.Services.AddTransient<ChangePasswordPage>();
            builder.Services.AddTransient<AttendanceHistoryPage>();
            builder.Services.AddTransient<SiteListPage>();
            builder.Services.AddTransient<LeavePage>();
            builder.Services.AddTransient<SalaryPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ForgotPasswordViewModel>();
            builder.Services.AddTransient<ForgotPasswordPage>();

            // Singleton Pages and Services
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<IGpsService, GpsService>();

            // Secure Storage
            builder.Services.AddSingleton<ISecureStorage>(
                SecureStorage.Default);

            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<IAppSecureStorage, AppSecureStorage>();
            builder.Services.AddSingleton<IAppStorageService, AppStorageService>();
            builder.Services.AddSingleton<ISiteService, SiteService>();

            // API Services
            builder.Services.AddScoped<ILoginService, LoginService>();
            builder.Services.AddTransient<IAttendanceService, AttendanceService>();
            builder.Services.AddTransient<IWorkerService, WorkerService>();
            builder.Services.AddTransient<ILeaveRequestService, LeaveRequestService>();
            builder.Services.AddTransient<IForgotPasswordService, ForgotPasswordService>();

            // Default HttpClient
            builder.Services.AddSingleton<HttpClient>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            App = builder.Build();

            return App;
        }
    }
}
