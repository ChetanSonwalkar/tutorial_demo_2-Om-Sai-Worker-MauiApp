
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using tutorial_demo_2.Models;
using tutorial_demo_2.Pages;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels
{
    public partial class AttendanceViewModel : ObservableObject
    {
        private readonly IAppStorageService _storageService;
        private readonly HttpClient _httpClient;
        private readonly IAppSecureStorage _secureStorage;
        private readonly IAttendanceService _attendanceService;
        private readonly SessionService _sessionService;

        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
        private readonly SemaphoreSlim _locationSemaphore = new(1, 1);
        private readonly SemaphoreSlim _navigationSemaphore = new(1, 1);

        private const int MAX_ALLOWED_DISTANCE = 1000;

        [ObservableProperty]
        private AttendanceRecord? lastAttendance;

        [ObservableProperty]
        private string currentDate = DateTime.Now.ToString("MMM dd, yyyy");

        [ObservableProperty]
        private string inTime = "--:--";

        [ObservableProperty]
        private string outTime = "--:--";

        [ObservableProperty]
        private string workingHours = "00:00";

        [ObservableProperty]
        private bool showClock;

        [ObservableProperty]
        private double latitude;

        [ObservableProperty]
        private double longitude;

        [ObservableProperty]
        private string geoLocationStatus = string.Empty;

        [ObservableProperty]
        private string currentAddress = string.Empty;

        [ObservableProperty]
        private string siteAddress = string.Empty;

        [ObservableProperty]
        private double liveDistance;

        [ObservableProperty]
        private string liveDistanceBackground = "#FF6B6B";

        [ObservableProperty]
        private ObservableCollection<SiteSelectedModel> siteLocations = new();

        [ObservableProperty]
        private ObservableCollection<SiteSelectedModel> filteredSiteLocations = new();

        [ObservableProperty]
        private int selectedSiteId;

        [ObservableProperty]
        private string selectedSite = string.Empty;

        [ObservableProperty]
        private string siteGeoLocation = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isSitePickerOpen;

        [ObservableProperty]
        private bool isSiteListVisible;

        [ObservableProperty]
        private string userRole = string.Empty;

        [ObservableProperty]
        private int? workerId;

        [ObservableProperty]
        private string? workmanId;

        [ObservableProperty]
        private bool showCaptureButton;

        [ObservableProperty]
        private bool showWorkerListButton;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isLocationLoading;

        public ICommand NavigateToSelfiePageCommand { get; }
        public ICommand NavigateToWorkerListCommand { get; }
        public ICommand NavigateToAttendanceHistoryCommand { get; }

        public AttendanceViewModel(
            IAppStorageService storageService,
            IAppSecureStorage secureStorage,
            HttpClient httpClient,
            IAttendanceService attendanceService,
            SessionService sessionService)
        {
            _storageService = storageService;
            _secureStorage = secureStorage ??
                throw new ArgumentNullException(nameof(secureStorage));
            _httpClient = httpClient;
            _attendanceService = attendanceService;
            _sessionService = sessionService;

            NavigateToSelfiePageCommand =
                new AsyncRelayCommand(NavigateToSelfiePage);

            NavigateToWorkerListCommand =
                new AsyncRelayCommand(NavigateToWorkerList);

            NavigateToAttendanceHistoryCommand =
                new AsyncRelayCommand(NavigateToAttendanceHistory);
        }

        public async Task RefreshData()
        {
            if (!await _refreshSemaphore.WaitAsync(0))
                return;

            try
            {
                IsBusy = true;

                await Task.WhenAll(
                    LoadSites(),
                    LoadLastAttendanceAsync());

                await GetCurrentLocation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RefreshData Error: {ex}");
            }
            finally
            {
                IsBusy = false;
                _refreshSemaphore.Release();
            }
        }

        private async Task NavigateToAttendanceHistory()
        {
            await Shell.Current.GoToAsync(
                nameof(AttendanceHistoryPage),
                true);
        }

        private async Task NavigateToWorkerList()
        {
            try
            {
                var userDetails =
                    await _secureStorage.GetUserDetailsAsync();

                if (userDetails == null)
                {
                    await ShowErrorAndResetUI(
                        "User details not found. Please login again.");

                    await Shell.Current.GoToAsync(
                        "//LoginPage",
                        true);

                    return;
                }

                var parameters = new Dictionary<string, object>
                {
                    ["SiteId"] = userDetails.SiteId
                };

                await Shell.Current.GoToAsync(
                    nameof(WorkerListPage),
                    true,
                    parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"NavigateToWorkerList Error: {ex}");

                await ShowErrorAndResetUI(
                    "Unable to open worker list.");
            }
        }

        internal async Task ExternalNavigateToSelfiePage(
            string? workmanIdParam = null,
            int? workerIdParam = null)
        {
            WorkmanId = workmanIdParam;
            WorkerId = workerIdParam;

            await NavigateToSelfiePage();
        }

        private async Task NavigateToSelfiePage()
        {
            if (!await _navigationSemaphore.WaitAsync(0))
                return;

            try
            {
                if (Latitude == 0 || Longitude == 0)
                {
                    await GetCurrentLocation();
                }

                if (string.IsNullOrWhiteSpace(WorkmanId) ||
                    WorkerId == null ||
                    WorkerId <= 0)
                {
                    var userDetails =
                        await _secureStorage.GetUserDetailsAsync();

                    if (userDetails == null)
                    {
                        await ShowErrorAndNavigateToLogin(
                            "User details not found. Please login again.");

                        return;
                    }

                    WorkmanId = userDetails.WorkmanId;
                    WorkerId = userDetails.WorkerId;
                }

                if (WorkerId == null || WorkerId <= 0)
                {
                    await ShowErrorAndResetUI(
                        "Worker ID is missing. Please login again.");

                    return;
                }

                if (string.IsNullOrWhiteSpace(WorkmanId))
                {
                    await ShowErrorAndResetUI(
                        "Workman ID is missing. Please login again.");

                    return;
                }

                if (Latitude == 0 && Longitude == 0)
                {
                    await ShowErrorAndResetUI(
                        "Current location is not available. Please try again.");

                    return;
                }

                if (IsRadiusCheckRequired())
                {
                    var allowedRadius = await GetAllowedRadiusAsync();

                    if (LiveDistance > allowedRadius)
                    {
                        await ShowErrorAndResetUI(
                            $"You are outside the allowed attendance radius of {allowedRadius:0} meters.");

                        return;
                    }
                }

                var token = await _secureStorage.GetAuthTokenAsync();

                if (string.IsNullOrWhiteSpace(token))
                {
                    await ShowErrorAndNavigateToLogin(
                        "Authentication token not found. Please login again.");

                    return;
                }

                var endpoint =
                    $"{ApiUrls.GetAttendanceValidateEndpoint}{WorkmanId}";

                using var request =
                    new HttpRequestMessage(HttpMethod.Get, endpoint);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                using var response =
                    await _httpClient.SendAsync(request);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Validate API URL: {endpoint}");

                System.Diagnostics.Debug.WriteLine(
                    $"Validate API HTTP Status: {(int)response.StatusCode}");

                System.Diagnostics.Debug.WriteLine(
                    $"Validate API Raw Response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    await ShowErrorAndResetUI(
                        $"Attendance validation failed ({(int)response.StatusCode}). {responseBody}");

                    return;
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result =
                    JsonSerializer.Deserialize<
                        ApiResponse<ValidateAttendanceResponse>>(
                        responseBody,
                        jsonOptions);

                if (result?.Data == null)
                {
                    await ShowErrorAndResetUI(
                        "Invalid attendance validation response.");

                    return;
                }

                if (!result.Success)
                {
                    var apiError = result.Errors == null
                        ? "Attendance validation was not successful."
                        : string.Join(
                            " ",
                            result.Errors.SelectMany(
                                x => x.Value ?? new List<string>()));

                    await ShowErrorAndResetUI(apiError);
                    return;
                }

                if (!result.Data.IsAttendanceAllowed)
                {
                    await ShowErrorAndResetUI(
                        result.Data.Remark ??
                        "Attendance is not allowed at this time.");

                    return;
                }

                string? attendanceType =
                    result.Data.GetInOutType();

                if (string.IsNullOrWhiteSpace(attendanceType))
                {
                    attendanceType =
                        result.Data.GetNextAttendanceType();
                }

                if (string.IsNullOrWhiteSpace(attendanceType))
                {
                    attendanceType =
                        await ResolveAttendanceTypeFromLastRecordAsync(
                            WorkerId.Value);
                }

                attendanceType =
                    attendanceType?.Trim().ToUpperInvariant();

                System.Diagnostics.Debug.WriteLine(
                    $"Resolved Attendance Type: {attendanceType}");

                if (attendanceType != "IN" &&
                    attendanceType != "OUT")
                {
                    await ShowErrorAndResetUI(
                        "Attendance type is missing or invalid. Please check the validation API response.");

                    return;
                }

                int siteId =
                    result.Data.SiteId.GetValueOrDefault();

                if (siteId <= 0)
                    siteId = SelectedSiteId;

                if (siteId <= 0)
                {
                    await ShowErrorAndResetUI(
                        "Site ID is missing. Please reload attendance page.");

                    return;
                }

                int siteShiftId =
                    result.Data.SiteShiftId.GetValueOrDefault();

                if (siteShiftId <= 0)
                {
                    await ShowErrorAndResetUI(
                        "Site Shift ID was not returned by the validation API.");

                    return;
                }

                var navigationParameter =
                    new Dictionary<string, object>
                    {
                        ["WorkerId"] = WorkerId.Value,
                        ["InOutType"] = attendanceType,
                        ["Status"] = result.Data.NewStatus ?? "Present",
                        ["SiteId"] = siteId,
                        ["SiteShiftId"] = siteShiftId,
                        ["GpsLocation"] =
                            result.Data.GpsLocation ??
                            GeoLocationStatus ??
                            $"{Latitude},{Longitude}"
                    };

                await Shell.Current.GoToAsync(
                    nameof(SelfiePage),
                    true,
                    navigationParameter);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Validate API JSON Error: {ex}");

                await ShowErrorAndResetUI(
                    "Unable to read attendance validation response.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"NavigateToSelfiePage Error: {ex}");

                await ShowErrorAndResetUI(
                    $"Unable to open selfie attendance: {ex.Message}");
            }
            finally
            {
                _navigationSemaphore.Release();
            }
        }

        private async Task<string?> ResolveAttendanceTypeFromLastRecordAsync(
            int workerId)
        {
            try
            {
                var response =
                    await _attendanceService.LastAttendanceAsync(workerId);

                if (response?.Success != true)
                    return null;

                var lastRecord = response.Data?
                    .Where(x => x.CreatedAt.Date == DateTime.Today)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (lastRecord == null)
                    return "IN";

                if (lastRecord.InTime.HasValue &&
                    !lastRecord.OutTime.HasValue)
                {
                    return "OUT";
                }

                if (lastRecord.InTime.HasValue &&
                    lastRecord.OutTime.HasValue)
                {
                    return "IN";
                }

                string? lastType =
                    lastRecord.InOutType?.Trim().ToUpperInvariant();

                if (lastType == "IN")
                    return "OUT";

                if (lastType == "OUT")
                    return "IN";

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ResolveAttendanceType Error: {ex}");

                return null;
            }
        }

        private bool IsRadiusCheckRequired()
        {
            return UserRole != "Field Officer" &&
                   UserRole != "Supervisor";
        }

        private async Task<double> GetAllowedRadiusAsync()
        {
            var userDetails =
                await _secureStorage.GetUserDetailsAsync();

            return userDetails?.AttendanceGeoRadius > 0
                ? userDetails.AttendanceGeoRadius
                : MAX_ALLOWED_DISTANCE;
        }

        private async Task ShowErrorAndResetUI(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                LiveDistanceBackground = "#FF6B6B";
                ShowCaptureButton = false;

                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    message,
                    "OK");
            });
        }

        private async Task ShowErrorAndNavigateToLogin(string message)
        {
            await ShowErrorAndResetUI(message);

            await Shell.Current.GoToAsync(
                "//LoginPage",
                true);
        }

        public async Task LoadLastAttendanceAsync()
        {
            try
            {
                var users = await _storageService.GetUserAsync();

                if (users == null)
                {
                    ShowClock = false;
                    return;
                }

                var response =
                    await _attendanceService.LastAttendanceAsync(
                        users.WorkerId);

                var lastRecord = response?.Data?
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefault();

                if (response?.Success == true &&
                    lastRecord != null)
                {
                    LastAttendance = lastRecord;

                    CurrentDate =
                        lastRecord.CreatedAt != default
                            ? lastRecord.CreatedAt.ToString("MMM dd, yyyy")
                            : "--";

                    InTime =
                        lastRecord.InTime?.ToString("hh:mm tt") ?? "--:--";

                    OutTime =
                        lastRecord.OutTime?.ToString("hh:mm tt") ?? "--:--";

                    WorkingHours =
                        ConvertMinutesToHHMM(lastRecord.WorkingHrs);

                    ShowClock = true;
                }
                else
                {
                    ShowClock = false;
                    InTime = "--:--";
                    OutTime = "--:--";
                    WorkingHours = "00:00";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadLastAttendance Error: {ex}");
            }
        }

        private string ConvertMinutesToHHMM(int? minutes)
        {
            if (minutes == null || minutes <= 0)
                return "00:00";

            int hours = minutes.Value / 60;
            int remainingMinutes = minutes.Value % 60;

            return $"{hours:D2}:{remainingMinutes:D2}";
        }

        public async Task LoadDataAsync()
        {
            await RefreshData();
        }

        [RelayCommand]
        private async Task MarkAttendance()
        {
            if (string.IsNullOrWhiteSpace(SelectedSite) ||
                string.IsNullOrWhiteSpace(SiteGeoLocation))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    "Please select a site before marking attendance.",
                    "OK");

                return;
            }

            await Shell.Current.DisplayAlertAsync(
                SelectedSite,
                SiteGeoLocation,
                "OK");
        }

        [RelayCommand]
        public async Task GetCurrentLocation()
        {
            if (!await _locationSemaphore.WaitAsync(0))
                return;

            try
            {
                IsLocationLoading = true;

                var permission =
                    await Permissions.CheckStatusAsync<
                        Permissions.LocationWhenInUse>();

                if (permission != PermissionStatus.Granted)
                {
                    permission =
                        await Permissions.RequestAsync<
                            Permissions.LocationWhenInUse>();
                }

                if (permission != PermissionStatus.Granted)
                {
                    GeoLocationStatus = "Location permission denied.";
                    ShowCaptureButton = false;
                    return;
                }

                Location? location = null;

                try
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Medium,
                        TimeSpan.FromSeconds(7));

                    location =
                        await Geolocation.Default.GetLocationAsync(request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"GPS Request Error: {ex.Message}");
                }

                if (location == null)
                {
                    location =
                        await Geolocation.Default.GetLastKnownLocationAsync();
                }

                if (location == null)
                {
                    GeoLocationStatus = "Location not available.";
                    ShowCaptureButton = false;
                    return;
                }

                Latitude = location.Latitude;
                Longitude = location.Longitude;

                GeoLocationStatus =
                    $"{Latitude.ToString(CultureInfo.InvariantCulture)}," +
                    $"{Longitude.ToString(CultureInfo.InvariantCulture)}";

                CalculateAndUpdateDistance();

                _ = GetAddressFromCoordinates(
                    Latitude,
                    Longitude,
                    "current");
            }
            catch (FeatureNotSupportedException)
            {
                GeoLocationStatus =
                    "Geolocation is not supported on this device.";

                ShowCaptureButton = false;
            }
            catch (PermissionException)
            {
                GeoLocationStatus =
                    "Location permission was denied.";

                ShowCaptureButton = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GetCurrentLocation Error: {ex}");

                GeoLocationStatus = "Location unavailable.";
                ShowCaptureButton = false;
            }
            finally
            {
                IsLocationLoading = false;
                _locationSemaphore.Release();
            }
        }

        private void CalculateAndUpdateDistance()
        {
            if (!IsRadiusCheckRequired())
            {
                LiveDistance = 0;
                LiveDistanceBackground = "#E0F7FF";
                ShowCaptureButton = true;
                return;
            }

            if (!TryGetSiteCoordinates(
                out double siteLatitude,
                out double siteLongitude))
            {
                LiveDistance = 0;
                LiveDistanceBackground = "#FF6B6B";
                ShowCaptureButton = false;
                return;
            }

            LiveDistance = Math.Round(
                CalculateDistance(
                    Latitude,
                    Longitude,
                    siteLatitude,
                    siteLongitude),
                2);

            _ = UpdateDistanceStatusAsync();
        }

        private async Task UpdateDistanceStatusAsync()
        {
            try
            {
                double allowedRadius =
                    await GetAllowedRadiusAsync();

                LiveDistanceBackground =
                    LiveDistance > allowedRadius
                        ? "#FF6B6B"
                        : "#E0F7FF";

                ShowCaptureButton =
                    LiveDistance <= allowedRadius;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateDistanceStatus Error: {ex}");

                ShowCaptureButton = false;
            }
        }

        private bool TryGetSiteCoordinates(
            out double siteLatitude,
            out double siteLongitude)
        {
            siteLatitude = 0;
            siteLongitude = 0;

            if (string.IsNullOrWhiteSpace(SiteGeoLocation))
                return false;

            var coordinates = SiteGeoLocation
                .Replace(" ", string.Empty)
                .Split(',');

            return coordinates.Length == 2 &&
                   double.TryParse(
                       coordinates[0],
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out siteLatitude) &&
                   double.TryParse(
                       coordinates[1],
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out siteLongitude);
        }

        private async Task GetAddressFromCoordinates(
            double latitude,
            double longitude,
            string addressFor)
        {
            try
            {
                var placemark =
                    (await Geocoding.Default.GetPlacemarksAsync(
                        latitude,
                        longitude))?.FirstOrDefault();

                string address = placemark != null
                    ? string.Join(", ", new[]
                    {
                        placemark.FeatureName,
                        placemark.Thoroughfare,
                        placemark.SubThoroughfare,
                        placemark.Locality,
                        placemark.AdminArea,
                        placemark.CountryName,
                        placemark.PostalCode
                    }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    : "No address found.";

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (addressFor == "site")
                        SiteAddress = address;
                    else if (addressFor == "current")
                        CurrentAddress = address;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Reverse Geocoding Error: {ex.Message}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (addressFor == "site")
                        SiteAddress = "Address unavailable.";
                    else if (addressFor == "current")
                        CurrentAddress = "Address unavailable.";
                });
            }
        }

        private static double CalculateDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double earthRadius = 6371000;

            double phi1 = lat1 * Math.PI / 180;
            double phi2 = lat2 * Math.PI / 180;
            double deltaPhi = (lat2 - lat1) * Math.PI / 180;
            double deltaLambda = (lon2 - lon1) * Math.PI / 180;

            double a =
                Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) *
                Math.Sin(deltaLambda / 2);

            double c =
                2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        [RelayCommand]
        private async Task LoadSites()
        {
            try
            {
                var token = await _storageService.GetTokenAsync();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var users = await _storageService.GetUserAsync();

                if (users == null)
                {
                    ShowCaptureButton = false;
                    return;
                }

                string workmanId = users.WorkmanId ?? string.Empty;

                ShowWorkerListButton = false;

                if (workmanId.StartsWith(
                    "FO",
                    StringComparison.OrdinalIgnoreCase))
                {
                    UserRole = "Field Officer";
                    IsSiteListVisible = true;
                }
                else if (workmanId.StartsWith(
                    "MD",
                    StringComparison.OrdinalIgnoreCase))
                {
                    UserRole = "Management";
                    IsSiteListVisible = false;
                }
                else if (workmanId.StartsWith(
                    "SUP",
                    StringComparison.OrdinalIgnoreCase))
                {
                    UserRole = "Supervisor";
                    IsSiteListVisible = false;
                    ShowWorkerListButton = true;
                }
                else
                {
                    UserRole = "Worker";
                    IsSiteListVisible = false;
                }

                if (UserRole is "Worker" or "Supervisor")
                {
                    SelectedSiteId = users.SiteId;
                    SelectedSite = users.SiteName ?? string.Empty;
                    SiteGeoLocation =
                        users.SiteGeoLocation ?? string.Empty;
                }
                else if (UserRole == "Field Officer")
                {
                    SelectedSiteId =
                        _sessionService.Get<int>("SiteId");

                    SelectedSite =
                        _sessionService.Get<string>("SiteName") ?? string.Empty;

                    SiteGeoLocation =
                        _sessionService.Get<string>("GpsLocation") ?? string.Empty;
                }
                else
                {
                    SelectedSiteId = 0;
                    SelectedSite = "Site not assigned!";
                    SiteGeoLocation = string.Empty;
                }

                if (TryGetSiteCoordinates(
                    out double siteLatitude,
                    out double siteLongitude))
                {
                    _ = GetAddressFromCoordinates(
                        siteLatitude,
                        siteLongitude,
                        "site");
                }
                else
                {
                    SiteAddress = "Site location not available.";
                }

                OnPropertyChanged(nameof(SelectedSite));
                OnPropertyChanged(nameof(IsSiteListVisible));

                if (Latitude != 0 || Longitude != 0)
                    CalculateAndUpdateDistance();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadSites Error: {ex}");
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            string searchValue = value ?? string.Empty;

            FilteredSiteLocations.Clear();

            foreach (var site in SiteLocations.Where(
                x => (x.SiteName ?? string.Empty).Contains(
                    searchValue,
                    StringComparison.OrdinalIgnoreCase)))
            {
                FilteredSiteLocations.Add(site);
            }
        }
    }
}











