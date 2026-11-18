using Microsoft.Maui.ApplicationModel;

namespace tutorial_demo_2.Services
{
	public interface IGpsService
	{
		Task<bool> IsGpsEnabled();
		Task EnableGps();
	}

#if ANDROID
    public class GpsService : IGpsService
    {
        public async Task<bool> IsGpsEnabled()
        {
            var locationManager = (Android.Locations.LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            return locationManager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider);
        }

        public async Task EnableGps()
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
    }
#else
	public class GpsService : IGpsService
	{
		public Task<bool> IsGpsEnabled()
		{
			// For non-Android platforms, return true or implement platform-specific checks
			return Task.FromResult(true);
		}

		public Task EnableGps()
		{
			// For non-Android platforms, do nothing or implement platform-specific behavior
			return Task.CompletedTask;
		}
	}
#endif
}