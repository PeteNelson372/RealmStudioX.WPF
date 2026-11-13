using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.Views.Celestial;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.ViewModels.Dialogs;

public sealed class CelestialLightingViewModel : ViewModelBase
{
    private readonly Action<CelestialLightingSettings> _applySettings;

    public CelestialLightingViewModel(
        Action<CelestialLightingSettings> applySettings)
    {
        _applySettings = applySettings
            ?? throw new ArgumentNullException(nameof(applySettings));

        AmbientLightColorBrush = CreateBrush(AmbientLightColor);

        ApplySettings();
    }

    private bool _enableAmbientLight = true;

    public bool EnableAmbientLight
    {
        get => _enableAmbientLight;
        set
        {
            if (SetProperty(ref _enableAmbientLight, value))
                ApplySettings();
        }
    }

    private Color _ambientLightColor = Colors.White;

    public Color AmbientLightColor
    {
        get => _ambientLightColor;
        set
        {
            if (SetProperty(ref _ambientLightColor, value))
            {
                AmbientLightColorBrush = CreateBrush(value);
                ApplySettings();
            }
        }
    }

    private Brush _ambientLightColorBrush =
        new SolidColorBrush(Colors.White);

    public Brush AmbientLightColorBrush
    {
        get => _ambientLightColorBrush;
        private set => SetProperty(ref _ambientLightColorBrush, value);
    }

    private double _ambientLightIntensity = 0.20;

    public double AmbientLightIntensity
    {
        get => _ambientLightIntensity;
        set
        {
            double clamped = double.IsFinite(value)
                ? Math.Clamp(value, 0.0, 1.0)
                : 0.0;

            if (SetProperty(ref _ambientLightIntensity, clamped))
                ApplySettings();
        }
    }

    private static Brush CreateBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private void ApplySettings()
    {
        _applySettings(new CelestialLightingSettings
        {
            EnableAmbientLight = EnableAmbientLight,
            AmbientLightColor = AmbientLightColor,
            AmbientLightIntensity = AmbientLightIntensity
        });
    }
}