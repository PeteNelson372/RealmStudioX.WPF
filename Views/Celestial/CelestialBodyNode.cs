using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialBodyNode
    {
        public CelestialBody Body { get; set; } = new();

        public Point3D Center { get; set; } = new(0, 0, 0);

        public ObservableCollection<CelestialBodyNode> Children { get; } = [];

        // Relationship to the parent body
        public double OrbitalDistance { get; set; } = 1.0;

        public double OrbitalInclination { get; set; } = 0.0;

        public double OrbitalAngle { get; set; } = 0.0;

        public double OrbitalSpeed { get; set; } = 0.0;

        public bool EnableOrbitAnimation { get; set; } = false;

        // local transform for the body, if any. This is used to apply additional transformations to the body, such as rotation or scaling.
        public Transform3D? LocalTransform { get; set; }
    }
}
