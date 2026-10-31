namespace RealmStudioX.WPF.WorldGlobe
{
    public sealed class SunSettings : SolarActivitySettings, ILocalStarDetailSettings
    {
        public double CoronaRotationSpeed { get; set; } = 12.0;
        public double CoronaOpacity { get; set; } = 0.18;
        public bool EnableCorona { get; set; } = true;

        public SunSettings Clone() => new()
        {
            EnableProminences = EnableProminences,
            EnableFlares = EnableFlares,
            ActivityIntensity = ActivityIntensity,
            ActivityFrequency = ActivityFrequency,
            ActivityGeometryScale = ActivityGeometryScale,
            ActivityThicknessScale = ActivityThicknessScale,

            CoronaRotationSpeed = CoronaRotationSpeed,
            CoronaOpacity = CoronaOpacity,
            EnableCorona = EnableCorona
        };
    }
}
