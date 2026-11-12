
using RealmStudioShapeRenderingLib;
using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.EditorUtilities;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.IO;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public class TerrestrialPlanetViewModel : ViewModelBase
{
    private readonly Action? _updateActivePlanet;

    public PlanetaryRingViewModel Rings { get; }

    public TerrestrialPlanetViewModel(Action? updateActiveTerrestrialPlanet = null)
    {
        _updateActivePlanet = updateActiveTerrestrialPlanet;

        Rings = new PlanetaryRingViewModel(
            _ringSettings,
            _ringRotationAnimation,
            _ringTurbulenceAnimation,
            UpdateTerrestrialPlanet);
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
                UpdateTerrestrialPlanet();
        }
    }

    private string _mapIdentifier = string.Empty;

    public string MapIdentifier
    {
        get => _mapIdentifier;
        set
        {
            if (SetProperty(ref _mapIdentifier, value))
                UpdateTerrestrialPlanet();
        }
    }

    private bool _enableAtmosphericTurbulence = true;

    public bool EnableAtmosphericTurbulence
    {
        get => _enableAtmosphericTurbulence;
        set
        {
            if (SetProperty(ref _enableAtmosphericTurbulence, value))
                UpdateTerrestrialPlanet();
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
                UpdateTerrestrialPlanet();
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
            Title = "Select Planet Texture",
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
            MapIdentifier = string.Empty;
        }
    });

    private void UpdateTerrestrialPlanet()
    {
        _updateActivePlanet?.Invoke();
    }
}
