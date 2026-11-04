namespace RealmStudioX.WPF.Views.Celestial;

public sealed class SunAnimationSettings : ICelestialBodyAnimationSettings
{
    public bool EnableProminences { get; set; } = true;
    public bool EnableFlares { get; set; } = true;

    public double ActivityIntensity { get; set; } = 1.0;
    public double ActivityFrequency { get; set; } = 8.0;
    public double ActivityGeometryScale { get; set; } = 0.30;
    public double ActivityThicknessScale { get; set; } = 1.4;

    public bool EnableCorona { get; set; } = true;
    public double CoronaOpacity { get; set; } = 0.18;
    public double CoronaRotationSpeed { get; set; } = 12.0;

    public ICelestialBodyAnimationSettings Clone()
    {
        return new SunAnimationSettings
        {
            EnableProminences = EnableProminences,
            EnableFlares = EnableFlares,
            ActivityIntensity = ActivityIntensity,
            ActivityFrequency = ActivityFrequency,
            ActivityGeometryScale = ActivityGeometryScale,
            ActivityThicknessScale = ActivityThicknessScale,
            EnableCorona = EnableCorona,
            CoronaOpacity = CoronaOpacity,
            CoronaRotationSpeed = CoronaRotationSpeed
        };
    }
}