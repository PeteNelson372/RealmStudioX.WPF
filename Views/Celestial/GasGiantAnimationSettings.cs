namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class GasGiantAnimationSettings : ICelestialBodyAnimationSettings
    {
        public bool EnableAtmosphericTurbulence { get; set; } = true;

        public double TurbulenceIntensity { get; set; } = 0.35;

        public ICelestialBodyAnimationSettings Clone() => new GasGiantAnimationSettings
        {
            EnableAtmosphericTurbulence = EnableAtmosphericTurbulence,
            TurbulenceIntensity = TurbulenceIntensity
        };
    }
}