using RealmStudioShapeRenderingLib;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    internal class LabelEditSession
    {
        public int CaretIndex;

        public string Text { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }

        public FontStyleModel FontStyle { get; set; } = new();

        public SKColor FontColor { get; set; } = Color.FromArgb(61, 53, 30).ToSKColor();

        public bool HasOutline { get; set; }
        public float OutlineWidth { get; set; }
        public SKColor OutlineColor { get; set; }

        public bool HasGlow { get; set; }
        public float GlowStrength { get; set; }
        public SKColor GlowColor { get; set; }

        // path/curve data
        public SKPath? CurvePath { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
