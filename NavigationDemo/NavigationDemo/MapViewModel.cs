using Esri.ArcGISRuntime.Mapping;

namespace NavigationDemo
{
    public class MapViewModel
    {
        public Map Map { get; set; } = new Map(BasemapStyle.ArcGISTopographic);
    }
}