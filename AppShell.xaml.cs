
using tutorial_demo_2.Pages;
using tutorial_demo_2.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using tutorial_demo_2.Services;
using System.ComponentModel;

namespace tutorial_demo_2
{
    public partial class AppShell : Shell, INotifyPropertyChanged
    {
        public bool IsSiteListVisible { get; set; }
        public bool IsAttendanceVisible { get; set; }

        public AppShell()
        {
            InitializeComponent();

            RegisterRoutes();

            Navigated += OnNavigated;

            BindingContext = this;

            SetUserPermissions();
        }


        private async void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            var currentRoute = e.Current.Location.OriginalString;

            if (currentRoute.Contains(nameof(AttendancePage)) ||
                currentRoute.Contains("AttendanceEntry"))
            {
                try
                {
                    var attendanceViewModel =
                        App.Services.GetRequiredService<AttendanceViewModel>();

                    await attendanceViewModel.LoadDataAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Attendance Navigation Error: {ex}");
                }
            }
        }


        private void RegisterRoutes()
        {
            var routes = new (string, Type)[]
            {
        (nameof(LoginPage), typeof(LoginPage)),
        ("AttendanceEntry", typeof(AttendancePage)),
        (nameof(AttendanceHistoryPage), typeof(AttendanceHistoryPage)),
        (nameof(SelfiePage), typeof(SelfiePage)),
        (nameof(WorkerListPage), typeof(WorkerListPage)),
        (nameof(ForgotPasswordPage), typeof(ForgotPasswordPage)),
        (nameof(UserList), typeof(UserList))
            };

            foreach (var (route, page) in routes)
            {
                Routing.RegisterRoute(route, page);
            }
        }

        public async void SetUserPermissions()
        {
            try
            {
                var storageService =
                    App.Services.GetRequiredService<IAppStorageService>();

                var users = await storageService.GetUserAsync();

                var workmanId = users?.WorkmanId ?? string.Empty;

                IsSiteListVisible = workmanId.Contains(
                    "FO",
                    StringComparison.OrdinalIgnoreCase);

                IsAttendanceVisible = !IsSiteListVisible;

                OnPropertyChanged(nameof(IsSiteListVisible));
                OnPropertyChanged(nameof(IsAttendanceVisible));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"SetUserPermissions Error: {ex}");
            }
        }

        public void ResetUserPermissions()
        {
            IsSiteListVisible = false;
            IsAttendanceVisible = false;

            OnPropertyChanged(nameof(IsSiteListVisible));
            OnPropertyChanged(nameof(IsAttendanceVisible));
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var shell = Shell.Current;

                    if (shell == null)
                        return;

                    if (shell.Navigation.ModalStack.Count > 0)
                    {
                        await shell.Navigation.PopModalAsync();
                        return;
                    }

                    if (shell.Navigation.NavigationStack.Count > 1)
                    {
                        await shell.GoToAsync("..");
                        return;
                    }

                    var currentRoute =
                        shell.CurrentState.Location.OriginalString;

                    if (!currentRoute.Contains(
                        "DashboardPage",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        await shell.GoToAsync("//DashboardPage");
                        return;
                    }

                    bool exit = await shell.DisplayAlertAsync(
                        "Exit App",
                        "Are you sure you want to exit?",
                        "Yes",
                        "No");

                    if (exit)
                    {
                        Application.Current?.Quit();
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Navigation Error",
                        ex.Message,
                        "OK");
                }
            });

            return true;
        }
    }
}

//using tutorial_demo_2.Pages;
//using tutorial_demo_2.ViewModels;
//using Microsoft.Extensions.DependencyInjection;
//using tutorial_demo_2.Services;
//using System.ComponentModel;


//namespace tutorial_demo_2
//{
//	public partial class AppShell : Shell, INotifyPropertyChanged
//	{

//		public bool IsSiteListVisible { get; set; }
//		public bool IsAttendanceVisible { get; set; }

//		public AppShell()
//		{
//			InitializeComponent();

//			// Register routes
//			RegisterRoutes();

//			// Listen for navigation events
//			Navigated += OnNavigated;

//			BindingContext = this; // Set binding context


//			SetUserPermissions();


//		}

//		private async void OnNavigated(object sender, ShellNavigatedEventArgs e)
//		{
//			if (e.Current.Location.OriginalString.Contains(nameof(AttendancePage)))
//			{
//				var attendanceViewModel = App.Services.GetRequiredService<AttendanceViewModel>();
//				await attendanceViewModel.LoadDataAsync();
//			}
//		}


//		private void RegisterRoutes()
//		{
//			var routes = new (string, Type)[]
//			{
//			(nameof(LoginPage), typeof(LoginPage)),
//			(nameof(DashboardPage), typeof(DashboardPage)),
//			(nameof(AttendancePage), typeof(AttendancePage)),
//			(nameof(AttendanceHistoryPage), typeof(AttendanceHistoryPage)),
//			(nameof(SelfiePage), typeof(SelfiePage)),
//			(nameof(WorkerListPage), typeof(WorkerListPage)),
//			(nameof(ChangePasswordPage), typeof(ChangePasswordPage)),
//			(nameof(ChangeProfileImagePage), typeof(ChangeProfileImagePage)),
//			(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage)),


//			(nameof(SiteListPage), typeof(SiteListPage)),
//			(nameof(UserList), typeof(UserList))
//			};

//			foreach (var (route, page) in routes)
//			{
//				Routing.RegisterRoute(route, page);
//			}
//		}


//		public async void SetUserPermissions()
//		{
//			var storageService = App.Services.GetRequiredService<IAppStorageService>();
//			var users = await storageService.GetUserAsync();
//			var workmanId = users?.WorkmanId ?? string.Empty;

//			IsSiteListVisible = workmanId.Contains("FO");
//			IsAttendanceVisible = !IsSiteListVisible;

//			// Ensure both properties update in UI
//			OnPropertyChanged(nameof(IsSiteListVisible));
//			OnPropertyChanged(nameof(IsAttendanceVisible));
//		}

//		public void ResetUserPermissions()
//		{
//			IsSiteListVisible = false;
//			IsAttendanceVisible = false;
//		}

//        protected override bool OnBackButtonPressed()
//        {
//            MainThread.BeginInvokeOnMainThread(async () =>
//            {
//                try
//                {
//                    var shell = Shell.Current;

//                    if (shell == null)
//                        return;

//                    // Close modal page first
//                    if (shell.Navigation.ModalStack.Count > 0)
//                    {
//                        await shell.Navigation.PopModalAsync();
//                        return;
//                    }

//                    // Navigate to previous page
//                    if (shell.Navigation.NavigationStack.Count > 1)
//                    {
//                        await shell.GoToAsync("..");
//                        return;
//                    }

//                    // If not on Dashboard, navigate to Dashboard
//                    var currentRoute = shell.CurrentState.Location.OriginalString;

//                    if (!currentRoute.Contains("DashboardPage",
//                        StringComparison.OrdinalIgnoreCase))
//                    {
//                        await shell.GoToAsync("//DashboardPage");
//                        return;
//                    }

//                    // Ask before exiting from Dashboard
//                    bool exit = await shell.DisplayAlertAsync(
//                        "Exit App",
//                        "Are you sure you want to exit?",
//                        "Yes",
//                        "No");

//                    if (exit)
//                    {
//                        Application.Current?.Quit();
//                    }
//                }
//                catch (Exception ex)
//                {
//                    await Shell.Current.DisplayAlertAsync(
//                        "Navigation Error",
//                        ex.Message,
//                        "OK");
//                }
//            });

//            return true;
//        }


//    }

//}
