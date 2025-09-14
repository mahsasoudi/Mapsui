using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Microsoft.Win32;
using NetTopologySuite.Geometries;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class MainViewModel : BindableBase
    {
        public Map Map { get; set; }

        public ObservableCollection<MemoryLayer> ShapeFiles { get; set; }

        private MemoryLayer _selectedShape;
        public MemoryLayer SelectedShape
        {
            get => _selectedShape;
            set
            {
                if (SetProperty(ref _selectedShape, value))
                {
                    if (_selectedShape.Extent != null)
                    {
                        var center = _selectedShape.Extent; 
                        Map.Navigator.ZoomToBox(center); // Zoom to selected layer
                    }
                }
            }
        }

        public DelegateCommand LoadShapeCommand { get; }
        public DelegateCommand RecenterMapCommand { get; }

        public MainViewModel()
        {
            // Initialize map with a base layer
            Map = new Map();
            InitializeMap();

            ShapeFiles = new ObservableCollection<MemoryLayer>();
            LoadShapeCommand = new DelegateCommand(OnLoadShape);
            RecenterMapCommand = new DelegateCommand(RecenterMapAction);
        }

        private void RecenterMapAction()
        {
            var baseLayer = Map.Layers[0]; // assume base layer is index 0
            if (baseLayer.Extent != null)              
                Map.Navigator.ZoomToBox(baseLayer.Extent);
        }

        private void InitializeMap()
        {
            // Create map with proper CRS
            Map.CRS = "EPSG:3857";// Web Mercator
            Map.BackColor = Color.White;


            // Create OpenStreetMap base layer
            var baseLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();

            // Insert base map at index 0
            Map.Layers.Insert(0, baseLayer);
        }
        private void OnLoadShape()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Shapefiles (*.shp)|*.shp",
                Title = "Select a shapefile"
            };

            if (dlg.ShowDialog() == true)
            {
                // Add shape to the map
                LoadShapeToMap(dlg.FileName);
            }
        }


        public static Geometry ToWebMercator(Geometry geom)
        {
            if (geom == null) return null;

            var geomFactory = new GeometryFactory();

            switch (geom)
            {
                case Point p:
                    return geomFactory.CreatePoint(LonLatToMercator(p.Coordinate));

                case LineString ls:
                    var coords = ls.Coordinates.Select(LonLatToMercator).ToArray();
                    return geomFactory.CreateLineString(coords);

                case Polygon poly:
                    var exterior = poly.ExteriorRing.Coordinates.Select(LonLatToMercator).ToArray();
                    var shell = geomFactory.CreateLinearRing(exterior);
                    return geomFactory.CreatePolygon(shell);

                default:
                    return geom; 
            }
        }

        private static Coordinate LonLatToMercator(Coordinate c)
        {
            double x = c.X * 20037508.34 / 180;
            double y = Math.Log(Math.Tan((90 + c.Y) * Math.PI / 360)) / (Math.PI / 180);
            y = y * 20037508.34 / 180;
            return new Coordinate(x, y);
        }


        private async Task LoadShapeToMap(string shapefilePath)
        {
            if (!File.Exists(shapefilePath))
                return;

            var shapeFile = new ShapeFile(shapefilePath, true);

            // Align shape with base layer
            MRect rect = shapeFile.GetExtent();
            if (rect == null) return;
            var section = new MSection(rect,1);
            var fetchInfo = new FetchInfo(section);
            var features = (await shapeFile.GetFeaturesAsync(fetchInfo)).ToList();
            foreach (var feature in features)
            {
                if (feature is GeometryFeature gf && gf.Geometry != null)
                {
                    gf.Geometry = ToWebMercator(gf.Geometry as NetTopologySuite.Geometries.Geometry);
                }
            }

            // Add MemoryLayer
            var layer = new MemoryLayer
            {
                Name = Path.GetFileNameWithoutExtension(shapefilePath),
                Features = features, 
                Style = new VectorStyle
                {
                    Fill = new Brush { Color = Mapsui.Styles.Color.LightGray },
                    Outline = new Pen { Color = Mapsui.Styles.Color.Gray, Width = 2 }
                }
            };         

            Map.Layers.Add(layer);
            ShapeFiles.Add(layer);
            SelectedShape = layer;
           
        }
    }
}
