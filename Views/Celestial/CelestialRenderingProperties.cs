using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial
{
    public class CelestialRenderingProperties
    {
        public Color BaseColor { get; set; } = Colors.White;

        public double Opacity { get; set; } = 1.0;
    }
}
