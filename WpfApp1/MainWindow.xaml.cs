using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Providers.Shapefile;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using System.IO;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MapControl MapView { get; private set; }
        public MainWindow()
        {
            InitializeComponent();

            MapView = new MapControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };


            MapControl.Children.Add(MapView);

            // Bind the MapControl's Map to the ViewModel
            if (DataContext is MainViewModel vm)
            {
                MapView.Map = vm.Map;
            }
        }
    }
}
