using System.Net.Http.Headers;
using System.Text.Json;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.Pages;

public partial class ChangeProfileImagePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AppSecureStorage _secureStorage;

    private byte[]? _selectedImageBytes;
    private string _selectedFileName = "profile.jpg";
    private bool _isUploading;

    public ChangeProfileImagePage()
    {
        InitializeComponent();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        _secureStorage = new AppSecureStorage();

        UploadButton.IsEnabled = false;
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            SetButtonsEnabled(false);
            SetStatus("Opening camera...", false);

            var permission =
                await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permission != PermissionStatus.Granted)
            {
                permission =
                    await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(
                    "Camera Permission",
                    "Camera permission is required to take a photo.",
                    "OK");

                return;
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync(
                    "Camera Unavailable",
                    "Camera is not supported on this device.",
                    "OK");

                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo == null)
            {
                SetStatus("Photo capture cancelled.", false);
                return;
            }

            await LoadImageAsync(photo);
        }
        catch (Exception ex)
        {
            SetStatus($"Camera Error: {ex.Message}", true);

            System.Diagnostics.Debug.WriteLine(
                $"Camera Capture Error: {ex}");

            await DisplayAlertAsync("Camera Error", ex.Message, "OK");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void OnChoosePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            SetButtonsEnabled(false);
            SetStatus("Opening gallery...", false);

            var photo = await MediaPicker.Default.PickPhotoAsync();

            if (photo == null)
            {
                SetStatus("Photo selection cancelled.", false);
                return;
            }

            await LoadImageAsync(photo);
        }
        catch (Exception ex)
        {
            SetStatus($"Gallery Error: {ex.Message}", true);

            System.Diagnostics.Debug.WriteLine(
                $"Gallery Selection Error: {ex}");

            await DisplayAlertAsync("Gallery Error", ex.Message, "OK");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task LoadImageAsync(FileResult photo)
    {
        try
        {
            await using var stream = await photo.OpenReadAsync();

            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);

            var imageBytes = memoryStream.ToArray();

            if (imageBytes.Length == 0)
                throw new Exception("Selected image is empty.");

            _selectedImageBytes = imageBytes;

            var extension = Path.GetExtension(photo.FileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            _selectedFileName = $"profile{extension.ToLowerInvariant()}";

            ProfileImage.Source = ImageSource.FromStream(
                () => new MemoryStream(imageBytes));

            UploadButton.IsEnabled = true;

            SetStatus("Photo selected successfully.", false);
        }
        catch (Exception ex)
        {
            _selectedImageBytes = null;
            ProfileImage.Source = "logo.png";
            UploadButton.IsEnabled = false;

            SetStatus(ex.Message, true);
        }
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        if (_isUploading)
            return;

        if (_selectedImageBytes == null ||
            _selectedImageBytes.Length == 0)
        {
            await DisplayAlertAsync(
                "Image Required",
                "Please select a photo first.",
                "OK");

            return;
        }

        try
        {
            _isUploading = true;

            SetButtonsEnabled(false);

            UploadButton.IsEnabled = false;
            UploadIndicator.IsVisible = true;
            UploadIndicator.IsRunning = true;

            SetStatus("Uploading profile photo...", false);

            var token = await _secureStorage.GetAuthTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Authentication token not found. Please login again.");

            var userDetails =
                await _secureStorage.GetUserDetailsAsync();

            if (userDetails == null)
                throw new Exception("Worker details not found. Please login again.");

            if (userDetails.WorkerId <= 0)
                throw new Exception("Invalid Worker ID. Please login again.");

            var apiUrl = ApiUrls.PostChangeProfileImageEndpoint;

            System.Diagnostics.Debug.WriteLine(
                $"Profile Image API URL: {apiUrl}");

            System.Diagnostics.Debug.WriteLine(
                $"Worker ID: {userDetails.WorkerId}");

            using var content = new MultipartFormDataContent();

            var fileContent =
                new ByteArrayContent(_selectedImageBytes);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(GetContentType(_selectedFileName));

            content.Add(fileContent, "file", _selectedFileName);

            content.Add(
                new StringContent(userDetails.WorkerId.ToString()),
                "workerId");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                apiUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = content;

            using var response =
                await _httpClient.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Upload HTTP Status: {(int)response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"Upload Response: {responseBody}");

            var apiMessage = GetApiMessage(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"HTTP {(int)response.StatusCode}: {apiMessage}");
            }

            if (ApiReportedFailure(responseBody))
            {
                throw new Exception(apiMessage);
            }

            SetStatus("Profile photo updated successfully.", false);

            await DisplayAlertAsync(
                "Success",
                "Profile photo updated successfully.",
                "OK");

            if (Shell.Current != null)
                await Shell.Current.GoToAsync("..");
            else if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
        }
        catch (TaskCanceledException)
        {
            SetStatus("Request timed out. Please try again.", true);

            await DisplayAlertAsync(
                "Timeout",
                "The upload request timed out. Please check your internet connection.",
                "OK");
        }
        catch (HttpRequestException ex)
        {
            SetStatus("Unable to connect to the server.", true);

            System.Diagnostics.Debug.WriteLine(
                $"Network Error: {ex}");

            await DisplayAlertAsync(
                "Connection Error",
                ex.Message,
                "OK");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, true);

            System.Diagnostics.Debug.WriteLine(
                $"Profile Upload Error: {ex}");

            await DisplayAlertAsync("Upload Failed", ex.Message, "OK");
        }
        finally
        {
            _isUploading = false;

            UploadIndicator.IsRunning = false;
            UploadIndicator.IsVisible = false;

            SetButtonsEnabled(true);

            UploadButton.IsEnabled =
                _selectedImageBytes != null &&
                _selectedImageBytes.Length > 0;
        }
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }

    private static string GetApiMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return "No response received from server.";

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object &&
                errors.TryGetProperty("message", out var errorMessage))
            {
                return errorMessage.GetString() ?? responseBody;
            }

            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? responseBody;
        }
        catch
        {
        }

        return responseBody;
    }

    private static bool ApiReportedFailure(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return false;

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            var root = document.RootElement;

            if (root.TryGetProperty("success", out var success))
            {
                return success.ValueKind == JsonValueKind.False;
            }

            if (root.TryGetProperty("Success", out success))
            {
                return success.ValueKind == JsonValueKind.False;
            }
        }
        catch
        {
        }

        return false;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError
            ? Colors.Red
            : Color.FromArgb("#16834A");
    }

    private void SetButtonsEnabled(bool enabled)
    {
        TakePhotoButton.IsEnabled = enabled;
        ChoosePhotoButton.IsEnabled = enabled;
    }
}

