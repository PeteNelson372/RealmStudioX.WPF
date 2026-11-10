using RealmStudioShapeRenderingLib;

using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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

        public CreatePlanetViewModel()
        {
            PlanetBlackHoleModel = new();
            PlanetNebulaModel = new();
            PlanetCoronaModel = new();
            PlanetSunModel = new();
            PlanetGasGiantModel = new();
            PlanetTerrestrialModel = new();
        }

        // These are the defaults used when creating a new planet.
        // Each new CelestialBodyNode receives its own cloned instance.
        private readonly CelestialBodyPlacementSettings _placement = new();

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

        public double PlanetDistance
        {
            get => _placement.Distance;
            set
            {
                double distance = Math.Clamp(value, 0.0, 1.0);

                if (_placement.Distance != distance)
                {
                    _placement.Distance = distance;
                    OnPropertyChanged();
                }
            }
        }

        public double PlanetOrbit
        {
            get => _placement.Orbit;
            set
            {
                double orbit = Math.Clamp(value, 0.0, 1.0);

                if (_placement.Orbit != orbit)
                {
                    _placement.Orbit = orbit;
                    OnPropertyChanged();
                }
            }
        }

        public double PlanetPlane
        {
            get => _placement.Plane;
            set
            {
                double plane = Math.Clamp(value, 0.0, 1.0);

                if (_placement.Plane != plane)
                {
                    _placement.Plane = plane;
                    OnPropertyChanged();
                }
            }
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

        /// <summary>
        /// The most recently created planet node.
        /// The scene coordinator can use this to add the node
        /// to the celestial hierarchy.
        /// </summary>
        private CelestialBodyNode? _createdPlanetNode;

        public CelestialBodyNode? CreatedPlanetNode
        {
            get => _createdPlanetNode;
            private set => SetProperty(ref _createdPlanetNode, value);
        }

        public ICommand CreatePlanetCommand => new RelayCommand(() =>
        {
            CreatedPlanetNode = CreatePlanetFromProperties();
        });

        #region Planet Creation

        private const double PlanetPositionScale = 6.0;
        private const double GlobeRadius = 1.0;
        private const double MinimumStarGap = 0.15;

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

            placement.Center = CalculatePlanetPosition(placement);

            return new CelestialBodyNode
            {
                Body = planet,
                Placement = placement
            };
        }

        /// <summary>
        /// Calculates the position of the planet relative to its
        /// parent celestial body.
        /// </summary>
        private static Point3D CalculatePlanetPosition(
            CelestialBodyPlacementSettings placement)
        {
            double distanceFromCenter =
                GlobeRadius +
                placement.Size +
                MinimumStarGap +
                placement.Distance * PlanetPositionScale;

            return new Point3D(
                -placement.Orbit * PlanetPositionScale,
                -placement.Plane * PlanetPositionScale,
                -distanceFromCenter);
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
                description: $"Planet of type {SelectedPlanetType}.");
        }

        #endregion
    }
}
