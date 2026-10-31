namespace RealmStudioX.WPF.WorldGlobe
{
    public sealed class GasGiantSettings : ILocalStarDetailSettings
    {
        public string TexturePath { get; set; } = string.Empty;
        public bool EnableAtmosphericTurbulence { get; set; } = true;
        public double TurbulenceIntensity { get; set; } = 0.35;

        public PlanetaryRingSettings RingSettings { get; set; } = new();

        public GasGiantSettings Clone() => new()
        {
            TexturePath = TexturePath,
            EnableAtmosphericTurbulence = EnableAtmosphericTurbulence,
            TurbulenceIntensity = TurbulenceIntensity,
            RingSettings = RingSettings.Clone()
        };
    }
}
