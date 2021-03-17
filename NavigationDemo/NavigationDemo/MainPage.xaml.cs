using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Navigation;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Xamarin.Forms;
using static Xamarin.Essentials.TextToSpeech;

namespace NavigationDemo
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

        }

        async void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            // Assign the map to the MapView.
            MainMapView.Map = new Map(BasemapStyle.ArcGISNavigation);
            await MainMapView.Map.LoadAsync();
            //MapPoint mapCenterPoint = new MapPoint(, SpatialReferences.WebMercator);
            await MainMapView.SetViewpointAsync(new Viewpoint(52.0135053, 4.3367553, 72223.819286));

            // Create the route task, using the online routing service.
            RouteTask routeTask = await RouteTask.CreateAsync(_routingUri);

            // Get the default route parameters.
            RouteParameters routeParams = await routeTask.CreateDefaultParametersAsync();

            // Explicitly set values for parameters.
            routeParams.ReturnDirections = true;
            routeParams.ReturnStops = true;
            routeParams.ReturnRoutes = true;
            routeParams.OutputSpatialReference = SpatialReferences.Wgs84;

            // Create stops for each location.
            Stop stop1 = new Stop(_conventionCenter) { Name = "Delft Netherlands" };
            Stop stop2 = new Stop(_memorial) { Name = "Rotterdam Netherlands" };
            //Stop stop3 = new Stop(_aerospaceMuseum) { Name = "Amsterdam Netherlands" };

            // Assign the stops to the route parameters.
            List<Stop> stopPoints = new List<Stop> { stop1, stop2 };
            routeParams.SetStops(stopPoints);

            // Get the route results.
            _routeResult = await routeTask.SolveRouteAsync(routeParams);
            _route = _routeResult.Routes[0];

            // Add a graphics overlay for the route graphics.
            MainMapView.GraphicsOverlays.Add(new GraphicsOverlay());

            // Add graphics for the stops.
            SimpleMarkerSymbol stopSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.OrangeRed, 20);
            MainMapView.GraphicsOverlays[0].Graphics.Add(new Graphic(_conventionCenter, stopSymbol));
            MainMapView.GraphicsOverlays[0].Graphics.Add(new Graphic(_memorial, stopSymbol));
            //MainMapView.GraphicsOverlays[0].Graphics.Add(new Graphic(_aerospaceMuseum, stopSymbol));

            // Create a graphic (with a dashed line symbol) to represent the route.
            _routeAheadGraphic = new Graphic(_route.RouteGeometry) { Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Color.BlueViolet, 5) };

            // Create a graphic (solid) to represent the route that's been traveled (initially empty).
            _routeTraveledGraphic = new Graphic { Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.LightBlue, 3) };

            // Add the route graphics to the map view.
            MainMapView.GraphicsOverlays[0].Graphics.Add(_routeAheadGraphic);
            MainMapView.GraphicsOverlays[0].Graphics.Add(_routeTraveledGraphic);

            // Set the map viewpoint to show the entire route.
            await MainMapView.SetViewpointGeometryAsync(_route.RouteGeometry, 100);

            // Enable the navigation button.
            StartNavigationButton.IsEnabled = true;
        }

        // Variables for tracking the navigation route.
        private RouteTracker _tracker;
        private RouteResult _routeResult;
        private Route _route;

        // List of driving directions for the route.
        private IReadOnlyList<DirectionManeuver> _directionsList;

        // Cancellation token for speech synthesizer.
        private CancellationTokenSource _speechToken = new CancellationTokenSource();

        // Graphics to show progress along the route.
        private Graphic _routeAheadGraphic;
        private Graphic _routeTraveledGraphic;

        // San Diego Convention Center.
        private readonly MapPoint _conventionCenter = new MapPoint(51.9995826, 4.3286784);

        // USS San Diego Memorial.
        private readonly MapPoint _memorial = new MapPoint(51.9280712, 4.4207887);

        // RH Fleet Aerospace Museum.
        private readonly MapPoint _aerospaceMuseum = new MapPoint(52.3547498, 4.833921);

        // Feature service for routing in San Diego.
        private readonly Uri _routingUri = new Uri("https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World");

        void Button_Clicked_1(System.Object sender, System.EventArgs e)
        {
            //Start navigation
            // Disable the start navigation button.
            StartNavigationButton.IsEnabled = false;

            // Get the directions for the route.
            _directionsList = _route.DirectionManeuvers;

            // Create a route tracker.
            _tracker = new RouteTracker(_routeResult, 0);
            _tracker.NewVoiceGuidance += SpeakDirection;

            // Handle route tracking status changes.
            _tracker.TrackingStatusChanged += TrackingStatusUpdated;

            // Turn on navigation mode for the map view.
            MainMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Navigation;
            MainMapView.LocationDisplay.AutoPanModeChanged += AutoPanModeChanged;

            // Add a data source for the location display.
            var simulationParameters = new SimulationParameters(DateTimeOffset.Now, 40.0);
            var simulatedDataSource = new SimulatedLocationDataSource();
            simulatedDataSource.SetLocationsWithPolyline(_route.RouteGeometry, simulationParameters);
            MainMapView.LocationDisplay.DataSource = new RouteTrackerDisplayLocationDataSource(simulatedDataSource, _tracker);

            // Use this instead if you want real location:
            // MyMapView.LocationDisplay.DataSource = new RouteTrackerLocationDataSource(new SystemLocationDataSource(), _tracker);

            // Enable the location display (this wil start the location data source).
            MainMapView.LocationDisplay.IsEnabled = true;
        }

        private void TrackingStatusUpdated(object sender, RouteTrackerTrackingStatusChangedEventArgs e)
        {
            TrackingStatus status = e.TrackingStatus;

            // Start building a status message for the UI.
            System.Text.StringBuilder statusMessageBuilder = new System.Text.StringBuilder();

            // Check the destination status.
            if (status.DestinationStatus == DestinationStatus.NotReached || status.DestinationStatus == DestinationStatus.Approaching)
            {
                statusMessageBuilder.AppendLine("Distance remaining: " +
                                            status.RouteProgress.RemainingDistance.DisplayText + " " +
                                            status.RouteProgress.RemainingDistance.DisplayTextUnits.PluralDisplayName);

                statusMessageBuilder.AppendLine("Time remaining: " +
                                                status.RouteProgress.RemainingTime.ToString(@"hh\:mm\:ss"));

                if (status.CurrentManeuverIndex + 1 < _directionsList.Count)
                {
                    statusMessageBuilder.AppendLine("Next direction: " + _directionsList[status.CurrentManeuverIndex + 1].DirectionText);
                }

                // Set geometries for progress and the remaining route.
                _routeAheadGraphic.Geometry = status.RouteProgress.RemainingGeometry;
                _routeTraveledGraphic.Geometry = status.RouteProgress.TraversedGeometry;
            }
            else if (status.DestinationStatus == DestinationStatus.Reached)
            {
                statusMessageBuilder.AppendLine("Destination reached.");

                // Set the route geometries to reflect the completed route.
                _routeAheadGraphic.Geometry = null;
                _routeTraveledGraphic.Geometry = status.RouteResult.Routes[0].RouteGeometry;

                // Navigate to the next stop (if there are stops remaining).
                if (status.RemainingDestinationCount > 1)
                {
                    _tracker.SwitchToNextDestinationAsync();
                }

                // Stop the simulated location data source.
                MainMapView.LocationDisplay.DataSource.StopAsync();
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                // Show the status information in the UI.
                //MessagesTextBlock.Text = statusMessageBuilder.ToString().TrimEnd('\n').TrimEnd('\r');
            });
        }

        private async void SpeakDirection(object sender, RouteTrackerNewVoiceGuidanceEventArgs e)
        {
            // Say the direction using voice synthesis.
            if (e.VoiceGuidance.Text?.Length > 0)
            {
                _speechToken.Cancel();
                _speechToken = new CancellationTokenSource();
                await SpeakAsync(e.VoiceGuidance.Text, _speechToken.Token);
            }
        }

        private void AutoPanModeChanged(object sender, LocationDisplayAutoPanMode e)
        {
            // Turn the recenter button on or off when the location display changes to or from navigation mode.
            //RecenterButton.IsEnabled = e != LocationDisplayAutoPanMode.Navigation;
        }

        // This location data source uses an input data source and a route tracker.
        // The location source that it updates is based on the snapped-to-route location from the route tracker.
        public class RouteTrackerDisplayLocationDataSource : LocationDataSource
        {
            private LocationDataSource _inputDataSource;
            private RouteTracker _routeTracker;

            public RouteTrackerDisplayLocationDataSource(LocationDataSource dataSource, RouteTracker routeTracker)
            {
                // Set the data source
                _inputDataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

                // Set the route tracker.
                _routeTracker = routeTracker ?? throw new ArgumentNullException(nameof(routeTracker));

                // Change the tracker location when the source location changes.
                _inputDataSource.LocationChanged += InputLocationChanged;

                // Update the location output when the tracker location updates.
                _routeTracker.TrackingStatusChanged += TrackingStatusChanged;
            }

            private void InputLocationChanged(object sender, Location e)
            {
                // Update the tracker location with the new location from the source (simulation or GPS).
                _routeTracker.TrackLocationAsync(e);
            }

            private void TrackingStatusChanged(object sender, RouteTrackerTrackingStatusChangedEventArgs e)
            {
                // Check if the tracking status has a location.
                if (e.TrackingStatus.DisplayLocation != null)
                {
                    // Call the base method for LocationDataSource to update the location with the tracked (snapped to route) location.
                    UpdateLocation(e.TrackingStatus.DisplayLocation);
                }
            }

            protected override Task OnStartAsync() => _inputDataSource.StartAsync();

            protected override Task OnStopAsync() => _inputDataSource.StartAsync();
        }
    }
}