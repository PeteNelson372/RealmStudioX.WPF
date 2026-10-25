using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe
{
    public sealed class Moon
    {
        internal SphereVisual3D moonVisual = new();
        internal string MoonName { get; set; } = string.Empty;
        internal Point3D MoonCenter { get; set; } = new Point3D(2, 2, 0);
        internal double MoonRadius { get; set; } = 0.1;
        internal string MoonTextureName { get; set; } = "Moon";
        internal string MoonTexturePath { get; set; } = string.Empty;
        internal Color MoonColor { get; set; } = Colors.White;
        internal int MoonDistance { get; set; } = 250;
        internal double MoonOrbitRotation { get; set; }
        internal double MoonPlaneRotation { get; set; }
        internal double MoonAxisRotation { get; set; }
    }
}
