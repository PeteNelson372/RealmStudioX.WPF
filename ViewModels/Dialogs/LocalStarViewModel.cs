using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.Core.Celestial;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Celestial;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Dialogs
{
    public class LocalStarViewModel(WorldGlobeViewer globeViewer,
        BlackHoleViewModel blackHoleModel,
        NebulaViewModel nebulaModel,
        CoronaViewModel coronaModel,
        SunViewModel sunModel,
        GasGiantViewModel gasGiantModel,
        TerrestrialPlanetViewModel terrestrialPlanetModel) : ViewModelBase
    {
        private WorldGlobeViewer _globeViewer = globeViewer;

        private BlackHoleViewModel _blackHoleModel = blackHoleModel;

        private NebulaViewModel _nebulaModel = nebulaModel;

        private CoronaViewModel _coronaModel = coronaModel;

        private SunViewModel _sunModel = sunModel;

        private GasGiantViewModel _gasGiantModel = gasGiantModel;

        private TerrestrialPlanetViewModel _terrestrialPlanetModel = terrestrialPlanetModel;

        public IReadOnlyList<CelestialBodyType> LocalStarTypes { get; } =
            [
                CelestialBodyType.Sun,
                CelestialBodyType.Corona,
                CelestialBodyType.BlackHole,
                CelestialBodyType.Nebula,
                CelestialBodyType.GasGiant,
                CelestialBodyType.TerrestrialPlanet
            ];

        private CelestialBody? _currentLocalStar;
        private bool _isInitializing;

        private bool _enableLocalStar = true;

        public bool EnableLocalStar
        {
            get => _enableLocalStar;
            set
            {
                if (SetProperty(ref _enableLocalStar, value))
                {
                    if (_enableLocalStar && _currentLocalStar != null)
                    {
                        _globeViewer.ShowLocalStar(_currentLocalStar, CalculateWorldPosition(), LocalStarSize);
                    }
                    else
                    {
                        _globeViewer.RemoveLocalStar();
                    }
                }
            }
        }


        private string _localStarName = "Local Star";

        public string LocalStarName
        {
            get => _localStarName;
            set
            {
                if (SetProperty(ref _localStarName, value ?? string.Empty))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private string _localStarDescription = string.Empty;

        public string LocalStarDescription
        {
            get => _localStarDescription;
            set
            {
                if (SetProperty(ref _localStarDescription, value ?? string.Empty))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private bool _localStarNameLocked;

        public bool LocalStarNameLocked
        {
            get => _localStarNameLocked;
            set => SetProperty(ref _localStarNameLocked, value);
        }


        private CelestialBodyType _selectedLocalStarType = CelestialBodyType.Sun;

        public CelestialBodyType SelectedLocalStarType
        {
            get => _selectedLocalStarType;
            set
            {
                if (SetProperty(ref _selectedLocalStarType, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarSize = 0.20;

        public double LocalStarSize
        {
            get => _localStarSize;
            set
            {
                double size = Math.Clamp(value, 0.01, 1.0);

                if (SetProperty(ref _localStarSize, size))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private const double PositionLimitXY = 10.0;
        private const double PositionLimitZ = 100.0;

        private double _localStarX = -2.0;
        public double LocalStarX
        {
            get => _localStarX;
            set => SetPosition(value, LocalStarY, LocalStarZ);
        }

        private double _localStarY = 0;
        public double LocalStarY
        {
            get => _localStarY;
            set => SetPosition(LocalStarX, value, LocalStarZ);
        }

        private double _localStarZ = -1.63;
        public double LocalStarZ
        {
            get => _localStarZ;
            set => SetPosition(LocalStarX, LocalStarY, value);
        }

        private void SetPosition(double x, double y, double z)
        {
            x = Math.Clamp(double.IsFinite(x) ? x : 0.0,
                -PositionLimitXY, PositionLimitXY);

            y = Math.Clamp(double.IsFinite(y) ? y : 0.0,
                -PositionLimitXY, PositionLimitXY);

            z = Math.Clamp(double.IsFinite(z) ? z : 0.0,
                -PositionLimitZ, PositionLimitZ);

            bool changedX = _localStarX != x;
            bool changedY = _localStarY != y;
            bool changedZ = _localStarZ != z;
            if (!changedX && !changedY && !changedZ)
                return;

            _localStarX = x;
            _localStarY = y;
            _localStarZ = z;

            if (changedX) OnPropertyChanged(nameof(LocalStarX));
            if (changedY) OnPropertyChanged(nameof(LocalStarY));
            if (changedZ) OnPropertyChanged(nameof(LocalStarZ));

            UpdateCurrentLocalStar();
        }

        private WorldRotationDirection _localStarRotationDirection = WorldRotationDirection.Prograde;

        public WorldRotationDirection LocalStarRotationDirection
        {
            get => _localStarRotationDirection;
            set
            {
                if (SetProperty(ref _localStarRotationDirection, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private double _localStarRotationRate = 1.0;

        public double LocalStarRotationRate
        {
            get => _localStarRotationRate;
            set
            {
                value = double.IsFinite(value) ? Math.Clamp(value, 0.0, 10.0) : 1.0;

                if (SetProperty(ref _localStarRotationRate, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        private const double MinimumAxialTilt = 0.0;
        private const double MaximumAxialTilt = 90.0;

        private double _localStarAxialTilt = 0.0;

        public double LocalStarAxialTilt
        {
            get => _localStarAxialTilt;
            set
            {
                value = double.IsFinite(value)
                    ? Math.Clamp(value, MinimumAxialTilt, MaximumAxialTilt)
                    : 0.0;

                if (SetProperty(ref _localStarAxialTilt, value))
                {
                    UpdateCurrentLocalStar();
                }
            }
        }

        public ICommand CreateLocalStarCommand => new RelayCommand(() =>
        {
            _currentLocalStar = CreateLocalStarFromProperties();

            if (EnableLocalStar)
            {
                _globeViewer.ShowLocalStar(_currentLocalStar, CalculateWorldPosition(), LocalStarSize);
            }
        });


        public ICommand GenerateLocalStarNameCommand => new RelayCommand(() =>
        {
            if (!LocalStarNameLocked)
            {
                LocalStarName = MainWindowViewModel.GenerateName();
            }
        });

        public ICommand LockLocalStarNameCommand => new RelayCommand(() =>
        {
            LocalStarNameLocked = !LocalStarNameLocked;
        });

        public ICommand GenerateLocalStarDescriptionCommand => new RelayCommand(async () =>
        {
            MapObjectDescriptionService descriptionService = _globeViewer.ViewModel.MainViewModel.MapObjectDescriptionService;

            if (localStarCharacteristics != null)
            {
                string query = descriptionService.BuildAiQuery("CelestialBody",
                    LocalStarName,
                    localStarCharacteristics.SelectedObjectType,
                    [.. localStarCharacteristics.ObjectCharacteristicsList]);

                try
                {
                    descriptionService.ClearDescription();
                    await descriptionService.GetMapObjectDescription(query);
                    string description = descriptionService.ObjectDescription;

                    if (!string.IsNullOrEmpty(description))
                    {
                        LocalStarDescription = description;
                    }
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception("An error occurred retrieving an object description.", ex);
                    MessageDialog dlg = MessageDialogFactory.ErrorDialog("Error retrieving local star description.", ex.Message);
                }
            }

        });

        private readonly ObjectCharacteristicsViewModel localStarCharacteristics = new();

        public ICommand SetLocalStarCharacteristicsCommand => new RelayCommand(() =>
        {
            ObjectCharacteristics localStarCharacteristicsDlg = new(localStarCharacteristics, MapObjectType.CelestialBody);
            localStarCharacteristicsDlg.ShowDialog();
        });

        public ICommand CreateLocalStarArticleCommand => new RelayCommand(() =>
        {
            // TODO: Connect to World Anvil article creation and upload functionality.
            MessageDialog dlg = MessageDialogFactory.InformationDialog(
                "Create Local Star Article",
                "This feature is not yet implemented. It will allow you to create a World Anvil article for the local star.");
        });


        private Color _starlightColor = Colors.Yellow;

        public Color StarlightColor
        {
            get => _starlightColor;
            set
            {
                if (SetProperty(ref _starlightColor, value))
                {
                    SolidColorBrush brush = new(value);
                    brush.Freeze();

                    StarlightColorBrush = brush;

                    UpdateCurrentLocalStar();
                }
            }
        }

        private Brush _starlightColorBrush = new SolidColorBrush(Colors.Yellow);

        public Brush StarlightColorBrush
        {
            get => _starlightColorBrush;
            private set => SetProperty(ref _starlightColorBrush, value);
        }

        private Point3D CalculateWorldPosition() =>
            new(-LocalStarX, -LocalStarY, -LocalStarZ);

        private CelestialBody CreateLocalStarFromProperties()
        {
            CelestialBody star = new();
            CopyPropertiesToCelestialBody(star);
            return star;
        }

        public void UpdateCurrentLocalStar()
        {
            if (_isInitializing || _currentLocalStar is null)
                return;

            CelestialBodyRendererType previousType = _currentLocalStar.RendererType;

            CopyPropertiesToCelestialBody(_currentLocalStar);

            // Keep the model current while hidden, but do not recreate the
            // Local Star in the viewer until the user enables it again.
            if (!EnableLocalStar)
                return;

            Point3D worldPosition = CalculateWorldPosition();

            if (previousType != _currentLocalStar.RendererType)
            {
                _globeViewer.ShowLocalStar(
                    _currentLocalStar,
                    worldPosition,
                    LocalStarSize);
            }
            else
            {
                _globeViewer.UpdateLocalStar(
                    _currentLocalStar,
                    worldPosition,
                    LocalStarSize);
            }
        }

        private void CopyPropertiesToCelestialBody(CelestialBody star)
        {
            CelestialBodyFactory.Configure(
                star,
                SelectedLocalStarType,
                LocalStarRotationRate,
                LocalStarRotationDirection,
                _blackHoleModel,
                _nebulaModel,
                _coronaModel,
                _sunModel,
                _gasGiantModel,
                _terrestrialPlanetModel,
                StarlightColor,
                $"Local star of type {SelectedLocalStarType}.");

            // Preserve user-defined metadata after factory configuration.
            star.Name = LocalStarName;
            star.Description = LocalStarDescription;
        }

        public void RestoreFromNode(
            CelestialBodyNode? localStarNode,
            CelestialBodyNode worldNode)
        {
            ArgumentNullException.ThrowIfNull(worldNode);

            _isInitializing = true;

            try
            {
                if (localStarNode is null)
                {
                    _currentLocalStar = null;
                    _enableLocalStar = false;

                    OnPropertyChanged(nameof(EnableLocalStar));
                    return;
                }

                ArgumentNullException.ThrowIfNull(localStarNode.Body);
                ArgumentNullException.ThrowIfNull(localStarNode.Placement);

                CelestialBody body = localStarNode.Body;
                CelestialBodyPlacementSettings placement = localStarNode.Placement;

                _currentLocalStar = body;

                // Common body properties.
                LocalStarName = body.Name ?? string.Empty;
                LocalStarDescription = body.Description ?? string.Empty;
                SelectedLocalStarType = body.BodyType;
                StarlightColor = body.BaseColor;

                // Placement properties.
                LocalStarSize = placement.Size;
                LocalStarAxialTilt = placement.AxialTilt;

                // The World Globe child is moved to the Local Star's position.
                // The Local Star UI uses the opposite sign convention.
                LocalStarX = -worldNode.Placement.Center.X;
                LocalStarY = -worldNode.Placement.Center.Y;
                LocalStarZ = -worldNode.Placement.Center.Z;

                // The Local Star's basic rotation is stored on its body animation.
                CelestialBodyAnimation? rotation =
                    body.Animations.FirstOrDefault(
                        animation =>
                            animation.AnimationType ==
                            CelestialBodyAnimationType.BasicRotation);

                if (rotation?.Settings is BasicRotationSettings rotationSettings)
                {
                    LocalStarRotationRate = rotationSettings.RotationRate;
                    LocalStarRotationDirection = rotationSettings.RotationDirection;
                }
                else
                {
                    // Retain placement defaults if the body has no BasicRotation.
                    LocalStarRotationRate = placement.RotationRate;
                    LocalStarRotationDirection = placement.RotationDirection;
                }

                InitializePointLightSettings(body);
                InitializeTextureSettings(body);
                InitializeAnimationSettings(body);
                InitializeRingSettings(body);

                // Set the backing field directly. The public setter would call
                // ShowLocalStar(), which would replace the node we just restored.
                _enableLocalStar = true;
                OnPropertyChanged(nameof(EnableLocalStar));
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void InitializePointLightSettings(CelestialBody body)
        {
            CelestialPointLightSettings settings =
                body.PointLightSettings ?? new CelestialPointLightSettings();

            switch (body.BodyType)
            {
                case CelestialBodyType.Sun:
                    _sunModel.EnablePointLight = settings.Enabled;
                    _sunModel.PointLightIntensity = settings.Intensity;
                    _sunModel.PointLightRange = settings.Range;
                    break;

                case CelestialBodyType.Corona:
                    _coronaModel.EnablePointLight = settings.Enabled;
                    _coronaModel.PointLightIntensity = settings.Intensity;
                    _coronaModel.PointLightRange = settings.Range;
                    break;

                case CelestialBodyType.Nebula:
                    _nebulaModel.EnablePointLight = settings.Enabled;
                    _nebulaModel.PointLightIntensity = settings.Intensity;
                    _nebulaModel.PointLightRange = settings.Range;
                    break;
            }
        }

        private void InitializeTextureSettings(CelestialBody body)
        {
            CelestialTextureSource? texture = body.TextureSource;

            if (texture is null)
                return;

            switch (body.BodyType)
            {
                case CelestialBodyType.GasGiant:
                    if (texture.SourceType == CelestialTextureSourceType.BitmapFile)
                        _gasGiantModel.TexturePath = texture.FilePath ?? string.Empty;
                    break;

                case CelestialBodyType.TerrestrialPlanet:
                    switch (texture.SourceType)
                    {
                        case CelestialTextureSourceType.BitmapFile:
                            _terrestrialPlanetModel.MapIdentifier = string.Empty;
                            _terrestrialPlanetModel.TexturePath =
                                texture.FilePath ?? string.Empty;
                            break;

                        case CelestialTextureSourceType.RealmMap:
                            _terrestrialPlanetModel.TexturePath = string.Empty;
                            _terrestrialPlanetModel.MapIdentifier =
                                texture.MapId ?? string.Empty;
                            break;
                    }
                    break;
            }
        }

        private void InitializeAnimationSettings(CelestialBody body)
        {
            switch (body.BodyType)
            {
                case CelestialBodyType.Sun:
                    InitializeSunSettings(body);
                    break;

                case CelestialBodyType.Corona:
                    InitializeCoronaSettings(body);
                    break;

                case CelestialBodyType.BlackHole:
                    InitializeBlackHoleSettings(body);
                    break;

                case CelestialBodyType.Nebula:
                    InitializeNebulaSettings(body);
                    break;

                case CelestialBodyType.GasGiant:
                    InitializeGasGiantSettings(body);
                    break;
            }
        }

        private void InitializeSunSettings(CelestialBody body)
        {
            SunAnimationSettings? settings =
                GetAnimationSettings<SunAnimationSettings>(
                    body,
                    CelestialBodyAnimationType.SolarActivity);

            if (settings is null)
                return;

            _sunModel.EnableProminences = settings.EnableProminences;
            _sunModel.EnableFlares = settings.EnableFlares;
            _sunModel.ActivityIntensity = settings.ActivityIntensity;
            _sunModel.ActivityFrequency = settings.ActivityFrequency;
            _sunModel.ActivityGeometryScale = settings.ActivityGeometryScale;
            _sunModel.ActivityThicknessScale = settings.ActivityThicknessScale;
            _sunModel.EnableCorona = settings.EnableCorona;
            _sunModel.CoronaOpacity = settings.CoronaOpacity;
            _sunModel.CoronaRotationSpeed = settings.CoronaRotationSpeed;
        }

        private void InitializeCoronaSettings(CelestialBody body)
        {
            CoronaAnimationSettings? settings =
                GetAnimationSettings<CoronaAnimationSettings>(
                    body,
                    CelestialBodyAnimationType.SolarActivity);

            if (settings is null)
                return;

            _coronaModel.EnableProminences = settings.EnableProminences;
            _coronaModel.EnableFlares = settings.EnableFlares;
            _coronaModel.ActivityIntensity = settings.ActivityIntensity;
            _coronaModel.ActivityFrequency = settings.ActivityFrequency;
            _coronaModel.ActivityGeometryScale = settings.ActivityGeometryScale;
            _coronaModel.ActivityThicknessScale = settings.ActivityThicknessScale;
        }

        private void InitializeBlackHoleSettings(CelestialBody body)
        {
            BlackHoleAnimationSettings? settings =
                GetAnimationSettings<BlackHoleAnimationSettings>(
                    body,
                    CelestialBodyAnimationType.BlackHoleActivity);

            if (settings is null)
                return;

            _blackHoleModel.EnableJets = settings.EnableJets;
            _blackHoleModel.JetLength = settings.JetLength;
            _blackHoleModel.JetPulseRate = settings.JetPulseRate;
            _blackHoleModel.EnableJetParticles = settings.EnableJetParticles;
            _blackHoleModel.JetParticleSpeed = settings.JetParticleSpeed;
            _blackHoleModel.JetParticleEmissionRate = settings.JetParticleEmissionRate;
            _blackHoleModel.JetParticleLifetime = settings.JetParticleLifetime;
            _blackHoleModel.JetParticleDispersion = settings.JetParticleDispersion;
            _blackHoleModel.EnableDiskParticles = settings.EnableDiskParticles;
            _blackHoleModel.DiskParticleEmissionRate = settings.DiskParticleEmissionRate;
            _blackHoleModel.DiskParticleLifetime = settings.DiskParticleLifetime;
            _blackHoleModel.DiskParticleOutflowSpeed = settings.DiskParticleOutflowSpeed;
        }

        private void InitializeNebulaSettings(CelestialBody body)
        {
            NebulaAnimationSettings? settings =
                GetAnimationSettings<NebulaAnimationSettings>(
                    body,
                    CelestialBodyAnimationType.NebulaActivity);

            if (settings is null)
                return;

            _nebulaModel.LayerCount = settings.LayerCount;
            _nebulaModel.Width = settings.Width;
            _nebulaModel.Height = settings.Height;
            _nebulaModel.Depth = settings.Depth;
            _nebulaModel.Opacity = settings.Opacity;
            _nebulaModel.Brightness = settings.Brightness;
            _nebulaModel.DetailIntensity = settings.DetailIntensity;
            _nebulaModel.RotationSpeed = settings.RotationSpeed;
            _nebulaModel.DriftSpeed = settings.DriftSpeed;
        }

        private void InitializeGasGiantSettings(CelestialBody body)
        {
            GasGiantAnimationSettings? settings =
                GetAnimationSettings<GasGiantAnimationSettings>(
                    body,
                    CelestialBodyAnimationType.AtmosphereTurbulenceActivity);

            if (settings is null)
                return;

            _gasGiantModel.EnableAtmosphericTurbulence =
                settings.EnableAtmosphericTurbulence;

            _gasGiantModel.TurbulenceIntensity =
                settings.TurbulenceIntensity;
        }

        private static TSettings? GetAnimationSettings<TSettings>(
            CelestialBody body,
            CelestialBodyAnimationType animationType)
            where TSettings : class
        {
            return body.Animations
                .FirstOrDefault(animation =>
                    animation.AnimationType == animationType)
                ?.Settings as TSettings;
        }

        private void InitializeRingSettings(CelestialBody body)
        {
            PlanetaryRingViewModel? rings = body.BodyType switch
            {
                CelestialBodyType.GasGiant => _gasGiantModel.Rings,
                CelestialBodyType.TerrestrialPlanet => _terrestrialPlanetModel.Rings,
                _ => null
            };

            if (rings is null)
                return;

            CelestialBodyAttachment? attachment =
                body.Attachments.FirstOrDefault(
                    item => item.Type == CelestialBodyAttachmentType.PlanetaryRings);

            if (attachment?.AttachmentSettings is not PlanetaryRingSettings settings)
            {
                rings.Enabled = false;
                return;
            }

            rings.Enabled = settings.Enabled && attachment.Enabled;
            rings.TexturePath = settings.TexturePath;
            rings.InnerRadiusScale = settings.InnerRadiusScale;
            rings.OuterRadiusScale = settings.OuterRadiusScale;
            rings.Inclination = settings.Inclination;

            CelestialBodyAnimation? rotation =
                attachment.Animations.FirstOrDefault(
                    item => item.AnimationType == CelestialBodyAnimationType.BasicRotation);

            if (rotation?.Settings is BasicRotationSettings rotationSettings)
            {
                rings.EnableRotation = rotation.Enabled;
                rings.RotationRate = rotationSettings.RotationRate;
                rings.RotationDirection = rotationSettings.RotationDirection;
            }

            CelestialBodyAnimation? turbulence =
                attachment.Animations.FirstOrDefault(
                    item => item.AnimationType == CelestialBodyAnimationType.RingTurbulenceActivity);

            if (turbulence?.Settings is RingTurbulenceSettings turbulenceSettings)
            {
                rings.EnableTurbulence = turbulence.Enabled;
                rings.TurbulenceIntensity = turbulenceSettings.Intensity;
            }
        }

    }
}
