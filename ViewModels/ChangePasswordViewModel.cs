
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IWorkerService _workerService;
    private readonly IAppSecureStorage _secureStorage;
    private readonly IAppStorageService _storageService;
    private readonly SessionService _sessionService;

    private int _workerId;

    private readonly Task _workerIdLoadTask;

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isCurrentPasswordHidden = true;

    [ObservableProperty]
    private bool isNewPasswordHidden = true;

    [ObservableProperty]
    private bool isConfirmPasswordHidden = true;

    [ObservableProperty]
    private string currentPasswordIcon = "👁";

    [ObservableProperty]
    private string newPasswordIcon = "👁";

    [ObservableProperty]
    private string confirmPasswordIcon = "👁";

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ChangePasswordViewModel(
        IWorkerService workerService,
        IAppSecureStorage secureStorage,
        IAppStorageService storageService,
        SessionService sessionService)
    {
        _workerService = workerService;
        _secureStorage = secureStorage;
        _storageService = storageService;
        _sessionService = sessionService;

        _workerIdLoadTask = LoadWorkerIdAsync();
    }

    [RelayCommand]
    private void ToggleCurrentPassword()
    {
        IsCurrentPasswordHidden = !IsCurrentPasswordHidden;

        CurrentPasswordIcon = IsCurrentPasswordHidden
            ? "👁"
            : "🙈";
    }

    [RelayCommand]
    private void ToggleNewPassword()
    {
        IsNewPasswordHidden = !IsNewPasswordHidden;

        NewPasswordIcon = IsNewPasswordHidden
            ? "👁"
            : "🙈";
    }

    [RelayCommand]
    private void ToggleConfirmPassword()
    {
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;

        ConfirmPasswordIcon = IsConfirmPasswordHidden
            ? "👁"
            : "🙈";
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        try
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("//DashboardPage");
            }
        }
        catch (Exception ex)
        {
            await ShowAlert("Navigation Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentPassword) ||
            string.IsNullOrWhiteSpace(NewPassword) ||
            string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "All fields are required.";
            await ShowAlert("Validation Error", ErrorMessage);
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "New passwords do not match.";
            await ShowAlert("Validation Error", ErrorMessage);
            return;
        }

        if (CurrentPassword == NewPassword)
        {
            ErrorMessage = "New password must be different from current password.";
            await ShowAlert("Validation Error", ErrorMessage);
            return;
        }

        if (!IsPasswordValid(NewPassword))
        {
            ErrorMessage = "Password must contain at least 8 characters, one uppercase letter, one number, and one special character.";
            await ShowAlert("Invalid Password", ErrorMessage);
            return;
        }

        IsBusy = true;

        try
        {
            await _workerIdLoadTask;

            if (_workerId <= 0)
            {
                ErrorMessage = "Worker details not found. Please login again.";

                await ShowAlert("Error", ErrorMessage);

                await Shell.Current.GoToAsync("//LoginPage", true);
                return;
            }

            var request = new ChangePasswordRequest
            {
                WorkerId = _workerId,
                OldPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword
            };

            var response = await _workerService.ChangePasswordAsync(request);

            if (response == null)
            {
                ErrorMessage = "No response received from server.";
                await ShowAlert("Error", ErrorMessage);
                return;
            }

            if (response.Success)
            {
                await ShowAlert(
                    "Success",
                    "Password changed successfully. Please login with your new password.");

                await ClearAllDataAsync();

                await Shell.Current.GoToAsync("//LoginPage", true);
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(response.Message)
                    ? "Unable to change password. Please try again."
                    : response.Message;

                await ShowAlert("Change Password Failed", ErrorMessage);
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;

            await ShowAlert("Network Error", ErrorMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            System.Diagnostics.Debug.WriteLine(
                $"Change Password Error: {ex}");

            await ShowAlert("Error", ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadWorkerIdAsync()
    {
        try
        {
            var userDetails = await _secureStorage.GetUserDetailsAsync();

            if (userDetails != null)
            {
                _workerId = userDetails.WorkerId;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Load Worker ID Error: {ex}");
        }
    }

    private async Task ClearAllDataAsync()
    {
        await _secureStorage.RemoveAllAsync();

        await _storageService.ClearStorageAsync();

        Preferences.Default.Clear();

        _sessionService.Remove("SiteId");
        _sessionService.Remove("SiteName");
        _sessionService.Remove("GpsLocation");
    }

    private bool IsPasswordValid(string password)
    {
        return Regex.IsMatch(
            password,
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()]).{8,}$");
    }

    private async Task ShowAlert(string title, string message)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync(
                title,
                message,
                "OK");
        }
    }
}


//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Services;

//namespace tutorial_demo_2.ViewModels
//{
//	public partial class ChangePasswordViewModel : ObservableObject
//	{
//		private readonly IWorkerService _workerService;
//		private readonly IAppSecureStorage _secureStorage;
//		private readonly IAppStorageService _storageService;
//		private readonly SessionService _sessionService;


//		private int _workerId;

//		[ObservableProperty]
//		private string currentPassword;

//		[ObservableProperty]
//		private string newPassword;

//		[ObservableProperty]
//		private string confirmPassword;

//		[ObservableProperty]
//		private bool isBusy = false;

//		public bool IsNotBusy => !IsBusy;


//		public ChangePasswordViewModel(
//			IWorkerService workerService,
//			IAppSecureStorage secureStorage,
//			IAppStorageService storageService,
//			SessionService sessionService)
//		{
//			_workerService = workerService;
//			_secureStorage = secureStorage;
//			_storageService = storageService;

//			LoadWorkerId();
//			IsBusy = false; // Ensure it starts as false
//			_sessionService = sessionService;
//		}

//		[RelayCommand]
//		private async Task ChangePasswordAsync()
//		{
//			if (IsBusy) return;
//			IsBusy = true;

//			if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
//			{
//				await ShowAlert("Error", "All fields are required.");
//				IsBusy = false;
//				return;
//			}

//			if (NewPassword != ConfirmPassword)
//			{
//				await ShowAlert("Error", "New passwords do not match.");
//				IsBusy = false;
//				return;
//			}

//			if (!IsPasswordValid(NewPassword))
//			{
//				await ShowAlert("Error", "Password must be at least 8 characters long and contain an uppercase letter, a number, and a special character.");
//				IsBusy = false;
//				return;
//			}

//			var request = new ChangePasswordRequest
//			{
//				WorkerId = _workerId,
//				OldPassword = CurrentPassword,
//				NewPassword = NewPassword,
//				ConfirmPassword = ConfirmPassword
//			};

//			var response = await _workerService.ChangePasswordAsync(request);

//			if (response.Success)
//			{
//				await ShowAlert("Success", "Password changed successfully.");

//				// ✅ Clear all session, cache, and storage before logging out
//				await ClearAllDataAsync();

//				// ✅ Delay navigation slightly to avoid UI conflicts
//				await Task.Delay(500);

//				try
//				{
//					await Shell.Current.GoToAsync("//LoginPage", true);
//				}
//				catch (Exception ex)
//				{
//					await ShowAlert("Navigation Error", ex.Message);
//				}
//			}
//			else
//			{
//				await ShowAlert("Error", response.Message);
//			}

//			IsBusy = false;
//		}


//		private async Task ClearAllDataAsync()
//		{
//			// ✅ Clear secure storage
//			await _secureStorage.RemoveAllAsync();

//			// ✅ Clear local app storage
//			await _storageService.ClearStorageAsync();

//			// ✅ Clear preferences (alternative to Application.Current.Properties)
//			Preferences.Clear();

//			_sessionService.Remove("SiteId");
//			_sessionService.Remove("SiteName");
//			_sessionService.Remove("GpsLocation");


//		}



//		private async void LoadWorkerId()
//		{
//			var userDetails = await _secureStorage.GetUserDetailsAsync();
//			if (userDetails != null)
//				_workerId = userDetails.WorkerId;
//			else
//				await Shell.Current.GoToAsync("//LoginPage", true);
//		}

//		private bool IsPasswordValid(string password)
//		{
//			var regex = new Regex(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()]).{8,}$");
//			return regex.IsMatch(password);
//		}

//		private async Task ShowAlert(string title, string message) =>
//			await Application.Current.MainPage.DisplayAlert(title, message, "OK");
//	}
//}
