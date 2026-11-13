using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial;

public sealed class CelestialLightingSettings
{
    public bool EnableAmbientLight { get; set; } = true;

    public Color AmbientLightColor { get; set; } = Colors.White;

    public double AmbientLightIntensity { get; set; } = 0.20;
}