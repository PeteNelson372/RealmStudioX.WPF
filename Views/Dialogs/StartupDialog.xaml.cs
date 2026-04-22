using RealmStudioX.WPF.ViewModels.Startup;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;


namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for StartupDialog.xaml
    /// </summary>
    public partial class StartupDialog : Window, INotifyPropertyChanged
    {
        private readonly string _themesFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Assets", "Themes");

        private readonly string _mapsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Maps");

        public StartupResult? Result { get; private set; }

        private double _aspectRatio = 1920.0 / 1080.0;
        private bool _lockAspect = true;

        public StartupViewModel ViewModel { get; }

        public StartupDialog()
        {
            InitializeComponent();

            ViewModel = new StartupViewModel(_mapsFolder, _themesFolder);
            ViewModel.RequestClose += OnRequestClose;

            DataContext = ViewModel;

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

            PresetList.ItemsSource = new List<MapSizePreset>
            {
                new() { Width = 1024, Height = 768, Display = "1024 x 768 (XGA)" },
                new() { Width = 1280, Height = 720, Display = "1280 x 720 (720P)" },
                new() { Width = 1280, Height = 1024, Display = "1280 x 1024 (SXGA)" },
                new() { Width = 1600, Height = 1200, Display = "1600 x 1200 (UXGA)" },
                new() { Width = 1920, Height = 1080, Display = "1920 x 1080 (1080P Full HD)", IsSelected = true },
                new() { Width = 2560, Height = 1080, Display = "2560 x 1080 (2K)" },
                new() { Width = 2048, Height = 1024, Display = "2048 x 1024 (Equirectangular 2K)" },
                new() { Width = 3840, Height = 2160, Display = "3840 x 2160 (4K Ultra HD)" },
                new() { Width = 4096, Height = 2048, Display = "4096 x 2048 (Equirectangular 4K)" },
                new() { Width = 3300, Height = 2250, Display = "3300 x 2250 (US Letter 300 DPI)" },
                new() { Width = 1754, Height = 1240, Display = "1754 x 1240 (A6 300 DPI)" },
                new() { Width = 2480, Height = 1754, Display = "2480 x 1754 (A5 300 DPI)" },
                new() { Width = 3508, Height = 2480, Display = "3508 x 2480 (A4 300 DPI)" },
                new() { Width = 4960, Height = 3508, Display = "4960 x 3508 (A3 300 DPI)" },
                new() { Width = 7016, Height = 4960, Display = "7016 x 4960 (A2 300 DPI)" },
                new() { Width = 7680, Height = 4320, Display = "7680 x 4320 (8K UHD)" },
                new() { Width = 8192, Height = 4096, Display = "8192 x 4096 (Equirectangular 8K)" },
                new() { Width = 10000, Height = 10000, Display = "10000 x 10000 (Maximum)" }
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

                ViewModel.Width = (int)WidthBox.Value;
            };

            HeightBox.ValueChanged += (s, e) =>
            {
                if (_lockAspect && WidthBox.Value > 0)
                {
                    var newWidth = Math.Round(HeightBox.Value * _aspectRatio);
                    WidthBox.SetValueSilently(newWidth);
                }

                UpdateAspect();

                ViewModel.Height = (int)HeightBox.Value;
            };
        }

        private void OnRequestClose(bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        private void AreaWidthBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AreaWidthBox.Value > 0)
            {
                var areaHeight = AreaWidthBox.Value / _aspectRatio;
                AreaHeightText.Text = areaHeight.ToString("0");

                ViewModel.AreaWidth = (float)AreaWidthBox.Value;
                ViewModel.AreaHeight = (float)areaHeight;
            }
        }

        private void OnPresetSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.RadioButton rb && rb.DataContext is MapSizePreset preset)
            {
                WidthBox.SetValue(preset.Width, false);
                HeightBox.SetValue(preset.Height, false);

                UpdateAspect();

                ViewModel.Width = (int)WidthBox.Value;
                ViewModel.Height = (int)HeightBox.Value;
            }
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
            WidthBox.Commit();
            HeightBox.Commit();

            var w = WidthBox.Value;
            var h = HeightBox.Value;

            WidthBox.SetValue(h, false);
            HeightBox.SetValue(w, false);

            UpdateAspect();

            ViewModel.Width = (int)WidthBox.Value;
            ViewModel.Height = (int)HeightBox.Value;
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {

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

    public class MapSizePreset
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Display { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
