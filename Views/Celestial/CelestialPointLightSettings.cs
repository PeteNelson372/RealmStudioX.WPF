namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Point-light configuration owned by an individual celestial body.
/// The defaults preserve the light emitted by existing Local Star renderers.
/// </summary>
public sealed class CelestialPointLightSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Normalized light intensity in the range 0 to 1.</summary>
    public double Intensity { get; set; } = 0.1;

    /// <summary>Effective attenuation range in world units.</summary>
    public double Range { get; set; } = 10.0;

    public CelestialPointLightSettings Clone() => new()
    {
        Enabled = Enabled,
        Intensity = Intensity,
        Range = Range
    };
}
