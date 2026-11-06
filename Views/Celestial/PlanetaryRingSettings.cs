namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class PlanetaryRingSettings : ICelestialBodyAttachmentSettings
    {
        public bool Enabled { get; set; } = false;

        public string TexturePath { get; set; } = string.Empty;

        // Radii are multiples of the host body's radius.
        public double InnerRadiusScale { get; set; } = 1.2;
        public double OuterRadiusScale { get; set; } = 2.0;

        // Inclination relative to the host body's equatorial plane.
        public double Inclination { get; set; }

        public PlanetaryRingSettings Clone() => new()
        {
            Enabled = Enabled,
            TexturePath = TexturePath,
            InnerRadiusScale = InnerRadiusScale,
            OuterRadiusScale = OuterRadiusScale,
            Inclination = Inclination
        };
    }
}