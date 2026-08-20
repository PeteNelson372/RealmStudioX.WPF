using RealmStudioShapeRenderingLib;
using SkiaSharp;

namespace RealmStudioX.WPF.Models.Map
{
    public class ResizeMapResult
    {
        public RealmCreationOperation CreationOperation { get; set; }
        public RealmStudioMap? Map { get; set; }
        public ResizeMapAnchorPoint AnchorPoint { get; set; } = ResizeMapAnchorPoint.CenterZoomed;
        public SKRect SelectedArea { get; set; } = SKRect.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IncludeTerrainSymbols { get; set; }
        public bool IncludeVegetationSymbols { get; set; }
        public bool IncludeStructureSymbols { get; set; }
        public bool IncludeMarkerSymbols { get; set; }
        public bool IncludeLabels { get; set; }
        public bool IncludeBoxes { get; set; }
        public bool IncludePaths { get; set; }
        public bool IncludeScale { get; set; }
        public bool IncludeGrid { get; set; }
        public bool IncludeRegions { get; set; }
        public bool IncludeDrawnShapes { get; set; }
        public bool IncludeHeightMap { get; set; }
    }
}