////using CommunityToolkit.Mvvm.ComponentModel;
////using CommunityToolkit.Mvvm.Input;
////using Microsoft.Maui.Devices.Sensors;
////using System.Collections.ObjectModel;
////using System.Globalization;
////using System.Net.Http.Headers;
////using System.Net.Http.Json;
////using System.Windows.Input;
////using tutorial_demo_2.Models;
////using tutorial_demo_2.Pages;
////using tutorial_demo_2.Services;

////using System.Text.Json;

////namespace tutorial_demo_2.ViewModels;

////public partial class AttendanceViewModel : ObservableObject
////{
////    private readonly IAppStorageService _storageService;
////    private readonly HttpClient _httpClient;
////    private readonly IAppSecureStorage _secureStorage;
////    private readonly IAttendanceService _attendanceService;
////    private readonly SessionService _sessionService;

////    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
////    private readonly SemaphoreSlim _locationSemaphore = new(1, 1);

////    private const int MAX_ALLOWED_DISTANCE = 1000;

////    [ObservableProperty]
////    private AttendanceRecord? _lastAttendance;

////    [ObservableProperty]
////    private string _currentDate = DateTime.Now.ToString("MMM dd, yyyy");

////    [ObservableProperty]
////    private string _inTime = "--:--";

////    [ObservableProperty]
////    private string _outTime = "--:--";

