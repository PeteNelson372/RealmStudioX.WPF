
using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.IO;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class PlanetaryRingViewModel : ViewModelBase
{
    private readonly Action? _settingsChanged;

    public PlanetaryRingSettings Settings { get; }

    public CelestialBodyAnimation RotationAnimation { get; }

    public CelestialBodyAnimation TurbulenceAnimation { get; }

    private BasicRotationSettings RotationSettings =>
        (BasicRotationSettings)RotationAnimation.Settings!;

    private RingTurbulenceSettings TurbulenceSettings =>
        (RingTurbulenceSettings)TurbulenceAnimation.Settings!;

    public PlanetaryRingViewModel(
        PlanetaryRingSettings settings,
        CelestialBodyAnimation rotationAnimation,
        CelestialBodyAnimation turbulenceAnimation,
        Action? settingsChanged = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        RotationAnimation = rotationAnimation
            ?? throw new ArgumentNullException(nameof(rotationAnimation));

        TurbulenceAnimation = turbulenceAnimation
            ?? throw new ArgumentNullException(nameof(turbulenceAnimation));

        if (RotationAnimation.AnimationType != CelestialBodyAnimationType.BasicRotation)
            throw new ArgumentException(
                "The rotation animation must be BasicRotation.",
                nameof(rotationAnimation));

        if (TurbulenceAnimation.AnimationType != CelestialBodyAnimationType.RingTurbulenceActivity)
            throw new ArgumentException(
                "The turbulence animation must be RingTurbulenceActivity.",
                nameof(turbulenceAnimation));

        RotationAnimation.Settings ??= new BasicRotationSettings();
        TurbulenceAnimation.Settings ??= new RingTurbulenceSettings();

        if (RotationAnimation.Settings is not BasicRotationSettings ||
            TurbulenceAnimation.Settings is not RingTurbulenceSettings)
        {
            throw new ArgumentException("The supplied animation settings have incompatible types.");
        }

        _settingsChanged = settingsChanged;
    }

    private void NotifySettingsChanged()
    {
        _settingsChanged?.Invoke();
    }

    // ------------------------------------------------------------
    // Ring visibility
    // ------------------------------------------------------------

    public bool Enabled
    {
        get => Settings.Enabled;
        set
        {
            if (Settings.Enabled == value)
                return;

            Settings.Enabled = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    // ------------------------------------------------------------
    // Texture
    // ------------------------------------------------------------

    public string TexturePath
    {
        get => Settings.TexturePath;
        set
        {
            if (Settings.TexturePath == value)
                return;

            Settings.TexturePath = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    public ICommand SelectTextureFileCommand => new RelayCommand(() =>
    {
        string textureDirectory = Path.Combine(
            ApplicationPreferences.Current.RootAssetDirectory,
            "Textures",
            "Rings");

        OpenFileDialog dialog = new()
        {
            Title = "Select Planetary Ring Texture",
            Filter = UserInterfaceUtilities.GetCommonImageFilter(),
            Multiselect = false,
            InitialDirectory = Directory.Exists(textureDirectory)
                ? textureDirectory
                : ApplicationPreferences.Current.RootAssetDirectory,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            TexturePath = dialog.FileName;
    });

    public ICommand ClearTextureCommand => new RelayCommand(() =>
    {
        TexturePath = string.Empty;
    });

    // ------------------------------------------------------------
    // Ring dimensions
    // ------------------------------------------------------------

    private const double MinimumRadiusScale = 1.01;
    private const double MaximumRadiusScale = 10.0;
    private const double MinimumRingWidth = 0.01;

    public double InnerRadiusScale
    {
        get => Settings.InnerRadiusScale;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    MinimumRadiusScale,
                    Math.Min(
                        MaximumRadiusScale - MinimumRingWidth,
                        Settings.OuterRadiusScale - MinimumRingWidth))
                : 1.2;

            if (Settings.InnerRadiusScale == value)
                return;

            Settings.InnerRadiusScale = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    public double OuterRadiusScale
    {
        get => Settings.OuterRadiusScale;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    Settings.InnerRadiusScale + MinimumRingWidth,
                    MaximumRadiusScale)
                : 2.0;

            if (Settings.OuterRadiusScale == value)
                return;

            Settings.OuterRadiusScale = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    // ------------------------------------------------------------
    // Ring inclination
    // ------------------------------------------------------------

    public double Inclination
    {
        get => Settings.Inclination;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, -90.0, 90.0)
                : 0.0;

            if (Settings.Inclination == value)
                return;

            Settings.Inclination = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    // ------------------------------------------------------------
    // Ring rotation
    // ------------------------------------------------------------

    public bool EnableRotation
    {
        get => RotationAnimation.Enabled;
        set
        {
            if (RotationAnimation.Enabled == value)
                return;

            RotationAnimation.Enabled = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    private const double MinimumRotationRate = 0.0;
    private const double MaximumRotationRate = 10.0;

    public double RotationRate
    {
        get => RotationSettings.RotationRate;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(value, MinimumRotationRate, MaximumRotationRate)
                : 1.0;

            if (RotationSettings.RotationRate == value)
                return;

            RotationSettings.RotationRate = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    public WorldRotationDirection RotationDirection
    {
        get => RotationSettings.RotationDirection;
        set
        {
            if (RotationSettings.RotationDirection == value)
                return;

            RotationSettings.RotationDirection = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    // ------------------------------------------------------------
    // Turbulence
    // ------------------------------------------------------------

    public bool EnableTurbulence
    {
        get => TurbulenceAnimation.Enabled;
        set
        {
            if (TurbulenceAnimation.Enabled == value)
                return;

            TurbulenceAnimation.Enabled = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    private const double MinimumTurbulenceIntensity = 0.0;
    private const double MaximumTurbulenceIntensity = 1.0;

    public double TurbulenceIntensity
    {
        get => TurbulenceSettings.Intensity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    MinimumTurbulenceIntensity,
                    MaximumTurbulenceIntensity)
                : 0.2;

            if (TurbulenceSettings.Intensity == value)
                return;

            TurbulenceSettings.Intensity = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }
}
