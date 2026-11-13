using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial;

internal sealed class CelestialLightingManager
{
    private readonly AmbientLight _ambientLight = new();

    public Model3DGroup Root { get; } = new();

    public CelestialLightingManager()
    {
        Root.Children.Add(_ambientLight);

        ApplySettings(new CelestialLightingSettings());
    }

    public void ApplySettings(CelestialLightingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.EnableAmbientLight)
        {
            _ambientLight.Color = Colors.Black;
            return;
        }

        double intensity = double.IsFinite(settings.AmbientLightIntensity)
            ? Math.Clamp(settings.AmbientLightIntensity, 0.0, 1.0)
            : 0.0;

        Color source = settings.AmbientLightColor;

        _ambientLight.Color = Color.FromRgb(
            ScaleChannel(source.R, intensity),
            ScaleChannel(source.G, intensity),
            ScaleChannel(source.B, intensity));
    }

    private static byte ScaleChannel(byte channel, double intensity)
    {
        return (byte)Math.Clamp(
            Math.Round(channel * intensity),
            0.0,
            255.0);
    }
}