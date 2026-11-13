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

        // Conceptual body identity
        public CelestialBodyType BodyType { get; set; } = CelestialBodyType.TerrestrialPlanet;

        // Display properties
        public Color BaseColor { get; set; } = Colors.White;

        /// <summary>
        /// Relative intensity of light emitted by this body, when supported
        /// by its renderer.
        /// </summary>
        public double LightIntensity { get; set; } = 0.5;

        // Rendering configuration
        public CelestialBodyRendererType RendererType { get; set; } = CelestialBodyRendererType.Planet;

        public CelestialTextureSource TextureSource { get; set; } = new();

        // Lighting configuration (owned by this body, not shared globally)
        public CelestialPointLightSettings PointLightSettings { get; set; } = new();

        // Animation configuration
        public ObservableCollection<CelestialBodyAnimation> Animations { get; } = [];

        // Attachments
        public ObservableCollection<CelestialBodyAttachment> Attachments { get; } = [];
    }
}
