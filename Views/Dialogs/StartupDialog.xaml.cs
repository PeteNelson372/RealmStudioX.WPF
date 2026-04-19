using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for StartupDialog.xaml
    /// </summary>
    public partial class StartupDialog : Window, INotifyPropertyChanged
    {
        private readonly string _mapsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Maps");

        public StartupResult? Result { get; private set; }

        private double _width = 1920;
        private double _height = 1080;

        private double _aspectRatio = 1920.0 / 1080.0;
        private bool _lockAspect = true;

        private double _areaWidth = 100;

        public StartupDialog()
        {
            InitializeComponent();

            MapTypeList.ItemsSource = new List<MapTypeOption>
            {
                new() { Name="World",               Icon="Earth",        Key="World" },
                new() { Name="Region",              Icon="Map",          Key="Region" },
                new() { Name="City",                Icon="City",         Key="City" },
                new() { Name="Building Floor",      Icon="HomeCity",     Key="Building" },
                new() { Name="Dungeon Level",       Icon="Layers",       Key="Dungeon" },
                new() { Name="Ship Deck",           Icon="Ferry",        Key="Ship" },
                new() { Name="Planet",              Icon="Orbit",        Key="Planet" },

                // Map sets
                new() { Name="Dungeon Set",  Icon="ViewList",     Key="DungeonSet" },
                new() { Name="Ship Set",     Icon="ViewList",     Key="ShipSet" },
                new() { Name="Building Set", Icon="ViewList",     Key="BuildingSet" },
                new() { Name="World Set",    Icon="ViewList",     Key="WorldSet" },
            };

            AreaWidthBox.ValueChanged += AreaWidthBox_ValueChanged;

            WidthBox.ValueChanged += (s, e) =>
            {
                if (_lockAspect && HeightBox.Value > 0)
                {
                    var newHeight = Math.Round(WidthBox.Value / _aspectRatio);
                    HeightBox.SetValueSilently(newHeight);
                }

                UpdateAspect();
            };

            HeightBox.ValueChanged += (s, e) =>
            {
                if (_lockAspect && WidthBox.Value > 0)
                {
                    var newWidth = Math.Round(HeightBox.Value * _aspectRatio);
                    WidthBox.SetValueSilently(newWidth);
                }

                UpdateAspect();
            };

            LoadMaps();
        }

        private void AreaWidthBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AreaWidthBox.Value > 0)
            {
                double height = AreaWidthBox.Value / _aspectRatio;
                AreaHeightText.Text = height.ToString("0");
            }
        }

        public double AreaWidth
        {
            get => _areaWidth;
            set
            {
                _areaWidth = value;
                OnPropertyChanged();
            }
        }

        private void LoadMaps()
        {
            if (!Directory.Exists(_mapsFolder))
                Directory.CreateDirectory(_mapsFolder);

            var files = Directory.GetFiles(_mapsFolder, "*.rsm"); // your format

            MapList.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f));
        }

        private void OnCreate(object sender, RoutedEventArgs e)
        {
            Result = new StartupResult
            {
                IsNew = true,
                Width = (int)_width,
                Height = (int)_height,
                Theme = ""
            };

            DialogResult = true;
            Close();
        }

        private void OnOpen(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null) return;

            var file = Path.Combine(_mapsFolder, MapList.SelectedItem + ".rsm");

            Result = new StartupResult
            {
                IsNew = false,
                FilePath = file
            };

            DialogResult = true;
            Close();
        }


        private void UpdateAspect()
        {
            if (WidthBox.Value > 0 && HeightBox.Value > 0)
            {
                _aspectRatio = WidthBox.Value / HeightBox.Value;
                AspectRatioText.Text = _aspectRatio.ToString("0.00");
            }
        }

        private void OnToggleAspectLock(object sender, RoutedEventArgs e)
        {
            _lockAspect = !_lockAspect;
        }

        private void OnSwapDimensions(object sender, RoutedEventArgs e)
        {
            var w = WidthBox.Value;
            var h = HeightBox.Value;

            WidthBox.Value = h;
            HeightBox.Value = w;

            UpdateAspect();
        }


        private void OnBrowse(object sender, RoutedEventArgs e)
        {

        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MapTypeOption
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Key { get; set; } = "";
    }
}
