using System.IO;
using System.Windows;


namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for StartupDialog.xaml
    /// </summary>
    public partial class StartupDialog : Window
    {
        private readonly string _mapsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RealmStudioX", "Maps");

        public StartupResult? Result { get; private set; }

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

            LoadMaps();
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
                Width = 0,
                Height = 0,
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

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class MapTypeOption
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Key { get; set; } = "";
    }
}
