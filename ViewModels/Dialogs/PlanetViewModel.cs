
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
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

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class PlanetViewModel : ViewModelBase
{
    private const double PositionLimit = 10.0;

    private readonly CelestialBodyNode _planetNode;
    private readonly Action<CelestialBodyNode> _updatePlanet;
    private readonly Action<CelestialBodyNode> _removePlanet;
    private readonly MapObjectDescriptionService _descriptionService;
    private readonly ObjectCharacteristicsViewModel _planetCharacteristics = new();

    private string _planetName = string.Empty;
    private string _planetDescription = string.Empty;
    private bool _planetNameLocked;

    private bool _isInitializing = true;

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
                ConfigureAndUpdatePlanet();
            }
        }
    }

    private Brush _planetColorBrush = new SolidColorBrush(Colors.White);
    public Brush PlanetColorBrush
    {
        get => _planetColorBrush;
        private set => SetProperty(ref _planetColorBrush, value);
    }

    public BlackHoleViewModel PlanetBlackHoleModel { get; }
    public NebulaViewModel PlanetNebulaModel { get; }
    public CoronaViewModel PlanetCoronaModel { get; }
    public SunViewModel PlanetSunModel { get; }
    public GasGiantViewModel PlanetGasGiantModel { get; }
    public TerrestrialPlanetViewModel PlanetTerrestrialModel { get; }

    public IReadOnlyList<CelestialBodyType> PlanetTypes { get; } =
        Enum.GetValues<CelestialBodyType>();

    private CelestialBodyType _selectedPlanetType;

    public CelestialBodyType SelectedPlanetType
    {
        get => _selectedPlanetType;
        set
        {
            if (SetProperty(ref _selectedPlanetType, value))
                ConfigureAndUpdatePlanet();
        }
    }

    #region Name and Description

    public string PlanetName
    {
        get => _planetName;
        set
        {
            if (SetProperty(ref _planetName, value ?? string.Empty))
            {
                _planetNode.Body.Name = _planetName;
                _updatePlanet(_planetNode);
            }
        }
    }

    public string PlanetDescription
    {
        get => _planetDescription;
        set
        {
            if (SetProperty(ref _planetDescription, value ?? string.Empty))
            {
                _planetNode.Body.Description = _planetDescription;
                _updatePlanet(_planetNode);
            }
        }
    }

    public bool PlanetNameLocked
    {
        get => _planetNameLocked;
        set => SetProperty(ref _planetNameLocked, value);
    }

    public ICommand GeneratePlanetNameCommand => new RelayCommand(() =>
    {
        if (!PlanetNameLocked)
        {
            PlanetName = MainWindowViewModel.GenerateName();
        }
    });

    public ICommand LockPlanetNameCommand => new RelayCommand(() =>
    {
        PlanetNameLocked = !PlanetNameLocked;
    });

    public ICommand GeneratePlanetDescriptionCommand => new RelayCommand(async () =>
    {
        try
        {
            string query = _descriptionService.BuildAiQuery(
                "CelestialBody",
                PlanetName,
                _planetCharacteristics.SelectedObjectType,
                [.. _planetCharacteristics.ObjectCharacteristicsList]);

            _descriptionService.ClearDescription();
            await _descriptionService.GetMapObjectDescription(query);

            string description = _descriptionService.ObjectDescription;

            if (!string.IsNullOrWhiteSpace(description))
            {
                PlanetDescription = description;
            }
        }
        catch (Exception ex)
        {
            RealmStudioXLogger.Exception(
                "An error occurred retrieving a planet description.", ex);

            MessageDialogFactory.ErrorDialog(
                "Error retrieving planet description.", ex.Message);
        }
    });

    public ICommand SetPlanetCharacteristicsCommand => new RelayCommand(() =>
    {
        ObjectCharacteristics dialog = new(
            _planetCharacteristics,
            MapObjectType.CelestialBody);

        dialog.ShowDialog();
    });

    public ICommand CreatePlanetArticleCommand => new RelayCommand(() =>
    {
        MessageDialogFactory.InformationDialog(
            "Create Planet Article",
            "This feature is not yet implemented. It will allow you to create a World Anvil article for the planet.");
    });

    #endregion

    public PlanetViewModel(
        CelestialBodyNode planetNode,
        Action<CelestialBodyNode> updatePlanet,
        Action<CelestialBodyNode> removePlanet,
        MapObjectDescriptionService descriptionService)
    {
        _planetNode = planetNode
            ?? throw new ArgumentNullException(nameof(planetNode));

        ArgumentNullException.ThrowIfNull(planetNode.Body);
        ArgumentNullException.ThrowIfNull(planetNode.Placement);

        _updatePlanet = updatePlanet
            ?? throw new ArgumentNullException(nameof(updatePlanet));

        _removePlanet = removePlanet
            ?? throw new ArgumentNullException(nameof(removePlanet));

        _descriptionService = descriptionService
            ?? throw new ArgumentNullException(nameof(descriptionService));

        _planetName = planetNode.Body.Name ?? string.Empty;
        _planetDescription = planetNode.Body.Description ?? string.Empty;

        _selectedPlanetType = planetNode.Body.BodyType;
        _planetColor = planetNode.Body.BaseColor;
        _planetColorBrush = CreateColorBrush(_planetColor);

        // The callbacks are suppressed until the existing body's settings
        // have been copied into the type-specific view models.
        PlanetBlackHoleModel = new(OnTypeSettingsChanged);
        PlanetNebulaModel = new(OnTypeSettingsChanged);
        PlanetCoronaModel = new(OnTypeSettingsChanged);
        PlanetSunModel = new(OnTypeSettingsChanged);
        PlanetGasGiantModel = new(OnTypeSettingsChanged);
        PlanetTerrestrialModel = new(OnTypeSettingsChanged);

        InitializeFromPlanet();

        _isInitializing = false;
    }

    #region Placement Properties

    public double PlanetSize
    {
        get => _planetNode.Placement.Size;
        set
        {
            value = Math.Clamp(value, 0.01, 1.0);

            if (_planetNode.Placement.Size == value)
                return;

            _planetNode.Placement.Size = value;
            OnPropertyChanged();

            _updatePlanet(_planetNode);
        }
    }

    public double PlanetX
    {
        get => _planetNode.Placement.Center.X;
        set => SetPlanetPosition(value, _planetNode.Placement.Center.Y, _planetNode.Placement.Center.Z);
    }

    public double PlanetY
    {
        get => _planetNode.Placement.Center.Y;
        set => SetPlanetPosition(_planetNode.Placement.Center.X, value, _planetNode.Placement.Center.Z);
    }

    public double PlanetZ
    {
        get => _planetNode.Placement.Center.Z;
        set => SetPlanetPosition(_planetNode.Placement.Center.X, _planetNode.Placement.Center.Y, value);
    }

    private void SetPlanetPosition(double x, double y, double z)
    {
        Point3D center = new(
            Math.Clamp(double.IsFinite(x) ? x : 0.0, -PositionLimit, PositionLimit),
            Math.Clamp(double.IsFinite(y) ? y : 0.0, -PositionLimit, PositionLimit),
            Math.Clamp(double.IsFinite(z) ? z : 0.0, -PositionLimit, PositionLimit));

        if (_planetNode.Placement.Center == center)
            return;

        _planetNode.Placement.Center = center;
        OnPropertyChanged(nameof(PlanetX));
        OnPropertyChanged(nameof(PlanetY));
        OnPropertyChanged(nameof(PlanetZ));
        _updatePlanet(_planetNode);
    }

    public WorldRotationDirection PlanetRotationDirection
    {
        get => _planetNode.Placement.RotationDirection;
        set
        {
            if (_planetNode.Placement.RotationDirection == value)
                return;

            _planetNode.Placement.RotationDirection = value;
            OnPropertyChanged();

            ConfigureAndUpdatePlanet();
        }
    }

    public double PlanetRotationRate
    {
        get => _planetNode.Placement.RotationRate;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, 0.0, 10.0)
                : 1.0;

            if (_planetNode.Placement.RotationRate == value)
                return;

            _planetNode.Placement.RotationRate = value;
            OnPropertyChanged();

            ConfigureAndUpdatePlanet();
        }
    }

    public double AxialTilt
    {
        get => _planetNode.Placement.AxialTilt;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, 0.0, 90.0)
                : 0.0;

            if (_planetNode.Placement.AxialTilt == value)
                return;

            _planetNode.Placement.AxialTilt = value;
            OnPropertyChanged();

            _updatePlanet(_planetNode);
        }
    }

    #endregion

    #region Commands

    public ICommand DeletePlanetCommand =>
        new RelayCommand(() => _removePlanet(_planetNode));

    #endregion

    #region Initialization

    private void InitializeFromPlanet()
    {
        CelestialBody body = _planetNode.Body;

        InitializeTextureSettings(body);
        InitializeAnimationSettings(body);
        InitializeRingSettings(body);
    }

    private void InitializeTextureSettings(CelestialBody body)
    {
        CelestialTextureSource? texture = body.TextureSource;

        if (texture is null)
            return;

        switch (texture.SourceType)
        {
            case CelestialTextureSourceType.BitmapFile:
                if (PlanetGasGiantModel is not null)
                {
                    PlanetGasGiantModel.TexturePath =
                        body.BodyType == CelestialBodyType.GasGiant
                            ? texture.FilePath ?? string.Empty
                            : string.Empty;
                }

                if (PlanetTerrestrialModel is not null)
                {
                    PlanetTerrestrialModel.TexturePath =
                        body.BodyType == CelestialBodyType.TerrestrialPlanet
                            ? texture.FilePath ?? string.Empty
                            : string.Empty;
                }
                break;

            case CelestialTextureSourceType.RealmMap:
                if (PlanetTerrestrialModel is not null)
                {
                    PlanetTerrestrialModel.MapIdentifier =
                        texture.MapId ?? string.Empty;
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
                body, CelestialBodyAnimationType.SolarActivity);

        if (settings is null)
            return;

        PlanetSunModel.EnableProminences = settings.EnableProminences;
        PlanetSunModel.EnableFlares = settings.EnableFlares;
        PlanetSunModel.ActivityIntensity = settings.ActivityIntensity;
        PlanetSunModel.ActivityFrequency = settings.ActivityFrequency;
        PlanetSunModel.ActivityGeometryScale = settings.ActivityGeometryScale;
        PlanetSunModel.ActivityThicknessScale = settings.ActivityThicknessScale;
        PlanetSunModel.EnableCorona = settings.EnableCorona;
        PlanetSunModel.CoronaOpacity = settings.CoronaOpacity;
        PlanetSunModel.CoronaRotationSpeed = settings.CoronaRotationSpeed;
    }

    private void InitializeCoronaSettings(CelestialBody body)
    {
        CoronaAnimationSettings? settings =
            GetAnimationSettings<CoronaAnimationSettings>(
                body, CelestialBodyAnimationType.SolarActivity);

        if (settings is null)
            return;

        PlanetCoronaModel.EnableProminences = settings.EnableProminences;
        PlanetCoronaModel.EnableFlares = settings.EnableFlares;
        PlanetCoronaModel.ActivityIntensity = settings.ActivityIntensity;
        PlanetCoronaModel.ActivityFrequency = settings.ActivityFrequency;
        PlanetCoronaModel.ActivityGeometryScale = settings.ActivityGeometryScale;
        PlanetCoronaModel.ActivityThicknessScale = settings.ActivityThicknessScale;
    }

    private void InitializeBlackHoleSettings(CelestialBody body)
    {
        BlackHoleAnimationSettings? settings =
            GetAnimationSettings<BlackHoleAnimationSettings>(
                body, CelestialBodyAnimationType.BlackHoleActivity);

        if (settings is null)
            return;

        PlanetBlackHoleModel.EnableJets = settings.EnableJets;
        PlanetBlackHoleModel.JetLength = settings.JetLength;
        PlanetBlackHoleModel.JetPulseRate = settings.JetPulseRate;
        PlanetBlackHoleModel.EnableJetParticles = settings.EnableJetParticles;
        PlanetBlackHoleModel.JetParticleSpeed = settings.JetParticleSpeed;
        PlanetBlackHoleModel.JetParticleEmissionRate = settings.JetParticleEmissionRate;
        PlanetBlackHoleModel.JetParticleLifetime = settings.JetParticleLifetime;
        PlanetBlackHoleModel.JetParticleDispersion = settings.JetParticleDispersion;
        PlanetBlackHoleModel.EnableDiskParticles = settings.EnableDiskParticles;
        PlanetBlackHoleModel.DiskParticleEmissionRate = settings.DiskParticleEmissionRate;
        PlanetBlackHoleModel.DiskParticleLifetime = settings.DiskParticleLifetime;
        PlanetBlackHoleModel.DiskParticleOutflowSpeed = settings.DiskParticleOutflowSpeed;
    }

    private void InitializeNebulaSettings(CelestialBody body)
    {
        NebulaAnimationSettings? settings =
            GetAnimationSettings<NebulaAnimationSettings>(
                body, CelestialBodyAnimationType.NebulaActivity);

        if (settings is null)
            return;

        PlanetNebulaModel.LayerCount = settings.LayerCount;
        PlanetNebulaModel.Width = settings.Width;
        PlanetNebulaModel.Height = settings.Height;
        PlanetNebulaModel.Depth = settings.Depth;
        PlanetNebulaModel.Opacity = settings.Opacity;
        PlanetNebulaModel.Brightness = settings.Brightness;
        PlanetNebulaModel.DetailIntensity = settings.DetailIntensity;
        PlanetNebulaModel.RotationSpeed = settings.RotationSpeed;
        PlanetNebulaModel.DriftSpeed = settings.DriftSpeed;
    }

    private void InitializeGasGiantSettings(CelestialBody body)
    {
        GasGiantAnimationSettings? settings =
            GetAnimationSettings<GasGiantAnimationSettings>(
                body, CelestialBodyAnimationType.AtmosphereTurbulenceActivity);

        if (settings is null)
            return;

        PlanetGasGiantModel.EnableAtmosphericTurbulence =
            settings.EnableAtmosphericTurbulence;

        PlanetGasGiantModel.TurbulenceIntensity =
            settings.TurbulenceIntensity;
    }

    private void InitializeRingSettings(CelestialBody body)
    {
        CelestialBodyAttachment? attachment =
            body.Attachments.FirstOrDefault(
                item => item.Type == CelestialBodyAttachmentType.PlanetaryRings);

        if (attachment?.AttachmentSettings is not PlanetaryRingSettings settings)
            return;

        PlanetaryRingViewModel rings =
            body.BodyType == CelestialBodyType.GasGiant
                ? PlanetGasGiantModel.Rings
                : PlanetTerrestrialModel.Rings;

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

    private static TSettings? GetAnimationSettings<TSettings>(
        CelestialBody body,
        CelestialBodyAnimationType animationType)
        where TSettings : class
    {
        return body.Animations
            .FirstOrDefault(animation => animation.AnimationType == animationType)
            ?.Settings as TSettings;
    }

    #endregion

    #region Updates

    private void OnTypeSettingsChanged()
    {
        if (_isInitializing)
            return;

        ConfigureAndUpdatePlanet();
    }

    private void ConfigureAndUpdatePlanet()
    {
        if (_isInitializing)
            return;

        CelestialBodyFactory.Configure(
            _planetNode.Body,
            SelectedPlanetType,
            _planetNode.Placement.RotationRate,
            _planetNode.Placement.RotationDirection,
            PlanetBlackHoleModel,
            PlanetNebulaModel,
            PlanetCoronaModel,
            PlanetSunModel,
            PlanetGasGiantModel,
            PlanetTerrestrialModel,
            baseColor: PlanetColor,
            description: PlanetDescription);

        // Factory configuration can assign type-specific metadata.
        // Restore the user's values so type changes never erase them.
        _planetNode.Body.Name = PlanetName;
        _planetNode.Body.Description = PlanetDescription;

        _updatePlanet(_planetNode);
    }

    #endregion
    private static SolidColorBrush CreateColorBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
