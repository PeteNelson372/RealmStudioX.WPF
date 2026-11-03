using System.Collections.ObjectModel;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialBodyNode
    {
        public CelestialBody Body { get; set; } = new();

        public ObservableCollection<CelestialBodyNode> Children { get; } = [];

        // Relationship to the parent body
        public double OrbitalDistance { get; set; } = 1.0;

        public double OrbitalInclination { get; set; } = 0.0;

        public double OrbitalAngle { get; set; } = 0.0;

        public double OrbitalSpeed { get; set; } = 0.0;

        public bool EnableOrbitAnimation { get; set; } = false;
    }
}
