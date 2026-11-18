using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace tutorial_demo_2
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
			Loaded += OnMainPageLoaded;
		}

		private async void OnMainPageLoaded(object sender, EventArgs e)
		{
			Debug.WriteLine("MainPage loaded - starting GPS check");
			await CheckAndEnableGps();
		}

		private async Task CheckAndEnableGps()
		{
			try
			{
				// 1. Check and request location permission
				var status = await CheckLocationPermission();
				if (!status)
				{
					Debug.WriteLine("Location permission denied");
					await ShowPermissionDeniedAndQuit();
					return;
				}

				// 2. Check and enable GPS if needed (Android only)
				if (DeviceInfo.Platform == DevicePlatform.Android)
				{
					var gpsEnabled = await IsGpsEnabled();
					Debug.WriteLine($"Initial GPS status: {gpsEnabled}");

					if (!gpsEnabled)
					{
						Debug.WriteLine("GPS is disabled - prompting user");
						var userAccepted = await DisplayAlertAsync(
							"GPS Required",
							"This app requires GPS to function properly. Please enable GPS.",
							"Open Settings", "Cancel");

						if (userAccepted)
						{
							Debug.WriteLine("User wants to enable GPS");
							await EnableGpsOnDevice();

							// Wait and check again
							await Task.Delay(3000);
							gpsEnabled = await IsGpsEnabled();
							Debug.WriteLine($"GPS status after enabling attempt: {gpsEnabled}");

							if (!gpsEnabled)
							{
								Debug.WriteLine("User didn't enable GPS");
								await ShowGpsRequiredAndQuit();
								return;
							}
						}
						else
						{
							Debug.WriteLine("User declined to enable GPS");
							await ShowGpsRequiredAndQuit();
							return;
						}
					}
				}

				// 3. All checks passed - navigate to login
				Debug.WriteLine("All checks passed - navigating to login");
				await Shell.Current.GoToAsync("//LoginPage");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in GPS check: {ex}");
				// Fallback - allow navigation anyway
				await Shell.Current.GoToAsync("//LoginPage");
			}
		}

		private async Task<bool> CheckLocationPermission()
		{
			try
			{
				var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
				if (status != PermissionStatus.Granted)
				{
					status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
				}
				return status == PermissionStatus.Granted;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Permission check error: {ex}");
				return false;
			}
		}

		private async Task ShowPermissionDeniedAndQuit()
		{
			await DisplayAlertAsync(
				"Permission Required",
				"Location permission is required for this app to work. Please enable it in settings.",
				"OK");
			Application.Current.Quit();
		}

		private async Task ShowGpsRequiredAndQuit()
		{
			await DisplayAlertAsync(
				"GPS Required",
				"This app requires GPS to be enabled. Please enable GPS in settings.",
				"OK");
			Application.Current.Quit();
		}

#if ANDROID
        private Task<bool> IsGpsEnabled()
        {
            try
            {
                var locationManager = (Android.Locations.LocationManager)Android.App.Application.Context
                    .GetSystemService(Android.Content.Context.LocationService);
                return Task.FromResult(
                    locationManager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider) ||
                    locationManager.IsProviderEnabled(Android.Locations.LocationManager.NetworkProvider));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPS check error: {ex}");
                return Task.FromResult(true); // Fail safe
            }
        }

        private Task EnableGpsOnDevice()
        {
            try
            {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening GPS settings: {ex}");
            }
            return Task.CompletedTask;
        }
#else
		private Task<bool> IsGpsEnabled() => Task.FromResult(true);
		private Task EnableGpsOnDevice() => Task.CompletedTask;
#endif
	}
}