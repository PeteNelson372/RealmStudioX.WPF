using RealmStudioShapeRenderingLib;

namespace RealmStudioX.WPF.Views.Celestial
{
    public sealed class CelestialTextureSource
    {
        public CelestialTextureSourceType SourceType { get; set; }
            = CelestialTextureSourceType.None;

        // Used when SourceType is RealmMap
        public string? MapId { get; set; }

        // Used when SourceType is Asset
        public string? AssetPath { get; set; }

        // Used when SourceType is BitmapFile
        public string? FilePath { get; set; }

        // Used when SourceType is Procedural
        public string? GeneratorId { get; set; }

        public Dictionary<string, string> GeneratorParameters { get; set; } = [];
    }
}
