using RealmStudioShapeRenderingLib;
using System.Collections.ObjectModel;

namespace RealmStudioX.WPF.Views.Celestial
{
    public class CelestialBodyAttachment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public CelestialBodyAttachmentType Type { get; set; }

        public CelestialTextureSource TextureSource { get; set; } = new();

        public ICelestialBodyAttachmentSettings? AttachmentSettings { get; set; }

        public ObservableCollection<CelestialBodyAnimation> Animations { get; } = [];

        public bool Enabled { get; set; } = true;
    }
}