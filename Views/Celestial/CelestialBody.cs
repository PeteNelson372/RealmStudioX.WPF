using RealmStudioShapeRenderingLib;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialBody
    {
        // Identity and descriptive information
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Body";
        public string Description { get; set; } = string.Empty;

        // Physical/display properties
        public double Radius { get; set; } = 1.0;
        public Color BaseColor { get; set; } = Colors.White;

        // Rendering configuration
        public CelestialBodyRendererType RendererType { get; set; } = CelestialBodyRendererType.Planet;

        public CelestialTextureSource TextureSource { get; set; } = new();

        // Animation configuration
        public ObservableCollection<CelestialBodyAnimation> Animations { get; } = [];

        // Attachments
        public ObservableCollection<CelestialBodyAttachment> Attachments { get; } = [];
    }
}
