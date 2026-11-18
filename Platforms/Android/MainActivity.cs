using Android.App;
using Android.Content.PM;
using Android.OS;
using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;


namespace tutorial_demo_2
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {

		protected override void OnCreate(Bundle savedInstanceState)
		{
			//base.OnCreate(savedInstanceState);

			//RequestLocationPermissions();

			base.OnCreate(savedInstanceState);

			// Handle Android 13+ photo permissions
			if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
			{
				if (CheckSelfPermission(Android.Manifest.Permission.ReadMediaImages) != Permission.Granted)
				{
					RequestPermissions(new string[] { Android.Manifest.Permission.ReadMediaImages }, 100);
				}
			}


		}


		private void RequestLocationPermissions()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.M) // Android 6+ (API 23+)
			{
				if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted ||
					ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
				{
					ActivityCompat.RequestPermissions(this, new string[]
					{
					Manifest.Permission.AccessFineLocation,
					Manifest.Permission.AccessCoarseLocation
					}, 0);
				}
			}
		}
	}
}
