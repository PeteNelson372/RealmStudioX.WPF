namespace RealmStudioX.WPF.WorldGlobe
{
    public class SolarActivitySettings
    {
        public bool EnableProminences { get; set; } = true;
        public bool EnableFlares { get; set; } = true;

        public double ActivityIntensity { get; set; } = 1.0;
        public double ActivityFrequency { get; set; } = 8.0;

        public double ActivityGeometryScale { get; set; } = 1.0;
        public double ActivityThicknessScale { get; set; } = 1.0;
    }
}
