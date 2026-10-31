using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.WorldGlobe;
using System.IO;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class PlanetaryRingViewModel : ViewModelBase
{
    private readonly Action? _settingsChanged;

    public PlanetaryRingSettings Settings { get; }

    public PlanetaryRingViewModel(PlanetaryRingSettings settings, Action? settingsChanged = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
        string textureDirectory = Path.Combine(ApplicationPreferences.Current.RootAssetDirectory,
            "Textures",
            "Planet");

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
        {
            TexturePath = dialog.FileName;
        }
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
        get => Settings.EnableRotation;
        set
        {
            if (Settings.EnableRotation == value)
                return;

            Settings.EnableRotation = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    private const double MinimumRotationRate = 0.0;
    private const double MaximumRotationRate = 10.0;

    public double RotationRate
    {
        get => Settings.RotationRate;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    MinimumRotationRate,
                    MaximumRotationRate)
                : 1.0;

            if (Settings.RotationRate == value)
                return;

            Settings.RotationRate = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    public WorldRotationDirection RotationDirection
    {
        get => Settings.RotationDirection;
        set
        {
            if (Settings.RotationDirection == value)
                return;

            Settings.RotationDirection = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    // ------------------------------------------------------------
    // Turbulence
    // ------------------------------------------------------------

    public bool EnableTurbulence
    {
        get => Settings.EnableTurbulence;
        set
        {
            if (Settings.EnableTurbulence == value)
                return;

            Settings.EnableTurbulence = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }

    private const double MinimumTurbulenceIntensity = 0.0;
    private const double MaximumTurbulenceIntensity = 1.0;

    public double TurbulenceIntensity
    {
        get => Settings.TurbulenceIntensity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    MinimumTurbulenceIntensity,
                    MaximumTurbulenceIntensity)
                : 0.2;

            if (Settings.TurbulenceIntensity == value)
                return;

            Settings.TurbulenceIntensity = value;
            OnPropertyChanged();
            NotifySettingsChanged();
        }
    }
}