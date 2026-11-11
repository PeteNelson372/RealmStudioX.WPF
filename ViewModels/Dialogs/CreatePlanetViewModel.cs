using RealmStudioShapeRenderingLib;

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
    public class CreatePlanetViewModel : ViewModelBase
    {
        public BlackHoleViewModel PlanetBlackHoleModel { get; }
        public NebulaViewModel PlanetNebulaModel { get; }
        public CoronaViewModel PlanetCoronaModel { get; }
        public SunViewModel PlanetSunModel { get; }
        public GasGiantViewModel PlanetGasGiantModel { get; }
        public TerrestrialPlanetViewModel PlanetTerrestrialModel { get; }

        private readonly Action<CelestialBodyNode> _addPlanet;

        public CreatePlanetViewModel(Action<CelestialBodyNode> addPlanet)
        {
            _addPlanet = addPlanet
                ?? throw new ArgumentNullException(nameof(addPlanet));

            PlanetBlackHoleModel = new();
            PlanetNebulaModel = new();
            PlanetCoronaModel = new();
            PlanetSunModel = new();
            PlanetGasGiantModel = new();
            PlanetTerrestrialModel = new();
        }

        // These are the defaults used when creating a new planet.
        // Each new CelestialBodyNode receives its own cloned instance.
        private readonly CelestialBodyPlacementSettings _placement = new()
        {
            // Preserve the previous default composition while allowing the
            // three coordinates to be edited independently.
            Center = new Point3D(0.0, -1.44, -1.72)
        };

        private const double PositionLimit = 10.0;

        private Color _planetColor = Colors.White;
        public Color PlanetColor
        {
            get => _planetColor;
            set
            {
                if (SetProperty(ref _planetColor, value))
                {
                    SolidColorBrush brush = new(value);
                    brush.Freeze();
                    PlanetColorBrush = brush;
                }
            }
        }

        private Brush _planetColorBrush = new SolidColorBrush(Colors.White);
        public Brush PlanetColorBrush
        {
            get => _planetColorBrush;
            private set => SetProperty(ref _planetColorBrush, value);
        }

        public IReadOnlyList<CelestialBodyType> PlanetTypes { get; } = Enum.GetValues<CelestialBodyType>();

        private CelestialBodyType _selectedPlanetType = CelestialBodyType.TerrestrialPlanet;

        public CelestialBodyType SelectedPlanetType
        {
            get => _selectedPlanetType;
            set => SetProperty(ref _selectedPlanetType, value);
        }

        #region Placement Properties

        public double PlanetSize
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

        public double PlanetX
        {
            get => _placement.Center.X;
            set => SetCoordinate(value, _placement.Center.Y, _placement.Center.Z);
        }

        public double PlanetY
        {
            get => _placement.Center.Y;
            set => SetCoordinate(_placement.Center.X, value, _placement.Center.Z);
        }

        public double PlanetZ
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
            OnPropertyChanged(nameof(PlanetX));
            OnPropertyChanged(nameof(PlanetY));
            OnPropertyChanged(nameof(PlanetZ));
        }

        public WorldRotationDirection PlanetRotationDirection
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

        public double PlanetRotationRate
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

        public ICommand CreatePlanetCommand => new RelayCommand(() =>
        {
            CelestialBodyNode planet = CreatePlanetFromProperties();

            _addPlanet(planet);
        });

        #region Planet Creation

        public ICommand SelectPlanetTextureFileCommand => new RelayCommand(() =>
        {
            string texturePath = SelectTextureFile();

            PlanetTerrestrialModel.TexturePath = texturePath;
        });

        public ICommand SelectGasGiantTextureFileCommand => new RelayCommand(() =>
        {
            string texturePath = SelectTextureFile();

            PlanetGasGiantModel.TexturePath = texturePath;
        });

        private static string SelectTextureFile()
        {
            string textureDirectory = Path.Combine(
                ApplicationPreferences.Current.RootAssetDirectory,
                "Textures",
                "Planet");

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

        public ICommand SelectPlanetMapTextureCommand => new RelayCommand(() =>
        {
            string mapId = SelectMapForTexture();

            PlanetTerrestrialModel.MapIdentifier = mapId;
        });

        private static string SelectMapForTexture()
        {
            // TODO: create a dialog that allows the user to select a map from the available maps in the project

            return string.Empty;
        }


        /// <summary>
        /// Creates a complete celestial body node, including its
        /// independent placement settings.
        /// </summary>
        private CelestialBodyNode CreatePlanetFromProperties()
        {
            CelestialBody planet = new();

            CopyPropertiesToCelestialBody(planet);

            CelestialBodyPlacementSettings placement = _placement.Clone();

            return new CelestialBodyNode
            {
                Body = planet,
                Placement = placement
            };
        }

        #endregion

        #region Celestial Body Configuration

        private void CopyPropertiesToCelestialBody(CelestialBody planet)
        {
            CelestialBodyFactory.Configure(
                planet,
                SelectedPlanetType,
                _placement.RotationRate,
                _placement.RotationDirection,
                PlanetBlackHoleModel,
                PlanetNebulaModel,
                PlanetCoronaModel,
                PlanetSunModel,
                PlanetGasGiantModel,
                PlanetTerrestrialModel,
                baseColor: PlanetColor,
                description: $"Planet of type {SelectedPlanetType}.");
        }

        #endregion
    }
}
