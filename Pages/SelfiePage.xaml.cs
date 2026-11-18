using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using SkiaSharp;
using System.Diagnostics;
using tutorial_demo_2.Services;
using tutorial_demo_2.ViewModels;

namespace tutorial_demo_2.Pages;

public partial class SelfiePage : ContentPage, IQueryAttributable
{
    private readonly ICameraProvider _cameraProvider;
    private readonly IAppSecureStorage _secureStorage;
    private readonly HttpClient _httpClient;
    private readonly SelfieViewModel _viewModel;

    private bool _isNavigatingBack;
    private bool _isFrontCamera = true;
    private bool _isFlashOn;
    private bool _isCameraInitialized;
    private bool _isProcessing;

    private const int MAX_IMAGE_DIMENSION = 300;
    private const int MAX_IMAGE_SIZE_BYTES = 50 * 1024;

    private readonly SemaphoreSlim _cameraLock = new(1, 1);

    public SelfiePage(
        ICameraProvider cameraProvider,
        HttpClient httpClient,
        IAppSecureStorage secureStorage)
    {
        InitializeComponent();

        _cameraProvider = cameraProvider;
        _secureStorage = secureStorage;
        _httpClient = httpClient;

        _viewModel = new SelfieViewModel(
            Navigation,
            secureStorage,
            httpClient);

        BindingContext = _viewModel;

        SubmitButtons.IsVisible = false;
        MyImageBox.IsVisible = false;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelfieViewModel.IsSubmitting))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SubmitAttendanceButton.IsEnabled =
                    !_viewModel.IsSubmitting;
            });
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("WorkerId", out var workerId))
                _viewModel.WorkerId = Convert.ToInt32(workerId);

            if (query.TryGetValue("InOutType", out var inOutType))
                _viewModel.InOutType =
                    Convert.ToString(inOutType) ?? string.Empty;

            if (query.TryGetValue("Status", out var status))
                _viewModel.Status =
                    Convert.ToString(status) ?? string.Empty;

            if (query.TryGetValue("SiteId", out var siteId))
                _viewModel.SiteId = Convert.ToInt32(siteId);

            if (query.TryGetValue("SiteShiftId", out var siteShiftId))
                _viewModel.SiteShiftId = Convert.ToInt32(siteShiftId);

            if (query.TryGetValue("GpsLocation", out var gpsLocation))
                _viewModel.GpsLocation =
                    Convert.ToString(gpsLocation) ?? string.Empty;

            Debug.WriteLine($"WorkerId: {_viewModel.WorkerId}");
            Debug.WriteLine($"InOutType: {_viewModel.InOutType}");
            Debug.WriteLine($"Status: {_viewModel.Status}");
            Debug.WriteLine($"SiteId: {_viewModel.SiteId}");
            Debug.WriteLine($"SiteShiftId: {_viewModel.SiteShiftId}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyQueryAttributes Error: {ex}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeCameraAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            MyCamera?.StopCameraPreview();
            _isCameraInitialized = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Camera stop error: {ex}");
        }
    }

    private async Task InitializeCameraAsync()
    {
        if (_isCameraInitialized)
            return;

        await _cameraLock.WaitAsync();

        try
        {
            if (_isCameraInitialized)
                return;

            CameraLoading.IsVisible = true;
            CameraLoading.IsRunning = true;
            CaptureButton.IsEnabled = false;

            var permissionStatus =
                await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus =
                    await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (permissionStatus != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(
                    "Camera Permission",
                    "Camera permission is required for selfie attendance. Please allow camera access in app settings.",
                    "OK");

                return;
            }

            await _cameraProvider.RefreshAvailableCameras(
                CancellationToken.None);

            var cameras = _cameraProvider.AvailableCameras;

            if (cameras == null || cameras.Count == 0)
            {
                await DisplayAlertAsync(
                    "Camera Error",
                    "No camera found on this device.",
                    "OK");

                return;
            }

            var frontCamera = cameras.FirstOrDefault(
                x => x.Position == CameraPosition.Front);

            var rearCamera = cameras.FirstOrDefault(
                x => x.Position == CameraPosition.Rear);

            var selectedCamera = frontCamera ?? rearCamera;

            if (selectedCamera == null)
            {
                await DisplayAlertAsync(
                    "Camera Error",
                    "Unable to select a camera.",
                    "OK");

                return;
            }

            _isFrontCamera = frontCamera != null;

            MyCamera.SelectedCamera = selectedCamera;
            MyCamera.CameraFlashMode = CameraFlashMode.Off;

            _isCameraInitialized = true;
            CameraButtons.IsVisible = true;
            MyCamera.IsVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"InitializeCameraAsync: {ex}");

            await DisplayAlertAsync(
                "Camera Error",
                $"Unable to open camera: {ex.Message}",
                "OK");
        }
        finally
        {
            CameraLoading.IsRunning = false;
            CameraLoading.IsVisible = false;
            CaptureButton.IsEnabled = true;

            _cameraLock.Release();
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (!_isCameraInitialized ||
            _isProcessing ||
            MyCamera?.Handler == null)
        {
            await DisplayAlertAsync(
                "Camera",
                "Camera is still initializing. Please wait a moment and try again.",
                "OK");

            return;
        }

        try
        {
            _isProcessing = true;

            CaptureButton.IsEnabled = false;
            flashToggleButton.IsEnabled = false;
            cameraToggleButton.IsEnabled = false;

            CaptureLoadingOverlay.IsVisible = true;

            await MyCamera.CaptureImage(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Capture error: {ex}");

            CaptureLoadingOverlay.IsVisible = false;
            CaptureButton.IsEnabled = true;
            flashToggleButton.IsEnabled = true;
            cameraToggleButton.IsEnabled = true;
            _isProcessing = false;

            await DisplayAlertAsync(
                "Capture Error",
                ex.Message,
                "OK");
        }
    }

    private async void MyCamera_MediaCaputure(
        object sender,
        MediaCapturedEventArgs e)
    {
        try
        {
            if (e.Media == null)
            {
                await DisplayAlertAsync(
                    "Error",
                    "Failed to capture image.",
                    "OK");

                return;
            }

            using var memoryStream = new MemoryStream();

            await e.Media.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (memoryStream.Length == 0)
            {
                await DisplayAlertAsync(
                    "Error",
                    "Captured image is empty.",
                    "OK");

                return;
            }

            byte[] processedImage = await ProcessImageAsync(
                memoryStream,
                _isFrontCamera);

            if (processedImage.Length == 0)
                return;

            _viewModel.SetCapturedImage(processedImage);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MyImage.Source = ImageSource.FromStream(
                    () => new MemoryStream(processedImage));

                MyImageBox.IsVisible = true;
                MyCamera.IsVisible = false;

                CameraButtons.IsVisible = false;
                SubmitButtons.IsVisible = true;

                MyCamera.StopCameraPreview();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Capture event error: {ex}");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlertAsync(
                    "Capture Error",
                    ex.Message,
                    "OK");
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CaptureLoadingOverlay.IsVisible = false;
                CaptureButton.IsEnabled = true;
                flashToggleButton.IsEnabled = true;
                cameraToggleButton.IsEnabled = true;
            });

            _isProcessing = false;
        }
    }

    private async Task<byte[]> ProcessImageAsync(
        Stream imageStream,
        bool isFrontCamera)
    {
        try
        {
            if (imageStream.CanSeek)
                imageStream.Position = 0;

            using var memoryStream = new MemoryStream();

            await imageStream.CopyToAsync(memoryStream);

            byte[] imageBytes = memoryStream.ToArray();

            if (imageBytes.Length == 0)
                throw new Exception("Image data is empty.");

            using var inputBitmap = SKBitmap.Decode(imageBytes);

            if (inputBitmap == null)
                throw new Exception("Failed to decode captured image.");

            using var rotatedBitmap = RotateBitmap(
                inputBitmap,
                isFrontCamera ? 270 : 90);

            double scale = Math.Min(
                1.0,
                Math.Min(
                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Width,
                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Height));

            int newWidth = Math.Max(
                1,
                (int)Math.Round(rotatedBitmap.Width * scale));

            int newHeight = Math.Max(
                1,
                (int)Math.Round(rotatedBitmap.Height * scale));

            using var resizedBitmap = rotatedBitmap.Resize(
                new SKImageInfo(newWidth, newHeight),
                new SKSamplingOptions(SKFilterMode.Linear));

            if (resizedBitmap == null)
                throw new Exception("Failed to resize image.");

            using var image = SKImage.FromBitmap(resizedBitmap);

            int quality = 80;
            byte[] outputBytes;

            while (true)
            {
                using var encodedData = image.Encode(
                    SKEncodedImageFormat.Jpeg,
                    quality);

                if (encodedData == null)
                    throw new Exception("JPEG encoding failed.");

                outputBytes = encodedData.ToArray();

                if (outputBytes.Length <= MAX_IMAGE_SIZE_BYTES ||
                    quality <= 10)
                {
                    break;
                }

                quality = Math.Max(10, quality - 10);
            }

            Debug.WriteLine(
                $"Processed image size: {outputBytes.Length} bytes");

            return outputBytes;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProcessImageAsync: {ex}");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlertAsync(
                    "Image Error",
                    ex.Message,
                    "OK");
            });

            return Array.Empty<byte>();
        }
    }

    private SKBitmap RotateBitmap(SKBitmap source, int degrees)
    {
        int normalizedDegrees = ((degrees % 360) + 360) % 360;

        if (normalizedDegrees == 0)
            return source.Copy();

        int newWidth = normalizedDegrees % 180 == 0
            ? source.Width
            : source.Height;

        int newHeight = normalizedDegrees % 180 == 0
            ? source.Height
            : source.Width;

        var rotatedBitmap = new SKBitmap(newWidth, newHeight);

        using var canvas = new SKCanvas(rotatedBitmap);

        canvas.Clear(SKColors.Transparent);

        canvas.Translate(newWidth / 2f, newHeight / 2f);
        canvas.RotateDegrees(normalizedDegrees);
        canvas.Translate(-source.Width / 2f, -source.Height / 2f);

        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();

        return rotatedBitmap;
    }

    private void Button_Flash_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (_isFrontCamera)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlertAsync(
                        "Flash",
                        "Flash is available only when supported by the rear camera.",
                        "OK");
                });

                return;
            }

            _isFlashOn = !_isFlashOn;

            MyCamera.CameraFlashMode = _isFlashOn
                ? CameraFlashMode.On
                : CameraFlashMode.Off;

            flashToggleButton.Text = _isFlashOn
                ? "Flash On"
                : "Flash Off";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Flash error: {ex.Message}");
        }
    }

    private async void ToggleCamera_Clicked(
        object sender,
        EventArgs e)
    {
        if (!_isCameraInitialized || _isProcessing)
            return;

        try
        {
            cameraToggleButton.IsEnabled = false;

            await _cameraProvider.RefreshAvailableCameras(
                CancellationToken.None);

            var cameras = _cameraProvider.AvailableCameras;

            if (cameras == null)
                return;

            var targetCamera = _isFrontCamera
                ? cameras.FirstOrDefault(x => x.Position == CameraPosition.Rear)
                : cameras.FirstOrDefault(x => x.Position == CameraPosition.Front);

            if (targetCamera == null)
            {
                await DisplayAlertAsync(
                    "Camera",
                    "The requested camera is not available.",
                    "OK");

                return;
            }

            MyCamera.StopCameraPreview();

            MyCamera.SelectedCamera = targetCamera;

            _isFrontCamera =
                targetCamera.Position == CameraPosition.Front;

            _isFlashOn = false;

            MyCamera.CameraFlashMode = CameraFlashMode.Off;
            flashToggleButton.Text = "Flash Off";

            await MyCamera.StartCameraPreview(
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Camera flip error: {ex}");

            await DisplayAlertAsync(
                "Camera Error",
                ex.Message,
                "OK");
        }
        finally
        {
            cameraToggleButton.IsEnabled = true;
        }
    }

    private async void Retake_Button(
        object sender,
        EventArgs e)
    {
        try
        {
            SubmitButtons.IsVisible = false;
            MyImageBox.IsVisible = false;
            MyImage.Source = null;

            CameraButtons.IsVisible = true;
            MyCamera.IsVisible = true;

            await MyCamera.StartCameraPreview(
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Retake error: {ex}");

            await DisplayAlertAsync(
                "Camera Error",
                ex.Message,
                "OK");
        }
    }

    private async Task NavigateBackAsync()
    {
        if (_isNavigatingBack)
            return;

        _isNavigatingBack = true;

        try
        {
            try
            {
                MyCamera?.StopCameraPreview();
                _isCameraInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Camera stop error: {ex}");
            }

            await Shell.Current.GoToAsync("..", true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Back navigation error: {ex}");

            await DisplayAlertAsync(
                "Navigation Error",
                ex.Message,
                "OK");
        }
        finally
        {
            _isNavigatingBack = false;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await NavigateBackAsync();
        });

        return true;
    }

    private async void BackButton_Clicked(
        object sender,
        EventArgs e)
    {
        await NavigateBackAsync();
    }
}



//using tutorial_demo_2.ViewModels;
//using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Maui.Core.Primitives;
//using CommunityToolkit.Maui.Views;
//using CommunityToolkit.Maui.Media;
//using tutorial_demo_2.Services;
//using Microsoft.Maui.Storage;
//using System.Net.Http;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Linq;
//using System;
//using System.Diagnostics;
//using SkiaSharp;

//namespace tutorial_demo_2.Pages;

//public partial class SelfiePage : ContentPage
//{
//    private readonly ICameraProvider _cameraProvider;
//    private readonly IAppSecureStorage _secureStorage;
//    private readonly HttpClient _httpClient;
//    private readonly SelfieViewModel _viewModel;

//    private const int MAX_IMAGE_DIMENSION = 300;
//    private const int MAX_IMAGE_SIZE_BYTES = 50 * 1024;

//    private bool isFrontCamera = true;
//    private string FlashMode = "Off";

//    public SelfiePage(
//        ICameraProvider cameraProvider,
//        HttpClient httpClient,
//        IAppSecureStorage secureStorage)
//    {
//        InitializeComponent();

//        SubmitButtons.IsVisible = false;

//        _cameraProvider = cameraProvider;
//        _secureStorage = secureStorage;
//        _httpClient = httpClient;

//        _viewModel = new SelfieViewModel(
//            Navigation,
//            secureStorage,
//            httpClient);

//        BindingContext = _viewModel;

//        SetFrontCamera();
//    }

//    private async void MyCamera_MediaCaputure(
//        object sender,
//        MediaCapturedEventArgs e)
//    {
//        try
//        {
//            if (e.Media == null)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Failed to capture image!",
//                    "OK");

//                return;
//            }

//            using var memoryStream = new MemoryStream();

//            await e.Media.CopyToAsync(memoryStream);
//            memoryStream.Position = 0;

//            if (memoryStream.Length == 0)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Captured image is empty.",
//                    "OK");

//                return;
//            }

//            byte[] processedImage = await ProcessImageAsync(
//                memoryStream,
//                isFrontCamera);

//            if (processedImage.Length == 0)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Failed to process image.",
//                    "OK");

//                return;
//            }

//            var imageSource = ImageSource.FromStream(
//                () => new MemoryStream(processedImage));

//            _viewModel.CapturedImage = processedImage;

//            Dispatcher.Dispatch(() =>
//            {
//                MyImage.Source = imageSource;
//                MyImageBox.IsVisible = true;
//                MyCamera.IsVisible = false;
//                SubmitButtons.IsVisible = true;
//                CameraButtons.IsVisible = false;
//                CaptureButton.IsEnabled = true;
//            });
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine(
//                $"MyCamera_MediaCaputure error: {ex}");

//            await Shell.Current.DisplayAlert(
//                "Error",
//                $"Failed to capture image: {ex.Message}",
//                "OK");
//        }
//    }

//    private async Task<byte[]> ProcessImageAsync(
//        Stream imageStream,
//        bool isFrontCamera)
//    {
//        try
//        {
//            if (imageStream.CanSeek)
//                imageStream.Position = 0;

//            using var memoryStream = new MemoryStream();

//            await imageStream.CopyToAsync(memoryStream);

//            byte[] imageBytes = memoryStream.ToArray();

//            if (imageBytes.Length == 0)
//                throw new Exception("Image data is empty.");

//            using var inputBitmap = SKBitmap.Decode(imageBytes);

//            if (inputBitmap == null)
//                throw new Exception("Failed to decode captured image.");

//            // Rotate according to camera position.
//            using var rotatedBitmap = RotateBitmap(
//                inputBitmap,
//                isFrontCamera ? 270 : 90);

//            // Preserve aspect ratio and avoid enlarging small images.
//            double scale = Math.Min(
//                1.0,
//                Math.Min(
//                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Width,
//                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Height));

//            int newWidth = Math.Max(
//                1,
//                (int)Math.Round(rotatedBitmap.Width * scale));

//            int newHeight = Math.Max(
//                1,
//                (int)Math.Round(rotatedBitmap.Height * scale));

//            // SkiaSharp 3.x compatible replacement for SKFilterQuality.High.
//            using var resizedBitmap = rotatedBitmap.Resize(
//                new SKImageInfo(newWidth, newHeight),
//                new SKSamplingOptions(SKFilterMode.Linear));

//            if (resizedBitmap == null)
//                throw new Exception("Failed to resize image.");

//            using var image = SKImage.FromBitmap(resizedBitmap);

//            int quality = 80;
//            byte[] outputBytes;

//            // Encode repeatedly until the image meets the size target,
//            // or the minimum quality is reached.
//            while (true)
//            {
//                using var encodedData = image.Encode(
//                    SKEncodedImageFormat.Jpeg,
//                    quality);

//                if (encodedData == null)
//                    throw new Exception("JPEG encoding failed.");

//                outputBytes = encodedData.ToArray();

//                if (outputBytes.Length <= MAX_IMAGE_SIZE_BYTES
//                    || quality <= 10)
//                {
//                    break;
//                }

//                quality = Math.Max(10, quality - 10);
//            }

//            Debug.WriteLine(
//                $"Processed image size: {outputBytes.Length} bytes");

//            return outputBytes;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"ProcessImageAsync error: {ex}");

//            await MainThread.InvokeOnMainThreadAsync(async () =>
//            {
//                await Shell.Current.DisplayAlert(
//                    "Image Error",
//                    ex.Message,
//                    "OK");
//            });

//            return Array.Empty<byte>();
//        }
//    }

//    private SKBitmap RotateBitmap(SKBitmap source, int degrees)
//    {
//        if (degrees == 0)
//            return source.Copy();

//        int normalizedDegrees = ((degrees % 360) + 360) % 360;

//        int newWidth = normalizedDegrees % 180 == 0
//            ? source.Width
//            : source.Height;

//        int newHeight = normalizedDegrees % 180 == 0
//            ? source.Height
//            : source.Width;

//        var rotatedBitmap = new SKBitmap(newWidth, newHeight);

//        using var canvas = new SKCanvas(rotatedBitmap);

//        canvas.Clear(SKColors.Transparent);

//        canvas.Translate(newWidth / 2f, newHeight / 2f);
//        canvas.RotateDegrees(normalizedDegrees);
//        canvas.Translate(-source.Width / 2f, -source.Height / 2f);

//        canvas.DrawBitmap(source, 0, 0);
//        canvas.Flush();

//        return rotatedBitmap;
//    }

//    private async void Button_Clicked(object sender, EventArgs e)
//    {
//        try
//        {
//            if (MyCamera == null)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Camera is not initialized.",
//                    "OK");

//                return;
//            }

//            CaptureButton.IsEnabled = false;

//            await MyCamera.CaptureImage(CancellationToken.None);
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"Capture error: {ex}");

//            CaptureButton.IsEnabled = true;

//            await Shell.Current.DisplayAlert(
//                "Error",
//                $"Failed to capture image: {ex.Message}",
//                "OK");
//        }
//    }

//    private void Button_Flash_Clicked(object sender, EventArgs e)
//    {
//        MyCamera.CameraFlashMode =
//            MyCamera.CameraFlashMode == CameraFlashMode.Off
//                ? CameraFlashMode.On
//                : CameraFlashMode.Off;

//        FlashMode =
//            MyCamera.CameraFlashMode == CameraFlashMode.Off
//                ? "🔅"
//                : "🔆";

//        flashToggleButton.Text = FlashMode;
//    }

//    private async void SetFrontCamera()
//    {
//        try
//        {
//            if (_cameraProvider == null || MyCamera == null)
//                return;

//            await _cameraProvider.RefreshAvailableCameras(
//                CancellationToken.None);

//            MyCamera.SelectedCamera =
//                _cameraProvider.AvailableCameras.FirstOrDefault(
//                    x => x.Position == CameraPosition.Front);

//            isFrontCamera = true;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"SetFrontCamera error: {ex.Message}");
//        }
//    }

//    private async void SetRearCamera()
//    {
//        try
//        {
//            if (_cameraProvider == null || MyCamera == null)
//                return;

//            await _cameraProvider.RefreshAvailableCameras(
//                CancellationToken.None);

//            MyCamera.SelectedCamera =
//                _cameraProvider.AvailableCameras.FirstOrDefault(
//                    x => x.Position == CameraPosition.Rear);

//            isFrontCamera = false;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"SetRearCamera error: {ex.Message}");
//        }
//    }

//    private void Retake_Button(object sender, EventArgs e)
//    {
//        MyImageBox.IsVisible = false;
//        MyCamera.IsVisible = true;

//        CaptureButton.IsEnabled = true;
//        CameraButtons.IsVisible = true;
//        SubmitButtons.IsVisible = false;

//        MyImage.Source = null;
//    }

//    private void ToggleCamera_Clicked(object sender, EventArgs e)
//    {
//        if (isFrontCamera)
//            SetRearCamera();
//        else
//            SetFrontCamera();
//    }

//    private async void OnBackButtonClicked(object sender, EventArgs e)
//    {
//        await Shell.Current.GoToAsync("//AttendancePage");
//    }
//}




//using tutorial_demo_2.ViewModels;
//using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Maui.Core.Primitives;
//using CommunityToolkit.Maui.Views;
//using CommunityToolkit.Maui.Media;
//using tutorial_demo_2.Services;
//using Microsoft.Maui.Storage;
//using System.Net.Http;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Linq;
//using System;
//using System.Diagnostics;
//using SkiaSharp;

//namespace tutorial_demo_2.Pages;

//public partial class SelfiePage : ContentPage
//{
//    private readonly ICameraProvider _cameraProvider;
//    private readonly IAppSecureStorage _secureStorage;
//    private readonly HttpClient _httpClient;
//    private readonly SelfieViewModel _viewModel;

//    private const int MAX_IMAGE_DIMENSION = 300;
//    private const int MAX_IMAGE_SIZE_BYTES = 50 * 1024;

//    private bool isFrontCamera = true;
//    private string FlashMode = "Off";

//    public SelfiePage(
//        ICameraProvider cameraProvider,
//        HttpClient httpClient,
//        IAppSecureStorage secureStorage)
//    {
//        InitializeComponent();

//        SubmitButtons.IsVisible = false;

//        _cameraProvider = cameraProvider;
//        _secureStorage = secureStorage;
//        _httpClient = httpClient;

//        _viewModel = new SelfieViewModel(
//            Navigation,
//            secureStorage,
//            httpClient);

//        BindingContext = _viewModel;

//        SetFrontCamera();
//    }

//    private async void MyCamera_MediaCaputure(
//        object sender,
//        MediaCapturedEventArgs e)
//    {
//        try
//        {
//            if (e.Media == null)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Failed to capture image!",
//                    "OK");

//                return;
//            }

//            using var memoryStream = new MemoryStream();

//            await e.Media.CopyToAsync(memoryStream);
//            memoryStream.Position = 0;

//            if (memoryStream.Length == 0)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Captured image is empty.",
//                    "OK");

//                return;
//            }

//            byte[] processedImage = await ProcessImageAsync(
//                memoryStream,
//                isFrontCamera);

//            if (processedImage.Length == 0)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Failed to process image.",
//                    "OK");

//                return;
//            }

//            var imageSource = ImageSource.FromStream(
//                () => new MemoryStream(processedImage));

//            _viewModel.CapturedImage = processedImage;

//            Dispatcher.Dispatch(() =>
//            {
//                MyImage.Source = imageSource;
//                MyImageBox.IsVisible = true;
//                MyCamera.IsVisible = false;
//                SubmitButtons.IsVisible = true;
//                CameraButtons.IsVisible = false;
//                CaptureButton.IsEnabled = true;
//            });
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine(
//                $"MyCamera_MediaCaputure error: {ex}");

//            await Shell.Current.DisplayAlert(
//                "Error",
//                $"Failed to capture image: {ex.Message}",
//                "OK");
//        }
//    }

//    private async Task<byte[]> ProcessImageAsync(
//        Stream imageStream,
//        bool isFrontCamera)
//    {
//        try
//        {
//            if (imageStream.CanSeek)
//                imageStream.Position = 0;

//            using var memoryStream = new MemoryStream();

//            await imageStream.CopyToAsync(memoryStream);

//            byte[] imageBytes = memoryStream.ToArray();

//            if (imageBytes.Length == 0)
//                throw new Exception("Image data is empty.");

//            using var inputBitmap = SKBitmap.Decode(imageBytes);

//            if (inputBitmap == null)
//                throw new Exception("Failed to decode captured image.");

//            // Rotate according to camera position.
//            using var rotatedBitmap = RotateBitmap(
//                inputBitmap,
//                isFrontCamera ? 270 : 90);

//            // Preserve aspect ratio and avoid enlarging small images.
//            double scale = Math.Min(
//                1.0,
//                Math.Min(
//                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Width,
//                    (double)MAX_IMAGE_DIMENSION / rotatedBitmap.Height));

//            int newWidth = Math.Max(
//                1,
//                (int)Math.Round(rotatedBitmap.Width * scale));

//            int newHeight = Math.Max(
//                1,
//                (int)Math.Round(rotatedBitmap.Height * scale));

//            // SkiaSharp 3.x compatible replacement for SKFilterQuality.High.
//            using var resizedBitmap = rotatedBitmap.Resize(
//                new SKImageInfo(newWidth, newHeight),
//                new SKSamplingOptions(SKFilterMode.Linear));

//            if (resizedBitmap == null)
//                throw new Exception("Failed to resize image.");

//            using var image = SKImage.FromBitmap(resizedBitmap);

//            int quality = 80;
//            byte[] outputBytes;

//            // Encode repeatedly until the image meets the size target,
//            // or the minimum quality is reached.
//            while (true)
//            {
//                using var encodedData = image.Encode(
//                    SKEncodedImageFormat.Jpeg,
//                    quality);

//                if (encodedData == null)
//                    throw new Exception("JPEG encoding failed.");

//                outputBytes = encodedData.ToArray();

//                if (outputBytes.Length <= MAX_IMAGE_SIZE_BYTES
//                    || quality <= 10)
//                {
//                    break;
//                }

//                quality = Math.Max(10, quality - 10);
//            }

//            Debug.WriteLine(
//                $"Processed image size: {outputBytes.Length} bytes");

//            return outputBytes;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"ProcessImageAsync error: {ex}");

//            await MainThread.InvokeOnMainThreadAsync(async () =>
//            {
//                await Shell.Current.DisplayAlert(
//                    "Image Error",
//                    ex.Message,
//                    "OK");
//            });

//            return Array.Empty<byte>();
//        }
//    }

//    private SKBitmap RotateBitmap(SKBitmap source, int degrees)
//    {
//        if (degrees == 0)
//            return source.Copy();

//        int normalizedDegrees = ((degrees % 360) + 360) % 360;

//        int newWidth = normalizedDegrees % 180 == 0
//            ? source.Width
//            : source.Height;

//        int newHeight = normalizedDegrees % 180 == 0
//            ? source.Height
//            : source.Width;

//        var rotatedBitmap = new SKBitmap(newWidth, newHeight);

//        using var canvas = new SKCanvas(rotatedBitmap);

//        canvas.Clear(SKColors.Transparent);

//        canvas.Translate(newWidth / 2f, newHeight / 2f);
//        canvas.RotateDegrees(normalizedDegrees);
//        canvas.Translate(-source.Width / 2f, -source.Height / 2f);

//        canvas.DrawBitmap(source, 0, 0);
//        canvas.Flush();

//        return rotatedBitmap;
//    }

//    private async void Button_Clicked(object sender, EventArgs e)
//    {
//        try
//        {
//            if (MyCamera == null)
//            {
//                await Shell.Current.DisplayAlert(
//                    "Error",
//                    "Camera is not initialized.",
//                    "OK");

//                return;
//            }

//            CaptureButton.IsEnabled = false;

//            await MyCamera.CaptureImage(CancellationToken.None);
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"Capture error: {ex}");

//            CaptureButton.IsEnabled = true;

//            await Shell.Current.DisplayAlert(
//                "Error",
//                $"Failed to capture image: {ex.Message}",
//                "OK");
//        }
//    }

//    private void Button_Flash_Clicked(object sender, EventArgs e)
//    {
//        MyCamera.CameraFlashMode =
//            MyCamera.CameraFlashMode == CameraFlashMode.Off
//                ? CameraFlashMode.On
//                : CameraFlashMode.Off;

//        FlashMode =
//            MyCamera.CameraFlashMode == CameraFlashMode.Off
//                ? "🔅"
//                : "🔆";

//        flashToggleButton.Text = FlashMode;
//    }

//    private async void SetFrontCamera()
//    {
//        try
//        {
//            if (_cameraProvider == null || MyCamera == null)
//                return;

//            await _cameraProvider.RefreshAvailableCameras(
//                CancellationToken.None);

//            MyCamera.SelectedCamera =
//                _cameraProvider.AvailableCameras.FirstOrDefault(
//                    x => x.Position == CameraPosition.Front);

//            isFrontCamera = true;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"SetFrontCamera error: {ex.Message}");
//        }
//    }

//    private async void SetRearCamera()
//    {
//        try
//        {
//            if (_cameraProvider == null || MyCamera == null)
//                return;

//            await _cameraProvider.RefreshAvailableCameras(
//                CancellationToken.None);

//            MyCamera.SelectedCamera =
//                _cameraProvider.AvailableCameras.FirstOrDefault(
//                    x => x.Position == CameraPosition.Rear);

//            isFrontCamera = false;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"SetRearCamera error: {ex.Message}");
//        }
//    }

//    private void Retake_Button(object sender, EventArgs e)
//    {
//        MyImageBox.IsVisible = false;
//        MyCamera.IsVisible = true;

//        CaptureButton.IsEnabled = true;
//        CameraButtons.IsVisible = true;
//        SubmitButtons.IsVisible = false;

//        MyImage.Source = null;
//    }

//    private void ToggleCamera_Clicked(object sender, EventArgs e)
//    {
//        if (isFrontCamera)
//            SetRearCamera();
//        else
//            SetFrontCamera();
//    }

//    private async void OnBackButtonClicked(object sender, EventArgs e)
//    {
//        await Shell.Current.GoToAsync("//AttendancePage");
//    }
//}
