using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using Android.Support.V4.Content;
using Android;

namespace NavigationDemo.Android
{
    [Activity(Label = "NavigationDemo", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        private const int LocationPermissionRequestCode = 99;
        private const int LocationRequesNoMap = 97;

        private Esri.ArcGISRuntime.Xamarin.Forms.MapView _lastUsedMapView;
        private TaskCompletionSource<bool> _permissionTCS;

        public async Task<bool> AskForLocationPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, LocationService) != Permission.Granted)
            {
                _permissionTCS = new TaskCompletionSource<bool>();
                RequestPermissions(new[] { Manifest.Permission.AccessFineLocation }, LocationRequesNoMap);
                return await _permissionTCS.Task;
            }
            else return true;
        }

        public async void AskForLocationPermission(Esri.ArcGISRuntime.Xamarin.Forms.MapView myMapView)
        {
            // Save the mapview for later.
            _lastUsedMapView = myMapView;

            // Only check if permission hasn't been granted yet.
            if (ContextCompat.CheckSelfPermission(this, LocationService) != Permission.Granted)
            {
                // Show the standard permission dialog.
                // Once the user has accepted or denied, OnRequestPermissionsResult is called with the result.
                RequestPermissions(new[] { Manifest.Permission.AccessFineLocation }, LocationPermissionRequestCode);
            }
            else
            {
                try
                {
                    // Explicit DataSource.LoadAsync call is used to surface any errors that may arise.
                    await myMapView.LocationDisplay.DataSource.StartAsync();
                    myMapView.LocationDisplay.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    ShowMessage(ex.Message, "Failed to start location display.");
                }
            }
        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == LocationPermissionRequestCode)
            {
                // If the permissions were granted, enable location.
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted && _lastUsedMapView != null)
                {
                    System.Diagnostics.Debug.WriteLine("User affirmatively gave permission to use location. Enabling location.");
                    try
                    {
                        // Explicit DataSource.LoadAsync call is used to surface any errors that may arise.
                        await _lastUsedMapView.LocationDisplay.DataSource.StartAsync();
                        _lastUsedMapView.LocationDisplay.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                        ShowMessage(ex.Message, "Failed to start location display.");
                    }
                }
                else
                {
                    ShowMessage("Location permissions not granted.", "Failed to start location display.");
                }

                // Reset the mapview.
                _lastUsedMapView = null;
            }
            else if (requestCode == LocationRequesNoMap)
            {
                _permissionTCS.TrySetResult(grantResults.Length == 1 && grantResults[0] == Permission.Granted);
            }
        }

        private void ShowMessage(string message, string title = "Error") => new AlertDialog.Builder(this).SetTitle(title).SetMessage(message).Show();

    }
}