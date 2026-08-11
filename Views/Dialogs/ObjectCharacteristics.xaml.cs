using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Dialogs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;

namespace RealmStudioX.WPF.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ObjectCharacteristics.xaml
    /// </summary>
    public partial class ObjectCharacteristics : ModalDialog, INotifyPropertyChanged
    {
        private readonly ObjectCharacteristicsViewModel ViewModel;
        public ObjectCharacteristics(ObjectCharacteristicsViewModel vm, MapObjectType objectType)
        {
            InitializeComponent();

            ObjectTypesList.ItemsSource = new List<string>();

            ViewModel = vm;

            DataContext = ViewModel;

            ViewModel.RequestClose += OnRequestClose;

            SetObjectType(objectType);
        }

        public override string WindowId { get; } = Guid.NewGuid().ToString();

        private void OnRequestClose()
        {
            Close();
        }

        public void SetObjectType(MapObjectType objectType)
        {
            if (objectType == MapObjectType.Realm)
            {
                ObjectTypesList.ItemsSource = realmTypes;
                TitleBarText.Text = "Realm Characteristics";
            }
            else if (objectType == MapObjectType.Map)
            {
                ObjectTypesList.ItemsSource = realmTypes;
                TitleBarText.Text = "Map Characteristics";
            }
            else if (objectType == MapObjectType.Landform)
            {
                ObjectTypesList.ItemsSource = landformTypes;
                TitleBarText.Text = "Landform Characteristics";
            }
            else if (objectType == MapObjectType.WaterSystem)
            {
                List<string> waterTypes = [];
                
                waterTypes.AddRange(waterSystemTypes);
                waterTypes.AddRange(waterFeatureTypes);
                waterTypes.AddRange(riverTypes);

                ObjectTypesList.ItemsSource = waterTypes;
                TitleBarText.Text = "Water System Characteristics";
            }
            else if (objectType == MapObjectType.Lake)
            {
                ObjectTypesList.ItemsSource = waterFeatureTypes;
                TitleBarText.Text = "Lake Characteristics";
            }
            else if (objectType == MapObjectType.River)
            {
                ObjectTypesList.ItemsSource = riverTypes;
                TitleBarText.Text = "River Characteristics";
            }
            else if (objectType == MapObjectType.WaterFeature)
            {
                List<string> waterTypes = [];

                waterTypes.AddRange(waterSystemTypes);
                waterTypes.AddRange(waterFeatureTypes);
                waterTypes.AddRange(riverTypes);

                ObjectTypesList.ItemsSource = waterTypes;
                TitleBarText.Text = "Water Feature Characteristics";
            }
            else if (objectType == MapObjectType.Region)
            {
                ObjectTypesList.ItemsSource = regionTypes;
                TitleBarText.Text = "Region Characteristics";
            }
            else if (objectType == MapObjectType.MapPath)
            {
                ObjectTypesList.ItemsSource = pathTypes;
                TitleBarText.Text = "Map Path Characteristics";
            }
            else if (objectType == MapObjectType.Symbol)
            {
                ObjectTypesList.ItemsSource = symbolTypes;
                TitleBarText.Text = "Symbol Characteristics";
            }
        }

        private readonly List<string> realmTypes =
        [
            "World",
            "Region",
            "City",
            "Interior",
            "Dungeon",
            "SolarSystem",
            "Ship",
            "Other",
        ];

        private readonly List<string> landformTypes =
        [
            "Continent",
            "Supercontinent",
            "Island",
            "Atoll",
            "Islet",
            "Archipelago",
            "Isle",
            "Cay",
            "Holm",
            "Skerry",
            "Spit",
            "Peninsula",
            "Cape",
            "Reef",
            "Beach",
            "Insel"

        ];

        private readonly List<string> waterSystemTypes =
        [
            "Watershed",
            "Basin",
            "Catchment",
            "Drainage",
            "River Basin",
            "Water System",
            "Drainage Area",
            "River System"
        ];

        private readonly List<string> waterFeatureTypes =
        [
            "Lake",
            "Pond",
            "Loch",
            "Inland Lagoon",
            "Inland Sea",
            "Reservoir",
            "Tarn",
            "Waterfall",
            "Spring",
            "Crater Lake",
            "Swamp",
            "Marsh",
            "Bog",
            "Fen",
            "Seep",
            "Waterhole",
            "Thermal Spring",
            "Hot Spring",
            "Geyser",
            "Mire",
        ];

        private readonly List<string> riverTypes =
        [
            "River",
            "Stream",
            "Brook",
            "Creek",
            "Rivulet",
            "Rill",
            "Run",
            "Branch",
            "Fork",
            "Tributary",
            "Course",
            "Channel",
            "Burn",
            "Beck",
            "Gill",
            "Ghyll",
            "Bourne",
            "Bourn",
            "Kill",
            "Freshet",
            "Sike",
        ];

        private readonly List<string> pathTypes =
        [
            // Common Types of Paths and Roads
            "Road",
            "Path",
            "Trail",
            "Track",
            "Lane",
            "Alley",
            "Way",
            "Route",

            // Urban or Formal Road Types
            "Avenue",
            "Boulevard",
            "Street",
            "Drive",
            "Court",
            "Place",
            "Terrace",
            "Crescent",
            "Circle",

            // Rural or Natural Pathways
            "Footpath",
            "Bridleway",
            "Byway",
            "Cartway",
            "Trackway",
            "Greenway",
            "Boardwalk",

            // Poetic, Archaic, or Fantasy-Inspired
            "Passage",
            "Walk",
            "Causeway",
            "Trackless Way",
            "Thoroughfare",
            "Pilgrim's Way",
            "Shadowpath",
            "Starroad",
            "Wanderer's Trail"
        ];

        private readonly List<string> regionTypes =
        [
            // Political / Administrative Regions
            "Country",
            "Nation",
            "State",
            "Province",
            "Region",
            "Territory",
            "County",
            "District",
            "Municipality",
            "Commune",
            "Canton",
            "Prefecture",
            "Realm",
            "Dominion",
            "Fantasy Realm",
            "Kingdom",
            "Empire",
            "Duchy",
            "Principality",
            "Republic",
            "Theocracy",
            "Tribal Land",
            "Confederation",
            "Federation",
            "Protectorate",
            "Colony",
            "Sultanate",
            "Caliphate",
            "City-State",
            "Barony",
            "Marches",
            "Commonwealth",

            // Natural Land Regions
            "Desert",
            "Forest",
            "Jungle",
            "Tundra",
            "Plain",
            "Prairie",
            "Savanna",
            "Steppe",
            "Wetland",
            "Marsh",
            "Swamp",
            "Glacier",
            "Highland",
            "Lowland",
            "Valley",
            "Canyon",

            // Oceanic and Coastal Regions
            "Ocean",
            "Sea",
            "Bay",
            "Gulf",
            "Cove",
            "Lagoon",
            "Sound",
            "Inlet",
            "Fjord",
            "Reef",
            "Atoll",
            "Strait",
            "Channel",
            "Continental Shelf"
        ];

        private readonly List<string> symbolTypes =
        [
            // Structures
            "House", "Home", "Hut", "Cottage", "Cabin", "Manor", "Villa", "Lodge", "Shack",
            "Inn", "Tavern", "Alehouse", "Pub", "Hostel", "Bunkhouse", "Hotel", "Motel", "Resort",
            "Bank", "Shop", "Store", "Market", "Temple", "Shrine", "Church", "Chapel", "Cathedral",
            "Library", "Hall", "Tower", "Sawmill", "Mill", "Warehouse", "Barn", "Stable", "Forge", "Workshop",
            "Academy", "Barracks", "Fort", "Keep", "Castle", "Citadel", "Outpost", "Garrison", "Watchtower",
            "Gatehouse", "Wall", "Palisade", "Fence", "Bridge", "Causeway", "Arch", "Dam", "Aqueduct", "Pier", "Dock",

            // Vegetation
            "Tree", "Oak", "Pine", "Willow", "Elm", "Fir", "Palm", "Maple", "Birch", "Cedar", "Cherry", "Apple", "Peach", "Redwood",
            "Cypress", "Sequoia", "Spruce", "Ash", "Beech", "Hickory", "Walnut", "Chestnut", "Poplar", "Sycamore",
            "Hemlock", "Larch", "Alder", "Dogwood", "Magnolia", "Hawthorn", "Juniper", "Yew", "Eucalyptus",
            "Shrub", "Bush", "Bramble", "Hedge", "Vine", "Reed", "Thicket",
            "Grass", "Turf", "Sod", "Moss", "Fern", "Lichen", "Flower", "Blossom", "Weed", "Undergrowth",

            // Terrain
            "Mountain", "Hill", "Peak", "Ridge", "Plateau", "Crag", "Bluff", "Cliff", "Escarpment",
            "Valley", "Ravine", "Gorge", "Gully", "Canyon", "Basin", "Hollow", "Dell",
            "Plain", "Steppe", "Field", "Meadow", "Dune", "Desert", "Moor", "Heath", "Marsh",
            "Bog", "Swamp", "Fen", "Mire", "Slope", "Rise", "Knoll", "Sinkhole"
        ];

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddCharacteristicTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCharacteristic();
                e.Handled = true;
            }
        }

        private void AddCharacteristic_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddCharacteristic();
        }

        private void AddCharacteristic()
        {
            string characteristic = AddCharacteristicTextBox.Text.ToLowerInvariant();

            if (!string.IsNullOrEmpty(characteristic) && !ViewModel.ObjectCharacteristicsList.Contains(characteristic))
            {
                ViewModel.ObjectCharacteristicsList.Add(characteristic);
            }

            AddCharacteristicTextBox.Text = string.Empty;
            AddCharacteristicTextBox.Focus();
        }

        private void RemoveCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is string characteristic)
            {
                ViewModel.ObjectCharacteristicsList.Remove(characteristic);
            }
        }
    }
}