////    [ObservableProperty]
////    private string _workingHours = "00:00";

////    [ObservableProperty]
////    private bool _showClock;

////    [ObservableProperty]
////    private double _latitude;

////    [ObservableProperty]
////    private double _longitude;

////    [ObservableProperty]
////    private string _geoLocationStatus = string.Empty;

////    [ObservableProperty]
////    private string _currentAddress = string.Empty;

////    [ObservableProperty]
////    private string _siteAddress = string.Empty;

////    [ObservableProperty]
////    private double _liveDistance;

////    [ObservableProperty]
////    private string _liveDistanceBackground = "#FF6B6B";

////    [ObservableProperty]
////    private ObservableCollection<SiteSelectedModel> _siteLocations = new();

////    [ObservableProperty]
////    private ObservableCollection<SiteSelectedModel> _filteredSiteLocations = new();

////    [ObservableProperty]
////    private int _selectedSiteId;

////    [ObservableProperty]
////    private string _selectedSite = string.Empty;

////    [ObservableProperty]
////    private string _siteGeoLocation = string.Empty;

////    [ObservableProperty]
////    private string _searchText = string.Empty;

////    [ObservableProperty]
////    private bool _isSitePickerOpen;

////    [ObservableProperty]
////    private bool _isSiteListVisible;

////    [ObservableProperty]
////    private string _userRole = string.Empty;

////    [ObservableProperty]
////    private int? _workerId;

////    [ObservableProperty]
////    private string? _workmanId;

////    [ObservableProperty]
////    private bool _showCaptureButton;

////    [ObservableProperty]
////    private bool _showWorkerListButton;

////    [ObservableProperty]
////    private bool _isBusy;

////    public ICommand NavigateToSelfiePageCommand { get; }
////    public ICommand NavigateToWorkerListCommand { get; }
////    public ICommand NavigateToAttendanceHistoryCommand { get; }

////    public AttendanceViewModel(
////        IAppStorageService storageService,
////        IAppSecureStorage secureStorage,
////        HttpClient httpClient,
////        IAttendanceService attendanceService,
////        SessionService sessionService)
////    {
////        _storageService = storageService;
////        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
////        _httpClient = httpClient;
////        _attendanceService = attendanceService;
////        _sessionService = sessionService;

////        NavigateToSelfiePageCommand = new AsyncRelayCommand(NavigateToSelfiePage);
////        NavigateToWorkerListCommand = new AsyncRelayCommand(NavigateToWorkerList);
////        NavigateToAttendanceHistoryCommand = new AsyncRelayCommand(NavigateToAttendanceHistory);
////    }

////    public async Task RefreshData()
////    {
////        if (!await _refreshSemaphore.WaitAsync(0))
////            return;

////        try
////        {
////            IsBusy = true;

////            await Task.WhenAll(
////                LoadSites(),
////                LoadLastAttendanceAsync()
////            );

////            await GetCurrentLocation();
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine($"RefreshData Error: {ex}");
////        }
////        finally
////        {
////            IsBusy = false;
////            _refreshSemaphore.Release();
////        }
////    }

////    private async Task NavigateToAttendanceHistory()
////    {
////        await Shell.Current.GoToAsync(nameof(AttendanceHistoryPage), true);
////    }

////    private async Task NavigateToWorkerList()
////    {
////        try
////        {
////            var userDetails = await _secureStorage.GetUserDetailsAsync();

////            if (userDetails == null)
////            {
////                LiveDistanceBackground = "#FF6B6B";
////                ShowCaptureButton = false;

////                await Shell.Current.DisplayAlertAsync(
////                    "Error",
////                    "User details not found. Please login again.",
////                    "OK");

////                await Shell.Current.GoToAsync("//LoginPage", true);
////                return;
////            }

////            var navigationParameter = new Dictionary<string, object>
////            {
////                { "SiteId", userDetails.SiteId }
////            };

////            await Shell.Current.GoToAsync(
////                nameof(WorkerListPage),
////                true,
////                navigationParameter);
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine($"NavigateToWorkerList Error: {ex}");

////            await Shell.Current.DisplayAlertAsync(
////                "Error",
////                "Unable to open worker list. Please try again.",
////                "OK");
////        }
////    }

////    internal async Task ExternalNavigateToSelfiePage(
////        string? workmanIdParam = null,
////        int? workerIdParam = null)
////    {
////        WorkmanId = workmanIdParam;
////        WorkerId = workerIdParam;

////        await NavigateToSelfiePage();
////    }


////    private async Task NavigateToSelfiePage()
////    {
////        if (IsBusy)
////            return;

////        IsBusy = true;

////        try
////        {
////            await GetCurrentLocation();

////            if (string.IsNullOrWhiteSpace(WorkmanId) || WorkerId == null)
////            {
////                var userDetails = await _secureStorage.GetUserDetailsAsync();

////                if (userDetails == null)
////                {
////                    await ShowErrorAndNavigateToLogin(
////                        "User details not found. Please login again.");
////                    return;
////                }

////                WorkmanId = userDetails.WorkmanId;
////                WorkerId = userDetails.WorkerId;
////            }

////            var userDetailsForRadius =
////     await _secureStorage.GetUserDetailsAsync();

////            bool isRadiusCheckRequired =
////                UserRole != "Field Officer" &&
////                UserRole != "Supervisor";

////            if (isRadiusCheckRequired &&
////                userDetailsForRadius != null &&
////                userDetailsForRadius.AttendanceGeoRadius > 0 &&
////                LiveDistance > userDetailsForRadius.AttendanceGeoRadius)
////            {
////                await ShowErrorAndResetUI(
////                    "You are too far from the site to mark attendance.");
////                return;
////            }

////            var token = await _secureStorage.GetAuthTokenAsync();

////            if (string.IsNullOrWhiteSpace(token))
////            {
////                await ShowErrorAndNavigateToLogin(
////                    "Authentication token not found. Please login again.");
////                return;
////            }

////            using var request = new HttpRequestMessage(
////                HttpMethod.Get,
////                $"{ApiUrls.GetAttendanceValidateEndpoint}{WorkmanId}");

////            request.Headers.Authorization =
////                new AuthenticationHeaderValue("Bearer", token);

////            using var response = await _httpClient.SendAsync(request);

////            var responseBody =
////                await response.Content.ReadAsStringAsync();

////            System.Diagnostics.Debug.WriteLine(
////                $"Validate API HTTP Status: {(int)response.StatusCode}");

////            System.Diagnostics.Debug.WriteLine(
////                $"Validate API Response: {responseBody}");

////            if (!response.IsSuccessStatusCode)
////            {
////                await ShowErrorAndResetUI(
////                    $"Attendance validation failed: {(int)response.StatusCode}\n{responseBody}");
////                return;
////            }

////            var result = System.Text.Json.JsonSerializer.Deserialize<
////                ApiResponse<ValidateAttendanceResponse>>(
////                    responseBody,
////                    new System.Text.Json.JsonSerializerOptions
////                    {
////                        PropertyNameCaseInsensitive = true
////                    });

////            if (result?.Data == null)
////            {
////                await ShowErrorAndResetUI(
////                    "Invalid response received from attendance validation API.");
////                return;
////            }

////            if (!result.Data.IsAttendanceAllowed)
////            {
////                await ShowErrorAndResetUI(
////                    result.Data.Remark ??
////                    "Your shift attendance time is over. Please try later.");
////                return;
////            }

////            string? attendanceType = result.Data.GetInOutType();

////            System.Diagnostics.Debug.WriteLine(
////                $"Resolved InOutType: {attendanceType}");

////            if (string.IsNullOrWhiteSpace(attendanceType))
////            {
////                await ShowErrorAndResetUI(
////                    "Attendance type was not returned by the validation API. Please check the API response.");
////                return;
////            }

////            if (result.Data.SiteId == null || result.Data.SiteId <= 0)
////            {
////                await ShowErrorAndResetUI(
////                    "Site ID was not returned by the validation API.");
////                return;
////            }

