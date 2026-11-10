using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialBodyNode
    {
        public CelestialBody Body { get; set; } = new();

        // Relationship to the parent body
        public CelestialBodyPlacementSettings Placement { get; set; } = new();

        public ObservableCollection<CelestialBodyNode> Children { get; } = [];

        public bool EnableOrbitAnimation { get; set; } = false;

        // local transform for the body, if any. This is used to apply additional transformations to the body, such as rotation or scaling.
        public Transform3D? LocalTransform { get; set; }
    }
}
