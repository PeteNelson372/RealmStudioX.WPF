namespace RealmStudioX.WPF.WorldGlobe;

/// <summary>
/// Appearance and animation options for a layered nebula local-star object.
/// These values are stored on LocalStar as an ILocalStarDetailSettings snapshot.
/// </summary>
public sealed class NebulaSettings : ILocalStarDetailSettings
{
    /// <summary>Number of cloud planes displayed. Valid range is 1..7.</summary>
    public int LayerCount { get; set; } = 5;

    /// <summary>Base width of the nebula before LocalStar.Radius scaling.</summary>
    public double Width { get; set; } = 18.0;

    /// <summary>Base height of the nebula before LocalStar.Radius scaling.</summary>
    public double Height { get; set; } = 12.0;

    /// <summary>Distance between the foremost and rearmost cloud planes.</summary>
    public double Depth { get; set; } = 5.0;

    /// <summary>Overall opacity multiplier for all cloud layers.</summary>
    public double Opacity { get; set; } = 0.9;

    /// <summary>Multiplicative brightness of the generated cloud texture.</summary>
    public double Brightness { get; set; } = 1.0;

    /// <summary>Controls how concentrated or diffuse the procedural cloud texture is.</summary>
    public double DetailIntensity { get; set; } = 1.0;

    /// <summary>Slow in-plane rotation speed in degrees per second.</summary>
    public double RotationSpeed { get; set; } = 1.2;

    /// <summary>Slow, deterministic cloud-layer drift in local scene units per second.</summary>
    public double DriftSpeed { get; set; } = 0.25;

    public NebulaSettings Clone() => new()
    {
        LayerCount = LayerCount,
        Width = Width,
        Height = Height,
        Depth = Depth,
        Opacity = Opacity,
        Brightness = Brightness,
        DetailIntensity = DetailIntensity,
        RotationSpeed = RotationSpeed,
        DriftSpeed = DriftSpeed
    };
}