////            if (result.Data.SiteShiftId == null ||
////                result.Data.SiteShiftId <= 0)
////            {
////                await ShowErrorAndResetUI(
////                    "Site Shift ID was not returned by the validation API.");
////                return;
////            }

////            var navigationParameter = new Dictionary<string, object>
////        {
////            { "WorkerId", WorkerId.Value },
////            { "InOutType", attendanceType },
////            { "Status", result.Data.NewStatus ?? "Present" },
////            { "SiteId", result.Data.SiteId.Value },
////            { "SiteShiftId", result.Data.SiteShiftId.Value },
////            { "GpsLocation", result.Data.GpsLocation ?? string.Empty }
////        };

////            await Shell.Current.GoToAsync("//DashboardPage");

////            await Shell.Current.GoToAsync(
////                nameof(SelfiePage),
////                true,
////                navigationParameter);
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine(
////                $"NavigateToSelfiePage Error: {ex}");

////            await ShowErrorAndResetUI(
////                "Something went wrong while opening selfie attendance. Please try again.");
////        }
////        finally
////        {
////            IsBusy = false;
////        }
////    }


////    private async Task ShowErrorAndResetUI(string message)
////    {
////        await MainThread.InvokeOnMainThreadAsync(async () =>
////        {
////            LiveDistanceBackground = "#FF6B6B";
////            ShowCaptureButton = false;

////            await Shell.Current.DisplayAlertAsync(
////                "Error",
////                message,
////                "OK");
////        });
////    }

////    private async Task ShowErrorAndNavigateToLogin(string message)
////    {
////        await ShowErrorAndResetUI(message);
////        await Shell.Current.GoToAsync("//LoginPage", true);
////    }

////    public async Task LoadLastAttendanceAsync()
////    {
////        try
////        {
////            var users = await _storageService.GetUserAsync();

////            if (users == null)
////                return;

////            var response = await _attendanceService.LastAttendanceAsync(
////                users.WorkerId);

////            var lastAttendance = response?.Data?.FirstOrDefault();

////            if (response?.Success != true || lastAttendance == null)
////            {
////                await MainThread.InvokeOnMainThreadAsync(() =>
////                {
////                    ShowClock = false;
////                });

////                return;
////            }

////            await MainThread.InvokeOnMainThreadAsync(() =>
////            {
////                LastAttendance = lastAttendance;

////                CurrentDate = lastAttendance.CreatedAt != default
////                    ? lastAttendance.CreatedAt.ToString("MMM dd, yyyy")
////                    : "--";

////                InTime = lastAttendance.InTime?.ToString("hh:mm tt") ?? "--:--";
////                OutTime = lastAttendance.OutTime?.ToString("hh:mm tt") ?? "--:--";
////                WorkingHours = ConvertMinutesToHHMM(lastAttendance.WorkingHrs);

////                ShowClock = true;
////            });
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine(
////                $"LoadLastAttendance Error: {ex}");
////        }
////    }

////    private string ConvertMinutesToHHMM(int? minutes)
////    {
////        if (minutes == null || minutes <= 0)
////            return "00:00";

////        var hours = minutes.Value / 60;
////        var remainingMinutes = minutes.Value % 60;

////        return $"{hours:D2}:{remainingMinutes:D2}";
////    }

////    public async Task LoadDataAsync()
////    {
////        Latitude = 0;
////        Longitude = 0;
////        LiveDistance = 0;
////        LiveDistanceBackground = "#FF6B6B";
////        CurrentAddress = string.Empty;
////        SiteAddress = string.Empty;
////        ShowCaptureButton = false;

////        await RefreshData();
////    }

////    [RelayCommand]
////    private async Task MarkAttendance()
////    {
////        if (string.IsNullOrWhiteSpace(SelectedSite) ||
////            string.IsNullOrWhiteSpace(SiteGeoLocation))
////        {
////            await Shell.Current.DisplayAlertAsync(
////                "Error",
////                "Please select a site before marking attendance.",
////                "OK");

////            return;
////        }

////        await Shell.Current.DisplayAlertAsync(
////            SelectedSite,
////            SiteGeoLocation,
////            "OK");
////    }

////    [RelayCommand]
////    public async Task GetCurrentLocation()
////    {
////        if (!await _locationSemaphore.WaitAsync(0))
////            return;

////        try
////        {
////            var permission = await Permissions.CheckStatusAsync<
////                Permissions.LocationWhenInUse>();

////            if (permission != PermissionStatus.Granted)
////            {
////                permission = await Permissions.RequestAsync<
////                    Permissions.LocationWhenInUse>();
////            }

////            if (permission != PermissionStatus.Granted)
////            {
////                GeoLocationStatus = "Permission Denied!";
////                ShowCaptureButton = false;
////                return;
////            }

////            var request = new GeolocationRequest(
////                GeolocationAccuracy.High,
////                TimeSpan.FromSeconds(7));

////            var location = await Geolocation.Default.GetLocationAsync(request);

////            if (location == null)
////            {
////                GeoLocationStatus = "Location not available.";
////                ShowCaptureButton = false;
////                return;
////            }

////            Latitude = location.Latitude;
////            Longitude = location.Longitude;

////            GeoLocationStatus = $"{Latitude}, {Longitude}";

////            //await GetAddressFromCoordinates(
////            //    Latitude,
////            //    Longitude,
////            //    "current");

////            //CalculateAndUpdateDistance();
////            await GetAddressFromCoordinates(
////    Latitude,
////    Longitude,
////    "current");

////            await CalculateAndUpdateDistanceAsync();
////        }
////        catch (FeatureNotSupportedException)
////        {
////            GeoLocationStatus = "Geolocation is not supported on this device.";
////            ShowCaptureButton = false;
////        }
////        catch (PermissionException)
////        {
////            GeoLocationStatus = "Location permission was denied.";
////            ShowCaptureButton = false;
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine(
////                $"GetCurrentLocation Error: {ex}");

////            GeoLocationStatus = "Location unavailable.";
////            ShowCaptureButton = false;
////        }
////        finally
////        {
////            _locationSemaphore.Release();
////        }
////    }


////    private async Task CalculateAndUpdateDistanceAsync()
////    {
////        if (UserRole == "Field Officer")
////        {
////            LiveDistanceBackground = "#E0F7FF";
////            LiveDistance = 0;
////            ShowCaptureButton = true;
////            return;
////        }

////        if (string.IsNullOrWhiteSpace(SiteGeoLocation))
////        {
////            ShowCaptureButton = false;
////            return;
////        }

////        var coordinates = SiteGeoLocation
////            .Replace(" ", string.Empty)
////            .Split(',');

////        if (coordinates.Length != 2 ||
////            !double.TryParse(
////                coordinates[0],
////                NumberStyles.Float,
////                CultureInfo.InvariantCulture,
////                out var siteLatitude) ||
////            !double.TryParse(
////                coordinates[1],
////                NumberStyles.Float,
////                CultureInfo.InvariantCulture,
////                out var siteLongitude))
////        {
////            ShowCaptureButton = false;
////            return;
////        }

////        LiveDistance = Math.Round(
////            CalculateDistance(
////                Latitude,
////                Longitude,
////                siteLatitude,
////                siteLongitude),
////            2);

////        await UpdateDistanceStatusAsync();
////    }



////    private async Task UpdateDistanceStatusAsync()
////    {
////        try
////        {
////            if (UserRole == "Field Officer")
////            {
////                LiveDistanceBackground = "#E0F7FF";
////                ShowCaptureButton = true;
////                return;
////            }

////            var userDetails = await _secureStorage.GetUserDetailsAsync();

////            var allowedRadius = userDetails?.AttendanceGeoRadius > 0
////                ? userDetails.AttendanceGeoRadius
////                : MAX_ALLOWED_DISTANCE;

////            LiveDistanceBackground =
////                LiveDistance > allowedRadius ? "#FF6B6B" : "#E0F7FF";

////            ShowCaptureButton =
////                LiveDistance <= allowedRadius && LiveDistance > 0;
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine(
////                $"UpdateDistanceStatus Error: {ex}");

////            ShowCaptureButton = false;
////        }
////    }


////    private async Task GetAddressFromCoordinates(
////        double latitude,
////        double longitude,
////        string addressFor)
////    {
////        try
////        {
////            var placemark = (await Geocoding.Default.GetPlacemarksAsync(
////                latitude,
////                longitude))?.FirstOrDefault();

////            var address = placemark != null
////                ? string.Join(", ", new[]
////                {
////                    placemark.FeatureName,
////                    placemark.Thoroughfare,
////                    placemark.SubThoroughfare,
////                    placemark.Locality,
////                    placemark.AdminArea,
////                    placemark.CountryName,
////                    placemark.PostalCode
////                }.Where(value => !string.IsNullOrWhiteSpace(value)))
////                : "No address found.";

////            if (addressFor == "site")
////                SiteAddress = address;
////            else if (addressFor == "current")
////                CurrentAddress = address;
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine(
////                $"GetAddressFromCoordinates Error: {ex.Message}");

////            if (addressFor == "site")
////                SiteAddress = "Address unavailable.";
////            else if (addressFor == "current")
////                CurrentAddress = "Address unavailable.";
////        }
////    }

////    private static double CalculateDistance(
////        double lat1,
////        double lon1,
////        double lat2,
////        double lon2)
////    {
////        const double earthRadius = 6371000;

////        var latitude1 = lat1 * Math.PI / 180;
////        var latitude2 = lat2 * Math.PI / 180;
////        var deltaLatitude = (lat2 - lat1) * Math.PI / 180;
////        var deltaLongitude = (lon2 - lon1) * Math.PI / 180;

////        var a =
////            Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
////            Math.Cos(latitude1) * Math.Cos(latitude2) *
////            Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);

////        var c = 2 * Math.Atan2(
////            Math.Sqrt(a),
////            Math.Sqrt(1 - a));

////        return earthRadius * c;
////    }

////    [RelayCommand]
////    private async Task LoadSites()
////    {
////        try
////        {
////            var token = await _storageService.GetTokenAsync();

////            if (!string.IsNullOrWhiteSpace(token))
////            {
////                _httpClient.DefaultRequestHeaders.Authorization =
////                    new AuthenticationHeaderValue("Bearer", token);
////            }

////            var users = await _storageService.GetUserAsync();

////            if (users == null)
////                return;

////            var workmanId = users.WorkmanId ?? string.Empty;

////            ShowWorkerListButton = false;

////            if (workmanId.StartsWith("FO", StringComparison.OrdinalIgnoreCase))
////            {
////                UserRole = "Field Officer";
////                IsSiteListVisible = true;
////            }
////            else if (workmanId.StartsWith("MD", StringComparison.OrdinalIgnoreCase))
////            {
////                UserRole = "Management";
////                IsSiteListVisible = false;
////            }
////            else if (workmanId.StartsWith("SUP", StringComparison.OrdinalIgnoreCase))
////            {
////                UserRole = "Supervisor";
////                IsSiteListVisible = false;
////                ShowWorkerListButton = true;
////            }
////            else
////            {
////                UserRole = "Worker";
////                IsSiteListVisible = false;
////            }

////            if (UserRole is "Worker" or "Supervisor")
////            {
////                SelectedSiteId = users.SiteId;
////                SelectedSite = users.SiteName ?? string.Empty;
////                SiteGeoLocation = users.SiteGeoLocation ?? string.Empty;
////            }
////            else if (UserRole == "Field Officer")
////            {
////                SelectedSiteId = _sessionService.Get<int>("SiteId");
////                SelectedSite = _sessionService.Get<string>("SiteName") ?? string.Empty;
////                SiteGeoLocation = _sessionService.Get<string>("GpsLocation") ?? string.Empty;
////            }
////            else
////            {
////                SelectedSiteId = 0;
////                SelectedSite = "Site not assigned!";
////                SiteGeoLocation = string.Empty;
////            }

////            if (!string.IsNullOrWhiteSpace(SiteGeoLocation))
////            {
////                var coordinates = SiteGeoLocation
////                    .Replace(" ", string.Empty)
////                    .Split(',');

////                if (coordinates.Length == 2 &&
////                    double.TryParse(
////                        coordinates[0],
////                        NumberStyles.Float,
////                        CultureInfo.InvariantCulture,
////                        out var siteLatitude) &&
////                    double.TryParse(
////                        coordinates[1],
////                        NumberStyles.Float,
////                        CultureInfo.InvariantCulture,
////                        out var siteLongitude))
////                {
////                    await GetAddressFromCoordinates(
////                        siteLatitude,
////                        siteLongitude,
////                        "site");
////                }
////                else
////                {
////                    SiteAddress = "Invalid site location.";
////                }
////            }
////            else
////            {
////                SiteAddress = "Site location not available.";
////            }

////            OnPropertyChanged(nameof(SelectedSite));
////            OnPropertyChanged(nameof(IsSiteListVisible));
////        }
////        catch (Exception ex)
////        {
////            System.Diagnostics.Debug.WriteLine($"LoadSites Error: {ex}");
////        }
////    }

////    partial void OnSearchTextChanged(string value)
////    {
////        var searchValue = value ?? string.Empty;

////        FilteredSiteLocations.Clear();

////        foreach (var site in SiteLocations.Where(site =>
////                     (site.SiteName ?? string.Empty).Contains(
////                         searchValue,
////                         StringComparison.OrdinalIgnoreCase)))
////        {
////            FilteredSiteLocations.Add(site);
////        }
////    }
////}



//using System.Collections.ObjectModel;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using tutorial_demo_2.Services;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Pages;
//using System.Windows.Input;
//using Microsoft.Maui.Devices.Sensors;


//namespace tutorial_demo_2.ViewModels
//{
//    public partial class AttendanceViewModel : ObservableObject
//    {

//        private readonly IAppStorageService _storageService;
//        private readonly HttpClient _httpClient;
//        private readonly IAppSecureStorage _secureStorage;
//        private readonly IAttendanceService _attendanceService;
//        private readonly SessionService _sessionService;

//        private const int MAX_ALLOWED_DISTANCE = 1000;

//        // ✅ Attendance Data
//        [ObservableProperty] private AttendanceRecord? _lastAttendance;
//        [ObservableProperty] private string _currentDate = DateTime.Now.ToString("MMM dd, yyyy");
//        [ObservableProperty] private string _inTime = "--:--";
//        [ObservableProperty] private string _outTime = "--:--";
//        [ObservableProperty] private string _workingHours = "00:00";
//        [ObservableProperty] private bool _showClock = false;

//        // ✅ Location Data
//        [ObservableProperty] private double _latitude;
//        [ObservableProperty] private double _longitude;
//        [ObservableProperty] private string _geoLocationStatus;
//        [ObservableProperty] private string _currentAddress;
//        [ObservableProperty] private string _siteAddress;
//        [ObservableProperty] private double _liveDistance;
//        [ObservableProperty] private string _liveDistanceBackground;

//        // ✅ Site Selection Data
//        [ObservableProperty] private ObservableCollection<SiteSelectedModel> _siteLocations = new();
//        [ObservableProperty] private ObservableCollection<SiteSelectedModel> _filteredSiteLocations = new();
//        [ObservableProperty] private int _selectedSiteId;
//        [ObservableProperty] private string _selectedSite;
//        [ObservableProperty] private string _siteGeoLocation;
//        [ObservableProperty] private string _searchText;
//        [ObservableProperty] private bool _isSitePickerOpen;
//        [ObservableProperty] private bool _isSiteListVisible;

//        // ✅ User Data
//        [ObservableProperty] private string _userRole;
//        [ObservableProperty] private int? _workerId;
//        [ObservableProperty] private string? _workmanId;

//        // ✅ UI State
//        [ObservableProperty] private bool _showCaptureButton = false;
//        [ObservableProperty] private bool _showWorkerListButton = false;
//        [ObservableProperty] private bool _isBusy;

//        // ✅ Commands
//        public ICommand NavigateToSelfiePageCommand { get; }
//        public ICommand NavigateToWorkerListCommand { get; }
//        public ICommand NavigateToAttendanceHistoryCommand { get; }

//        public AttendanceViewModel(
//            IAppStorageService storageService,
//            IAppSecureStorage secureStorage,
//            HttpClient httpClient,
//            IAttendanceService attendanceService,
//            SessionService sessionService)
//        {
//            _storageService = storageService;
//            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
//            _httpClient = httpClient;
//            _attendanceService = attendanceService;
//            _sessionService = sessionService;

//            NavigateToSelfiePageCommand = new AsyncRelayCommand(NavigateToSelfiePage);
//            NavigateToWorkerListCommand = new AsyncRelayCommand(NavigateToWorkerList);
//            NavigateToAttendanceHistoryCommand = new AsyncRelayCommand(NavigateToAttendanceHistory);

//            // Initialize ViewModel
//            Task.Run(InitializeAsync);

//        }

//        private async Task InitializeAsync()
//        {
//            IsBusy = true;
//            try
//            {
//                await LoadSites();
//                await GetCurrentLocation();
//                await LoadLastAttendanceAsync();
//            }
//            finally
//            {
//                IsBusy = false;
//            }
//        }

//        public async Task RefreshData()
//        {
//            await LoadSites();
//            await GetCurrentLocation();
//            await LoadLastAttendanceAsync();
//        }

//        private async Task NavigateToAttendanceHistory() => await Shell.Current.GoToAsync(nameof(AttendanceHistoryPage), true);

//        private async Task NavigateToWorkerList()
//        {
//            var userDetails = await _secureStorage.GetUserDetailsAsync();

//            if (userDetails == null)
//            {
//                await MainThread.InvokeOnMainThreadAsync(async () =>
//                {
//                    LiveDistanceBackground = "#FF6B6B"; // Default color
//                    ShowCaptureButton = false;

//                    await Shell.Current.DisplayAlertAsync("Error", "User details not found. Please login again.", "OK");
//                    await Shell.Current.GoToAsync("//LoginPage", true);
//                });

//                return;
//            }

//            var navigationParameter = new Dictionary<string, object>
//    {
//        { "SiteId", userDetails.SiteId }
//    };

//            await Shell.Current.GoToAsync(nameof(WorkerListPage), true, navigationParameter);
//        }

//        internal async Task ExternalNavigateToSelfiePage(string? workmanIdParam = null, int? workerIdParam = null)
//        {
//            WorkmanId = workmanIdParam;
//            WorkerId = workerIdParam;
//            await NavigateToSelfiePage();
//        }

//        private async Task NavigateToSelfiePage()
//        {
//            if (IsBusy) return; // Prevent multiple executions
//            IsBusy = true;

//            try
//            {
//                // Refresh location before navigation
//                await GetCurrentLocation();

//                // Check if the user is too far from the site
//                //if (LiveDistance > MAX_ALLOWED_DISTANCE)
//                //{
//                //	await ShowErrorAndResetUI("You are too far from the site to mark attendance.");
//                //	return;
//                //}


//                // Load user details if not already available
//                if (string.IsNullOrEmpty(WorkmanId) || WorkerId == null)
//                {
//                    var userDetails = await _secureStorage.GetUserDetailsAsync();
//                    if (userDetails == null)
//                    {
//                        await ShowErrorAndNavigateToLogin("User details not found. Please login again.");
//                        return;
//                    }


//                    WorkmanId = userDetails.WorkmanId;
//                    WorkerId = userDetails.WorkerId;
//                }



//                bool isRadiusCheckRequired =
//                    UserRole != "Field Officer" &&
//                    UserRole != "Supervisor";

//                if (isRadiusCheckRequired)
//                {
//                    var radiusCheckDetails = await _secureStorage.GetUserDetailsAsync();
//                    var allowedRadius = radiusCheckDetails?.AttendanceGeoRadius > 0
//                        ? radiusCheckDetails.AttendanceGeoRadius
//                        : MAX_ALLOWED_DISTANCE;

//                    if (LiveDistance > allowedRadius)
//                    {
//                        await ShowErrorAndResetUI(
//                            $"You are outside the allowed attendance radius of {allowedRadius:0} meters.");
//                        return;
//                    }
//                }


//                // Validate authentication token
//                var token = await _secureStorage.GetAuthTokenAsync();
//                if (string.IsNullOrEmpty(token))
//                {
//                    await ShowErrorAndNavigateToLogin("Authentication token not found. Please login again.");
//                    return;
//                }

//                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                // Call API to validate attendance
//                var endpoint = $"{ApiUrls.GetAttendanceValidateEndpoint}{WorkmanId}";
//                var response = await _httpClient.GetAsync(endpoint);

//                if (!response.IsSuccessStatusCode)
//                {
//                    var errorContent = await response.Content.ReadAsStringAsync();
//                    throw new HttpRequestException($"API Error: {errorContent}");
//                }

//                var result = await response.Content.ReadFromJsonAsync<ApiResponse<ValidateAttendanceResponse>>();

//                if (result?.Data?.IsAttendanceAllowed == true)
//                {
//                    var navigationParameter = new Dictionary<string, object>
//{
//    { "WorkerId", WorkerId.Value },
//    { "InOutType", attendanceType },
//    { "Status", result.Data.NewStatus ?? "Present" },
//    { "SiteId", result.Data.SiteId.Value },
//    { "SiteShiftId", result.Data.SiteShiftId.Value },
//    { "GpsLocation", result.Data.GpsLocation ?? string.Empty }
//};

//                    await Shell.Current.GoToAsync("//DashboardPage");

//                    await Shell.Current.GoToAsync(
//                        nameof(SelfiePage),
//                        true,
//                        navigationParameter);
//                }
//                else
//                {
//                    var message = result?.Data?.Remark ?? "Your shift attendance time is over! Please try later.";
//                    await ShowErrorAndResetUI(message);
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"Error in NavigateToSelfiePage: {ex.Message}");
//                await ShowErrorAndResetUI("Something went wrong. Please try again.");
//            }
//            finally
//            {
//                IsBusy = false;
//            }
//        }

//        private async Task ShowErrorAndResetUI(string message)
//        {
//            await MainThread.InvokeOnMainThreadAsync(async () =>
//            {
//                LiveDistanceBackground = "#FF6B6B"; // Default color
//                ShowCaptureButton = false;
//                await Shell.Current.DisplayAlertAsync("Error", message, "OK");
//            });
//        }

//        private async Task ShowErrorAndNavigateToLogin(string message)
//        {
//            await ShowErrorAndResetUI(message);
//            await Shell.Current.GoToAsync("//LoginPage", true);
//        }



//        public async Task LoadLastAttendanceAsync()
//        {
//            IsBusy = true; // Indicate loading state
//            try
//            {
//                var users = await _storageService.GetUserAsync();
//                var workerId = users.WorkerId;

//                var response = await _attendanceService.LastAttendanceAsync(workerId);
//                var lastAttendance = response?.Data?.FirstOrDefault();

//                if (response?.Success == true && lastAttendance != null)
//                {
//                    // Update UI-bound properties safely
//                    CurrentDate = lastAttendance.CreatedAt != default
//                        ? lastAttendance.CreatedAt.ToString("MMM dd, yyyy")
//                        : "--";

//                    InTime = lastAttendance.InTime?.ToString("hh:mm tt") ?? "--:--";
//                    OutTime = lastAttendance.OutTime?.ToString("hh:mm tt") ?? "--:--";
//                    WorkingHours = ConvertMinutesToHHMM(lastAttendance.WorkingHrs);

//                    ShowClock = true;
//                }
//            }
//            catch (Exception ex)
//            {
//                await Shell.Current.DisplayAlertAsync("Error", $"Something went wrong: {ex.Message}", "OK");
//            }
//            finally
//            {
//                IsBusy = false; // Ensure loading state is reset
//            }
//        }

//        private string ConvertMinutesToHHMM(int? minutes)
//        {
//            if (minutes == null || minutes <= 0) return "00:00";

//            int hours = minutes.Value / 60;
//            int remainingMinutes = minutes.Value % 60;
//            return $"{hours:D2}:{remainingMinutes:D2}";
//        }

//        public async Task LoadDataAsync()
//        {
//            // Reset values on page load
//            Latitude = Longitude = LiveDistance = 0;
//            LiveDistanceBackground = "#FF6B6B"; // Default color
//            CurrentAddress = SiteGeoLocation = string.Empty;
//            ShowCaptureButton = false;

//            try
//            {
//                // Run both tasks in parallel to improve performance
//                await Task.WhenAll(LoadSites(), GetCurrentLocation());
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error in LoadDataAsync: {ex.Message}");
//            }
//        }

//        [RelayCommand]
//        private async Task MarkAttendance()
//        {
//            if (string.IsNullOrWhiteSpace(SelectedSite) || string.IsNullOrWhiteSpace(SiteGeoLocation))
//            {
//                await MainThread.InvokeOnMainThreadAsync(() =>
//                    Shell.Current.DisplayAlertAsync("Error", "Please select a site before marking attendance.", "OK"));
//                return;
//            }

//            await MainThread.InvokeOnMainThreadAsync(() =>
//                Shell.Current.DisplayAlertAsync(SelectedSite, SiteGeoLocation, "OK"));
//        }


//        [RelayCommand]
//        public async Task GetCurrentLocation()
//        {
//            try
//            {
//                // Request permission only if not granted
//                if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted &&
//                    await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
//                {
//                    GeoLocationStatus = "Permission Denied!";
//                    return;
//                }

//                // Get location with highest accuracy
//                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

//                if (location == null)
//                {
//                    GeoLocationStatus = "Location not available.";
//                    return;
//                }

//                // Assign latitude & longitude
//                (Latitude, Longitude) = (location.Latitude, location.Longitude);
//                GeoLocationStatus = $"{Latitude}, {Longitude}";

//                // Reverse Geocoding
//                await GetAddressFromCoordinates(Latitude, Longitude, "current");

//                if (IsRadiusCheckRequired())
//                {
//                    if (TryGetSiteCoordinates(out double siteLatitude, out double siteLongitude))
//                    {
//                        LiveDistance = Math.Round(
//                            CalculateDistance(
//                                Latitude,
//                                Longitude,
//                                siteLatitude,
//                                siteLongitude),
//                            2);

//                        var allowedRadius = await GetAllowedRadiusAsync();

//                        LiveDistanceBackground =
//                            LiveDistance > allowedRadius ? "#FF6B6B" : "#E0F7FF";

//                        ShowCaptureButton =
//                            LiveDistance <= allowedRadius && LiveDistance > 0;
//                    }
//                    else
//                    {
//                        LiveDistance = 0;
//                        LiveDistanceBackground = "#FF6B6B";
//                        ShowCaptureButton = false;
//                    }
//                }
//                else
//                {
//                    LiveDistance = 0;
//                    LiveDistanceBackground = "#E0F7FF";
//                    ShowCaptureButton = true;
//                }
//            }
//            catch (FeatureNotSupportedException)
//            {
//                GeoLocationStatus = "Geolocation is not supported on this device.";
//            }
//            catch (PermissionException)
//            {
//                GeoLocationStatus = "Location permission was denied.";
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching location: {ex.Message}");
//                GeoLocationStatus = "..."; // Hide error messages from users
//            }
//        }



//        private bool IsRadiusCheckRequired()
//        {
//            return UserRole != "Field Officer" &&
//                   UserRole != "Supervisor";
//        }

//        private async Task<double> GetAllowedRadiusAsync()
//        {
//            var userDetails = await _secureStorage.GetUserDetailsAsync();

//            return userDetails?.AttendanceGeoRadius > 0
//                ? userDetails.AttendanceGeoRadius
//                : MAX_ALLOWED_DISTANCE;
//        }

//        private bool TryGetSiteCoordinates(
//            out double siteLatitude,
//            out double siteLongitude)
//        {
//            siteLatitude = 0;
//            siteLongitude = 0;

//            if (string.IsNullOrWhiteSpace(SiteGeoLocation))
//                return false;

//            var coordinates = SiteGeoLocation
//                .Replace(" ", string.Empty)
//                .Split(',');

//            return coordinates.Length == 2 &&
//                   double.TryParse(
//                       coordinates[0],
//                       System.Globalization.NumberStyles.Float,
//                       System.Globalization.CultureInfo.InvariantCulture,
//                       out siteLatitude) &&
//                   double.TryParse(
//                       coordinates[1],
//                       System.Globalization.NumberStyles.Float,
//                       System.Globalization.CultureInfo.InvariantCulture,
//                       out siteLongitude);
//        }

//        private async Task GetAddressFromCoordinates(double latitude, double longitude, string addressFor)
//        {
//            try
//            {
//                var placemark = (await Geocoding.Default.GetPlacemarksAsync(latitude, longitude))?.FirstOrDefault();

//                string address = placemark != null
//                    ? string.Join(", ", new[]
//                    {
//                placemark.FeatureName,
//                placemark.Thoroughfare,
//                placemark.SubThoroughfare,
//                placemark.Locality,
//                placemark.AdminArea,
//                placemark.CountryName,
//                placemark.PostalCode
//                    }.Where(s => !string.IsNullOrEmpty(s)))
//                    : "No address found.";

//                Console.WriteLine(address);

//                if (addressFor == "site")
//                    SiteAddress = address;
//                else if (addressFor == "current")
//                    CurrentAddress = address;
//            }
//            catch (Exception ex)
//            {
//                string errorMessage = $"Error fetching address: {ex.Message}";
//                Console.WriteLine(errorMessage);

//                if (addressFor == "site")
//                    SiteAddress = errorMessage;
//                else if (addressFor == "current")
//                    CurrentAddress = errorMessage;
//            }
//        }


//        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
//        {
//            const double R = 6371000; // Earth's radius in meters
//            var φ1 = lat1 * Math.PI / 180;
//            var φ2 = lat2 * Math.PI / 180;
//            var Δφ = (lat2 - lat1) * Math.PI / 180;
//            var Δλ = (lon2 - lon1) * Math.PI / 180;

//            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
//                    Math.Cos(φ1) * Math.Cos(φ2) *
//                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

//            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
//            return R * c;
//        }

//        [RelayCommand]
//        private async Task LoadSites()
//        {
//            try
//            {
//                var token = await _storageService.GetTokenAsync();
//                if (!string.IsNullOrEmpty(token))
//                {
//                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//                }

//                var users = await _storageService.GetUserAsync();
//                var workmanId = users.WorkmanId ?? string.Empty; // Ensure non-null value

//                // Determine user role & visibility settings
//                if (workmanId.StartsWith("FO"))
//                {
//                    UserRole = "Field Officer";
//                    IsSiteListVisible = true;
//                }
//                else if (workmanId.StartsWith("MD"))
//                {
//                    UserRole = "Management";
//                    IsSiteListVisible = false;
//                }
//                else if (workmanId.StartsWith("SUP"))
//                {
//                    UserRole = "Supervisor";
//                    IsSiteListVisible = false;
//                    ShowWorkerListButton = true;
//                }
//                else
//                {
//                    UserRole = "Worker";
//                    IsSiteListVisible = false;
//                    ShowWorkerListButton = false;
//                }

//                // Assign site details based on role
//                if (UserRole is "Worker" or "Supervisor")
//                {
//                    SelectedSiteId = users.SiteId;
//                    SelectedSite = users.SiteName;
//                    SiteGeoLocation = users.SiteGeoLocation;
//                }
//                else if (UserRole == "Field Officer")
//                {
//                    SelectedSiteId = _sessionService.Get<int>("SiteId");
//                    SelectedSite = _sessionService.Get<string>("SiteName");
//                    SiteGeoLocation = _sessionService.Get<string>("GpsLocation");
//                }
//                else
//                {
//                    SelectedSiteId = 0;
//                    SelectedSite = "Site not assigned!";
//                    SiteGeoLocation = ",";
//                }

//                // Process Geolocation
//                if (!string.IsNullOrWhiteSpace(SiteGeoLocation))
//                {
//                    var coordinates = SiteGeoLocation.Replace(" ", "").Split(',');
//                    if (coordinates.Length == 2 &&
//                        double.TryParse(coordinates[0], out double latitude) &&
//                        double.TryParse(coordinates[1], out double longitude))
//                    {
//                        await GetAddressFromCoordinates(latitude, longitude, "site");
//                        Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
//                    }
//                    else
//                    {
//                        Console.WriteLine("Invalid location format.");
//                    }
//                }

//                // Notify UI in one batch update
//                OnPropertyChanged(nameof(SelectedSite));
//                OnPropertyChanged(nameof(IsSiteListVisible));
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Error loading sites: {ex.Message}");
//            }
//        }



//        // Automatically updates UI when search text changes
//        partial void OnSearchTextChanged(string value)
//        {
//            FilteredSiteLocations.Clear();
//            foreach (var site in SiteLocations.Where(s => s.SiteName.ToLower().Contains(value?.ToLower() ?? "")))
//            {
//                FilteredSiteLocations.Add(site);
//            }

//        }
//    }
//}






////using System.Collections.ObjectModel;
////using System.Net.Http.Headers;
////using System.Net.Http.Json;
////using CommunityToolkit.Mvvm.ComponentModel;
////using CommunityToolkit.Mvvm.Input;
////using tutorial_demo_2.Services;
////using tutorial_demo_2.Models;
////using tutorial_demo_2.Pages;
////using System.Windows.Input;
////using Microsoft.Maui.Devices.Sensors;


////namespace tutorial_demo_2.ViewModels
////{
////	public partial class AttendanceViewModel : ObservableObject
////	{

////		private readonly IAppStorageService _storageService;
////		private readonly HttpClient _httpClient;
////		private readonly IAppSecureStorage _secureStorage;
////		private readonly IAttendanceService _attendanceService;
////		private readonly SessionService _sessionService;

////		private const int MAX_ALLOWED_DISTANCE = 1000; // Max allowed distance in meters

////		// ✅ Attendance Data
////		[ObservableProperty] private AttendanceRecord? _lastAttendance;
////		[ObservableProperty] private string _currentDate = DateTime.Now.ToString("MMM dd, yyyy");
////		[ObservableProperty] private string _inTime = "--:--";
////		[ObservableProperty] private string _outTime = "--:--";
////		[ObservableProperty] private string _workingHours = "00:00";
////		[ObservableProperty] private bool _showClock = false;

////		// ✅ Location Data
////		[ObservableProperty] private double _latitude;
////		[ObservableProperty] private double _longitude;
////		[ObservableProperty] private string _geoLocationStatus;
////		[ObservableProperty] private string _currentAddress;
////		[ObservableProperty] private string _siteAddress;
////		[ObservableProperty] private double _liveDistance;
////		[ObservableProperty] private string _liveDistanceBackground;

////		// ✅ Site Selection Data
////		[ObservableProperty] private ObservableCollection<SiteSelectedModel> _siteLocations = new();
////		[ObservableProperty] private ObservableCollection<SiteSelectedModel> _filteredSiteLocations = new();
////		[ObservableProperty] private int _selectedSiteId;
////		[ObservableProperty] private string _selectedSite;
////		[ObservableProperty] private string _siteGeoLocation;
////		[ObservableProperty] private string _searchText;
////		[ObservableProperty] private bool _isSitePickerOpen;
////		[ObservableProperty] private bool _isSiteListVisible;

////		// ✅ User Data
////		[ObservableProperty] private string _userRole;
////		[ObservableProperty] private int? _workerId;
////		[ObservableProperty] private string? _workmanId;

////		// ✅ UI State
////		[ObservableProperty] private bool _showCaptureButton = false;
////		[ObservableProperty] private bool _showWorkerListButton = false;
////		[ObservableProperty] private bool _isBusy;

////		// ✅ Commands
////		public ICommand NavigateToSelfiePageCommand { get; }
////		public ICommand NavigateToWorkerListCommand { get; }
////		public ICommand NavigateToAttendanceHistoryCommand { get; }

////		public AttendanceViewModel(
////			IAppStorageService storageService,
////			IAppSecureStorage secureStorage,
////			HttpClient httpClient,
////			IAttendanceService attendanceService,
////			SessionService sessionService)
////		{
////			_storageService = storageService;
////			_secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
////			_httpClient = httpClient;
////			_attendanceService = attendanceService;
////			_sessionService = sessionService;

////			NavigateToSelfiePageCommand = new AsyncRelayCommand(NavigateToSelfiePage);
////			NavigateToWorkerListCommand = new AsyncRelayCommand(NavigateToWorkerList);
////			NavigateToAttendanceHistoryCommand = new AsyncRelayCommand(NavigateToAttendanceHistory);

////			// Initialize ViewModel
////			Task.Run(InitializeAsync);

////		}

////		private async Task InitializeAsync()
////		{
////			IsBusy = true;
////			try
////			{
////				await Task.WhenAll(LoadSites(), GetCurrentLocation());
////			}
////			finally
////			{
////				IsBusy = false;
////			}
////		}

////		public async Task RefreshData()
////		{
////			await Task.WhenAll(LoadSites(), GetCurrentLocation(), LoadLastAttendanceAsync());
////		}

////		private async Task NavigateToAttendanceHistory() => await Shell.Current.GoToAsync(nameof(AttendanceHistoryPage), true);

////		private async Task NavigateToWorkerList()
////		{
////			var userDetails = await _secureStorage.GetUserDetailsAsync();

////			if (userDetails == null)
////			{
////				await MainThread.InvokeOnMainThreadAsync(async () =>
////				{
////					LiveDistanceBackground = "#FF6B6B"; // Default color
////					ShowCaptureButton = false;

////					await Shell.Current.DisplayAlertAsync("Error", "User details not found. Please login again.", "OK");
////					await Shell.Current.GoToAsync("//LoginPage", true);
////				});

////				return;
////			}

////			var navigationParameter = new Dictionary<string, object>
////	{
////		{ "SiteId", userDetails.SiteId }
////	};

////			await Shell.Current.GoToAsync(nameof(WorkerListPage), true, navigationParameter);
////		}

////		internal async Task ExternalNavigateToSelfiePage(string? workmanIdParam = null, int? workerIdParam = null)
////		{
////			WorkmanId = workmanIdParam;
////			WorkerId = workerIdParam;
////			await NavigateToSelfiePage();
////		}

////		private async Task NavigateToSelfiePage()
////		{
////			if (IsBusy) return; // Prevent multiple executions
////			IsBusy = true;

////			try
////			{
////				// Refresh location before navigation
////				await GetCurrentLocation();

////				// Check if the user is too far from the site
////				//if (LiveDistance > MAX_ALLOWED_DISTANCE)
////				//{
////				//	await ShowErrorAndResetUI("You are too far from the site to mark attendance.");
////				//	return;
////				//}


////				// Load user details if not already available
////				if (string.IsNullOrEmpty(WorkmanId) || WorkerId == null)
////				{
////					var userDetails = await _secureStorage.GetUserDetailsAsync();
////					if (userDetails == null)
////					{
////						await ShowErrorAndNavigateToLogin("User details not found. Please login again.");
////						return;
////					}


////					WorkmanId = userDetails.WorkmanId;
////					WorkerId = userDetails.WorkerId;
////				}



////				// Get user details again if we need them for the radius check
////				var radiusCheckDetails = await _secureStorage.GetUserDetailsAsync();
////				if (radiusCheckDetails != null && LiveDistance > radiusCheckDetails.AttendanceGeoRadius)
////				{
////					await ShowErrorAndResetUI("You are too far from the site to mark attendance.");
////					return;
////				}


////				// Validate authentication token
////				var token = await _secureStorage.GetAuthTokenAsync();
////				if (string.IsNullOrEmpty(token))
////				{
////					await ShowErrorAndNavigateToLogin("Authentication token not found. Please login again.");
////					return;
////				}

////				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

////				// Call API to validate attendance
////				var endpoint = $"{ApiUrls.GetAttendanceValidateEndpoint}{WorkmanId}";
////				var response = await _httpClient.GetAsync(endpoint);

////				if (!response.IsSuccessStatusCode)
////				{
////					var errorContent = await response.Content.ReadAsStringAsync();
////					throw new HttpRequestException($"API Error: {errorContent}");
////				}

////				var result = await response.Content.ReadFromJsonAsync<ApiResponse<ValidateAttendanceResponse>>();

////				if (result?.Data?.IsAttendanceAllowed == true)
////				{
////					var navigationParameter = new Dictionary<string, object>
////					{
////						{ "WorkerId", WorkerId ?? 0 }, // ✅ Safe conversion
////						{ "InOutType", result.Data.InOutType },
////						{ "Status", result.Data.NewStatus },
////						{ "SiteId", result.Data.SiteId ?? 0 },
////						{ "SiteShiftId", result.Data.SiteShiftId ?? 0 },
////						{ "GpsLocation", result.Data.GpsLocation ?? string.Empty }
////					};

////					await Shell.Current.GoToAsync("//DashboardPage");
////					await Shell.Current.GoToAsync(nameof(SelfiePage), true, navigationParameter);
////				}
////				else
////				{
////					var message = result?.Data?.Remark ?? "Your shift attendance time is over! Please try later.";
////					await ShowErrorAndResetUI(message);
////				}
////			}
////			catch (Exception ex)
////			{
////				System.Diagnostics.Debug.WriteLine($"Error in NavigateToSelfiePage: {ex.Message}");
////				await ShowErrorAndResetUI("Something went wrong. Please try again.");
////			}
////			finally
////			{
////				IsBusy = false;
////			}
////		}

////		private async Task ShowErrorAndResetUI(string message)
////		{
////			await MainThread.InvokeOnMainThreadAsync(async () =>
////			{
////				LiveDistanceBackground = "#FF6B6B"; // Default color
////				ShowCaptureButton = false;
////				await Shell.Current.DisplayAlertAsync("Error", message, "OK");
////			});
////		}

////		private async Task ShowErrorAndNavigateToLogin(string message)
////		{
////			await ShowErrorAndResetUI(message);
////			await Shell.Current.GoToAsync("//LoginPage", true);
////		}



////		public async Task LoadLastAttendanceAsync()
////		{
////			IsBusy = true; // Indicate loading state
////			try
////			{
////				var users = await _storageService.GetUserAsync();
////				var workerId = users.WorkerId;

////				var response = await _attendanceService.LastAttendanceAsync(workerId);
////				var lastAttendance = response?.Data?.FirstOrDefault();

////				if (response?.Success == true && lastAttendance != null)
////				{
////					// Update UI-bound properties safely
////					CurrentDate = lastAttendance.CreatedAt != default
////						? lastAttendance.CreatedAt.ToString("MMM dd, yyyy")
////						: "--";

////					InTime = lastAttendance.InTime?.ToString("hh:mm tt") ?? "--:--";
////					OutTime = lastAttendance.OutTime?.ToString("hh:mm tt") ?? "--:--";
////					WorkingHours = ConvertMinutesToHHMM(lastAttendance.WorkingHrs);

////					ShowClock = true;
////				}
////			}
////			catch (Exception ex)
////			{
////				await Shell.Current.DisplayAlertAsync("Error", $"Something went wrong: {ex.Message}", "OK");
////			}
////			finally
////			{
////				IsBusy = false; // Ensure loading state is reset
////			}
////		}

////		private string ConvertMinutesToHHMM(int? minutes)
////		{
////			if (minutes == null || minutes <= 0) return "00:00";

////			int hours = minutes.Value / 60;
////			int remainingMinutes = minutes.Value % 60;
////			return $"{hours:D2}:{remainingMinutes:D2}";
////		}

////		public async Task LoadDataAsync()
////		{
////			// Reset values on page load
////			Latitude = Longitude = LiveDistance = 0;
////			LiveDistanceBackground = "#FF6B6B"; // Default color
////			CurrentAddress = SiteGeoLocation = string.Empty;
////			ShowCaptureButton = false;

////			try
////			{
////				// Run both tasks in parallel to improve performance
////				await Task.WhenAll(LoadSites(), GetCurrentLocation());
////			}
////			catch (Exception ex)
////			{
////				Console.WriteLine($"Error in LoadDataAsync: {ex.Message}");
////			}
////		}

////		[RelayCommand]
////		private async Task MarkAttendance()
////		{
////			if (string.IsNullOrWhiteSpace(SelectedSite) || string.IsNullOrWhiteSpace(SiteGeoLocation))
////			{
////				await MainThread.InvokeOnMainThreadAsync(() =>
////					Shell.Current.DisplayAlertAsync("Error", "Please select a site before marking attendance.", "OK"));
////				return;
////			}

////			await MainThread.InvokeOnMainThreadAsync(() =>
////				Shell.Current.DisplayAlertAsync(SelectedSite, SiteGeoLocation, "OK"));
////		}


////		[RelayCommand]
////		public async Task GetCurrentLocation()
////		{
////			try
////			{
////				// Request permission only if not granted
////				if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted &&
////					await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
////				{
////					GeoLocationStatus = "Permission Denied!";
////					return;
////				}

////				// Get location with highest accuracy
////				var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

////				if (location == null)
////				{
////					GeoLocationStatus = "Location not available.";
////					return;
////				}

////				// Assign latitude & longitude
////				(Latitude, Longitude) = (location.Latitude, location.Longitude);
////				GeoLocationStatus = $"{Latitude}, {Longitude}";

////				// Reverse Geocoding
////				await GetAddressFromCoordinates(Latitude, Longitude, "current");

////				// Calculate Distance
////				if (!string.IsNullOrWhiteSpace(SiteGeoLocation))
////				{
////					var coordinates = SiteGeoLocation.Split(',');

////					if (coordinates.Length == 2 &&
////						double.TryParse(coordinates[0].Trim(), out double siteLat) &&
////						double.TryParse(coordinates[1].Trim(), out double siteLon))
////					{
////						LiveDistance = Math.Round(CalculateDistance(Latitude, Longitude, siteLat, siteLon), 2);

////						// Get user details again if we need them for the radius check
////						var radiusCheckDetails = await _secureStorage.GetUserDetailsAsync();
////						var allowedRadius = radiusCheckDetails?.AttendanceGeoRadius > 0
////							? radiusCheckDetails.AttendanceGeoRadius
////							: 1000;

////						LiveDistanceBackground = LiveDistance > allowedRadius ? "#FF6B6B" : "#E0F7FF";
////						ShowCaptureButton = LiveDistance <= allowedRadius && LiveDistance > 0;
////					}
////				}
////			}
////			catch (FeatureNotSupportedException)
////			{
////				GeoLocationStatus = "Geolocation is not supported on this device.";
////			}
////			catch (PermissionException)
////			{
////				GeoLocationStatus = "Location permission was denied.";
////			}
////			catch (Exception ex)
////			{
////				Console.WriteLine($"Error fetching location: {ex.Message}");
////				GeoLocationStatus = "..."; // Hide error messages from users
////			}
////			finally
////			{
////				LoadLastAttendanceAsync(); // Always execute
////			}
////		}


////		private async Task GetAddressFromCoordinates(double latitude, double longitude, string addressFor)
////		{
////			try
////			{
////				var placemark = (await Geocoding.Default.GetPlacemarksAsync(latitude, longitude))?.FirstOrDefault();

////				string address = placemark != null
////					? string.Join(", ", new[]
////					{
////				placemark.FeatureName,
////				placemark.Thoroughfare,
////				placemark.SubThoroughfare,
////				placemark.Locality,
////				placemark.AdminArea,
////				placemark.CountryName,
////				placemark.PostalCode
////					}.Where(s => !string.IsNullOrEmpty(s)))
////					: "No address found.";

////				Console.WriteLine(address);

////				if (addressFor == "site")
////					SiteAddress = address;
////				else if (addressFor == "current")
////					CurrentAddress = address;
////			}
////			catch (Exception ex)
////			{
////				string errorMessage = $"Error fetching address: {ex.Message}";
////				Console.WriteLine(errorMessage);

////				if (addressFor == "site")
////					SiteAddress = errorMessage;
////				else if (addressFor == "current")
////					CurrentAddress = errorMessage;
////			}
////		}


////		private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
////		{
////			const double R = 6371000; // Earth's radius in meters
////			var φ1 = lat1 * Math.PI / 180;
////			var φ2 = lat2 * Math.PI / 180;
////			var Δφ = (lat2 - lat1) * Math.PI / 180;
////			var Δλ = (lon2 - lon1) * Math.PI / 180;

////			var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
////					Math.Cos(φ1) * Math.Cos(φ2) *
////					Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

////			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
////			return R * c;
////		}

////		[RelayCommand]
////		private async Task LoadSites()
////		{
////			try
////			{
////				var token = await _storageService.GetTokenAsync();
////				if (!string.IsNullOrEmpty(token))
////				{
////					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
////				}

////				var users = await _storageService.GetUserAsync();
////				var workmanId = users.WorkmanId ?? string.Empty; // Ensure non-null value

////				// Determine user role & visibility settings
////				if (workmanId.StartsWith("FO"))
////				{
////					UserRole = "Field Officer";
////					IsSiteListVisible = true;
////				}
////				else if (workmanId.StartsWith("MD"))
////				{
////					UserRole = "Management";
////					IsSiteListVisible = false;
////				}
////				else if (workmanId.StartsWith("SUP"))
////				{
////					UserRole = "Supervisor";
////					IsSiteListVisible = false;
////					ShowWorkerListButton = true;
////				}
////				else
////				{
////					UserRole = "Worker";
////					IsSiteListVisible = false;
////					ShowWorkerListButton = false;
////				}

////				// Assign site details based on role
////				if (UserRole is "Worker" or "Supervisor")
////				{
////					SelectedSiteId = users.SiteId;
////					SelectedSite = users.SiteName;
////					SiteGeoLocation = users.SiteGeoLocation;
////				}
////				else if (UserRole == "Field Officer")
////				{
////					SelectedSiteId = _sessionService.Get<int>("SiteId");
////					SelectedSite = _sessionService.Get<string>("SiteName");
////					SiteGeoLocation = _sessionService.Get<string>("GpsLocation");
////				}
////				else
////				{
////					SelectedSiteId = 0;
////					SelectedSite = "Site not assigned!";
////					SiteGeoLocation = ",";
////				}

////				// Process Geolocation
////				if (!string.IsNullOrWhiteSpace(SiteGeoLocation))
////				{
////					var coordinates = SiteGeoLocation.Replace(" ", "").Split(',');
////					if (coordinates.Length == 2 &&
////						double.TryParse(coordinates[0], out double latitude) &&
////						double.TryParse(coordinates[1], out double longitude))
////					{
////						GetAddressFromCoordinates(latitude, longitude, "site");
////						Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");
////					}
////					else
////					{
////						Console.WriteLine("Invalid location format.");
////					}
////				}

////				// Notify UI in one batch update
////				OnPropertyChanged(nameof(SelectedSite));
////				OnPropertyChanged(nameof(IsSiteListVisible));
////			}
////			catch (Exception ex)
////			{
////				Console.WriteLine($"❌ Error loading sites: {ex.Message}");
////			}
////		}



////		// Automatically updates UI when search text changes
////		partial void OnSearchTextChanged(string value)
////		{
////			FilteredSiteLocations.Clear();
////			foreach (var site in SiteLocations.Where(s => s.SiteName.ToLower().Contains(value?.ToLower() ?? "")))
////			{
////				FilteredSiteLocations.Add(site);
////			}

////		}
////	}
////}
