using RealmStudioX.WPF.EditorUtilities;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.WorldGlobe
{
    public sealed class PlanetaryRing
    {
        internal byte[] RingImage = WorldGlobeAssets.ReadBytes("satturnringcolor2");
        internal double InnerRadius { get; set; } = 1.5;
        internal double OuterRadius { get; set; } = 2.5;
        internal Color RingColor { get; set; } = Colors.White;
        internal double RingTiltAngle { get; set; }
        internal double RingBrushOpacity { get; set; } = 100;

    }
}
