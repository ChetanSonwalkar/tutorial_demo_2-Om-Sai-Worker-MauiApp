
using tutorial_demo_2.Services;
using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class AttendancePage : ContentPage
{
    private readonly AttendanceViewModel _viewModel;
    private bool _isLoading;
    private bool _isNavigatingBack;

    public AttendancePage(
        IAppStorageService storageService,
        IAppSecureStorage secureStorage,
        HttpClient httpClient,
        IAttendanceService attendanceService,
        AttendanceViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            await _viewModel.RefreshData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"AttendancePage Error: {ex}");

            await DisplayAlertAsync(
                "Attendance Error",
                "Unable to load attendance data. Please try again.",
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await NavigateBackAsync();
    }

    private async Task NavigateBackAsync()
    {
        if (_isNavigatingBack)
            return;

        _isNavigatingBack = true;

        try
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1 ||
                Shell.Current.Navigation.ModalStack.Count > 0)
            {
                await Shell.Current.GoToAsync("..", true);
            }
            else
            {
                await Shell.Current.GoToAsync(
                    "//DashboardPage",
                    true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Attendance Back Navigation Error: {ex}");

            try
            {
                await Shell.Current.GoToAsync(
                    "//DashboardPage",
                    true);
            }
            catch (Exception navigationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Dashboard Navigation Error: {navigationException}");
            }
        }
        finally
        {
            _isNavigatingBack = false;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(
            async () => await NavigateBackAsync());

        return true;
    }
}



//using tutorial_demo_2.Services;
//using tutorial_demo_2.ViewModels;

//namespace tutorial_demo_2.Pages;

//public partial class AttendancePage : ContentPage
//{
//	private readonly IAppStorageService _storageService;
//	private readonly HttpClient _httpClient;
//	private readonly IAppSecureStorage _secureStorage;
//	private readonly IAttendanceService _attendanceService;

//	public AttendancePage(
//		IAppStorageService storageService,
//		IAppSecureStorage secureStorage,
//		HttpClient httpClient,
//		IAttendanceService attendanceService,
//		AttendanceViewModel viewModel) // Inject the ViewModel
//	{
//		try
//		{
//			InitializeComponent();
//			_storageService = storageService;
//			_httpClient = httpClient;
//			_secureStorage = secureStorage;
//			_attendanceService = attendanceService;

//			// Use the injected ViewModel
//			BindingContext = viewModel;
//		}
//		catch (Exception ex)
//		{
//			Console.WriteLine($"AttendancePage Error: {ex.Message}");
//			Shell.Current.DisplayAlertAsync("Error", $"Something went wrong: {ex.Message}", "OK");
//		}
//	}

//	protected override void OnAppearing()
//	{
//		base.OnAppearing();

//		// Assuming your ViewModel is set as the BindingContext
//		if (BindingContext is AttendanceViewModel viewModel)
//		{
//			viewModel.RefreshData();
//		}
//	}


//    private async void AttendanceButton_Clicked(object sender, EventArgs e)
//    {
//        try
//        {
//            await Shell.Current.GoToAsync(nameof(AttendanceHistoryPage));
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlert("Navigation Error", ex.ToString(), "OK");
//        }
//    }


//}