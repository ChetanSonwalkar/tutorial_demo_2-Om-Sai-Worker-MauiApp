
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

[QueryProperty(nameof(WorkerId), "WorkerId")]
[QueryProperty(nameof(InOutType), "InOutType")]
[QueryProperty(nameof(Status), "Status")]
[QueryProperty(nameof(SiteId), "SiteId")]
[QueryProperty(nameof(SiteShiftId), "SiteShiftId")]
[QueryProperty(nameof(GpsLocation), "GpsLocation")]
public partial class SelfieViewModel : ObservableObject
{
    private readonly IAppSecureStorage _secureStorage;
    private readonly HttpClient _httpClient;
    private byte[] _capturedImage = Array.Empty<byte>();
    private Location? _currentLocation;
    private int _submitStarted;

    [ObservableProperty]
    private int workerId;

    [ObservableProperty]
    private string inOutType = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private int siteId;

    [ObservableProperty]
    private int siteShiftId;

    [ObservableProperty]
    private string gpsLocation = string.Empty;

    [ObservableProperty]
    private bool isSubmitting;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public byte[] CapturedImage
    {
        get => _capturedImage;
        set
        {
            _capturedImage = value ?? Array.Empty<byte>();
            OnPropertyChanged(nameof(CapturedImage));
            OnPropertyChanged(nameof(CapturedImageSource));
        }
    }

    public ImageSource? CapturedImageSource =>
        _capturedImage.Length > 0
            ? ImageSource.FromStream(() => new MemoryStream(_capturedImage))
            : null;

    public ICommand BackNavigationCommand { get; }

    public SelfieViewModel(
        INavigation navigation,
        IAppSecureStorage secureStorage,
        HttpClient httpClient)
    {
        _secureStorage = secureStorage;
        _httpClient = httpClient;
        BackNavigationCommand = new Command(async () => await BackNavigationAsync());
    }

    public void SetCapturedImage(byte[] imageData)
    {
        CapturedImage = imageData;
    }

    public void SetCurrentLocation(Location? location)
    {
        _currentLocation = location;
    }

    public void ApplyAttendanceData(
        int workerId,
        string? inOutType,
        string? status,
        int siteId,
        int siteShiftId,
        string? gpsLocation)
    {
        WorkerId = workerId;
        InOutType = inOutType?.Trim() ?? string.Empty;
        Status = status?.Trim() ?? string.Empty;
        SiteId = siteId;
        SiteShiftId = siteShiftId;
        GpsLocation = gpsLocation?.Trim() ?? string.Empty;
    }

    //private async Task BackNavigationAsync()
    //{
    //    try
    //    {
    //        await Shell.Current.GoToAsync("..", true);
    //    }
    //    catch (Exception ex)
    //    {
    //        await ShowAlertAsync("Navigation Error", ex.Message);
    //    }
    //}

