using RealmStudioShapeRenderingLib;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe
{
    public sealed class LocalStar
    {
        public Point3D Center { get; set; } = new(3, 3, 0);

        public double Radius { get; set; } = 0.15;

        public LocalStarType StarImageType { get; set; } = LocalStarType.Sun;

        // Color of the star's light source.
        public Color StarColor { get; set; } = Colors.Yellow;

        // Normalized light strength: 0.0 to 1.0.
        public double LightIntensity { get; set; } = 0.1;

        // Distance at which the point light stops affecting the scene.
        public double LightRange { get; set; } = 10.0;

        // rotation rate
        public double RotationRate = 0.0;

        // rotation direction
        public WorldRotationDirection RotationDirection = WorldRotationDirection.Prograde;

        public Transform3D LocationTransform { get; set; } = Transform3D.Identity;

        public Quaternion RotationQuaternion { get; set; } = Quaternion.Identity;

        public ILocalStarDetailSettings? LocalStarDetailSettings { get; set; }
    }
}
