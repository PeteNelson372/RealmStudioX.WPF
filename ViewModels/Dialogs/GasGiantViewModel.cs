
using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.IO;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public class GasGiantViewModel : ViewModelBase
{
    Action? _updateActiveGasGiant;

    public PlanetaryRingViewModel Rings { get; }

    public GasGiantViewModel(Action? updateActiveGasGiant = null)
    {
        _updateActiveGasGiant = updateActiveGasGiant;

        Rings = new PlanetaryRingViewModel(
            _ringSettings,
            _ringRotationAnimation,
            _ringTurbulenceAnimation,
            UpdateActiveGasGiant);
    }

    private readonly PlanetaryRingSettings _ringSettings = new();

    private readonly CelestialBodyAnimation _ringRotationAnimation = new()
    {
        AnimationType = CelestialBodyAnimationType.BasicRotation,
        Enabled = true,
        Settings = new BasicRotationSettings
        {
            RotationRate = 1.0,
            RotationDirection = WorldRotationDirection.Prograde
        }
    };

    private readonly CelestialBodyAnimation _ringTurbulenceAnimation = new()
    {
        AnimationType = CelestialBodyAnimationType.RingTurbulenceActivity,
        Enabled = false,
        Settings = new RingTurbulenceSettings
        {
            Intensity = 0.2
        }
    };

    private string _texturePath = string.Empty;

    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if (SetProperty(ref _texturePath, value))
                UpdateActiveGasGiant();
        }
    }

    private bool _enableAtmosphericTurbulence = true;

    public bool EnableAtmosphericTurbulence
    {
        get => _enableAtmosphericTurbulence;
        set
        {
            if (SetProperty(ref _enableAtmosphericTurbulence, value))
                UpdateActiveGasGiant();
        }
    }

    private const double MinimumTurbulenceIntensity = 0.0;
    private const double MaximumTurbulenceIntensity = 1.0;
    private const double DefaultTurbulenceIntensity = 0.35;

    private double _turbulenceIntensity = DefaultTurbulenceIntensity;

    public double TurbulenceIntensity
    {
        get => _turbulenceIntensity;
        set
        {
            value = double.IsFinite(value)
                ? Math.Clamp(
                    value,
                    MinimumTurbulenceIntensity,
                    MaximumTurbulenceIntensity)
                : DefaultTurbulenceIntensity;

            if (SetProperty(ref _turbulenceIntensity, value))
                UpdateActiveGasGiant();
        }
    }

    public ICommand SelectTextureFileCommand => new RelayCommand(() =>
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
            TexturePath = dialog.FileName;
        }
    });

    private void UpdateActiveGasGiant()
    {
        _updateActiveGasGiant?.Invoke();
    }
}