    private async Task BackNavigationAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..", true);
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Navigation Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SubmitAttendanceAsync()
    {
        if (Interlocked.Exchange(ref _submitStarted, 1) == 1)
            return;

        IsSubmitting = true;
        ErrorMessage = string.Empty;

        try
        {
            var userDetails = await _secureStorage.GetUserDetailsAsync();

            if (WorkerId <= 0)
                WorkerId = userDetails?.WorkerId ?? 0;

            if (WorkerId <= 0)
            {
                await ShowAlertAsync("Login Required", "Worker ID not found. Please login again.");
                await Shell.Current.GoToAsync("//LoginPage", true);
                return;
            }

            if (CapturedImage.Length == 0)
            {
                await ShowAlertAsync("Image Required", "Please capture a selfie first.");
                return;
            }

            if (SiteId <= 0)
            {
                await ShowAlertAsync("Validation Error", "Site ID is missing. Go back and select the site again.");
                return;
            }

            if (SiteShiftId <= 0)
            {
                await ShowAlertAsync("Validation Error", "Shift ID is missing. Go back and load attendance details again.");
                return;
            }

            if (string.IsNullOrWhiteSpace(InOutType))
            {
                ErrorMessage = "InOutType was not received from ValidateAttendance API.";
                await ShowAlertAsync(
                    "Attendance Type Missing",
                    "Attendance type is missing. Go back to the attendance page. The ValidateAttendance API response must contain the attendance type.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Status))
                Status = "Present";

            var token = await _secureStorage.GetAuthTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                await ShowAlertAsync("Authentication Error", "Login token not found. Please login again.");
                await Shell.Current.GoToAsync("//LoginPage", true);
                return;
            }

            if (_currentLocation == null)
            {
                _currentLocation = await GetCurrentLocationAsync();
            }

            if (_currentLocation == null)
            {
                await ShowAlertAsync(
                    "Location Required",
                    "Unable to get your current GPS location. Turn on device location, allow location permission for this app, and try again.");
                return;
            }

            string geoLocation = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                _currentLocation.Latitude,
                _currentLocation.Longitude);

            string endpoint = ApiUrls.PostSelfieAttendanceEndpoint;

            System.Diagnostics.Debug.WriteLine("========== SELFIE ATTENDANCE REQUEST ==========");
            System.Diagnostics.Debug.WriteLine($"Endpoint: {endpoint}");
            System.Diagnostics.Debug.WriteLine($"WorkerId: {WorkerId}");
            System.Diagnostics.Debug.WriteLine($"SiteId: {SiteId}");
            System.Diagnostics.Debug.WriteLine($"ShiftId: {SiteShiftId}");
            System.Diagnostics.Debug.WriteLine($"GeoLocation: {geoLocation}");
            System.Diagnostics.Debug.WriteLine($"InOutType: {InOutType}");
            System.Diagnostics.Debug.WriteLine($"Status: {Status}");
            System.Diagnostics.Debug.WriteLine($"GpsLocation: {GpsLocation}");
            System.Diagnostics.Debug.WriteLine($"Image bytes: {CapturedImage.Length}");

            using var formData = new MultipartFormDataContent();

            AddFormField(formData, "WorkerId", WorkerId);
            AddFormField(formData, "SiteId", SiteId);
            AddFormField(formData, "ShiftId", SiteShiftId);
            AddFormField(formData, "GeoLocation", geoLocation);
            AddFormField(formData, "InOutType", InOutType.Trim());
            AddFormField(formData, "GpsLocation", GpsLocation ?? string.Empty);
            AddFormField(formData, "Status", Status.Trim());

            using var imageContent = new ByteArrayContent(CapturedImage);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            formData.Add(imageContent, "SelfieImage", "selfie.jpg");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = formData
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("========== SELFIE ATTENDANCE RESPONSE ==========");
            System.Diagnostics.Debug.WriteLine($"HTTP: {(int)response.StatusCode} {response.ReasonPhrase}");
            System.Diagnostics.Debug.WriteLine(responseBody);
            System.Diagnostics.Debug.WriteLine("================================================");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = ExtractApiError(responseBody);
                await ShowAlertAsync($"Attendance Failed ({(int)response.StatusCode})", ErrorMessage);
                return;
            }

            bool success = IsApiSuccess(responseBody);

            if (!success)
            {
                ErrorMessage = ExtractApiError(responseBody);
                await ShowAlertAsync("Attendance Not Confirmed", ErrorMessage);
                return;
            }

            await ShowAlertAsync("Success", "Attendance submitted successfully.");
            await Shell.Current.GoToAsync("//DashboardPage", true);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"HTTP ERROR: {ex}");
            await ShowAlertAsync("Network Error", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"TIMEOUT: {ex}");
            await ShowAlertAsync("Request Timeout", "The attendance request timed out. Check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"ATTENDANCE ERROR: {ex}");
            await ShowAlertAsync("Attendance Error", ex.Message);
        }
        finally
        {
            IsSubmitting = false;
            Interlocked.Exchange(ref _submitStarted, 0);
        }
    }

    private async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (permission != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("Location permission was not granted.");
                return null;
            }

            var request = new GeolocationRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(15));

            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location == null)
            {
                location = await Geolocation.Default.GetLastKnownLocationAsync();
            }

            if (location != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Selfie GPS: {location.Latitude}, {location.Longitude}");
            }

            return location;
        }
        catch (FeatureNotSupportedException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GPS not supported: {ex}");
            return null;
        }
        catch (PermissionException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GPS permission error: {ex}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GPS error: {ex}");
            return null;
        }
    }

    private static void AddFormField(
        MultipartFormDataContent content,
        string name,
        object value)
    {
        content.Add(
            new StringContent(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
            name);
    }

    private static bool IsApiSuccess(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return false;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (string.Equals(property.Name, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        if (property.Value.ValueKind == JsonValueKind.True)
                            return true;

                        if (property.Value.ValueKind == JsonValueKind.False)
                            return false;
                    }
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static string ExtractApiError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return "The server returned an empty response.";

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return ReadError(document.RootElement) ?? responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }

    private static string? ReadError(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        if (element.ValueKind == JsonValueKind.Array)
        {
            var messages = element.EnumerateArray()
                .Select(ReadError)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            string result = string.Join(", ", messages);
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        string[] keys = { "message", "error", "title", "detail", "errors", "details" };

        foreach (var property in element.EnumerateObject())
        {
            if (keys.Any(key => string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                string? message = ReadError(property.Value);
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
        }

        return null;
    }

    private static Task ShowAlertAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(
            () => Shell.Current.DisplayAlertAsync(title, message, "OK"));
    }

   
}




//using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Maui.Core.Primitives;
//using System.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using System.Net.Http.Headers;
//using tutorial_demo_2.Services;
//using System.Windows.Input;

//using System.Text.Json;
//using Microsoft.Maui.Controls;


//namespace tutorial_demo_2.ViewModels
//{

//	[QueryProperty(nameof(WorkerId), "WorkerId")]
//	[QueryProperty(nameof(InOutType), "InOutType")]
//	[QueryProperty(nameof(Status), "Status")]
//	[QueryProperty(nameof(SiteId), "SiteId")]
//	[QueryProperty(nameof(SiteShiftId), "SiteShiftId")]
//	[QueryProperty(nameof(GpsLocation), "GpsLocation")]

//	public partial class SelfieViewModel : ObservableObject
//	{
//		private readonly INavigation _navigation;

//		private byte[] _capturedImageData;
//		private Location _currentLocation;
//		private readonly IAppSecureStorage _secureStorage;
//		private readonly HttpClient _httpClient;


//		private byte[] _capturedImage;
//		public byte[] CapturedImage
//		{
//			get => _capturedImage;
//			set
//			{
//				_capturedImage = value;
//				OnPropertyChanged(nameof(CapturedImage));
//				OnPropertyChanged(nameof(CapturedImageSource)); // Update UI
//			}
//		}

//		public ImageSource CapturedImageSource => CapturedImage != null
//			? ImageSource.FromStream(() => new MemoryStream(CapturedImage))
//			: null;

//		public Command UploadCommand { get; }
//		public ICommand BackNavigationCommand { get; }

//		public ICommand NavigateToChangePasswordCommand { get; }



//		[ObservableProperty]
//		private int workerId;


//		[ObservableProperty]
//        private string inOutType;  

//        [ObservableProperty]
//        private string status;     

//        [ObservableProperty]
//        private int siteId;        

//        [ObservableProperty]
//        private int siteShiftId;   

//        [ObservableProperty]
//        private string gpsLocation;


//		public SelfieViewModel(INavigation navigation, IAppSecureStorage secureStorage, HttpClient httpClient)
//		{
//			_navigation = navigation;
//			_secureStorage = secureStorage;
//			_httpClient = httpClient;

//			BackNavigationCommand = new Command(async () => await BackNavigation());

//		}


//		private async Task BackNavigation()
//		{
//			//await Shell.Current.GoToAsync("/AttendancePage", true);
//			await Shell.Current.GoToAsync("..", true); // Navigate back safely

//		}



//		public void SetCapturedImage(byte[] imageData)
//		{
//			_capturedImageData = imageData;
//		}

//		public void SetCurrentLocation(Location location)
//		{
//			_currentLocation = location;
//		}


//		[RelayCommand]
//		private async Task SubmitAttendance()
//		{
//			if (_capturedImage == null)
//			{
//				await Shell.Current.DisplayAlert("Error", "Please capture a photo first", "OK");
//				return;
//			}

//			if (string.IsNullOrEmpty(Status))
//			{
//				Status = "Present";
//			}

//			try
//			{
//				var userDetails = await _secureStorage.GetUserDetailsAsync();
//				var token = await _secureStorage.GetAuthTokenAsync();
//				if(WorkerId == null)
//				{
//					if (userDetails == null)
//					{
//						await Shell.Current.DisplayAlert("Error", "User details not found. Please login again.", "OK");
//						await Shell.Current.GoToAsync("//LoginPage", true);
//						return;
//					}
//					WorkerId = userDetails.WorkerId;
//				}

//				if (!string.IsNullOrEmpty(token))
//				{
//					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//				}

//				var formData = new MultipartFormDataContent();
//				formData.Add(new StringContent(WorkerId.ToString()), "workerId");

//				formData.Add(new StringContent(SiteId.ToString()), "siteId");
//				formData.Add(new StringContent(SiteShiftId.ToString()), "shiftId");
//				formData.Add(new StringContent($"{_currentLocation?.Latitude ?? 0},{_currentLocation?.Longitude ?? 0}"), "geoLocation");
//				formData.Add(new StringContent(InOutType ?? string.Empty), "inOutType");
//				formData.Add(new StringContent(GpsLocation ?? string.Empty), "gpsLocation");
//				formData.Add(new StringContent(Status), "status");

//				var imageContent = new ByteArrayContent(_capturedImage);
//				imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
//				formData.Add(imageContent, "selfieImage", "selfie.jpg");

//				var response = await _httpClient.PostAsync(ApiUrls.PostSelfieAttendanceEndpoint, formData);
//				if (response.IsSuccessStatusCode)
//				{
//					await Shell.Current.DisplayAlert("Success", "Attendance submitted successfully", "OK");
//					//await Shell.Current.GoToAsync(".."); // Navigates back one step

//					// ✅ Navigate to AttendancePage and clear the back stack
//					await Shell.Current.GoToAsync("//DashboardPage");
//					await Shell.Current.GoToAsync("AttendancePage");

//					//	await Shell.Current.GoToAsync($"//{nameof(AttendancePage)}"); // Double slash `//` resets stack

//				}
//				else
//				{
//					//var error = await response.Content.ReadAsStringAsync();
//					//await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance: {error}", "OK");

//						var errorJson = await response.Content.ReadAsStringAsync();

//						// Parse the JSON response
//						using (JsonDocument doc = JsonDocument.Parse(errorJson))
//						{
//							if (doc.RootElement.TryGetProperty("errors", out JsonElement errors))
//							{
//								if (errors.TryGetProperty("message", out JsonElement message))
//								{
//									// Display the error message
//									await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance: {message.GetString()}", "OK");
//								}
//								else
//								{
//									await Shell.Current.DisplayAlert("Error", "Failed to submit attendance: Unknown error", "OK");
//								}
//							}
//							else
//							{
//								await Shell.Current.DisplayAlert("Error", "Failed to submit attendance: Invalid response", "OK");
//							}
//						}



//					}
//			}
//			catch (Exception ex)
//			{
//				await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance!: {ex.Message}", "OK");
//			}
//		}


//	}
//}




//using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Maui.Core.Primitives;
//using System.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using System.Net.Http.Headers;
//using tutorial_demo_2.Services;
//using System.Windows.Input;
//using System.Text.Json;
//using Microsoft.Maui.Controls;


//namespace tutorial_demo_2.ViewModels
//{

//	[QueryProperty(nameof(WorkerId), "WorkerId")]
//	[QueryProperty(nameof(InOutType), "InOutType")]
//	[QueryProperty(nameof(Status), "Status")]
//	[QueryProperty(nameof(SiteId), "SiteId")]
//	[QueryProperty(nameof(SiteShiftId), "SiteShiftId")]
//	[QueryProperty(nameof(GpsLocation), "GpsLocation")]

//	public partial class SelfieViewModel : ObservableObject
//	{
//		private readonly INavigation _navigation;

//		private byte[] _capturedImageData;
//		private Location _currentLocation;
//		private readonly IAppSecureStorage _secureStorage;
//		private readonly HttpClient _httpClient;


//		private byte[] _capturedImage;
//		public byte[] CapturedImage
//		{
//			get => _capturedImage;
//			set
//			{
//				_capturedImage = value;
//				OnPropertyChanged(nameof(CapturedImage));
//				OnPropertyChanged(nameof(CapturedImageSource)); // Update UI
//			}
//		}

//		public ImageSource CapturedImageSource => CapturedImage != null
//			? ImageSource.FromStream(() => new MemoryStream(CapturedImage))
//			: null;

//		public Command UploadCommand { get; }
//		public ICommand BackNavigationCommand { get; }

//		public ICommand NavigateToChangePasswordCommand { get; }



//		[ObservableProperty]
//		private int workerId;


//		[ObservableProperty]
//        private string inOutType;  

//        [ObservableProperty]
//        private string status;     

//        [ObservableProperty]
//        private int siteId;        

//        [ObservableProperty]
//        private int siteShiftId;   

//        [ObservableProperty]
//        private string gpsLocation;


//		public SelfieViewModel(INavigation navigation, IAppSecureStorage secureStorage, HttpClient httpClient)
//		{
//			_navigation = navigation;
//			_secureStorage = secureStorage;
//			_httpClient = httpClient;

//			BackNavigationCommand = new Command(async () => await BackNavigation());

//		}


//		private async Task BackNavigation()
//		{
//			//await Shell.Current.GoToAsync("/AttendancePage", true);
//			await Shell.Current.GoToAsync("..", true); // Navigate back safely

//		}



//		public void SetCapturedImage(byte[] imageData)
//		{
//			_capturedImageData = imageData;
//		}

//		public void SetCurrentLocation(Location location)
//		{
//			_currentLocation = location;
//		}


//		[RelayCommand]
//		private async Task SubmitAttendance()
//		{
//			if (_capturedImage == null)
//			{
//				await Shell.Current.DisplayAlert("Error", "Please capture a photo first", "OK");
//				return;
//			}

//			if (string.IsNullOrEmpty(Status))
//			{
//				Status = "Present";
//			}

//			try
//			{
//				var userDetails = await _secureStorage.GetUserDetailsAsync();
//				var token = await _secureStorage.GetAuthTokenAsync();
//				if(WorkerId == null)
//				{
//					if (userDetails == null)
//					{
//						await Shell.Current.DisplayAlert("Error", "User details not found. Please login again.", "OK");
//						await Shell.Current.GoToAsync("//LoginPage", true);
//						return;
//					}
//					WorkerId = userDetails.WorkerId;
//				}

//				if (!string.IsNullOrEmpty(token))
//				{
//					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//				}

//				var formData = new MultipartFormDataContent();
//				formData.Add(new StringContent(WorkerId.ToString()), "workerId");

//				formData.Add(new StringContent(SiteId.ToString()), "siteId");
//				formData.Add(new StringContent(SiteShiftId.ToString()), "shiftId");
//				formData.Add(new StringContent($"{_currentLocation?.Latitude ?? 0},{_currentLocation?.Longitude ?? 0}"), "geoLocation");
//				formData.Add(new StringContent(InOutType ?? string.Empty), "inOutType");
//				formData.Add(new StringContent(GpsLocation ?? string.Empty), "gpsLocation");
//				formData.Add(new StringContent(Status), "status");

//				var imageContent = new ByteArrayContent(_capturedImage);
//				imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
//				formData.Add(imageContent, "selfieImage", "selfie.jpg");

//				var response = await _httpClient.PostAsync(ApiUrls.PostSelfieAttendanceEndpoint, formData);
//				if (response.IsSuccessStatusCode)
//				{
//					await Shell.Current.DisplayAlert("Success", "Attendance submitted successfully", "OK");
//					//await Shell.Current.GoToAsync(".."); // Navigates back one step

//					// ✅ Navigate to AttendancePage and clear the back stack
//					await Shell.Current.GoToAsync("//DashboardPage");
//					await Shell.Current.GoToAsync("AttendancePage");

//					//	await Shell.Current.GoToAsync($"//{nameof(AttendancePage)}"); // Double slash `//` resets stack

//				}
//				else
//				{
//					//var error = await response.Content.ReadAsStringAsync();
//					//await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance: {error}", "OK");

//						var errorJson = await response.Content.ReadAsStringAsync();

//						// Parse the JSON response
//						using (JsonDocument doc = JsonDocument.Parse(errorJson))
//						{
//							if (doc.RootElement.TryGetProperty("errors", out JsonElement errors))
//							{
//								if (errors.TryGetProperty("message", out JsonElement message))
//								{
//									// Display the error message
//									await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance: {message.GetString()}", "OK");
//								}
//								else
//								{
//									await Shell.Current.DisplayAlert("Error", "Failed to submit attendance: Unknown error", "OK");
//								}
//							}
//							else
//							{
//								await Shell.Current.DisplayAlert("Error", "Failed to submit attendance: Invalid response", "OK");
//							}
//						}



//					}
//			}
//			catch (Exception ex)
//			{
//				await Shell.Current.DisplayAlert("Error", $"Failed to submit attendance!: {ex.Message}", "OK");
//			}
//		}


//	}
//}
