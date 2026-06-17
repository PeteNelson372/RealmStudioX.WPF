using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.CreateOpenMap
{
    public class CreateMapViewModel : ViewModelBase
    {
        private readonly string _mapsFolder;
        private readonly string _themesFolder;

        public ObservableCollection<string> Themes { get; } = [];


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

        private float _aspectRatio = (float)1920 / 1080;
        public float AspectRatio
        {
            get => _aspectRatio;
            set => SetProperty(ref _aspectRatio, value);
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

        private string _mapAreaUnits = "Miles";
        public string MapAreaUnits
        {
            get => _mapAreaUnits;
            set => SetProperty(ref _mapAreaUnits, value);
        }

        private RealmMapType _selectedMapType = RealmMapType.World;

        public RealmMapType SelectedMapType
        {
            get => _selectedMapType;
            set => _selectedMapType = value;
        }


        public CreateOpenPackageResult? Result { get; private set; }

        public ICommand CreateProjectCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool?>? RequestClose;

        public CreateMapViewModel(string mapsFolder, string themesFolder)
        {
            _mapsFolder = mapsFolder;
            _themesFolder = themesFolder;

            CreateProjectCommand = new RelayCommand(Create);
            CancelCommand = new RelayCommand(Cancel);

            LoadThemes();
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
            Result = new CreateOpenPackageResult
            {
                CreationOperation = RealmCreationOperation.CreateMap,
                MapName = MapName,
                IsNew = true,
                MapType = SelectedMapType,
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
        // CANCEL
        // -------------------------
        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(false);
        }
    }
}
