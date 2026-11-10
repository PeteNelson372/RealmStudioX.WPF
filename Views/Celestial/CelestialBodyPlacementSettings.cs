using RealmStudioShapeRenderingLib;
using System.Windows.Media.Media3D;

namespace RealmStudioX.WPF.Views.Celestial
{
    public class CelestialBodyPlacementSettings
    {
        public Point3D Center { get; set; } = new(0, 0, 0);

        public double Size { get; set; } = 0.09;

        public double Distance { get; set; } = 0.08;

        public double Orbit { get; set; } = 0.0;

        public double Plane { get; set; } = 0.24;

        public WorldRotationDirection RotationDirection { get; set; } = WorldRotationDirection.Prograde;

        public double RotationRate { get; set; } = 1.0;

        public double AxialTilt { get; set; } = 0.0;

        public CelestialBodyPlacementSettings Clone()
        {
            return new CelestialBodyPlacementSettings
            {
                Center = Center,
                Size = Size,
                Distance = Distance,
                Orbit = Orbit,
                Plane = Plane,
                RotationDirection = RotationDirection,
                RotationRate = RotationRate,
                AxialTilt = AxialTilt
            };
        }
    }
}
