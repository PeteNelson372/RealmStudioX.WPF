using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Startup
{
    public class StartupViewModel : ViewModelBase
    {
        private readonly string _mapsFolder;
        private readonly string _themesFolder;

        public ObservableCollection<string> Themes { get; } = new();
        public ObservableCollection<string> Maps { get; } = new();


        private string _mapName = string.Empty;
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        private string? _selectedTheme;
        public string? SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        private string? _selectedMap;
        public string? SelectedMap
        {
            get => _selectedMap;
            set
            {
                if (SetProperty(ref _selectedMap, value))
                {
                    ((RelayCommand)OpenCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private int _width = 1920;
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private int _height = 1080;
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private float _areaWidth = 100;
        public float AreaWidth
        {
            get => _areaWidth;
            set => SetProperty(ref _areaWidth, value);
        }

        private float _areaHeight = 75;
        public float AreaHeight
        {
            get => _areaHeight;
            set => SetProperty(ref _areaHeight, value);
        }

        private string _mapAreaUnits = "feet";
        public string MapAreaUnits
        {
            get => _mapAreaUnits;
            set => SetProperty(ref _mapAreaUnits, value);
        }

        public StartupResult? Result { get; private set; }

        public ICommand CreateCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool?>? RequestClose;

        public StartupViewModel(string mapsFolder, string themesFolder)
        {
            _mapsFolder = mapsFolder;
            _themesFolder = themesFolder;

            CreateCommand = new RelayCommand(Create);
            OpenCommand = new RelayCommand(Open, CanOpen);
            CancelCommand = new RelayCommand(Cancel);

            LoadThemes();
            LoadMaps();
        }

        private void LoadMaps()
        {
            if (!Directory.Exists(_mapsFolder))
                return;

            foreach (var file in Directory.GetFiles(_mapsFolder, "*.rsm"))
            {
                Maps.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void LoadThemes()
        {
            if (!Directory.Exists(_themesFolder))
                return;

            var files = Directory.GetFiles(_themesFolder, "*.rstheme");

            foreach (var file in files)
            {
                Themes.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void Create()
        {
            Result = new StartupResult
            {
                IsNew = true,
                Width = Width,
                Height = Height,
                MapAreaUnits = MapAreaUnits,
                MapAreaWidth = AreaWidth,
                MapAreaHeight = AreaHeight,
                Theme = SelectedTheme
            };

            RequestClose?.Invoke(true);
        }

        // -------------------------
        // OPEN
        // -------------------------
        private bool CanOpen()
        {
            return SelectedMap != null;
        }

        private void Open()
        {
            if (SelectedMap == null)
                return;

            var file = Path.Combine(_mapsFolder, SelectedMap + ".rsm");

            Result = new StartupResult
            {
                IsNew = false,
                FilePath = file
            };

            RequestClose?.Invoke(true);
        }

        // -------------------------
        // CANCEL
        // -------------------------
        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(false);
        }
    }
}
