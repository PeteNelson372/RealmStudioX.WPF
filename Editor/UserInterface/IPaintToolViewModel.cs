using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    interface IPaintToolViewModel
    {
        public ColorPalette? PaintPalette { get; }
        public SKColor PaintingColor { get; set; }
        public int BrushSpacing { get; set; }
        public int PaintBrushSize { get; set; }
        public BrushPatternItem? SelectedBrushPattern { get; set; }
        public ObservableCollection<BrushPatternItem> BrushPatterns { get; }
    }
}
