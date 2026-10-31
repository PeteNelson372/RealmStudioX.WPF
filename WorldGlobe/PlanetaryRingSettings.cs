using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.WorldGlobe;

public sealed class PlanetaryRingSettings
{
    public bool Enabled { get; set; } = false;

    public string TexturePath { get; set; } = string.Empty;

    // Radii are multiples of the host body's radius.
    public double InnerRadiusScale { get; set; } = 1.2;
    public double OuterRadiusScale { get; set; } = 2.0;

    // Inclination relative to the host body's equatorial plane.
    public double Inclination { get; set; }

    public bool EnableRotation { get; set; } = true;
    public double RotationRate { get; set; } = 1.0;
    public WorldRotationDirection RotationDirection { get; set; } = WorldRotationDirection.Prograde;

    public bool EnableTurbulence { get; set; }
    public double TurbulenceIntensity { get; set; } = 0.2;

    public PlanetaryRingSettings Clone() => new()
    {
        Enabled = Enabled,
        TexturePath = TexturePath,
        InnerRadiusScale = InnerRadiusScale,
        OuterRadiusScale = OuterRadiusScale,
        Inclination = Inclination,
        EnableRotation = EnableRotation,
        RotationRate = RotationRate,
        RotationDirection = RotationDirection,
        EnableTurbulence = EnableTurbulence,
        TurbulenceIntensity = TurbulenceIntensity
    };
}