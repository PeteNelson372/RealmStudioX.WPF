
using RealmStudioShapeRenderingLib;
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.WPF.Editor.Services;
using RealmStudioX.WPF.Editor.UserInterface;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using RealmStudioX.WPF.Views.Celestial;
using RealmStudioX.WPF.Views.Dialogs;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.IO;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class MoonViewModel : ViewModelBase
{
    private const double PositionLimit = 10.0;

    private readonly CelestialBodyNode _moonNode;
    private readonly Action<CelestialBodyNode> _updateMoon;
    private readonly Action<CelestialBodyNode> _removeMoon;
    private readonly MapObjectDescriptionService _descriptionService;
    private readonly Func<string?, string?> _selectProjectMap;
    private readonly ObjectCharacteristicsViewModel _moonCharacteristics = new();

    private string _moonName = string.Empty;
    private string _moonDescription = string.Empty;
    private bool _moonNameLocked;

    private bool _isInitializing = true;

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
                ConfigureAndUpdateMoon();
            }
        }
    }

    private Brush _moonColorBrush = new SolidColorBrush(Colors.White);
    public Brush MoonColorBrush
    {
        get => _moonColorBrush;
        private set => SetProperty(ref _moonColorBrush, value);
    }

    public BlackHoleViewModel MoonBlackHoleModel { get; }
    public NebulaViewModel MoonNebulaModel { get; }
    public CoronaViewModel MoonCoronaModel { get; }
    public SunViewModel MoonSunModel { get; }
    public GasGiantViewModel MoonGasGiantModel { get; }
    public TerrestrialPlanetViewModel MoonTerrestrialModel { get; }

    public IReadOnlyList<CelestialBodyType> MoonTypes { get; } =
        Enum.GetValues<CelestialBodyType>();

    private CelestialBodyType _selectedMoonType;

    public CelestialBodyType SelectedMoonType
    {
        get => _selectedMoonType;
        set
        {
            if (SetProperty(ref _selectedMoonType, value))
                ConfigureAndUpdateMoon();
        }
    }

    #region Name and Description

    public string MoonName
    {
        get => _moonName;
        set
        {
            if (SetProperty(ref _moonName, value ?? string.Empty))
            {
                _moonNode.Body.Name = _moonName;
                _updateMoon(_moonNode);
            }
        }
    }

    public string MoonDescription
    {
        get => _moonDescription;
        set
        {
            if (SetProperty(ref _moonDescription, value ?? string.Empty))
            {
                _moonNode.Body.Description = _moonDescription;
                _updateMoon(_moonNode);
            }
        }
    }

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
        ObjectCharacteristics dialog = new(
            _moonCharacteristics,
            MapObjectType.CelestialBody);

        dialog.ShowDialog();
    });

    public ICommand CreateMoonArticleCommand => new RelayCommand(() =>
    {
        MessageDialogFactory.InformationDialog(
            "Create Moon Article",
            "This feature is not yet implemented. It will allow you to create a World Anvil article for the moon.");
    });

    #endregion

    public MoonViewModel(
        CelestialBodyNode moonNode,
        Action<CelestialBodyNode> updateMoon,
        Action<CelestialBodyNode> removeMoon,
        MapObjectDescriptionService descriptionService,
        Func<string?, string?> selectProjectMap)
    {
        _moonNode = moonNode
            ?? throw new ArgumentNullException(nameof(moonNode));

        ArgumentNullException.ThrowIfNull(moonNode.Body);
        ArgumentNullException.ThrowIfNull(moonNode.Placement);

        _updateMoon = updateMoon
            ?? throw new ArgumentNullException(nameof(updateMoon));

        _removeMoon = removeMoon
            ?? throw new ArgumentNullException(nameof(removeMoon));

        _descriptionService = descriptionService
            ?? throw new ArgumentNullException(nameof(descriptionService));

        _selectProjectMap = selectProjectMap
            ?? throw new ArgumentNullException(nameof(selectProjectMap));

        _moonName = moonNode.Body.Name ?? string.Empty;
        _moonDescription = moonNode.Body.Description ?? string.Empty;

        _selectedMoonType = moonNode.Body.BodyType;
        _moonColor = moonNode.Body.BaseColor;
        _moonColorBrush = CreateColorBrush(_moonColor);

        // The callbacks are suppressed until the existing body's settings
        // have been copied into the type-specific view models.
        MoonBlackHoleModel = new(OnTypeSettingsChanged);
        MoonNebulaModel = new(OnTypeSettingsChanged);
        MoonCoronaModel = new(OnTypeSettingsChanged);
        MoonSunModel = new(OnTypeSettingsChanged);
        MoonGasGiantModel = new(OnTypeSettingsChanged);
        MoonTerrestrialModel = new(OnTypeSettingsChanged);

        InitializeFromMoon();

        _isInitializing = false;
    }

    #region Placement Properties

    public double MoonSize
    {
        get => _moonNode.Placement.Size;
        set
        {
            value = Math.Clamp(value, 0.01, 1.0);

            if (_moonNode.Placement.Size == value)
                return;

            _moonNode.Placement.Size = value;
            OnPropertyChanged();

            _updateMoon(_moonNode);
        }
    }

    public double MoonX
    {
        get => _moonNode.Placement.Center.X;
        set => SetMoonPosition(value, _moonNode.Placement.Center.Y, _moonNode.Placement.Center.Z);
    }

    public double MoonY
    {
        get => _moonNode.Placement.Center.Y;
        set => SetMoonPosition(_moonNode.Placement.Center.X, value, _moonNode.Placement.Center.Z);
    }

    public double MoonZ
    {
        get => _moonNode.Placement.Center.Z;
        set => SetMoonPosition(_moonNode.Placement.Center.X, _moonNode.Placement.Center.Y, value);
    }

    private void SetMoonPosition(double x, double y, double z)
    {
        Point3D center = new(
            Math.Clamp(double.IsFinite(x) ? x : 0.0, -PositionLimit, PositionLimit),
            Math.Clamp(double.IsFinite(y) ? y : 0.0, -PositionLimit, PositionLimit),
            Math.Clamp(double.IsFinite(z) ? z : 0.0, -PositionLimit, PositionLimit));

        if (_moonNode.Placement.Center == center)
            return;

        _moonNode.Placement.Center = center;
        OnPropertyChanged(nameof(MoonX));
        OnPropertyChanged(nameof(MoonY));
        OnPropertyChanged(nameof(MoonZ));
        _updateMoon(_moonNode);
    }

    public WorldRotationDirection MoonRotationDirection
    {
        get => _moonNode.Placement.RotationDirection;
        set
        {
            if (_moonNode.Placement.RotationDirection == value)
                return;

            _moonNode.Placement.RotationDirection = value;
            OnPropertyChanged();

            ConfigureAndUpdateMoon();
        }
    }

    public double MoonRotationRate
    {
        get => _moonNode.Placement.RotationRate;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, 0.0, 10.0)
                : 1.0;

            if (_moonNode.Placement.RotationRate == value)
                return;

            _moonNode.Placement.RotationRate = value;
            OnPropertyChanged();

            ConfigureAndUpdateMoon();
        }
    }

    public double AxialTilt
    {
        get => _moonNode.Placement.AxialTilt;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, 0.0, 90.0)
                : 0.0;

            if (_moonNode.Placement.AxialTilt == value)
                return;

            _moonNode.Placement.AxialTilt = value;
            OnPropertyChanged();

            _updateMoon(_moonNode);
        }
    }

    #endregion

    #region Commands

    public ICommand SelectMoonMapTextureCommand => new RelayCommand(() =>
    {
        string? mapId = _selectProjectMap(
            MoonTerrestrialModel.MapIdentifier);

        if (string.IsNullOrWhiteSpace(mapId))
            return;

        // The two terrestrial texture sources are mutually exclusive.
        MoonTerrestrialModel.TexturePath = string.Empty;
        MoonTerrestrialModel.MapIdentifier = mapId;
    });

    public ICommand SelectMoonTextureFileCommand => new RelayCommand(() =>
    {
        string textureDirectory = Path.Combine(
            ApplicationPreferences.Current.RootAssetDirectory,
            "Textures",
            "Moon");

        OpenFileDialog dialog = new()
        {
            Title = "Select Moon Texture",
            Filter = UserInterfaceUtilities.GetCommonImageFilter(),
            Multiselect = false,
            InitialDirectory = Directory.Exists(textureDirectory)
                ? textureDirectory
                : ApplicationPreferences.Current.RootAssetDirectory,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
            return;

        MoonTerrestrialModel.MapIdentifier = string.Empty;
        MoonTerrestrialModel.TexturePath = dialog.FileName;
    });

    public ICommand DeleteMoonCommand =>
        new RelayCommand(() => _removeMoon(_moonNode));

    #endregion

    #region Initialization

    private void InitializeFromMoon()
    {
        CelestialBody body = _moonNode.Body;

        InitializeTextureSettings(body);
        InitializeAnimationSettings(body);
        InitializePointLightSettings(body);
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
                if (MoonGasGiantModel is not null)
                {
                    MoonGasGiantModel.TexturePath =
                        body.BodyType == CelestialBodyType.GasGiant
                            ? texture.FilePath ?? string.Empty
                            : string.Empty;
                }

                if (MoonTerrestrialModel is not null)
                {
                    MoonTerrestrialModel.TexturePath =
                        body.BodyType == CelestialBodyType.TerrestrialPlanet
                            ? texture.FilePath ?? string.Empty
                            : string.Empty;
                }
                break;

            case CelestialTextureSourceType.RealmMap:
                if (MoonTerrestrialModel is not null)
                {
                    MoonTerrestrialModel.MapIdentifier =
                        texture.MapId ?? string.Empty;
                }
                break;
        }
    }

    private void InitializePointLightSettings(CelestialBody body)
    {
        CelestialPointLightSettings settings =
            body.PointLightSettings ?? new CelestialPointLightSettings();

        switch (body.BodyType)
        {
            case CelestialBodyType.Sun:
                MoonSunModel.EnablePointLight = settings.Enabled;
                MoonSunModel.PointLightIntensity = settings.Intensity;
                MoonSunModel.PointLightRange = settings.Range;
                break;

            case CelestialBodyType.Corona:
                MoonCoronaModel.EnablePointLight = settings.Enabled;
                MoonCoronaModel.PointLightIntensity = settings.Intensity;
                MoonCoronaModel.PointLightRange = settings.Range;
                break;

            case CelestialBodyType.Nebula:
                MoonNebulaModel.EnablePointLight = settings.Enabled;
                MoonNebulaModel.PointLightIntensity = settings.Intensity;
                MoonNebulaModel.PointLightRange = settings.Range;
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

        MoonSunModel.EnableProminences = settings.EnableProminences;
        MoonSunModel.EnableFlares = settings.EnableFlares;
        MoonSunModel.ActivityIntensity = settings.ActivityIntensity;
        MoonSunModel.ActivityFrequency = settings.ActivityFrequency;
        MoonSunModel.ActivityGeometryScale = settings.ActivityGeometryScale;
        MoonSunModel.ActivityThicknessScale = settings.ActivityThicknessScale;
        MoonSunModel.EnableCorona = settings.EnableCorona;
        MoonSunModel.CoronaOpacity = settings.CoronaOpacity;
        MoonSunModel.CoronaRotationSpeed = settings.CoronaRotationSpeed;
    }

    private void InitializeCoronaSettings(CelestialBody body)
    {
        CoronaAnimationSettings? settings =
            GetAnimationSettings<CoronaAnimationSettings>(
                body, CelestialBodyAnimationType.SolarActivity);

        if (settings is null)
            return;

        MoonCoronaModel.EnableProminences = settings.EnableProminences;
        MoonCoronaModel.EnableFlares = settings.EnableFlares;
        MoonCoronaModel.ActivityIntensity = settings.ActivityIntensity;
        MoonCoronaModel.ActivityFrequency = settings.ActivityFrequency;
        MoonCoronaModel.ActivityGeometryScale = settings.ActivityGeometryScale;
        MoonCoronaModel.ActivityThicknessScale = settings.ActivityThicknessScale;
    }

    private void InitializeBlackHoleSettings(CelestialBody body)
    {
        BlackHoleAnimationSettings? settings =
            GetAnimationSettings<BlackHoleAnimationSettings>(
                body, CelestialBodyAnimationType.BlackHoleActivity);

        if (settings is null)
            return;

        MoonBlackHoleModel.EnableJets = settings.EnableJets;
        MoonBlackHoleModel.JetLength = settings.JetLength;
        MoonBlackHoleModel.JetPulseRate = settings.JetPulseRate;
        MoonBlackHoleModel.EnableJetParticles = settings.EnableJetParticles;
        MoonBlackHoleModel.JetParticleSpeed = settings.JetParticleSpeed;
        MoonBlackHoleModel.JetParticleEmissionRate = settings.JetParticleEmissionRate;
        MoonBlackHoleModel.JetParticleLifetime = settings.JetParticleLifetime;
        MoonBlackHoleModel.JetParticleDispersion = settings.JetParticleDispersion;
        MoonBlackHoleModel.EnableDiskParticles = settings.EnableDiskParticles;
        MoonBlackHoleModel.DiskParticleEmissionRate = settings.DiskParticleEmissionRate;
        MoonBlackHoleModel.DiskParticleLifetime = settings.DiskParticleLifetime;
        MoonBlackHoleModel.DiskParticleOutflowSpeed = settings.DiskParticleOutflowSpeed;
    }

    private void InitializeNebulaSettings(CelestialBody body)
    {
        NebulaAnimationSettings? settings =
            GetAnimationSettings<NebulaAnimationSettings>(
                body, CelestialBodyAnimationType.NebulaActivity);

        if (settings is null)
            return;

        MoonNebulaModel.LayerCount = settings.LayerCount;
        MoonNebulaModel.Width = settings.Width;
        MoonNebulaModel.Height = settings.Height;
        MoonNebulaModel.Depth = settings.Depth;
        MoonNebulaModel.Opacity = settings.Opacity;
        MoonNebulaModel.Brightness = settings.Brightness;
        MoonNebulaModel.DetailIntensity = settings.DetailIntensity;
        MoonNebulaModel.RotationSpeed = settings.RotationSpeed;
        MoonNebulaModel.DriftSpeed = settings.DriftSpeed;
    }

    private void InitializeGasGiantSettings(CelestialBody body)
    {
        GasGiantAnimationSettings? settings =
            GetAnimationSettings<GasGiantAnimationSettings>(
                body, CelestialBodyAnimationType.AtmosphereTurbulenceActivity);

        if (settings is null)
            return;

        MoonGasGiantModel.EnableAtmosphericTurbulence =
            settings.EnableAtmosphericTurbulence;

        MoonGasGiantModel.TurbulenceIntensity =
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
                ? MoonGasGiantModel.Rings
                : MoonTerrestrialModel.Rings;

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

        ConfigureAndUpdateMoon();
    }

    private void ConfigureAndUpdateMoon()
    {
        if (_isInitializing)
            return;

        CelestialBodyFactory.Configure(
            _moonNode.Body,
            SelectedMoonType,
            _moonNode.Placement.RotationRate,
            _moonNode.Placement.RotationDirection,
            MoonBlackHoleModel,
            MoonNebulaModel,
            MoonCoronaModel,
            MoonSunModel,
            MoonGasGiantModel,
            MoonTerrestrialModel,
            baseColor: MoonColor,
            description: MoonDescription);

        // Factory configuration can assign type-specific metadata.
        // Restore the user's values so type changes never erase them.
        _moonNode.Body.Name = MoonName;
        _moonNode.Body.Description = MoonDescription;

        _updateMoon(_moonNode);
    }

    #endregion
    private static SolidColorBrush CreateColorBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