//using System.Net.Http.Headers;
//using tutorial_demo_2.Models;
//using tutorial_demo_2.Services;

//namespace tutorial_demo_2.Pages
//{
//	public partial class ChangeProfileImagePage : ContentPage
//	{
//		private FileResult _selectedPhoto;
//		private readonly HttpClient _httpClient = new HttpClient();
//		private readonly AppSecureStorage _secureStorage = new AppSecureStorage();

//		public ChangeProfileImagePage()
//		{
//			InitializeComponent();
//		}

//		private async void OnTakePhotoClicked(object sender, EventArgs e)
//		{
//			try
//			{
//				var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
//				if (status != PermissionStatus.Granted)
//				{
//					status = await Permissions.RequestAsync<Permissions.Camera>();
//					if (status != PermissionStatus.Granted)
//					{
//						await DisplayAlertAsync("Permission Required", "Camera permission is required to take photos", "OK");
//						return;
//					}
//				}

//				if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
//				{
//					status = await Permissions.CheckStatusAsync<Permissions.Photos>();
//					if (status != PermissionStatus.Granted)
//					{
//						status = await Permissions.RequestAsync<Permissions.Photos>();
//					}
//				}
//				else
//				{
//					status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
//					if (status != PermissionStatus.Granted)
//					{
//						status = await Permissions.RequestAsync<Permissions.StorageRead>();
//					}
//				}

//				if (MediaPicker.Default.IsCaptureSupported)
//				{
//					_selectedPhoto = await MediaPicker.Default.CapturePhotoAsync();

//					if (_selectedPhoto != null)
//					{
//						await LoadImage(_selectedPhoto);
//						UploadButton.IsEnabled = true;
//					}
//				}
//				else
//				{
//					await DisplayAlertAsync("Error", "Camera not supported on this device", "OK");
//				}
//			}
//			catch (Exception ex)
//			{
//				StatusLabel.Text = $"Error: {ex.Message}";
//			}
//		}

//		private async void OnChoosePhotoClicked(object sender, EventArgs e)
//		{
//			try
//			{
//				_selectedPhoto = await MediaPicker.Default.PickPhotoAsync();

//				if (_selectedPhoto != null)
//				{
//					await LoadImage(_selectedPhoto);
//					UploadButton.IsEnabled = true;
//				}
//			}
//			catch (Exception ex)
//			{
//				StatusLabel.Text = $"Error: {ex.Message}";
//			}
//		}

//		private async Task LoadImage(FileResult photo)
//		{
//			var stream = await photo.OpenReadAsync();
//			ProfileImage.Source = ImageSource.FromStream(() => stream);
//		}

//		private async void OnUploadClicked(object sender, EventArgs e)
//		{
//			if (_selectedPhoto == null)
//			{
//				await DisplayAlertAsync("Error", "No image selected", "OK");
//				return;
//			}

//			try
//			{
//				UploadButton.IsEnabled = false;
//				StatusLabel.Text = "Uploading...";

//				// Get auth token from AppSecureStorage
//				var token = await _secureStorage.GetAuthTokenAsync();
//				if (string.IsNullOrEmpty(token))
//				{
//					await DisplayAlertAsync("Error", "Authentication required", "OK");
//					return;
//				}

//				// Prepare multipart form data
//				var content = new MultipartFormDataContent();
//				var fileStream = await _selectedPhoto.OpenReadAsync();
//				var fileContent = new StreamContent(fileStream);
//				fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
//				content.Add(fileContent, "file", _selectedPhoto.FileName);

//				// Add worker ID
//				var userDetails = await _secureStorage.GetUserDetailsAsync();
//				if (userDetails != null )
//				{
//					content.Add(new StringContent(userDetails.WorkerId.ToString()), "workerId");
//				}

//				// Set auth header
//				_httpClient.DefaultRequestHeaders.Authorization =
//					new AuthenticationHeaderValue("Bearer", token);

//				// Upload to API
//				var response = await _httpClient.PostAsync(
//					ApiUrls.PostChangeProfileImageEndpoint,
//					content);

//				if (response.IsSuccessStatusCode)
//				{
//					StatusLabel.Text = "Profile image updated successfully!";
//					await Task.Delay(2000);
//					await Navigation.PopAsync();
//				}
//				else
//				{
//					var error = await response.Content.ReadAsStringAsync();
//					StatusLabel.Text = $"Upload failed: {error}";
//					UploadButton.IsEnabled = true;
//				}
//			}
//			catch (Exception ex)
//			{
//				StatusLabel.Text = $"Error: {ex.Message}";
//				UploadButton.IsEnabled = true;
//			}
//		}
//	}
//}
