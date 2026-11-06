namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class NebulaAnimationSettings : ICelestialBodyAnimationSettings
    {
        public int LayerCount { get; set; } = 5;
        public double Width { get; set; } = 18.0;
        public double Height { get; set; } = 12.0;
        public double Depth { get; set; } = 5.0;
        public double Opacity { get; set; } = 0.9;
        public double Brightness { get; set; } = 1.0;
        public double DetailIntensity { get; set; } = 1.0;
        public double RotationSpeed { get; set; } = 1.2;
        public double DriftSpeed { get; set; } = 0.25;

        public ICelestialBodyAnimationSettings Clone() => new NebulaAnimationSettings
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
}