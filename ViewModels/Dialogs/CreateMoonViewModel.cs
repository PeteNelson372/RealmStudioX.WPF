using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Dialogs;

using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.IO;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class CreateMoonViewModel : ViewModelBase
    {
        public BlackHoleViewModel MoonBlackHoleModel { get; }
        public NebulaViewModel MoonNebulaModel { get; }
        public CoronaViewModel MoonCoronaModel { get; }
        public SunViewModel MoonSunModel { get; }
        public GasGiantViewModel MoonGasGiantModel { get; }
        public TerrestrialPlanetViewModel MoonTerrestrialModel { get; }

        private readonly Action<CelestialBodyNode, CelestialBodyNode> _addMoon;
        private readonly CelestialBodyNode _parentNode;
        private readonly MapObjectDescriptionService _descriptionService;
        private readonly Func<string?, string?> _selectProjectMap;
        private readonly ObjectCharacteristicsViewModel _moonCharacteristics = new();

        public CreateMoonViewModel(
            CelestialBodyNode parentNode,
            Action<CelestialBodyNode, CelestialBodyNode> addMoon,
            MapObjectDescriptionService descriptionService,
            Func<string?, string?> selectProjectMap)
        {
            _parentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));

            _addMoon = addMoon
                ?? throw new ArgumentNullException(nameof(addMoon));

            _descriptionService = descriptionService
                ?? throw new ArgumentNullException(nameof(descriptionService));

            _selectProjectMap = selectProjectMap
                ?? throw new ArgumentNullException(nameof(selectProjectMap));

            MoonBlackHoleModel = new();
            MoonNebulaModel = new();
            MoonCoronaModel = new();
            MoonSunModel = new();
            MoonGasGiantModel = new();
            MoonTerrestrialModel = new();
        }

        // These are the defaults used when creating a new moon.
        // Each new CelestialBodyNode receives its own cloned instance.
        private readonly CelestialBodyPlacementSettings _placement = new()
        {
            // Preserve the previous default composition while allowing the
            // three coordinates to be edited independently.
            Center = new Point3D(0.0, -0.35, -0.45),
            Size = 0.08
        };

        private const double PositionLimit = 10.0;

        private Color _moonColor = Colors.White;
        public Color MoonColor
        {
            get => _moonColor;
            set
            {
                if (SetProperty(ref _moonColor, value))
                {
                    SolidColorBrush brush = new(value);
                    brush.Freeze();
                    MoonColorBrush = brush;
                }
            }
        }

        private Brush _moonColorBrush = new SolidColorBrush(Colors.White);
        public Brush MoonColorBrush
        {
            get => _moonColorBrush;
            private set => SetProperty(ref _moonColorBrush, value);
        }

        public IReadOnlyList<CelestialBodyType> MoonTypes { get; } = Enum.GetValues<CelestialBodyType>();

        private CelestialBodyType _selectedMoonType = CelestialBodyType.TerrestrialPlanet;

        public CelestialBodyType SelectedMoonType
        {
            get => _selectedMoonType;
            set => SetProperty(ref _selectedMoonType, value);
        }

        #region Name and Description

        private string _moonName = "New Moon";

        public string MoonName
        {
            get => _moonName;
            set => SetProperty(ref _moonName, value ?? string.Empty);
        }

        private string _moonDescription = string.Empty;

        public string MoonDescription
        {
            get => _moonDescription;
            set => SetProperty(ref _moonDescription, value ?? string.Empty);
        }

        private bool _moonNameLocked;

        public bool MoonNameLocked
        {
            get => _moonNameLocked;
            set => SetProperty(ref _moonNameLocked, value);
        }

        public ICommand GenerateMoonNameCommand => new RelayCommand(() =>
        {
            if (!MoonNameLocked)
            {
                MoonName = MainWindowViewModel.GenerateName();
            }
        });

        public ICommand LockMoonNameCommand => new RelayCommand(() =>
        {
            MoonNameLocked = !MoonNameLocked;
        });

        public ICommand GenerateMoonDescriptionCommand => new RelayCommand(async () =>
        {
            try
            {
                string query = _descriptionService.BuildAiQuery(
                    "CelestialBody",
                    MoonName,
                    _moonCharacteristics.SelectedObjectType,
                    [.. _moonCharacteristics.ObjectCharacteristicsList]);

                _descriptionService.ClearDescription();
                await _descriptionService.GetMapObjectDescription(query);

                string description = _descriptionService.ObjectDescription;

                if (!string.IsNullOrWhiteSpace(description))
                {
                    MoonDescription = description;
                }
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception(
                    "An error occurred retrieving a moon description.", ex);

                MessageDialogFactory.ErrorDialog(
                    "Error retrieving moon description.", ex.Message);
            }
        });

        public ICommand SetMoonCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics dialog = new(_moonCharacteristics, MapObjectType.CelestialBody);

            dialog.ShowDialog();
        });

        public ICommand CreateMoonArticleCommand => new RelayCommand(() =>
        {
            MessageDialogFactory.InformationDialog(
                "Create Moon Article",
                "This feature is not yet implemented. It will allow you to create a World Anvil article for the moon.");
        });

        #endregion

        #region Placement Properties

        public double MoonSize
        {
            get => _placement.Size;
            set
            {
                double size = Math.Clamp(value, 0.01, 1.0);

                if (_placement.Size != size)
                {
                    _placement.Size = size;
                    OnPropertyChanged();
                }
            }
        }

        public double MoonX
        {
            get => _placement.Center.X;
            set => SetCoordinate(value, _placement.Center.Y, _placement.Center.Z);
        }

        public double MoonY
        {
            get => _placement.Center.Y;
            set => SetCoordinate(_placement.Center.X, value, _placement.Center.Z);
        }

        public double MoonZ
        {
            get => _placement.Center.Z;
            set => SetCoordinate(_placement.Center.X, _placement.Center.Y, value);
        }

        private void SetCoordinate(double x, double y, double z)
        {
            Point3D center = new(
                Math.Clamp(double.IsFinite(x) ? x : 0.0, -PositionLimit, PositionLimit),
                Math.Clamp(double.IsFinite(y) ? y : 0.0, -PositionLimit, PositionLimit),
                Math.Clamp(double.IsFinite(z) ? z : 0.0, -PositionLimit, PositionLimit));

            if (_placement.Center == center)
                return;

            _placement.Center = center;
            OnPropertyChanged(nameof(MoonX));
            OnPropertyChanged(nameof(MoonY));
            OnPropertyChanged(nameof(MoonZ));
        }

        public WorldRotationDirection MoonRotationDirection
        {
            get => _placement.RotationDirection;
            set
            {
                if (_placement.RotationDirection != value)
                {
                    _placement.RotationDirection = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MoonRotationRate
        {
            get => _placement.RotationRate;
            set
            {
                double rate = double.IsFinite(value)
                    ? Math.Clamp(value, 0.0, 10.0)
                    : 1.0;

                if (_placement.RotationRate != rate)
                {
                    _placement.RotationRate = rate;
                    OnPropertyChanged();
                }
            }
        }

        private const double MinimumAxialTilt = 0.0;
        private const double MaximumAxialTilt = 90.0;

        public double AxialTilt
        {
            get => _placement.AxialTilt;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, MinimumAxialTilt, MaximumAxialTilt)
                    : 0.0;

                if (_placement.AxialTilt != value)
                {
                    _placement.AxialTilt = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public ICommand CreateMoonCommand => new RelayCommand(() =>
        {
            CelestialBodyNode moon = CreateMoonFromProperties();

            _addMoon(_parentNode, moon);
        });

        #region Moon Creation

        public ICommand SelectMoonTextureFileCommand => new RelayCommand(() =>
        {
            string texturePath = SelectTextureFile();

            // Cancellation must leave the current selection untouched.
            if (string.IsNullOrWhiteSpace(texturePath))
                return;

            MoonTerrestrialModel.MapIdentifier = string.Empty;
            MoonTerrestrialModel.TexturePath = texturePath;
        });

        public ICommand SelectGasGiantTextureFileCommand => new RelayCommand(() =>
        {
            string texturePath = SelectTextureFile();

            if (string.IsNullOrWhiteSpace(texturePath))
                return;

            MoonGasGiantModel.TexturePath = texturePath;
        });

        private static string SelectTextureFile()
        {
            string textureDirectory = Path.Combine(
                ApplicationPreferences.Current.RootAssetDirectory,
                "Textures",
                "Moon");

            OpenFileDialog dialog = new()
            {
                Title = "Select Gas Giant Texture",
                Filter = UserInterfaceUtilities.GetCommonImageFilter(),
                Multiselect = false,
                InitialDirectory = Directory.Exists(textureDirectory)
                    ? textureDirectory
                    : ApplicationPreferences.Current.RootAssetDirectory,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        public ICommand SelectMoonMapTextureCommand => new RelayCommand(() =>
        {
            string? mapId = _selectProjectMap(
                MoonTerrestrialModel.MapIdentifier);

            // A cancelled dialog returns null. Do not erase an existing
            // bitmap or project-map selection in that case.
            if (string.IsNullOrWhiteSpace(mapId))
                return;

            MoonTerrestrialModel.TexturePath = string.Empty;
            MoonTerrestrialModel.MapIdentifier = mapId;
        });


        /// <summary>
        /// Creates a complete celestial body node, including its
        /// independent placement settings.
        /// </summary>
        private CelestialBodyNode CreateMoonFromProperties()
        {
            CelestialBody moon = new();

            CopyPropertiesToCelestialBody(moon);

            CelestialBodyPlacementSettings placement = _placement.Clone();

            return new CelestialBodyNode
            {
                Body = moon,
                Placement = placement
            };
        }

        #endregion

        #region Celestial Body Configuration

        private void CopyPropertiesToCelestialBody(CelestialBody moon)
        {
            CelestialBodyFactory.Configure(
                moon,
                SelectedMoonType,
                _placement.RotationRate,
                _placement.RotationDirection,
                MoonBlackHoleModel,
                MoonNebulaModel,
                MoonCoronaModel,
                MoonSunModel,
                MoonGasGiantModel,
                MoonTerrestrialModel,
                baseColor: MoonColor,
                description: MoonDescription);

            // Apply user-defined metadata after factory configuration.
            moon.Name = MoonName;
            moon.Description = MoonDescription;
        }

        #endregion
    }
}
