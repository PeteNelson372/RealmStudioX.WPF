using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.Models.Startup;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class CreateOpenMapViewModel : ViewModelBase
    {
        private readonly string _mapsFolder;
        private readonly string _themesFolder;

        public ObservableCollection<string> Themes { get; } = [];
        public ObservableCollection<ProjectListEntry> ProjectListEntries { get; } = [];


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

        private ProjectListEntry? _selectedProjectEntry;
        public ProjectListEntry? SelectedProjectEntry
        {
            get => _selectedProjectEntry;
            set
            {
                if (SetProperty(ref _selectedProjectEntry, value))
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

        private RealmProjectType _selectedRealmType = RealmProjectType.World;

        public RealmProjectType SelectedRealmType
        {
            get => _selectedRealmType;
            set => _selectedRealmType = value;
        }

        private RealmMapType _selectedMapType = RealmMapType.World;

        public RealmMapType SelectedMapType
        {
            get => _selectedMapType;
            set => _selectedMapType = value;
        }


        public CreateOpenPackageResult? Result { get; set; }

        public ICommand CreateProjectCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool?>? RequestClose;

        public CreateOpenMapViewModel(string mapsFolder, string themesFolder)
        {
            _mapsFolder = mapsFolder;
            _themesFolder = themesFolder;

            CreateProjectCommand = new RelayCommand(Create);
            OpenCommand = new RelayCommand(Open, CanOpen);
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
            if (SelectedRealmType == RealmProjectType.NotSet && SelectedMapType != RealmMapType.NotSet)
            {
                // derive the realm type from the map type
                switch (SelectedMapType)
                {
                    case RealmMapType.World: SelectedRealmType = RealmProjectType.World; break;
                    case RealmMapType.Region: SelectedRealmType = RealmProjectType.Region; break;
                    case RealmMapType.City: SelectedRealmType = RealmProjectType.City; break;
                    case RealmMapType.InteriorFloor: SelectedRealmType = RealmProjectType.Interior; break;
                    case RealmMapType.DungeonLevel: SelectedRealmType = RealmProjectType.Dungeon; break;
                    case RealmMapType.ShipDeck: SelectedRealmType = RealmProjectType.Ship; break;
                    case RealmMapType.SolarSystemBody: SelectedRealmType = RealmProjectType.SolarSystem; break;
                    case RealmMapType.Other: SelectedRealmType = RealmProjectType.Other; break;
                }
            }

            if (SelectedRealmType != RealmProjectType.NotSet && SelectedMapType == RealmMapType.NotSet)
            {
                // derive the realm type from the map type
                switch (SelectedRealmType)
                {
                    case RealmProjectType.World: SelectedMapType = RealmMapType.World; break;
                    case RealmProjectType.Region: SelectedMapType = RealmMapType.Region; break;
                    case RealmProjectType.City: SelectedMapType = RealmMapType.City; break;
                    case RealmProjectType.Interior: SelectedMapType = RealmMapType.InteriorFloor; break;
                    case RealmProjectType.Dungeon: SelectedMapType = RealmMapType.DungeonLevel; break;
                    case RealmProjectType.Ship: SelectedMapType = RealmMapType.ShipDeck; break;
                    case RealmProjectType.SolarSystem: SelectedMapType = RealmMapType.SolarSystemBody; break;
                    case RealmProjectType.Other: SelectedMapType = RealmMapType.Other; break;
                }
            }

            Result = new CreateOpenPackageResult
            {
                CreationOperation = RealmCreationOperation.CreateProject,
                MapName = MapName,
                IsNew = true,
                ProjectType = SelectedRealmType,
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
        // OPEN
        // -------------------------
        private bool CanOpen()
        {
            return SelectedProjectEntry != null;
        }

        private void Open()
        {
            if (SelectedProjectEntry == null)
                return;

            Result = new CreateOpenPackageResult
            {
                CreationOperation = RealmCreationOperation.CreateProject,
                IsNew = false,
                Project = SelectedProjectEntry.Project
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
