
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using tutorial_demo_2.Models;
using tutorial_demo_2.Pages;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ILoginService _loginService;
    private readonly IAppSecureStorage _secureStorage;
    private readonly IAppStorageService _storageService;
    private readonly SessionService _sessionService;

    private const string RememberMeKey = "Login.RememberMe";
    private const string LoginIdentifierKey = "Login.Identifier";
    private const string PasswordKey = "Login.Password";
    // SUP00000462,  FO00000475,   Pass@123
    private readonly Task _initializationTask;

    [ObservableProperty]
    private string _loginIdentifier = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    private string _passwordVisibilityText = "👁";

    [ObservableProperty]
    private bool _rememberMe;

    public LoginViewModel(
        ILoginService loginService,
        IAppSecureStorage secureStorage,
        IAppStorageService storageService,
        SessionService sessionService)
    {
        _loginService = loginService;
        _secureStorage = secureStorage;
        _storageService = storageService;
        _sessionService = sessionService;

        _initializationTask = LoadRememberedCredentialsAsync();
    }

    private async Task LoadRememberedCredentialsAsync()
    {
        try
        {
            RememberMe = Preferences.Default.Get(
                RememberMeKey,
                false);

            if (!RememberMe)
                return;

            LoginIdentifier = Preferences.Default.Get(
                LoginIdentifierKey,
                string.Empty);

            Password = await SecureStorage.Default.GetAsync(
                PasswordKey) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(LoginIdentifier) ||
                string.IsNullOrWhiteSpace(Password))
            {
                LoginIdentifier = string.Empty;
                Password = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Remember Me Load Error: {ex}");
        }
    }

    private async Task SaveRememberedCredentialsAsync()
    {
        try
        {
            if (RememberMe)
            {
                Preferences.Default.Set(
                    LoginIdentifierKey,
                    LoginIdentifier.Trim());

                await SecureStorage.Default.SetAsync(
                    PasswordKey,
                    Password);

                Preferences.Default.Set(
                    RememberMeKey,
                    true);
            }
            else
            {
                Preferences.Default.Set(RememberMeKey, false);

                Preferences.Default.Remove(LoginIdentifierKey);

                SecureStorage.Default.Remove(PasswordKey);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Remember Me Save Error: {ex}");
        }
    }

    [RelayCommand]
    private void TogglePassword()
    {
        IsPasswordHidden = !IsPasswordHidden;

        PasswordVisibilityText = IsPasswordHidden
            ? "👁"
            : "🙈";
    }

    [RelayCommand]
    private async Task LoginAsync(ImageButton button)
    {
        if (IsBusy)
            return;

        try
        {
            await _initializationTask;

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(LoginIdentifier) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter login credentials.";
                return;
            }

            IsBusy = true;

            if (button != null)
            {
                await button.ScaleTo(0.9, 100, Easing.Linear);
                await button.ScaleTo(1, 100, Easing.Linear);
            }

            await LogoutAsync();

            var loginRequest = new LoginRequest
            {
                LoginIdentifier = LoginIdentifier.Trim().ToUpperInvariant(),
                Password = Password
            };

            var loginResponse = await _loginService.LoginAsync(loginRequest);

            if (!loginResponse.Success || loginResponse.Data == null)
            {
                ErrorMessage =
                    loginResponse.Errors is Dictionary<string, List<string>> errors &&
                    errors.TryGetValue("workmanId", out var errorList)
                        ? errorList.FirstOrDefault() ?? "Invalid credentials."
                        : "Invalid credentials.";

                return;
            }

            var userDetails = new UserDetailsModel
            {
                Id = loginResponse.Data.WorkerId,
                WorkmanId = loginResponse.Data.WorkmanId.ToString(),
                WorkerId = loginResponse.Data.WorkerId,
                Name = $"{loginResponse.Data.FirstName} {loginResponse.Data.MiddleName} {loginResponse.Data.LastName}".Trim(),
                ProfileImage = loginResponse.Data.ProfileImage,
                SiteId = loginResponse.Data.SiteId,
                SiteName = loginResponse.Data.SiteName,
                SiteGeoLocation = loginResponse.Data.SiteGeoLocation,
                AttendanceGeoRadius = loginResponse.Data.AttendanceGeoRadius
            };

            var setAuthTokenTask =
                _secureStorage.SetAuthTokenAsync(loginResponse.Data.Token);

            var setUserDetailsTask =
                _secureStorage.SetUserDetailsAsync(userDetails);

            await Task.WhenAll(
                setAuthTokenTask,
                setUserDetailsTask);

            await SaveRememberedCredentialsAsync();

            var appShell = (AppShell)Shell.Current;

            appShell.SetUserPermissions();

            await Shell.Current.GoToAsync(
                "//DashboardPage",
                true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LogoutAsync()
    {
        await _storageService.ClearStorageAsync();

        _sessionService.Remove("SiteId");
        _sessionService.Remove("SiteName");
        _sessionService.Remove("GpsLocation");

        var appShell = (AppShell)Shell.Current;

        appShell.IsSiteListVisible = false;
        appShell.IsAttendanceVisible = false;

        appShell.ResetUserPermissions();
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
